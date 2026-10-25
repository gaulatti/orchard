using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

// Measures the actual realized template tree. Content and templates remain
// owned by the consumer; none are reconstructed or moved for measurement.
internal sealed class BleeckerTableContentMinimum(Action changed)
{
    private readonly Dictionary<UIElement, double> preferred = [];
    private readonly Dictionary<FrameworkElement, long[]> constraints = [];
    private readonly TextBlock probe = new();
    private readonly DependencyPropertyChangedCallback constraintChanged = (_, _) => changed();
    private static readonly DependencyProperty[] ConstraintProperties = [FrameworkElement.WidthProperty, FrameworkElement.MinWidthProperty, FrameworkElement.MaxWidthProperty, FrameworkElement.MarginProperty];
    internal void Clear()
    {
        foreach (var (element, tokens) in constraints) for (var i = 0; i < tokens.Length; i++) element.UnregisterPropertyChangedCallback(ConstraintProperties[i], tokens[i]);
        constraints.Clear(); preferred.Clear();
    }
    internal void Capture(UIElement root)
    {
        preferred.Clear();
        void Visit(UIElement node)
        {
            preferred[node] = node.DesiredSize.Width;
            // Changing a descendant's constraints can leave the row's native
            // desired size unchanged. Invalidate the shared columns directly.
            if (node is FrameworkElement tracked && !constraints.ContainsKey(tracked))
                constraints.Add(tracked, ConstraintProperties.Select(property => tracked.RegisterPropertyChangedCallback(property, constraintChanged)).ToArray());
            // An explicit width determines this subtree's minimum through
            // Bound. Its descendants cannot contribute to that result.
            if (node.Visibility == Visibility.Collapsed || node is FrameworkElement element && double.IsFinite(element.Width)) return;
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
                if (VisualTreeHelper.GetChild(node, i) is UIElement child) Visit(child);
        }
        Visit(root);
        foreach (var element in constraints.Keys.Where(element => !preferred.ContainsKey(element)).ToArray())
        {
            var tokens = constraints[element]; for (var i = 0; i < tokens.Length; i++) element.UnregisterPropertyChangedCallback(ConstraintProperties[i], tokens[i]);
            constraints.Remove(element);
        }
    }
    private static UIElement[] Children(UIElement node) => Enumerable.Range(0, VisualTreeHelper.GetChildrenCount(node))
        .Select(i => VisualTreeHelper.GetChild(node, i)).OfType<UIElement>().Where(e => e.Visibility == Visibility.Visible).ToArray();
    private static double Horizontal(Thickness value) => value.Left + value.Right;
    private static double Bound(FrameworkElement element, double width)
    {
        var value = double.IsFinite(element.Width) ? element.Width : width;
        return Math.Max(0, Math.Max(element.MinWidth, Math.Min(element.MaxWidth, value)) + Horizontal(element.Margin));
    }
    private double Preferred(UIElement node) => preferred.TryGetValue(node, out var width) ? width : node.DesiredSize.Width;
    internal double Measure(UIElement node)
    {
        if (node.Visibility == Visibility.Collapsed) return 0;
        var element = node as FrameworkElement;
        if (element is not null && double.IsFinite(element.Width)) return Bound(element, 0);
        var children = Children(node);
        double width;
        switch (node)
        {
            case TextBlock text: width = TextMinimum(text) + Horizontal(text.Padding); break;
            case Border border: width = (border.Child is { } child ? Measure(child) : 0) + Horizontal(border.Padding) + Horizontal(border.BorderThickness); break;
            case Grid grid: width = GridMinimum(grid) + Horizontal(grid.Padding) + Horizontal(grid.BorderThickness); break;
            case StackPanel stack:
                // A horizontal native StackPanel measures its children with an
                // infinite horizontal constraint, so they cannot wrap there.
                width = stack.Orientation == Orientation.Horizontal
                    ? children.Sum(Preferred) + stack.Spacing * Math.Max(0, children.Length - 1)
                    : children.Select(Measure).DefaultIfEmpty().Max();
                width += Horizontal(stack.Padding) + Horizontal(stack.BorderThickness); break;
            case ContentPresenter presenter:
                width = children.Select(Measure).DefaultIfEmpty().Max() + Horizontal(presenter.Padding) + Horizontal(presenter.BorderThickness); break;
            case ScrollViewer:
                // Its content has its own viewport and scrolling contract.
                width = Math.Max(0, node.DesiredSize.Width - (element is null ? 0 : Horizontal(element.Margin))); break;
            case Control when children.Length > 0:
                // The control's realized template already contains its own
                // padding/border presentation; do not count those twice.
                width = children.Select(Measure).DefaultIfEmpty().Max(); break;
            default:
                // Opaque/native leaves and custom panels supply their normal
                // unconstrained desired size, captured before the zero probe.
                width = Math.Max(0, Preferred(node) - (element is null ? 0 : Horizontal(element.Margin))); break;
        }
        return element is null ? width : Bound(element, width);
    }
    private double GridMinimum(Grid grid)
    {
        var count = Math.Max(1, grid.ColumnDefinitions.Count);
        var widths = new double[count]; var fixedColumns = new bool[count];
        for (var i = 0; i < grid.ColumnDefinitions.Count; i++)
        {
            var column = grid.ColumnDefinitions[i]; fixedColumns[i] = column.Width.IsAbsolute;
            widths[i] = Math.Max(column.MinWidth, column.Width.IsAbsolute ? Math.Min(column.MaxWidth, column.Width.Value) : 0);
        }
        foreach (var child in grid.Children.Where(e => e.Visibility == Visibility.Visible).OrderBy(e => e is FrameworkElement f ? Grid.GetColumnSpan(f) : 1))
        {
            var first = Math.Min(child is FrameworkElement positioned ? Grid.GetColumn(positioned) : 0, count - 1);
            var span = Math.Min(child is FrameworkElement spanned ? Grid.GetColumnSpan(spanned) : 1, count - first);
            var required = Measure(child) - grid.ColumnSpacing * (span - 1);
            var current = widths.Skip(first).Take(span).Sum();
            var flexible = Enumerable.Range(first, span).Where(i => !fixedColumns[i]).ToArray();
            if (required > current && flexible.Length > 0) foreach (var index in flexible) widths[index] += (required - current) / flexible.Length;
        }
        for (var i = 0; i < grid.ColumnDefinitions.Count; i++)
            widths[i] = Math.Max(grid.ColumnDefinitions[i].MinWidth, Math.Min(grid.ColumnDefinitions[i].MaxWidth, widths[i]));
        return widths.Sum() + grid.ColumnSpacing * (count - 1);
    }
    private sealed record TextStyle(FontFamily Family, double Size, Windows.UI.Text.FontWeight Weight, Windows.UI.Text.FontStyle Style, Windows.UI.Text.FontStretch Stretch, int Spacing)
    {
        internal TextStyle Apply(Inline value) => new(
            value.ReadLocalValue(TextElement.FontFamilyProperty) == DependencyProperty.UnsetValue ? Family : value.FontFamily,
            value.ReadLocalValue(TextElement.FontSizeProperty) == DependencyProperty.UnsetValue ? Size : value.FontSize,
            value is Bold ? value.FontWeight : value.ReadLocalValue(TextElement.FontWeightProperty) == DependencyProperty.UnsetValue ? Weight : value.FontWeight,
            value is Italic ? value.FontStyle : value.ReadLocalValue(TextElement.FontStyleProperty) == DependencyProperty.UnsetValue ? Style : value.FontStyle,
            value.ReadLocalValue(TextElement.FontStretchProperty) == DependencyProperty.UnsetValue ? Stretch : value.FontStretch,
            value.ReadLocalValue(TextElement.CharacterSpacingProperty) == DependencyProperty.UnsetValue ? Spacing : value.CharacterSpacing);
    }
    private double TextMinimum(TextBlock text)
    {
        var initial = new TextStyle(text.FontFamily, text.FontSize, text.FontWeight, text.FontStyle, text.FontStretch, text.CharacterSpacing);
        var fragments = new List<(string Text, TextStyle Style, UIElement? Embedded)>();
        void Append(string value, TextStyle style)
        {
            if (fragments.Count > 0 && fragments[^1].Embedded is null && fragments[^1].Style == style)
                fragments[^1] = (fragments[^1].Text + value, style, null);
            else fragments.Add((value, style, null));
        }
        void Walk(InlineCollection values, TextStyle inherited)
        {
            foreach (var inline in values)
            {
                var style = inherited.Apply(inline);
                if (inline is Run run) Append(run.Text, style);
                else if (inline is Span span) Walk(span.Inlines, style);
                else if (inline is LineBreak) Append("\n", style);
                else if (inline is InlineUIContainer { Child: { } child }) fragments.Add(("", style, child));
            }
        }
        if (text.Inlines.Count == 0) Append(text.Text, initial); else Walk(text.Inlines, initial);
        var maximum = 0d; var word = 0d;
        void Break() { maximum = Math.Max(maximum, word); word = 0; }
        double Width(string value, TextStyle style)
        {
            probe.UseLayoutRounding = text.UseLayoutRounding; probe.FontFamily = style.Family; probe.FontSize = style.Size;
            probe.FontWeight = style.Weight; probe.FontStyle = style.Style; probe.FontStretch = style.Stretch;
            probe.CharacterSpacing = style.Spacing; probe.Language = text.Language; probe.FlowDirection = text.FlowDirection;
            probe.IsTextScaleFactorEnabled = text.IsTextScaleFactorEnabled; probe.Text = value;
            probe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); return probe.DesiredSize.Width;
        }
        foreach (var fragment in fragments)
        {
            if (fragment.Embedded is { } child) { Break(); maximum = Math.Max(maximum, Measure(child)); continue; }
            if (text.TextWrapping == TextWrapping.NoWrap)
            {
                var lines = fragment.Text.Split('\n'); for (var i = 0; i < lines.Length; i++) { word += Width(lines[i], fragment.Style); if (i < lines.Length - 1) Break(); }
            }
            else if (text.TextWrapping == TextWrapping.Wrap)
            {
                var characters = System.Globalization.StringInfo.GetTextElementEnumerator(fragment.Text);
                while (characters.MoveNext()) maximum = Math.Max(maximum, Width(characters.GetTextElement(), fragment.Style));
            }
            else foreach (System.Text.RegularExpressions.Match part in System.Text.RegularExpressions.Regex.Matches(fragment.Text, @"[^\s-]+-?|\s+|-"))
            {
                if (string.IsNullOrWhiteSpace(part.Value)) Break();
                else { word += Width(part.Value, fragment.Style); if (part.Value.EndsWith('-')) Break(); }
            }
        }
        Break(); return maximum;
    }
}
