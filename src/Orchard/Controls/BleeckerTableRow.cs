using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;

namespace Orchard;

[ContentProperty(Name = nameof(Cells))]
public sealed class BleeckerTableRow : Control
{
    private Grid? slot;
    private Border? selected, hover, stroke, sectionTop;
    private bool ownsFooterWeight;
    private readonly CellPanel panel;
    internal BleeckerTable? Table;
    internal double LayoutHeight;
    internal bool HeaderRow, LastBodyRow, FirstFooterRow;
    public BleeckerTableRow()
    {
        DefaultStyleKey = typeof(BleeckerTableRow); IsTabStop = false; UseLayoutRounding = false; panel = new(this);
        Cells.CollectionChanged += (_, _) => { SyncCells(); Table?.InvalidateTable(); InvalidateMeasure(); };
        PointerEntered += (_, _) => { if (hover is not null && !HeaderRow) hover.Opacity = 1; };
        PointerExited += (_, _) => { if (hover is not null) hover.Opacity = 0; };
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) { slot?.Children.Clear(); slot = null; selected = hover = stroke = sectionTop = null; } });
    }
    public ObservableCollection<BleeckerTableCell> Cells { get; } = [];
    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(BleeckerTableRow), new PropertyMetadata(false, (o, _) => ((BleeckerTableRow)o).UpdateSelection()));
    public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    protected override void OnApplyTemplate()
    {
        slot?.Children.Clear(); if (Template is null) { slot = null; return; } base.OnApplyTemplate(); slot = GetTemplateChild("PART_Cells") as Grid ?? throw new InvalidOperationException("A table row requires PART_Cells.");
        selected = GetTemplateChild("PART_Selected") as Border; hover = GetTemplateChild("PART_Hover") as Border; stroke = GetTemplateChild("PART_Stroke") as Border; sectionTop = GetTemplateChild("PART_SectionTop") as Border; slot.Children.Add(panel); SyncCells(); UpdateSelection(); UpdateSection();
    }
    internal void InvalidateCellArrangement() => panel.InvalidateArrange();
    internal void SetSection(bool header, bool lastBody, bool footer, bool firstFooter)
    {
        HeaderRow = header; LastBodyRow = lastBody; FirstFooterRow = firstFooter;
        var local = ReadLocalValue(FontWeightProperty);
        if (ownsFooterWeight && !Equals(local, Microsoft.UI.Text.FontWeights.Medium)) ownsFooterWeight = false;
        if (!footer && ownsFooterWeight) { ClearValue(FontWeightProperty); ownsFooterWeight = false; }
        if (footer && ReferenceEquals(ReadLocalValue(FontWeightProperty), DependencyProperty.UnsetValue))
        {
            var styled = false; for (var style = Style; style is not null; style = style.BasedOn) styled |= style.Setters.OfType<Setter>().Any(s => s.Property == FontWeightProperty);
            if (!styled) { FontWeight = Microsoft.UI.Text.FontWeights.Medium; ownsFooterWeight = true; }
        }
        UpdateSection();
    }
    private void UpdateSection()
    {
        if (stroke is not null) stroke.BorderThickness = LastBodyRow || Cells.Count == 0 ? new Thickness() : new Thickness(0, 0, 0, 1);
        if (sectionTop is not null) sectionTop.Opacity = FirstFooterRow ? 1 : 0;
    }
    private void UpdateSelection() { if (selected is not null) selected.Opacity = IsSelected ? 1 : 0; }
    private void SyncCells()
    {
        foreach (var old in panel.Children.OfType<BleeckerTableCell>().Where(c => !Cells.Contains(c)).ToArray()) { panel.Children.Remove(old); old.ReleaseTable(); }
        for (var i = 0; i < Cells.Count; i++) if (i >= panel.Children.Count || !ReferenceEquals(panel.Children[i], Cells[i]))
        {
            var old = panel.Children.IndexOf(Cells[i]);
            if (old >= 0) panel.Children.Move((uint)old, (uint)i);
            else panel.Children.Insert(i, Cells[i]);
        }
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new RowPeer(this);
    private sealed class RowPeer(BleeckerTableRow owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerTableRow);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem;
        protected override IList<AutomationPeer> GetChildrenCore() => owner.Cells.Concat(owner.Table?.EmptyCells(owner) ?? []).Select(CreatePeerForElement).ToArray();
    }
    private sealed class CellPanel(BleeckerTableRow row) : Panel
    {
        protected override Size MeasureOverride(Size available)
        {
            if (row.Table is null) return new Size();
            foreach (var cell in row.Cells) cell.Measure(new Size(row.Table.CellWidth(cell), double.PositiveInfinity));
            return new Size(row.Table.TableWidth, row.LayoutHeight);
        }
        protected override Size ArrangeOverride(Size final)
        {
            if (row.Table is not null) foreach (var cell in row.Cells) cell.Arrange(row.Table.CellBounds(cell));
            return final;
        }
    }
}

public sealed class BleeckerTableHeader : Grid
{
    public BleeckerTableHeader() { Style = TableSectionStyles.Get("BleeckerTableHeaderStyle"); UseLayoutRounding = false; }
}
public sealed class BleeckerTableBody : Grid
{
    public BleeckerTableBody() { Style = TableSectionStyles.Get("BleeckerTableBodyStyle"); UseLayoutRounding = false; }
}
public sealed class BleeckerTableFooter : Grid
{
    public BleeckerTableFooter() { Style = TableSectionStyles.Get("BleeckerTableFooterStyle"); UseLayoutRounding = false; }
}
internal static class TableSectionStyles
{
    internal static Style Get(string key) => (Style)new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/Orchard.xaml") }[key];
}
