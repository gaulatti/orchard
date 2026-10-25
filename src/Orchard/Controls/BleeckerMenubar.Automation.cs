using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Orchard;

public sealed partial class BleeckerMenubar
{
    private sealed class MenuItemsPanel : StackPanel
    {
        internal MenuItemsPanel(MenuLevel level) { Level = level; Padding = new Thickness(0, 4, 0, 4); UseLayoutRounding = false; }
        internal MenuLevel Level { get; }
        protected override AutomationPeer OnCreateAutomationPeer() => new MenuPeer(this);
        private sealed class MenuPeer(MenuItemsPanel panel) : FrameworkElementAutomationPeer(panel)
        {
            protected override string GetClassNameCore() => nameof(BleeckerMenubarMenu);
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Menu;
            protected override string GetNameCore() => string.IsNullOrEmpty(AutomationProperties.GetName(panel.Level.Surface)) ? panel.Level.Anchor.Label : AutomationProperties.GetName(panel.Level.Surface);
            protected override bool IsControlElementCore() => panel.Level.AutomationEligible;
            protected override bool IsContentElementCore() => panel.Level.AutomationEligible;
            protected override bool IsEnabledCore() => panel.Level.Surface.IsEnabled && panel.Level.AutomationEligible;
            protected override IList<AutomationPeer> GetChildrenCore() => panel.Level.AutomationEligible
                ? panel.Children.OfType<FrameworkElement>().Where(element => element.Visibility == Visibility.Visible && element is MenubarButton or RadioItemsPanel).Select(CreatePeerForElement).OfType<AutomationPeer>().ToArray()
                : [];
        }
    }

    private sealed class RadioItemsPanel : StackPanel
    {
        internal RadioItemsPanel(MenuLevel level) { Level = level; UseLayoutRounding = false; }
        internal MenuLevel Level { get; }
        internal IEnumerable<MenubarButton> Buttons => Children.OfType<MenubarButton>();
        internal bool AutomationEligible => Level.AutomationEligible && Parent is MenuItemsPanel;
        protected override AutomationPeer OnCreateAutomationPeer() => new GroupPeer(this);
        internal void Changed()
        {
            FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
        }
        private sealed class GroupPeer(RadioItemsPanel panel) : FrameworkElementAutomationPeer(panel), ISelectionProvider
        {
            protected override string GetClassNameCore() => nameof(BleeckerMenubarRadioGroup);
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
            protected override bool IsControlElementCore() => panel.AutomationEligible;
            protected override bool IsContentElementCore() => panel.AutomationEligible;
            protected override bool IsEnabledCore() => panel.Level.Surface.IsEnabled && panel.AutomationEligible;
            protected override object? GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.Selection ? this : base.GetPatternCore(pattern);
            protected override IList<AutomationPeer> GetChildrenCore() => panel.AutomationEligible ? panel.Buttons.Where(button => button.Visibility == Visibility.Visible).Select(CreatePeerForElement).OfType<AutomationPeer>().ToArray() : [];
            public bool CanSelectMultiple => false;
            public bool IsSelectionRequired => false;
            public IRawElementProviderSimple[] GetSelection() => panel.AutomationEligible ? panel.Buttons.Where(button => button.IsChecked).Select(button => ProviderFromPeer(CreatePeerForElement(button))).ToArray() : [];
        }
    }

    private sealed partial class MenuLevel
    {
        internal bool AutomationEligible => IsOpen && owner.InputAllowed;
        internal AutomationPeer? AutomationPeer => AutomationEligible ? FrameworkElementAutomationPeer.CreatePeerForElement(items) : null;
        private void NotifyMenu(AutomationEvents change)
        {
            anchor.InvalidateMetadata();
            foreach (var group in radioPanels.Values) group.Changed();
            var peer = FrameworkElementAutomationPeer.FromElement(items);
            if (peer is null && Microsoft.UI.Xaml.Automation.Peers.AutomationPeer.ListenerExists(change)) peer = FrameworkElementAutomationPeer.CreatePeerForElement(items);
            peer?.InvalidatePeer();
            if (peer is not null && Microsoft.UI.Xaml.Automation.Peers.AutomationPeer.ListenerExists(change)) peer.RaiseAutomationEvent(change);
        }
        private static void Place(Panel panel, IReadOnlyList<UIElement> desired)
        {
            for (var i = 0; i < desired.Count; i++)
            {
                var element = desired[i];
                if (element is FrameworkElement { Parent: Panel old } && !ReferenceEquals(old, panel)) old.Children.Remove(element);
                var current = panel.Children.IndexOf(element);
                if (current != i) { if (current >= 0) panel.Children.RemoveAt(current); panel.Children.Insert(i, element); }
            }
            while (panel.Children.Count > desired.Count) panel.Children.RemoveAt(panel.Children.Count - 1);
        }
    }
}
