using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;

namespace Orchard;

/// <summary>Vertically stacks fixed panels and shares the remaining height between growing panels.</summary>
[ContentProperty(Name = nameof(Children))]
public sealed partial class BleeckerPanelColumn : Control
{
    private readonly BleeckerPanelFlowHost host;
    public BleeckerPanelColumn()
    {
        DefaultStyleKey = typeof(BleeckerPanelColumn); IsTabStop = false; UseLayoutRounding = false;
        host = new(this, Children, horizontal: false);
    }
    public ObservableCollection<UIElement> Children { get; } = [];
    public static readonly DependencyProperty GrowProperty = DependencyProperty.Register(nameof(Grow), typeof(bool), typeof(BleeckerPanelColumn), new PropertyMetadata(false));
    /// <summary>When this column is inside a PanelLayout, share its remaining horizontal space.</summary>
    public bool Grow { get => (bool)GetValue(GrowProperty); set => SetValue(GrowProperty, value); }
    protected override void OnApplyTemplate() { host.Detach(); base.OnApplyTemplate(); host.Attach(GetTemplateChild("PART_Root") as Grid); }
    protected override Size MeasureOverride(Size availableSize) { host.Constrain(availableSize); return base.MeasureOverride(availableSize); }
    protected override AutomationPeer OnCreateAutomationPeer() => new ColumnPeer(this);
    private sealed class ColumnPeer(BleeckerPanelColumn owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerPanelColumn);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }
}
