using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard;

public sealed partial class BleeckerLineChart
{
    private void MoveRadialBarPointer(PointerRoutedEventArgs args)
    {
        var raw = args.GetCurrentPoint(this).Position; var x = Math.Floor(raw.X + .5) - radialCx; var y = Math.Floor(raw.Y + .5) - radialCy;
        var radius = Math.Sqrt(x * x + y * y); var angle = -Math.Atan2(y, x) * 180 / Math.PI;
        var min = Math.Min(radialStart, radialEnd); var max = Math.Max(radialStart, radialEnd);
        while (angle < min) angle += 360; while (angle > max && angle - 360 >= min) angle -= 360;
        if (Data.Count == 0 || radialBand == 0 || radius == 0 || radius < radialInner || radius > radialOuter || angle < min || angle > max) { interaction.Exit(); PaintInteraction(); return; }
        var index = Math.Clamp((int)Math.Floor((radius - Math.Min(radialInner, radialOuter)) / radialBand), 0, Data.Count - 1);
        if (radialInner > radialOuter) index = Data.Count - index - 1;
        interaction.Pointer(index, angle); PaintInteraction();
    }
    private void ShowRadialBarActive(int index)
    {
        if (Data.Count == 0 || tooltip is null || tooltipItems is null || tooltipContainer is null || active is null) { ClearActive(); return; }
        active.Children.Clear(); tooltipItems.Children.Clear(); var title = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var heading = Text(title, 16, "BleeckerTextSecondaryBrush"); heading.FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 }; AutomationProperties.SetName(heading, title);
        if (FontFamily.Source.EndsWith("/EncodeSans-Regular.ttf#Encode Sans", StringComparison.Ordinal)) heading.FontFamily = Resource<TextBlock>("<TextBlock FontFamily=\"{StaticResource BleeckerPrimarySemiboldFontFamily}\" />").FontFamily;
        heading.LineHeight = 24; heading.LineStackingStrategy = LineStackingStrategy.BlockLineHeight; tooltipItems.Children.Add(heading); var descriptions = new List<string> { title };
        var entry = radialEntries.FirstOrDefault(entry => entry.Index == index);
        if (entry?.Value is not null)
        {
            var value = ChartReactText(entry.Value); var content = " : " + value; var row = Text(ChartVisibleText(content), 16, "BleeckerTextPrimaryBrush"); AutomationProperties.SetName(row, content);
            row.LineHeight = 24; row.LineStackingStrategy = LineStackingStrategy.BlockLineHeight; row.Margin = new Thickness(0, 4, 0, 4);
            var separator = Text(value.Length == 0 ? ":" : ": \u200B", 16, "BleeckerTextPrimaryBrush"); separator.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var text = Text(ChartVisibleText(value), 16, "BleeckerTextPrimaryBrush"); text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            row.Width = Math.Ceiling(BleeckerPieText.InlineWidth(separator) * 64) / 64 + Math.Ceiling(BleeckerPieText.InlineWidth(text) * 64) / 64;
            tooltipItems.Children.Add(row); descriptions.Add(content);
        }
        var tick = entry?.Tick ?? (Data.Count == 0 ? 0 : Math.Min(radialInner, radialOuter) + (index + .5) * radialBand);
        Point coordinate;
        if (interaction.PointerY is double angle)
        {
            var ring = BleeckerPieGeometry.Path(radialCx, radialCy, tick, tick, new(radialStart, radialEnd, 0));
            if (ring is not null) active.Children.Add(Resource<Path>($"<Path Data=\"{ring}\" Fill=\"{{ThemeResource BleeckerRadialBarCursorFillBrush}}\" Stroke=\"{{ThemeResource BleeckerRadialBarCursorBrush}}\" />"));
            var point = BleeckerPieGeometry.Polar(radialCx, radialCy, tick, angle); coordinate = new(point.X, point.Y);
        }
        else coordinate = new((5 + RenderWidth) / 2, tick);
        tooltip.Visibility = tooltipContainer.Visibility = Visibility.Visible; tooltip.Width = double.NaN; tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); tooltip.Width = Math.Ceiling(tooltip.DesiredSize.Width * 64) / 64; tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        foreach (var text in tooltipItems.Children.OfType<TextBlock>()) text.RenderTransform = new TranslateTransform { Y = 18 - text.BaselineOffset };
        tooltipContainer.Margin = new Thickness(BleeckerTooltipPlacement.Position(coordinate.X, tooltip.DesiredSize.Width, 5, RenderWidth - 5 - radialLegendWidth), BleeckerTooltipPlacement.Position(coordinate.Y, tooltip.DesiredSize.Height, 5, RenderHeight - 5), 0, 0);
        if (descriptions.Count == 1) tooltip.Visibility = tooltipContainer.Visibility = Visibility.Collapsed;
        UpdateTooltipEffects(); AutomationProperties.SetHelpText(this, string.Join("; ", descriptions));
    }
}
