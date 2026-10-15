using System.Collections.ObjectModel;
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
    private bool currencyMode;
    private ObservableCollection<object?>? currencyData, observedCurrencyData;
    private BleeckerCurrencyChartLayout? currencyLayout;
    internal void UseCurrencyProfile(ObservableCollection<object?> data)
    {
        var changed = !currencyMode || !ReferenceEquals(currencyData, data);
        currencyMode = true; sparklineMode = barMode = areaMode = pieMode = scatterMode = radarMode = radialBarMode = funnelMode = sankeyMode = false;
        currencyData = data;
        if (changed && IsLoaded) Observe();
        QueueRender();
    }
    private void ObserveCurrency()
    {
        if (!currencyMode || currencyData is null) return;
        observedCurrencyData = currencyData; observedCurrencyData.CollectionChanged += OnCollectionChanged;
        foreach (var record in currencyData.OfType<INotifyPropertyChanged>()) if (observedRecords.Add(record)) record.PropertyChanged += OnRecordChanged;
    }
    private void StopObservingCurrency()
    {
        if (observedCurrencyData is not null) observedCurrencyData.CollectionChanged -= OnCollectionChanged;
        observedCurrencyData = null;
    }
    private void RenderCurrency()
    {
        if (plot is null || legend is null || currencyData is null) return;
        currencyLayout = BleeckerCurrencyChartLayout.Compute(currencyData, ActualWidth, ActualHeight);
        var layout = currencyLayout; left = layout.Left; right = layout.Right; top = layout.Top; bottom = layout.Bottom;
        plot.Clip = null; plot.Children.Clear(); legend.Children.Clear(); rendered.Clear();
        var visibleY = new List<double>(); var boundary = 0d;
        if (bottom > top) for (var i = layout.Ticks.Length - 1; i >= 0; i--)
        {
            var coordinate = layout.Y(layout.Ticks[i]); if (i == layout.Ticks.Length - 1) coordinate = Math.Max(coordinate, 12);
            if (coordinate - 12 < boundary || coordinate + 12 > RenderHeight) continue;
            var label = Text("$" + BleeckerChartScale.TickLabel(layout.Ticks[i]), 12, "BleeckerTextSecondaryBrush"); label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            AddText(plot, label, left - 8 - label.DesiredSize.Width, coordinate + 4.26 - label.BaselineOffset); visibleY.Add(layout.Y(layout.Ticks[i])); boundary = coordinate + 17;
        }
        if (right > left && bottom > top)
        {
            var grid = layout.Ticks.Length == 0 ? new[] { top, bottom } : visibleY.AsEnumerable().Reverse().Concat(new[] { top, bottom }).Distinct();
            foreach (var y in grid) plot.Children.Insert(0, Resource<Line>($"<Line X1=\"{N(left)}\" X2=\"{N(right)}\" Y1=\"{N(y)}\" Y2=\"{N(y)}\" Stroke=\"{{ThemeResource BleeckerBorderBrush}}\" StrokeThickness=\"1\" StrokeDashArray=\"3,3\" />"));
        }
        boundary = RenderWidth;
        for (var i = right > left ? layout.Categories.Length - 1 : -1; i >= 0; i--)
        {
            var value = layout.AxisText(i); var probe = Text(value, 16, "BleeckerTextSecondaryBrush"); probe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); var size = Math.Ceiling(BleeckerPieText.InlineWidth(probe) * 64) / 64;
            var coordinate = layout.X(i); if (i == layout.Categories.Length - 1) coordinate = Math.Min(coordinate, RenderWidth - size / 2);
            if (coordinate - size / 2 < 0 || coordinate + size / 2 > boundary) continue;
            var label = Text(value, 12, "BleeckerTextSecondaryBrush"); label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            AddText(plot, label, coordinate - label.DesiredSize.Width / 2, RenderHeight - 18.48 - label.BaselineOffset); boundary = coordinate - size / 2 - 5;
        }
        if (layout.HasCurve)
        {
            var path = Resource<Path>("<Path Stroke=\"{ThemeResource BleeckerDesertBrush}\" StrokeThickness=\"2\" />"); path.Data = Geometry(BleeckerMonotoneCurve.Create(layout.Points)); plot.Children.Add(path);
        }
        PaintInteraction(); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private void MoveCurrencyPointer(PointerRoutedEventArgs args)
    {
        if (currencyLayout is not { } layout || layout.InspectionCount == 0) return;
        var point = args.GetCurrentPoint(this).Position;
        if (point.X < left || point.X > right || point.Y < top || point.Y > bottom) { interaction.Exit(); PaintInteraction(); return; }
        var count = layout.Categories.Length; var index = count <= 1 || right <= left ? 0 : (int)Math.Ceiling((point.X - left) / (right - left) * (count - 1) - .5);
        interaction.Pointer(Math.Clamp(index, 0, layout.InspectionCount - 1), point.Y); PaintInteraction();
    }
    private void ShowCurrencyActive(int index, double? pointerY)
    {
        if (currencyLayout is not { } layout || active is null || tooltip is null || tooltipItems is null || tooltipContainer is null) return;
        active.Children.Clear(); tooltipItems.Children.Clear();
        var category = layout.CategoryText(index); var payload = layout.PayloadIndex(index); var raw = payload < 0 ? null : BleeckerCurrencyChartLayout.Read(layout.Records[payload], "total");
        var help = new List<string>();
        if (category.Length > 0)
        {
            var heading = Text(ChartVisibleText(category), 16, "BleeckerTextSecondaryBrush"); heading.FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 }; heading.LineHeight = 24; heading.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            if (FontFamily.Source == "ms-appx:///Orchard/Assets/Fonts/EncodeSans-Regular.ttf#Encode Sans") heading.FontFamily = Resource<TextBlock>("<TextBlock FontFamily=\"{StaticResource BleeckerPrimarySemiboldFontFamily}\" />").FontFamily;
            tooltipItems.Children.Add(heading); help.Add(category);
        }
        if (double.IsFinite(layout.X(index))) active.Children.Add(Resource<Line>($"<Line X1=\"{N(layout.X(index))}\" X2=\"{N(layout.X(index))}\" Y1=\"{N(top)}\" Y2=\"{N(bottom)}\" Stroke=\"{{ThemeResource BleeckerCurrencyCursorBrush}}\" />"));
        if (raw is not null)
        {
            var value = "$" + BleeckerCurrencyChartLayout.StringValue(raw); var content = "Total : " + value;
            var row = Text(ChartVisibleText(content), 16, "BleeckerTextPrimaryBrush"); row.LineHeight = 24; row.LineStackingStrategy = LineStackingStrategy.BlockLineHeight; row.Margin = new Thickness(0, 4, 0, 4);
            row.Width = ChartFragmentsWidth(new[] { "Total", " : ", ChartVisibleText(value) });
            tooltipItems.Children.Add(row); help.Add(content);
            if (layout.ActivePoint(index) is { } point)
            {
                // WinUI's shape bounds include the stroke; SVG's r excludes it.
                var dot = Resource<Ellipse>("<Ellipse Width=\"14\" Height=\"14\" Fill=\"{ThemeResource BleeckerDesertBrush}\" Stroke=\"{ThemeResource BleeckerCardBrush}\" StrokeThickness=\"2\" />"); Canvas.SetLeft(dot, point.X - 7); Canvas.SetTop(dot, point.Y - 7); active.Children.Add(dot);
            }
        }
        tooltip.Visibility = tooltipContainer.Visibility = raw is not null ? Visibility.Visible : Visibility.Collapsed;
        tooltip.Width = double.NaN; tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); tooltip.Width = Math.Ceiling(tooltip.DesiredSize.Width * 64) / 64; tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        foreach (var label in tooltipItems.Children.OfType<TextBlock>()) label.RenderTransform = new TranslateTransform { Y = 18 - label.BaselineOffset };
        tooltipContainer.Margin = double.IsFinite(layout.X(index)) ? new Thickness(BleeckerTooltipPlacement.Position(layout.X(index), tooltip.DesiredSize.Width, left, right), BleeckerTooltipPlacement.Position(pointerY ?? (top + RenderHeight) / 2, tooltip.DesiredSize.Height, top, bottom), 0, 0) : new Thickness(0);
        UpdateTooltipEffects(); AutomationProperties.SetHelpText(this, string.Join("; ", help));
    }
}
