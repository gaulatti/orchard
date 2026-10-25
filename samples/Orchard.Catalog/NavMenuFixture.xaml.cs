using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class NavMenuFixture : StackPanel
{
    private readonly ObservableCollection<BleeckerNavItem> items = [new("#home", "Home"), new("#components", "Components"), new("https://example.invalid/source", "Source", true)];
    public NavMenuFixture()
    {
        InitializeComponent(); foreach (var menu in new[] { Horizontal, Vertical, Rtl, Custom }) menu.Items = items;
        Constrained.Items = [new("#explore", "Explore our reusable components"), new("#guide", "Read the complete guide")];
        Markup.Items = [(BleeckerNavItem)Resources["MarkupItem"]];
    }
    private void Invoked(object? sender, BleeckerNavItem item) => Status.Text = item.Label + ": " + item.Href + (item.External ? " (external)" : "");
    private void Reorder(object sender, RoutedEventArgs args) => items.Move(0, items.Count - 1);
    private void Rename(object sender, RoutedEventArgs args) => items[0].Label = items[0].Label == "Start" ? "Home" : "Start";
}
