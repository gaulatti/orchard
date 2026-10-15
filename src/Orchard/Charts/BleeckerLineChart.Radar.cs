using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard;

public sealed partial class BleeckerLineChart
{
    private bool radarMode;
    private ObservableCollection<BleeckerRadarSeries>? radarSeries, observedRadarSeries;
    private readonly HashSet<BleeckerRadarSeries> observedRadarItems = [];
    private readonly List<object> radarSubjects = [];
    private readonly List<(BleeckerRadarSeries Series, Brush Paint, RadarPoint[] Points)> radarRendered = [];
    private double radarCx, radarCy, radarRadius, radarLegendHeight;
    private double[] radarTicks = [];
    private sealed record RadarPoint(IReadOnlyDictionary<string, object?> Record, object? Value, Point Upper, Point Lower);
    internal void UseRadarProfile(ObservableCollection<BleeckerRadarSeries> series)
    {
        var changed = !radarMode || !ReferenceEquals(radarSeries, series);
        radarMode = true; sparklineMode = barMode = areaMode = pieMode = scatterMode = radialBarMode = false; radarSeries = series;
        if (changed && IsLoaded) Observe(); QueueRender();
    }
    private void ObserveRadar()
    {
        if (!radarMode || radarSeries is null) return;
        observedRadarSeries = radarSeries; observedRadarSeries.CollectionChanged += OnCollectionChanged;
        foreach (var series in radarSeries) if (observedRadarItems.Add(series)) series.PropertyChanged += OnRadarSeriesChanged;
    }
    private void StopObservingRadar()
    {
        if (observedRadarSeries is not null) observedRadarSeries.CollectionChanged -= OnCollectionChanged;
        foreach (var series in observedRadarItems) series.PropertyChanged -= OnRadarSeriesChanged;
        observedRadarSeries = null; observedRadarItems.Clear();
    }
    private void OnRadarSeriesChanged(object? sender, PropertyChangedEventArgs args) => QueueRender();
    private int RadarInspectionCount => Data.Count == 0 ? 0 : Math.Max(1, radarSubjects.Count);
    private static object? RadarCategory(object? value) => value is string text ? text : value is not bool && BleeckerChartData.Number(value) is double number ? number : null;
    private double RadarAngle(object? category)
    {
        var index = category is null ? -1 : radarSubjects.IndexOf(category);
        return index < 0 ? radarSubjects.Count == 0 ? 0 : 360d / radarSubjects.Count : 90 - 360d * index / radarSubjects.Count;
    }
    private Point RadarPosition(double radius, double angle) => new(radarCx + radius * Math.Cos(-angle * Math.PI / 180), radarCy + radius * Math.Sin(-angle * Math.PI / 180));
    private double RadarValueRadius(object? value)
    {
        if (value is null) return 0;
        var number = value is bool flag ? flag ? 1d : 0d : BleeckerChartData.Number(value);
        return number is double scalar ? BleeckerChartScale.Position(scalar, radarTicks[0], radarTicks[^1], 0, radarRadius) : 0;
    }
    private void RenderRadar()
    {
        if (radarSeries is null) throw new InvalidOperationException("A mounted Radar Chart requires Series.");
        foreach (var series in radarSeries) { _ = series.Key; _ = series.Name; }
        plot!.Children.Clear(); plot.Clip = null; legend!.Children.Clear(); rendered.Clear(); radarRendered.Clear(); radarSubjects.Clear();
        foreach (var row in Data)
        {
            var category = RadarCategory(BleeckerChartData.Read(row, XAxisKey!));
            if (category is not null && !radarSubjects.Contains(category)) radarSubjects.Add(category);
        }
        radarLegendHeight = RenderRadarLegend();
        radarCx = RenderWidth / 2; radarCy = RenderHeight / 2;
        radarRadius = Math.Max(0, Math.Min(RenderWidth - 20, RenderHeight - 15 - radarLegendHeight) / 2) * .8;
        radarTicks = BleeckerChartScale.Ticks(radarSeries.SelectMany(series => Data.SelectMany(row => BleeckerChartData.NumericDomain(BleeckerChartData.Read(row, series.Key)))), false);
        foreach (var subject in radarTicks.Length > 0 ? radarSubjects : [])
        {
            var point = RadarPosition(radarRadius, RadarAngle(subject));
            var line = Resource<Microsoft.UI.Xaml.Shapes.Line>($"<Line X1=\"{N(radarCx)}\" Y1=\"{N(radarCy)}\" X2=\"{N(point.X)}\" Y2=\"{N(point.Y)}\" Stroke=\"{{ThemeResource BleeckerBorderBrush}}\" />"); line.Tag = "radar-spoke"; plot.Children.Add(line);
        }
        if (radarSubjects.Count > 0)
            foreach (var value in radarTicks)
            {
                var radius = BleeckerChartScale.Position(value, radarTicks[0], radarTicks[^1], 0, radarRadius);
                var geometry = RadarPolygon(radarSubjects.Select(subject => RadarPosition(radius, RadarAngle(subject))).ToArray(), false);
                var ring = Resource<Path>($"<Path Data=\"{geometry}\" Stroke=\"{{ThemeResource BleeckerBorderBrush}}\" />"); ring.Tag = "radar-ring"; plot.Children.Add(ring);
            }
        for (var s = 0; s < radarSeries.Count && radarTicks.Length > 0; s++)
        {
            var series = radarSeries[s]; var points = new List<RadarPoint>(); var range = false;
            for (var rowIndex = 0; rowIndex < Data.Count; rowIndex++)
            {
                var row = Data[rowIndex]; var value = BleeckerChartData.Read(row, series.Key);
                var category = BleeckerChartData.TryRead(row, XAxisKey!, out var rawSubject) ? RadarCategory(rawSubject) : (object)(double)rowIndex;
                var angle = RadarAngle(category); var upper = value is IList array ? array.Count == 0 ? null : array[array.Count - 1] : value;
                var lower = value is IList baseline ? baseline.Count == 0 ? null : baseline[0] : value;
                range |= value is IList { Count: >= 2 };
                points.Add(new(row, value, RadarPosition(RadarValueRadius(upper), angle), RadarPosition(RadarValueRadius(lower), angle)));
            }
            if (points.Count == 0) continue;
            var upperPath = RadarPolygon(points.Select(point => point.Upper).ToArray());
            var fillPath = range ? upperPath.TrimEnd('Z') + "L" + RadarPolygon(points.Select(point => point.Lower).Reverse().ToArray())[1..] : upperPath;
            var fill = Resource<Path>($"<Path Data=\"{fillPath}\" Fill=\"{{ThemeResource {BleeckerChartTheme.PickColor(s)}}}\" Opacity=\"{(series.Fill ? "0.35" : "0")}\" />"); ((PathGeometry)fill.Data).FillRule = FillRule.Nonzero; fill.Tag = ("radar-fill", s); if (series.Color is not null) fill.Fill = series.Color; plot.Children.Add(fill);
            var stroke = Resource<Path>($"<Path Data=\"{upperPath}\" Stroke=\"{{ThemeResource {BleeckerChartTheme.PickColor(s)}}}\" />"); stroke.Tag = ("radar-stroke", s); if (series.Color is not null) stroke.Stroke = series.Color; plot.Children.Add(stroke);
            if (range)
            {
                var baseline = Resource<Path>($"<Path Data=\"{RadarPolygon(points.Select(point => point.Lower).ToArray())}\" Stroke=\"{{ThemeResource {BleeckerChartTheme.PickColor(s)}}}\" />"); baseline.Tag = ("radar-base", s); if (series.Color is not null) baseline.Stroke = series.Color; plot.Children.Add(baseline);
            }
            radarRendered.Add((series, stroke.Stroke, points.ToArray()));
        }
        RenderRadarAxes(); PaintInteraction(); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private static string RadarPolygon(IReadOnlyList<Point> points, bool repeatFirst = true)
    {
        if (points.Count == 0) return "";
        static string Coordinate(double value) => N(Math.Floor(value * 10000 + .5) / 10000);
        var closed = repeatFirst ? points.Append(points[0]).ToArray() : points;
        return string.Join("", closed.Select((point, index) => (index == 0 ? "M" : "L") + Coordinate(point.X) + "," + Coordinate(point.Y))) + "Z";
    }
    private void RenderRadarAxes()
    {
        foreach (var subject in radarSubjects)
        {
            var angle = RadarAngle(subject); var radians = -angle * Math.PI / 180; var cos = Math.Cos(radians); var sin = Math.Sin(radians);
            var point = RadarPosition(radarRadius + 8, angle); var text = Text(BleeckerChartData.Label(subject), 12, "BleeckerTextSecondaryBrush");
            text.Tag = "radar-subject"; text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var anchor = cos > 1e-5 ? 0 : cos < -1e-5 ? text.DesiredSize.Width : text.DesiredSize.Width / 2;
            var baseline = Math.Abs(cos) <= Math.Sqrt(.5) ? sin > 0 ? 8.52 : 0 : 4.26;
            AddText(plot!, text, point.X - anchor, point.Y + baseline - text.BaselineOffset);
        }
        if (radarTicks.Length == 0) return;
        var end = RadarPosition(radarRadius, 0);
        var axis = Resource<Microsoft.UI.Xaml.Shapes.Line>($"<Line X1=\"{N(radarCx)}\" Y1=\"{N(radarCy)}\" X2=\"{N(end.X)}\" Y2=\"{N(end.Y)}\" Stroke=\"{{ThemeResource BleeckerBorderBrush}}\" />"); axis.Tag = "radar-radius-axis"; plot!.Children.Add(axis);
        foreach (var value in radarTicks)
        {
            var x = radarCx + BleeckerChartScale.Position(value, radarTicks[0], radarTicks[^1], 0, radarRadius);
            var text = Text(YAxisTickFormatter?.Invoke(value) ?? BleeckerChartScale.TickLabel(value), 10, "BleeckerTextSecondaryBrush"); text.Tag = "radar-radius-tick";
            text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); text.RenderTransform = new RotateTransform { Angle = 90, CenterY = text.BaselineOffset };
            AddText(plot, text, x, radarCy - text.BaselineOffset);
        }
    }
}
