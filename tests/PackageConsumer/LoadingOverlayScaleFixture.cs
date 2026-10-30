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

internal static class LoadingOverlayScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 640, Height = 480 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.Activate();
            var animationsEnabled = new UISettings().AnimationsEnabled;
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var size in new[] { (Width: 320, Height: 240), (Width: 640, Height: 480) })
            {
                stage = $"{theme}/{size.Width}x{size.Height}/150-percent";
                root.Width = size.Width; root.Height = size.Height; root.RequestedTheme = theme;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(size.Width * 3 / 2, size.Height * 3 / 2));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, size.Width * 3 / 2, size.Height * 3 / 2));
                var overlay = new BleeckerLoadingOverlay { Label = "Preparing local preview", IsVisible = true };
                root.Children.Add(overlay);
                await LayoutAsync(root);
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 && overlay.IsLoaded &&
                    overlay.ActualTheme == theme, "Loading Overlay did not adopt native 150% theme and scale.");
                var portal = VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot)
                    .Select(popup => popup.Child).OfType<FrameworkElement>()
                    .Single(item => item.Name == "PART_Portal");
                Require(Math.Abs(portal.ActualWidth - size.Width) < .5 &&
                    Math.Abs(portal.ActualHeight - size.Height) < .5, "Loading portal clipped at native 150% scale.");
                foreach (var name in new[] { "PART_Card", "PART_Spinner", "PART_LabelHost", "PART_Label" })
                {
                    var part = Descendants<FrameworkElement>(portal).Single(item => item.Name == name);
                    Require(part.ActualWidth > 0 && part.ActualHeight > 0 &&
                        Within(Bounds(part, portal), new Rect(0, 0, portal.ActualWidth, portal.ActualHeight)),
                        $"Loading Overlay {name} clipped at native 150% scale.");
                }
                if (!animationsEnabled)
                {
                    var canvas = Descendants<Canvas>(portal).Single(item => item.Name == "PART_Canvas");
                    var rotation = (RotateTransform)canvas.RenderTransform;
                    var start = rotation.Angle;
                    await Task.Delay(120);
                    await LayoutAsync(root);
                    Require(rotation.Angle == start, "Loading spinner continued rotating with reduced motion.");
                }
                cases.Add(new { theme = theme.ToString(), size.Width, size.Height,
                    scale = root.XamlRoot.RasterizationScale, passed = true });
                overlay.IsVisible = false;
                root.Children.Remove(overlay);
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled, reducedMotionStaticVerified = !animationsEnabled,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerLoadingOverlay).Assembly.Location)))
            }));
            window.Close(); Environment.Exit(0);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = false, stage, error = error.ToString() }));
            window.Close(); Environment.Exit(1);
        }
    }

    private static Rect Bounds(FrameworkElement element, FrameworkElement parent) =>
        element.TransformToVisual(parent).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
    private static bool Within(Rect inner, Rect outer) => inner.X >= outer.X - .5 && inner.Y >= outer.Y - .5 &&
        inner.Right <= outer.Right + .5 && inner.Bottom <= outer.Bottom + .5;
    private static async Task LayoutAsync(Canvas root)
    {
        for (var index = 0; index < 3; index++)
        {
            root.Measure(new Size(root.Width, root.Height));
            root.Arrange(new Rect(0, 0, root.Width, root.Height));
            root.UpdateLayout();
            await Task.Delay(16);
        }
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is T found) yield return found;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            foreach (var child in Descendants<T>(VisualTreeHelper.GetChild(parent, index))) yield return child;
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
