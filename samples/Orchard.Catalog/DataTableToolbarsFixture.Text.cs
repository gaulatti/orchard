using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class DataTableToolbarsFixture
{
    // Use the same CSS line-box rule as Section Header and Stat Card:
    // rounded natural ascent/descent, then floored half-leading. The visible
    // text keeps its original wrapping and line allocation.
    private sealed class SourceLineBox : Panel
    {
        private readonly TextBlock text;
        private readonly TextBlock metrics = new() { Text = "Mg", Opacity = 0, IsHitTestVisible = false, UseLayoutRounding = false };
        private readonly Windows.UI.ViewManagement.UISettings settings = new();
        internal SourceLineBox(TextBlock text)
        {
            this.text = text; UseLayoutRounding = false;
            AutomationProperties.SetAccessibilityView(metrics, AccessibilityView.Raw);
            foreach (var (name, property) in new[] {
                (nameof(TextBlock.FontFamily), TextBlock.FontFamilyProperty), (nameof(TextBlock.FontSize), TextBlock.FontSizeProperty),
                (nameof(TextBlock.FontWeight), TextBlock.FontWeightProperty), (nameof(TextBlock.FontStyle), TextBlock.FontStyleProperty),
                (nameof(TextBlock.FontStretch), TextBlock.FontStretchProperty), (nameof(TextBlock.IsTextScaleFactorEnabled), TextBlock.IsTextScaleFactorEnabledProperty) })
                metrics.SetBinding(property, new Binding { Source = text, Path = new PropertyPath(name) });
            Children.Add(text); Children.Add(metrics);
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            metrics.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            text.Measure(availableSize); return text.DesiredSize;
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            metrics.Arrange(new Rect(0, 0, metrics.DesiredSize.Width, metrics.DesiredSize.Height));
            text.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            var ascent = Math.Round(metrics.BaselineOffset, MidpointRounding.AwayFromZero);
            var descent = Math.Round(metrics.ActualHeight - metrics.BaselineOffset, MidpointRounding.AwayFromZero);
            var lineHeight = text.LineHeight * (text.IsTextScaleFactorEnabled ? settings.TextScaleFactor : 1);
            var baseline = ascent + Math.Floor((lineHeight - ascent - descent) / 2);
            text.Arrange(new Rect(0, baseline - text.BaselineOffset, finalSize.Width, finalSize.Height));
            return finalSize;
        }
    }
}
