using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

public readonly record struct BleeckerSelectRequestedEventArgs(string Value,BleeckerSelectOption Option);

/// <summary>A branded native choice menu over the caller's original options and value.</summary>
public partial class BleeckerSelect:Control
{
    private Grid? root;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue uiQueue=Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()??throw new InvalidOperationException("Select requires a UI dispatcher.");
    private SizingHost? sizing;
    private BleeckerSelectSurface? select;
    private BleeckerSelectAppearance? appearance;
    private readonly BleeckerSelectModel model=new();
    private readonly BleeckerSelectOption headerEmpty=new();
    private readonly Dictionary<BleeckerSelectItem,string> keys=[];
    private readonly List<BleeckerSelectOption> observedItems=[];
    private INotifyCollectionChanged? observedOptions;
    private XamlRoot? observedRoot;
    private BleeckerSelectOption[] menu=[];
    private bool updating,autoFocused,autoFocusCaptured,mountAutoFocus;
    internal virtual bool HeaderRecipe=>false;
    internal BleeckerSelectModel.Metrics Metrics=>BleeckerSelectModel.Sizes((int)Size);
    internal BleeckerContentText? ValueText=>appearance?.Label;
    internal BleeckerContentText? IconText=>appearance?.Icon;
    internal AutomationPeer Metadata=>FrameworkElementAutomationPeer.CreatePeerForElement(this);
    internal IEnumerable<AutomationPeer> DescriptionPeers=>((MetadataPeer)Metadata).Descriptions();
    public BleeckerSelect()
    {
        DefaultStyleKey=typeof(BleeckerSelect);IsTabStop=false;UseLayoutRounding=false;Options=new ObservableCollection<BleeckerSelectOption>();AutomationProperties.SetAccessibilityView(this,AccessibilityView.Raw);
        Loaded+=(_,_)=>{autoFocusCaptured=true;mountAutoFocus=AutoFocus&&IsEnabled;Observe();Render();TryAutoFocus();};Unloaded+=(_,_)=>{select?.DismissWithoutFocus();Disconnect();autoFocused=autoFocusCaptured=false;};
        RegisterPropertyChangedCallback(TemplateProperty,(_,_)=>{if(Template is null)Detach();});
        foreach(var property in new[]{IsEnabledProperty,FontFamilyProperty,FontSizeProperty,FontWeightProperty,ForegroundProperty,BackgroundProperty,BorderBrushProperty,BorderThicknessProperty,CornerRadiusProperty,PaddingProperty,IsTextScaleFactorEnabledProperty,LanguageProperty,StyleProperty,AutomationProperties.NameProperty,AutomationProperties.LabeledByProperty,AutomationProperties.HelpTextProperty,AutomationProperties.AutomationIdProperty,AutomationProperties.IsRequiredForFormProperty,AutomationProperties.IsDataValidForFormProperty})RegisterPropertyChangedCallback(property,(_,_)=>Render());
    }
    private static DependencyProperty Property(string name,Type type,object? value)=>DependencyProperty.Register(name,type,typeof(BleeckerSelect),new PropertyMetadata(value,(owner,_)=>((BleeckerSelect)owner).Render()));
    public static readonly DependencyProperty ValueProperty=Property(nameof(Value),typeof(string),"");
    public static readonly DependencyProperty PlaceholderProperty=Property(nameof(Placeholder),typeof(string),"Select…");
    public static readonly DependencyProperty SizeProperty=Property(nameof(Size),typeof(BleeckerControlSize),BleeckerControlSize.Medium);
    public static readonly DependencyProperty ErrorProperty=Property(nameof(Error),typeof(bool),false);
    public static readonly DependencyProperty RequiredProperty=Property(nameof(Required),typeof(bool),false);
    public static readonly DependencyProperty AutoFocusProperty=Property(nameof(AutoFocus),typeof(bool),false);
    public static readonly DependencyProperty StartIconProperty=Property(nameof(StartIcon),typeof(object),null);
    public static readonly DependencyProperty StartIconTemplateProperty=Property(nameof(StartIconTemplate),typeof(DataTemplate),null);
    public static readonly DependencyProperty StartIconTemplateSelectorProperty=Property(nameof(StartIconTemplateSelector),typeof(DataTemplateSelector),null);
    public static readonly DependencyProperty OptionsProperty=DependencyProperty.Register(nameof(Options),typeof(object),typeof(BleeckerSelect),new PropertyMetadata(null,(owner,args)=>{var select=(BleeckerSelect)owner;select.DisconnectOptions();if(args.NewValue is null&&!select.IsLoaded&&select.GetBindingExpression(OptionsProperty)is not null)return;if(args.NewValue is not IEnumerable<BleeckerSelectOption>)throw new ArgumentException("Select Options requires a non-null sequence of original BleeckerSelectOption objects.");if(select.IsLoaded)select.ObserveOptions();select.Render();}));
    public string Value {get=>GetValue(ValueProperty) as string??throw new InvalidOperationException("Select Value binding must resolve before mounting.");set=>SetValue(ValueProperty,value??throw new ArgumentNullException(nameof(value)));}
    public string Placeholder {get=>(string)GetValue(PlaceholderProperty);set=>SetValue(PlaceholderProperty,value??throw new ArgumentNullException(nameof(value)));}
    public BleeckerControlSize Size {get=>(BleeckerControlSize)GetValue(SizeProperty);set{if(!Enum.IsDefined(value))throw new ArgumentOutOfRangeException(nameof(value));SetValue(SizeProperty,value);}}
    public bool Error {get=>(bool)GetValue(ErrorProperty);set=>SetValue(ErrorProperty,value);}
    public bool Required {get=>(bool)GetValue(RequiredProperty);set=>SetValue(RequiredProperty,value);}
    public bool AutoFocus {get=>(bool)GetValue(AutoFocusProperty);set=>SetValue(AutoFocusProperty,value);}
    public object? StartIcon {get=>GetValue(StartIconProperty);set=>SetValue(StartIconProperty,value);}
    public DataTemplate? StartIconTemplate {get=>(DataTemplate?)GetValue(StartIconTemplateProperty);set=>SetValue(StartIconTemplateProperty,value);}
    public DataTemplateSelector? StartIconTemplateSelector {get=>(DataTemplateSelector?)GetValue(StartIconTemplateSelectorProperty);set=>SetValue(StartIconTemplateSelectorProperty,value);}
    public IEnumerable<BleeckerSelectOption> Options {get=>GetValue(OptionsProperty) as IEnumerable<BleeckerSelectOption>??throw new InvalidOperationException("Select Options binding must resolve before mounting.");set=>SetValue(OptionsProperty,value??throw new ArgumentNullException(nameof(value)));}
    public event EventHandler<BleeckerSelectRequestedEventArgs>? ValueRequested;
    protected override void OnApplyTemplate()
    {
        Detach();if(Template is null)return;base.OnApplyTemplate();root=GetTemplateChild("PART_Layout") as Grid??throw new InvalidOperationException("Select requires PART_Layout.");
        select=new BleeckerSelectSurface(()=>updating,()=>GetValue(OptionsProperty),()=>model.Display(Value,Placeholder,menu),item=>item.Tag is BleeckerSelectOption option&&model.Matches(Value,menu,option),"Select options",GetType().Name){Name="SelectTrigger",MinWidth=0,MinHeight=0,UseLayoutRounding=false,FlowDirection=FlowDirection.LeftToRight};
        select.Loaded+=SelectLoaded;appearance=new(this,select);select.Appearance=appearance;select.RequestSelection=Request;select.Template=appearance.Template;select.SetBinding(FontFamilyProperty,new Binding{Source=this,Path=new PropertyPath(nameof(FontFamily))});
        sizing=new(this,select){UseLayoutRounding=false,FlowDirection=FlowDirection.LeftToRight};root.Children.Add(sizing);if(IsLoaded)Observe();Render();
    }
    private void Request(BleeckerSelectItem item)
    {
        if(updating||!IsLoaded||!IsEnabled||select is null||!select.Items.Contains(item)||!item.IsEnabled||item.Tag is not BleeckerSelectOption option||!Options.Any(original=>ReferenceEquals(original,option))&& !ReferenceEquals(option,headerEmpty))return;
        var requested=model.Request(Value,menu,option);if(requested is not null)ValueRequested?.Invoke(this,new(requested,option));Render();
    }
    private void Render()
    {
        if(select is null||appearance is null)return;
        if(!IsLoaded&&(GetValue(OptionsProperty)is not IEnumerable<BleeckerSelectOption>||GetValue(ValueProperty)is not string))return;
        _=Metrics;var originals=Options.ToArray();if(originals.Any(option=>option is null))throw new ArgumentException("Select options must not be null.");
        headerEmpty.Label=Placeholder;menu=HeaderRecipe&&!originals.Any(option=>option.Value=="")?[headerEmpty,..originals]:originals;
        var restore=select.PreserveOptionsFocus(item=>keys.TryGetValue(item,out var key)?key:((BleeckerSelectOption)item.Tag).Value);var unused=select.Items.OfType<BleeckerSelectItem>().ToList();var next=new List<BleeckerSelectItem>();var nextKeys=new Dictionary<BleeckerSelectItem,string>();updating=true;
        try
        {
            appearance.Update();select.IsEnabled=IsEnabled;
            foreach(var option in menu){var index=unused.FindIndex(item=>keys.TryGetValue(item,out var key)&&key==option.Value);var item=index>=0?unused[index]:new BleeckerSelectItem(select);if(index>=0)unused.RemoveAt(index);item.Tag=option;item.Content=option.Label;item.IsEnabled=!option.IsDisabled;next.Add(item);nextKeys.Add(item,option.Value);}
            for(var index=select.Items.Count-1;index>=0;index--)if(!next.Contains(select.Items[index]))select.Items.RemoveAt(index);
            for(var index=0;index<next.Count;index++){var current=select.Items.IndexOf(next[index]);if(current==index)continue;if(current>=0)select.Items.RemoveAt(current);select.Items.Insert(index,next[index]);}
            keys.Clear();foreach(var entry in nextKeys)keys.Add(entry.Key,entry.Value);select.SelectedItem=next.LastOrDefault(item=>model.Matches(Value,menu,(BleeckerSelectOption)item.Tag));select.RefreshOpenValue();sizing?.InvalidateMeasure();InvalidateMeasure();FrameworkElementAutomationPeer.FromElement(select)?.InvalidatePeer();FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
        }
        finally{updating=false;}
        restore?.Invoke();TryAutoFocus();
    }
    private void ObserveOptions(){DisconnectOptions();observedOptions=Options as INotifyCollectionChanged;if(observedOptions is not null)observedOptions.CollectionChanged+=OptionsChanged;foreach(var option in Options.Distinct()){if(option is null)throw new ArgumentException("Select options must not be null.");option.PropertyChanged+=OptionChanged;observedItems.Add(option);}}
    private void DisconnectOptions(){if(observedOptions is not null)observedOptions.CollectionChanged-=OptionsChanged;observedOptions=null;foreach(var option in observedItems)option.PropertyChanged-=OptionChanged;observedItems.Clear();}
    private void Dispatch(Action action){if(uiQueue.HasThreadAccess){if(IsLoaded)action();}else if(!uiQueue.TryEnqueue(()=>{if(IsLoaded)action();}))throw new InvalidOperationException("Select could not dispatch its original producer notification.");}
    private void OptionsChanged(object? sender,NotifyCollectionChangedEventArgs args)=>Dispatch(()=>{if(ReferenceEquals(sender,GetValue(OptionsProperty))){ObserveOptions();Render();}});
    private void OptionChanged(object? sender,PropertyChangedEventArgs args)=>Dispatch(()=>{if(observedItems.Any(option=>ReferenceEquals(option,sender)))Render();});
    private void Observe(){ObserveOptions();if(observedRoot is not null)observedRoot.Changed-=RootChanged;observedRoot=XamlRoot;if(observedRoot is not null)observedRoot.Changed+=RootChanged;}
    private void RootChanged(XamlRoot sender,XamlRootChangedEventArgs args){select?.QueueOpenPosition();TryAutoFocus();}
    private void Disconnect(){DisconnectOptions();if(observedRoot is not null)observedRoot.Changed-=RootChanged;observedRoot=null;}
    private void SelectLoaded(object sender,RoutedEventArgs args)=>TryAutoFocus();
    private void TryAutoFocus(){if(!autoFocusCaptured||!mountAutoFocus||autoFocused||!IsLoaded||XamlRoot?.IsHostVisible!=true||select?.IsLoaded!=true)return;autoFocused=true;if(!IsEnabled)return;if(!select.Focus(FocusState.Programmatic))throw new InvalidOperationException("Select could not apply requested autofocus.");}
    private void Detach(){select?.DismissWithoutFocus();if(select is not null){select.RequestSelection=null;select.Loaded-=SelectLoaded;}appearance?.Dispose();root?.Children.Clear();root=null;select=null;appearance=null;sizing=null;keys.Clear();menu=[];}
    internal static bool Supplied(Control owner,DependencyProperty property){if(owner.ReadLocalValue(property)!=DependencyProperty.UnsetValue)return true;for(var style=owner.Style;style is not null;style=style.BasedOn)if(style.Setters.OfType<Setter>().Any(setter=>setter.Property==property))return true;return false;}
    protected override AutomationPeer OnCreateAutomationPeer()=>new MetadataPeer(this);
    private sealed class MetadataPeer(BleeckerSelect owner):FrameworkElementAutomationPeer(owner)
    {
        internal IEnumerable<AutomationPeer> Descriptions()=>base.GetDescribedByCore();
        protected override string GetNameCore()=>Supplied(owner,AutomationProperties.NameProperty)||AutomationProperties.GetLabeledBy(owner)is not null?base.GetNameCore():owner.Placeholder;
        protected override bool IsRequiredForFormCore()=>owner.Required||base.IsRequiredForFormCore();
        protected override bool IsDataValidForFormCore()=>!owner.Error&&base.IsDataValidForFormCore();
    }
    private sealed partial class SizingHost(BleeckerSelect owner,FrameworkElement select):Panel
    {
        protected override Size MeasureOverride(Size available)
        {
            if(Children.Count==0)Children.Add(select);
            select.Measure(new Size(owner.HeaderRecipe?double.PositiveInfinity:available.Width,owner.Metrics.Height));
            if(owner.HeaderRecipe&&select.DesiredSize.Width>available.Width)
                select.Measure(new Size(available.Width,owner.Metrics.Height));
            return new(owner.HeaderRecipe?Math.Min(select.DesiredSize.Width,available.Width):double.IsFinite(available.Width)?available.Width:select.DesiredSize.Width,owner.Metrics.Height);
        }
        protected override Size ArrangeOverride(Size final){select.Arrange(new Rect(0,0,final.Width,final.Height));return final;}
    }
}
