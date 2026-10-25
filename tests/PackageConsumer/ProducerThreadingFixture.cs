using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Testing;

// Shared by the catalog and isolated NuGet application. Only public Orchard APIs
// are used. This checks producer-to-view delivery, without opening any popup,
// activating a window, changing system preferences or sending native input.
internal static class ProducerThreadingFixture
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);

    internal static async Task RunAsync(Window window, string reportPath, string hostPath)
    {
        var checks = new List<object>(); var failures = new List<object>(); var workerThreads = new List<int>(); var selectRecipes = new List<string>(); var guards = 0; var activationEvents = 0; var focusEvents = 0;
        var uiThread = Environment.CurrentManagedThreadId; var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var root = new Canvas { Width = 1280, Height = 900 }; window.Content = root;
        window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activationEvents++; };
        root.GotFocus += (_, _) => focusEvents++;
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground);
            Require(!window.Visible && !window.AppWindow.IsVisible && !IsWindowVisible(handle) && foreground != Environment.ProcessId && activationEvents == 0 && focusEvents == 0, "Producer regression window became visible, activated or focused.");
        }
        async Task Layout()
        {
            for (var index = 0; index < 4; index++) { root.Measure(new Size(1280, 900)); root.Arrange(new Rect(0, 0, 1280, 900)); root.UpdateLayout(); Guard(); await Task.Delay(10); }
            Require(root.XamlRoot is { IsHostVisible: false }, "Producer regression must have a hidden XamlRoot.");
        }
        async Task Worker(Action change)
        {
            var producerThread = await Task.Run(() => { Require(Environment.CurrentManagedThreadId != uiThread, "Producer did not run on a worker thread."); change(); return Environment.CurrentManagedThreadId; });
            Require(producerThread != uiThread, "Producer returned to its owning UI thread."); workerThreads.Add(producerThread); await Layout();
        }
        async Task Check(string id, Func<Task> action)
        {
            try { Guard(); await action(); checks.Add(new { id, passed = true }); }
            catch (Exception error) { checks.Add(new { id, passed = false }); failures.Add(new { id, error = error.ToString() }); }
            finally { root.Children.Clear(); await Layout(); }
        }
        async Task Mount(FrameworkElement element) { root.Children.Add(element); await Layout(); Require(element.IsLoaded, "Component did not mount."); }
        static bool HasText(DependencyObject owner, string text) => Nodes(owner).OfType<TextBlock>().Any(node => node.Text == text);
        static ItemsControl Surface(DependencyObject owner) => Nodes(owner).OfType<ItemsControl>().Single();
        static bool Choice(ItemsControl surface, BleeckerSelectOption original, string label) => surface.Items.OfType<Button>().Any(item => ReferenceEquals(item.Tag, original) && Equals(item.Content, label));
        try
        {
            Guard(); await Layout();
            await Check("select-worker-options-and-metadata", async () =>
            {
                foreach (var header in new[] { false, true })
                {
                var first = new BleeckerSelectOption("first", "Original"); var options = new ObservableCollection<BleeckerSelectOption> { first };
                BleeckerSelect select = header ? new BleeckerHeaderSelect() : new BleeckerSelect { Width = 320 }; select.Options = options; select.Value = "first"; await Mount(select); var surface = Surface(select);
                await Worker(() => first.Label = "Worker label"); Require(HasText(select, "Worker label") && Choice(surface, first, "Worker label"), "Select did not render original worker metadata.");
                var added = new BleeckerSelectOption("second", "Second"); await Worker(() => options.Add(added)); Require(Choice(surface, added, "Second"), "Select did not render the original worker collection item.");
                var replacement = new BleeckerSelectOption("new", "Replacement"); select.Options = new ObservableCollection<BleeckerSelectOption> { replacement }; select.Value = "new"; await Layout();
                await Worker(() => first.Label = "Retired"); Require(HasText(select, "Replacement") && surface.Items.Count == (header ? 2 : 1) && ReferenceEquals(select.Options.Single(), replacement), "Retired Select producer changed its replacement.");
                root.Children.Remove(select); await Layout(); await Worker(() => replacement.Label = "Detached"); Require(!select.IsLoaded && Choice(surface, replacement, "Replacement"), "Unloaded Select kept rendering producer notifications.");
                selectRecipes.Add(select.GetType().Name);
                }
            });
            await Check("collection-filters-worker-options-and-metadata", async () =>
            {
                var option = new BleeckerSelectOption("draft", "Draft"); var options = new ObservableCollection<BleeckerSelectOption> { option };
                var filters = new BleeckerCollectionFilters { Width = 1100, DefaultExpanded = true, FilterOptions = new ObservableCollection<BleeckerCollectionFilterOption> { new("status", "Status", BleeckerCollectionFilterType.Select, options) }, CurrentFilters = new Dictionary<string, BleeckerCollectionFilterValue> { ["status"] = "draft" } };
                await Mount(filters); await Worker(() => option.Label = "Worker draft"); Require(HasText(filters, "Worker draft"), "CollectionFilters chip did not render original worker metadata.");
                var next = new BleeckerSelectOption("ready", "Ready"); await Worker(() => options.Add(next)); Require(Nodes(filters).OfType<ItemsControl>().Any(surface => Choice(surface, next, "Ready")), "CollectionFilters did not deliver its worker collection to the composed Select.");
                root.Children.Remove(filters); await Layout(); await Worker(() => option.Label = "Detached draft"); Require(!filters.IsLoaded && !HasText(filters, "Detached draft"), "Unloaded CollectionFilters kept rendering producer notifications.");
            });
            await Check("pagination-worker-table-options-and-replacement", async () =>
            {
                var source = new PaginationSource(); var options = new ObservableCollection<int> { 10, 20 };
                var pagination = new BleeckerDataTablePagination { Width = 1100, Table = source, PageSizeOptions = options }; await Mount(pagination); var surface = Surface(pagination);
                await Worker(() => source.SetPageIndex(1)); Require(HasText(pagination, "Page 2 of 3"), "Pagination did not render the worker table state.");
                await Worker(() => options.Add(50)); Require(surface.Items.OfType<Button>().Any(item => Equals(item.Tag, 50)), "Pagination did not render the worker page-size collection.");
                var replacement = new PaginationSource(); pagination.Table = replacement; await Layout(); Require(source.Subscribers == 0 && replacement.Subscribers == 1, "Pagination did not transfer its observer to the replacement table.");
                await Worker(() => source.SetPageIndex(2)); Require(HasText(pagination, "Page 1 of 3"), "Retired Pagination table changed the replacement view.");
                root.Children.Remove(pagination); await Layout(); Require(replacement.Subscribers == 0, "Unloaded Pagination retained its observer."); await Worker(() => replacement.SetPageIndex(2)); Require(HasText(pagination, "Page 1 of 3"), "Unloaded Pagination kept rendering table notifications.");
            });
            await Check("column-toggle-worker-table-and-replacement", async () =>
            {
                var source = new ColumnSource(); var picker = new BleeckerDataTableColumnToggle { Table = source }; await Mount(picker);
                BleeckerCheckbox Checkbox() => Nodes(picker).OfType<BleeckerCheckbox>().Single(box => ReferenceEquals(box.Tag, source.Column));
                Require(Checkbox().IsChecked == true, "Column picker did not render its original column."); await Worker(() => source.SetVisible(false)); Require(Checkbox().IsChecked == false, "Column picker did not render the worker column state.");
                var replacement = new ColumnSource(); picker.Table = replacement; await Layout(); Require(source.Subscribers == 0 && replacement.Subscribers == 1, "Column picker did not transfer its table observer.");
                await Worker(() => source.SetVisible(true)); Require(Nodes(picker).OfType<BleeckerCheckbox>().Single().IsChecked == true && ReferenceEquals(Nodes(picker).OfType<BleeckerCheckbox>().Single().Tag, replacement.Column), "Retired column table changed replacement ownership.");
                root.Children.Remove(picker); await Layout(); Require(replacement.Subscribers == 0, "Unloaded column picker retained its table observer."); await Worker(() => replacement.SetVisible(false)); Require(Nodes(picker).OfType<BleeckerCheckbox>().Single().IsChecked == true, "Unloaded column picker kept rendering notifications.");
            });
            await Check("select-filter-worker-table-options-and-metadata", async () =>
            {
                var source = new FilterSource(); var option = new BleeckerSelectOption("draft", "Draft"); var options = new ObservableCollection<BleeckerSelectOption> { option };
                var filter = new BleeckerDataTableSelectFilter { Table = source, ColumnId = "status", Options = options }; await Mount(filter); var surface = Surface(filter);
                await Worker(() => source.Column.SetFilterValue("draft")); Require(HasText(filter, "Draft"), "Select filter did not render the worker column value.");
                await Worker(() => option.Label = "Worker draft"); Require(HasText(filter, "Worker draft") && Choice(surface, option, "Worker draft"), "Select filter did not render the original worker option metadata.");
                var added = new BleeckerSelectOption("ready", "Ready"); await Worker(() => options.Add(added)); Require(Choice(surface, added, "Ready"), "Select filter did not render the original worker collection item.");
                var replacement = new FilterSource(); filter.Table = replacement; await Layout(); Require(source.Subscribers == 0 && replacement.Subscribers == 1, "Select filter did not transfer its table observer.");
                await Worker(() => source.Column.SetFilterValue("ready")); Require(HasText(filter, "All"), "Retired filter column changed the replacement view.");
                root.Children.Remove(filter); await Layout(); Require(replacement.Subscribers == 0, "Unloaded Select filter retained its table observer."); await Worker(() => option.Label = "Detached draft"); Require(Choice(surface, option, "Worker draft"), "Unloaded Select filter kept rendering option notifications.");
            });
        }
        catch (Exception error) { failures.Add(new { id = "harness", error = error.ToString() }); }
        bool isolated;
        try { Guard(); isolated = true; } catch (Exception error) { isolated = false; failures.Add(new { id = "isolation", error = error.ToString() }); }
        var passed = failures.Count == 0 && checks.Count == 5 && isolated;
        static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(new { passed, diagnosticOnly = true, scope = "hidden-producer-threading-regression", checks, failures, uiThread, workerThreads, selectRecipes, guardChecks = guards, hiddenWindow = true, activateCalled = false, showCalled = false, focusCalled = false, inputSent = false, activationEvents, focusEvents, isolated, processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), hostSha256 = Hash(hostPath), librarySha256 = Hash(typeof(BleeckerSelect).Assembly.Location), visibleRuntimeVerified = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
        window.Close(); Environment.Exit(passed ? 0 : 1);
    }

    private static IEnumerable<DependencyObject> Nodes(DependencyObject owner)
    {
        yield return owner;
        var children = new List<DependencyObject>();
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(owner); index++) children.Add(VisualTreeHelper.GetChild(owner, index));
        if (owner is Popup { Child: { } child } && !children.Contains(child)) children.Add(child);
        // Closed popup children do not have applied templates. Traverse their
        // public content too; do not open a popup just to inspect prepared state.
        if (owner is ContentControl { Content: DependencyObject content } && !children.Contains(content) && VisualTreeHelper.GetParent(content) is null) children.Add(content);
        foreach (var nested in children) foreach (var node in Nodes(nested)) yield return node;
    }
    private abstract class Source
    {
        private EventHandler? changed;
        internal int Subscribers { get; private set; }
        public event EventHandler? Changed { add { changed += value; Subscribers++; } remove { changed -= value; Subscribers--; } }
        internal void Publish() => changed?.Invoke(this, EventArgs.Empty);
    }
    private sealed class PaginationSource : Source, IBleeckerDataTablePaginationSource
    {
        public int PageIndex { get; private set; }
        public int PageSize { get; private set; } = 10;
        public int PageCount => 3; public int FilteredRowCount => 25; public int FilteredSelectedRowCount => 0;
        public bool CanPreviousPage => PageIndex > 0; public bool CanNextPage => PageIndex < 2;
        public void SetPageIndex(int value) { PageIndex = value; Publish(); }
        public void SetPageSize(int value) { PageSize = value; Publish(); }
        public void PreviousPage() => SetPageIndex(PageIndex - 1); public void NextPage() => SetPageIndex(PageIndex + 1);
    }
    private sealed class ColumnSource : Source, IBleeckerDataTableColumnVisibilitySource
    {
        internal sealed class VisibilityColumn : IBleeckerDataTableVisibilityColumn
        {
            public string Id => "status"; public bool HasAccessor => true; public bool CanHide => true; public bool IsVisible { get; internal set; } = true; public object Header => "Status";
            public void ToggleVisibility(bool visible) => throw new InvalidOperationException("Read-only producer regression must not request a column mutation.");
        }
        internal VisibilityColumn Column { get; } = new();
        public IReadOnlyList<IBleeckerDataTableVisibilityColumn> GetAllColumns() => [Column];
        internal void SetVisible(bool visible) { Column.IsVisible = visible; Publish(); }
    }
    private sealed class FilterSource : Source, IBleeckerDataTableFilterSource
    {
        internal sealed class FilterColumn(FilterSource owner) : IBleeckerDataTableFilterColumn
        {
            private object? value;
            public object? GetFilterValue() => value;
            public void SetFilterValue(object? next) { value = next; owner.Publish(); }
        }
        internal FilterColumn Column { get; }
        internal FilterSource() => Column = new(this);
        public int ColumnFilterCount => Column.GetFilterValue() is null ? 0 : 1; public object? GlobalFilter => null;
        public IBleeckerDataTableFilterColumn? GetColumn(string id) => id == "status" ? Column : null;
        public void ResetColumnFilters() => Column.SetFilterValue(null);
    }
}
