namespace Orchard.Catalog;

internal static class NativeCapturePixels
{
    // Alpha 255 makes premultiplied and straight values identical. Any other
    // alpha must keep Windows' conversion and its rounding rules.
    internal static bool TryConvertOpaque(ReadOnlySpan<byte> bgra, Span<byte> rgba)
    {
        if (bgra.Length == 0 || bgra.Length % 4 != 0 || rgba.Length != bgra.Length)
            throw new ArgumentException("Invalid capture pixel buffers.");
        for (var at = 0; at < bgra.Length; at += 4)
        {
            if (bgra[at + 3] != 255) return false;
            rgba[at] = bgra[at + 2]; rgba[at + 1] = bgra[at + 1];
            rgba[at + 2] = bgra[at]; rgba[at + 3] = 255;
        }
        return true;
    }
}
