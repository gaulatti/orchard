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

internal static class NavMenuScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 900, Height = 180 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.Activate();
            var animationsEnabled = new UISettings().AnimationsEnabled;
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var direction in Enum.GetValues<BleeckerNavDirection>())
            foreach (var viewport in new[] { 480, 900 })
            foreach (var rtl in new[] { false, true })
            {
                stage = $"{theme}/{direction}/{viewport}/{rtl}/150-percent";
                root.Width = viewport; root.RequestedTheme = theme;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(viewport * 3 / 2, 270));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, viewport * 3 / 2, 270));
                var menu = new BleeckerNavMenu
                {
                    Style = (Style)Application.Current.Resources[typeof(BleeckerNavMenu)],
                    Width = viewport - 40, Direction = direction,
                    FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                    Items = new ObservableCollection<BleeckerNavItem>
                    {
                        new("#home", "Home"), new("#about", "About"), new("#contact", "Contact")
                    }
                };
                Canvas.SetLeft(menu, 20); Canvas.SetTop(menu, 20);
                root.Children.Add(menu);
                await LayoutAsync(root);
                var links = Descendants<HyperlinkButton>(menu).Where(link => link.Name == "NavLink").ToArray();
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    menu.IsLoaded && menu.ActualTheme == theme && links.Length == 3 &&
                    Within(Bounds(menu, root), new Rect(0, 0, root.Width, root.Height)),
                    "Nav Menu owner clipped at native 150% scale.");
                foreach (var link in links)
                    Require(link.ActualWidth > 0 && link.ActualHeight > 0 &&
                        Within(Bounds(link, menu), new Rect(0, 0, menu.ActualWidth, menu.ActualHeight)),
                        "Nav Menu link clipped at native 150% scale.");
                if (!animationsEnabled)
                {
                    var before = links.Select(link => Bounds(link, menu)).ToArray();
                    var originalColors = links.Select(link => (link.Foreground as SolidColorBrush)?.Color.ToString()).ToArray();
                    root.RequestedTheme = theme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
                    await LayoutAsync(root);
                    var colors = links.Select(link => (link.Foreground as SolidColorBrush)?.Color.ToString()).ToArray();
                    Require(colors.Where((color, index) => color != originalColors[index]).Any(),
                        "Nav Menu theme change did not repaint its links.");
                    await Task.Delay(250);
                    await LayoutAsync(root);
                    Require(links.Select((link, index) => Bounds(link, menu).Equals(before[index]) &&
                        (link.Foreground as SolidColorBrush)?.Color.ToString() == colors[index]).All(value => value),
                        "Nav Menu links continued moving or repainting with reduced motion.");
                    root.RequestedTheme = theme;
                    await LayoutAsync(root);
                }
                cases.Add(new { theme = theme.ToString(), direction = direction.ToString(), viewport, rtl,
                    scale = root.XamlRoot.RasterizationScale, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled, reducedMotionStaticVerified = !animationsEnabled,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerNavMenu).Assembly.Location)))
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
