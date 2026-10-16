using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using VirtualKey = Windows.System.VirtualKey;

namespace Orchard;

public sealed partial class BleeckerDataTableColumnToggle
{
    private UIElement? inputRoot;
    private PointerEventHandler? outsidePress, pointerRelease;
    private KeyEventHandler? openKeys;
    private RoutedEventHandler? outsideFocus;
    private (Rect Anchor, double Width, double Height, double RootWidth, double RootHeight, FlowDirection Direction)? position;
    private void InitializePopupInput()
    {
        outsidePress = (_, args) => { if (!IsInside(args.OriginalSource, trigger) && !IsInside(args.OriginalSource, dialog)) Close(false); };
        pointerRelease = (_, _) => { foreach (var row in rows) row.LabelPressed = false; };
        outsideFocus = (_, args) => { if (popup?.IsOpen == true && !updating && !initialFocusPending && !ReferenceEquals(args.OriginalSource, popup) && !IsInside(args.OriginalSource, dialog) && !IsInside(args.OriginalSource, trigger)) Close(false); };
        openKeys = (_, args) =>
        {
            if (popup?.IsOpen != true || args.Handled) return;
            if (args.Key == VirtualKey.Escape) { args.Handled = true; if (!DispatcherQueue.TryEnqueue(() => Close(true))) throw new InvalidOperationException("Column picker could not schedule keyboard dismissal."); return; }
            var focused = FocusManager.GetFocusedElement(XamlRoot);
            if (args.Key != VirtualKey.Tab || !IsInside(focused, dialog)) return;
            var checks = rows.Where(row => row.Checkbox.IsEnabled).Select(row => row.Checkbox).ToArray();
            var reverse = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;
            var index = Array.FindIndex(checks, item => ReferenceEquals(item, focused));
            if (checks.Length == 0) { args.Handled = true; return; }
            var next = reverse ? index <= 0 ? checks.Length - 1 : index - 1 : (index + 1) % checks.Length;
            checks[next].Focus(FocusState.Keyboard); args.Handled = true;
        };
    }
    private static bool IsInside(object? node, DependencyObject? ancestor)
    {
        if (ancestor is null) return false;
        while (node is DependencyObject current) { if (ReferenceEquals(current, ancestor)) return true; node = VisualTreeHelper.GetParent(current); }
        return false;
    }
    private void Open()
    {
        if (!IsLoaded || !IsEnabled || popup is null || dialog is null) return;
        Refresh(); popup.XamlRoot = XamlRoot; dialog.RequestedTheme = ActualTheme; dialog.FlowDirection = FlowDirection;
        initialFocusPending = true; popup.IsOpen = true;
    }
    private void Close(bool focus)
    {
        if (popup?.IsOpen != true) { DetachPopupInput(); return; }
        restoreFocus = focus;
        DetachPopupInput(); popup.IsOpen = false;
    }
    private void PopupOpened(object? sender, object args)
    {
        inputRoot = XamlRoot.Content; inputRoot.AddHandler(UIElement.PointerPressedEvent, outsidePress!, true); inputRoot.AddHandler(UIElement.PointerReleasedEvent, pointerRelease!, true); inputRoot.AddHandler(UIElement.KeyDownEvent, openKeys!, true); inputRoot.GotFocus += outsideFocus;
        LayoutUpdated += OpenLayout; position = null; UpdatePosition();
        void FocusInitial()
        {
            if (popup?.IsOpen != true || dialog is null) return;
            var target = rows.Count > 0 ? (Control)rows[0].Checkbox : dialog;
            if (!target.Focus(openingFromKeyboard ? FocusState.Keyboard : FocusState.Pointer)) throw new InvalidOperationException("Column picker could not focus its source initial target.");
            initialFocusPending = false;
        }
        if (rows.Count == 0 || rows[0].Checkbox.IsLoaded) FocusInitial();
        else if (!DispatcherQueue.TryEnqueue(FocusInitial)) throw new InvalidOperationException("Column picker could not schedule initial focus.");
        NotifyExpansion(false, true);
    }
    private void PopupClosed(object? sender, object args)
    {
        DetachPopupInput(); initialFocusPending = false;
        if (restoreFocus && IsLoaded && IsEnabled) trigger?.Focus(FocusState.Programmatic); restoreFocus = false; NotifyExpansion(true, false);
    }
    private void DetachPopupInput()
    {
        LayoutUpdated -= OpenLayout; position = null;
        if (inputRoot is not null) { inputRoot.RemoveHandler(UIElement.PointerPressedEvent, outsidePress!); inputRoot.RemoveHandler(UIElement.PointerReleasedEvent, pointerRelease!); inputRoot.RemoveHandler(UIElement.KeyDownEvent, openKeys!); inputRoot.GotFocus -= outsideFocus; inputRoot = null; }
        foreach (var row in rows) row.LabelPressed = false;
    }
    private void SurfaceChanged(object sender, SizeChangedEventArgs args) => UpdatePosition();
    private void OpenLayout(object? sender, object args) => UpdatePosition();
    private void UpdatePosition()
    {
        if (popup?.IsOpen != true || trigger is null || surface is null || XamlRoot is null) return;
        surface.UpdateLayout();
        var root = XamlRoot.Content; var anchor = trigger.TransformToVisual(root).TransformBounds(new Rect(0, 0, trigger.ActualWidth, trigger.ActualHeight));
        var width = surface.ActualWidth; var height = surface.ActualHeight; if (width <= 0 || height <= 0) return;
        var next = (anchor, width, height, XamlRoot.Size.Width, XamlRoot.Size.Height, FlowDirection); if (position == next) return; position = next;
        var x = FlowDirection == FlowDirection.RightToLeft ? anchor.Left : anchor.Right - width;
        x = Math.Max(12, Math.Min(x, XamlRoot.Size.Width - 12 - width));
        var y = anchor.Bottom + 4;
        if (y + height > XamlRoot.Size.Height - 12 && anchor.Top - 4 >= XamlRoot.Size.Height - anchor.Bottom - 4) y = anchor.Top - 4 - height;
        y = Math.Max(12, Math.Min(y, XamlRoot.Size.Height - 12 - height));
        // A template-owned Popup's offsets start at its local layout origin.
        var origin = host!.TransformToVisual(root).TransformPoint(new Point()); var localX = x - origin.X; var localY = y - origin.Y;
        // Popup offsets are rounded to device pixels independently of a
        // fractional template origin. Retain the actual target alignment in
        // the painted child instead of relaxing the source geometric bound.
        var scale = XamlRoot.RasterizationScale; var roundedX = Math.Round(localX * scale) / scale; var roundedY = Math.Round(localY * scale) / scale;
        popup.HorizontalOffset = roundedX; popup.VerticalOffset = roundedY;
        dialog!.RenderTransform = new TranslateTransform { X = localX - roundedX, Y = localY - roundedY };
    }
    private void NotifyExpansion(bool before, bool after) => FrameworkElementAutomationPeer.FromElement(trigger!)?.RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, before ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed, after ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
    private sealed class PickerDialog : ContentControl
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new DialogPeer(this);
        private sealed class DialogPeer(PickerDialog owner) : FrameworkElementAutomationPeer(owner)
        {
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
            protected override string GetClassNameCore() => "OrchardColumnVisibilityPopover";
            protected override string GetLocalizedControlTypeCore() => "dialog";
        }
    }
    private sealed class TriggerButton : Button
    {
        private readonly BleeckerDataTableColumnToggle owner;
        internal TriggerButton(BleeckerDataTableColumnToggle owner) { this.owner = owner; PointerPressed += (_, _) => owner.openingFromKeyboard = false; KeyDown += (_, args) => { if (args.Key is VirtualKey.Enter or VirtualKey.Space) owner.openingFromKeyboard = true; }; }
        protected override AutomationPeer OnCreateAutomationPeer() => new TriggerPeer(this, owner);
        private sealed class TriggerPeer(TriggerButton button, BleeckerDataTableColumnToggle owner) : ButtonAutomationPeer(button), IExpandCollapseProvider
        {
            protected override object GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.ExpandCollapse ? this : base.GetPatternCore(pattern);
            public ExpandCollapseState ExpandCollapseState => owner.popup?.IsOpen == true ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
            private void Guard() { if (!owner.IsEnabled) throw new ElementNotEnabledException(); if (!owner.IsLoaded || !button.IsLoaded || !ReferenceEquals(owner.trigger, button)) throw new ElementNotAvailableException(); }
            public void Expand() { Guard(); owner.openingFromKeyboard = true; owner.Open(); }
            public void Collapse() { Guard(); owner.Close(true); }
        }
    }
}
