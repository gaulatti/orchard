using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.UI.Content;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

/// <summary>Observes disabled control geometry without enabling native input.</summary>
internal sealed class BleeckerDisabledPointer(Control owner, Func<FrameworkElement?> bounds, Action<bool> changed)
{
    private static readonly ConditionalWeakTable<UIElement, RootObserver> roots = new();
    private RootObserver? observer;
    private bool lifecycleUpdateQueued;
    internal bool IsOver { get; private set; }
    internal bool OwnsCursor => BleeckerPointerCursor.IsOwner(this);

    internal void Update()
    {
        var root = owner.IsLoaded && !owner.IsEnabled && bounds() is not null ? owner.XamlRoot?.Content : null;
        if (ReferenceEquals(root, observer?.Root)) { observer?.Refresh(); return; }
        // Enabling retains the hover fact until native enabled input takes over.
        Detach(clearHover: root is not null || !owner.IsEnabled);
        if (root is null) return;
        observer = roots.GetValue(root, value => new RootObserver(value));
        observer.Add(this);
    }

    internal void ReconcileLifecycle()
    {
        if (lifecycleUpdateQueued) return;
        lifecycleUpdateQueued = true;
        // A rapid remove/add can deliver Unloaded after the element is already
        // loaded in its new parent. Reconcile the authoritative current state
        // once the queued lifecycle notifications have completed.
        if (!owner.DispatcherQueue.TryEnqueue(() => { lifecycleUpdateQueued = false; Update(); })) lifecycleUpdateQueued = false;
    }

    internal void Detach(bool clearHover = true)
    {
        var previous = observer;
        observer = null;
        previous?.Remove(this);
        if (clearHover) SetOver(false, false, previous?.Root);
    }

    private void SetOver(bool value, bool mouse, UIElement? root)
    {
        if (IsOver != value) { IsOver = value; changed(value); }
        if (value && mouse && root is not null) BleeckerPointerCursor.Block(this, root);
        else if (root is not null) BleeckerPointerCursor.Release(this);
    }

    private bool Contains(Point position, UIElement root)
    {
        if (!owner.IsLoaded || owner.IsEnabled || bounds() is not FrameworkElement target) return false;
        var point = root.TransformToVisual(target).TransformPoint(position);
        if (target is Border or Control)
        {
            var radius = target is Border border ? border.CornerRadius : ((Control)target).CornerRadius;
            return BleeckerRoundedPointerHit.Contains(target.ActualWidth, target.ActualHeight,
                radius.TopLeft, radius.TopRight, radius.BottomRight, radius.BottomLeft, point.X, point.Y);
        }
        return point.X >= 0 && point.Y >= 0 && point.X < target.ActualWidth && point.Y < target.ActualHeight;
    }

    private bool Hit(Point position, DependencyObject? source, UIElement root)
    {
        if (!Contains(position, root) || bounds() is not FrameworkElement target) return false;
        // The branded box itself may be decorative and not hit-testable.
        // Visibility/clipping/occlusion belong to its owning native control.
        var ancestors = new HashSet<DependencyObject>();
        var ownerSeen = false;
        for (DependencyObject? item = target; item is not null; item = VisualTreeHelper.GetParent(item))
        {
            ancestors.Add(item);
            ownerSeen |= ReferenceEquals(item, owner);
            if (item is UIElement element && (element.Visibility == Visibility.Collapsed || ownerSeen && !element.IsHitTestVisible)) return false;
            if (item is UIElement { Clip: RectangleGeometry clip } clipped)
            {
                var local = root.TransformToVisual(clipped).TransformPoint(position);
                if (clip.Transform is not null)
                {
                    if (clip.Transform.Inverse is not GeneralTransform inverse) return false;
                    local = inverse.TransformPoint(local);
                }
                if (!clip.Rect.Contains(local)) return false;
            }
            if (item is ScrollContentPresenter viewport)
            {
                var local = root.TransformToVisual(viewport).TransformPoint(position);
                if (local.X < 0 || local.Y < 0 || local.X >= viewport.ActualWidth || local.Y >= viewport.ActualHeight) return false;
            }
        }
        if (source is null) return false;
        if (ancestors.Contains(source)) return true;
        for (var item = source; item is not null; item = VisualTreeHelper.GetParent(item))
        {
            if (ReferenceEquals(item, owner)) return true;
            if (ancestors.Contains(item))
            {
                // Native scroll chrome can stand in for a disabled child;
                // a sibling overlay within its content continues to occlude it.
                var control = item;
                while (control is not null && control is not Control) control = VisualTreeHelper.GetParent(control);
                if (control is not ScrollViewer viewer || viewer.Content is not DependencyObject content || !ancestors.Contains(content)) return false;
                for (var branch = source; branch is not null && !ReferenceEquals(branch, viewer); branch = VisualTreeHelper.GetParent(branch))
                    if (ReferenceEquals(branch, viewer.Content) || branch is Control) return false;
                return true;
            }
            if (item is Control) return false;
        }
        return false;
    }

