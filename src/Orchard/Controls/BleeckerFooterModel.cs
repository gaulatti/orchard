namespace Orchard;

internal static class BleeckerFooterModel
{
    internal static int Columns(double viewport) => viewport < 768 ? 1 : viewport < 1024 ? 2 : 4;
    internal static int BrandSpan(double viewport, int sections) => viewport >= 1024 && sections <= 2 ? 2 : 1;
    internal static double Container(double viewport, double available)
    {
        var maximum = viewport >= 1536 ? 1536 : viewport >= 1280 ? 1280 : viewport >= 1024 ? 1024 : viewport >= 768 ? 768 : viewport >= 640 ? 640 : double.PositiveInfinity;
        return Math.Min(available, maximum);
    }
    internal static bool DefaultSource(object? bottomRight) => bottomRight is null;
    internal static bool IsSource(string href) => href.Contains("github.com", StringComparison.Ordinal);
}
