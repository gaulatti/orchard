using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard.Testing;

internal static class PanelScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 800, Height = 520 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.Activate();
            var animationsEnabled = new UISettings().AnimationsEnabled;
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var viewport in new[] { 400, 800 })
            foreach (var withSlots in new[] { false, true })
            {
                stage = $"{theme}/{viewport}/{withSlots}/150-percent";
                root.Width = viewport; root.RequestedTheme = theme;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(viewport * 3 / 2, 780));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, viewport * 3 / 2, 780));
                var input = new BleeckerInput { Text = "Fictional local value" };
                var body = new StackPanel { Children = { input, new Border { Height = 600 } } };
                var filter = new BleeckerInput { Text = "Local filter" };
                var action = new BleeckerButton { Content = "Local action" };
                var panel = new BleeckerPanel
                {
                    Style = (Style)Application.Current.Resources[typeof(BleeckerPanel)],
                    Title = "A local panel title that may wrap", Count = 12,
                    Content = body, Filter = withSlots ? filter : null,
                    Toolbar = withSlots ? action : null,
                    Width = viewport == 400 ? 360 : 640, Height = 420
                };
                Canvas.SetLeft(panel, 20); Canvas.SetTop(panel, 20);
                root.Children.Add(panel);
                await LayoutAsync(root);
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    panel.IsLoaded && panel.ActualTheme == theme && ReferenceEquals(panel.Content, body) &&
                    Within(Bounds(panel, root), new Rect(0, 0, root.Width, root.Height)),
                    "Panel owner clipped or lost caller content at native 150% scale.");
                foreach (var name in new[] { "PART_ClipRoot", "PART_Root", "PanelHeader", "PanelRow", "PanelCount", "PanelFeed", "PanelFilter", "PanelToolbar" })
                {
                    var part = Descendants<FrameworkElement>(panel).Single(item => item.Name == name);
                    if (part.Visibility != Visibility.Visible || part.ActualWidth == 0 || part.ActualHeight == 0) continue;
                    Require(Within(Bounds(part, panel), new Rect(0, 0, panel.ActualWidth, panel.ActualHeight)),
                        $"Panel {name} clipped at native 150% scale.");
                }
                if (withSlots)
                    Require(ReferenceEquals(panel.Filter, filter) && ReferenceEquals(panel.Toolbar, action),
                        "Panel lost the caller's filter or action.");
                if (!animationsEnabled)
                {
                    var accent = Descendants<Border>(panel).Single(item => item.Name == "PART_LeftAccent");
                    var before = ((SolidColorBrush)accent.Background).Color;
                    panel.Accent = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 180, 60, 40));
                    await LayoutAsync(root);
                    var after = ((SolidColorBrush)accent.Background).Color;
                    Require(after != before, "Panel accent did not update with reduced motion.");
                    await Task.Delay(120);
                    await LayoutAsync(root);
                    Require(((SolidColorBrush)accent.Background).Color == after,
                        "Panel accent continued animating with reduced motion.");
                }
                cases.Add(new { theme = theme.ToString(), viewport, withSlots,
                    scale = root.XamlRoot.RasterizationScale, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled, reducedMotionSnapVerified = !animationsEnabled,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerPanel).Assembly.Location)))
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
