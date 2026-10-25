namespace Orchard;

internal static class BleeckerPanelModel
{
    internal readonly record struct Row(double DragX, double DotX, double TitleX, double TitleWidth, double CountX, double Height);
    internal static double TitleWidth(double width, double minimum, double dragWidth, double countWidth, bool drag, bool count) =>
        Math.Max(minimum, width - 40 - 6 - (drag ? dragWidth : 0) - (count ? countWidth : 0) - 12 * (1 + (drag ? 1 : 0) + (count ? 1 : 0)));
    internal static Row Header(double width, double titleMinimum, double titleHeight, double dragWidth, double dragHeight, double countWidth, double countHeight, bool drag, bool count)
    {
        foreach (var value in new[] { width, titleMinimum, titleHeight, dragWidth, dragHeight, countWidth, countHeight })
            if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(width), "Panel measurements must be finite and nonnegative.");
        var dotX = 20 + (drag ? dragWidth + 12 : 0); var titleX = dotX + 18;
        var titleWidth = TitleWidth(width, titleMinimum, dragWidth, countWidth, drag, count);
        return new(20, dotX, titleX, titleWidth, titleX + titleWidth + 12, Math.Max(56, 28 + Math.Max(Math.Max(titleHeight, drag ? dragHeight : 0), Math.Max(count ? countHeight : 0, 6))));
    }
    internal static double HeaderHeight(double rowHeight, bool filter, double filterHeight, bool toolbar, double toolbarHeight) =>
        rowHeight + (filter ? filterHeight + 16 : 0) + (toolbar ? toolbarHeight + 25 : 0) + 1;
}
