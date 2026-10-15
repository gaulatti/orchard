using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

/// <summary>Responsive consumer table search, filter content, reset and actions.</summary>
public sealed partial class BleeckerDataTableToolbar:Control
{
    private Grid? host;
    private ToolbarPanel? layout;
    private BleeckerInput? search;
    private Button? reset;
    private IBleeckerDataTableFilterSource? observedTable;
    private EventHandler? tableChanged;
    private XamlRoot? observedRoot;
    private bool updatingText;
    private bool resetting;
    private readonly Slot filters,actions;
    public BleeckerDataTableToolbar()
    {
        DefaultStyleKey=typeof(BleeckerDataTableToolbar);filters=new(this);actions=new(this);
        Loaded+=(_,_)=>{if(Table is null)throw new InvalidOperationException("A mounted table toolbar requires its consumer Table.");filters.Disconnect();actions.Disconnect();Observe();Refresh();};
        Unloaded+=(_,_)=>{Disconnect();filters.Disconnect();actions.Disconnect();DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>{if(IsLoaded){Observe();Refresh();}});};
        RegisterPropertyChangedCallback(FontFamilyProperty,(_,_)=>Refresh());RegisterPropertyChangedCallback(FontSizeProperty,(_,_)=>Refresh());
        RegisterPropertyChangedCallback(TemplateProperty,(_,_)=>{if(Template is null)DetachTemplate();});
    }
    private static DependencyProperty Property(string name,Type type,object? initial)=>DependencyProperty.Register(name,type,typeof(BleeckerDataTableToolbar),new PropertyMetadata(initial,(owner,_)=>{var bar=(BleeckerDataTableToolbar)owner;if(bar.IsLoaded)bar.Observe();bar.Refresh();}));
    public static readonly DependencyProperty TableProperty=Property(nameof(Table),typeof(object),null);
    public IBleeckerDataTableFilterSource? Table{get=>GetValue(TableProperty) as IBleeckerDataTableFilterSource;set=>SetValue(TableProperty,value);}
    public static readonly DependencyProperty SearchColumnProperty=Property(nameof(SearchColumn),typeof(string),null);
    public string? SearchColumn{get=>(string?)GetValue(SearchColumnProperty);set=>SetValue(SearchColumnProperty,value);}
    public static readonly DependencyProperty SearchPlaceholderProperty=Property(nameof(SearchPlaceholder),typeof(string),"Search...");
    public string SearchPlaceholder{get=>(string)GetValue(SearchPlaceholderProperty);set=>SetValue(SearchPlaceholderProperty,value??throw new ArgumentNullException(nameof(value)));}
    public static readonly DependencyProperty FilterRenderersProperty=Property(nameof(FilterRenderers),typeof(object),null);
    public object? FilterRenderers{get=>GetValue(FilterRenderersProperty);set=>SetValue(FilterRenderersProperty,value);}
    public static readonly DependencyProperty FilterTemplateProperty=Property(nameof(FilterTemplate),typeof(DataTemplate),null);
    public DataTemplate? FilterTemplate{get=>(DataTemplate?)GetValue(FilterTemplateProperty);set=>SetValue(FilterTemplateProperty,value);}
    public static readonly DependencyProperty ActionsProperty=Property(nameof(Actions),typeof(object),null);
    public object? Actions{get=>GetValue(ActionsProperty);set=>SetValue(ActionsProperty,value);}
    public static readonly DependencyProperty ActionTemplateProperty=Property(nameof(ActionTemplate),typeof(DataTemplate),null);
    public DataTemplate? ActionTemplate{get=>(DataTemplate?)GetValue(ActionTemplateProperty);set=>SetValue(ActionTemplateProperty,value);}
    public static readonly DependencyProperty SpacingProperty=Property(nameof(Spacing),typeof(double),12d);
    public double Spacing{get=>(double)GetValue(SpacingProperty);set=>SetValue(SpacingProperty,value);}
    protected override void OnApplyTemplate()
    {
        DetachTemplate();base.OnApplyTemplate();if(Template is null)return;
        host=GetTemplateChild("PART_Layout") as Grid??throw new InvalidOperationException("Table toolbar requires PART_Layout.");
        layout=new ToolbarPanel();host.Children.Add(layout);
        search=new BleeckerInput{Name="PART_Search",Height=36,MinWidth=0,UseLayoutRounding=false,HorizontalAlignment=HorizontalAlignment.Stretch};
        search.SetBinding(FontFamilyProperty,new Binding{Source=this,Path=new PropertyPath(nameof(FontFamily))});
        search.TextChanged+=SearchChanged;layout.Search=search;
        reset=new Button{Name="PART_Reset",MinWidth=0,MinHeight=36,Height=36,Padding=new Thickness(14,0,14,0),FontSize=13,UseLayoutRounding=false,CharacterSpacing=0};
        var palette=new ResourceDictionary{Source=new Uri("ms-appx:///Orchard/Themes/Buttons/Ghost.g.xaml")};reset.Resources.MergedDictionaries.Add(palette);reset.Style=(Style)palette[typeof(Button)];
        var caption=new TextBlock{Text="Reset",FontSize=13,LineHeight=19.5,LineStackingStrategy=LineStackingStrategy.BlockLineHeight,UseLayoutRounding=false,VerticalAlignment=VerticalAlignment.Center};
        caption.SetBinding(TextBlock.ForegroundProperty,new Binding{Source=reset,Path=new PropertyPath(nameof(Foreground))});caption.SetBinding(TextBlock.FontFamilyProperty,new Binding{Source=reset,Path=new PropertyPath(nameof(FontFamily))});caption.SetBinding(TextBlock.FontWeightProperty,new Binding{Source=reset,Path=new PropertyPath(nameof(FontWeight))});
        var cross=new PathGeometry();
        var first=new PathFigure{StartPoint=new Point(18,6),IsFilled=false};first.Segments.Add(new LineSegment{Point=new Point(6,18)});cross.Figures.Add(first);
        var second=new PathFigure{StartPoint=new Point(6,6),IsFilled=false};second.Segments.Add(new LineSegment{Point=new Point(18,18)});cross.Figures.Add(second);
        var icon=new Microsoft.UI.Xaml.Shapes.Path{Data=cross,StrokeThickness=2,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round};
        icon.SetBinding(Microsoft.UI.Xaml.Shapes.Shape.StrokeProperty,new Binding{Source=reset,Path=new PropertyPath(nameof(Foreground))});var canvas=new Canvas{Width=24,Height=24};canvas.Children.Add(icon);var glyph=new Viewbox{Width=14,Height=14,Child=canvas,VerticalAlignment=VerticalAlignment.Center};AutomationProperties.SetAccessibilityView(glyph,AccessibilityView.Raw);
        var content=new Grid{ColumnSpacing=8,UseLayoutRounding=false,VerticalAlignment=VerticalAlignment.Center};content.ColumnDefinitions.Add(new(){Width=GridLength.Auto});content.ColumnDefinitions.Add(new(){Width=GridLength.Auto});content.Children.Add(glyph);var lineBox=new CaptionLineBox(caption);Grid.SetColumn(lineBox,1);content.Children.Add(lineBox);reset.Content=content;
        AutomationProperties.SetName(reset,"Reset");reset.Click+=ResetClicked;layout.Reset=reset;
        if(IsLoaded)Observe();Refresh();
    }
    private void DetachTemplate()
    {
        if(search is not null)search.TextChanged-=SearchChanged;if(reset is not null){reset.Click-=ResetClicked;reset.Content=null;}
        layout?.Children.Clear();host?.Children.Clear();layout=null;host=null;search=null;reset=null;
    }
    private void Observe()
    {
        Disconnect();observedTable=Table;
        if(GetValue(TableProperty) is not null && observedTable is null)throw new ArgumentException("Table must implement IBleeckerDataTableFilterSource.");
        if(observedTable is null)throw new InvalidOperationException("A mounted table toolbar requires its consumer Table.");
        var source=observedTable;tableChanged=(_,_)=>TableChanged(source);observedTable.Changed+=tableChanged;observedRoot=XamlRoot;if(observedRoot is not null)observedRoot.Changed+=RootChanged;
    }
    private void Disconnect(){if(observedTable is not null&&tableChanged is not null)observedTable.Changed-=tableChanged;observedTable=null;tableChanged=null;if(observedRoot is not null)observedRoot.Changed-=RootChanged;observedRoot=null;}
    private void RootChanged(XamlRoot sender,XamlRootChangedEventArgs args)=>Refresh();
    private void TableChanged(IBleeckerDataTableFilterSource source)
    {
        void Apply(){if(IsLoaded&&ReferenceEquals(source,Table)&&ReferenceEquals(source,observedTable))Refresh();}
        if(DispatcherQueue.HasThreadAccess)Apply();else DispatcherQueue.TryEnqueue(Apply);
    }
    private void Refresh()
    {
        if(resetting||layout is null||search is null||reset is null)return;
        var source=Table;if(source is not null&&source.ColumnFilterCount<0)throw new InvalidOperationException("ColumnFilterCount must be nonnegative.");
        if(!double.IsFinite(Spacing)||Spacing<0)throw new ArgumentOutOfRangeException(nameof(Spacing));
        search.Visibility=string.IsNullOrEmpty(SearchColumn)?Visibility.Collapsed:Visibility.Visible;
        search.PlaceholderText=SearchPlaceholder;AutomationProperties.SetName(search,SearchPlaceholder);
        var text=string.IsNullOrEmpty(SearchColumn)?"":BleeckerDataTableValue.InputText(source?.GetColumn(SearchColumn)?.GetFilterValue());
        if(search.Text!=text){updatingText=true;try{var start=search.SelectionStart;search.Text=text;search.Select(Math.Min(start,text.Length),0);}finally{updatingText=false;}}
        reset.Visibility=source is not null&&(source.ColumnFilterCount>0||BleeckerDataTableValue.Truthy(source.GlobalFilter))?Visibility.Visible:Visibility.Collapsed;
        filters.Update(FilterRenderers,FilterTemplate);actions.Update(Actions,ActionTemplate);
        layout.SetItems(search.Visibility==Visibility.Visible?search:null,filters.Elements,reset.Visibility==Visibility.Visible?reset:null,actions.Elements);
        layout.Wide=XamlRoot is not null&&XamlRoot.Size.Width>=640;layout.Spacing=Spacing;layout.InvalidateMeasure();
    }
    private void SearchChanged(object sender,TextChangedEventArgs args)
    {
        if(updatingText||search is null||!IsLoaded||string.IsNullOrEmpty(SearchColumn))return;
        if(!IsEnabled){Refresh();return;}
        var column=Table!.GetColumn(SearchColumn);
        // TextChanged may be queued after a controlled presentation update.
        // Displaying a numeric/object filter must not write a string back.
        if(search.Text==BleeckerDataTableValue.InputText(column?.GetFilterValue()))return;
        column?.SetFilterValue(search.Text);Refresh();
    }
    private void ResetClicked(object sender,RoutedEventArgs args)
    {
        if(!IsEnabled||!IsLoaded)return;
        // WinUI raises Click while routing PointerReleased. React publishes
        // this conditional removal after its input handler. Preserve that
        // boundary so removing Reset cannot swallow its real release.
        resetting=true;
        try{Table!.ResetColumnFilters();}
        finally{resetting=false;DispatcherQueue.TryEnqueue(()=>{if(IsLoaded)Refresh();});}
    }
    protected override AutomationPeer OnCreateAutomationPeer()=>new ToolbarPeer(this);
    private sealed class ToolbarPeer(BleeckerDataTableToolbar owner):FrameworkElementAutomationPeer(owner)
    {protected override string GetClassNameCore()=>nameof(BleeckerDataTableToolbar);protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Group;}
}
