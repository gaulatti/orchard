using System.Collections;

namespace Orchard;

// State/row-model algorithms adapted from TanStack table-core8.21.3 (MIT).
// See licenses/LICENSE-TanStackTable.txt and docs/data-table.md.
/// <summary>The Data Table's client row model, shared with native content contexts.</summary>
public sealed partial class BleeckerDataTableModel : IBleeckerDataTableFilterSource, IBleeckerDataTablePaginationSource, IBleeckerDataTableColumnVisibilitySource
{
    private sealed class UndefinedValue { }
    /// <summary>Represents a missing JavaScript value separately from an explicit null.</summary>
    public static object Undefined { get; } = new UndefinedValue();
    private readonly Dictionary<string, BleeckerDataTableColumnContext> columns = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> visibility = new(StringComparer.Ordinal);
    private readonly HashSet<string> selected = new(StringComparer.Ordinal);
    private BleeckerDataTableSort[] sorting = [];
    private BleeckerDataTableFilter[] filters = [];
    private BleeckerDataTableRow[] core = [];
    public event EventHandler? Changed;
    public IReadOnlyList<BleeckerDataTableColumnContext> Columns { get; private set; } = [];
    public IReadOnlyList<BleeckerDataTableColumnContext> LeafColumns { get; private set; } = [];
    public IReadOnlyList<BleeckerDataTableColumnContext> VisibleColumns => LeafColumns.Where(c => c.IsVisible).ToArray();
    public IReadOnlyList<BleeckerDataTableSort> Sorting => sorting;
    public IReadOnlyList<BleeckerDataTableFilter> ColumnFilters => filters;
    public IReadOnlyDictionary<string, bool> ColumnVisibility => visibility;
    public IReadOnlyList<BleeckerDataTableRow> CoreRows => core;
    public IReadOnlyList<BleeckerDataTableRow> FilteredRows { get; private set; } = [];
    public IReadOnlyList<BleeckerDataTableRow> SortedRows { get; private set; } = [];
    public IReadOnlyList<BleeckerDataTableRow> PageRows => SortedRows.Skip(PageIndex * PageSize).Take(PageSize).ToArray();
    public IReadOnlyList<BleeckerDataTableRow> FilteredSelectedRows => FilteredRows.Where(r => r.IsSelected).ToArray();
    public IReadOnlySet<string> SelectedRowIds => new BleeckerTableSelection(selected);
    public int PageIndex { get; private set; }
    public int PageSize { get; private set; } = 10;
    public int PageCount => (int)Math.Ceiling((double)FilteredRows.Count / PageSize);
    public int FilteredRowCount => FilteredRows.Count;
    public int FilteredSelectedRowCount => FilteredSelectedRows.Count;
    public int ColumnFilterCount => filters.Length;
    public object? GlobalFilter { get; private set; }
    public bool CanPreviousPage => PageIndex > 0;
    public bool CanNextPage => PageIndex < PageCount - 1;
    public bool AllPageRowsSelected => PageRows.Count > 0 && PageRows.All(r => r.IsSelected);
    public BleeckerDataTableColumnContext? GetColumn(string id) => columns.GetValueOrDefault(id);
    IBleeckerDataTableFilterColumn? IBleeckerDataTableFilterSource.GetColumn(string id) => GetColumn(id);
    IReadOnlyList<IBleeckerDataTableVisibilityColumn> IBleeckerDataTableColumnVisibilitySource.GetAllColumns() => Columns;
    public void SetData(IEnumerable? data)
    {
        var items = data?.Cast<object>().ToArray() ?? [];
        if (items.Any(item => item is null)) throw new ArgumentException("Data rows must be original non-null objects.", nameof(data));
        core = items.Select((item, i) => new BleeckerDataTableRow(this, item, i)).ToArray(); Recompute(true);
    }
    public void SetColumns(IEnumerable<BleeckerDataTableColumn> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions); var nextColumns = new Dictionary<string, BleeckerDataTableColumnContext>(StringComparer.Ordinal);
        BleeckerDataTableColumnContext Create(BleeckerDataTableColumn definition, BleeckerDataTableColumnContext? parent)
        {
            ArgumentNullException.ThrowIfNull(definition); var context = new BleeckerDataTableColumnContext(this, definition, parent);
            if (!nextColumns.TryAdd(context.Id, context)) throw new ArgumentException("Data Table column IDs must be unique.", nameof(definitions));
            context.Children = definition.Columns.Select(child => Create(child, context)).ToArray(); return context;
        }
        var roots = definitions.Select(definition => Create(definition, null)).ToArray();
        columns.Clear(); foreach (var pair in nextColumns) columns.Add(pair.Key, pair.Value); Columns = roots;
        LeafColumns = columns.Values.Where(c => c.Children.Count == 0).ToArray();
        core = core.Select(row => new BleeckerDataTableRow(this, row.Original, row.Index)).ToArray(); Recompute(false);
    }
    public void RefreshData() { core = core.Select(row => new BleeckerDataTableRow(this, row.Original, row.Index)).ToArray(); Recompute(true); }
    public void SetSorting(IEnumerable<BleeckerDataTableSort> value) { sorting = value.ToArray(); Recompute(true); }
    internal void ToggleSorting(BleeckerDataTableColumnContext column, bool? descending, bool multi)
    {
        var existing = Array.FindIndex(sorting, s => s.Id == column.Id);
        var first = column.Definition.SortDescendingFirst ?? (FilteredRows.FirstOrDefault()?.GetValue(column.Id) is not string);
        var next = existing < 0 ? first : sorting[existing].Descending == first ? !first : (bool?)null;
        var result = sorting.ToList();
        var action = result.Count > 0 && column.Definition.EnableMultiSort && multi ? existing >= 0 ? "toggle" : "add"
            : result.Count > 0 && existing != result.Count - 1 ? "replace" : existing >= 0 ? "toggle" : "replace";
        if (action == "toggle" && descending is null && next is null) action = "remove";
        var sort = new BleeckerDataTableSort(column.Id, descending ?? next ?? false);
        if (action == "replace") result = [sort]; else if (action == "add") result.Add(sort); else if (action == "remove") result.RemoveAt(existing); else result[existing] = sort;
        SetSorting(result);
    }
    internal void SetColumnFilter(BleeckerDataTableColumnContext column, object? value)
    {
        var next = filters.ToList(); var index = next.FindIndex(f => f.Id == column.Id);
        if (RemoveFilter(column, value)) { if (index >= 0) next.RemoveAt(index); }
        else if (index < 0) next.Add(new(column.Id, value)); else next[index] = new(column.Id, value);
        filters = next.ToArray(); Recompute(true);
    }
    public void ResetColumnFilters() { filters = []; Recompute(true); }
    public void SetGlobalFilter(object? value) { GlobalFilter = value; Recompute(true); }
    internal void SetVisibility(string id, bool visible) { visibility[id] = visible; Changed?.Invoke(this, EventArgs.Empty); }
    public bool IsRowSelected(string id) => selected.Contains(id);
    public void SetRowSelection(IEnumerable<string> ids) { selected.Clear(); selected.UnionWith(ids); Changed?.Invoke(this, EventArgs.Empty); }
    internal void ToggleRowSelected(string id, bool value) { if (value) selected.Add(id); else selected.Remove(id); Changed?.Invoke(this, EventArgs.Empty); }
    public void ToggleAllPageRowsSelected(bool? value = null)
    {
        var next = value ?? !AllPageRowsSelected; foreach (var row in PageRows) { if (next) selected.Add(row.Id); else selected.Remove(row.Id); } Changed?.Invoke(this, EventArgs.Empty);
    }
    public void ToggleAllRowsSelected(bool? value = null)
    {
        var next = value ?? !(FilteredRows.Count > 0 && FilteredRows.All(r => r.IsSelected)); foreach (var row in FilteredRows) { if (next) selected.Add(row.Id); else selected.Remove(row.Id); } Changed?.Invoke(this, EventArgs.Empty);
    }
    public void SetPageIndex(int pageIndex) { PageIndex = Math.Clamp(pageIndex, 0, Math.Max(0, PageCount - 1)); Changed?.Invoke(this, EventArgs.Empty); }
    public void SetPageSize(int pageSize)
    {
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));
        var top = PageIndex * PageSize; PageSize = pageSize; PageIndex = top / pageSize; Changed?.Invoke(this, EventArgs.Empty);
    }
    public void PreviousPage() => SetPageIndex(PageIndex - 1);
    public void NextPage() => SetPageIndex(PageIndex + 1);
    private void Recompute(bool resetPage)
    {
        FilteredRows = FilterRows(); SortedRows = SortRows(FilteredRows);
        if (resetPage) PageIndex = 0; Changed?.Invoke(this, EventArgs.Empty);
    }
    public IReadOnlyList<BleeckerDataTableHeaderGroup> GetHeaderGroups()
    {
        var maxDepth = 1; void Find(IReadOnlyList<BleeckerDataTableColumnContext> values, int depth) { maxDepth = Math.Max(maxDepth, depth); foreach (var column in values.Where(c => c.IsVisible && c.Children.Count > 0)) Find(column.Children, depth + 1); } Find(Columns, 1);
        var groups = new List<BleeckerDataTableHeaderGroup>();
        void Group(IReadOnlyList<BleeckerDataTableHeader> headers, int depth)
        {
            groups.Add(new(depth.ToString(System.Globalization.CultureInfo.InvariantCulture), headers)); var parents = new List<BleeckerDataTableHeader>();
            foreach (var header in headers)
            {
                var column = header.Column.Depth == depth && header.Column.Parent is { } parent ? parent : header.Column;
                var placeholder = ReferenceEquals(column, header.Column);
                if (parents.LastOrDefault() is { } last && ReferenceEquals(last.Column, column)) last.SubHeaders.Add(header);
                else { var next = new BleeckerDataTableHeader(column, depth + "_" + column.Id + "_" + header.Id, depth, placeholder); next.SubHeaders.Add(header); parents.Add(next); }
            }
            if (depth > 0) Group(parents, depth - 1);
        }
        Group(VisibleColumns.Select(c => new BleeckerDataTableHeader(c, c.Id, maxDepth)).ToArray(), maxDepth - 1); groups.Reverse(); return groups;
    }
}
