using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Testing;

internal static class HeaderScaleFixture
{
    private static IEnumerable<FrameworkElement> Elements(DependencyObject owner)
    {
        if (owner is FrameworkElement element) yield return element;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(owner); index++)
            foreach (var child in Elements(VisualTreeHelper.GetChild(owner, index))) yield return child;
    }

    private static bool Visible(FrameworkElement element)
    {
        for (DependencyObject? current = element; current is not null; current = VisualTreeHelper.GetParent(current))
            if (current is FrameworkElement { Visibility: Visibility.Collapsed }) return false;
        return true;
    }

    private static Rect Box(FrameworkElement element, FrameworkElement root) =>
        element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

    private static void RequireInside(Rect box, int width, int height, string name)
    {
        if (box.Width <= 0 || box.Height <= 0 || box.X < -.5 || box.Y < -.5 || box.Right > width + .5 || box.Bottom > height + .5)
            throw new InvalidOperationException($"Header {name} clipped at native 150%: {box}.");
    }

    internal static async Task RunAsync(Window window, string output)
    {
        var cases = new List<object>();
        var stage = "initial";
        try
        {
            var root = new Grid();
            var styles = new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/Header.xaml") };
            root.Resources.MergedDictionaries.Add(styles);
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            var animationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var width in new[] { 800, 1280 })
            foreach (var open in width < 1024 ? new[] { false, true } : new[] { false })
            {
                stage = $"{theme}/{width}/{open}";
                const int height = 500;
                root.Width = width;
                root.Height = height;
                root.RequestedTheme = theme;
                var pixels = new SizeInt32((int)(width * 1.5), (int)(height * 1.5));
                window.AppWindow.ResizeClient(pixels);
                island.SiteBridge.MoveAndResize(new RectInt32(0, 0, pixels.Width, pixels.Height));
                var header = new BleeckerHeader
                {
                    Style = (Style)styles[typeof(BleeckerHeader)],
                    Position = BleeckerHeaderPosition.Static,
                    Brand = new BleeckerHeaderBrand { Href = "#library", BrandName = "Fictional library" },
                    Navigation = new[] { new BleeckerNavItem("#one", "One"), new BleeckerNavItem("#two", "Two"), new BleeckerNavItem("#three", "Three") }
                };
                root.Children.Clear();
                root.Children.Add(header);
                for (var attempt = 0; attempt < 4; attempt++)
                {
                    root.Measure(new Size(width, height));
                    root.Arrange(new Rect(0, 0, width, height));
                    root.UpdateLayout();
                    await Task.Delay(16);
                }
                if (open) header.ToggleNavigation();
                for (var attempt = 0; attempt < 4; attempt++) { root.UpdateLayout(); await Task.Delay(16); }
                await NativeRenderReadiness.RenderAsync(root);
                if (Math.Abs(root.XamlRoot.RasterizationScale - 1.5) > .0001)
                    throw new InvalidOperationException("Header did not render at native 150% scale.");
                var surface = Elements(header).Single(item => item.Name == "HeaderSurface");
                RequireInside(Box(surface, root), width, height, "surface");
                var links = Elements(header).OfType<HyperlinkButton>().Where(link => link.Name == "NavLink" && Visible(link))
                    .Where(link => width >= 1024 || open).ToArray();
                if (links.Length != (width < 1024 && !open ? 0 : 3))
                    throw new InvalidOperationException($"Header visible link count differs: {links.Length}.");
                foreach (var link in links) RequireInside(Box(link, root), width, height, "navigation link");
                if (open)
                {
                    var mobile = Elements(header).Single(item => item.Name == "HeaderMobilePanel");
                    RequireInside(Box(mobile, root), width, height, "open mobile panel");
                }
                var toggle = Elements(header).Single(item => item.Name == "HeaderToggle");
                if (width < 1024) RequireInside(Box(toggle, root), width, height, "mobile toggle");
                var first = Box(surface, root);
                await Task.Delay(50);
                root.UpdateLayout();
                if (!first.Equals(Box(surface, root))) throw new InvalidOperationException("Header continued moving after presentation.");
                cases.Add(new { theme = theme.ToString(), width, open, scale = root.XamlRoot.RasterizationScale, visibleLinks = links.Length, surface = first, stable = true });
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled, osSettingsChanged = false,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerHeader).Assembly.Location)))
            }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close();
            Environment.Exit(0);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = false, stage, error = error.ToString(), cases }));
            window.Close();
            Environment.Exit(1);
        }
    }
}
