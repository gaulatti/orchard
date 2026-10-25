namespace Orchard;

public enum BleeckerDataTableSorting { Auto, Basic, Text, TextCaseSensitive, Alphanumeric, AlphanumericCaseSensitive, DateTime }
public enum BleeckerDataTableFiltering { Auto, IncludesString, IncludesStringSensitive, EqualsString, Equals, WeakEquals, ArrayIncludes, ArrayIncludesAll, ArrayIncludesSome, NumberRange }
public enum BleeckerDataTableUndefinedOrder { None, AscendingFirst, AscendingLast, First, Last }
public sealed record BleeckerDataTableSort(string Id, bool Descending);
public sealed record BleeckerDataTableFilter(string Id, object? Value);

/// <summary>A caller-owned column definition and its original content producers.</summary>
public sealed class BleeckerDataTableColumn
{
    public BleeckerDataTableColumn(string? id = null, string? accessorKey = null,
        Func<object, int, object?>? accessor = null, object? header = null,
        Func<BleeckerDataTableHeader, object?>? headerContentSelector = null,
        Func<BleeckerDataTableCell, object?>? cellContentSelector = null,
        IEnumerable<BleeckerDataTableColumn>? columns = null,
        BleeckerDataTableSorting sorting = BleeckerDataTableSorting.Auto,
        BleeckerDataTableFiltering filtering = BleeckerDataTableFiltering.Auto,
        Func<BleeckerDataTableRow, BleeckerDataTableRow, string, int>? comparison = null,
        Func<BleeckerDataTableRow, string, object?, bool>? filter = null,
        bool enableSorting = true, bool enableMultiSort = true, bool enableHiding = true,
        bool enableGlobalFilter = true, bool? sortDescendingFirst = null, bool invertSorting = false,
        BleeckerDataTableUndefinedOrder undefinedOrder = BleeckerDataTableUndefinedOrder.AscendingLast)
    {
        Id = id ?? accessorKey?.Replace('.', '_') ?? header as string ?? throw new ArgumentException("An accessor function or display column requires an ID or string header.");
        AccessorKey = accessorKey; Accessor = accessor; Header = header ?? accessorKey ?? (accessor is not null ? Id : null);
        HeaderContentSelector = headerContentSelector; CellContentSelector = cellContentSelector; Columns = columns?.ToArray() ?? [];
        Sorting = sorting; Filtering = filtering; Comparison = comparison; Filter = filter;
        EnableSorting = enableSorting; EnableMultiSort = enableMultiSort; EnableHiding = enableHiding; EnableGlobalFilter = enableGlobalFilter;
        SortDescendingFirst = sortDescendingFirst; InvertSorting = invertSorting; UndefinedOrder = undefinedOrder;
    }
    public string Id { get; }
    public string? AccessorKey { get; }
    public Func<object, int, object?>? Accessor { get; }
    public object? Header { get; }
    public Func<BleeckerDataTableHeader, object?>? HeaderContentSelector { get; }
    public Func<BleeckerDataTableCell, object?>? CellContentSelector { get; }
    public IReadOnlyList<BleeckerDataTableColumn> Columns { get; }
    public BleeckerDataTableSorting Sorting { get; }
    public BleeckerDataTableFiltering Filtering { get; }
    public Func<BleeckerDataTableRow, BleeckerDataTableRow, string, int>? Comparison { get; }
    public Func<BleeckerDataTableRow, string, object?, bool>? Filter { get; }
    public bool EnableSorting { get; }
    public bool EnableMultiSort { get; }
    public bool EnableHiding { get; }
    public bool EnableGlobalFilter { get; }
    public bool? SortDescendingFirst { get; }
    public bool InvertSorting { get; }
    public BleeckerDataTableUndefinedOrder UndefinedOrder { get; }
}

