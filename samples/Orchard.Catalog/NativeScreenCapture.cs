using System.Runtime.InteropServices;
using Windows.Foundation;

namespace Orchard.Catalog;

// Hosted desktop verification only. Screen pixels include composition child
// visuals that RenderTargetBitmap omits, including the switch thumb shadow.
internal static class NativeScreenCapture
{
    internal static async Task<Rect> CaptureClientAsync(IntPtr window, string path)
    {
        if (Environment.GetEnvironmentVariable("CI") != "true" || GetForegroundWindow() != window)
            throw new InvalidOperationException("Composed capture requires the hosted catalog's foreground window.");
        var origin = new ScreenPoint(0, 0);
        if (!GetClientRect(window, out var client) || !ClientToScreen(window, ref origin))
            throw new InvalidOperationException("Could not locate the catalog client on screen.");
        var width = client.Right - client.Left;
        var height = client.Bottom - client.Top;
        if (width <= 0 || height <= 0) throw new InvalidOperationException("Catalog client has no screen area.");
        foreach (var point in new[] { new ScreenPoint(origin.X + 1, origin.Y + 1),
            new ScreenPoint(origin.X + width - 2, origin.Y + height - 2), new ScreenPoint(origin.X + width / 2, origin.Y + height / 2) })
            if (GetAncestor(WindowFromPoint(point), 2) != window)
                throw new InvalidOperationException("Catalog client is occluded or outside the interactive screen.");
        if (DwmFlush() < 0) throw new InvalidOperationException("Desktop composition did not flush before capture.");

        var screen = GetDC(IntPtr.Zero);
        var memory = IntPtr.Zero;
        var bitmap = IntPtr.Zero;
        var previous = IntPtr.Zero;
        var pixels = new byte[checked(width * height * 4)];
        try
        {
            memory = CreateCompatibleDC(screen);
            bitmap = CreateCompatibleBitmap(screen, width, height);
            if (screen == IntPtr.Zero || memory == IntPtr.Zero || bitmap == IntPtr.Zero)
                throw new InvalidOperationException("Could not allocate the catalog screen capture.");
            previous = SelectObject(memory, bitmap);
            if (previous == IntPtr.Zero || previous == new IntPtr(-1))
                throw new InvalidOperationException("Could not select the catalog capture bitmap.");
            if (!BitBlt(memory, 0, 0, width, height, screen, origin.X, origin.Y, 0x40CC0020))
                throw new InvalidOperationException("Could not copy the composed catalog pixels.");
            SelectObject(memory, previous); previous = IntPtr.Zero;
            var info = new BitmapInfo { Size = 40, Width = width, Height = -height, Planes = 1, BitCount = 32 };
            if (GetDIBits(memory, bitmap, 0, (uint)height, pixels, ref info, 0) != height)
                throw new InvalidOperationException("Could not read the composed catalog bitmap.");
        }
        finally
        {
            if (previous != IntPtr.Zero && previous != new IntPtr(-1)) SelectObject(memory, previous);
            if (bitmap != IntPtr.Zero) DeleteObject(bitmap);
            if (memory != IntPtr.Zero) DeleteDC(memory);
            if (screen != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screen);
        }
        // BI_RGB reserves the fourth byte; the copied screen is fully opaque.
        for (var offset = 3; offset < pixels.Length; offset += 4) pixels[offset] = 255;
        if (pixels.Where((_, offset) => offset % 4 != 3).Distinct().Count() <= 8)
            throw new InvalidOperationException("Composed catalog capture is blank.");
        await MainWindow.SavePngAsync(path, pixels, width, height);
        return new Rect(origin.X, origin.Y, width, height);
    }

    [StructLayout(LayoutKind.Sequential)] private readonly struct ScreenPoint(int x, int y) { public readonly int X = x, Y = y; }
    [StructLayout(LayoutKind.Sequential)] private struct ScreenRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo
    {
        public uint Size; public int Width, Height; public ushort Planes, BitCount;
        public uint Compression, SizeImage; public int XPelsPerMeter, YPelsPerMeter;
        public uint ClrUsed, ClrImportant, Color;
    }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetClientRect(IntPtr window, out ScreenRect rect);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ClientToScreen(IntPtr window, ref ScreenPoint point);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(ScreenPoint point);
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int width, int height);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool BitBlt(IntPtr destination, int x, int y, int width, int height, IntPtr source, int sourceX, int sourceY, uint operation);
    [DllImport("gdi32.dll")] private static extern int GetDIBits(IntPtr dc, IntPtr bitmap, uint start, uint lines, [Out] byte[] pixels, ref BitmapInfo info, uint usage);
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
}
