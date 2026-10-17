using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Orchard;

/// <summary>A consumer-controlled, single-selection group of value-keyed radio options.</summary>
public sealed partial class BleeckerRadioGroup : Control
{
    private readonly Dictionary<string, OptionButton> buttons = new(StringComparer.Ordinal);
    private readonly ResourceDictionary palette = new() { Source = new Uri("ms-appx:///Orchard/Themes/RadioGroup.xaml") };
    private BleeckerRadioPanel? panel;
    private ObservableCollection<BleeckerRadioOption>? observed;
    private int revision;
    public BleeckerRadioGroup()
    {
        DefaultStyleKey = typeof(BleeckerRadioGroup); IsTabStop = false; TabFocusNavigation=KeyboardNavigationMode.Once;
        Options = [];
        Loaded += (_, _) => { Observe(); Reconcile(); };
        Unloaded += (_, _) => { if (!IsLoaded) StopObserving(); };
        GettingFocus+=(_,args)=>
        {
            if(args.FocusState!=FocusState.Keyboard || args.Direction is not (FocusNavigationDirection.Next or FocusNavigationDirection.Previous) || args.NewFocusedElement is not OptionButton target || !ReferenceEquals(target.Group,this))return;
            if(args.OldFocusedElement is OptionButton previous && ReferenceEquals(previous.Group,this))return;
            if(Entry() is {} entry && !ReferenceEquals(entry,target))args.TrySetNewFocusedElement(entry);
        };
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => Synchronize());
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
    }
    public static readonly DependencyProperty OptionsProperty = DependencyProperty.Register(nameof(Options), typeof(object), typeof(BleeckerRadioGroup), new PropertyMetadata(null, (owner, _) => { var group=(BleeckerRadioGroup)owner; group.Observe(); group.Reconcile(); }));
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(string), typeof(BleeckerRadioGroup), new PropertyMetadata(null, (owner, _) => ((BleeckerRadioGroup)owner).Synchronize()));
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(BleeckerRadioGroup), new PropertyMetadata(Orientation.Vertical, (owner, _) => ((BleeckerRadioGroup)owner).Reconcile()));
    public ObservableCollection<BleeckerRadioOption> Options { get => (ObservableCollection<BleeckerRadioOption>)GetValue(OptionsProperty); set { BleeckerRadioModel.Validate(value); SetValue(OptionsProperty,value); } }
    public string? Value { get => (string?)GetValue(ValueProperty); set => SetValue(ValueProperty,value); }
    public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(OrientationProperty,value); } }
    public event EventHandler<BleeckerRadioRequestedEventArgs>? ValueRequested;
    internal IReadOnlyList<Button> OptionButtons => Options.Select(option => (Button)buttons[option.Value]).ToArray();
    protected override void OnApplyTemplate()
    {
        Detach(); base.OnApplyTemplate();
        var root=GetTemplateChild("PART_Root") as Grid ?? throw new InvalidOperationException("RadioGroup requires PART_Root.");
        panel=new BleeckerRadioPanel { UseLayoutRounding=false }; root.Children.Add(panel); Reconcile();
    }
    private void Detach() { revision++; panel?.Children.Clear(); panel=null; buttons.Clear(); }
    private void StopObserving() { if (observed is not null) observed.CollectionChanged-=Changed; observed=null; }
    private void Observe()
    {
        StopObserving(); BleeckerRadioModel.Validate(Options);
        if (IsLoaded) { observed=Options; observed.CollectionChanged+=Changed; }
    }
    private void Changed(object? sender, NotifyCollectionChangedEventArgs args) => Reconcile();
    private void Reconcile()
    {
        BleeckerRadioModel.Validate(Options);
        if (!Enum.IsDefined(Orientation)) throw new ArgumentException("Unknown radio orientation.");
        if (panel is null) return;
        var focused=XamlRoot is null?null:FocusManager.GetFocusedElement(XamlRoot) as OptionButton;
        var restore=focused is not null && ReferenceEquals(focused.Group,this) && buttons.Values.Contains(focused);
        var focusState=focused?.FocusState??FocusState.Unfocused;
        var current=++revision;
        var wanted=Options.Select(option=>option.Value).ToHashSet(StringComparer.Ordinal);
        foreach (var old in buttons.Keys.Where(key=>!wanted.Contains(key)).ToArray()) { panel.Children.Remove(buttons[old]); buttons.Remove(old); }
        panel.Orientation=Orientation;
        for (var index=0;index<Options.Count;index++)
        {
            var option=Options[index];
            if (!buttons.TryGetValue(option.Value,out var button)) { button=new OptionButton(this,option) { Style=(Style)palette["BleeckerRadioOptionStyle"] }; buttons.Add(option.Value,button); }
            button.Update(option); AutomationProperties.SetPositionInSet(button,index+1); AutomationProperties.SetSizeOfSet(button,Options.Count);
            if (panel.Children.IndexOf(button)!=index) { panel.Children.Remove(button); panel.Children.Insert(index,button); }
        }
        panel.InvalidateMeasure(); Synchronize();
        if (restore) DispatcherQueue.TryEnqueue(()=>
        {
            if (current!=revision || !IsLoaded) return;
            var target=focused is not null && buttons.TryGetValue(focused.Option.Value,out var retained) && retained.IsEnabled?retained:Entry();
            target?.Focus(focusState==FocusState.Unfocused?FocusState.Keyboard:focusState);
        });
    }
    private OptionButton? Entry() => !IsEnabled?null:Options.Where(option=>!option.Disabled).Select(option=>buttons.GetValueOrDefault(option.Value)).FirstOrDefault(button=>button?.Option.Value==Value)
        ?? Options.Where(option=>!option.Disabled).Select(option=>buttons.GetValueOrDefault(option.Value)).FirstOrDefault(button=>button is not null);
    private void Synchronize()
    {
        foreach (var button in buttons.Values) { button.IsEnabled=IsEnabled&&!button.Option.Disabled; button.IsTabStop=button.IsEnabled; button.Select(Value==button.Option.Value); }
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private bool Owns(OptionButton button) => IsLoaded && button.IsLoaded && buttons.TryGetValue(button.Option.Value,out var current) && ReferenceEquals(current,button);
    private void Request(OptionButton button)
    {
        if (!Owns(button) || !IsEnabled || !button.IsEnabled || Value==button.Option.Value) return;
        ValueRequested?.Invoke(this,new(button.Option.Value));
    }
    private void Navigate(object sender,KeyRoutedEventArgs args)
    {
        if (FocusManager.GetFocusedElement(XamlRoot) is not OptionButton focused || !Owns(focused)) return;
        if (args.Key is not (VirtualKey.Left or VirtualKey.Right or VirtualKey.Up or VirtualKey.Down or VirtualKey.Home or VirtualKey.End)) return;
        var enabled=Options.Where(option=>!option.Disabled).Select(option=>buttons[option.Value]).ToArray();
        var index=Array.IndexOf(enabled,focused); if (index<0 || enabled.Length==0) return;
        var direction=args.Key is VirtualKey.Left or VirtualKey.Up?-1:1;
        if (FlowDirection==FlowDirection.RightToLeft && args.Key is VirtualKey.Left or VirtualKey.Right) direction=-direction;
        var next=args.Key==VirtualKey.Home?0:args.Key==VirtualKey.End?enabled.Length-1:Math.Clamp(index+direction,0,enabled.Length-1);
        args.Handled=true; var target=enabled[next]; if(!target.Focus(FocusState.Keyboard))throw new InvalidOperationException("The enabled radio navigation target refused keyboard focus.");
        if (!Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down)) Request(target);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new GroupPeer(this);
    private sealed class GroupPeer(BleeckerRadioGroup group):FrameworkElementAutomationPeer(group),ISelectionProvider
    {
        public bool CanSelectMultiple=>false;
        public bool IsSelectionRequired=>false;
        public IRawElementProviderSimple[] GetSelection()=>group.buttons.Values.Where(button=>group.Value==button.Option.Value).Select(button=>ProviderFromPeer(CreatePeerForElement(button))).ToArray();
        protected override string GetClassNameCore()=>nameof(BleeckerRadioGroup);
        protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Group;
        protected override object GetPatternCore(PatternInterface pattern)=>pattern==PatternInterface.Selection?this:base.GetPatternCore(pattern);
    }
    private sealed partial class OptionButton:BleeckerRadioGroupOptionBase
    {
        internal BleeckerRadioGroup Group { get; }
        internal BleeckerRadioOption Option { get; private set; }
        internal OptionButton(BleeckerRadioGroup group,BleeckerRadioOption option) { Group=group; Option=option; Click+=(_,_)=>Group.Request(this); }
        internal void Update(BleeckerRadioOption option) { Option=option; SetText(option.Label,option.Description); AutomationProperties.SetName(this,option.Label); AutomationProperties.SetHelpText(this,option.Description??""); }
        protected override void OnKeyDown(KeyRoutedEventArgs args) { Group.Navigate(this,args);if(!args.Handled)base.OnKeyDown(args); }
        protected override AutomationPeer OnCreateAutomationPeer()=>new OptionPeer(this);
        private sealed class OptionPeer(OptionButton button):ButtonAutomationPeer(button),ISelectionItemProvider
        {
            public bool IsSelected=>button.Group.Value==button.Option.Value;
            public IRawElementProviderSimple SelectionContainer=>ProviderFromPeer(CreatePeerForElement(button.Group));
            private void Guard() { if (!button.Group.Owns(button)) throw new ElementNotAvailableException(); if (!button.IsEnabled || !button.Group.IsEnabled) throw new ElementNotEnabledException(); }
            public void Select() { Guard(); button.Group.Request(button); }
            public void AddToSelection() { Guard(); if (button.Group.Value is not null && !IsSelected) throw new InvalidOperationException("Radio groups cannot add a second selection."); Select(); }
            public void RemoveFromSelection() { Guard(); if (IsSelected) throw new InvalidOperationException("A radio item cannot clear the group's selection."); }
            protected override string GetClassNameCore()=>"BleeckerRadioOption";
            protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.RadioButton;
            protected override object GetPatternCore(PatternInterface pattern)=>pattern==PatternInterface.SelectionItem?this:pattern==PatternInterface.Invoke?null!:base.GetPatternCore(pattern);
        }
    }
}
