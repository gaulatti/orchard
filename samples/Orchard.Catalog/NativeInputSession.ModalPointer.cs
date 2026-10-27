using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Content;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

internal sealed partial class NativeInputSession
{
    // The same native island receives outside input before routed XAML.
    // Observe it without changing Handled or manufacturing a routed event.
    internal async Task<object> DismissModalPointerSourceOutsideAsync(FrameworkElement anchor, UIElement modal, FrameworkElement outside)
    {
        FrameworkElement modalContent;
        if (modal is Popup popup)
        {
            Require(popup.IsOpen && popup.IsLightDismissEnabled && ReferenceEquals(popup.XamlRoot, root), "Outside input requires its original open modal popup.");
            var ownsAnchor = false;
            for (DependencyObject? node = popup.PlacementTarget; node is not null; node = VisualTreeHelper.GetParent(node)) if (ReferenceEquals(node, anchor)) { ownsAnchor = true; break; }
            modalContent = (FrameworkElement)popup.Child;
            Require(ownsAnchor, "Modal input lost its exact placement target.");
        }
        else
        {
            Require(modal is Border { Name: "PopupBorder", IsLoaded: true }, "Outside input requires its original hosted popup border.");
            modalContent = (FrameworkElement)modal;
            var ownsRoot = false;
            for (DependencyObject? node = modalContent; node is not null; node = VisualTreeHelper.GetParent(node)) if (ReferenceEquals(node, root.Content)) { ownsRoot = true; break; }
            Require(ownsRoot && ReferenceEquals(anchor.XamlRoot, root), "Hosted modal input lost its exact catalog root/anchor.");
        }
        Require(ReferenceEquals(modalContent.XamlRoot.ContentIslandEnvironment, root.ContentIslandEnvironment), "Modal input lost its exact environment.");
        var island = ContentIsland.FindAllForCurrentThread().SingleOrDefault(item => ReferenceEquals(item.Environment, root.ContentIslandEnvironment)) ?? throw new InvalidOperationException("The exact original input island is unavailable.");
        Require(island.Environment.AppWindowId.Value == root.ContentIslandEnvironment.AppWindowId.Value, "Modal island belongs to another window.");
        var source = InputPointerSource.GetForIsland(island) ?? throw new InvalidOperationException("The original modal input source is unavailable.");
        await RequireForegroundAsync();
        var bounds = await AwaitPointerPresentationAsync(outside, null, JsonSerializer.Serialize(DescribeViewports(outside)));
        var x = (int)(bounds.X + bounds.Width / 2); var y = (int)(bounds.Y + bounds.Height / 2);
        Require(!ScreenBounds(modalContent).Contains(new Point(x, y)) && NativeScreenCapture.OwnsScreenPoint(Window, x, y), "Modal input lost its exact owned outside point.");
        Require(ReadPointerPosition() != new Point(x, y), "This source action requires its original changed pointer location.");
        var watch = Stopwatch.StartNew(); var receipts = new List<object>(); var order = new List<string>(); var errors = new List<string>();
        var moves = 0; var downs = 0; var ups = 0; uint? pointerId = null; ulong lastTimestamp = 0; uint lastFrame = 0; var phase = "move";
        var moved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var pressed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Observe(string kind, InputPointerSource sender, Microsoft.UI.Input.PointerEventArgs args)
        {
            if (kind == "move" && phase != "move") return;
            var point = args.CurrentPoint; var client = NativeScreenCapture.ClientBounds(Window); var actualX = client.X + point.Position.X * root.RasterizationScale; var actualY = client.Y + point.Position.Y * root.RasterizationScale;
            var exact = ReferenceEquals(sender, source) && point.PointerDeviceType.ToString() == "Mouse" && Math.Abs(actualX - x) < .001 && Math.Abs(actualY - y) < .001 && NativeScreenCapture.OwnsScreenPoint(Window, x, y) && OwnsForeground;
            exact &= pointerId is null || pointerId == point.PointerId; pointerId ??= point.PointerId;
            exact &= point.FrameId > lastFrame && point.Timestamp >= lastTimestamp; lastFrame = point.FrameId; lastTimestamp = point.Timestamp;
            exact &= kind == "down" ? point.Properties.IsLeftButtonPressed && point.IsInContact : !point.Properties.IsLeftButtonPressed && !point.IsInContact;
            order.Add(kind); receipts.Add(new { kind, milliseconds = watch.Elapsed.TotalMilliseconds, exactSource = ReferenceEquals(sender, source), island = island.Id, point.FrameId, point.PointerId, point.Timestamp, point.Position, actualX, actualY, point.IsInContact, point.Properties.IsLeftButtonPressed, update = point.Properties.PointerUpdateKind.ToString(), args.Handled, exact });
            if (!exact) errors.Add("Modal native pointer event lost its original source, frame, pointer, window, point or button state.");
            if (kind == "move") { moves++; moved.TrySetResult(); } else if (kind == "down") { downs++; pressed.TrySetResult(); } else { ups++; released.TrySetResult(); }
        }
        var move = new TypedEventHandler<InputPointerSource, Microsoft.UI.Input.PointerEventArgs>((sender, args) => Observe("move", sender, args));
        var down = new TypedEventHandler<InputPointerSource, Microsoft.UI.Input.PointerEventArgs>((sender, args) => Observe("down", sender, args));
        var up = new TypedEventHandler<InputPointerSource, Microsoft.UI.Input.PointerEventArgs>((sender, args) => Observe("up", sender, args));
        source.PointerMoved += move; source.PointerPressed += down; source.PointerReleased += up;
        try
        {
            SendPointerMovement(x, y); await moved.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Require(moves == 1 && errors.Count == 0 && ReadPointerPosition() == new Point(x, y), "Modal movement did not reach its exact original input source.");
            phase = "down";
            try { PointerButton(true); await pressed.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
            finally { phase = "up"; PointerButton(false); }
            await released.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Require(moves == 1 && downs == 1 && ups == 1 && errors.Count == 0 && order.SequenceEqual(new[] { "move", "down", "up" }) && ReadPointerPosition() == new Point(x, y) && OwnsForeground && OwnsCurrentPoint, "Modal input lost exact native count/order/ownership/position.");
        }
        catch (Exception error) { errors.Add(error.ToString()); throw; }
        finally
        {
            source.PointerMoved -= move; source.PointerPressed -= down; source.PointerReleased -= up;
            await File.WriteAllTextAsync(Path.Combine(folder, $"{prefix}-native-modal-input-source.json"), JsonSerializer.Serialize(new { layer = "winui-input-pointer-source", routedXamlReceiptsClaimed = false, passed = errors.Count == 0 && moves == 1 && downs == 1 && ups == 1, island = island.Id, window = Window.ToInt64(), x, y, moves, downs, ups, order, receipts, handlersDetached = true, errors }, new JsonSerializerOptions { WriteIndented = true }));
        }
        return new { layer = "winui-input-pointer-source", routedXamlReceiptsClaimed = false, moves, downs, ups, order, receipts, sourceRouteParityVerified = false };
    }
}
