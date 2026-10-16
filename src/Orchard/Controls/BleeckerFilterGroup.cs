using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

/// <summary>A wrapping collection of consumer-owned filters. Clear/removal never mutates Filters automatically.</summary>
[ContentProperty(Name=nameof(Filters))]
public sealed class BleeckerFilterGroup : Control
{
    private Grid? host;
    private WrapLayout? layout;
    private BleeckerFilterAction? clear;
    private readonly List<Entry> entries=[];
    private readonly Dictionary<string,TextBlock> palette=[];
    private readonly List<(DependencyObject Owner,DependencyProperty Property,long Token)> paletteObservers=[];
    private readonly HashSet<BleeckerFilter> models=[];
    private ObservableCollection<BleeckerFilter>? collection;
    private ICommand? observedCommand;
    private EventHandler? cleared;
    public BleeckerFilterGroup()
    {
        DefaultStyleKey=typeof(BleeckerFilterGroup);Filters=[];
        Loaded+=(_,_)=>{Observe();Render();};Unloaded+=(_,_)=>{if(IsLoaded)return;StopObserving();clear?.Detach();};ActualThemeChanged+=(_,_)=>Render();
        foreach(var property in new[]{FontFamilyProperty,FontSizeProperty,FontWeightProperty,FontStyleProperty,FontStretchProperty,CharacterSpacingProperty,IsTextScaleFactorEnabledProperty,FlowDirectionProperty,IsEnabledProperty})RegisterPropertyChangedCallback(property,(_,_)=>Render());
        RegisterPropertyChangedCallback(TemplateProperty,(_,_)=>{if(Template is null)DetachTemplate();});
    }
    public static readonly DependencyProperty FiltersProperty=DependencyProperty.Register(nameof(Filters),typeof(object),typeof(BleeckerFilterGroup),new PropertyMetadata(null,Changed));
    public static readonly DependencyProperty ClearAllCommandProperty=DependencyProperty.Register(nameof(ClearAllCommand),typeof(ICommand),typeof(BleeckerFilterGroup),new PropertyMetadata(null,Changed));
    public static readonly DependencyProperty ClearAllCommandParameterProperty=DependencyProperty.Register(nameof(ClearAllCommandParameter),typeof(object),typeof(BleeckerFilterGroup),new PropertyMetadata(null,Changed));
    public static readonly DependencyProperty ClearAllTextProperty=DependencyProperty.Register(nameof(ClearAllText),typeof(string),typeof(BleeckerFilterGroup),new PropertyMetadata("Clear all",Changed));
    public ObservableCollection<BleeckerFilter> Filters{get=>GetValue(FiltersProperty)as ObservableCollection<BleeckerFilter>??throw new InvalidOperationException("FilterGroup Filters must be supplied before mounting.");set=>SetValue(FiltersProperty,value??throw new ArgumentNullException(nameof(value)));}
    public ICommand? ClearAllCommand{get=>(ICommand?)GetValue(ClearAllCommandProperty);set=>SetValue(ClearAllCommandProperty,value);}
    public object? ClearAllCommandParameter{get=>GetValue(ClearAllCommandParameterProperty);set=>SetValue(ClearAllCommandParameterProperty,value);}
    public string ClearAllText{get=>(string)GetValue(ClearAllTextProperty);set=>SetValue(ClearAllTextProperty,value??throw new ArgumentNullException(nameof(value)));}
    public event EventHandler? ClearAllRequested{add{cleared+=value;Render();}remove{cleared-=value;Render();}}
    private bool CanClear=>IsLoaded&&IsEnabled&&Filters.Count>0&&(ClearAllCommand is not null||cleared is not null)&&(ClearAllCommand?.CanExecute(ClearAllCommandParameter)??true);
    private static void Changed(DependencyObject owner,DependencyPropertyChangedEventArgs args)
    {
        var group=(BleeckerFilterGroup)owner;
        if(args.Property==FiltersProperty&&args.NewValue is not ObservableCollection<BleeckerFilter>&&!(args.NewValue is null&&!group.IsLoaded&&group.GetBindingExpression(FiltersProperty)is not null))throw new ArgumentException("FilterGroup requires a non-null ObservableCollection<BleeckerFilter>.");
        if(args.Property==ClearAllTextProperty&&args.NewValue is not string)throw new ArgumentException("ClearAllText must be a string.");
        if(group.IsLoaded)group.Observe();group.Render();
    }
    protected override void OnApplyTemplate()
    {
        DetachTemplate();base.OnApplyTemplate();if(Template is null)return;
        host=GetTemplateChild("PART_Content")as Grid??throw new InvalidOperationException("FilterGroup requires PART_Content.");
        foreach(var name in new[]{"Clear","ClearHover","Transparent"})palette[name]=GetTemplateChild("PART_"+name)as TextBlock??throw new InvalidOperationException("FilterGroup requires PART_"+name+".");
        layout=new WrapLayout(this);host.Children.Add(layout);
        BleeckerFilterAction? current=null;
        current=new BleeckerFilterAction(this,false,InvokeClear,()=>CanClear&&ReferenceEquals(clear,current),()=>palette["Clear"].Foreground,()=>palette["ClearHover"].Foreground,()=>palette["Transparent"].Foreground,()=>ClearAllText){Name="ClearAllFilters"};clear=current;
        if(IsLoaded)Observe();Render();
    }
    private void DetachTemplate(){StopObserving();clear?.Detach();foreach(var entry in entries)entry.Dispose();entries.Clear();layout?.Children.Clear();host?.Children.Clear();clear=null;layout=null;host=null;palette.Clear();}
    private void Observe()
    {
        StopObserving();if(GetValue(FiltersProperty)is not ObservableCollection<BleeckerFilter> values)return;
        collection=values;collection.CollectionChanged+=CollectionChanged;
        foreach(var model in values){if(model is null)throw new ArgumentException("FilterGroup cannot contain null filters.");if(models.Add(model))model.PropertyChanged+=ModelChanged;}
        observedCommand=ClearAllCommand;if(observedCommand is not null)observedCommand.CanExecuteChanged+=CommandChanged;
        foreach(var text in palette.Values)
        {
            paletteObservers.Add((text,TextBlock.ForegroundProperty,text.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty,(_,_)=>{Observe();Render();})));
            if(text.Foreground is SolidColorBrush brush)
                foreach(var property in new[]{SolidColorBrush.ColorProperty,Brush.OpacityProperty})
                    paletteObservers.Add((brush,property,brush.RegisterPropertyChangedCallback(property,(_,_)=>Render())));
        }
        clear?.Attach();
    }
    private void StopObserving()
    {
        if(collection is not null)collection.CollectionChanged-=CollectionChanged;collection=null;
        foreach(var model in models)model.PropertyChanged-=ModelChanged;models.Clear();if(observedCommand is not null)observedCommand.CanExecuteChanged-=CommandChanged;observedCommand=null;
        foreach(var item in paletteObservers)item.Owner.UnregisterPropertyChangedCallback(item.Property,item.Token);paletteObservers.Clear();
    }
    private void CollectionChanged(object? sender,NotifyCollectionChangedEventArgs args){Observe();Render();}
    private void ModelChanged(object? sender,PropertyChangedEventArgs args)=>Render();
    private void CommandChanged(object? sender,EventArgs args)=>DispatcherQueue.TryEnqueue(Render);
    private void InvokeClear(){if(!CanClear)return;var command=ClearAllCommand;var callback=cleared;command?.Execute(ClearAllCommandParameter);callback?.Invoke(this,EventArgs.Empty);}
    private void Render()
    {
        if(layout is null||clear is null||GetValue(FiltersProperty)is not ObservableCollection<BleeckerFilter> filters)return;
        var remaining=entries.ToList();var next=new List<Entry>();
        foreach(var model in filters)
        {
            if(model is null)throw new ArgumentException("FilterGroup cannot contain null filters.");
            var entry=remaining.FirstOrDefault(item=>ReferenceEquals(item.Model,model));if(entry is null){entry=new Entry(this,model);layout.Children.Add(entry.Chip);}else remaining.Remove(entry);
            entry.Update();next.Add(entry);
        }
        foreach(var removed in remaining){removed.Dispose();layout.Children.Remove(removed.Chip);}entries.Clear();entries.AddRange(next);
        var show=filters.Count>0&&(ClearAllCommand is not null||cleared is not null);
        if(show&&!layout.Children.Contains(clear))layout.Children.Add(clear);if(!show&&layout.Children.Contains(clear))layout.Children.Remove(clear);
        var focused=XamlRoot is null?null:FocusManager.GetFocusedElement(XamlRoot)as Control;
        var retainedFocus=focused is not null&&entries.Any(entry=>Contains(entry.Chip,focused));
        for(var index=0;index<entries.Count;index++)
        {
            var child=entries[index].Chip;
            if(index<layout.Children.Count&&ReferenceEquals(layout.Children[index],child))continue;
            layout.Children.Remove(child);layout.Children.Insert(index,child);
        }
        if(retainedFocus&&focused!.IsLoaded&&!ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot),focused))focused.Focus(FocusState.Keyboard);
        clear.Update();Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(clear,ClearAllText);
        for(var index=0;index<entries.Count;index++)entries[index].Chip.TabIndex=index;clear.TabIndex=entries.Count;
        layout.InvalidateMeasure();
    }
    private static bool Contains(DependencyObject ancestor,DependencyObject child)
    {for(DependencyObject? node=child;node is not null;node=VisualTreeHelper.GetParent(node))if(ReferenceEquals(node,ancestor))return true;return false;}
    private sealed class Entry:IDisposable
    {
        private readonly BleeckerFilterGroup owner;internal BleeckerFilter Model{get;}internal BleeckerFilterChip Chip{get;}=new(){HorizontalAlignment=HorizontalAlignment.Left};private bool handler,active=true;
        internal Entry(BleeckerFilterGroup owner,BleeckerFilter model){this.owner=owner;Model=model;Chip.InvocationAllowed=()=>active&&owner.IsLoaded&&owner.IsEnabled&&owner.Filters.Contains(Model);}
        internal void Update()
        {
            Chip.Label=Model.Label;Chip.Value=Model.Value;Chip.RemoveCommand=Model.RemoveCommand;Chip.RemoveCommandParameter=Model.RemoveCommandParameter;
            Chip.FontFamily=owner.FontFamily;Chip.FontSize=owner.FontSize;Chip.FontWeight=owner.FontWeight;Chip.FontStyle=owner.FontStyle;Chip.FontStretch=owner.FontStretch;Chip.CharacterSpacing=owner.CharacterSpacing;Chip.IsTextScaleFactorEnabled=owner.IsTextScaleFactorEnabled;Chip.FlowDirection=owner.FlowDirection;
            if(handler!=Model.HasRemoveHandler){if(handler)Chip.RemoveRequested-=Remove;handler=Model.HasRemoveHandler;if(handler)Chip.RemoveRequested+=Remove;}
        }
        private void Remove(object? sender,EventArgs args){if(owner.IsLoaded&&owner.IsEnabled&&owner.Filters.Contains(Model))Model.NotifyRemove();}
        public void Dispose(){active=false;if(handler)Chip.RemoveRequested-=Remove;handler=false;}
    }
    private sealed class WrapLayout(BleeckerFilterGroup owner):Panel
    {
        private readonly List<(UIElement Child,double X,double Y,double Width,double Height)> placed=[];
        protected override Size MeasureOverride(Size available)
        {
            placed.Clear();var children=owner.entries.Select(entry=>(UIElement)entry.Chip).ToList();if(owner.clear is not null&&Children.Contains(owner.clear))children.Add(owner.clear);
            double x=0,y=0,rowHeight=0,usedWidth=0;var rowStart=0;
            foreach(var child in children)
            {
                child.Measure(new Size(available.Width,double.PositiveInfinity));var size=child.DesiredSize;
                if(x>0&&x+8+size.Width>available.Width){CenterRow(rowStart,rowHeight);rowStart=placed.Count;y+=rowHeight+8;x=0;rowHeight=0;}
                if(x>0)x+=8;placed.Add((child,x,y,size.Width,size.Height));x+=size.Width;usedWidth=Math.Max(usedWidth,x);rowHeight=Math.Max(rowHeight,size.Height);
            }
            CenterRow(rowStart,rowHeight);return new Size(usedWidth,placed.Count==0?0:y+rowHeight);
        }
        private void CenterRow(int start,double height){for(var index=start;index<placed.Count;index++){var item=placed[index];placed[index]=(item.Child,item.X,item.Y+(height-item.Height)/2,item.Width,item.Height);}}
        protected override Size ArrangeOverride(Size final){foreach(var item in placed)item.Child.Arrange(new Rect(item.X,item.Y,item.Width,item.Height));return final;}
    }
}
