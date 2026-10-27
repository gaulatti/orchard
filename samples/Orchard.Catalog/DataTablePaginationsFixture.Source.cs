using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Catalog;

public sealed partial class DataTablePaginationsFixture
{
    internal async Task VerifySourceAsync(BleeckerColorScheme scheme, string folder)
    {
        using var bytes = new MemoryStream(); await CatalogReferenceResources.Open(typeof(DataTablePaginationsFixture).Assembly, "Orchard.Catalog.Reference.DataTablePagination.json")!.CopyToAsync(bytes);
        using var reference = JsonDocument.Parse(bytes.ToArray()); var reports = new List<object>(); var failures = new List<string>();
        var root = CatalogFixtureRoot.From(XamlRoot); var app = AppWindow.GetFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId); var original = app.ClientSize;
        try
        {
            foreach (var sample in reference.RootElement.GetProperty("cases").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == scheme.ToString()))
            {
                var id = sample.GetProperty("id").GetString()!; var props = sample.GetProperty("props"); var viewport = sample.GetProperty("viewportWidth").GetInt32();
                if (app.ClientSize.Width != viewport) app.ResizeClient(new SizeInt32(viewport, original.Height));
                var a = SourceTable(props); var b = new PaginationTable(8, pageSize: 2, prefix: "Desk ", idPrefix: "desk-"); var active = a;
                var options = new ObservableCollection<int>(props.TryGetProperty("pageSizeOptions", out var sizes) ? sizes.EnumerateArray().Select(item => item.GetInt32()) : [10, 20, 30, 50, 100]);
                var bar = new BleeckerDataTablePagination { Table = active, PageSizeOptions = options, Width = sample.GetProperty("hostWidth").GetDouble(), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, FlowDirection = sample.GetProperty("direction").GetString() == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                var host = new Grid { Margin = new Thickness(16), Background = root.Background }; Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count);
                var probe = new TextBlock { Text = active.VisibleText, Margin = new Thickness(0, 200, 0, 0), VerticalAlignment = VerticalAlignment.Top };
                EventHandler changed = (sender, _) => { if (ReferenceEquals(sender, active)) probe.Text = active.VisibleText; }; a.Changed += changed; b.Changed += changed;
                host.Children.Add(bar); host.Children.Add(probe); root.Children.Add(host);
                try
                {
                    await ReadyAsync(() => bar.IsLoaded); bar.ApplyTemplate(); await MainWindow.AwaitPresentationAsync(bar);
                    var buttons = Descendants<Button>(bar).Where(item => item.Name.StartsWith("PART_", StringComparison.Ordinal)).ToDictionary(item => item.Name); var combo = Chooser(bar);
                    var step = 0;
                    foreach (var point in sample.GetProperty("snapshots").EnumerateArray())
                    {
                        var action = point.GetProperty("action").GetString()!; var expected = point.GetProperty("state"); step++;
                        switch (action)
                        {
                            case "first": Invoke(buttons["PART_First"]); break; case "previous": Invoke(buttons["PART_Previous"]); break; case "next": Invoke(buttons["PART_Next"]); break; case "last": Invoke(buttons["PART_Last"]); break;
                            case "sort-desc": active.SetSort(true); break; case "filter-draft": active.SetFilter(true); break; case "clear-filter": active.SetFilter(false); break;
                            case "external-page": active.SetPagination(2, 7); break; case "external-selection": active.SetSelection(["row-003", "row-040"]); break;
                            case "external-options": options.Clear(); foreach (var value in new[] { 7, 14, 21 }) options.Add(value); break;
                            case "replace-table": active = b; bar.Table = active; probe.Text = active.VisibleText; break;
                            case "stale-table": a.SetPageIndex(1); break; case "restore-table": active = a; bar.Table = active; probe.Text = active.VisibleText; break;
                            case "keyboard-size-20": SelectSize(combo, 20); break; case "keyboard-last": SelectSize(combo, 500); break;
                            case "open": Expand(combo, true); await ReadyAsync(() => ChooserIsOpen(combo)); break; case "open-escape": Expand(combo, false); break;
                            case "disabled-previous": Require(!buttons["PART_Previous"].IsEnabled, "Disabled previous unexpectedly became enabled."); break;
                            default: if (action.StartsWith("size-", StringComparison.Ordinal)) SelectSize(combo, int.Parse(action[5..], CultureInfo.InvariantCulture)); break;
                        }
                        await MainWindow.AwaitPresentationAsync(bar); var errors = new List<string>(); var layoutDifferences = new List<string>();
                        void Check(string key, Action verify) { try { verify(); } catch (InvalidOperationException error) { errors.Add(key + ": " + error.Message); } }
                        var source = expected.GetProperty("table"); var sourceState = source.GetProperty("pagination");
                        Check("consumer", () =>
                        {
                            Require(ReferenceEquals(bar.Table, active) && source.GetProperty("name").GetString() == (ReferenceEquals(active, a) ? "A" : "B"), "Active original table differs.");
                            Require(active.PageIndex == sourceState.GetProperty("pageIndex").GetInt32() && active.PageSize == sourceState.GetProperty("pageSize").GetInt32() && active.PageCount == source.GetProperty("pageCount").GetInt32(), "Producer pagination differs.");
                            Require(active.FilteredRowCount == source.GetProperty("filteredCount").GetInt32() && active.FilteredSelectedRowCount == source.GetProperty("selectedCount").GetInt32(), "Filtered selection counts differ.");
                            Require(active.CanNextPage == source.GetProperty("canNext").GetBoolean() && active.CanPreviousPage == source.GetProperty("canPrevious").GetBoolean(), "Producer navigation flags differ.");
                            Require(probe.Text == expected.GetProperty("visibleRows").GetString() && active.VisibleText == probe.Text, "Visible consumer row projection differs.");
                            Require(active.VisibleRows.Select(row => row.Id).SequenceEqual(source.GetProperty("rows").EnumerateArray().Select(row => row.GetProperty("id").GetString())), "Original row order differs.");
                            Require(active.VisibleRows.All(row => active.Rows.Any(originalRow => ReferenceEquals(originalRow, row))), "The pagination view replaced original row objects.");
                            Require(a.Subscribers == (ReferenceEquals(active, a) ? 2 : 1) && b.Subscribers == (ReferenceEquals(active, b) ? 2 : 1), "Table replacement retained a stale observer.");
                        });
                        var children = Elements(expected.GetProperty("surface")).ToArray(); var right = children[1]; var rightChildren = Elements(right).ToArray();
                        var texts = Descendants<TextBlock>(bar).Where(item => item.Name is "PART_Selection" or "PART_Page" or "PART_RowsLabel").ToDictionary(item => item.Name);
                        Check("labels", () => Require(texts["PART_Selection"].Text == children[0].GetProperty("text").GetString() && texts["PART_Page"].Text == rightChildren[1].GetProperty("text").GetString(), "Rendered summary/page labels differ."));
                        var sourceButtons = Elements(rightChildren[2]).ToArray();
                        foreach (var (name, index) in new[] { ("PART_First", 0), ("PART_Previous", 1), ("PART_Next", 2), ("PART_Last", 3) })
                        {
                            var button = buttons[name]; var sourceButton = sourceButtons[index]; var visible = reference.RootElement.GetProperty("styles")[sourceButton.GetProperty("styleIndex").GetInt32()].GetProperty("display").GetString() != "none";
                            Check(name, () => Require(button.IsEnabled == !sourceButton.GetProperty("disabled").GetBoolean() && (button.Visibility == Visibility.Visible) == visible, "Navigation visibility/enablement differs."));
                            CompareLayout(name, Bounds(button, bar), sourceButton.GetProperty("box"), layoutDifferences, visible);
                        }
                        var sourceCombo = Descendants(expected.GetProperty("surface")).Single(item => item.GetProperty("tag").GetString() == "BUTTON" && item.GetProperty("attributes").TryGetProperty("role", out var role) && role.GetString() == "combobox");
                        Check("size-label", () => Require((SelectedOption(combo)?.Content?.ToString() ?? "") == sourceCombo.GetProperty("text").GetString(), "Controlled page-size text differs."));
                        CompareLayout("surface", new Rect(0, 0, bar.ActualWidth, bar.ActualHeight), expected.GetProperty("surface").GetProperty("box"), layoutDifferences, true);
                        CompareLayout("summary", Bounds(texts["PART_Selection"], bar), children[0].GetProperty("box"), layoutDifferences, texts["PART_Selection"].Visibility == Visibility.Visible);
                        CompareLayout("page", Bounds(texts["PART_Page"], bar), rightChildren[1].GetProperty("box"), layoutDifferences, true);
                        CompareLayout("size", Bounds(combo, bar), sourceCombo.GetProperty("box"), layoutDifferences, true);
                        var file = $"data-table-pagination-source-{id}-{step:00}-{action}-{scheme}.png"; await MainWindow.CaptureElementAsync(root, Path.Combine(folder, file));
                        var bounds = Bounds(bar, root); reports.Add(new { id, step, action, errors, layoutDifferences, file, surface = new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height }, active = ReferenceEquals(active, a) ? "A" : "B", active.PageIndex, active.PageSize, active.PageCount, active.FilteredRowCount, active.FilteredSelectedRowCount, probe = probe.Text, popupOpen = ChooserIsOpen(combo) }); failures.AddRange(errors.Select(error => id + "/" + action + ": " + error));
                    }
                }
                finally { root.Children.Remove(host); a.Changed -= changed; b.Changed -= changed; }
            }
        }
        finally { app.ResizeClient(original); }
        await File.WriteAllTextAsync(Path.Combine(folder, $"data-table-pagination-source-{scheme}.json"), JsonSerializer.Serialize(new { format = "orchard-data-table-pagination-native-v1", scheme = scheme.ToString(), checkedGatesPassed = failures.Count == 0, nativeComponentAccepted = false, referenceSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes.ToArray())).ToLowerInvariant(), failures, reports }, new JsonSerializerOptions { WriteIndented = true }));
        Require(failures.Count == 0, "Native pagination source-state checks failed; see the source replay report.");
    }
    private static PaginationTable SourceTable(JsonElement props)
    {
        static int Number(JsonElement props, string key, int initial) => props.TryGetProperty(key, out var value) ? value.GetInt32() : initial;
        var table = new PaginationTable(Number(props, "rows", 47), Number(props, "pageIndex", 0), Number(props, "pageSize", 10), props.TryGetProperty("manualPageCount", out var count) ? count.GetInt32() : null, props.TryGetProperty("prefix", out var prefix) ? prefix.GetString()! : "Coverage ");
        if (props.TryGetProperty("selection", out var selected)) table.SetSelection(selected.EnumerateObject().Where(item => item.Value.GetBoolean()).Select(item => item.Name));
        if (props.TryGetProperty("filters", out var filters) && filters.GetArrayLength() > 0) table.SetFilter(true); return table;
    }
    private static void Invoke(Button button) => ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke)).Invoke();
    private static ItemsControl Chooser(DependencyObject root) => Descendants<ItemsControl>(root).Single(item => item.Name == "PART_PageSize");
    private static IExpandCollapseProvider ChooserProvider(ItemsControl combo) => (IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(combo).GetPattern(PatternInterface.ExpandCollapse);
    private static bool ChooserIsOpen(ItemsControl combo) => ChooserProvider(combo).ExpandCollapseState == ExpandCollapseState.Expanded;
    private static void Expand(ItemsControl combo, bool open) { var provider = ChooserProvider(combo); if (open) provider.Expand(); else provider.Collapse(); }
    private static ISelectionItemProvider OptionProvider(Button item) => (ISelectionItemProvider)FrameworkElementAutomationPeer.CreatePeerForElement(item).GetPattern(PatternInterface.SelectionItem);
    private static Button? SelectedOption(ItemsControl combo) => combo.Items.OfType<Button>().FirstOrDefault(item => OptionProvider(item).IsSelected);
    private static void SelectSize(ItemsControl combo, int value) { OptionProvider(combo.Items.OfType<Button>().Single(item => (int)item.Tag == value)).Select(); if (ChooserIsOpen(combo)) Expand(combo, false); }
    private static IEnumerable<JsonElement> Elements(JsonElement node) => node.GetProperty("children").EnumerateArray().Where(item => item.GetProperty("tag").GetString() != "#text");
    private static IEnumerable<JsonElement> Descendants(JsonElement node) { yield return node; if (node.TryGetProperty("children", out var children)) foreach (var child in children.EnumerateArray()) foreach (var descendant in Descendants(child)) yield return descendant; }
    private static Rect Bounds(FrameworkElement element, UIElement relative) => element.TransformToVisual(relative).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
    private static void CompareLayout(string name, Rect actual, JsonElement expected, List<string> differences, bool visible)
    { if (!visible) return; foreach (var (key, value) in new[] { ("x", actual.X), ("y", actual.Y), ("width", actual.Width), ("height", actual.Height) }) if (Math.Abs(value - expected.GetProperty(key).GetDouble()) > 1d / 32) differences.Add($"{name}.{key}: {value:R}/{expected.GetProperty(key).GetDouble():R}"); }
}
