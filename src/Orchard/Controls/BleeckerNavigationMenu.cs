using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

/// <summary>Original caller links and descriptive groups, with host-owned destination handling.</summary>
[ContentProperty(Name = nameof(Entries))]
public sealed partial class BleeckerNavigationMenu : Control
{
    private Grid? layout;
    private EntryPanel? panel;
    private TextBlock? primaryProbe;
    private long primaryToken;
    private readonly Dictionary<string, EntrySlot> slots = [];
    private readonly HashSet<INotifyCollectionChanged> collections = [];
    private readonly HashSet<BleeckerNavigationMenuLink> links = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<BleeckerNavigationMenuGroup> groups = new(ReferenceEqualityComparer.Instance);
    private ICommand? observedCommand;
    private readonly List<(FrameworkElement Element, long Token)> declarationVisibility = [];
    private IReadOnlyList<IBleeckerNavigationMenuEntry> entries = [];
    private int? activeIndex;
    private bool rendering, pending, positioning;
    private Canvas? plane;
    private Popup? popup;
    private XamlRoot? observedRoot;
    private UIElement? input;
    private BleeckerModalHost? host;
    private BleeckerModalLayerHostCore.Branch? branch;
    private BleeckerPopupScope.Registration? scope;
    private IDisposable? scopeObserver;
    private XamlRoot? scopeRoot;
    private BleeckerModalHost? inputHost;

