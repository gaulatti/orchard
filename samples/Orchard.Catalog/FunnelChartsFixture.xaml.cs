using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard.Catalog;

public sealed partial class FunnelChartsFixture : UserControl
{
    public FunnelChartsFixture() => InitializeComponent();
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        Require(XamlRoot.RasterizationScale == 1 && new Windows.UI.ViewManagement.UISettings().TextScaleFactor == 1, "Frozen Funnel checks require display/text scale 1.");
        using var input = new NativeInputSession(XamlRoot, folder, $"funnel-chart-{scheme}"); await input.AcquireAsync(Specimen);
        var root = CatalogFixtureRoot.From(XamlRoot); var host = new Grid { Margin = new Thickness(20), Background = root.Background }; Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count); root.Children.Add(host);
        using var reference = JsonDocument.Parse(CatalogReferenceResources.Open(typeof(FunnelChartsFixture).Assembly, "Orchard.Catalog.Reference.FunnelChartLayout.json")!);
        var reports = new List<object>(); var failures = new List<string>();
        try
        {
            await input.MoveAsync(host, localPoint: new Point(850, 600));
            foreach (var sample in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("scheme").GetString() == scheme.ToString()))
            {
                var id = sample.GetProperty("id").GetString()!; var props = sample.GetProperty("props"); var expected = sample.GetProperty("normal");
                var data = new ObservableCollection<IReadOnlyDictionary<string, object?>>(props.GetProperty("data").EnumerateArray().Select(row => (IReadOnlyDictionary<string, object?>)Read(row)!));
                var owner = new BleeckerFunnelChart { Data = data, Width = expected.GetProperty("width").GetDouble(), Height = expected.GetProperty("height").GetDouble(), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                if (props.TryGetProperty("showLabels", out var show)) owner.ShowLabels = show.GetBoolean();
                if (props.TryGetProperty("formatter", out _)) owner.ValueFormatter = value => "Consumer " + value.ToString("F2", CultureInfo.InvariantCulture);
                AutomationProperties.SetName(owner, "Consumer funnel comparison"); host.Children.Clear(); host.Children.Add(owner); await AwaitAsync(() => owner.IsLoaded, "Funnel did not mount."); owner.ApplyTemplate(); UpdateLayout(); await FrameAsync(); UpdateLayout();
                var chart = Descendants<BleeckerLineChart>(owner).Single(); var plot = Part<Canvas>(chart, "PART_Plot"); var errors = new List<string>();
                void Check(string boundary, Action action) { try { action(); } catch (InvalidOperationException error) { errors.Add(boundary + ": " + error.Message); } }
                Check("ownership", () => Require(ReferenceEquals(chart.Data, data) && !owner.IsTabStop && chart.IsTabStop && AutomationProperties.GetName(chart) == "Consumer funnel comparison" && FrameworkElementAutomationPeer.CreatePeerForElement(chart).GetClassName() == nameof(BleeckerFunnelChart), "Funnel lost original records or its single named inspection stop."));
                Check("paths", () => ComparePaths(plot, expected.GetProperty("paths")));
                Check("labels", () => CompareLabels(plot, expected.GetProperty("labels")));
                if (new[] { "default", "narrow", "multilingual", "increasing", "long-name", "short" }.Contains(id)) await MainWindow.CaptureElementAsync(root, System.IO.Path.Combine(folder, $"funnel-chart-{id}-{scheme}.png"));
                foreach (var state in sample.GetProperty("states").EnumerateArray())
                {
                    var action = state.GetProperty("action").GetString()!;
                    if (action == "focus") Require(chart.Focus(FocusState.Keyboard), "Funnel refused keyboard focus.");
                    else if (action.StartsWith("Arrow", StringComparison.Ordinal) || action == "Enter") await input.KeyAsync(action switch { "ArrowRight" => 0x27, "ArrowLeft" => 0x25, "ArrowDown" => 0x28, "ArrowUp" => 0x26, "Enter" => 0x0D, _ => throw new InvalidOperationException("Unknown source key.") }, chart);
                    else if (action == "hover") { var target = state.GetProperty("target"); await input.MoveAsync(chart, localPoint: new Point(target.GetProperty("x").GetDouble(), target.GetProperty("y").GetDouble())); }
                    else if (action == "outside") await input.MoveAsync(host, localPoint: new Point(850, 600));
                    else throw new InvalidOperationException("Unknown source Funnel action.");
                    var value = state.GetProperty("state"); await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == value.GetProperty("tooltip").GetString(), $"Funnel {id}/{action} payload differs: {AutomationProperties.GetHelpText(chart)}");
                    if (value.GetProperty("tooltipVisible").GetBoolean()) await AwaitAsync(() => Math.Abs(Part<Border>(chart, "PART_Tooltip").ActualHeight - value.GetProperty("tooltipBox").GetProperty("height").GetDouble()) < .001 && Math.Abs(Part<Border>(chart, "PART_Tooltip").ActualWidth - Part<Border>(chart, "PART_Tooltip").Width) < .001, "Funnel tooltip did not arrange.");
                    Check(action, () => CompareInspection(chart, value));
                }
                reports.Add(new { id, errors }); failures.AddRange(errors.Select(error => id + ": " + error));
                await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"funnel-chart-{scheme}.json"), JsonSerializer.Serialize(new { passed = failures.Count == 0, reports }, new JsonSerializerOptions { WriteIndented = true }));
            }
            Require(failures.Count == 0, "Funnel mismatches: " + string.Join(" | ", failures));
            await VerifyMutationAsync(host, input, folder, scheme);
        }
        finally { root.Children.Remove(host); }
    }
    private static void ComparePaths(Canvas plot, JsonElement source)
    {
        var actual = plot.Children.OfType<Path>().Where(path => path.Tag is ValueTuple<string, int> tag && tag.Item1 == "funnel").ToArray();
        var expected = source.EnumerateArray().Where(path => !Regex.IsMatch(path.GetProperty("attributes").GetProperty("d").GetString()!, "NaN|Infinity")).ToArray(); Require(actual.Length == expected.Length, $"paintable path count {actual.Length}/{expected.Length}");
        for (var i = 0; i < actual.Length; i++)
        {
            var geometry = ((PathGeometry)actual[i].Data).Figures.Single(); Require(geometry.IsClosed, "Funnel path is open.");
            var numbers = Numbers(expected[i].GetProperty("attributes").GetProperty("d").GetString()!); var points = new[] { geometry.StartPoint }.Concat(geometry.Segments.Cast<LineSegment>().Select(line => line.Point)).ToArray(); Require(numbers.Length == points.Length * 2, "Funnel path segments differ.");
            for (var j = 0; j < points.Length; j++) { Near(points[j].X, numbers[j * 2]); Near(points[j].Y, numbers[j * 2 + 1]); }
            ComparePaint(actual[i].Fill, expected[i].GetProperty("fill").GetString()!); ComparePaint(actual[i].Stroke, expected[i].GetProperty("stroke").GetString()!); Near(actual[i].StrokeThickness, 1);
        }
    }
    private static void CompareLabels(Canvas plot, JsonElement source)
    {
        var actual = plot.Children.OfType<TextBlock>().Where(label => label.Tag is ValueTuple<string, int, int> tag && tag.Item1 == "funnel-label").GroupBy(label => ((ValueTuple<string, int, int>)label.Tag).Item2).ToArray(); var expected = source.EnumerateArray().ToArray(); Require(actual.Length == expected.Length, $"label count {actual.Length}/{expected.Length}");
        for (var i = 0; i < actual.Length; i++)
        {
            var labels = actual[i].ToArray(); var spans = expected[i].GetProperty("spans").EnumerateArray().ToArray(); Require(labels.Length == spans.Length, "Funnel label wrapping differs.");
            var baseline = Number(expected[i].GetProperty("attributes").GetProperty("y").GetString()!); var x = Number(expected[i].GetProperty("attributes").GetProperty("x").GetString()!);
            for (var line = 0; line < labels.Length; line++)
            {
                var dy = spans[line].GetProperty("attributes").GetProperty("dy").GetString()!; baseline += dy.EndsWith("em", StringComparison.Ordinal) ? Number(dy[..^2]) * 16 : Number(dy);
                Require(labels[line].Text == spans[line].GetProperty("text").GetString(), "Funnel label text differs."); Near(Canvas.GetLeft(labels[line]) + labels[line].DesiredSize.Width / 2, x); Near(Canvas.GetTop(labels[line]) + labels[line].BaselineOffset, baseline); ComparePaint(labels[line].Foreground, expected[i].GetProperty("fill").GetString()!);
            }
        }
    }
    private static void CompareInspection(BleeckerLineChart chart, JsonElement source)
    {
        var shown = source.GetProperty("tooltipVisible").GetBoolean(); var tooltip = Part<Border>(chart, "PART_Tooltip"); Require((tooltip.Visibility == Visibility.Visible) == shown, "Funnel inspection visibility differs"); Require(Part<Canvas>(chart, "PART_Active").Children.Count == source.GetProperty("cursor").GetArrayLength(), "Funnel cursor differs"); if (!shown) return;
        var rows = source.GetProperty("tooltipLines").EnumerateArray().Where(row => row.GetProperty("text").GetString()!.Length > 0).ToArray(); Require(Part<StackPanel>(chart, "PART_TooltipItems").Children.OfType<TextBlock>().Select(row => row.Text).SequenceEqual(rows.Select(row => row.GetProperty("rendered").GetString())), "Funnel rendered inspection text differs");
        var box = source.GetProperty("tooltipBox"); var margin = Part<Grid>(chart, "PART_TooltipContainer").Margin; Near(margin.Left, box.GetProperty("x").GetDouble()); Near(margin.Top, box.GetProperty("y").GetDouble()); Near(tooltip.ActualWidth, box.GetProperty("width").GetDouble()); Near(tooltip.ActualHeight, box.GetProperty("height").GetDouble());
    }
    private static void ComparePaint(Brush brush, string source) { var c = ((SolidColorBrush)brush).Color; var values = Numbers(source); Require(c.R == values[0] && c.G == values[1] && c.B == values[2] && Math.Round(c.A * brush.Opacity) == Math.Round((values.Length == 3 ? 1 : values[3]) * 255), "source paint differs"); }
    private static object? Read(JsonElement value) => value.ValueKind switch { JsonValueKind.Object => value.EnumerateObject().ToDictionary(p => p.Name, p => p.Name == "fill" && p.Value.ValueKind == JsonValueKind.String ? new SolidColorBrush(Paint(p.Value.GetString()!)) : Read(p.Value)), JsonValueKind.Array => value.EnumerateArray().Select(Read).ToArray(), JsonValueKind.Number => value.GetDouble(), JsonValueKind.String => value.GetString(), JsonValueKind.True => true, JsonValueKind.False => false, JsonValueKind.Null => null, _ => throw new InvalidOperationException("Unknown frozen Funnel value.") };
    private static Windows.UI.Color Paint(string value) => value == "transparent" ? Windows.UI.Color.FromArgb(0, 0, 0, 0) : Windows.UI.Color.FromArgb(255, Convert.ToByte(value.Substring(1, 2), 16), Convert.ToByte(value.Substring(3, 2), 16), Convert.ToByte(value.Substring(5, 2), 16));
    private static double Number(string value) => double.Parse(value, CultureInfo.InvariantCulture);
    private static double[] Numbers(string value) => Regex.Matches(value, @"[-+]?(?:\d*\.)?\d+(?:[eE][-+]?\d+)?").Select(match => Number(match.Value)).ToArray();
    private static IEnumerable<T> Descendants<T>(DependencyObject owner) where T : DependencyObject { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++) { var child = VisualTreeHelper.GetChild(owner, i); if (child is T item) yield return item; foreach (var nested in Descendants<T>(child)) yield return nested; } }
    private static T Part<T>(BleeckerLineChart chart, string name) where T : DependencyObject => (T)((FrameworkElement)VisualTreeHelper.GetChild(chart, 0)).FindName(name);
    private async Task AwaitAsync(Func<bool> condition, string message) { var watch = Stopwatch.StartNew(); var readiness = new NativeReadiness(DispatcherQueue); while (!condition() && watch.Elapsed < TimeSpan.FromSeconds(2)) { UpdateLayout(); if (condition()) break; await readiness.WaitAsync(); } Require(condition(), message); }
    private static async Task FrameAsync() { var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); void Frame(object? sender, object args) => ready.TrySetResult(); CompositionTarget.Rendering += Frame; try { await ready.Task.WaitAsync(TimeSpan.FromSeconds(2)); } finally { CompositionTarget.Rendering -= Frame; } }
    private static void Near(double actual, double expected) => Require(Math.Abs(actual - expected) < .001, $"source coordinate {actual:R}/{expected:R}");
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
