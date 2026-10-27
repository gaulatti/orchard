using System.Runtime.CompilerServices;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

/// <summary>A nonmodal, caller-controlled or internally controlled anchored content surface.</summary>
public sealed partial class BleeckerPopover : ContentControl
{
    private static readonly ConditionalWeakTable<XamlRoot, List<BleeckerPopover>> openPopovers = new();
    private Grid? root;
    private BleeckerContentText? triggerSlot, bodySlot;
    private BleeckerOverlaySurface? surface;
    private FocusHost? focusHost;
    private Popup? popup;
    private BleeckerModalHost? modalHost;
    private BleeckerModalLayerHostCore.Branch? modalBranch;
    private bool presented;
    private BleeckerPopupScope.Registration? popupScope;
    private XamlRoot? observedRoot;
    private UIElement? inputRoot;
    private BleeckerPopoverModel? model;
    private readonly HashSet<FrameworkElement> triggers = [];
    private (object? Value, DataTemplate? Template, DataTemplateSelector? Selector)? triggerValue, bodyValue;
    private bool rendering, renderPending, mountPending, positioning, inputsConnected, outsideInteraction, pointerDismiss, focusPending, focusing, lastOpen;
    private int generation, triggerGeneration;
    private bool presentationQueued, naturalDirty = true;
    private int focusAttempts;
    private Size naturalContent, naturalSurface;

