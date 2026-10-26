using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

internal static class BleeckerAutomationMetadata
{
    internal static bool HasSuppliedValue(FrameworkElement owner, DependencyProperty property)
    {
        if (owner.ReadLocalValue(property) != DependencyProperty.UnsetValue) return true;
        static bool Sets(Style? style, DependencyProperty property)
        {
            for (; style is not null; style = style.BasedOn)
                if (style.Setters.OfType<Setter>().Any(setter => setter.Property == property)) return true;
            return false;
        }
        if (owner.Style is { } explicitStyle) return Sets(explicitStyle, property);
        var type = owner.GetType();
        for (DependencyObject? scope = owner; scope is not null; scope = VisualTreeHelper.GetParent(scope))
            if (scope is FrameworkElement element && element.Resources.TryGetValue(type, out var resource) && resource is Style implicitStyle)
                return Sets(implicitStyle, property);
        return Application.Current.Resources.TryGetValue(type, out var applicationStyle) && applicationStyle is Style style && Sets(style, property);
    }

    internal static bool TryGetName(FrameworkElement owner, AutomationPeer peer, out string name)
    {
        name = AutomationProperties.GetName(owner);
        if (HasSuppliedValue(owner, AutomationProperties.NameProperty) || !string.IsNullOrEmpty(name)) return true;
        if (AutomationProperties.GetLabeledBy(owner) is null) return false;
        name = peer.GetLabeledBy()?.GetName() ?? "";
        return true;
    }
}
