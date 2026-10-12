using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace Orchard;

/// <summary>A branded native tooltip. Full placement and timing parity is in progress.</summary>
public sealed class BleeckerTooltip : ToolTip
{
    private BleeckerOverlaySurface? surface;
    private Polygon? arrow;
    private UIElement? inputRoot;

    public BleeckerTooltip()
    {
        DefaultStyleKey = typeof(BleeckerTooltip);
        Placement = PlacementMode.Top;
        RegisterPropertyChangedCallback(PlacementProperty, (_, _) => UpdateArrow());
        Opened += (_, _) =>
        {
            DetachInput();
            inputRoot = XamlRoot?.Content;
            inputRoot?.AddHandler(KeyDownEvent, new KeyEventHandler(OnRootKeyDown), true);
        };
        Closed += (_, _) => DetachInput();
        Unloaded += (_, _) => DetachInput();
    }

    private void OnRootKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (!IsOpen || args.Key != Windows.System.VirtualKey.Escape) return;
        IsOpen = false;
        args.Handled = true;
    }

    private void DetachInput()
    {
        inputRoot?.RemoveHandler(KeyDownEvent, new KeyEventHandler(OnRootKeyDown));
        inputRoot = null;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        surface = GetTemplateChild("PART_Surface") as BleeckerOverlaySurface;
        arrow = GetTemplateChild("PART_Arrow") as Polygon;
        UpdateArrow();
    }

    private void UpdateArrow()
    {
        if (surface is null || arrow is null) return;
        var vertical = Placement is PlacementMode.Top or PlacementMode.Bottom;
        arrow.Width = vertical ? 10 : 5;
        arrow.Height = vertical ? 5 : 10;
        arrow.HorizontalAlignment = Placement switch
        {
            PlacementMode.Left => HorizontalAlignment.Right,
            PlacementMode.Right => HorizontalAlignment.Left,
            _ => HorizontalAlignment.Center
        };
        arrow.VerticalAlignment = Placement switch
        {
            PlacementMode.Top => VerticalAlignment.Bottom,
            PlacementMode.Bottom => VerticalAlignment.Top,
            _ => VerticalAlignment.Center
        };
        surface.Margin = Placement switch
        {
            PlacementMode.Top => new Thickness(0, 0, 0, 5),
            PlacementMode.Bottom => new Thickness(0, 5, 0, 0),
            PlacementMode.Left => new Thickness(0, 0, 5, 0),
            PlacementMode.Right => new Thickness(5, 0, 0, 0),
            _ => new Thickness(0)
        };
        arrow.Visibility = Placement is PlacementMode.Top or PlacementMode.Bottom or PlacementMode.Left or PlacementMode.Right
            ? Visibility.Visible : Visibility.Collapsed;
        arrow.Points = Placement switch
        {
            PlacementMode.Bottom => Points(0, 5, 5, 0, 10, 5),
            PlacementMode.Left => Points(0, 0, 5, 5, 0, 10),
            PlacementMode.Right => Points(5, 0, 0, 5, 5, 10),
            _ => Points(0, 0, 10, 0, 5, 5)
        };
    }

    private static PointCollection Points(double x1, double y1, double x2, double y2, double x3, double y3)
        => [new Point(x1, y1), new Point(x2, y2), new Point(x3, y3)];
}