/// <summary>The original column context exposed to content and companion controls.</summary>
public sealed class BleeckerDataTableColumnContext : IBleeckerDataTableFilterColumn, IBleeckerDataTableVisibilityColumn
{
    internal BleeckerDataTableColumnContext(BleeckerDataTableModel table, BleeckerDataTableColumn definition, BleeckerDataTableColumnContext? parent)
    { Table = table; Definition = definition; Parent = parent; }
    public BleeckerDataTableModel Table { get; }
    public BleeckerDataTableColumn Definition { get; }
    public BleeckerDataTableColumnContext? Parent { get; }
    public int Depth => Parent is null ? 0 : Parent.Depth + 1;
    public string Id => Definition.Id;
    public object? Header => Definition.Header;
    public bool HasAccessor => Definition.AccessorKey is not null || Definition.Accessor is not null;
    public bool CanSort => Definition.EnableSorting && HasAccessor;
    public bool CanHide => Definition.EnableHiding;
    public bool IsVisible => Children.Count > 0 ? Children.Any(c => c.IsVisible) : !Table.ColumnVisibility.TryGetValue(Id, out var visible) || visible;
    public bool? IsSorted => Table.Sorting.FirstOrDefault(s => s.Id == Id)?.Descending;
    public IReadOnlyList<BleeckerDataTableColumnContext> Children { get; internal set; } = [];
    public object? GetFilterValue() => Table.ColumnFilters.FirstOrDefault(f => f.Id == Id)?.Value;
    public void SetFilterValue(object? value) => Table.SetColumnFilter(this, value);
    public void ToggleVisibility(bool visible) { if (CanHide) Table.SetVisibility(Id, visible); }
    public void ToggleSorting(bool? descending = null, bool multi = false) => Table.ToggleSorting(this, descending, multi);
    public void ClearSorting() => Table.SetSorting(Table.Sorting.Where(s => s.Id != Id));
}

public sealed class BleeckerDataTableRow
{
    private readonly Dictionary<string, object?> values = new(StringComparer.Ordinal);
    internal BleeckerDataTableRow(BleeckerDataTableModel table, object original, int index)
    { Table = table; Original = original; Index = index; Id = index.ToString(System.Globalization.CultureInfo.InvariantCulture); }
    public BleeckerDataTableModel Table { get; }
    public object Original { get; }
    public int Index { get; }
    public string Id { get; }
    public bool IsSelected => Table.IsRowSelected(Id);
    public object? GetValue(string columnId)
    {
        if (values.TryGetValue(columnId, out var value)) return value;
        var column = Table.GetColumn(columnId);
        value = column?.Definition.Accessor is { } accessor ? accessor(Original, Index)
            : column?.Definition.AccessorKey is { } key ? BleeckerDataTableModel.Access(Original, key) : BleeckerDataTableModel.Undefined;
        values.Add(columnId, value); return value;
    }
    public void ToggleSelected(bool? selected = null) => Table.ToggleRowSelected(Id, selected ?? !IsSelected);
    public IReadOnlyList<BleeckerDataTableCell> GetVisibleCells() => Table.VisibleColumns.Select(column => new BleeckerDataTableCell(this, column)).ToArray();
}

public sealed class BleeckerDataTableCell(BleeckerDataTableRow row, BleeckerDataTableColumnContext column)
{
    public BleeckerDataTableRow Row { get; } = row;
    public BleeckerDataTableColumnContext Column { get; } = column;
    public BleeckerDataTableModel Table => Row.Table;
    public string Id => Row.Id + "_" + Column.Id;
    public object? GetValue() => Row.GetValue(Column.Id);
    public object? RenderContent() => Column.Definition.CellContentSelector is { } render ? render(this) : BleeckerDataTableModel.CellText(GetValue());
}

public sealed class BleeckerDataTableHeader
{
    internal BleeckerDataTableHeader(BleeckerDataTableColumnContext column, string id, int depth, bool placeholder = false)
    { Column = column; Id = id; Depth = depth; IsPlaceholder = placeholder; }
    public BleeckerDataTableColumnContext Column { get; }
    public BleeckerDataTableModel Table => Column.Table;
    public string Id { get; }
    public int Depth { get; }
    public bool IsPlaceholder { get; }
    public int ColumnSpan => SubHeaders.Count == 0 ? 1 : SubHeaders.Sum(h => h.ColumnSpan);
    public int RowSpan => 0;
    internal List<BleeckerDataTableHeader> SubHeaders { get; } = [];
    public object? RenderContent() => IsPlaceholder ? null : Column.Definition.HeaderContentSelector is { } render ? render(this) : Column.Header;
}
public sealed record BleeckerDataTableHeaderGroup(string Id, IReadOnlyList<BleeckerDataTableHeader> Headers);
