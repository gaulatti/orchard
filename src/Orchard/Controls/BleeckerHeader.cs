using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A responsive, host-routed Header over original brand, navigation and action content.</summary>
public sealed partial class BleeckerHeader : Control
{
    private Grid? root;
    private HeaderSurface? surface;
    private BleeckerHeaderBrand? observedBrand;
    private INotifyCollectionChanged? observedNavigation;
    private readonly HashSet<BleeckerNavItem> observedItems = new(ReferenceEqualityComparer.Instance);
    private XamlRoot? observedRoot;
    private DependencyObject? scrollOwner;
    private FrameworkElement? scrollViewport;
    private readonly TranslateTransform shift = new();
    private readonly UISettings settings = new();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue uiQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException("Header requires a UI dispatcher.");
    private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
    private readonly BleeckerCssTransition<double> maximum = new((from, to, progress) => from + (to - from) * progress, BleeckerCssEasing.Default);
    private readonly BleeckerCssTransition<double> opacity = new((from, to, progress) => from + (to - from) * progress, BleeckerCssEasing.Default);
    private bool menuOpen, rendering, motionSubscribed, positioning;
    private double availableWidth;

    public BleeckerHeader()
    {
        maximum.Retarget(0, 0, 200, false); opacity.Retarget(0, 0, 200, false);
        DefaultStyleKey = typeof(BleeckerHeader); Navigation = new ObservableCollection<BleeckerNavItem>();
        Loaded += (_, _) => { Observe(); LayoutUpdated += LayoutChanged; if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged += MotionChanged; Render(); };
        Unloaded += (_, _) => { LayoutUpdated -= LayoutChanged; Disconnect(); StopMotion(); if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= MotionChanged; PresentMenu(false); };
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
        foreach (var property in new[] { FlowDirectionProperty, LanguageProperty, IsEnabledProperty, IsTextScaleFactorEnabledProperty, ForegroundProperty, FontFamilyProperty, FontSizeProperty }) RegisterPropertyChangedCallback(property, (_, _) => Render());
    }
    private static DependencyProperty Property(string name, Type type, object? value) => DependencyProperty.Register(name, type, typeof(BleeckerHeader), new PropertyMetadata(value, (owner, _) => { var control = (BleeckerHeader)owner; if (control.IsLoaded) control.Observe(); control.Render(); }));
    public static readonly DependencyProperty BrandProperty = Property(nameof(Brand), typeof(BleeckerHeaderBrand), null);
    public static readonly DependencyProperty NavigationProperty = Property(nameof(Navigation), typeof(object), null);
    public static readonly DependencyProperty ActionsProperty = Property(nameof(Actions), typeof(object), null);
    public static readonly DependencyProperty MobileActionsProperty = Property(nameof(MobileActions), typeof(object), null);
    public static readonly DependencyProperty ActionsTemplateProperty = Property(nameof(ActionsTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty MobileActionsTemplateProperty = Property(nameof(MobileActionsTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty ActionsTemplateSelectorProperty = Property(nameof(ActionsTemplateSelector), typeof(DataTemplateSelector), null);
    public static readonly DependencyProperty MobileActionsTemplateSelectorProperty = Property(nameof(MobileActionsTemplateSelector), typeof(DataTemplateSelector), null);
    public static readonly DependencyProperty LinkTemplateProperty = Property(nameof(LinkTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty LinkStyleProperty = Property(nameof(LinkStyle), typeof(Style), null);
    public static readonly DependencyProperty CommandProperty = Property(nameof(Command), typeof(ICommand), null);
    public static readonly DependencyProperty CommandParameterProperty = Property(nameof(CommandParameter), typeof(object), null);
    public static readonly DependencyProperty PositionProperty = Property(nameof(Position), typeof(BleeckerHeaderPosition), BleeckerHeaderPosition.Fixed);
    public static readonly DependencyProperty FullWidthProperty = Property(nameof(FullWidth), typeof(bool), false);
    public static readonly DependencyProperty HeaderHeightProperty = Property(nameof(HeaderHeight), typeof(double), 72d);
    public BleeckerHeaderBrand Brand { get => GetValue(BrandProperty) as BleeckerHeaderBrand ?? throw new InvalidOperationException("Header Brand must resolve before mounting."); set => SetValue(BrandProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public IEnumerable<BleeckerNavItem> Navigation { get => GetValue(NavigationProperty) as IEnumerable<BleeckerNavItem> ?? throw new InvalidOperationException("Header Navigation must resolve before mounting."); set => SetValue(NavigationProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }
    public object? MobileActions { get => GetValue(MobileActionsProperty); set => SetValue(MobileActionsProperty, value); }
    public DataTemplate? ActionsTemplate { get => (DataTemplate?)GetValue(ActionsTemplateProperty); set => SetValue(ActionsTemplateProperty, value); }
    public DataTemplate? MobileActionsTemplate { get => (DataTemplate?)GetValue(MobileActionsTemplateProperty); set => SetValue(MobileActionsTemplateProperty, value); }
    public DataTemplateSelector? ActionsTemplateSelector { get => (DataTemplateSelector?)GetValue(ActionsTemplateSelectorProperty); set => SetValue(ActionsTemplateSelectorProperty, value); }
    public DataTemplateSelector? MobileActionsTemplateSelector { get => (DataTemplateSelector?)GetValue(MobileActionsTemplateSelectorProperty); set => SetValue(MobileActionsTemplateSelectorProperty, value); }
    /// <summary>Receives BleeckerNavLinkContext for the brand and both navigation directions.</summary>
    public DataTemplate? LinkTemplate { get => (DataTemplate?)GetValue(LinkTemplateProperty); set => SetValue(LinkTemplateProperty, value); }
    public Style? LinkStyle { get => (Style?)GetValue(LinkStyleProperty); set => SetValue(LinkStyleProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
    public BleeckerHeaderPosition Position { get => (BleeckerHeaderPosition)GetValue(PositionProperty); set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(PositionProperty, value); } }
    public bool FullWidth { get => (bool)GetValue(FullWidthProperty); set => SetValue(FullWidthProperty, value); }
    public double HeaderHeight { get => (double)GetValue(HeaderHeightProperty); set { if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(HeaderHeightProperty, value); } }
    public bool IsMenuOpen => menuOpen;
    public event EventHandler<BleeckerNavItem>? ItemInvoked;
    public event EventHandler<BleeckerHeaderBrand>? BrandInvoked;
    public void ToggleNavigation() { if (IsLoaded && IsEnabled && surface is not null) SetMenuOpen(!menuOpen); }
    private void SetMenuOpen(bool value) { if (menuOpen == value) return; menuOpen = value; surface?.UpdateIcon(); PresentMenu(true); }
    private void NavigationInvoked(object? sender, BleeckerNavItem item) { if (ReferenceEquals(sender, surface?.MobileNavigation)) SetMenuOpen(false); ItemInvoked?.Invoke(this, item); }
    protected override void OnApplyTemplate()
    {
        Detach(); base.OnApplyTemplate(); if (Template is null) return;
        root = GetTemplateChild("PART_Header") as Grid ?? throw new InvalidOperationException("Header requires PART_Header.");
        root.RenderTransform = shift; surface = new(this) { Name = "HeaderSurface", UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight }; root.Children.Add(surface);
        if (IsLoaded) Observe(); Render();
    }
    private void Detach() { StopMotion(); surface?.Release(); root?.Children.Clear(); root = null; surface = null; }
    private void Render()
    {
        if (!Enum.IsDefined(Position)) throw new ArgumentOutOfRangeException(nameof(Position));
        if (!double.IsFinite(HeaderHeight) || HeaderHeight < 0) throw new ArgumentOutOfRangeException(nameof(HeaderHeight));
        if (rendering || surface is null) return;
        if (GetValue(BrandProperty) is not BleeckerHeaderBrand) { if (IsLoaded) _ = Brand; return; }
        rendering = true;
        try { surface.Sync(); UpdateSurface(); surface.InvalidateMenu(); InvalidateMeasure(); }
        finally { rendering = false; }
    }
    private void Observe()
    {
        DisconnectNavigation();
        if (Navigation is INotifyCollectionChanged collection) { observedNavigation = collection; collection.CollectionChanged += NavigationChanged; }
        foreach (var item in Navigation) { if (item is null) throw new ArgumentException("Header navigation cannot contain null items."); if (observedItems.Add(item)) item.Changed += ItemChanged; }
        var brand = GetValue(BrandProperty) as BleeckerHeaderBrand;
        if (!ReferenceEquals(brand, observedBrand)) { if (observedBrand is not null) observedBrand.Changed -= BrandChanged; observedBrand = brand; if (brand is not null) brand.Changed += BrandChanged; }
        if (ReferenceEquals(XamlRoot, observedRoot)) return;
        if (observedRoot is not null) observedRoot.Changed -= RootChanged; observedRoot = XamlRoot; if (observedRoot is not null) observedRoot.Changed += RootChanged;
    }
    private void DisconnectNavigation() { if (observedNavigation is not null) observedNavigation.CollectionChanged -= NavigationChanged; observedNavigation = null; foreach (var item in observedItems) item.Changed -= ItemChanged; observedItems.Clear(); }
    private void NavigationChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        void Update() { if (!IsLoaded || !ReferenceEquals(sender, observedNavigation)) return; Observe(); Render(); }
        if (uiQueue.HasThreadAccess) Update(); else uiQueue.TryEnqueue(Update);
    }
    private void ItemChanged(object? sender, EventArgs args)
    {
        void Update() { if (IsLoaded && sender is BleeckerNavItem item && observedItems.Contains(item)) Render(); }
        if (uiQueue.HasThreadAccess) Update(); else uiQueue.TryEnqueue(Update);
    }
    private void Disconnect() { DisconnectNavigation(); if (observedBrand is not null) observedBrand.Changed -= BrandChanged; observedBrand = null; if (observedRoot is not null) observedRoot.Changed -= RootChanged; observedRoot = null; DisconnectScroll(); }
    private void BrandChanged(object? sender, EventArgs args) => Render();
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) { UpdateSurface(); surface?.InvalidateMeasure(); InvalidateMeasure(); UpdatePosition(); }
    private double Viewport => XamlRoot?.Size.Width ?? availableWidth;
    private void UpdateSurface()
    {
        if (root is null) return;
        var width = Position == BleeckerHeaderPosition.Fixed ? Viewport : availableWidth;
        if (!double.IsFinite(width) || width < 0) throw new InvalidOperationException("Header requires a finite viewport or available width.");
        var height = HeaderHeight + 1;
        if (root.Width != width) root.Width = width; if (root.Height != height) root.Height = height;
        var margin = new Thickness(0, 0, availableWidth - width, Position == BleeckerHeaderPosition.Fixed ? -height : 0);
        if (root.Margin != margin) root.Margin = margin;
    }
    protected override Size MeasureOverride(Size available) { availableWidth = double.IsFinite(available.Width) ? available.Width : XamlRoot?.Size.Width ?? throw new InvalidOperationException("Header measurement requires a finite available width."); UpdateSurface(); return base.MeasureOverride(available); }
    private void LayoutChanged(object? sender, object args) => UpdatePosition();
    private void ViewerChanged(object? sender, ScrollViewerViewChangedEventArgs args) => UpdatePosition();
    private void ViewChanged(ScrollView sender, object args) => UpdatePosition();
    private void DisconnectScroll() { if (scrollOwner is ScrollViewer viewer) viewer.ViewChanged -= ViewerChanged; if (scrollOwner is ScrollView view) view.ViewChanged -= ViewChanged; scrollOwner = null; scrollViewport = null; }
    private static ScrollContentPresenter? Presenter(DependencyObject parent)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++) { var child = VisualTreeHelper.GetChild(parent, index); if (child is ScrollContentPresenter found) return found; if (child is ScrollViewer or ScrollView) continue; if (Presenter(child) is { } nested) return nested; }
        return null;
    }
    private void UpdatePosition()
    {
        if (positioning || root is not { IsLoaded: true } || XamlRoot is null) return; positioning = true;
        try
        {
            if (Position == BleeckerHeaderPosition.Static) { DisconnectScroll(); shift.Y = 0; return; }
            DependencyObject? next = null;
            for (var ancestor = VisualTreeHelper.GetParent(this); ancestor is not null; ancestor = VisualTreeHelper.GetParent(ancestor)) if (ancestor is ScrollViewer or ScrollView) { next = ancestor; break; }
            if (!ReferenceEquals(scrollOwner, next)) { DisconnectScroll(); scrollOwner = next; if (next is ScrollViewer viewer) { viewer.ApplyTemplate(); scrollViewport = Presenter(viewer) ?? throw new InvalidOperationException("Header requires its ScrollViewer presenter."); viewer.ViewChanged += ViewerChanged; } else if (next is ScrollView view) { view.ApplyTemplate(); scrollViewport = view.ScrollPresenter ?? throw new InvalidOperationException("Header requires its ScrollView presenter."); view.ViewChanged += ViewChanged; } }
            var viewport = Position == BleeckerHeaderPosition.Fixed ? XamlRoot.Content as FrameworkElement : scrollViewport ?? XamlRoot.Content as FrameworkElement;
            if (viewport is null) throw new InvalidOperationException("Header requires a loaded positioning viewport.");
            var transform = root.TransformToVisual(viewport); var origin = transform.TransformPoint(new Point()); var scale = transform.TransformPoint(new Point(0, 1)).Y - origin.Y;
            if (!(scale > 0) || !double.IsFinite(scale)) throw new InvalidOperationException("Header positioning requires a positive vertical scale.");
            var normalTop = origin.Y / scale - shift.Y; var parent = VisualTreeHelper.GetParent(this) as FrameworkElement ?? throw new InvalidOperationException("Header requires a layout parent.");
            var parentBox = parent.TransformToVisual(viewport).TransformBounds(new Rect(0, 0, parent.ActualWidth, parent.ActualHeight));
            var value = BleeckerHeaderModel.Shift(Position, normalTop, parentBox.Bottom / scale, HeaderHeight + 1);
            if (Math.Abs(shift.Y - value) > .000001) shift.Y = value;
        }
        finally { positioning = false; }
    }
    private void MotionChanged(UISettings sender, object args) => uiQueue.TryEnqueue(() => PresentMenu(false));
    private void PresentMenu(bool animate)
    {
        var time = clock.Elapsed.TotalMilliseconds; var motion = animate && IsLoaded && settings.AnimationsEnabled;
        maximum.Retarget(menuOpen ? 384 : 0, time, 200, motion); opacity.Retarget(menuOpen ? 1 : 0, time, 200, motion); surface?.InvalidateMenu();
        if ((maximum.IsRunning(time) || opacity.IsRunning(time)) && !motionSubscribed) { motionSubscribed = true; CompositionTarget.Rendering += Frame; }
    }
    private void Frame(object? sender, object args) { surface?.InvalidateMenu(); if (!maximum.IsRunning(clock.Elapsed.TotalMilliseconds) && !opacity.IsRunning(clock.Elapsed.TotalMilliseconds)) StopMotion(); }
    private void StopMotion() { if (motionSubscribed) CompositionTarget.Rendering -= Frame; motionSubscribed = false; }
    protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);
    private sealed class Peer(BleeckerHeader owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetClassNameCore() => nameof(BleeckerHeader);
        protected override AutomationLandmarkType GetLandmarkTypeCore() => AutomationLandmarkType.Custom;
        protected override string GetLocalizedLandmarkTypeCore() => "banner";
        protected override Rect GetBoundingRectangleCore() => owner.root is null ? base.GetBoundingRectangleCore() : new FrameworkElementAutomationPeer(owner.root).GetBoundingRectangle();
        protected override bool IsOffscreenCore() => owner.root is null ? base.IsOffscreenCore() : new FrameworkElementAutomationPeer(owner.root).IsOffscreen();
    }
}
