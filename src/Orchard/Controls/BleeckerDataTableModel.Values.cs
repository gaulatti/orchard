using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Orchard;

public sealed partial class BleeckerDataTableModel
{
    private static bool Missing(object? value) => ReferenceEquals(value, Undefined);
    private static bool Nullish(object? value) => value is null || Missing(value);
    private static bool Number(object? value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
    public static string CellText(object? value) => Missing(value) ? "" : BleeckerDataTableValue.InputText(value);
    internal static object? Access(object row, string key)
    {
        object? value = row;
        foreach (var part in key.Split('.'))
        {
            if (Nullish(value)) return Undefined;
            if (value is IDictionary dictionary)
            {
                object? next = Undefined; foreach (DictionaryEntry entry in dictionary) if (entry.Key is string name && string.Equals(name, part, StringComparison.Ordinal)) { next = entry.Value; break; } value = next;
            }
            else if (value is IReadOnlyDictionary<string, object?> map)
            {
                object? next = Undefined; foreach (var entry in map) if (string.Equals(entry.Key, part, StringComparison.Ordinal)) { next = entry.Value; break; } value = next;
            }
            else if (value is IList list && int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var index)) value = index < list.Count ? list[index] : Undefined;
            else
            {
                var type = value!.GetType(); var property = type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance);
                value = property is { CanRead: true } && property.GetIndexParameters().Length == 0 ? property.GetValue(value) : type.GetField(part, BindingFlags.Public | BindingFlags.Instance) is { } field ? field.GetValue(value) : Undefined;
            }
        }
        return value;
    }
    private static bool StrictEqual(object? a, object? b)
    {
        if (Number(a) && Number(b)) return Convert.ToDouble(a, CultureInfo.InvariantCulture) == Convert.ToDouble(b, CultureInfo.InvariantCulture);
        return a is string x && b is string y ? string.Equals(x, y, StringComparison.Ordinal) : a is bool p && b is bool q ? p == q : ReferenceEquals(a, b);
    }
    private static double Numeric(object? value)
    {
        if (Missing(value)) return double.NaN; if (value is null) return 0; if (value is bool flag) return flag ? 1 : 0;
        if (Number(value)) return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        if (value is DateTime date) return new DateTimeOffset(date.ToUniversalTime()).ToUnixTimeMilliseconds();
        if (value is DateTimeOffset offset) return offset.ToUnixTimeMilliseconds();
        var text = CellText(value).Trim(); if (text.Length == 0) return 0;
        if (text is "Infinity" or "+Infinity") return double.PositiveInfinity; if (text == "-Infinity") return double.NegativeInfinity;
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : double.NaN;
    }
    private static bool WeakEqual(object? a, object? b)
    {
        if (Nullish(a) || Nullish(b)) return Nullish(a) && Nullish(b); if (StrictEqual(a, b)) return true;
        return a is bool || b is bool || Number(a) && b is string || a is string && Number(b) ? Numeric(a) == Numeric(b) : false;
    }
    private static bool Greater(object? a, object? b) => a is string x && b is string y ? string.CompareOrdinal(x, y) > 0 : Numeric(a) > Numeric(b);
    private static double ParseFloat(object? value)
    {
        if (Number(value)) return Numeric(value);
        var text = Missing(value) ? "undefined" : CellText(value);
        var match = Regex.Match(text, @"^\s*([+-]?(?:Infinity|(?:(?:\d+\.?\d*)|(?:\.\d+))(?:[eE][+-]?\d+)?))");
        return match.Success ? Numeric(match.Groups[1].Value) : double.NaN;
    }
    private static (object? Low, object? High) Bounds(object? value)
    {
        var items = value is string text ? text.Select(c => (object)c.ToString()).ToArray()
            : value is IEnumerable values ? values.Cast<object?>().Take(2).ToArray() : throw new ArgumentException("A numeric range filter requires an array or string.");
        return (items.Length > 0 ? items[0] : Undefined, items.Length > 1 ? items[1] : Undefined);
    }
    private static bool EmptyFilter(object? value) => Nullish(value) || value is string { Length: 0 };
    private BleeckerDataTableFiltering Filtering(BleeckerDataTableColumnContext column)
    {
        if (column.Definition.Filtering != BleeckerDataTableFiltering.Auto) return column.Definition.Filtering;
        var value = core.FirstOrDefault()?.GetValue(column.Id) ?? Undefined;
        if (value is string) return BleeckerDataTableFiltering.IncludesString;
        if (Number(value)) return BleeckerDataTableFiltering.NumberRange;
        // Pinned auto detection checks objects before its array branch.
        if (!Nullish(value)) return BleeckerDataTableFiltering.Equals;
        return BleeckerDataTableFiltering.WeakEquals;
    }
    private bool RemoveFilter(BleeckerDataTableColumnContext column, object? value)
    {
        if (EmptyFilter(value)) return true; if (column.Definition.Filter is not null) return false;
        var kind = Filtering(column);
        if (kind == BleeckerDataTableFiltering.NumberRange) { var bounds = Bounds(value); return EmptyFilter(bounds.Low) && EmptyFilter(bounds.High); }
        return kind is BleeckerDataTableFiltering.ArrayIncludesAll or BleeckerDataTableFiltering.ArrayIncludesSome && value is IEnumerable values && !values.Cast<object?>().Any();
    }
    private BleeckerDataTableSorting SortingKind(BleeckerDataTableColumnContext column)
    {
        if (column.Definition.Sorting != BleeckerDataTableSorting.Auto) return column.Definition.Sorting;
        var hasText = false;
        // This intentionally matches8.21.3's slice(10), including short arrays.
        foreach (var row in FilteredRows.Skip(10))
        {
            var value = row.GetValue(column.Id); if (value is DateTime or DateTimeOffset) return BleeckerDataTableSorting.DateTime;
            if (value is string text) { hasText = true; if (Regex.IsMatch(text, "[0-9]")) return BleeckerDataTableSorting.Alphanumeric; }
        }
        return hasText ? BleeckerDataTableSorting.Text : BleeckerDataTableSorting.Basic;
    }
    private static string SortText(object? value) => value is string text ? text : Number(value) && double.IsFinite(Numeric(value)) ? CellText(value) : "";
    private static int Alphanumeric(string a, string b)
    {
        var first = Regex.Matches(a, @"[0-9]+|[^0-9]+").Select(m => m.Value).ToArray(); var second = Regex.Matches(b, @"[0-9]+|[^0-9]+").Select(m => m.Value).ToArray();
        for (var i = 0; i < Math.Min(first.Length, second.Length); i++)
        {
            var x = first[i]; var y = second[i]; var xn = Regex.IsMatch(x, "^[0-9]+$"); var yn = Regex.IsMatch(y, "^[0-9]+$");
            var result = xn && yn ? Numeric(x).CompareTo(Numeric(y)) : xn != yn ? xn ? 1 : -1 : string.CompareOrdinal(x, y);
            if (result != 0) return Math.Sign(result);
        }
        return first.Length - second.Length;
    }
    private IReadOnlyList<BleeckerDataTableRow> SortRows(IReadOnlyList<BleeckerDataTableRow> rows)
    {
        var active = sorting.Select(sort => (Sort: sort, Column: GetColumn(sort.Id))).Where(s => s.Column?.CanSort == true).Select(s => (s.Sort, Column: s.Column!, Kind: SortingKind(s.Column!))).ToArray();
        if (active.Length == 0) return rows;
        var result = rows.ToArray(); Array.Sort(result, (a, b) =>
        {
            foreach (var (sort, column, kind) in active)
            {
                var x = a.GetValue(column.Id); var y = b.GetValue(column.Id); var value = 0; var missingX = Missing(x); var missingY = Missing(y);
                if ((missingX || missingY) && column.Definition.UndefinedOrder != BleeckerDataTableUndefinedOrder.None)
                {
                    if (column.Definition.UndefinedOrder == BleeckerDataTableUndefinedOrder.First) return missingX ? -1 : 1;
                    if (column.Definition.UndefinedOrder == BleeckerDataTableUndefinedOrder.Last) return missingX ? 1 : -1;
                    value = missingX == missingY ? 0 : missingX ? 1 : -1;
                    if (column.Definition.UndefinedOrder == BleeckerDataTableUndefinedOrder.AscendingFirst) value = -value;
                }
                if (value == 0) value = column.Definition.Comparison is { } custom ? custom(a, b, column.Id) : kind switch
                {
                    BleeckerDataTableSorting.Basic => StrictEqual(x, y) ? 0 : Greater(x, y) ? 1 : -1,
                    BleeckerDataTableSorting.DateTime => Numeric(x) > Numeric(y) ? 1 : Numeric(x) < Numeric(y) ? -1 : 0,
                    BleeckerDataTableSorting.Text => string.CompareOrdinal(SortText(x).ToLowerInvariant(), SortText(y).ToLowerInvariant()),
                    BleeckerDataTableSorting.TextCaseSensitive => string.CompareOrdinal(SortText(x), SortText(y)),
                    BleeckerDataTableSorting.Alphanumeric => Alphanumeric(SortText(x).ToLowerInvariant(), SortText(y).ToLowerInvariant()),
                    BleeckerDataTableSorting.AlphanumericCaseSensitive => Alphanumeric(SortText(x), SortText(y)),
                    _ => throw new InvalidOperationException("Automatic sorting must be resolved before use.")
                };
                if (value != 0) return Math.Sign(value) * (sort.Descending ? -1 : 1) * (column.Definition.InvertSorting ? -1 : 1);
            }
            return a.Index.CompareTo(b.Index);
        }); return result;
    }
}
