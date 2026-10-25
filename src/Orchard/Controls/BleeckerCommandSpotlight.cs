using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI.Core;

namespace Orchard;

/// <summary>A reusable command spotlight with original caller actions and shared modal ownership.</summary>
public sealed partial class BleeckerCommandSpotlight : Control
{
    private BleeckerCommandSpotlightState<BleeckerCommandSpotlightAction> state = new(a => a.Describe());
    private readonly HashSet<BleeckerCommandSpotlightAction> observedActions = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<string, TextBlock> palette = [];
    private IEnumerable<BleeckerCommandSpotlightAction>? actionSource;
    private INotifyCollectionChanged? collection;
    private IReadOnlyList<BleeckerCommandSpotlightAction> actions = [];
    private Grid? declaration;
    private Trigger? trigger;
    private BleeckerModalHost? host;
    private BleeckerModalHost.Registration? registration;
    private XamlRoot? observedRoot;
    private UIElement? keyboardRoot;
    private readonly KeyEventHandler keyboardHandler;
    private IDisposable? popupObservation;
    private bool rendering, pending, ready, queued, dataDirty = true;
    private ICommand? observedCommand;
    private int mountGeneration;
    internal double Rem => FontSize / 16;
    private static bool CallerStyleSets(Style? style, DependencyProperty property)
    {
        for (; style is not null; style = style.BasedOn)
            if (style.Setters.OfType<Setter>().Any(setter => setter.Property == property)) return true;
        return false;
    }
    internal bool HasCallerFontFamily { get { if (ReadLocalValue(FontFamilyProperty) != DependencyProperty.UnsetValue) return true; for (var style = Style; style is not null; style = style.BasedOn) if (style.Setters.OfType<Setter>().Any(s => s.Property == FontFamilyProperty)) return true; return false; } }

