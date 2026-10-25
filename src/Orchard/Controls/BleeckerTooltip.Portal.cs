using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerTooltip
{
    private BleeckerModalHost? modalHost;
    private BleeckerModalLayerHostCore.Branch? modalBranch;
    private BleeckerPopupScope.Registration? popupScope;
    private readonly Border presentationRoot = new() { Name = "TooltipPortalRoot", UseLayoutRounding = false };
    internal bool PresentationMounted => modalBranch is { } branch
        ? modalHost?.IsBranchAttached(branch) == true : popup?.IsOpen == true;
    private bool InteractionAllowed => target is not null && BleeckerPopupScope.AllowsInput(target.XamlRoot, target) &&
        (modalBranch is { } branch ? modalHost?.IsBranchAttached(branch) == true && branch.Frame.IsHitTestVisible :
            BleeckerModalHost.Find(target) is not { } host || host.AllowsFocusTarget(target));

    private void MountPresentation()
    {
        presentationRoot.Child = this;
        presentationRoot.IsHitTestVisible = false;
        AutomationProperties.SetAccessibilityView(presentationRoot, AccessibilityView.Raw);
        modalHost = BleeckerModalHost.Find(target!);
        if (modalHost is null)
        {
            popup = new Popup { XamlRoot = XamlRoot, RequestedTheme = target!.ActualTheme, ShouldConstrainToRootBounds = true, Child = presentationRoot };
            popup.IsOpen = true;
        }
        else
        {
            // Unowned source tooltips use z-180; a modal owner supplies its own
            // overlay+1 ordering. Keep the original content control and peer.
            modalBranch = modalHost.AttachPopupBranch(target!, presentationRoot, new Point(), 180);
            modalBranch.Frame.RequestedTheme = target!.ActualTheme;
            modalHost.ScopeChanged += ModalScopeChanged;
        }
        popupScope = BleeckerPopupScope.Attach(target!, this, false, () => InteractionAllowed, SynchronizeInputScope);
    }
    private void ClosePresentation()
    {
        popupScope?.Dispose(); popupScope = null;
        if (modalHost is not null) modalHost.ScopeChanged -= ModalScopeChanged;
        var branch = modalBranch; modalBranch = null;
        if (branch is not null && modalHost?.IsBranchAttached(branch) == true) modalHost.DetachBranch(branch);
        modalHost = null;
        if (popup is not null) { popup.IsOpen = false; popup.Child = null; }
        presentationRoot.Child = null;
        DetachInput();
    }
    private void ModalScopeChanged()
    {
        if (modalBranch is { } branch && modalHost?.IsBranchAttached(branch) != true)
        {
            IsOpen = false;
            FinishClose();
            return;
        }
        if (!InteractionAllowed) { hoverTimer.Stop(); transitArea = null; }
        SynchronizeInputScope();
    }
    private void SynchronizeInputScope()
    {
        var allowed = InteractionAllowed;
        if (!allowed) { hoverTimer.Stop(); transitArea = null; }
        presentationRoot.IsHitTestVisible = allowed;
    }
    private void SynchronizePresentationTheme()
    {
        if (target is null) return;
        if (popup is not null && popup.RequestedTheme != target.ActualTheme) popup.RequestedTheme = target.ActualTheme;
        if (modalBranch is { } branch && branch.Frame.RequestedTheme != target.ActualTheme) branch.Frame.RequestedTheme = target.ActualTheme;
    }
    private void PositionPresentation(double x, double y)
    {
        if (modalBranch is { } branch) modalHost!.PositionBranch(branch, new Point(x, y));
        else if (popup is not null)
        {
            if (Math.Abs(popup.HorizontalOffset - x) > .01) popup.HorizontalOffset = x;
            if (Math.Abs(popup.VerticalOffset - y) > .01) popup.VerticalOffset = y;
        }
    }
    internal static bool DismissForEscape(XamlRoot root)
    {
        if (!current.TryGetValue(root, out var slot) || slot.Value?.TryGetTarget(out var value) != true || value is not { IsOpen: true } tip || !tip.InteractionAllowed) return false;
        tip.IsOpen = false;
        return true;
    }
    private void OnRootPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args)
    {
        if (!InteractionAllowed || BleeckerPopupScope.Contains(XamlRoot, this, args.OriginalSource as DependencyObject)) return;
        OnTargetPressed(sender, args);
    }
}
