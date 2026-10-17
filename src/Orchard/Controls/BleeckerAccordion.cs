using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A single-expansion accordion retaining consumer-owned content and commands.</summary>
public sealed partial class BleeckerAccordion : Control
{
    private StackPanel? rows;
    private readonly Dictionary<string,Entry> entries=new(StringComparer.Ordinal);
    private ObservableCollection<BleeckerAccordionItem>? observed;
    private readonly HashSet<BleeckerAccordionItem> observedItems=[];
    private readonly Dictionary<string,TextBlock> probes=[];
    private readonly List<(DependencyObject Owner,DependencyProperty Property,long Token)> paletteTokens=[];
    private XamlRoot? observedRoot;
    private readonly UISettings settings=new();
    private readonly AccessibilitySettings accessibility=new();
    private bool initialized;
    private string? expandedId;
    private double lastTextScale=1;
    private readonly List<BleeckerDateShadow> shadows=[];
    internal bool Compact=>XamlRoot is null||XamlRoot.Size.Width<640;
    public string? ExpandedId=>initialized?expandedId:DefaultExpandedId;
    internal IReadOnlyList<Button> HeaderButtons=>Items.Select(item=>(Button)entries[item.Id].Header).ToArray();
    public BleeckerAccordion()
    {
        DefaultStyleKey=typeof(BleeckerAccordion);Items=[];
        Loaded+=(_,_)=>{if(!initialized){expandedId=DefaultExpandedId;initialized=true;}Observe();ObservePalette();Reconcile();};
        Unloaded+=(_,_)=>{if(!IsLoaded){StopObserving();StopPalette();foreach(var entry in entries.Values)entry.Stop();}};
        ActualThemeChanged+=(_,_)=>Refresh();
        LayoutUpdated+=(_,_)=>{if(IsLoaded&&lastTextScale!=settings.TextScaleFactor){lastTextScale=settings.TextScaleFactor;Refresh();}};
        foreach(var property in new[]{FontFamilyProperty,FontSizeProperty,CharacterSpacingProperty,IsTextScaleFactorEnabledProperty,ForegroundProperty,FlowDirectionProperty})RegisterPropertyChangedCallback(property,(_,_)=>Refresh());
        RegisterPropertyChangedCallback(TemplateProperty,(_,_)=>{if(Template is null)Detach();});
    }
    private static DependencyProperty Property(string name,Type type,object? value=null)=>DependencyProperty.Register(name,type,typeof(BleeckerAccordion),new PropertyMetadata(value,(owner,_)=>{var accordion=(BleeckerAccordion)owner;if(accordion.IsLoaded)accordion.Observe();accordion.Reconcile();}));
    public static readonly DependencyProperty ItemsProperty=Property(nameof(Items),typeof(object));
    public static readonly DependencyProperty DefaultExpandedIdProperty=Property(nameof(DefaultExpandedId),typeof(string));
    public static readonly DependencyProperty TitleTemplateProperty=Property(nameof(TitleTemplate),typeof(DataTemplate));
    public static readonly DependencyProperty SubtitleTemplateProperty=Property(nameof(SubtitleTemplate),typeof(DataTemplate));
    public static readonly DependencyProperty ContentTemplateProperty=Property(nameof(ContentTemplate),typeof(DataTemplate));
    public static readonly DependencyProperty ActionsTemplateProperty=Property(nameof(ActionsTemplate),typeof(DataTemplate));
    public ObservableCollection<BleeckerAccordionItem> Items {get=>GetValue(ItemsProperty) as ObservableCollection<BleeckerAccordionItem>??throw new InvalidOperationException("Accordion Items must be an observable collection.");set{Validate(value);SetValue(ItemsProperty,value);}}
    public string? DefaultExpandedId {get=>(string?)GetValue(DefaultExpandedIdProperty);set=>SetValue(DefaultExpandedIdProperty,value);}
    public DataTemplate? TitleTemplate {get=>(DataTemplate?)GetValue(TitleTemplateProperty);set=>SetValue(TitleTemplateProperty,value);}
    public DataTemplate? SubtitleTemplate {get=>(DataTemplate?)GetValue(SubtitleTemplateProperty);set=>SetValue(SubtitleTemplateProperty,value);}
    public DataTemplate? ContentTemplate {get=>(DataTemplate?)GetValue(ContentTemplateProperty);set=>SetValue(ContentTemplateProperty,value);}
    public DataTemplate? ActionsTemplate {get=>(DataTemplate?)GetValue(ActionsTemplateProperty);set=>SetValue(ActionsTemplateProperty,value);}
    private static void Validate(IEnumerable<BleeckerAccordionItem> items){ArgumentNullException.ThrowIfNull(items);var ids=new HashSet<string>(StringComparer.Ordinal);foreach(var item in items){if(item is null||item.Id is null||!ids.Add(item.Id))throw new ArgumentException("Accordion items require non-null, ordinal-unique IDs.",nameof(items));}}
    protected override void OnApplyTemplate()
    {
        Detach();base.OnApplyTemplate();if(Template is null)return;
        rows=GetTemplateChild("PART_Rows") as StackPanel??throw new InvalidOperationException("Accordion requires PART_Rows.");
        foreach(var (suffix,recipe) in new[]{("Near",new BleeckerDateShadow.Recipe(2,1,0,.025f,26,55,77)),("Far",new BleeckerDateShadow.Recipe(30,10,-18,.18f,26,55,77))}){
            var host=GetTemplateChild("PART_Shadow"+suffix) as Grid??throw new InvalidOperationException("Accordion requires shadow host "+suffix);
            var mask=GetTemplateChild("PART_Mask"+suffix) as Microsoft.UI.Xaml.Shapes.Path??throw new InvalidOperationException("Accordion requires shadow mask "+suffix);
            var exterior=GetTemplateChild("PART_Exterior"+suffix) as Microsoft.UI.Xaml.Shapes.Path??throw new InvalidOperationException("Accordion requires shadow exterior "+suffix);
            var shadow=new BleeckerDateShadow(this,host,mask,exterior);shadows.Add(shadow);shadow.Set(recipe);
        }
        foreach(var key in new[]{"Primary","Secondary","Divider","BodyBorder","Body","Hover"})probes[key]=GetTemplateChild("PART_"+key) as TextBlock??throw new InvalidOperationException("Accordion requires palette "+key);
        ObservePalette();Reconcile();
    }
    private void Detach(){StopPalette();foreach(var shadow in shadows)shadow.Dispose();shadows.Clear();foreach(var entry in entries.Values)entry.Release();rows?.Children.Clear();entries.Clear();rows=null;probes.Clear();}
    private void StopPalette(){foreach(var item in paletteTokens)item.Owner.UnregisterPropertyChangedCallback(item.Property,item.Token);paletteTokens.Clear();}
    private void ObservePalette()
    {
        StopPalette();if(!IsLoaded)return;
        void Watch(DependencyObject target,DependencyProperty property,Action changed)=>paletteTokens.Add((target,property,target.RegisterPropertyChangedCallback(property,(_,_)=>changed())));
        foreach(var probe in probes.Values){Watch(probe,TextBlock.ForegroundProperty,()=>{ObservePalette();Refresh();});if(probe.Foreground is SolidColorBrush brush){Watch(brush,SolidColorBrush.ColorProperty,Refresh);Watch(brush,Brush.OpacityProperty,Refresh);}}
    }
    private void Observe()
    {
        StopObserving();Validate(Items);observed=Items;observed.CollectionChanged+=ItemsChanged;foreach(var item in Items)if(observedItems.Add(item))item.PropertyChanged+=ItemChanged;
        observedRoot=XamlRoot;if(observedRoot is not null)observedRoot.Changed+=RootChanged;
    }
    private void StopObserving(){if(observed is not null)observed.CollectionChanged-=ItemsChanged;observed=null;foreach(var item in observedItems)item.PropertyChanged-=ItemChanged;observedItems.Clear();if(observedRoot is not null)observedRoot.Changed-=RootChanged;observedRoot=null;}
    private void RootChanged(XamlRoot sender,XamlRootChangedEventArgs args)=>Refresh();
    private void ItemsChanged(object? sender,NotifyCollectionChangedEventArgs args){Observe();Reconcile();}
    private void ItemChanged(object? sender,PropertyChangedEventArgs args)=>Reconcile();
    private void Refresh(){foreach(var entry in entries.Values)entry.Update(entry.Item);}
    private Brush Paint(string key)=>probes[key].Foreground;
    private FontFamily Font(string key)=>(FontFamily)Application.Current.Resources[key];
    private void Reconcile()
    {
        Validate(Items);if(rows is null)return;
        var wanted=Items.Select(item=>item.Id).ToHashSet(StringComparer.Ordinal);
        foreach(var key in entries.Keys.Where(key=>!wanted.Contains(key)).ToArray()){var old=entries[key];old.Release();rows.Children.Remove(old);entries.Remove(key);}
        var positions=rows.Children.Cast<Entry>().Select((entry,index)=>(entry,index)).ToDictionary(pair=>pair.entry,pair=>pair.index);var ordered=new List<Entry>();var stationary=new HashSet<Entry>();var highest=-1;
        for(var index=0;index<Items.Count;index++){var item=Items[index];if(!entries.TryGetValue(item.Id,out var entry)){entry=new(this,item);entries.Add(item.Id,entry);}entry.Update(item);entry.Divider.Visibility=index==Items.Count-1?Visibility.Collapsed:Visibility.Visible;ordered.Add(entry);if(positions.TryGetValue(entry,out var previous)&&previous>highest){stationary.Add(entry);highest=previous;}}
        var anchors=new Entry?[ordered.Count];Entry? next=null;for(var index=ordered.Count-1;index>=0;index--){anchors[index]=next;if(stationary.Contains(ordered[index]))next=ordered[index];}
        for(var index=0;index<ordered.Count;index++){var entry=ordered[index];if(stationary.Contains(entry))continue;rows.Children.Remove(entry);if(anchors[index] is {} anchor)rows.Children.Insert(rows.Children.IndexOf(anchor),entry);else rows.Children.Add(entry);}
        rows.InvalidateMeasure();
    }
    private bool Owns(Entry entry)=>IsLoaded&&entry.IsLoaded&&entries.TryGetValue(entry.Key,out var current)&&ReferenceEquals(current,entry);
    private void SetExpanded(Entry entry,bool value)
    {
        if(!Owns(entry))throw new ElementNotAvailableException();if(!IsEnabled||!entry.Header.IsEnabled)throw new ElementNotEnabledException();
        var next=value?entry.Key:ExpandedId==entry.Key?null:ExpandedId;if(next==ExpandedId)return;expandedId=next;foreach(var row in entries.Values)row.UpdateExpansion(true);
    }
    protected override AutomationPeer OnCreateAutomationPeer()=>new ListPeer(this);
    private sealed class ListPeer(BleeckerAccordion owner):FrameworkElementAutomationPeer(owner){protected override string GetClassNameCore()=>nameof(BleeckerAccordion);protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.List;}
}
