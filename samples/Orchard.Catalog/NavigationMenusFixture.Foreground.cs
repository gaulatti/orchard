using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class NavigationMenusFixture
{
    internal async Task VerifyForegroundAsync(string folder, ElementTheme theme)
    {
        static IEnumerable<FrameworkElement> Elements(DependencyObject parent)
        {
            if (parent is FrameworkElement element) yield return element;
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                foreach (var child in Elements(VisualTreeHelper.GetChild(parent, i))) yield return child;
        }
        static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
        async Task AwaitAsync(Func<bool> condition, string message)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (!condition() && watch.Elapsed < TimeSpan.FromSeconds(2))
            {
                Default.UpdateLayout();
                await Task.Delay(16);
            }
            Require(condition(), message);
        }

        Result.Text = "No fictional destination selected";
        await MainWindow.AwaitPresentationAsync(Default);
        using var input = new NativeInputSession(XamlRoot, folder, $"navigation-menu-{theme}");
        await input.AcquireAsync(Result);
        var home = Elements(Default).OfType<HyperlinkButton>().Single(link =>
            link.DataContext is BleeckerNavigationLinkContext context && context.Item.Label == "Home");
        Require(home.Focus(FocusState.Keyboard), "Navigation Menu Home rejected keyboard focus.");
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), home),
            "Navigation Menu Home did not hold keyboard focus.");
        input.Key(0x0D);
        await AwaitAsync(() => Result.Text.Contains("Home; href: /fictional/home", StringComparison.Ordinal),
            "Keyboard Enter did not invoke the original Home link.");

        var trigger = Elements(Default).OfType<Button>().Single(button =>
            button.DataContext is BleeckerNavigationMenuGroup group && group.Trigger == "Components");
        await input.ClickAsync(trigger);
        HyperlinkButton? buttons = null;
        await AwaitAsync(() =>
        {
            var roots = new List<DependencyObject> { XamlRoot.Content };
            roots.AddRange(VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot)
                .Where(popup => popup.Child is not null).Select(popup => (DependencyObject)popup.Child));
            buttons = roots.SelectMany(Elements).OfType<HyperlinkButton>().FirstOrDefault(link =>
                link.IsLoaded && link.DataContext is BleeckerNavigationLinkContext context &&
                context.Item.Label == "Buttons");
            return buttons is not null;
        }, "Pointer click did not open the Navigation Menu group.");
        await input.ClickAsync(buttons!);
        await AwaitAsync(() => Result.Text.Contains("Buttons; href: /fictional/buttons", StringComparison.Ordinal),
            $"Pointer click did not invoke the original grouped link. Result: {Result.Text}; enabled: {buttons!.IsEnabled}; command: {(buttons.DataContext as BleeckerNavigationLinkContext)?.Command.CanExecute(null)}.");
        await File.WriteAllTextAsync(Path.Combine(folder, $"navigation-menu-foreground-{theme}.json"),
            JsonSerializer.Serialize(new { passed = true, theme, keyboardHomeFocused = true,
                keyboardHomeInvoked = true, pointerGroupOpened = true, pointerGroupedLinkInvoked = true }));
    }
}
