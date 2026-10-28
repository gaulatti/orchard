using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Orchard.Catalog;

public sealed partial class ContextMenusFixture : StackPanel
{
    public ContextMenusFixture() => InitializeComponent();
    private void Changed(object sender,RoutedEventArgs args) => Status.Text = "Selected: " + ((MenuFlyoutItem)sender).Text;
    private void OutsideChanged(object sender,RoutedEventArgs args) => Status.Text = "Outside action activated";
    private void OpenOwned(object sender,RoutedEventArgs args) => OwnedModal.IsOpen = true;
    private void CloseOwned(object sender,RoutedEventArgs args) => OwnedModal.IsOpen = false;
    private void OwnedChanged(object sender,RoutedEventArgs args) => OwnedStatus.Text = "Selected: " + ((MenuFlyoutItem)sender).Text;
    private void OwnedOutsideChanged(object sender,RoutedEventArgs args) => OwnedStatus.Text = "Owned outside action activated";
}
