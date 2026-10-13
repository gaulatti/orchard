namespace Orchard;

// Frozen Bleecker attention-color.ts. Values remain continuous until a native
// pixel/brush boundary needs encoding; CSS color-mix uses premultiplied Oklab.
internal sealed record BleeckerAttentionColor(
    int Hue, double Intensity, int RedMix, double Coverage,
    (double L, double A, double B, double Alpha) Accent,
    (double L, double A, double B, double Alpha) Start,
    (double L, double A, double B, double Alpha) Middle,
    (double L, double A, double B, double Alpha) End,
    (double L, double A, double B, double Alpha) Border,
    double MiddlePosition, double EndPosition)
{
    internal static BleeckerAttentionColor Create(double hue, double rawIntensity)
    {
        // These defaults are explicitly defined by the frozen producer.
        var roundedHue = Math.Floor((double.IsFinite(hue) ? hue : 210) + .5);
        var normalizedHue = (int)((roundedHue % 360 + 360) % 360);
        var intensity = Math.Clamp(double.IsFinite(rawIntensity) ? rawIntensity : 0, 0, 10);
        var redMix = Round(intensity * 10); var coverage = 24 + intensity * 7.2;
        var strength = Round(10 + intensity * 6.2);
        var accent = BleeckerColorMix.MixPremultiplied(Hsl(normalizedHue, .66, .52), Hsl(6, .78, .56), redMix / 100d);
        // The frozen browser parses these legacy rgba() endpoints as byte alpha
        // before converting them to the color-mix interpolation space.
        var backdrop = BleeckerColorMix.Premultiply((17, 30, 41, 240 / 255d));
        var white = BleeckerColorMix.Premultiply((255, 255, 255, 15 / 255d));
        return new(normalizedHue, intensity, redMix, coverage, accent,
            BleeckerColorMix.MixPremultiplied(backdrop, accent, strength / 100d),
            BleeckerColorMix.MixPremultiplied(backdrop, accent, Round(strength * .66) / 100d), backdrop,
            BleeckerColorMix.MixPremultiplied(white, accent, Round(12 + intensity * 2.4) / 100d),
            FixedOne(coverage * .58) / 100, FixedOne(coverage) / 100);
    }
    internal (double L, double A, double B, double Alpha) At(double position)
    {
        if (position <= 0) return Start;
        if (position >= EndPosition) return End;
        return position <= MiddlePosition
            ? BleeckerColorMix.MixPremultiplied(Start, Middle, position / MiddlePosition)
            : BleeckerColorMix.MixPremultiplied(Middle, End, (position - MiddlePosition) / (EndPosition - MiddlePosition));
    }
    private static int Round(double value) => (int)Math.Floor(value + .5);
    private static double FixedOne(double value)
    {
        // ECMAScript toFixed rounds the exact binary value, with positive ties
        // toward the greater decimal integer; multiplying a double first can
        // introduce a second rounding and .NET F1 uses a different tie rule.
        var bits = BitConverter.DoubleToInt64Bits(value);
        var exponent = (int)((bits >> 52) & 0x7ff) - 1023 - 52;
        var mantissa = new System.Numerics.BigInteger((bits & 0x000f_ffff_ffff_ffffL) | (1L << 52));
        var numerator = mantissa * 10;
        if (exponent >= 0) return (double)(numerator << exponent) / 10;
        var denominator = System.Numerics.BigInteger.One << -exponent;
        var whole = System.Numerics.BigInteger.DivRem(numerator, denominator, out var remainder);
        if (remainder * 2 >= denominator) whole++;
        return (double)whole / 10;
    }
    private static (double L, double A, double B, double Alpha) Hsl(double hue, double saturation, double lightness)
    {
        var amplitude = saturation * Math.Min(lightness, 1 - lightness);
        double Channel(double offset)
        {
            var k = (offset + hue / 30) % 12;
            return lightness - amplitude * Math.Max(-1, Math.Min(Math.Min(k - 3, 9 - k), 1));
        }
        return BleeckerColorMix.Premultiply(Channel(0), Channel(8), Channel(4), 1);
    }
}
