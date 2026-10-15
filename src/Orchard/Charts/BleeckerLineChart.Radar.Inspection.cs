using System.Collections;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard;

public sealed partial class BleeckerLineChart
{
    private double RenderRadarLegend()
    {
        if (!ShowLegend) return 0;
        var rows = new List<List<(Canvas Item, double Width)>>(); var rowWidth = 0d; var width = RenderWidth - 20;
        foreach (var entry in radarSeries!.Select((series, index) => (Series: series, Index: index)).OrderBy(entry => entry.Series.Name, StringComparer.Ordinal))
        {
            if (rows.Count == 0) rows.Add([]);
            var series = entry.Series; var item = new Canvas { Height = 24 };
            var icon = Resource<Rectangle>($"<Rectangle Width=\"14\" Height=\"10.5\" Fill=\"{{ThemeResource {BleeckerChartTheme.PickColor(entry.Index)}}}\" />");
            Canvas.SetTop(icon, 8.40625); item.Children.Add(icon);
            var label = Text(series.Name, 16, BleeckerChartTheme.PickColor(entry.Index)!);
            if (series.Color is not null) { icon.Fill = series.Color; label.Foreground = series.Color; }
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); Canvas.SetLeft(label, 18); Canvas.SetTop(label, 18 - label.BaselineOffset); item.Children.Add(label);
            var entryWidth = 28 + Math.Ceiling(BleeckerPieText.InlineWidth(label) * 64) / 64;
            if (rowWidth > 0 && rowWidth + entryWidth > width) { rows.Add([]); rowWidth = 0; }
            item.Width = entryWidth - 10; rows[^1].Add((item, entryWidth)); rowWidth += entryWidth;
        }
        var height = 16 + rows.Count * 24; var y = RenderHeight - 5 - height + 16;
        foreach (var row in rows)
        {
            var x = Math.Floor((width - row.Sum(entry => entry.Width)) * 32) / 64;
            foreach (var entry in row) { Canvas.SetLeft(entry.Item, x); Canvas.SetTop(entry.Item, y); legend!.Children.Add(entry.Item); x += entry.Width; }
            y += 24;
        }
        return height;
    }
    private void MoveRadarPointer(PointerRoutedEventArgs args)
    {
        var raw = args.GetCurrentPoint(this).Position; var point = new Point(Math.Floor(raw.X + .5), Math.Floor(raw.Y + .5)); var dx = point.X - radarCx; var dy = point.Y - radarCy;
        var radius = Math.Sqrt(dx * dx + dy * dy);
        if (Data.Count == 0 || radius == 0 || radius > radarRadius) { interaction.Exit(); PaintInteraction(); return; }
        var angle = -Math.Atan2(dy, dx) * 180 / Math.PI;
        while (angle > 90) angle -= 360;
        var count = Math.Max(1, radarSubjects.Count);
        var index = (int)Math.Floor((90 - angle) * count / 360 + .5) % count;
        interaction.Pointer(index, radius); PaintInteraction();
    }
    private void ShowRadarActive(int index)
    {
        if (Data.Count == 0 || active is null || tooltip is null || tooltipItems is null || tooltipContainer is null) { ClearActive(); return; }
        active.Children.Clear(); tooltipItems.Children.Clear();
        var hasTick = index >= 0 && index < radarSubjects.Count;
        var subject = hasTick ? radarSubjects[index] : null;
        var record = hasTick ? Data.FirstOrDefault(row => Equals(RadarCategory(BleeckerChartData.Read(row, XAxisKey!)), subject)) : Data[0];
        if (record is null) { ClearActive(); return; }
        var descriptions = new List<string>(); var headingPresent = subject is not null;
        if (headingPresent)
        {
            var headingText = ChartReactText(subject); var heading = Text(ChartVisibleText(headingText), 16, "BleeckerTextSecondaryBrush"); AutomationProperties.SetName(heading, headingText); heading.FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 };
            if (FontFamily.Source.EndsWith("/EncodeSans-Regular.ttf#Encode Sans", StringComparison.Ordinal))
                heading.FontFamily = Resource<TextBlock>("<TextBlock FontFamily=\"{StaticResource BleeckerPrimarySemiboldFontFamily}\" />").FontFamily;
            heading.LineHeight = 24; heading.LineStackingStrategy = LineStackingStrategy.BlockLineHeight; tooltipItems.Children.Add(heading); descriptions.Add(headingText);
        }
        foreach (var series in radarSeries!.OrderBy(series => series.Name, StringComparer.Ordinal))
        {
            var raw = BleeckerChartData.Read(record, series.Key); if (raw is null) continue;
            var formatted = raw is not string and not bool and not IList && BleeckerChartData.Number(raw) is double number && TooltipFormatter is not null ? TooltipFormatter(number) : ChartReactText(raw);
            var content = " : " + formatted; var text = Text(ChartVisibleText(content), 16, "BleeckerTextPrimaryBrush"); AutomationProperties.SetName(text, content); text.LineHeight = 24; text.LineStackingStrategy = LineStackingStrategy.BlockLineHeight; text.Margin = new Thickness(0, 4, 0, 4);
            // HTML allocates the separator and value spans independently. Keep
            // the trailing separator space in its intrinsic-width measurement.
            var visibleValue = ChartVisibleText(formatted); var separator = Text(visibleValue.Length == 0 ? ":" : ": \u200B", 16, "BleeckerTextPrimaryBrush");
            separator.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); var contentWidth = Math.Ceiling(BleeckerPieText.InlineWidth(separator) * 64) / 64;
            if (raw is IList) contentWidth += ChartFragmentsWidth(ChartReactFragments(raw).Select(ChartVisibleText).Where(fragment => fragment.Length > 0).ToArray());
            else
            {
                var valueText = Text(visibleValue, 16, "BleeckerTextPrimaryBrush"); valueText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                contentWidth += Math.Ceiling(BleeckerPieText.InlineWidth(valueText) * 64) / 64;
            }
            text.Width = contentWidth;
            tooltipItems.Children.Add(text); descriptions.Add(content);
        }
        foreach (var entry in radarRendered)
        {
            var point = entry.Points.FirstOrDefault(point => ReferenceEquals(point.Record, record));
            if (point is null || !double.IsFinite(point.Upper.X) || !double.IsFinite(point.Upper.Y)) continue;
            var dot = Resource<Ellipse>("<Ellipse Width=\"10\" Height=\"10\" Stroke=\"{ThemeResource BleeckerRadarActiveStrokeBrush}\" StrokeThickness=\"2\" />"); dot.Fill = entry.Paint;
            Canvas.SetLeft(dot, point.Upper.X - 5); Canvas.SetTop(dot, point.Upper.Y - 5); active.Children.Add(dot);
        }
        var angle = hasTick ? RadarAngle(subject) : 0d;
        if (interaction.PointerY is not null)
        {
            var end = hasTick ? RadarPosition(radarRadius, angle) : new Point();
            var ray = Resource<Path>($"<Path Data=\"M{N(hasTick ? radarCx : 0)},{N(hasTick ? radarCy : 0)}L{N(Math.Floor(end.X * 10000 + .5) / 10000)},{N(Math.Floor(end.Y * 10000 + .5) / 10000)}\" Stroke=\"{{ThemeResource BleeckerRadarCursorBrush}}\" />"); active.Children.Insert(0, ray);
        }
        tooltip.Visibility = tooltipContainer.Visibility = Visibility.Visible; tooltip.Width = double.NaN; tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        tooltip.Width = Math.Ceiling(tooltip.DesiredSize.Width * 64) / 64; tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        foreach (var text in tooltipItems.Children.OfType<TextBlock>()) text.RenderTransform = new TranslateTransform { Y = 18 - text.BaselineOffset };
        // The pinned keyboard selector uses (chart-center-X, angle), while
        // pointer inspection projects its radius onto the selected angle.
        var coordinate = interaction.PointerY is double radius ? RadarPosition(radius, angle) : new Point(radarCx, angle);
        tooltipContainer.Margin = hasTick ? new Thickness(BleeckerTooltipPlacement.Position(coordinate.X, tooltip.DesiredSize.Width, 0, RenderWidth - 20), BleeckerTooltipPlacement.Position(coordinate.Y, tooltip.DesiredSize.Height, 10, RenderHeight - 5 - radarLegendHeight), 0, 0) : interaction.PointerY.HasValue ? new Thickness(10, 10, 0, 0) : new Thickness(0);
        if (tooltipItems.Children.Count == (headingPresent ? 1 : 0)) tooltip.Visibility = tooltipContainer.Visibility = Visibility.Collapsed;
        UpdateTooltipEffects(); AutomationProperties.SetHelpText(this, string.Join("; ", descriptions));
    }
}
