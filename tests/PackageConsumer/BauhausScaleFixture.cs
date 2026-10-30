using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace Orchard.Testing;

internal static class BauhausScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas
            {
                Width = 600, Height = 420,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 76, 104))
            };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900, 630));
            island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, 900, 630));
            window.Activate();
            var source = new WriteableBitmap(12, 4);
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                root.RequestedTheme = theme;
                foreach (var (type, width, height) in new[]
                {
                    (BleeckerBauhausType.Nazca, 288d, 96d),
                    (BleeckerBauhausType.Autostrada, 96d, 96d),
                    (BleeckerBauhausType.Pompeii, 72d, 144d),
                    (BleeckerBauhausType.Monitor, 40d, 32d)
                })
                {
                    stage = $"{theme}/{type}/150-percent";
                    var control = new BleeckerBauhausBackground
                    {
                        Type = type, Width = width, Height = height, ImageSource = source
                    };
                    Canvas.SetLeft(control, 16);
                    Canvas.SetTop(control, 16);
                    root.Children.Add(control);
                    await LayoutAsync(root);
                    control.ApplyTemplate();
                    var template = (FrameworkElement)VisualTreeHelper.GetChild(control, 0);
                    var visualRoot = (Grid)template.FindName("PART_Root");
                    var image = (Image)template.FindName("PART_Image");
                    var outputSurface = (Border)template.FindName("PART_Output");
                    await UntilAsync(() => ElementCompositionPreview.GetElementChildVisual(outputSurface) is SpriteVisual);
                    var outputVisual = (SpriteVisual)ElementCompositionPreview.GetElementChildVisual(outputSurface);
                    var clip = visualRoot.Clip as RectangleGeometry;
                    Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                        control.ActualTheme == theme &&
                        control.ActualWidth == width && control.ActualHeight == height &&
                        outputSurface.ActualWidth == width && outputSurface.ActualHeight == height &&
                        clip is not null && clip.Rect == new Rect(0, 0, width, height) &&
                        outputVisual.IsVisible && outputVisual.Size.X == width && outputVisual.Size.Y == height &&
                        ReferenceEquals(image.Source, source),
                        $"Bauhaus {type} escaped its native 150% bounds in {theme}.");
                    cases.Add(new { theme = theme.ToString(), type = type.ToString(), scale = root.XamlRoot.RasterizationScale,
                        width = control.ActualWidth, height = control.ActualHeight, passed = true });
                    root.Children.Clear();
                    await LayoutAsync(root);
                }
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = true, cases,
                librarySha256 = Hash(typeof(BleeckerBauhausBackground).Assembly.Location) }));
            window.Close(); Environment.Exit(0);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = false, stage, error = error.ToString() }));
            window.Close(); Environment.Exit(1);
        }
    }

    private static async Task LayoutAsync(Canvas root)
    {
        for (var i = 0; i < 3; i++)
        {
            root.Measure(new Size(root.Width, root.Height));
            root.Arrange(new Rect(0, 0, root.Width, root.Height));
            root.UpdateLayout();
            await Task.Delay(16);
        }
    }

    private static async Task UntilAsync(Func<bool> ready)
    {
        for (var i = 0; i < 300; i++)
        {
            if (ready()) return;
            await Task.Delay(16);
        }
        throw new TimeoutException("Bauhaus native composition output did not load.");
    }

    private static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
