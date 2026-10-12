using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard;

/// <summary>A one-pixel Bleecker divider, decorative unless explicitly semantic.</summary>
public sealed class BleeckerSeparator : Control
{
    public BleeckerSeparator() => DefaultStyleKey = typeof(BleeckerSeparator);

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(BleeckerSeparator),
        new PropertyMetadata(Orientation.Horizontal, (owner, _) => ((BleeckerSeparator)owner).OnOrientationChanged()));
    public static readonly DependencyProperty DecorativeProperty = DependencyProperty.Register(
        nameof(Decorative), typeof(bool), typeof(BleeckerSeparator),
        new PropertyMetadata(true, (owner, _) => FrameworkElementAutomationPeer.FromElement((BleeckerSeparator)owner)?.InvalidatePeer()));

    public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public bool Decorative { get => (bool)GetValue(DecorativeProperty); set => SetValue(DecorativeProperty, value); }

    private void OnOrientationChanged()
    {
        if (!Enum.IsDefined(Orientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
        InvalidateMeasure();
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        base.MeasureOverride(availableSize);
        return Orientation == Orientation.Horizontal
            ? new Size(double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width, 1)
            : new Size(1, double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(Orientation == Orientation.Horizontal
        ? new Size(finalSize.Width, double.IsNaN(Height) ? 1 : finalSize.Height)
        : new Size(double.IsNaN(Width) ? 1 : finalSize.Width, finalSize.Height));

    protected override AutomationPeer OnCreateAutomationPeer() => new SeparatorPeer(this);
    private sealed class SeparatorPeer(BleeckerSeparator owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerSeparator);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Separator;
        protected override AutomationOrientation GetOrientationCore() => owner.Orientation == Orientation.Horizontal
            ? AutomationOrientation.Horizontal : AutomationOrientation.Vertical;
        protected override bool IsControlElementCore() => !owner.Decorative;
        protected override bool IsContentElementCore() => !owner.Decorative;
    }
}
