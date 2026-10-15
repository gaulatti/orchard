using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Orchard.Catalog;

public sealed partial class InputsFixture
{
    private async Task VerifyReadingDirectionAsync(BleeckerColorScheme scheme, string folder)
    {
        var inputs = new[] { SmallInput, MediumInput, LargeInput };
        var originals = inputs.Select(input => (input.Text, input.PlaceholderText, input.FlowDirection)).ToArray();
        var results = new List<object>();
        try
        {
            foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            foreach (var filled in new[] { true, false })
            {
                foreach (var input in inputs)
                {
                    input.FlowDirection = direction;
                    input.PlaceholderText = "أخبار القاهرة";
                    input.Text = filled ? "أخبار القاهرة" : "";
                }
                UpdateLayout();
                await MainWindow.CaptureElementAsync(this, Path.Combine(folder, $"input-direction-{direction}-{filled}-{scheme}.png"));
                var bitmap = new RenderTargetBitmap();
                await bitmap.RenderAsync(this);
                var buffer = await bitmap.GetPixelsAsync();
                var pixels = new byte[(int)buffer.Length];
                using (var reader = DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
                var scale = XamlRoot.RasterizationScale;
                foreach (var input in inputs)
                {
                    var bounds = input.TransformToVisual(this).TransformBounds(new Rect(0, 0, input.ActualWidth, input.ActualHeight));
                    // RenderTargetBitmap includes negative RTL overflow even with a fixture clip.
                    // Locate the opaque border box in this isolated row instead of assuming its bitmap origin.
                    var middle = (int)Math.Round((bounds.Top + bounds.Height / 2) * scale);
                    var opaque = Enumerable.Range(0, bitmap.PixelWidth)
                        .Where(x => pixels[(middle * bitmap.PixelWidth + x) * 4 + 3] == 255).ToArray();
                    var isolated = opaque.Length > 0 && Math.Abs(opaque.Last() - opaque.First() + 1 - input.ActualWidth * scale) <= 2;
                    if (!isolated)
                    {
                        await MainWindow.SavePngAsync(Path.Combine(folder,$"input-direction-analysis-{direction}-{filled}-{scheme}.png"),pixels,bitmap.PixelWidth,bitmap.PixelHeight);
                        throw new InvalidOperationException($"Reading-direction capture does not contain the expected isolated opaque input box: {JsonSerializer.Serialize(new {
                            input.Name,input.InputSize,input.IsEnabled,input.FocusState,bounds,middle,scale,bitmap.PixelWidth,bitmap.PixelHeight,
                            opaqueCount=opaque.Length,first=opaque.Length>0?(int?)opaque.First():null,last=opaque.Length>0?(int?)opaque.Last():null,expectedWidth=input.ActualWidth*scale })}.");
                    }
                    var bitmapLeft = opaque.First() / scale;
                    var background = ((SolidColorBrush)Find<Border>(input, "BorderElement").Background).Color;
                    var brush = (SolidColorBrush)(filled ? input.Foreground : input.PlaceholderForeground);
                    var bg = (background.R + background.G + background.B) / 3d;
                    var color = brush.Color;
                    var alpha = color.A / 255d * brush.Opacity;
                    var fg = (color.R + color.G + color.B) / 3d * alpha + bg * (1 - alpha);
                    var positions = new List<double>();
                    for (var y = (int)Math.Ceiling((bounds.Top + 5) * scale); y < (bounds.Bottom - 5) * scale; y++)
                    for (var x = (int)Math.Ceiling((bitmapLeft + 5) * scale); x < (bitmapLeft + bounds.Width - 5) * scale; x++)
                    {
                        var offset = (y * bitmap.PixelWidth + x) * 4;
                        var value = (pixels[offset] + pixels[offset + 1] + pixels[offset + 2]) / 3d;
                        if ((value - bg) / (fg - bg) > .5) positions.Add(x / scale - bitmapLeft);
                    }
                    Require(positions.Count > 5, "Reading-direction capture has no visible glyphs.");
                    var center = positions.Average();
                    Require(direction == FlowDirection.LeftToRight ? center < input.ActualWidth * .4 : center > input.ActualWidth * .6,
                        $"{scheme} {input.InputSize} {(filled ? "entered" : "placeholder")} text did not align to {direction} start: {center:F1}.");
                    results.Add(new { size = input.InputSize.ToString(), direction = direction.ToString(), filled, glyphCenterX = center, scale });
                }
            }
            File.WriteAllText(Path.Combine(folder, $"input-direction-{scheme}.json"),
                JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            for (var index = 0; index < inputs.Length; index++)
            {
                inputs[index].Text = originals[index].Text;
                inputs[index].PlaceholderText = originals[index].PlaceholderText;
                inputs[index].FlowDirection = originals[index].FlowDirection;
            }
            UpdateLayout();
        }
    }
}
