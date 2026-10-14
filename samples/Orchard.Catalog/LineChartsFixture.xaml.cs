using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Orchard.Catalog;

public sealed partial class LineChartsFixture : UserControl
{
    public LineChartsFixture()
    {
        InitializeComponent();
        Chart.Series.Add(new BleeckerChartSeries { Key = "total", Name = "Consumer total" });
        Chart.Series.Add(new BleeckerChartSeries { Key = "selected", Name = "Consumer selected" });
        foreach (var record in new[] { Record("Harbor", 100, 50), Record("Reed", 220, 180), Record("Vale", 140, 90), Record("North", 310, 240) }) Chart.Data.Add(record);
    }
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        using var input = new NativeInputSession(XamlRoot, folder, $"line-chart-{scheme}"); await input.AcquireAsync(Chart); await input.MoveAsync(Before);
        Chart.ApplyTemplate(); UpdateLayout(); var root = (FrameworkElement)VisualTreeHelper.GetChild(Chart, 0);
        var plot = (Canvas)root.FindName("PART_Plot"); var legend = (Canvas)root.FindName("PART_Legend");
        await AwaitAsync(() => plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Count() == 2, "Line chart did not render both consumer series.");
        Require(Chart.ActualWidth == 640 && Chart.ActualHeight == 300, "Line chart lost its source default dimensions.");
        var paths = plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().ToArray();
        var first = (PathGeometry)paths[0].Data;
        Require(first.Figures[0].Segments.Count == 3 && first.Figures[0].Segments.All(segment => segment is BezierSegment), "Native chart did not consume monotone cubic geometry.");
        Require(Math.Abs(first.Figures[0].StartPoint.X - 60) < .001 && Math.Abs(first.Figures[0].StartPoint.Y - 157.8125) < .001, "Native line did not cross the source scale/layout boundary.");
        Require(plot.Children.OfType<Line>().Count() == 5 && legend.Children.Count == 2, "Native chart lost source grid or legend defaults.");
        Require(plot.Children.OfType<TextBlock>().Any(label => label.Text == "Harbor") && plot.Children.OfType<TextBlock>().Any(label => label.Text == "North"),
            "Category tick visibility incorrectly clipped labels to the plot instead of the full chart view box.");
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(Chart).GetName() == "Fictional coverage totals" && Chart.IsTabStop, "Native chart lost its consumer name or single keyboard entry.");
        Require(Chart.Focus(FocusState.Keyboard), "Native chart refused keyboard focus."); input.Key(0x27);
        var popup = (Border)root.FindName("PART_Tooltip"); var items = (StackPanel)root.FindName("PART_TooltipItems");
        await AwaitAsync(() => popup.Visibility == Visibility.Visible && items.Children.OfType<TextBlock>().First().Text == "Reed", "Arrow navigation did not inspect the second consumer category.");
        Require(((Canvas)root.FindName("PART_Active")).Children.OfType<Ellipse>().Count() == 2, "Active chart category did not show both source dots.");
        await NativeScreenCapture.CaptureClientAsync(input.Window, System.IO.Path.Combine(folder, $"line-chart-{scheme}-keyboard.png"));
        Chart.TooltipFormatter = value => $"amount {value}"; Chart.YAxisTickFormatter = value => $"${value}";
        await AwaitAsync(() => items.Children.OfType<TextBlock>().Any(item => item.Text.Contains("amount 220")), "Consumer tooltip formatter did not reach the native overlay.");
        Chart.Series[0].Name = "Updated consumer total";
        await AwaitAsync(() => items.Children.OfType<TextBlock>().Any(item => item.Text.StartsWith("Updated consumer total")), "Series mutation did not update the native chart consumer.");
        var explicitBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent); Chart.Series[0].Color = explicitBrush;
        await AwaitAsync(() => plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Any(path => ReferenceEquals(path.Stroke, explicitBrush)), "Explicit transparent paint lost native precedence.");
        Chart.Data[1] = Record("Reed changed", 80, 40);
        await AwaitAsync(() => items.Children.OfType<TextBlock>().First().Text == "Reed changed", "Consumer record replacement did not update active chart content.");
        Chart.ShowLegend = false; Chart.ShowGrid = false;
        await AwaitAsync(() => legend.Children.Count == 0 && plot.Children.OfType<Line>().Count() == 0, "Grid/legend visibility did not cross the native producer boundary.");
        Chart.Data.Clear(); await AwaitAsync(() => !plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Any() && popup.Visibility == Visibility.Collapsed, "Empty consumer data retained live series or tooltip artifacts.");
        foreach (var record in new[] { Record("Harbor", 100, 50), Record("Reed", 220, 180), Record("Vale", 140, 90), Record("North", 310, 240) }) Chart.Data.Add(record);
        Chart.Series[0].Name = "Consumer total"; Chart.Series[0].Color = null; Chart.TooltipFormatter = null; Chart.YAxisTickFormatter = null; Chart.ShowLegend = true; Chart.ShowGrid = true;
        await AwaitAsync(() => plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Count() == 2 && legend.Children.Count == 2, "Native chart did not restore consumer data and defaults.");
        await input.MoveAsync(Before); await MainWindow.CaptureElementAsync(Chart, System.IO.Path.Combine(folder, $"line-chart-{scheme}.png"));
        var sourceCases = await VerifySourceCasesAsync(scheme, folder, plot, legend);
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"line-chart-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, sourceCases, curves = 2, keyboard = true, liveData = true, explicitTransparent = true }));
    }
    private async Task<int> VerifySourceCasesAsync(BleeckerColorScheme scheme, string folder, Canvas plot, Canvas legend)
    {
        using var stream = typeof(LineChartsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.LineLayout.json") ?? throw new InvalidOperationException("Missing frozen Line Chart layout evidence.");
        using var reference = JsonDocument.Parse(stream);
        Require(reference.RootElement.GetProperty("sourceSha256").GetString() == "673539e9a2c5a264fbfb6c35f09e7d956d2dbde9670f3ae6cf1888e752ec403a" && reference.RootElement.GetProperty("recharts").GetString() == "3.8.1", "Line Chart reference changed its source boundary.");
        var checkedCases = 0;
        foreach (var specimen in reference.RootElement.GetProperty("cases").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == scheme.ToString()))
        {
            var id = specimen.GetProperty("id").GetString()!; var props = specimen.GetProperty("props");
            Chart.Width = specimen.GetProperty("width").GetDouble(); Chart.Height = specimen.GetProperty("height").GetDouble();
            Chart.XAxisKey = props.GetProperty("xAxisKey").GetString();
            Chart.XAxisLabel = props.TryGetProperty("xAxisLabel", out var xLabel) ? xLabel.GetString() : null;
            Chart.YAxisLabel = props.TryGetProperty("yAxisLabel", out var yLabel) ? yLabel.GetString() : null;
            Chart.ShowGrid = !props.TryGetProperty("showGrid", out var showGrid) || showGrid.GetBoolean();
            Chart.ShowLegend = !props.TryGetProperty("showLegend", out var showLegend) || showLegend.GetBoolean();
            Chart.YAxisTickFormatter = props.TryGetProperty("format", out var format) && format.GetBoolean() ? value => $"${value}" : null;
            Chart.Series = new ObservableCollection<BleeckerChartSeries>(props.GetProperty("series").EnumerateArray().Select(item => new BleeckerChartSeries { Key = item.GetProperty("key").GetString()!, Name = item.GetProperty("name").GetString()!, Color = item.TryGetProperty("color", out var color) ? new SolidColorBrush(color.GetString() == "transparent" ? Microsoft.UI.Colors.Transparent : Windows.UI.Color.FromArgb(255, 25, 115, 145)) : null }));
            Chart.Data = new ObservableCollection<IReadOnlyDictionary<string, object?>>(props.GetProperty("data").EnumerateArray().Select(item => (IReadOnlyDictionary<string, object?>)item.EnumerateObject().ToDictionary(property => property.Name, property => Value(property.Value))));
            UpdateLayout(); await Task.Delay(32); UpdateLayout();
            var nativeCurves = plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().ToArray(); var sourceCurves = specimen.GetProperty("curves").EnumerateArray().ToArray();
            Require(nativeCurves.Length == sourceCurves.Length, $"{id}: native/source curve count differs.");
            for (var index = 0; index < sourceCurves.Length; index++)
            {
                CompareCurve((PathGeometry)nativeCurves[index].Data, sourceCurves[index].GetProperty("d").GetString()!, id);
                Require(nativeCurves[index].StrokeThickness==2,$"{id}: native/source line width differs.");
                ComparePaint(nativeCurves[index].Stroke,sourceCurves[index].GetProperty("stroke").GetString()!,id);
            }
            var nativeGrid = plot.Children.OfType<Line>().ToArray(); var sourceGrid = specimen.GetProperty("grid").EnumerateArray().ToArray();
            Require(nativeGrid.Length == sourceGrid.Length, $"{id}: native/source grid count differs.");
            for (var index = 0; index < sourceGrid.Length; index++)
            {
                var expected = sourceGrid[index]; var line = nativeGrid[index];
                Require(Math.Abs(line.X1 - Number(expected.GetProperty("x1"))) < .001 && Math.Abs(line.X2 - Number(expected.GetProperty("x2"))) < .001 && Math.Abs(line.Y1 - Number(expected.GetProperty("y1"))) < .001 && Math.Abs(line.Y2 - Number(expected.GetProperty("y2"))) < .001, $"{id}: native/source grid coordinates differ.");
            }
            var ticks = plot.Children.OfType<TextBlock>().Where(label => label.Text != Chart.XAxisLabel && label.Text != Chart.YAxisLabel).ToArray();
            var expectedTicks = specimen.GetProperty("xTicks").EnumerateArray().Concat(specimen.GetProperty("yTicks").EnumerateArray()).Select(item => item.GetProperty("text").GetString()!).Order(StringComparer.Ordinal).ToArray();
            Require(ticks.Select(label => label.Text).Order(StringComparer.Ordinal).SequenceEqual(expectedTicks), $"{id}: native/source visible tick labels differ: {string.Join('|', ticks.Select(label => label.Text))}.");
            foreach(var expected in specimen.GetProperty("xTicks").EnumerateArray().Concat(specimen.GetProperty("yTicks").EnumerateArray()))
                ComparePaint(ticks.First(label=>label.Text==expected.GetProperty("text").GetString()).Foreground,expected.GetProperty("fill").GetString()!,id);
            var nativeLegend = legend.Children.OfType<StackPanel>().Select(group => group.Children.OfType<TextBlock>().Single().Text).ToArray();
            Require(nativeLegend.SequenceEqual(specimen.GetProperty("legend").EnumerateArray().Select(item => item.GetString()!)), $"{id}: native/source legend order differs.");
            await MainWindow.CaptureElementAsync(Chart, System.IO.Path.Combine(folder, $"line-chart-{scheme}-{id}.png")); checkedCases++;
        }
        Chart.Width = 640; Chart.XAxisLabel = null; Chart.YAxisLabel = null; Chart.YAxisTickFormatter = null;
        Chart.Series = new ObservableCollection<BleeckerChartSeries> { new() { Key = "total", Name = "Consumer total" }, new() { Key = "selected", Name = "Consumer selected" } };
        Chart.Data = new ObservableCollection<IReadOnlyDictionary<string, object?>> { Record("Harbor", 100, 50), Record("Reed", 220, 180), Record("Vale", 140, 90), Record("North", 310, 240) };
        Require(checkedCases == 11, "Frozen Line Chart specimen count changed."); return checkedCases;
    }
    private static object? Value(JsonElement value) => value.ValueKind switch { JsonValueKind.String => value.GetString(), JsonValueKind.Number => value.GetDouble(), JsonValueKind.Null => null, _ => throw new InvalidOperationException("Unexpected frozen chart data type.") };
    private static double Number(JsonElement value) => value.ValueKind == JsonValueKind.Number ? value.GetDouble() : double.Parse(value.GetString()!, CultureInfo.InvariantCulture);
    private static void ComparePaint(Brush actual,string expected,string id)
    {
        Require(actual is SolidColorBrush,$"{id}: source paint did not resolve to a native solid brush.");
        var brush=(SolidColorBrush)actual;var channels=Regex.Matches(expected,@"[0-9]+(?:\.[0-9]+)?").Select(match=>double.Parse(match.Value,CultureInfo.InvariantCulture)).ToArray();
        Require(channels.Length is 3 or 4,$"{id}: unexpected source paint syntax.");var alpha=channels.Length==4?channels[3]:1;
        Require(Math.Abs(brush.Color.A/255d*brush.Opacity-alpha)<1e-9&&(alpha==0||brush.Color.R==channels[0]&&brush.Color.G==channels[1]&&brush.Color.B==channels[2]),$"{id}: native/source chart paint differs.");
    }
    private static void CompareCurve(PathGeometry actual, string expected, string id)
    {
        var commands = new List<(string Kind, double[] Values)>();
        foreach (var figure in actual.Figures)
        {
            commands.Add(("M", [figure.StartPoint.X, figure.StartPoint.Y]));
            foreach (var segment in figure.Segments)
                if (segment is BezierSegment cubic) commands.Add(("C", [cubic.Point1.X, cubic.Point1.Y, cubic.Point2.X, cubic.Point2.Y, cubic.Point3.X, cubic.Point3.Y]));
                else if (segment is LineSegment line) commands.Add(("L", [line.Point.X, line.Point.Y]));
                else throw new InvalidOperationException("Unexpected chart path segment.");
            if (figure.IsClosed) commands.Add(("Z", []));
        }
        var source = Regex.Matches(expected, "([MLCZ])([^MLCZ]*)").ToArray(); Require(source.Length == commands.Count, $"{id}: source/native command count differs.");
        for (var index = 0; index < source.Length; index++)
        {
            var values = source[index].Groups[2].Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(value => double.Parse(value, CultureInfo.InvariantCulture)).ToArray(); var command = commands[index];
            Require(source[index].Groups[1].Value == command.Kind && values.Length == command.Values.Length, $"{id}: source/native command kind differs.");
            // D3's frozen SVG producer rounds coordinates to three decimal places.
            for (var coordinate = 0; coordinate < values.Length; coordinate++) Require(Math.Abs(values[coordinate] - command.Values[coordinate]) <= .000501, $"{id}: source/native curve coordinate differs.");
        }
    }
    private static IReadOnlyDictionary<string, object?> Record(string label, double total, double selected) => new Dictionary<string, object?> { { "label", label }, { "total", total }, { "selected", selected } };
    private async Task AwaitAsync(Func<bool> condition, string message)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew(); while (!condition() && clock.Elapsed < TimeSpan.FromSeconds(2)) { UpdateLayout(); await Task.Delay(16); }
        Require(condition(), message);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
