using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Testing;

internal static class AuthShellScaleFixture
{
    private static IEnumerable<TextBlock> Texts(DependencyObject owner)
    {
        if (owner is TextBlock text) yield return text;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++)
            foreach (var child in Texts(VisualTreeHelper.GetChild(owner, i))) yield return child;
    }

    private static T Find<T>(DependencyObject owner, string name) where T : FrameworkElement
    {
        if (owner is T match && match.Name == name) return match;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++)
        {
            try { return Find<T>(VisualTreeHelper.GetChild(owner, i), name); }
            catch (InvalidOperationException) { }
        }
        throw new InvalidOperationException($"Auth Shell part {name} is missing.");
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
            foreach (var layout in new[] { BleeckerAuthShellLayout.Centered, BleeckerAuthShellLayout.Split })
            {
                stage = $"{theme}/{layout}";
                var width = layout == BleeckerAuthShellLayout.Split ? 1280 : 640;
                const int height = 720;
                root.Width = width;
                root.Height = height;
                root.RequestedTheme = theme;
                var pixels = new SizeInt32((int)(width * 1.5), (int)(height * 1.5));
                window.AppWindow.ResizeClient(pixels);
                island.SiteBridge.MoveAndResize(new RectInt32(0, 0, pixels.Width, pixels.Height));
                var body = new StackPanel { Spacing = 12 };
                body.Children.Add(new TextBlock { Text = "Fictional member access in a reusable component", TextWrapping = TextWrapping.Wrap });
                body.Children.Add(new TextBox { Text = "Fictional member", Width = 240, HorizontalAlignment = HorizontalAlignment.Left });
                body.Children.Add(new Button { Content = "Local action" });
                var shell = new BleeckerAuthShell
                {
                    Layout = layout, Side = BleeckerAuthShellSide.End,
                    Brand = "Orchard Atelier", Footer = "Fictional local component demo",
                    Aside = layout == BleeckerAuthShellLayout.Split ? "A place for fictional collections and their readers" : null,
                    Content = body
                };
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
                    throw new InvalidOperationException("Auth Shell did not use native 150% rasterization.");
                var names = layout == BleeckerAuthShellLayout.Split
                    ? new[] { "AuthShellBrand", "AuthShellContent", "AuthShellFooter", "AuthShellAside" }
                    : new[] { "AuthShellBrand", "AuthShellContent", "AuthShellFooter" };
                var bounds = new List<object>();
                foreach (var name in names)
                {
                    var part = Find<FrameworkElement>(shell, name);
                    var box = part.TransformToVisual(root).TransformBounds(new Rect(0, 0, part.ActualWidth, part.ActualHeight));
                    if (box.Width <= 0 || box.Height <= 0 || box.X < -0.5 || box.Y < -0.5 || box.Right > width + 0.5 || box.Bottom > height + 0.5)
                        throw new InvalidOperationException($"Auth Shell {name} clipped at 150%: {box}.");
                    bounds.Add(new { name, box.X, box.Y, box.Width, box.Height });
                }
                var visibleText = 0;
                foreach (var text in Texts(shell).Where(item => item.Visibility == Visibility.Visible && !string.IsNullOrEmpty(item.Text)))
                {
                    var box = text.TransformToVisual(root).TransformBounds(new Rect(0, 0, text.ActualWidth, text.ActualHeight));
                    if (box.Width <= 0 || box.Height <= 0 || box.X < -0.5 || box.Y < -0.5 || box.Right > width + 0.5 || box.Bottom > height + 0.5)
                        throw new InvalidOperationException($"Auth Shell visible text clipped at 150%: {box}.");
                    visibleText++;
                }
                if (visibleText < 3) throw new InvalidOperationException("Auth Shell did not render its caller and slot text.");
                var first = Find<FrameworkElement>(shell, "AuthShellContent");
                var firstBox = first.TransformToVisual(root).TransformBounds(new Rect(0, 0, first.ActualWidth, first.ActualHeight));
                await Task.Delay(100);
                root.UpdateLayout();
                var settled = first.TransformToVisual(root).TransformBounds(new Rect(0, 0, first.ActualWidth, first.ActualHeight));
                if (!firstBox.Equals(settled)) throw new InvalidOperationException("Auth Shell content kept moving after it settled.");
                cases.Add(new { theme = theme.ToString(), layout = layout.ToString(), scale = root.XamlRoot.RasterizationScale, bounds, visibleText, stable = true });
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled, osSettingsChanged = false,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerAuthShell).Assembly.Location)))
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
