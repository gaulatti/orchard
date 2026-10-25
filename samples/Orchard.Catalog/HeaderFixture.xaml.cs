using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class HeaderFixture : StackPanel
{
    private readonly ObservableCollection<BleeckerNavItem> items = [new("#home", "Home"), new("#components", "Components"), new("#tokens", "Tokens")];
    public HeaderFixture() { InitializeComponent(); Default.Navigation = Custom.Navigation = Composed.Navigation = items; }
    private void Toggle(object sender, RoutedEventArgs args) => Default.ToggleNavigation();
    private void Reorder(object sender, RoutedEventArgs args) { if (items.Count > 1) items.Move(0, items.Count - 1); }
    private void Rename(object sender, RoutedEventArgs args) => Default.Brand.BrandName = Default.Brand.BrandName == "Fictional library" ? "A fictional component library" : "Fictional library";
    private void Invoked(object? sender, BleeckerNavItem item) => Status.Text = "Host request: " + item.Label + " → " + item.Href;
    private void BrandInvoked(object? sender, BleeckerHeaderBrand brand) => Status.Text = "Original brand request: " + brand.Href;
}
