using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Orchard.Catalog;

internal static class NativeCapturePixels
{
    // Alpha 255 makes premultiplied and straight values identical. Any other
    // alpha must keep Windows' conversion and its rounding rules.
    internal static bool TryConvertOpaque(ReadOnlySpan<byte> bgra, Span<byte> rgba)
    {
        if (bgra.Length == 0 || bgra.Length % 4 != 0 || rgba.Length != bgra.Length)
            throw new ArgumentException("Invalid capture pixel buffers.");
        var at = 0;
        if (Vector128.IsHardwareAccelerated)
        {
            var alpha = Vector128.Create(0xff000000u);
            var channels = Vector128.Create((byte)2, 1, 0, 3, 6, 5, 4, 7, 10, 9, 8, 11, 14, 13, 12, 15);
            ref var source = ref MemoryMarshal.GetReference(bgra);
            ref var destination = ref MemoryMarshal.GetReference(rgba);
            for (; at <= bgra.Length - Vector128<byte>.Count; at += Vector128<byte>.Count)
            {
                var pixels = Vector128.LoadUnsafe(ref source, (nuint)at);
                if (!Vector128.EqualsAll(pixels.AsUInt32() & alpha, alpha)) return false;
                Vector128.Shuffle(pixels, channels).StoreUnsafe(ref destination, (nuint)at);
            }
        }
        for (; at < bgra.Length; at += 4)
        {
            if (bgra[at + 3] != 255) return false;
            rgba[at] = bgra[at + 2]; rgba[at + 1] = bgra[at + 1];
            rgba[at + 2] = bgra[at]; rgba[at + 3] = 255;
        }
        return true;
    }
}
