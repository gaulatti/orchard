using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Testing;

internal static class AdminShellScaleFixture
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
            var root = new Grid();
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            var animationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var width in new[] { 640, 1280 })
            foreach (var explicitPadding in new[] { false, true })
            {
                stage = $"{theme}/{width}/{explicitPadding}";
                const int height = 720;
                root.Width = width;
                root.Height = height;
                root.RequestedTheme = theme;
                var pixels = new SizeInt32((int)(width * 1.5), (int)(height * 1.5));
                window.AppWindow.ResizeClient(pixels);
                island.SiteBridge.MoveAndResize(new RectInt32(0, 0, pixels.Width, pixels.Height));
                var body = new StackPanel { Spacing = 12 };
                body.Children.Add(new TextBlock { Text = "Fictional monitoring and editorial components", TextWrapping = TextWrapping.Wrap });
                body.Children.Add(new TextBox { Text = "Fictional local note", Width = 240, HorizontalAlignment = HorizontalAlignment.Left });
                body.Children.Add(new Button { Content = "Local action" });
                var shell = new BleeckerAdminShell
                {
                    Header = "Orchard Atelier", Sidebar = new StackPanel { Width = 160, Children = { new TextBlock { Text = "Fictional sidebar" } } }, Content = body,
                    Footer = "Fictional local component demo", Height = height, MinHeight = 0
                };
                if (explicitPadding) shell.ContentPadding = new Thickness(0);
                root.Children.Clear();
                root.Children.Add(shell);
                for (var i = 0; i < 3; i++)
                {
                    root.Measure(new Size(width, height));
                    root.Arrange(new Rect(0, 0, width, height));
                    root.UpdateLayout();
                    await Task.Delay(16);
                }
                await NativeRenderReadiness.RenderAsync(root);
                if (Math.Abs(root.XamlRoot.RasterizationScale - 1.5) > .0001)
                    throw new InvalidOperationException("Admin Shell did not use native 150% rasterization.");
                var parts = new[] { "AdminShellHeader", "AdminShellSidebar", "AdminShellMain", "AdminShellContent", "AdminShellFooter" };
                var bounds = new List<object>();
                foreach (var name in parts)
                {
                    var part = Elements(shell).Single(item => item.Name == name);
                    var box = part.TransformToVisual(root).TransformBounds(new Rect(0, 0, part.ActualWidth, part.ActualHeight));
                    CheckBox(box, width, height, name);
                    bounds.Add(new { name, box.X, box.Y, box.Width, box.Height });
                }
                var visibleText = 0;
                var textBounds = new List<object>();
                foreach (var text in Elements(shell).OfType<TextBlock>().Where(item => item.Visibility == Visibility.Visible && !string.IsNullOrEmpty(item.Text)))
                {
                    var box = text.TransformToVisual(root).TransformBounds(new Rect(0, 0, text.ActualWidth, text.ActualHeight));
                    if (box.Width <= 0 || box.Height <= 0 || box.X < -0.5 || box.Y < -2.5 || box.Right > width + 0.5 || box.Bottom > height + 0.5)
                        throw new InvalidOperationException($"Admin Shell {text.Name} visible text clipped at 150%: {box}.");
                    textBounds.Add(new { text.Name, box.X, box.Y, box.Width, box.Height });
                    visibleText++;
                }
                if (visibleText < 3) throw new InvalidOperationException("Admin Shell did not render its slot and caller text.");
                var bitmap = new RenderTargetBitmap();
                await bitmap.RenderAsync(root);
                var pixelsBgra = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());
                var topRowInk = new List<int>();
                for (var y = 0; y < 6; y++)
                {
                    var background = (y * bitmap.PixelWidth + bitmap.PixelWidth - 2) * 4;
                    var ink = 0;
                    for (var x = 0; x < Math.Min(bitmap.PixelWidth / 2, 400); x++)
                    {
                        var pixel = (y * bitmap.PixelWidth + x) * 4;
                        if (Enumerable.Range(0, 3).Any(channel => Math.Abs(pixelsBgra[pixel + channel] - pixelsBgra[background + channel]) > 16)) ink++;
                    }
                    topRowInk.Add(ink);
                }
                if (topRowInk.Any(count => count != 0))
                    throw new InvalidOperationException("Admin Shell header ink touched the viewport's top edge at 150%.");
                var content = Elements(shell).Single(item => item.Name == "AdminShellContent");
                var first = content.TransformToVisual(root).TransformBounds(new Rect(0, 0, content.ActualWidth, content.ActualHeight));
                await Task.Delay(100);
                root.UpdateLayout();
                var settled = content.TransformToVisual(root).TransformBounds(new Rect(0, 0, content.ActualWidth, content.ActualHeight));
                if (!first.Equals(settled)) throw new InvalidOperationException("Admin Shell content kept moving after it settled.");
                cases.Add(new { theme = theme.ToString(), width, explicitPadding, scale = root.XamlRoot.RasterizationScale, bounds, textBounds, visibleText, topRowInk, stable = true });
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled, osSettingsChanged = false,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerAdminShell).Assembly.Location)))
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

    private static void CheckBox(Rect box, double width, double height, string name)
    {
        if (box.Width <= 0 || box.Height <= 0 || box.X < -0.5 || box.Y < -0.5 || box.Right > width + 0.5 || box.Bottom > height + 0.5)
            throw new InvalidOperationException($"Admin Shell {name} clipped at 150%: {box}.");
    }
}
