using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard.Testing;

internal static class FeedColumnScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 600, Height = 340 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900, 510));
            island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, 900, 510));
            window.Activate();
            var animationsEnabled = new UISettings().AnimationsEnabled;
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var mode in new[] { "default", "compact", "wrapped", "rtl" })
            {
                stage = $"{theme}/{mode}/150-percent";
                root.RequestedTheme = theme;
                var filter = mode == "default" ? null : new TextBox { Text = "Fictional local filter" };
                var body = new StackPanel { Spacing = 8 };
                for (var index = 0; index < 12; index++)
                    body.Children.Add(new TextBlock { Text = $"Fictional local item {index + 1}", TextWrapping = TextWrapping.Wrap });
                var column = new BleeckerFeedColumn
                {
                    Title = mode == "wrapped" ? "A longer fictional coverage title that wraps" : "Local coverage",
                    Count = mode == "default" ? null : 12,
                    Filter = filter,
                    Content = body,
                    Height = 300,
                    FlowDirection = mode == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
                };
                if (mode == "compact") { column.Width = 260; column.MinWidth = 260; }
                Canvas.SetLeft(column, 20);
                Canvas.SetTop(column, 16);
                root.Children.Add(column);
                await LayoutAsync(root);
                var surface = Find<Border>(column, "PART_Surface");
                var header = Find<Border>(column, "FeedColumnHeader");
                var row = Find<FrameworkElement>(column, "FeedColumnRow");
                var title = Find<TextBlock>(column, "FeedColumnTitleText");
                var viewport = column.Viewport ?? throw new InvalidOperationException("Feed Column viewport missing.");
                var frame = Bounds(column, root);
                var interior = new Rect(0, 0, column.ActualWidth, column.ActualHeight);
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    column.IsLoaded && column.ActualTheme == ElementTheme.Dark &&
                    column.ActualWidth >= (mode == "compact" ? 260 : 372) &&
                    column.ActualHeight >= 300 && Within(frame, new Rect(0, 0, root.Width, root.Height)),
                    $"Feed Column frame clipped: {frame}.");
                foreach (var part in new FrameworkElement[] { surface, header, row, title, viewport })
                    Require(part.ActualWidth > 0 && part.ActualHeight > 0 && Within(Bounds(part, column), interior),
                        $"Feed Column {part.Name} clipped within its owner: {Bounds(part, column)}.");
                Require(viewport.ScrollableHeight > 0 && title.DesiredSize.Height <= title.ActualHeight + .5,
                    "Feed Column title or scrolling body clipped.");
                if (filter is not null)
                    Require(filter.IsLoaded && Within(Bounds(filter, column), interior),
                        $"Feed Column filter clipped: {Bounds(filter, column)}.");
                if (column.Count is not null)
                {
                    var count = Find<Border>(column, "FeedColumnCount");
                    Require(Within(Bounds(count, column), interior),
                        $"Feed Column count clipped: {Bounds(count, column)}.");
                }
                if (!animationsEnabled)
                {
                    var before = Bounds(header, column);
                    await Task.Delay(120);
                    await LayoutAsync(root);
                    var after = Bounds(header, column);
                    Require(Math.Abs(before.X - after.X) < .01 && Math.Abs(before.Y - after.Y) < .01 &&
                        Math.Abs(before.Width - after.Width) < .01 && Math.Abs(before.Height - after.Height) < .01,
                        "Feed Column static header shifted with reduced motion.");
                }
                cases.Add(new { theme = theme.ToString(), mode, scale = root.XamlRoot.RasterizationScale,
                    width = column.ActualWidth, height = column.ActualHeight, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = true, cases,
                animationsEnabled, reducedMotionStaticVerified = !animationsEnabled,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerFeedColumn).Assembly.Location))) }));
            window.Close(); Environment.Exit(0);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = false, stage, error = error.ToString() }));
            window.Close(); Environment.Exit(1);
        }
    }

    private static T Find<T>(DependencyObject parent, string name) where T : FrameworkElement =>
        Descendants<T>(parent).Single(element => element.Name == name);

    private static Rect Bounds(FrameworkElement element, FrameworkElement parent) =>
        element.TransformToVisual(parent).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

    private static bool Within(Rect inner, Rect outer) =>
        inner.X >= outer.X - .5 && inner.Y >= outer.Y - .5 &&
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
