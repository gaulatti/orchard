using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard.Testing;

internal static class ScrollAreaScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 600, Height = 300 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900, 450));
            island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, 900, 450));
            window.Activate();
            var animationsEnabled = new UISettings().AnimationsEnabled;
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var mode in new[] { "vertical", "horizontal", "both", "rtl", "empty" })
            {
                stage = $"{theme}/{mode}/150-percent";
                root.RequestedTheme = theme;
                var content = mode == "empty" ? null : new Border
                {
                    Width = 520, Height = 480,
                    Child = new TextBlock { Text = "Fictional local content", TextWrapping = TextWrapping.Wrap }
                };
                var area = new BleeckerScrollArea
                {
                    Width = mode == "empty" ? 220 : 256,
                    Height = mode == "empty" ? 80 : 180,
                    Content = content,
                    Orientation = mode switch
                    {
                        "horizontal" => BleeckerScrollAreaOrientation.Horizontal,
                        "both" or "rtl" => BleeckerScrollAreaOrientation.Both,
                        _ => BleeckerScrollAreaOrientation.Vertical
                    },
                    FlowDirection = mode == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
                };
                Canvas.SetLeft(area, 20); Canvas.SetTop(area, 20);
                root.Children.Add(area);
                await LayoutAsync(root);
                var viewport = Descendants<ScrollView>(area).Single(view => view.Name == "ScrollAreaViewport");
                var interior = new Rect(0, 0, area.ActualWidth, area.ActualHeight);
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    area.IsLoaded && area.ActualTheme == theme &&
                    Math.Abs(area.ActualWidth - area.Width) < .5 &&
                    Math.Abs(area.ActualHeight - area.Height) < .5 &&
                    Within(Bounds(area, root), new Rect(0, 0, root.Width, root.Height)) &&
                    Within(Bounds(viewport, area), interior),
                    "ScrollArea frame or viewport clipped at native 150% scale.");
                Require(area.ViewportWidth > 0 && area.ViewportHeight > 0 &&
                    area.ViewportWidth <= area.ActualWidth + .5 &&
                    area.ViewportHeight <= area.ActualHeight + .5,
                    "ScrollArea viewport dimensions exceed its owner.");
                if (content is not null)
                {
                    Require(ReferenceEquals(area.Content, content) &&
                        area.ExtentWidth > area.ViewportWidth && area.ExtentHeight > area.ViewportHeight,
                        "ScrollArea lost original overflowing content.");
                    if (!animationsEnabled)
                    {
                        area.ScrollTo(0, 80);
                        await LayoutAsync(root);
                        var after = area.VerticalOffset;
                        await Task.Delay(120);
                        await LayoutAsync(root);
                        Require(after > 0 && Math.Abs(area.VerticalOffset - after) < .01,
                            "ScrollArea moved after a reduced-motion scroll request.");
                    }
                }
                cases.Add(new { theme = theme.ToString(), mode, scale = root.XamlRoot.RasterizationScale,
                    width = area.ActualWidth, height = area.ActualHeight, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = true, cases,
                animationsEnabled, reducedMotionStaticVerified = !animationsEnabled,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerScrollArea).Assembly.Location))) }));
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
        for (var index = 0; index < 4; index++)
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
