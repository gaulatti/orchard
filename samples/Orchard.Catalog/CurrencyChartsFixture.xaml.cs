using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using ShapePath = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard.Catalog;

public sealed partial class CurrencyChartsFixture : UserControl
{
    public CurrencyChartsFixture() { InitializeComponent(); Specimen.Data = DefaultData(); }
    private static ObservableCollection<object?> DefaultData() => [Row("Harbor", 100d), Row("Reed", 220d), Row("Vale", 140d), Row("North", 310d)];
    private static Dictionary<string, object?> Row(string name, object? value) => new() { ["name"] = name, ["total"] = value };
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        Require(XamlRoot.RasterizationScale == 1 && new Windows.UI.ViewManagement.UISettings().TextScaleFactor == 1, "Frozen Chart checks require display/text scale 1.");
        var root = (Grid)XamlRoot.Content; var host = new Grid { Margin = new Thickness(20), Background = root.Background }; Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count); root.Children.Add(host);
        var before = new Button { Content = "Before chart", VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Right }; host.Children.Add(before);
        using var input = new NativeInputSession(XamlRoot, folder, $"currency-chart-{scheme}"); await input.AcquireAsync(before); await input.MoveAsync(before);
        using var reference = JsonDocument.Parse(typeof(CurrencyChartsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.ChartLayout.json")!);
        var reports = new List<object>(); var failures = new List<string>();
        try
        {
            var owner = new BleeckerChart { Width = 640, Height = 300, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top }; AutomationProperties.SetName(owner, "Consumer currency totals");
            await MountAsync(host, owner); owner.ApplyTemplate(); host.UpdateLayout(); await FrameAsync(); host.UpdateLayout();
            var inner = Descendants<BleeckerLineChart>(owner).Single(); var plot = Descendants<Canvas>(owner).Single(c => c.Name == "PART_Plot"); var active = Descendants<Canvas>(owner).Single(c => c.Name == "PART_Active");
            var popup = Descendants<Border>(owner).Single(c => c.Name == "PART_Tooltip"); var items = Descendants<StackPanel>(owner).Single(c => c.Name == "PART_TooltipItems"); var container = Descendants<Grid>(owner).Single(c => c.Name == "PART_TooltipContainer");
            await FocusAsync(inner); host.UpdateLayout();
            foreach (var sample in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("scheme").GetString() == scheme.ToString()))
            {
                var clock = Stopwatch.StartNew();
                var id = sample.GetProperty("id").GetString()!; var source = sample.GetProperty("normal"); var data = new ObservableCollection<object?>((object?[])Read(sample.GetProperty("props").GetProperty("data"))!);
                owner.Width = source.GetProperty("width").GetDouble(); owner.Height = source.GetProperty("height").GetDouble(); owner.Data = data; host.UpdateLayout(); await FrameAsync(); host.UpdateLayout();
                var errors = new List<string>(); void Check(string boundary, Action action) { try { action(); } catch (InvalidOperationException error) { errors.Add(boundary + ": " + error.Message); } }
                var mountedMilliseconds = clock.Elapsed.TotalMilliseconds;
                Check("ownership", () => { var peer = FrameworkElementAutomationPeer.CreatePeerForElement(inner); Require(ReferenceEquals(owner.Data, data) && !owner.IsTabStop && inner.IsTabStop && peer.GetClassName() == nameof(BleeckerChart) && peer.GetName() == "Consumer currency totals" && peer.GetAutomationControlType() == AutomationControlType.Group, "Chart lost original data or its single named keyboard entry."); });
                Check("curve", () => CompareCurve(plot, source)); Check("axes", () => CompareAxes(plot, source)); Check("grid", () => CompareGrid(plot, source));
                Require(Descendants<Canvas>(owner).Single(c => c.Name == "PART_Legend").Children.Count == 0, "Chart invented a legend.");
                // These comparisons inspect live XAML geometry and arranged
                // properties after actual focus/key event receipts.
                // Physical pointer gates and actual captures retain their
                // independent two-frame presentation/commit checks.
                var firstState = sample.GetProperty("states")[0].GetProperty("state"); Check("inspection", () => CompareInspection(active, popup, items, container, firstState));
                var inspectedMilliseconds = clock.Elapsed.TotalMilliseconds;
                if (id is "default" or "partial-names" or "duplicate-partial" or "no-names")
                {
                    foreach (var state in sample.GetProperty("states").EnumerateArray().Skip(1).Take(4))
                    {
                        var key = state.GetProperty("action").GetString(); await input.KeyAsync(key == "ArrowRight" ? (ushort)0x27 : key == "ArrowLeft" ? (ushort)0x25 : (ushort)0x0D, inner); host.UpdateLayout(); Check(key!, () => CompareInspection(active, popup, items, container, state.GetProperty("state"), comparePlacement: false));
                    }
                    await input.MoveAsync(inner, localPoint: new Point(61, 84)); host.UpdateLayout(); Require(popup.Visibility == Visibility.Visible, "Physical pointer did not enter Chart inspection."); await input.MoveAsync(before);
                    // Restore index zero through real keyboard input before the
                    // next source data replacement on this mounted consumer.
                    await input.KeyAsync(id is "no-names" or "partial-names" ? (ushort)0x0D : (ushort)0x25, inner); host.UpdateLayout(); Check("reset", () => CompareInspection(active, popup, items, container, firstState, comparePlacement: false));
                }
                if (id is "default" or "negative" or "missing" or "narrow" or "short" or "multilingual") { before.Focus(FocusState.Keyboard); await MainWindow.AwaitPresentationAsync(owner); await MainWindow.CaptureElementAsync(root, System.IO.Path.Combine(folder, $"currency-chart-{id}-{scheme}.png")); await FocusAsync(inner); await input.KeyAsync(0x0D, inner); host.UpdateLayout(); }
                reports.Add(new { id, errors, mountedMilliseconds, inspectedMilliseconds, elapsedMilliseconds = clock.Elapsed.TotalMilliseconds }); failures.AddRange(errors.Select(error => id + ": " + error));
            }
            host.Children.Remove(owner);
            await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"currency-chart-{scheme}.json"), JsonSerializer.Serialize(new { passed = failures.Count == 0, reports }, new JsonSerializerOptions { WriteIndented = true }));
            Require(failures.Count == 0, "Chart mismatches: " + string.Join(" | ", failures)); await VerifyLifecycleAsync(host, before, input);
        }
        finally { root.Children.Remove(host); }
    }
    private static void CompareCurve(Canvas plot, JsonElement source)
    {
        var actual = plot.Children.OfType<ShapePath>().ToArray(); var expected = source.GetProperty("curves").EnumerateArray().ToArray(); Require(actual.Length == expected.Length, $"curve count {actual.Length}/{expected.Length}");
        for (var i = 0; i < actual.Length; i++)
        {
            var commands = new List<double>(); foreach (var figure in ((PathGeometry)actual[i].Data).Figures) { commands.AddRange([figure.StartPoint.X, figure.StartPoint.Y]); foreach (var segment in figure.Segments) if (segment is BezierSegment cubic) commands.AddRange([cubic.Point1.X, cubic.Point1.Y, cubic.Point2.X, cubic.Point2.Y, cubic.Point3.X, cubic.Point3.Y]); else if (segment is LineSegment line) commands.AddRange([line.Point.X, line.Point.Y]); else throw new InvalidOperationException("Unknown curve segment."); }
            var values = expected[i].GetProperty("attributes").TryGetProperty("d", out var d) ? Numbers(d.GetString()!) : []; Require(commands.Count == values.Length, "Curve command structure differs."); for (var j = 0; j < values.Length; j++) Near(commands[j], values[j]); ComparePaint(actual[i].Stroke, expected[i].GetProperty("stroke").GetString()!); Near(actual[i].StrokeThickness, 2);
        }
    }
    private static void CompareAxes(Canvas plot, JsonElement source)
    {
        var actual = plot.Children.OfType<TextBlock>().ToList(); var expected = source.GetProperty("xTicks").EnumerateArray().Concat(source.GetProperty("yTicks").EnumerateArray()).ToArray(); Require(actual.Count == expected.Length, $"tick count {actual.Count}/{expected.Length}");
        foreach (var tick in expected)
        {
            var text = tick.GetProperty("text").GetString()!; var attrs = tick.GetProperty("attributes"); var label = actual.Where(label => label.Text == text).MinBy(label => Math.Abs(Canvas.GetLeft(label) + label.DesiredSize.Width * (attrs.GetProperty("text-anchor").GetString() == "middle" ? .5 : 1) - Number(attrs, "x"))) ?? throw new InvalidOperationException("Missing tick " + text); actual.Remove(label);
            var anchor = attrs.GetProperty("text-anchor").GetString(); var x = Canvas.GetLeft(label) + label.DesiredSize.Width * (anchor == "middle" ? .5 : 1); Near(x, Number(attrs, "x")); Near(Canvas.GetTop(label) + label.BaselineOffset, Number(attrs, "y") + (anchor == "middle" ? 8.52 : 4.26)); Near(label.FontSize, 12); ComparePaint(label.Foreground, tick.GetProperty("fill").GetString()!);
        }
    }
    private static void CompareGrid(Canvas plot, JsonElement source)
    {
        var actual = plot.Children.OfType<Line>().OrderBy(line => line.Y1).ToArray(); var expected = source.GetProperty("grid").EnumerateArray().OrderBy(line => Number(line.GetProperty("attributes"), "y1")).ToArray(); Require(actual.Length == expected.Length, $"grid count {actual.Length}/{expected.Length}");
        for (var i = 0; i < actual.Length; i++) { var attrs = expected[i].GetProperty("attributes"); Near(actual[i].X1, Number(attrs, "x1")); Near(actual[i].X2, Number(attrs, "x2")); Near(actual[i].Y1, Number(attrs, "y1")); Near(actual[i].Y2, Number(attrs, "y2")); Require(actual[i].StrokeDashArray.SequenceEqual(new[] { 3d, 3d }), "Grid lost source dashes."); ComparePaint(actual[i].Stroke, expected[i].GetProperty("stroke").GetString()!); }
    }
    private static void CompareInspection(Canvas active, Border popup, StackPanel items, Grid container, JsonElement source, bool comparePlacement = true)
    {
        var tip = source.GetProperty("tooltip"); var visible = tip.GetProperty("visible").GetBoolean(); Require((popup.Visibility == Visibility.Visible) == visible, "Tooltip visibility differs.");
        Require(string.Concat(items.Children.OfType<TextBlock>().Select(t => t.Text)) == tip.GetProperty("text").GetString(), "Tooltip text differs.");
        var dots = active.Children.OfType<Ellipse>().ToArray(); var expected = source.GetProperty("dots").EnumerateArray().ToArray(); Require(dots.Length == expected.Length, "Active dot count differs.");
        for (var i = 0; i < dots.Length; i++) { var attrs = expected[i].GetProperty("attributes"); Near(dots[i].Width, 2 * Number(attrs, "r") + 2); Near(dots[i].Height, 2 * Number(attrs, "r") + 2); Near(Canvas.GetLeft(dots[i]) + dots[i].Width / 2, Number(attrs, "cx")); Near(Canvas.GetTop(dots[i]) + dots[i].Height / 2, Number(attrs, "cy")); Near(dots[i].StrokeThickness, 2); ComparePaint(dots[i].Fill, expected[i].GetProperty("fill").GetString()!); ComparePaint(dots[i].Stroke, expected[i].GetProperty("stroke").GetString()!); }
        var cursors = active.Children.OfType<Line>().ToArray(); var sourceCursors = source.GetProperty("cursor").EnumerateArray().ToArray(); Require(cursors.Length == sourceCursors.Length, "Cursor count differs.");
        for (var i = 0; i < cursors.Length; i++) { var coordinates = Numbers(sourceCursors[i].GetProperty("attributes").GetProperty("d").GetString()!); Near(cursors[i].X1, coordinates[0]); Near(cursors[i].Y1, coordinates[1]); Near(cursors[i].X2, coordinates[2]); Near(cursors[i].Y2, coordinates[3]); Near(cursors[i].StrokeThickness, 1); ComparePaint(cursors[i].Stroke, sourceCursors[i].GetProperty("stroke").GetString()!); }
        if (visible && comparePlacement) { var box = tip.GetProperty("box"); Near(popup.ActualWidth, box.GetProperty("width").GetDouble()); Near(popup.ActualHeight, box.GetProperty("height").GetDouble()); Near(container.Margin.Left, box.GetProperty("x").GetDouble()); Near(container.Margin.Top, box.GetProperty("y").GetDouble()); }
    }
    private async Task VerifyLifecycleAsync(Grid host, Button before, NativeInputSession input)
    {
        var record = new ObservableRecord(); var records = new ObservableCollection<object?> { record, Row("B", 20d) }; var owner = new BleeckerChart { Width = 320, Height = 200, Data = records, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Top }; var after = new Button { Content = "After chart", HorizontalAlignment = HorizontalAlignment.Left }; host.Children.Clear(); stack.Children.Add(before); stack.Children.Add(owner); stack.Children.Add(after); host.Children.Add(stack);
        await AwaitAsync(() => owner.IsLoaded && after.IsLoaded, "Lifecycle Chart did not mount."); owner.ApplyTemplate(); UpdateLayout(); await MainWindow.AwaitPresentationAsync(owner);
        var inner = Descendants<BleeckerLineChart>(owner).Single(); var plot = Descendants<Canvas>(owner).Single(c => c.Name == "PART_Plot"); var popup = Descendants<Border>(owner).Single(c => c.Name == "PART_Tooltip"); var items = Descendants<StackPanel>(owner).Single(c => c.Name == "PART_TooltipItems");
        Require(before.Focus(FocusState.Keyboard), "Before Chart refused focus."); input.Key(0x09);
        await AwaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), inner), "Physical Tab did not enter the Chart's single inspection stop."); record["name"] = "Updated"; record["total"] = "10.00";
        await AwaitAsync(() => string.Concat(items.Children.OfType<TextBlock>().Select(t => t.Text)) == "UpdatedTotal : $10.00", "Original record notifications did not reach inspection.");
        input.Key(0x09);
        // Native focus can move before the routed blur updates inspection. A
        // reverse Tab issued in that gap can correctly suppress the stale blur.
        // Observe the complete departure before testing re-entry and Enter.
        await AwaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), after) && popup.Visibility == Visibility.Collapsed && items.Children.Count == 0, "Physical Tab did not finish leaving Chart inspection.");
        input.ShiftKey(0x09); await AwaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), inner), "Reverse Tab skipped Chart inspection."); await input.KeyAsync(0x0D, inner);
        await AwaitAsync(() => popup.Visibility == Visibility.Visible && string.Concat(items.Children.OfType<TextBlock>().Select(t => t.Text)) == "UpdatedTotal : $10.00", "Enter did not reactivate Chart inspection before collection changes.");
        records.Move(0, 1); await AwaitAsync(() => items.Children.OfType<TextBlock>().FirstOrDefault()?.Text == "B", "Record reorder did not reach active inspection."); records.RemoveAt(0); await AwaitAsync(() => items.Children.OfType<TextBlock>().FirstOrDefault()?.Text == "Updated", "Record removal did not reach inspection.");
        var replacement = DefaultData(); owner.Data = replacement; await AwaitAsync(() => items.Children.OfType<TextBlock>().FirstOrDefault()?.Text == "Harbor", "Root collection replacement did not reach inspection."); Require(record.Subscribers == 0, "Old records retained subscriptions.");
        replacement.Clear(); await AwaitAsync(() => !plot.Children.OfType<ShapePath>().Any() && popup.Visibility == Visibility.Collapsed && !items.Children.Any(), "Empty reset retained live output.");
        replacement.Add(record); replacement.Add(record); await AwaitAsync(() => plot.Children.OfType<ShapePath>().Any(), "Original append did not reach the plot."); Require(record.Subscribers == 1 && ReferenceEquals(replacement[0], replacement[1]), "Shared original records were cloned or subscribed twice."); replacement.Clear();
        owner.Width = 240; owner.FontFamily = new FontFamily("Segoe UI"); owner.Data = DefaultData(); await AwaitAsync(() => plot.Children.OfType<Line>().All(line => line.X2 == 220) && plot.Children.OfType<TextBlock>().All(text => text.FontFamily.Source == "Segoe UI"), "Resize/font changes did not reach chart consumers."); owner.Width = 320;
        owner.Data = records; await AwaitAsync(() => record.Subscribers == 1, "Original collection did not reconnect."); before.Focus(FocusState.Keyboard); stack.Children.Remove(owner); await AwaitAsync(() => !owner.IsLoaded && record.Subscribers == 0, "Detached Chart retained observers."); stack.Children.Insert(1, owner); await AwaitAsync(() => owner.IsLoaded && record.Subscribers == 1, "Remounted Chart did not reconnect.");
        stack.Children.Remove(owner); stack.Children.Insert(1, owner); await MainWindow.AwaitPresentationAsync(owner); Require(record.Subscribers == 1, "Rapid reparent duplicated or lost record observers."); var template = owner.Template; owner.Template = null; owner.ApplyTemplate(); owner.Template = template; owner.ApplyTemplate(); UpdateLayout(); await MainWindow.AwaitPresentationAsync(owner); Require(record.Subscribers == 1, "Template replacement retained obsolete observers.");
        owner.IsEnabled = false; Require(!Descendants<BleeckerLineChart>(owner).Single().Focus(FocusState.Keyboard), "Disabled Chart accepted keyboard focus."); stack.Children.Remove(owner); await AwaitAsync(() => record.Subscribers == 0, "Lifecycle cleanup retained records.");
    }
    private sealed class ObservableRecord : Dictionary<string, object?>, INotifyPropertyChanged
    {
        private PropertyChangedEventHandler? handlers; internal int Subscribers { get; private set; }
        public event PropertyChangedEventHandler? PropertyChanged { add { handlers += value; Subscribers++; } remove { handlers -= value; Subscribers--; } }
        public ObservableRecord() { base["name"] = "A"; base["total"] = 10d; }
        public new object? this[string key] { get => base[key]; set { base[key] = value; handlers?.Invoke(this, new PropertyChangedEventArgs(key)); } }
    }
    private async Task AwaitAsync(Func<bool> condition, string message) { var watch = Stopwatch.StartNew(); var readiness = new NativeReadiness(DispatcherQueue); while (!condition() && watch.Elapsed < TimeSpan.FromSeconds(2)) { UpdateLayout(); if (condition()) break; await readiness.WaitAsync(); } Require(condition(), message); }
    private static async Task MountAsync(Panel host, FrameworkElement element)
    {
        var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); void Loaded(object sender, RoutedEventArgs args) => loaded.TrySetResult();
        element.Loaded += Loaded;
        try { host.Children.Insert(0, element); await loaded.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
        finally { element.Loaded -= Loaded; }
    }
    private static async Task FocusAsync(Control element)
    {
        var focused = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); void Focused(object sender, RoutedEventArgs args) { if (ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(element.XamlRoot), element)) focused.TrySetResult(); }
        element.GotFocus += Focused;
        try { Require(element.Focus(FocusState.Keyboard), "Chart refused keyboard focus."); await focused.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
        finally { element.GotFocus -= Focused; }
    }
    private static async Task FrameAsync() { var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); void Frame(object? sender, object args) => ready.TrySetResult(); CompositionTarget.Rendering += Frame; try { await ready.Task.WaitAsync(TimeSpan.FromSeconds(2)); } finally { CompositionTarget.Rendering -= Frame; } }
    private static IEnumerable<T> Descendants<T>(DependencyObject owner) where T : DependencyObject { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++) { var child = VisualTreeHelper.GetChild(owner, i); if (child is T found) yield return found; foreach (var descendant in Descendants<T>(child)) yield return descendant; } }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Near(double actual, double expected) => Require(Math.Abs(actual - expected) < .001, $"{actual:R}/{expected:R}");
    private static double Number(JsonElement attrs, string key) => double.Parse(attrs.GetProperty(key).GetString()!, CultureInfo.InvariantCulture);
    private static double[] Numbers(string value) => Regex.Matches(value, @"[-+]?(?:\d*\.)?\d+(?:[eE][-+]?\d+)?").Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
    private static void ComparePaint(Brush brush, string source) { var c = ((SolidColorBrush)brush).Color; var values = Numbers(source); Require(c.R == values[0] && c.G == values[1] && c.B == values[2] && Math.Round(c.A * brush.Opacity) == Math.Round((values.Length == 3 ? 1 : values[3]) * 255), "Chart paint differs."); }
    private static object? Read(JsonElement value) => value.ValueKind switch { JsonValueKind.Object => value.EnumerateObject().ToDictionary(p => p.Name, p => Read(p.Value)), JsonValueKind.Array => value.EnumerateArray().Select(Read).ToArray(), JsonValueKind.Number => value.GetDouble(), JsonValueKind.String => value.GetString(), JsonValueKind.Null => null, JsonValueKind.True => true, JsonValueKind.False => false, _ => throw new InvalidOperationException("Unknown Chart source value.") };
}