    public BleeckerPopover()
    {
        DefaultStyleKey = typeof(BleeckerPopover); IsTabStop = false; UseLayoutRounding = false;
        Loaded += LoadedOwner; Unloaded += UnloadedOwner;
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); else { triggerGeneration++; ClosePresentation(false); } });
        foreach (var property in new[] { ContentProperty, ContentTemplateProperty, ContentTemplateSelectorProperty, IsEnabledProperty, FlowDirectionProperty, IsTextScaleFactorEnabledProperty, LanguageProperty, FontFamilyProperty, FontSizeProperty, FontWeightProperty, ForegroundProperty, PaddingProperty, BorderThicknessProperty, HorizontalContentAlignmentProperty, VerticalContentAlignmentProperty, AutomationProperties.NameProperty })
            RegisterPropertyChangedCallback(property, (_, _) => Render());
    }
    private static DependencyProperty Property(string name, Type type, object? value) => DependencyProperty.Register(name, type, typeof(BleeckerPopover), new PropertyMetadata(value, (owner, _) => ((BleeckerPopover)owner).Render()));
    public static readonly DependencyProperty TriggerProperty = Property(nameof(Trigger), typeof(object), null);
    public static readonly DependencyProperty TriggerTemplateProperty = Property(nameof(TriggerTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty TriggerTemplateSelectorProperty = Property(nameof(TriggerTemplateSelector), typeof(DataTemplateSelector), null);
    public static readonly DependencyProperty OpenProperty = Property(nameof(Open), typeof(object), null);
    public static readonly DependencyProperty DefaultOpenProperty = Property(nameof(DefaultOpen), typeof(bool), false);
    public static readonly DependencyProperty SideProperty = Property(nameof(Side), typeof(BleeckerPopoverSide), BleeckerPopoverSide.Bottom);
    public static readonly DependencyProperty AlignProperty = Property(nameof(Align), typeof(BleeckerPopoverAlign), BleeckerPopoverAlign.Center);
    public static readonly DependencyProperty SideOffsetProperty = Property(nameof(SideOffset), typeof(double), 4d);
    public static readonly DependencyProperty SurfaceStyleProperty = Property(nameof(SurfaceStyle), typeof(Style), null);
    public object? Trigger { get => GetValue(TriggerProperty); set => SetValue(TriggerProperty, value); }
    public DataTemplate? TriggerTemplate { get => (DataTemplate?)GetValue(TriggerTemplateProperty); set => SetValue(TriggerTemplateProperty, value); }
    public DataTemplateSelector? TriggerTemplateSelector { get => (DataTemplateSelector?)GetValue(TriggerTemplateSelectorProperty); set => SetValue(TriggerTemplateSelectorProperty, value); }
    public bool? Open { get => GetValue(OpenProperty) is bool value ? value : GetValue(OpenProperty) is null ? null : throw new ArgumentException("Popover Open requires Boolean or null."); set => SetValue(OpenProperty, value); }
    public bool DefaultOpen { get => (bool)GetValue(DefaultOpenProperty); set => SetValue(DefaultOpenProperty, value); }
    public BleeckerPopoverSide Side { get => (BleeckerPopoverSide)GetValue(SideProperty); set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(SideProperty, value); } }
    public BleeckerPopoverAlign Align { get => (BleeckerPopoverAlign)GetValue(AlignProperty); set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(AlignProperty, value); } }
    public double SideOffset { get => (double)GetValue(SideOffsetProperty); set { if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(SideOffsetProperty, value); } }
    public Style? SurfaceStyle { get => (Style?)GetValue(SurfaceStyleProperty); set => SetValue(SurfaceStyleProperty, value); }
    public bool IsOpen => Open ?? model?.Effective(null) ?? DefaultOpen;
    public BleeckerPopoverSide ActualSide { get; private set; } = BleeckerPopoverSide.Bottom;
    public BleeckerPopoverAlign ActualAlign { get; private set; } = BleeckerPopoverAlign.Center;
    public event EventHandler<bool>? OpenRequested;
    public event EventHandler<CancelEventArgs>? OpenChanging;
    public event EventHandler? Opened;
    public event EventHandler? Closed;
    public void Toggle() => Request(!IsOpen);
    public void Close() => Request(false);

    private void LoadedOwner(object sender, RoutedEventArgs args) { model ??= new(DefaultOpen); LayoutUpdated += LayoutOwner; ObserveRoot(); Render(); }
    private void UnloadedOwner(object sender, RoutedEventArgs args) { triggerGeneration++; LayoutUpdated -= LayoutOwner; ClosePresentation(false); ObserveHost(null); DisconnectRoot(); StopTriggers(); }
    private void ObserveHost(BleeckerModalHost? next)
    {
        if (ReferenceEquals(modalHost, next)) return;
        ClosePresentation(false);
        if (modalHost is not null) modalHost.ScopeChanged -= ModalScopeChanged;
        modalHost = next;
        if (modalHost is not null) modalHost.ScopeChanged += ModalScopeChanged;
    }
    private void ModalScopeChanged() => QueuePresentation();
    private bool PresentationOpen => modalBranch is { } branch ? modalHost?.IsBranchAttached(branch) == true : popup?.IsOpen == true;
    private bool InteractionAllowed => modalBranch is not { } branch || modalHost?.IsBranchAttached(branch) == true && branch.Frame.IsHitTestVisible;
    private void ObserveRoot()
    {
        if (ReferenceEquals(observedRoot, XamlRoot)) return;
        ClosePresentation(false); DisconnectRoot(); observedRoot = XamlRoot;
        if (observedRoot is not null) observedRoot.Changed += RootChanged;
    }
    private void DisconnectRoot() { if (observedRoot is not null) observedRoot.Changed -= RootChanged; observedRoot = null; }
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => QueuePresentation();
    private void LayoutOwner(object? sender, object args)
    {
        if (!IsLoaded) return; ObserveRoot(); ReconcileTriggers();
        if (bodySlot is not null && naturalContent != new Size(bodySlot.NaturalWidth, bodySlot.NaturalHeight)) naturalDirty = true;
        if (IsOpen && IsEnabled) QueuePresentation(); else ClosePresentation(IsLoaded && !outsideInteraction);
    }

    protected override void OnApplyTemplate()
    {
        DetachTemplate(); base.OnApplyTemplate(); if (Template is null) return;
        root = GetTemplateChild("PART_Layout") as Grid ?? throw new InvalidOperationException("Popover requires a Grid named PART_Layout.");
        root.FlowDirection = FlowDirection.LeftToRight;
        triggerSlot = new("PopoverTrigger", "BleeckerPrimaryFontFamily", 16, 24, "BleeckerTextPrimaryBrush");
        bodySlot = new("PopoverContent", "BleeckerPrimaryFontFamily", 16, 24, "BleeckerTextPrimaryBrush");
        surface = new() { Name = "PopoverSurface", Content = bodySlot, UseLayoutRounding = false };
        surface.Loaded += SurfaceLoaded;
        foreach (var property in new[] { nameof(Background), nameof(BorderBrush), nameof(BorderThickness), nameof(CornerRadius), nameof(Padding), nameof(FontFamily), nameof(FontSize), nameof(FontWeight), nameof(Foreground), nameof(HorizontalContentAlignment), nameof(VerticalContentAlignment) })
            surface.SetBinding(PropertyForSurface(property), new Binding { Source = this, Path = new PropertyPath(property) });
        focusHost = new() { Name = "PopoverFocusHost", Content = surface, IsTabStop = false, UseSystemFocusVisuals = false, TabFocusNavigation = KeyboardNavigationMode.Cycle, UseLayoutRounding = false };
        focusHost.KeyDown += PopupKey; focusHost.AddHandler(PointerPressedEvent, new PointerEventHandler(PopupPointer), true);
        popup = new() { Name = "PopoverPopup", Child = focusHost, ShouldConstrainToRootBounds = true, IsLightDismissEnabled = false, FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false };
        popup.Opened += PopupOpened; popup.Closed += PopupClosed;
        root.Children.Add(triggerSlot); root.Children.Add(popup); triggerSlot.Tapped += TriggerTapped;
        // Let the native template finish retiring its old popup before the
        // same original input controls join the replacement popup root.
        var mountedRoot = root; mountPending = true;
        if (!DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => { if (ReferenceEquals(root, mountedRoot)) { mountPending = false; Render(); } })) throw new InvalidOperationException("Popover could not mount its template.");
    }
    private static DependencyProperty PropertyForSurface(string name) => name switch
    {
        nameof(Background) => Control.BackgroundProperty, nameof(BorderBrush) => Control.BorderBrushProperty, nameof(BorderThickness) => Control.BorderThicknessProperty,
        nameof(CornerRadius) => Control.CornerRadiusProperty, nameof(Padding) => Control.PaddingProperty, nameof(FontFamily) => Control.FontFamilyProperty,
        nameof(FontSize) => Control.FontSizeProperty, nameof(FontWeight) => Control.FontWeightProperty, nameof(Foreground) => Control.ForegroundProperty,
        nameof(HorizontalContentAlignment) => Control.HorizontalContentAlignmentProperty, nameof(VerticalContentAlignment) => Control.VerticalContentAlignmentProperty,
        _ => throw new ArgumentException("Unsupported Popover surface property.", nameof(name))
    };
    private void SetSlot(BleeckerContentText slot, object? value, DataTemplate? template, DataTemplateSelector? selector, ref (object? Value, DataTemplate? Template, DataTemplateSelector? Selector)? previous)
    {
        if (previous is { } old && ReferenceEquals(old.Value, value) && ReferenceEquals(old.Template, template) && ReferenceEquals(old.Selector, selector)) return;
        slot.Set(value, template, selector: selector); previous = (value, template, selector); if (ReferenceEquals(slot, triggerSlot)) triggerGeneration++;
    }
    private readonly Style defaultSurfaceStyle = CreateSurfaceStyle();
    private static Style CreateSurfaceStyle() { var style = new Style(typeof(BleeckerOverlaySurface)); style.Setters.Add(new Setter(MinWidthProperty, 224d)); return style; }
    private void Render()
    {
        if (mountPending) return;
        if (rendering) { renderPending = true; return; }
        _ = Open;
        if (!Enum.IsDefined(Side) || !Enum.IsDefined(Align) || !double.IsFinite(SideOffset)) throw new ArgumentException("Invalid Popover placement.");
        if (root is null || triggerSlot is null || bodySlot is null || surface is null || popup is null) return;
        rendering = true;
        try
        {
            SetSlot(triggerSlot, Trigger, TriggerTemplate, TriggerTemplateSelector, ref triggerValue);
            SetSlot(bodySlot, Content, ContentTemplate, ContentTemplateSelector, ref bodyValue);
            foreach (var slot in new[] { triggerSlot, bodySlot })
            {
                if (slot.FlowDirection != FlowDirection) slot.FlowDirection = FlowDirection;
                if (slot.Text.FontFamily.Source != FontFamily.Source) slot.Text.FontFamily = FontFamily;
                if (slot.Text.FontSize != FontSize) slot.Text.FontSize = FontSize;
                if (slot.Text.FontWeight != FontWeight) slot.Text.FontWeight = FontWeight;
                if (!ReferenceEquals(slot.Text.Foreground, Foreground)) slot.Text.Foreground = Foreground;
                if (slot.Text.LineHeight != FontSize * 1.5) slot.Text.LineHeight = FontSize * 1.5;
                if (slot.Text.IsTextScaleFactorEnabled != IsTextScaleFactorEnabled) slot.Text.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled;
                if (slot.Text.Language != Language) slot.Text.Language = Language;
            }
            if (!ReferenceEquals(surface.Style, SurfaceStyle ?? defaultSurfaceStyle)) surface.Style = SurfaceStyle ?? defaultSurfaceStyle;
            if (surface.FlowDirection != FlowDirection) surface.FlowDirection = FlowDirection;
            naturalDirty = true;
            AutomationProperties.SetName(focusHost!, AutomationProperties.GetName(this));
            ReconcileTriggers();
            if (IsLoaded && IsEnabled && IsOpen && XamlRoot is not null)
            {
                QueuePresentation();
            }
            else ClosePresentation(IsLoaded && !outsideInteraction);
            var next = IsOpen;
            if (lastOpen != next) { (FrameworkElementAutomationPeer.FromElement(this) as PopoverPeer)?.Changed(lastOpen, next); lastOpen = next; }
        }
        finally { rendering = false; }
        if (renderPending) { renderPending = false; Render(); }
    }
    private void Request(bool next)
    {
        if (!IsLoaded || !IsEnabled || model is null || root is null) return;
        if (next && TriggerRoot() is null) throw new InvalidOperationException("An opening Popover requires a rendered trigger.");
        if (model.Effective(Open) == next) return;
        var changing = new CancelEventArgs(); OpenChanging?.Invoke(this, changing); if (changing.Cancel) return;
        if (!model.Request(Open, next)) return;
        OpenRequested?.Invoke(this, next); Render();
    }
    private FrameworkElement? TriggerRoot()
    {
        if (triggerSlot is null) return null;
        if (Trigger is FrameworkElement direct && TriggerTemplate is null && TriggerTemplateSelector is null) return Within(direct, triggerSlot) ? direct : null;
        var presenter = Elements(triggerSlot).OfType<ContentPresenter>().FirstOrDefault();
        return presenter is not null && VisualTreeHelper.GetChildrenCount(presenter) > 0 ? VisualTreeHelper.GetChild(presenter, 0) as FrameworkElement : null;
    }
    private void QueuePresentation()
    {
        if (presentationQueued || mountPending || !IsLoaded || root is null) return;
        // A caller control measured before its containing portal mounts can
        // receive its native root after Loaded. Reconcile at the presentation
        // boundary instead of relying on a later LayoutUpdated notification.
        ObserveRoot();
        if (observedRoot is null) return;
        var currentRoot = root; presentationQueued = true;
        if (!DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            presentationQueued = false;
            if (!ReferenceEquals(currentRoot, root) || !IsLoaded || popup is null) return;
            if (!IsOpen || !IsEnabled) { ClosePresentation(!outsideInteraction); return; }
            if (TriggerRoot() is null) return;
            ObserveHost(BleeckerModalHost.Find(this));
            if (modalBranch is { } retired && modalHost?.IsBranchAttached(retired) != true) ClosePresentation(false);
            if (modalHost is not null && modalBranch is null)
            {
                popup.Child = null;
                modalBranch = modalHost.AttachPopupBranch(this, focusHost!, new Point());
            }
            else if (modalHost is null && !ReferenceEquals(popup.Child, focusHost)) popup.Child = focusHost;
            UpdatePosition();
            if (!presented)
            {
                outsideInteraction = pointerDismiss = false; focusPending = true; focusAttempts = 0; generation++;
                if (modalBranch is not null) PresentationOpened(); else popup.IsOpen = true;
            }
            TryInitialFocus();
        })) throw new InvalidOperationException("Popover could not schedule its native presentation.");
    }
    private void UpdatePosition()
    {
        if (mountPending || positioning || !IsLoaded || root is null || surface is null || focusHost is null || popup is null || observedRoot?.Content is not UIElement input || TriggerRoot() is not { } anchor) return;
        positioning = true;
        try
        {
            if (popup.RequestedTheme != ActualTheme) { popup.RequestedTheme = ActualTheme; naturalDirty = true; }
            if (focusHost.RequestedTheme != ActualTheme) { focusHost.RequestedTheme = ActualTheme; naturalDirty = true; }
            if (naturalDirty)
            {
                surface.ApplyTemplate();
                surface.InvalidateMeasure();
                surface.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                naturalSurface = surface.DesiredSize; naturalContent = new(bodySlot!.NaturalWidth, bodySlot.NaturalHeight); naturalDirty = false;
            }
            var size = naturalSurface;
            if (double.IsNaN(focusHost.Width) || Math.Abs(focusHost.Width - size.Width) > .000001) focusHost.Width = size.Width;
            if (double.IsNaN(focusHost.Height) || Math.Abs(focusHost.Height - size.Height) > .000001) focusHost.Height = size.Height;
            var bounds = anchor.TransformToVisual(input).TransformBounds(new Rect(0, 0, anchor.ActualWidth, anchor.ActualHeight));
            var placement = BleeckerPopoverModel.Place(new(bounds.X, bounds.Y, bounds.Width, bounds.Height), size.Width, size.Height, observedRoot.Size.Width, observedRoot.Size.Height, Side, Align, SideOffset, FlowDirection == FlowDirection.RightToLeft, observedRoot.RasterizationScale);
            var origin = root.TransformToVisual(input).TransformPoint(new Point()); ActualSide = placement.Side; ActualAlign = placement.Align;
            if (modalBranch is not null) modalHost!.PositionBranch(modalBranch, new(placement.X, placement.Y));
            else
            {
                if (Math.Abs(popup.HorizontalOffset - (placement.X - origin.X)) > .000001) popup.HorizontalOffset = placement.X - origin.X;
                if (Math.Abs(popup.VerticalOffset - (placement.Y - origin.Y)) > .000001) popup.VerticalOffset = placement.Y - origin.Y;
            }
        }
        finally { positioning = false; }
    }
    private void PopupOpened(object? sender, object args)
    {
        if (!IsLoaded || observedRoot is null || !ReferenceEquals(sender, popup) || popup?.IsOpen != true) return;
        PresentationOpened();
    }
    private void PresentationOpened()
    {
        if (presented || observedRoot is null) return;
        presented = true;
        popupScope = BleeckerPopupScope.Attach(this, focusHost!, false, () => InteractionAllowed);
        naturalDirty = true;
        var list = openPopovers.GetOrCreateValue(observedRoot); if (!list.Contains(this)) list.Add(this); ConnectInputs(); QueuePresentation(); TryInitialFocus(); Opened?.Invoke(this, EventArgs.Empty);
    }
    private void SurfaceLoaded(object sender, RoutedEventArgs args) { if (ReferenceEquals(sender, surface)) { naturalDirty = true; QueuePresentation(); } }
    private void PopupClosed(object? sender, object args) { if (!ReferenceEquals(sender, popup) || popup?.IsOpen == true || modalBranch is not null) return; PresentationClosed(); }
    private void PresentationClosed()
    {
        if (!presented) return;
        presented = false; popupScope?.Dispose(); popupScope = null; RemoveOpenRegistration(); DisconnectInputs(); generation++; focusPending = false; Closed?.Invoke(this, EventArgs.Empty);
    }
    private void ClosePresentation(bool restore)
    {
        if (!presented && modalBranch is null && popup?.IsOpen != true) return;
        var trigger = restore && InteractionAllowed && observedRoot?.IsHostVisible == true ? TriggerRoot() : null;
        var target = trigger is null ? null : FocusManager.FindFirstFocusableElement(trigger) ?? trigger;
        if (modalBranch is { } branch)
        {
            modalBranch = null;
            if (modalHost?.IsBranchAttached(branch) == true) modalHost.DetachBranch(branch);
        }
        if (popup is not null) popup.IsOpen = false;
        PresentationClosed();
        if (target is not null && modalHost is not null && !modalHost.AllowsFocusTarget(target)) target = null;
        if (target is not null) _ = FocusManager.TryFocusAsync(target, FocusState.Programmatic);
    }
    private void RemoveOpenRegistration() { if (observedRoot is not null && openPopovers.TryGetValue(observedRoot, out var list)) list.Remove(this); }
    private bool Topmost => observedRoot is not null && openPopovers.TryGetValue(observedRoot, out var list) && ReferenceEquals(list.LastOrDefault(owner => owner.PresentationOpen && owner.InteractionAllowed), this);
    private static bool Within(DependencyObject? value, DependencyObject? ancestor)
    {
        if (ancestor is null) return false;
        for (var node = value; node is not null; node = node is Microsoft.UI.Xaml.Documents.TextElement text ? text.ElementStart.Parent : VisualTreeHelper.GetParent(node)) if (ReferenceEquals(node, ancestor)) return true;
        return false;
    }
    private bool Contains(DependencyObject? value)
    {
        if (Within(value, triggerSlot) || Within(value, focusHost)) return true;
        if (observedRoot is not null && bodySlot is not null && BleeckerPopupScope.Contains(observedRoot, bodySlot, value)) return true;
        return observedRoot is not null && openPopovers.TryGetValue(observedRoot, out var list) && list.Any(child => !ReferenceEquals(child, this) && Within(child, bodySlot) && child.Contains(value));
    }
    private void ConnectInputs()
    {
        if (inputsConnected || observedRoot?.Content is not UIElement element) return;
        inputRoot = element; inputRoot.AddHandler(PointerPressedEvent, new PointerEventHandler(RootPointer), true); inputRoot.AddHandler(KeyDownEvent, new KeyEventHandler(PopupKey), true);
        FocusManager.GotFocus += FocusChanged; inputsConnected = true;
    }
    private void DisconnectInputs()
    {
        if (!inputsConnected) return;
        inputRoot?.RemoveHandler(PointerPressedEvent, new PointerEventHandler(RootPointer)); inputRoot?.RemoveHandler(KeyDownEvent, new KeyEventHandler(PopupKey)); inputRoot = null;
        FocusManager.GotFocus -= FocusChanged; inputsConnected = false;
    }
    private void RootPointer(object sender, PointerRoutedEventArgs args)
    {
        if (!PresentationOpen || !InteractionAllowed || Contains(args.OriginalSource as DependencyObject)) return;
        outsideInteraction = pointerDismiss = true; Request(false);
    }
    private void PopupPointer(object sender, PointerRoutedEventArgs args)
    {
        if (modalBranch is not null) return; // The shared plane already routes this event to the root listeners.
        if (observedRoot is not null && openPopovers.TryGetValue(observedRoot, out var list)) foreach (var owner in list.ToArray()) owner.RootPointer(sender, args);
    }
    private void FocusChanged(object? sender, FocusManagerGotFocusEventArgs args)
    {
        if (!PresentationOpen || !InteractionAllowed || observedRoot is null || pointerDismiss || !ReferenceEquals(args.NewFocusedElement, FocusManager.GetFocusedElement(observedRoot)) || Contains(args.NewFocusedElement)) return;
        outsideInteraction = true; Request(false);
    }
    private void PopupKey(object sender, KeyRoutedEventArgs args)
    {
        if (args.Handled || !PresentationOpen || !InteractionAllowed || !Topmost) return;
        if (args.Key == Windows.System.VirtualKey.Escape) { if (observedRoot is null || !BleeckerTooltip.DismissForEscape(observedRoot)) Request(false); args.Handled = true; return; }
        if (args.Key != Windows.System.VirtualKey.Tab || bodySlot is null || observedRoot is null || observedRoot.IsHostVisible == false) return;
        var first = FocusManager.FindFirstFocusableElement(bodySlot); var last = FocusManager.FindLastFocusableElement(bodySlot);
        if (first is null) { args.Handled = true; return; }
        var shift = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift) & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;
        var focused = FocusManager.GetFocusedElement(observedRoot); var target = shift && ReferenceEquals(focused, first) ? last : !shift && ReferenceEquals(focused, last) ? first : ReferenceEquals(focused, focusHost) ? shift ? last : first : null;
        if (target is not null) { _ = FocusManager.TryFocusAsync(target, FocusState.Keyboard); args.Handled = true; }
    }
    private async void TryInitialFocus()
    {
        if (!focusPending || focusing || !PresentationOpen || !InteractionAllowed || observedRoot?.IsHostVisible != true || bodySlot is null || focusHost?.IsLoaded != true) return;
        focusPending = false;
        var focused = FocusManager.GetFocusedElement(observedRoot) as DependencyObject;
        if (Contains(focused) && !Within(focused, triggerSlot)) return;
        var target = FocusManager.FindFirstFocusableElement(bodySlot) ?? focusHost; var current = generation; focusing = true;
        var retry = false;
        try
        {
            if (current == generation && PresentationOpen && InteractionAllowed)
            {
                var result = await FocusManager.TryFocusAsync(target, FocusState.Programmatic);
                retry = !result.Succeeded && current == generation && PresentationOpen && InteractionAllowed && ++focusAttempts < 3;
            }
        }
        finally
        {
            focusing = false;
            if (retry)
            {
                focusPending = true;
                if (!DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, TryInitialFocus))
                    throw new InvalidOperationException("Popover could not retry native content focus.");
            }
        }
    }

    private void ReconcileTriggers()
    {
        if (triggerSlot is null || !IsLoaded) return;
        IEnumerable<FrameworkElement> Buttons(DependencyObject node)
        {
            if (node is ButtonBase or BleeckerButton) { yield return (FrameworkElement)node; yield break; }
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var button in Buttons(VisualTreeHelper.GetChild(node, i))) yield return button;
        }
        var next = Buttons(triggerSlot).ToHashSet();
        foreach (var old in triggers.Where(button => !next.Contains(button)).ToArray()) { Unhook(old); triggers.Remove(old); }
        foreach (var button in next.Where(button => !triggers.Contains(button))) { if (button is ButtonBase native) native.Click += TriggerClick; else ((BleeckerButton)button).Click += TriggerClick; triggers.Add(button); }
    }
    private void Unhook(FrameworkElement button) { if (button is ButtonBase native) native.Click -= TriggerClick; else ((BleeckerButton)button).Click -= TriggerClick; }
    private void StopTriggers() { foreach (var button in triggers) Unhook(button); triggers.Clear(); }
    private void TriggerClick(object sender, RoutedEventArgs args)
    {
        if (sender is not FrameworkElement element || !triggers.Contains(element) || !Within(element, triggerSlot) || element is Control { IsEnabled: false } || element is BleeckerButton { IsLoading: true }) return;
        var current = triggerGeneration;
        if (!DispatcherQueue.TryEnqueue(() => { if (current == triggerGeneration && IsLoaded && IsEnabled && triggers.Contains(element) && Within(element, triggerSlot) && element is not Control { IsEnabled: false } && element is not BleeckerButton { IsLoading: true }) Toggle(); })) throw new InvalidOperationException("Popover could not dispatch its trigger request.");
    }
    private void TriggerTapped(object sender, TappedRoutedEventArgs args)
    {
        if (args.Handled || !Within(args.OriginalSource as DependencyObject, triggerSlot)) return;
        for (var node = args.OriginalSource as DependencyObject; node is not null && !ReferenceEquals(node, triggerSlot); node = VisualTreeHelper.GetParent(node)) if (node is ButtonBase or BleeckerButton) return;
        Toggle();
    }
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    {
        if (node is FrameworkElement element) yield return element;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Elements(VisualTreeHelper.GetChild(node, i))) yield return child;
    }
    private void DetachTemplate()
    {
        mountPending = false; naturalDirty = true; triggerGeneration++; ClosePresentation(false); DisconnectInputs(); RemoveOpenRegistration(); StopTriggers();
        if (popup is not null) { popup.Opened -= PopupOpened; popup.Closed -= PopupClosed; popup.Child = null; }
        if (triggerSlot is not null) triggerSlot.Tapped -= TriggerTapped;
        if (focusHost is not null) { focusHost.KeyDown -= PopupKey; focusHost.RemoveHandler(PointerPressedEvent, new PointerEventHandler(PopupPointer)); focusHost.Content = null; }
        if (surface is not null) { surface.Loaded -= SurfaceLoaded; surface.Content = null; }
        triggerSlot?.Release(); bodySlot?.Release(); root?.Children.Clear(); root = null; triggerSlot = bodySlot = null; surface = null; focusHost = null; popup = null; triggerValue = bodyValue = null; generation++;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new PopoverPeer(this);
    private sealed class PopoverPeer(BleeckerPopover owner) : FrameworkElementAutomationPeer(owner), IExpandCollapseProvider
    {
        protected override string GetClassNameCore() => nameof(BleeckerPopover);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetNameCore() => string.IsNullOrEmpty(base.GetNameCore()) ? owner.triggerSlot?.AccessibleName() ?? "" : base.GetNameCore();
        protected override object GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.ExpandCollapse ? this : base.GetPatternCore(pattern);
        public ExpandCollapseState ExpandCollapseState => owner.IsOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        public void Expand() { if (!owner.IsEnabled) throw new ElementNotEnabledException(); owner.Request(true); }
        public void Collapse() { if (!owner.IsEnabled) throw new ElementNotEnabledException(); owner.Request(false); }
        internal void Changed(bool oldValue, bool newValue) => RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed, newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
    }
    private sealed class FocusHost : ContentControl
    {
        internal FocusHost() { Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ContentControl'><ContentPresenter Content='{TemplateBinding Content}' HorizontalAlignment='Stretch' VerticalAlignment='Stretch'/></ControlTemplate>"); }
        protected override AutomationPeer OnCreateAutomationPeer() => new ContentPeer(this);
        private sealed class ContentPeer(FocusHost owner) : FrameworkElementAutomationPeer(owner)
        {
            protected override string GetClassNameCore() => "BleeckerPopoverContent";
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
        }
    }
}
