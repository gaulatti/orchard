using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

/// <summary>A themed menu bar with caller-controlled action and selection requests.</summary>
public sealed partial class BleeckerMenubar : Control
{
    private Grid? layout;
    private StackPanel? triggers;
    private readonly Dictionary<string, RootMenu> roots = [];
    private readonly HashSet<INotifyCollectionChanged> collections = [];
    private IReadOnlyList<BleeckerMenubarMenu> menus = [];
    private int? activeIndex;
    private Canvas? plane;
    private Popup? popup;
    private XamlRoot? observedRoot;
    private UIElement? input;
    private BleeckerModalHost? host;
    private BleeckerModalLayerHostCore.Branch? branch;
    private BleeckerPopupScope.Registration? scope;
    private IDisposable? scopeObserver;
    private bool rendering, renderAgain, positioning;

    public BleeckerMenubar()
    {
        DefaultStyleKey = typeof(BleeckerMenubar); IsTabStop = false; UseLayoutRounding = false;
        Loaded += (_, _) => Render();
        Unloaded += (_, _) => { activeIndex = null; RetirePresentation(); DisconnectCollections(); };
        ActualThemeChanged += (_, _) => UpdatePosition();
        LayoutUpdated += (_, _) => UpdatePosition();
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => { if (!IsEnabled) { activeIndex = null; foreach (var root in roots.Values) root.SetOpen(false, true); RetirePresentation(); } Render(); });
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); });
    }

    private static DependencyProperty Property(string name, Type type) => DependencyProperty.Register(name, type, typeof(BleeckerMenubar), new PropertyMetadata(null, (owner, _) => ((BleeckerMenubar)owner).Render()));
    public static readonly DependencyProperty MenusProperty = Property(nameof(Menus), typeof(object));
    public static readonly DependencyProperty TriggerStyleProperty = Property(nameof(TriggerStyle), typeof(Style));
    public static readonly DependencyProperty ItemStyleProperty = Property(nameof(ItemStyle), typeof(Style));
    public static readonly DependencyProperty SurfaceStyleProperty = Property(nameof(SurfaceStyle), typeof(Style));
    public static readonly DependencyProperty SubmenuSurfaceStyleProperty = Property(nameof(SubmenuSurfaceStyle), typeof(Style));
    /// <summary>Original caller menus. Observable collections update without replacing this binding.</summary>
    public IEnumerable<BleeckerMenubarMenu>? Menus { get => (IEnumerable<BleeckerMenubarMenu>?)GetValue(MenusProperty); set => SetValue(MenusProperty, value); }
    public Style? TriggerStyle { get => (Style?)GetValue(TriggerStyleProperty); set => SetValue(TriggerStyleProperty, value); }
    public Style? ItemStyle { get => (Style?)GetValue(ItemStyleProperty); set => SetValue(ItemStyleProperty, value); }
    public Style? SurfaceStyle { get => (Style?)GetValue(SurfaceStyleProperty); set => SetValue(SurfaceStyleProperty, value); }
    public Style? SubmenuSurfaceStyle { get => (Style?)GetValue(SubmenuSurfaceStyleProperty); set => SetValue(SubmenuSurfaceStyleProperty, value); }

    /// <summary>Rereads ordinary mutable collections; observable collections are observed automatically.</summary>
    public void Refresh() => Render();

    protected override void OnApplyTemplate()
    {
        DetachTemplate(); base.OnApplyTemplate();
        var rootPart = GetTemplateChild("PART_Layout"); var triggerPart = GetTemplateChild("PART_Triggers");
        if (rootPart is not null && rootPart is not Grid || triggerPart is not null && triggerPart is not StackPanel)
            throw new InvalidOperationException("Menubar PART_Layout must be a Grid and PART_Triggers a StackPanel.");
        layout = rootPart as Grid; triggers = triggerPart as StackPanel;
        Render();
    }

    private void DetachTemplate()
    {
        activeIndex = null;
        RetirePresentation(); foreach (var root in roots.Values) root.Dispose(); roots.Clear();
        triggers?.Children.Clear(); layout = null; triggers = null;
    }

    private void DisconnectCollections()
    { foreach (var collection in collections) collection.CollectionChanged -= CollectionChanged; collections.Clear(); }
    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) => Render();
    private void ObserveCollections()
    {
        DisconnectCollections();
        if (!IsLoaded) return;
        void Add(object? value) { if (value is INotifyCollectionChanged collection && collections.Add(collection)) collection.CollectionChanged += CollectionChanged; }
        void Entries(IReadOnlyList<BleeckerMenubarEntry> entries, HashSet<object> ancestors)
        {
            if (!ancestors.Add(entries)) throw new ArgumentException("Menubar entries cannot contain a recursive collection cycle.");
            Add(entries);
            foreach (var entry in entries)
            {
                ArgumentNullException.ThrowIfNull(entry);
                if (entry is BleeckerMenubarSubmenu sub) Entries(sub.Items, ancestors);
                if (entry is BleeckerMenubarRadioGroup group) { Add(group.Items); foreach (var item in group.Items) ArgumentNullException.ThrowIfNull(item); }
            }
            ancestors.Remove(entries);
        }
        Add(Menus); foreach (var menu in menus) Entries(menu.Items, new(ReferenceEqualityComparer.Instance));
    }

    private void Render()
    {
        if (rendering) { renderAgain = true; return; }
        rendering = true;
        try
        {
            menus = Menus?.ToArray() ?? [];
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var menu in menus) { ArgumentNullException.ThrowIfNull(menu); if (!keys.Add(menu.Trigger)) throw new ArgumentException("Menubar trigger strings must be unique."); }
            ObserveCollections();
            if (layout is null || triggers is null) return;
            foreach (var key in roots.Keys.Where(key => !keys.Contains(key)).ToArray()) { roots[key].Dispose(); roots.Remove(key); }
            for (var i = 0; i < menus.Count; i++)
            {
                var menu = menus[i];
                if (!roots.TryGetValue(menu.Trigger, out var root)) { root = new(this); roots.Add(menu.Trigger, root); }
                root.Update(menu, i);
                var current = triggers.Children.IndexOf(root.Trigger);
                if (current != i) { if (current >= 0) triggers.Children.RemoveAt(current); triggers.Children.Insert(i, root.Trigger); }
            }
            while (triggers.Children.Count > menus.Count) triggers.Children.RemoveAt(triggers.Children.Count - 1);
            foreach (var root in roots.Values) root.SetOpen(IsLoaded && IsEnabled && activeIndex == root.Index);
            UpdatePosition();
        }
        finally { rendering = false; }
        if (renderAgain) { renderAgain = false; Render(); }
    }

    private bool HostInteractionAllowed => branch is { } current
        ? host?.IsBranchAttached(current) == true && current.Frame.IsHitTestVisible
        : BleeckerModalHost.Find(this) is not { } owner || owner.AllowsFocusTarget(this);
    private bool InputAllowed => IsEnabled && IsLoaded && BleeckerPopupScope.AllowsInput(XamlRoot, this) && HostInteractionAllowed;
    private void Toggle(RootMenu root) { if (!InputAllowed) return; activeIndex = activeIndex == root.Index ? null : root.Index; Render(); }
    private void Close() { activeIndex = null; Render(); }
    private void Activate(Action callback)
    {
        if (!InputAllowed) return;
        callback(); // A throwing original callback must not run the later close.
        Close();
    }

    private void EnsurePresentation()
    {
        if (plane is not null || layout is null || !IsLoaded) return;
        observedRoot = XamlRoot ?? throw new InvalidOperationException("Menubar requires its actual XamlRoot.");
        input = observedRoot.Content ?? throw new InvalidOperationException("Menubar requires root content.");
        plane = new Canvas { Name = "MenubarPlane", Width = observedRoot.Size.Width, Height = observedRoot.Size.Height, FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false, IsTabStop = false };
        plane.AddHandler(PointerPressedEvent, new PointerEventHandler(RootPointer), true);
        host = BleeckerModalHost.Find(this);
        if (host is not null) { branch = host.AttachPopupBranch(this, plane, new Point()); host.ScopeChanged += ScopeChanged; }
        else
        {
            popup = new Popup { Child = plane, IsLightDismissEnabled = false, ShouldConstrainToRootBounds = true, FlowDirection = FlowDirection.LeftToRight };
            layout.Children.Add(popup); popup.IsOpen = true;
        }
        scope = BleeckerPopupScope.Attach(this, plane, false, () => InputAllowed);
        scopeObserver = BleeckerPopupScope.Observe(observedRoot, ScopeChanged);
        input.AddHandler(PointerPressedEvent, new PointerEventHandler(RootPointer), true);
        observedRoot.Changed += RootChanged;
    }
    private void ScopeChanged()
    {
        if (branch is not null && host?.IsBranchAttached(branch) != true) { activeIndex = null; RetirePresentation(); return; }
        if (plane is not null) plane.IsHitTestVisible = InputAllowed;
        foreach (var root in roots.Values) root.Trigger.InvalidateMetadata();
    }
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdatePosition();
    private void RootPointer(object sender, PointerRoutedEventArgs args)
    {
        if (observedRoot is null || activeIndex is null || !InputAllowed) return;
        if (!BleeckerPopupScope.Contains(observedRoot, this, args.OriginalSource as DependencyObject)) Close();
    }
    private void RetirePresentation()
    {
        foreach (var root in roots.Values) root.RetireSurface();
        scopeObserver?.Dispose(); scopeObserver = null; scope?.Dispose(); scope = null;
        if (input is not null) input.RemoveHandler(PointerPressedEvent, new PointerEventHandler(RootPointer)); input = null;
        if (observedRoot is not null) observedRoot.Changed -= RootChanged;
        if (host is not null) { host.ScopeChanged -= ScopeChanged; if (branch is not null && host.IsBranchAttached(branch)) host.DetachBranch(branch); }
        branch = null; host = null;
        if (popup is not null) { popup.IsOpen = false; popup.Child = null; layout?.Children.Remove(popup); } popup = null;
        if (plane is not null) { plane.RemoveHandler(PointerPressedEvent, new PointerEventHandler(RootPointer)); plane.Children.Clear(); } plane = null; observedRoot = null;
    }
    private void UpdatePosition()
    {
        if (positioning || plane is null || observedRoot?.Content is not UIElement target || !IsLoaded) return;
        positioning = true;
        try
        {
            if (!ReferenceEquals(observedRoot, XamlRoot)) { activeIndex = null; RetirePresentation(); return; }
            if (plane.Width != observedRoot.Size.Width) plane.Width = observedRoot.Size.Width;
            if (plane.Height != observedRoot.Size.Height) plane.Height = observedRoot.Size.Height;
            if (plane.RequestedTheme != ActualTheme) plane.RequestedTheme = ActualTheme;
            if (popup is not null && layout is not null)
            {
                var origin = layout.TransformToVisual(target).TransformPoint(new Point());
                if (Math.Abs(popup.HorizontalOffset + origin.X) > .000001) popup.HorizontalOffset = -origin.X;
                if (Math.Abs(popup.VerticalOffset + origin.Y) > .000001) popup.VerticalOffset = -origin.Y;
            }
            foreach (var root in roots.Values) root.Position(target);
        }
        finally { positioning = false; }
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new MenubarPeer(this);
    private sealed class MenubarPeer(BleeckerMenubar owner) : FrameworkElementAutomationPeer(owner)
    { protected override string GetClassNameCore() => nameof(BleeckerMenubar); protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.MenuBar; }
}
