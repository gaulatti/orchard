using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace Orchard.Testing;

// A native inline-layout primitive probe, not an implemented HoverCard.
internal static class HoverCardPlacementProbe
{
    private delegate bool WindowVisitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    private sealed class Inline : Panel
    {
        internal readonly Button Trigger = new() { Content = "Hover", Width = 80, Height = 32, MinWidth = 0, MinHeight = 0 };
        internal readonly TextBox Editor = new() { Text = "Original", Width = 80, Height = 24, MinWidth = 0, MinHeight = 0, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        internal readonly Border Surface;
        internal bool Top, Open = true;
        internal Inline()
        {
            UseLayoutRounding = false;
            Surface = new() { Width = 288, Height = 94, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 25, 80, 120)), Child = Editor, UseLayoutRounding = false };
            Children.Add(Trigger); Children.Add(Surface); Canvas.SetZIndex(Surface, 50);
        }
        protected override Size MeasureOverride(Size available) { Trigger.Measure(available); Surface.Measure(new(double.PositiveInfinity, double.PositiveInfinity)); return Trigger.DesiredSize; }
        protected override Size ArrangeOverride(Size final)
        {
            Trigger.Arrange(new Rect(0, 0, 80, 32)); Surface.Arrange(new Rect(-104, Top ? -102 : 40, 288, 94));
            Surface.RenderTransform = new TranslateTransform { Y = Open ? 0 : Top ? -4 : 4 }; Surface.Opacity = Open ? 1 : 0; Surface.IsHitTestVisible = Open;
            return final;
        }
    }
    internal static async Task RunAsync(Window window, string output)
    {
        var root = new Canvas { Width = 800, Height = 600, Background = new SolidColorBrush(Microsoft.UI.Colors.White), UseLayoutRounding = false }; window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(800, 600));
        var activations = 0; var focus = 0; var guards = 0; var cases = new List<object>(); var captures = new List<object>(); var failures = new List<object>();
        window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++;
        void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var owner); visible |= owner == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && activations == 0 && focus == 0, "Inline probe acquired visibility, activation or focus.");
        }
        async Task Layout() { for (var i = 0; i < 6; i++) { root.Measure(new Size(800, 600)); root.Arrange(new Rect(0, 0, 800, 600)); root.UpdateLayout(); Guard(); await Task.Delay(20); } }
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        foreach (var name in new[] { "bottom", "top", "closed", "clipped" })
        {
            try
            {
                root.Children.Clear(); var inline = new Inline { Top = name == "top", Open = name != "closed" }; var host = new Canvas { Width = 140, Height = 110, UseLayoutRounding = false }; Canvas.SetLeft(host, 320); Canvas.SetTop(host, 240); host.Children.Add(inline); root.Children.Add(host);
                if (name == "clipped") host.Clip = new RectangleGeometry { Rect = new Rect(0, 0, 140, 110) };
                await Layout(); Require(root.XamlRoot is { IsHostVisible: false, RasterizationScale: 1 } && root.XamlRoot.Size == new Size(800, 600), "Inline probe requires its actual hidden unscaled client.");
                var bounds = inline.Surface.TransformToVisual(root).TransformBounds(new Rect(0, 0, inline.Surface.ActualWidth, inline.Surface.ActualHeight)); var expected = new Rect(216, name == "top" ? 138 : name == "closed" ? 284 : 280, 288, 94);
                Require(bounds == expected && inline.DesiredSize == new Size(80, 32) && inline.ActualWidth == 80 && inline.ActualHeight == 32, "Inline surface changed flow or placement.");
                var bitmap = new RenderTargetBitmap(); var operation = bitmap.RenderAsync(root); var task = operation.AsTask(); if (await Task.WhenAny(task, Task.Delay(2000)) != task) { operation.Cancel(); throw new TimeoutException("Inline ancestor capture exceeded two seconds."); } await task; Guard();
                Require(bitmap.PixelWidth == 800 && bitmap.PixelHeight == 600, "Inline ancestor capture was rescaled.");
                var buffer = await bitmap.GetPixelsAsync(); var pixels = new byte[buffer.Length]; using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
                int[] Pixel(int x, int y) { var i = (y * 800 + x) * 4; return [pixels[i + 2], pixels[i + 1], pixels[i], pixels[i + 3]]; }
                using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream(); var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream);
                encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied, 800, 600, 96, 96, pixels); await encoder.FlushAsync();
                using var pngReader = new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0)); await pngReader.LoadAsync((uint)stream.Size); var png = new byte[stream.Size]; pngReader.ReadBytes(png);
                var file = name + ".png"; await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, file), png); captures.Add(new { name, file, sha256 = Convert.ToHexStringLower(SHA256.HashData(png)), width = 800, height = 600 });
                var samples = new List<object>();
                foreach (var (x, offset) in new[] { (260, 50), (340, 50), (340, 80) })
                {
                    var y = (int)bounds.Y + offset; var hit = VisualTreeHelper.FindElementsInHostCoordinates(new Point(x, y), root).Contains(inline.Surface); var color = Pixel(x, y);
                    var visible = name is "bottom" or "top" || name == "clipped" && x == 340 && offset == 50;
                    samples.Add(new { x, y, rgba = color, surfaceHit = hit });
                    Require(color.SequenceEqual(visible ? new[] { 25, 80, 120, 255 } : new[] { 255, 255, 255, 255 }), $"Inline overflow or ancestor clip changed actual pixels at {x},{y}.");
                    Require(hit == visible, $"Inline overflow or ancestor clip changed child hit testing at {x},{y}.");
                }
                var focusable = ReferenceEquals(FocusManager.FindFirstFocusableElement(inline.Surface), inline.Editor); Require(focusable, "Opacity/hit-test hiding removed the original editor from focus traversal.");
                Require(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0, "Inline surface introduced a popup.");
                cases.Add(new { name, passed = true, bounds = new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height }, flow = new { width = inline.ActualWidth, height = inline.ActualHeight }, samples, originalEditorFocusable = focusable, clipping = name == "clipped" });
            }
            catch (Exception error) { failures.Add(new { name, error = error.ToString() }); }
        }
        root.Children.Clear(); await Layout();
        string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = failures.Count == 0, scope = "hidden-inline-hover-card-primitive", diagnosticOnly = true, cases, captures, failures, guards, activations, focus, librarySha256 = Hash(typeof(BleeckerPopover).Assembly.Location), hostSha256 = Hash(typeof(HoverCardPlacementProbe).Assembly.Location), processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), activateCalled = false, showCalled = false, focusCalled = false, inputSent = false, nativeHoverCardImplemented = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
        window.Close(); Environment.Exit(failures.Count == 0 ? 0 : 1);
    }
}
