using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard;

/// <summary>An auto-filling grid of original caller controls, retaining empty column tracks.</summary>
public sealed class BleeckerFeedGrid : Panel
{
    private UIElement[] visible = [];
    private double[] columns = [], rows = [];
    public BleeckerFeedGrid() { UseLayoutRounding = false; IsTabStop = false; }
    private static void Changed(DependencyObject owner, DependencyPropertyChangedEventArgs args) { var grid = (BleeckerFeedGrid)owner; grid.Validate(); grid.InvalidateMeasure(); }
    public static readonly DependencyProperty MinColumnWidthProperty = DependencyProperty.Register(nameof(MinColumnWidth), typeof(double), typeof(BleeckerFeedGrid), new PropertyMetadata(340d, Changed));
    public static readonly DependencyProperty GapProperty = DependencyProperty.Register(nameof(Gap), typeof(double), typeof(BleeckerFeedGrid), new PropertyMetadata(12d, Changed));
    public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register(nameof(Padding), typeof(Thickness), typeof(BleeckerFeedGrid), new PropertyMetadata(new Thickness(16), Changed));
    public double MinColumnWidth { get => (double)GetValue(MinColumnWidthProperty); set { Number(value); SetValue(MinColumnWidthProperty, value); } }
    public double Gap { get => (double)GetValue(GapProperty); set { Number(value); SetValue(GapProperty, value); } }
    public Thickness Padding { get => (Thickness)GetValue(PaddingProperty); set { Thickness(value); SetValue(PaddingProperty, value); } }
    private static void Number(double value) { if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Feed grid sizes must be finite and nonnegative."); }
    private static void Thickness(Thickness value) { Number(value.Left); Number(value.Top); Number(value.Right); Number(value.Bottom); }
    private void Validate() { Number(MinColumnWidth); Number(Gap); Thickness(Padding); }
    protected override Size MeasureOverride(Size available)
    {
        Validate(); visible = Children.Where(child => child.Visibility == Visibility.Visible).ToArray();
        var width = Math.Max(0, available.Width - Padding.Left - Padding.Right);
        if (!double.IsFinite(width))
        {
            foreach (var child in visible) child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            width = Math.Max(MinColumnWidth, visible.Select(child => child.DesiredSize.Width).DefaultIfEmpty(0).Max());
        }
        columns = BleeckerFeedGridModel.Tracks(width, MinColumnWidth, Gap); rows = new double[(visible.Length + columns.Length - 1) / columns.Length];
        for (var index = 0; index < visible.Length; index++)
        {
            visible[index].Measure(new Size(columns[index % columns.Length], double.PositiveInfinity));
            rows[index / columns.Length] = Math.Max(rows[index / columns.Length], visible[index].DesiredSize.Height);
        }
        return new Size(width + Padding.Left + Padding.Right, rows.Sum() + Gap * Math.Max(0, rows.Length - 1) + Padding.Top + Padding.Bottom);
    }
    protected override Size ArrangeOverride(Size final)
    {
        double x = Padding.Left, y = Padding.Top;
        for (var index = 0; index < visible.Length; index++)
        {
            var column = index % columns.Length; var row = index / columns.Length;
            if (column == 0) { x = Padding.Left; if (row > 0) y += rows[row - 1] + Gap; }
            var width = columns[column]; var height = rows[row];
            if (visible[index] is FrameworkElement child)
            {
                if (double.IsFinite(child.Width)) width = child.DesiredSize.Width;
                else width = Math.Max(child.MinWidth + child.Margin.Left + child.Margin.Right, Math.Min(width, child.MaxWidth + child.Margin.Left + child.Margin.Right));
                if (double.IsFinite(child.Height)) height = child.DesiredSize.Height;
            }
            visible[index].Arrange(new Rect(x, y, width, height)); x += columns[column] + Gap;
        }
        return final;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new GridPeer(this);
    private sealed class GridPeer(BleeckerFeedGrid owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerFeedGrid);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }
}
