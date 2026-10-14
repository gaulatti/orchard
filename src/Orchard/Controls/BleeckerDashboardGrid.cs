using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard;

/// <summary>A viewport-responsive dashboard grid with consumer-owned children.</summary>
public sealed class BleeckerDashboardGrid : Panel
{
    private XamlRoot? observedRoot;
    private readonly List<Cell> cells=[];
    private double[] tracks=[],rowHeights=[];
    private sealed record Cell(UIElement Element,int Row,int Column,int Span);

    public BleeckerDashboardGrid()
    {
        UseLayoutRounding=false; IsTabStop=false;
        Loaded+=(_,_)=>{ObserveRoot();InvalidateMeasure();};
        Unloaded+=(_,_)=>
        {
            DisconnectRoot();
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>{if(IsLoaded){ObserveRoot();InvalidateMeasure();}});
        };
    }
    public static readonly DependencyProperty ColumnsProperty=DependencyProperty.Register(nameof(Columns),typeof(int),typeof(BleeckerDashboardGrid),new PropertyMetadata(0,Changed));
    public static readonly DependencyProperty GapProperty=DependencyProperty.Register(nameof(Gap),typeof(double),typeof(BleeckerDashboardGrid),new PropertyMetadata(BleeckerTokens.Spacing["group"],Changed));
    /// <summary>Zero uses the source's automatic 1/2/3/4-column recipe; explicit values are 1–6.</summary>
    public int Columns {get=>(int)GetValue(ColumnsProperty);set{ValidateColumns(value);SetValue(ColumnsProperty,value);}}
    public double Gap {get=>(double)GetValue(GapProperty);set{ValidateGap(value);SetValue(GapProperty,value);}}
    private static void Changed(DependencyObject owner,DependencyPropertyChangedEventArgs args)
    {
        var grid=(BleeckerDashboardGrid)owner;ValidateColumns(grid.Columns);ValidateGap(grid.Gap);grid.InvalidateMeasure();
    }
    private static void ValidateColumns(int value){if(value is <0 or >6)throw new ArgumentOutOfRangeException(nameof(Columns),"Columns must be automatic (0) or 1–6.");}
    private static void ValidateGap(double value){if(!double.IsFinite(value)||value<0)throw new ArgumentOutOfRangeException(nameof(Gap),"Gap must be finite and nonnegative.");}
    private void ObserveRoot(){if(!IsLoaded){DisconnectRoot();return;}if(ReferenceEquals(observedRoot,XamlRoot))return;DisconnectRoot();observedRoot=XamlRoot;if(observedRoot is not null)observedRoot.Changed+=RootChanged;}
    private void DisconnectRoot(){if(observedRoot is not null)observedRoot.Changed-=RootChanged;observedRoot=null;}
    private void RootChanged(XamlRoot sender,XamlRootChangedEventArgs args)=>InvalidateMeasure();
    private int ExplicitColumns(double viewport)
    {
        if(Columns==1||viewport<640)return 1;
        if(Columns==2||viewport<1024)return 2;
        if(Columns==3||viewport<1280)return 3;
        return Columns==0?4:Columns;
    }
    private static int Span(UIElement element,double viewport,int columns)
    {
        if(element is not BleeckerDashboardSpan span)return 1;
        BleeckerDashboardSpan.Validate(span.Span);
        return span.Span==BleeckerDashboardSpanSize.Full?columns:Math.Min((int)span.Span,viewport<640?1:viewport<1024?2:viewport<1280?3:4);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        ValidateColumns(Columns);ValidateGap(Gap);ObserveRoot();
        var viewport=XamlRoot?.Size.Width??0;var explicitCount=ExplicitColumns(viewport);
        var visible=Children.Where(child=>child.Visibility==Visibility.Visible).ToArray();
        var count=Math.Max(explicitCount,visible.Select(child=>Span(child,viewport,explicitCount)).DefaultIfEmpty(1).Max());
        cells.Clear();var row=0;var column=0;
        foreach(var child in visible)
        {
            var span=Span(child,viewport,explicitCount);
            if(child is BleeckerDashboardSpan {Span:BleeckerDashboardSpanSize.Full}&&column>0){row++;column=0;}
            if(column+span>count){row++;column=0;}
            cells.Add(new Cell(child,row,column,span));column+=span;
        }
        tracks=new double[count];rowHeights=new double[cells.Count==0?0:cells[^1].Row+1];
        foreach(var cell in cells)
        {
            cell.Element.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
            if(cell.Column>=explicitCount)
            {
                var occupied=Enumerable.Range(cell.Column,cell.Span).Sum(i=>tracks[i])+Gap*(cell.Span-1);
                var extra=Math.Max(0,cell.Element.DesiredSize.Width-occupied)/cell.Span;
                for(var i=cell.Column;i<cell.Column+cell.Span;i++)tracks[i]+=extra;
            }
        }
        var autoWidth=tracks.Skip(explicitCount).Sum();
        var natural=double.IsFinite(availableSize.Width)?availableSize.Width:
            cells.Select(cell=>Math.Max(0,cell.Element.DesiredSize.Width-Gap*(cell.Span-1))/cell.Span).DefaultIfEmpty(0).Max()*explicitCount+autoWidth+Gap*(count-1);
        var fraction=(float)(Math.Max(0,natural-autoWidth-Gap*(count-1))/explicitCount);
        for(var i=0;i<explicitCount;i++)tracks[i]=Math.Floor((double)fraction*(i+1)*64)/64-Math.Floor((double)fraction*i*64)/64;
        foreach(var cell in cells)
        {
            cell.Element.Measure(new Size(CellWidth(cell),double.PositiveInfinity));
            rowHeights[cell.Row]=Math.Max(rowHeights[cell.Row],cell.Element.DesiredSize.Height);
        }
        return new Size(natural,rowHeights.Sum()+Gap*Math.Max(0,rowHeights.Length-1));
    }
    private double CellWidth(Cell cell)=>Enumerable.Range(cell.Column,cell.Span).Sum(i=>tracks[i])+Gap*(cell.Span-1);
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach(var cell in cells)
        {
            var x=tracks.Take(cell.Column).Sum()+Gap*cell.Column;var y=rowHeights.Take(cell.Row).Sum()+Gap*cell.Row;
            cell.Element.Arrange(new Rect(x,y,CellWidth(cell),rowHeights[cell.Row]));
        }
        return finalSize;
    }
    protected override AutomationPeer OnCreateAutomationPeer()=>new GridPeer(this);
    private sealed class GridPeer(BleeckerDashboardGrid owner):FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore()=>nameof(BleeckerDashboardGrid);
        protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Group;
    }
}