    public BleeckerNavigationMenu()
    {
        DefaultStyleKey = typeof(BleeckerNavigationMenu); IsTabStop = false; UseLayoutRounding = false;
        Entries = new BleeckerNavigationMenuEntries();
        Loaded += (_, _) => { ObserveDeclarationVisibility(); ConnectInputScope(); Render(); };
        Unloaded += (_, _) => { DisconnectInputScope(); activeIndex = null; RetirePresentation(); Disconnect(); DisconnectDeclarationVisibility(); foreach (var slot in slots.Values) slot.Notify(); };
        ActualThemeChanged += (_, _) => { foreach (var slot in slots.Values) slot.Paint(); UpdatePosition(); };
        LayoutUpdated += (_, _) => { ConnectInputScope(); UpdatePosition(); };
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); });
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => { if (!IsEnabled) { activeIndex = null; RetirePresentation(); } Render(); });
        foreach (var property in new[] { FlowDirectionProperty, LanguageProperty, IsTextScaleFactorEnabledProperty }) RegisterPropertyChangedCallback(property, (_, _) => Render());
    }
    private static DependencyProperty Property(string name, Type type) => DependencyProperty.Register(name, type, typeof(BleeckerNavigationMenu), new PropertyMetadata(null, (owner, _) => ((BleeckerNavigationMenu)owner).Render()));
    public static readonly DependencyProperty EntriesProperty = Property(nameof(Entries), typeof(object));
    public static readonly DependencyProperty LinkTemplateProperty = Property(nameof(LinkTemplate), typeof(DataTemplate));
    public static readonly DependencyProperty TopLinkStyleProperty = Property(nameof(TopLinkStyle), typeof(Style));
    public static readonly DependencyProperty GroupLinkStyleProperty = Property(nameof(GroupLinkStyle), typeof(Style));
    public static readonly DependencyProperty TriggerStyleProperty = Property(nameof(TriggerStyle), typeof(Style));
    public static readonly DependencyProperty SurfaceStyleProperty = Property(nameof(SurfaceStyle), typeof(Style));
    public static readonly DependencyProperty CommandProperty = Property(nameof(Command), typeof(ICommand));
    public static readonly DependencyProperty CommandParameterProperty = Property(nameof(CommandParameter), typeof(object));
    public IEnumerable<IBleeckerNavigationMenuEntry> Entries { get => (IEnumerable<IBleeckerNavigationMenuEntry>)GetValue(EntriesProperty); set => SetValue(EntriesProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public DataTemplate? LinkTemplate { get => (DataTemplate?)GetValue(LinkTemplateProperty); set => SetValue(LinkTemplateProperty, value); }
    public Style? TopLinkStyle { get => (Style?)GetValue(TopLinkStyleProperty); set => SetValue(TopLinkStyleProperty, value); }
    public Style? GroupLinkStyle { get => (Style?)GetValue(GroupLinkStyleProperty); set => SetValue(GroupLinkStyleProperty, value); }
    public Style? TriggerStyle { get => (Style?)GetValue(TriggerStyleProperty); set => SetValue(TriggerStyleProperty, value); }
    public Style? SurfaceStyle { get => (Style?)GetValue(SurfaceStyleProperty); set => SetValue(SurfaceStyleProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
    public event EventHandler<BleeckerNavigationMenuLink>? ItemInvoked;
    /// <summary>Rereads ordinary mutable collections; notification producers update automatically.</summary>
    public void Refresh() => Render();

    protected override void OnApplyTemplate()
    {
        DetachTemplate(); base.OnApplyTemplate();
        var part = GetTemplateChild("PART_Entries");
        if (part is not null && part is not Grid) throw new InvalidOperationException("Navigation Menu PART_Entries must be a Grid.");
        layout = part as Grid; if (layout is null) return;
        panel = new EntryPanel(this) { FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false }; layout.Children.Add(panel);
        primaryProbe = (TextBlock)XamlReader.Load("<TextBlock xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Foreground='{ThemeResource BleeckerTextPrimaryBrush}' Visibility='Collapsed' AutomationProperties.AccessibilityView='Raw'/>");layout.Children.Add(primaryProbe);
        primaryToken = primaryProbe.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty, (_, _) => { foreach (var slot in slots.Values) slot.Paint(); });
        Render();
    }
    private void DetachTemplate()
    { activeIndex = null; RetirePresentation(); foreach (var slot in slots.Values) slot.Dispose(); slots.Clear(); panel?.Children.Clear(); if (primaryProbe is not null) primaryProbe.UnregisterPropertyChangedCallback(TextBlock.ForegroundProperty, primaryToken); layout?.Children.Clear(); panel = null; layout = null; primaryProbe = null; }
    private void Disconnect()
    {
        foreach (var c in collections) c.CollectionChanged -= CollectionChanged; collections.Clear();
        foreach (var l in links) { l.Changed -= ItemChanged; l.DescriptionChanged -= ItemChanged; } links.Clear();
        foreach (var g in groups) g.Changed -= ItemChanged; groups.Clear();
        if (observedCommand is not null) observedCommand.CanExecuteChanged -= CommandChanged; observedCommand = null;
    }
    private void Observe()
    {
        Disconnect(); if (!IsLoaded) return;
        void Collection(object value) { if (value is INotifyCollectionChanged c && collections.Add(c)) c.CollectionChanged += CollectionChanged; }
        void Link(BleeckerNavigationMenuLink l) { if (links.Add(l)) { l.Changed += ItemChanged; l.DescriptionChanged += ItemChanged; } }
        Collection(Entries);
        foreach (var e in entries)
            if (e is BleeckerNavigationMenuLink l) Link(l);
            else if (e is BleeckerNavigationMenuGroup g) { if (groups.Add(g)) g.Changed += ItemChanged; Collection(g.Items); foreach (var child in g.Items) { ArgumentNullException.ThrowIfNull(child); Link(child); } }
        observedCommand = Command; if (observedCommand is not null) observedCommand.CanExecuteChanged += CommandChanged;
    }
    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) => Render();
    private void ItemChanged(object? sender, EventArgs args) => Render();
    private void CommandChanged(object? sender, EventArgs args) { foreach (var slot in slots.Values) slot.Notify(); }
    private static string Key(IBleeckerNavigationMenuEntry entry) => entry switch { BleeckerNavigationMenuLink l => "link:" + l.Href, BleeckerNavigationMenuGroup g => "group:" + g.Trigger, _ => throw new ArgumentException("Navigation Menu entries must be original links or groups.") };
    private void Render()
    {
        if (rendering) { pending = true; return; }
        if (GetValue(EntriesProperty) is not IEnumerable<IBleeckerNavigationMenuEntry> values) { if (!IsLoaded && GetBindingExpression(EntriesProperty) is not null) return; throw new InvalidOperationException("Navigation Menu entries must resolve before mounting."); }
        rendering = true;
        try
        {
            entries = values.ToArray(); var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var e in entries) { ArgumentNullException.ThrowIfNull(e); if (!keys.Add(Key(e))) throw new ArgumentException("Navigation Menu sibling entry keys must be unique."); }
            Observe(); if (layout is null || panel is null) return;
            foreach (var key in slots.Keys.Where(k => !keys.Contains(k)).ToArray()) { slots[key].Dispose(); slots.Remove(key); }
            for (var i = 0; i < entries.Count; i++)
            {
                var key = Key(entries[i]); if (!slots.TryGetValue(key, out var slot)) { slot = new(this); slots.Add(key, slot); }
                slot.Update(entries[i], i); var current = panel.Children.IndexOf(slot.Root);
                if (current != i) { if (current >= 0) panel.Children.RemoveAt(current); panel.Children.Insert(i, slot.Root); }
            }
            while (panel.Children.Count > entries.Count) panel.Children.RemoveAt(panel.Children.Count - 1);
            foreach (var slot in slots.Values) slot.SetOpen(IsLoaded && IsEnabled && activeIndex == slot.Index);
            panel.InvalidateMeasure(); UpdatePosition();
        }
        finally { rendering = false; }
        if (pending) { pending = false; Render(); }
    }
    private bool DeclarationVisible
    {
        get { for (DependencyObject? node = this; node is not null; node = VisualTreeHelper.GetParent(node)) if (node is FrameworkElement f && f.Visibility != Visibility.Visible) return false; return true; }
    }
    private void DisconnectDeclarationVisibility() { foreach (var (element, token) in declarationVisibility) element.UnregisterPropertyChangedCallback(VisibilityProperty, token); declarationVisibility.Clear(); }
    private void ObserveDeclarationVisibility()
    {
        DisconnectDeclarationVisibility();
        for (DependencyObject? node = this; node is not null; node = VisualTreeHelper.GetParent(node))
            if (node is FrameworkElement element) declarationVisibility.Add((element, element.RegisterPropertyChangedCallback(VisibilityProperty, (_, _) =>
            {
                if (!DeclarationVisible) { activeIndex = null; RetirePresentation(); foreach (var slot in slots.Values) slot.Notify(); }
                else Render();
            })));
    }
    private bool HostInteractionAllowed => branch is { } current
        ? host?.IsBranchAttached(current) == true && current.Frame.IsHitTestVisible
        : BleeckerModalHost.Find(this) is not { } owner || owner.AllowsFocusTarget(this);
    private bool InputAllowed => IsEnabled && IsLoaded && DeclarationVisible && BleeckerPopupScope.AllowsInput(XamlRoot, this) && HostInteractionAllowed;
    private void ConnectInputScope()
    {
        var root = IsLoaded ? XamlRoot : null; var owner = IsLoaded ? BleeckerModalHost.Find(this) : null;
        if (ReferenceEquals(root, scopeRoot) && ReferenceEquals(owner, inputHost)) return;
        DisconnectInputScope(); scopeRoot = root; inputHost = owner;
        if (root is not null) scopeObserver = BleeckerPopupScope.Observe(root, ScopeChanged);
        if (inputHost is not null) inputHost.ScopeChanged += ScopeChanged;
        foreach (var slot in slots.Values) slot.Notify();
    }
    private void DisconnectInputScope()
    {
        scopeObserver?.Dispose(); scopeObserver = null; scopeRoot = null;
        if (inputHost is not null) inputHost.ScopeChanged -= ScopeChanged; inputHost = null;
    }
    internal bool CanInvoke(BleeckerNavigationLinkContext context, bool ignoreCommandCoercion = false)
    {
        if (!InputAllowed || !context.Mounted || context.Element?.IsLoaded != true || !(Command?.CanExecute(CommandParameter) ?? true)) return false;
        if (!slots.Values.SelectMany(s => s.Links).Any(s => ReferenceEquals(s.Context, context))) return false;
        for (DependencyObject? node = context.Element; node is not null; node = VisualTreeHelper.GetParent(node))
        {
            if (node is FrameworkElement f && f.Visibility != Visibility.Visible) return false;
            // ButtonBase coerces IsEnabled from Command.CanExecute. Reading that
            // same coerced value while answering CanExecute would latch it off
            // at first mount. Execute rechecks the actual element separately.
            var ownCommandButton = ignoreCommandCoercion && ReferenceEquals(node, context.Element) && node is ButtonBase b && ReferenceEquals(b.Command, context.Command);
            if (node is Control c && !c.IsEnabled && !ownCommandButton) return false;
        }
        return true;
    }
    internal void Invoke(BleeckerNavigationLinkContext context)
    {
        if (!CanInvoke(context)) return;
        var original = context.Item; if (context.IsGrouped) Close();
        Command?.Execute(CommandParameter); ItemInvoked?.Invoke(this, original);
    }
    internal void Close() { activeIndex = null; Render(); }
    private void Toggle(EntrySlot slot) { if (!InputAllowed) return; activeIndex = activeIndex == slot.Index ? null : slot.Index; Render(); }
    private void Expand(EntrySlot slot) { if (!InputAllowed) return; activeIndex = slot.Index; Render(); }
    private void EnsurePresentation()
    {
        if (plane is not null || layout is null || !IsLoaded) return;
        observedRoot = XamlRoot ?? throw new InvalidOperationException("Navigation Menu requires its actual XamlRoot."); input = observedRoot.Content ?? throw new InvalidOperationException("Navigation Menu requires root content.");
        plane = new Canvas { Name = "NavigationMenuPlane", Width = observedRoot.Size.Width, Height = observedRoot.Size.Height, FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false, IsTabStop = false };
        plane.AddHandler(PointerPressedEvent, new PointerEventHandler(RootPointer), true); host = BleeckerModalHost.Find(this);
        if (host is not null) branch = host.AttachPopupBranch(this, plane, new Point());
        else { popup = new Popup { Child = plane, IsLightDismissEnabled = false, ShouldConstrainToRootBounds = true, FlowDirection = FlowDirection.LeftToRight }; layout.Children.Add(popup); popup.IsOpen = true; }
        scope = BleeckerPopupScope.Attach(this, plane, false, () => InputAllowed);
        input.AddHandler(PointerPressedEvent, new PointerEventHandler(RootPointer), true); observedRoot.Changed += RootChanged;
    }
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdatePosition();
    private void ScopeChanged() { if (branch is not null && host?.IsBranchAttached(branch) != true) { activeIndex = null; RetirePresentation(); return; } if (plane is not null) plane.IsHitTestVisible = InputAllowed; foreach (var slot in slots.Values) slot.Notify(); }
    private void RootPointer(object sender, PointerRoutedEventArgs args) { if (activeIndex is not null && observedRoot is not null && InputAllowed && !BleeckerPopupScope.Contains(observedRoot, this, args.OriginalSource as DependencyObject)) Close(); }
    private void RetirePresentation()
    {
        foreach (var slot in slots.Values) slot.RetireSurface(); scope?.Dispose(); scope = null;
        if (input is not null) input.RemoveHandler(PointerPressedEvent, new PointerEventHandler(RootPointer)); input = null;
        if (observedRoot is not null) observedRoot.Changed -= RootChanged;
        var previousHost = host; var previousBranch = branch; branch = null; host = null;
        if (previousBranch is not null && previousHost?.IsBranchAttached(previousBranch) == true) previousHost.DetachBranch(previousBranch);
        if (popup is not null) { popup.IsOpen = false; popup.Child = null; layout?.Children.Remove(popup); } popup = null;
        if (plane is not null) { plane.RemoveHandler(PointerPressedEvent, new PointerEventHandler(RootPointer)); plane.Children.Clear(); } plane = null; observedRoot = null;
    }
    private void UpdatePosition()
    {
        if (positioning || plane is null || observedRoot?.Content is not UIElement target || !IsLoaded) return; positioning = true;
        try { if (!ReferenceEquals(XamlRoot, observedRoot)) { activeIndex = null; RetirePresentation(); return; } if (plane.Width != observedRoot.Size.Width) plane.Width = observedRoot.Size.Width; if (plane.Height != observedRoot.Size.Height) plane.Height = observedRoot.Size.Height; if (plane.RequestedTheme != ActualTheme) plane.RequestedTheme = ActualTheme;
            if (popup is not null && layout is not null) { var origin = layout.TransformToVisual(target).TransformPoint(new Point()); if (Math.Abs(popup.HorizontalOffset + origin.X) > .000001) popup.HorizontalOffset = -origin.X; if (Math.Abs(popup.VerticalOffset + origin.Y) > .000001) popup.VerticalOffset = -origin.Y; }
            foreach (var slot in slots.Values) slot.Position(target);
        } finally { positioning = false; }
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new MenuPeer(this);
    private sealed class MenuPeer(BleeckerNavigationMenu owner) : FrameworkElementAutomationPeer(owner)
    { protected override string GetClassNameCore() => nameof(BleeckerNavigationMenu); protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group; }
}
