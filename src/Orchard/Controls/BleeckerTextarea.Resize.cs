using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerTextarea
{
    private UIElement? resizeRoot;
    private Pointer? resizePointer;
    private Point resizeOrigin;
    private double resizeOriginalHeight;
    private readonly InputSystemCursor resizeCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);


    private bool InResizeGrip(Point point)
    {
        if (resizeGrip is null) return false;
        // The editor's RTL coordinates are mirrored, while its chrome template
        // deliberately stays LTR. Transform to the actual grip's coordinates.
        var local = TransformToVisual(resizeGrip).TransformPoint(point);
        return local.X >= 0 && local.Y >= 0 && local.X < resizeGrip.ActualWidth && local.Y < resizeGrip.ActualHeight;
    }

    private void UpdateResizeGripDirection()
    {
        if (resizeGrip is null) return;
        CancelResize();
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        resizeGrip.HorizontalAlignment = rtl ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        resizeGrip.RenderTransformOrigin = new Point(.5, .5);
        resizeGrip.RenderTransform = new ScaleTransform { ScaleX = rtl ? -1 : 1 };
    }

    private void UpdateResizeRoot()
    {
        var next = IsLoaded ? XamlRoot?.Content : null;
        if (ReferenceEquals(next, resizeRoot)) return;
        CancelResize();
        if (resizeRoot is not null)
        {
            resizeRoot.RemoveHandler(PointerPressedEvent, new PointerEventHandler(OnResizePressed));
            resizeRoot.RemoveHandler(PointerMovedEvent, new PointerEventHandler(OnResizeMoved));
            resizeRoot.RemoveHandler(PointerReleasedEvent, new PointerEventHandler(OnResizeReleased));
            resizeRoot.RemoveHandler(PointerCanceledEvent, new PointerEventHandler(OnResizeReleased));
            resizeRoot.RemoveHandler(PointerCaptureLostEvent, new PointerEventHandler(OnResizeReleased));
        }
        resizeRoot = next;
        if (resizeRoot is null) return;
        resizeRoot.AddHandler(PointerPressedEvent, new PointerEventHandler(OnResizePressed), true);
        resizeRoot.AddHandler(PointerMovedEvent, new PointerEventHandler(OnResizeMoved), true);
        resizeRoot.AddHandler(PointerReleasedEvent, new PointerEventHandler(OnResizeReleased), true);
        resizeRoot.AddHandler(PointerCanceledEvent, new PointerEventHandler(OnResizeReleased), true);
        resizeRoot.AddHandler(PointerCaptureLostEvent, new PointerEventHandler(OnResizeReleased), true);
    }

    private void OnResizePressed(object sender, PointerRoutedEventArgs args)
    {
        if (resizeRoot is null || resizePointer is not null || args.Pointer.PointerDeviceType != PointerDeviceType.Mouse ||
            !args.GetCurrentPoint(resizeRoot).Properties.IsLeftButtonPressed) return;
        BleeckerDisabledPointer.RefreshRoot(resizeRoot);
        if (resizePointerObserver?.IsOver != true) return;
        // Capture on the enabled root: disabling text editing must not disable
        // the browser's independent resize affordance or enable the editor.
        resizeOrigin = args.GetCurrentPoint(resizeRoot).Position;
        resizeOriginalHeight = ActualHeight;
        if (!resizeRoot.CapturePointer(args.Pointer)) return;
        resizePointer = args.Pointer;
        args.Handled = true;
        UpdateResizeCursor();
    }

    private void OnResizeMoved(object sender, PointerRoutedEventArgs args)
    {
        if (resizeRoot is null || resizePointer?.PointerId != args.Pointer.PointerId) return;
        // Clamp only the output. Keeping the original pointer anchor preserves
        // the source's dead zone after dragging beyond its minimum height.
        Height = Math.Clamp(resizeOriginalHeight + args.GetCurrentPoint(resizeRoot).Position.Y - resizeOrigin.Y, MinHeight, MaxHeight);
        args.Handled = true;
        UpdateResizeCursor();
    }

    private void OnResizeReleased(object sender, PointerRoutedEventArgs args)
    {
        if (resizePointer?.PointerId != args.Pointer.PointerId) return;
        CancelResize();
        args.Handled = true;
    }

    private void CancelResize()
    {
        var pointer = resizePointer;
        resizePointer = null;
        if (pointer is not null) resizeRoot?.ReleasePointerCapture(pointer);
        UpdateResizeCursor();
    }

    private void UpdateResizeCursor()
    {
        var over = resizePointer is not null || resizePointerObserver?.IsOver == true;
        ProtectedCursor = over ? resizeCursor : IsEnabled ? textCursor : disabledCursor;
        if (IsLoaded && over) BleeckerPointerCursor.Select(this, resizeCursor);
        else if (IsLoaded && disabledPointer?.IsOver == true)
            BleeckerPointerCursor.Select(this, IsEnabled ? textCursor : disabledCursor);
    }

    private sealed class TextareaResizeGrip : Control
    {
        internal TextareaResizeGrip() => ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
        // The root owns the gesture, including disabled controls. Consume the
        // leaf press before RichEdit can focus or select text.
        protected override void OnPointerPressed(PointerRoutedEventArgs args) => args.Handled = true;
        protected override AutomationPeer OnCreateAutomationPeer() => new ResizeGripAutomationPeer(this);
    }

    private sealed class ResizeGripAutomationPeer(TextareaResizeGrip owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Thumb;
        protected override string GetClassNameCore() => "TextareaResizeGrip";
    }
}
