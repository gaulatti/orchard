using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Orchard.Catalog;

public sealed partial class SteppersFixture
{
    // Diagnostic only: rehost the original reference frame so its entire surface
    // is visible. No control, font, brush, scale or screenshot pixels are changed.
    internal async Task CaptureRasterAsync(Grid catalogRoot, FrameworkElement foregroundTarget, string folder)
    {
        Require(XamlRoot.RasterizationScale == 1 && new Windows.UI.ViewManagement.UISettings().TextScaleFactor == 1,
            "Stepper raster diagnostic requires display and text scale 1.");
        var parent = (Panel)Frame.Parent;
        var position = parent.Children.IndexOf(Frame);
        var host = new Grid { Margin = new Thickness(20), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        Grid.SetRowSpan(host, catalogRoot.RowDefinitions.Count);
        Grid.SetColumnSpan(host, catalogRoot.ColumnDefinitions.Count);
        using var input = new NativeInputSession(XamlRoot, folder, "stepper-raster");
        await input.AcquireAsync(foregroundTarget);
        var reports = new List<object>();
        parent.Children.Remove(Frame);
        host.Children.Add(Frame);
        catalogRoot.Children.Add(host);
        try
        {
            using var reference = ReadReference();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                catalogRoot.RequestedTheme = theme;
                await MainWindow.AwaitPresentationAsync(Frame);
                UpdateFrameBackground();
                await input.MoveAsync(Frame, localPoint: new Point(1000, 720));
                foreach (var spec in reference.RootElement.GetProperty("cases").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == theme.ToString()))
                {
                    Apply(spec);
                    await SettleAsync();
                    var id = spec.GetProperty("id").GetString()!;
                    var stem = $"stepper-raster-{id}-{theme}";
                    var client = NativeScreenCapture.ClientBounds(input.Window);
                    var screen = input.ScreenBounds(Frame);
                    var bounds = new Rect(screen.X - client.X, screen.Y - client.Y, screen.Width, screen.Height);
                    Require(bounds.Width == 1024 && bounds.Height == 745 && bounds.Left >= 0 && bounds.Top >= 0 &&
                        bounds.Right <= client.Width && bounds.Bottom <= client.Height &&
                        bounds.X == Math.Floor(bounds.X) && bounds.Y == Math.Floor(bounds.Y),
                        "Stepper reference frame must be fully visible at integer device coordinates.");
                    var before = stem + "-before.png";
                    var bitmapFile = stem + "-bitmap.png";
                    var after = stem + "-after.png";
                    await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, before));
                    var bitmap = new RenderTargetBitmap();
                    await bitmap.RenderAsync(Frame);
                    Require(bitmap.PixelWidth == 1024 && bitmap.PixelHeight == 745, "Stepper bitmap changed reference dimensions.");
                    var buffer = await bitmap.GetPixelsAsync();
                    var pixels = new byte[checked((int)buffer.Length)];
                    using (var reader = DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
                    await MainWindow.SavePngAsync(Path.Combine(folder, bitmapFile), pixels, bitmap.PixelWidth, bitmap.PixelHeight);
                    await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, after));
                    reports.Add(new { id, scheme = theme.ToString(), before, bitmap = bitmapFile, after,
                        frame = new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height },
                        client = new { width = client.Width, height = client.Height }, sourceFile = spec.GetProperty("file").GetString() });
                    await File.WriteAllTextAsync(Path.Combine(folder, "stepper-raster.json"), JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true }));
                }
                await MainWindow.CaptureElementAsync(Frame, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
            }
        }
        finally
        {
            host.Children.Remove(Frame);
            catalogRoot.Children.Remove(host);
            parent.Children.Insert(position, Frame);
        }
        Require(ReferenceEquals(Frame.Parent, parent) && parent.Children.IndexOf(Frame) == position && !catalogRoot.Children.Contains(host),
            "Stepper raster diagnostic did not restore its original catalog parent.");
        await File.WriteAllTextAsync(Path.Combine(folder, "stepper-raster-restoration.json"),
            JsonSerializer.Serialize(new { restored = true, originalIndex = position, overlayRemoved = true }));
    }
}
