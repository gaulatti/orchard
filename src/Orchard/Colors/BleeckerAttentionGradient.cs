using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Orchard;

// CSS's gradient image uses the padding box as its positioning area and
// repeats into the border box. Its 45-degree line projects onto (1,-1),
// including the magic corners on non-square surfaces.
internal sealed class BleeckerAttentionGradient
{
    internal ImageBrush Brush { get; } = new() { Stretch = Stretch.Fill };
    private (BleeckerAttentionColor Color, double Width, double Height, Thickness Border, double Scale)? rendered;

    internal void Update(BleeckerAttentionColor color, double width, double height, Thickness border, double scale)
    {
        if (width <= 0 || height <= 0) return;
        if (!double.IsFinite(width) || !double.IsFinite(height) || !double.IsFinite(scale) || scale <= 0)
            throw new ArgumentOutOfRangeException(nameof(scale), "Attention gradient requires finite geometry and a positive raster scale.");
        var next = (color, width, height, border, scale);
        if (rendered == next) return;
        var pixelWidth = checked((int)Math.Ceiling(width * scale));
        var pixelHeight = checked((int)Math.Ceiling(height * scale));
        var bytes = new byte[checked(pixelWidth * pixelHeight * 4)];
        var imageWidth = Math.Max(0, width - border.Left - border.Right);
        var imageHeight = Math.Max(0, height - border.Top - border.Bottom);
        if (imageWidth > 0 && imageHeight > 0)
        {
            for (var y = 0; y < pixelHeight; y++)
            for (var x = 0; x < pixelWidth; x++)
            {
                var imageX = Repeat((x + .5) / scale - border.Left, imageWidth);
                var imageY = Repeat((y + .5) / scale - border.Top, imageHeight);
                var position = (imageX + imageHeight - imageY) / (imageWidth + imageHeight);
                var paint = BleeckerColorMix.EncodePremultiplied(color.At(position));
                var offset = (y * pixelWidth + x) * 4;
                // WriteableBitmap's BGRA8 buffer is premultiplied in encoded
                // sRGB. Alpha is retained until this actual pixel boundary.
                bytes[offset] = Channel(paint.B * paint.Alpha);
                bytes[offset + 1] = Channel(paint.G * paint.Alpha);
                bytes[offset + 2] = Channel(paint.R * paint.Alpha);
                bytes[offset + 3] = Channel(paint.Alpha * 255);
            }
        }
        var bitmap = new WriteableBitmap(pixelWidth, pixelHeight);
        using (var stream = bitmap.PixelBuffer.AsStream()) stream.Write(bytes);
        bitmap.Invalidate(); Brush.ImageSource = bitmap; rendered = next;
    }
    internal void Clear() { Brush.ImageSource = null; rendered = null; }
    private static double Repeat(double value, double length) => (value % length + length) % length;
    private static byte Channel(double value) => (byte)Math.Clamp(Math.Round(value), 0, 255);
}
