using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;
internal static class SimpleDataTableFixture
{
    private delegate bool Visitor(nint handle, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node) { if (node is FrameworkElement e) yield return e; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Elements(VisualTreeHelper.GetChild(node, i))) yield return child; }
    private static Dictionary<string, object?> Row(string id, string name, object? count, string status) => new() { ["id"] = id, ["name"] = name, ["count"] = count, ["status"] = status };
    private static object[] Rows() => [Row("cedar", "Cedar bulletin", 12, "Queued"), Row("birch", "Birch update", 3, "Ready"), Row("ash", "Ash brief", 25, "Draft")];
    private static object[] Mixed() => [Row("null", "Null count", null, "Unset"), Row("boolean", "Boolean count", false, "False"), Row("array", "Array count", new[] { "2", "3" }, "Array"), Row("object", "Object count", new object(), "Object")];
    private static object? Value(object row, string key) => ((Dictionary<string, object?>)row)[key];
    private static string Id(object row) => (string)Value(row, "id")!;
    private static object Box(FrameworkElement element, UIElement root) { var box = element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight)); return new { x = box.X, y = box.Y, width = box.Width, height = box.Height }; }
    private static string Text(object? value) => value is TextBlock text ? text.Text : value?.ToString() ?? "";
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    internal static async Task RunAsync(Window window, string output, string hostAssembly)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        using var reference = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(output)!, "source-reference.json")));
        var root = (Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='900' Height='760' Background='{ThemeResource BleeckerBackgroundBrush}'/>");
        window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900, 760));
        var guards = 0; var focus = 0; var activations = 0; var stage = "initial"; var cases = new List<object>(); var histories = new List<object>(); var contracts = new List<object>(); var pngs = new List<object>();
        root.GotFocus += (_, _) => focus++; window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((h, _) => { GetWindowThreadProcessId(h, out var p); visible |= p == Environment.ProcessId && IsWindowVisible(h); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "DataTable probe left its hidden boundary.");
        }
        async Task Layout()
        {
            for (var i = 0; i < 6; i++) { var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Dispatcher refused layout."); await idle.Task; root.Measure(new Size(900, 760)); root.Arrange(new Rect(0, 0, 900, 760)); root.UpdateLayout(); Guard(); await Task.Delay(12); }
        }
        async Task Capture(string file)
        {
            Guard(); var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root); Guard();
            var pixels = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());
            using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels); await encoder.FlushAsync(); stream.Seek(0);
            using var reader = new DataReader(stream.GetInputStreamAt(0)); await reader.LoadAsync((uint)stream.Size); var bytes = new byte[(int)stream.Size]; reader.ReadBytes(bytes);
            await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, file), bytes); pngs.Add(new { file, width = bitmap.PixelWidth, height = bitmap.PixelHeight, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)) }); Guard();
        }
        try
        {
            foreach (var scheme in new[] { "Light", "Dark" })
            {
                root.RequestedTheme = Enum.Parse<ElementTheme>(scheme);
                foreach (var entry in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("scheme").GetString() == scheme && !c.GetProperty("name").GetString()!.StartsWith("primitives", StringComparison.Ordinal)))
                {
                    var name = entry.GetProperty("name").GetString()!; var spec = entry.GetProperty("spec"); stage = scheme + "-static-" + name;
                    var context = new Context(name == "mixed-values" ? Mixed() : spec.TryGetProperty("rows", out _) ? [] : Rows()); var data = context.Data;
                    if (spec.TryGetProperty("emptyMessage", out var message)) data.EmptyMessage = message.GetString();
                    if (spec.TryGetProperty("selectable", out var selectable)) data.IsSelectable = selectable.GetBoolean();
                    if (spec.TryGetProperty("caption", out _)) data.Caption = null;
                    if (spec.TryGetProperty("footer", out _)) data.FooterRowsSelector = null;
                    if (spec.TryGetProperty("width", out var width)) data.Width = width.GetDouble();
                    if (spec.TryGetProperty("rtl", out _)) data.FlowDirection = FlowDirection.RightToLeft;
                    if (spec.TryGetProperty("sort", out var sort)) data.Sort = new(sort.GetProperty("field").GetString()!, sort.GetProperty("order").GetString() == "asc" ? BleeckerTableSortOrder.Ascending : BleeckerTableSortOrder.Descending);
                    if (spec.TryGetProperty("keys", out var keys)) data.SelectedKeys = keys.EnumerateArray().Select(k => k.GetString()!).ToHashSet(StringComparer.Ordinal);
                    root.Children.Add(data); await Layout(); var snapshot = context.Snapshot(root); await Capture($"simple-table-{scheme}-{name}.png");
                    Require(Elements(data).OfType<BleeckerSortableTableHeader>().Single(h => h.Field == "status").Content is string, "Nonsortable header retained a native action.");
                    var gridPeer = FrameworkElementAutomationPeer.CreatePeerForElement(context.Table); var grid = (IGridProvider)gridPeer.GetPattern(PatternInterface.Grid);
                    for (var rowIndex = 0; rowIndex < grid.RowCount; rowIndex++) for (var column = 0; column < grid.ColumnCount; column++) Require(grid.GetItem(rowIndex, column) is not null, "Lazy grid lookup has no original or empty provider.");
                    gridPeer.GetChildren();
                    var nativeRows = context.Table.Sections.OfType<Grid>().SelectMany(section => section.Children.OfType<BleeckerTableRow>()).ToArray();
                    for (var rowIndex = 0; rowIndex < nativeRows.Length; rowIndex++)
                    {
                        var row = nativeRows[rowIndex]; var rowPeer = FrameworkElementAutomationPeer.CreatePeerForElement(row); var peers = rowPeer.GetChildren(); var filled = row.Cells.Sum(c => c.ColumnSpan);
                        Require(peers.Count == row.Cells.Count + grid.ColumnCount - filled, "Sparse row metadata did not retain original cells and empty grid positions.");
                        foreach (var peer in peers) { var item = (IGridItemProvider)peer.GetPattern(PatternInterface.GridItem); Require(item.Row == rowIndex && item.ContainingGrid is not null && ReferenceEquals(peer.GetParent(), rowPeer), "Original or empty cell lost its containing grid or original row parent."); }
                        for (var column = 0; column < grid.ColumnCount; column++) Require(grid.GetItem(rowIndex, column) is not null, "A sparse native grid position has no provider.");
                    }
                    cases.Add(new { scheme, name, passed = true, snapshot, sparseGridMetadataVerified = true }); root.Children.Clear(); await Layout(); Require(!data.IsLoaded, "Static DataTable remained loaded.");
                }
                foreach (var history in reference.RootElement.GetProperty("histories").EnumerateArray().Where(h => h.GetProperty("scheme").GetString() == scheme && h.GetProperty("action").GetString() is not ("row-hover" or "original-action")))
                {
                    var action = history.GetProperty("action").GetString()!; stage = scheme + "-" + action;
                    var context = new Context(action == "coercion-sort" ? Mixed() : Rows(), action); var data = context.Data;
                    if (action.StartsWith("controlled-sort", StringComparison.Ordinal)) data.Sort = new("count", BleeckerTableSortOrder.Descending);
                    if (action.StartsWith("controlled-selection", StringComparison.Ordinal)) data.SelectedKeys = new HashSet<string>(["outside"], StringComparer.Ordinal);
                    root.Children.Add(data); await Layout(); var snapshots = new List<object>();
                    foreach (var expected in history.GetProperty("snapshots").EnumerateArray())
                    {
                        var label = expected.GetProperty("stage").GetString()!; stage = scheme + "-" + action + "-" + label;
                        void Sort(string field) { var header = Elements(data).OfType<BleeckerSortableTableHeader>().Single(h => h.Field == field); var button = (Button)header.Content; ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke)).Invoke(); }
                        void RowSelect(int index) { var body = context.Table.Sections.OfType<BleeckerTableBody>().Single(); var row = body.Children.OfType<BleeckerTableRow>().ElementAt(index); var check = (BleeckerCheckbox)row.Cells[0].Content; ((IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(check).GetPattern(PatternInterface.Toggle)).Toggle(); }
                        void All() { var check = Elements(data).OfType<BleeckerCheckbox>().Single(c => AutomationProperties.GetName(c) == "Select all"); ((IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(check).GetPattern(PatternInterface.Toggle)).Toggle(); }
                        switch (label)
                        {
                            case "initial": break;
                            case "name-asc": case "name-desc": case "custom-asc": case "custom-desc": Sort("name"); break;
                            case "count-asc": case "count-desc": case "accepted-asc": case "accepted-desc": case "coercion-asc": case "coercion-desc": case "sort-rebinds-index-key": Sort("count"); break;
                            case "rejected-request": if (action.StartsWith("controlled-sort", StringComparison.Ordinal)) Sort("count"); else RowSelect(0); break;
                            case "released-control": if (action.StartsWith("controlled-sort", StringComparison.Ordinal)) data.Sort = null; else data.SelectedKeys = null; break;
                            case "one-selected": case "accepted-one": case "first-index-selected": RowSelect(0); break;
                            case "all-selected": case "none-selected": case "accepted-all": case "all-replaces-outside-key": All(); break;
                            case "accepted-row-off": RowSelect(1); break;
                            case "original-records-replaced": context.Items.Clear(); context.Items.Add(Row("cedar", "Cedar revised", 1, "Ready")); context.Items.Add(Row("ash", "Ash revised", 2, "Queued")); break;
                            default: throw new InvalidOperationException("Unimplemented native source transition: " + label);
                        }
                        await Layout(); var rows = context.Table.Sections.OfType<BleeckerTableBody>().Single().Children.OfType<BleeckerTableRow>().ToArray(); var expectedRows = expected.GetProperty("rows").EnumerateArray().ToArray();
                        Require(rows.Select(r => Id(r.Tag)).SequenceEqual(expectedRows.Select(r => r.GetProperty("id").GetString())), stage + ": visible row order differs.");
                        Require(rows.Where(r => r.IsSelected).Select(r => Id(r.Tag)).SequenceEqual(expectedRows.Where(r => r.GetProperty("selected").GetString() == "true").Select(r => r.GetProperty("id").GetString())), stage + ": selected rows differ.");
                        Require(context.FooterOrder == expected.GetProperty("footerOrder").GetString(), stage + ": original footer producer order differs.");
                        Require(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(context.Events), JsonNode.Parse(expected.GetProperty("events").GetRawText())), stage + ": native request callbacks differ.");
                        var allCheck = Elements(data).OfType<BleeckerCheckbox>().Single(c => AutomationProperties.GetName(c) == "Select all"); var expectedAll = expected.GetProperty("checkboxes")[0];
                        Require((allCheck.IsChecked == true) == expectedAll.GetProperty("checked").GetBoolean() && (!allCheck.IsChecked.HasValue) == expectedAll.GetProperty("indeterminate").GetBoolean(), stage + ": header selection flags differ.");
                        foreach (var row in rows) Require(context.Items.Any(original => ReferenceEquals(original, row.Tag)), "Native row replaced an original item.");
                        snapshots.Add(new { stage = label, snapshot = context.Snapshot(root), nativeRequestsMatchSource = true, originalRowsAndFooterProducerRetained = true });
                    }
                    histories.Add(new { scheme, action, passed = true, snapshots, retainedNativeInstance = true }); root.Children.Clear(); await Layout();
                }
                stage = scheme + "-original-content-contract";
                var original = new Button { Content = "Original fictional command" }; var clicks = 0; original.Click += (_, _) => clicks++;
                var notifying = new NotifyingRow(); var contractItems = Rows(); contractItems[0] = notifying;
                var retained = new Context(contractItems); retained.Data.Columns.Add(new("action", "Original action", isSortable: false, cellContentSelector: (row, _) => Id(row) == "cedar" ? original : null)); root.Children.Add(retained.Data); await Layout();
                var primitive = retained.Table; var originalCells = primitive.Sections.OfType<BleeckerTableBody>().Single().Children.OfType<BleeckerTableRow>().Single(r => Id(r.Tag) == "cedar").Cells.ToArray();
                ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(original).GetPattern(PatternInterface.Invoke)).Invoke(); await Layout(); Require(clicks == 1 && original.IsLoaded, "Original native cell command failed.");
                retained.Data.Sort = new("count"); await Layout(); var reordered = primitive.Sections.OfType<BleeckerTableBody>().Single().Children.OfType<BleeckerTableRow>().Single(r => Id(r.Tag) == "cedar"); Require(originalCells.SequenceEqual(reordered.Cells) && original.IsLoaded, "Sorting replaced original native cells or command.");
                notifying.SetCount(0); await Layout(); Require(ReferenceEquals(primitive.Sections.OfType<BleeckerTableBody>().Single().Children.OfType<BleeckerTableRow>().First().Tag, notifying) && retained.FooterOrder == "cedar,birch,ash", "Original item notification did not cross sort, rows and footer.");
                var bindings = new BindingState();
                retained.Data.SetBinding(BleeckerSimpleDataTable.SortProperty, new Binding { Source = bindings, Path = new PropertyPath(nameof(BindingState.Sort)), Mode = BindingMode.OneWay });
                retained.Data.SetBinding(BleeckerSimpleDataTable.SelectedKeysProperty, new Binding { Source = bindings, Path = new PropertyPath(nameof(BindingState.SelectedKeys)), Mode = BindingMode.OneWay }); await Layout();
                var sortBinding = retained.Data.ReadLocalValue(BleeckerSimpleDataTable.SortProperty); var selectionBinding = retained.Data.ReadLocalValue(BleeckerSimpleDataTable.SelectedKeysProperty);
                var headerAction = (Button)Elements(retained.Data).OfType<BleeckerSortableTableHeader>().Single(h => h.Field == "name").Content;
                ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(headerAction).GetPattern(PatternInterface.Invoke)).Invoke(); await Layout();
                var firstCheck = (BleeckerCheckbox)primitive.Sections.OfType<BleeckerTableBody>().Single().Children.OfType<BleeckerTableRow>().First().Cells[0].Content;
                ((IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(firstCheck).GetPattern(PatternInterface.Toggle)).Toggle(); await Layout();
                Require(retained.Data.EffectiveSort == bindings.Sort && retained.Data.EffectiveSelectedKeys.SetEquals(["outside"]) && ReferenceEquals(sortBinding, retained.Data.ReadLocalValue(BleeckerSimpleDataTable.SortProperty)) && ReferenceEquals(selectionBinding, retained.Data.ReadLocalValue(BleeckerSimpleDataTable.SelectedKeysProperty)), "Rejected native requests rewrote caller bindings or visible values.");
                bindings.Set(new("count"), new HashSet<string>(["cedar"], StringComparer.Ordinal)); await Layout();
                Require(retained.Data.EffectiveSort == bindings.Sort && retained.Data.EffectiveSelectedKeys.SetEquals(["cedar"]), "Live caller bindings stopped reaching original native rows.");
                retained.Data.ClearValue(BleeckerSimpleDataTable.SortProperty); retained.Data.ClearValue(BleeckerSimpleDataTable.SelectedKeysProperty); await Layout();
                Require(retained.Data.EffectiveSort == new BleeckerTableSort("name") && retained.Data.EffectiveSelectedKeys.SetEquals(["outside", "ash"]), "Releasing native bindings did not expose the last internal requests.");
                var template = retained.Data.Template; retained.Data.Template = null; await Layout(); Require(!original.IsLoaded, "Cleared composition template retained loaded original action."); retained.Data.Template = template; await Layout(); Require(original.IsLoaded && ReferenceEquals(retained.Table, primitive), "Composition template restoration replaced original table or action.");
                root.Children.Clear(); await Layout(); Require(!original.IsLoaded, "Removed composition retained original action."); var before = retained.FooterCalls; notifying.SetCount(2); await Layout(); Require(retained.FooterCalls == before, "Removed table kept observing caller item changes.");
                contracts.Add(new { scheme, passed = true, originalNativeActionInvoked = true, originalCellsRetainedAfterSort = true, originalTableAndContentRestored = true, collectionChangesObserved = true, rowPropertyNotificationsObserved = true, controlledBindingsRetained = true, removedItemObserversReleased = true });
            }
            string Hash(string p) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-simple-data-table-native-v1", passed = true, cases, histories, contracts, pngs, guards, focus, activations, activateCalled = false, showWindowCalled = false, focusCalled = false, hardwareInputSent = false, physicalInputVerified = false, sourceRasterParityVerified = false, nativeComponentAccepted = false, publicApisOnly = true, librarySha256 = Hash(typeof(BleeckerSimpleDataTable).Assembly.Location), hostSha256 = Hash(hostAssembly) }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(0);
        }
        catch (Exception error) { await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-simple-data-table-native-v1", passed = false, stage, error = error.ToString(), cases, histories, guards, focus, activations }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(1); }
    }
    private sealed class Context
    {
        internal readonly BleeckerSimpleDataTable Data;
        internal readonly ObservableCollection<object> Items;
        internal readonly List<object> Events = [];
        internal string FooterOrder = "";
        internal int FooterCalls;
        internal BleeckerTable Table => Elements(Data).OfType<BleeckerTable>().Single();
        internal Context(object[] items, string action = "")
        {
            Items = new(items); Data = new() { Width = 660, Margin = new Thickness(20), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, IsSelectable = true, Caption = "Fictional local component data" };
            Data.GetRowKey = (row, index) => action == "index-keys" ? index.ToString(CultureInfo.InvariantCulture) : Id(row);
            Data.Columns.Add(new("name", "Fictional item", comparison: action == "custom-compare" ? (a, b) => ((string)Value(b, "name")!).Length.CompareTo(((string)Value(a, "name")!).Length) : null));
            Data.Columns.Add(new("count", "Count", alignment: BleeckerTableCellAlignment.Right));
            var nativeLabels = new Dictionary<object, TextBlock>(ReferenceEqualityComparer.Instance);
            Data.Columns.Add(new("status", "Status", isSortable: false, cellContentSelector: (row, index) => { if (!nativeLabels.TryGetValue(row, out var label)) { label = new() { UseLayoutRounding = false, LineHeight = 20, LineStackingStrategy = LineStackingStrategy.BlockLineHeight, TextWrapping = TextWrapping.WrapWholeWords }; nativeLabels.Add(row, label); } label.Text = Value(row, "status") + ":" + index; return label; }));
            var footer = new BleeckerTableRow(); var label = new BleeckerTableCell { ColumnSpan = 2, Content = "Fictional total" }; var total = new BleeckerTableCell(); footer.Cells.Add(label); footer.Cells.Add(total);
            Data.FooterRowsSelector = sorted => { FooterCalls++; FooterOrder = string.Join(',', sorted.Select(Id)); var sum = 0d; foreach (var row in sorted) { var value = Value(row, "count"); sum += value is null ? 0 : value is bool boolean ? boolean ? 1 : 0 : double.TryParse(value.ToString(), CultureInfo.InvariantCulture, out var number) ? number : double.NaN; } total.Content = sum.ToString(CultureInfo.InvariantCulture); return [footer]; };
            Data.SortRequested += (_, sort) => { Events.Add(new { kind = "sort", value = new { field = sort.Field, order = sort.Order == BleeckerTableSortOrder.Ascending ? "asc" : "desc" } }); if (action == "controlled-sort-accept") Data.Sort = sort; };
            Data.SelectionRequested += (_, keys) => { Events.Add(new { kind = "selection", value = keys.ToArray() }); if (action == "controlled-selection-accept") Data.SelectedKeys = keys; };
            Data.ItemsSource = Items;
        }
        internal object Snapshot(UIElement root)
        {
            var table = Table; var head = table.Sections.OfType<BleeckerTableHeader>().Single(); var body = table.Sections.OfType<BleeckerTableBody>().Single(); var foot = table.Sections.OfType<BleeckerTableFooter>().SingleOrDefault();
            object Cell(BleeckerTableCell c) => new { text = c is BleeckerSortableTableHeader sort ? sort.Label : Text(c.Content), box = Box(c, root), colSpan = c.ColumnSpan, rowSpan = c.RowSpan };
            object SortButtonSnapshot(BleeckerSortableTableHeader heading)
            {
                var button = (Button)heading.Content; var color = ((SolidColorBrush)button.Foreground).Color;
                var icon = Elements(button).Single(e => e.Name is "PART_Up" or "PART_Down" or "PART_Unsorted" && e.Visibility == Visibility.Visible);
                return new { text = heading.Label, box = Box(button, root), fontSize = button.FontSize, fontWeight = button.FontWeight.Weight, fontFamily = button.FontFamily.Source, color = new[] { (int)color.R, color.G, color.B, color.A }, icon = icon.Name, opacity = icon.Opacity };
            }
            return new { fixture = Box(Data, root), table = Box(table, root), caption = table.Sections.OfType<BleeckerTableCaption>().SingleOrDefault() is { } cap ? new { text = Text(cap.Content), box = Box(cap, root) } : null,
                header = head.Children.OfType<BleeckerTableRow>().SelectMany(r => r.Cells).Select(Cell).ToArray(), body = body.Children.OfType<BleeckerTableRow>().SelectMany(r => r.Cells).Select(Cell).ToArray(), footer = foot?.Children.OfType<BleeckerTableRow>().SelectMany(r => r.Cells).Select(Cell).ToArray() ?? [],
                rows = body.Children.OfType<BleeckerTableRow>().Select((r, index) => new { id = r.Tag is null ? null : Id(r.Tag), index = r.Tag is null ? null : index.ToString(CultureInfo.InvariantCulture), selected = r.Tag is null ? null : r.IsSelected ? "true" : "false", box = Box(r, root) }).ToArray(),
                checkboxes = Elements(Data).OfType<BleeckerCheckbox>().Select(c => new { label = AutomationProperties.GetName(c), @checked = c.IsChecked == true, indeterminate = !c.IsChecked.HasValue, box = Box(c, root) }).ToArray(),
                sortButtons = Elements(Data).OfType<BleeckerSortableTableHeader>().Where(h => h.IsSortable).Select(SortButtonSnapshot).ToArray(), footerOrder = foot is null ? null : FooterOrder, events = Events.ToArray() };
        }
    }
    private sealed class NotifyingRow : Dictionary<string, object?>, System.ComponentModel.INotifyPropertyChanged
    {
        internal NotifyingRow() : base(Row("cedar", "Cedar bulletin", 12, "Queued")) { }
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        internal void SetCount(int count) { this["count"] = count; PropertyChanged?.Invoke(this, new("count")); }
    }
    private sealed class BindingState : System.ComponentModel.INotifyPropertyChanged
    {
        public BleeckerTableSort Sort { get; private set; } = new("count", BleeckerTableSortOrder.Descending);
        public IReadOnlySet<string> SelectedKeys { get; private set; } = new HashSet<string>(["outside"], StringComparer.Ordinal);
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        internal void Set(BleeckerTableSort sort, IReadOnlySet<string> selected) { Sort = sort; SelectedKeys = selected; PropertyChanged?.Invoke(this, new(nameof(Sort))); PropertyChanged?.Invoke(this, new(nameof(SelectedKeys))); }
    }
}
