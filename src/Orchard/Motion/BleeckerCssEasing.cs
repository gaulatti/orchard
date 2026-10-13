namespace Orchard;

// Tailwind 4.2.1's default transition curve, used by the pinned BrandLockup
// opacity utility. Bleecker's explicitly premium transitions remain separate.
internal static class BleeckerCssEasing
{
    internal static double Default(double progress)
    {
        if (progress <= 0) return 0;
        if (progress >= 1) return 1;
        double low = 0, high = 1;
        for (var iteration = 0; iteration < 24; iteration++)
        {
            var t = (low + high) / 2;
            var x = 3 * (1 - t) * (1 - t) * t * .4 + 3 * (1 - t) * t * t * .2 + t * t * t;
            if (x < progress) low = t; else high = t;
        }
        var parameter = (low + high) / 2;
        return 3 * (1 - parameter) * parameter * parameter + parameter * parameter * parameter;
    }
}
