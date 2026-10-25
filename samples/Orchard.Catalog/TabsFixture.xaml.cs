using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Orchard.Catalog;

public sealed partial class TabsFixture : UserControl
{
    internal static ObservableCollection<BleeckerTabItem> Items() =>
        [new("overview", "Overview", panelId: "overview-panel"), new("activity", "Activity", panelId: "activity-panel"), new("settings", "Settings")];
    public TabsFixture()
    {
        InitializeComponent();
        foreach (var tabs in new[] { Controlled, Declined, Disabled }) tabs.Items = Items();
        Controlled.ActiveTabRequested += (_, args) => { Controlled.ActiveTab = args.Id; Panel.Text = args.Id + " panel"; };
        foreach (var variant in Enum.GetValues<BleeckerTabsVariant>())
            foreach (var size in Enum.GetValues<BleeckerControlSize>())
            {
                var tabs = new BleeckerTabs { Items = Items(), ActiveTab = "activity", Variant = variant, Size = size, Stretch = true };
                AutomationProperties.SetName(tabs, $"{variant} {size}");
                tabs.ActiveTabRequested += (_, args) => tabs.ActiveTab = args.Id;
                Variants.Children.Add(tabs);
            }
        foreach (var tabs in new[] { Navigation, CustomLinks })
        {
            tabs.Items = [new("overview", "Overview", "/demo/overview"), new("reference", "Reference", "https://example.invalid/reference", external: true)];
            tabs.NavigationRequested += (_, args) => NavigationStatus.Text = $"{args.Item.Label}: {args.Item.Href}; external={args.Item.External}";
        }
    }

