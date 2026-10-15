using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
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

public sealed partial class RadialBarChartsFixture : UserControl
{
    public RadialBarChartsFixture() => InitializeComponent();
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        Require(XamlRoot.RasterizationScale == 1 && new Windows.UI.ViewManagement.UISettings().TextScaleFactor == 1, "Frozen Radial Bar checks require display/text scale 1.");
        using var input = new NativeInputSession(XamlRoot, folder, $"radial-bar-chart-{scheme}"); await input.AcquireAsync(Specimen);
        var root = (Grid)XamlRoot.Content; var host = new Grid { Margin = new Thickness(20), Background = root.Background }; Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count); root.Children.Add(host);
        using var reference = JsonDocument.Parse(typeof(RadialBarChartsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.RadialBarChartLayout.json")!);
        var reports = new List<object>(); var failures = new List<string>();
        try
        {
            await input.MoveAsync(host, localPoint: new Point(850, 600));
            foreach (var sample in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("scheme").GetString() == scheme.ToString()))
            {
                var id = sample.GetProperty("id").GetString()!; var props = sample.GetProperty("props"); var expected = sample.GetProperty("normal"); var data = new ObservableCollection<IReadOnlyDictionary<string, object?>>(props.GetProperty("data").EnumerateArray().Select(row => (IReadOnlyDictionary<string, object?>)Read(row)!));
                var owner = new BleeckerRadialBarChart { Data = data, Width = expected.GetProperty("width").GetDouble(), Height = expected.GetProperty("height").GetDouble(), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                if (props.TryGetProperty("showLegend", out var show)) owner.ShowLegend = show.GetBoolean();
                if (props.TryGetProperty("innerRadius", out var inner)) owner.InnerRadius = Read(inner)!;
                if (props.TryGetProperty("outerRadius", out var outer)) owner.OuterRadius = Read(outer)!;
                if (props.TryGetProperty("startAngle", out var start)) owner.StartAngle = start.GetDouble();
                if (props.TryGetProperty("endAngle", out var end)) owner.EndAngle = end.GetDouble();
                AutomationProperties.SetName(owner, "Consumer radial comparison"); host.Children.Clear(); host.Children.Add(owner); await AwaitAsync(() => owner.IsLoaded, "Radial Bar did not mount."); owner.ApplyTemplate(); UpdateLayout(); await FrameAsync(); UpdateLayout();
                var chart = Descendants<BleeckerLineChart>(owner).Single(); var plot = Part<Canvas>(chart, "PART_Plot"); var errors = new List<string>();
                void Check(string boundary, Action action) { try { action(); } catch (InvalidOperationException error) { errors.Add(boundary + ": " + error.Message); } }
                Check("ownership", () => Require(ReferenceEquals(chart.Data, data) && !owner.IsTabStop && chart.IsTabStop && AutomationProperties.GetName(chart) == "Consumer radial comparison" && FrameworkElementAutomationPeer.CreatePeerForElement(chart).GetClassName() == nameof(BleeckerRadialBarChart), "Radial Bar lost original data or its single named inspection boundary."));
                Check("foreground", () => CompareSectors(plot, "radial-foreground", expected.GetProperty("foreground")));
                Check("background", () => CompareSectors(plot, "radial-background", expected.GetProperty("background")));
                Check("labels", () => CompareLabels(chart, expected.GetProperty("labels")));
                Check("legend", () => CompareLegend(chart, expected));
                if (new[] { "default", "narrow", "reverse", "ranges", "multilingual", "partial" }.Contains(id)) await MainWindow.CaptureElementAsync(root, System.IO.Path.Combine(folder, $"radial-bar-chart-{id}-{scheme}.png"));
                foreach (var state in sample.GetProperty("states").EnumerateArray())
                {
                    var action = state.GetProperty("action").GetString()!;
                    if (action == "focus") Require(chart.Focus(FocusState.Keyboard), "Radial Bar refused keyboard focus.");
                    else if (action is "ArrowRight" or "ArrowLeft" or "Enter") await input.KeyAsync(action == "ArrowRight" ? (ushort)0x27 : action == "ArrowLeft" ? (ushort)0x25 : (ushort)0x0D, chart);
                    else if (action == "hover") { var target = state.GetProperty("target"); await input.MoveAsync(chart, localPoint: new Point(target.GetProperty("x").GetDouble(), target.GetProperty("y").GetDouble())); }
                    else if (action == "outside") await input.MoveAsync(host, localPoint: new Point(850, 600));
                    else throw new InvalidOperationException("Unknown source Radial Bar action.");
                    var value = state.GetProperty("state"); await AwaitAsync(() => Inspection(chart) == value.GetProperty("tooltip").GetString(), $"Radial Bar {id}/{action} payload differs: {Inspection(chart)}");
                    if (value.GetProperty("tooltipVisible").GetBoolean()) await AwaitAsync(() => Math.Abs(Part<Border>(chart, "PART_Tooltip").ActualHeight - value.GetProperty("tooltipBox").GetProperty("height").GetDouble()) < .001 && Math.Abs(Part<Border>(chart, "PART_Tooltip").ActualWidth - Part<Border>(chart, "PART_Tooltip").Width) < .001, "Radial Bar tooltip did not arrange.");
                    Check(action, () => CompareInspection(chart, value));
                }
                var glyphSha256 = NativeGlyphGeometry.Hash(plot.Children.OfType<Path>().Where(path => path.Tag is ValueTuple<string, int, int> tag && tag.Item1 == "radial-label-glyph").Select(path => (PathGeometry)path.Data));
                reports.Add(new { id, errors, glyphSha256 }); failures.AddRange(errors.Select(error => id + ": " + error));
                await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"radial-bar-chart-{scheme}.json"), JsonSerializer.Serialize(new { passed = failures.Count == 0, reports }, new JsonSerializerOptions { WriteIndented = true }));
            }
            Require(failures.Count == 0, "Radial Bar mismatches: " + string.Join(" | ", failures));
            await VerifyMutationAsync(host, input, folder, scheme);
        }
        finally { root.Children.Remove(host); }
    }
    private static void CompareSectors(Canvas plot, string role, JsonElement source)
    {
        var paths = plot.Children.OfType<Path>().Where(path => path.Tag is ValueTuple<string, int> tag && tag.Item1 == role).ToArray(); var expected = source.EnumerateArray().ToArray(); Require(paths.Length == expected.Length, $"{role} count {paths.Length}/{expected.Length}");
        for (var i = 0; i < paths.Length; i++) { ComparePath(paths[i].Data, expected[i].GetProperty("attributes").GetProperty("d").GetString()!); ComparePaint(paths[i].Fill, expected[i].GetProperty("fill").GetString()!); }
    }
    private static void CompareLabels(BleeckerLineChart chart, JsonElement source)
    {
        var labels = ((IEnumerable)typeof(BleeckerLineChart).GetField("radialLabels", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(chart)!).Cast<object>().ToArray(); var expected = source.EnumerateArray().ToArray(); Require(labels.Length == expected.Length, $"label count {labels.Length}/{expected.Length}");
        for (var i = 0; i < labels.Length; i++)
        {
            Require(Get<string>(labels[i], "Text") == expected[i].GetProperty("text").GetString(), "curved label text differs");
            ComparePath(Parse(Get<string>(labels[i], "Path")), expected[i].GetProperty("path").GetString()!);
            var glyphs = ((IEnumerable)Get<object>(labels[i], "Glyphs")).Cast<object>().ToArray(); var characters = expected[i].GetProperty("characters").EnumerateArray().ToArray(); Require(glyphs.Length == characters.Length, $"curved glyph count {glyphs.Length}/{characters.Length}");
            for (var j = 0; j < glyphs.Length; j++)
            {
                if (characters[j].TryGetProperty("error", out _)) continue;
                var start = Get<Point>(glyphs[j], "Start"); var end = Get<Point>(glyphs[j], "End"); var a = characters[j].GetProperty("start"); var b = characters[j].GetProperty("end");
                Near(start.X, a.GetProperty("x").GetDouble()); Near(start.Y, a.GetProperty("y").GetDouble()); Near(end.X, b.GetProperty("x").GetDouble()); Near(end.Y, b.GetProperty("y").GetDouble()); Near(Get<double>(glyphs[j], "Rotation"), characters[j].GetProperty("rotation").GetDouble());
            }
        }
    }
    private static void CompareLegend(BleeckerLineChart chart, JsonElement source)
    {
        var labels = Part<Canvas>(chart, "PART_Legend").Children.OfType<Canvas>().SelectMany(canvas => canvas.Children.OfType<TextBlock>()).ToArray(); var expected = source.GetProperty("legend").EnumerateArray().ToArray(); Require(labels.Select(label => label.Text).SequenceEqual(expected.Select(label => label.GetProperty("text").GetString())), "legend labels differ");
        for (var i = 0; i < labels.Length; i++) { var parent = (Canvas)VisualTreeHelper.GetParent(labels[i]); Near(Canvas.GetLeft(parent) + Canvas.GetLeft(labels[i]), expected[i].GetProperty("box").GetProperty("x").GetDouble()); Near(Canvas.GetTop(parent) + Canvas.GetTop(labels[i]) + labels[i].BaselineOffset, expected[i].GetProperty("box").GetProperty("y").GetDouble() + 16); ComparePaint(labels[i].Foreground, expected[i].GetProperty("color").GetString()!); }
    }
    private static void CompareInspection(BleeckerLineChart chart, JsonElement source)
    {
        var shown = source.GetProperty("tooltipVisible").GetBoolean(); Require((Part<Border>(chart, "PART_Tooltip").Visibility == Visibility.Visible) == shown, "inspection visibility differs");
        var cursors = Part<Canvas>(chart, "PART_Active").Children.OfType<Path>().ToArray(); var expected = source.GetProperty("cursor").EnumerateArray().ToArray(); Require(cursors.Length == expected.Length, "cursor count differs"); for (var i = 0; i < cursors.Length; i++) ComparePath(cursors[i].Data, expected[i].GetProperty("attributes").GetProperty("d").GetString()!);
        if (!shown) return;
        var lines = source.GetProperty("tooltipLines").EnumerateArray().Where(line => !string.IsNullOrEmpty(line.GetProperty("text").GetString())).ToArray(); Require(Part<StackPanel>(chart, "PART_TooltipItems").Children.OfType<TextBlock>().Select(text => text.Text).SequenceEqual(lines.Select(line => line.GetProperty("rendered").GetString())), "rendered tooltip rows differ");
        var box = source.GetProperty("tooltipBox"); var margin = Part<Grid>(chart, "PART_TooltipContainer").Margin; Near(margin.Left, box.GetProperty("x").GetDouble()); Near(margin.Top, box.GetProperty("y").GetDouble()); Near(Part<Border>(chart, "PART_Tooltip").ActualWidth, box.GetProperty("width").GetDouble()); Near(Part<Border>(chart, "PART_Tooltip").ActualHeight, box.GetProperty("height").GetDouble());
    }
    private static Geometry Parse(string path) => ((Path)XamlReader.Load("<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Data='" + path + "' />")).Data;
    private static void ComparePath(Geometry actual, string source)
    {
        var expected = (PathGeometry)Parse(source); var geometry = (PathGeometry)actual; Require(geometry.Figures.Count == expected.Figures.Count, "path figure count differs");
        for (var i = 0; i < geometry.Figures.Count; i++)
        {
            var a = geometry.Figures[i]; var e = expected.Figures[i]; Near(a.StartPoint.X, e.StartPoint.X); Near(a.StartPoint.Y, e.StartPoint.Y); Require(a.IsClosed == e.IsClosed && a.Segments.Count == e.Segments.Count, "path closure/segments differ");
            for (var j = 0; j < a.Segments.Count; j++)
            {
                if (a.Segments[j] is LineSegment line && e.Segments[j] is LineSegment other) { Near(line.Point.X, other.Point.X); Near(line.Point.Y, other.Point.Y); }
                else if (a.Segments[j] is ArcSegment arc && e.Segments[j] is ArcSegment otherArc) { Near(arc.Point.X, otherArc.Point.X); Near(arc.Point.Y, otherArc.Point.Y); Near(arc.Size.Width, otherArc.Size.Width); Near(arc.Size.Height, otherArc.Size.Height); Require(arc.IsLargeArc == otherArc.IsLargeArc && arc.SweepDirection == otherArc.SweepDirection, "arc flags differ"); }
                else throw new InvalidOperationException("Unknown source sector segment.");
            }
        }
    }
    private static void ComparePaint(Brush brush, string source) { var c = ((SolidColorBrush)brush).Color; var values = Regex.Matches(source, @"[\d.]+").Select(m => double.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray(); Require(c.R == values[0] && c.G == values[1] && c.B == values[2] && Math.Round(c.A * brush.Opacity) == Math.Round((values.Length == 3 ? 1 : values[3]) * 255), "source paint differs"); }
    private static object? Read(JsonElement value) => value.ValueKind switch { JsonValueKind.Object => value.EnumerateObject().ToDictionary(p => p.Name, p => p.Name == "fill" && p.Value.ValueKind == JsonValueKind.String ? new SolidColorBrush(Paint(p.Value.GetString()!)) : Read(p.Value)), JsonValueKind.Array => value.EnumerateArray().Select(Read).ToArray(), JsonValueKind.Number => value.GetDouble(), JsonValueKind.String => value.GetString(), JsonValueKind.True => true, JsonValueKind.False => false, JsonValueKind.Null => null, _ => throw new InvalidOperationException("Unknown frozen Radial value.") };
    private static Windows.UI.Color Paint(string value) => value == "transparent" ? Windows.UI.Color.FromArgb(0, 0, 0, 0) : Windows.UI.Color.FromArgb(255, Convert.ToByte(value.Substring(1, 2), 16), Convert.ToByte(value.Substring(3, 2), 16), Convert.ToByte(value.Substring(5, 2), 16));
    private static T Get<T>(object value, string property) => (T)value.GetType().GetProperty(property)!.GetValue(value)!;
    private static string Inspection(BleeckerLineChart chart) => string.Concat(Part<StackPanel>(chart, "PART_TooltipItems").Children.OfType<TextBlock>().Select(AutomationProperties.GetName));
    private static IEnumerable<T> Descendants<T>(DependencyObject owner) where T : DependencyObject { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++) { var child = VisualTreeHelper.GetChild(owner, i); if (child is T item) yield return item; foreach (var nested in Descendants<T>(child)) yield return nested; } }
    private static T Part<T>(DependencyObject owner, string name) where T : FrameworkElement => Descendants<T>(owner).FirstOrDefault(item => item.Name == name) ?? throw new InvalidOperationException("Missing Radial Bar part " + name);
    private async Task AwaitAsync(Func<bool> condition, string message) { var watch = Stopwatch.StartNew(); while (!condition() && watch.Elapsed < TimeSpan.FromSeconds(2)) { UpdateLayout(); await Task.Delay(16); } Require(condition(), message); }
    private static async Task FrameAsync() { var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); void Frame(object? sender, object args) => ready.TrySetResult(); CompositionTarget.Rendering += Frame; try { await ready.Task.WaitAsync(TimeSpan.FromSeconds(2)); } finally { CompositionTarget.Rendering -= Frame; } }
    private static void Near(double actual, double expected) => Require(Math.Abs(actual - expected) < .001, $"coordinate {actual:R}/{expected:R}");
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
