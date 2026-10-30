using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard.Testing;

internal static class OtpInputScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 480, Height = 320 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(720, 480));
            island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, 720, 480));
            window.Activate();
            var animationsEnabled = new UISettings().AnimationsEnabled;
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var mode in new[] { "normal", "secure", "disabled", "large", "rtl" })
            {
                stage = $"{theme}/{mode}/150-percent";
                root.RequestedTheme = theme;
                var input = new BleeckerOtpInput { Value = "4827", Length = 6, IsSecure = mode == "secure",
                    IsEnabled = mode != "disabled" };
                if (mode == "large") input.FontSize = 28;
                if (mode == "rtl") input.FlowDirection = FlowDirection.RightToLeft;
                Canvas.SetLeft(input, 16);
                Canvas.SetTop(input, 16);
                root.Children.Add(input);
                await LayoutAsync(root);
                var slots = Descendants<Grid>(input).Where(item => item.Name.StartsWith("OtpSlot", StringComparison.Ordinal)).ToArray();
                var owner = Bounds(input, root);
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    input.ActualTheme == theme && input.IsLoaded && slots.Length == 6 &&
                    input.ActualWidth >= 280 && input.ActualHeight >= 48 &&
                    Within(owner, new Rect(0, 0, root.Width, root.Height)),
                    $"OTP Input frame clipped at native 150%: {owner}.");
                for (var index = 0; index < slots.Length; index++)
                {
                    var slot = slots[index];
                    var editor = Descendants<Control>(slot).Single(item => item.Name == "OtpEditor" + index);
                    var chrome = Descendants<Border>(slot).Single(item => item.Name == "OtpChrome");
                    var slotBounds = Bounds(slot, input);
                    var editorBounds = Bounds(editor, input);
                    var chromeBounds = Bounds(chrome, input);
                    Require(Math.Abs(slot.ActualWidth - 40) < .01 && Math.Abs(slot.ActualHeight - 48) < .01 &&
                        Within(slotBounds, new Rect(0, 0, input.ActualWidth, input.ActualHeight)) &&
                        Within(editorBounds, slotBounds) && Within(chromeBounds, slotBounds) &&
                        editor.IsEnabled == input.IsEnabled &&
                        (input.IsSecure ? editor is PasswordBox : editor is TextBox),
                        $"OTP Input slot {index} clipped at native 150%: slot={slotBounds}, editor={editorBounds}, chrome={chromeBounds}.");
                }
                if (mode == "normal" && !animationsEnabled)
                {
                    var editor = Descendants<Control>(slots[0]).Single(item => item.Name == "OtpEditor0");
                    var ring = Descendants<Border>(slots[0]).Single(item => item.Name == "OtpRing");
                    Require(editor.Focus(FocusState.Keyboard), "OTP Input did not focus its first editor.");
                    await LayoutAsync(root);
                    Require(Math.Abs(ring.Opacity - 1) < .001,
                        $"OTP Input did not snap its focus ring to the reduced-motion endpoint: {ring.Opacity}.");
                }
                cases.Add(new { theme = theme.ToString(), mode, scale = root.XamlRoot.RasterizationScale,
                    width = input.ActualWidth, height = input.ActualHeight, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = true, cases, animationsEnabled,
                reducedMotionRuntimeVerified = !animationsEnabled,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerOtpInput).Assembly.Location))) }));
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

    private static bool Within(Rect inner, Rect outer) =>
        inner.X >= outer.X - .5 && inner.Y >= outer.Y - .5 &&
        inner.Right <= outer.Right + .5 && inner.Bottom <= outer.Bottom + .5;

    private static async Task LayoutAsync(Canvas root)
    {
        for (var i = 0; i < 3; i++)
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
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            foreach (var child in Descendants<T>(VisualTreeHelper.GetChild(parent, i))) yield return child;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
