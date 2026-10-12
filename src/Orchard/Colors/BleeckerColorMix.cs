namespace Orchard;

// CSS color transitions interpolate premultiplied Oklab, including alpha.
internal static class BleeckerColorMix
{
    internal static (byte R, byte G, byte B, double Alpha) Mix(
        (byte R, byte G, byte B, double Alpha) from, (byte R, byte G, byte B, double Alpha) to, double progress)
    {
        if (progress <= 0) return from;
        if (progress >= 1) return to;
        var alpha = from.Alpha + (to.Alpha - from.Alpha) * progress;
        if (alpha <= 0) return (0, 0, 0, 0);
        var first = ToLab(from.R, from.G, from.B);
        var second = ToLab(to.R, to.G, to.B);
        var lab = new double[3];
        for (var index = 0; index < 3; index++)
            lab[index] = (first[index] * from.Alpha * (1 - progress) + second[index] * to.Alpha * progress) / alpha;
        var l = Math.Pow(lab[0] + .3963377774 * lab[1] + .2158037573 * lab[2], 3);
        var m = Math.Pow(lab[0] - .1055613458 * lab[1] - .0638541728 * lab[2], 3);
        var s = Math.Pow(lab[0] - .0894841775 * lab[1] - 1.2914855480 * lab[2], 3);
        return (Encode(4.0767416621 * l - 3.3077115913 * m + .2309699292 * s),
            Encode(-1.2684380046 * l + 2.6097574011 * m - .3413193965 * s),
            Encode(-.0041960863 * l - .7034186147 * m + 1.7076147010 * s), alpha);
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
        var r = Linear(red); var g = Linear(green); var b = Linear(blue);
        var l = Math.Cbrt(.4122214708 * r + .5363325363 * g + .0514459929 * b);
        var m = Math.Cbrt(.2119034982 * r + .6806995451 * g + .1073969566 * b);
        var s = Math.Cbrt(.0883024619 * r + .2817188376 * g + .6299787005 * b);
        return [.2104542553 * l + .7936177850 * m - .0040720468 * s,
            1.9779984951 * l - 2.4285922050 * m + .4505937099 * s,
            .0259040371 * l + .7827717662 * m - .8086757660 * s];
    }

    private static double Linear(byte channel)
    {
        var value = channel / 255d;
        return value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4);
    }
    private static byte Encode(double value)
    {
        var encoded = value <= .0031308 ? 12.92 * value : 1.055 * Math.Pow(value, 1 / 2.4) - .055;
        return (byte)Math.Clamp(Math.Round(encoded * 255), 0, 255);
    }
}
