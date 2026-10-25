using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;
public sealed partial class ScrollAreasFixture : StackPanel
{
    public ScrollAreasFixture() => InitializeComponent();
    private void Vertical(object sender, RoutedEventArgs args) => Area.Orientation = BleeckerScrollAreaOrientation.Vertical;
    private void Horizontal(object sender, RoutedEventArgs args) => Area.Orientation = BleeckerScrollAreaOrientation.Horizontal;
    private void Both(object sender, RoutedEventArgs args) => Area.Orientation = BleeckerScrollAreaOrientation.Both;
    private void Maximum(object sender, RoutedEventArgs args) => Area.MaxHeight = Area.MaxHeight == 240 ? 160 : 240;
    private void Direction(object sender, RoutedEventArgs args) => Area.FlowDirection = Area.FlowDirection == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    private void Scroll(object sender, RoutedEventArgs args) => Area.ScrollTo(80, 120);
    private void Action(object sender, RoutedEventArgs args) => Status.Text = "Original caller action ran.";
    private void Changed(object? sender, EventArgs args) => Status.Text = $"Offsets {Area.HorizontalOffset:0.##} / {Area.VerticalOffset:0.##}; viewport {Area.ViewportWidth:0.##} × {Area.ViewportHeight:0.##}; extent {Area.ExtentWidth:0.##} × {Area.ExtentHeight:0.##}";
}
