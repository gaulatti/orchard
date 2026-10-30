using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard.Testing;

internal static class AlertScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 600, Height = 420 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900, 630));
            island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, 900, 630));
            window.Activate();
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                stage = theme + "/150-percent";
                root.RequestedTheme = theme;
                var alert = new BleeckerAlert
                {
                    Duration = 0,
                    Type = BleeckerAlertType.Info,
                    Message = "Fictional regional coverage update · 東京 · São Paulo · القاهرة — a long local alert that must wrap within its native notification surface."
                };
                Canvas.SetLeft(alert, 240);
                Canvas.SetTop(alert, 180);
                root.Children.Add(alert);
                await LayoutAsync(root);
                alert.ApplyTemplate();
                var template = (FrameworkElement)VisualTreeHelper.GetChild(alert, 0);
                var popup = (Popup)template.FindName("PART_Popup");
                var surface = (BleeckerOverlaySurface)template.FindName("PART_Surface");
                var message = (TextBlock)template.FindName("PART_Message");
                var close = (Button)template.FindName("PART_Close");
                var motion = (FrameworkElement)template.FindName("PART_MotionRoot");
                await UntilAsync(() => popup.IsOpen && surface.ActualHeight > 0 && motion.Opacity == 1);
                await LayoutAsync(root);
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    surface.ActualWidth == 448 && 96 + surface.ActualHeight <= root.Height &&
                    message.ActualHeight >= 48 && message.Text == alert.Message,
                    $"Alert did not fit the native 150% host: {theme}, {surface.ActualWidth}x{surface.ActualHeight}.");
                CheckInside(surface, message, theme, "message");
                CheckInside(surface, close, theme, "close");
                Require(!message.IsTextTrimmed && new UISettings().AnimationsEnabled == false &&
                    motion.RenderTransform is TranslateTransform { Y: 0 },
                    $"Alert text or reduced-motion endpoint failed at native 150%: {theme}.");
                cases.Add(new { theme = theme.ToString(), scale = root.XamlRoot.RasterizationScale,
                    width = surface.ActualWidth, height = surface.ActualHeight, messageHeight = message.ActualHeight, passed = true });
                alert.Close();
                await UntilAsync(() => !popup.IsOpen);
                root.Children.Clear();
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = true, cases,
                librarySha256 = Hash(typeof(BleeckerAlert).Assembly.Location) }));
            window.Close(); Environment.Exit(0);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = false, stage, error = error.ToString() }));
            window.Close(); Environment.Exit(1);
        }
    }

    private static void CheckInside(FrameworkElement surface, FrameworkElement child, ElementTheme theme, string name)
    {
        var bounds = child.TransformToVisual(surface).TransformBounds(new Rect(0, 0, child.ActualWidth, child.ActualHeight));
        Require(bounds.X >= -.5 && bounds.Y >= -.5 && bounds.Right <= surface.ActualWidth + .5 &&
            bounds.Bottom <= surface.ActualHeight + .5,
            $"Alert {name} clipped the native 150% surface in {theme}: {bounds}.");
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
        throw new TimeoutException("Alert did not reach its expected native popup state.");
    }

    private static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
