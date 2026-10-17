namespace Orchard;

// Premultiplied OKLab interpolation for CSS rules that select that color space.
internal static class BleeckerColorMix
{
    internal static (byte R, byte G, byte B, double Alpha) Mix(
        (byte R, byte G, byte B, double Alpha) from, (byte R, byte G, byte B, double Alpha) to, double progress)
    {
        if (progress <= 0) return from;
        if (progress >= 1) return to;
        return EncodePremultiplied(MixPremultiplied(Premultiply(from), Premultiply(to), progress));
    }

    internal static (double L, double A, double B, double Alpha) Premultiply((byte R, byte G, byte B, double Alpha) color)
    {
        var lab = ToLab(color.R, color.G, color.B);
        return (lab[0] * color.Alpha, lab[1] * color.Alpha, lab[2] * color.Alpha, color.Alpha);
    }
    internal static (double L, double A, double B, double Alpha) Premultiply(double red, double green, double blue, double alpha)
    {
        var lab = ToLab(red, green, blue);
        return (lab[0] * alpha, lab[1] * alpha, lab[2] * alpha, alpha);
    }
    internal static (double L, double A, double B, double Alpha) MixPremultiplied(
        (double L, double A, double B, double Alpha) from, (double L, double A, double B, double Alpha) to, double progress) =>
        (from.L + (to.L - from.L) * progress, from.A + (to.A - from.A) * progress,
            from.B + (to.B - from.B) * progress, from.Alpha + (to.Alpha - from.Alpha) * progress);

    internal static (byte R, byte G, byte B, double Alpha) EncodePremultiplied((double L, double A, double B, double Alpha) color)
    {
        if (color.Alpha <= 0) return (0, 0, 0, 0);
        var first = color.L / color.Alpha; var second = color.A / color.Alpha; var third = color.B / color.Alpha;
        var l = Math.Pow(.9999999984505198 * first + .39633779217376786 * second + .2158037580607588 * third, 3);
        var m = Math.Pow(1.0000000088817609 * first - .10556134232365635 * second - .06385417477170591 * third, 3);
        var s = Math.Pow(1.0000000546724108 * first - .08948418209496575 * second - 1.2914855378640917 * third, 3);
        return (Encode(4.0765482669796285 * l - 3.3078604530458593 * m + .23101769051428148 * s),
            Encode(-1.2684805115952908 * l + 2.6099303721593876 * m - .3413293183059835 * s),
            Encode(-.004155928994110783 * l - .7035236918221306 * m + 1.7078390411687758 * s), color.Alpha);
    }

    internal static double Premium(double progress)
    {
        if (progress <= 0) return 0;
        if (progress >= 1) return 1;
        double low = 0, high = 1;
        for (var iteration = 0; iteration < 24; iteration++)
        {
            var t = (low + high) / 2;
            var x = 3 * (1 - t) * (1 - t) * t * .22 + 3 * (1 - t) * t * t * .36 + t * t * t;
            if (x < progress) low = t; else high = t;
        }
        return 1 - Math.Pow(1 - (low + high) / 2, 3);
    }

    private static double[] ToLab(byte red, byte green, byte blue)
    {
        return ToLab(red / 255d, green / 255d, blue / 255d);
    }
    private static double[] ToLab(double red, double green, double blue)
    {
        var r = Linear(red); var g = Linear(green); var b = Linear(blue);
        // Chromium uses Skia's ICC 16.16 sRGB-to-XYZ D50 matrix, then
        // Bradford-adapts D50 to D65 before Oklab. The combined matrix below
        // preserves that path; the common direct-sRGB approximation can move
        // an interpolated channel across an 8-bit rounding boundary.
        var l = Math.Cbrt(.41225508589273585 * r + .5363614196591441 * g + .05143204820523725 * b);
        var m = Math.Cbrt(.21191224386886334 * r + .6806760054798207 * g + .10737498988305468 * b);
        var s = Math.Cbrt(.08829789188456846 * r + .2817014746049734 * g + .6298922622684924 * b);
        return [.2104542553 * l + .7936177850 * m - .0040720468 * s,
            1.9779984951 * l - 2.4285922050 * m + .4505937099 * s,
            .0259040371 * l + .7827717662 * m - .8086757660 * s];
    }

    private static double Linear(double value)
    {
        return value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4);
    }
    private static byte Encode(double value)
    {
        var encoded = value <= .0031308 ? 12.92 * value : 1.055 * Math.Pow(value, 1 / 2.4) - .055;
        return (byte)Math.Clamp(Math.Round(encoded * 255), 0, 255);
    }
}
