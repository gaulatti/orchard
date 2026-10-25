using Microsoft.UI.Dispatching;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using VirtualKey = Windows.System.VirtualKey;

namespace Orchard;

public sealed partial class BleeckerContextMenu
{
    // The original presenter remains the native items/commands owner and the
    // native placement engine's measure target. Only its styled surface moves.
    private sealed class PortalSession : IDisposable
    {
        private readonly BleeckerContextMenu menu;
        private readonly MenuFlyoutPresenter presenter;
        private readonly FrameworkElement target;
        private readonly BleeckerModalHost? host;
        private static readonly ConditionalWeakTable<XamlRoot, List<PortalSession>> openSessions = new();
        private readonly XamlRoot root;
        private readonly UIElement input;
        private readonly Canvas plane = new() { Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent), FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false };
        private readonly Border measureProxy = new() { IsHitTestVisible = false, UseLayoutRounding = false };
        private readonly List<(DependencyProperty Property, long Token)> callbacks = [];
        private readonly Dictionary<FrameworkElement, List<(DependencyProperty Property, long Token)>> contentCallbacks = [];
        private Grid? slot;
        private BleeckerOverlaySurface? surface;
        private MenuSurface? menuSurface;
        private Popup? popup;
        private Popup? presentationPopup;
        private BleeckerModalLayerHostCore.Branch? branch;
        private BleeckerPopupScope.Registration? scope;
        private IDisposable? scopeObserver;
        private object? priorAccessibilityView, priorPopupTheme;
        private bool priorLightDismiss, priorHitTest;
        private bool disposed, queued, updating, mounting, focusPending;

