using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Orchard;

public enum BleeckerTableSortOrder { Ascending, Descending }
public enum BleeckerTableCellAlignment { Left, Center, Right }

/// <summary>A caller-owned sort field and direction.</summary>
public sealed record BleeckerTableSort(string Field, BleeckerTableSortOrder Order = BleeckerTableSortOrder.Ascending);

/// <summary>An original table column, with optional native content and comparison producers.</summary>
public sealed record BleeckerTableColumn
{
    public BleeckerTableColumn(string key, string header, bool isSortable = true,
        BleeckerTableCellAlignment alignment = BleeckerTableCellAlignment.Left,
        Func<object, object?>? valueSelector = null, Func<object, int, object?>? cellContentSelector = null,
        Comparison<object>? comparison = null)
    {
        ArgumentNullException.ThrowIfNull(key); ArgumentNullException.ThrowIfNull(header);
        Key = key; Header = header; IsSortable = isSortable; Alignment = alignment;
        ValueSelector = valueSelector; CellContentSelector = cellContentSelector; Comparison = comparison;
    }
    public string Key { get; }
    public string Header { get; }
    public bool IsSortable { get; }
    public BleeckerTableCellAlignment Alignment { get; }
    public Func<object, object?>? ValueSelector { get; }
    public Func<object, int, object?>? CellContentSelector { get; }
    public Comparison<object>? Comparison { get; }
}

// The native view exposes IReadOnlySet<string>; its ordered implementation is
// internal and does not add a second public consumer selection factory.
internal sealed class BleeckerTableSelection : IReadOnlySet<string>
{
    private readonly string[] ordered;
    private readonly HashSet<string> keys = new(StringComparer.Ordinal);
    internal BleeckerTableSelection(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var items = new List<string>();
        foreach (var value in values)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (keys.Add(value)) items.Add(value);
        }
        ordered = items.ToArray();
    }
    public int Count => ordered.Length;
    public bool Contains(string item) => keys.Contains(item);
    public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)ordered).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public bool IsProperSubsetOf(IEnumerable<string> other) => keys.IsProperSubsetOf(other);
    public bool IsProperSupersetOf(IEnumerable<string> other) => keys.IsProperSupersetOf(other);
    public bool IsSubsetOf(IEnumerable<string> other) => keys.IsSubsetOf(other);
    public bool IsSupersetOf(IEnumerable<string> other) => keys.IsSupersetOf(other);
    public bool Overlaps(IEnumerable<string> other) => keys.Overlaps(other);
    public bool SetEquals(IEnumerable<string> other) => keys.SetEquals(other);
}

internal sealed record BleeckerTableProjectedRow(object Item, string Key, int Index, bool IsSelected);

// State is separated from the native presenter so rejected controlled requests,
// later release of control, and post-sort keying retain the pinned behavior.
internal sealed class BleeckerTableState
{
    internal BleeckerTableSort InternalSort { get; private set; } = new("");
    internal BleeckerTableSelection InternalSelection { get; private set; } = new([]);
    internal BleeckerTableSort Sort(BleeckerTableSort? controlled) => controlled ?? InternalSort;
    private static BleeckerTableSelection Exact(IReadOnlySet<string> keys) => keys as BleeckerTableSelection ?? new(keys);
    // The producer owns the values, not an alternate membership policy.
    // Source string keys remain ordinal even if a CLR set ignores case.
    internal IReadOnlySet<string> Selection(IReadOnlySet<string>? controlled) => controlled is null ? InternalSelection : Exact(controlled);
    internal BleeckerTableSort RequestSort(string field, BleeckerTableSort current)
    {
        ArgumentNullException.ThrowIfNull(field);
        return InternalSort = new(field, current.Field == field && current.Order == BleeckerTableSortOrder.Ascending
            ? BleeckerTableSortOrder.Descending : BleeckerTableSortOrder.Ascending);
    }
    internal BleeckerTableSelection RequestRow(IReadOnlySet<string> current, string key, bool selected)
    {
        current = Exact(current);
        var values = current.Where(value => selected || value != key).ToList();
        if (selected && !current.Contains(key)) values.Add(key);
        return InternalSelection = new(values);
    }
    internal BleeckerTableSelection RequestAll(IEnumerable<string> rowKeys, bool selected) => InternalSelection = new(selected ? rowKeys : []);
    internal static (bool All, bool Some) SelectionFlags(IEnumerable<string> rowKeys, IReadOnlySet<string> selected)
    {
        selected = Exact(selected);
        var keys = rowKeys.ToArray(); var all = keys.Length > 0 && keys.All(selected.Contains);
        return (all, !all && keys.Any(selected.Contains));
    }
    internal static object? Value(object row, string key)
    {
        ArgumentNullException.ThrowIfNull(row); ArgumentNullException.ThrowIfNull(key);
        if (row is IReadOnlyDictionary<string, object?> values)
        {
            foreach (var pair in values) if (string.Equals(pair.Key, key, StringComparison.Ordinal)) return pair.Value;
            return null;
        }
        if (row is IDictionary dictionary)
        {
            foreach (DictionaryEntry pair in dictionary) if (pair.Key is string name && string.Equals(name, key, StringComparison.Ordinal)) return pair.Value;
            return null;
        }
        var type = row.GetType();
        var property = type.GetProperty(key, BindingFlags.Public | BindingFlags.Instance);
        if (property is not null && property.GetIndexParameters().Length == 0) return property.GetValue(row);
        return type.GetField(key, BindingFlags.Public | BindingFlags.Instance)?.GetValue(row);
    }
    internal static object? CellValue(object row, BleeckerTableColumn column) => column.ValueSelector is { } selector ? selector(row) : Value(row, column.Key);
    internal static string CellText(object? value) => BleeckerDataTableValue.InputText(value);
    private static bool Number(object? value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
    internal static int DefaultCompare(object? a, object? b)
    {
        if (Number(a) && Number(b))
        {
            var difference = Convert.ToDouble(a, CultureInfo.InvariantCulture) - Convert.ToDouble(b, CultureInfo.InvariantCulture);
            return difference > 0 ? 1 : difference < 0 ? -1 : 0;
        }
        return CultureInfo.CurrentCulture.CompareInfo.Compare(CellText(a), CellText(b), CompareOptions.None);
    }
    internal static IReadOnlyList<object> Sorted(IReadOnlyList<object> rows, IReadOnlyList<BleeckerTableColumn> columns, BleeckerTableSort sort)
    {
        if (sort.Field.Length == 0) return rows;
        var column = columns.FirstOrDefault(c => c.Key == sort.Field);
        int Compare(object a, object b)
        {
            var result = column?.Comparison is { } custom ? custom(a, b)
                : DefaultCompare(column is null ? Value(a, sort.Field) : CellValue(a, column), column is null ? Value(b, sort.Field) : CellValue(b, column));
            var sign = Math.Sign(result);
            return sort.Order == BleeckerTableSortOrder.Ascending ? sign : -sign;
        }
        return rows.OrderBy(row => row, Comparer<object>.Create(Compare)).ToArray();
    }
    internal static IReadOnlyList<BleeckerTableProjectedRow> Project(IReadOnlyList<object> sorted, Func<object, int, string> key, IReadOnlySet<string> selected)
    {
        ArgumentNullException.ThrowIfNull(key);
        selected = Exact(selected);
        return sorted.Select((row, index) =>
        {
            var value = key(row, index) ?? throw new ArgumentException("A table row key must be a string.");
            return new BleeckerTableProjectedRow(row, value, index, selected.Contains(value));
        }).ToArray();
    }
}
