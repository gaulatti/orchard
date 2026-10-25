using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;

namespace Orchard;

/// <summary>The root content host shared by Orchard modal surfaces.</summary>
/// <remarks>Place one host around the entire content of a window. Modal surfaces
/// use its private overlay plane; caller content and bindings are retained.</remarks>
public sealed partial class BleeckerModalHost : ContentControl
{
    private static readonly ConditionalWeakTable<XamlRoot, BleeckerModalHost> hosts = new();
    private readonly BleeckerModalLayerHostCore core = new();
    private readonly ContentPresenter background = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
    private readonly List<Registration> registrations = [];
    private Grid? templateRoot;
    private XamlRoot? observedRoot;
    private UIElement? inputRoot;
    private long focusGeneration;

    public BleeckerModalHost()
    {
        DefaultStyleKey = typeof(BleeckerModalHost); IsTabStop = false; UseLayoutRounding = false;
        foreach (var (target, source) in new[] { (ContentPresenter.ContentProperty, nameof(Content)), (ContentPresenter.ContentTemplateProperty, nameof(ContentTemplate)), (ContentPresenter.ContentTemplateSelectorProperty, nameof(ContentTemplateSelector)) })
            background.SetBinding(target, new Binding { Source = this, Path = new PropertyPath(source) });
        core.BackgroundContent = background;
        core.ScopeChanged += () => ScopeChanged?.Invoke();
        Loaded += (_, _) => ConnectRoot();
        Unloaded += (_, _) =>
        {
            DisconnectRoot();
            foreach (var registration in registrations.ToArray())
            {
                if (registration.Layer.Active) core.Deactivate(registration.Layer);
                core.DetachLayer(registration.Layer); registrations.Remove(registration);
            }
            core.DetachBackgroundBranches();
        };
        LayoutUpdated += (_, _) => { if (IsLoaded) { ConnectRoot(); if (core.ActiveSurface is not null) ValidateViewport(); } };
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); });
    }

    protected override void OnApplyTemplate()
    {
        DetachTemplate(); base.OnApplyTemplate(); if (Template is null) return;
        templateRoot = GetTemplateChild("PART_Layout") as Grid ?? throw new InvalidOperationException("ModalHost requires a Grid named PART_Layout.");
        templateRoot.Children.Add(core);
        if (IsLoaded) ConnectRoot();
    }
    private void DetachTemplate()
    {
        DisconnectRoot(); templateRoot?.Children.Remove(core); templateRoot = null;
    }
    private void ConnectRoot()
    {
        if (templateRoot is null || XamlRoot is not { } root) return;
        if (ReferenceEquals(observedRoot, root)) return;
        if (hosts.TryGetValue(root, out var other) && !ReferenceEquals(other, this)) throw new InvalidOperationException("A XamlRoot can contain only one BleeckerModalHost.");
        DisconnectRoot(); hosts.Add(root, this); observedRoot = root; inputRoot = root.Content;
        root.Changed += RootChanged; FocusManager.GettingFocus += OnGettingFocus;
        if (inputRoot is not null) inputRoot.KeyDown += RootKeyDown;
    }
    private void DisconnectRoot()
    {
        focusGeneration++;
        if (observedRoot is not null)
        {
            observedRoot.Changed -= RootChanged;
            if (hosts.TryGetValue(observedRoot, out var owner) && ReferenceEquals(owner, this)) hosts.Remove(observedRoot);
        }
        if (inputRoot is not null) inputRoot.KeyDown -= RootKeyDown;
        FocusManager.GettingFocus -= OnGettingFocus; inputRoot = null; observedRoot = null;
    }
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        if (!ReferenceEquals(inputRoot, sender.Content))
        {
            if (inputRoot is not null) inputRoot.KeyDown -= RootKeyDown;
            inputRoot = sender.Content; if (inputRoot is not null) inputRoot.KeyDown += RootKeyDown;
        }
        // XamlRoot reports a resize before its descendants finish layout.
        // LayoutUpdated validates the arranged viewport, not this old size.
    }
    private void ValidateViewport()
    {
        if (observedRoot is not { } root || templateRoot is null || !IsLoaded) throw new InvalidOperationException("A modal surface requires a loaded BleeckerModalHost.");
        var transform = core.TransformToVisual(root.Content);
        var corners = new[] { new Point(), new Point(core.ActualWidth, 0), new Point(0, core.ActualHeight), new Point(core.ActualWidth, core.ActualHeight) };
        var expected = new[] { new Point(), new Point(root.Size.Width, 0), new Point(0, root.Size.Height), new Point(root.Size.Width, root.Size.Height) };
        // RTL mirrors the native coordinate basis while retaining exactly the
        // same physical viewport. Match corner positions without assuming its
        // direction; a skewed/partial plane still cannot pass this check.
        var rootTransform = root.Content.RenderTransform;
        var rootTransformed = root.Content.Projection is not null || rootTransform is not null && new[] { new Point(), new Point(1, 0), new Point(0, 1) }.Any(point => { var actual = rootTransform.TransformPoint(point); return Math.Abs(actual.X - point.X) > .0001 || Math.Abs(actual.Y - point.Y) > .0001; });
        if (rootTransformed || Math.Abs(core.ActualWidth - root.Size.Width) > .01 || Math.Abs(core.ActualHeight - root.Size.Height) > .01 || corners.Any(point => { var actual = transform.TransformPoint(point); return !expected.Any(target => Math.Abs(actual.X - target.X) <= .01 && Math.Abs(actual.Y - target.Y) <= .01); }))
            throw new InvalidOperationException($"BleeckerModalHost must cover the entire XamlRoot without a margin, transform or constrained size. Core={core.ActualWidth},{core.ActualHeight}; root={root.Size}; corners={string.Join(';', corners.Select(point => transform.TransformPoint(point)))}; direction={FlowDirection}.");
    }
    internal static BleeckerModalHost Require(FrameworkElement declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        if (declaration.XamlRoot is not { } root || !hosts.TryGetValue(root, out var host) || !host.IsLoaded || !Within(declaration, host.core))
            throw new InvalidOperationException("An Orchard modal surface must be declared within the window's BleeckerModalHost.");
        host.ValidateViewport(); return host;
    }
    internal static BleeckerModalHost? Find(FrameworkElement declaration) => declaration.XamlRoot is { } root && hosts.TryGetValue(root, out var host) && host.IsLoaded && Within(declaration, host.core) ? host : null;
    internal event Action? ScopeChanged;
    internal sealed class Registration(BleeckerModalHost host, FrameworkElement declaration, BleeckerModalLayerHostCore.Layer layer, Action dismiss, Registration[] ancestors) : IDisposable
    {
        internal BleeckerModalHost Host { get; } = host;
        internal FrameworkElement Declaration { get; } = declaration;
        internal BleeckerModalLayerHostCore.Layer Layer { get; } = layer;
        internal Action Dismiss { get; } = dismiss;
        internal Registration[] Ancestors { get; } = ancestors;
        internal bool IsAttached => Host.registrations.Contains(this);
        public void Dispose() { if (IsAttached) Host.Detach(this); }
    }
    internal Registration Attach(FrameworkElement declaration, FrameworkElement surface, UIElement overlay, Action dismiss)
    {
        if (!ReferenceEquals(Require(declaration), this)) throw new InvalidOperationException("The modal declaration belongs to another host.");
        ArgumentNullException.ThrowIfNull(dismiss);
        if (registrations.Any(r => ReferenceEquals(r.Declaration, declaration))) throw new InvalidOperationException("The modal declaration is already attached.");
        var ancestors = Ancestors(declaration);
        var result = new Registration(this, declaration, core.AttachLayer(surface, overlay), dismiss, ancestors); registrations.Add(result); return result;
    }
    internal void Activate(Registration registration)
    {
        RequireRegistration(registration); ValidateViewport();
        core.Activate(registration.Layer, registration.Ancestors.Select(r => r.Layer.Surface), FocusManager.GetFocusedElement(observedRoot!) as DependencyObject);
        QueueInitialFocus();
    }
    internal void Deactivate(Registration registration)
    {
        RequireRegistration(registration); var removal = core.Deactivate(registration.Layer);
        if (!removal.WasActive) return;
        focusGeneration++;
        if (!CanFocus) return;
        if (removal.PreviousFocus is { } previous && Connected(previous) && core.AllowsFocusTarget(previous)) Focus(previous, FocusState.Programmatic);
        else if (core.ActiveSurface is not null) Focus(FirstTarget(), FocusState.Programmatic);
    }
    internal void Detach(Registration registration)
    {
        RequireRegistration(registration); if (registration.Layer.Active) Deactivate(registration);
        core.DetachLayer(registration.Layer); registrations.Remove(registration);
    }
    internal BleeckerModalLayerHostCore.Branch AttachBranch(FrameworkElement declaration, FrameworkElement root, Point position)
    {
        var owner = Ancestors(declaration).LastOrDefault() ?? throw new InvalidOperationException("The branch has no modal owner.");
        return core.AttachBranch(owner.Layer, root, position);
    }
    internal BleeckerModalLayerHostCore.Branch AttachPopupBranch(FrameworkElement declaration, FrameworkElement root, Point position, int backgroundZIndex = 50)
    {
        var owner = Ancestors(declaration).LastOrDefault();
        return owner is null ? core.AttachBackgroundBranch(root, position, backgroundZIndex) : core.AttachBranch(owner.Layer, root, position);
    }
    internal bool IsBranchAttached(BleeckerModalLayerHostCore.Branch branch) => core.IsBranchAttached(branch);
    internal void PositionBranch(BleeckerModalLayerHostCore.Branch branch, Point position) => core.PositionBranch(branch, position);
    internal void DetachBranch(BleeckerModalLayerHostCore.Branch branch) => core.DetachBranch(branch);
    private Registration[] Ancestors(FrameworkElement declaration)
    {
        if (!ReferenceEquals(Require(declaration), this)) throw new InvalidOperationException("The declaration belongs to another modal host.");
        var result = new List<Registration>();
        for (DependencyObject? node = declaration; node is not null; node = VisualTreeHelper.GetParent(node))
        {
            var owner = registrations.SingleOrDefault(r => ReferenceEquals(r.Layer.Surface, node) || r.Layer.Branches.Any(b => ReferenceEquals(b.Root, node)));
            if (owner is not null)
            {
                result.Add(owner); result.AddRange(owner.Ancestors.Reverse()); break;
            }
        }
        result.Reverse(); return result.ToArray();
    }
    private void RequireRegistration(Registration registration)
    {
        if (!ReferenceEquals(registration.Host, this) || !registrations.Contains(registration)) throw new InvalidOperationException("The modal registration belongs to another host or is detached.");
    }
    internal FrameworkElement? ActiveSurface => core.ActiveSurface;
    internal bool BackgroundPointerBlocked => core.BackgroundPointerBlocked;
    internal bool AllowsFocusTarget(DependencyObject target) => core.AllowsFocusTarget(target);
    private bool CanFocus => IsLoaded && templateRoot is not null && observedRoot?.IsHostVisible == true;
    private void QueueInitialFocus()
    {
        var generation = ++focusGeneration;
        if (!DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => { if (generation == focusGeneration && CanFocus && core.ActiveSurface is not null) Focus(FirstTarget(), FocusState.Programmatic); }))
            throw new InvalidOperationException("ModalHost could not queue initial focus.");
    }
    private DependencyObject? FirstTarget() => SequentialTargets().FirstOrDefault() ?? core.ActiveSurface;
    private void Focus(DependencyObject? target, FocusState state)
    {
        if (CanFocus && target is not null && Connected(target) && core.AllowsFocusTarget(target)) _ = FocusManager.TryFocusAsync(target, state);
    }
    private bool Connected(DependencyObject target)
    {
        if (observedRoot is null || !Within(target, observedRoot.Content)) return false;
        for (DependencyObject? node = target; node is not null; node = VisualTreeHelper.GetParent(node))
            if (node is UIElement { Visibility: not Visibility.Visible } or Control { IsEnabled: false }) return false;
        return true;
    }
    private void OnGettingFocus(object? sender, GettingFocusEventArgs args)
    {
        if (args.NewFocusedElement is DependencyObject target && ShouldCancelFocusTarget(target)) args.TryCancel();
    }
    internal bool ShouldCancelFocusTarget(DependencyObject target)
    {
        var targetRoot = target switch { UIElement element => element.XamlRoot, TextElement text => text.XamlRoot, _ => null };
        return core.ActiveSurface is not null && observedRoot is not null && ReferenceEquals(targetRoot, observedRoot) && !core.AllowsFocusTarget(target);
    }
    private void RootKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (args.Handled || !CanFocus || observedRoot is null) return;
        if (args.Key == VirtualKey.Escape && BleeckerTooltip.DismissForEscape(observedRoot)) { args.Handled = true; return; }
        var shift = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;
        var plan = PlanKey(args.Key, shift, FocusManager.GetFocusedElement(observedRoot) as DependencyObject);
        if (!plan.Handled) return;
        args.Handled = true;
        if (plan.Dismiss) registrations.Single(r => ReferenceEquals(r.Layer.Surface, core.ActiveSurface)).Dismiss();
        else Focus(plan.Target, FocusState.Keyboard);
    }
    internal readonly record struct KeyPlan(bool Handled, bool Dismiss, DependencyObject? Target);
    internal KeyPlan PlanKey(VirtualKey key, bool shift, DependencyObject? focused)
    {
        if (core.ActiveSurface is null) return default;
        if (key == VirtualKey.Escape) return new(true, true, null);
        if (key != VirtualKey.Tab) return default;
        var targets = SequentialTargets();
        if (targets.Count == 0) return new(true, false, core.ActiveSurface);
        var index = targets.FindIndex(target => ReferenceEquals(target, focused));
        var next = index < 0 ? shift ? targets.Count - 1 : 0 : (index + (shift ? -1 : 1) + targets.Count) % targets.Count;
        return new(true, false, targets[next]);
    }
    internal List<DependencyObject> SequentialTargets()
    {
        if (core.ActiveSurface is not { } surface) return [];
        var registration = registrations.Single(r => ReferenceEquals(r.Layer.Surface, surface));
        var candidates = new List<(DependencyObject Target, int Index)>();
        void Walk(DependencyObject node, bool include)
        {
            if (node is UIElement { Visibility: not Visibility.Visible } or Control { IsEnabled: false }) return;
            if (include && node is FrameworkElement { IsTabStop: true, ActualWidth: > 0, ActualHeight: > 0 } element && Connected(element)) candidates.Add((element, element.TabIndex));
            if (node is TextBlock text) foreach (var inline in text.Inlines) Inline(inline);
            if (node is RichTextBlock rich) foreach (var paragraph in rich.Blocks.OfType<Paragraph>()) foreach (var inline in paragraph.Inlines) Inline(inline);
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) Walk(VisualTreeHelper.GetChild(node, i), true);
        }
        void Inline(Inline inline)
        {
            if (inline is Hyperlink { IsTabStop: true } link && link.ElementStart.VisualParent is FrameworkElement { ActualWidth: > 0, ActualHeight: > 0 } && Connected(link)) candidates.Add((link, link.TabIndex));
            if (inline is Span span) foreach (var child in span.Inlines) Inline(child);
            if (inline is InlineUIContainer { Child: { } element }) Walk(element, true);
        }
        Walk(surface, false); foreach (var branch in registration.Layer.Branches) Walk(branch.Root, true);
        // WinUI uses MaxValue for the ordinary sequential order. Explicit
        // smaller TabIndex values retain their native priority; ties stay in
        // surface/branch traversal order. Library radio groups own IsTabStop.
        return candidates.DistinctBy(c => c.Target, ReferenceEqualityComparer.Instance).OrderBy(c => c.Index).Select(c => c.Target).ToList();
    }
    private static bool Within(DependencyObject target, DependencyObject root)
    {
        for (DependencyObject? node = target; node is not null; node = VisualTreeHelper.GetParent(node)) if (ReferenceEquals(node, root)) return true;
        return false;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new HostPeer(this);
    private sealed class HostPeer(BleeckerModalHost owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerModalHost);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
        protected override bool IsControlElementCore() => false;
        protected override bool IsContentElementCore() => false;
        // Keep the core's scope/structure notifications in the peer tree.
        // ContentControl's content shortcut must not bypass that boundary.
        protected override IList<AutomationPeer> GetChildrenCore() => FrameworkElementAutomationPeer.CreatePeerForElement(owner.core) is { } peer ? new[] { peer } : [];
    }
}

