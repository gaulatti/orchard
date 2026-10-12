using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Orchard;

/// <summary>A compact, keyboard-accessible information trigger. Empty help text removes the trigger.</summary>
public sealed partial class BleeckerFieldHelpTooltip : Button
{
    private readonly BleeckerTooltip tooltip = new() { Placement = PlacementMode.Bottom };

    public BleeckerFieldHelpTooltip()
    {
        DefaultStyleKey = typeof(BleeckerFieldHelpTooltip);
        InitializeMotion();
        UpdateText();
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(BleeckerFieldHelpTooltip),
        new PropertyMetadata(string.Empty, (owner, _) => ((BleeckerFieldHelpTooltip)owner).UpdateText()));
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    private void UpdateText()
    {
        tooltip.Content = Text;
        Visibility = string.IsNullOrWhiteSpace(Text) ? Visibility.Collapsed : Visibility.Visible;
        BleeckerTooltipService.SetToolTip(this, Visibility == Visibility.Collapsed ? null : tooltip);
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new HelpPeer(this);
    private sealed class HelpPeer(BleeckerFieldHelpTooltip owner) : ButtonAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerFieldHelpTooltip);
        protected override string GetNameCore() => string.IsNullOrEmpty(base.GetNameCore()) ? owner.Text : base.GetNameCore();
    }
}