    public BleeckerCommandSpotlight()
    {
        DefaultStyleKey = typeof(BleeckerCommandSpotlight); IsTabStop = false; UseLayoutRounding = false;
        keyboardHandler = WindowKeyDown; Actions = new BleeckerCommandSpotlightActions();
        Loaded += LoadedOwner; Unloaded += UnloadedOwner;
        ActualThemeChanged += (_, _) => QueueRefresh();
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); });
        foreach (var property in new[] { FontSizeProperty, FontFamilyProperty, ForegroundProperty, FlowDirectionProperty, LanguageProperty, IsTextScaleFactorEnabledProperty, IsEnabledProperty }) RegisterPropertyChangedCallback(property, (_, _) => Refresh());
    }
    private static DependencyProperty Property(string name, Type type, object? value = null) => DependencyProperty.Register(name, type, typeof(BleeckerCommandSpotlight), new PropertyMetadata(value, (owner, _) => ((BleeckerCommandSpotlight)owner).Refresh()));
    public static readonly DependencyProperty ActionsProperty = Property(nameof(Actions), typeof(object));
    public static readonly DependencyProperty OpenProperty = Property(nameof(Open), typeof(object));
    public static readonly DependencyProperty EmptyMessageProperty = Property(nameof(EmptyMessage), typeof(string), "No matching commands.");
    public static readonly DependencyProperty HotkeyProperty = Property(nameof(Hotkey), typeof(string), "k");
    public static readonly DependencyProperty PlaceholderProperty = Property(nameof(Placeholder), typeof(string), "Search commands...");
    public static readonly DependencyProperty ShowTriggerProperty = Property(nameof(ShowTrigger), typeof(bool), true);
    public static readonly DependencyProperty TriggerLabelProperty = Property(nameof(TriggerLabel), typeof(string), "Search");
    public static readonly DependencyProperty SurfaceStyleProperty = Property(nameof(SurfaceStyle), typeof(Style));
    public static readonly DependencyProperty TriggerStyleProperty = Property(nameof(TriggerStyle), typeof(Style));
    public static readonly DependencyProperty OverlayStyleProperty = Property(nameof(OverlayStyle), typeof(Style));
    public static readonly DependencyProperty OverlayTopOffsetProperty = Property(nameof(OverlayTopOffset), typeof(double), double.NaN);
    public static readonly DependencyProperty ActionCommandProperty = Property(nameof(ActionCommand), typeof(ICommand));
    public IEnumerable<BleeckerCommandSpotlightAction> Actions { get => (IEnumerable<BleeckerCommandSpotlightAction>)GetValue(ActionsProperty); set => SetValue(ActionsProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public bool? Open { get => GetValue(OpenProperty) is bool value ? value : GetValue(OpenProperty) is null ? null : throw new ArgumentException("Spotlight Open requires Boolean or null."); set => SetValue(OpenProperty, value); }
    public bool IsOpen => state.Effective(Open);
    public string Query => state.Query;
    public BleeckerCommandSpotlightAction? SelectedAction => state.EnterTarget;
    public string? SelectedActionId => state.SelectedId;
    public string EmptyMessage { get => (string)GetValue(EmptyMessageProperty); set => SetValue(EmptyMessageProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public string Hotkey { get => (string)GetValue(HotkeyProperty); set => SetValue(HotkeyProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public bool ShowTrigger { get => (bool)GetValue(ShowTriggerProperty); set => SetValue(ShowTriggerProperty, value); }
    public string TriggerLabel { get => (string)GetValue(TriggerLabelProperty); set => SetValue(TriggerLabelProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public Style? SurfaceStyle { get => (Style?)GetValue(SurfaceStyleProperty); set => SetValue(SurfaceStyleProperty, value); }
    public Style? TriggerStyle { get => (Style?)GetValue(TriggerStyleProperty); set => SetValue(TriggerStyleProperty, value); }
    public Style? OverlayStyle { get => (Style?)GetValue(OverlayStyleProperty); set => SetValue(OverlayStyleProperty, value); }
    /// <summary>Explicit native pixel top offset, or NaN for the source default of 12% of the window height.</summary>
    public double OverlayTopOffset { get => (double)GetValue(OverlayTopOffsetProperty); set { if (double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(OverlayTopOffsetProperty, value); } }
    /// <summary>Optional outer command, receiving the exact original action before its own callback.</summary>
    public ICommand? ActionCommand { get => (ICommand?)GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }
    public Exception? LastActionError { get; private set; }
    public event EventHandler<bool>? OpenRequested;
    public event EventHandler<BleeckerCommandSpotlightAction>? ActionSelected;
    public event EventHandler<BleeckerCommandSpotlightNavigationRequest>? NavigationRequested;
    public event EventHandler<BleeckerCommandSpotlightActionFailure>? ActionFailed;
    public event EventHandler? QueryChanged;
    public void Show() { if (DeclarationInputAllowed) Request(true); }
    public void Close() { if (SurfaceInputAllowed) Request(false); }
    private void Request(bool next) { var query = state.Query; state.Request(next, Open, value => OpenRequested?.Invoke(this, value)); Refresh(); if (query != state.Query) QueryChanged?.Invoke(this, EventArgs.Empty); }
    public void SetQuery(string value) { ArgumentNullException.ThrowIfNull(value); if (!SurfaceInputAllowed) return; state.SetQuery(value); Refresh(); QueryChanged?.Invoke(this, EventArgs.Empty); }
    /// <summary>Selects an enabled original action without invoking it, using the same path as row hover.</summary>
    public void SelectAction(BleeckerCommandSpotlightAction action) { ArgumentNullException.ThrowIfNull(action); if (!SurfaceInputAllowed) return; if (!state.Filtered.Any(a => ReferenceEquals(a, action))) throw new ArgumentException("SelectAction requires a current original filtered action.", nameof(action)); state.Hover(action); body?.Update(); }
    public void InvokeAction(BleeckerCommandSpotlightAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!SurfaceInputAllowed || action.Disabled || ActionCommand?.CanExecute(action) == false) return;
        if (!state.Filtered.Any(a => ReferenceEquals(a, action))) throw new ArgumentException("Spotlight requires an original action in its current filtered list.", nameof(action));
        BleeckerCommandSpotlightInvocation.Run(action, original => { ActionSelected?.Invoke(this, original); ActionCommand?.Execute(original); }, () => action.OnSelectAsync, () => string.IsNullOrEmpty(action.Href) ? null : () => Navigate(action), () => Request(false), error => ReportFailure(action, error));
    }
    private void Navigate(BleeckerCommandSpotlightAction action)
    {
        var request = new BleeckerCommandSpotlightNavigationRequest(action); NavigationRequested?.Invoke(this, request); if (request.Handled) return;
        if (!request.External) throw new InvalidOperationException("Internal Spotlight navigation requires the caller to handle NavigationRequested with its original href.");
        var uri = new Uri(request.Href, UriKind.Absolute); _ = Launch();
        async Task Launch() { try { if (!await Launcher.LaunchUriAsync(uri)) ReportFailure(action, new InvalidOperationException("Windows did not handle the external Spotlight URI.")); } catch (Exception error) { ReportFailure(action, error); } }
    }
    private void ReportFailure(BleeckerCommandSpotlightAction action, Exception error)
    {
        void Report() { LastActionError = error; ActionFailed?.Invoke(this, new(action, error)); }
        if (DispatcherQueue.HasThreadAccess) Report(); else if (!DispatcherQueue.TryEnqueue(Report)) throw new InvalidOperationException("Spotlight could not report an action failure.");
    }
    private bool DeclarationEnabled
    {
        get { if (!IsLoaded || declaration is null || !ready) return false; for (DependencyObject? node = this; node is not null; node = VisualTreeHelper.GetParent(node)) if (node is FrameworkElement { Visibility: not Visibility.Visible } or Control { IsEnabled: false }) return false; return true; }
    }
    private bool DeclarationInputAllowed => DeclarationEnabled && BleeckerPopupScope.AllowsInput(XamlRoot, this) && host?.AllowsFocusTarget(this) != false;
    private bool SurfaceInputAllowed => DeclarationEnabled && IsOpen && registration?.Layer.Active == true && focusSurface is not null && host?.AllowsFocusTarget(focusSurface) == true && BleeckerPopupScope.AllowsInput(XamlRoot, focusSurface);
    private void LoadedOwner(object sender, RoutedEventArgs args) { dataDirty = true; LayoutUpdated += LayoutOwner; QueueRefresh(); }
    private void UnloadedOwner(object sender, RoutedEventArgs args)
    {
        mountGeneration++; ready = false; LayoutUpdated -= LayoutOwner; DetachPresentation(); DisconnectRoot(); StopActions(); state = new(a => a.Describe()); dataDirty = true;
        if (observedCommand is not null) observedCommand.CanExecuteChanged -= CommandChanged; observedCommand = null;
    }
    protected override void OnApplyTemplate()
    {
        DetachTemplate(); base.OnApplyTemplate(); if (Template is null) return;
        declaration = GetTemplateChild("PART_Root") as Grid ?? throw new InvalidOperationException("Command Spotlight requires PART_Root.");
        foreach (var name in PaletteNames) palette[name] = GetTemplateChild("PART_" + name) as TextBlock ?? throw new InvalidOperationException("Command Spotlight requires PART_" + name);
        trigger = new(this); declaration.Children.Add(trigger); QueueRefresh();
    }
    private void DetachTemplate() { mountGeneration++; ready = false; DetachPresentation(); declaration?.Children.Clear(); declaration = null; trigger = null; palette.Clear(); }
    private void QueueRefresh()
    {
        if (queued) return; queued = true; var generation = mountGeneration;
        if (!DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => { queued = false; if (generation != mountGeneration) { if (IsLoaded) QueueRefresh(); return; } ready = true; Refresh(); })) throw new InvalidOperationException("Spotlight could not queue presentation.");
    }
    private void LayoutOwner(object? sender, object args) { if (!IsLoaded) return; ConnectRoot(); var width = XamlRoot?.Size.Width ?? ActualWidth; trigger?.SetNarrow(width < 640); UpdateInput(); }
    private void ConnectRoot()
    {
        var root = XamlRoot; var input = root?.Content;
        if (ReferenceEquals(root, observedRoot) && ReferenceEquals(input, keyboardRoot)) return;
        DetachPresentation(); DisconnectRoot(); observedRoot = root; keyboardRoot = input; host = BleeckerModalHost.Find(this);
        if (root is not null) { root.Changed += RootChanged; popupObservation = BleeckerPopupScope.Observe(root, UpdateInput); }
        if (host is not null) host.ScopeChanged += UpdateInput;
        keyboardRoot?.AddHandler(UIElement.KeyDownEvent, keyboardHandler, true);
    }
    private void DisconnectRoot()
    {
        keyboardRoot?.RemoveHandler(UIElement.KeyDownEvent, keyboardHandler); keyboardRoot = null;
        if (observedRoot is not null) observedRoot.Changed -= RootChanged; observedRoot = null;
        if (host is not null) host.ScopeChanged -= UpdateInput; host = null; popupObservation?.Dispose(); popupObservation = null;
    }
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) { ConnectRoot(); overlay?.InvalidateMeasure(); Refresh(); }
    private void WindowKeyDown(object sender, KeyRoutedEventArgs args)
    {
        var modifiers = (Down(VirtualKey.Shift) ? BleeckerCommandSpotlightKeyboard.Shift : 0)
            | (Down(VirtualKey.Control) ? BleeckerCommandSpotlightKeyboard.Control : 0)
            | (Down(VirtualKey.Menu) ? BleeckerCommandSpotlightKeyboard.Alt : 0)
            | ((InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.CapitalLock) & CoreVirtualKeyStates.Locked) != 0 ? BleeckerCommandSpotlightKeyboard.CapsLock : 0)
            | (Down(VirtualKey.LeftWindows) || Down(VirtualKey.RightWindows) ? BleeckerCommandSpotlightKeyboard.Meta : 0);
        if (ProcessWindowsHotkey((uint)args.OriginalKey, args.KeyStatus.ScanCode, args.KeyStatus.IsExtendedKey, modifiers, FocusManager.GetFocusedElement(XamlRoot) as DependencyObject)) args.Handled = true;
    }
    private static bool Down(VirtualKey key) => (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) != 0;
    private bool ProcessWindowsHotkey(uint key, uint scan, bool extended, int modifiers, DependencyObject? target)
    {
        if ((modifiers & (BleeckerCommandSpotlightKeyboard.Control | BleeckerCommandSpotlightKeyboard.Meta)) == 0) return false;
        var translated = BleeckerCommandSpotlightKeyboard.Read(key, scan, extended, modifiers);
        return ProcessHotkey(translated.Name, (translated.Modifiers & BleeckerCommandSpotlightKeyboard.Control) != 0,
            (translated.Modifiers & BleeckerCommandSpotlightKeyboard.Meta) != 0, target);
    }
    private bool ProcessHotkey(string key, bool control, bool meta, DependencyObject? target)
    {
        if ((!control && !meta) || BleeckerCommandSpotlightState<BleeckerCommandSpotlightAction>.Lower(key) != BleeckerCommandSpotlightState<BleeckerCommandSpotlightAction>.Lower(Hotkey)) return false;
        if (IsOpen) { if (!SurfaceInputAllowed) return false; Request(false); return true; }
        if (!DeclarationInputAllowed) return false;
        for (var node = target; node is not null; node = VisualTreeHelper.GetParent(node)) if (node is TextBox or PasswordBox or RichEditBox or ComboBox or AutoSuggestBox) return false;
        Request(true); return true;
    }
    private void StopActions()
    {
        if (collection is not null) collection.CollectionChanged -= CollectionChanged; collection = null;
        foreach (var action in observedActions) action.Changed -= ActionChanged; observedActions.Clear(); actionSource = null;
    }
    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) { dataDirty = true; Refresh(); }
    private void ActionChanged(object? sender, EventArgs args) => Refresh(); // Preserve source array/query memoization.
    private void CommandChanged(object? sender, EventArgs args) => QueueRefresh();
    private void Refresh()
    {
        if (rendering) { pending = true; return; } rendering = true;
        try
        {
            if (dataDirty || !ReferenceEquals(actionSource, Actions))
            {
                StopActions(); actionSource = Actions; actions = Actions.ToArray(); state.SetActions(actions, IsOpen); dataDirty = false;
                if (IsLoaded) { collection = Actions as INotifyCollectionChanged; if (collection is not null) collection.CollectionChanged += CollectionChanged; foreach (var action in actions) if (observedActions.Add(action)) action.Changed += ActionChanged; }
            }
            if (!ReferenceEquals(ActionCommand, observedCommand)) { if (observedCommand is not null) observedCommand.CanExecuteChanged -= CommandChanged; observedCommand = IsLoaded ? ActionCommand : null; if (observedCommand is not null) observedCommand.CanExecuteChanged += CommandChanged; }
            if (!ready || !IsLoaded || declaration is null) return;
            ConnectRoot(); state.Reconcile(IsOpen); trigger?.Update(); UpdatePresence(); UpdatePresentation(); body?.Update(); UpdateInput(); portal?.InvalidateMeasure(); overlay?.InvalidateMeasure();
        }
        finally { rendering = false; }
        if (pending) { pending = false; Refresh(); }
    }
    private void UpdateInput() { if (trigger is not null) trigger.IsEnabled = DeclarationInputAllowed; body?.UpdateInput(); }
    internal SolidColorBrush Brush(string key) => palette[key].Foreground as SolidColorBrush ?? throw new InvalidOperationException("Spotlight requires semantic brush " + key);
    protected override AutomationPeer OnCreateAutomationPeer() => new DeclarationPeer(this);
    private sealed class DeclarationPeer(BleeckerCommandSpotlight owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerCommandSpotlight);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }
}
