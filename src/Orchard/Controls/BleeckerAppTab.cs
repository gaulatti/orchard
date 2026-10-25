using Microsoft.UI.Xaml.Controls;

namespace Orchard;

/// <summary>A caller-owned destination with stable value equality and a native Windows font icon.</summary>
public sealed record BleeckerAppTab
{
    public BleeckerAppTab(object? value, string label, FontIconSource icon, long? badge = null, string? accessibilityLabel = null, bool isDisabled = false)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(icon);
        Value = value; Label = label; Icon = icon; Badge = badge;
        AccessibilityLabel = accessibilityLabel ?? label; IsDisabled = isDisabled;
    }
    public object? Value { get; }
    public string Label { get; }
    public FontIconSource Icon { get; }
    public long? Badge { get; }
    public string AccessibilityLabel { get; }
    public bool IsDisabled { get; }
}
