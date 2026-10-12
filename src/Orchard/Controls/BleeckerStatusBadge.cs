using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

public enum BleeckerStatusBadgeVariant { Default, Live, Offline, Warning, Info }

/// <summary>A consumer-owned status label with an optional description.</summary>
public sealed class BleeckerStatusBadge : Control
{
    private ResourceDictionary? palette;
    private TextBlock? description;
    public BleeckerStatusBadge() { DefaultStyleKey = typeof(BleeckerStatusBadge); ApplyVariant(); }
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(BleeckerStatusBadge), new PropertyMetadata("", OnTextChanged));
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(BleeckerStatusBadge), new PropertyMetadata("", OnTextChanged));
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(nameof(Variant), typeof(BleeckerStatusBadgeVariant), typeof(BleeckerStatusBadge), new PropertyMetadata(BleeckerStatusBadgeVariant.Default, (owner, _) => ((BleeckerStatusBadge)owner).ApplyVariant()));
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string Description { get => (string)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public BleeckerStatusBadgeVariant Variant { get => (BleeckerStatusBadgeVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    private static void OnTextChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        var badge = (BleeckerStatusBadge)owner;
        badge.UpdateDescription();
        FrameworkElementAutomationPeer.FromElement(badge)?.InvalidatePeer();
    }
    protected override void OnApplyTemplate() { base.OnApplyTemplate(); description = GetTemplateChild("PART_Description") as TextBlock; UpdateDescription(); }
    private void UpdateDescription() { if (description is not null) description.Visibility = string.IsNullOrEmpty(Description) ? Visibility.Collapsed : Visibility.Visible; }
    private void ApplyVariant()
    {
        if (!Enum.IsDefined(Variant)) throw new ArgumentOutOfRangeException(nameof(Variant));
        var next = new ResourceDictionary { Source = new Uri($"ms-appx:///Orchard/Themes/StatusBadges/{Variant}.g.xaml") };
        if (palette is null) { palette = next; Resources.MergedDictionaries.Add(palette); return; }
        foreach (var theme in new[] { "Light", "Default" })
        foreach (var key in new[] { "StatusBadgeBackground", "StatusBadgeBorder", "StatusBadgeForeground", "StatusBadgeDot" })
        {
            var current = (SolidColorBrush)((ResourceDictionary)palette.ThemeDictionaries[theme])[key];
            var replacement = (SolidColorBrush)((ResourceDictionary)next.ThemeDictionaries[theme])[key];
            current.Color = replacement.Color; current.Opacity = replacement.Opacity;
        }
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new BadgePeer(this);
    private sealed class BadgePeer(BleeckerStatusBadge owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerStatusBadge);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
        protected override string GetNameCore() => string.IsNullOrEmpty(base.GetNameCore()) ? owner.Label ?? "" : base.GetNameCore();
        protected override string GetHelpTextCore() => string.IsNullOrEmpty(base.GetHelpTextCore()) ? owner.Description ?? "" : base.GetHelpTextCore();
        protected override IList<AutomationPeer> GetChildrenCore() => null!;
    }
}
