using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace Orchard.Testing;

// Exercise native root-constrained Popup placement without showing, activating,
// focusing or sending input. This is a primitive probe, not Popover acceptance.
internal static class PopoverPlacementProbe
{
    private delegate bool WindowVisitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    internal static async Task RunAsync(Window window, string output)
    {
        var root = new Canvas { Width = 800, Height = 600, Background = new SolidColorBrush(Microsoft.UI.Colors.White) };
        window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(800, 600));
        var surface = new Border { Width = 224, Height = 78, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 25, 80, 120)) };
        var popup = new Popup { Child = surface, ShouldConstrainToRootBounds = true, IsLightDismissEnabled = false, HorizontalOffset = -72, VerticalOffset = 36 };
        var anchor = new Grid { Width = 80, Height = 32 }; Canvas.SetLeft(anchor, 320); Canvas.SetTop(anchor, 240); anchor.Children.Add(popup); root.Children.Add(anchor);
        var activations = 0; var focus = 0; var guards = 0; string? failure = null; var stage = "created"; object? placement = null; int[]? rgba = null;
        window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++; surface.GotFocus += (_, _) => focus++;
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var owner); visible |= owner == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            if (visible || window.Visible || window.AppWindow.IsVisible || foreground == Environment.ProcessId || activations != 0 || focus != 0) throw new InvalidOperationException("Own native Popup probe acquired desktop visibility, activation or focus.");
        }
        async Task Settle() { for (var tick = 0; tick < 6; tick++) { root.Measure(new Size(800, 600)); root.Arrange(new Rect(0, 0, 800, 600)); root.UpdateLayout(); Guard(); await Task.Delay(20); } }
        try
        {
            await Settle(); stage = "opening"; popup.IsOpen = true; await Settle(); stage = "opened";
            if (root.XamlRoot is not { IsHostVisible: false, RasterizationScale: 1 } || root.XamlRoot.Size != new Size(800, 600)) throw new InvalidOperationException("Probe requires its actual hidden unscaled client.");
            if (!popup.IsOpen || !surface.IsLoaded || !ReferenceEquals(surface.XamlRoot, root.XamlRoot) || !VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Contains(popup)) throw new InvalidOperationException("Native Popup did not join its actual XAML root.");
            var bounds = surface.TransformToVisual(root).TransformBounds(new Rect(0, 0, surface.ActualWidth, surface.ActualHeight)); placement = new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height };
            if (bounds != new Rect(248, 276, 224, 78)) throw new InvalidOperationException("Native Popup offsets did not preserve the anchor-relative viewport rectangle: " + JsonSerializer.Serialize(placement));
            var bitmap = new RenderTargetBitmap(); var rendering = bitmap.RenderAsync(surface).AsTask(); if (await Task.WhenAny(rendering, Task.Delay(2000)) != rendering) throw new TimeoutException("Native Popup child rendering exceeded two seconds."); await rendering; Guard();
            if (bitmap.PixelWidth != 224 || bitmap.PixelHeight != 78) throw new InvalidOperationException("Native Popup surface capture was rescaled.");
            var buffer = await bitmap.GetPixelsAsync(); var pixels = new byte[buffer.Length]; using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
            var pixel = (20 * 224 + 20) * 4; rgba = [pixels[pixel + 2], pixels[pixel + 1], pixels[pixel], pixels[pixel + 3]];
            if (!rgba.SequenceEqual(new[] { 25, 80, 120, 255 })) throw new InvalidOperationException("Actual Popup child pixels differ.");
            stage = "verified";
        }
        catch (Exception error) { failure = error.ToString(); }
        finally { popup.IsOpen = false; }
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = failure is null, scope = "hidden-native-popup-placement-probe", failure, stage, placement, rgba, guards, activations, focus, activateCalled = false, showCalled = false, focusCalled = false, inputSent = false, nativePopoverImplemented = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
        window.Close(); Environment.Exit(failure is null ? 0 : 1);
    }
}
