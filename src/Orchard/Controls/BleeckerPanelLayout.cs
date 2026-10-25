using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;

namespace Orchard;

/// <summary>A single horizontal row of fixed and equally growing caller-owned panels.</summary>
[ContentProperty(Name = nameof(Children))]
public sealed partial class BleeckerPanelLayout : Control
{
    private readonly BleeckerPanelFlowHost host;
    public BleeckerPanelLayout()
    {
        DefaultStyleKey = typeof(BleeckerPanelLayout); IsTabStop = false; UseLayoutRounding = false;
        host = new(this, Children, horizontal: true);
    }
    public ObservableCollection<UIElement> Children { get; } = [];
    /// <summary>Growth metadata for caller controls other than BleeckerPanel and BleeckerPanelColumn.</summary>
    public static readonly DependencyProperty GrowProperty = DependencyProperty.RegisterAttached("Grow", typeof(bool), typeof(BleeckerPanelLayout), new PropertyMetadata(false));
    public static bool GetGrow(DependencyObject element) => element switch
    {
        BleeckerPanel panel => panel.Grow,
        BleeckerPanelColumn column => column.Grow,
        _ => (bool)element.GetValue(GrowProperty)
    };
    public static void SetGrow(DependencyObject element, bool value)
    {
        if (element is BleeckerPanel panel) panel.Grow = value;
        else if (element is BleeckerPanelColumn column) column.Grow = value;
        else element.SetValue(GrowProperty, value);
    }
    protected override void OnApplyTemplate() { host.Detach(); base.OnApplyTemplate(); host.Attach(GetTemplateChild("PART_Root") as Grid); }
    protected override Size MeasureOverride(Size availableSize) { host.Constrain(availableSize); return base.MeasureOverride(availableSize); }
    protected override AutomationPeer OnCreateAutomationPeer() => new LayoutPeer(this);
    private sealed class LayoutPeer(BleeckerPanelLayout owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerPanelLayout);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }
}
