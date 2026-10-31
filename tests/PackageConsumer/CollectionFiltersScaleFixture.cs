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

internal static class CollectionFiltersScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 900, Height = 700 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.Activate();
            var animationsEnabled = new UISettings().AnimationsEnabled;
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var viewport in new[] { 480, 900 })
            foreach (var expanded in new[] { false, true })
            foreach (var rtl in new[] { false, true })
            {
                stage = $"{theme}/{viewport}/{expanded}/{rtl}/150-percent";
                root.Width = viewport; root.RequestedTheme = theme;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(viewport * 3 / 2, 1050));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, viewport * 3 / 2, 1050));
                var filters = new BleeckerCollectionFilters
                {
                    Style = (Style)Application.Current.Resources[typeof(BleeckerCollectionFilters)],
                    Width = viewport - 40, DefaultExpanded = expanded,
                    FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                    FilterOptions = new ObservableCollection<BleeckerCollectionFilterOption>
                    {
                        new("status", "Availability", BleeckerCollectionFilterType.Select,
                            [new("available", "In stock"), new("preorder", "Pre-order")]),
                        new("featured", "Featured edit", BleeckerCollectionFilterType.Boolean)
                    },
                    SortOptions = new ObservableCollection<BleeckerCollectionSortOption>
                    {
                        new("createdAt", "Newest arrivals"), new("title", "Name")
                    },
                    CurrentFilters = new Dictionary<string, BleeckerCollectionFilterValue>
                    {
                        ["status"] = "available", ["featured"] = false
                    },
                    CurrentSort = new("createdAt", BleeckerSortOrder.Descending)
                };
                Canvas.SetLeft(filters, 20); Canvas.SetTop(filters, 20);
                root.Children.Add(filters);
                await LayoutAsync(root);
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    filters.IsLoaded && filters.ActualTheme == theme && filters.IsExpanded == expanded &&
                    Within(Bounds(filters, root), new Rect(0, 0, root.Width, root.Height)),
                    "Collection Filters owner clipped at native 150% scale.");
                var actions = Descendants<Button>(filters).Where(button => button.IsLoaded &&
                    button.Visibility == Visibility.Visible && button.ActualWidth > 0 && button.ActualHeight > 0).ToArray();
                Require(actions.Length >= 3, "Collection Filters lost its visible actions.");
                foreach (var action in actions)
                    Require(Within(Bounds(action, filters), new Rect(0, 0, filters.ActualWidth, filters.ActualHeight)),
                        $"Collection Filters action {action.Name} clipped at native 150% scale.");
                if (!animationsEnabled)
                {
                    var before = actions.Select(action => Bounds(action, filters)).ToArray();
                    await Task.Delay(250);
                    await LayoutAsync(root);
                    Require(actions.Select((action, index) => Bounds(action, filters).Equals(before[index])).All(value => value),
                        "Collection Filters actions continued moving with reduced motion.");
                }
                cases.Add(new { theme = theme.ToString(), viewport, expanded, rtl,
                    scale = root.XamlRoot.RasterizationScale, actionCount = actions.Length, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, animationsEnabled, reducedMotionStaticVerified = !animationsEnabled,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerCollectionFilters).Assembly.Location)))
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
