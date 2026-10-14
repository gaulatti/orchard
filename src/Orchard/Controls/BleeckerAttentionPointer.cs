using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

// Visual hover is independent of native button enablement. The frozen source
// applies its CSS :hover lift to disabled buttons too, without enabling actions.
internal static class BleeckerAttentionPointer
{
    private static readonly ConditionalWeakTable<UIElement, RootObserver> roots = new();
    private static readonly ConditionalWeakTable<BleeckerAttentionSurface,RootObserver> registrations = new();
    internal static void Add(BleeckerAttentionSurface owner)
    {
        var root = (UIElement)owner.XamlRoot.Content;
        var observer=roots.GetValue(root, value => new RootObserver(value));
        if(registrations.TryGetValue(owner,out var previous))
        {
            if(ReferenceEquals(previous,observer))return;
            previous.Remove(owner);registrations.Remove(owner);
        }
        registrations.Add(owner,observer);observer.Add(owner);
    }
    internal static void Remove(BleeckerAttentionSurface owner)
    {
        if (registrations.TryGetValue(owner,out var observer)) { observer.Remove(owner); registrations.Remove(owner); }
        owner.SetPointerOver(false);
    }
    private sealed class RootObserver(UIElement root)
    {
        private readonly HashSet<BleeckerAttentionSurface> owners = [];
        private BleeckerAttentionSurface[] snapshot = [];
        internal void Add(BleeckerAttentionSurface owner)
        {
            if (owners.Count == 0) CompositionTarget.Rendering += OnFrame;
            owners.Add(owner);
            snapshot=owners.ToArray();
        }
        internal void Remove(BleeckerAttentionSurface owner)
        {
            owners.Remove(owner);
            snapshot=owners.ToArray();
            if (owners.Count == 0) CompositionTarget.Rendering -= OnFrame;
        }
        private void OnFrame(object? sender, object args)
        {
            var xamlRoot = root.XamlRoot;
            if (xamlRoot is null) return;
            var window = Microsoft.UI.Win32Interop.GetWindowFromWindowId(xamlRoot.ContentIslandEnvironment.AppWindowId);
            var visible = GetCursorPos(out var screen) && GetAncestor(WindowFromPoint(screen), 2) == window;
            var point = screen;
            visible &= ScreenToClient(window, ref point);
            var logical = new Point(point.X / xamlRoot.RasterizationScale, point.Y / xamlRoot.RasterizationScale);
            var hit = visible ? VisualTreeHelper.FindElementsInHostCoordinates(logical, root).FirstOrDefault() : null;
            foreach (var owner in snapshot)
            {
                // A disabled native button is absent from hit testing. Its
                // eligible ancestor can still own this source hover area.
                var contained = hit is not null && owner.IsLoaded && ReferenceEquals(owner.XamlRoot?.Content,root) && owner.Visibility == Visibility.Visible &&
                    (IsWithin(hit, owner) || IsWithin(owner, hit));
                if (contained && owner.PointerSurface is FrameworkElement surface)
                {
                    var local = root.TransformToVisual(surface).TransformPoint(logical);
                    var radius = owner.CornerRadius;
                    contained = BleeckerRoundedPointerHit.Contains(surface.ActualWidth, surface.ActualHeight,
                        radius.TopLeft, radius.TopRight, radius.BottomRight, radius.BottomLeft, local.X, local.Y);
                    for (DependencyObject? ancestor = owner; contained && ancestor is not null; ancestor = VisualTreeHelper.GetParent(ancestor))
                    {
                        if (ancestor is UIElement element && (element.Visibility != Visibility.Visible || !element.IsHitTestVisible)) contained = false;
                        if (ancestor is ScrollContentPresenter viewport)
                        {
                            var clipped = root.TransformToVisual(viewport).TransformPoint(logical);
                            contained &= clipped.X >= 0 && clipped.Y >= 0 && clipped.X < viewport.ActualWidth && clipped.Y < viewport.ActualHeight;
                        }
                        if (ancestor is UIElement { Clip:RectangleGeometry clip } clippedElement)
                        {
                            var clipped = root.TransformToVisual(clippedElement).TransformPoint(logical);
                            if (clip.Transform is not null && clip.Transform.Inverse is GeneralTransform inverse) clipped = inverse.TransformPoint(clipped);
                            contained &= clip.Rect.Contains(clipped);
                        }
                    }
                }
                owner.SetPointerOver(contained);
            }
        }
        private static bool IsWithin(DependencyObject child, DependencyObject owner)
        {
            for (var current = child; current is not null; current = VisualTreeHelper.GetParent(current))
                if (ReferenceEquals(current, owner)) return true;
            return false;
        }
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X; public int Y; }
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(NativePoint point);
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ScreenToClient(IntPtr window, ref NativePoint point);
}
