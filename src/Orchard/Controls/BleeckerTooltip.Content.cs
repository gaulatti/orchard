using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Orchard;

public sealed partial class BleeckerTooltip
{
    private BleeckerContentText? contentSlot;
    private (object? Value, DataTemplate? Template, DataTemplateSelector? Selector)? renderedContent;
    private void SynchronizeContent()
    {
        if (contentSlot is not { } slot) return;
        if (renderedContent is not { } before || !ReferenceEquals(before.Value, Content) || !ReferenceEquals(before.Template, ContentTemplate) || !ReferenceEquals(before.Selector, ContentTemplateSelector))
        {
            slot.Set(Content, ContentTemplate, selector: ContentTemplateSelector);
            renderedContent = (Content, ContentTemplate, ContentTemplateSelector);
            FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
        }
        if (slot.FlowDirection != FlowDirection) slot.FlowDirection = FlowDirection;
        var text = slot.Text;
        if (text.FlowDirection != FlowDirection) text.FlowDirection = FlowDirection;
        if (text.FontFamily.Source != FontFamily.Source) text.FontFamily = FontFamily;
        if (text.FontSize != FontSize) text.FontSize = FontSize;
        if (text.FontWeight != FontWeight) text.FontWeight = FontWeight;
        if (text.FontStyle != FontStyle) text.FontStyle = FontStyle;
        if (text.FontStretch != FontStretch) text.FontStretch = FontStretch;
        if (text.CharacterSpacing != CharacterSpacing) text.CharacterSpacing = CharacterSpacing;
        if (!ReferenceEquals(text.Foreground, Foreground)) text.Foreground = Foreground;
        if (text.LineHeight != FontSize * 1.625) text.LineHeight = FontSize * 1.625;
        if (text.IsTextScaleFactorEnabled != IsTextScaleFactorEnabled) text.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled;
        if (text.Language != Language) text.Language = Language;
    }
}
