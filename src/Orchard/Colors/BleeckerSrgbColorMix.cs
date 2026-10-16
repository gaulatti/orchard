namespace Orchard;

// Legacy RGB CSS colors transition in encoded sRGB, as captured from the
// pinned Stepper. Keep this distinct from explicit OKLab color-mix rules.
internal static class BleeckerSrgbColorMix
{
    internal static (double R, double G, double B, double Alpha) Premultiply((byte R, byte G, byte B, double Alpha) color) =>
        (color.R * color.Alpha, color.G * color.Alpha, color.B * color.Alpha, color.Alpha);

    internal static (double R, double G, double B, double Alpha) MixPremultiplied(
        (double R, double G, double B, double Alpha) from, (double R, double G, double B, double Alpha) to, double progress) =>
        (from.R + (to.R - from.R) * progress, from.G + (to.G - from.G) * progress,
            from.B + (to.B - from.B) * progress, from.Alpha + (to.Alpha - from.Alpha) * progress);

    internal static (byte R, byte G, byte B, double Alpha) EncodePremultiplied((double R, double G, double B, double Alpha) color) =>
        color.Alpha <= 0 ? (0, 0, 0, 0) :
            (Encode(color.R / color.Alpha), Encode(color.G / color.Alpha), Encode(color.B / color.Alpha), color.Alpha);

    private static byte Encode(double channel) => (byte)Math.Clamp(Math.Round(channel), 0, 255);
}
