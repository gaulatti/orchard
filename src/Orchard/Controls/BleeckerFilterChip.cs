using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A filter label/value and optional native remove action. The consumer owns removal.</summary>
public sealed class BleeckerFilterChip : Control
{
    private Grid? host;
    private FilterLayout? layout;
    private Border? surface;
    private EventHandler? removed;
    private ICommand? observed;
    private readonly UISettings settings=new();
    internal Func<bool>? InvocationAllowed { get; set; }
    private readonly Dictionary<string,TextBlock> palette=[];
    private readonly List<(DependencyObject Owner,DependencyProperty Property,long Token)> observers=[];
    public BleeckerFilterChip()
    {
        DefaultStyleKey=typeof(BleeckerFilterChip);
        Loaded+=(_,_)=>{settings.TextScaleFactorChanged+=TextScaleChanged;Observe();Render();};Unloaded+=(_,_)=>{if(IsLoaded)return;settings.TextScaleFactorChanged-=TextScaleChanged;StopObserving();layout?.Detach();};ActualThemeChanged+=(_,_)=>Render();
        foreach(var property in new[]{FontFamilyProperty,FontSizeProperty,FontWeightProperty,CharacterSpacingProperty,IsTextScaleFactorEnabledProperty,FlowDirectionProperty,IsEnabledProperty,ForegroundProperty,TabIndexProperty,AutomationProperties.NameProperty,AutomationProperties.LabeledByProperty})RegisterPropertyChangedCallback(property,(_,_)=>Render());
        RegisterPropertyChangedCallback(TemplateProperty,(_,_)=>{if(Template is null)DetachTemplate();});
    }
    public static readonly DependencyProperty LabelProperty=DependencyProperty.Register(nameof(Label),typeof(string),typeof(BleeckerFilterChip),new PropertyMetadata("",Changed));
    public static readonly DependencyProperty ValueProperty=DependencyProperty.Register(nameof(Value),typeof(string),typeof(BleeckerFilterChip),new PropertyMetadata(null,Changed));
    public static readonly DependencyProperty RemoveCommandProperty=DependencyProperty.Register(nameof(RemoveCommand),typeof(ICommand),typeof(BleeckerFilterChip),new PropertyMetadata(null,Changed));
    public static readonly DependencyProperty RemoveCommandParameterProperty=DependencyProperty.Register(nameof(RemoveCommandParameter),typeof(object),typeof(BleeckerFilterChip),new PropertyMetadata(null,Changed));
    public string Label{get=>(string)GetValue(LabelProperty);set=>SetValue(LabelProperty,value??throw new ArgumentNullException(nameof(value)));}
    public string? Value{get=>(string?)GetValue(ValueProperty);set=>SetValue(ValueProperty,value);}
    public ICommand? RemoveCommand{get=>(ICommand?)GetValue(RemoveCommandProperty);set=>SetValue(RemoveCommandProperty,value);}
    public object? RemoveCommandParameter{get=>GetValue(RemoveCommandParameterProperty);set=>SetValue(RemoveCommandParameterProperty,value);}
    public event EventHandler? RemoveRequested{add{removed+=value;Render();}remove{removed-=value;Render();}}
    internal bool HasRemove=>RemoveCommand is not null||removed is not null;
    internal bool CanRemove=>IsLoaded&&IsEnabled&&HasRemove&&(InvocationAllowed?.Invoke()??true)&&(RemoveCommand?.CanExecute(RemoveCommandParameter)??true);
    private static void Changed(DependencyObject owner,DependencyPropertyChangedEventArgs args)
    {var chip=(BleeckerFilterChip)owner;if(args.Property==LabelProperty&&args.NewValue is not string)throw new ArgumentException("FilterChip Label must be a string.");if(chip.IsLoaded)chip.Observe();chip.Render();}
    protected override void OnApplyTemplate()
    {
        DetachTemplate();base.OnApplyTemplate();if(Template is null)return;
        host=GetTemplateChild("PART_Content")as Grid??throw new InvalidOperationException("FilterChip requires PART_Content.");
        surface=GetTemplateChild("PART_Surface")as Border??throw new InvalidOperationException("FilterChip requires PART_Surface.");
        foreach(var name in new[]{"Secondary","RemoveHover","RemoveHoverBackground"})palette[name]=GetTemplateChild("PART_"+name)as TextBlock??throw new InvalidOperationException("FilterChip requires PART_"+name+".");
        layout=new FilterLayout(this);host.Children.Add(layout);if(IsLoaded)Observe();Render();
    }
    private void DetachTemplate(){StopObserving();layout?.Detach();host?.Children.Clear();layout=null;host=null;surface=null;palette.Clear();}
    private void Observe()
    {
        StopObserving();observed=RemoveCommand;if(observed is not null)observed.CanExecuteChanged+=CommandChanged;
        foreach(var item in palette.Values)
        {
            observers.Add((item,TextBlock.ForegroundProperty,item.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty,(_,_)=>{Observe();Render();})));
            if(item.Foreground is SolidColorBrush brush)
                foreach(var property in new[]{SolidColorBrush.ColorProperty,Brush.OpacityProperty})
                    observers.Add((brush,property,brush.RegisterPropertyChangedCallback(property,(_,_)=>Render())));
        }
        layout?.Attach();
    }
    private void StopObserving(){if(observed is not null)observed.CanExecuteChanged-=CommandChanged;observed=null;foreach(var item in observers)item.Owner.UnregisterPropertyChangedCallback(item.Property,item.Token);observers.Clear();}
    private void CommandChanged(object? sender,EventArgs args)=>DispatcherQueue.TryEnqueue(Render);
    private void TextScaleChanged(UISettings sender,object args)=>DispatcherQueue.TryEnqueue(Render);
    private Brush Paint(string key)=>palette[key].Foreground;
    private void Render(){layout?.Update();}
    private void Invoke(){if(!CanRemove)return;var command=RemoveCommand;var callback=removed;command?.Execute(RemoveCommandParameter);callback?.Invoke(this,EventArgs.Empty);}

    private sealed class FilterLayout:Panel
    {
        private readonly BleeckerFilterChip owner;
        private readonly BleeckerFilterText label=new(){Name="FilterLabel"},value=new(){Name="FilterValue"};
        private readonly BleeckerFilterAction action;
        private double labelWidth,valueWidth,height,width;
        internal FilterLayout(BleeckerFilterChip owner)
        {
            this.owner=owner;UseLayoutRounding=false;Name="FilterLayout";
            action=new BleeckerFilterAction(owner,true,()=>owner.Invoke(),()=>owner.CanRemove&&ReferenceEquals(owner.layout,this),()=>owner.Paint("Secondary"),()=>owner.Paint("RemoveHover"),()=>owner.Paint("RemoveHoverBackground")){Name="RemoveFilterAction"};
            Children.Add(label);Children.Add(value);Children.Add(action);
        }
        internal void Attach()=>action.Attach();internal void Detach()=>action.Detach();
        internal void Update()
        {
            label.Set(owner,owner.Label,false,18);value.Set(owner,owner.Value??"",true,18);label.Text.Foreground=owner.Paint("Secondary");value.Text.Foreground=owner.Foreground;
            value.Visibility=string.IsNullOrEmpty(owner.Value)?Visibility.Collapsed:Visibility.Visible;action.Visibility=owner.HasRemove?Visibility.Visible:Visibility.Collapsed;
            var explicitName=AutomationProperties.GetName(owner);var labeledBy=AutomationProperties.GetLabeledBy(owner);AutomationProperties.SetLabeledBy(action,labeledBy);
            AutomationProperties.SetName(action,!string.IsNullOrEmpty(explicitName)?explicitName:labeledBy is not null?"":"Remove "+owner.Label+" filter");
            action.Update();InvalidateMeasure();
            action.TabIndex=owner.TabIndex;
        }
        protected override Size MeasureOverride(Size available)
        {
            var hasValue=value.Visibility==Visibility.Visible;var hasAction=action.Visibility==Visibility.Visible;
            var gap=6*((hasValue?1:0)+(hasAction?1:0));var preferred=label.PreferredWidth+(hasValue?value.PreferredWidth:0)+(hasAction?16:0)+gap;
            var minimum=label.MinimumWidth+(hasValue?value.MinimumWidth:0)+(hasAction?16:0)+gap;
            width=Math.Max(minimum,Math.Min(preferred,available.Width));labelWidth=label.PreferredWidth;valueWidth=hasValue?value.PreferredWidth:0;
            var actionWidth=hasAction?16:0;var total=labelWidth+valueWidth+actionWidth;
            if(width<preferred&&total>0)
            {
                // Flex-shrink uses each item's original content basis; freeze
                // text at min-content and redistribute the remaining deficit.
                var deficit=preferred-width;var activeLabel=labelWidth>label.MinimumWidth;var activeValue=hasValue&&valueWidth>value.MinimumWidth;var activeAction=hasAction;
                for(var pass=0;pass<3&&deficit>.000001;pass++)
                {
                    var basis=(activeLabel?label.PreferredWidth:0)+(activeValue?value.PreferredWidth:0)+(activeAction?16:0);if(basis==0)break;var used=0d;
                    if(activeLabel){var next=Math.Max(label.MinimumWidth,labelWidth-deficit*label.PreferredWidth/basis);used+=labelWidth-next;labelWidth=next;activeLabel=labelWidth>label.MinimumWidth+.000001;}
                    if(activeValue){var next=Math.Max(value.MinimumWidth,valueWidth-deficit*value.PreferredWidth/basis);used+=valueWidth-next;valueWidth=next;activeValue=valueWidth>value.MinimumWidth+.000001;}
                    if(activeAction){activeAction=false;}deficit-=used;
                }
            }
            label.Measure(new Size(labelWidth,double.PositiveInfinity));value.Measure(new Size(valueWidth,double.PositiveInfinity));action.Measure(new Size(16,16));
            height=Math.Max(label.DesiredSize.Height,Math.Max(hasValue?value.DesiredSize.Height:0,hasAction?16:0));return new Size(width,height);
        }
        protected override Size ArrangeOverride(Size final)
        {
            var x=0d;label.Arrange(new Rect(x,(final.Height-label.DesiredSize.Height)/2,labelWidth,label.DesiredSize.Height));x+=labelWidth;
            if(value.Visibility==Visibility.Visible){x+=6;value.Arrange(new Rect(x,(final.Height-value.DesiredSize.Height)/2,valueWidth,value.DesiredSize.Height));x+=valueWidth;}
            if(action.Visibility==Visibility.Visible){x+=6;action.Arrange(new Rect(x,(final.Height-16)/2,16,16));}return final;
        }
    }
}
