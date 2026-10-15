using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>Consumer-owned links and keyed expandable groups with independent navigation scrolling.</summary>
[ContentProperty(Name = nameof(Items))]
public sealed partial class BleeckerSidebar : Control
{
    private Grid? host;
    private SidebarLayout? layout;
    private readonly List<Entry> entries = [];
    private readonly HashSet<BleeckerSidebarItem> observedItems = [];
    private readonly HashSet<ObservableCollection<BleeckerSidebarItem>> observedCollections = [];
    private readonly HashSet<ICommand> observedCommands = [];
    private readonly ResourceDictionary actions = new() { Source = new Uri("ms-appx:///Orchard/Themes/SidebarActions.xaml") };
    private readonly UISettings settings = new();
    private readonly AccessibilitySettings accessibility = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly BleeckerCssTransition<double> widthMotion = new((a,b,p) => a+(b-a)*p, BleeckerCssEasing.Default);
    private readonly BleeckerCssTransition<Thickness> paddingMotion = new((a,b,p) => new(a.Left+(b.Left-a.Left)*p,a.Top+(b.Top-a.Top)*p,a.Right+(b.Right-a.Right)*p,a.Bottom+(b.Bottom-a.Bottom)*p), BleeckerCssEasing.Default);
    private double preferredWidth = 256;
    private bool rendering;
    private DataTemplate? renderedLinkTemplate;

