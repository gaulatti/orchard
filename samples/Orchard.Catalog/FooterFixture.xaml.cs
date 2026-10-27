using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class FooterFixture : StackPanel
{
    private readonly ObservableCollection<BleeckerFooterSection> sections = [];
    public FooterFixture()
    {
        InitializeComponent();
        var navigation = new BleeckerFooterSection { Title = "Navigation", Items = new ObservableCollection<BleeckerNavItem> { new("#home", "Home"), new("#components", "Components"), new("#tokens", "Tokens") } };
        var resources = (BleeckerFooterSection)Resources["Resources"]; resources.Items = new ObservableCollection<BleeckerNavItem> { new("https://github.com/example/fictional", "Source", true), new("#catalog", "Catalog") };
        sections.Add(navigation); sections.Add(resources); Default.Sections = Custom.Sections = sections;
    }
    private void Invoked(object? sender, BleeckerNavItem item) => Status.Text = "Host request: " + item.Label + " → " + item.Href;
    private void VariantChanged(object sender, SelectionChangedEventArgs args) => ApplyVariant();
    internal void ShowVariant(int index) { VariantPicker.SelectedIndex = index; ApplyVariant(); }
    private void ApplyVariant()
    {
        if (DefaultGroup is null || CustomGroup is null || MinimalGroup is null) return;
        DefaultGroup.Visibility = VariantPicker.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        CustomGroup.Visibility = VariantPicker.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        MinimalGroup.Visibility = VariantPicker.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Reorder(object sender, RoutedEventArgs args) { if (sections.Count > 1) sections.Move(0, sections.Count - 1); }
    private void Rename(object sender, RoutedEventArgs args) { const string longLabel = "A deliberately long fictional component navigation label that wraps across several lines"; var first = sections.SelectMany(section => section.Items).First(); first.Label = first.Label == longLabel ? "Home" : longLabel; }
    private void OverrideRight(object sender, RoutedEventArgs args) => Default.BottomRight = Default.BottomRight is null ? "" : null;
    private void ToggleAccent(object sender, RoutedEventArgs args) => Default.ShowBottomAccent = !Default.ShowBottomAccent;
}
