using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

// Layout and ownership plane shared by the public modal host and popup adapters.
// This core never opens a Popup or calls any focus API.
internal sealed class BleeckerModalLayerHostCore : Grid
{
    internal sealed class Layer(FrameworkElement surface, UIElement overlay, Grid frame)
    {
        internal FrameworkElement Surface { get; } = surface;
        internal UIElement Overlay { get; } = overlay;
        internal Grid Frame { get; } = frame;
        internal bool Active { get; set; }
        internal readonly List<Branch> Branches = [];
    }
    internal sealed class Branch(Layer? owner, FrameworkElement root, Border frame)
    {
        internal Layer? Owner { get; } = owner;
        internal FrameworkElement Root { get; } = root;
        internal Border Frame { get; } = frame;
    }
    private readonly ContentPresenter background = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
    private readonly Grid portals = new() { FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false };
    private readonly Dictionary<FrameworkElement, Layer> layers = new(ReferenceEqualityComparer.Instance);
    private readonly List<Branch> backgroundBranches = [];
    private readonly BleeckerModalStackModel<FrameworkElement, DependencyObject> stack = new();
    private IDisposable? popupScopeObservation;

    internal BleeckerModalLayerHostCore()
    {
        UseLayoutRounding = false; Children.Add(background); Children.Add(portals);
        Loaded += (_, _) => { popupScopeObservation?.Dispose(); if (XamlRoot is { } root) popupScopeObservation = BleeckerPopupScope.Observe(root, PeerChanged); };
        Unloaded += (_, _) => { popupScopeObservation?.Dispose(); popupScopeObservation = null; };
    }
    internal UIElement? BackgroundContent
    {
        get => background.Content as UIElement;
        set
        {
            if (ReferenceEquals(background.Content, value)) return;
            if (value is not null && VisualTreeHelper.GetParent(value) is not null) throw new InvalidOperationException("Modal background content already has a visual parent.");
            background.Content = value;
        }
    }
    internal FrameworkElement? ActiveSurface => stack.Active?.Layer;
    internal bool BackgroundPointerBlocked => !background.IsHitTestVisible;
    internal event Action? ScopeChanged;

