using System.Collections;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard;

public sealed partial class BleeckerLineChart
{
    private bool funnelMode, funnelShowLabels = true;
    private BleeckerFunnelGeometry.Entry[] funnelEntries = [];
    internal void UseFunnelProfile(bool labels)
    {
        funnelMode = true; sparklineMode = barMode = areaMode = pieMode = scatterMode = radarMode = radialBarMode = false;
        funnelShowLabels = labels; QueueRender();
    }
    private Brush? FunnelPaint(int index)
    {
        var value = BleeckerChartData.Read(Data[index], "fill");
        return value is null ? null : value as Brush ?? throw new InvalidOperationException("Funnel fill requires a native Brush.");
    }
    private void RenderFunnel()
    {
        plot!.Children.Clear(); plot.Clip = new RectangleGeometry { Rect = new Rect(0, 0, RenderWidth, RenderHeight) }; legend!.Children.Clear(); rendered.Clear();
        funnelEntries = BleeckerFunnelGeometry.Compute(Data.ToArray(), RenderWidth, RenderHeight);
        foreach (var entry in funnelEntries)
        {
            if (entry.Path is string data)
            {
                var path = Resource<Path>($"<Path Data=\"{data}\" Fill=\"{{ThemeResource {BleeckerChartTheme.PickColor(entry.Index)}}}\" Stroke=\"{{ThemeResource BleeckerFunnelBorderBrush}}\" StrokeThickness=\"1\" />");
                path.Tag = ("funnel", entry.Index); if (FunnelPaint(entry.Index) is Brush paint) path.Fill = paint; plot.Children.Add(path);
            }
            if (funnelShowLabels) RenderFunnelLabel(entry);
        }
        PaintInteraction(); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private static string FunnelString(object? value) => value switch
    {
        null => "",
        bool flag => flag ? "true" : "false",
        IList array => string.Join(",", array.Cast<object?>().Select(FunnelString)),
        _ => ChartReactText(value)
    };
    private string[] FunnelWords(string value, double width)
    {
        if (width == 0 || double.IsNaN(width)) return [string.Join(" ", Regex.Split(value, "[ \\f\\n\\r\\t\\v\\u2028\\u2029]+"))];
        double Measure(string text) { var label = Text(text, 16, "BleeckerTextPrimaryBrush"); label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); return Math.Ceiling(BleeckerPieText.InlineWidth(label) * 64) / 64; }
        var space = Measure("\u00A0\u200B"); var lines = new List<List<string>>(); var lineWidth = 0d;
        foreach (var word in Regex.Split(value, "[ \\f\\n\\r\\t\\v\\u2028\\u2029]+"))
        {
            var measured = Measure(word);
            if (lines.Count > 0 && lineWidth + measured + space < width) { lines[^1].Add(word); lineWidth += measured + space; }
            else { lines.Add([word]); lineWidth = measured; }
        }
        return lines.Select(line => string.Join(" ", line)).ToArray();
    }
    private void RenderFunnelLabel(BleeckerFunnelGeometry.Entry entry)
    {
        var name = BleeckerChartData.Read(Data[entry.Index], "name");
        if (name is null || !double.IsFinite(entry.CenterX) || !double.IsFinite(entry.CenterY)) return;
        var lines = FunnelWords(FunnelString(name) + ": ", entry.LabelWidth);
        for (var i = 0; i < lines.Length; i++)
        {
            var label = Text(lines[i], 16, "BleeckerTextPrimaryBrush"); label.Tag = ("funnel-label", entry.Index, i); label.IsHitTestVisible = false;
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            AddText(plot!, label, entry.CenterX - label.DesiredSize.Width / 2, entry.CenterY + 5.68 - (lines.Length - 1) * 8 + i * 16 - label.BaselineOffset);
        }
    }
    private void MoveFunnelPointer(PointerRoutedEventArgs args)
    {
        for (var source = args.OriginalSource as DependencyObject; source is not null && !ReferenceEquals(source, this); source = VisualTreeHelper.GetParent(source))
            if (source is Path { Tag: ValueTuple<string, int> tag } && tag.Item1 == "funnel" && tag.Item2 < funnelEntries.Length) { interaction.Pointer(tag.Item2, 0); PaintInteraction(); return; }
        interaction.Exit(); PaintInteraction();
    }
    private void ShowFunnelActive(int index)
    {
        if (tooltip is null || tooltipItems is null || tooltipContainer is null || active is null || index >= funnelEntries.Length) { ClearActive(); return; }
        var entry = funnelEntries[index]; var raw = entry.Value; if (raw is null) { ClearActive(); return; }
        active.Children.Clear(); tooltipItems.Children.Clear();
        var value = raw is not string and not bool and not IList && BleeckerChartData.Number(raw) is double number && TooltipFormatter is not null ? TooltipFormatter(number) : ChartReactText(raw);
        var content = " : " + value; var row = Text(ChartVisibleText(content), 16, "BleeckerTextPrimaryBrush"); AutomationProperties.SetName(row, content);
        row.LineHeight = 24; row.LineStackingStrategy = LineStackingStrategy.BlockLineHeight; row.Margin = new Thickness(0, 4, 0, 4);
        var visible = ChartVisibleText(value); var separator = Text(visible.Length == 0 ? ":" : ": \u200B", 16, "BleeckerTextPrimaryBrush"); separator.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var width = Math.Ceiling(BleeckerPieText.InlineWidth(separator) * 64) / 64;
        if (raw is IList) width += ChartFragmentsWidth(ChartReactFragments(raw).Select(ChartVisibleText).Where(fragment => fragment.Length > 0).ToArray());
        else { var text = Text(visible, 16, "BleeckerTextPrimaryBrush"); text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); width += Math.Ceiling(BleeckerPieText.InlineWidth(text) * 64) / 64; }
        row.Width = width; tooltipItems.Children.Add(row);
        tooltip.Visibility = tooltipContainer.Visibility = Visibility.Visible; tooltip.Width = double.NaN; tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); tooltip.Width = Math.Ceiling(tooltip.DesiredSize.Width * 64) / 64; tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        row.RenderTransform = new TranslateTransform { Y = 18 - row.BaselineOffset };
        // The browser discards an entire nonfinite CSS transform. Its tooltip
        // then stays at the wrapper's explicit top/left zero, including zero data.
        tooltipContainer.Margin = double.IsFinite(entry.CenterX) && double.IsFinite(entry.CenterY)
            ? new Thickness(BleeckerTooltipPlacement.Position(entry.CenterX, tooltip.DesiredSize.Width, 5, RenderWidth - 5), BleeckerTooltipPlacement.Position(entry.CenterY, tooltip.DesiredSize.Height, 5, RenderHeight - 5), 0, 0)
            : new Thickness(0);
        UpdateTooltipEffects(); AutomationProperties.SetHelpText(this, content);
    }
}
