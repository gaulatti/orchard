namespace Orchard;

internal static class BleeckerFeedColumnModel
{
    internal static double TitleWidth(double width, double minimum, double countWidth, bool count) => Math.Max(minimum, width - 18 - (count ? countWidth + 10 : 0));
    internal static double RowHeight(double titleHeight, double countHeight) => Math.Max(28, Math.Max(titleHeight, countHeight));
    internal static double HeaderHeight(double rowHeight, double filterHeight) => 49 + rowHeight + filterHeight;
    internal static (double L, double A, double B, double Alpha) Border((byte R, byte G, byte B, double Alpha) accent) =>
        BleeckerColorMix.MixPremultiplied(BleeckerColorMix.Premultiply((255, 255, 255, 19 / 255d)), BleeckerColorMix.Premultiply(accent), .15);
    internal static (double L, double A, double B, double Alpha) Gradient((byte R, byte G, byte B, double Alpha) accent, double distance)
    {
        var middle = BleeckerColorMix.Premultiply((24, 37, 49, 224 / 255d));
        if (distance < 208) return BleeckerColorMix.MixPremultiplied(BleeckerColorMix.MixPremultiplied(middle, BleeckerColorMix.Premultiply(accent), .11), middle, Math.Clamp(distance / 208, 0, 1));
        return BleeckerColorMix.MixPremultiplied(middle, BleeckerColorMix.Premultiply((13, 24, 33, 245 / 255d)), Math.Clamp((distance - 208) / 304, 0, 1));
    }
}
