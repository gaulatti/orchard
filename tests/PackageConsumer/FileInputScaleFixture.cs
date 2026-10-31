using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Storage;

namespace Orchard.Testing;

internal static class FileInputScaleFixture
{
    private static IEnumerable<FrameworkElement> Elements(DependencyObject owner)
    {
        if (owner is FrameworkElement element) yield return element;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++)
            foreach (var child in Elements(VisualTreeHelper.GetChild(owner, i))) yield return child;
    }

    internal static async Task RunAsync(Window window, string output)
    {
        var cases = new List<object>();
        var stage = "initial";
        try
        {
            var folder = Path.GetDirectoryName(output)!;
            var filePath = Path.Combine(folder, "fictional-long-file-name-retaining-extension.txt");
            await File.WriteAllTextAsync(filePath, "Fictional local fixture only.");
            var file = await StorageFile.GetFileFromPathAsync(filePath);
            var root = new Grid();
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            var animationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var width in new[] { 260, 480 })
            foreach (var size in new[] { BleeckerInputSize.Small, BleeckerInputSize.Medium, BleeckerInputSize.Large })
            foreach (var selected in new[] { false, true })
            {
                stage = $"{theme}/{width}/{size}/{selected}";
                const int height = 160;
                root.Width = width;
                root.Height = height;
                root.RequestedTheme = theme;
                var pixels = new SizeInt32((int)(width * 1.5), (int)(height * 1.5));
                window.AppWindow.ResizeClient(pixels);
                island.SiteBridge.MoveAndResize(new RectInt32(0, 0, pixels.Width, pixels.Height));
                var input = new BleeckerFileInput
                {
                    InputSize = size,
                    Width = width - 32,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(16)
                };
                if (selected) input.SetFiles([file]);
                root.Children.Clear();
                root.Children.Add(input);
                for (var i = 0; i < 3; i++)
                {
                    root.Measure(new Size(width, height));
                    root.Arrange(new Rect(0, 0, width, height));
                    root.UpdateLayout();
                    await Task.Delay(16);
                }
                await NativeRenderReadiness.RenderAsync(root);
                if (Math.Abs(root.XamlRoot.RasterizationScale - 1.5) > .0001)
                    throw new InvalidOperationException("File Input did not use native 150% rasterization.");
                var bounds = new List<object>();
                foreach (var name in new[] { "FileRoot", "FileChrome", "FileChooseChrome", "FileChooseText", "FileSelectionText" })
                {
                    var part = Elements(input).Single(item => item.Name == name);
                    var box = part.TransformToVisual(root).TransformBounds(new Rect(0, 0, part.ActualWidth, part.ActualHeight));
                    if (box.Width <= 0 || box.Height <= 0 || box.X < 0 || box.Y < 0 || box.Right > width + .5 || box.Bottom > height + .5)
                        throw new InvalidOperationException($"File Input {name} clipped at 150%: {box}.");
                    bounds.Add(new { name, box.X, box.Y, box.Width, box.Height });
                }
                var chrome = Elements(input).Single(item => item.Name == "FileChrome");
                var first = chrome.TransformToVisual(root).TransformBounds(new Rect(0, 0, chrome.ActualWidth, chrome.ActualHeight));
                await Task.Delay(100);
                root.UpdateLayout();
                var settled = chrome.TransformToVisual(root).TransformBounds(new Rect(0, 0, chrome.ActualWidth, chrome.ActualHeight));
                if (!first.Equals(settled)) throw new InvalidOperationException("File Input kept moving after it settled.");
                cases.Add(new { theme = theme.ToString(), width, size = size.ToString(), selected,
                    scale = root.XamlRoot.RasterizationScale, bounds, stable = true });
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled, osSettingsChanged = false,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerFileInput).Assembly.Location)))
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
