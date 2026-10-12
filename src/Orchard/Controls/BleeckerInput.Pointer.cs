using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Content;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerInput
{
    private UIElement? pointerRoot;
    [ThreadStatic] private static BleeckerInput? pointerCursorOwner;
    private readonly InputSystemCursor textCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
    private readonly InputSystemCursor disabledCursor = InputSystemCursor.Create(InputSystemCursorShape.UniversalNo);

    private void InitializePointer()
    {
        PointerEntered += (_, _) => SetPointerOver(true);
        PointerExited += (_, _) => SetPointerOver(false);
        Loaded += (_, _) => UpdatePointerObservation();
        Unloaded += (_, _) => { DetachPointerObservation(); pointerOver = false; };
        LayoutUpdated += OnPointerLayoutUpdated;
    }

    private void SetPointerOver(bool value)
    {
        if (pointerOver == value) return;
        pointerOver = value;
        UpdateAppearance();
    }

    private void UpdatePointerObservation()
    {
        ProtectedCursor = IsEnabled ? textCursor : disabledCursor;
        var root = IsLoaded && !IsEnabled ? XamlRoot?.Content : null;
        if (ReferenceEquals(root, pointerRoot)) return;
        var enabledUnderPointer = IsEnabled && pointerOver && ReferenceEquals(pointerCursorOwner, this);
        DetachPointerObservation();
        if (enabledUnderPointer) SetCursor(LoadCursorW(IntPtr.Zero, new IntPtr(32513)));
        pointerRoot = root;
        if (pointerRoot is null) return;
        // TextBox can route disabled input to its enclosing scroll chrome.
        // Observe at the root without enabling editing or keyboard focus.
        pointerRoot.AddHandler(PointerMovedEvent, new PointerEventHandler(OnDisabledPointerMoved), true);
        pointerRoot.AddHandler(PointerEnteredEvent, new PointerEventHandler(OnDisabledPointerMoved), true);
        pointerRoot.AddHandler(PointerExitedEvent, new PointerEventHandler(OnDisabledPointerExited), true);
        pointerRoot.AddHandler(PointerCanceledEvent, new PointerEventHandler(OnDisabledPointerCanceled), true);
        CompositionTarget.Rendering += OnPointerLayoutUpdated;
        RefreshDisabledMouse();
    }

    private void DetachPointerObservation()
    {
        RestorePointerCursor();
        CompositionTarget.Rendering -= OnPointerLayoutUpdated;
        pointerRoot?.RemoveHandler(PointerMovedEvent, new PointerEventHandler(OnDisabledPointerMoved));
        pointerRoot?.RemoveHandler(PointerEnteredEvent, new PointerEventHandler(OnDisabledPointerMoved));
        pointerRoot?.RemoveHandler(PointerExitedEvent, new PointerEventHandler(OnDisabledPointerExited));
        pointerRoot?.RemoveHandler(PointerCanceledEvent, new PointerEventHandler(OnDisabledPointerCanceled));
        pointerRoot = null;
    }

    private bool IsDisabledPointerOver(Point position, DependencyObject? source)
    {
        if (pointerRoot is null || !IsLoaded || IsEnabled) return false;
        var point = pointerRoot.TransformToVisual(this).TransformPoint(position);
        if (point.X < 0 || point.Y < 0 || point.X >= ActualWidth || point.Y >= ActualHeight) return false;
        var ancestors = new HashSet<DependencyObject>();
        for (DependencyObject? item = this; item is not null; item = VisualTreeHelper.GetParent(item))
        {
            ancestors.Add(item);
            if (item is UIElement element && (element.Visibility == Visibility.Collapsed || !element.IsHitTestVisible)) return false;
            if (item is UIElement { Clip: RectangleGeometry clip } clipped)
            {
                var local = pointerRoot.TransformToVisual(clipped).TransformPoint(position);
                if (clip.Transform is not null)
                {
                    if (clip.Transform.Inverse is not GeneralTransform inverse) return false;
                    local = inverse.TransformPoint(local);
                }
                if (!clip.Rect.Contains(local)) return false;
            }
            if (item is ScrollContentPresenter viewport)
            {
                var local = pointerRoot.TransformToVisual(viewport).TransformPoint(position);
                if (local.X < 0 || local.Y < 0 || local.X >= viewport.ActualWidth || local.Y >= viewport.ActualHeight) return false;
            }
        }
        if (source is null) return false;
        if (ancestors.Contains(source)) return true;
        for (var item = source; item is not null; item = VisualTreeHelper.GetParent(item))
        {
            if (ReferenceEquals(item, this)) return true;
            if (ancestors.Contains(item))
            {
                // Only native scroll chrome may stand in for a disabled child.
                // A sibling overlay within the content must continue to occlude it.
                var owner = item;
                while (owner is not null && owner is not Control) owner = VisualTreeHelper.GetParent(owner);
                if (owner is not ScrollViewer viewer || viewer.Content is not DependencyObject content || !ancestors.Contains(content)) return false;
                for (var branch = source; branch is not null && !ReferenceEquals(branch, viewer); branch = VisualTreeHelper.GetParent(branch))
                    if (ReferenceEquals(branch, viewer.Content) || branch is Control) return false;
                return true;
            }
            if (item is Control) return false;
        }
        return false;
    }

    private void OnDisabledPointerMoved(object sender, PointerRoutedEventArgs args)
    {
        if (pointerRoot is null) return;
        var position = args.GetCurrentPoint(pointerRoot).Position;
        SetPointerOver(IsDisabledPointerOver(position, args.OriginalSource as DependencyObject));
        UpdatePointerCursor(args.Pointer.PointerDeviceType == PointerDeviceType.Mouse);
    }

    private void UpdatePointerCursor(bool mouse)
    {
        if (pointerOver && mouse)
        {
            SetCursor(LoadCursorW(IntPtr.Zero, new IntPtr(32648)));
            pointerCursorOwner = this;
        }
        else RestorePointerCursor();
    }

    private void RestorePointerCursor()
    {
        if (!ReferenceEquals(pointerCursorOwner, this)) return;
        pointerCursorOwner = null;
        if (GetCursor() != LoadCursorW(IntPtr.Zero, new IntPtr(32648)) || pointerRoot is null) return;
        // The native override does not change WinUI's selected input cursor.
        // Reapply that current value instead of a stale cursor saved on entry.
        var island = ContentIsland.FindAllForCurrentThread().Single(item => item.Environment.Equals(pointerRoot.XamlRoot.ContentIslandEnvironment));
        var source = InputPointerSource.GetForIsland(island);
        var cursor = source.Cursor;
        source.Cursor = null;
        source.Cursor = cursor;
        if (cursor is null) SetCursor(IntPtr.Zero);
        else if (cursor is InputSystemCursor system)
        {
            // WinUI can update its selected cursor without refreshing the native
            // cursor after a stationary-pointer layout change. IDs are from WinUser.h.
            var resource = system.CursorShape switch
            {
                InputSystemCursorShape.Arrow => 32512,
                InputSystemCursorShape.IBeam => 32513,
                InputSystemCursorShape.Wait => 32514,
                InputSystemCursorShape.Cross => 32515,
                InputSystemCursorShape.UpArrow => 32516,
                InputSystemCursorShape.SizeNorthwestSoutheast => 32642,
                InputSystemCursorShape.SizeNortheastSouthwest => 32643,
                InputSystemCursorShape.SizeWestEast => 32644,
                InputSystemCursorShape.SizeNorthSouth => 32645,
                InputSystemCursorShape.SizeAll => 32646,
                InputSystemCursorShape.UniversalNo => 32648,
                InputSystemCursorShape.Hand => 32649,
                InputSystemCursorShape.AppStarting => 32650,
                InputSystemCursorShape.Help => 32651,
                InputSystemCursorShape.Pin => 32671,
                InputSystemCursorShape.Person => 32672,
                _ => throw new ArgumentOutOfRangeException(nameof(system.CursorShape))
            };
            SetCursor(LoadCursorW(IntPtr.Zero, new IntPtr(resource)));
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetCursor();
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SetCursor(IntPtr cursor);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr LoadCursorW(IntPtr instance, IntPtr name);
    private void OnDisabledPointerExited(object sender, PointerRoutedEventArgs args)
    {
        if (ReferenceEquals(args.OriginalSource, pointerRoot)) OnDisabledPointerCanceled(sender, args);
        else OnDisabledPointerMoved(sender, args);
    }

    private void OnDisabledPointerCanceled(object sender, PointerRoutedEventArgs args)
    {
        SetPointerOver(false);
        RestorePointerCursor();
    }

    private void OnPointerLayoutUpdated(object? sender, object args)
    {
        RefreshDisabledMouse();
    }

    private void RefreshDisabledMouse()
    {
        if (pointerRoot is null || !IsLoaded || IsEnabled) return;
        // A scroll or layout change can move the disabled editor away from a
        // stationary pointer without delivering a TextBox pointer event.
        // Query its current screen position rather than retaining event coordinates
        // across window moves, DPI changes or unload/reload.
        var window = Microsoft.UI.Win32Interop.GetWindowFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
        if (!GetCursorPos(out var screen) || GetAncestor(WindowFromPoint(screen), 2) != window || !ScreenToClient(window, ref screen))
        {
            SetPointerOver(false);
            RestorePointerCursor();
            return;
        }
        var scale = XamlRoot.RasterizationScale;
        var position = new Point(screen.X / scale, screen.Y / scale);
        var local = pointerRoot.TransformToVisual(this).TransformPoint(position);
        if (local.X < 0 || local.Y < 0 || local.X >= ActualWidth || local.Y >= ActualHeight)
        {
            SetPointerOver(false);
            RestorePointerCursor();
            return;
        }
        var source = VisualTreeHelper.FindElementsInHostCoordinates(position, pointerRoot).FirstOrDefault();
        SetPointerOver(IsDisabledPointerOver(position, source));
        UpdatePointerCursor(true);
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct NativePointerPoint { public int X; public int Y; }
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePointerPoint point);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool ScreenToClient(IntPtr window, ref NativePointerPoint point);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(NativePointerPoint point);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr window, uint flags);
}
