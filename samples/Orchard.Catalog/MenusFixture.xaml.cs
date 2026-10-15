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
        await MainWindow.AwaitPresentationAsync(this);
        using var input = new NativeInputSession(XamlRoot, folder, $"menus-{scheme}");
        var heading = (FrameworkElement)((StackPanel)Content).Children[0];
        await input.AcquireAsync(heading);
        await input.MoveAsync(heading);
        Menu.ShowAt(Trigger);
        await AwaitPresenterAsync();
        var presenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Select(popup => popup.Child).OfType<MenuFlyoutPresenter>().Single();
        Require(Menu.IsConstrainedToRootBounds, "Branded menu escaped the application viewport.");
        Require(presenter.Padding.Left == 6 && presenter.CornerRadius.TopLeft == 7 && presenter.MinWidth == 176 && !presenter.IsDefaultShadowEnabled, "Menu presenter retained Windows geometry or shadow.");
        Require(presenter.FontFamily.Source.Contains("EncodeSans-Regular.ttf"), "Menu presenter retained Windows typography.");
        VerifyShadow((BleeckerOverlaySurface)VisualTreeHelper.GetChild(presenter, 0));
        VerifyShadow(ShadowPreview);
        VerifyLabels(presenter);
        VerifyInset(InsetAction);
        VerifyInset(Submenu);
        VerifyContentAndShortcuts();
        foreach (var item in Menu.Items.OfType<MenuFlyoutItem>().Where(item => item is not BleeckerMenuLabel))
            Require(item.FontSize == 14 && item.CornerRadius.TopLeft == 7 && item.Padding.Top == 6 && item.ActualHeight == 32, $"Menu item geometry mismatch: {item.Text} {item.ActualHeight}.");
        var root = (Grid)VisualTreeHelper.GetChild(Action, 0);
        VisualStateManager.GoToState(Action, "PointerOver", false);
        Require(root.Background is SolidColorBrush hover && Math.Abs(hover.Opacity - (scheme == BleeckerColorScheme.Dark ? .15 : .1)) < .001, "Menu hover did not use Bleecker's tint.");
        VisualStateManager.GoToState(Action, "Normal", false);
        Require(Action.Focus(FocusState.Keyboard), "Menu action refused keyboard focus.");
        var focusWait = System.Diagnostics.Stopwatch.StartNew();
        while (Action.FocusState != FocusState.Keyboard && focusWait.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(16);
        Require(Action.FocusState == FocusState.Keyboard && ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Action),
            $"Menu keyboard focus did not arrive: state={Action.FocusState}, open={Menu.IsOpen}, foreground={input.OwnsForeground}, focused={Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot)?.GetType().Name}.");
        var focusHighlight = (Border)root.Children[0];
        Require(focusHighlight.Opacity == 1 && ((Border)focusHighlight.Child).Background is SolidColorBrush focus && Math.Abs(focus.Opacity - (scheme == BleeckerColorScheme.Dark ? .15 : .1)) < .001,
            $"Keyboard focus did not visibly highlight the menu row: {Action.FocusState}, states={string.Join(',', VisualStateManager.GetVisualStateGroups(root).Select(group => group.CurrentState?.Name))}.");
        await MainWindow.CaptureElementAsync(presenter, Path.Combine(folder, $"menu-{scheme.ToString().ToLowerInvariant()}.png"));
        Compact.Focus(FocusState.Keyboard);
        await MainWindow.AwaitPresentationAsync(presenter);
        Require(focusHighlight.Opacity == 0, "Menu focus highlight remained on the previous row.");
        Compact.IsChecked = true;
        Require(!Comfortable.IsChecked && Compact.IsChecked, "Native radio group retained multiple selections.");
        Comfortable.IsChecked = true;
        Require(!Compact.IsChecked, "Native radio group did not restore selection.");
        var expansion = (IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Submenu).GetPattern(PatternInterface.ExpandCollapse);
        expansion.Expand();
        await AwaitPresenterAsync(presenter);
        var childPresenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Select(popup => popup.Child).OfType<MenuFlyoutPresenter>().Single(candidate => candidate != presenter);
        Require(childPresenter.FontFamily.Source.Contains("EncodeSans-Regular.ttf") && childPresenter.Padding.Left == 6, "Submenu popup retained Windows appearance.");
        Require(childPresenter.MinWidth == 144 && childPresenter.ActualWidth == 144 && presenter.MinWidth == 176,
            $"Root/submenu geometry mismatch: root={presenter.MinWidth}, child={childPresenter.MinWidth}/{childPresenter.ActualWidth}.");
        VerifyShadow((BleeckerOverlaySurface)VisualTreeHelper.GetChild(childPresenter, 0));
        VerifyInset(NestedInsetAction);
        var nestedLabel = FrameworkElementAutomationPeer.CreatePeerForElement(childPresenter).GetChildren()
            .SingleOrDefault(peer => peer.GetName() == "More actions");
        Require(nestedLabel is not null && nestedLabel.GetAutomationControlType() == AutomationControlType.Text &&
            nestedLabel.GetPattern(PatternInterface.Invoke) is null, "Submenu omitted or made its label actionable.");
        await MainWindow.CaptureElementAsync(childPresenter, Path.Combine(folder, $"submenu-{scheme.ToString().ToLowerInvariant()}.png"));
        expansion.Collapse();
        await AwaitMenuStateAsync(() => !VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Any(popup => ReferenceEquals(popup.Child, childPresenter)));
        var checkPeer = new ToggleMenuFlyoutItemAutomationPeer(Checkbox);
        await InvokeAndWaitForCloseAsync(() => ((IToggleProvider)checkPeer.GetPattern(PatternInterface.Toggle)).Toggle());
        Require(!Checkbox.IsChecked, "Native checkbox menu action did not toggle.");
        Checkbox.IsChecked = true;
        if (!Menu.IsOpen) { Menu.ShowAt(Trigger); await AwaitPresenterAsync(); }
        var before = actions;
        await InvokeAndWaitForCloseAsync(() => ((IInvokeProvider)new MenuFlyoutItemAutomationPeer(Action).GetPattern(PatternInterface.Invoke)).Invoke());
        Require(actions == before + 1 && !Menu.IsOpen, "Native menu invocation did not execute and dismiss.");
        Menu.ShowAt(Trigger);
        await AwaitPresenterAsync();
        await InvokeAndWaitForCloseAsync(() => ((IInvokeProvider)new MenuFlyoutItemAutomationPeer(InsetAction).GetPattern(PatternInterface.Invoke)).Invoke());
        Require(actions == before + 2 && !Menu.IsOpen, "Inset menu invocation did not execute and dismiss.");
        Menu.ShowAt(Trigger);
        await AwaitPresenterAsync();
        await InvokeAndWaitForCloseAsync(() => ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(RichAction).GetPattern(PatternInterface.Invoke)).Invoke());
        Require(actions == before + 3 && !Menu.IsOpen, "Composed menu invocation did not execute and dismiss.");
        var originalStyle = Menu.MenuFlyoutPresenterStyle;
        var consumerStyle = new Style(typeof(MenuFlyoutPresenter)) { BasedOn = originalStyle };
        consumerStyle.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 220d));
        Menu.MenuFlyoutPresenterStyle = consumerStyle;
        Menu.ShowAt(Trigger);
        await AwaitPresenterAsync();
        presenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Select(popup => popup.Child).OfType<MenuFlyoutPresenter>().Single();
        Require(presenter.MinWidth == 220 && presenter.ActualWidth == 220, "Consumer presenter width was overridden.");
        await InvokeAndWaitForCloseAsync(Menu.Hide);
        await AwaitMenuStateAsync(() => !Menu.IsOpen && !VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Any(popup => ReferenceEquals(popup.Child, presenter)));
        Menu.MenuFlyoutPresenterStyle = originalStyle;
        Menu.ShowAt(Trigger);
        await AwaitPresenterAsync();
        presenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Select(popup => popup.Child).OfType<MenuFlyoutPresenter>().Single();
        Require(presenter.MinWidth == 176, "Restored default root menu lost its width.");
        await InvokeAndWaitForCloseAsync(Menu.Hide);
        await AwaitMenuStateAsync(() => !Menu.IsOpen && !VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Any(popup => ReferenceEquals(popup.Child, presenter)));
    }
    private async Task AwaitPresenterAsync(MenuFlyoutPresenter? parent = null)
    {
        MenuFlyoutPresenter? presenter = null;
        await AwaitMenuStateAsync(() => (presenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Select(popup => popup.Child).OfType<MenuFlyoutPresenter>().SingleOrDefault(candidate => candidate != parent)) is { IsLoaded: true, ActualWidth: > 0, ActualHeight: > 0 });
        await MainWindow.AwaitPresentationAsync(presenter!);
        Require(presenter!.IsLoaded && presenter.ActualWidth > 0 && presenter.ActualHeight > 0, "Menu lost its presented popup.");
    }
    private async Task AwaitMenuStateAsync(Func<bool> condition)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && watch.Elapsed < TimeSpan.FromSeconds(2)) { UpdateLayout(); if (condition()) break; await Task.Delay(16); }
        Require(condition(), "Menu popup did not complete its lifecycle transition.");
    }
    private void VerifyContentAndShortcuts()
    {
        Require(RichText.FontFamily.Source.Contains("EncodeSans-Regular.ttf") && RichText.FontSize == 14 && RichAction.ActualHeight == 32,
            "Composed menu content inherited Windows typography or row height.");
        var content = RichAction.Content;
        var replacement = new TextBlock { Text = "Editorial\nreview", LineHeight = 20, LineStackingStrategy = LineStackingStrategy.BlockLineHeight };
        RichAction.Content = replacement;
        RichAction.UpdateLayout();
        Require(RichAction.ActualHeight == 52 && replacement.FontSize == 14 && replacement.FontFamily.Source.Contains("EncodeSans-Regular.ttf"),
            "Composed menu did not update variable-height content.");
        RichAction.Content = content;
        RichAction.UpdateLayout();
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(RichAction);
        Require(peer.GetName() == "Edit profile" && peer.GetPattern(PatternInterface.Invoke) is not null,
            "Composed menu lost its native action name or invoke pattern.");
        foreach (var item in new MenuFlyoutItem[] { Action, InsetAction, RichAction, Checkbox, Compact })
        {
            var root = (Grid)VisualTreeHelper.GetChild(item, 0);
            var layout = (Grid)root.Children[1];
            var shortcut = layout.Children.OfType<TextBlock>().Single(text => text.Name == "KeyboardAcceleratorTextBlock");
            var hasShortcut = !string.IsNullOrEmpty(item.KeyboardAcceleratorTextOverride);
            Require((hasShortcut ? shortcut.Visibility == Visibility.Visible && shortcut.ActualWidth > 0 : shortcut.ActualWidth == 0) && shortcut.Margin == new Thickness(0),
                $"Menu shortcut layout mismatch: {item.Text}, hint={hasShortcut}, visibility={shortcut.Visibility}, width={shortcut.ActualWidth}, margin={shortcut.Margin}.");
        }
    }
    private static void VerifyInset(Control item)
    {
        var root = (Grid)VisualTreeHelper.GetChild(item, 0);
        var content = (Grid)root.Children[1];
        var text = content.Children.OfType<TextBlock>().Single(child => child.Name == "TextBlock");
        Require(item.Padding == new Thickness(32, 6, 8, 6) && item.ActualHeight == 32 &&
            text.TransformToVisual(item).TransformPoint(default).X == 32,
            $"Inset menu row differs from the reference: {item.Name}.");
        Require(item.FontSize == 14 && item.FontFamily.Source.Contains("EncodeSans-Regular.ttf") && item.CornerRadius.TopLeft == 7,
            "Inset menu row lost the base typography or corners.");
    }
    private void VerifyLabels(MenuFlyoutPresenter presenter)
    {
        var children = FrameworkElementAutomationPeer.CreatePeerForElement(presenter).GetChildren();
        foreach (var label in Menu.Items.OfType<BleeckerMenuLabel>())
        {
            var layout = (Grid)VisualTreeHelper.GetChild(label, 0);
            var text = (TextBlock)layout.Children[1];
            Require(label.ActualHeight == 27 && label.FontSize == 10 && label.CharacterSpacing == 90 &&
                label.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf") && text.LineHeight == 15,
                $"Menu label typography/geometry mismatch: {label.Text}, height={label.ActualHeight}.");
            Require(text.TransformToVisual(label).TransformPoint(default).X == (label.Inset ? 32 : 8), "Menu label inset mismatch.");
            Require(!label.IsHitTestVisible && !label.IsTabStop && !label.Focus(FocusState.Keyboard), "Menu section label became an action.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(label);
            Require(peer.IsControlElement() && peer.IsContentElement() && peer.GetAutomationControlType() == AutomationControlType.Text && peer.GetName() == label.Text &&
                peer.GetPattern(PatternInterface.Invoke) is null && peer.GetPattern(PatternInterface.Toggle) is null,
                "Menu section label lost its text-only accessibility contract.");
            var child = children.SingleOrDefault(candidate => candidate.GetName() == label.Text);
            Require(child is not null && child.GetAutomationControlType() == AutomationControlType.Text &&
                child.GetPattern(PatternInterface.Invoke) is null && child.GetPattern(PatternInterface.Toggle) is null,
                $"Presenter omitted the text-only label: {label.Text}; children={string.Join(',', children.Select(candidate => candidate.GetName()))}.");
        }
        var sectionLayout = (Grid)VisualTreeHelper.GetChild(SectionLabel, 0);
        var sectionText = (TextBlock)sectionLayout.Children[1];
        var originalLanguage = SectionLabel.Language;
        SectionLabel.Language = "tr-TR";
        SectionLabel.Text = "içerik";
        Require(sectionText.Text == "İÇERİK", "Menu label ignored live text/language changes.");
        var paddingStyle = new Style(typeof(BleeckerMenuLabel));
        paddingStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 6, 8, 6)));
        SectionLabel.Style = paddingStyle;
        SectionLabel.Inset = true;
        SectionLabel.UpdateLayout();
        Require(SectionLabel.Padding.Left == 12 && sectionText.TransformToVisual(SectionLabel).TransformPoint(default).X == 36,
            "Inset replaced consumer label padding.");
        SectionLabel.Style = null;
        SectionLabel.Inset = false;
        SectionLabel.Language = originalLanguage;
        SectionLabel.Text = "Account";
        SectionLabel.UpdateLayout();
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
            await MainWindow.AwaitPresentationAsync(this);
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
