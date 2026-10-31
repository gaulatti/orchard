using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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
            var filterCases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var viewport in new[] { 400, 700 })
            {
                stage = $"select-filter/{theme}/{viewport}/150-percent";
                root.Width = viewport; root.Height = 420; root.RequestedTheme = theme;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(viewport * 3 / 2, 630));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, viewport * 3 / 2, 630));
                var model = new BleeckerDataTableModel();
                model.SetColumns([new BleeckerDataTableColumn(accessorKey: "status", header: "Status")]);
                model.SetData(new[] {
                    new Dictionary<string, object?> { ["status"] = "Draft" },
                    new Dictionary<string, object?> { ["status"] = "Published" } });
                var filter = new BleeckerDataTableSelectFilter
                {
                    Table = model, ColumnId = "status", Width = viewport - 40,
                    Options = new[] { new BleeckerSelectOption("Draft", "Draft"), new BleeckerSelectOption("Published", "Published") }
                };
                Canvas.SetLeft(filter, 20); Canvas.SetTop(filter, 20);
                root.Children.Add(filter);
                await LayoutAsync(root);
                var chooser = Descendants<ItemsControl>(filter).Single(item => item.Name == "PART_Select");
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    filter.IsLoaded && filter.ActualTheme == theme &&
                    Within(Bounds(filter, root), new Rect(0, 0, root.Width, root.Height)) &&
                    Within(Bounds(chooser, filter), new Rect(0, 0, filter.ActualWidth, filter.ActualHeight)),
                    "Data Table Select Filter trigger clipped at native 150% scale.");
                var expansion = (IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(chooser).GetPattern(PatternInterface.ExpandCollapse);
                expansion.Expand();
                await LayoutAsync(root);
                var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot)
                    .Single(item => item.Child is Border { Name: "PopupBorder" });
                var surface = Descendants<BleeckerOverlaySurface>(popup.Child).Single(item => item.Name == "PopupSurface");
                var options = Descendants<Button>(surface).Where(item => item.Tag is BleeckerSelectOption).ToArray();
                Require(popup.ShouldConstrainToRootBounds && popup.IsOpen && surface.IsLoaded && surface.Opacity == 1 &&
                    Within(Bounds(surface, root), new Rect(0, 0, root.Width, root.Height)) &&
                    options.Length == 3 && options.All(item => item.IsLoaded &&
                        Within(Bounds(item, surface), new Rect(0, 0, surface.ActualWidth, surface.ActualHeight))),
                    "Data Table Select Filter popup or options clipped at native 150% scale.");
                if (!animationsEnabled)
                {
                    var before = Bounds(surface, root);
                    await Task.Delay(120);
                    await LayoutAsync(root);
                    Require(Bounds(surface, root).Equals(before) && surface.Opacity == 1,
                        "Data Table Select Filter popup continued moving with reduced motion.");
                }
                filterCases.Add(new { theme = theme.ToString(), viewport,
                    scale = root.XamlRoot.RasterizationScale, visibleOptions = options.Length,
                    popupBounds = Bounds(surface, root), passed = true });
                expansion.Collapse();
                await LayoutAsync(root);
                Require(!popup.IsOpen, "Data Table Select Filter popup remained open after collapse.");
                root.Children.Clear();
                await LayoutAsync(root);
            }
            var toolbarCases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var viewport in new[] { 400, 700 })
            {
                stage = $"data-table-toolbar/{theme}/{viewport}/150-percent";
                root.Width = viewport; root.Height = 420; root.RequestedTheme = theme;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(viewport * 3 / 2, 630));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, viewport * 3 / 2, 630));
                var model = new BleeckerDataTableModel();
                model.SetColumns([new BleeckerDataTableColumn(accessorKey: "name", header: "Name")]);
                model.SetData(new[] { new Dictionary<string, object?> { ["name"] = "Fictional record" } });
                model.SetGlobalFilter("Fictional");
                var filterButton = new Button { Content = "Status", Width = 176, Height = 36 };
                var actionButton = new Button { Content = "New record", Width = 104, Height = 36 };
                var toolbar = new BleeckerDataTableToolbar
                {
                    Table = model, SearchColumn = "name", Width = viewport - 40,
                    FilterRenderers = filterButton, Actions = actionButton
                };
                Canvas.SetLeft(toolbar, 20); Canvas.SetTop(toolbar, 20);
                root.Children.Add(toolbar);
                await LayoutAsync(root);
                var search = Descendants<BleeckerInput>(toolbar).Single(item => item.Name == "PART_Search");
                var reset = Descendants<Button>(toolbar).Single(item => item.Name == "PART_Reset");
                var controls = new FrameworkElement[] { search, filterButton, reset, actionButton };
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    toolbar.IsLoaded && toolbar.ActualTheme == theme &&
                    Within(Bounds(toolbar, root), new Rect(0, 0, root.Width, root.Height)) &&
                    controls.All(item => item.IsLoaded && item.Visibility == Visibility.Visible &&
                        item.ActualWidth > 0 && item.ActualHeight > 0 &&
                        Within(Bounds(item, toolbar), new Rect(0, 0, toolbar.ActualWidth, toolbar.ActualHeight))),
                    "Data Table Toolbar search, filter, reset or action clipped at native 150% scale.");
                var caption = Descendants<TextBlock>(reset).Single(item => item.Text == "Reset");
                Require(Within(Bounds(caption, reset), new Rect(0, 0, reset.ActualWidth, reset.ActualHeight)),
                    "Data Table Toolbar Reset caption clipped at native 150% scale.");
                if (!animationsEnabled)
                {
                    var before = controls.Select(item => Bounds(item, toolbar)).ToArray();
                    await Task.Delay(120);
                    await LayoutAsync(root);
                    Require(controls.Select((item, index) => Bounds(item, toolbar).Equals(before[index])).All(value => value),
                        "Data Table Toolbar controls continued moving with reduced motion.");
                }
                toolbarCases.Add(new { theme = theme.ToString(), viewport,
                    scale = root.XamlRoot.RasterizationScale, controls = controls.Length, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            var dateCases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var viewport in new[] { 400, 700 })
            {
                stage = $"date-controls/{theme}/{viewport}/150-percent";
                root.Width = viewport; root.Height = 440; root.RequestedTheme = theme;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(viewport * 3 / 2, 660));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, viewport * 3 / 2, 660));
                var date = new DateTime(2026, 10, 17);
                var value = new DateTimeOffset(date, TimeZoneInfo.Local.GetUtcOffset(date));
                var calendar = new BleeckerCalendar { Width = 280, Value = value };
                Canvas.SetLeft(calendar, 20); Canvas.SetTop(calendar, 20);
                root.Children.Add(calendar);
                await LayoutAsync(root);
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    calendar.IsLoaded && calendar.ActualTheme == theme &&
                    Within(Bounds(calendar, root), new Rect(0, 0, root.Width, root.Height)) &&
                    Descendants<ToggleButton>(calendar).Where(day => int.TryParse(day.Content?.ToString(), out _)).Count() >= 28 &&
                    Descendants<ToggleButton>(calendar).Where(day => int.TryParse(day.Content?.ToString(), out _)).All(day => day.IsLoaded &&
                        Within(Bounds(day, calendar), new Rect(0, 0, calendar.ActualWidth, calendar.ActualHeight))),
                    "Calendar or day cells clipped at native 150% scale.");
                dateCases.Add(new { kind = "calendar", theme = theme.ToString(), viewport,
                    scale = root.XamlRoot.RasterizationScale, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
                var picker = new BleeckerDatePicker { Width = viewport - 40, Value = value };
                Canvas.SetLeft(picker, 20); Canvas.SetTop(picker, 20);
                root.Children.Add(picker);
                await LayoutAsync(root);
                var trigger = Descendants<Button>(picker).Single(item => item.Name == "PART_Trigger");
                Require(picker.IsLoaded && picker.ActualTheme == theme &&
                    Within(Bounds(picker, root), new Rect(0, 0, root.Width, root.Height)) &&
                    Within(Bounds(trigger, picker), new Rect(0, 0, picker.ActualWidth, picker.ActualHeight)),
                    "Date Picker trigger clipped at native 150% scale.");
                ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(trigger).GetPattern(PatternInterface.Invoke)).Invoke();
                await LayoutAsync(root);
                var pickerCalendar = VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot)
                    .SelectMany(popup => Descendants<BleeckerCalendar>(popup.Child)).Single();
                Require(picker.IsDropDownOpen && pickerCalendar.IsLoaded &&
                    Within(Bounds(pickerCalendar, root), new Rect(0, 0, root.Width, root.Height)),
                    "Date Picker calendar clipped at native 150% scale.");
                if (!animationsEnabled)
                {
                    var before = Bounds(pickerCalendar, root);
                    await Task.Delay(120);
                    await LayoutAsync(root);
                    Require(Bounds(pickerCalendar, root).Equals(before),
                        "Date Picker calendar continued moving with reduced motion.");
                }
                dateCases.Add(new { kind = "date-picker", theme = theme.ToString(), viewport,
                    scale = root.XamlRoot.RasterizationScale, passed = true });
                ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(trigger).GetPattern(PatternInterface.Invoke)).Invoke();
                await LayoutAsync(root);
                Require(!picker.IsDropDownOpen, "Date Picker calendar remained open.");
                root.Children.Clear();
                await LayoutAsync(root);
                var range = new BleeckerDateRangePicker
                {
                    Width = viewport - 40, Value = new("2026-10-10", "2026-10-20"),
                    Presets = new[] { new BleeckerDatePreset("Seven days", 7) }
                };
                Canvas.SetLeft(range, 20); Canvas.SetTop(range, 20);
                root.Children.Add(range);
                await LayoutAsync(root);
                var fields = Descendants<BleeckerDatePicker>(range).ToArray();
                Require(range.IsLoaded && range.ActualTheme == theme &&
                    Within(Bounds(range, root), new Rect(0, 0, root.Width, root.Height)) &&
                    fields.Length == 2 && fields.All(field => field.IsLoaded &&
                        Within(Bounds(field, range), new Rect(0, 0, range.ActualWidth, range.ActualHeight))),
                    "Date Range Picker fields clipped at native 150% scale.");
                var startField = fields[0];
                var startTrigger = Descendants<Button>(startField).Single(item => item.Name == "PART_Trigger");
                ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(startTrigger).GetPattern(PatternInterface.Invoke)).Invoke();
                await LayoutAsync(root);
                var rangeCalendar = VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot)
                    .SelectMany(popup => Descendants<BleeckerCalendar>(popup.Child)).Single();
                Require(startField.IsDropDownOpen && rangeCalendar.IsLoaded &&
                    Within(Bounds(rangeCalendar, root), new Rect(0, 0, root.Width, root.Height)),
                    "Date Range Picker calendar clipped at native 150% scale.");
                if (!animationsEnabled)
                {
                    var before = Bounds(rangeCalendar, root);
                    await Task.Delay(120);
                    await LayoutAsync(root);
                    Require(Bounds(rangeCalendar, root).Equals(before),
                        "Date Range Picker calendar continued moving with reduced motion.");
                }
                dateCases.Add(new { kind = "date-range-picker", theme = theme.ToString(), viewport,
                    scale = root.XamlRoot.RasterizationScale, passed = true });
                ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(startTrigger).GetPattern(PatternInterface.Invoke)).Invoke();
                await LayoutAsync(root);
                Require(!startField.IsDropDownOpen, "Date Range Picker calendar remained open.");
                root.Children.Clear();
                await LayoutAsync(root);
            }
            var chartCases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var viewport in new[] { 400, 700 })
            foreach (var kind in new[] { "bar", "sankey" })
            {
                stage = $"{kind}-chart/{theme}/{viewport}/150-percent";
                root.Width = viewport; root.Height = 400; root.RequestedTheme = theme;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(viewport * 3 / 2, 600));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, viewport * 3 / 2, 600));
                Control wrapper;
                if (kind == "bar")
                {
                    var bar = new BleeckerBarChart { XAxisKey = "label", ShowLegend = false };
                    bar.Series.Add(new BleeckerChartSeries { Key = "v", Name = "Fictional" });
                    bar.Data.Add(new Dictionary<string, object?> { ["label"] = "A", ["v"] = 10d });
                    bar.Data.Add(new Dictionary<string, object?> { ["label"] = "B", ["v"] = 20d });
                    wrapper = bar;
                }
                else
                {
                    wrapper = new BleeckerSankeyChart { Data = new()
                    {
                        Nodes = [new BleeckerSankeyNode { Name = "A" }, new BleeckerSankeyNode { Name = "B" }],
                        Links = [new BleeckerSankeyLink { Source = 0, Target = 1, Value = 10 }]
                    } };
                }
                wrapper.Width = viewport - 40; wrapper.Height = 300;
                Canvas.SetLeft(wrapper, 20); Canvas.SetTop(wrapper, 20);
                root.Children.Add(wrapper);
                await LayoutAsync(root);
                var chart = Descendants<BleeckerLineChart>(wrapper).Single();
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    wrapper.IsLoaded && wrapper.ActualTheme == theme && chart.IsLoaded &&
                    chart.ActualWidth > 0 && chart.ActualHeight > 0 &&
                    Within(Bounds(wrapper, root), new Rect(0, 0, root.Width, root.Height)) &&
                    Within(Bounds(chart, wrapper), new Rect(0, 0, wrapper.ActualWidth, wrapper.ActualHeight)),
                    "Bar or Sankey chart clipped at native 150% scale.");
                if (!animationsEnabled)
                {
                    var before = Bounds(chart, root);
                    await Task.Delay(120);
                    await LayoutAsync(root);
                    Require(Bounds(chart, root).Equals(before),
                        "Bar or Sankey chart continued moving with reduced motion.");
                }
                chartCases.Add(new { kind, theme = theme.ToString(), viewport,
                    scale = root.XamlRoot.RasterizationScale, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                passed = true, cases, tableCases, columnToggleCases, filterCases, toolbarCases, dateCases, chartCases,
                animationsEnabled, reducedMotionStaticVerified = !animationsEnabled,
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
