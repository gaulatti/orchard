using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Testing;

internal static class EdgeDialogScaleFixture
{
    private static IEnumerable<FrameworkElement> Elements(DependencyObject owner)
    {
        if (owner is FrameworkElement element) yield return element;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(owner); index++)
            foreach (var child in Elements(VisualTreeHelper.GetChild(owner, index))) yield return child;
    }

    private static Rect Box(FrameworkElement element, FrameworkElement root) =>
        element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

    private static void Inside(Rect box, int width, int height, string name)
    {
        if (box.Width <= 0 || box.Height <= 0 || box.X < -.5 || box.Y < -.5 || box.Right > width + .5 || box.Bottom > height + .5)
            throw new InvalidOperationException($"{name} clipped at native 150%: {box}.");
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
            var content = new Grid();
            var host = new BleeckerModalHost { Content = content };
            root.Children.Add(host);
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var width in new[] { 420, 600 })
            foreach (var kind in new[] { "Drawer", "Left", "Right", "Top", "Bottom" })
            {
                stage = $"{theme}/{width}/{kind}";
                const int height = 340;
                root.RequestedTheme = theme;
                root.Width = width;
                root.Height = height;
                host.Width = width;
                host.Height = height;
                var pixels = new SizeInt32((int)(width * 1.5), (int)(height * 1.5));
                window.AppWindow.ResizeClient(pixels);
                island.SiteBridge.MoveAndResize(new RectInt32(0, 0, pixels.Width, pixels.Height));
                BleeckerEdgeDialog dialog = kind == "Drawer" ? new BleeckerDrawer() :
                    new BleeckerSheet { Side = Enum.Parse<BleeckerSheetSide>(kind) };
                dialog.Title = "Fictional component example";
                dialog.Content = new TextBlock { Name = "ScaleBody", Text = "Review the local component example before continuing.", TextWrapping = TextWrapping.Wrap };
                content.Children.Add(dialog);
                dialog.IsOpen = true;
                var prefix = kind == "Drawer" ? "Drawer" : "Sheet";
                FrameworkElement? surface = null;
                FrameworkElement? paint = null;
                FrameworkElement? body = null;
                FrameworkElement? action = null;
                for (var attempt = 0; attempt < 80; attempt++)
                {
                    root.Measure(new Size(width, height));
                    root.Arrange(new Rect(0, 0, width, height));
                    root.UpdateLayout();
                    surface = Elements(root).FirstOrDefault(item => item.Name == prefix + "Surface" && item.IsLoaded);
                    paint = Elements(root).FirstOrDefault(item => item.Name == prefix + "PaintSurface" && item.IsLoaded);
                    body = Elements(root).FirstOrDefault(item => item.Name == "ScaleBody" && item.IsLoaded);
                    action = Elements(root).FirstOrDefault(item => item.Name == (kind == "Drawer" ? "DrawerHandle" : "SheetClose") && item.IsLoaded);
                    if (surface?.ActualWidth > 0 && paint?.ActualWidth > 0 && body?.ActualWidth > 0 && action?.ActualWidth > 0) break;
                    await Task.Delay(20);
                }
                if (surface is null || paint is null || body is null || action is null || !dialog.IsOpen)
                    throw new InvalidOperationException("Drawer or Sheet did not present its surface and content.");
                if (Math.Abs(root.XamlRoot.RasterizationScale - 1.5) > .0001)
                    throw new InvalidOperationException("Drawer or Sheet did not render at native 150% scale.");
                await Task.Delay(50);
                // Reacquire the live presentation after the previous edge closes.
                for (var attempt = 0; attempt < 10; attempt++)
                {
                    root.UpdateLayout();
                    surface = Elements(root).FirstOrDefault(item => item.Name == prefix + "Surface" && item.IsLoaded && item.ActualWidth > 0 && item.ActualHeight > 0);
                    paint = Elements(root).FirstOrDefault(item => item.Name == prefix + "PaintSurface" && item.IsLoaded && item.ActualWidth > 0 && item.ActualHeight > 0);
                    body = Elements(root).FirstOrDefault(item => item.Name == "ScaleBody" && item.IsLoaded && item.ActualWidth > 0 && item.ActualHeight > 0);
                    action = Elements(root).FirstOrDefault(item => item.Name == (kind == "Drawer" ? "DrawerHandle" : "SheetClose") && item.IsLoaded && item.ActualWidth > 0 && item.ActualHeight > 0);
                    if (surface is not null && paint is not null && body is not null && action is not null) break;
                    await Task.Delay(20);
                }
                if (surface is null || paint is null || body is null || action is null)
                    throw new InvalidOperationException("Drawer or Sheet did not settle at native 150% scale.");
                var surfaceBox = Box(surface, root);
                Inside(surfaceBox, width, height, $"{kind} surface");
                Inside(Box(paint, root), width, height, $"{kind} paint surface");
                Inside(Box(body, root), width, height, $"{kind} caller content");
                Inside(Box(action, root), width, height, $"{kind} action or handle");
                if (!new Windows.UI.ViewManagement.UISettings().AnimationsEnabled &&
                    (surface.RenderTransform is not TranslateTransform transform ||
                     Math.Abs(transform.X) > .01 || Math.Abs(transform.Y) > .01))
                    throw new InvalidOperationException($"Reduced-motion {kind} did not reach its open endpoint.");
                await Task.Delay(50);
                root.UpdateLayout();
                if (!surfaceBox.Equals(Box(surface, root)))
                    throw new InvalidOperationException($"{kind} surface continued moving after presentation.");
                cases.Add(new { theme = theme.ToString(), width, kind, scale = root.XamlRoot.RasterizationScale, surface = surfaceBox, stable = true });
                dialog.IsOpen = false;
                await Task.Delay(220);
                content.Children.Clear();
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled,
                osSettingsChanged = false,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerDrawer).Assembly.Location)))
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
