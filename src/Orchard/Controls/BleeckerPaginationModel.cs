namespace Orchard;

internal static class BleeckerPaginationModel
{
    internal static bool IsVisible(double totalPages) => !(totalPages <= 1);

    internal static double[] Pages(double currentPage, double totalPages)
    {
        if (!IsVisible(totalPages) || double.IsNaN(totalPages)) return [];
        var length = (int)Math.Floor(Math.Min(totalPages, 7));
        var pages = new double[length];
        for (var index = 0; index < length; index++)
            pages[index] = totalPages <= 7 || currentPage <= 4 ? index + 1
                : currentPage >= totalPages - 3 ? totalPages - 6 + index
                : currentPage - 3 + index;
        return pages;
    }

    // The source flex row shrinks the two 36px arrows down to the 20px
    // glyph plus its borders. Number buttons keep their min-content widths.
    internal static double ArrowWidth(double available, IReadOnlyList<double> pages)
    {
        var numberWidth = pages.Sum() + Math.Max(0, pages.Count - 1) * 4;
        return Math.Clamp((available - numberWidth - 12) / 2, 22, 36);
    }

    internal static double Start(double available, IReadOnlyList<double> pages)
    {
        var total = pages.Sum() + Math.Max(0, pages.Count - 1) * 4 + ArrowWidth(available, pages) * 2 + 12;
        return Math.Floor((available - total) / 2 * 64) / 64;
    }
}
