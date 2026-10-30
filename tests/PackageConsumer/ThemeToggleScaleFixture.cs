using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Testing;

internal static class ThemeToggleScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 320, Height = 240 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(480, 360));
            island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, 480, 360));
            window.Activate();
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var mode in Enum.GetValues<BleeckerThemeMode>())
            foreach (var size in Enum.GetValues<BleeckerIconButtonSize>())
            foreach (var variant in Enum.GetValues<BleeckerIconButtonVariant>())
            foreach (var enabled in new[] { true, false })
            {
                stage = $"{theme}/{mode}/{size}/{variant}/{enabled}/150-percent";
                root.RequestedTheme = theme;
                var toggle = new BleeckerThemeToggle { Mode = mode, Size = size, Variant = variant, IsEnabled = enabled };
                Canvas.SetLeft(toggle, 16);
                Canvas.SetTop(toggle, 16);
                root.Children.Add(toggle);
                await LayoutAsync(root);
                var icon = Descendants<ContentControl>(toggle).Single(item => item.Name == "ThemeIcon");
                var vector = Descendants<Viewbox>(icon).Single();
                var expected = size switch { BleeckerIconButtonSize.Small => 32d, BleeckerIconButtonSize.Medium => 40d, _ => 44d };
                var owner = toggle.TransformToVisual(root).TransformBounds(new Rect(0, 0, toggle.ActualWidth, toggle.ActualHeight));
                var iconBounds = icon.TransformToVisual(toggle).TransformBounds(new Rect(0, 0, icon.ActualWidth, icon.ActualHeight));
                var vectorBounds = vector.TransformToVisual(toggle).TransformBounds(new Rect(0, 0, vector.ActualWidth, vector.ActualHeight));
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 && toggle.ActualTheme == theme &&
                    toggle.IsLoaded && toggle.ActualWidth == expected && toggle.ActualHeight == expected &&
                    owner.X >= 0 && owner.Y >= 0 && owner.Right <= root.Width && owner.Bottom <= root.Height &&
                    iconBounds.X >= -.5 && iconBounds.Y >= -.5 && iconBounds.Right <= expected + .5 && iconBounds.Bottom <= expected + .5 &&
                    vectorBounds.X >= -.5 && vectorBounds.Y >= -.5 && vectorBounds.Right <= expected + .5 && vectorBounds.Bottom <= expected + .5 &&
                    vector.ActualWidth == 18 && vector.ActualHeight == 18,
                    $"Theme Toggle clipped its native icon at 150%: owner={owner}, icon={iconBounds}, vector={vectorBounds}.");
                cases.Add(new { theme = theme.ToString(), mode = mode.ToString(), size = size.ToString(), variant = variant.ToString(), enabled,
                    scale = root.XamlRoot.RasterizationScale, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = true, cases,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerThemeToggle).Assembly.Location))) }));
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

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is T found) yield return found;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            foreach (var child in Descendants<T>(VisualTreeHelper.GetChild(parent, i))) yield return child;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
