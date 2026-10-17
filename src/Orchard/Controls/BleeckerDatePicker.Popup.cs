using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;

namespace Orchard;

public sealed partial class BleeckerDatePicker
{
    private UIElement? inputRoot;
    private XamlRoot? popupRoot;
    private PointerEventHandler? outsidePress;
    private KeyEventHandler? keys;
    private RoutedEventHandler? focus;
    private (Rect Anchor, double Width, double Height, double RootWidth, double RootHeight, double Scale)? position;
    private void Opened(object? sender, object args)
    {
        if (!ReferenceEquals(sender, popup) || calendar is null) return;
        var openingPopup = popup;
        outsidePress = (_, e) => { if (!Inside(e.OriginalSource, dialog) && !Inside(e.OriginalSource, trigger)) Close(false); };
        focus = (_, e) => { if (!initialFocus && IsDropDownOpen && !Inside(e.OriginalSource, dialog) && !Inside(e.OriginalSource, trigger)) Close(false); };
        keys = (_, e) =>
        {
            if (!IsDropDownOpen || e.Handled) return;
            if (e.Key == VirtualKey.Escape) { e.Handled = true; Close(true); return; }
            if (e.Key != VirtualKey.Tab || calendar is null || !Inside(FocusManager.GetFocusedElement(XamlRoot), dialog)) return;
            var controls = new Control[] { calendar.PreviousButton, calendar.NextButton }.Concat(calendar.DayButtons).Concat(clear is { Visibility: Visibility.Visible } ? new Control[] { clear } : []).Where(x => x.IsEnabled).ToArray();
            var reverse = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;
            var index = Array.FindIndex(controls, x => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), x));
            var next = index + (reverse ? -1 : 1);
            e.Handled = true;
            if (next >= 0 && next < controls.Length) controls[next].Focus(FocusState.Keyboard);
            else { Close(true); if (!reverse) FocusManager.TryMoveFocus(FocusNavigationDirection.Next, new FindNextElementOptions { SearchRoot = XamlRoot.Content }); }
        };
        inputRoot = XamlRoot.Content; inputRoot.AddHandler(UIElement.PointerPressedEvent, outsidePress, true); inputRoot.AddHandler(UIElement.KeyDownEvent, keys, true); inputRoot.GotFocus += focus;
        popupRoot = XamlRoot; LayoutUpdated += LayoutChanged; popupRoot.Changed += RootChanged; UpdatePosition();
        void Initial()
        {
            if (!ReferenceEquals(popup, openingPopup) || !IsDropDownOpen || calendar is null) return;
            calendar.ApplyTemplate(); calendar.UpdateLayout(); UpdatePosition();
            if (!calendar.PreviousButton.Focus(keyboardOpening ? FocusState.Keyboard : FocusState.Pointer)) throw new InvalidOperationException("Date popup could not focus Previous month.");
            initialFocus = false;
        }
        if (calendar.IsLoaded) Initial(); else if (!DispatcherQueue.TryEnqueue(Initial)) throw new InvalidOperationException("Date popup could not schedule initial focus.");
        trigger?.RefreshState();
    }
    private void Close(bool restore)
    {
        if (popup is null) return;
        var focusState = (FocusManager.GetFocusedElement(XamlRoot) as Control)?.FocusState == FocusState.Keyboard ? FocusState.Keyboard : FocusState.Programmatic;
        var previousPopup = popup; DetachInput(); previousPopup.IsOpen = false;
        if (ReferenceEquals(popup, previousPopup)) ReleasePopup();
        if (restore && IsLoaded && IsEnabled) trigger?.Focus(focusState);
        trigger?.RefreshState();
    }
    private void Closed(object? sender, object args) { if (ReferenceEquals(sender, popup)) { DetachInput(); ReleasePopup(); trigger?.RefreshState(); } }
    private void ReleasePopup()
    {
        if (calendar is not null) calendar.DateRequested -= Requested;
        if (clear is not null) clear.Click -= ClearClicked;
        if (surface is not null) { surface.SizeChanged -= SurfaceChanged; surface.Content = null; surface.Resources.MergedDictionaries.Remove(palette); }
        if (popup is not null) { popup.Opened -= Opened; popup.Closed -= Closed; popup.Child = null; host?.Children.Remove(popup); }
        content?.Children.Clear(); popup = null; dialog = null; calendar = null; clear = null; content = null; surface = null; initialFocus = false;
    }
    private void DetachInput()
    {
        LayoutUpdated -= LayoutChanged; if (popupRoot is not null) popupRoot.Changed -= RootChanged; popupRoot = null;
        if (inputRoot is not null) { inputRoot.RemoveHandler(UIElement.PointerPressedEvent, outsidePress!); inputRoot.RemoveHandler(UIElement.KeyDownEvent, keys!); inputRoot.GotFocus -= focus; }
        inputRoot = null; outsidePress = null; keys = null; focus = null; position = null;
    }
    private static bool Inside(object? value, DependencyObject? ancestor)
    { if (ancestor is null) return false; while (value is DependencyObject current) { if (ReferenceEquals(current, ancestor)) return true; value = VisualTreeHelper.GetParent(current); } return false; }
    private void LayoutChanged(object? sender, object args) => UpdatePosition();
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdatePosition();
    private void SurfaceChanged(object sender, SizeChangedEventArgs args) => UpdatePosition();
    private void UpdatePosition()
    {
        if (!IsDropDownOpen || surface is null || host is null || trigger is null) return;
        var root = XamlRoot.Content; var anchor = trigger.TransformToVisual(root).TransformBounds(new Rect(0, 0, trigger.ActualWidth, trigger.ActualHeight));
        var width = surface.ActualWidth; var height = surface.ActualHeight; if (width <= 0 || height <= 0) return;
        var state = (anchor, width, height, XamlRoot.Size.Width, XamlRoot.Size.Height, XamlRoot.RasterizationScale); if (position == state) return; position = state;
        var x = FlowDirection == FlowDirection.RightToLeft ? anchor.Right - width : anchor.Left;
        x = Math.Max(12, Math.Min(x, XamlRoot.Size.Width - 12 - width));
        var y = anchor.Bottom + 4;
        if (y + height > XamlRoot.Size.Height - 12 && anchor.Top - 4 > XamlRoot.Size.Height - anchor.Bottom - 4) y = anchor.Top - 4 - height;
        y = Math.Max(12, Math.Min(y, XamlRoot.Size.Height - 12 - height));
        var scale = XamlRoot.RasterizationScale;
        x = Math.Round(x * scale, MidpointRounding.AwayFromZero) / scale;
        y = Math.Round(y * scale, MidpointRounding.AwayFromZero) / scale;
        var origin = host.TransformToVisual(root).TransformPoint(new Point());
        popup!.HorizontalOffset = x - origin.X; popup.VerticalOffset = y - origin.Y;
    }
}
