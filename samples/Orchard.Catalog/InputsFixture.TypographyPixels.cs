using System.Text.Json;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Catalog;

public sealed partial class InputsFixture
{
    private async Task VerifyTypographyPixelsAsync(BleeckerInput input, string state, string folder, BleeckerColorScheme scheme)
    {
        var path = Path.Combine(folder, $"input-typography-alignment-{input.InputSize}-{state}-{scheme}.json");
        var textScale = new Windows.UI.ViewManagement.UISettings().TextScaleFactor;
        if (Math.Abs(XamlRoot.RasterizationScale - 1) > .0001 || Math.Abs(textScale - 1) > .0001)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(new { verified = false,
                reason = "The retained browser profile covers 100% display and text scale only.",
                scale = XamlRoot.RasterizationScale, textScale }));
            return;
        }
        using var stream = typeof(InputsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.InputTypography.json")!;
        using var reference = JsonDocument.Parse(stream);
        var root = reference.RootElement;
        var profile = root.GetProperty("profiles").EnumerateArray().Single(item =>
            item.GetProperty("size").GetString() == input.InputSize.ToString() && item.GetProperty("state").GetString() == state);
        var expected = profile.GetProperty("rows").EnumerateArray().Select(item => item.GetDouble()).ToArray();
        var xStart = root.GetProperty("xStart").GetInt32();
        var xEnd = root.GetProperty("xEnd").GetInt32();
        var yStart = root.GetProperty("yStart").GetInt32();
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(this);
        var origin = input.TransformToVisual(this).TransformPoint(new Windows.Foundation.Point());
        var left = (int)Math.Round(origin.X);
        var top = (int)Math.Round(origin.Y);
        Require(input.ActualWidth == 320 && left >= 0 && top >= 0 &&
            left + input.ActualWidth <= bitmap.PixelWidth && top + input.ActualHeight <= bitmap.PixelHeight,
            "Typography comparison input is outside the fixture capture.");
        var buffer = await bitmap.GetPixelsAsync();
        var pixels = new byte[(int)buffer.Length];
        using (var reader = DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
        var background = ((SolidColorBrush)Find<Border>(input, "BorderElement").Background).Color;
        var brush = (SolidColorBrush)(state == "filled" ? input.Foreground : input.PlaceholderForeground);
        var foreground = brush.Color;
        var bg = (background.R + background.G + background.B) / 3d;
        var alpha = foreground.A / 255d * brush.Opacity;
        var fg = (foreground.R + foreground.G + foreground.B) / 3d * alpha + bg * (1 - alpha);
        Require(Math.Abs(fg - bg) > 20, "Typography fixture lacks enough contrast to measure its glyphs.");
        double[] Rows(int shift) => Enumerable.Range(0, expected.Length).Select(row =>
            Enumerable.Range(xStart, xEnd - xStart).Average(x =>
            {
                var offset = ((top + yStart + row + shift) * bitmap.PixelWidth + left + x) * 4;
                var value = (pixels[offset] + pixels[offset + 1] + pixels[offset + 2]) / 3d;
                return Math.Max(0, (value - bg) / (fg - bg));
            })).ToArray();
        Require(Rows(0).Sum() > 1, "Typography capture contains no measurable text.");
        var scores = Enumerable.Range(-2, 5).Select(shift => new
        {
            nativeSampleShiftY = shift,
            squaredError = Rows(shift).Zip(expected, (actual, target) => Math.Pow(actual - target, 2)).Sum()
        }).ToArray();
        var best = scores.MinBy(score => score.squaredError)!;
        File.WriteAllText(path, JsonSerializer.Serialize(new { verified = best.nativeSampleShiftY == 0,
            referenceCommit = root.GetProperty("referenceCommit").GetString(), scores },
            new JsonSerializerOptions { WriteIndented = true }));
        Require(best.nativeSampleShiftY == 0,
            $"{scheme} {input.InputSize} {state} glyphs align best at vertical sample offset {best.nativeSampleShiftY}px, not zero.");
    }
}
