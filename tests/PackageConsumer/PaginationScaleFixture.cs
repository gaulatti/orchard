using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard.Testing;

internal static class PaginationScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 700, Height = 180 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.Activate();
            var animationsEnabled = new UISettings().AnimationsEnabled;
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var viewport in new[] { 400, 700 })
            foreach (var page in new[] { 1d, 5d, 9d })
            foreach (var rtl in new[] { false, true })
            {
                stage = $"{theme}/{viewport}/{page}/{rtl}/150-percent";
                root.Width = viewport; root.RequestedTheme = theme;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(viewport * 3 / 2, 270));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, viewport * 3 / 2, 270));
                var pagination = new BleeckerPagination
                {
                    Style = (Style)Application.Current.Resources[typeof(BleeckerPagination)],
                    Width = viewport - 40, CurrentPage = page, TotalPages = 9,
                    HasPrevPage = page > 1, HasNextPage = page < 9,
                    FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
                };
                Canvas.SetLeft(pagination, 20); Canvas.SetTop(pagination, 20);
                root.Children.Add(pagination);
                await LayoutAsync(root);
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    pagination.IsLoaded && pagination.ActualTheme == theme &&
                    Within(Bounds(pagination, root), new Rect(0, 0, root.Width, root.Height)),
                    "Pagination owner clipped at native 150% scale.");
                var row = Descendants<FrameworkElement>(pagination).Single(item => item.Name == "PaginationNavigation");
                var buttons = Descendants<Button>(pagination).ToArray();
                Require(buttons.Length >= 3 &&
                    Within(Bounds(row, pagination), new Rect(0, 0, pagination.ActualWidth, pagination.ActualHeight)),
                    "Pagination row clipped or lost its actions at native 150% scale.");
                foreach (var button in buttons)
                    Require(button.ActualWidth > 0 && button.ActualHeight > 0 &&
                        Within(Bounds(button, pagination), new Rect(0, 0, pagination.ActualWidth, pagination.ActualHeight)),
                        "Pagination action clipped at native 150% scale.");
                if (!animationsEnabled)
                {
                    var before = buttons.Select(button => Bounds(button, pagination)).ToArray();
                    await Task.Delay(120);
                    await LayoutAsync(root);
                    Require(buttons.Select((button, index) => Bounds(button, pagination).Equals(before[index])).All(value => value),
                        "Pagination actions continued moving with reduced motion.");
                }
                cases.Add(new { theme = theme.ToString(), viewport, page, rtl,
                    scale = root.XamlRoot.RasterizationScale, actionCount = buttons.Length, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled, reducedMotionStaticVerified = !animationsEnabled,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerPagination).Assembly.Location)))
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
