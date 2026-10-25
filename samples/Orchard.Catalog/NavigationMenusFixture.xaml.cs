using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class NavigationMenusFixture : UserControl
{
    private readonly BleeckerNavigationMenuLink retained = new("/fictional/retained", "Original retained link", "Edit its local draft, then update this label.");
    public NavigationMenusFixture()
    {
        InitializeComponent();
        Custom.Entries = new ObservableCollection<IBleeckerNavigationMenuEntry> { new BleeckerNavigationMenuLink("/fictional/home", "Home"), new BleeckerNavigationMenuGroup("Components", [retained]) };
        Rtl.Entries = Default.Entries; Disabled.Entries = Default.Entries;
    }
    private void Invoked(object? sender, BleeckerNavigationMenuLink item) => Result.Text = $"Host received original link: {item.Label}; href: {item.Href}; external: {item.External}";
    private void Rename(object sender, RoutedEventArgs args) => retained.Label = retained.Label == "Original retained link" ? "Updated retained link" : "Original retained link";
}
