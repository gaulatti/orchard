using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>Opt-in reference line-box metrics for branded text with an explicit LineHeight.</summary>
public static class BleeckerTextLayout
{
    public static readonly DependencyProperty UseReferenceLineBoxProperty = DependencyProperty.RegisterAttached(
        "UseReferenceLineBox", typeof(bool), typeof(BleeckerTextLayout), new PropertyMetadata(false, Changed));
    public static bool GetUseReferenceLineBox(DependencyObject element) => (bool)element.GetValue(UseReferenceLineBoxProperty);
    public static void SetUseReferenceLineBox(DependencyObject element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (element is not TextBlock) throw new ArgumentException("Reference line boxes require a TextBlock.", nameof(element));
        element.SetValue(UseReferenceLineBoxProperty, value);
    }
    private static void Changed(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        if (owner is not TextBlock text) throw new ArgumentException("Reference line boxes require a TextBlock.");
        text.Loaded -= Loaded;
        // Reparenting can temporarily clear IsLoaded while the logical parent
        // already exists. Re-enabling must wrap that parent immediately.
        if ((bool)args.NewValue) { text.Loaded += Loaded; Wrap(text); }
        else if (text.Parent is ReferenceLineBox box) Replace(box, text, () => box.Release());
    }
    private static void Loaded(object sender, RoutedEventArgs args) => Wrap((TextBlock)sender);
    private static void Wrap(TextBlock text)
    {
        if (!GetUseReferenceLineBox(text) || text.Parent is null) return;
        if (text.Parent is ReferenceLineBox) return;
        var horizontal = text.HorizontalAlignment; var vertical = text.VerticalAlignment;
        var box = new ReferenceLineBox(text, horizontal, vertical);
        Replace(text, box, () => { text.HorizontalAlignment = HorizontalAlignment.Stretch; text.VerticalAlignment = VerticalAlignment.Stretch; box.Children.Add(text); });
    }
    private static void Replace(FrameworkElement old, FrameworkElement next, Action move)
    {
        Grid.SetColumn(next, Grid.GetColumn(old)); Grid.SetRow(next, Grid.GetRow(old)); Grid.SetColumnSpan(next, Grid.GetColumnSpan(old)); Grid.SetRowSpan(next, Grid.GetRowSpan(old));
        if (old.Parent is Panel panel) { var index = panel.Children.IndexOf(old); panel.Children.RemoveAt(index); move(); panel.Children.Insert(index, next); }
        else if (old.Parent is Border border) { border.Child = null; move(); border.Child = next; }
        else throw new InvalidOperationException("Reference line boxes require a Panel or Border parent.");
    }
    private sealed class ReferenceLineBox : Panel
    {
        private readonly TextBlock text, natural = new() { Text = "Mg", UseLayoutRounding = false, IsTextScaleFactorEnabled = false }, advance = new() { UseLayoutRounding = false, IsTextScaleFactorEnabled = false };
        private readonly HorizontalAlignment originalHorizontal;
        private readonly VerticalAlignment originalVertical;
        private readonly UISettings settings = new();
        private readonly Transform? originalTransform;
        private readonly TranslateTransform baselineShift = new();
        private readonly TransformGroup transforms = new();
        private double lineHeight;
        internal ReferenceLineBox(TextBlock text, HorizontalAlignment horizontal, VerticalAlignment vertical)
        {
            this.text = text; originalHorizontal = HorizontalAlignment = horizontal; originalVertical = VerticalAlignment = vertical; UseLayoutRounding = false;
            originalTransform = text.RenderTransform;
            if (originalTransform is not null) transforms.Children.Add(originalTransform);
            transforms.Children.Add(baselineShift); text.RenderTransform = transforms;
        }
        internal void Release() { Children.Remove(text); text.HorizontalAlignment = originalHorizontal; text.VerticalAlignment = originalVertical; if (ReferenceEquals(text.RenderTransform, transforms)) text.RenderTransform = originalTransform!; }
        protected override Size MeasureOverride(Size availableSize)
        {
            if (!(text.LineHeight > 0)) throw new InvalidOperationException("Reference line boxes require an explicit positive LineHeight.");
            var scale = text.IsTextScaleFactorEnabled ? settings.TextScaleFactor : 1;
            foreach (var probe in new[] { natural, advance })
            {
                probe.FontFamily = text.FontFamily; probe.FontSize = text.FontSize * scale; probe.FontWeight = text.FontWeight;
                probe.FontStretch = text.FontStretch; probe.FontStyle = text.FontStyle; probe.Language = text.Language;
                Typography.SetNumeralAlignment(probe, Typography.GetNumeralAlignment(text));
            }
            advance.Text = text.Text; natural.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); advance.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var tracking = text.FontSize * scale * text.CharacterSpacing / 1000;
            var width = Math.Min(availableSize.Width, Math.Ceiling((advance.DesiredSize.Width + tracking * new StringInfo(text.Text).LengthInTextElements) * 64) / 64);
            lineHeight = text.LineHeight * scale; text.Measure(new Size(width, lineHeight));
            return new Size(width, lineHeight);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            text.Arrange(new Rect(0, 0, finalSize.Width, lineHeight));
            var ascent = Math.Round(natural.BaselineOffset, MidpointRounding.AwayFromZero);
            var descent = Math.Round(natural.DesiredSize.Height - natural.BaselineOffset, MidpointRounding.AwayFromZero);
            var baseline = ascent + Math.Floor((lineHeight - ascent - descent) / 2);
            baselineShift.Y = baseline - text.BaselineOffset;
            return finalSize;
        }
    }
}
