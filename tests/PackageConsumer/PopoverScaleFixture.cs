using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Testing;

internal static class PopoverScaleFixture
{
    private static IEnumerable<FrameworkElement> Elements(DependencyObject owner)
    {
        if (owner is FrameworkElement element) yield return element;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(owner); index++)
            foreach (var child in Elements(VisualTreeHelper.GetChild(owner, index))) yield return child;
    }

    internal static async Task RunAsync(Window window, string output)
    {
        var cases = new List<object>();
        var stage = "initial";
        try
        {
            var root = new Grid();
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            var animationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var width in new[] { 320, 480 })
            {
                stage = $"{theme}/{width}";
                const int height = 240;
                root.RequestedTheme = theme;
                root.Width = width;
                root.Height = height;
                var pixels = new SizeInt32((int)(width * 1.5), (int)(height * 1.5));
                window.AppWindow.ResizeClient(pixels);
                island.SiteBridge.MoveAndResize(new RectInt32(0, 0, pixels.Width, pixels.Height));
                var text = new TextBlock { Text = "Fictional local settings", TextWrapping = TextWrapping.Wrap };
                var editor = new TextBox { Width = 160, Text = "Original caller value" };
                var content = new StackPanel { Width = 180, Spacing = 8 };
                content.Children.Add(text);
                content.Children.Add(editor);
                var popover = new BleeckerPopover
                {
                    Trigger = new Button { Content = "Open settings" },
                    Content = content,
                    Open = true,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(40, 32, 0, 0)
                };
                root.Children.Clear();
                root.Children.Add(popover);
                Popup? popup = null;
                FrameworkElement? surface = null;
                for (var attempt = 0; attempt < 60; attempt++)
                {
                    root.Measure(new Size(width, height));
                    root.Arrange(new Rect(0, 0, width, height));
                    root.UpdateLayout();
                    popup = Elements(popover).OfType<Popup>().FirstOrDefault();
                    surface = popup?.Child is { } child ? Elements(child).FirstOrDefault(item => item.Name == "PopoverSurface") : null;
                    if (popup?.IsOpen == true && surface?.IsLoaded == true && surface.ActualWidth > 0) break;
                    await Task.Delay(25);
                }
                if (popup?.IsOpen != true || surface?.IsLoaded != true)
                    throw new InvalidOperationException("Popover did not present its original content.");
                if (Math.Abs(root.XamlRoot.RasterizationScale - 1.5) > .0001 ||
                    Math.Abs(surface.XamlRoot.RasterizationScale - 1.5) > .0001)
                    throw new InvalidOperationException("Popover did not render at native 150% scale.");
                await Task.Delay(150);
                root.UpdateLayout();
                var bounds = surface.TransformToVisual(root).TransformBounds(new Rect(0, 0, surface.ActualWidth, surface.ActualHeight));
                var textBounds = text.TransformToVisual(root).TransformBounds(new Rect(0, 0, text.ActualWidth, text.ActualHeight));
                if (bounds.Width <= 0 || bounds.Height <= 0 || bounds.X < 0 || bounds.Y < 0 ||
                    bounds.Right > width + .5 || bounds.Bottom > height + .5 ||
                    textBounds.X < bounds.X - .5 || textBounds.Y < bounds.Y - .5 ||
                    textBounds.Right > bounds.Right + .5 || textBounds.Bottom > bounds.Bottom + .5)
                    throw new InvalidOperationException($"Popover clipped at native 150%: surface={bounds}; text={textBounds}.");
                var original = bounds;
                await Task.Delay(100);
                root.UpdateLayout();
                var settled = surface.TransformToVisual(root).TransformBounds(new Rect(0, 0, surface.ActualWidth, surface.ActualHeight));
                if (!original.Equals(settled)) throw new InvalidOperationException($"Popover kept moving after it settled: before={original}; after={settled}.");
                if (!ReferenceEquals(popover.Content, content) || editor.Text != "Original caller value")
                    throw new InvalidOperationException("Popover replaced or changed caller content.");
                cases.Add(new { theme = theme.ToString(), width, scale = root.XamlRoot.RasterizationScale,
                    surface = new { bounds.X, bounds.Y, bounds.Width, bounds.Height }, stable = true });
                popover.Open = false;
                root.Children.Clear();
                await Task.Delay(30);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled, osSettingsChanged = false,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerPopover).Assembly.Location)))
            }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close();
            Environment.Exit(0);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = false, stage, error = error.ToString(), cases }));
            window.Close();
            Environment.Exit(1);
        }
    }
}
