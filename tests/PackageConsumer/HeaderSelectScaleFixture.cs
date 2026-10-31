using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Testing;

internal static class HeaderSelectScaleFixture
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
            throw new InvalidOperationException($"Header Select {name} clipped at native 150%: {box}.");
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
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var width in new[] { 420, 600 })
            {
                stage = $"{theme}/{width}";
                const int height = 300;
                root.RequestedTheme = theme;
                root.Width = width;
                root.Height = height;
                var pixels = new SizeInt32((int)(width * 1.5), (int)(height * 1.5));
                window.AppWindow.ResizeClient(pixels);
                island.SiteBridge.MoveAndResize(new RectInt32(0, 0, pixels.Width, pixels.Height));
                var options = new[]
                {
                    new BleeckerSelectOption("a", "Fictional library Alpha"),
                    new BleeckerSelectOption("b", "Fictional library Beta")
                };
                var header = new BleeckerHeaderSelect
                {
                    Placeholder = "Local suite", Options = options, Value = "a",
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(24, 36, 0, 0)
                };
                root.Children.Clear();
                root.Children.Add(header);
                for (var attempt = 0; attempt < 20; attempt++)
                {
                    root.Measure(new Size(width, height));
                    root.Arrange(new Rect(0, 0, width, height));
                    root.UpdateLayout();
                    if (header.IsLoaded && header.ActualWidth > 0) break;
                    await Task.Delay(20);
                }
                if (Math.Abs(root.XamlRoot.RasterizationScale - 1.5) > .0001)
                    throw new InvalidOperationException("Header Select did not render at native 150% scale.");
                var trigger = Elements(header).Single(item => item.Name == "SelectTrigger");
                Inside(Box(trigger, root), width, height, "trigger");
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(trigger);
                var expand = (IExpandCollapseProvider?)peer.GetPattern(PatternInterface.ExpandCollapse)
                    ?? throw new InvalidOperationException("Header Select lacks its native expand contract.");
                expand.Expand();
                FrameworkElement? surface = null;
                for (var attempt = 0; attempt < 50; attempt++)
                {
                    root.UpdateLayout();
                    surface = Elements(root).FirstOrDefault(item => item.Name == "PopupSurface" && item.IsLoaded && item.ActualWidth > 0);
                    surface ??= VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot)
                        .Where(popup => popup.Child is not null)
                        .SelectMany(popup => Elements(popup.Child!))
                        .FirstOrDefault(item => item.Name == "PopupSurface" && item.IsLoaded && item.ActualWidth > 0);
                    if (surface is not null) break;
                    await Task.Delay(20);
                }
                if (surface is null || expand.ExpandCollapseState != ExpandCollapseState.Expanded)
                    throw new InvalidOperationException("Header Select did not open its original options.");
                await Task.Delay(50);
                root.UpdateLayout();
                var surfaceBox = Box(surface, root);
                Inside(surfaceBox, width, height, "open surface");
                var rows = Elements(surface).OfType<Button>().ToArray();
                if (rows.Length < 2) throw new InvalidOperationException("Header Select lost its original options.");
                foreach (var row in rows) Inside(Box(row, root), width, height, "option");
                await Task.Delay(50);
                root.UpdateLayout();
                if (!surfaceBox.Equals(Box(surface, root)))
                    throw new InvalidOperationException("Header Select surface continued moving after presentation.");
                if (!ReferenceEquals(header.Options, options) || header.Value != "a")
                    throw new InvalidOperationException("Header Select replaced caller options or value.");
                cases.Add(new { theme = theme.ToString(), width, scale = root.XamlRoot.RasterizationScale, surface = surfaceBox, optionCount = rows.Length, stable = true });
                expand.Collapse();
                root.Children.Clear();
                await Task.Delay(25);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled,
                osSettingsChanged = false,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerHeaderSelect).Assembly.Location)))
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