    internal async Task VerifyAsync(ElementTheme theme, string folder)
    {
        var requests = new List<string>();
        var accept = false;
        void Requested(object? sender, BleeckerTabRequestedEventArgs args) { requests.Add(args.Id); if (accept) Declined.ActiveTab = args.Id; }
        Declined.ActiveTabRequested += Requested;
        try
        {
            await MainWindow.AwaitPresentationAsync(this);
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(Declined);
            Require(peer.GetAutomationControlType() == AutomationControlType.Tab, "Panel tabs lost the Tab role.");
            var selection = (ISelectionProvider)peer.GetPattern(PatternInterface.Selection);
            Require(!selection.CanSelectMultiple && selection.GetSelection().Length == 1, "Tabs selection provider differs.");
            var first = Declined.TabButtons[0];
            var second = Declined.TabButtons[1];
            var secondPeer = FrameworkElementAutomationPeer.CreatePeerForElement(second);
            var secondSelection = (ISelectionItemProvider)secondPeer.GetPattern(PatternInterface.SelectionItem);
            secondSelection.Select();
            Require(Declined.ActiveTab == "overview" && requests.SequenceEqual(new[] { "activity" }) && !secondSelection.IsSelected, "A declined request mutated consumer selection.");
            accept = true;
            secondSelection.Select();
            Require(Declined.ActiveTab == "activity" && secondSelection.IsSelected && requests.Count == 2, "An accepted request did not reach the native selection.");
            Declined.Items.Move(1, 0);
            Require(ReferenceEquals(second, Declined.TabButtons[0]) && Declined.ActiveTab == "activity", "Reordering replaced the selected item or value.");
            Declined.Items[0] = new("activity", "Updated activity");
            Require(ReferenceEquals(second, Declined.TabButtons[0]) && secondPeer.GetName() == "Updated activity", "Replacing a label lost keyed identity or its accessible name.");
            Declined.Items.RemoveAt(0);
            await MainWindow.AwaitPresentationAsync(Declined);
            Require(Declined.ActiveTab == "activity" && selection.GetSelection().Length == 0, "Removing a selected tab invented a replacement selection.");
            var staleRejected = false;
            try { secondSelection.Select(); } catch (ElementNotAvailableException) { staleRejected = true; }
            Require(staleRejected, "A removed tab remained actionable.");
            Declined.Items.Insert(0, new("activity", "Activity"));
            await MainWindow.AwaitPresentationAsync(Declined);
            Require(selection.GetSelection().Length == 1, "Reinserting the consumer's active identifier lost selection.");
            var disabled = (ISelectionItemProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Disabled.TabButtons[0]).GetPattern(PatternInterface.SelectionItem);
            var disabledRejected = false;
            try { disabled.Select(); } catch (ElementNotEnabledException) { disabledRejected = true; }
            Require(disabledRejected, "Disabled tabs accepted an automation request.");
            var navigationPeer = FrameworkElementAutomationPeer.CreatePeerForElement(Navigation);
            Require(navigationPeer.GetPattern(PatternInterface.Selection) is null && navigationPeer.GetAutomationControlType() == AutomationControlType.Group, "Navigation links acquired panel-selection semantics.");
            var linkPeer = FrameworkElementAutomationPeer.CreatePeerForElement(Navigation.TabButtons[1]);
            Require(linkPeer.GetAutomationControlType() == AutomationControlType.Hyperlink && linkPeer.GetPattern(PatternInterface.SelectionItem) is null, "A destination lost hyperlink semantics.");
            BleeckerTabItem? destination = null;
            void Navigate(object? sender, BleeckerTabNavigationEventArgs args) => destination = args.Item;
            Navigation.NavigationRequested += Navigate;
            try { ((IInvokeProvider)linkPeer.GetPattern(PatternInterface.Invoke)).Invoke(); }
            finally { Navigation.NavigationRequested -= Navigate; }
            Require(destination?.Href == "https://example.invalid/reference" && destination.External && Navigation.ActiveTab == "overview", "Navigation changed the destination or active value.");
            var probe = new TabCommand();
            var parameter = new object();
            var originalLink = Navigation.Items[0];
            Navigation.Items[0] = new("overview", "Overview", originalLink.Href, command: probe, commandParameter: parameter);
            var commandPeer = (IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Navigation.TabButtons[0]).GetPattern(PatternInterface.Invoke);
            try
            {
                commandPeer.Invoke();
                Require(probe.Executions == 1 && ReferenceEquals(probe.Parameter, parameter), "Navigation lost its original command parameter.");
                probe.SetEnabled(false, notify: false);
                var commandRejected = false;
                try { commandPeer.Invoke(); } catch (ElementNotEnabledException) { commandRejected = true; }
                Require(commandRejected && probe.Executions == 1, "Activation did not reevaluate CanExecute at the boundary.");
                probe.SetEnabled(false, notify: true);
                Require(!Navigation.TabButtons[0].IsEnabled, "CanExecuteChanged did not disable the native link.");
                probe.SetEnabled(true, notify: true);
                Require(Navigation.TabButtons[0].IsEnabled, "CanExecuteChanged did not reenable the native link.");
            }
            finally { Navigation.Items[0] = originalLink; }
            var custom = MainWindow.Desc<Button>(CustomLinks).First();
            var context = (BleeckerTabLinkContext)custom.DataContext;
            var customRequests = 0;
            void CustomNavigation(object? sender, BleeckerTabNavigationEventArgs args) => customRequests++;
            CustomLinks.NavigationRequested += CustomNavigation;
            try
            {
                Require(context.Command.CanExecute(null), "A mounted custom link command was disabled.");
                context.Command.Execute(null);
                Require(customRequests == 1, "A custom renderer did not route exactly one navigation request.");
                var original = CustomLinks.Items[0];
                CustomLinks.Items.RemoveAt(0);
                await MainWindow.AwaitPresentationAsync(CustomLinks);
                context.Command.Execute(null);
                Require(!context.Command.CanExecute(null) && customRequests == 1, "A removed custom link retained an actionable command.");
                CustomLinks.Items.Insert(0, original);
            }
            finally { CustomLinks.NavigationRequested -= CustomNavigation; }
            var template = Declined.Template;
            Declined.Template = null; Declined.ApplyTemplate(); Declined.Template = template; Declined.ApplyTemplate();
            await MainWindow.AwaitPresentationAsync(Declined);
            Require(Declined.ActiveTab == "activity" && Declined.TabButtons.Count == 3, "Template replacement lost consumer state.");
            Declined.Items = Items(); Declined.ActiveTab = "overview";
            foreach (var tabs in Variants.Children.OfType<BleeckerTabs>())
            {
                Require(tabs.TabButtons.Count == 3 && tabs.ActualHeight >= (tabs.Size == BleeckerControlSize.Small ? 32 : tabs.Size == BleeckerControlSize.Medium ? 40 : 48), "A variant/size did not render its controls.");
                var widths = tabs.TabButtons.Select(button => button.ActualWidth).ToArray();
                var inset = tabs.Variant == BleeckerTabsVariant.Underline ? 0 : tabs.Variant == BleeckerTabsVariant.Segmented ? 8 : 10;
                Require(widths.Max() - widths.Min() <= 1d / 64 && Math.Abs(widths.Sum() + 8 + inset - tabs.ActualWidth) < .001,
                    "Stretched tabs did not fill equal rendered tracks.");
            }
            await VerifyInputAsync(theme, folder);
            await File.WriteAllTextAsync(Path.Combine(folder, $"tabs-runtime-{theme}.json"), JsonSerializer.Serialize(new
            { passed = true, theme = theme.ToString(), variants = 9, consumerControlled = true, staleRejected, disabledRejected, keyedIdentity = true, navigationMetadataPreserved = true, templateReplacement = true, requests }));
        }
        finally { Declined.ActiveTabRequested -= Requested; Declined.Items = Items(); Declined.ActiveTab = "overview"; }
    }

