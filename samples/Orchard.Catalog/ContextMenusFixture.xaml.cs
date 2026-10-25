using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Orchard.Catalog;

public sealed partial class ContextMenusFixture : StackPanel
{
    public ContextMenusFixture() => InitializeComponent();
    private void Changed(object sender,RoutedEventArgs args) => Status.Text = "Selected: " + ((MenuFlyoutItem)sender).Text;
}
