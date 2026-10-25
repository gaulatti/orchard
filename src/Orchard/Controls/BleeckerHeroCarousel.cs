using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using VirtualKey = Windows.System.VirtualKey;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A reusable, caller-owned hero with retained slides and owning-window arrow input.</summary>
[ContentProperty(Name = nameof(Items))]
public sealed partial class BleeckerHeroCarousel : Control
{
    private readonly Dictionary<string, SlideSlot> slots = new(StringComparer.Ordinal);
    private readonly HashSet<INotifyCollectionChanged> collections = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<BleeckerHeroCarouselItem> observedItems = new(ReferenceEqualityComparer.Instance);
    private readonly List<(FrameworkElement Element, DependencyProperty Property, long Token)> ancestorObservers = [];
    private readonly Dictionary<string, TextBlock> palette = [];
    private readonly List<(TextBlock Element, long Token)> paletteObservers = [];
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly UISettings settings = new();
    private IReadOnlyList<BleeckerHeroCarouselItem> items = [];
    private BleeckerHeroCarouselState? state;
    private HeroPanel? panel;
    private Grid? templateRoot;
    private ControlTemplate? appliedTemplate;
    private DispatcherQueueTimer? timer;
    private long? timerGeneration;
    private double requestedInterval;
    private int timerTicks;
    private ICommand? observedCommand;
    private IDisposable? scopeObserver;
    private bool rendering, pending, frameSubscribed, hovered, keyboardFocused, themeQueued;
    private VisualStateGroup? componentStates;
    private XamlRoot? observedRoot;
    private UIElement? keyboardRoot;
    private readonly KeyEventHandler windowKeyHandler;
    internal double RemScale => FontSize / 16;
    private double Now => clock.Elapsed.TotalMilliseconds;
    private bool MotionEnabled => IsLoaded && settings.AnimationsEnabled;

