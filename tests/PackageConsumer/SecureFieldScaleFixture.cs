using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard.Testing;

internal static class SecureFieldScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 360, Height = 100 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(540, 150));
            island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, 540, 150));
            window.Activate();
            var animationsEnabled = new UISettings().AnimationsEnabled;
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var width in new[] { 160, 320 })
            foreach (var revealed in new[] { false, true })
            foreach (var enabled in new[] { false, true })
            foreach (var font in new[] { 14d, 28d })
            {
                stage = $"{theme}/{width}/{revealed}/{enabled}/{font}/150-percent";
                root.RequestedTheme = theme;
                var field = new BleeckerSecureField
                {
                    Width = width, FontSize = font, IsEnabled = enabled, IsRevealed = revealed,
                    Password = "Fictional", PlaceholderText = "Local sample"
                };
                Canvas.SetLeft(field, 20);
                Canvas.SetTop(field, 20);
                root.Children.Add(field);
                await LayoutAsync(root);
                var editor = Descendants<PasswordBox>(field).Single(item => item.Name == "SecurePasswordEditor");
                var reveal = Descendants<BleeckerIconButton>(field).Single(item => item.Name == "SecureRevealButton");
                var border = Descendants<Border>(field).Single(item => item.Name == "SecureBorder");
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    field.IsLoaded && field.ActualTheme == theme &&
                    editor.ActualWidth > 0 && editor.ActualHeight > 0 &&
                    reveal.ActualWidth > 0 && reveal.ActualHeight > 0 &&
                    Within(Bounds(field, root), new Rect(0, 0, root.Width, root.Height)) &&
                    Within(Bounds(editor, field), new Rect(0, 0, field.ActualWidth, field.ActualHeight)) &&
                    Within(Bounds(reveal, field), new Rect(0, 0, field.ActualWidth, field.ActualHeight)) &&
                    Within(Bounds(border, field), new Rect(0, 0, field.ActualWidth, field.ActualHeight)),
                    "Secure Field editor, reveal action or border clipped at native 150% scale.");
                Require(editor.PasswordRevealMode == (revealed ? PasswordRevealMode.Visible : PasswordRevealMode.Hidden) &&
                    reveal.IsEnabled == enabled &&
                    AutomationProperties.GetName(reveal) == (revealed ? "Hide password" : "Show password"),
                    "Secure Field reveal or disabled state did not render.");
                if (!animationsEnabled)
                {
                    field.IsRevealed = !revealed;
                    await LayoutAsync(root);
                    Require(editor.PasswordRevealMode == (revealed ? PasswordRevealMode.Hidden : PasswordRevealMode.Visible),
                        "Secure Field reveal did not snap with reduced motion.");
                    var opacity = Descendants<FrameworkElement>(field).Single(item => item.Name == "SecureInterior").Opacity;
                    await Task.Delay(220);
                    await LayoutAsync(root);
                    Require(Descendants<FrameworkElement>(field).Single(item => item.Name == "SecureInterior").Opacity == opacity,
                        "Secure Field interior continued animating with reduced motion.");
                }
                cases.Add(new { theme = theme.ToString(), width, revealed, enabled, font,
                    scale = root.XamlRoot.RasterizationScale, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled, reducedMotionStaticVerified = !animationsEnabled,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerSecureField).Assembly.Location)))
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
