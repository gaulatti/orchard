using System.Collections.ObjectModel;
using System.Globalization;

namespace Orchard.Catalog;

// Consumer-owned fictional data. The view receives counts/actions, never rows.
internal sealed class PaginationTable : IBleeckerDataTablePaginationSource
{
    internal sealed record Row(string Id, string Title, string Status);
    private EventHandler? changed;
    private readonly HashSet<string> selection = new(StringComparer.Ordinal);
    private bool draftOnly, descending;
    private readonly bool autoReset;
    internal IReadOnlyList<Row> Rows { get; }
    internal ObservableCollection<Row> VisibleRows { get; } = [];
    internal int Subscribers { get; private set; }
    internal List<(string Name, int? Value)> Requests { get; } = [];
    public event EventHandler? Changed { add { changed += value; Subscribers++; } remove { changed -= value; Subscribers--; } }
    public int PageIndex { get; private set; }
    public int PageSize { get; private set; }
    internal int? ManualPageCount { get; }
    public int PageCount => ManualPageCount ?? (int)Math.Ceiling((double)FilteredRowCount / PageSize);
    public int FilteredRowCount => Filtered().Count();
    public int FilteredSelectedRowCount => Filtered().Count(row => selection.Contains(row.Id));
    public bool CanPreviousPage => PageIndex > 0;
    public bool CanNextPage => PageCount == -1 || PageCount > 0 && PageIndex < PageCount - 1;
    internal string VisibleText => string.Join('|', VisibleRows.Select(row => row.Title));
    internal IReadOnlySet<string> Selection => selection;
    internal bool DraftOnly => draftOnly;
    internal bool Descending => descending;
    internal PaginationTable(int rows = 47, int pageIndex = 0, int pageSize = 10, int? manualPageCount = null, string prefix = "Coverage ", string idPrefix = "row-", bool autoReset = true)
    {
        Rows = Array.AsReadOnly(Enumerable.Range(1, rows).Select(index => new Row(idPrefix + index.ToString("D3", CultureInfo.InvariantCulture), prefix + index.ToString("D3", CultureInfo.InvariantCulture), index % 2 == 1 ? "Draft" : "Published")).ToArray());
        PageIndex = pageIndex; PageSize = pageSize; ManualPageCount = manualPageCount; this.autoReset = autoReset; Publish();
    }
    private IEnumerable<Row> Filtered() => Rows.Where(row => !draftOnly || row.Status == "Draft");
    public void SetPageIndex(int value) { Requests.Add((nameof(SetPageIndex), value)); ApplyIndex(value); }
    public void PreviousPage() { Requests.Add((nameof(PreviousPage), null)); ApplyIndex(PageIndex - 1); }
    public void NextPage() { Requests.Add((nameof(NextPage), null)); ApplyIndex(PageIndex + 1); }
    private void ApplyIndex(int value) { PageIndex = Math.Max(0, Math.Min(value, ManualPageCount is >= 0 ? ManualPageCount.Value - 1 : int.MaxValue)); Publish(); }
    public void SetPageSize(int value)
    {
        Requests.Add((nameof(SetPageSize), value)); var next = Math.Max(1, value); var top = (long)PageSize * PageIndex;
        PageSize = next; PageIndex = checked((int)(top / next)); Publish();
    }
    internal void SetPagination(int index, int size) { PageIndex = index; PageSize = size; Publish(); }
    internal void SetSelection(IEnumerable<string> ids) { selection.Clear(); foreach (var id in ids) selection.Add(id); Publish(); }
    internal void SetFilter(bool draft) { draftOnly = draft; if (autoReset) PageIndex = 0; Publish(); }
    internal void SetSort(bool reverse) { descending = reverse; if (autoReset) PageIndex = 0; Publish(); }
    private void Publish()
    {
        var ordered = descending ? Filtered().OrderByDescending(row => row.Title, StringComparer.Ordinal) : Filtered();
        var current = ManualPageCount is null ? ordered.Skip(checked(PageIndex * PageSize)).Take(PageSize).ToArray() : ordered.ToArray();
        VisibleRows.Clear(); foreach (var row in current) VisibleRows.Add(row); changed?.Invoke(this, EventArgs.Empty);
    }
}
