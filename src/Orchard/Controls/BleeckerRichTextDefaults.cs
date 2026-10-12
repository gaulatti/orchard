using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

// LineHeight is not inherited through a consumer DataTemplate's declaration
// scope. Supply the label's defaults only where the consumer supplied none.
internal sealed class BleeckerRichTextDefaults
{
    private sealed record OwnedValue(FrameworkElement Owner, DependencyProperty Property, object Value);
    private readonly List<OwnedValue> values = [];
    private readonly Dictionary<FrameworkElement, long> styles = [];

    internal void Apply(DependencyObject root)
    {
        var current = Text(root).ToHashSet();
        foreach (var text in styles.Keys.Where(text => !current.Contains(text)).ToArray()) Detach(text);
        foreach (var text in current) ApplyText(text);
    }

    private void ApplyText(FrameworkElement text)
    {
        var height = text is TextBlock ? TextBlock.LineHeightProperty : RichTextBlock.LineHeightProperty;
        var stacking = text is TextBlock ? TextBlock.LineStackingStrategyProperty : RichTextBlock.LineStackingStrategyProperty;
        Supply(text, height, 20d);
        Supply(text, stacking, LineStackingStrategy.BlockLineHeight);
    }

    private void Supply(FrameworkElement text, DependencyProperty property, object value)
    {
        if (text.ReadLocalValue(property) != DependencyProperty.UnsetValue || HasSetter(text.Style, property)) return;
        if (Equals(text.GetValue(property), value)) return;
        if (!styles.ContainsKey(text)) styles.Add(text, text.RegisterPropertyChangedCallback(FrameworkElement.StyleProperty, (_, _) =>
        {
            foreach (var owned in values.Where(item => ReferenceEquals(item.Owner, text) && HasSetter(text.Style, item.Property)).ToArray())
                Release(owned);
            ApplyText(text);
        }));
        values.Add(new(text, property, value));
        text.SetValue(property, value);
    }

    private static bool HasSetter(Style? style, DependencyProperty property)
    {
        for (; style is not null; style = style.BasedOn)
            if (style.Setters.OfType<Setter>().Any(setter => setter.Property == property)) return true;
        return false;
    }

    private void Release(OwnedValue owned)
    {
        values.Remove(owned);
        // A consumer's subsequent local value or binding remains authoritative.
        if (owned.Owner.GetBindingExpression(owned.Property) is null && Equals(owned.Owner.ReadLocalValue(owned.Property), owned.Value))
            owned.Owner.ClearValue(owned.Property);
    }

    internal void Clear()
    {
        foreach (var text in styles.Keys.ToArray()) Detach(text);
    }

    private void Detach(FrameworkElement text)
    {
        text.UnregisterPropertyChangedCallback(FrameworkElement.StyleProperty, styles[text]);
        styles.Remove(text);
        foreach (var owned in values.Where(item => ReferenceEquals(item.Owner, text)).ToArray()) Release(owned);
    }

    private static IEnumerable<FrameworkElement> Text(DependencyObject parent)
    {
        if (parent is TextBlock or RichTextBlock) yield return (FrameworkElement)parent;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            foreach (var text in Text(VisualTreeHelper.GetChild(parent, index))) yield return text;
    }
}