    internal Layer AttachLayer(FrameworkElement surface, UIElement overlay)
    {
        ArgumentNullException.ThrowIfNull(surface); ArgumentNullException.ThrowIfNull(overlay);
        if (layers.ContainsKey(surface)) throw new InvalidOperationException("The modal surface is already attached.");
        if (VisualTreeHelper.GetParent(overlay) is not null || !DeclaredWithin(surface, overlay)) throw new InvalidOperationException("A modal overlay must be unparented and contain its surface.");
        var frame = new Grid { Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent), IsHitTestVisible = false, UseLayoutRounding = false };
        Canvas.SetZIndex(frame, 50); frame.Children.Add(overlay); var layer = new Layer(surface, overlay, frame); layers.Add(surface, layer); portals.Children.Add(frame); return layer;
    }
    internal void Activate(Layer layer, IEnumerable<FrameworkElement> ancestors, DependencyObject? previousFocus)
    {
        RequireLayer(layer); if (layer.Active) throw new InvalidOperationException("The modal layer is already active.");
        stack.Add(layer.Surface, ancestors, previousFocus); layer.Active = true; Update();
    }
    internal BleeckerModalStackModel<FrameworkElement, DependencyObject>.Removal Deactivate(Layer layer)
    {
        RequireLayer(layer); if (!layer.Active) throw new InvalidOperationException("The modal layer is not active.");
        var result = stack.Remove(layer.Surface, Owns); layer.Active = false; layer.Frame.IsHitTestVisible = false;
        foreach (var branch in layer.Branches) branch.Frame.IsHitTestVisible = false;
        Update(); return result;
    }
    internal void DetachLayer(Layer layer)
    {
        RequireLayer(layer); if (layer.Active) Deactivate(layer);
        foreach (var branch in layer.Branches.ToArray()) if (IsBranchAttached(branch)) DetachBranch(branch);
        portals.Children.Remove(layer.Frame); layer.Frame.Children.Clear(); layers.Remove(layer.Surface); Changed();
    }
    internal Branch AttachBranch(Layer layer, FrameworkElement root, Point position)
    {
        RequireLayer(layer); ArgumentNullException.ThrowIfNull(root);
        return AttachBranchCore(layer, root, position);
    }
    internal Branch AttachBackgroundBranch(FrameworkElement root, Point position, int zIndex = 50) => AttachBranchCore(null, root, position, zIndex);
    private Branch AttachBranchCore(Layer? layer, FrameworkElement root, Point position, int backgroundZIndex = 50)
    {
        ArgumentNullException.ThrowIfNull(root);
        ValidatePosition(position);
        if (VisualTreeHelper.GetParent(root) is not null) throw new InvalidOperationException("A portaled branch must leave its prior visual parent before attaching.");
        var frame = new Border { Child = root, Margin = new(position.X, position.Y, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, UseLayoutRounding = false };
        Canvas.SetZIndex(frame, layer is null ? backgroundZIndex : Canvas.GetZIndex(layer.Frame) + 1);
        var branch = new Branch(layer, root, frame); (layer?.Branches ?? backgroundBranches).Add(branch); portals.Children.Add(frame); Update(); return branch;
    }
    internal bool IsBranchAttached(Branch branch) => branch.Owner is { } owner
        ? layers.TryGetValue(owner.Surface, out var attached) && ReferenceEquals(attached, owner) && owner.Branches.Contains(branch)
        : backgroundBranches.Contains(branch);
    internal void DetachBackgroundBranches() { foreach (var branch in backgroundBranches.ToArray()) if (IsBranchAttached(branch)) DetachBranch(branch); }
    internal void PositionBranch(Branch branch, Point position)
    {
        if (!IsBranchAttached(branch)) throw new InvalidOperationException("The portaled branch is not attached to this host.");
        ValidatePosition(position);
        var margin = new Thickness(position.X, position.Y, 0, 0);
        if (branch.Frame.Margin != margin) branch.Frame.Margin = margin;
    }
    private static void ValidatePosition(Point position)
    {
        if (!double.IsFinite(position.X) || !double.IsFinite(position.Y)) throw new ArgumentOutOfRangeException(nameof(position));
    }
    internal void DetachBranch(Branch branch)
    {
        if (!IsBranchAttached(branch)) throw new InvalidOperationException("The portaled branch is not attached to this host.");
        (branch.Owner?.Branches ?? backgroundBranches).Remove(branch);
        portals.Children.Remove(branch.Frame); branch.Frame.Child = null; Changed();
    }
    internal bool AllowsFocusTarget(DependencyObject target) => stack.Active is not { } active || Owns(active.Layer, target);

    private bool Owns(FrameworkElement surface, DependencyObject target) => Within(target, surface) ||
        layers.TryGetValue(surface, out var layer) && layer.Branches.Any(branch => Within(target, branch.Root));
    private static bool Within(DependencyObject target, DependencyObject root)
    {
        for (DependencyObject? node = target; node is not null; node = VisualTreeHelper.GetParent(node)) if (ReferenceEquals(node, root)) return true;
        return false;
    }
    private static bool DeclaredWithin(DependencyObject target, DependencyObject root)
    {
        if (ReferenceEquals(target, root)) return true;
        // An unattached overlay has declared children before the native visual
        // parent chain exists. Validate that authoritative owned structure.
        IEnumerable<DependencyObject> children = root switch
        {
            Panel panel => panel.Children.Cast<DependencyObject>(),
            Border { Child: { } child } => [child],
            ContentPresenter { Content: DependencyObject child } => [child],
            ContentControl { Content: DependencyObject child } => [child],
            _ => Enumerable.Range(0, VisualTreeHelper.GetChildrenCount(root)).Select(i => VisualTreeHelper.GetChild(root, i))
        };
        return children.Any(child => DeclaredWithin(target, child));
    }
    private void RequireLayer(Layer layer)
    {
        if (!layers.TryGetValue(layer.Surface, out var attached) || !ReferenceEquals(attached, layer)) throw new InvalidOperationException("The modal layer belongs to a different host or is detached.");
    }
    private void Update()
    {
        background.IsHitTestVisible = stack.Active is null;
        foreach (var branch in backgroundBranches) branch.Frame.IsHitTestVisible = stack.Active is null;
        foreach (var entry in stack.Entries)
        {
            var layer = layers[entry.Layer]; layer.Frame.IsHitTestVisible = entry.Interactive; Canvas.SetZIndex(layer.Frame, entry.OverlayZIndex);
            foreach (var branch in layer.Branches) { branch.Frame.IsHitTestVisible = entry.Interactive; Canvas.SetZIndex(branch.Frame, entry.BranchZIndex); }
        }
        foreach (var layer in layers.Values.Where(layer => !layer.Active)) foreach (var branch in layer.Branches) branch.Frame.IsHitTestVisible = false;
        Changed();
    }
    private void PeerChanged() => (FrameworkElementAutomationPeer.FromElement(this) as HostPeer)?.Changed();
    private void Changed() { PeerChanged(); ScopeChanged?.Invoke(); }
    protected override AutomationPeer OnCreateAutomationPeer() => new HostPeer(this);
    private sealed class HostPeer(BleeckerModalLayerHostCore owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerModalLayerHostCore);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
        protected override bool IsControlElementCore() => false;
        protected override bool IsContentElementCore() => false;
        protected override IList<AutomationPeer> GetChildrenCore()
        {
            if (owner.XamlRoot is { } root && BleeckerPopupScope.ActiveContentRoots(root) is { } scoped)
                return scoped.Where(node => Within(node, owner)).OfType<UIElement>().SelectMany(Peers).ToArray();
            var roots = owner.stack.Active is { } active
                ? new UIElement[] { active.Layer }.Concat(owner.layers[active.Layer].Branches.Select(branch => (UIElement)branch.Root))
                : (owner.BackgroundContent is { } content ? new UIElement[] { content } : []).Concat(owner.backgroundBranches.Select(branch => (UIElement)branch.Root));
            return roots.SelectMany(Peers).ToArray();
        }
        private static IEnumerable<AutomationPeer> Peers(UIElement root)
        {
            if (FrameworkElementAutomationPeer.CreatePeerForElement(root) is { } peer) { yield return peer; yield break; }
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) if (VisualTreeHelper.GetChild(root, i) is UIElement child) foreach (var nested in Peers(child)) yield return nested;
        }
        internal void Changed() { InvalidatePeer(); if (ListenerExists(AutomationEvents.StructureChanged)) RaiseAutomationEvent(AutomationEvents.StructureChanged); }
    }
}
