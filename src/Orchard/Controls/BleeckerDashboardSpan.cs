using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

public enum BleeckerDashboardSpanSize {One=1,Two=2,Three=3,Four=4,Full=-1}

/// <summary>A consumer content wrapper with responsive dashboard column span.</summary>
public sealed class BleeckerDashboardSpan:ContentControl
{
    public BleeckerDashboardSpan()=>DefaultStyleKey=typeof(BleeckerDashboardSpan);
    public static readonly DependencyProperty SpanProperty=DependencyProperty.Register(nameof(Span),typeof(BleeckerDashboardSpanSize),typeof(BleeckerDashboardSpan),new PropertyMetadata(BleeckerDashboardSpanSize.One,
        (owner,args)=>{Validate((BleeckerDashboardSpanSize)args.NewValue);((BleeckerDashboardSpan)owner).InvalidateMeasure();if(VisualTreeHelper.GetParent(owner) is BleeckerDashboardGrid grid)grid.InvalidateMeasure();}));
    public BleeckerDashboardSpanSize Span {get=>(BleeckerDashboardSpanSize)GetValue(SpanProperty);set{Validate(value);SetValue(SpanProperty,value);}}
    internal static void Validate(BleeckerDashboardSpanSize value){if(value is not(BleeckerDashboardSpanSize.One or BleeckerDashboardSpanSize.Two or BleeckerDashboardSpanSize.Three or BleeckerDashboardSpanSize.Four or BleeckerDashboardSpanSize.Full))throw new ArgumentOutOfRangeException(nameof(Span),"Span must be 1–4 or Full.");}
    protected override AutomationPeer OnCreateAutomationPeer()=>new SpanPeer(this);
    private sealed class SpanPeer(BleeckerDashboardSpan owner):FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore()=>nameof(BleeckerDashboardSpan);
        protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Group;
    }
}
