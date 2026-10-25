using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;

namespace Orchard;

/// <summary>A horizontally scrollable native table retaining original sections and cells.</summary>
[ContentProperty(Name = nameof(Sections))]
public sealed class BleeckerTable : Control
{
    private ScrollViewer? scroll;
    private readonly TableLayout layout;
    private bool measuring;
    private readonly List<BleeckerTableRow> rows = [];
    private readonly Dictionary<(int Row, int Column), BleeckerTableCell> emptyCells = [];
    private double[] widths = [], heights = [];
    private double tableWidth;
    internal double TableWidth => tableWidth;
    public BleeckerTable()
    {
        DefaultStyleKey = typeof(BleeckerTable); IsTabStop = false; UseLayoutRounding = false; layout = new(this);
        Sections.CollectionChanged += (_, _) => { SyncSections(); InvalidateTable(); };
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) { if (scroll is not null) scroll.Content = null; scroll = null; } FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer(); });
    }
    public ObservableCollection<FrameworkElement> Sections { get; } = [];
    public double HorizontalOffset => scroll?.HorizontalOffset ?? 0;
    public double ScrollableWidth => scroll?.ScrollableWidth ?? 0;
    public void ScrollTo(double horizontalOffset)
    {
        if (!double.IsFinite(horizontalOffset)) throw new ArgumentOutOfRangeException(nameof(horizontalOffset));
        if (scroll is null) throw new InvalidOperationException("Apply the table template before scrolling.");
        scroll.ChangeView(horizontalOffset, null, null, true);
    }
    protected override void OnApplyTemplate()
    {
        if (scroll is not null) scroll.Content = null;
        if (Template is null) { scroll = null; return; }
        base.OnApplyTemplate(); scroll = GetTemplateChild("PART_Scroll") as ScrollViewer ?? throw new InvalidOperationException("Table requires PART_Scroll.");
        AutomationProperties.SetAccessibilityView(scroll, AccessibilityView.Raw); scroll.Content = layout; SyncSections();
    }
    internal void InvalidateTable() { if (!measuring) { layout.InvalidateMeasure(); InvalidateMeasure(); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer(); } }
    private void SyncSections()
    {
        foreach (var section in Sections) if (section is not BleeckerTableHeader and not BleeckerTableBody and not BleeckerTableFooter and not BleeckerTableCaption) throw new ArgumentException("Table sections must be a header, body, footer or caption.");
        if (Sections.OfType<BleeckerTableCaption>().Count() > 1) throw new ArgumentException("A table has one caption.");
        var ordered = Sections.OrderBy(s => s is BleeckerTableHeader ? 0 : s is BleeckerTableBody ? 1 : s is BleeckerTableFooter ? 2 : 3).ToArray();
        foreach (var old in layout.Children.OfType<FrameworkElement>().Where(e => !ordered.Contains(e)).ToArray()) layout.Children.Remove(old);
        for (var i = 0; i < ordered.Length; i++) if (i >= layout.Children.Count || !ReferenceEquals(layout.Children[i], ordered[i]))
        {
            var old = layout.Children.IndexOf(ordered[i]);
            if (old >= 0) layout.Children.Move((uint)old, (uint)i);
            else layout.Children.Insert(i, ordered[i]);
        }
    }
    protected override Size MeasureOverride(Size availableSize) { layout.Viewport = availableSize.Width; return base.MeasureOverride(availableSize); }
    internal double CellWidth(BleeckerTableCell cell) => widths.Skip(cell.LogicalColumn).Take(cell.ColumnSpan).Sum();
    internal Rect CellBounds(BleeckerTableCell cell)
    {
        var width = CellWidth(cell); var x = widths.Take(cell.LogicalColumn).Sum();
        // WinUI mirrors the inherited RTL coordinate space. Reflecting the
        // columns here as well would put the first logical column back left.
        return new Rect(x, 0, width, heights.Skip(cell.LogicalRow).Take(cell.EffectiveRowSpan).Sum());
    }
    internal IEnumerable<BleeckerTableHead> HeaderCells(int column, int span) => rows.Where(r => r.HeaderRow).SelectMany(r => r.Cells).OfType<BleeckerTableHead>().Where(c => c.LogicalColumn < column + span && c.LogicalColumn + c.ColumnSpan > column);
    internal IEnumerable<BleeckerTableCell> EmptyCells(BleeckerTableRow row) => emptyCells.Where(pair => pair.Key.Row == rows.IndexOf(row)).OrderBy(pair => pair.Key.Column).Select(pair => pair.Value);
    private BleeckerTableCell EmptyCell(int row, int column)
    {
        if (!emptyCells.TryGetValue((row, column), out var cell)) { cell = new() { Content = "" }; emptyCells.Add((row, column), cell); }
        cell.Table = this; cell.LogicalRow = row; cell.LogicalColumn = column; return cell;
    }
    private void SyncEmptyCells(int columns)
    {
        // UIA's rectangular grid still needs a provider at an empty position
        // in a ragged section. These metadata-only cells never enter or alter
        // the consumer's Cells collection or the measured visual layout.
        var occupied = new HashSet<(int Row, int Column)>();
        foreach (var cell in rows.SelectMany(row => row.Cells))
            for (var row = cell.LogicalRow; row < cell.LogicalRow + cell.EffectiveRowSpan; row++)
                for (var column = cell.LogicalColumn; column < cell.LogicalColumn + cell.ColumnSpan; column++) occupied.Add((row, column));
        var live = new HashSet<(int Row, int Column)>();
        for (var row = 0; row < rows.Count; row++) for (var column = 0; column < columns; column++)
            if (!occupied.Contains((row, column))) { live.Add((row, column)); EmptyCell(row, column); }
        foreach (var key in emptyCells.Keys.Where(key => !live.Contains(key)).ToArray()) { emptyCells[key].ReleaseTable(); emptyCells.Remove(key); }
    }
    private sealed class TableLayout(BleeckerTable owner) : Panel
    {
        internal double Viewport = double.PositiveInfinity;
        private readonly List<(Grid Section, BleeckerTableRow[] Rows)> sections = [];
        private double height;
        protected override Size MeasureOverride(Size availableSize)
        {
            owner.measuring = true;
            try
            {
                var previous = owner.rows.SelectMany(r => r.Cells).ToArray(); owner.rows.Clear(); sections.Clear(); var columnCount = 0;
                foreach (var section in Children.OfType<Grid>())
                {
                    var items = section.Children.OfType<BleeckerTableRow>().ToArray();
                    if (items.Length != section.Children.Count) throw new ArgumentException("A table section contains native table rows.");
                    var occupied = new HashSet<(int, int)>();
                    for (var localRow = 0; localRow < items.Length; localRow++)
                    {
                        var row = items[localRow]; row.Table = owner; row.SetSection(section is BleeckerTableHeader, section is BleeckerTableBody && localRow == items.Length - 1, section is BleeckerTableFooter, section is BleeckerTableFooter && localRow == 0); row.ApplyTemplate();
                        var column = 0;
                        foreach (var cell in row.Cells)
                        {
                            if (cell.ColumnSpan < 1 || cell.RowSpan < 0) throw new ArgumentException("Invalid native table span.");
                            while (occupied.Contains((localRow, column))) column++;
                            cell.Table = owner; cell.LogicalRow = owner.rows.Count; cell.LogicalColumn = column; cell.EffectiveRowSpan = cell.RowSpan == 0 ? items.Length - localRow : Math.Min(cell.RowSpan, items.Length - localRow);
                            for (var r = localRow; r < localRow + cell.EffectiveRowSpan; r++) for (var c = column; c < checked(column + cell.ColumnSpan); c++) occupied.Add((r, c));
                            column += cell.ColumnSpan; columnCount = Math.Max(columnCount, column);
                        }
                        owner.rows.Add(row);
                    }
                    sections.Add((section, items));
                }
                var cells = owner.rows.SelectMany(r => r.Cells).ToArray(); var liveCells = new HashSet<BleeckerTableCell>(cells, ReferenceEqualityComparer.Instance);
                foreach (var old in previous.Where(c => !liveCells.Contains(c))) old.ReleaseTable();
                owner.SyncEmptyCells(columnCount);
                if (!previous.SequenceEqual(cells)) FrameworkElementAutomationPeer.FromElement(owner)?.InvalidatePeer();
                var minimum = new double[columnCount]; var preferred = new double[columnCount]; var fixedColumns = new bool[columnCount];
                var measures = cells.Select(c => (Cell: c, Size: c.Intrinsic())).ToArray();
                foreach (var (cell, size) in measures.Where(m => m.Cell.ColumnSpan == 1))
                {
                    var i = cell.LogicalColumn; minimum[i] = Math.Max(minimum[i], size.Minimum); preferred[i] = Math.Max(preferred[i], size.Preferred);
                    if (cell.ColumnWidth.IsAbsolute) { fixedColumns[i] = true; preferred[i] = Math.Max(minimum[i], cell.ColumnWidth.Value); }
                }
                foreach (var (cell, size) in measures.Where(m => m.Cell.ColumnSpan > 1))
                {
                    Spread(minimum, cell.LogicalColumn, cell.ColumnSpan, size.Minimum);
                    Spread(preferred, cell.LogicalColumn, cell.ColumnSpan, size.Preferred);
                }
                for (var i = 0; i < columnCount; i++) preferred[i] = Math.Max(minimum[i], preferred[i]);
                var captionSize = Children.OfType<BleeckerTableCaption>().SingleOrDefault()?.Intrinsic() ?? (Minimum: 0d, Preferred: 0d);
                var target = Math.Max(Math.Max(minimum.Sum(), captionSize.Minimum), double.IsFinite(Viewport) ? Viewport : Math.Max(preferred.Sum(), captionSize.Preferred));
                owner.widths = minimum.ToArray();
                if (target >= preferred.Sum())
                {
                    var fixedTotal = Enumerable.Range(0, columnCount).Where(i => fixedColumns[i]).Sum(i => preferred[i]);
                    var autoTotal = Enumerable.Range(0, columnCount).Where(i => !fixedColumns[i]).Sum(i => preferred[i]);
                    for (var i = 0; i < columnCount; i++) owner.widths[i] = fixedColumns[i] ? preferred[i] : autoTotal > 0 ? preferred[i] * (target - fixedTotal) / autoTotal : columnCount > 0 ? target / columnCount : 0;
                }
                else
                {
                    var flexible = preferred.Sum() - minimum.Sum();
                    for (var i = 0; i < columnCount; i++) owner.widths[i] += flexible > 0 ? (target - minimum.Sum()) * (preferred[i] - minimum[i]) / flexible : 0;
                }
                owner.tableWidth = columnCount == 0 ? target : owner.widths.Sum();
                owner.heights = new double[owner.rows.Count]; var footer = sections.Any(s => s.Section is BleeckerTableFooter && s.Rows.Length > 0);
                for (var i = 0; i < owner.rows.Count; i++)
                {
                    var row = owner.rows[i]; var natural = row.HeaderRow && row.Cells.Count > 0 ? 44d : 0;
                    foreach (var cell in row.Cells) { cell.Measure(new Size(owner.CellWidth(cell), double.PositiveInfinity)); if (cell.EffectiveRowSpan == 1) natural = Math.Max(natural, cell.DesiredSize.Height); }
                    owner.heights[i] = Math.Max(row.MinHeight, natural + (row.Cells.Count == 0 || row.HeaderRow ? 0 : row.LastBodyRow && !footer ? .5 : 1));
                }
                foreach (var cell in cells.Where(c => c.EffectiveRowSpan > 1)) Spread(owner.heights, cell.LogicalRow, cell.EffectiveRowSpan, cell.DesiredSize.Height);
                // Shared bounds can change while a row's outer size stays
                // identical. Its panel must still arrange retained cells.
                foreach (var row in owner.rows) row.InvalidateCellArrangement();
                height = 0; var index = 0;
                foreach (var (section, items) in sections)
                {
                    while (section.RowDefinitions.Count < items.Length) section.RowDefinitions.Add(new RowDefinition());
                    while (section.RowDefinitions.Count > items.Length) section.RowDefinitions.RemoveAt(section.RowDefinitions.Count - 1);
                    var total = 0d;
                    for (var r = 0; r < items.Length; r++) { var value = owner.heights[index++]; items[r].LayoutHeight = value; Grid.SetRow(items[r], r); if (section.RowDefinitions[r].Height.Value != value || !section.RowDefinitions[r].Height.IsAbsolute) section.RowDefinitions[r].Height = new GridLength(value); total += value; }
                    section.Measure(new Size(owner.TableWidth, total)); height += total;
                }
                if (footer) height += .5;
                foreach (var caption in Children.OfType<BleeckerTableCaption>()) { caption.Measure(new Size(owner.TableWidth, double.PositiveInfinity)); height += caption.DesiredSize.Height; }
                return new Size(owner.TableWidth, height);
            }
            finally { owner.measuring = false; }
        }
        private static void Spread(double[] values, int start, int count, double requested)
        {
            var current = values.Skip(start).Take(count).Sum(); if (requested <= current) return;
            for (var i = start; i < start + count; i++) values[i] += (requested - current) / count;
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var y = 0d; var row = 0;
            foreach (var (section, items) in sections) { var h = owner.heights.Skip(row).Take(items.Length).Sum(); section.Arrange(new Rect(0, y, owner.TableWidth, h)); row += items.Length; y += h; }
            if (sections.Any(s => s.Section is BleeckerTableFooter && s.Rows.Length > 0)) y += .5;
            foreach (var caption in Children.OfType<BleeckerTableCaption>()) { caption.Arrange(new Rect(0, y, owner.TableWidth, caption.DesiredSize.Height)); y += caption.DesiredSize.Height; }
            return new Size(finalSize.Width, height);
        }
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new TablePeer(this);
    private sealed class TablePeer(BleeckerTable owner) : FrameworkElementAutomationPeer(owner), IGridProvider, ITableProvider
    {
        protected override string GetClassNameCore() => nameof(BleeckerTable);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Table;
        protected override string GetNameCore() => AutomationProperties.GetName(owner) is { Length: > 0 } name ? name : owner.Sections.OfType<BleeckerTableCaption>().SingleOrDefault()?.Content as string ?? base.GetNameCore();
        protected override IList<AutomationPeer> GetChildrenCore() => owner.layout.IsLoaded ? owner.rows.Select(CreatePeerForElement).Concat(owner.Sections.OfType<BleeckerTableCaption>().Select(CreatePeerForElement)).ToArray() : [];
        protected override object GetPatternCore(PatternInterface pattern) => pattern is PatternInterface.Grid or PatternInterface.Table ? this : pattern == PatternInterface.Scroll && owner.scroll is not null ? CreatePeerForElement(owner.scroll).GetPattern(pattern) : base.GetPatternCore(pattern);
        public int RowCount => owner.layout.IsLoaded ? owner.rows.Count : 0;
        public int ColumnCount => owner.layout.IsLoaded ? owner.widths.Length : 0;
        public IRawElementProviderSimple GetItem(int row, int column)
        {
            if (row < 0 || row >= RowCount || column < 0 || column >= ColumnCount) throw new ArgumentOutOfRangeException(nameof(row));
            var cell = owner.rows.SelectMany(r => r.Cells).FirstOrDefault(c => c.LogicalRow <= row && c.LogicalRow + c.EffectiveRowSpan > row && c.LogicalColumn <= column && c.LogicalColumn + c.ColumnSpan > column) ?? owner.EmptyCell(row, column);
            return ProviderFromPeer(CreatePeerForElement(cell));
        }
        public IRawElementProviderSimple[] GetColumnHeaders() => owner.layout.IsLoaded ? owner.rows.Where(r => r.HeaderRow).SelectMany(r => r.Cells).OfType<BleeckerTableHead>().Select(c => ProviderFromPeer(CreatePeerForElement(c))).ToArray() : [];
        public IRawElementProviderSimple[] GetRowHeaders() => [];
        public RowOrColumnMajor RowOrColumnMajor => RowOrColumnMajor.RowMajor;
    }
}
