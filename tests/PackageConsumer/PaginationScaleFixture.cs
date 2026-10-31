using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard.Testing;

internal static class PaginationScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 700, Height = 180 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.Activate();
            var animationsEnabled = new UISettings().AnimationsEnabled;
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var viewport in new[] { 400, 700 })
            foreach (var page in new[] { 1d, 5d, 9d })
            foreach (var rtl in new[] { false, true })
            {
                stage = $"{theme}/{viewport}/{page}/{rtl}/150-percent";
                root.Width = viewport; root.RequestedTheme = theme;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(viewport * 3 / 2, 270));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, viewport * 3 / 2, 270));
                var pagination = new BleeckerPagination
                {
                    Style = (Style)Application.Current.Resources[typeof(BleeckerPagination)],
                    Width = viewport - 40, CurrentPage = page, TotalPages = 9,
                    HasPrevPage = page > 1, HasNextPage = page < 9,
                    FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
                };
                Canvas.SetLeft(pagination, 20); Canvas.SetTop(pagination, 20);
                root.Children.Add(pagination);
                await LayoutAsync(root);
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    pagination.IsLoaded && pagination.ActualTheme == theme &&
                    Within(Bounds(pagination, root), new Rect(0, 0, root.Width, root.Height)),
                    "Pagination owner clipped at native 150% scale.");
                var row = Descendants<FrameworkElement>(pagination).Single(item => item.Name == "PaginationNavigation");
                var buttons = Descendants<Button>(pagination).ToArray();
                Require(buttons.Length >= 3 &&
                    Within(Bounds(row, pagination), new Rect(0, 0, pagination.ActualWidth, pagination.ActualHeight)),
                    "Pagination row clipped or lost its actions at native 150% scale.");
                foreach (var button in buttons)
                    Require(button.ActualWidth > 0 && button.ActualHeight > 0 &&
                        Within(Bounds(button, pagination), new Rect(0, 0, pagination.ActualWidth, pagination.ActualHeight)),
                        "Pagination action clipped at native 150% scale.");
                if (!animationsEnabled)
                {
                    var before = buttons.Select(button => Bounds(button, pagination)).ToArray();
                    await Task.Delay(120);
                    await LayoutAsync(root);
                    Require(buttons.Select((button, index) => Bounds(button, pagination).Equals(before[index])).All(value => value),
                        "Pagination actions continued moving with reduced motion.");
                }
                cases.Add(new { theme = theme.ToString(), viewport, page, rtl,
                    scale = root.XamlRoot.RasterizationScale, actionCount = buttons.Length, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            var tableCases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var viewport in new[] { 400, 700 })
            foreach (var populated in new[] { true, false })
            {
                stage = $"data-table/{theme}/{viewport}/{populated}/150-percent";
                root.Width = viewport; root.Height = 420; root.RequestedTheme = theme;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(viewport * 3 / 2, 630));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, viewport * 3 / 2, 630));
                var model = new BleeckerDataTableModel();
                model.SetColumns([new BleeckerDataTableColumn(accessorKey: "name", header: "Name")]);
                model.SetData(populated
                    ? Enumerable.Range(0, 37).Select(index => new Dictionary<string, object?> { ["name"] = $"Fictional row {index}" }).ToArray()
                    : []);
                if (populated) model.SetPageIndex(1);
                var pagination = new BleeckerDataTablePagination { Table = model, Width = viewport - 40 };
                Canvas.SetLeft(pagination, 20); Canvas.SetTop(pagination, 20);
                root.Children.Add(pagination);
                await LayoutAsync(root);
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    pagination.IsLoaded && pagination.ActualTheme == theme &&
                    Within(Bounds(pagination, root), new Rect(0, 0, root.Width, root.Height)),
                    "Data Table Pagination owner clipped at native 150% scale.");
                var chooser = Descendants<ItemsControl>(pagination).Single(item => item.Name == "PART_PageSize");
                var controls = Descendants<FrameworkElement>(pagination)
                    .Where(item => item.Name is "PART_Selection" or "PART_RowsLabel" or "PART_Page" or "PART_PageSize" or "PART_First" or "PART_Previous" or "PART_Next" or "PART_Last")
                    .Where(item => item.Visibility == Visibility.Visible).ToArray();
                Require(controls.Length >= 5 && controls.All(item => item.ActualWidth > 0 && item.ActualHeight > 0 &&
                    Within(Bounds(item, pagination), new Rect(0, 0, pagination.ActualWidth, pagination.ActualHeight))),
                    "Data Table Pagination text or actions clipped at native 150% scale.");
                var page = (TextBlock)controls.Single(item => item.Name == "PART_Page");
                Require(page.Text == (populated ? "Page 2 of 4" : "Page 1 of 0"), "Data Table Pagination lost the consumer's page state.");
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(chooser);
                var expansion = (IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse);
                expansion.Expand();
                await LayoutAsync(root);
                var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot)
                    .Single(item => item.Child is not null && Descendants<BleeckerOverlaySurface>(item.Child).Any(surface => surface.Name == "PopupSurface"));
                var surface = Descendants<BleeckerOverlaySurface>(popup.Child).Single(item => item.Name == "PopupSurface");
                Require(popup.ShouldConstrainToRootBounds && popup.IsOpen && surface.IsLoaded && surface.Opacity == 1 &&
                    Within(Bounds(surface, root), new Rect(0, 0, root.Width, root.Height)),
                    "Data Table Pagination chooser clipped at native 150% scale.");
                if (!animationsEnabled)
                {
                    var before = Bounds(surface, root);
                    await Task.Delay(120);
                    await LayoutAsync(root);
                    Require(Bounds(surface, root).Equals(before) && surface.Opacity == 1,
                        "Data Table Pagination chooser continued moving with reduced motion.");
                }
                tableCases.Add(new { theme = theme.ToString(), viewport, populated,
                    scale = root.XamlRoot.RasterizationScale, visibleControls = controls.Length,
                    chooserBounds = Bounds(surface, root), passed = true });
                expansion.Collapse();
                await LayoutAsync(root);
                Require(!popup.IsOpen, "Data Table Pagination chooser remained open after collapse.");
                root.Children.Clear();
                await LayoutAsync(root);
            }
            var columnToggleCases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var viewport in new[] { 400, 700 })
            {
                stage = $"column-toggle/{theme}/{viewport}/150-percent";
                root.Width = viewport; root.Height = 420; root.RequestedTheme = theme;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(viewport * 3 / 2, 630));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, viewport * 3 / 2, 630));
                var model = new BleeckerDataTableModel();
                model.SetColumns([
                    new BleeckerDataTableColumn(accessorKey: "name", header: "Name"),
                    new BleeckerDataTableColumn(accessorKey: "status", header: "Status"),
                    new BleeckerDataTableColumn(accessorKey: "city", header: "City")]);
                var toggle = new BleeckerDataTableColumnToggle { Table = model };
                Canvas.SetLeft(toggle, 20); Canvas.SetTop(toggle, 20);
                root.Children.Add(toggle);
                await LayoutAsync(root);
                var trigger = Descendants<Button>(toggle).Single(item => item.Name == "PART_Trigger");
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    toggle.IsLoaded && toggle.ActualTheme == theme &&
                    Within(Bounds(toggle, root), new Rect(0, 0, root.Width, root.Height)) &&
                    Within(Bounds(trigger, root), new Rect(0, 0, root.Width, root.Height)),
                    "Data Table Column Toggle trigger clipped at native 150% scale.");
                var expansion = (IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(trigger).GetPattern(PatternInterface.ExpandCollapse);
                expansion.Expand();
                await LayoutAsync(root);
                var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot)
                    .Single(item => item.Child is FrameworkElement { Name: "PART_Dialog" });
                var surface = Descendants<BleeckerOverlaySurface>(popup.Child).Single(item => item.Name == "PART_Surface");
                var checks = Descendants<BleeckerCheckbox>(surface).ToArray();
                Require(popup.ShouldConstrainToRootBounds && popup.IsOpen && surface.IsLoaded && surface.Opacity == 1 &&
                    Within(Bounds(surface, root), new Rect(0, 0, root.Width, root.Height)) &&
                    checks.Length == 3 && checks.All(item => item.IsLoaded &&
                        Within(Bounds(item, surface), new Rect(0, 0, surface.ActualWidth, surface.ActualHeight))),
                    "Data Table Column Toggle chooser or options clipped at native 150% scale.");
                if (!animationsEnabled)
                {
                    var before = Bounds(surface, root);
                    await Task.Delay(120);
                    await LayoutAsync(root);
                    Require(Bounds(surface, root).Equals(before) && surface.Opacity == 1,
                        "Data Table Column Toggle chooser continued moving with reduced motion.");
                }
                columnToggleCases.Add(new { theme = theme.ToString(), viewport,
                    scale = root.XamlRoot.RasterizationScale, visibleOptions = checks.Length,
                    chooserBounds = Bounds(surface, root), passed = true });
                expansion.Collapse();
                await LayoutAsync(root);
                Require(!popup.IsOpen, "Data Table Column Toggle chooser remained open after collapse.");
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, tableCases, columnToggleCases, animationsEnabled, reducedMotionStaticVerified = !animationsEnabled,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerPagination).Assembly.Location)))
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
