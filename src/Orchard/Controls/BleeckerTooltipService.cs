using Microsoft.UI.Xaml;

namespace Orchard;

/// <summary>Attaches an Orchard tooltip to its explicit input and placement owner.</summary>
public static class BleeckerTooltipService
{
    public static BleeckerTooltip? GetToolTip(DependencyObject owner) => (BleeckerTooltip?)owner.GetValue(ToolTipProperty);
    public static void SetToolTip(DependencyObject owner, BleeckerTooltip? value) => owner.SetValue(ToolTipProperty, value);
    public static readonly DependencyProperty ToolTipProperty = DependencyProperty.RegisterAttached("ToolTip", typeof(BleeckerTooltip), typeof(BleeckerTooltipService), new PropertyMetadata(null, (owner, args) =>
    {
        if (owner is not FrameworkElement target) throw new ArgumentException("Tooltip owner must be a FrameworkElement.");
        if (args.OldValue is BleeckerTooltip old) old.PlacementTarget = null;
        if (args.NewValue is BleeckerTooltip tip)
        {
            if (tip.PlacementTarget is not null && !ReferenceEquals(tip.PlacementTarget, target)) throw new InvalidOperationException("A tooltip cannot have multiple owners.");
            tip.PlacementTarget = target;
        }
    }));
}
