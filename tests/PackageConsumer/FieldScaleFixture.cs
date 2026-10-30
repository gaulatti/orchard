using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard.Testing;

internal static class FieldScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 640, Height = 480 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.Activate();
            var animationsEnabled = new UISettings().AnimationsEnabled;
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var width in new[] { 320, 640 })
            foreach (var hasError in new[] { false, true })
            {
                stage = $"{theme}/{width}/{hasError}/150-percent";
                root.Width = width; root.RequestedTheme = theme;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(width * 3 / 2, 720));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, width * 3 / 2, 720));
                var input = new BleeckerInput { Text = "Fictional local title" };
                var field = new BleeckerField
                {
                    Label = "Coverage title", Description = "Use a concise title for the local preview.",
                    Error = hasError ? "Check the local value" : null,
                    Required = true, Content = input, Width = width == 320 ? 280 : 480,
                    Style = (Style)Application.Current.Resources[typeof(BleeckerField)]
                };
                Canvas.SetLeft(field, 20); Canvas.SetTop(field, 20);
                root.Children.Add(field);
                await LayoutAsync(root);
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    field.IsLoaded && field.ActualTheme == theme && ReferenceEquals(field.Content, input) &&
                    Within(Bounds(field, root), new Rect(0, 0, root.Width, root.Height)),
                    "Field owner clipped or lost caller input at native 150% scale.");
                foreach (var name in new[] { "FieldHeader", "FieldLabel", "FieldLabelText", "FieldControl", "FieldDescription", "FieldError" })
                {
                    var part = Descendants<FrameworkElement>(field).Single(item => item.Name == name);
                    if (part.Visibility != Visibility.Visible || part.ActualWidth == 0 || part.ActualHeight == 0) continue;
                    Require(Within(Bounds(part, field), new Rect(0, 0, field.ActualWidth, field.ActualHeight)),
                        $"Field {name} clipped at native 150% scale.");
                }
                Require(Within(Bounds(input, field), new Rect(0, 0, field.ActualWidth, field.ActualHeight)),
                    "Caller input clipped inside Field at native 150% scale.");
                if (!animationsEnabled)
                {
                    var before = field.ActualHeight;
                    await Task.Delay(120);
                    await LayoutAsync(root);
                    Require(Math.Abs(field.ActualHeight - before) < .01,
                        "Field layout continued moving with reduced motion.");
                }
                cases.Add(new { theme = theme.ToString(), width, hasError,
                    scale = root.XamlRoot.RasterizationScale, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled, reducedMotionStaticVerified = !animationsEnabled,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerField).Assembly.Location)))
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
