using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard.Catalog;

public sealed partial class RadarChartsFixture : UserControl
{
    public RadarChartsFixture()
    {
        InitializeComponent();
        foreach (var (subject, a, b) in new[] { ("Math", 120d, 110d), ("Science", 98d, 130d), ("History", 86d, 130d), ("Language", 99d, 100d), ("Art", 85d, 90d), ("Sport", 65d, 85d) })
            Specimen.Data.Add(new Dictionary<string, object?> { ["subject"] = subject, ["a"] = a, ["b"] = b });
    }
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        Require(XamlRoot.RasterizationScale == 1 && new Windows.UI.ViewManagement.UISettings().TextScaleFactor == 1, "Frozen Radar checks require display/text scale 1.");
        using var input = new NativeInputSession(XamlRoot, folder, $"radar-chart-{scheme}"); await input.AcquireAsync(Specimen);
        var root = (Grid)XamlRoot.Content; var host = new Grid { Margin = new Thickness(20), Background = root.Background }; Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count); root.Children.Add(host);
        using var reference = JsonDocument.Parse(typeof(RadarChartsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.RadarChartLayout.json")!);
        var reports = new List<object>(); var failures = new List<string>();
        try
        {
            await input.MoveAsync(host, localPoint: new Point(850, 600));
            foreach (var sample in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("scheme").GetString() == scheme.ToString()))
            {
                var id = sample.GetProperty("id").GetString()!; var props = sample.GetProperty("props"); var expected = sample.GetProperty("normal");
                // Each source sequence's verified final outside action also
                // establishes pointer setup for the next fresh chart mount.
                Require(sample.GetProperty("states").EnumerateArray().Last().GetProperty("action").GetString() == "outside", "Radar source sequence must finish outside the chart.");
                var data = Records(props.GetProperty("data")); var owner = new BleeckerRadarChart { Data = data, SubjectKey = props.GetProperty("subjectKey").GetString()!, Width = expected.GetProperty("width").GetDouble(), Height = expected.GetProperty("height").GetDouble(), ShowLegend = !props.TryGetProperty("showLegend", out var shown) || shown.GetBoolean(), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                foreach (var item in props.GetProperty("series").EnumerateArray()) owner.Series.Add(new BleeckerRadarSeries { Key = item.GetProperty("key").GetString()!, Name = item.GetProperty("name").GetString()!, Fill = item.TryGetProperty("fill", out var fill) && fill.GetBoolean(), Color = item.TryGetProperty("color", out var color) ? new SolidColorBrush(Paint(color.GetString()!)) : null });
                if (props.TryGetProperty("format", out var format) && format.GetBoolean()) owner.TickFormatter = value => "T" + value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                AutomationProperties.SetName(owner, "Consumer radar comparison"); host.Children.Clear(); host.Children.Add(owner); await AwaitAsync(() => owner.IsLoaded, "Radar did not mount."); owner.ApplyTemplate(); UpdateLayout(); await FrameAsync(); UpdateLayout();
                var chart = Descendants<BleeckerLineChart>(owner).Single(); var plot = Part<Canvas>(chart, "PART_Plot"); var errors = new List<string>();
                void Check(string boundary, Action check) { try { check(); } catch (InvalidOperationException error) { errors.Add(boundary + ": " + error.Message); } }
                Check("ownership", () => Require(ReferenceEquals(chart.Data, data) && !owner.IsTabStop && chart.IsTabStop && AutomationProperties.GetName(chart) == "Consumer radar comparison" && FrameworkElementAutomationPeer.CreatePeerForElement(chart).GetClassName() == nameof(BleeckerRadarChart), "Radar lost original data or its single named inspection boundary."));
                Check("grid", () => CompareGrid(plot, expected));
                Check("subjects", () => CompareText(plot, "radar-subject", expected.GetProperty("angleTicks"), false));
                Check("radius", () => CompareText(plot, "radar-radius-tick", expected.GetProperty("radiusTicks"), true));
                Check("polygons", () => ComparePolygons(plot, owner, expected.GetProperty("polygons")));
                Check("legend", () => CompareLegend(chart, expected));
                if (new[] { "default", "filled", "narrow", "range", "multiple", "negative" }.Contains(id)) await MainWindow.CaptureElementAsync(root, System.IO.Path.Combine(folder, $"radar-chart-{id}-{scheme}.png"));
                foreach (var state in sample.GetProperty("states").EnumerateArray())
                {
                    var action = state.GetProperty("action").GetString();
                    if (action == "focus") Require(chart.Focus(FocusState.Keyboard), "Radar refused keyboard focus.");
                    else if (action is "ArrowRight" or "ArrowLeft" or "Enter") await input.KeyAsync(action == "ArrowRight" ? (ushort)0x27 : action == "ArrowLeft" ? (ushort)0x25 : (ushort)0x0D, chart);
                    else if (action == "hoverTop") await input.MoveAsync(chart, localPoint: new Point(owner.Width / 2, owner.Height / 2 - 30));
                    else if (action == "hoverRight") await input.MoveAsync(chart, localPoint: new Point(owner.Width / 2 + 30, owner.Height / 2));
                    else if (action == "outside") await input.MoveAsync(host, localPoint: new Point(850, 600));
                    else throw new InvalidOperationException("Unknown Radar reference action.");
                    var value = state.GetProperty("state"); await AwaitAsync(() => Inspection(chart) == value.GetProperty("tooltip").GetString(), $"Radar {id}/{action} payload did not settle: {Inspection(chart)}");
                    if (value.GetProperty("tooltipVisible").GetBoolean()) await AwaitAsync(() => Math.Abs(Part<Border>(chart, "PART_Tooltip").ActualHeight - value.GetProperty("tooltipBox").GetProperty("height").GetDouble()) < .001 && Math.Abs(Part<Border>(chart, "PART_Tooltip").ActualWidth - Part<Border>(chart, "PART_Tooltip").Width) < .001, $"Radar {id}/{action} tooltip did not arrange its measured size.");
                    Check(action!, () => CompareInspection(chart, value));
                }
                reports.Add(new { id, errors }); failures.AddRange(errors.Select(error => id + ": " + error));
                await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"radar-chart-{scheme}.json"), JsonSerializer.Serialize(new { passed = failures.Count == 0, reports }, new JsonSerializerOptions { WriteIndented = true }));
            }
            Require(failures.Count == 0, "Radar mismatches: " + string.Join(" | ", failures));
            await VerifyMutationAsync(host, input, folder, scheme);
        }
        finally { root.Children.Remove(host); }
    }
    private static void CompareGrid(Canvas plot, JsonElement source)
    {
        var spokes = plot.Children.OfType<Line>().Where(line => Equals(line.Tag, "radar-spoke")).ToArray(); var lines = source.GetProperty("grid").EnumerateArray().Where(s => s.GetProperty("tag").GetString() == "line").ToArray();
        Require(spokes.Length == lines.Length, $"spokes {spokes.Length}/{lines.Length}");
        for (var i = 0; i < spokes.Length; i++) { Near(spokes[i].X1, Attribute(lines[i], "x1")); Near(spokes[i].Y1, Attribute(lines[i], "y1")); Near(spokes[i].X2, Attribute(lines[i], "x2")); Near(spokes[i].Y2, Attribute(lines[i], "y2")); }
        var rings = plot.Children.OfType<Path>().Where(path => Equals(path.Tag, "radar-ring")).ToArray(); var paths = source.GetProperty("grid").EnumerateArray().Where(s => s.GetProperty("tag").GetString() == "path" && s.GetProperty("attributes").GetProperty("d").GetString() != "Z").ToArray(); Require(rings.Length == paths.Length, $"rings {rings.Length}/{paths.Length}");
        for (var i = 0; i < rings.Length; i++) ComparePath(rings[i].Data, paths[i].GetProperty("attributes").GetProperty("d").GetString()!);
        var axis = plot.Children.OfType<Line>().Where(line => Equals(line.Tag, "radar-radius-axis")).ToArray(); var expectedAxis = source.GetProperty("radiusAxis").EnumerateArray().ToArray(); Require(axis.Length == expectedAxis.Length, "radius axis count differs");
        for (var i = 0; i < axis.Length; i++) { Near(axis[i].X1, Attribute(expectedAxis[i], "x1")); Near(axis[i].Y1, Attribute(expectedAxis[i], "y1")); Near(axis[i].X2, Attribute(expectedAxis[i], "x2")); Near(axis[i].Y2, Attribute(expectedAxis[i], "y2")); }
    }
    private static void CompareText(Canvas plot, string tag, JsonElement source, bool rotated)
    {
        var actual = plot.Children.OfType<TextBlock>().Where(t => Equals(t.Tag, tag)).ToArray(); var expected = source.EnumerateArray().ToArray(); Require(actual.Select(t => t.Text).SequenceEqual(expected.Select(t => t.GetProperty("text").GetString())), tag + " text differs");
        for (var i = 0; i < actual.Length; i++)
        {
            var text = actual[i]; var attributes = expected[i].GetProperty("attributes"); var x = double.Parse(attributes.GetProperty("x").GetString()!, System.Globalization.CultureInfo.InvariantCulture); var y = double.Parse(attributes.GetProperty("y").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
            if (rotated) { Require(text.RenderTransform is RotateTransform { Angle: 90 }, "radius tick did not retain source rotation"); var p = text.TransformToVisual(plot).TransformPoint(new Point(0, text.BaselineOffset)); Near(p.X, x); Near(p.Y, y); }
            else { var anchor = attributes.GetProperty("text-anchor").GetString(); Near(Canvas.GetLeft(text) + (anchor == "middle" ? text.DesiredSize.Width / 2 : anchor == "end" ? text.DesiredSize.Width : 0), x); var dy = expected[i].GetProperty("tspans")[0].GetProperty("attributes").GetProperty("dy").GetString()!; Near(Canvas.GetTop(text) + text.BaselineOffset, y + double.Parse(dy.Replace("em", ""), System.Globalization.CultureInfo.InvariantCulture) * 12); }
            ComparePaint(text.Foreground, expected[i].GetProperty("fill").GetString()!);
        }
    }
    private static void ComparePolygons(Canvas plot, BleeckerRadarChart owner, JsonElement source)
    {
        var expected = source.EnumerateArray().ToArray(); var cursor = 0;
        for (var i = 0; i < owner.Series.Count; i++)
        {
            var strokes = plot.Children.OfType<Path>().Where(p => p.Tag is ValueTuple<string, int> tag && tag.Item1 == "radar-stroke" && tag.Item2 == i).ToArray();
            if (strokes.Length == 0) continue;
            var fill = plot.Children.OfType<Path>().Single(p => p.Tag is ValueTuple<string, int> tag && tag == ("radar-fill", i)); var baseline = plot.Children.OfType<Path>().FirstOrDefault(p => p.Tag is ValueTuple<string, int> tag && tag == ("radar-base", i));
            Require(cursor < expected.Length, "native Radar has an extra polygon");
            ComparePath(fill.Data, expected[cursor].GetProperty("attributes").GetProperty("d").GetString()!); ComparePaint(fill.Fill, expected[cursor].GetProperty("fill").GetString()!); Near(fill.Opacity, double.Parse(expected[cursor].GetProperty("fillOpacity").GetString()!, System.Globalization.CultureInfo.InvariantCulture));
            if (baseline is null) { ComparePath(strokes[0].Data, expected[cursor].GetProperty("attributes").GetProperty("d").GetString()!); ComparePaint(strokes[0].Stroke, expected[cursor].GetProperty("stroke").GetString()!); cursor++; }
            else { Require(cursor + 2 < expected.Length, "source Radar range boundary is absent"); ComparePath(strokes[0].Data, expected[cursor + 1].GetProperty("attributes").GetProperty("d").GetString()!); ComparePath(baseline.Data, expected[cursor + 2].GetProperty("attributes").GetProperty("d").GetString()!); cursor += 3; }
        }
        Require(cursor == expected.Length, $"polygon parts {cursor}/{expected.Length}");
    }
    private static void CompareLegend(BleeckerLineChart chart, JsonElement source)
    {
        var labels = Part<Canvas>(chart, "PART_Legend").Children.OfType<Canvas>().SelectMany(c => c.Children.OfType<TextBlock>()).ToArray(); var expected = source.GetProperty("legend").EnumerateArray().ToArray(); Require(labels.Select(t => t.Text).SequenceEqual(expected.Select(t => t.GetProperty("text").GetString())), "legend labels differ");
        for (var i = 0; i < labels.Length; i++) { var parent = (Canvas)VisualTreeHelper.GetParent(labels[i]); Near(Canvas.GetLeft(parent) + Canvas.GetLeft(labels[i]), expected[i].GetProperty("box").GetProperty("x").GetDouble()); Near(Canvas.GetTop(parent) + Canvas.GetTop(labels[i]) + labels[i].BaselineOffset, expected[i].GetProperty("box").GetProperty("y").GetDouble() + 16); ComparePaint(labels[i].Foreground, expected[i].GetProperty("color").GetString()!); }
    }
    private static void CompareInspection(BleeckerLineChart chart, JsonElement source)
    {
        var shown = source.GetProperty("tooltipVisible").GetBoolean(); Require((Part<Border>(chart, "PART_Tooltip").Visibility == Visibility.Visible) == shown, "inspection visibility differs");
        var lines = source.GetProperty("tooltipLines").EnumerateArray().Where(line => !string.IsNullOrEmpty(line.GetProperty("text").GetString())).ToArray();
        if (shown) Require(Part<StackPanel>(chart, "PART_TooltipItems").Children.OfType<TextBlock>().Select(text => text.Text).SequenceEqual(lines.Select(line => line.GetProperty("rendered").GetString())), "rendered tooltip rows differ");
        Require(AutomationProperties.GetHelpText(chart) == string.Join("; ", lines.Select(line => line.GetProperty("text").GetString()).Where(text => !string.IsNullOrEmpty(text))), "accessible tooltip payload differs");
        var active = Part<Canvas>(chart, "PART_Active"); var dots = active.Children.OfType<Ellipse>().ToArray(); var expectedDots = source.GetProperty("activeDots").EnumerateArray().ToArray(); Require(dots.Length == expectedDots.Length, $"active dots {dots.Length}/{expectedDots.Length}");
        for (var i = 0; i < dots.Length; i++) { Near(Canvas.GetLeft(dots[i]) + dots[i].Width / 2, Attribute(expectedDots[i], "cx")); Near(Canvas.GetTop(dots[i]) + dots[i].Height / 2, Attribute(expectedDots[i], "cy")); Near((dots[i].Width - dots[i].StrokeThickness) / 2, Attribute(expectedDots[i], "r")); ComparePaint(dots[i].Fill, expectedDots[i].GetProperty("fill").GetString()!); ComparePaint(dots[i].Stroke, expectedDots[i].GetProperty("stroke").GetString()!); }
        var cursor = active.Children.OfType<Path>().ToArray(); var expectedCursor = source.GetProperty("cursor").EnumerateArray().ToArray(); Require(cursor.Length == expectedCursor.Length, "cursor count differs"); for (var i = 0; i < cursor.Length; i++) ComparePath(cursor[i].Data, expectedCursor[i].GetProperty("attributes").GetProperty("d").GetString()!);
        if (shown) { var box = source.GetProperty("tooltipBox"); var margin = Part<Grid>(chart, "PART_TooltipContainer").Margin; Near(margin.Left, box.GetProperty("x").GetDouble()); Near(margin.Top, box.GetProperty("y").GetDouble()); Near(Part<Border>(chart, "PART_Tooltip").ActualHeight, box.GetProperty("height").GetDouble()); Near(Part<Border>(chart, "PART_Tooltip").ActualWidth, box.GetProperty("width").GetDouble()); }
    }
    private static void ComparePath(Geometry actual, string source)
    {
        var expected = (PathGeometry)((Path)XamlReader.Load("<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Data='" + source + "' />")).Data; var geometry = (PathGeometry)actual; Require(geometry.Figures.Count == expected.Figures.Count, "polygon figure count differs");
        for (var i = 0; i < geometry.Figures.Count; i++) { var a = geometry.Figures[i]; var e = expected.Figures[i]; Near(a.StartPoint.X, e.StartPoint.X); Near(a.StartPoint.Y, e.StartPoint.Y); Require(a.IsClosed == e.IsClosed && a.Segments.Count == e.Segments.Count, "polygon closure/segments differ"); for (var j = 0; j < a.Segments.Count; j++) { var line = (LineSegment)a.Segments[j]; var other = (LineSegment)e.Segments[j]; Near(line.Point.X, other.Point.X); Near(line.Point.Y, other.Point.Y); } }
    }
    private static double Attribute(JsonElement shape, string key) => double.Parse(shape.GetProperty("attributes").GetProperty(key).GetString()!, System.Globalization.CultureInfo.InvariantCulture);
    private static void ComparePaint(Brush brush, string value) { var c = ((SolidColorBrush)brush).Color; var v = Regex.Matches(value, @"[\d.]+").Select(m => double.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray(); Require(c.R == v[0] && c.G == v[1] && c.B == v[2] && Math.Round(c.A * brush.Opacity) == Math.Round((v.Length == 3 ? 1 : v[3]) * 255), "paint differs from " + value); }
    private static Windows.UI.Color Paint(string value) => value == "transparent" ? Windows.UI.Color.FromArgb(0, 0, 0, 0) : Windows.UI.Color.FromArgb(255, Convert.ToByte(value.Substring(1, 2), 16), Convert.ToByte(value.Substring(3, 2), 16), Convert.ToByte(value.Substring(5, 2), 16));
    private static string Inspection(BleeckerLineChart chart) => string.Concat(Part<StackPanel>(chart, "PART_TooltipItems").Children.OfType<TextBlock>().Select(AutomationProperties.GetName));
    private static ObservableCollection<IReadOnlyDictionary<string, object?>> Records(JsonElement data) => new(data.EnumerateArray().Select(row => (IReadOnlyDictionary<string, object?>)Read(row)!));
    private static object? Read(JsonElement value) => value.ValueKind switch { JsonValueKind.Object => value.EnumerateObject().ToDictionary(p => p.Name, p => Read(p.Value)), JsonValueKind.Array => value.EnumerateArray().Select(Read).ToArray(), JsonValueKind.Number => value.GetDouble(), JsonValueKind.String => value.GetString(), JsonValueKind.True => true, JsonValueKind.False => false, JsonValueKind.Null => null, _ => throw new InvalidOperationException("Unknown frozen value.") };
    private static IEnumerable<T> Descendants<T>(DependencyObject owner) where T : DependencyObject { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++) { var child = VisualTreeHelper.GetChild(owner, i); if (child is T item) yield return item; foreach (var nested in Descendants<T>(child)) yield return nested; } }
    private static T Part<T>(DependencyObject owner, string name) where T : FrameworkElement => Descendants<T>(owner).FirstOrDefault(item => item.Name == name) ?? throw new InvalidOperationException("Missing Radar part " + name);
    private async Task AwaitAsync(Func<bool> condition, string message) { var watch = Stopwatch.StartNew(); var readiness = new NativeReadiness(DispatcherQueue); while (!condition() && watch.Elapsed < TimeSpan.FromSeconds(2)) { UpdateLayout(); if (condition()) break; await readiness.WaitAsync(); } Require(condition(), message); }
    private static async Task FrameAsync() { var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); void Frame(object? sender, object args) => ready.TrySetResult(); CompositionTarget.Rendering += Frame; try { await ready.Task.WaitAsync(TimeSpan.FromSeconds(2)); } finally { CompositionTarget.Rendering -= Frame; } }
    private static void Near(double actual, double expected) => Require(Math.Abs(actual - expected) < .001, $"coordinate {actual:R}/{expected:R}");
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