    private sealed class RootObserver(UIElement root)
    {
        private readonly List<BleeckerDisabledPointer> clients = [];
        private BleeckerDisabledPointer[] snapshot = [];
        private bool refreshQueued;
        internal UIElement Root => root;

        internal void Add(BleeckerDisabledPointer client)
        {
            if (clients.Count == 0)
            {
                root.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(OnMoved), true);
                root.AddHandler(UIElement.PointerEnteredEvent, new PointerEventHandler(OnMoved), true);
                root.AddHandler(UIElement.PointerExitedEvent, new PointerEventHandler(OnExited), true);
                root.AddHandler(UIElement.PointerCanceledEvent, new PointerEventHandler(OnCanceled), true);
                if (root is FrameworkElement element) element.LayoutUpdated += OnLayout;
                CompositionTarget.Rendering += OnLayout;
            }
            clients.Add(client);
            snapshot = clients.ToArray();
            Refresh();
        }

        internal void Remove(BleeckerDisabledPointer client)
        {
            BleeckerPointerCursor.Release(client);
            clients.Remove(client);
            snapshot = clients.ToArray();
            if (clients.Count != 0) return;
            root.RemoveHandler(UIElement.PointerMovedEvent, new PointerEventHandler(OnMoved));
            root.RemoveHandler(UIElement.PointerEnteredEvent, new PointerEventHandler(OnMoved));
            root.RemoveHandler(UIElement.PointerExitedEvent, new PointerEventHandler(OnExited));
            root.RemoveHandler(UIElement.PointerCanceledEvent, new PointerEventHandler(OnCanceled));
            if (root is FrameworkElement element) element.LayoutUpdated -= OnLayout;
            CompositionTarget.Rendering -= OnLayout;
        }

        private void OnLayout(object? sender, object args) => Refresh();
        private void OnMoved(object sender, PointerRoutedEventArgs args)
        {
            Apply(args.GetCurrentPoint(root).Position, args.OriginalSource as DependencyObject,
                args.Pointer.PointerDeviceType == PointerDeviceType.Mouse);
            if (refreshQueued) return;
            refreshQueued = true;
            if (!root.DispatcherQueue.TryEnqueue(() => { refreshQueued = false; if (clients.Count != 0) Refresh(); })) refreshQueued = false;
        }
        private void OnExited(object sender, PointerRoutedEventArgs args)
        {
            if (ReferenceEquals(args.OriginalSource, root)) Clear();
            else OnMoved(sender, args);
        }
        private void OnCanceled(object sender, PointerRoutedEventArgs args) => Clear();
        private void Clear() { foreach (var client in snapshot) client.SetOver(false, false, root); }
        private void Apply(Point position, DependencyObject? source, bool mouse)
        {
            foreach (var client in snapshot) client.SetOver(client.Hit(position, source, root), mouse, root);
        }

        internal void Refresh()
        {
            if (root.XamlRoot is not XamlRoot xamlRoot) { Clear(); return; }
            var window = Microsoft.UI.Win32Interop.GetWindowFromWindowId(xamlRoot.ContentIslandEnvironment.AppWindowId);
            if (!GetCursorPos(out var screen) || GetAncestor(WindowFromPoint(screen), 2) != window || !ScreenToClient(window, ref screen)) { Clear(); return; }
            var position = new Point(screen.X / xamlRoot.RasterizationScale, screen.Y / xamlRoot.RasterizationScale);
            // All disabled controls share one native pointer query and one hit
            // test per observation, including stationary-pointer layout changes.
            var source = snapshot.Any(client => client.Contains(position, root))
                ? VisualTreeHelper.FindElementsInHostCoordinates(position, root).FirstOrDefault() : null;
            Apply(position, source, true);
        }
    }

    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X; public int Y; }
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ScreenToClient(IntPtr window, ref NativePoint point);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(NativePoint point);
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);
}

