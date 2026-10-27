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

public sealed partial class BarChartsFixture : UserControl
{
    public BarChartsFixture()
    {
        InitializeComponent();
        foreach (var (name, total, selected) in new[] { ("Harbor", 100d, 50d), ("Reed", 220d, 180d), ("Vale", 140d, 90d), ("North", 310d, 240d) })
            Specimen.Data.Add(new Dictionary<string, object?> { ["label"] = name, ["total"] = total, ["selected"] = selected });
        Vertical.Data = Specimen.Data;
    }
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        Require(XamlRoot.RasterizationScale == 1 && new Windows.UI.ViewManagement.UISettings().TextScaleFactor == 1, "Frozen Bar Chart checks require display/text scale 1.");
        using var input = new NativeInputSession(XamlRoot, folder, $"bar-chart-{scheme}"); await input.AcquireAsync(Specimen);
        var root = CatalogFixtureRoot.From(XamlRoot);
        var host = new Grid { Margin = new Thickness(20), Background = root.Background };
        var outside = new Button { Content = "Outside chart", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Style = (Style)Application.Current.Resources["BleeckerSecondaryButtonStyle"] };
        Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count); root.Children.Add(host);
        using var reference = JsonDocument.Parse(CatalogReferenceResources.Open(typeof(BarChartsFixture).Assembly, "Orchard.Catalog.Reference.BarChartLayout.json")!);
        var reports = new List<object>(); var failures = new List<string>();
        try
        {
            foreach (var item in reference.RootElement.GetProperty("cases").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == scheme.ToString()))
            {
                var id = item.GetProperty("id").GetString()!; var props = item.GetProperty("props"); var expected = item.GetProperty("normal");
                var chart = new BleeckerBarChart
                {
                    Data = new(props.GetProperty("data").EnumerateArray().Select(row => (IReadOnlyDictionary<string, object?>)Read(row)!)),
                    XAxisKey = props.GetProperty("xAxisKey").GetString(), Layout = Enum.Parse<BleeckerBarChartLayout>(props.GetProperty("layout").GetString()!, true),
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
                    chart.YAxisTickFormatter = value => value is double number ? "Y" + number.ToString(System.Globalization.CultureInfo.InvariantCulture) : "Category " + value;
                    chart.TooltipFormatter = value => "Value " + value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                AutomationProperties.SetName(chart, "Consumer bar comparison"); host.Children.Clear(); host.Children.Add(chart); host.Children.Add(outside);
                await AwaitAsync(() => chart.IsLoaded, "Bar Chart did not mount."); chart.ApplyTemplate(); UpdateLayout(); await FrameAsync(); UpdateLayout();
                var inner = Part<BleeckerLineChart>(chart, "PART_Chart"); var plot = Part<Canvas>(inner, "PART_Plot"); var legend = Part<Canvas>(inner, "PART_Legend");
                var errors = new List<string>();
                void Check(Action assertion) { try { assertion(); } catch (InvalidOperationException error) { errors.Add(error.Message); } }
                var bars = plot.Children.OfType<Path>().ToArray(); var sourceBars = expected.GetProperty("bars").EnumerateArray().ToArray();
                Check(() => Require(bars.Length == sourceBars.Length, $"bar count {bars.Length}/{sourceBars.Length}"));
                for (var i = 0; i < Math.Min(bars.Length, sourceBars.Length); i++)
                {
                    var bar = i;
                    Check(() => CompareGeometry((PathGeometry)bars[bar].Data, sourceBars[bar].GetProperty("path").GetString()!));
                    Check(() => ComparePaint(bars[bar].Fill, sourceBars[bar].GetProperty("fill").GetString()!));
                }
                var lines = plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Line>().ToArray(); var sourceGrid = expected.GetProperty("grid").EnumerateArray().ToArray();
                Check(() => Require(lines.Length == sourceGrid.Length, $"grid count {lines.Length}/{sourceGrid.Length}"));
                for (var i = 0; i < Math.Min(lines.Length, sourceGrid.Length); i++)
                {
                    var line = i; Check(() => { Near(lines[line].X1, sourceGrid[line].GetProperty("x1").GetDouble()); Near(lines[line].X2, sourceGrid[line].GetProperty("x2").GetDouble()); Near(lines[line].Y1, sourceGrid[line].GetProperty("y1").GetDouble()); Near(lines[line].Y2, sourceGrid[line].GetProperty("y2").GetDouble()); });
                }
                foreach (var axis in new[] { "x", "y" }) Check(() =>
                {
                    var labels = plot.Children.OfType<TextBlock>().Where(text => ((string?)text.Tag)?.StartsWith("bar-" + axis + "-", StringComparison.Ordinal) == true)
                        .GroupBy(text => (string)text.Tag).Select(group => string.Concat(group.Select(text => text.Text))).ToArray();
                    var source = expected.GetProperty(axis + "Ticks").EnumerateArray().Select(tick => tick.GetProperty("text").GetString()).ToArray();
                    // Numeric ticks share a tag; categories have one group per row.
                    if (chart.Layout == BleeckerBarChartLayout.Horizontal && axis == "y" || chart.Layout == BleeckerBarChartLayout.Vertical && axis == "x")
                        labels = plot.Children.OfType<TextBlock>().Where(text => Equals(text.Tag, "bar-" + axis + "-tick")).Select(text => text.Text).ToArray();
                    Require(labels.SequenceEqual(source), axis + " labels: " + string.Join("|", labels) + " / " + string.Join("|", source));
                });
                Check(() => Require(legend.Children.Count == expected.GetProperty("legend").GetArrayLength(), "legend entries differ"));
                if (chart.Layout == BleeckerBarChartLayout.Vertical) Check(() =>
                {
                    var actualLines = plot.Children.OfType<TextBlock>().Where(text => ((string?)text.Tag)?.StartsWith("bar-y-category-", StringComparison.Ordinal) == true).GroupBy(text => (string)text.Tag).ToArray();
                    var markup = expected.GetProperty("yMarkup").EnumerateArray().ToArray();
                    Require(actualLines.Length == markup.Length, "category line groups differ");
                    for (var group = 0; group < actualLines.Length; group++)
                    {
                        var source = System.Xml.Linq.XElement.Parse(markup[group].GetString()!); var spans = source.Elements().ToArray(); var native = actualLines[group].ToArray();
                        Require(native.Length == spans.Length, "category wrap line count differs");
                        var baseline = double.Parse(source.Attribute("y")!.Value, System.Globalization.CultureInfo.InvariantCulture);
                        for (var line = 0; line < native.Length; line++)
                        {
                            Require(native[line].Text == spans[line].Value, "category wrap text differs");
                            baseline += double.Parse(spans[line].Attribute("dy")!.Value.Replace("em", ""), System.Globalization.CultureInfo.InvariantCulture) * 12;
                            Near(Canvas.GetTop(native[line]) + native[line].BaselineOffset, baseline);
                        }
                    }
                });
                Check(() => Require(ReferenceEquals(chart.Data, inner.Data) && ReferenceEquals(chart.Series, inner.Series) && !chart.IsTabStop && inner.IsTabStop && AutomationProperties.GetName(inner) == "Consumer bar comparison" && FrameworkElementAutomationPeer.CreatePeerForElement(inner).GetClassName() == nameof(BleeckerBarChart), "consumer identity or native inspection boundary differs"));
                if (id is "default" or "narrow" or "negative" or "types" or "long-labels")
                    await NativeScreenCapture.CaptureClientAsync(input.Window, System.IO.Path.Combine(folder, $"bar-chart-{id}-{chart.Layout}-{scheme}.png"));
                var handover = id == "default" && chart.Layout == BleeckerBarChartLayout.Horizontal;
                if (handover)
                {
                    var firstBar = expected.GetProperty("bars")[0];
                    await input.MoveAsync(inner, localPoint: new Point(firstBar.GetProperty("x").GetDouble() + firstBar.GetProperty("width").GetDouble() / 2, firstBar.GetProperty("y").GetDouble() + firstBar.GetProperty("height").GetDouble() / 2));
                    await AwaitAsync(() => InspectionText(inner) == item.GetProperty("focus").GetProperty("tooltip").GetString(), "Bar Chart physical hover did not inspect the first source category.");
                }
                await FocusAsync(inner);
                await AwaitAsync(() => InspectionText(inner) == item.GetProperty("focus").GetProperty("tooltip").GetString(), "Bar Chart focus inspection did not settle.");
                Check(() => VerifyInspection(inner, item.GetProperty("focus"), false));
                if (handover)
                {
                    await input.KeyAsync(0x27, inner);
                    Require(Part<Border>(inner, "PART_Tooltip").Visibility == Visibility.Visible, "Bar Chart hid the source mouse-priority tooltip during a real key action.");
                    Check(() => VerifyInspection(inner, item.GetProperty("focus"), false));
                    await input.MoveAsync(outside);
                    await AwaitAsync(() => InspectionText(inner) == item.GetProperty("key").GetProperty("tooltip").GetString(), "Bar Chart pointer exit did not restore the remembered keyboard category.");
                    Check(() => VerifyInspection(inner, item.GetProperty("key"), true));
                    await input.KeyAsync(0x25, inner);
                    await AwaitAsync(() => InspectionText(inner) == item.GetProperty("focus").GetProperty("tooltip").GetString(), "Bar Chart Left did not restore its initial category after handover.");
                }
                await input.KeyAsync(chart.Layout == BleeckerBarChartLayout.Horizontal ? (ushort)0x27 : (ushort)0x28, inner);
                await AwaitAsync(() => InspectionText(inner) == item.GetProperty("key").GetProperty("tooltip").GetString(), "Bar Chart key inspection did not settle.");
                Check(() => VerifyInspection(inner, item.GetProperty("key"), true));
                await input.KeyAsync(0x27, inner);
                await AwaitAsync(() => InspectionText(inner) == item.GetProperty("right").GetProperty("tooltip").GetString(), "Bar Chart Right inspection did not settle.");
                Check(() => VerifyInspection(inner, item.GetProperty("right"), true));
                reports.Add(new { id, layout = chart.Layout.ToString(), errors, bars = bars.Length, grid = lines.Length, labels = plot.Children.OfType<TextBlock>().Select(text => text.Text).ToArray() });
                failures.AddRange(errors.Select(error => id + "/" + chart.Layout + ": " + error));
                await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"bar-chart-{scheme}.json"), JsonSerializer.Serialize(new { passed = failures.Count == 0, reports }, new JsonSerializerOptions { WriteIndented = true }));
            }
            Require(failures.Count == 0, "Bar Chart mismatches: " + string.Join(" | ", failures));
            await VerifyMutationAsync(host, input, folder, scheme);
        }
        finally { root.Children.Remove(host); }
    }
    private static void VerifyInspection(BleeckerLineChart chart, JsonElement expected, bool visibility)
    {
        var text = string.Concat(Part<StackPanel>(chart, "PART_TooltipItems").Children.OfType<TextBlock>().Select(text => text.Text));
        Require(text == expected.GetProperty("tooltip").GetString(), "tooltip: " + text + "/" + expected.GetProperty("tooltip").GetString());
        if (visibility) Require((Part<Border>(chart, "PART_Tooltip").Visibility == Visibility.Visible) == expected.GetProperty("visible").GetBoolean(), "tooltip visibility differs");
        var cursors = Part<Canvas>(chart, "PART_Plot").Children.OfType<Microsoft.UI.Xaml.Shapes.Rectangle>().ToArray(); var source = expected.GetProperty("cursor").EnumerateArray().ToArray();
        Require(cursors.Length == source.Length, "category cursor count differs");
        for (var i = 0; i < cursors.Length; i++)
        {
            Near(Canvas.GetLeft(cursors[i]), double.Parse(source[i].GetProperty("x").GetString()!, System.Globalization.CultureInfo.InvariantCulture));
            Near(Canvas.GetTop(cursors[i]), double.Parse(source[i].GetProperty("y").GetString()!, System.Globalization.CultureInfo.InvariantCulture));
            Near(cursors[i].Width, double.Parse(source[i].GetProperty("width").GetString()!, System.Globalization.CultureInfo.InvariantCulture));
            Near(cursors[i].Height, double.Parse(source[i].GetProperty("height").GetString()!, System.Globalization.CultureInfo.InvariantCulture));
            ComparePaint(cursors[i].Fill, source[i].GetProperty("fill").GetString()!);
        }
    }
    private static string InspectionText(BleeckerLineChart chart) => string.Concat(Part<StackPanel>(chart, "PART_TooltipItems").Children.OfType<TextBlock>().Select(text => text.Text));
    private static async Task FocusAsync(BleeckerLineChart chart)
    {
        var focused = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); void Focused(object sender, RoutedEventArgs args) { if (ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(chart.XamlRoot), chart)) focused.TrySetResult(); }
        chart.GotFocus += Focused;
        try { Require(chart.Focus(FocusState.Keyboard), "Could not focus Bar Chart."); await focused.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
        finally { chart.GotFocus -= Focused; }
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
        Require(actual.Figures.Count == expected.Figures.Count, "bar figure count differs");
        for (var i = 0; i < actual.Figures.Count; i++)
        {
            var a = actual.Figures[i]; var e = expected.Figures[i]; PointNear(a.StartPoint, e.StartPoint); Require(a.IsClosed == e.IsClosed && a.Segments.Count == e.Segments.Count, "bar segment count differs");
            for (var s = 0; s < a.Segments.Count; s++)
            {
                Require(a.Segments[s].GetType() == e.Segments[s].GetType(), "bar segment type differs");
                if (a.Segments[s] is LineSegment line) PointNear(line.Point, ((LineSegment)e.Segments[s]).Point);
                else if (a.Segments[s] is ArcSegment arc)
                {
                    var other = (ArcSegment)e.Segments[s]; PointNear(arc.Point, other.Point); Near(arc.Size.Width, other.Size.Width); Near(arc.Size.Height, other.Size.Height);
                    Require(arc.SweepDirection == other.SweepDirection && arc.IsLargeArc == other.IsLargeArc && arc.RotationAngle == other.RotationAngle, "bar arc differs");
                }
                else throw new InvalidOperationException("Unexpected bar segment.");
            }
        }
    }
    private static void PointNear(Point actual, Point expected) { Near(actual.X, expected.X); Near(actual.Y, expected.Y); }
    private static void Near(double actual, double expected) => Require(Math.Abs(actual - expected) < .001, $"coordinate {actual:R}/{expected:R}");
    private static IEnumerable<T> Descendants<T>(DependencyObject owner) where T : DependencyObject { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++) { var child = VisualTreeHelper.GetChild(owner, i); if (child is T item) yield return item; foreach (var nested in Descendants<T>(child)) yield return nested; } }
    private static T Part<T>(DependencyObject owner, string name) where T : FrameworkElement => Descendants<T>(owner).FirstOrDefault(element => element.Name == name) ?? throw new InvalidOperationException("Missing Bar Chart part " + name);
    private async Task AwaitAsync(Func<bool> ready, string message) { var watch = Stopwatch.StartNew(); var readiness = new NativeReadiness(DispatcherQueue); while (!ready() && watch.Elapsed < TimeSpan.FromSeconds(2)) { UpdateLayout(); if (ready()) break; await readiness.WaitAsync(); } Require(ready(), message); }
    private static async Task FrameAsync() { var frame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); void Rendered(object? sender, object args) => frame.TrySetResult(); CompositionTarget.Rendering += Rendered; try { await frame.Task.WaitAsync(TimeSpan.FromSeconds(2)); } finally { CompositionTarget.Rendering -= Rendered; } }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
