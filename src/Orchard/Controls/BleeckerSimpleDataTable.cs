using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>The reusable table.tsx DataTable composition, independent of TanStack.</summary>
public sealed class BleeckerSimpleDataTable : Control
{
    private readonly BleeckerTable table = new();
    private readonly BleeckerTableHeader header = new();
    private readonly BleeckerTableRow heading = new();
    private readonly BleeckerTableBody body = new();
    private readonly BleeckerTableFooter footer = new();
    private readonly BleeckerTableCaption caption = new();
    private readonly BleeckerTableRow emptyRow = new() { MinHeight = 96 };
    private readonly BleeckerTableCell emptyCell = new() { HorizontalContentAlignment = HorizontalAlignment.Center };
    private readonly BleeckerTableHead allCell = new() { ColumnWidth = new GridLength(40) };
    private readonly BleeckerCheckbox all = Checkbox("Select all");
    private readonly BleeckerTableState state = new();
    private readonly Dictionary<string, BleeckerSortableTableHeader> headers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RowView> views = new(StringComparer.Ordinal);
    private readonly List<INotifyPropertyChanged> observedRows = [];
    private Grid? host;
    private TextBlock? secondary;
    private INotifyCollectionChanged? observedItems;
    private bool refreshing, refreshAgain;
    private IReadOnlyList<BleeckerTableProjectedRow> projection = [];
    private Func<object, int, string>? getRowKey;
    private Func<IReadOnlyList<object>, IEnumerable<BleeckerTableRow>>? footerRowsSelector;
    private Func<BleeckerTableColumn, Style?>? cellStyleSelector, headerStyleSelector;
    public BleeckerSimpleDataTable()
    {
        DefaultStyleKey = typeof(BleeckerSimpleDataTable); IsTabStop = false; UseLayoutRounding = false;
        header.Children.Add(heading); emptyRow.Cells.Add(emptyCell); allCell.Content = all;
        all.Checked += AllToggled; all.Unchecked += AllToggled;
        Columns.CollectionChanged += (_, _) => RefreshIfMounted();
        Loaded += (_, _) => { Observe(); Refresh(); };
        Unloaded += (_, _) => StopObserving();
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) { host?.Children.Clear(); host = null; secondary = null; } });
        ActualThemeChanged += (_, _) => { if (secondary is not null) emptyCell.Foreground = secondary.Foreground; };
    }
    public ObservableCollection<BleeckerTableColumn> Columns { get; } = [];
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(BleeckerSimpleDataTable), new PropertyMetadata(null, InputsChanged));
    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public static readonly DependencyProperty SortProperty = DependencyProperty.Register(nameof(Sort), typeof(object), typeof(BleeckerSimpleDataTable), new PropertyMetadata(null, Changed));
    /// <summary>Null uses internal state. Requests also update internal state while controlled.</summary>
    public BleeckerTableSort? Sort { get => (BleeckerTableSort?)GetValue(SortProperty); set => SetValue(SortProperty, value); }
    public static readonly DependencyProperty SelectedKeysProperty = DependencyProperty.Register(nameof(SelectedKeys), typeof(object), typeof(BleeckerSimpleDataTable), new PropertyMetadata(null, Changed));
    public IReadOnlySet<string>? SelectedKeys { get => (IReadOnlySet<string>?)GetValue(SelectedKeysProperty); set => SetValue(SelectedKeysProperty, value); }
    public static readonly DependencyProperty IsSelectableProperty = DependencyProperty.Register(nameof(IsSelectable), typeof(bool), typeof(BleeckerSimpleDataTable), new PropertyMetadata(false, Changed));
    public bool IsSelectable { get => (bool)GetValue(IsSelectableProperty); set => SetValue(IsSelectableProperty, value); }
    public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(nameof(Caption), typeof(string), typeof(BleeckerSimpleDataTable), new PropertyMetadata(null, Changed));
    public string? Caption { get => (string?)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }
    public static readonly DependencyProperty EmptyMessageProperty = DependencyProperty.Register(nameof(EmptyMessage), typeof(string), typeof(BleeckerSimpleDataTable), new PropertyMetadata("No results.", Changed));
    public string? EmptyMessage { get => (string?)GetValue(EmptyMessageProperty); set => SetValue(EmptyMessageProperty, value); }
    /// <summary>Required for nonempty data; called with the original item and its sorted index.</summary>
    public Func<object, int, string>? GetRowKey { get => getRowKey; set { getRowKey = value; RefreshIfMounted(); } }
    /// <summary>Receives original sorted items and returns original native footer rows.</summary>
    public Func<IReadOnlyList<object>, IEnumerable<BleeckerTableRow>>? FooterRowsSelector { get => footerRowsSelector; set { footerRowsSelector = value; RefreshIfMounted(); } }
    public Func<BleeckerTableColumn, Style?>? CellStyleSelector { get => cellStyleSelector; set { cellStyleSelector = value; RefreshIfMounted(); } }
    public Func<BleeckerTableColumn, Style?>? HeaderStyleSelector { get => headerStyleSelector; set { headerStyleSelector = value; RefreshIfMounted(); } }
    public BleeckerTableSort EffectiveSort => state.Sort(Sort);
    public IReadOnlySet<string> EffectiveSelectedKeys => state.Selection(SelectedKeys);
    public event EventHandler<BleeckerTableSort>? SortRequested;
    public event EventHandler<IReadOnlySet<string>>? SelectionRequested;
    private static void Changed(DependencyObject owner, DependencyPropertyChangedEventArgs _) => ((BleeckerSimpleDataTable)owner).RefreshIfMounted();
    private static void InputsChanged(DependencyObject owner, DependencyPropertyChangedEventArgs _)
    {
        var control = (BleeckerSimpleDataTable)owner; if (control.IsLoaded) control.Observe(); control.RefreshIfMounted();
    }
    protected override void OnApplyTemplate()
    {
        host?.Children.Clear(); base.OnApplyTemplate();
        host = GetTemplateChild("PART_TableHost") as Grid ?? throw new InvalidOperationException("A simple DataTable requires PART_TableHost.");
        secondary = GetTemplateChild("PART_Secondary") as TextBlock ?? throw new InvalidOperationException("A simple DataTable requires PART_Secondary.");
        emptyCell.Foreground = secondary.Foreground; host.Children.Add(table); Refresh();
    }
    private static BleeckerCheckbox Checkbox(string name)
    {
        var checkbox = new BleeckerCheckbox { Width = 20, Height = 20, MinWidth = 0, MinHeight = 0, IsThreeState = false, UseLayoutRounding = false };
        AutomationProperties.SetName(checkbox, name); return checkbox;
    }
    private void SortChanged(object? sender, BleeckerTableSort requested)
    {
        var next = state.RequestSort(requested.Field, EffectiveSort);
        SortRequested?.Invoke(this, next); Refresh();
    }
    private void AllToggled(object sender, RoutedEventArgs args)
    {
        if (refreshing) return;
        // Native indeterminate -> checked, matching the original input's click.
        var next = state.RequestAll(projection.Select(row => row.Key), !BleeckerTableState.SelectionFlags(projection.Select(row => row.Key), EffectiveSelectedKeys).All);
        SelectionRequested?.Invoke(this, next); Refresh();
    }
    private void RowToggled(RowView view)
    {
        if (refreshing) return;
        var next = state.RequestRow(EffectiveSelectedKeys, view.Key, view.Checkbox.IsChecked == true);
        SelectionRequested?.Invoke(this, next); Refresh();
    }
    private void RefreshIfMounted() { if (host is not null) Refresh(); }
    /// <summary>Refreshes non-observable data or caller-owned mutable selection values.</summary>
    public void Refresh()
    {
        if (refreshing) { refreshAgain = true; return; }
        do
        {
            refreshAgain = false; refreshing = true;
            try { Reconcile(); }
            finally { refreshing = false; }
        } while (refreshAgain);
    }
    private void Reconcile()
    {
        var presentations = new BleeckerTableColumnStyles();
        var items = ItemsSource?.Cast<object>().ToArray() ?? [];
        if (items.Length > 0 && GetRowKey is null) throw new InvalidOperationException("A simple DataTable requires GetRowKey for nonempty data.");
        var columns = Columns.ToArray();
        if (columns.Select(c => c.Key).Distinct(StringComparer.Ordinal).Count() != columns.Length) throw new ArgumentException("Table column keys must be unique.");
        var sorted = BleeckerTableState.Sorted(items, columns, EffectiveSort);
        projection = items.Length == 0 ? [] : BleeckerTableState.Project(sorted, GetRowKey!, EffectiveSelectedKeys);
        if (projection.Select(r => r.Key).Distinct(StringComparer.Ordinal).Count() != projection.Count) throw new ArgumentException("Table row keys must be unique after sorting.");
        var headingCells = new List<BleeckerTableCell>(); if (IsSelectable) headingCells.Add(allCell);
        foreach (var key in headers.Keys.Except(columns.Select(c => c.Key)).ToArray()) { headers[key].SortRequested -= SortChanged; headers.Remove(key); }
        foreach (var column in columns)
        {
            if (!headers.TryGetValue(column.Key, out var cell)) { cell = new(); cell.SortRequested += SortChanged; headers.Add(column.Key, cell); }
            cell.Field = column.Key; cell.Label = column.Header; cell.IsSortable = column.IsSortable; cell.CurrentSort = EffectiveSort;
            cell.ApplyColumnStyle(HeaderStyleSelector?.Invoke(column), column.Alignment == BleeckerTableCellAlignment.Right ? HorizontalAlignment.Right : HorizontalAlignment.Left, presentations);
            headingCells.Add(cell);
        }
        Sync(heading.Cells, headingCells);
        var live = projection.Select(row => row.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var key in views.Keys.Where(key => !live.Contains(key)).ToArray())
        {
            var removed = views[key]; body.Children.Remove(removed.Row); foreach (var cell in removed.Cells.Values) cell.Content = null; views.Remove(key);
        }
        var desiredRows = new List<BleeckerTableRow>(); var plans = new List<(BleeckerTableCell Cell, object? Content)>();
        foreach (var row in projection)
        {
            if (!views.TryGetValue(row.Key, out var view)) { view = new(this, row.Key); views.Add(row.Key, view); }
            view.Row.Tag = row.Item; view.Row.IsSelected = row.IsSelected; view.Checkbox.IsChecked = row.IsSelected;
            var cells = new List<BleeckerTableCell>(); if (IsSelectable) cells.Add(view.SelectionCell);
            foreach (var key in view.Cells.Keys.Except(columns.Select(c => c.Key)).ToArray()) { view.Cells[key].Content = null; view.Cells.Remove(key); }
            foreach (var column in columns)
            {
                if (!view.Cells.TryGetValue(column.Key, out var cell)) { cell = new(); view.Cells.Add(column.Key, cell); }
                cell.ApplyColumnStyle(CellStyleSelector?.Invoke(column), column.Alignment switch { BleeckerTableCellAlignment.Right => HorizontalAlignment.Right, BleeckerTableCellAlignment.Center => HorizontalAlignment.Center, _ => HorizontalAlignment.Left }, presentations);
                plans.Add((cell, column.CellContentSelector is { } render ? render(row.Item, row.Index) : BleeckerTableState.CellText(BleeckerTableState.CellValue(row.Item, column)))); cells.Add(cell);
            }
            Sync(view.Row.Cells, cells); desiredRows.Add(view.Row);
        }
        // Release moved visuals before attaching them to a different keyed cell.
        foreach (var plan in plans) if (!ReferenceEquals(plan.Cell.Content, plan.Content) && plan.Cell.Content is UIElement) plan.Cell.Content = null;
        foreach (var plan in plans) if (!ReferenceEquals(plan.Cell.Content, plan.Content) && !Equals(plan.Cell.Content, plan.Content)) plan.Cell.Content = plan.Content;
        var flags = BleeckerTableState.SelectionFlags(projection.Select(r => r.Key), EffectiveSelectedKeys);
        all.IsChecked = flags.Some ? null : flags.All;
        if (projection.Count == 0) { emptyCell.ColumnSpan = Math.Max(1, columns.Length + (IsSelectable ? 1 : 0)); emptyCell.Content = EmptyMessage; desiredRows.Add(emptyRow); }
        var structureChanged = SyncChildren(body, desiredRows);
        var footerRows = FooterRowsSelector?.Invoke(sorted)?.ToArray() ?? [];
        structureChanged |= SyncChildren(footer, footerRows);
        var sections = new List<FrameworkElement> { header, body }; if (FooterRowsSelector is not null) sections.Add(footer);
        if (!string.IsNullOrEmpty(Caption)) { caption.Content = Caption; sections.Add(caption); }
        Sync(table.Sections, sections);
        // New native rows have no shared-layout owner until measured.
        if (structureChanged) table.InvalidateTable();
    }
    private static void Sync<T>(ObservableCollection<T> target, IReadOnlyList<T> desired) where T : class
    {
        var wanted = new HashSet<T>(desired, ReferenceEqualityComparer.Instance);
        foreach (var old in target.Where(item => !wanted.Contains(item)).ToArray()) target.Remove(old);
        for (var i = 0; i < desired.Count; i++) if (i >= target.Count || !ReferenceEquals(target[i], desired[i]))
        {
            var old = target.IndexOf(desired[i]);
            if (old >= 0) target.Move(old, i); else target.Insert(i, desired[i]);
        }
    }
    private static bool SyncChildren(Grid target, IReadOnlyList<BleeckerTableRow> desired)
    {
        var changed = false;
        var wanted = new HashSet<BleeckerTableRow>(desired, ReferenceEqualityComparer.Instance);
        foreach (var old in target.Children.OfType<BleeckerTableRow>().Where(row => !wanted.Contains(row)).ToArray()) { target.Children.Remove(old); changed = true; }
        var present = new HashSet<UIElement>(target.Children, ReferenceEqualityComparer.Instance);
        for (var i = 0; i < desired.Count; i++) if (i >= target.Children.Count || !ReferenceEquals(target.Children[i], desired[i]))
        {
            if (present.Contains(desired[i])) { var old = target.Children.IndexOf(desired[i]); target.Children.Move((uint)old, (uint)i); }
            else { target.Children.Insert(i, desired[i]); present.Add(desired[i]); }
            changed = true;
        }
        return changed;
    }
    private void Observe()
    {
        StopObserving(); observedItems = ItemsSource as INotifyCollectionChanged;
        if (observedItems is not null) observedItems.CollectionChanged += ItemsChanged;
        if (ItemsSource is not null) foreach (var row in ItemsSource.OfType<INotifyPropertyChanged>().Distinct<INotifyPropertyChanged>(ReferenceEqualityComparer.Instance)) { observedRows.Add(row); row.PropertyChanged += RowChanged; }
    }
    private void StopObserving()
    {
        if (observedItems is not null) observedItems.CollectionChanged -= ItemsChanged; observedItems = null;
        foreach (var row in observedRows) row.PropertyChanged -= RowChanged; observedRows.Clear();
    }
    private void ItemsChanged(object? sender, NotifyCollectionChangedEventArgs args) { Observe(); RefreshIfMounted(); }
    private void RowChanged(object? sender, PropertyChangedEventArgs args) => RefreshIfMounted();
    private sealed class RowView
    {
        internal readonly string Key;
        internal readonly BleeckerTableRow Row = new();
        internal readonly BleeckerCheckbox Checkbox;
        internal readonly BleeckerTableCell SelectionCell;
        internal readonly Dictionary<string, BleeckerTableCell> Cells = new(StringComparer.Ordinal);
        internal RowView(BleeckerSimpleDataTable owner, string key)
        {
            Key = key; Checkbox = BleeckerSimpleDataTable.Checkbox("Select row " + key); SelectionCell = new() { Content = Checkbox };
            Checkbox.Checked += (_, _) => owner.RowToggled(this); Checkbox.Unchecked += (_, _) => owner.RowToggled(this);
        }
    }
}
