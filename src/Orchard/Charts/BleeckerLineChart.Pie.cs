using System.Collections;
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
    private bool pieMode, pieDonutIdentity;
    private string pieNameKey = "name", pieDataKey = "value";
    private object pieInnerRadius = 0d;
    private object?[] pieValues = [];
    private BleeckerPieGeometry.Sector[] pieSectors = [];
    private double pieCx, pieCy, pieInner, pieOuter;
    internal void UsePieProfile(string nameKey, string dataKey, object radius, bool donut)
    {
        pieMode = true; sparklineMode = barMode = areaMode = scatterMode = radarMode = radialBarMode = funnelMode = sankeyMode = false;
        pieNameKey = nameKey; pieDataKey = dataKey; pieInnerRadius = radius; pieDonutIdentity = donut; QueueRender();
    }
    private object? PieValue(int index, string key, object? missing) => BleeckerChartData.TryRead(Data[index], key, out var value) ? value : missing;
    private static string PieReactText(object? value) => value is null or bool ? "" : value is IList list ? string.Concat(list.Cast<object?>().Select(PieReactText)) : value is double number ? BleeckerChartScale.TickLabel(number) : BleeckerChartData.Label(value);
    private string PieLabelName(int index) { var value = PieValue(index, pieNameKey, index); return value is null ? "null" : value is bool flag ? flag ? "true" : "false" : PieReactText(value); }
    private Brush? PieColor(int index)
    {
        var value = BleeckerChartData.Read(Data[index], "color");
        return value is null ? null : value as Brush ?? throw new InvalidOperationException("Pie data color requires a native Brush.");
    }
    private void RenderPie()
    {
        plot!.Children.Clear(); plot.Clip = null; legend!.Children.Clear(); rendered.Clear();
        left = top = 5; right = RenderWidth - 5; bottom = RenderHeight - 5 - (ShowLegend ? 36 : 0);
        pieCx = (left + right) / 2; pieCy = (top + bottom) / 2;
        var radius = Math.Max(0, Math.Min(right - left, bottom - top) / 2); pieOuter = radius * .8; pieInner = BleeckerPieGeometry.Radius(pieInnerRadius, radius);
        pieValues = Enumerable.Range(0, Data.Count).Select(i => PieValue(i, pieDataKey, 0d)).ToArray(); pieSectors = BleeckerPieGeometry.Sectors(pieValues);
        RenderPieLegend();
        for (var i = 0; i < pieSectors.Length; i++)
        {
            var sector = pieSectors[i]; var pathData = BleeckerPieGeometry.Path(pieCx, pieCy, pieInner, pieOuter, sector);
            Path? path = null;
            if (pathData is not null)
            {
                path = Resource<Path>($"<Path Data=\"{pathData}\" Fill=\"{{ThemeResource {BleeckerChartTheme.PickColor(i)}}}\" Stroke=\"{{ThemeResource BleeckerPieBorderBrush}}\" StrokeThickness=\"1\" />");
                path.Tag = i; if (PieColor(i) is Brush color) path.Fill = color; plot.Children.Add(path);
            }
            var label = Text(PieLabelName(i) + ": " + Math.Round(sector.Percent * 100, MidpointRounding.AwayFromZero).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%", 16, BleeckerChartTheme.PickColor(i)!);
            if (PieColor(i) is Brush paint) label.Foreground = paint;
            label.IsHitTestVisible = false; label.Tag = "pie-label-" + i; label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var position = BleeckerPieGeometry.Polar(pieCx, pieCy, pieOuter + 20, (sector.Start + sector.End) / 2);
            AddText(plot, label, position.X > pieCx ? position.X : position.X < pieCx ? position.X - label.DesiredSize.Width : position.X - label.DesiredSize.Width / 2, position.Y - label.BaselineOffset);
        }
        PaintInteraction(); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private void RenderPieLegend()
    {
        if (!ShowLegend || Data.Count == 0) return;
        var entries = new List<(FrameworkElement Element, double Width)>();
        foreach (var i in Enumerable.Range(0, Data.Count).OrderBy(i => PieReactText(PieValue(i, pieNameKey, i)), StringComparer.Ordinal))
        {
            var entry = new Canvas { Height = 24 };
            var icon = Resource<Path>($"<Path Data=\"M0,4h32v24h-32z\" Fill=\"{{ThemeResource {BleeckerChartTheme.PickColor(i)}}}\" />");
            var iconCanvas = new Canvas { Width = 32, Height = 32 }; iconCanvas.Children.Add(icon);
            var iconView = new Viewbox { Width = 14, Height = 14, Child = iconCanvas }; Canvas.SetTop(iconView, 6.65625); entry.Children.Add(iconView);
            var label = Text(PieReactText(PieValue(i, pieNameKey, i)), 16, BleeckerChartTheme.PickColor(i)!);
            if (PieColor(i) is Brush paint) { icon.Fill = paint; label.Foreground = paint; }
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); Canvas.SetLeft(label, 18); Canvas.SetTop(label, 18 - label.BaselineOffset); entry.Children.Add(label);
            entry.Width = 18 + Math.Round(label.DesiredSize.Width * 64, MidpointRounding.AwayFromZero) / 64; entries.Add((entry, entry.Width + 10));
        }
        var rows = new List<List<(FrameworkElement Element, double Width)>> { new() }; var width = 0d;
        foreach (var entry in entries) { if (width > 0 && width + entry.Width > RenderWidth - 10) { rows.Add([]); width = 0; } rows[^1].Add(entry); width += entry.Width; }
        for (var row = 0; row < rows.Count; row++)
        {
            var x = (RenderWidth - rows[row].Sum(item => item.Width)) / 2;
            foreach (var entry in rows[row]) { Canvas.SetLeft(entry.Element, x); Canvas.SetTop(entry.Element, RenderHeight - 41 + row * 24); legend!.Children.Add(entry.Element); x += entry.Width; }
        }
    }
    private void MovePiePointer(PointerRoutedEventArgs args)
    {
        for (var source = args.OriginalSource as DependencyObject; source is not null && !ReferenceEquals(source, this); source = VisualTreeHelper.GetParent(source))
            if (source is Path { Tag: int index } && index < pieSectors.Length) { interaction.Pointer(index, 0); PaintInteraction(); return; }
        interaction.Exit(); PaintInteraction();
    }
    private void ShowPieActive(int index)
    {
        if (tooltip is null || tooltipItems is null || tooltipContainer is null || index >= pieSectors.Length) { ClearActive(); return; }
        active!.Children.Clear(); tooltipItems.Children.Clear();
        var raw = pieValues[index]; if (raw is null) { ClearActive(); return; }
        var label = Text(" : " + PieReactText(raw), 16, "BleeckerTextPrimaryBrush"); label.LineHeight = 24; label.LineStackingStrategy = LineStackingStrategy.BlockLineHeight; label.Margin = new Thickness(0, 4, 0, 4); tooltipItems.Children.Add(label);
        var sector = pieSectors[index]; var point = BleeckerPieGeometry.Polar(pieCx, pieCy, (pieInner + pieOuter) / 2, (sector.Start + sector.End) / 2);
        tooltip.Visibility = tooltipContainer.Visibility = Visibility.Visible; tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        label.RenderTransform = new TranslateTransform { Y = 18 - label.BaselineOffset };
        tooltipContainer.Margin = new Thickness(BleeckerTooltipPlacement.Position(point.X, tooltip.DesiredSize.Width, left, right), BleeckerTooltipPlacement.Position(point.Y, tooltip.DesiredSize.Height, top, bottom), 0, 0);
        UpdateTooltipEffects(); AutomationProperties.SetHelpText(this, label.Text);
    }
}
