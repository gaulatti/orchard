using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard.Testing;

internal static class InputScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 640, Height = 180 };
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
            foreach (var error in new[] { false, true })
            foreach (var adornments in new[] { false, true })
            {
                stage = $"{theme}/{width}/{size}/{error}/{adornments}/150-percent";
                root.Width = width; root.RequestedTheme = theme;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(width * 3 / 2, 270));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, width * 3 / 2, 270));
                var input = new BleeckerInput
                {
                    Width = width - 40, InputSize = size, IsError = error,
                    Text = "Fictional local value", PlaceholderText = "Enter a local value",
                    StartIcon = adornments ? "S" : null, EndIcon = adornments ? "E" : null
                };
                Canvas.SetLeft(input, 20); Canvas.SetTop(input, 20);
                root.Children.Add(input);
                await LayoutAsync(root);
                var viewport = Descendants<FrameworkElement>(input).Single(item => item.Name == "InputTextViewport");
                var editor = Descendants<FrameworkElement>(input).Single(item => item.Name == "ContentElement");
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    input.IsLoaded && input.ActualTheme == theme &&
                    Within(Bounds(input, root), new Rect(0, 0, root.Width, root.Height)) &&
                    viewport.ActualWidth > 0 && editor.ActualWidth > 0 &&
                    Within(Bounds(viewport, input), new Rect(0, 0, input.ActualWidth, input.ActualHeight)) &&
                    Within(Bounds(editor, input), new Rect(0, 0, input.ActualWidth, input.ActualHeight)),
                    "Input text viewport clipped at native 150% scale.");
                if (adornments)
                    foreach (var name in new[] { "StartAdornment", "EndAdornment" })
                    {
                        var adornment = Descendants<FrameworkElement>(input).Single(item => item.Name == name);
                        Require(Within(Bounds(adornment, input), new Rect(0, 0, input.ActualWidth, input.ActualHeight)),
                            $"Input {name} clipped at native 150% scale.");
                    }
                if (!animationsEnabled)
                {
                    var chrome = Descendants<Border>(input).Single(item => item.Name == "BorderElement");
                    input.IsError = !error;
                    await LayoutAsync(root);
                    var paint = Paint(chrome.BorderBrush);
                    await Task.Delay(220);
                    await LayoutAsync(root);
                    Require(Paint(chrome.BorderBrush) == paint,
                        "Input border continued animating with reduced motion.");
                }
                cases.Add(new { theme = theme.ToString(), width, size = size.ToString(), error, adornments,
                    scale = root.XamlRoot.RasterizationScale, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled, reducedMotionStaticVerified = !animationsEnabled,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerInput).Assembly.Location)))
            }));
            window.Close(); Environment.Exit(0);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = false, stage, error = error.ToString() }));
            window.Close(); Environment.Exit(1);
        }
    }

    private static string Paint(Brush? value) => value is SolidColorBrush brush
        ? $"{brush.Color}/{brush.Opacity:R}" : value?.GetType().Name ?? "null";
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
