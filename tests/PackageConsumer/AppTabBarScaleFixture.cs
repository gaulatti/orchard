using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard.Testing;

internal static class AppTabBarScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 600, Height = 220 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900, 330));
            island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, 900, 330));
            window.Activate();
            var animationsEnabled = new UISettings().AnimationsEnabled;
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var mode in new[] { "normal", "narrow", "large", "rtl" })
            {
                stage = $"{theme}/{mode}/150-percent";
                root.RequestedTheme = theme;
                var bar = new BleeckerAppTabBar
                {
                    Tabs = [new("overview", mode == "narrow" ? "Daily fictional coverage review" : "Overview", Icon(Symbol.Home), 100),
                            new("queue", mode == "narrow" ? "International fictional queue" : "Queue", Icon(Symbol.Mail), 7)],
                    SelectedValue = "overview",
                    Width = mode == "narrow" ? 120 : 480,
                    FlowDirection = mode == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
                };
                if (mode == "large") bar.FontSize = 20;
                Canvas.SetLeft(bar, 20); Canvas.SetTop(bar, 20);
                root.Children.Add(bar);
                await LayoutAsync(root);
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 && bar.IsLoaded &&
                    bar.ActualTheme == theme && bar.ActualHeight >= 59 &&
                    Within(Bounds(bar, root), new Rect(0, 0, root.Width, root.Height)),
                    "App Tab Bar did not fit at native 150% scale.");
                var buttons = Descendants<Button>(bar).Where(button => button.DataContext is BleeckerAppTab).ToArray();
                Require(buttons.Length == 2, "App Tab Bar lost destinations.");
                foreach (var button in buttons)
                {
                    Require(button.ActualHeight >= 54 && Within(Bounds(button, bar), new Rect(0, 0, bar.ActualWidth, bar.ActualHeight)),
                        "App Tab Bar destination clipped.");
                    foreach (var part in Descendants<FrameworkElement>(button).Where(item =>
                        item.Name is "AppTabIcon" or "AppTabLabel" or "AppTabBadge"))
                        Require(part.ActualWidth > 0 && part.ActualHeight > 0 && Within(Bounds(part, bar),
                            new Rect(0, 0, bar.ActualWidth, bar.ActualHeight)),
                            $"App Tab Bar {part.Name} clipped.");
                }
                if (!animationsEnabled)
                {
                    var before = Bounds(buttons[0], bar);
                    await Task.Delay(120);
                    await LayoutAsync(root);
                    Require(Close(before, Bounds(buttons[0], bar)), "App Tab Bar shifted with reduced motion.");
                }
                cases.Add(new { theme = theme.ToString(), mode, scale = root.XamlRoot.RasterizationScale,
                    width = bar.ActualWidth, height = bar.ActualHeight, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = true, cases,
                animationsEnabled, reducedMotionStaticVerified = !animationsEnabled,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerAppTabBar).Assembly.Location))) }));
            window.Close(); Environment.Exit(0);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = false, stage, error = error.ToString() }));
            window.Close(); Environment.Exit(1);
        }
    }

    private static FontIconSource Icon(Symbol symbol) => new() { Glyph = char.ConvertFromUtf32((int)symbol) };
    private static Rect Bounds(FrameworkElement element, FrameworkElement parent) =>
        element.TransformToVisual(parent).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
    private static bool Within(Rect inner, Rect outer) => inner.X >= outer.X - .5 && inner.Y >= outer.Y - .5 &&
        inner.Right <= outer.Right + .5 && inner.Bottom <= outer.Bottom + .5;
    private static bool Close(Rect first, Rect second) => Math.Abs(first.X - second.X) < .01 &&
        Math.Abs(first.Y - second.Y) < .01 && Math.Abs(first.Width - second.Width) < .01 &&
        Math.Abs(first.Height - second.Height) < .01;
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
