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

internal static class TabsScaleFixture
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
            foreach (var variant in Enum.GetValues<BleeckerTabsVariant>())
            foreach (var size in Enum.GetValues<BleeckerControlSize>())
            foreach (var viewport in new[] { 480, 700 })
            {
                stage = $"{theme}/{variant}/{size}/{viewport}/150-percent";
                root.Width = viewport; root.RequestedTheme = theme;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(viewport * 3 / 2, 270));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, viewport * 3 / 2, 270));
                var tabs = new BleeckerTabs
                {
                    Style = (Style)Application.Current.Resources[typeof(BleeckerTabs)],
                    Width = viewport - 40, Variant = variant, Size = size, Stretch = true,
                    ActiveTab = "activity",
                    Items = new ObservableCollection<BleeckerTabItem>
                    {
                        new("overview", "Overview"), new("activity", "Activity"), new("settings", "Settings")
                    }
                };
                Canvas.SetLeft(tabs, 20); Canvas.SetTop(tabs, 20);
                root.Children.Add(tabs);
                await LayoutAsync(root);
                var buttons = Descendants<Button>(tabs).ToArray();
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    tabs.IsLoaded && tabs.ActualTheme == theme && buttons.Length == 3 &&
                    Within(Bounds(tabs, root), new Rect(0, 0, root.Width, root.Height)),
                    "Tabs owner clipped at native 150% scale.");
                foreach (var button in buttons)
                    Require(button.ActualWidth > 0 && button.ActualHeight > 0 &&
                        Within(Bounds(button, tabs), new Rect(0, 0, tabs.ActualWidth, tabs.ActualHeight)),
                        "Tab button clipped at native 150% scale.");
                if (!animationsEnabled)
                {
                    var before = buttons.Select(button => Bounds(button, tabs)).ToArray();
                    tabs.ActiveTab = "overview";
                    await LayoutAsync(root);
                    var paint = buttons.Select(Paint).ToArray();
                    await Task.Delay(220);
                    await LayoutAsync(root);
                    Require(buttons.Select((button, index) => Bounds(button, tabs).Equals(before[index])).All(value => value),
                        "Tab buttons continued moving with reduced motion.");
                    Require(buttons.Select((button, index) => Paint(button) == paint[index]).All(value => value),
                        "Tab selection paint continued animating with reduced motion.");
                }
                cases.Add(new { theme = theme.ToString(), variant = variant.ToString(),
                    size = size.ToString(), viewport, scale = root.XamlRoot.RasterizationScale, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled, reducedMotionStaticVerified = !animationsEnabled,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerTabs).Assembly.Location)))
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
    private static string Paint(Button button)
    {
        var border = Descendants<Border>(button).Single(item => item.Name == "TabBorder");
        static string BrushValue(Brush? value) => value is SolidColorBrush brush
            ? $"{brush.Color}/{brush.Opacity:R}" : value?.GetType().Name ?? "null";
        return $"{BrushValue(border.Background)}/{BrushValue(border.BorderBrush)}";
    }
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
