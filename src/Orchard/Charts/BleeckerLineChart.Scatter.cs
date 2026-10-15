using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard;

public sealed partial class BleeckerLineChart
{
    private bool scatterMode;
    private ObservableCollection<BleeckerScatterSeries>? scatterSeries, observedScatterSeries;
    private readonly HashSet<BleeckerScatterSeries> observedScatterItems = [];
    private readonly HashSet<ObservableCollection<IReadOnlyDictionary<string, object?>>> observedScatterData = [];
    private Func<object?, string>? scatterXFormatter, scatterYFormatter;
    private readonly List<ScatterPoint> scatterPoints = [];
    private readonly Dictionary<(BleeckerScatterSeries Series, string Key, int Occurrence, int Row), Path> scatterShapes = [];
    private readonly Dictionary<Path, int> scatterShapePalettes = [];
    private Canvas? scatterPlot;
    private sealed record ScatterPoint(BleeckerScatterSeries Series, int SeriesIndex, int RowIndex, IReadOnlyDictionary<string, object?> Record, double? X, double? Y, double Radius);
    internal void UseScatterProfile(ObservableCollection<BleeckerScatterSeries> series, Func<object?, string>? xFormatter, Func<object?, string>? yFormatter)
    {
        var changed = !scatterMode || !ReferenceEquals(scatterSeries, series);
        scatterMode = true; sparklineMode = barMode = areaMode = pieMode = false;
        scatterSeries = series; scatterXFormatter = xFormatter; scatterYFormatter = yFormatter;
        if (changed && IsLoaded) Observe(); QueueRender();
    }
    private void ObserveScatter()
    {
        if (!scatterMode || scatterSeries is null) return;
        observedScatterSeries = scatterSeries; observedScatterSeries.CollectionChanged += OnCollectionChanged;
        foreach (var item in scatterSeries)
        {
            if (observedScatterItems.Add(item)) item.PropertyChanged += OnScatterSeriesChanged;
            if (item.Data is not { } data) continue;
            if (!ReferenceEquals(data, Data) && observedScatterData.Add(data)) data.CollectionChanged += OnCollectionChanged;
            foreach (var record in data.OfType<INotifyPropertyChanged>()) if (observedRecords.Add(record)) record.PropertyChanged += OnRecordChanged;
        }
    }
    private void StopObservingScatter()
    {
        if (observedScatterSeries is not null) observedScatterSeries.CollectionChanged -= OnCollectionChanged;
        foreach (var item in observedScatterItems) item.PropertyChanged -= OnScatterSeriesChanged;
        foreach (var data in observedScatterData) data.CollectionChanged -= OnCollectionChanged;
        observedScatterSeries = null; observedScatterItems.Clear(); observedScatterData.Clear();
    }
    private void OnScatterSeriesChanged(object? sender, PropertyChangedEventArgs args) { Observe(); QueueRender(); }
    private int InspectionCount => scatterMode ? scatterSeries?.Sum(series => (series.Data ?? Data).Count) ?? 0 : Data.Count;
    private static double? ScatterNumber(object? value) => value is bool flag ? flag ? 1 : 0 : BleeckerChartData.Number(value);
    private static bool ScatterTruthy(object? value) => value is not null && value is not false && value is not "" && !(value is not string and not bool && BleeckerChartData.Number(value) is double number && number == 0) && !(value is double invalid && double.IsNaN(invalid));
    private static string ScatterCircle(double radius)
    {
        var r = (Math.Floor(radius * 1000 + .5) / 1000).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        return $"M{r},0A{r},{r},0,1,1,-{r},0A{r},{r},0,1,1,{r},0";
    }
    private void RenderScatter()
    {
        if (scatterSeries is null || scatterSeries.Count == 0) throw new InvalidOperationException("A mounted Scatter Chart requires at least one series, as the first series owns its axes.");
        foreach (var series in scatterSeries) { _ = series.Key; _ = series.Name; _ = series.XKey; _ = series.YKey; }
        if (!ReferenceEquals(scatterPlot, plot)) { scatterShapes.Clear(); scatterShapePalettes.Clear(); scatterPlot = plot; }
        var retained = scatterShapes.Values.ToHashSet();
        foreach (var child in plot!.Children.ToArray()) if (child is not Path path || !retained.Contains(path)) plot.Children.Remove(child);
        plot.Clip = null; legend!.Children.Clear(); rendered.Clear(); scatterPoints.Clear();
        var usedShapes = new HashSet<(BleeckerScatterSeries Series, string Key, int Occurrence, int Row)>();
        var occurrences = new Dictionary<BleeckerScatterSeries, int>();
        var first = scatterSeries[0]; var records = scatterSeries.SelectMany(series => series.Data ?? Data).ToArray();
        left = 60 + (string.IsNullOrEmpty(YAxisLabel) ? 0 : 20); right = RenderWidth - 20; top = 10;
        bottom = RenderHeight - (string.IsNullOrEmpty(XAxisLabel) ? 5 : 25) - 30 - RenderScatterLegend();
        var xTicks = BleeckerChartScale.Ticks(records.Select(row => BleeckerChartData.Number(BleeckerChartData.Read(row, first.XKey))).OfType<double>());
        var yTicks = BleeckerChartScale.Ticks(records.Select(row => BleeckerChartData.Number(BleeckerChartData.Read(row, first.YKey))).OfType<double>());
        double XValue(double value) => BleeckerChartScale.Position(value, xTicks[0], xTicks[^1], left, right);
        double YValue(double value) => BleeckerChartScale.Position(value, yTicks[0], yTicks[^1], bottom, top);
        if (ShowGrid)
        {
            var xs = xTicks.Select(XValue).Append(left).Append(right).Distinct(); var ys = yTicks.Select(YValue).Append(top).Append(bottom).Distinct();
            foreach (var y in ys) BarGridLine(left, y, right, y); foreach (var x in xs) BarGridLine(x, top, x, bottom);
        }
        var xLabels = new List<(double Value, double Coordinate)>(); var boundary = RenderWidth;
        for (var i = xTicks.Length - 1; i >= 0; i--)
        {
            var value = xTicks[i]; var ruler = Text(scatterXFormatter is null ? BleeckerChartScale.TickLabel(value) : scatterXFormatter(value), 16, "BleeckerTextSecondaryBrush");
            ruler.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); var width = Math.Ceiling(ruler.DesiredSize.Width * 64) / 64;
            var coordinate = i == xTicks.Length - 1 ? Math.Min(XValue(value), RenderWidth - width / 2) : XValue(value);
            if (coordinate - width / 2 < 0 || coordinate + width / 2 > boundary) continue;
            xLabels.Add((value, coordinate)); boundary = coordinate - width / 2 - 5;
        }
        xLabels.Reverse();
        foreach (var (value, coordinate) in xLabels)
        {
            var label = Text(scatterXFormatter is null ? BleeckerChartScale.TickLabel(value) : scatterXFormatter(value), 12, "BleeckerTextSecondaryBrush"); label.Tag = "scatter-x-tick"; label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            AddText(plot, label, coordinate - label.DesiredSize.Width / 2, bottom + 16.52 - label.BaselineOffset);
        }
        foreach (var value in yTicks)
        {
            var label = Text(scatterYFormatter is null ? BleeckerChartScale.TickLabel(value) : scatterYFormatter(value), 12, "BleeckerTextSecondaryBrush"); label.Tag = "scatter-y-tick"; label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            AddText(plot, label, left - 8 - label.DesiredSize.Width, Math.Clamp(YValue(value), top + 2, bottom) + 4.26 - label.BaselineOffset);
        }
        if (!string.IsNullOrEmpty(XAxisLabel))
        {
            var label = Text(XAxisLabel, 12, "BleeckerTextSecondaryBrush"); label.Tag = "scatter-x-label"; label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            AddText(plot, label, (left + right - label.DesiredSize.Width) / 2, bottom + 45 - label.BaselineOffset);
        }
        if (!string.IsNullOrEmpty(YAxisLabel))
        {
            var label = Text(YAxisLabel, 12, "BleeckerTextSecondaryBrush"); label.Tag = "scatter-y-label"; label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            label.RenderTransform = new RotateTransform { Angle = -90 };
            AddText(plot, label, left - 55 + 4.26 - label.BaselineOffset, (top + bottom) / 2);
        }
        var zValues = first.ZKey is null ? [] : records.Select(row => BleeckerChartData.Number(BleeckerChartData.Read(row, first.ZKey))).OfType<double>().ToArray();
        var zMin = zValues.Length == 0 ? 0 : Math.Min(0, zValues.Min()); var zMax = zValues.Length == 0 ? 0 : zValues.Max();
        for (var s = 0; s < scatterSeries.Count; s++)
        {
            var series = scatterSeries[s]; var data = series.Data ?? Data;
            var occurrence = occurrences.GetValueOrDefault(series); occurrences[series] = occurrence + 1;
            for (var i = 0; i < data.Count; i++)
            {
                var row = data[i]; var x = ScatterNumber(BleeckerChartData.Read(row, first.XKey)); var y = ScatterNumber(BleeckerChartData.Read(row, first.YKey)); var z = first.ZKey is null ? null : BleeckerChartData.Read(row, first.ZKey);
                var size = first.ZKey is null ? 64 : !ScatterTruthy(z) ? 60 : ScatterNumber(z) is double number ? zMin == zMax ? 230 : BleeckerChartScale.Position(number, zMin, zMax, 60, 400) : double.NaN;
                var point = new ScatterPoint(series, s, i, row, x is double xv && xTicks.Length > 0 ? XValue(xv) : null, y is double yv && yTicks.Length > 0 ? YValue(yv) : null, Math.Sqrt(Math.Max(0, size) / Math.PI));
                var index = scatterPoints.Count; scatterPoints.Add(point);
                if (point.X is not double cx || point.Y is not double cy || !double.IsFinite(point.Radius)) continue;
                var key = (series, series.Key, occurrence, i); usedShapes.Add(key);
                if (!scatterShapes.TryGetValue(key, out var shape))
                {
                    shape = Resource<Path>($"<Path Stretch=\"None\"><Path.Style><Style TargetType=\"Path\"><Setter Property=\"Fill\" Value=\"{{ThemeResource {BleeckerChartTheme.PickColor(s)}}}\" /></Style></Path.Style></Path>");
                    scatterShapes.Add(key, shape); scatterShapePalettes.Add(shape, s); plot.Children.Add(shape);
                }
                if (scatterShapePalettes[shape] != s)
                {
                    shape.Style = Resource<Path>($"<Path Stretch=\"None\"><Path.Style><Style TargetType=\"Path\"><Setter Property=\"Fill\" Value=\"{{ThemeResource {BleeckerChartTheme.PickColor(s)}}}\" /></Style></Path.Style></Path>").Style;
                    scatterShapePalettes[shape] = s;
                }
                var parsed = Resource<Path>($"<Path Data=\"{ScatterCircle(point.Radius)}\" />");
                var geometry = parsed.Data; parsed.Data = null; shape.Data = geometry;
                if (series.Color is not null) shape.Fill = series.Color; else shape.ClearValue(Shape.FillProperty);
                shape.Tag = index; Canvas.SetZIndex(shape, index + 1); Canvas.SetLeft(shape, cx); Canvas.SetTop(shape, cy);
            }
        }
        foreach (var key in scatterShapes.Keys.Where(key => !usedShapes.Contains(key)).ToArray()) { var shape = scatterShapes[key]; plot.Children.Remove(shape); scatterShapePalettes.Remove(shape); scatterShapes.Remove(key); }
        PaintInteraction(); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private double RenderScatterLegend()
    {
        if (!ShowLegend) return 0;
        var collection = scatterSeries!;
        var marginLeft = string.IsNullOrEmpty(YAxisLabel) ? 0 : 20; var width = RenderWidth - marginLeft - 20;
        var rows = new List<List<(Canvas Item, double Width)>> { new() }; var rowWidth = 0d;
        foreach (var series in collection.OrderBy(series => series.Name, StringComparer.Ordinal))
        {
            var s = collection.IndexOf(series); var item = new Canvas { Height = 24 };
            var icon = Resource<Path>($"<Path Data=\"M32,16 A16,16,0,1,1,0,16 A16,16,0,1,1,32,16\" Fill=\"{{ThemeResource {BleeckerChartTheme.PickColor(s)}}}\" />");
            var iconCanvas = new Canvas { Width = 32, Height = 32 }; iconCanvas.Children.Add(icon); var view = new Viewbox { Width = 14, Height = 14, Child = iconCanvas }; Canvas.SetTop(view, 6.65625); item.Children.Add(view);
            var label = Text(series.Name, 16, BleeckerChartTheme.PickColor(s)!); if (series.Color is not null) { icon.Fill = series.Color; label.Foreground = series.Color; }
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); Canvas.SetLeft(label, 18); Canvas.SetTop(label, 18 - label.BaselineOffset); item.Children.Add(label);
            var entryWidth = 28 + Math.Ceiling(label.DesiredSize.Width * 64) / 64;
            if (rowWidth > 0 && rowWidth + entryWidth > width) { rows.Add([]); rowWidth = 0; }
            item.Width = entryWidth - 10; rows[^1].Add((item, entryWidth)); rowWidth += entryWidth;
        }
        var height = 16 + rows.Count * 24; var y = RenderHeight - (string.IsNullOrEmpty(XAxisLabel) ? 5 : 25) - height + 16;
        foreach (var row in rows) { var x = marginLeft + (width - row.Sum(entry => entry.Width)) / 2; foreach (var entry in row) { Canvas.SetLeft(entry.Item, x); Canvas.SetTop(entry.Item, y); legend!.Children.Add(entry.Item); x += entry.Width; } y += 24; }
        return height;
    }
    private void MoveScatterPointer(PointerRoutedEventArgs args)
    {
        for (var source = args.OriginalSource as DependencyObject; source is not null && !ReferenceEquals(source, this); source = VisualTreeHelper.GetParent(source))
            if (source is Path { Tag: int index } && index < scatterPoints.Count) { interaction.Pointer(index, 0); PaintInteraction(); return; }
        interaction.Exit(); PaintInteraction();
    }
    private void ShowScatterActive(int index)
    {
        var keyboardSeries = scatterPoints.FirstOrDefault()?.SeriesIndex;
        var point = interaction.PointerY is not null ? scatterPoints.ElementAtOrDefault(index) : scatterPoints.FirstOrDefault(point => point.SeriesIndex == keyboardSeries && point.RowIndex == index);
        if (point is null || point.X is not double x || point.Y is not double y || tooltip is null || tooltipItems is null || tooltipContainer is null)
        {
            ClearActive();
            // The source's keyboard selector can address a later series row
            // beyond the default tooltip item's payload. It still draws the
            // numeric X crosshair at the chart's default inspection Y.
            if (point is null && interaction.PointerY is null && scatterPoints.ElementAtOrDefault(index)?.X is double coordinate)
                DrawScatterCross(coordinate, (top + RenderHeight) / 2);
            return;
        }
        active!.Children.Clear(); tooltipItems.Children.Clear();
        var first = scatterSeries![0]; var keys = new[] { first.XKey, first.YKey }.ToList(); var z = first.ZKey is null ? null : BleeckerChartData.Read(point.Record, first.ZKey); if (ScatterTruthy(z)) keys.Add(first.ZKey!);
        var descriptions = new List<string>();
        foreach (var key in keys.Distinct().Order(StringComparer.Ordinal))
        {
            var raw = BleeckerChartData.Read(point.Record, key); if (raw is null) continue;
            var value = raw is not string and not bool && BleeckerChartData.Number(raw) is double number && TooltipFormatter is not null ? TooltipFormatter(number) : raw is bool ? "" : BleeckerChartData.Label(raw);
            var label = Text(key + " : " + value, 16, "BleeckerTextPrimaryBrush"); label.LineHeight = 24; label.LineStackingStrategy = LineStackingStrategy.BlockLineHeight; label.Margin = new Thickness(0, 4, 0, 4); tooltipItems.Children.Add(label); descriptions.Add(label.Text);
        }
        if (descriptions.Count == 0) { ClearActive(); return; }
        DrawScatterCross(x, y);
        tooltip.Visibility = tooltipContainer.Visibility = Visibility.Visible; tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        foreach (var label in tooltipItems.Children.OfType<TextBlock>()) label.RenderTransform = new TranslateTransform { Y = 18 - label.BaselineOffset };
        tooltipContainer.Margin = new Thickness(BleeckerTooltipPlacement.Position(x, tooltip.DesiredSize.Width, left, right), BleeckerTooltipPlacement.Position(y, tooltip.DesiredSize.Height, top, bottom), 0, 0);
        UpdateTooltipEffects(); AutomationProperties.SetHelpText(this, string.Join("; ", descriptions));
    }
    private void DrawScatterCross(double x, double y) => active!.Children.Add(Resource<Path>($"<Path Data=\"M{N(x)},{N(top)}V{N(bottom)}M{N(left)},{N(y)}H{N(right)}\" Stroke=\"{{ThemeResource BleeckerScatterCursorBrush}}\" StrokeThickness=\"1\" StrokeDashArray=\"3,3\" />"));
}