/// <summary>One native override owner across all Orchard disabled controls.</summary>
internal static class BleeckerPointerCursor
{
    private static readonly ConditionalWeakTable<UIElement, RootCursor> roots = new();
    [ThreadStatic] private static object? owner;
    [ThreadStatic] private static RootCursor? active;
    internal static bool IsOwner(object candidate) => ReferenceEquals(owner, candidate);
    internal static void Block(object candidate, UIElement root)
    {
        var selection = roots.GetValue(root, value => new RootCursor(value));
        if (!ReferenceEquals(active, selection)) active?.Restore();
        active = selection; owner = candidate;
        selection.Block();
    }
    internal static void Release(object candidate)
    {
        if (!IsOwner(candidate)) return;
        owner = null;
        active?.Restore(); active = null;
    }

    internal static void Select(UIElement control, InputCursor cursor)
    {
        if (control.XamlRoot?.Content is UIElement root) Source(root).Cursor = cursor;
    }

    private static InputPointerSource Source(UIElement root)
    {
        var environment = root.XamlRoot.ContentIslandEnvironment;
        var island = ContentIsland.FindAllForCurrentThread().Single(item => item.Environment.Equals(environment));
        return InputPointerSource.GetForIsland(island);
    }

    private sealed class RootCursor(UIElement root)
    {
        private readonly InputSystemCursor blocked = InputSystemCursor.Create(InputSystemCursorShape.UniversalNo);
        private readonly InputSystemCursor republishedBlocked = InputSystemCursor.Create(InputSystemCursorShape.UniversalNo);
        private InputPointerSource? source;
        private InputCursor? selected;
        private InputSystemCursor? publishedBlocked;

        internal void Block()
        {
            var current = Source(root);
            var cursor = current.Cursor;
            // Preserve cursor values published by WinUI while our override is
            // active. Our own object is not a new producer selection.
            if (source is null || !IsBlocked(cursor)) selected = cursor;
            source = current;
            publishedBlocked ??= blocked;
            // Native hit-test updates can replace the visible cursor while the
            // source retains our object. Reassigning that same object does not
            // always republish it. Change object identity only when Windows
            // actually displays another cursor over our owned input target.
            var visible = new CursorInformation { Size = (uint)Marshal.SizeOf<CursorInformation>() };
            if (ReadCursor(ref visible) && (visible.Flags & 1) != 0 &&
                visible.Cursor != LoadSystemCursor(IntPtr.Zero, new IntPtr(32648)) && IsBlocked(cursor))
                publishedBlocked = ReferenceEquals(publishedBlocked, blocked) ? republishedBlocked : blocked;
            current.Cursor = publishedBlocked;
        }

        private bool IsBlocked(InputCursor? cursor) => ReferenceEquals(cursor, blocked) || ReferenceEquals(cursor, republishedBlocked);

        internal void Restore()
        {
            if (source is not null && root.XamlRoot is not null)
            {
                var cursor = source.Cursor;
                if (!IsBlocked(cursor))
                {
                    selected = cursor;
                    // WinUI can publish its restored selection before Windows
                    // displays it. Change the publisher object before returning
                    // that exact producer selection, including resource cursors.
                    source.Cursor = ReferenceEquals(publishedBlocked, blocked) ? republishedBlocked : blocked;
                }
                source.Cursor = selected;
            }
            source = null; selected = null;
        }
    }

    [StructLayout(LayoutKind.Sequential)] private struct CursorPoint { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)] private struct CursorInformation
    {
        public uint Size, Flags;
        public IntPtr Cursor;
        public CursorPoint Position;
    }
    [DllImport("user32.dll", EntryPoint = "GetCursorInfo")]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ReadCursor(ref CursorInformation info);
    [DllImport("user32.dll", EntryPoint = "LoadCursorW")] private static extern IntPtr LoadSystemCursor(IntPtr instance, IntPtr name);
}
