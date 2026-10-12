using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class MenusFixture : UserControl
{
    public MenusFixture() => InitializeComponent();
    private int actions;
    private void OnAction(object sender, RoutedEventArgs args) => Result.Text = $"Menu actions: {++actions}";
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
        await Task.Delay(100);
        Menu.ShowAt(Trigger);
        await Task.Delay(200);
        var presenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Select(popup => popup.Child).OfType<MenuFlyoutPresenter>().Single();
        Require(presenter.Padding.Left == 6 && presenter.CornerRadius.TopLeft == 7 && presenter.MinWidth == 176 && !presenter.IsDefaultShadowEnabled, "Menu presenter retained Windows geometry or shadow.");
        Require(presenter.FontFamily.Source.Contains("EncodeSans-Regular.ttf"), "Menu presenter retained Windows typography.");
        foreach (var item in Menu.Items.OfType<MenuFlyoutItem>())
            Require(item.FontSize == 14 && item.CornerRadius.TopLeft == 7 && item.Padding.Top == 6 && item.ActualHeight == 32, $"Menu item geometry mismatch: {item.Text} {item.ActualHeight}.");
        var root = (Grid)VisualTreeHelper.GetChild(Action, 0);
        VisualStateManager.GoToState(Action, "PointerOver", false);
        Require(root.Background is SolidColorBrush hover && Math.Abs(hover.Opacity - (scheme == BleeckerColorScheme.Dark ? .15 : .1)) < .001, "Menu hover did not use Bleecker's tint.");
        VisualStateManager.GoToState(Action, "Normal", false);
        Action.Focus(FocusState.Keyboard);
        await Task.Delay(100);
        var focusHighlight = (Border)root.Children[0];
        Require(focusHighlight.Opacity == 1 && ((Border)focusHighlight.Child).Background is SolidColorBrush focus && Math.Abs(focus.Opacity - (scheme == BleeckerColorScheme.Dark ? .15 : .1)) < .001,
            $"Keyboard focus did not visibly highlight the menu row: {Action.FocusState}, states={string.Join(',', VisualStateManager.GetVisualStateGroups(root).Select(group => group.CurrentState?.Name))}.");
        await MainWindow.CaptureElementAsync(presenter, Path.Combine(folder, $"menu-{scheme.ToString().ToLowerInvariant()}.png"));
        Compact.Focus(FocusState.Keyboard);
        await Task.Delay(100);
        Require(focusHighlight.Opacity == 0, "Menu focus highlight remained on the previous row.");
        Compact.IsChecked = true;
        Require(!Comfortable.IsChecked && Compact.IsChecked, "Native radio group retained multiple selections.");
        Comfortable.IsChecked = true;
        Require(!Compact.IsChecked, "Native radio group did not restore selection.");
        var expansion = (IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Submenu).GetPattern(PatternInterface.ExpandCollapse);
        expansion.Expand();
        await Task.Delay(200);
        var childPresenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Select(popup => popup.Child).OfType<MenuFlyoutPresenter>().Single(candidate => candidate != presenter);
        Require(childPresenter.FontFamily.Source.Contains("EncodeSans-Regular.ttf") && childPresenter.Padding.Left == 6, "Submenu popup retained Windows appearance.");
        await MainWindow.CaptureElementAsync(childPresenter, Path.Combine(folder, $"submenu-{scheme.ToString().ToLowerInvariant()}.png"));
        expansion.Collapse();
        await Task.Delay(100);
        var checkPeer = new ToggleMenuFlyoutItemAutomationPeer(Checkbox);
        ((IToggleProvider)checkPeer.GetPattern(PatternInterface.Toggle)).Toggle();
        await Task.Delay(200);
        Require(!Checkbox.IsChecked, "Native checkbox menu action did not toggle.");
        Checkbox.IsChecked = true;
        if (!Menu.IsOpen) { Menu.ShowAt(Trigger); await Task.Delay(150); }
        var before = actions;
        ((IInvokeProvider)new MenuFlyoutItemAutomationPeer(Action).GetPattern(PatternInterface.Invoke)).Invoke();
        await Task.Delay(100);
        Require(actions == before + 1 && !Menu.IsOpen, "Native menu invocation did not execute and dismiss.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
