using Microsoft.UI.Xaml;

namespace Orchard;

/// <summary>A caller-owned destination. Derive to retain additional application metadata.</summary>
[Microsoft.UI.Xaml.Data.Bindable]
public class BleeckerNavItem : DependencyObject
{
    public BleeckerNavItem() { }
    public BleeckerNavItem(string href, string label, bool external = false) { Href = href; Label = label; External = external; }
    internal event EventHandler? Changed;
    private static void Change(DependencyObject owner, DependencyPropertyChangedEventArgs _) => ((BleeckerNavItem)owner).Changed?.Invoke(owner, EventArgs.Empty);
    public static readonly DependencyProperty HrefProperty = DependencyProperty.Register(nameof(Href), typeof(string), typeof(BleeckerNavItem), new PropertyMetadata("", Change));
    public string Href { get => (string)GetValue(HrefProperty); set => SetValue(HrefProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(BleeckerNavItem), new PropertyMetadata("", Change));
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public static readonly DependencyProperty ExternalProperty = DependencyProperty.Register(nameof(External), typeof(bool), typeof(BleeckerNavItem), new PropertyMetadata(false, Change));
    public bool External { get => (bool)GetValue(ExternalProperty); set => SetValue(ExternalProperty, value); }
}
