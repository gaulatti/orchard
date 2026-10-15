using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
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

public sealed partial class SunburstChartsFixture : UserControl
{
    public SunburstChartsFixture() => InitializeComponent();
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        Require(XamlRoot.RasterizationScale == 1 && new Windows.UI.ViewManagement.UISettings().TextScaleFactor == 1, "Frozen Sunburst checks require display/text scale 1.");
        var root = (Grid)XamlRoot.Content; var host = new Grid { Margin = new Thickness(20), Background = root.Background }; Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count); root.Children.Add(host);
        using var reference = JsonDocument.Parse(typeof(SunburstChartsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.SunburstChartLayout.json")!);
        var reports = new List<object>(); var failures = new List<string>();
        try
        {
            foreach (var sample in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("scheme").GetString() == scheme.ToString()))
            {
                var id = sample.GetProperty("id").GetString()!; var props = sample.GetProperty("props"); var source = sample.GetProperty("normal"); var data = (IReadOnlyDictionary<string, object?>)Read(props.GetProperty("data"))!;
                var owner = new BleeckerSunburstChart { Data = data, Width = source.GetProperty("width").GetDouble(), Height = source.GetProperty("height").GetDouble(), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                if (props.TryGetProperty("dataKey", out var key)) owner.DataKey = key.GetString()!; if (props.TryGetProperty("nameKey", out var name)) owner.NameKey = name.GetString()!;
                AutomationProperties.SetName(owner, "Consumer Sunburst comparison"); host.Children.Clear(); host.Children.Add(owner); await AwaitAsync(() => owner.IsLoaded, "Sunburst did not mount."); owner.ApplyTemplate(); UpdateLayout(); await FrameAsync(); UpdateLayout();
                var errors = new List<string>(); void Check(string boundary, Action action) { try { action(); } catch (InvalidOperationException error) { errors.Add(boundary + ": " + error.Message); } }
                Check("ownership", () => Require(ReferenceEquals(owner.Data, data) && !owner.IsTabStop && FrameworkElementAutomationPeer.CreatePeerForElement(owner).GetAutomationControlType() == AutomationControlType.Image && AutomationProperties.GetName(owner) == "Consumer Sunburst comparison", "Sunburst lost original tree or its named, noninteractive image role."));
                Check("sectors", () => CompareSectors(owner, source.GetProperty("paths"))); Check("labels", () => CompareLabels(owner, source.GetProperty("labels")));
                Require(!Descendants<ToolTip>(owner).Any(), "Sunburst invented visible inspection.");
                if (new[] { "default", "branch", "missing-branch", "root-zero", "narrow", "short" }.Contains(id)) await MainWindow.CaptureElementAsync(root, System.IO.Path.Combine(folder, $"sunburst-chart-{id}-{scheme}.png"));
                var origin = owner.TransformToVisual(root).TransformPoint(new Point());
                reports.Add(new { id, errors, viewport = new { x = origin.X, y = origin.Y, width = owner.ActualWidth, height = owner.ActualHeight } }); failures.AddRange(errors.Select(error => id + ": " + error));
                await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"sunburst-chart-{scheme}.json"), JsonSerializer.Serialize(new { passed = failures.Count == 0, reports }, new JsonSerializerOptions { WriteIndented = true }));
            }
            Require(failures.Count == 0, "Sunburst mismatches: " + string.Join(" | ", failures));
            await VerifyMutationAsync(host, folder, scheme);
        }
        finally { root.Children.Remove(host); }
    }
    private static void CompareSectors(BleeckerSunburstChart owner, JsonElement source)
    {
        var actual = Sectors(owner); var expected = source.EnumerateArray().Where(p => !Regex.IsMatch(p.GetProperty("attributes").GetProperty("d").GetString()!, "NaN|Infinity")).ToArray(); Require(actual.Length == expected.Length, $"sector count {actual.Length}/{expected.Length}");
        for (var i = 0; i < actual.Length; i++)
        {
            var geometry = (PathGeometry)actual[i].Data; var figure = geometry.Figures.Single(); var values = new List<double> { figure.StartPoint.X, figure.StartPoint.Y };
            foreach (var segment in figure.Segments)
                if (segment is ArcSegment arc) values.AddRange([arc.Size.Width, arc.Size.Height, arc.RotationAngle, arc.IsLargeArc ? 1 : 0, arc.SweepDirection == SweepDirection.Clockwise ? 1 : 0, arc.Point.X, arc.Point.Y]);
                else if (segment is LineSegment line) values.AddRange([line.Point.X, line.Point.Y]); else throw new InvalidOperationException("Unknown Sunburst segment.");
            var numbers = Numbers(expected[i].GetProperty("attributes").GetProperty("d").GetString()!); Require(values.Count == numbers.Length && figure.IsClosed && geometry.FillRule == FillRule.Nonzero, "Sunburst path structure differs."); for (var j = 0; j < numbers.Length; j++) Near(values[j], numbers[j]);
            ComparePaint(actual[i].Fill, expected[i].GetProperty("fill").GetString()!); ComparePaint(actual[i].Stroke, expected[i].GetProperty("stroke").GetString()!); Near(actual[i].StrokeThickness, 2);
        }
    }
    private static void CompareLabels(BleeckerSunburstChart owner, JsonElement source)
    {
        var actual = Labels(owner); var expected = source.EnumerateArray().ToArray(); Require(actual.Length == expected.Length, $"label count {actual.Length}/{expected.Length}");
        for (var i = 0; i < actual.Length; i++)
        {
            object Value(string key) => actual[i].GetType().GetProperty(key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(actual[i])!;
            var attributes = expected[i].GetProperty("attributes"); Require((string)Value("Text") == expected[i].GetProperty("text").GetString(), "Sunburst value label text differs.");
            Near((double)Value("X"), Number(attributes.GetProperty("x").GetString()!)); Near((double)Value("Baseline"), Number(attributes.GetProperty("y").GetString()!)); Near((double)Value("Advance"), expected[i].GetProperty("textLength").GetDouble());
        }
        var strokes = Plot(owner).Children.OfType<Path>().Where(p => p.Tag is ValueTuple<string, int> tag && tag.Item1 == "sunburst-label-outline").ToArray(); var ink = Plot(owner).Children.OfType<Path>().Where(p => p.Tag is ValueTuple<string, int> tag && tag.Item1 == "sunburst-label").ToArray(); Require(strokes.Length == actual.Length && ink.Length == actual.Length, "Sunburst lost actual glyph paint layers.");
        for (var i = 0; i < ink.Length; i++) { Require(!ReferenceEquals(strokes[i].Data, ink[i].Data) && GeometrySignature((PathGeometry)strokes[i].Data) == GeometrySignature((PathGeometry)ink[i].Data) && Plot(owner).Children.IndexOf(strokes[i]) < Plot(owner).Children.IndexOf(ink[i]), "Sunburst changed the glyph outline or source stroke-before-fill paint order."); ComparePaint(strokes[i].Stroke, "rgb(255, 255, 255)"); ComparePaint(ink[i].Fill, "rgb(0, 0, 0)"); Near(strokes[i].StrokeThickness, 1); }
    }
    private static string GeometrySignature(PathGeometry geometry) => JsonSerializer.Serialize(new { geometry.FillRule, Figures = geometry.Figures.Select(figure => new { figure.StartPoint, figure.IsClosed, figure.IsFilled, Segments = figure.Segments.Select(segment => segment switch { LineSegment line => new[] { line.Point.X, line.Point.Y }, BezierSegment curve => new[] { curve.Point1.X, curve.Point1.Y, curve.Point2.X, curve.Point2.Y, curve.Point3.X, curve.Point3.Y }, QuadraticBezierSegment curve => new[] { curve.Point1.X, curve.Point1.Y, curve.Point2.X, curve.Point2.Y }, _ => throw new InvalidOperationException("Unknown Sunburst glyph segment.") }) }) });
    private static object[] Labels(BleeckerSunburstChart owner) => ((IEnumerable)typeof(BleeckerSunburstChart).GetField("labels", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!).Cast<object>().ToArray();
    private static Canvas Plot(BleeckerSunburstChart owner) => Descendants<Canvas>(owner).Single(canvas => canvas.Name == "PART_Plot");
    private static Path[] Sectors(BleeckerSunburstChart owner) => Plot(owner).Children.OfType<Path>().Where(p => p.Tag is ValueTuple<string, int> tag && tag.Item1 == "sunburst-sector").ToArray();
    private static object? Read(JsonElement value) => value.ValueKind switch { JsonValueKind.Object => value.EnumerateObject().ToDictionary(p => p.Name, p => p.Name == "fill" && p.Value.ValueKind == JsonValueKind.String ? new SolidColorBrush(Paint(p.Value.GetString()!)) : Read(p.Value)), JsonValueKind.Array => value.EnumerateArray().Select(Read).ToArray(), JsonValueKind.Number => value.GetDouble(), JsonValueKind.String => value.GetString(), JsonValueKind.Null => null, JsonValueKind.True => true, JsonValueKind.False => false, _ => throw new InvalidOperationException("Unknown source Sunburst value.") };
    private static Windows.UI.Color Paint(string value) => value == "transparent" ? Windows.UI.Color.FromArgb(0, 0, 0, 0) : Windows.UI.Color.FromArgb(255, Convert.ToByte(value.Substring(1, 2), 16), Convert.ToByte(value.Substring(3, 2), 16), Convert.ToByte(value.Substring(5, 2), 16));
    private static void ComparePaint(Brush brush, string source) { var c = ((SolidColorBrush)brush).Color; var values = Numbers(source); Require(c.R == values[0] && c.G == values[1] && c.B == values[2] && Math.Round(c.A * brush.Opacity) == Math.Round((values.Length == 3 ? 1 : values[3]) * 255), "Sunburst paint differs."); }
    private static double Number(string value) => double.Parse(value, CultureInfo.InvariantCulture);
    private static double[] Numbers(string value) => Regex.Matches(value, @"[-+]?(?:\d*\.)?\d+(?:[eE][-+]?\d+)?").Select(match => Number(match.Value)).ToArray();
    private static IEnumerable<T> Descendants<T>(DependencyObject owner) where T : DependencyObject { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++) { var child = VisualTreeHelper.GetChild(owner, i); if (child is T item) yield return item; foreach (var nested in Descendants<T>(child)) yield return nested; } }
    private async Task AwaitAsync(Func<bool> condition, string message) { var watch = Stopwatch.StartNew(); while (!condition() && watch.Elapsed < TimeSpan.FromSeconds(2)) { UpdateLayout(); await Task.Delay(16); } Require(condition(), message); }
    private static async Task FrameAsync() { var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); void Frame(object? sender, object args) => ready.TrySetResult(); CompositionTarget.Rendering += Frame; try { await ready.Task.WaitAsync(TimeSpan.FromSeconds(2)); } finally { CompositionTarget.Rendering -= Frame; } }
    private static void Near(double actual, double expected) => Require(Math.Abs(actual - expected) < .001, $"source metric {actual:R}/{expected:R}");
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
