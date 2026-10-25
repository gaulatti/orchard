using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>The client-filtered, sorted and paginated Data Table composition.</summary>
public sealed class BleeckerDataTable : Control
{
    private readonly BleeckerTable table = new();
    private readonly BleeckerTableHeader header = new();
    private readonly BleeckerTableBody body = new();
    private readonly Dictionary<string, BleeckerTableRow> headingRows = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BleeckerTableHead> headingCells = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BleeckerTableRow> rows = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BleeckerTableCell> cells = new(StringComparer.Ordinal);
    private readonly BleeckerTableRow emptyRow = new() { MinHeight = 96 };
    private readonly BleeckerTableCell emptyCell = new() { Content = "No results.", HorizontalContentAlignment = HorizontalAlignment.Center };
    private readonly List<INotifyPropertyChanged> observedRows = [];
    private readonly List<BleeckerCssShadow> surfaceShadows=[];
    private INotifyCollectionChanged? observedItems;
    private Grid? host, searchHost;
    private BleeckerInput? search;
    private TextBlock? summary, secondary;
    private Button? previous, next;
    private bool refreshing, refreshAgain;
    private Func<BleeckerDataTableColumnContext, Style?>? cellStyleSelector, headerStyleSelector;
    public BleeckerDataTable()
    {
        DefaultStyleKey = typeof(BleeckerDataTable); IsTabStop = false; UseLayoutRounding = false;
        header.Style=TableSectionStyles.Get("BleeckerDataTableHeaderStyle");
        emptyRow.Cells.Add(emptyCell); table.Sections.Add(header); table.Sections.Add(body);
        Columns.CollectionChanged += (_, _) => Model.SetColumns(Columns);
        Model.Changed += (_, _) => RefreshOnDispatcher();
        Loaded += (_, _) => { Observe(); Model.SetData(ItemsSource); };
        Unloaded += (_, _) => StopObserving();
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); });
        ActualThemeChanged += (_, _) => { if (secondary is not null) emptyCell.Foreground = secondary.Foreground; };
    }
    public BleeckerDataTableModel Model { get; } = new();
    public ObservableCollection<BleeckerDataTableColumn> Columns { get; } = [];
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(BleeckerDataTable), new PropertyMetadata(null, (owner, _) =>
    { var control = (BleeckerDataTable)owner; if (control.IsLoaded) control.Observe(); control.Model.SetData(control.ItemsSource); }));
    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public static readonly DependencyProperty SearchKeyProperty = DependencyProperty.Register(nameof(SearchKey), typeof(string), typeof(BleeckerDataTable), new PropertyMetadata(null, (owner, _) => ((BleeckerDataTable)owner).Refresh()));
    public string? SearchKey { get => (string?)GetValue(SearchKeyProperty); set => SetValue(SearchKeyProperty, value); }
    public Func<BleeckerDataTableColumnContext, Style?>? CellStyleSelector { get => cellStyleSelector; set { cellStyleSelector = value; Refresh(); } }
    public Func<BleeckerDataTableColumnContext, Style?>? HeaderStyleSelector { get => headerStyleSelector; set { headerStyleSelector = value; Refresh(); } }
    protected override void OnApplyTemplate()
    {
        DetachTemplate(); if (Template is null) return; base.OnApplyTemplate();
        T Part<T>(string name) where T : FrameworkElement => GetTemplateChild(name) as T ?? throw new InvalidOperationException("Data Table requires " + name + ".");
        host = Part<Grid>("PART_TableHost"); searchHost = Part<Grid>("PART_SearchHost"); search = Part<BleeckerInput>("PART_Search");
        summary = Part<TextBlock>("PART_Summary"); secondary = Part<TextBlock>("PART_Secondary"); previous = Part<Button>("PART_Previous"); next = Part<Button>("PART_Next");
        // Caller templates own decorative surfaces. The default surface opts
        // into both canonical shadow layers and requires the complete group.
        if(GetTemplateChild("PART_Surface") is Border surface)
        {
            for(var i=0;i<BleeckerTokens.SurfaceShadows.Count;i++)
            {
                var suffix=i==0?"Near":"Far";var recipe=BleeckerTokens.SurfaceShadows[i];
                var shadow=new BleeckerCssShadow(this,surface,Part<Grid>("PART_Shadow"+suffix),Part<Microsoft.UI.Xaml.Shapes.Path>("PART_Mask"+suffix),Part<Microsoft.UI.Xaml.Shapes.Path>("PART_Exterior"+suffix));
                surfaceShadows.Add(shadow);shadow.Set(new(recipe.Blur,recipe.Y,recipe.Spread,recipe.Opacity,recipe.R,recipe.G,recipe.B));
            }
        }
        search.TextChanged += SearchChanged; previous.Click += PreviousClicked; next.Click += NextClicked;
        search.StartIcon = Glyph("<Ellipse Width='16' Height='16' StrokeThickness='2' Stroke='{ThemeResource BleeckerTextSecondaryBrush}' Margin='3,3,5,5'/><Path Data='M21,21 L16.65,16.65' StrokeThickness='2' Stroke='{ThemeResource BleeckerTextSecondaryBrush}' StrokeStartLineCap='Round' StrokeEndLineCap='Round'/>",16);
        previous.Content = Chevron(previous, "M15,18 L9,12 L15,6"); next.Content = Chevron(next, "M9,18 L15,12 L9,6");
        AutomationProperties.SetName(previous,"Previous page"); AutomationProperties.SetName(next,"Next page");
        host.Children.Add(table); emptyCell.Foreground = secondary.Foreground; Refresh();
    }
    private static Viewbox Glyph(string markup, double size) => (Viewbox)XamlReader.Load("<Viewbox xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Width='"+size+"' Height='"+size+"' FlowDirection='LeftToRight' AutomationProperties.AccessibilityView='Raw'><Grid Width='24' Height='24'>"+markup+"</Grid></Viewbox>");
    private static Viewbox Chevron(Button button,string data)
    {
        // Match the existing pagination companion's native 32-DIP icon recipe.
        // BleeckerButton's text-size padding does not fit a 16-DIP icon here.
        var palette=new ResourceDictionary{Source=new Uri("ms-appx:///Orchard/Themes/Buttons/Secondary.g.xaml")};
        button.Resources.MergedDictionaries.Add(palette);button.Style=(Style)palette[typeof(Button)];
        var canvas=new Canvas{Width=24,Height=24,IsHitTestVisible=false};
        var path=(Microsoft.UI.Xaml.Shapes.Path)XamlReader.Load("<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Data='"+data+"' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'/>");
        BleeckerButtonGlyphForeground.Attach(path);canvas.Children.Add(path);
        var glyph=new Viewbox{Width=16,Height=16,Child=canvas,IsHitTestVisible=false,FlowDirection=FlowDirection.LeftToRight};AutomationProperties.SetAccessibilityView(glyph,AccessibilityView.Raw);return glyph;
    }
    private void DetachTemplate()
    {
        foreach(var shadow in surfaceShadows)shadow.Dispose();surfaceShadows.Clear();
        if (search is not null) search.TextChanged -= SearchChanged;
        if (previous is not null) previous.Click -= PreviousClicked;
        if (next is not null) next.Click -= NextClicked;
        host?.Children.Clear(); host = searchHost = null; search = null; summary = secondary = null; previous = next = null;
    }
    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        if (searchHost is not null)
        {
            var width = double.IsFinite(availableSize.Width) ? availableSize.Width : double.IsFinite(Width) ? Width : 384;
            var desired = Math.Min(384, Math.Max(0,width)); if (searchHost.Width != desired) searchHost.Width = desired;
        }
        return base.MeasureOverride(availableSize);
    }
    private void PreviousClicked(object sender,RoutedEventArgs args) => Model.PreviousPage();
    private void NextClicked(object sender,RoutedEventArgs args) => Model.NextPage();
    private void SearchChanged(object sender, TextChangedEventArgs args)
    {
        if (refreshing || search is null || string.IsNullOrEmpty(SearchKey)) return;
        var column = Model.GetColumn(SearchKey); if (column is null) { Refresh(); return; }
        column.SetFilterValue(search.Text);
    }
    private void RefreshOnDispatcher()
    {
        if (DispatcherQueue.HasThreadAccess) Refresh();
        else if (!DispatcherQueue.TryEnqueue(Refresh)) throw new InvalidOperationException("Data Table dispatcher rejected a producer update.");
    }
    public void Refresh()
    {
        if (host is null) return; if (refreshing) { refreshAgain = true; return; }
        do { refreshAgain = false; refreshing = true; try { Reconcile(); } finally { refreshing = false; } } while (refreshAgain);
    }
    private void Reconcile()
    {
        if (search is null || searchHost is null || summary is null || previous is null || next is null) return;
        var presentations = new BleeckerTableColumnStyles();
        searchHost.Visibility = string.IsNullOrEmpty(SearchKey) ? Visibility.Collapsed : Visibility.Visible;
        search.PlaceholderText = "Search " + SearchKey + "..."; AutomationProperties.SetName(search, search.PlaceholderText);
        search.Text = SearchKey is null ? "" : BleeckerDataTableValue.InputText(Model.GetColumn(SearchKey)?.GetFilterValue());
        summary.Text = Model.FilteredSelectedRowCount > 0 ? Model.FilteredSelectedRowCount + " of " + Model.FilteredRowCount + " row(s) selected." : "";
        previous.IsEnabled = Model.CanPreviousPage; next.IsEnabled = Model.CanNextPage;
        var desiredHeaders = new List<BleeckerTableRow>(); var headerPlans = new List<(BleeckerTableRow Row,List<BleeckerTableCell> Cells)>(); var plans = new List<(BleeckerTableCell Cell, object? Content)>(); var liveHeads = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in Model.GetHeaderGroups())
        {
            if (!headingRows.TryGetValue(group.Id,out var row)) { row = new(); headingRows.Add(group.Id,row); }
            var desired = new List<BleeckerTableCell>();
            foreach (var context in group.Headers)
            {
                liveHeads.Add(context.Id); if (!headingCells.TryGetValue(context.Id,out var cell)) { cell = new(); headingCells.Add(context.Id,cell); }
                var content=context.RenderContent();
                // The source checkbox has a block wrapper, so its default
                // follows logical start even inside a physical text-left head.
                var alignment=content is BleeckerCheckbox&&FlowDirection==FlowDirection.RightToLeft?HorizontalAlignment.Right:HorizontalAlignment.Left;
                cell.Tag = context; cell.ApplyColumnStyle(HeaderStyleSelector?.Invoke(context.Column),alignment,presentations); plans.Add((cell,content)); desired.Add(cell);
            }
            headerPlans.Add((row,desired)); desiredHeaders.Add(row);
        }
        // Leaf headers can move between levels when original groups change.
        // Release their old native parent before attaching the desired cells.
        foreach(var oldRow in headingRows.Values){var desired=headerPlans.FirstOrDefault(plan=>ReferenceEquals(plan.Row,oldRow)).Cells;foreach(var old in oldRow.Cells.Where(cell=>desired is null||!desired.Contains(cell)).ToArray())oldRow.Cells.Remove(old);}
        foreach(var plan in headerPlans)Sync(plan.Row.Cells,plan.Cells);
        foreach(var key in headingRows.Keys.Where(key=>!headerPlans.Any(plan=>ReferenceEquals(plan.Row,headingRows[key]))).ToArray())headingRows.Remove(key);
        foreach (var id in headingCells.Keys.Where(id=>!liveHeads.Contains(id)).ToArray()) { headingCells[id].Content=null; headingCells.Remove(id); }
        var structureChanged=SyncChildren(header,desiredHeaders);
        var desiredRows = new List<BleeckerTableRow>(); var liveRows = new HashSet<string>(StringComparer.Ordinal); var liveCells = new HashSet<string>(StringComparer.Ordinal);
        foreach (var context in Model.PageRows)
        {
            liveRows.Add(context.Id); if (!rows.TryGetValue(context.Id,out var row)) { row=new();rows.Add(context.Id,row); }
            row.Tag=context; // Selection is owned by the source contexts, not the primitive's selected-fill prop.
            var desired = new List<BleeckerTableCell>();
            foreach(var item in context.GetVisibleCells())
            {
                liveCells.Add(item.Id); if(!cells.TryGetValue(item.Id,out var cell)){cell=new();cells.Add(item.Id,cell);}
                cell.Tag=item;cell.ApplyColumnStyle(CellStyleSelector?.Invoke(item.Column),HorizontalAlignment.Left,presentations);plans.Add((cell,item.RenderContent()));desired.Add(cell);
            }
            Sync(row.Cells,desired);desiredRows.Add(row);
        }
        foreach(var id in rows.Keys.Where(id=>!liveRows.Contains(id)).ToArray())rows.Remove(id);
        foreach(var id in cells.Keys.Where(id=>!liveCells.Contains(id)).ToArray()){cells[id].Content=null;cells.Remove(id);}
        foreach(var plan in plans)if(!ReferenceEquals(plan.Cell.Content,plan.Content)&&plan.Cell.Content is UIElement)plan.Cell.Content=null;
        foreach(var plan in plans)if(!ReferenceEquals(plan.Cell.Content,plan.Content)&&!Equals(plan.Cell.Content,plan.Content))plan.Cell.Content=plan.Content;
        if(desiredRows.Count==0){emptyCell.ColumnSpan=Math.Max(1,Columns.Count);desiredRows.Add(emptyRow);}
        structureChanged|=SyncChildren(body,desiredRows);
        // New rows/cells have no shared-layout owner yet. Their content changes
        // cannot invalidate the table, and unchanged retained cells may provide
        // no other signal when a filtered page expands.
        if(structureChanged)table.InvalidateTable();
    }
    private static void Sync<T>(ObservableCollection<T> target,IReadOnlyList<T> desired) where T:class
    {
        var live=new HashSet<T>(desired,ReferenceEqualityComparer.Instance);foreach(var old in target.Where(item=>!live.Contains(item)).ToArray())target.Remove(old);
        for(var i=0;i<desired.Count;i++)if(i>=target.Count||!ReferenceEquals(target[i],desired[i])){var old=target.IndexOf(desired[i]);if(old>=0)target.Move(old,i);else target.Insert(i,desired[i]);}
    }
    private static bool SyncChildren(Grid target,IReadOnlyList<BleeckerTableRow> desired)
    {
        var changed=false;
        var live=new HashSet<BleeckerTableRow>(desired,ReferenceEqualityComparer.Instance);foreach(var old in target.Children.OfType<BleeckerTableRow>().Where(row=>!live.Contains(row)).ToArray()){target.Children.Remove(old);changed=true;}
        for(var i=0;i<desired.Count;i++)if(i>=target.Children.Count||!ReferenceEquals(target.Children[i],desired[i])){var old=target.Children.IndexOf(desired[i]);if(old>=0)target.Children.Move((uint)old,(uint)i);else target.Children.Insert(i,desired[i]);changed=true;}
        return changed;
    }
    private void Observe()
    {
        StopObserving();observedItems=ItemsSource as INotifyCollectionChanged;if(observedItems is not null)observedItems.CollectionChanged+=ItemsChanged;
        if(ItemsSource is not null)foreach(var item in ItemsSource.OfType<INotifyPropertyChanged>().Distinct<INotifyPropertyChanged>(ReferenceEqualityComparer.Instance)){observedRows.Add(item);item.PropertyChanged+=ItemChanged;}
    }
    private void StopObserving(){if(observedItems is not null)observedItems.CollectionChanged-=ItemsChanged;observedItems=null;foreach(var item in observedRows)item.PropertyChanged-=ItemChanged;observedRows.Clear();}
    private void ItemsChanged(object? sender,NotifyCollectionChangedEventArgs args)
    {
        void Update(){if(!IsLoaded)return;Observe();Model.SetData(ItemsSource);}if(DispatcherQueue.HasThreadAccess)Update();else if(!DispatcherQueue.TryEnqueue(Update))throw new InvalidOperationException("Data Table dispatcher rejected a collection update.");
    }
    private void ItemChanged(object? sender,PropertyChangedEventArgs args)
    {
        void Update(){if(IsLoaded)Model.RefreshData();}if(DispatcherQueue.HasThreadAccess)Update();else if(!DispatcherQueue.TryEnqueue(Update))throw new InvalidOperationException("Data Table dispatcher rejected an item update.");
    }
    protected override AutomationPeer OnCreateAutomationPeer()=>new DataTablePeer(this);
    private sealed class DataTablePeer(BleeckerDataTable owner):FrameworkElementAutomationPeer(owner)
    {protected override string GetClassNameCore()=>nameof(BleeckerDataTable);protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Group;}
}
