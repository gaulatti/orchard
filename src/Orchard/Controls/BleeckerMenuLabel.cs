using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Orchard;

/// <summary>A non-actionable section label within a Bleecker menu.</summary>
public sealed class BleeckerMenuLabel : MenuFlyoutItem
{
    private TextBlock? text;
    private FrameworkElement? inset;

    public BleeckerMenuLabel()
    {
        DefaultStyleKey = typeof(BleeckerMenuLabel);
        RegisterPropertyChangedCallback(LanguageProperty, (_, _) => UpdateText());
        RegisterPropertyChangedCallback(TextProperty, (_, _) => UpdateText());
    }

    public static readonly DependencyProperty InsetProperty = DependencyProperty.Register(
        nameof(Inset), typeof(bool), typeof(BleeckerMenuLabel),
        new PropertyMetadata(false, (owner, _) => ((BleeckerMenuLabel)owner).UpdateInset()));

    public bool Inset { get => (bool)GetValue(InsetProperty); set => SetValue(InsetProperty, value); }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        text = GetTemplateChild("PART_Text") as TextBlock;
        inset = GetTemplateChild("PART_Inset") as FrameworkElement;
        UpdateText();
        UpdateInset();
    }

    private void UpdateText()
    {
        if (text is not null) text.Text = Text.ToUpper(CultureInfo.GetCultureInfo(Language));
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }

    private void UpdateInset() { if (inset is not null) inset.Width = Inset ? 24 : 0; }

    protected override AutomationPeer OnCreateAutomationPeer() => new LabelPeer(this);
    private sealed class LabelPeer(BleeckerMenuLabel owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerMenuLabel);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
        protected override bool IsControlElementCore() => true;
        protected override bool IsContentElementCore() => true;
        protected override string GetNameCore()
        {
            if (BleeckerAutomationMetadata.TryGetName(owner, this, out var callerName)) return callerName;
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) ? owner.Text : name;
        }
    }
}
