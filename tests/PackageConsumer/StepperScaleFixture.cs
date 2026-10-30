using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard.Testing;

internal static class StepperScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 600, Height = 400 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900, 600));
            island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, 900, 600));
            window.Activate();
            var animationsEnabled = new UISettings().AnimationsEnabled;
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var mode in new[] { "horizontal", "narrow", "vertical", "rtl" })
            {
                stage = $"{theme}/{mode}/150-percent";
                root.RequestedTheme = theme;
                var steps = new[]
                {
                    new BleeckerStep("draft", "Draft") { Description = "Write a fictional story" },
                    new BleeckerStep("review", "Review") { Description = "Check the local facts" },
                    new BleeckerStep("publish", "Publish") { Description = "Release the edition" }
                };
                var stepper = new BleeckerStepper
                {
                    Steps = new System.Collections.ObjectModel.ObservableCollection<BleeckerStep>(steps),
                    ActiveStep = "review",
                    Orientation = mode == "vertical" ? Orientation.Vertical : Orientation.Horizontal,
                    Width = mode switch { "narrow" => 250, "vertical" => 260, _ => 480 },
                    FlowDirection = mode == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
                };
                Canvas.SetLeft(stepper, 20); Canvas.SetTop(stepper, 20);
                root.Children.Add(stepper);
                await LayoutAsync(root);
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    stepper.IsLoaded && stepper.ActualTheme == theme &&
                    Within(Bounds(stepper, root), new Rect(0, 0, root.Width, root.Height)),
                    "Stepper owner clipped at native 150% scale.");
                var entries = Descendants<FrameworkElement>(stepper).Where(item => item.Name == "StepEntry").ToArray();
                Require(entries.Length == steps.Length, "Stepper lost a caller step.");
                foreach (var entry in entries)
                {
                    Require(Within(Bounds(entry, stepper), new Rect(0, 0, stepper.ActualWidth, stepper.ActualHeight)),
                        "Stepper entry clipped at native 150% scale.");
                    foreach (var part in Descendants<FrameworkElement>(entry).Where(item => item.Name is "StepButton" or "Labels"))
                        Require(part.ActualWidth > 0 && part.ActualHeight > 0 &&
                            Within(Bounds(part, stepper), new Rect(0, 0, stepper.ActualWidth, stepper.ActualHeight)),
                            $"Stepper {part.Name} clipped at native 150% scale.");
                }
                if (!animationsEnabled)
                {
                    var fill = Descendants<Border>(entries[1]).Single(item => item.Name == "CircleFill");
                    var brush = (SolidColorBrush)fill.Background;
                    var before = brush.Color;
                    stepper.ActiveStep = "publish";
                    await LayoutAsync(root);
                    var settled = brush.Color;
                    await Task.Delay(120);
                    await LayoutAsync(root);
                    Require(before != settled && brush.Color == settled,
                        "Stepper color continued moving with reduced motion.");
                }
                cases.Add(new { theme = theme.ToString(), mode, scale = root.XamlRoot.RasterizationScale,
                    width = stepper.ActualWidth, height = stepper.ActualHeight, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = true, cases,
                animationsEnabled, reducedMotionStaticVerified = !animationsEnabled,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerStepper).Assembly.Location))) }));
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
