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
using Windows.Foundation;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard.Catalog;

public sealed partial class AreaChartsFixture : UserControl
{
    public AreaChartsFixture()
    {
        InitializeComponent();
        foreach (var (name, total, selected) in new[] { ("Harbor", 100d, 50d), ("Reed", 220d, 180d), ("Vale", 140d, 90d), ("North", 310d, 240d) })
            Specimen.Data.Add(new Dictionary<string, object?> { ["label"] = name, ["total"] = total, ["selected"] = selected });
        StackedSpecimen.Data = Specimen.Data;
    }
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        Require(XamlRoot.RasterizationScale == 1 && new Windows.UI.ViewManagement.UISettings().TextScaleFactor == 1, "Frozen Area Chart checks require display/text scale 1.");
        using var input = new NativeInputSession(XamlRoot, folder, $"area-chart-{scheme}"); await input.AcquireAsync(Specimen);
        var root = (Grid)XamlRoot.Content;
        var host = new Grid { Margin = new Thickness(20), Background = root.Background };
        var outside = new Button { Content = "Outside chart", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Style = (Style)Application.Current.Resources["BleeckerSecondaryButtonStyle"] };
        Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count); root.Children.Add(host);
        using var reference = JsonDocument.Parse(typeof(AreaChartsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.AreaChartLayout.json")!);
        var reports = new List<object>(); var failures = new List<string>();
        try
        {
            foreach (var item in reference.RootElement.GetProperty("cases").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == scheme.ToString()))
            {
                var id = item.GetProperty("id").GetString()!; var props = item.GetProperty("props"); var expected = item.GetProperty("normal");
                var chart = new BleeckerAreaChart
                {
                    Data = new(props.GetProperty("data").EnumerateArray().Select(row => (IReadOnlyDictionary<string, object?>)Read(row)!)),
                    XAxisKey = props.GetProperty("xAxisKey").GetString(), Stacked = props.GetProperty("stacked").GetBoolean(),
                    Width = expected.GetProperty("width").GetDouble(), Height = expected.GetProperty("height").GetDouble(),
                    ShowGrid = !props.TryGetProperty("showGrid", out var grid) || grid.GetBoolean(), ShowLegend = !props.TryGetProperty("showLegend", out var showLegend) || showLegend.GetBoolean(),
                    HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top
                };
                foreach (var s in props.GetProperty("series").EnumerateArray())
                {
                    var series = new BleeckerChartSeries { Key = s.GetProperty("key").GetString()!, Name = s.GetProperty("name").GetString()! };
                    if (s.TryGetProperty("color", out var color)) series.Color = new SolidColorBrush(Paint(color.GetString()!));
                    chart.Series.Add(series);
                }
                if (props.TryGetProperty("format", out var format) && format.GetBoolean())
                {
                    chart.YAxisTickFormatter = value => "Y" + value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    chart.TooltipFormatter = value => "Value " + value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                AutomationProperties.SetName(chart, "Consumer area comparison"); host.Children.Clear(); host.Children.Add(chart); host.Children.Add(outside);
                await AwaitAsync(() => chart.IsLoaded, "Area Chart did not mount."); chart.ApplyTemplate(); UpdateLayout(); await FrameAsync(); UpdateLayout();
                var inner = Part<BleeckerLineChart>(chart, "PART_Chart"); var plot = Part<Canvas>(inner, "PART_Plot"); var legend = Part<Canvas>(inner, "PART_Legend");
                var errors = new List<string>();
                void Check(Action assertion) { try { assertion(); } catch (InvalidOperationException error) { errors.Add(error.Message); } }
                var areas = plot.Children.OfType<Path>().ToArray(); var sourceAreas = expected.GetProperty("areas").EnumerateArray().ToArray();
                Check(() => Require(areas.Length == sourceAreas.Length, $"area paths {areas.Length}/{sourceAreas.Length}"));
                for (var i = 0; i < Math.Min(areas.Length, sourceAreas.Length); i++)
                {
                    var n = i; Check(() => {
                        var source = sourceAreas[n]; CompareGeometry((PathGeometry)areas[n].Data, source.GetProperty("path").ValueKind == JsonValueKind.Null ? "" : source.GetProperty("path").GetString()!);
                        if (source.GetProperty("fill").GetString() != "none") { ComparePaint(areas[n].Fill, source.GetProperty("fill").GetString()!); Near(areas[n].Opacity, .2); Require(((PathGeometry)areas[n].Data).Figures.All(figure => figure.IsFilled), "area figure is not filled"); }
                        else { ComparePaint(areas[n].Stroke, source.GetProperty("stroke").GetString()!); Near(areas[n].StrokeThickness, 2); }
                    });
                }
                Check(() => VerifyDots(plot, expected.GetProperty("dots")));
                var lines = plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Line>().ToArray(); var sourceGrid = expected.GetProperty("grid").EnumerateArray().ToArray();
                Check(() => Require(lines.Length == sourceGrid.Length, $"grid count {lines.Length}/{sourceGrid.Length}"));
                for (var i = 0; i < Math.Min(lines.Length, sourceGrid.Length); i++)
                {
                    var line = i; Check(() => { Near(lines[line].X1, sourceGrid[line].GetProperty("x1").GetDouble()); Near(lines[line].X2, sourceGrid[line].GetProperty("x2").GetDouble()); Near(lines[line].Y1, sourceGrid[line].GetProperty("y1").GetDouble()); Near(lines[line].Y2, sourceGrid[line].GetProperty("y2").GetDouble()); });
                }
                foreach (var axis in new[] { "x", "y" }) Check(() => {
                    var labels = plot.Children.OfType<TextBlock>().Where(text => Equals(text.Tag, "area-" + axis)).Select(text => text.Text);
                    Require(labels.SequenceEqual(expected.GetProperty(axis + "Ticks").EnumerateArray().Select(tick => tick.GetProperty("text").GetString())), axis + " tick labels differ: " + string.Join("|", labels));
                });
                Check(() => Require(legend.Children.Count == expected.GetProperty("legend").GetArrayLength(), "legend entries differ"));
                Check(() => Require(ReferenceEquals(chart.Data, inner.Data) && ReferenceEquals(chart.Series, inner.Series) && !chart.IsTabStop && inner.IsTabStop && AutomationProperties.GetName(inner) == "Consumer area comparison" && FrameworkElementAutomationPeer.CreatePeerForElement(inner).GetClassName() == nameof(BleeckerAreaChart), "consumer identity or native inspection boundary differs"));
                if (id is "default" or "narrow" or "negative" or "types" or "long-labels")
                    await NativeScreenCapture.CaptureClientAsync(input.Window, System.IO.Path.Combine(folder, $"area-chart-{id}-{chart.Stacked}-{scheme}.png"));
                var handover = id == "default" && !chart.Stacked;
                if (handover)
                {
                    var firstDot = item.GetProperty("focus").GetProperty("dots")[0];
                    await input.MoveAsync(inner, localPoint: new Point(firstDot.GetProperty("cx").GetDouble(), firstDot.GetProperty("cy").GetDouble()));
                    await AwaitAsync(() => InspectionText(inner) == item.GetProperty("focus").GetProperty("tooltip").GetString(), "Area Chart physical hover did not inspect the first source category.");
                }
                await FocusAsync(inner);
                await AwaitAsync(() => InspectionText(inner) == item.GetProperty("focus").GetProperty("tooltip").GetString(), "Area Chart focus inspection did not settle.");
                Check(() => VerifyInspection(inner, item.GetProperty("focus"), false));
                if (handover)
                {
                    await input.KeyAsync(0x27, inner);
                    Require(Part<Border>(inner, "PART_Tooltip").Visibility == Visibility.Visible, "Area Chart hid the source mouse-priority tooltip during a real key action.");
                    Check(() => VerifyInspection(inner, item.GetProperty("focus"), false));
                    await input.MoveAsync(outside);
                    await AwaitAsync(() => InspectionText(inner) == item.GetProperty("key").GetProperty("tooltip").GetString(), "Area Chart pointer exit did not restore the remembered keyboard category.");
                    Check(() => VerifyInspection(inner, item.GetProperty("key"), true));
                    await input.KeyAsync(0x25, inner);
                    await AwaitAsync(() => InspectionText(inner) == item.GetProperty("focus").GetProperty("tooltip").GetString(), "Area Chart Left did not restore its initial category after handover.");
                }
                await input.KeyAsync((ushort)0x27, inner);
                await AwaitAsync(() => InspectionText(inner) == item.GetProperty("key").GetProperty("tooltip").GetString(), "Area Chart key inspection did not settle.");
                Check(() => VerifyInspection(inner, item.GetProperty("key"), true));
                await input.KeyAsync(0x27, inner);
                await AwaitAsync(() => InspectionText(inner) == item.GetProperty("right").GetProperty("tooltip").GetString(), "Area Chart Right inspection did not settle.");
                Check(() => VerifyInspection(inner, item.GetProperty("right"), true));
                reports.Add(new { id, stacked = chart.Stacked, errors, areas = areas.Length, grid = lines.Length, labels = plot.Children.OfType<TextBlock>().Select(text => text.Text).ToArray() });
                failures.AddRange(errors.Select(error => id + "/" + chart.Stacked + ": " + error));
                await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"area-chart-{scheme}.json"), JsonSerializer.Serialize(new { passed = failures.Count == 0, reports }, new JsonSerializerOptions { WriteIndented = true }));
            }
            Require(failures.Count == 0, "Area Chart mismatches: " + string.Join(" | ", failures));
            await VerifyMutationAsync(host, input, folder, scheme);
        }
        finally { root.Children.Remove(host); }
    }
    private static void VerifyInspection(BleeckerLineChart chart, JsonElement expected, bool visibility)
    {
        var text = string.Concat(Part<StackPanel>(chart, "PART_TooltipItems").Children.OfType<TextBlock>().Select(text => text.Text));
        Require(text == expected.GetProperty("tooltip").GetString(), "tooltip: " + text + "/" + expected.GetProperty("tooltip").GetString());
        if (visibility) Require((Part<Border>(chart, "PART_Tooltip").Visibility == Visibility.Visible) == expected.GetProperty("visible").GetBoolean(), "tooltip visibility differs");
        var active = Part<Canvas>(chart, "PART_Active");
        var cursors = active.Children.OfType<Microsoft.UI.Xaml.Shapes.Line>().ToArray(); var source = expected.GetProperty("cursor").EnumerateArray().ToArray();
        Require(cursors.Length == source.Length, "inspection cursor count differs");
        for (var i = 0; i < cursors.Length; i++) CompareGeometry(new PathGeometry { Figures = { new PathFigure { StartPoint = new Point(cursors[i].X1, cursors[i].Y1), IsFilled = false, Segments = { new LineSegment { Point = new Point(cursors[i].X2, cursors[i].Y2) } } } } }, source[i].GetProperty("path").GetString()!);
        VerifyDots(active, expected.GetProperty("dots"), Part<Canvas>(chart, "PART_Plot").Children.OfType<Microsoft.UI.Xaml.Shapes.Ellipse>().ToArray());
    }
    private static async Task FocusAsync(BleeckerLineChart chart)
    {
        var focused = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); void Focused(object sender, RoutedEventArgs args) { if (ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(chart.XamlRoot), chart)) focused.TrySetResult(); }
        chart.GotFocus += Focused;
        try { Require(chart.Focus(FocusState.Keyboard), "Could not focus Area Chart."); await focused.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
        finally { chart.GotFocus -= Focused; }
    }
    private static void VerifyDots(Canvas canvas, JsonElement expected, Microsoft.UI.Xaml.Shapes.Ellipse[]? persistent = null)
    {
        var actual = (persistent ?? []).Concat(canvas.Children.OfType<Microsoft.UI.Xaml.Shapes.Ellipse>()).ToArray(); var source = expected.EnumerateArray().ToArray();
        Require(actual.Length == source.Length, $"dots {actual.Length}/{source.Length}");
        for (var i = 0; i < actual.Length; i++) {
            Near(Canvas.GetLeft(actual[i]) + actual[i].Width / 2, source[i].GetProperty("cx").GetDouble()); Near(Canvas.GetTop(actual[i]) + actual[i].Height / 2, source[i].GetProperty("cy").GetDouble());
            Near((actual[i].Width - 2) / 2, source[i].GetProperty("r").GetDouble()); ComparePaint(actual[i].Fill, source[i].GetProperty("fill").GetString()!); ComparePaint(actual[i].Stroke, source[i].GetProperty("stroke").GetString()!);
        }
    }
    private static string InspectionText(BleeckerLineChart chart) => string.Concat(Part<StackPanel>(chart, "PART_TooltipItems").Children.OfType<TextBlock>().Select(text => text.Text));
    private static object? Read(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => value.EnumerateObject().ToDictionary(property => property.Name, property => Read(property.Value)),
        JsonValueKind.Array => value.EnumerateArray().Select(Read).ToArray(), JsonValueKind.Number => value.GetDouble(), JsonValueKind.String => value.GetString(),
        JsonValueKind.True => true, JsonValueKind.False => false, JsonValueKind.Null => null, _ => throw new InvalidOperationException("Unsupported frozen value.")
    };
    private static Windows.UI.Color Paint(string color) => color == "transparent" ? Windows.UI.Color.FromArgb(0, 0, 0, 0) : Windows.UI.Color.FromArgb(255, Convert.ToByte(color.Substring(1, 2), 16), Convert.ToByte(color.Substring(3, 2), 16), Convert.ToByte(color.Substring(5, 2), 16));
    private static void ComparePaint(Brush brush, string source)
    {
        var channels = Regex.Matches(source, @"[\d.]+").Select(match => double.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var color = ((SolidColorBrush)brush).Color;
        Require(color.R == channels[0] && color.G == channels[1] && color.B == channels[2] && color.A == (channels.Length == 3 ? 255 : channels[3] * 255), "paint differs from " + source);
    }
    private static void CompareGeometry(PathGeometry actual, string source)
    {
        var expected = (PathGeometry)((Path)XamlReader.Load("<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Data='" + source + "' />")).Data;
        Require(actual.Figures.Count == expected.Figures.Count, "area figure count differs");
        for (var i = 0; i < actual.Figures.Count; i++)
        {
            var a = actual.Figures[i]; var e = expected.Figures[i]; PointNear(a.StartPoint, e.StartPoint); Require(a.IsClosed == e.IsClosed && a.Segments.Count == e.Segments.Count, "area segment count differs");
            for (var s = 0; s < a.Segments.Count; s++)
            {
                Require(a.Segments[s].GetType() == e.Segments[s].GetType(), "area segment type differs");
                if (a.Segments[s] is LineSegment line) PointNear(line.Point, ((LineSegment)e.Segments[s]).Point);
                else if (a.Segments[s] is ArcSegment arc)
                {
                    var other = (ArcSegment)e.Segments[s]; PointNear(arc.Point, other.Point); Near(arc.Size.Width, other.Size.Width); Near(arc.Size.Height, other.Size.Height);
                    Require(arc.SweepDirection == other.SweepDirection && arc.IsLargeArc == other.IsLargeArc && arc.RotationAngle == other.RotationAngle, "area arc differs");
                }
                else if (a.Segments[s] is BezierSegment bezier) { var other = (BezierSegment)e.Segments[s]; PointNear(bezier.Point1, other.Point1); PointNear(bezier.Point2, other.Point2); PointNear(bezier.Point3, other.Point3); }
                else throw new InvalidOperationException("Unexpected area segment.");
            }
        }
    }
    private static void PointNear(Point actual, Point expected) { Near(actual.X, expected.X); Near(actual.Y, expected.Y); }
    private static void Near(double actual, double expected) => Require(Math.Abs(actual - expected) < .001, $"coordinate {actual:R}/{expected:R}");
    private static IEnumerable<T> Descendants<T>(DependencyObject owner) where T : DependencyObject { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++) { var child = VisualTreeHelper.GetChild(owner, i); if (child is T item) yield return item; foreach (var nested in Descendants<T>(child)) yield return nested; } }
    private static T Part<T>(DependencyObject owner, string name) where T : FrameworkElement => Descendants<T>(owner).FirstOrDefault(element => element.Name == name) ?? throw new InvalidOperationException("Missing Area Chart part " + name);
    private async Task AwaitAsync(Func<bool> ready, string message) { var watch = Stopwatch.StartNew(); while (!ready() && watch.Elapsed < TimeSpan.FromSeconds(2)) { UpdateLayout(); if (ready()) break; await Task.Delay(16); } Require(ready(), message); }
    private static async Task FrameAsync() { var frame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); void Rendered(object? sender, object args) => frame.TrySetResult(); CompositionTarget.Rendering += Rendered; try { await frame.Task.WaitAsync(TimeSpan.FromSeconds(2)); } finally { CompositionTarget.Rendering -= Rendered; } }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
