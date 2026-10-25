using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Orchard;

// Preserve premultiplied Oklab through the three CSS stops, then encode at
// the actual raster boundary. CSS positions this 45-degree image on padding.
internal sealed class BleeckerFeedColumnGradient
{
    internal ImageBrush Brush { get; } = new() { Stretch = Stretch.Fill };
    private ((byte R, byte G, byte B, double Alpha) Accent, double Width, double Height, Thickness Border, double Scale)? rendered;
    internal void Update((byte R, byte G, byte B, double Alpha) accent, double width, double height, Thickness border, double scale)
    {
        if (width <= 0 || height <= 0) return;
        var next = (accent, width, height, border, scale); if (rendered == next) return;
        var pixelWidth = checked((int)Math.Ceiling(width * scale)); var pixelHeight = checked((int)Math.Ceiling(height * scale));
        var bytes = new byte[checked(pixelWidth * pixelHeight * 4)]; var innerWidth = Math.Max(0, width - border.Left - border.Right); var innerHeight = Math.Max(0, height - border.Top - border.Bottom);
        if (innerWidth > 0 && innerHeight > 0)
        for (var y = 0; y < pixelHeight; y++) for (var x = 0; x < pixelWidth; x++)
        {
            var imageX = Repeat((x + .5) / scale - border.Left, innerWidth); var imageY = Repeat((y + .5) / scale - border.Top, innerHeight);
            var paint = BleeckerColorMix.EncodePremultiplied(BleeckerFeedColumnModel.Gradient(accent, (imageX + innerHeight - imageY) / Math.Sqrt(2)));
            var offset = (y * pixelWidth + x) * 4; bytes[offset] = Channel(paint.B * paint.Alpha); bytes[offset + 1] = Channel(paint.G * paint.Alpha); bytes[offset + 2] = Channel(paint.R * paint.Alpha); bytes[offset + 3] = Channel(255 * paint.Alpha);
        }
        var bitmap = new WriteableBitmap(pixelWidth, pixelHeight); using (var stream = bitmap.PixelBuffer.AsStream()) stream.Write(bytes); bitmap.Invalidate(); Brush.ImageSource = bitmap; rendered = next;
    }
    internal void Clear() { Brush.ImageSource = null; rendered = null; }
    private static double Repeat(double value, double length) => (value % length + length) % length;
    private static byte Channel(double value) => (byte)Math.Clamp(Math.Round(value), 0, 255);
}
