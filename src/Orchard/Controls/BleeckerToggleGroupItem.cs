using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Orchard;

public sealed class BleeckerToggleGroupItem : ToggleButton
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(string), typeof(BleeckerToggleGroupItem),
        new PropertyMetadata("", (owner, _) => ((BleeckerToggleGroupItem)owner).Group?.Synchronize()));
    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    internal BleeckerToggleGroup? Group { get; set; }
    internal Style? AssignedGroupStyle { get; set; }

    protected override AutomationPeer OnCreateAutomationPeer() => new ItemPeer(this);

    private sealed class ItemPeer(BleeckerToggleGroupItem item) : ToggleButtonAutomationPeer(item), ISelectionItemProvider
    {
        public bool IsSelected => item.IsChecked == true;
        public IRawElementProviderSimple SelectionContainer => ProviderFromPeer(CreatePeerForElement(OwnerGroup));
        private BleeckerToggleGroup OwnerGroup => item.Group ?? throw new InvalidOperationException("The toggle item is not attached to a group.");
        public void AddToSelection()
        {
            EnsureEnabled();
            if (OwnerGroup.SelectionMode == BleeckerSelectionMode.Single && !string.IsNullOrEmpty(OwnerGroup.SelectedValue) && OwnerGroup.SelectedValue != item.Value)
                throw new InvalidOperationException("Single-selection groups cannot add a second selected item. Use Select.");
            OwnerGroup.SetSelected(item, true, false);
        }
        public void RemoveFromSelection() { EnsureEnabled(); OwnerGroup.SetSelected(item, false, false); }
        public void Select() { EnsureEnabled(); OwnerGroup.SetSelected(item, true, true); }
        private void EnsureEnabled()
        {
            if (!item.IsEnabled) throw new ElementNotEnabledException();
        }
        protected override object GetPatternCore(PatternInterface patternInterface) => patternInterface == PatternInterface.SelectionItem ? this : base.GetPatternCore(patternInterface);
    }
}