        internal PortalSession(BleeckerContextMenu menu, MenuFlyoutPresenter presenter, FrameworkElement target, BleeckerModalHost? host)
        {
            this.menu = menu; this.presenter = presenter; this.target = target; this.host = host;
            root = target.XamlRoot ?? throw new InvalidOperationException("Context Menu requires the target's actual XamlRoot.");
            input = root.Content ?? throw new InvalidOperationException("Context Menu requires root content.");
            AutomationProperties.SetAccessibilityView(measureProxy, AccessibilityView.Raw);
            if (host is not null) host.ScopeChanged += ScopeChanged;
            target.Unloaded += TargetUnloaded; target.ActualThemeChanged += TargetThemeChanged;
            root.Changed += RootChanged; presenter.LayoutUpdated += LayoutChanged;
            input.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(PreviewKey), true);
            input.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(RootPointer), true);
            FocusManager.GettingFocus += GettingFocus;
            FocusManager.GotFocus += GotFocus;
            foreach (var property in new[] { FrameworkElement.WidthProperty, FrameworkElement.HeightProperty, FrameworkElement.MinWidthProperty, FrameworkElement.MinHeightProperty, FrameworkElement.MaxWidthProperty, FrameworkElement.MaxHeightProperty, FrameworkElement.FlowDirectionProperty, FrameworkElement.LanguageProperty, FrameworkElement.DataContextProperty, FrameworkElement.RequestedThemeProperty, Control.PaddingProperty, Control.FontSizeProperty, Control.FontFamilyProperty })
                callbacks.Add((property, presenter.RegisterPropertyChangedCallback(property, (_, _) => { menuSurface?.InvalidateMeasure(); QueueUpdate(); })));
        }
        internal bool Interactive => !disposed && (host is not null ? branch is { } attached && host.IsBranchAttached(attached) && attached.Frame.IsHitTestVisible : presentationPopup?.IsOpen == true);
        internal bool InputEligible => Interactive && menuSurface is not null && BleeckerPopupScope.AllowsInput(root, menuSurface);
        private bool Topmost => openSessions.TryGetValue(root, out var items) && ReferenceEquals(items.LastOrDefault(s => s.InputEligible), this);
        private void ScopeChanged() { if (menuSurface is not null) FrameworkElementAutomationPeer.FromElement(menuSurface)?.InvalidatePeer(); QueueUpdate(); }
        private void TargetUnloaded(object sender, RoutedEventArgs args) => QueueUpdate();
        private void TargetThemeChanged(FrameworkElement sender, object args) => QueueUpdate();
        private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) { menuSurface?.InvalidateMeasure(); QueueUpdate(); }
        private void LayoutChanged(object? sender, object args) { if (!updating) { ObserveContent(); UpdatePosition(); } }
        private void ContentMeasureChanged(DependencyObject sender, DependencyProperty property) => menuSurface?.InvalidateMeasure();
        private void ContentSizeChanged(object sender, SizeChangedEventArgs args) => menuSurface?.InvalidateMeasure();
        private void ObserveContent()
        {
            if (disposed || surface is null) return;
            var current = Elements(surface).ToHashSet();
            foreach (var old in contentCallbacks.Keys.Where(e => !current.Contains(e)).ToArray()) StopContent(old);
            foreach (var element in current.Where(e => !contentCallbacks.ContainsKey(e)))
            {
                var properties = new List<DependencyProperty> { FrameworkElement.WidthProperty, FrameworkElement.HeightProperty, FrameworkElement.MinWidthProperty, FrameworkElement.MinHeightProperty, FrameworkElement.MaxWidthProperty, FrameworkElement.MaxHeightProperty, FrameworkElement.MarginProperty, FrameworkElement.StyleProperty };
                if (element is Control) properties.AddRange([Control.PaddingProperty, Control.FontFamilyProperty, Control.FontSizeProperty, Control.FontWeightProperty, Control.TemplateProperty]);
                if (element is MenuFlyoutItem) properties.Add(MenuFlyoutItem.TextProperty);
                if (element is BleeckerMenuContentItem) properties.Add(BleeckerMenuContentItem.ContentProperty);
                if (element is TextBlock) properties.AddRange([TextBlock.TextProperty, TextBlock.FontSizeProperty, TextBlock.FontFamilyProperty, TextBlock.FontWeightProperty, TextBlock.CharacterSpacingProperty, TextBlock.LineHeightProperty, TextBlock.TextWrappingProperty]);
                element.SizeChanged += ContentSizeChanged;
                contentCallbacks.Add(element, properties.Distinct().Select(p => (p, element.RegisterPropertyChangedCallback(p, ContentMeasureChanged))).ToList());
            }
        }
        private void StopContent(FrameworkElement element)
        {
            element.SizeChanged -= ContentSizeChanged;
            foreach (var (property, token) in contentCallbacks[element]) element.UnregisterPropertyChangedCallback(property, token);
            contentCallbacks.Remove(element);
        }
        internal void QueueUpdate()
        {
            if (disposed || queued) return; queued = true;
            if (!presenter.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                queued = false; if (disposed) return;
                if (host is { IsLoaded: false } || !target.IsLoaded || !ReferenceEquals(target.XamlRoot, root) || branch is { } existing && host?.IsBranchAttached(existing) != true) { Retire(); return; }
                if (slot is null && presenter.IsLoaded) Mount();
                UpdatePosition(); TryInitialFocus();
            })) throw new InvalidOperationException("Context Menu could not schedule its shared surface.");
        }
        private static IEnumerable<FrameworkElement> Elements(DependencyObject parent)
        {
            if (parent is FrameworkElement element) yield return element;
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) foreach (var child in Elements(VisualTreeHelper.GetChild(parent, i))) yield return child;
        }
        private void Mount()
        {
            if (disposed || mounting || slot is not null) return; mounting = true;
            try
            {
                presenter.ApplyTemplate();
                var candidate = Elements(presenter).OfType<Grid>().SingleOrDefault(e => e.Name == "PART_ContextPortalSlot")
                    ?? throw new InvalidOperationException("A hosted Context Menu presenter template requires PART_ContextPortalSlot.");
                var original = candidate.Children.OfType<BleeckerOverlaySurface>().SingleOrDefault(e => e.Name == "PART_ContextSurface")
                    ?? throw new InvalidOperationException("A hosted Context Menu presenter template requires PART_ContextSurface.");
                popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(root).SingleOrDefault(p => ReferenceEquals(p.Child, presenter));
                if (popup is null || !popup.IsOpen) return;
                var bounds = presenter.TransformToVisual(input).TransformBounds(new Rect(0, 0, presenter.ActualWidth, presenter.ActualHeight));
                if (bounds.Width <= 0 || bounds.Height <= 0) { QueueUpdate(); return; }
                priorAccessibilityView = presenter.ReadLocalValue(AutomationProperties.AccessibilityViewProperty);
                priorPopupTheme = popup.ReadLocalValue(FrameworkElement.RequestedThemeProperty);
                priorLightDismiss = popup.IsLightDismissEnabled; priorHitTest = popup.IsHitTestVisible;
                slot = candidate; surface = original; measureProxy.Width = bounds.Width; measureProxy.Height = bounds.Height;
                measureProxy.UseLayoutRounding = presenter.UseLayoutRounding;
                slot.Children.Remove(surface); slot.Children.Add(measureProxy);
                menuSurface = new MenuSurface(this, surface) { Name = "BleeckerContextMenuPortal", Width = bounds.Width, Height = bounds.Height, IsTabStop = true, TabFocusNavigation = KeyboardNavigationMode.Cycle, UseLayoutRounding = presenter.UseLayoutRounding };
                menuSurface.Mount();
                plane.Width = root.Size.Width; plane.Height = root.Size.Height; plane.Children.Add(menuSurface);
                popup.IsLightDismissEnabled = false; popup.IsHitTestVisible = false;
                AutomationProperties.SetAccessibilityView(presenter, AccessibilityView.Raw);
                plane.PointerPressed += PlanePointer;
                menuSurface.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(PreviewKey), true);
                menuSurface.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(PopupPointer), true);
                if (host is not null) branch = host.AttachPopupBranch(target, plane, new Point());
                else
                {
                    presentationPopup = new Popup { XamlRoot = root, Child = plane, ShouldConstrainToRootBounds = true, IsLightDismissEnabled = false, FlowDirection = FlowDirection.LeftToRight };
                    presentationPopup.IsOpen = true;
                }
                openSessions.GetOrCreateValue(root).Add(this);
                scopeObserver = BleeckerPopupScope.Observe(root, ScopeChanged);
                ApplyModality();
                ObserveContent();
                focusPending = true; UpdatePosition();
                menuSurface.InvalidateMeasure();
            }
            finally { mounting = false; }
        }
        internal void ModalityChanged() { if (disposed) return; if (menuSurface is not null) ApplyModality(); QueueUpdate(); }
        private void ApplyModality()
        {
            if (menuSurface is null) return;
            plane.Background = menu.IsModal ? new SolidColorBrush(Microsoft.UI.Colors.Transparent) : null;
            scope?.Dispose();
            scope = BleeckerPopupScope.Attach(target, menuSurface, menu.IsModal, () => Interactive);
            FrameworkElementAutomationPeer.FromElement(menuSurface)?.InvalidatePeer();
        }
        internal Size MeasureConstraint()
        {
            var width = Math.Min(root.Size.Width, presenter.MaxWidth); var height = Math.Min(root.Size.Height, presenter.MaxHeight);
            if (!double.IsNaN(presenter.Width)) width = Math.Min(width, presenter.Width);
            if (!double.IsNaN(presenter.Height)) height = Math.Min(height, presenter.Height);
            return new Size(Math.Max(0, width), Math.Max(0, height));
        }
        internal void Measured(Size desired)
        {
            if (disposed) return;
            var width = Math.Max(presenter.MinWidth, desired.Width); var height = Math.Max(presenter.MinHeight, desired.Height);
            if (Math.Abs(measureProxy.Width - width) > .0001) measureProxy.Width = width;
            if (Math.Abs(measureProxy.Height - height) > .0001) measureProxy.Height = height;
        }
        private void UpdatePosition()
        {
            if (disposed || updating || menuSurface is null || surface is null || popup is null || !Interactive || !presenter.IsLoaded) return;
            updating = true;
            try
            {
                if (plane.Width != root.Size.Width) plane.Width = root.Size.Width;
                if (plane.Height != root.Size.Height) plane.Height = root.Size.Height;
                var theme = presenter.RequestedTheme == ElementTheme.Default ? target.ActualTheme : presenter.RequestedTheme;
                if (popup.RequestedTheme != theme) popup.RequestedTheme = theme;
                if (menuSurface.RequestedTheme != theme) menuSurface.RequestedTheme = theme;
                if (presentationPopup is not null && presentationPopup.RequestedTheme != theme) presentationPopup.RequestedTheme = theme;
                menuSurface.IsHitTestVisible = InputEligible;
                if (menuSurface.FlowDirection != presenter.FlowDirection) menuSurface.FlowDirection = presenter.FlowDirection;
                if (menuSurface.Language != presenter.Language) menuSurface.Language = presenter.Language;
                if (!ReferenceEquals(menuSurface.DataContext, presenter.DataContext)) menuSurface.DataContext = presenter.DataContext;
                var bounds = presenter.TransformToVisual(input).TransformBounds(new Rect(0, 0, presenter.ActualWidth, presenter.ActualHeight));
                var transform = plane.TransformToVisual(input).Inverse ?? throw new InvalidOperationException("Context Menu requires an invertible shared plane.");
                var local = transform.TransformBounds(bounds);
                if (double.IsNaN(menuSurface.Width) || Math.Abs(menuSurface.Width - local.Width) > .0001) menuSurface.Width = local.Width;
                if (double.IsNaN(menuSurface.Height) || Math.Abs(menuSurface.Height - local.Height) > .0001) menuSurface.Height = local.Height;
                if (Canvas.GetLeft(menuSurface) != local.X) Canvas.SetLeft(menuSurface, local.X);
                if (Canvas.GetTop(menuSurface) != local.Y) Canvas.SetTop(menuSurface, local.Y);
            }
            finally { updating = false; }
        }
        private static bool Within(DependencyObject? node, DependencyObject root)
        {
            for (; node is not null; node = node is TextElement text ? text.ElementStart.Parent : VisualTreeHelper.GetParent(node)) if (ReferenceEquals(node, root)) return true;
            return false;
        }
        private bool Contains(DependencyObject? node) => menuSurface is not null && BleeckerPopupScope.Contains(root, menuSurface, node);
        private void PlanePointer(object sender, PointerRoutedEventArgs args)
        {
            if (!menu.IsModal || !InputEligible || !Topmost || Contains(args.OriginalSource as DependencyObject)) return;
            args.Handled = true; menu.Hide();
        }
        private void RootPointer(object sender, PointerRoutedEventArgs args)
        {
            if (menu.IsModal || !InputEligible || !Topmost || Contains(args.OriginalSource as DependencyObject)) return;
            menu.Hide(); // Nonmodal outside input continues to the original target.
        }
        private void PopupPointer(object sender, PointerRoutedEventArgs args)
        {
            if (host is not null || !openSessions.TryGetValue(root, out var items)) return;
            foreach (var session in items.ToArray()) if (!ReferenceEquals(session, this)) session.RootPointer(sender, args);
        }
        private void PreviewKey(object sender, KeyRoutedEventArgs args)
        {
            if (disposed || menuSurface is null) return;
            if (!Interactive) { if (Contains(args.OriginalSource as DependencyObject)) args.Handled = true; return; }
            if (args.Handled || !InputEligible || !Topmost) return;
            if (args.Key == VirtualKey.Escape) { args.Handled = true; if (!BleeckerTooltip.DismissForEscape(root)) menu.Hide(); }
            else if (args.Key == VirtualKey.Tab) args.Handled = true;
        }
        private void GettingFocus(object? sender, GettingFocusEventArgs args)
        {
            if (disposed || args.NewFocusedElement is not DependencyObject element) return;
            var elementRoot = element switch { UIElement ui => ui.XamlRoot, TextElement text => text.XamlRoot, _ => null };
            if (!ReferenceEquals(elementRoot, root)) return;
            if (menuSurface is null)
            {
                if (Within(element, presenter)) { args.TryCancel(); QueueUpdate(); }
                return;
            }
            if (menu.IsModal && InputEligible && scope is not null && BleeckerPopupScope.IsActiveTrap(scope) && !Contains(element)) args.TryCancel();
        }
        private void GotFocus(object? sender, FocusManagerGotFocusEventArgs args)
        {
            if (disposed || menu.IsModal || !InputEligible || !Topmost || !ReferenceEquals(args.NewFocusedElement, FocusManager.GetFocusedElement(root)) || Contains(args.NewFocusedElement)) return;
            menu.Hide();
        }
        private async void TryInitialFocus()
        {
            if (!focusPending || !Interactive || root.IsHostVisible != true || menuSurface is null) return;
            focusPending = false;
            if (Contains(FocusManager.GetFocusedElement(root) as DependencyObject)) return;
            var first = menu.Items.OfType<Control>().FirstOrDefault(item => item.IsEnabled && item.IsTabStop && item.Visibility == Visibility.Visible);
            if (!disposed && Interactive) await FocusManager.TryFocusAsync(first ?? (DependencyObject)menuSurface, FocusState.Programmatic);
        }
        private void Retire()
        {
            // Structural owner retirement wins over a caller's canceled close.
            // Closing the original marker preserves native Closed and item cleanup.
            if (popup?.IsOpen == true) popup.IsOpen = false;
            else menu.Hide();
            if (!disposed) Dispose();
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (host is not null) host.ScopeChanged -= ScopeChanged;
            target.Unloaded -= TargetUnloaded; target.ActualThemeChanged -= TargetThemeChanged;
            root.Changed -= RootChanged; presenter.LayoutUpdated -= LayoutChanged;
            input.RemoveHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(PreviewKey)); FocusManager.GettingFocus -= GettingFocus;
            input.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(RootPointer)); FocusManager.GotFocus -= GotFocus;
            foreach (var (property, token) in callbacks) presenter.UnregisterPropertyChangedCallback(property, token);
            callbacks.Clear(); scopeObserver?.Dispose(); scopeObserver = null; scope?.Dispose(); scope = null;
            if (openSessions.TryGetValue(root, out var sessions)) sessions.Remove(this);
            foreach (var element in contentCallbacks.Keys.ToArray()) StopContent(element);
            plane.PointerPressed -= PlanePointer;
            if (branch is not null && host?.IsBranchAttached(branch) == true) host.DetachBranch(branch); branch = null;
            if (menuSurface is not null)
            {
                menuSurface.RemoveHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(PreviewKey));
                menuSurface.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(PopupPointer));
            }
            if (presentationPopup is not null) { presentationPopup.IsOpen = false; presentationPopup.Child = null; presentationPopup = null; }
            if (menuSurface is not null && surface is not null) menuSurface.Children.Remove(surface);
            plane.Children.Clear();
            if (slot is not null && surface is not null)
            {
                slot.Children.Remove(measureProxy);
                if (VisualTreeHelper.GetParent(surface) is not null) throw new InvalidOperationException("Retired Context Menu surface retained a visual parent.");
                slot.Children.Add(surface);
                Restore(presenter, AutomationProperties.AccessibilityViewProperty, priorAccessibilityView!);
            }
            if (popup is not null && slot is not null)
            {
                popup.IsLightDismissEnabled = priorLightDismiss; popup.IsHitTestVisible = priorHitTest;
                Restore(popup, FrameworkElement.RequestedThemeProperty, priorPopupTheme!);
            }
            slot = null; surface = null; menuSurface = null; popup = null;
        }
        private static void Restore(DependencyObject owner, DependencyProperty property, object value)
        {
            if (ReferenceEquals(value, DependencyProperty.UnsetValue)) owner.ClearValue(property); else owner.SetValue(property, value);
        }
        internal string AccessibleName() => FrameworkElementAutomationPeer.CreatePeerForElement(presenter).GetName();
        internal IEnumerable<AutomationPeer> ItemPeers() => menu.Items.OfType<FrameworkElement>().Where(e => e.IsLoaded && e.Visibility == Visibility.Visible).Select(FrameworkElementAutomationPeer.CreatePeerForElement).Where(p => p is not null);
    }

    private sealed class MenuSurface(PortalSession owner, BleeckerOverlaySurface surface) : Panel
    {
        internal PortalSession Session { get; } = owner;
        internal void Mount() { if (Children.Count == 0) Children.Add(surface); }
        protected override Size MeasureOverride(Size availableSize)
        {
            Mount(); surface.Measure(Session.MeasureConstraint()); var desired = surface.DesiredSize; Session.Measured(desired);
            // Natural sizing feeds the native placement engine. The displayed
            // surface uses that engine's actual rounded box, just as it did
            // before projection; a fractional natural width must not enlarge it.
            var arranged = new Size(double.IsNaN(Width) ? desired.Width : Width, double.IsNaN(Height) ? desired.Height : Height);
            surface.Measure(arranged); return arranged;
        }
        protected override Size ArrangeOverride(Size finalSize) { surface.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height)); return finalSize; }
        protected override AutomationPeer OnCreateAutomationPeer() => new MenuSurfacePeer(this);
    }
    private sealed class MenuSurfacePeer(MenuSurface owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerContextMenu);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Menu;
        protected override string GetNameCore() => owner.Session.AccessibleName();
        protected override bool IsControlElementCore() => owner.Session.InputEligible;
        protected override bool IsContentElementCore() => owner.Session.InputEligible;
        protected override IList<AutomationPeer> GetChildrenCore() => owner.Session.InputEligible ? owner.Session.ItemPeers().ToArray() : [];
    }
}
