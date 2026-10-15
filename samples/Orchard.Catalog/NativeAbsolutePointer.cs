namespace Orchard.Catalog;

internal static class NativeAbsolutePointer
{
    // SendInput maps 65,536 normalized units across the virtual desktop.
    // Select the requested pixel's interior rather than rounding an edge.
    internal static int Normalize(int pixel, int origin, int extent)
    {
        if (extent <= 1) throw new ArgumentOutOfRangeException(nameof(extent));
        var offset = (long)pixel - origin;
        if (offset < 0 || offset >= extent) throw new ArgumentOutOfRangeException(nameof(pixel));
        var normalized = (offset * 65536 + 32768) / extent;
        if (normalized is < 0 or > 65535 || normalized * extent / 65536 != offset)
            throw new InvalidOperationException("The virtual desktop cannot represent this exact pixel in absolute input.");
        return (int)normalized;
    }
}
