using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;

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
        Require(Menu.IsConstrainedToRootBounds, "Branded menu escaped the application viewport.");
        Require(presenter.Padding.Left == 6 && presenter.CornerRadius.TopLeft == 7 && presenter.MinWidth == 176 && !presenter.IsDefaultShadowEnabled, "Menu presenter retained Windows geometry or shadow.");
        Require(presenter.FontFamily.Source.Contains("EncodeSans-Regular.ttf"), "Menu presenter retained Windows typography.");
        VerifyShadow((BleeckerOverlaySurface)VisualTreeHelper.GetChild(presenter, 0));
        VerifyShadow(ShadowPreview);
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
        Require(childPresenter.MinWidth == 144 && childPresenter.ActualWidth == 144 && presenter.MinWidth == 176,
            $"Root/submenu geometry mismatch: root={presenter.MinWidth}, child={childPresenter.MinWidth}/{childPresenter.ActualWidth}.");
        VerifyShadow((BleeckerOverlaySurface)VisualTreeHelper.GetChild(childPresenter, 0));
        await MainWindow.CaptureElementAsync(childPresenter, Path.Combine(folder, $"submenu-{scheme.ToString().ToLowerInvariant()}.png"));
        expansion.Collapse();
        await Task.Delay(100);
        var checkPeer = new ToggleMenuFlyoutItemAutomationPeer(Checkbox);
        await InvokeAndWaitForCloseAsync(() => ((IToggleProvider)checkPeer.GetPattern(PatternInterface.Toggle)).Toggle());
        Require(!Checkbox.IsChecked, "Native checkbox menu action did not toggle.");
        Checkbox.IsChecked = true;
        if (!Menu.IsOpen) { Menu.ShowAt(Trigger); await Task.Delay(150); }
        var before = actions;
        await InvokeAndWaitForCloseAsync(() => ((IInvokeProvider)new MenuFlyoutItemAutomationPeer(Action).GetPattern(PatternInterface.Invoke)).Invoke());
        Require(actions == before + 1 && !Menu.IsOpen, "Native menu invocation did not execute and dismiss.");
        var originalStyle = Menu.MenuFlyoutPresenterStyle;
        var consumerStyle = new Style(typeof(MenuFlyoutPresenter)) { BasedOn = originalStyle };
        consumerStyle.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 220d));
        Menu.MenuFlyoutPresenterStyle = consumerStyle;
        Menu.ShowAt(Trigger);
        await Task.Delay(200);
        presenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Select(popup => popup.Child).OfType<MenuFlyoutPresenter>().Single();
        Require(presenter.MinWidth == 220 && presenter.ActualWidth == 220, "Consumer presenter width was overridden.");
        Menu.Hide();
        await Task.Delay(150);
        Menu.MenuFlyoutPresenterStyle = originalStyle;
        Menu.ShowAt(Trigger);
        await Task.Delay(200);
        presenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Select(popup => popup.Child).OfType<MenuFlyoutPresenter>().Single();
        Require(presenter.MinWidth == 176, "Restored default root menu lost its width.");
        Menu.Hide();
        await Task.Delay(150);
    }
    private async Task InvokeAndWaitForCloseAsync(System.Action invoke)
    {
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnClosed(object? sender, object args) => closed.TrySetResult();
        Menu.Closed += OnClosed;
        try
        {
            invoke();
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally { Menu.Closed -= OnClosed; }
    }
    private static void VerifyShadow(BleeckerOverlaySurface surface)
    {
        Require(!surface.IsTabStop && surface.FontFamily.Source.Contains("EncodeSans-Regular.ttf"), "Overlay surface inherited Windows typography or keyboard focus.");
        var root = (Grid)VisualTreeHelper.GetChild(surface, 0);
        var layers = (ContainerVisual)ElementCompositionPreview.GetElementChildVisual((Grid)root.Children[1]);
        var sprites = layers.Children.OfType<SpriteVisual>().ToArray();
        Require(sprites.Length == 2, "Overlay did not render two shadow layers.");
        foreach (var sprite in sprites)
        {
            var shadow = (DropShadow)sprite.Shadow;
            var far = shadow.BlurRadius == 64;
            Require((far || shadow.BlurRadius == 14) && shadow.Offset.Y == (far ? 24 : 4) &&
                Math.Abs(shadow.Opacity - (far ? .28 : .06)) < .001 && shadow.Color.R == 26 && shadow.Color.G == 55 && shadow.Color.B == 77,
                "Overlay shadow differs from the canonical recipe.");
            Require(Math.Abs(sprite.Size.X - (surface.ActualWidth - (far ? 40 : 0))) < .01,
                "Overlay shadow lost its negative spread.");
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
