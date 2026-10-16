namespace Orchard.Catalog;

/// <summary>Independent fictional consumer state; the picker never owns these records.</summary>
public sealed class ColumnVisibilityTable : IBleeckerDataTableColumnVisibilitySource
{
    public sealed record Row(string Id, string Title, string Status, string Locked);
    public sealed record Request(Column Column, bool Visible);
    public sealed class Column : IBleeckerDataTableVisibilityColumn
    {
        private readonly ColumnVisibilityTable owner;
        internal Column(ColumnVisibilityTable owner, string id, object? header, Func<Row, string>? accessor = null, bool canHide = true, Column[]? children = null)
        { this.owner = owner; Id = id; Header = header; Accessor = accessor; HidingAllowed = canHide; Children = children ?? []; }
        public string Id { get; }
        public object? Header { get; }
        internal Func<Row, string>? Accessor { get; }
        internal Column[] Children { get; }
        internal bool HidingAllowed { get; }
        public bool HasAccessor => Accessor is not null;
        public bool CanHide => owner.EnableHiding && HidingAllowed;
        public bool IsVisible => Children.Length > 0 ? Children.Any(column => column.IsVisible) : !owner.visibility.TryGetValue(Id, out var value) || value;
        public void ToggleVisibility(bool visible)
        {
            if (!CanHide) return;
            owner.visibility[Id] = visible; owner.Requests.Add(new(this, visible)); owner.Publish();
        }
    }
    private EventHandler? changed;
    private bool enableHiding = true;
    private readonly Dictionary<string, bool> visibility = new(StringComparer.Ordinal);
    public event EventHandler? Changed { add { changed += value; Subscribers++; } remove { changed -= value; Subscribers--; } }
    public int Subscribers { get; private set; }
    public string Name { get; }
    public Row[] Rows { get; }
    public Column[] Columns { get; }
    public List<Request> Requests { get; } = [];
    public IReadOnlyDictionary<string, bool> Visibility => visibility;
    public bool EnableHiding { get => enableHiding; set { enableHiding = value; Publish(); } }
    public ColumnVisibilityTable(string kind = "basic", bool second = false, bool emptyRows = false, IReadOnlyDictionary<string, bool>? initial = null)
    {
        Name = second ? "B" : "A";
        Rows = emptyRows ? [] : second ? [new("s1", "Desk North", "Queued", "C"), new("s2", "Desk West", "Ready", "D")] : [new("r1", "Harbor", "Draft", "A"), new("r2", "Reed", "Published", "B")];
        Column Make(string id, object? header, Func<Row, string>? accessor = null, bool canHide = true, Column[]? children = null) => new(this, id, header, accessor, canHide, children);
        Columns = kind switch
        {
            "empty" => [Make("select", "Select"), Make("locked", "Locked", row => row.Locked, false)],
            "long" => Enumerable.Range(0, 20).Select(index => Make("field-" + index, "Field " + index, row => row.Title)).ToArray(),
            "group" => [Make("group", "Group", children: [Make("title", "Story", row => row.Title), Make("status", "Status", row => row.Status)]), Make("extra", "Extra", row => row.Title)],
            "basic" => [Make("select", "Select"), Make("title", second ? "Desk story" : "Story", row => row.Title), Make("status", "Status", row => row.Status), Make("rendered", (Func<string>)(() => "Rendered heading"), row => row.Title), Make("blank", "", row => row.Status), Make("locked", "Locked", row => row.Locked, false)],
            _ => throw new ArgumentException("Unknown fictional column shape.", nameof(kind))
        };
        if (initial is not null) foreach (var pair in initial) visibility.Add(pair.Key, pair.Value);
    }
    public IReadOnlyList<IBleeckerDataTableVisibilityColumn> GetAllColumns() => Columns;
    public IEnumerable<Column> Eligible => Columns.Where(column => column.HasAccessor && column.CanHide);
    public IEnumerable<Column> Leaves => Columns.SelectMany(LeavesOf);
    private static IEnumerable<Column> LeavesOf(Column column) => column.Children.Length == 0 ? [column] : column.Children.SelectMany(LeavesOf);
    public string VisibleCells => string.Join(';', Rows.Select(row => string.Join('|', Leaves.Where(column => column.IsVisible).Select(column => column.Id + "=" + (column.Accessor?.Invoke(row) ?? "")))));
    public void ExternalVisibility(IReadOnlyDictionary<string, bool> values) { visibility.Clear(); foreach (var pair in values) visibility.Add(pair.Key, pair.Value); Publish(); }
    public void Publish() => changed?.Invoke(this, EventArgs.Empty);
}
