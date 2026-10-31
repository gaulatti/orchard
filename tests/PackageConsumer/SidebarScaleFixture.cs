using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard.Testing;

internal static class SidebarScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 400, Height = 500 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(600, 750));
            island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, 600, 750));
            window.Activate();
            var animationsEnabled = new UISettings().AnimationsEnabled;
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var collapsed in new[] { false, true })
            foreach (var rtl in new[] { false, true })
            foreach (var constrained in new[] { false, true })
            {
                stage = $"{theme}/{collapsed}/{rtl}/{constrained}/150-percent";
                root.RequestedTheme = theme;
                var sidebar = new BleeckerSidebar
                {
                    Height = 400, Collapsed = collapsed, Header = "Local workspace", Footer = "Local only",
                    FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                    Items = new ObservableCollection<BleeckerSidebarItem>
                    {
                        new() { Id = "overview", Label = "Overview", Href = "/overview", Active = true },
                        new() { Id = "drafts", Label = "Drafts", Href = "/drafts" }
                    }
                };
                if (constrained) sidebar.Width = collapsed ? 64 : 220;
                Canvas.SetLeft(sidebar, 20); Canvas.SetTop(sidebar, 20);
                root.Children.Add(sidebar);
                await LayoutAsync(root);
                var navigation = Descendants<ScrollViewer>(sidebar).Single(item => item.Name == "PART_Navigation");
                var links = Descendants<HyperlinkButton>(sidebar).Where(link => link.Tag is BleeckerSidebarItem).ToArray();
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    sidebar.IsLoaded && sidebar.ActualTheme == theme && links.Length == 2 &&
                    Within(Bounds(sidebar, root), new Rect(0, 0, root.Width, root.Height)) &&
                    Within(Bounds(navigation, sidebar), new Rect(0, 0, sidebar.ActualWidth, sidebar.ActualHeight)),
                    "Sidebar owner or navigation clipped at native 150% scale.");
                foreach (var link in links)
                    Require(link.ActualWidth > 0 && link.ActualHeight > 0 &&
                        Within(Bounds(link, sidebar), new Rect(0, 0, sidebar.ActualWidth, sidebar.ActualHeight)),
                        "Sidebar link clipped at native 150% scale.");
                if (!animationsEnabled && !constrained)
                {
                    sidebar.Collapsed = !collapsed;
                    await LayoutAsync(root);
                    var width = sidebar.ActualWidth;
                    Require(Math.Abs(width - (collapsed ? 256 : 64)) < 1,
                        "Sidebar did not snap to the reduced-motion target width.");
                    await Task.Delay(250);
                    await LayoutAsync(root);
                    Require(Math.Abs(sidebar.ActualWidth - width) < .01,
                        "Sidebar width continued animating with reduced motion.");
                }
                cases.Add(new { theme = theme.ToString(), collapsed, rtl, constrained,
                    scale = root.XamlRoot.RasterizationScale, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled, reducedMotionStaticVerified = !animationsEnabled,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerSidebar).Assembly.Location)))
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
