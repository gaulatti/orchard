using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>Window-root notification stacking with original caller content and actions.</summary>
public sealed partial class BleeckerSonner : Control
{
    private Grid? declaration;
    private Canvas? plane;
    private ToastStack? stack;
    private Popup? popup;
    private XamlRoot? root;
    private BleeckerModalHost? modalHost;
    private BleeckerModalLayerHostCore.Branch? branch;
    private BleeckerPopupScope.Registration? scope;
    private IDisposable? scopeObserver;
    private readonly List<ToastEntry> entries = [];
    private readonly HashSet<ToastEntry> pendingFrames = [];
    private readonly Dictionary<string, TextBlock> probes = [];
    private readonly List<(TextBlock Element, long Token)> probeTokens = [];
    private readonly List<(FrameworkElement Element, long Token)> declarationVisibility = [];
    private readonly UISettings settings = new();
    private bool frameSubscribed, positioning, themePending;
    internal double RemScale => FontSize / 16;
    internal bool MotionEnabled => settings.AnimationsEnabled;

    public BleeckerSonner()
    {
        DefaultStyleKey = typeof(BleeckerSonner); IsTabStop = false; UseLayoutRounding = false;
        Loaded += (_, _) => { ObserveVisibility(); BleeckerToast.Register(this); QueuePresentation(); };
        Unloaded += (_, _) => { BleeckerToast.Unregister(); ClearEntries(); RetirePresentation(); DisconnectVisibility(); };
        ActualThemeChanged += (_, _) => QueueThemeRefresh();
        LayoutUpdated += (_, _) => PositionStack();
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); });
        foreach (var property in new[] { FontSizeProperty, FlowDirectionProperty, LanguageProperty, IsTextScaleFactorEnabledProperty })
            RegisterPropertyChangedCallback(property, (_, _) => RefreshPresentation());
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => ScopeChanged());
    }

    public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(nameof(Position), typeof(BleeckerSonnerPosition), typeof(BleeckerSonner), new PropertyMetadata(BleeckerSonnerPosition.BottomRight, (owner, args) =>
    { if (!Enum.IsDefined((BleeckerSonnerPosition)args.NewValue)) throw new ArgumentOutOfRangeException(nameof(Position)); ((BleeckerSonner)owner).PositionStack(); }));
    public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(nameof(Duration), typeof(double), typeof(BleeckerSonner), new PropertyMetadata(4000d, (owner, _) =>
    { var host = (BleeckerSonner)owner; if (host.IsLoaded) BleeckerToast.Register(host); }));
    private static DependencyProperty CreateStyleProperty(string name) => DependencyProperty.Register(name, typeof(Style), typeof(BleeckerSonner), new PropertyMetadata(null, (owner, _) => ((BleeckerSonner)owner).RefreshPresentation()));
    public static readonly DependencyProperty SurfaceStyleProperty = CreateStyleProperty(nameof(SurfaceStyle));
    public static readonly DependencyProperty DismissButtonStyleProperty = CreateStyleProperty(nameof(DismissButtonStyle));
    public static readonly DependencyProperty LinkActionStyleProperty = CreateStyleProperty(nameof(LinkActionStyle));
    public static readonly DependencyProperty ButtonActionStyleProperty = CreateStyleProperty(nameof(ButtonActionStyle));
    public BleeckerSonnerPosition Position { get => (BleeckerSonnerPosition)GetValue(PositionProperty); set => SetValue(PositionProperty, value); }
    public double Duration { get => (double)GetValue(DurationProperty); set => SetValue(DurationProperty, value); }
    public Style? SurfaceStyle { get => (Style?)GetValue(SurfaceStyleProperty); set => SetValue(SurfaceStyleProperty, value); }
    public Style? DismissButtonStyle { get => (Style?)GetValue(DismissButtonStyleProperty); set => SetValue(DismissButtonStyleProperty, value); }
    public Style? LinkActionStyle { get => (Style?)GetValue(LinkActionStyleProperty); set => SetValue(LinkActionStyleProperty, value); }
    public Style? ButtonActionStyle { get => (Style?)GetValue(ButtonActionStyleProperty); set => SetValue(ButtonActionStyleProperty, value); }

    /// <summary>Adds caller data to this loaded host; the returned ID is opaque.</summary>
    public string Show(BleeckerSonnerToast toast)
    {
        RequireThread(); ArgumentNullException.ThrowIfNull(toast);
        if (!IsLoaded) throw new InvalidOperationException("Sonner.Show requires a loaded host.");
        if (!Enum.IsDefined(toast.Variant)) throw new ArgumentOutOfRangeException(nameof(toast.Variant));
        ApplyTemplate(); EnsurePresentation();
        var entry = new ToastEntry(this, Guid.NewGuid().ToString("N"), toast, toast.HasDuration ? toast.Duration : Duration);
        entries.Add(entry); stack!.Children.Add(entry.Surface); entry.Start(); QueueFrame(entry); stack.InvalidateMeasure(); PositionStack();
        return entry.Id;
    }
    /// <summary>Removes the matching ID immediately, preserving the source imperative contract.</summary>
    public void Dismiss(string id)
    { RequireThread(); ArgumentNullException.ThrowIfNull(id); if (entries.FirstOrDefault(e => e.Id == id) is { } entry) RemoveEntry(entry); }
    private void RequireThread() { if (!DispatcherQueue.HasThreadAccess) throw new InvalidOperationException("Sonner APIs must run on the host's UI dispatcher."); }

    protected override void OnApplyTemplate()
    {
        DetachTemplate(); base.OnApplyTemplate();
        declaration = GetTemplateChild("PART_Declaration") as Grid ?? throw new InvalidOperationException("Sonner requires a Grid PART_Declaration.");
        if (IsLoaded) { ObserveVisibility(); QueuePresentation(); }
    }
    private void DetachTemplate()
    {
        ClearEntries(); RetirePresentation();
        foreach (var (element, token) in probeTokens) element.UnregisterPropertyChangedCallback(TextBlock.ForegroundProperty, token);
        probeTokens.Clear(); probes.Clear(); declaration?.Children.Clear(); declaration = null;
    }
    private Brush Ink(string key)
    {
        if (probes.TryGetValue(key, out var existing)) return existing.Foreground;
        if (declaration is null) throw new InvalidOperationException("Sonner's template has not been applied.");
        var probe = (TextBlock)XamlReader.Load("<TextBlock xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Foreground='{ThemeResource " + key + "}' Visibility='Collapsed' AutomationProperties.AccessibilityView='Raw'/>");
        probes.Add(key, probe); declaration.Children.Add(probe);
        probeTokens.Add((probe, probe.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty, (_, _) => { foreach (var entry in entries.ToArray()) entry.Paint(); })));
        return probe.Foreground;
    }
    private void QueueFrame(ToastEntry entry)
    { pendingFrames.Add(entry); if (!frameSubscribed) { frameSubscribed = true; CompositionTarget.Rendering += InitialFrame; } }
    private void InitialFrame(object? sender, object args)
    {
        CompositionTarget.Rendering -= InitialFrame; frameSubscribed = false;
        var pending = pendingFrames.ToArray(); pendingFrames.Clear();
        foreach (var entry in pending) if (entries.Contains(entry)) entry.Frame();
    }
    private void RemoveEntry(ToastEntry entry)
    {
        if (!entries.Remove(entry)) return;
        pendingFrames.Remove(entry); if (pendingFrames.Count == 0 && frameSubscribed) { CompositionTarget.Rendering -= InitialFrame; frameSubscribed = false; } stack?.Children.Remove(entry.Surface); entry.Dispose(); stack?.InvalidateMeasure();
        PositionStack();
    }
    private void ClearEntries()
    { foreach (var entry in entries.ToArray()) entry.Dispose(); entries.Clear(); pendingFrames.Clear(); stack?.Children.Clear(); if (frameSubscribed) { CompositionTarget.Rendering -= InitialFrame; frameSubscribed = false; } }
    private bool DeclarationVisible
    { get { for (DependencyObject? node = this; node is not null; node = VisualTreeHelper.GetParent(node)) if (node is FrameworkElement element && element.Visibility != Visibility.Visible) return false; return true; } }
    private bool HostInteractionAllowed => branch is { } current
        ? modalHost?.IsBranchAttached(current) == true && current.Frame.IsHitTestVisible
        : BleeckerModalHost.Find(this) is not { } owner || owner.AllowsFocusTarget(this);
    private bool InputAllowed => IsLoaded && IsEnabled && DeclarationVisible && BleeckerPopupScope.AllowsInput(XamlRoot, this) && HostInteractionAllowed;
    private void DisconnectVisibility() { foreach (var (element, token) in declarationVisibility) element.UnregisterPropertyChangedCallback(VisibilityProperty, token); declarationVisibility.Clear(); }
    private void ObserveVisibility()
    { DisconnectVisibility(); for (DependencyObject? node = this; node is not null; node = VisualTreeHelper.GetParent(node)) if (node is FrameworkElement element) declarationVisibility.Add((element, element.RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => ScopeChanged()))); }

    private void EnsurePresentation()
    {
        if (plane is not null) return;
        if (declaration is null) throw new InvalidOperationException("Sonner requires its applied declaration template.");
        root = XamlRoot ?? throw new InvalidOperationException("Sonner requires an actual XamlRoot.");
        plane = new Canvas { Name = "SonnerPlane", Width = root.Size.Width, Height = root.Size.Height, FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false, IsTabStop = false };
        stack = new ToastStack(this) { Name = "SonnerNotifications", FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false };
        AutomationProperties.SetName(stack, "Notifications"); AutomationProperties.SetLiveSetting(stack, AutomationLiveSetting.Polite);
        plane.Children.Add(stack); stack.SizeChanged += StackSizeChanged;
        modalHost = BleeckerModalHost.Find(this);
        if (modalHost is not null) { branch = modalHost.AttachPopupBranch(this, plane, new Point()); modalHost.ScopeChanged += ScopeChanged; }
        else { popup = new Popup { Child = plane, IsLightDismissEnabled = false, ShouldConstrainToRootBounds = true, FlowDirection = FlowDirection.LeftToRight }; declaration.Children.Add(popup); popup.IsOpen = true; }
        scope = BleeckerPopupScope.Attach(this, plane, false, () => InputAllowed); scopeObserver = BleeckerPopupScope.Observe(root, ScopeChanged); root.Changed += RootChanged;
        PositionStack(); ScopeChanged();
    }
    private void QueuePresentation()
    {
        if (!DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => { if (IsLoaded) { ApplyTemplate(); EnsurePresentation(); } }))
            throw new InvalidOperationException("The Sonner dispatcher rejected its presentation initialization.");
    }
    private void StackSizeChanged(object sender, SizeChangedEventArgs args) => PositionStack();
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => PositionStack();
    private void ScopeChanged()
    {
        if (branch is not null && modalHost?.IsBranchAttached(branch) != true) { ClearEntries(); RetirePresentation(); return; }
        if (plane is not null) { plane.IsHitTestVisible = InputAllowed; plane.Visibility = DeclarationVisible ? Visibility.Visible : Visibility.Collapsed; }
        foreach (var entry in entries.ToArray()) entry.Notify();
    }
    private void RefreshPresentation()
    {
        if (!double.IsFinite(FontSize) || FontSize <= 0) throw new InvalidOperationException("Sonner FontSize must be positive and finite.");
        foreach (var entry in entries.ToArray()) entry.Refresh(); stack?.InvalidateMeasure(); PositionStack(); ScopeChanged();
    }
    private void QueueThemeRefresh()
    {
        if (themePending) return; themePending = true;
        if (!DispatcherQueue.TryEnqueue(() => { themePending = false; if (IsLoaded) RefreshPresentation(); }))
            throw new InvalidOperationException("The Sonner dispatcher rejected its theme refresh.");
    }
    private void PositionStack()
    {
        if (positioning || root is null || plane is null || stack is null || !IsLoaded) return;
        positioning = true;
        try
        {
            if (!ReferenceEquals(XamlRoot, root)) { ClearEntries(); RetirePresentation(); return; }
            var width = root.Size.Width; var height = root.Size.Height; var factor = RemScale;
            if (plane.Width != width) plane.Width = width; if (plane.Height != height) plane.Height = height;
            plane.RequestedTheme = ActualTheme;
            foreach (var entry in entries) if (entry.Surface.RequestedTheme != ActualTheme) entry.Surface.RequestedTheme = ActualTheme;
            var hostWidth = Math.Min(352 * factor, width); if (stack.Width != hostWidth) stack.Width = hostWidth;
            var x = Position switch { BleeckerSonnerPosition.TopLeft or BleeckerSonnerPosition.BottomLeft => 16 * factor, BleeckerSonnerPosition.TopCenter or BleeckerSonnerPosition.BottomCenter => (width - hostWidth) / 2, _ => width - 16 * factor - hostWidth };
            var y = Position is BleeckerSonnerPosition.TopLeft or BleeckerSonnerPosition.TopCenter or BleeckerSonnerPosition.TopRight ? 16 * factor : height - 16 * factor - stack.ActualHeight;
            if (Canvas.GetLeft(stack) != x) Canvas.SetLeft(stack, x); if (Canvas.GetTop(stack) != y) Canvas.SetTop(stack, y);
            if (popup is not null && root.Content is UIElement target && declaration is not null)
            { var origin = declaration.TransformToVisual(target).TransformPoint(new Point()); if (Math.Abs(popup.HorizontalOffset + origin.X) > .000001) popup.HorizontalOffset = -origin.X; if (Math.Abs(popup.VerticalOffset + origin.Y) > .000001) popup.VerticalOffset = -origin.Y; }
        }
        finally { positioning = false; }
    }
    private void RetirePresentation()
    {
        scopeObserver?.Dispose(); scopeObserver = null; scope?.Dispose(); scope = null;
        if (root is not null) root.Changed -= RootChanged;
        if (stack is not null) stack.SizeChanged -= StackSizeChanged;
        if (modalHost is not null) { modalHost.ScopeChanged -= ScopeChanged; if (branch is not null && modalHost.IsBranchAttached(branch)) modalHost.DetachBranch(branch); } branch = null; modalHost = null;
        if (popup is not null) { popup.IsOpen = false; popup.Child = null; declaration?.Children.Remove(popup); } popup = null;
        plane?.Children.Clear(); plane = null; stack = null; root = null;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new SonnerPeer(this);
    private sealed class SonnerPeer(BleeckerSonner owner) : FrameworkElementAutomationPeer(owner)
    { protected override string GetClassNameCore() => nameof(BleeckerSonner); protected override string GetNameCore() => "Notifications"; protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group; }
    private sealed class ToastStack(BleeckerSonner owner) : Panel
    {
        protected override Size MeasureOverride(Size available)
        { var factor = owner.RemScale; var width = double.IsFinite(available.Width) ? available.Width : Width; var height = 32 * factor; foreach (var child in Children) { child.Measure(new Size(Math.Max(0, width - 32 * factor), double.PositiveInfinity)); height += child.DesiredSize.Height; } height += Math.Max(0, Children.Count - 1) * 8 * factor; return new Size(width, height); }
        protected override Size ArrangeOverride(Size final)
        { var factor = owner.RemScale; var y = 16 * factor; foreach (var child in Children) { child.Arrange(new Rect(16 * factor, y, Math.Max(0, final.Width - 32 * factor), child.DesiredSize.Height)); y += child.DesiredSize.Height + 8 * factor; } return final; }
    }
}