    private async Task VerifyInputAsync(ElementTheme theme, string folder)
    {
        var root = (Grid)XamlRoot.Content;
        var host = new StackPanel { Width = 520, Margin = new Thickness(24), Spacing = 12, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count);
        var before = new Button { Content = "Before tabs" };
        var tabs = new BleeckerTabs { Items = Items(), ActiveTab = "overview", Stretch = true };
        var after = new Button { Content = "After tabs" };
        host.Children.Add(before); host.Children.Add(tabs); host.Children.Add(after); root.Children.Add(host);
        using var input = new NativeInputSession(XamlRoot, folder, "tabs-" + theme);
        var requests = new List<string>(); var accept = true;
        tabs.ActiveTabRequested += (_, args) => { requests.Add(args.Id); if (accept) tabs.ActiveTab = args.Id; };
        try
        {
            await MainWindow.AwaitPresentationAsync(host); await input.AcquireAsync(before);
            await input.MoveAsync(after); await input.MoveAsync(before);
            Require(before.Focus(FocusState.Keyboard), "The predecessor refused keyboard focus.");
            await input.HeldKeyAsync(0x09, before, () => tabs.TabButtons[0], () => MainWindow.AwaitPresentationAsync(tabs));
            Require(ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), tabs.TabButtons[0]), "Tab did not enter the selected tab.");
            await input.HeldKeyAsync(0x27, tabs.TabButtons[0], () => tabs.TabButtons[1], () => MainWindow.AwaitPresentationAsync(tabs));
            Require(tabs.ActiveTab == "activity" && requests.SequenceEqual(new[] { "activity" }), "Right arrow did not request the next tab.");
            await input.HeldKeyAsync(0x23, tabs.TabButtons[1], () => tabs.TabButtons[2], () => MainWindow.AwaitPresentationAsync(tabs));
            Require(tabs.ActiveTab == "settings", "End did not request the last tab.");
            await input.HeldKeyAsync(0x24, tabs.TabButtons[2], () => tabs.TabButtons[0], () => MainWindow.AwaitPresentationAsync(tabs));
            Require(tabs.ActiveTab == "overview", "Home did not request the first tab.");
            await input.HeldKeyAsync(0x23, tabs.TabButtons[0], () => tabs.TabButtons[2], () => MainWindow.AwaitPresentationAsync(tabs));
            await input.HeldKeyAsync(0x27, tabs.TabButtons[2], () => tabs.TabButtons[0], () => MainWindow.AwaitPresentationAsync(tabs));
            Require(tabs.ActiveTab == "overview", "Right arrow did not wrap.");
            accept = false;
            await input.HeldKeyAsync(0x25, tabs.TabButtons[0], () => tabs.TabButtons[2], () => MainWindow.AwaitPresentationAsync(tabs));
            Require(tabs.ActiveTab == "overview" && requests[^1] == "settings", "A declined arrow request changed selection.");
            accept = true;
            await input.KeyAsync(0x20, tabs.TabButtons[2]);
            Require(tabs.ActiveTab == "settings", "Space did not activate the focused tab.");
            await input.HeldKeyAsync(0x09, tabs.TabButtons[2], () => after, () => MainWindow.AwaitPresentationAsync(tabs));
            await input.HeldKeyAsync(0x09, after, () => tabs.TabButtons[2], () => MainWindow.AwaitPresentationAsync(tabs), 0x10);
            tabs.Items[1] = new("activity", "Unavailable", disabled: true);
            tabs.ActiveTab = "overview"; tabs.TabButtons[0].Focus(FocusState.Keyboard);
            await input.HeldKeyAsync(0x27, tabs.TabButtons[0], () => tabs.TabButtons[2], () => MainWindow.AwaitPresentationAsync(tabs));
            Require(tabs.ActiveTab == "settings", "Keyboard navigation did not skip a disabled item.");
            tabs.FlowDirection = FlowDirection.RightToLeft;
            await MainWindow.AwaitPresentationAsync(tabs);
            await input.HeldKeyAsync(0x27, tabs.TabButtons[2], () => tabs.TabButtons[0], () => MainWindow.AwaitPresentationAsync(tabs));
            Require(tabs.ActiveTab == "overview", "RTL horizontal navigation differs from Windows conventions.");
            tabs.FlowDirection = FlowDirection.LeftToRight;
            tabs.Items[1] = new("activity", "Activity");
            await MainWindow.AwaitPresentationAsync(tabs);
            await input.HeldPointerAsync(tabs.TabButtons[1], async () =>
            {
                await MainWindow.AwaitPresentationAsync(tabs);
                var surface = MainWindow.Desc<Grid>(tabs.TabButtons[1]).Single(value => value.Name == "TabSurfaceRoot");
                Require(surface.RenderTransform is Microsoft.UI.Xaml.Media.TranslateTransform { Y: 1 }, "Held pointer did not apply the source's immediate one-pixel displacement.");
            });
            await MainWindow.AwaitPresentationAsync(tabs);
            Require(MainWindow.Desc<Grid>(tabs.TabButtons[1]).Single(value => value.Name == "TabSurfaceRoot").RenderTransform is Microsoft.UI.Xaml.Media.TranslateTransform { Y: 0 }, "Releasing the pointer retained the pressed displacement.");
            Require(tabs.ActiveTab == "activity", "Pointer selection did not reach the consumer.");
            await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"tabs-input-{theme}.png"));
            await File.WriteAllTextAsync(Path.Combine(folder, $"tabs-input-{theme}.json"), JsonSerializer.Serialize(new { passed = true, requests, arrowWrap = true, homeEnd = true, declinedRequests = true, space = true, tabTraversal = true, disabledSkip = true, rtl = true, pointer = true, immediatePressDisplacement = true }));
        }
        finally { root.Children.Remove(host); }
    }
    private sealed class TabCommand : ICommand
    {
        private bool enabled = true;
        internal int Executions { get; private set; }
        internal object? Parameter { get; private set; }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => enabled;
        public void Execute(object? parameter) { Executions++; Parameter = parameter; }
        internal void SetEnabled(bool value, bool notify) { enabled = value; if (notify) CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
