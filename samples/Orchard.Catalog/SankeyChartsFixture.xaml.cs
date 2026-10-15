using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard.Catalog;

public sealed partial class SankeyChartsFixture : UserControl
{
    public SankeyChartsFixture() => InitializeComponent();
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        Require(XamlRoot.RasterizationScale == 1 && new Windows.UI.ViewManagement.UISettings().TextScaleFactor == 1, "Frozen Sankey checks require display/text scale 1.");
        using var input = new NativeInputSession(XamlRoot, folder, $"sankey-chart-{scheme}"); await input.AcquireAsync(Specimen);
        var root = (Grid)XamlRoot.Content; var host = new Grid { Margin = new Thickness(20), Background = root.Background }; Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count); root.Children.Add(host);
        using var reference = JsonDocument.Parse(typeof(SankeyChartsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.SankeyChartLayout.json")!);
        var reports = new List<object>(); var failures = new List<string>();
        try
        {
            await input.MoveAsync(host, localPoint: new Point(850, 600));
            foreach (var sample in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("scheme").GetString() == scheme.ToString()))
            {
                var id = sample.GetProperty("id").GetString()!; var props = sample.GetProperty("props"); var expected = sample.GetProperty("normal"); var data = Data(props.GetProperty("data"));
                var owner = new BleeckerSankeyChart { Data = data, Width = expected.GetProperty("width").GetDouble(), Height = expected.GetProperty("height").GetDouble(), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                if (props.TryGetProperty("nodeWidth", out var width)) owner.NodeWidth = width.GetDouble();
                if (props.TryGetProperty("linkCurvature", out var curve)) owner.LinkCurvature = curve.GetDouble();
                if (props.TryGetProperty("linkOpacity", out var opacity)) owner.LinkOpacity = opacity.GetDouble();
                if (props.TryGetProperty("nodeRadius", out var radius)) owner.NodeRadius = radius.GetDouble();
                if (props.TryGetProperty("nodeColors", out var palette)) owner.NodeColors = new(palette.EnumerateArray().Select(value => (Brush)new SolidColorBrush(Paint(value.GetString()!))));
                AutomationProperties.SetName(owner, "Consumer Sankey comparison"); host.Children.Clear(); host.Children.Add(owner); await AwaitAsync(() => owner.IsLoaded, "Sankey did not mount."); owner.ApplyTemplate(); UpdateLayout(); await FrameAsync(); UpdateLayout();
                var chart = Descendants<BleeckerLineChart>(owner).Single(); var plot = Part<Canvas>(chart, "PART_Plot"); var errors = new List<string>();
                void Check(string boundary, Action action) { try { action(); } catch (InvalidOperationException error) { errors.Add(boundary + ": " + error.Message); } }
                Check("ownership", () => Require(ReferenceEquals(chart.Data, data.Nodes) && !owner.IsTabStop && chart.IsTabStop && AutomationProperties.GetName(chart) == "Consumer Sankey comparison" && FrameworkElementAutomationPeer.CreatePeerForElement(chart).GetClassName() == nameof(BleeckerSankeyChart), "Sankey lost its original nodes or single named stop."));
                Check("graph", () => CompareGraph(plot, expected, owner.NodeRadius));
                if (new[] { "default", "crossing", "narrow", "short", "paint", "multilingual" }.Contains(id)) await MainWindow.CaptureElementAsync(root, System.IO.Path.Combine(folder, $"sankey-chart-{id}-{scheme}.png"));
                foreach (var state in sample.GetProperty("states").EnumerateArray())
                {
                    var action = state.GetProperty("action").GetString()!;
                    // The browser chart has no Tab stop. Native Windows keyboard
                    // inspection is verified separately as an explicit adaptation.
                    if (action is "Tab" or "ArrowRight" or "ArrowLeft" or "Enter") continue;
                    if (action == "hover") { var target = state.GetProperty("target"); await input.MoveAsync(chart, localPoint: new Point(target.GetProperty("x").GetDouble(), target.GetProperty("y").GetDouble())); }
                    else if (action == "outside") await input.MoveAsync(host, localPoint: new Point(850, 600));
                    else throw new InvalidOperationException("Unknown source Sankey action.");
                    var value = state.GetProperty("state"); await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == value.GetProperty("tooltip").GetString(), $"Sankey {id}/{action} payload differs: {AutomationProperties.GetHelpText(chart)}");
                    if (value.GetProperty("tooltipVisible").GetBoolean()) await AwaitAsync(() => Math.Abs(Part<Border>(chart, "PART_Tooltip").ActualHeight - value.GetProperty("tooltipBox").GetProperty("height").GetDouble()) < .001 && Math.Abs(Part<Border>(chart, "PART_Tooltip").ActualWidth - Part<Border>(chart, "PART_Tooltip").Width) < .001, "Sankey tooltip did not arrange.");
                    await AwaitAsync(() => OpacityMatches(plot, value), "Sankey flow opacity did not settle.");
                    Check(action, () => CompareInspection(chart, value));
                }
                reports.Add(new { id, errors }); failures.AddRange(errors.Select(error => id + ": " + error));
                await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"sankey-chart-{scheme}.json"), JsonSerializer.Serialize(new { passed = failures.Count == 0, reports }, new JsonSerializerOptions { WriteIndented = true }));
            }
            Require(failures.Count == 0, "Sankey mismatches: " + string.Join(" | ", failures));
            await VerifyMutationAsync(host, input, folder, scheme);
        }
        finally { root.Children.Remove(host); }
    }
    private static bool OpacityMatches(Canvas plot, JsonElement state) => Paths(plot, "sankey-link").All(path => { var at = ((ValueTuple<string, int>)path.Tag).Item2; var item = state.GetProperty("links")[at]; return Math.Abs(path.Opacity - Number(item.GetProperty("opacity").GetString()!) * Math.Clamp(Number(item.GetProperty("fillOpacity").GetString()!), 0, 1)) < .001; });
    private static void CompareGraph(Canvas plot, JsonElement source, double radius)
    {
        var nodes = source.GetProperty("nodes").EnumerateArray().ToArray(); var actualNodes = Paths(plot, "sankey-node");
        var expectedNodes = nodes.Where(node => { var a = node.GetProperty("attributes"); return new[] { "x", "y", "width", "height" }.All(key => double.IsFinite(Attr(a, key))) && Attr(a, "width") > 0 && Attr(a, "height") > 0; }).ToArray();
        Require(actualNodes.Length == expectedNodes.Length, $"node count {actualNodes.Length}/{expectedNodes.Length}");
        foreach (var path in actualNodes)
        {
            var item = nodes[((ValueTuple<string, int>)path.Tag).Item2]; var attrs = item.GetProperty("attributes"); var x = Attr(attrs, "x"); var y = Attr(attrs, "y"); var w = Attr(attrs, "width"); var h = Attr(attrs, "height");
            var figure = ((PathGeometry)path.Data).Figures.Single(); var points = new[] { figure.StartPoint }.Concat(figure.Segments.Select(segment => segment switch { LineSegment line => line.Point, ArcSegment arc => arc.Point, _ => throw new InvalidOperationException("Unknown node segment.") })).ToArray();
            Near(points.Min(p => p.X), x); Near(points.Max(p => p.X), x + w); Near(points.Min(p => p.Y), y); Near(points.Max(p => p.Y), y + h);
            foreach (var arc in figure.Segments.OfType<ArcSegment>()) { Near(arc.Size.Width, Math.Min(Math.Max(0, radius), w / 2)); Near(arc.Size.Height, Math.Min(Math.Max(0, radius), h / 2)); }
            ComparePaint(path.Fill, item.GetProperty("fill").GetString()!); ComparePaint(path.Stroke, item.GetProperty("stroke").GetString()!); Near(path.StrokeThickness, 2); Require(figure.IsClosed, "Sankey node is open.");
        }
        var links = source.GetProperty("links").EnumerateArray().ToArray(); var actualLinks = Paths(plot, "sankey-link");
        var expectedLinks = links.Where(link => !Regex.IsMatch(link.GetProperty("attributes").GetProperty("d").GetString()!, "NaN|Infinity")).ToArray(); Require(actualLinks.Length == expectedLinks.Length, $"flow count {actualLinks.Length}/{expectedLinks.Length}");
        foreach (var path in actualLinks)
        {
            var item = links[((ValueTuple<string, int>)path.Tag).Item2]; var numbers = Numbers(item.GetProperty("attributes").GetProperty("d").GetString()!); var figure = ((PathGeometry)path.Data).Figures.Single();
            var points = new List<Point> { figure.StartPoint }; foreach (var segment in figure.Segments) { if (segment is BezierSegment cubic) { points.Add(cubic.Point1); points.Add(cubic.Point2); points.Add(cubic.Point3); } else if (segment is LineSegment line) points.Add(line.Point); else throw new InvalidOperationException("Unknown flow segment."); }
            Require(points.Count * 2 == numbers.Length && figure.IsClosed && ((PathGeometry)path.Data).FillRule == FillRule.Nonzero, "Sankey cubic path or source fill rule differs."); for (var i = 0; i < points.Count; i++) { Near(points[i].X, numbers[i * 2]); Near(points[i].Y, numbers[i * 2 + 1]); }
            ComparePaint(path.Fill, item.GetProperty("fill").GetString()!); Near(path.Opacity, Math.Clamp(Number(item.GetProperty("fillOpacity").GetString()!), 0, 1)); Require(path.Stroke is null, "Sankey flow gained a stroke.");
        }
    }
    private static void CompareInspection(BleeckerLineChart chart, JsonElement source)
    {
        var shown = source.GetProperty("tooltipVisible").GetBoolean(); var tooltip = Part<Border>(chart, "PART_Tooltip"); Require((tooltip.Visibility == Visibility.Visible) == shown, "Sankey inspection visibility differs."); Require(Part<Canvas>(chart, "PART_Active").Children.Count == source.GetProperty("cursor").GetArrayLength(), "Sankey cursor differs."); if (!shown) return;
        var rows = source.GetProperty("tooltipLines").EnumerateArray().Where(row => row.GetProperty("text").GetString()!.Length > 0).ToArray(); Require(Part<StackPanel>(chart, "PART_TooltipItems").Children.OfType<TextBlock>().Select(row => row.Text).SequenceEqual(rows.Select(row => row.GetProperty("rendered").GetString())), "Sankey rendered inspection text differs.");
        var box = source.GetProperty("tooltipBox"); var margin = Part<Grid>(chart, "PART_TooltipContainer").Margin; Near(margin.Left, box.GetProperty("x").GetDouble()); Near(margin.Top, box.GetProperty("y").GetDouble()); Near(tooltip.ActualWidth, box.GetProperty("width").GetDouble()); Near(tooltip.ActualHeight, box.GetProperty("height").GetDouble());
    }
    private static Path[] Paths(Canvas plot, string kind) => plot.Children.OfType<Path>().Where(path => path.Tag is ValueTuple<string, int> tag && tag.Item1 == kind).ToArray();
    private static BleeckerSankeyChartData Data(JsonElement value) => new() { Nodes = Rows(value.GetProperty("nodes")), Links = Rows(value.GetProperty("links")) };
    private static ObservableCollection<IReadOnlyDictionary<string, object?>> Rows(JsonElement value) => new(value.EnumerateArray().Select(row => (IReadOnlyDictionary<string, object?>)Read(row)!));
    private static object? Read(JsonElement value) => value.ValueKind switch { JsonValueKind.Object => value.EnumerateObject().ToDictionary(p => p.Name, p => p.Name == "fill" && p.Value.ValueKind == JsonValueKind.String ? new SolidColorBrush(Paint(p.Value.GetString()!)) : Read(p.Value)), JsonValueKind.Number => value.GetDouble(), JsonValueKind.String => value.GetString(), _ => throw new InvalidOperationException("Unknown Sankey value.") };
    private static Windows.UI.Color Paint(string value) => value == "transparent" ? Windows.UI.Color.FromArgb(0, 0, 0, 0) : Windows.UI.Color.FromArgb(255, Convert.ToByte(value.Substring(1, 2), 16), Convert.ToByte(value.Substring(3, 2), 16), Convert.ToByte(value.Substring(5, 2), 16));
    private static void ComparePaint(Brush brush, string source) { var c = ((SolidColorBrush)brush).Color; var values = Numbers(source); Require(c.R == values[0] && c.G == values[1] && c.B == values[2] && Math.Round(c.A * brush.Opacity) == Math.Round((values.Length == 3 ? 1 : values[3]) * 255), "Sankey source paint differs."); }
    private static double Attr(JsonElement value, string key) => value.TryGetProperty(key, out var attr) && double.TryParse(attr.GetString(), CultureInfo.InvariantCulture, out var result) ? result : double.NaN;
    private static double Number(string value) => double.Parse(value, CultureInfo.InvariantCulture);
    private static double[] Numbers(string value) => Regex.Matches(value, @"[-+]?(?:\d*\.)?\d+(?:[eE][-+]?\d+)?").Select(match => Number(match.Value)).ToArray();
    private static IEnumerable<T> Descendants<T>(DependencyObject owner) where T : DependencyObject { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++) { var child = VisualTreeHelper.GetChild(owner, i); if (child is T item) yield return item; foreach (var nested in Descendants<T>(child)) yield return nested; } }
    private static T Part<T>(BleeckerLineChart chart, string name) where T : DependencyObject => (T)((FrameworkElement)VisualTreeHelper.GetChild(chart, 0)).FindName(name);
    private async Task AwaitAsync(Func<bool> condition, string message) { var watch = Stopwatch.StartNew(); while (!condition() && watch.Elapsed < TimeSpan.FromSeconds(2)) { UpdateLayout(); await Task.Delay(16); } Require(condition(), message); }
    private static async Task FrameAsync() { var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); void Frame(object? sender, object args) => ready.TrySetResult(); CompositionTarget.Rendering += Frame; try { await ready.Task.WaitAsync(TimeSpan.FromSeconds(2)); } finally { CompositionTarget.Rendering -= Frame; } }
    private static void Near(double actual, double expected) => Require(Math.Abs(actual - expected) < .001, $"source coordinate {actual:R}/{expected:R}");
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
