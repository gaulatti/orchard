using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.System;

namespace Orchard;

/// <summary>A native text editor with a search adornment and consumer-owned clear action.</summary>
public sealed class BleeckerSearchInput : Control
{
    private Grid? host;
    private BleeckerFilterAction? clear;
    private EventHandler? clearRequested;
    private ICommand? observedCommand;
    private readonly Dictionary<string,TextBlock> palette=[];
    private readonly List<(DependencyObject Owner,DependencyProperty Property,long Token)> observers=[];
    private bool applyingPadding;
    public BleeckerSearchInput()
    {
        DefaultStyleKey=typeof(BleeckerSearchInput);
        Editor=new BleeckerInput{Name="SearchEditor"};
        Bind(Editor,TextBox.TextProperty,nameof(Text),BindingMode.TwoWay);
        Bind(Editor,TextBox.PlaceholderTextProperty,nameof(PlaceholderText));
        Bind(Editor,TextBox.IsReadOnlyProperty,nameof(IsReadOnly));
        Bind(Editor,TextBox.MaxLengthProperty,nameof(MaxLength));
        Bind(Editor,IsEnabledProperty,nameof(IsEditorEnabled));
        Bind(Editor,BleeckerInput.InputSizeProperty,nameof(InputSize));
        Bind(Editor,BleeckerInput.IsErrorProperty,nameof(IsError));
        foreach(var(property,name)in new[]{(FontFamilyProperty,nameof(FontFamily)),(FontWeightProperty,nameof(FontWeight)),(FontStyleProperty,nameof(FontStyle)),(FontStretchProperty,nameof(FontStretch)),(CharacterSpacingProperty,nameof(CharacterSpacing)),(IsTextScaleFactorEnabledProperty,nameof(IsTextScaleFactorEnabled)),(FlowDirectionProperty,nameof(FlowDirection)),(ForegroundProperty,nameof(Foreground)),(TabIndexProperty,nameof(TabIndex))})Bind(Editor,property,name);
        foreach(var property in new[]{AutomationProperties.NameProperty,AutomationProperties.LabeledByProperty,TabIndexProperty})RegisterPropertyChangedCallback(property,(_,_)=>Update());
        Editor.TextChanged+=(_,_)=>Update();
        Editor.KeyDown+=EditorKeyDown;
        Editor.RegisterPropertyChangedCallback(PaddingProperty,(_,_)=>ApplyPadding());
        Loaded+=(_,_)=>{Observe();Update();};
        Unloaded+=(_,_)=>{if(!IsLoaded){StopObserving();clear?.Detach();}};
        ActualThemeChanged+=(_,_)=>Update(false);
        RegisterPropertyChangedCallback(TemplateProperty,(_,_)=>{if(Template is null)DetachTemplate();});
        RegisterPropertyChangedCallback(IsEnabledProperty,(_,_)=>Update());
    }
    /// <summary>The native editor, retained across outer template replacement. Selection, clipboard and text events use its normal TextBox APIs.</summary>
    public BleeckerInput Editor { get; }
    public static readonly DependencyProperty TextProperty=DependencyProperty.Register(nameof(Text),typeof(string),typeof(BleeckerSearchInput),new PropertyMetadata("",Changed));
    public static readonly DependencyProperty PlaceholderTextProperty=DependencyProperty.Register(nameof(PlaceholderText),typeof(string),typeof(BleeckerSearchInput),new PropertyMetadata("",Changed));
    public static readonly DependencyProperty InputSizeProperty=DependencyProperty.Register(nameof(InputSize),typeof(BleeckerInputSize),typeof(BleeckerSearchInput),new PropertyMetadata(BleeckerInputSize.Medium,Changed));
    public static readonly DependencyProperty IsErrorProperty=DependencyProperty.Register(nameof(IsError),typeof(bool),typeof(BleeckerSearchInput),new PropertyMetadata(false,Changed));
    public static readonly DependencyProperty IsReadOnlyProperty=DependencyProperty.Register(nameof(IsReadOnly),typeof(bool),typeof(BleeckerSearchInput),new PropertyMetadata(false,Changed));
    public static readonly DependencyProperty IsEditorEnabledProperty=DependencyProperty.Register(nameof(IsEditorEnabled),typeof(bool),typeof(BleeckerSearchInput),new PropertyMetadata(true,Changed));
    public static readonly DependencyProperty MaxLengthProperty=DependencyProperty.Register(nameof(MaxLength),typeof(int),typeof(BleeckerSearchInput),new PropertyMetadata(0,Changed));
    public static readonly DependencyProperty ClearLabelProperty=DependencyProperty.Register(nameof(ClearLabel),typeof(string),typeof(BleeckerSearchInput),new PropertyMetadata("Clear search",Changed));
    public static readonly DependencyProperty ClearCommandProperty=DependencyProperty.Register(nameof(ClearCommand),typeof(ICommand),typeof(BleeckerSearchInput),new PropertyMetadata(null,Changed));
    public static readonly DependencyProperty ClearCommandParameterProperty=DependencyProperty.Register(nameof(ClearCommandParameter),typeof(object),typeof(BleeckerSearchInput),new PropertyMetadata(null,Changed));
    public string Text {get=>(string)GetValue(TextProperty);set=>SetValue(TextProperty,value??throw new ArgumentNullException(nameof(value)));}
    public string PlaceholderText {get=>(string)GetValue(PlaceholderTextProperty);set=>SetValue(PlaceholderTextProperty,value??throw new ArgumentNullException(nameof(value)));}
    public BleeckerInputSize InputSize {get=>(BleeckerInputSize)GetValue(InputSizeProperty);set=>SetValue(InputSizeProperty,value);}
    public bool IsError {get=>(bool)GetValue(IsErrorProperty);set=>SetValue(IsErrorProperty,value);}
    public bool IsReadOnly {get=>(bool)GetValue(IsReadOnlyProperty);set=>SetValue(IsReadOnlyProperty,value);}
    public bool IsEditorEnabled {get=>(bool)GetValue(IsEditorEnabledProperty);set=>SetValue(IsEditorEnabledProperty,value);}
    public int MaxLength {get=>(int)GetValue(MaxLengthProperty);set=>SetValue(MaxLengthProperty,value);}
    public string ClearLabel {get=>(string)GetValue(ClearLabelProperty);set=>SetValue(ClearLabelProperty,value??throw new ArgumentNullException(nameof(value)));}
    public ICommand? ClearCommand {get=>(ICommand?)GetValue(ClearCommandProperty);set=>SetValue(ClearCommandProperty,value);}
    public object? ClearCommandParameter {get=>GetValue(ClearCommandParameterProperty);set=>SetValue(ClearCommandParameterProperty,value);}
    public event EventHandler? ClearRequested {add{clearRequested+=value;Update();}remove{clearRequested-=value;Update();}}
    private bool HasClear=>Text.Length>0&&(ClearCommand is not null||clearRequested is not null);
    private bool CanClear=>IsLoaded&&IsEnabled&&HasClear&&(ClearCommand?.CanExecute(ClearCommandParameter)??true);
    private void Bind(FrameworkElement target,DependencyProperty property,string path,BindingMode mode=BindingMode.OneWay)=>target.SetBinding(property,new Binding{Source=this,Path=new PropertyPath(path),Mode=mode,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});
    private static void Changed(DependencyObject target,DependencyPropertyChangedEventArgs args)
    {
        var owner=(BleeckerSearchInput)target;
        if(args.Property==TextProperty&&args.NewValue is not string)throw new ArgumentException("Search text must be a string.");
        if(args.Property==ClearCommandProperty&&owner.IsLoaded)owner.Observe();
        owner.Update();
    }
    protected override void OnApplyTemplate()
    {
        DetachTemplate();base.OnApplyTemplate();if(Template is null)return;
        host=GetTemplateChild("PART_Content")as Grid??throw new InvalidOperationException("SearchInput requires PART_Content.");
        foreach(var key in new[]{"Secondary","Hover","HoverBackground"})palette[key]=GetTemplateChild("PART_"+key)as TextBlock??throw new InvalidOperationException("SearchInput requires PART_"+key+".");
        Editor.StartIcon=CreateSearchIcon();host.Children.Add(Editor);
        clear=new BleeckerFilterAction(this,true,InvokeClear,()=>CanClear&&host is not null,()=>palette["Secondary"].Foreground,()=>palette["Hover"].Foreground,()=>palette["HoverBackground"].Foreground,iconSize:14,pressedScale:.92)
        {Name="ClearSearch",HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,10,0),Width=18,Height=18};
        host.Children.Add(clear);if(IsLoaded)Observe();Update(false);
    }
    private Canvas CreateSearchIcon()
    {
        var icon=new Canvas{Width=16,Height=16};
        var circle=new Ellipse{Width=32d/3,Height=32d/3,StrokeThickness=4d/3};Canvas.SetLeft(circle,2);Canvas.SetTop(circle,2);
        var stem=new Line{X1=14,Y1=14,X2=11.1,Y2=11.1,StrokeThickness=4d/3,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round};
        foreach(var shape in new Shape[]{circle,stem}){shape.SetBinding(Shape.StrokeProperty,new Binding{Source=palette["Secondary"],Path=new PropertyPath("Foreground")});icon.Children.Add(shape);}
        return icon;
    }
    private void DetachTemplate(){StopObserving();clear?.Detach();clear=null;host?.Children.Clear();host=null;palette.Clear();}
    private void ApplyPadding()
    {
        if(applyingPadding)return;applyingPadding=true;
        try{Editor.Padding=new Thickness(40,0,32,0);}finally{applyingPadding=false;}
    }
    private void Update(bool animate=true)
    {
        ApplyPadding();AutomationProperties.SetName(Editor,AutomationProperties.GetName(this));AutomationProperties.SetLabeledBy(Editor,AutomationProperties.GetLabeledBy(this));if(clear is null)return;
        clear.Visibility=HasClear?Visibility.Visible:Visibility.Collapsed;clear.TabIndex=TabIndex;
        AutomationProperties.SetName(clear,ClearLabel);clear.Update(animate);
    }
    private void Observe()
    {
        StopObserving();observedCommand=ClearCommand;if(observedCommand is not null)observedCommand.CanExecuteChanged+=CommandChanged;
        foreach(var item in palette.Values)
        {
            observers.Add((item,TextBlock.ForegroundProperty,item.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty,(_,_)=>{Observe();Update(false);})));
            if(item.Foreground is SolidColorBrush brush)foreach(var property in new[]{SolidColorBrush.ColorProperty,Brush.OpacityProperty})observers.Add((brush,property,brush.RegisterPropertyChangedCallback(property,(_,_)=>Update(false))));
        }
        clear?.Attach();
    }
    private void StopObserving(){if(observedCommand is not null)observedCommand.CanExecuteChanged-=CommandChanged;observedCommand=null;foreach(var item in observers)item.Owner.UnregisterPropertyChangedCallback(item.Property,item.Token);observers.Clear();}
    private void CommandChanged(object? sender,EventArgs args)=>DispatcherQueue.TryEnqueue(()=>Update());
    private void InvokeClear(){if(!CanClear)return;var command=ClearCommand;var callback=clearRequested;command?.Execute(ClearCommandParameter);callback?.Invoke(this,EventArgs.Empty);}
    private void EditorKeyDown(object sender,KeyRoutedEventArgs args)
    {
        if(args.Key!=VirtualKey.Escape||!Editor.IsEnabled||Editor.IsReadOnly||Editor.Text.Length==0)return;
        Editor.Text="";args.Handled=true;
    }
}
