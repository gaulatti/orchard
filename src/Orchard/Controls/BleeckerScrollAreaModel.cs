namespace Orchard;

public enum BleeckerScrollAreaOrientation { Vertical, Horizontal, Both }

// Source viewport/extent geometry. A hidden user axis still has a scroll
// range. Native rendering, input and original content remain host concerns.
internal static class BleeckerScrollAreaModel
{
    internal const double VerticalBarWidth = 6;
    internal const double HorizontalBarHeight = 15;
    internal readonly record struct Insets(double Left, double Top, double Right, double Bottom)
    {
        internal double Horizontal => Left + Right;
        internal double Vertical => Top + Bottom;
    }
    internal readonly record struct Metrics(double Width, double Height, double ViewportWidth, double ViewportHeight,
        double ContentWidth, double ContentHeight, double ExtentWidth, double ExtentHeight, double ClientLeft, double ClientTop, bool VerticalBar, bool HorizontalBar)
    {
        internal double MaximumHorizontal => Math.Max(0, ExtentWidth - ViewportWidth);
        internal double MaximumVertical => Math.Max(0, ExtentHeight - ViewportHeight);
        internal double DomClientWidth => DomInteger(ViewportWidth);
        internal double DomClientHeight => DomInteger(ViewportHeight);
        internal double DomExtentWidth => DomInteger(ExtentWidth);
        internal double DomExtentHeight => DomInteger(ExtentHeight);
    }
    private static double DomInteger(double value) => Math.Floor(value + .5);
    internal static Metrics Measure(double width, double? contentWidth, double contentHeight, double maximumHeight,
        BleeckerScrollAreaOrientation orientation, bool rtl, Insets padding = default, Insets border = default, double? height = null)
    {
        if (!Enum.IsDefined(orientation)) throw new ArgumentOutOfRangeException(nameof(orientation));
        foreach (var value in new[] { width, contentWidth ?? 0, contentHeight, padding.Left, padding.Top, padding.Right, padding.Bottom, border.Left, border.Top, border.Right, border.Bottom, height ?? 0 })
            if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (double.IsNaN(maximumHeight) || maximumHeight < 0 || double.IsNegativeInfinity(maximumHeight)) throw new ArgumentOutOfRangeException(nameof(maximumHeight));
        var vertical = false; var horizontal = false; Metrics result = default;
        for (var pass = 0; pass < 8; pass++)
        {
            var viewportWidth = Math.Max(0, width - border.Horizontal - (vertical ? VerticalBarWidth : 0));
            var actualContentWidth = contentWidth ?? Math.Max(0, viewportWidth - padding.Horizontal);
            var naturalHeight = contentHeight + padding.Vertical + border.Vertical + (horizontal ? HorizontalBarHeight : 0);
            var outerHeight = Math.Max(padding.Vertical + border.Vertical, Math.Min(height ?? naturalHeight, maximumHeight));
            var viewportHeight = Math.Max(0, outerHeight - border.Vertical - (horizontal ? HorizontalBarHeight : 0));
            var extentWidth = Math.Max(viewportWidth, actualContentWidth + padding.Horizontal);
            var extentHeight = Math.Max(viewportHeight, contentHeight + padding.Vertical);
            result = new(width, outerHeight, viewportWidth, viewportHeight, actualContentWidth, contentHeight, extentWidth, extentHeight,
                border.Left + (rtl && vertical ? VerticalBarWidth : 0), border.Top, vertical, horizontal);
            var nextVertical = orientation != BleeckerScrollAreaOrientation.Horizontal && extentHeight > viewportHeight;
            var nextHorizontal = orientation != BleeckerScrollAreaOrientation.Vertical && extentWidth > viewportWidth;
            if (vertical == nextVertical && horizontal == nextHorizontal) return result;
            vertical = nextVertical; horizontal = nextHorizontal;
        }
        throw new InvalidOperationException("ScrollArea viewport/scrollbar geometry did not converge.");
    }
    internal static (double Left, double Top) Clamp(Metrics metrics, double left, double top, bool rtl)
    {
        if (!double.IsFinite(left) || !double.IsFinite(top)) throw new ArgumentOutOfRangeException(nameof(left));
        return (rtl ? Math.Clamp(left, -metrics.MaximumHorizontal, 0) : Math.Clamp(left, 0, metrics.MaximumHorizontal), Math.Clamp(top, 0, metrics.MaximumVertical));
    }
    internal static (double X, double Y) ContentOrigin(Metrics metrics, Insets padding, bool rtl, double left = 0, double top = 0) =>
        (metrics.ClientLeft + padding.Left + (rtl ? metrics.ViewportWidth - padding.Horizontal - metrics.ContentWidth : 0) - left,
         metrics.ClientTop + padding.Top - top);
    internal static bool UserHorizontal(BleeckerScrollAreaOrientation orientation) => orientation is BleeckerScrollAreaOrientation.Horizontal or BleeckerScrollAreaOrientation.Both;
    internal static bool UserVertical(BleeckerScrollAreaOrientation orientation) => orientation is BleeckerScrollAreaOrientation.Vertical or BleeckerScrollAreaOrientation.Both;
}
