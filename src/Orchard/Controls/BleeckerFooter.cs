using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A viewport-responsive footer with caller-owned sections, links and bottom content.</summary>
public sealed partial class BleeckerFooter : Control
{
    private Grid? host;
    private FooterPanel? layout;
    private readonly List<SectionSlot> sections = [];
    private readonly List<FooterLink> links = [];
    private readonly HashSet<INotifyCollectionChanged> collections = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<BleeckerNavItem> items = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<BleeckerFooterSection> models = new(ReferenceEqualityComparer.Instance);
    private BleeckerFooterBrand? observedBrand;
    private ICommand? observedCommand;
    private XamlRoot? observedRoot;
    private readonly ResourceDictionary actions = new() { Source = new Uri("ms-appx:///Orchard/Themes/FooterLinks.xaml") };
    private readonly UISettings settings = new();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue uiQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException("Footer requires a UI dispatcher.");
    private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
    private bool rendering, motionSubscribed;
    private long focusVersion;
    public BleeckerFooter()
    {
        DefaultStyleKey = typeof(BleeckerFooter); Sections = new ObservableCollection<BleeckerFooterSection>();
        Loaded += (_, _) => { Subscribe(); Render(); };
        Unloaded += (_, _) => { Disconnect(); StopMotion(); foreach (var link in links) link.Suspend(); };
        ActualThemeChanged += (_, _) => { foreach (var link in links) link.Paint(false); };
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
        foreach (var property in new[] { FlowDirectionProperty, LanguageProperty, IsEnabledProperty, IsTextScaleFactorEnabledProperty }) RegisterPropertyChangedCallback(property, (_, _) => Render());
        GotFocus += (_, _) => focusVersion++; LostFocus += (_, _) => focusVersion++;
    }
    private static DependencyProperty Property(string name, Type type, object? value) => DependencyProperty.Register(name, type, typeof(BleeckerFooter), new PropertyMetadata(value, (owner, _) => { var footer = (BleeckerFooter)owner; if (footer.IsLoaded) footer.Subscribe(); footer.Render(); }));
    public static readonly DependencyProperty BrandProperty = Property(nameof(Brand), typeof(BleeckerFooterBrand), null);
    public static readonly DependencyProperty SectionsProperty = Property(nameof(Sections), typeof(object), null);
    public static readonly DependencyProperty BottomLeftProperty = Property(nameof(BottomLeft), typeof(object), null);
    public static readonly DependencyProperty BottomRightProperty = Property(nameof(BottomRight), typeof(object), null);
    public static readonly DependencyProperty BottomLeftTemplateProperty = Property(nameof(BottomLeftTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty BottomRightTemplateProperty = Property(nameof(BottomRightTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty BottomLeftTemplateSelectorProperty = Property(nameof(BottomLeftTemplateSelector), typeof(DataTemplateSelector), null);
    public static readonly DependencyProperty BottomRightTemplateSelectorProperty = Property(nameof(BottomRightTemplateSelector), typeof(DataTemplateSelector), null);
    public static readonly DependencyProperty LinkTemplateProperty = Property(nameof(LinkTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty LinkStyleProperty = Property(nameof(LinkStyle), typeof(Style), null);
    public static readonly DependencyProperty ShowBottomAccentProperty = Property(nameof(ShowBottomAccent), typeof(bool), true);
    public static readonly DependencyProperty CommandProperty = Property(nameof(Command), typeof(ICommand), null);
    public static readonly DependencyProperty CommandParameterProperty = Property(nameof(CommandParameter), typeof(object), null);
    public BleeckerFooterBrand Brand { get => (BleeckerFooterBrand?)GetValue(BrandProperty) ?? throw new InvalidOperationException("Footer Brand must resolve before mounting."); set => SetValue(BrandProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public IEnumerable<BleeckerFooterSection> Sections { get => GetValue(SectionsProperty) as IEnumerable<BleeckerFooterSection> ?? throw new InvalidOperationException("Footer Sections must resolve before mounting."); set => SetValue(SectionsProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public object? BottomLeft { get => GetValue(BottomLeftProperty); set => SetValue(BottomLeftProperty, value); }
    public object? BottomRight { get => GetValue(BottomRightProperty); set => SetValue(BottomRightProperty, value); }
    public DataTemplate? BottomLeftTemplate { get => (DataTemplate?)GetValue(BottomLeftTemplateProperty); set => SetValue(BottomLeftTemplateProperty, value); }
    public DataTemplate? BottomRightTemplate { get => (DataTemplate?)GetValue(BottomRightTemplateProperty); set => SetValue(BottomRightTemplateProperty, value); }
    public DataTemplateSelector? BottomLeftTemplateSelector { get => (DataTemplateSelector?)GetValue(BottomLeftTemplateSelectorProperty); set => SetValue(BottomLeftTemplateSelectorProperty, value); }
    public DataTemplateSelector? BottomRightTemplateSelector { get => (DataTemplateSelector?)GetValue(BottomRightTemplateSelectorProperty); set => SetValue(BottomRightTemplateSelectorProperty, value); }
    public DataTemplate? LinkTemplate { get => (DataTemplate?)GetValue(LinkTemplateProperty); set => SetValue(LinkTemplateProperty, value); }
    public Style? LinkStyle { get => (Style?)GetValue(LinkStyleProperty); set => SetValue(LinkStyleProperty, value); }
    public bool ShowBottomAccent { get => (bool)GetValue(ShowBottomAccentProperty); set => SetValue(ShowBottomAccentProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
    public event EventHandler<BleeckerNavItem>? ItemInvoked;
    protected override void OnApplyTemplate()
    {
        Detach(); base.OnApplyTemplate(); if (Template is null) return;
        host = GetTemplateChild("PART_Footer") as Grid ?? throw new InvalidOperationException("Footer requires PART_Footer.");
        layout = new(this) { FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false }; host.Children.Add(layout); if (IsLoaded) Subscribe(); Render();
    }
    private void Detach() { foreach (var link in links.ToArray()) link.Retire(); links.Clear(); sections.Clear(); host?.Children.Clear(); layout = null; host = null; StopMotion(); }
    private bool CanInvoke(BleeckerNavLinkContext context)
    {
        if (!IsLoaded || !IsEnabled || !context.Mounted || context.Element?.IsLoaded != true || !links.Any(link => ReferenceEquals(link.Context, context)) || !(Command?.CanExecute(CommandParameter) ?? true)) return false;
        var mounted = false;
        for (DependencyObject? node = context.Element; node is not null; node = VisualTreeHelper.GetParent(node)) { if (node is FrameworkElement element && element.Visibility != Visibility.Visible) return false; mounted |= ReferenceEquals(node, host); }
        return mounted;
    }
    private void Invoke(BleeckerNavLinkContext context) { if (!CanInvoke(context)) return; var original = context.Item; Command?.Execute(CommandParameter); ItemInvoked?.Invoke(this, original); }
    private void Render()
    {
        if (rendering || layout is null) return;
        if (GetValue(BrandProperty) is not BleeckerFooterBrand) { if (IsLoaded) _ = Brand; return; }
        rendering = true;
        try
        {
            var values = Sections.ToArray(); if (values.Any(section => section is null || section.Title is null)) throw new ArgumentException("Footer requires original non-null sections and titles.");
            var focused = XamlRoot is null ? null : Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot) as Control; var focusState = focused?.FocusState ?? FocusState.Unfocused;
            layout.Sync(values); layout.InvalidateMeasure(); InvalidateMeasure(); foreach (var link in links) { link.Context.Notify(); link.Paint(false); }
            if (focused is not null && focusState != FocusState.Unfocused && Descends(focused, layout) && !ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), focused))
            {
                var displaced = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot); var version = focusVersion;
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => { if (IsLoaded && focused.IsLoaded && layout is not null && Descends(focused, layout) && focusVersion == version && ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), displaced)) focused.Focus(focusState); });
            }
        }
        finally { rendering = false; }
    }
    private static bool Descends(DependencyObject child, DependencyObject ancestor) { for (DependencyObject? node = child; node is not null; node = VisualTreeHelper.GetParent(node)) if (ReferenceEquals(node, ancestor)) return true; return false; }
    private void Subscribe()
    {
        Disconnect(); observedBrand = GetValue(BrandProperty) as BleeckerFooterBrand; if (observedBrand is not null) observedBrand.Changed += ModelChanged;
        void Collection(object value) { if (value is INotifyCollectionChanged collection && collections.Add(collection)) collection.CollectionChanged += CollectionChanged; }
        Collection(Sections);
        foreach (var section in Sections) { if (section is null) throw new ArgumentException("Footer sections cannot be null."); if (models.Add(section)) section.Changed += ModelChanged; Collection(section.Items); foreach (var item in section.Items) { if (item is null) throw new ArgumentException("Footer items cannot be null."); if (items.Add(item)) item.Changed += ModelChanged; } }
        observedCommand = Command; if (observedCommand is not null) observedCommand.CanExecuteChanged += CommandChanged;
        observedRoot = XamlRoot; if (observedRoot is not null) observedRoot.Changed += RootChanged;
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged += MotionChanged;
    }
    private void Disconnect()
    {
        foreach (var collection in collections) collection.CollectionChanged -= CollectionChanged; collections.Clear();
        foreach (var section in models) section.Changed -= ModelChanged; models.Clear(); foreach (var item in items) item.Changed -= ModelChanged; items.Clear();
        if (observedBrand is not null) observedBrand.Changed -= ModelChanged; observedBrand = null;
        if (observedCommand is not null) observedCommand.CanExecuteChanged -= CommandChanged; observedCommand = null;
        if (observedRoot is not null) observedRoot.Changed -= RootChanged; observedRoot = null;
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= MotionChanged;
    }
    private void Dispatch(Action action)
    {
        if (uiQueue.HasThreadAccess) { if (IsLoaded) action(); }
        else if (!uiQueue.TryEnqueue(() => { if (IsLoaded) action(); })) throw new InvalidOperationException("Footer could not dispatch a producer update.");
    }
    private void ModelChanged(object? sender, EventArgs args) => Dispatch(() => { Subscribe(); Render(); });
    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) => Dispatch(() => { Subscribe(); Render(); });
    private void CommandChanged(object? sender, EventArgs args) => Dispatch(() => { foreach (var link in links) link.Context.Notify(); });
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) { layout?.InvalidateMeasure(); InvalidateMeasure(); }
    private void MotionChanged(UISettings sender, object args) => uiQueue.TryEnqueue(() => { if (IsLoaded) { foreach (var link in links) link.Paint(false); StopMotion(); } });
    private void EnsureMotion() { if (motionSubscribed || !IsLoaded) return; motionSubscribed = true; CompositionTarget.Rendering += MotionFrame; }
    private void StopMotion() { if (!motionSubscribed) return; motionSubscribed = false; CompositionTarget.Rendering -= MotionFrame; }
    private void MotionFrame(object? sender, object args) { foreach (var link in links) link.Present(); if (!links.Any(link => link.Running)) StopMotion(); }
    protected override AutomationPeer OnCreateAutomationPeer() => new FooterPeer(this);
    private sealed class FooterPeer(BleeckerFooter owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerFooter);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetNameCore() => string.IsNullOrEmpty(base.GetNameCore()) ? "Footer" : base.GetNameCore();
        protected override string GetLocalizedLandmarkTypeCore() => "content information";
    }
}
