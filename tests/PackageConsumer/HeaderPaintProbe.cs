using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.UI;

namespace Orchard.Testing;

internal static class HeaderPaintProbe
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);

    internal static async Task RunAsync(Window window, string output)
    {
        var root = new Canvas { Width = 640, Height = 900, UseLayoutRounding = false };
        window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(640, 900));
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window); var focus = 0; var activations = 0; var guards = 0; var rows = new List<object>();
        window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++;
        void Guard() { guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); if (window.Visible || window.AppWindow.IsVisible || IsWindowVisible(handle) || foreground == Environment.ProcessId || focus != 0 || activations != 0) throw new InvalidOperationException("Paint probe acquired the desktop."); }
        static SolidColorBrush Brush(Color color, double opacity = 1) => new(color) { Opacity = opacity };
        static Color Alpha(Color color, double value) => Color.FromArgb((byte)Math.Round(255 * value), color.R, color.G, color.B);
        void Add(Canvas parent, FrameworkElement child, double top = 0) { Canvas.SetTop(child, top); parent.Children.Add(child); }
        var variants = new[] { "separate-borders", "separate-shapes", "element-opacity", "color-alpha", "single-inner", "single-outer" };
        foreach (var scheme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            root.Children.Clear(); root.RequestedTheme = scheme;
            var dark = scheme == ElementTheme.Dark; var baseColor = dark ? Color.FromArgb(255, 13, 24, 33) : Microsoft.UI.Colors.White;
            root.Background = Brush(baseColor);
            var fill = dark ? Color.FromArgb(255, 24, 37, 51) : Microsoft.UI.Colors.White;
            var edge = dark ? Microsoft.UI.Colors.White : Color.FromArgb(255, 230, 213, 184); var edgeOpacity = dark ? .09 : .25;
            for (var index = 0; index < variants.Length; index++)
            {
                var tile = new Canvas { Width = 64, Height = 64, UseLayoutRounding = false }; Canvas.SetLeft(tile, index * 96); root.Children.Add(tile);
                Add(tile, new Border { Width = 64, Height = 64, Background = Brush(baseColor) });
                if (variants[index].StartsWith("single-", StringComparison.Ordinal))
                    Add(tile, new Border { Width = 64, Height = 41, Background = Brush(fill, .95), BorderBrush = Brush(edge, edgeOpacity), BorderThickness = new Thickness(0, 0, 0, 1), BackgroundSizing = variants[index] == "single-outer" ? BackgroundSizing.OuterBorderEdge : BackgroundSizing.InnerBorderEdge, UseLayoutRounding = false });
                else if (variants[index] == "separate-shapes")
                {
                    Add(tile, new Microsoft.UI.Xaml.Shapes.Rectangle { Width = 64, Height = 41, Fill = Brush(fill, .95) });
                    Add(tile, new Microsoft.UI.Xaml.Shapes.Rectangle { Width = 64, Height = 1, Fill = Brush(edge, edgeOpacity) }, 40);
                }
                else
                {
                    var element = variants[index] == "element-opacity"; var alpha = variants[index] == "color-alpha";
                    Add(tile, new Border { Width = 64, Height = 41, Background = Brush(alpha ? Alpha(fill, .95) : fill, element || alpha ? 1 : .95), Opacity = element ? .95 : 1, UseLayoutRounding = false });
                    Add(tile, new Border { Width = 64, Height = 1, Background = Brush(alpha ? Alpha(edge, edgeOpacity) : edge, element || alpha ? 1 : edgeOpacity), Opacity = element ? edgeOpacity : 1, UseLayoutRounding = false }, 40);
                }
            }
            for (var tick = 0; tick < 6; tick++) { root.Measure(new Size(640, 900)); root.Arrange(new Rect(0, 0, 640, 900)); root.UpdateLayout(); Guard(); await Task.Delay(20); }
            if (root.XamlRoot is not { IsHostVisible: false, RasterizationScale: 1 } || root.XamlRoot.Size != new Size(640, 900)) throw new InvalidOperationException("Probe client differs.");
            var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root); Guard();
            if (bitmap.PixelWidth != 640 || bitmap.PixelHeight != 900) throw new InvalidOperationException($"Probe dimensions differ: {bitmap.PixelWidth}x{bitmap.PixelHeight}.");
            var buffer = await bitmap.GetPixelsAsync(); var pixels = new byte[buffer.Length]; using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
            int[] Pixel(int x, int y) { var at = (y * 640 + x) * 4; return [pixels[at + 2], pixels[at + 1], pixels[at], pixels[at + 3]]; }
            for (var index = 0; index < variants.Length; index++) rows.Add(new { scheme = scheme.ToString(), variant = variants[index], background = Pixel(index * 96 + 32, 20), border = Pixel(index * 96 + 32, 40) });
        }
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { scope = "hidden-header-paint-primitive-probe", rows, guards, activations, focus, visible = window.Visible, sourceRasterParityVerified = false }, new JsonSerializerOptions { WriteIndented = true }));
        window.Close(); Environment.Exit(0);
    }
}
