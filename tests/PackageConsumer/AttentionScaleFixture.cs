using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Testing;

internal static class AttentionScaleFixture
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
            var attentionStyles = new ResourceDictionary
            {
                Source = new Uri("ms-appx:///Orchard/Themes/Attention.xaml")
            };
            root.Resources.MergedDictionaries.Add(attentionStyles);
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            var animationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var width in new[] { 260, 420 })
            foreach (var element in new[] { BleeckerAttentionElement.Article, BleeckerAttentionElement.Button })
            {
                stage = $"{theme}/{width}/{element}";
                const int height = 240;
                root.RequestedTheme = theme;
                root.Width = width;
                root.Height = height;
                var pixels = new SizeInt32((int)(width * 1.5), (int)(height * 1.5));
                window.AppWindow.ResizeClient(pixels);
                island.SiteBridge.MoveAndResize(new RectInt32(0, 0, pixels.Width, pixels.Height));
                var surface = new BleeckerAttentionSurface
                {
                    Hue = 48,
                    Intensity = 8,
                    Style = (Style)attentionStyles[typeof(BleeckerAttentionSurface)],
                    Element = element,
                    Density = width == 260 ? BleeckerAttentionDensity.Compact : BleeckerAttentionDensity.Comfortable,
                    Content = "Fictional local attention content with a longer label",
                    Width = width - 32,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(16)
                };
                root.Children.Clear();
                root.Children.Add(surface);
                for (var attempt = 0; attempt < 8; attempt++)
                {
                    root.Measure(new Size(width, height));
                    root.Arrange(new Rect(0, 0, width, height));
                    root.UpdateLayout();
                    if (surface.IsLoaded && surface.Background is ImageBrush { ImageSource: not null }) break;
                    await Task.Delay(25);
                }
                await NativeRenderReadiness.RenderAsync(root);
                if (Math.Abs(root.XamlRoot.RasterizationScale - 1.5) > .0001)
                    throw new InvalidOperationException("Attention Surface did not use native 150% scale.");
                var parts = Elements(surface).ToArray();
                var paint = parts.Single(item => item.Name == "PART_Surface");
                var text = parts.Single(item => item.Name == "PART_String");
                var bounds = surface.TransformToVisual(root).TransformBounds(new Rect(0, 0, surface.ActualWidth, surface.ActualHeight));
                var paintBounds = paint.TransformToVisual(root).TransformBounds(new Rect(0, 0, paint.ActualWidth, paint.ActualHeight));
                var textBounds = text.TransformToVisual(root).TransformBounds(new Rect(0, 0, text.ActualWidth, text.ActualHeight));
                if (bounds.Width <= 0 || bounds.Height <= 0 || bounds.X < 0 || bounds.Y < 0 ||
                    bounds.Right > width + .5 || bounds.Bottom > height + .5 ||
                    paintBounds.X < bounds.X - .5 || paintBounds.Y < bounds.Y - .5 ||
                    paintBounds.Right > bounds.Right + .5 || paintBounds.Bottom > bounds.Bottom + .5 ||
                    textBounds.X < paintBounds.X - .5 || textBounds.Y < paintBounds.Y - .5 ||
                    textBounds.Right > paintBounds.Right + .5 || textBounds.Bottom > paintBounds.Bottom + .5)
                    throw new InvalidOperationException($"Attention Surface clipped at 150%: owner={bounds}; paint={paintBounds}; text={textBounds}.");
                var first = bounds;
                await Task.Delay(100);
                root.UpdateLayout();
                var settled = surface.TransformToVisual(root).TransformBounds(new Rect(0, 0, surface.ActualWidth, surface.ActualHeight));
                if (!first.Equals(settled)) throw new InvalidOperationException($"Attention Surface kept moving after layout: {first} to {settled}.");
                cases.Add(new { theme = theme.ToString(), width, element = element.ToString(), scale = root.XamlRoot.RasterizationScale,
                    bounds = new { bounds.X, bounds.Y, bounds.Width, bounds.Height }, stable = true });
                root.Children.Clear();
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled, osSettingsChanged = false,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerAttentionSurface).Assembly.Location)))
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
