using System.Collections;

namespace Orchard.Catalog;

/// <summary>Independent consumer table with stable columns and original fictional rows.</summary>
internal sealed class SelectFilterTable : IBleeckerDataTableFilterSource
{
    internal sealed record Row(string Id, string Title, string Status);
    internal sealed class Column(SelectFilterTable owner, string id) : IBleeckerDataTableFilterColumn
    {
        internal string Id => id;
        public object? GetFilterValue() => owner.filters.GetValueOrDefault(id);
        public void SetFilterValue(object? value)
        {
            owner.Requests.Add((this, value));
            if (value is null or "") owner.filters.Remove(id); else owner.filters[id] = value;
            owner.Publish();
        }
    }
    private readonly Dictionary<string, Column> columns;
    private readonly Dictionary<string, object?> filters = new(StringComparer.Ordinal);
    private EventHandler? changed;
    internal int Subscribers { get; private set; }
    internal string Name { get; }
    internal Row[] Rows { get; }
    internal List<(Column Column, object? Value)> Requests { get; } = [];
    internal IReadOnlyDictionary<string, object?> Filters => filters;
    internal IEnumerable<Row> VisibleRows => Rows.Where(row => filters.All(pair => pair.Key switch
    {
        "title" => row.Title.Contains(pair.Value is null ? "undefined" : Text(pair.Value), StringComparison.OrdinalIgnoreCase),
        "status" => string.Equals(row.Status, pair.Value is null ? "undefined" : Text(pair.Value), StringComparison.OrdinalIgnoreCase),
        _ => throw new InvalidOperationException("Unknown original consumer column.")
    }));
    internal string VisibleText => string.Join('|', VisibleRows.Select(row => row.Id + ":" + row.Title + ":" + row.Status));
    internal SelectFilterTable(bool second = false, IEnumerable<KeyValuePair<string, object?>>? initial = null)
    {
        Name = second ? "B" : "A";
        Rows = second ? [new("desk", "Desk North", "Published"), new("wire", "Wire West", "Draft")] : [new("harbor", "Harbor", "Draft"), new("reed", "Reed", "Published"), new("vale", "Vale", "Draft")];
        columns = new(StringComparer.Ordinal) { ["title"] = new(this, "title"), ["status"] = new(this, "status") };
        if (initial is not null) foreach (var pair in initial) filters.Add(pair.Key, pair.Value);
    }
    public event EventHandler? Changed { add { changed += value; Subscribers++; } remove { changed -= value; Subscribers--; } }
    public int ColumnFilterCount => filters.Count;
    public object? GlobalFilter => null;
    public IBleeckerDataTableFilterColumn? GetColumn(string id) => columns.GetValueOrDefault(id);
    public void ResetColumnFilters() { filters.Clear(); Publish(); }
    internal void SetExternalFilters(IEnumerable<KeyValuePair<string, object?>> value) { filters.Clear(); foreach (var pair in value) filters.Add(pair.Key, pair.Value); Publish(); }
    private void Publish() => changed?.Invoke(this, EventArgs.Empty);
    private static string Text(object? value) => value switch
    {
        null => "", string text => text, bool flag => flag ? "true" : "false",
        IEnumerable array => string.Join(',', array.Cast<object?>().Select(Text)),
        IFormattable number => number.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => "[object Object]"
    };
}
