using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Text.Json;

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

    internal async Task VerifyInputAsync(BleeckerColorScheme scheme, string folder)
    {
        using var input = new NativeInputSession(XamlRoot, folder, $"header-{scheme}");
        var root = CatalogFixtureRoot.From(XamlRoot);
        var host = new Grid { Margin = new Thickness(16), Background = root.Background };
        Grid.SetRowSpan(host, root.RowDefinitions.Count);
        Grid.SetColumnSpan(host, root.ColumnDefinitions.Count);
        root.Children.Add(host);
        var navigation = new ObservableCollection<BleeckerNavItem>
        {
            new("#one", "One"), new("#two", "Two"), new("#three", "Three")
        };
        var invoked = new List<string>();
        var header = new BleeckerHeader
        {
            Position = BleeckerHeaderPosition.Static,
            Brand = new BleeckerHeaderBrand { Href = "#library", BrandName = "Fictional library" },
            Navigation = navigation
        };
        header.ItemInvoked += (_, item) => invoked.Add(item.Href);
        host.Children.Add(header);
        try
        {
            await AwaitAsync(() => header.IsLoaded, "Header did not mount.");
            await input.AcquireAsync(host);
            UpdateLayout();
            var width = (int)Math.Round(XamlRoot.Size.Width);
            var narrow = width < 1024;
            var menu = Find<BleeckerNavMenu>(header, narrow ? "HeaderMobileNavigation" : "HeaderDesktopNavigation");
            var links = FindAll<HyperlinkButton>(menu, "NavLink").ToArray();
            Require(links.Length == 3, "Header did not retain three caller navigation links.");
            var toggle = narrow ? Find<BleeckerIconButton>(header, "HeaderToggle") : null;
            if (narrow)
            {
                await input.ClickAsync(toggle!);
                await AwaitAsync(() => header.IsMenuOpen, "Pointer did not open the narrow Header menu.");
                var panel = Find<FrameworkElement>(header, "HeaderMobilePanel");
                await AwaitAsync(() => { UpdateLayout(); return panel.ActualHeight > 40 && links[0].ActualWidth > 0; }, "Narrow Header menu did not lay out.");
                Require(toggle!.Focus(FocusState.Keyboard), "Narrow Header toggle could not take focus.");
                input.Key(0x09);
                await Task.Delay(100);
                if (!ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), links[0]))
                {
                    var focused = FocusManager.GetFocusedElement(XamlRoot) as FrameworkElement;
                    var direct = links[0].Focus(FocusState.Keyboard);
                    var context = links[0].Tag as BleeckerNavLinkContext;
                    throw new InvalidOperationException($"Tab skipped the first narrow Header link: focused={focused?.GetType().Name}/{focused?.Name}, directFocus={direct}, panelHeight={panel.ActualHeight}, linkWidth={links[0].ActualWidth}, linkHeight={links[0].ActualHeight}, loaded={links[0].IsLoaded}, enabled={links[0].IsEnabled}, visible={links[0].Visibility}, tabStop={links[0].IsTabStop}, canExecute={context?.Command.CanExecute(null)}, menuVisible={menu.Visibility}, menuEnabled={menu.IsEnabled}, ownerLoaded={header.IsLoaded}, ownerEnabled={header.IsEnabled}.");
                }
            }
            else Require(links[0].Focus(FocusState.Keyboard), "First desktop Header link could not take focus.");
            input.Key(0x09);
            await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), links[1]), "Tab did not reach the next caller Header link.");
            input.Key(0x20);
            await AwaitAsync(() => invoked.Count == 1 && invoked[0] == "#two", "Space did not invoke the original second item.");
            if (narrow)
            {
                await input.ClickAsync(toggle!);
                await AwaitAsync(() => header.IsMenuOpen, "Narrow Header menu did not reopen.");
            }
            await input.ClickAsync(links[2]);
            await AwaitAsync(() => invoked.Count == 2 && invoked[1] == "#three", "Pointer did not invoke the original third item.");
            Require(ReferenceEquals(header.Navigation, navigation) && ReferenceEquals(navigation[1], menu.Items.ElementAt(1)), "Header replaced caller navigation objects.");
            await File.WriteAllTextAsync(Path.Combine(folder, $"header-input-{scheme}-{width}.json"), JsonSerializer.Serialize(new
            {
                passed = true, theme = scheme.ToString(), width, narrow,
                keyboardInvocations = 1, pointerInvocations = 1,
                narrowToggleOpened = narrow, callerItemsRetained = true, hardwareInputSent = true
            }));
        }
        finally { root.Children.Remove(host); }
    }

    private static T Find<T>(DependencyObject parent, string name) where T : FrameworkElement =>
        FindAll<T>(parent, name).Single();

    private static IEnumerable<T> FindAll<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        if (parent is T item && item.Name == name) yield return item;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            foreach (var child in FindAll<T>(VisualTreeHelper.GetChild(parent, index), name)) yield return child;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task AwaitAsync(Func<bool> predicate, string message)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (predicate()) return;
            await Task.Delay(20);
        }
        throw new InvalidOperationException(message);
    }
}
