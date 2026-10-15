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
public sealed partial class PieChartsFixture : UserControl
{
    public PieChartsFixture() { InitializeComponent(); DonutSpecimen.Data = Specimen.Data; }
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        Require(XamlRoot.RasterizationScale == 1 && new Windows.UI.ViewManagement.UISettings().TextScaleFactor == 1, "Frozen Pie checks require display/text scale 1.");
        using var input = new NativeInputSession(XamlRoot, folder, $"pie-chart-{scheme}"); await input.AcquireAsync(Specimen);
        var root = (Grid)XamlRoot.Content; var host = new Grid { Margin = new Thickness(20), Background = root.Background };
        Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count); root.Children.Add(host);
        using var reference = JsonDocument.Parse(typeof(PieChartsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.PieChartLayout.json")!);
        var reports = new List<object>(); var failures = new List<string>();
        try {
            foreach (var item in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("scheme").GetString() == scheme.ToString()))
            {
                var id = item.GetProperty("id").GetString()!; var props = item.GetProperty("props"); var expected = item.GetProperty("normal"); var donut = props.GetProperty("mode").GetString() == "donut";
                var rows = new ObservableCollection<IReadOnlyDictionary<string, object?>>(props.GetProperty("data").EnumerateArray().Select(row => (IReadOnlyDictionary<string, object?>)Read(row)!));
                foreach (var row in rows.Cast<Dictionary<string, object?>>()) if (row.TryGetValue("color", out var color) && color is string text) row["color"] = new SolidColorBrush(Paint(text));
                Control owner;
                if (donut) owner = new BleeckerDonutChart { Data = rows, ShowLegend = !props.TryGetProperty("showLegend", out var dl) || dl.GetBoolean(), CenterValue = props.TryGetProperty("centerValue", out var cv) ? cv.GetString() : null, CenterLabel = props.TryGetProperty("centerLabel", out var cl) ? cl.GetString() : null };
                else owner = new BleeckerPieChart { Data = rows, ShowLegend = !props.TryGetProperty("showLegend", out var pl) || pl.GetBoolean(), NameKey = props.TryGetProperty("nameKey", out var nk) ? nk.GetString()! : "name", DataKey = props.TryGetProperty("dataKey", out var dk) ? dk.GetString()! : "value", InnerRadius = props.TryGetProperty("innerRadius", out var radius) ? Read(radius)! : 0d };
                owner.Width = expected.GetProperty("width").GetDouble(); owner.Height = expected.GetProperty("height").GetDouble(); owner.HorizontalAlignment = HorizontalAlignment.Left; owner.VerticalAlignment = VerticalAlignment.Top;
                AutomationProperties.SetName(owner, "Consumer circular comparison"); host.Children.Clear(); host.Children.Add(owner);
                await AwaitAsync(() => owner.IsLoaded, "Pie did not mount."); owner.ApplyTemplate(); UpdateLayout(); await FrameAsync(); UpdateLayout();
                var chart = Descendants<BleeckerLineChart>(owner).Single(); var plot = Part<Canvas>(chart, "PART_Plot"); var legend = Part<Canvas>(chart, "PART_Legend");
                var errors = new List<string>(); void Check(Action action) { try { action(); } catch (InvalidOperationException error) { errors.Add(error.Message); } }
                var paths = plot.Children.OfType<Path>().ToArray(); var source = expected.GetProperty("sectors").EnumerateArray().ToArray();
                Check(() => Require(paths.Length == source.Length, $"sectors {paths.Length}/{source.Length}"));
                for (var i = 0; i < Math.Min(paths.Length, source.Length); i++) { var n = i; Check(() => { CompareGeometry((PathGeometry)paths[n].Data, source[n].GetProperty("path").GetString()!); ComparePaint(paths[n].Fill, source[n].GetProperty("fill").GetString()!); ComparePaint(paths[n].Stroke, source[n].GetProperty("stroke").GetString()!); Near(paths[n].StrokeThickness, 1); Require(((PathGeometry)paths[n].Data).Figures.All(figure => figure.IsFilled), "sector not filled"); }); }
                var labels = plot.Children.OfType<TextBlock>().ToArray(); var sourceLabels = expected.GetProperty("labels").EnumerateArray().ToArray();
                Check(() => Require(labels.Select(label => label.Text).SequenceEqual(sourceLabels.Select(label => label.GetProperty("text").GetString())), "slice labels differ"));
                for (var i = 0; i < Math.Min(labels.Length, sourceLabels.Length); i++) { var n = i; Check(() => { ComparePaint(labels[n].Foreground, sourceLabels[n].GetProperty("fill").GetString()!); Near(Canvas.GetTop(labels[n]) + labels[n].BaselineOffset, double.Parse(sourceLabels[n].GetProperty("y").GetString()!, System.Globalization.CultureInfo.InvariantCulture)); }); }
                var legendGroups = legend.Children.OfType<Canvas>().ToArray(); var legends = legendGroups.Select(panel => panel.Children.OfType<TextBlock>().Single()).ToArray();
                Check(() => Require(legends.Select(label => label.Text).SequenceEqual(expected.GetProperty("legend").EnumerateArray().Select(label => label.GetProperty("text").GetString())), "legend text differs"));
                var legendSource = expected.GetProperty("legend").EnumerateArray().ToArray();
                for (var i = 0; i < Math.Min(legends.Length, legendSource.Length); i++) { var n = i; Check(() => {
                    Near(Canvas.GetTop(legendGroups[n]) + Canvas.GetTop(legends[n]) + legends[n].BaselineOffset, legendSource[n].GetProperty("box").GetProperty("y").GetDouble() + 16);
                    Near(Canvas.GetTop(legendGroups[n].Children.OfType<Viewbox>().Single()), 6.65625);
                    ComparePaint(legends[n].Foreground, legendSource[n].GetProperty("color").GetString()!);
                }); }
                Check(() => Require(!owner.IsTabStop && chart.IsTabStop && ReferenceEquals(chart.Data, rows) && AutomationProperties.GetName(chart) == "Consumer circular comparison" && FrameworkElementAutomationPeer.CreatePeerForElement(chart).GetClassName() == (donut ? nameof(BleeckerDonutChart) : nameof(BleeckerPieChart)), "inspection identity or original data differs"));
                if (donut) Check(() => {
                    var center = Part<Canvas>(owner, "PART_Center"); var text = center.Children.OfType<TextBlock>().ToArray(); var centerSource = expected.GetProperty("center").EnumerateArray().ToArray();
                    Require(text.Select(t => t.Text).SequenceEqual(centerSource.Select(t => t.GetProperty("visibleText").GetString())), "center text differs");
                    for (var i = 0; i < text.Length; i++) {
                        ComparePaint(text[i].Foreground, centerSource[i].GetProperty("color").GetString()!); Near(text[i].FontSize, double.Parse(centerSource[i].GetProperty("fontSize").GetString()!.Replace("px", ""), System.Globalization.CultureInfo.InvariantCulture));
                        var value = Equals(text[i].Tag, "pie-center-value");
                        Near(text[i].LineHeight, value ? 32 : 16); Near(text[i].FontWeight.Weight, value ? 600 : 400); Near(text[i].CharacterSpacing, value ? -20 : 0);
                        Near(Canvas.GetTop(text[i]) + text[i].BaselineOffset, centerSource[i].GetProperty("box").GetProperty("y").GetDouble() + (value ? 26 : 12));
                        var plain = new TextBlock { Text = text[i].Text, FontFamily = text[i].FontFamily, FontSize = text[i].FontSize, FontWeight = text[i].FontWeight, UseLayoutRounding = false }; plain.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        Require(Math.Abs(Canvas.GetLeft(text[i]) - centerSource[i].GetProperty("box").GetProperty("x").GetDouble()) < .001, $"center x={Canvas.GetLeft(text[i]):R}/{centerSource[i].GetProperty("box").GetProperty("x").GetDouble():R}, nativeWidth={text[i].DesiredSize.Width:R}, unspaced={plain.DesiredSize.Width:R}, text={text[i].Text}");
                    }
                });
                if (id is "default" or "narrow" or "center" or "mixed-sign" or "long-labels" or "ring-large") await NativeScreenCapture.CaptureClientAsync(input.Window, System.IO.Path.Combine(folder, $"pie-chart-{id}-{donut}-{scheme}.png"));
                Require(chart.Focus(FocusState.Keyboard), "Could not focus native Pie inspection.");
                await AwaitAsync(() => InspectionText(chart) == item.GetProperty("focus").GetProperty("tooltip").GetString(), "Pie focus payload did not settle."); Check(() => Inspection(chart, item.GetProperty("focus")));
                foreach (var (state, key) in new[] { ("right", (ushort)0x27), ("left", (ushort)0x25), ("enter", (ushort)0x0D) }) { await input.KeyAsync(key, chart); await AwaitAsync(() => InspectionText(chart) == item.GetProperty(state).GetProperty("tooltip").GetString(), "Pie " + state + " payload did not settle."); Check(() => Inspection(chart, item.GetProperty(state))); }
                reports.Add(new { id, donut, errors, sectors = paths.Length }); failures.AddRange(errors.Select(error => $"{id}/{donut}: {error}"));
                await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"pie-chart-{scheme}.json"), JsonSerializer.Serialize(new { passed = failures.Count == 0, reports }, new JsonSerializerOptions { WriteIndented = true }));
            }
            Require(failures.Count == 0, "Pie/Donut mismatches: " + string.Join(" | ", failures));
            await VerifyMutationAsync(host, input, folder, scheme);
        } finally { root.Children.Remove(host); }
    }
    private static string InspectionText(BleeckerLineChart chart) => string.Concat(Part<StackPanel>(chart, "PART_TooltipItems").Children.OfType<TextBlock>().Select(text => text.Text));
    private static void Inspection(BleeckerLineChart chart, JsonElement expected) {
        Require((Part<Border>(chart, "PART_Tooltip").Visibility == Visibility.Visible) == expected.GetProperty("tooltipVisible").GetBoolean(), "tooltip visibility differs");
        Require(Part<Canvas>(chart, "PART_Active").Children.Count == 0, "Pie manufactured an active cursor or dot");
    }
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
        Require(actual.Figures.Count == expected.Figures.Count, "pie figure count differs");
        for (var i = 0; i < actual.Figures.Count; i++)
        {
            var a = actual.Figures[i]; var e = expected.Figures[i]; PointNear(a.StartPoint, e.StartPoint); Require(a.IsClosed == e.IsClosed && a.Segments.Count == e.Segments.Count, "pie segment count differs");
            for (var s = 0; s < a.Segments.Count; s++)
            {
                Require(a.Segments[s].GetType() == e.Segments[s].GetType(), "pie segment type differs");
                if (a.Segments[s] is LineSegment line) PointNear(line.Point, ((LineSegment)e.Segments[s]).Point);
                else if (a.Segments[s] is ArcSegment arc)
                {
                    var other = (ArcSegment)e.Segments[s]; PointNear(arc.Point, other.Point); Near(arc.Size.Width, other.Size.Width); Near(arc.Size.Height, other.Size.Height);
                    Require(arc.SweepDirection == other.SweepDirection && arc.IsLargeArc == other.IsLargeArc && arc.RotationAngle == other.RotationAngle, "pie arc differs");
                }
                else if (a.Segments[s] is BezierSegment bezier) { var other = (BezierSegment)e.Segments[s]; PointNear(bezier.Point1, other.Point1); PointNear(bezier.Point2, other.Point2); PointNear(bezier.Point3, other.Point3); }
                else throw new InvalidOperationException("Unexpected pie segment.");
            }
        }
    }
    private static void PointNear(Point actual, Point expected) { Near(actual.X, expected.X); Near(actual.Y, expected.Y); }
    private static void Near(double actual, double expected) => Require(Math.Abs(actual - expected) < .001, $"coordinate {actual:R}/{expected:R}");
    private static IEnumerable<T> Descendants<T>(DependencyObject owner) where T : DependencyObject { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++) { var child = VisualTreeHelper.GetChild(owner, i); if (child is T item) yield return item; foreach (var nested in Descendants<T>(child)) yield return nested; } }
    private static T Part<T>(DependencyObject owner, string name) where T : FrameworkElement => Descendants<T>(owner).FirstOrDefault(element => element.Name == name) ?? throw new InvalidOperationException("Missing Pie Chart part " + name);
    private async Task AwaitAsync(Func<bool> ready, string message) { var watch = Stopwatch.StartNew(); var readiness = new NativeReadiness(DispatcherQueue); while (!ready() && watch.Elapsed < TimeSpan.FromSeconds(2)) { UpdateLayout(); if (ready()) break; await readiness.WaitAsync(); } Require(ready(), message); }
    private static async Task FrameAsync() { var frame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); void Rendered(object? sender, object args) => frame.TrySetResult(); CompositionTarget.Rendering += Rendered; try { await frame.Task.WaitAsync(TimeSpan.FromSeconds(2)); } finally { CompositionTarget.Rendering -= Rendered; } }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
