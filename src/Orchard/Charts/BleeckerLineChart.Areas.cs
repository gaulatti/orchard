using System.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard;

public sealed partial class BleeckerLineChart
{
    private bool areaMode, areaStacked;
    private readonly List<(BleeckerChartSeries Series, Brush Brush, BleeckerMonotoneCurve.Point[] Upper, BleeckerMonotoneCurve.Point[] Lower, bool Range)> areaRendered = [];
    internal void UseAreaProfile(bool stacked) { areaMode = true; barMode = sparklineMode = false; areaStacked = stacked; QueueRender(); }
    private void RenderAreas()
    {
        plot!.Clip = null; plot.Children.Clear(); legend!.Children.Clear(); rendered.Clear(); areaRendered.Clear();
        left = 60; right = RenderWidth - 20; top = 10; bottom = RenderHeight - 35 - (ShowLegend && Series.Count == 0 ? 16 : RenderLegend());
        if (right <= left || bottom <= top) { ClearActive(); return; }
        var keys = Series.Select(series => series.Key).ToArray();
        var ticks = BleeckerChartScale.Ticks(BleeckerAreaGeometry.Domain(Data, keys, areaStacked));
        if (ticks.Length > 0) { min = ticks[0]; max = ticks[^1]; }
        if (ShowGrid) foreach (var y in ticks.Length == 0 ? new[] { top, bottom } : ticks.Select(Y))
            plot.Children.Add(Resource<Line>($"<Line X1=\"{N(left)}\" X2=\"{N(right)}\" Y1=\"{N(y)}\" Y2=\"{N(y)}\" Stroke=\"{{ThemeResource BleeckerBorderBrush}}\" StrokeThickness=\"1\" StrokeDashArray=\"3,3\" />"));
        foreach (var tick in ticks)
        {
            var label = Text(YAxisTickFormatter is null ? BleeckerChartScale.TickLabel(tick) : YAxisTickFormatter(tick), 12, "BleeckerTextSecondaryBrush"); label.Tag = "area-y";
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            AddText(plot, label, left - 8 - label.DesiredSize.Width, Math.Clamp(Y(tick), top + 2, bottom) + 4.26 - label.BaselineOffset);
        }
        var boundary = RenderWidth; var categories = new List<(TextBlock Text, double X)>();
        for (var i = Data.Count - 1; i >= 0; i--)
        {
            var value = BleeckerChartData.Label(BleeckerChartData.Read(Data[i], XAxisKey!));
            var probe = Text(value, 16, "BleeckerTextSecondaryBrush"); probe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var coordinate = i == Data.Count - 1 ? Math.Min(X(i), RenderWidth - probe.DesiredSize.Width / 2) : X(i);
            if (coordinate - probe.DesiredSize.Width / 2 < 0 || coordinate + probe.DesiredSize.Width / 2 > boundary) continue;
            var label = Text(value, 12, "BleeckerTextSecondaryBrush"); label.Tag = "area-x"; label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            categories.Add((label, coordinate)); boundary = coordinate - probe.DesiredSize.Width / 2 - 5;
        }
        foreach (var (label, x) in categories.AsEnumerable().Reverse()) AddText(plot, label, x - label.DesiredSize.Width / 2, bottom + 16.52 - label.BaselineOffset);
        var baseline = max < 0 ? max : Math.Max(min, 0);
        var values = BleeckerAreaGeometry.Values(Data, keys, areaStacked, baseline);
        for (var s = 0; s < Series.Count; s++)
        {
            var series = Series[s]; var range = !areaStacked && Data.Any(row => BleeckerChartData.Read(row, series.Key) is IList);
            var upper = new BleeckerMonotoneCurve.Point[Data.Count]; var lower = new BleeckerMonotoneCurve.Point[Data.Count];
            for (var i = 0; i < Data.Count; i++)
            {
                var raw = BleeckerChartData.Read(Data[i], series.Key); var value = values[s][i];
                var y = ticks.Length == 0 || raw is null ? double.NaN : Y(value.Upper);
                upper[i] = new(X(i), y); lower[i] = new(X(i), double.IsFinite(y) ? Y(value.Lower) : double.NaN);
            }
            var fill = Resource<Path>($"<Path Fill=\"{{ThemeResource {BleeckerChartTheme.PickColor(s)}}}\" />");
            fill.Data = Geometry(BleeckerAreaGeometry.Area(upper, lower)); fill.Opacity = .2; fill.Tag = "area-fill";
            if (series.Color is not null) fill.Fill = series.Color;
            Brush color = fill.Fill;
            if (Data.Count > 1 && ticks.Length > 0)
            {
                foreach (var figure in ((PathGeometry)fill.Data).Figures) figure.IsFilled = true;
                plot.Children.Add(fill); color = fill.Fill;
                void Stroke(BleeckerMonotoneCurve.Point[] points) {
                    var path = Resource<Path>($"<Path Stroke=\"{{ThemeResource {BleeckerChartTheme.PickColor(s)}}}\" StrokeThickness=\"2\" />");
                    if (series.Color is not null) path.Stroke = series.Color;
                    path.Data = Geometry(BleeckerMonotoneCurve.Create(points)); path.Tag = "area-stroke"; plot.Children.Add(path);
                }
                Stroke(upper); if (range) Stroke(lower);
            }
            else if (Data.Count == 1 && double.IsFinite(upper[0].Y)) {
                var dot = Resource<Ellipse>($"<Ellipse Width=\"8\" Height=\"8\" Fill=\"{{ThemeResource {BleeckerChartTheme.PickColor(s)}}}\" Stroke=\"{{ThemeResource {BleeckerChartTheme.PickColor(s)}}}\" StrokeThickness=\"2\" />");
                if (series.Color is not null) dot.Fill = dot.Stroke = series.Color;
                Canvas.SetLeft(dot, upper[0].X - 4); Canvas.SetTop(dot, upper[0].Y - 4); plot.Children.Add(dot); color = dot.Fill;
            }
            areaRendered.Add((series, color, upper, lower, range));
        }
        PaintInteraction(); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private void AddAreaDot(Canvas canvas, BleeckerMonotoneCurve.Point point, Brush color)
    {
        const int radius = 4;
        var dot = Resource<Ellipse>($"<Ellipse Width=\"{radius * 2 + 2}\" Height=\"{radius * 2 + 2}\" Stroke=\"{{ThemeResource BleeckerAreaDotBorderBrush}}\" StrokeThickness=\"2\" />");
        dot.Fill = color;
        Canvas.SetLeft(dot, point.X - radius - 1); Canvas.SetTop(dot, point.Y - radius - 1); canvas.Children.Add(dot);
    }
    private void ShowAreaActive(int index, double? pointerY)
    {
        if (active is null || tooltip is null || tooltipItems is null || tooltipContainer is null || index < 0 || index >= Data.Count) return;
        active.Children.Clear(); tooltipItems.Children.Clear();
        var category = BleeckerChartData.Label(BleeckerChartData.Read(Data[index], XAxisKey!));
        var heading = Text(category, 16, "BleeckerTextSecondaryBrush"); heading.FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 };
        heading.LineHeight = 24; heading.LineStackingStrategy = LineStackingStrategy.BlockLineHeight; tooltipItems.Children.Add(heading);
        active.Children.Add(Resource<Line>($"<Line X1=\"{N(X(index))}\" X2=\"{N(X(index))}\" Y1=\"{N(top)}\" Y2=\"{N(bottom)}\" Stroke=\"{{ThemeResource BleeckerAreaCursorBrush}}\" />"));
        var payloadIndex = Enumerable.Range(0, Data.Count).First(i => BleeckerChartData.Label(BleeckerChartData.Read(Data[i], XAxisKey!)) == category);
        foreach (var entry in areaRendered)
        {
            if (double.IsFinite(entry.Upper[payloadIndex].Y)) AddAreaDot(active, entry.Upper[payloadIndex], entry.Brush);
            if (entry.Range && double.IsFinite(entry.Lower[payloadIndex].Y)) AddAreaDot(active, entry.Lower[payloadIndex], entry.Brush);
        }
        var help = new List<string> { category };
        foreach (var series in Series.OrderBy(series => series.Name, StringComparer.Ordinal))
        {
            var raw = BleeckerChartData.Read(Data[payloadIndex], series.Key); if (raw is null) continue;
            var value = BleeckerChartData.Number(raw);
            var formatted = raw is bool ? "" : raw is IList range ? string.Concat(range.Cast<object?>().Where(item => item is not bool).Select(BleeckerChartData.Label)) : TooltipFormatter is not null && raw is not string && value is double number ? TooltipFormatter(number) : BleeckerChartData.Label(raw);
            var label = Text(" : " + formatted, 16, "BleeckerTextPrimaryBrush"); label.LineHeight = 24; label.LineStackingStrategy = LineStackingStrategy.BlockLineHeight; label.Margin = new Thickness(0, 4, 0, 4);
            tooltipItems.Children.Add(label); help.Add(label.Text);
        }
        tooltip.Visibility = tooltipContainer.Visibility = help.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        tooltipContainer.Margin = new Thickness(BleeckerTooltipPlacement.Position(X(index), tooltip.DesiredSize.Width, left, right), BleeckerTooltipPlacement.Position(pointerY ?? (top + RenderHeight) / 2, tooltip.DesiredSize.Height, top, bottom), 0, 0);
        UpdateTooltipEffects(); AutomationProperties.SetHelpText(this, string.Join("; ", help));
    }
}
