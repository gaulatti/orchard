using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerDataTablePagination
{
    private sealed class FlexRow : Panel
    {
        internal double Gap;
        internal double Minimum;
        private double[] widths = [];
        protected override Size MeasureOverride(Size available)
        {
            var items = Children.Where(item => item.Visibility == Visibility.Visible).ToArray();
            foreach (var item in items) item.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var basis = items.Select(item => LayoutWidth(item.DesiredSize.Width)).ToArray();
            var minima = items.Select(MinimumWidth).ToArray(); Minimum = minima.Sum() + Math.Max(0, items.Length - 1) * Gap;
            widths = Shrink(basis, minima, double.IsFinite(available.Width) ? available.Width - Math.Max(0, items.Length - 1) * Gap : basis.Sum());
            for (var index = 0; index < items.Length; index++) items[index].Measure(new Size(widths[index], double.PositiveInfinity));
            return new Size(widths.Sum() + Math.Max(0, items.Length - 1) * Gap, items.Select(item => item.DesiredSize.Height).DefaultIfEmpty(0).Max());
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var x = 0d; var items = Children.Where(item => item.Visibility == Visibility.Visible).ToArray();
            for (var index = 0; index < items.Length; index++) { var item = items[index]; item.Arrange(new Rect(x, (finalSize.Height - item.DesiredSize.Height) / 2, widths[index], item.DesiredSize.Height)); x += widths[index] + Gap; }
            return finalSize;
        }
    }
    private sealed class PaginationPanel : Panel
    {
        private readonly TextBlock summary;
        private readonly FlexRow right;
        internal bool Wide;
        private double summaryWidth, rightWidth;
        internal PaginationPanel(TextBlock summary, FlexRow right) { this.summary = summary; this.right = right; Children.Add(summary); Children.Add(right); }
        protected override Size MeasureOverride(Size available)
        {
            summary.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); right.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var natural = new[] { LayoutWidth(summary.DesiredSize.Width), LayoutWidth(right.DesiredSize.Width) };
            if (Wide)
            {
                var widths = Shrink(natural, [MinimumWidth(summary), right.Minimum], double.IsFinite(available.Width) ? available.Width - 16 : natural.Sum()); summaryWidth = widths[0]; rightWidth = widths[1];
            }
            else { summaryWidth = natural[0]; rightWidth = natural[1]; }
            summary.Measure(new Size(summaryWidth, double.PositiveInfinity)); right.Measure(new Size(rightWidth, double.PositiveInfinity));
            return new Size(double.IsFinite(available.Width) ? available.Width : Wide ? summaryWidth + 16 + rightWidth : Math.Max(summaryWidth, rightWidth), Wide ? Math.Max(summary.DesiredSize.Height, right.DesiredSize.Height) : summary.DesiredSize.Height + 16 + right.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Wide)
            { summary.Arrange(new Rect(0, (finalSize.Height - summary.DesiredSize.Height) / 2, summaryWidth, summary.DesiredSize.Height)); right.Arrange(new Rect(Math.Max(summaryWidth + 16, finalSize.Width - rightWidth), (finalSize.Height - right.DesiredSize.Height) / 2, rightWidth, right.DesiredSize.Height)); }
            else
            { summary.Arrange(new Rect((finalSize.Width - summaryWidth) / 2, 0, summaryWidth, summary.DesiredSize.Height)); right.Arrange(new Rect((finalSize.Width - rightWidth) / 2, summary.DesiredSize.Height + 16, rightWidth, right.DesiredSize.Height)); }
            return finalSize;
        }
    }
    private static double MinimumWidth(UIElement item)
    {
        if (item.Visibility != Visibility.Visible) return 0;
        if (item is FlexRow row) return row.Minimum;
        if (item is not TextBlock text) return item.DesiredSize.Width;
        var probe = new TextBlock { FontFamily = text.FontFamily, FontSize = text.FontSize, FontWeight = text.FontWeight, FontStyle = text.FontStyle, FontStretch = text.FontStretch, CharacterSpacing = text.CharacterSpacing, IsTextScaleFactorEnabled = text.IsTextScaleFactorEnabled, UseLayoutRounding = false };
        var width = 0d; foreach (var word in text.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries)) { probe.Text = word; probe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); width = Math.Max(width, probe.DesiredSize.Width); } return LayoutWidth(width);
    }
    // CSS uses 1/64px layout units. Round intrinsic widths up so converting a
    // native measured width back to float cannot wrap its last word spuriously.
    private static double LayoutWidth(double width) => Math.Ceiling(width * 64) / 64;
    private static double[] Shrink(double[] basis, double[] minima, double available)
    {
        var widths = basis.ToArray(); var deficit = Math.Max(0, basis.Sum() - Math.Max(minima.Sum(), available)); var frozen = new bool[basis.Length];
        while (deficit > .000001)
        {
            var weight = basis.Where((_, index) => !frozen[index]).Sum(); if (weight == 0) break; var removed = 0d;
            for (var index = 0; index < widths.Length; index++) if (!frozen[index]) { var desired = widths[index] - deficit * basis[index] / weight; var next = Math.Max(minima[index], desired); removed += widths[index] - next; widths[index] = next; if (next <= minima[index]) frozen[index] = true; }
            if (removed <= .000001) break; deficit -= removed;
        }
        return widths;
    }
}
