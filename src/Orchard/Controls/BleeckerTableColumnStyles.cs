using Microsoft.UI.Xaml;

namespace Orchard;

// One synchronous reconciliation owns this cache. Retained cells keep their
// existing presentation; newly presented cells can share equivalent native
// styles without retaining caller styles beyond their normal cell lifetime.
internal sealed class BleeckerTableColumnStyles
{
    private readonly Dictionary<(Type Target, HorizontalAlignment Alignment), Style> defaults = [];
    private readonly Dictionary<Style, Dictionary<(Type Target, HorizontalAlignment Alignment), Style>> based = new(ReferenceEqualityComparer.Instance);

    internal Style Get(Type target, Style? source, HorizontalAlignment alignment)
    {
        var styles = defaults;
        if (source is not null && !based.TryGetValue(source, out styles))
        {
            styles = [];
            based.Add(source, styles);
        }
        var key = (target, alignment);
        if (!styles.TryGetValue(key, out var presentation))
        {
            presentation = new Style { TargetType = target, BasedOn = source };
            presentation.Setters.Add(new Setter(Microsoft.UI.Xaml.Controls.Control.HorizontalContentAlignmentProperty, alignment));
            styles.Add(key, presentation);
        }
        return presentation;
    }
}
