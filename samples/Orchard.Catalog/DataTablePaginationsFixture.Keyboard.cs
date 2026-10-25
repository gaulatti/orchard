using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace Orchard.Catalog;

public sealed partial class DataTablePaginationsFixture
{
    internal async Task VerifyKeyboardAsync(BleeckerColorScheme scheme, string folder, bool pairedKeys = false)
    {
        using var bytes = new MemoryStream(); await CatalogReferenceResources.Open(typeof(DataTablePaginationsFixture).Assembly, "Orchard.Catalog.Reference.DataTablePaginationKeyboard.json")!.CopyToAsync(bytes);
        using var reference = JsonDocument.Parse(bytes.ToArray()); var reports = new List<object>(); var failures = new List<string>();
        var root = (Grid)XamlRoot.Content; var app = AppWindow.GetFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId); var original = app.ClientSize;
        try
        {
            foreach (var sample in reference.RootElement.GetProperty("cases").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == scheme.ToString()))
            {
                var id = sample.GetProperty("id").GetString()!; var props = sample.GetProperty("props"); var table = SourceTable(props);
                var bar = new BleeckerDataTablePagination { Table = table, PageSizeOptions = props.TryGetProperty("pageSizeOptions", out var options) ? options.EnumerateArray().Select(item => item.GetInt32()).ToArray() : [10, 20, 30, 50, 100], Width = sample.GetProperty("hostWidth").GetDouble(), HorizontalAlignment = HorizontalAlignment.Left, FlowDirection = sample.GetProperty("direction").GetString() == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                var before = new Button { Name = "Before", Content = "Before", HorizontalAlignment = HorizontalAlignment.Left }; var after = new Button { Name = "After", Content = "After", HorizontalAlignment = HorizontalAlignment.Left };
                var probe = new TextBlock { Text = table.VisibleText, Width = bar.Width, TextWrapping = TextWrapping.Wrap }; table.Changed += (_, _) => probe.Text = table.VisibleText;
                var column = new StackPanel { Spacing = 16, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top }; column.Children.Add(before); column.Children.Add(bar); column.Children.Add(after); column.Children.Add(probe);
                var host = new Grid { Margin = new Thickness(16), Background = root.Background }; Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count); host.Children.Add(column); root.Children.Add(host);
                using var input = new NativeInputSession(XamlRoot, folder, $"pagination-keyboard-{id}-{scheme}");
                try
                {
                    await ReadyAsync(() => bar.IsLoaded); bar.ApplyTemplate(); await MainWindow.AwaitPresentationAsync(bar); await input.AcquireAsync(before);
                    var width = sample.GetProperty("viewportWidth").GetInt32(); var height = sample.GetProperty("viewportHeight").GetInt32(); app.ResizeClient(new SizeInt32(width, height));
                    await ReadyAsync(() => XamlRoot.Size.Width == width && XamlRoot.Size.Height == height); await MainWindow.AwaitPresentationAsync(bar);
                    if (input.PointerIsAtCenter(before)) await input.ClickCurrentPositionAsync(before); else await input.ClickAsync(before);
                    if (id == "first")
                    {
                        using var currentPoint = new NativeInputSession(XamlRoot, folder, $"pagination-keyboard-current-point-{scheme}");
                        await currentPoint.ClickCurrentPositionAsync(before);
                    }
                    var combo = Chooser(bar); var buttons = Descendants<Button>(bar).ToDictionary(item => item.Name);
                    Control Target(string role) => role switch { "before" => before, "after" => after, "combo" => combo, "first" => buttons["PART_First"], "previous" => buttons["PART_Previous"], "next" => buttons["PART_Next"], "last" => buttons["PART_Last"], _ when role.StartsWith("option:", StringComparison.Ordinal) => combo.Items.OfType<Button>().Single(item => item.Content.ToString() == role[7..]), _ => throw new InvalidOperationException("Unknown original focus role: " + role) };
                    string Describe(object? node) => node switch { Button item when item.Tag is int => "option:" + item.Content, _ when ReferenceEquals(node, before) => "before", _ when ReferenceEquals(node, after) => "after", _ when ReferenceEquals(node, combo) => "combo", Button item => item.Name, null => "null", _ => node.GetType().Name };
                    var step = 0;
                    foreach (var point in sample.GetProperty("snapshots").EnumerateArray())
                    {
                        var phase = point.GetProperty("phase").GetString()!; var expected = point.GetProperty("state"); var expectedRole = expected.GetProperty("focused").GetString()!; var errors = new List<string>(); object? receipt = null;
                        if (point.GetProperty("key").ValueKind == JsonValueKind.String)
                        {
                            try { receipt = pairedKeys ? await PaginationPairedKeyAsync(input, point, Describe, errors) : await PaginationKeyAsync(input, Target(expectedRole), point.GetProperty("key").GetString()!, Describe, errors); }
                            catch (Exception error) { errors.Add(error.ToString()); }
                        }
                        await MainWindow.AwaitPresentationAsync(bar);
                        if (!ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), Target(expectedRole))) errors.Add("Focus differs: " + Describe(FocusManager.GetFocusedElement(XamlRoot)) + "/" + expectedRole);
                        if (ChooserIsOpen(combo) != expected.GetProperty("popupOpen").GetBoolean()) errors.Add("Popup state differs.");
                        var expectedTable = expected.GetProperty("table"); var pagination = expectedTable.GetProperty("pagination");
                        if (table.PageIndex != pagination.GetProperty("pageIndex").GetInt32() || table.PageSize != pagination.GetProperty("pageSize").GetInt32() || table.PageCount != expectedTable.GetProperty("pageCount").GetInt32()) errors.Add("Producer pagination differs.");
                        if (table.Requests.Count != expected.GetProperty("events").EnumerateArray().Count(item => item.GetProperty("kind").GetString() == "pagination")) errors.Add("Original pagination callback count differs.");
                        if (probe.Text != expected.GetProperty("visibleRows").GetString() || table.VisibleText != probe.Text || !ReferenceEquals(bar.Table, table) || !table.VisibleRows.All(row => table.Rows.Any(originalRow => ReferenceEquals(originalRow, row)))) errors.Add("Original consumer rows differ.");
                        Require(input.OwnsForeground, "Keyboard capture lost the catalog's foreground window.");
                        var file = $"data-table-pagination-keyboard-{id}-{step++:00}-{phase}-{scheme}.png"; var client = await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, file));
                        var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).SingleOrDefault(item => item.Child is not null && Descendants<Button>(item.Child).Any(option => combo.Items.Contains(option)));
                        var popupBorder = popup?.Child is FrameworkElement popupChild ? Descendants<FrameworkElement>(popupChild).Prepend(popupChild).Single(item => item.Name == "PopupBorder") : null;
                        var popupBounds = popupBorder is not null ? (Windows.Foundation.Rect?)Bounds(popupBorder, root) : null;
                        var focusBounds = FocusManager.GetFocusedElement(XamlRoot) is FrameworkElement focusedElement ? (Windows.Foundation.Rect?)Bounds(focusedElement, root) : null;
                        var scroller = popup is not null ? Descendants<ScrollViewer>(popup.Child).Single(item => item.Name == "ScrollViewer") : null;
                        var viewportBounds = scroller is not null ? (Windows.Foundation.Rect?)Bounds(scroller, root) : null;
                        if (expectedRole.StartsWith("option:", StringComparison.Ordinal) && viewportBounds is { } viewport && focusBounds is { } focusedBox &&
                            (focusedBox.Top < viewport.Top - 1d / 32 || focusedBox.Bottom > viewport.Bottom + 1d / 32))
                        {
                            errors.Add($"Focused option is outside the visible popup viewport: item={focusedBox}, popup={viewport}, offset={scroller!.VerticalOffset}, viewport={scroller.ViewportHeight}, extent={scroller.ExtentHeight}, scrollable={scroller.ScrollableHeight}, actual={scroller.ActualHeight}.");
                        }
                        var valuePresenter = Descendants<ContentPresenter>(combo).Single(item => item.Name == "ContentPresenter");
                        var displayed = valuePresenter.Content is TextBlock selectedText ? selectedText.Text : valuePresenter.Content?.ToString() ?? "";
                        var controlledDisplay = combo.Items.OfType<Button>().FirstOrDefault(item => (int)item.Tag == table.PageSize)?.Content?.ToString() ?? "";
                        if (displayed != controlledDisplay) errors.Add("Displayed chooser value differs from the original producer.");
                        reports.Add(new { id, phase, file, errors, receipt, capture = "desktop-client", client, focus = Describe(FocusManager.GetFocusedElement(XamlRoot)), expectedRole, popupOpen = ChooserIsOpen(combo), table.PageIndex, table.PageSize, table.PageCount, callbacks = table.Requests.Count, surface = Bounds(bar, root), chooser = Bounds(combo, root), popup = popupBounds, popupChildType = popup?.Child.GetType().Name, placement = popup?.ActualPlacement.ToString(), horizontalOffset = popup?.HorizontalOffset, verticalOffset = popup?.VerticalOffset, displayedValue = valuePresenter.Content is TextBlock valueText ? valueText.Text : valuePresenter.Content?.ToString(), valueBox = Bounds(valuePresenter, root), valuePresenter.Opacity, probe = probe.Text }); failures.AddRange(errors.Select(error => id + "/" + phase + ": " + error));
                        Require(errors.Count == 0, "Pagination keyboard replay differs; see its exact native receipt and state report.");
                    }
                }
                finally { root.Children.Remove(host); }
            }
        }
        catch (Exception error) { failures.Add(error.ToString()); throw; }
        finally
        {
            app.ResizeClient(original);
            await File.WriteAllTextAsync(Path.Combine(folder, $"data-table-pagination-keyboard-{scheme}.json"), JsonSerializer.Serialize(new { format = pairedKeys ? "orchard-data-table-pagination-native-paired-v1" : "orchard-data-table-pagination-native-keyboard-v1", scheme = scheme.ToString(), pairedKeys, passed = failures.Count == 0, nativeComponentAccepted = false, referenceSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes.ToArray())).ToLowerInvariant(), failures, reports }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private async Task<object> PaginationKeyAsync(NativeInputSession input, Control to, string key, Func<object?, string> describe, List<string> errors)
    {
        var from = FocusManager.GetFocusedElement(XamlRoot) as Control ?? throw new InvalidOperationException("Keyboard phase has no focused native control."); var downs = 0; var ups = 0; var shiftDowns = 0; var shiftUps = 0; var downOwned = true; var upOwned = true; var shiftOwned = true;
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var downTarget = ""; var upTarget = "";
        var code = key switch { "Tab" or "Shift+Tab" => 0x09, "ArrowDown" => 0x28, "ArrowUp" => 0x26, "Home" => 0x24, "End" => 0x23, "Enter" => 0x0D, "Space" => 0x20, "Escape" => 0x1B, "5" => 0x35, "0" => 0x30, _ => throw new InvalidOperationException("Unknown original key: " + key) };
        var down = new KeyEventHandler((_, args) => { if ((ushort)args.Key == code) { downs++; downOwned &= ReferenceEquals(args.OriginalSource, from); downTarget = describe(args.OriginalSource); } else if (args.Key == Windows.System.VirtualKey.Shift) { shiftDowns++; shiftOwned &= ReferenceEquals(args.OriginalSource, from); } });
        var up = new KeyEventHandler((_, args) => { if ((ushort)args.Key == code) { ups++; upOwned &= ReferenceEquals(args.OriginalSource, to); upTarget = describe(args.OriginalSource); released.TrySetResult(); } else if (args.Key == Windows.System.VirtualKey.Shift) { shiftUps++; shiftOwned &= ReferenceEquals(args.OriginalSource, to); } });
        // Observe the explicit original owners once. A constrained popup's key
        // route also reaches the content root; registering both roots would
        // count the same routed key twice.
        from.AddHandler(UIElement.KeyDownEvent, down, true); to.AddHandler(UIElement.KeyUpEvent, up, true);
        try
        {
            if (key == "Shift+Tab") input.ShiftKey((ushort)code);
            else await input.HeldKeyAsync((ushort)code, from, () => to, async () =>
            {
                await ReadyAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), to));
                await MainWindow.AwaitPresentationAsync(to);
            });
            try { await released.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (TimeoutException) { errors.Add("The explicit release target did not receive its key within two seconds."); }
            if (key == "Shift+Tab") await ReadyAsync(() => shiftUps > 0);
            if (downs != 1 || ups != 1 || !downOwned || !upOwned) errors.Add("Key did not retain exactly one original down/up owner.");
            if (key == "Shift+Tab" && (shiftDowns != 1 || shiftUps != 1 || !shiftOwned)) errors.Add("Shift modifier lost its exact focus transfer.");
        }
        finally { from.RemoveHandler(UIElement.KeyDownEvent, down); to.RemoveHandler(UIElement.KeyUpEvent, up); }
        return new { key, downs, ups, downOwned, upOwned, shiftDowns, shiftUps, shiftOwned, from = describe(from), to = describe(to), downTarget, upTarget };
    }
}