    public BleeckerHeroCarousel()
    {
        DefaultStyleKey = typeof(BleeckerHeroCarousel); IsTabStop = false; UseLayoutRounding = false;
        Items = new BleeckerHeroCarouselItems();
        Loaded += LoadedComponent; Unloaded += UnloadedComponent;
        PointerEntered += (_, _) => { ApplyHover(true); VisualStateManager.GoToState(this,"PointerOver",true); };
        PointerExited += (_, _) => { ApplyHover(false); VisualStateManager.GoToState(this,"Normal",true); };
        GotFocus += (_, _) => { keyboardFocused = true; UpdateAppearance(); };
        LostFocus += (_, _) => { keyboardFocused = FocusWithin(); UpdateAppearance(); };
        windowKeyHandler = WindowKeyDown;
        ActualThemeChanged += (_, _) =>
        {
            if (themeQueued) return; themeQueued = true;
            if (!DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => { themeQueued = false; if (IsLoaded) Render(); })) throw new InvalidOperationException("Hero Carousel theme dispatcher rejected an update.");
        };
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => UpdateInput());
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachPresentation(); });
        foreach (var property in new[] { FontSizeProperty, FontFamilyProperty, StyleProperty, FlowDirectionProperty, LanguageProperty, IsTextScaleFactorEnabledProperty }) RegisterPropertyChangedCallback(property, (_, _) => Render());
        RegisterPropertyChangedCallback(ForegroundProperty, (_, _) => UpdateAppearance());
    }

    private static DependencyProperty Property(string name, Type type, object? initial = null) => DependencyProperty.Register(name, type, typeof(BleeckerHeroCarousel), new PropertyMetadata(initial, (owner, _) => ((BleeckerHeroCarousel)owner).Render()));
    public static readonly DependencyProperty ItemsProperty = Property(nameof(Items), typeof(object));
    public static readonly DependencyProperty AutoAdvanceMillisecondsProperty = Property(nameof(AutoAdvanceMilliseconds), typeof(double), 5000d);
    public static readonly DependencyProperty ShowControlsProperty = Property(nameof(ShowControls), typeof(bool), true);
    public static readonly DependencyProperty ShowIndicatorsProperty = Property(nameof(ShowIndicators), typeof(bool), true);
    public static readonly DependencyProperty DefaultBackgroundProperty = Property(nameof(DefaultBackground), typeof(Brush));
    public static readonly DependencyProperty EmptyStateProperty = Property(nameof(EmptyState), typeof(object));
    public static readonly DependencyProperty EmptyStateTemplateProperty = Property(nameof(EmptyStateTemplate), typeof(DataTemplate));
    public static readonly DependencyProperty EmptyStateTemplateSelectorProperty = Property(nameof(EmptyStateTemplateSelector), typeof(DataTemplateSelector));
    public static readonly DependencyProperty LinkTemplateProperty = Property(nameof(LinkTemplate), typeof(DataTemplate));
    public static readonly DependencyProperty LinkStyleProperty = Property(nameof(LinkStyle), typeof(Style));
    public static readonly DependencyProperty ArrowStyleProperty = Property(nameof(ArrowStyle), typeof(Style));
    public static readonly DependencyProperty IndicatorStyleProperty = Property(nameof(IndicatorStyle), typeof(Style));
    public static readonly DependencyProperty TagStyleProperty = Property(nameof(TagStyle), typeof(Style));
    public static readonly DependencyProperty CtaStyleProperty = Property(nameof(CtaStyle), typeof(Style));
    public static readonly DependencyProperty CommandProperty = Property(nameof(Command), typeof(ICommand));
    public static readonly DependencyProperty CommandParameterProperty = Property(nameof(CommandParameter), typeof(object));
    public IEnumerable<BleeckerHeroCarouselItem> Items { get => (IEnumerable<BleeckerHeroCarouselItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public double AutoAdvanceMilliseconds { get => (double)GetValue(AutoAdvanceMillisecondsProperty); set => SetValue(AutoAdvanceMillisecondsProperty, value); }
    public bool ShowControls { get => (bool)GetValue(ShowControlsProperty); set => SetValue(ShowControlsProperty, value); }
    public bool ShowIndicators { get => (bool)GetValue(ShowIndicatorsProperty); set => SetValue(ShowIndicatorsProperty, value); }
    public Brush? DefaultBackground { get => (Brush?)GetValue(DefaultBackgroundProperty); set => SetValue(DefaultBackgroundProperty, value); }
    public object? EmptyState { get => GetValue(EmptyStateProperty); set => SetValue(EmptyStateProperty, value); }
    public DataTemplate? EmptyStateTemplate { get => (DataTemplate?)GetValue(EmptyStateTemplateProperty); set => SetValue(EmptyStateTemplateProperty, value); }
    public DataTemplateSelector? EmptyStateTemplateSelector { get => (DataTemplateSelector?)GetValue(EmptyStateTemplateSelectorProperty); set => SetValue(EmptyStateTemplateSelectorProperty, value); }
    public DataTemplate? LinkTemplate { get => (DataTemplate?)GetValue(LinkTemplateProperty); set => SetValue(LinkTemplateProperty, value); }
    public Style? LinkStyle { get => (Style?)GetValue(LinkStyleProperty); set => SetValue(LinkStyleProperty, value); }
    public Style? ArrowStyle { get => (Style?)GetValue(ArrowStyleProperty); set => SetValue(ArrowStyleProperty, value); }
    public Style? IndicatorStyle { get => (Style?)GetValue(IndicatorStyleProperty); set => SetValue(IndicatorStyleProperty, value); }
    public Style? TagStyle { get => (Style?)GetValue(TagStyleProperty); set => SetValue(TagStyleProperty, value); }
    public Style? CtaStyle { get => (Style?)GetValue(CtaStyleProperty); set => SetValue(CtaStyleProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
    public int CurrentIndex => state?.Index ?? 0;
    public event EventHandler? CurrentIndexChanged;
    public event EventHandler<BleeckerHeroCarouselItem>? ItemInvoked;
    public event EventHandler<BleeckerHeroCarouselItem>? ImageFailed;
    public void Refresh() => Render();
    public void MoveNext() { if (InputAllowed && state?.Next() == true) SelectionUpdated(); }
    public void MovePrevious() { if (InputAllowed && state?.Previous() == true) SelectionUpdated(); }
    public void SelectSlide(int index) { var before=CurrentIndex; if (InputAllowed && state?.Select(index) == true && CurrentIndex!=before) SelectionUpdated(); }

    private void LoadedComponent(object sender, RoutedEventArgs args)
    {
        state = new(items.Count, AutoAdvanceMilliseconds); state.SetHovered(hovered); ObserveAncestors();
        if (XamlRoot is { } root) { observedRoot=root;root.Changed+=RootChanged;scopeObserver = BleeckerPopupScope.Observe(root, UpdateInput); }
        ConnectWindowKeyboard(); Render();
    }
    private void UnloadedComponent(object sender, RoutedEventArgs args)
    {
        DisconnectWindowKeyboard();
        if(observedRoot is not null)observedRoot.Changed-=RootChanged;observedRoot=null;
        state?.Remove(); state = null; StopTimer(); Disconnect(); DisconnectAncestors(); scopeObserver?.Dispose(); scopeObserver = null;
        hovered = keyboardFocused = false; UnsubscribeFrames();
        foreach (var slot in slots.Values) slot.Dispose(); slots.Clear(); panel?.ClearSlides(); panel?.Refresh();
    }
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        var part = GetTemplateChild("PART_Root"); if (part is not null && part is not Grid) throw new InvalidOperationException("Hero Carousel PART_Root must be a Grid.");
        var preserveSlides=panel is not null&&part is Grid&&ReferenceEquals(appliedTemplate,Template);
        if(preserveSlides&&ReferenceEquals(part,templateRoot)){Render();return;}
        DetachPresentation(preserveSlides);appliedTemplate=Template;
        templateRoot = part as Grid; if (templateRoot is null) return;
        if(GetTemplateChild("PART_Chrome") is FrameworkElement chrome){componentStates=VisualStateManager.GetVisualStateGroups(chrome).FirstOrDefault(g=>g.Name=="CommonStates");if(componentStates is not null)componentStates.CurrentStateChanged+=ComponentStateChanged;}
        foreach (var key in new[] { "Foreground", "TitleHover", "Description", "DescriptionHover", "CtaBackground", "CtaHover", "CtaForeground", "ArrowBackground", "ArrowHover", "ArrowBorder", "ArrowBorderHover", "ArrowForeground", "ArrowForegroundHover", "TagBackground", "TagBorder", "Indicator", "IndicatorHover", "IndicatorActive", "NavigationBackground", "NavigationBorder", "Separator", "EmptyForeground" })
        {
            var probe = (TextBlock)XamlReader.Load($"<TextBlock xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Foreground='{{ThemeResource BleeckerHero{key}Brush}}' Visibility='Collapsed' AutomationProperties.AccessibilityView='Raw'/>");
            palette.Add(key, probe); templateRoot.Children.Add(probe); paletteObservers.Add((probe, probe.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty, (_, _) => UpdateAppearance())));
        }
        foreach (var key in new[] { "DeepSea", "Dusk", "AccentOxblood" })
        {
            var probe = (TextBlock)XamlReader.Load($"<TextBlock xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Foreground='{{ThemeResource Bleecker{key}Brush}}' Visibility='Collapsed' AutomationProperties.AccessibilityView='Raw'/>");
            palette.Add(key, probe); templateRoot.Children.Add(probe); paletteObservers.Add((probe, probe.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty, (_, _) => UpdateAppearance())));
        }
        panel = new(this) { Name = "HeroCarouselViewport", FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false }; templateRoot.Children.Add(panel);foreach(var slot in slots.Values)panel.AddSlide(slot.Root);Render();
    }
    private void DetachPresentation(bool preserveSlides=false)
    {
        if(componentStates is not null)componentStates.CurrentStateChanged-=ComponentStateChanged;componentStates=null;
        if(!preserveSlides)StopTimer();UnsubscribeFrames();if(preserveSlides)panel?.ClearSlides();else{foreach(var slot in slots.Values)slot.Dispose();slots.Clear();}panel?.Dispose();panel=null;appliedTemplate=null;
        foreach (var (element, token) in paletteObservers) element.UnregisterPropertyChangedCallback(TextBlock.ForegroundProperty, token); paletteObservers.Clear(); palette.Clear(); templateRoot?.Children.Clear(); templateRoot = null;
    }
    private void ApplyHover(bool value){hovered=value;state?.SetHovered(value);ScheduleTimer();UpdateAppearance();}
    private void RootChanged(XamlRoot sender,XamlRootChangedEventArgs args){if(IsLoaded){ConnectWindowKeyboard();Render();}}
    private void ComponentStateChanged(object sender,VisualStateChangedEventArgs args)=>ApplyHover(componentStates?.CurrentState?.Name=="PointerOver");
    private SolidColorBrush Brush(string key) => palette.TryGetValue(key, out var value) && value.Foreground is SolidColorBrush brush ? brush : throw new InvalidOperationException("Hero Carousel requires its semantic solid palette: " + key);
    private bool HasCallerFontFamily
    {
        get
        {
            if (ReadLocalValue(FontFamilyProperty) != DependencyProperty.UnsetValue) return true;
            for (var style = Style; style is not null; style = style.BasedOn)
                if (style.Setters.OfType<Setter>().Any(setter => setter.Property == FontFamilyProperty)) return true;
            return false;
        }
    }
    private static string Key(BleeckerHeroCarouselItem item) => item.Id is not null ? "id:" + item.Id : "value:" + item.Title + ":" + item.Href;
    private void Render()
    {
        if (!DispatcherQueue.HasThreadAccess) throw new InvalidOperationException("Hero Carousel data must change on its owning UI dispatcher.");
        if (rendering) { pending = true; return; }
        if (GetValue(ItemsProperty) is not IEnumerable<BleeckerHeroCarouselItem> values) { if (!IsLoaded && GetBindingExpression(ItemsProperty) is not null) return; throw new InvalidOperationException("Hero Carousel items must resolve before mounting."); }
        rendering = true;
        try
        {
            items = values.ToArray(); var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in items) { ArgumentNullException.ThrowIfNull(item); if (item.Title is null || item.Description is null || item.Href is null || item.Tags is null) throw new ArgumentException("Hero Carousel retains non-null original title, description, href and tags."); if (item.Kind is { } kind && !Enum.IsDefined(kind)) throw new ArgumentException("Hero Carousel kind is invalid."); if (!keys.Add(Key(item))) throw new ArgumentException("Hero Carousel sibling keys must be unique."); foreach (var tag in item.Tags) ArgumentNullException.ThrowIfNull(tag); }
            state?.Configure(items.Count, AutoAdvanceMilliseconds); Observe();
            if (panel is null) return;
            foreach (var key in slots.Keys.Where(k => !keys.Contains(k)).ToArray()) { slots[key].Dispose(); slots.Remove(key); }
            for (var i = 0; i < items.Count; i++) { var key = Key(items[i]); if (!slots.TryGetValue(key, out var slot)) { slot = new(this); slots.Add(key, slot); panel.AddSlide(slot.Root); } slot.Update(items[i], i); Canvas.SetZIndex(slot.Root, i); }
            panel.Refresh(); UpdateSelection(false); ScheduleTimer();
        }
        finally { rendering = false; }
        if (pending) { pending = false; Render(); }
    }
    private void Observe()
    {
        Disconnect(); if (!IsLoaded) return;
        void Collection(object value) { if (value is INotifyCollectionChanged collection && collections.Add(collection)) collection.CollectionChanged += CollectionChanged; }
        Collection(Items); foreach (var item in items) { if (observedItems.Add(item)) item.Changed += ItemChanged; Collection(item.Tags); }
        observedCommand = Command; if (observedCommand is not null) observedCommand.CanExecuteChanged += CommandChanged;
    }
    private void Disconnect()
    { foreach (var collection in collections) collection.CollectionChanged -= CollectionChanged; collections.Clear(); foreach (var item in observedItems) item.Changed -= ItemChanged; observedItems.Clear(); if (observedCommand is not null) observedCommand.CanExecuteChanged -= CommandChanged; observedCommand = null; }
    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) => Render();
    private void ItemChanged(object? sender, EventArgs args) => Render();
    private void CommandChanged(object? sender, EventArgs args)
    { if (DispatcherQueue.HasThreadAccess) UpdateInput(); else if (!DispatcherQueue.TryEnqueue(UpdateInput)) throw new InvalidOperationException("Hero Carousel command dispatcher rejected an update."); }
    private void ObserveAncestors()
    {
        DisconnectAncestors(); for (DependencyObject? node = this; node is not null; node = VisualTreeHelper.GetParent(node)) if (node is FrameworkElement element)
        { ancestorObservers.Add((element, VisibilityProperty, element.RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => UpdateInput()))); if (element is Control) ancestorObservers.Add((element, IsEnabledProperty, element.RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => UpdateInput()))); }
    }
    private void DisconnectAncestors() { foreach (var (element, property, token) in ancestorObservers) element.UnregisterPropertyChangedCallback(property, token); ancestorObservers.Clear(); }
    private bool InputAllowed
    {
        get
        {
            if (!IsLoaded || panel is null || !ReferenceEquals(appliedTemplate,Template) || state?.IsMounted != true || !BleeckerPopupScope.AllowsInput(XamlRoot, this)) return false;
            for (DependencyObject? node = this; node is not null; node = VisualTreeHelper.GetParent(node)) if (node is FrameworkElement element && (element.Visibility != Visibility.Visible || element is Control control && !control.IsEnabled)) return false;
            return true;
        }
    }
    internal bool CanInvoke(BleeckerHeroCarouselLinkContext context, bool ignoreOwnCommand)
    {
        if (!InputAllowed || !context.Mounted || context.Element?.IsLoaded != true || !(Command?.CanExecute(CommandParameter) ?? true) || !slots.Values.Any(s=>ReferenceEquals(s.Context,context))) return false;
        for (DependencyObject? node = context.Element; node is not null; node = VisualTreeHelper.GetParent(node))
        { if (node is FrameworkElement element && element.Visibility != Visibility.Visible) return false; var own = ignoreOwnCommand && ReferenceEquals(node, context.Element) && node is ButtonBase button && ReferenceEquals(button.Command, context.Command); if (node is Control control && !control.IsEnabled && !own) return false; }
        return true;
    }
    internal void Invoke(BleeckerHeroCarouselLinkContext context) { if (!CanInvoke(context, false)) return; var original = context.Item; Command?.Execute(CommandParameter); ItemInvoked?.Invoke(this, original); }
    private bool FocusWithin()
    { if (XamlRoot is null) return false; for (var node = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject; node is not null; node = VisualTreeHelper.GetParent(node)) if (ReferenceEquals(node, this)) return true; return false; }
    private void ConnectWindowKeyboard()
    {
        var root = IsLoaded ? XamlRoot?.Content : null;
        if (ReferenceEquals(root, keyboardRoot)) return;
        DisconnectWindowKeyboard(); keyboardRoot = root;
        // The web document listener neither requires carousel focus nor consumes the key.
        // Receive handled events as well so every mounted carousel sees the same arrow.
        keyboardRoot?.AddHandler(UIElement.KeyDownEvent, windowKeyHandler, true);
    }
    private void DisconnectWindowKeyboard()
    { keyboardRoot?.RemoveHandler(UIElement.KeyDownEvent, windowKeyHandler); keyboardRoot = null; }
    private void WindowKeyDown(object sender, KeyRoutedEventArgs args)
    { if (ReferenceEquals(sender, keyboardRoot)) ProcessWindowKey(args.Key, FocusManager.GetFocusedElement(XamlRoot) as DependencyObject); }
    private void ProcessWindowKey(VirtualKey key, DependencyObject? focusedElement)
    {
        if (!InputAllowed || key is not (VirtualKey.Left or VirtualKey.Right)) return;
        // Native counterparts of INPUT/TEXTAREA retain their own arrow semantics.
        // RichEditBox corresponds to contenteditable, which the source does not exclude.
        for (var node = focusedElement; node is not null; node = VisualTreeHelper.GetParent(node))
            if (node is TextBox or PasswordBox or CheckBox or RadioButton or Slider or ToggleSwitch or BleeckerFileInput or BleeckerSwitch or BleeckerSlider or BleeckerRadioGroup or BleeckerOtpInput) return;
        if ((key == VirtualKey.Left ? state?.Previous() : state?.Next()) == true) SelectionUpdated();
    }
    private void SelectionUpdated() { UpdateSelection(true); CurrentIndexChanged?.Invoke(this, EventArgs.Empty); }
    private void UpdateSelection(bool animate)
    { foreach (var slot in slots.Values) slot.Select(state is { HasActiveSlide: true } current && current.Index == slot.Index, animate); panel?.UpdateControls(); UpdateInput(); QueueFrames(); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer(); }
    private void UpdateInput() { foreach (var slot in slots.Values) slot.UpdateInput(); panel?.UpdateControls(); }
    private void UpdateAppearance() { if (panel is null) return; foreach (var slot in slots.Values) slot.Paint(); panel.Paint(); panel.UpdateControls(); QueueFrames(); }
    private void ScheduleTimer()
    {
        if (!IsLoaded || panel is null || state?.ShouldSchedule != true) { StopTimer(); return; }
        if (timer is not null && timer.IsRunning && timerGeneration == state.TimerGeneration) return;
        StopTimer(); timer ??= DispatcherQueue.CreateTimer(); timer.IsRepeating = true; timer.Tick += TimerTick;
        var delay = double.IsFinite(AutoAdvanceMilliseconds) ? Math.Truncate(AutoAdvanceMilliseconds) % 4294967296d : 0; if (delay < 0) delay += 4294967296d; if (delay >= 2147483648d) delay -= 4294967296d;
        requestedInterval = Math.Max(0, delay); timerTicks = 0; timer.Interval = TimeSpan.FromMilliseconds(requestedInterval); timerGeneration = state.TimerGeneration; timer.Start();
    }
    private void TimerTick(DispatcherQueueTimer sender, object args)
    {
        if (timerGeneration is not long generation || state?.Tick(generation) != true) return;
        if (++timerTicks == 6 && requestedInterval < 4) sender.Interval = TimeSpan.FromMilliseconds(4);
        SelectionUpdated();
    }
    private void StopTimer() { if (timer is not null) { timer.Stop(); timer.Tick -= TimerTick; } timerGeneration = null; }
    private void QueueFrames()
    { if (!IsLoaded || frameSubscribed || !(slots.Values.Any(s=>s.IsAnimating(Now)) || panel?.IsAnimating(Now) == true)) return; frameSubscribed = true; CompositionTarget.Rendering += Rendering; }
    private void Rendering(object? sender, object args)
    { var now = Now; foreach (var slot in slots.Values) slot.Present(now); panel?.Present(now); if (!slots.Values.Any(s=>s.IsAnimating(now)) && panel?.IsAnimating(now) != true) UnsubscribeFrames(); }
    private void UnsubscribeFrames() { if (!frameSubscribed) return; frameSubscribed = false; CompositionTarget.Rendering -= Rendering; }
    protected override AutomationPeer OnCreateAutomationPeer() => new HeroPeer(this);
    protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)=>base.ArrangeOverride(new(finalSize.Width,600*RemScale));
    private sealed class HeroPeer(BleeckerHeroCarousel owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerHeroCarousel);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetNameCore() { var supplied = base.GetNameCore(); return string.IsNullOrEmpty(supplied) ? "Featured content" : supplied; }
        protected override IList<AutomationPeer> GetChildrenCore()
        {
            var result = new List<AutomationPeer>(); if (!owner.IsLoaded || owner.panel is null) return result;
            foreach (var slot in owner.slots.Values.Where(s=>s.Context.IsActive)) if (FrameworkElementAutomationPeer.CreatePeerForElement(slot.Root) is { } peer) result.Add(peer);
            foreach (var control in owner.panel.AutomationControls.Concat(owner.panel.AutomationEmptyContent)) if (FrameworkElementAutomationPeer.CreatePeerForElement(control) is { } peer) result.Add(peer);
            return result;
        }
    }
}
