using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard.Testing;

internal static class TimelineScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 600, Height = 500 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.Activate();
            var animationsEnabled = new UISettings().AnimationsEnabled;
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var width in new[] { 600, 800 })
            foreach (var rtl in new[] { false, true })
            {
                stage = $"{theme}/{width}/{rtl}/150-percent";
                root.Width = width;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(width * 3 / 2, 750));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, width * 3 / 2, 750));
                root.RequestedTheme = theme;
                var first = new BleeckerTimelineItem("created", "Record created")
                    { Timestamp = "09:15", Description = "Fictional local record" };
                var timeline = new BleeckerTimeline
                {
                    Items = new ObservableCollection<BleeckerTimelineItem>
                    {
                        first, new("review", "Editorial review") { Description = "Read the local note" }
                    },
                    Width = width == 600 ? 320 : 500,
                    FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
                };
                Canvas.SetLeft(timeline, 20); Canvas.SetTop(timeline, 20);
                root.Children.Add(timeline);
                await LayoutAsync(root);
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    timeline.IsLoaded && timeline.ActualTheme == theme &&
                    Within(Bounds(timeline, root), new Rect(0, 0, root.Width, root.Height)),
                    "Timeline owner clipped at native 150% scale.");
                var rows = Descendants<FrameworkElement>(timeline).Where(item => item.Name == "TimelineRow").ToArray();
                Require(rows.Length == 2, "Timeline lost a caller-owned record.");
                foreach (var row in rows)
                {
                    Require(Within(Bounds(row, timeline), new Rect(0, 0, timeline.ActualWidth, timeline.ActualHeight)),
                        "Timeline row clipped at native 150% scale.");
                    foreach (var part in Descendants<FrameworkElement>(row)
                        .Where(item => item.Name is "TimelineIcon" or "TimelineTitle" or "TimelineTimestamp" or "TimelineDescription"))
                    {
                        if (part.Visibility != Visibility.Visible || part.ActualWidth == 0 || part.ActualHeight == 0) continue;
                        Require(Within(Bounds(part, timeline), new Rect(0, 0, timeline.ActualWidth, timeline.ActualHeight)),
                            $"Timeline {part.Name} clipped at native 150% scale.");
                    }
                }
                if (!animationsEnabled)
                {
                    var frame = Descendants<Border>(rows[0]).Single(item => item.Name == "TimelineIconFrame");
                    var before = ((SolidColorBrush)frame.Background).Color;
                    first.Status = BleeckerTimelineStatus.Error;
                    await LayoutAsync(root);
                    var settled = ((SolidColorBrush)frame.Background).Color;
                    await Task.Delay(120);
                    await LayoutAsync(root);
                    Require(before != settled && ((SolidColorBrush)frame.Background).Color == settled,
                        "Timeline status color continued moving with reduced motion.");
                }
                cases.Add(new { theme = theme.ToString(), width, rtl, scale = root.XamlRoot.RasterizationScale, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled, reducedMotionStaticVerified = !animationsEnabled,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerTimeline).Assembly.Location)))
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
