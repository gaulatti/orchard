using System.Collections;
using System.Text.RegularExpressions;
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
    private bool barMode;
    private BleeckerBarChartLayout barLayout;
    private Func<object?, string>? barYAxisFormatter;
    private FrameworkElement? barCursor;
    private int barGridCount;
    private double? barPointerX;
    private bool VerticalBars => barLayout == BleeckerBarChartLayout.Vertical;
    internal void UseBarProfile(BleeckerBarChartLayout layout, Func<object?, string>? formatter)
    {
        barMode = true; sparklineMode = areaMode = pieMode = scatterMode = false; barLayout = layout; barYAxisFormatter = formatter; QueueRender();
    }
    private double Band => Data.Count == 0 ? 0 : (VerticalBars ? bottom - top : right - left) / Data.Count;
    private double Category(int index) => (VerticalBars ? top : left) + Band * (index + .5);
    private double Numeric(double value) => VerticalBars ? BleeckerChartScale.Position(value, min, max, left, right) : Y(value);

    private void RenderBars()
    {
        plot!.Children.Clear(); legend!.Children.Clear(); rendered.Clear();
        plot.Clip = new RectangleGeometry { Rect = new Rect(0, 0, ActualWidth, ActualHeight) };
        left = 60; right = RenderWidth - 20; top = 10; bottom = RenderHeight - 35 - (ShowLegend && Series.Count == 0 ? 16 : RenderLegend());
        if (right <= left || bottom <= top) { ClearActive(); return; }
        var ticks = BleeckerChartScale.Ticks(Series.SelectMany(series => Data.SelectMany(row => BleeckerBarGeometry.DomainValues(BleeckerChartData.Read(row, series.Key)))));
        if (ticks.Length > 0) { min = ticks[0]; max = ticks[^1]; }
        var categories = BarCategoryTicks();
        if (ShowGrid)
        {
            var horizontal = VerticalBars ? categories.Select(Category).ToList() : ticks.Select(Numeric).ToList();
            var vertical = VerticalBars ? ticks.Select(Numeric).ToList() : categories.Select(Category).ToList();
            if (!horizontal.Contains(top)) horizontal.Add(top); if (!horizontal.Contains(bottom)) horizontal.Add(bottom);
            if (!vertical.Contains(left)) vertical.Add(left); if (!vertical.Contains(right)) vertical.Add(right);
            foreach (var y in horizontal) BarGridLine(left, y, right, y);
            foreach (var x in vertical) BarGridLine(x, top, x, bottom);
        }
        barGridCount = plot.Children.Count;
        foreach (var number in ticks)
        {
            var label = Text(!VerticalBars && barYAxisFormatter is not null ? barYAxisFormatter(number) : BleeckerChartScale.TickLabel(number), 12, "BleeckerTextSecondaryBrush");
            label.Tag = VerticalBars ? "bar-x-tick" : "bar-y-tick";
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            if (VerticalBars) AddText(plot, label, Numeric(number) - label.DesiredSize.Width / 2, bottom + 16.52 - label.BaselineOffset);
            else AddText(plot, label, left - 8 - label.DesiredSize.Width, Math.Clamp(Numeric(number), top + 2, bottom) + 4.26 - label.BaselineOffset);
        }
        foreach (var index in categories)
        {
            var raw = BleeckerChartData.Read(Data[index], XAxisKey!);
            var value = VerticalBars && barYAxisFormatter is not null ? barYAxisFormatter(raw) : BleeckerChartData.Label(raw);
            var lines = VerticalBars ? BarWords(value, 60) : new[] { value };
            for (var line = 0; line < lines.Length; line++)
            {
                var label = Text(lines[line], 12, "BleeckerTextSecondaryBrush"); label.Tag = (VerticalBars ? "bar-y-category-" : "bar-x-category-") + index;
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                if (VerticalBars) AddText(plot, label, left - 8 - label.DesiredSize.Width, Category(index) + 4.26 - (lines.Length - 1) * 6 + line * 12 - label.BaselineOffset);
                else AddText(plot, label, Math.Min(Category(index), RenderWidth - label.DesiredSize.Width / 2) - label.DesiredSize.Width / 2, bottom + 16.52 - label.BaselineOffset);
            }
        }
        var baseline = Math.Clamp(0, min, max);
        for (var s = 0; s < Series.Count; s++)
        {
            var series = Series[s]; var slot = BleeckerBarGeometry.Slot(Band, Series.Count, s);
            for (var index = 0; index < Data.Count && ticks.Length > 0; index++)
            {
                var values = BleeckerBarGeometry.Values(BleeckerChartData.Read(Data[index], series.Key), baseline); if (values is null) continue;
                var start = Numeric(values.Value.Start); var end = Numeric(values.Value.End);
                var x = VerticalBars ? start : left + Band * index + slot.Offset;
                var y = VerticalBars ? top + Band * index + slot.Offset : end;
                var width = VerticalBars ? end - start : slot.Size;
                var height = VerticalBars ? slot.Size : start - end;
                var geometry = BleeckerBarGeometry.Rectangle(x, y, width, height); if (geometry is null) continue;
                var path = Resource<Path>($"<Path Data=\"{geometry}\" Fill=\"{{ThemeResource {BleeckerChartTheme.PickColor(s)}}}\" />");
                path.Tag = $"bar-{s}-{index}"; if (series.Color is not null) path.Fill = series.Color;
                plot.Children.Add(path);
            }
        }
        PaintInteraction(); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private void BarGridLine(double x1, double y1, double x2, double y2) => plot!.Children.Add(Resource<Line>(
        $"<Line X1=\"{N(x1)}\" Y1=\"{N(y1)}\" X2=\"{N(x2)}\" Y2=\"{N(y2)}\" Stroke=\"{{ThemeResource BleeckerBorderBrush}}\" StrokeThickness=\"1\" StrokeDashArray=\"3,3\" />"));

    private int[] BarCategoryTicks()
    {
        var result = new List<int>(); var boundary = VerticalBars ? RenderHeight : RenderWidth;
        for (var i = Data.Count - 1; i >= 0; i--)
        {
            // The pinned producer's DOM ruler allocates at the 16px body
            // size even though its SVG tick glyphs are 12px. The captured
            // measurement trace establishes this constraint independently.
            var label = Text(BleeckerChartData.Label(BleeckerChartData.Read(Data[i], XAxisKey!)), 16, "BleeckerTextSecondaryBrush");
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var size = VerticalBars ? label.DesiredSize.Height : label.DesiredSize.Width;
            var coordinate = i == Data.Count - 1 ? Math.Min(Category(i), boundary - size / 2) : Category(i);
            if (coordinate - size / 2 < 0 || coordinate + size / 2 > boundary) continue;
            result.Add(i); boundary = coordinate - size / 2 - 5;
        }
        result.Reverse(); return result.ToArray();
    }
    private string[] BarWords(string value, double width)
    {
        var lines = new List<string>(); var current = "";
        foreach (var word in Regex.Split(value, @"\s+").Where(word => word.Length > 0))
        {
            var candidate = current.Length == 0 ? word : current + " " + word;
            var probe = Text(candidate, 16, "BleeckerTextSecondaryBrush"); probe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            if (current.Length > 0 && probe.DesiredSize.Width > width) { lines.Add(current); current = word; }
            else current = candidate;
        }
        if (current.Length > 0) lines.Add(current); return lines.ToArray();
    }
    private void MoveBarPointer(PointerRoutedEventArgs args)
    {
        if (Data.Count == 0 || right <= left || bottom <= top) return;
        var point = args.GetCurrentPoint(this).Position;
        if (point.X < left || point.X > right || point.Y < top || point.Y > bottom) { interaction.Exit(); barPointerX = null; PaintInteraction(); return; }
        var position = VerticalBars ? point.Y - top : point.X - left;
        var index = (int)Math.Ceiling(position / Band) - 1; if (position == 0) index = 0;
        barPointerX = point.X; interaction.Pointer(Math.Clamp(index, 0, Data.Count - 1), point.Y); PaintInteraction();
    }
    private void ClearBarCursor() { if (barCursor is not null) plot?.Children.Remove(barCursor); barCursor = null; }
    private void ShowBarActive(int index, double? pointerY)
    {
        if (plot is null || active is null || tooltipItems is null || tooltip is null || tooltipContainer is null || index < 0 || index >= Data.Count) return;
        active.Children.Clear(); tooltipItems.Children.Clear(); ClearBarCursor();
        var category = BleeckerChartData.Label(BleeckerChartData.Read(Data[index], XAxisKey!));
        var heading = Text(category, 16, "BleeckerTextSecondaryBrush"); heading.FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 };
        heading.LineHeight = 24; heading.LineStackingStrategy = LineStackingStrategy.BlockLineHeight; tooltipItems.Children.Add(heading);
        var payload = Data.FirstOrDefault(row => BleeckerChartData.Label(BleeckerChartData.Read(row, XAxisKey!)) == category)!;
        var help = new List<string> { category };
        foreach (var series in Series.OrderBy(series => series.Name, StringComparer.Ordinal))
        {
            var raw = BleeckerChartData.Read(payload, series.Key); if (raw is null) continue;
            var number = BleeckerChartData.Number(raw);
            var formatted = raw is bool ? "" : raw is IList range ? string.Concat(range.Cast<object?>().Where(value => value is not bool).Select(BleeckerChartData.Label)) : TooltipFormatter is not null && raw is not string && number is double numeric ? TooltipFormatter(numeric) : BleeckerChartData.Label(raw);
            var label = Text(" : " + formatted, 16, "BleeckerTextPrimaryBrush");
            label.LineHeight = 24; label.LineStackingStrategy = LineStackingStrategy.BlockLineHeight; label.Margin = new Thickness(0, 4, 0, 4);
            tooltipItems.Children.Add(label); help.Add(label.Text);
        }
        var cursor = Resource<Rectangle>("<Rectangle Fill=\"{ThemeResource BleeckerBarCursorBrush}\" />");
        cursor.Width = VerticalBars ? right - left - 1 : Band; cursor.Height = VerticalBars ? Band : bottom - top - 1;
        Canvas.SetLeft(cursor, VerticalBars ? left + .5 : left + index * Band); Canvas.SetTop(cursor, VerticalBars ? top + index * Band : top + .5);
        plot.Children.Insert(Math.Min(barGridCount, plot.Children.Count), cursor); barCursor = cursor;
        tooltip.Visibility = tooltipContainer.Visibility = help.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var anchorX = VerticalBars ? pointerY.HasValue ? barPointerX!.Value : (left + RenderWidth) / 2 : Category(index);
        var anchorY = VerticalBars ? Category(index) : pointerY ?? (top + RenderHeight) / 2;
        tooltipContainer.Margin = new Thickness(BleeckerTooltipPlacement.Position(anchorX, tooltip.DesiredSize.Width, left, right), BleeckerTooltipPlacement.Position(anchorY, tooltip.DesiredSize.Height, top, bottom), 0, 0);
        UpdateTooltipEffects(); AutomationProperties.SetHelpText(this, string.Join("; ", help));
    }
}