    public BleeckerSidebar()
    {
        DefaultStyleKey = typeof(BleeckerSidebar);
        Items = new ObservableCollection<BleeckerSidebarItem>();
        AutomationProperties.SetName(this, "Sidebar navigation");
        Loaded += (_,_) =>
        {
            settings.ColorValuesChanged += ColorsChanged;
            if (OperatingSystem.IsWindowsVersionAtLeast(10,0,19041)) settings.AnimationsEnabledChanged += MotionChanged;
            Observe(); Render(); RetargetLayout(false);
        };
        Unloaded += (_,_) =>
        {
            settings.ColorValuesChanged -= ColorsChanged;
            if (OperatingSystem.IsWindowsVersionAtLeast(10,0,19041)) settings.AnimationsEnabledChanged -= MotionChanged;
            StopObserving(); StopRendering();
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => { if (IsLoaded) { Observe(); Render(); } });
        };
        RegisterPropertyChangedCallback(PaddingProperty, (_,_) => RetargetLayout(false));
        RegisterPropertyChangedCallback(WidthProperty, (_,_) => RetargetLayout(false));
        RegisterPropertyChangedCallback(FontFamilyProperty, (_,_) => Render());
        RegisterPropertyChangedCallback(FontSizeProperty, (_,_) => Render());
        RegisterPropertyChangedCallback(FontWeightProperty, (_,_) => Render());
        RegisterPropertyChangedCallback(CharacterSpacingProperty, (_,_) => Render());
        RegisterPropertyChangedCallback(IsEnabledProperty, (_,_) => { foreach(var entry in entries) entry.Notify(); });
        RegisterPropertyChangedCallback(TemplateProperty, (_,_) => { if (Template is null) DetachTemplate(); });
    }
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(nameof(Items),typeof(object),typeof(BleeckerSidebar),new PropertyMetadata(null,Changed));
    public ObservableCollection<BleeckerSidebarItem> Items { get => GetValue(ItemsProperty) as ObservableCollection<BleeckerSidebarItem> ?? throw new InvalidOperationException("Sidebar Items must resolve before mounting."); set => SetValue(ItemsProperty,value ?? throw new ArgumentNullException(nameof(value))); }
    public static readonly DependencyProperty CollapsedProperty = DependencyProperty.Register(nameof(Collapsed),typeof(bool),typeof(BleeckerSidebar),new PropertyMetadata(false,Changed));
    public bool Collapsed { get => (bool)GetValue(CollapsedProperty); set => SetValue(CollapsedProperty,value); }
    public static readonly DependencyProperty CollapsedPaddingProperty = DependencyProperty.Register(nameof(CollapsedPadding),typeof(Thickness),typeof(BleeckerSidebar),new PropertyMetadata(new Thickness(8,20,8,20),Changed));
    public Thickness CollapsedPadding { get => (Thickness)GetValue(CollapsedPaddingProperty); set => SetValue(CollapsedPaddingProperty,value); }
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(nameof(Header),typeof(object),typeof(BleeckerSidebar),new PropertyMetadata(null,Changed));
    public object? Header { get => GetValue(HeaderProperty); set => SetValue(HeaderProperty,value); }
    public static readonly DependencyProperty FooterProperty = DependencyProperty.Register(nameof(Footer),typeof(object),typeof(BleeckerSidebar),new PropertyMetadata(null,Changed));
    public object? Footer { get => GetValue(FooterProperty); set => SetValue(FooterProperty,value); }
    public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register(nameof(HeaderTemplate),typeof(DataTemplate),typeof(BleeckerSidebar),new PropertyMetadata(null,Changed));
    public DataTemplate? HeaderTemplate { get => (DataTemplate?)GetValue(HeaderTemplateProperty); set => SetValue(HeaderTemplateProperty,value); }
    public static readonly DependencyProperty FooterTemplateProperty = DependencyProperty.Register(nameof(FooterTemplate),typeof(DataTemplate),typeof(BleeckerSidebar),new PropertyMetadata(null,Changed));
    public DataTemplate? FooterTemplate { get => (DataTemplate?)GetValue(FooterTemplateProperty); set => SetValue(FooterTemplateProperty,value); }
    public static readonly DependencyProperty LinkTemplateProperty = DependencyProperty.Register(nameof(LinkTemplate),typeof(DataTemplate),typeof(BleeckerSidebar),new PropertyMetadata(null,Changed));
    public DataTemplate? LinkTemplate { get => (DataTemplate?)GetValue(LinkTemplateProperty); set => SetValue(LinkTemplateProperty,value); }
    public event EventHandler<BleeckerSidebarItem>? ItemInvoked;
    internal void Invoke(BleeckerSidebarItem item) { item.Command?.Execute(item.CommandParameter); ItemInvoked?.Invoke(this,item); }
    private static void Changed(DependencyObject owner,DependencyPropertyChangedEventArgs args)
    {
        var control=(BleeckerSidebar)owner;
        if(args.Property==ItemsProperty && args.NewValue is not ObservableCollection<BleeckerSidebarItem> && !(args.NewValue is null && !control.IsLoaded && control.GetBindingExpression(ItemsProperty) is not null))throw new ArgumentException("Sidebar Items requires a non-null ObservableCollection<BleeckerSidebarItem>.");
        if(control.IsLoaded)control.Observe();
        control.Render(); control.RetargetLayout(args.Property==CollapsedProperty);
    }
    protected override void OnApplyTemplate()
    {
        DetachTemplate(); base.OnApplyTemplate(); if(Template is null)return;
        host=GetTemplateChild("PART_Items") as Grid ?? throw new InvalidOperationException("Sidebar requires PART_Items.");
        layout=new SidebarLayout(this); host.Children.Add(layout);
        Render(); RetargetLayout(false);
    }
    private void DetachTemplate()
    {
        StopRendering(); foreach(var entry in entries)entry.DisposeVisual();
        layout?.Release(); host?.Children.Clear(); layout=null;host=null;
    }
    private void Observe()
    {
        StopObserving(); if(GetValue(ItemsProperty) is not ObservableCollection<BleeckerSidebarItem> items)return;
        void Collection(ObservableCollection<BleeckerSidebarItem> collection)
        { if(observedCollections.Add(collection))collection.CollectionChanged+=CollectionChanged; }
        void Item(BleeckerSidebarItem item)
        {
            if(item is null || item.Id is null || item.Label is null)throw new ArgumentException("Sidebar items require non-null id and label.");
            if(observedItems.Add(item))item.Changed+=ItemChanged;
            if(item.Command is ICommand command && observedCommands.Add(command))command.CanExecuteChanged+=CommandChanged;
        }
        Collection(items);
        foreach(var item in items){Item(item);Collection(item.Items);foreach(var child in item.Items)Item(child);}
    }
    private void StopObserving()
    {
        foreach(var item in observedItems)item.Changed-=ItemChanged;
        foreach(var collection in observedCollections)collection.CollectionChanged-=CollectionChanged;
        foreach(var command in observedCommands)command.CanExecuteChanged-=CommandChanged;
        observedItems.Clear();observedCollections.Clear();observedCommands.Clear();
    }
    private void CollectionChanged(object? sender,NotifyCollectionChangedEventArgs args){Observe();Render();}
    private void ItemChanged(object? sender,EventArgs args){Observe();Render();}
    private void CommandChanged(object? sender,EventArgs args)
    { void Apply(){foreach(var entry in entries)entry.Notify();} if(DispatcherQueue.HasThreadAccess)Apply();else DispatcherQueue.TryEnqueue(Apply); }
    private void Render()
    {
        if(layout is null)return;
        if(GetValue(ItemsProperty) is not ObservableCollection<BleeckerSidebarItem> items){if(IsLoaded)_=Items;return;}
        if(!ReferenceEquals(renderedLinkTemplate,LinkTemplate)){foreach(var entry in entries)entry.ReplaceLinkTemplate();renderedLinkTemplate=LinkTemplate;}
        var remaining=RetainEntries(entries,items.Select(item=>item.Id));var next=new List<Entry>();var visible=new List<UIElement>();
        foreach(var item in items)
        {
            if(item is null || item.Id is null || item.Label is null)throw new ArgumentException("Sidebar items require non-null id and label.");
            var found=remaining.FindIndex(e=>e.Key==item.Id);
            var entry=found>=0?remaining[found]:new Entry(this,item.Id,item.Items.Any(child=>child.Active));
            if(found>=0)remaining.RemoveAt(found);
            entry.Update(item);next.Add(entry);if(entry.Visible)visible.Add(entry.Root);
        }
        foreach(var entry in remaining)entry.DisposeVisual();
        entries.Clear();entries.AddRange(next);Reconcile(layout.NavigationItems,visible);
        layout.UpdateSlots();layout.InvalidateMeasure();
    }
    internal static void Reconcile(Panel panel,IReadOnlyList<UIElement> next)
    {
        for(var index=panel.Children.Count-1;index>=0;index--)if(!next.Contains(panel.Children[index]))panel.Children.RemoveAt(index);
        for(var index=0;index<next.Count;index++)
        {var child=next[index];if(index<panel.Children.Count&&ReferenceEquals(panel.Children[index],child))continue;if(panel.Children.Contains(child))panel.Children.Remove(child);panel.Children.Insert(index,child);}
    }
    private static List<Entry> RetainEntries(IEnumerable<Entry> previous,IEnumerable<string> keys)
    {
        var wanted=keys.ToList();var retained=new List<Entry>();
        foreach(var entry in previous){var at=wanted.IndexOf(entry.Key);if(at<0)entry.DisposeVisual();else{wanted.RemoveAt(at);retained.Add(entry);}}
        return retained;
    }
    private void RetargetLayout(bool animate)
    {
        var now=clock.Elapsed.TotalMilliseconds;var enabled=animate&&IsLoaded&&settings.AnimationsEnabled&&!accessibility.HighContrast;
        widthMotion.Retarget(double.IsNaN(Width)?Collapsed?64:256:Width,now,200,enabled);
        paddingMotion.Retarget(Collapsed?CollapsedPadding:Padding,now,200,enabled);
        ApplyLayoutFrame(now); if((widthMotion.IsRunning(now)||paddingMotion.IsRunning(now))&&!rendering){rendering=true;CompositionTarget.Rendering+=LayoutFrame;}
    }
    private void LayoutFrame(object? sender,object args)
    {var now=clock.Elapsed.TotalMilliseconds;ApplyLayoutFrame(now);if(!widthMotion.IsRunning(now)&&!paddingMotion.IsRunning(now))StopRendering();}
    private void ApplyLayoutFrame(double time){preferredWidth=widthMotion.ValueAt(time);if(layout is not null){layout.Insets=paddingMotion.ValueAt(time);layout.InvalidateMeasure();}InvalidateMeasure();}
    private void StopRendering(){if(rendering)CompositionTarget.Rendering-=LayoutFrame;rendering=false;}
    private void ColorsChanged(UISettings sender,object args)=>DispatcherQueue.TryEnqueue(()=>RetargetLayout(false));
    private void MotionChanged(UISettings sender,UISettingsAnimationsEnabledChangedEventArgs args)=>DispatcherQueue.TryEnqueue(()=>RetargetLayout(false));
    protected override Size MeasureOverride(Size availableSize)
    {var width=Math.Min(availableSize.Width,double.IsNaN(Width)?preferredWidth:Width);var measured=base.MeasureOverride(new Size(Math.Max(0,width),availableSize.Height));return new Size(Math.Max(0,width),double.IsFinite(availableSize.Height)?availableSize.Height:measured.Height);}
    protected override AutomationPeer OnCreateAutomationPeer()=>new SidebarPeer(this);
    private sealed class SidebarPeer(BleeckerSidebar owner):FrameworkElementAutomationPeer(owner)
    {protected override string GetClassNameCore()=>nameof(BleeckerSidebar);protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Group;}
}
