using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Testing;

internal static class HoverCardScaleFixture
{
    private static IEnumerable<FrameworkElement> Elements(DependencyObject owner)
    {
        if (owner is FrameworkElement element) yield return element;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(owner); index++)
            foreach (var child in Elements(VisualTreeHelper.GetChild(owner, index))) yield return child;
    }

    private static Rect Box(FrameworkElement element, FrameworkElement root) =>
        element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

    private static void RequireInside(Rect box, int width, int height, string name)
    {
        if (box.Width <= 0 || box.Height <= 0 || box.X < -.5 || box.Y < -.5 || box.Right > width + .5 || box.Bottom > height + .5)
            throw new InvalidOperationException($"Hover Card {name} clipped at native 150%: {box}.");
    }

    internal static async Task RunAsync(Window window, string output)
    {
        var cases = new List<object>();
        var stage = "initial";
        try
        {
            var root = new Grid();
            var styles = new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/HoverCard.xaml") };
            root.Resources.MergedDictionaries.Add(styles);
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var width in new[] { 420, 600 })
            foreach (var side in new[] { BleeckerHoverCardSide.Bottom, BleeckerHoverCardSide.Top })
            {
                stage = $"{theme}/{width}/{side}";
                const int height = 360;
                root.Width = width;
                root.Height = height;
                root.RequestedTheme = theme;
                var pixels = new SizeInt32((int)(width * 1.5), (int)(height * 1.5));
                window.AppWindow.ResizeClient(pixels);
                island.SiteBridge.MoveAndResize(new RectInt32(0, 0, pixels.Width, pixels.Height));
                var trigger = new Button { Content = "Fictional contributor", Width = 120, Height = 32 };
                var note = new TextBox { Text = "Original caller note", Width = 220 };
                var body = new StackPanel { Spacing = 8 };
                body.Children.Add(new TextBlock { Text = "Mira Quinn", TextWrapping = TextWrapping.Wrap });
                body.Children.Add(note);
                var card = new BleeckerHoverCard
                {
                    Style = (Style)styles[typeof(BleeckerHoverCard)],
                    Side = side,
                    Trigger = trigger,
                    Content = body,
                    Margin = new Thickness((width - 120) / 2d, side == BleeckerHoverCardSide.Top ? 220 : 48, 0, 0)
                };
                root.Children.Clear();
                root.Children.Add(card);
                for (var attempt = 0; attempt < 8; attempt++)
                {
                    root.Measure(new Size(width, height));
                    root.Arrange(new Rect(0, 0, width, height));
                    root.UpdateLayout();
                    await Task.Delay(16);
                }
                await NativeRenderReadiness.RenderAsync(root);
                if (Math.Abs(root.XamlRoot.RasterizationScale - 1.5) > .0001)
                    throw new InvalidOperationException("Hover Card did not render at native 150% scale.");
                var surface = Elements(card).Single(item => item.Name == "HoverCardSurface");
                var tooltip = Elements(card).Single(item => item.Name == "HoverCardTooltip");
                var triggerBox = Box(trigger, root);
                var surfaceBox = Box(surface, root);
                var tooltipBox = Box(tooltip, root);
                var noteBox = Box(note, root);
                RequireInside(triggerBox, width, height, "original trigger");
                RequireInside(surfaceBox, width, height, "surface");
                RequireInside(tooltipBox, width, height, "tooltip host");
                RequireInside(noteBox, width, height, "original caller note");
                if (!ReferenceEquals(card.Trigger, trigger) || !ReferenceEquals(card.Content, body) || note.Text != "Original caller note")
                    throw new InvalidOperationException("Hover Card replaced caller-owned content at native 150%.");
                await Task.Delay(50);
                root.UpdateLayout();
                if (!surfaceBox.Equals(Box(surface, root))) throw new InvalidOperationException("Hover Card surface continued moving after presentation.");
                cases.Add(new { theme = theme.ToString(), width, side = side.ToString(), scale = root.XamlRoot.RasterizationScale, trigger = triggerBox, surface = surfaceBox, note = noteBox, stable = true });
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled,
                osSettingsChanged = false,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerHoverCard).Assembly.Location)))
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
