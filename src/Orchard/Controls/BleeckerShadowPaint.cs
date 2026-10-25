using System.Numerics;

namespace Orchard;

internal static class BleeckerShadowPaint
{
    // CSS's premultiplied paint bytes are authoritative. ColorHdr accepts a
    // straight float color, so divide the rounded channels by the rounded alpha.
    internal static Vector4 EncodeColor(float opacity, byte red, byte green, byte blue)
    {
        if (!float.IsFinite(opacity) || opacity < 0 || opacity > 1)
            throw new ArgumentOutOfRangeException(nameof(opacity));
        var alpha = MathF.Round(opacity * 255, MidpointRounding.AwayFromZero);
        if (alpha == 0) return Vector4.Zero;
        float Channel(byte value) => MathF.Round(value * alpha / 255, MidpointRounding.AwayFromZero) / alpha;
        return new(Channel(red), Channel(green), Channel(blue), alpha / 255);
    }
}
