using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

internal sealed partial class BleeckerSelectSurface
{
    private Border? popupRoot;
    private ItemsControl? popupItems;
    private Canvas? modalCanvas;
    private BleeckerModalHost? modalHost;
    private BleeckerModalLayerHostCore.Branch? modalBranch;
    private BleeckerPopupScope.Registration? popupScope;
    private bool openNotified, positioning;
    private bool HostInteractionAllowed => modalBranch is { } branch
        ? modalHost?.IsBranchAttached(branch) == true && branch.Frame.IsHitTestVisible
        : BleeckerModalHost.Find(this) is not { } host || host.AllowsFocusTarget(this);
    private bool InteractionAllowed => HostInteractionAllowed && (popupScope is { } scope ? BleeckerPopupScope.IsActiveTrap(scope) : BleeckerPopupScope.AllowsInput(XamlRoot, this));
    private bool CanTakeFocus => IsLoaded && XamlRoot?.IsHostVisible == true && InteractionAllowed;

    private void SetPresentation(bool value)
    {
        if (positionedPopup is null || popupRoot is null) { if (value) throw new InvalidOperationException("The mounted chooser requires its popup template."); return; }
        if (value)
        {
            if (IsDropDownOpen || !IsLoaded || !IsEnabled || !InteractionAllowed) return;
            modalHost = BleeckerModalHost.Find(this);
            if (modalHost is null) { if (!ReferenceEquals(positionedPopup.Child, popupRoot)) positionedPopup.Child = popupRoot; positionedPopup.IsOpen = true; return; }
            // Radix Select's modal content disables outside pointer input. The
            // transparent canvas catches that first outside press in our plane.
            positionedPopup.Child = null;
            modalCanvas = new Canvas { Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent), FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false, Width = XamlRoot.Size.Width, Height = XamlRoot.Size.Height };
            modalCanvas.PointerPressed += OutsidePointer;
            modalCanvas.Children.Add(popupRoot);
            modalBranch = modalHost.AttachPopupBranch(this, modalCanvas, new Point());
            modalHost.ScopeChanged += ModalScopeChanged;
            LayoutUpdated += PortalLayout;
            PresentationOpened(); QueueOpenPosition();
            return;
        }
        if (modalBranch is { } current)
        {
            modalBranch = null;
            if (modalHost is not null) { modalHost.ScopeChanged -= ModalScopeChanged; if (modalHost.IsBranchAttached(current)) modalHost.DetachBranch(current); }
            LayoutUpdated -= PortalLayout;
            if (modalCanvas is not null) { modalCanvas.PointerPressed -= OutsidePointer; modalCanvas.Children.Clear(); modalCanvas = null; }
            positionedPopup.Child = popupRoot;
            PresentationClosed(); modalHost = null;
        }
        else if (positionedPopup.IsOpen) positionedPopup.IsOpen = false;
    }
    private void ModalScopeChanged()
    {
        if (!InteractionAllowed) { ClearControlledFocus(); StopScroll(); }
        QueueOpenPosition();
    }
    private void PortalLayout(object? sender, object args) => QueueOpenPosition();
    private void OutsidePointer(object sender, PointerRoutedEventArgs args)
    {
        if (!IsDropDownOpen || !InteractionAllowed || Within(args.OriginalSource as DependencyObject, popupRoot)) return;
        args.Handled = true;
        var current = modalBranch;
        if (!DispatcherQueue.TryEnqueue(() => { if (ReferenceEquals(current, modalBranch) && IsDropDownOpen) Close(true); })) throw new InvalidOperationException("Select could not finish outside-pointer dismissal.");
    }
    private void KeepPopupFocus(object? sender, GettingFocusEventArgs args)
    {
        if (!IsDropDownOpen || !CanTakeFocus || args.NewFocusedElement is not DependencyObject target || Within(target, popupRoot)) return;
        var targetRoot = target switch { UIElement element => element.XamlRoot, TextElement text => text.XamlRoot, _ => null };
        if (ReferenceEquals(targetRoot, XamlRoot)) args.TryCancel();
    }
    private static bool Within(DependencyObject? node, DependencyObject? root)
    {
        if (root is null) return false;
        for (; node is not null; node = node is TextElement text ? text.ElementStart.Parent : VisualTreeHelper.GetParent(node)) if (ReferenceEquals(node, root)) return true;
        return false;
    }
    private void PositionHostedContent()
    {
        if (modalCanvas is null || modalBranch is null || popupRoot is null || XamlRoot is not { } root || !modalHost!.IsBranchAttached(modalBranch)) return;
        if (modalCanvas.Width != root.Size.Width) modalCanvas.Width = root.Size.Width;
        if (modalCanvas.Height != root.Size.Height) modalCanvas.Height = root.Size.Height;
        if (modalCanvas.RequestedTheme != ActualTheme) modalCanvas.RequestedTheme = ActualTheme;
        popupRoot.Measure(new Size(popupRoot.Width, double.PositiveInfinity));
        var anchor = TransformToVisual(root.Content).TransformBounds(new Rect(0, 0, ActualWidth, ActualHeight));
        var size = popupRoot.DesiredSize;
        var placement = BleeckerPopoverModel.Place(new(anchor.X, anchor.Y, anchor.Width, anchor.Height), size.Width, size.Height, root.Size.Width, root.Size.Height, BleeckerPopoverSide.Bottom, BleeckerPopoverAlign.Start, 4, (Appearance?.Owner.FlowDirection ?? FlowDirection) == FlowDirection.RightToLeft, root.RasterizationScale, 8);
        if (double.IsNaN(Canvas.GetLeft(popupRoot)) || Canvas.GetLeft(popupRoot) != placement.X) Canvas.SetLeft(popupRoot, placement.X);
        if (double.IsNaN(Canvas.GetTop(popupRoot)) || Canvas.GetTop(popupRoot) != placement.Y) Canvas.SetTop(popupRoot, placement.Y);
    }
}
