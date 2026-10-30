using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard.Testing;

internal static class SearchInputScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 640, Height = 240 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.Activate();
            var animationsEnabled = new UISettings().AnimationsEnabled;
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var width in new[] { 320, 640 })
            foreach (var size in Enum.GetValues<BleeckerInputSize>())
            foreach (var hasError in new[] { false, true })
            {
                stage = $"{theme}/{width}/{size}/{hasError}/150-percent";
                root.Width = width; root.RequestedTheme = theme;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(width * 3 / 2, 360));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, width * 3 / 2, 360));
                var control = new BleeckerSearchInput
                {
                    Width = width - 40, Text = "Fictional local query", InputSize = size,
                    IsError = hasError, PlaceholderText = "Search local items"
                };
                control.ClearRequested += (_, _) => { };
                Canvas.SetLeft(control, 20); Canvas.SetTop(control, 20);
                root.Children.Add(control);
                await LayoutAsync(root);
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    control.IsLoaded && control.ActualTheme == theme &&
                    ReferenceEquals(control.Editor, Descendants<BleeckerInput>(control).Single()) &&
                    Within(Bounds(control, root), new Rect(0, 0, root.Width, root.Height)),
                    "Search Input owner or editor failed at native 150% scale.");
                var clear = Descendants<FrameworkElement>(control).Single(item => item.Name == "ClearSearch");
                Require(clear.Visibility == Visibility.Visible && clear.ActualWidth > 0 &&
                    Within(Bounds(control.Editor, control), new Rect(0, 0, control.ActualWidth, control.ActualHeight)) &&
                    Within(Bounds(clear, control), new Rect(0, 0, control.ActualWidth, control.ActualHeight)),
                    "Search Input editor or clear action clipped at native 150% scale.");
                if (!animationsEnabled)
                {
                    var before = Bounds(clear, control);
                    await Task.Delay(120);
                    await LayoutAsync(root);
                    Require(Bounds(clear, control).Equals(before),
                        "Search Input clear action kept moving with reduced motion.");
                }
                cases.Add(new { theme = theme.ToString(), width, size = size.ToString(), hasError,
                    scale = root.XamlRoot.RasterizationScale, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled, reducedMotionStaticVerified = !animationsEnabled,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerSearchInput).Assembly.Location)))
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
