using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Orchard.Catalog;

internal sealed partial class NativeInputSession
{
    internal bool PointerIsAtCenter(FrameworkElement target)
    {
        var bounds = ScreenBounds(target); var point = ReadPointerPosition();
        return point.X == (int)(bounds.X + bounds.Width / 2) && point.Y == (int)(bounds.Y + bounds.Height / 2);
    }

    // A real click needs no movement when the pointer is already positioned.
    // Prove its two native button events at the exact target instead of
    // inventing a PointerMoved receipt or moving away and back.
    internal async Task ClickCurrentPositionAsync(FrameworkElement target)
    {
        await RequireForegroundAsync();
        var bounds = await AwaitPointerPresentationAsync(target, null, JsonSerializer.Serialize(DescribeViewports(target)));
        var expected = ReadPointerPosition();
        Require(expected.X == (int)(bounds.X + bounds.Width / 2) && expected.Y == (int)(bounds.Y + bounds.Height / 2) && NativeScreenCapture.OwnsScreenPoint(Window, (int)expected.X, (int)expected.Y), "Current-position click lost its exact owned target.");
        var watch = Stopwatch.StartNew(); var downs = 0; var ups = 0; var downExact = true; var upExact = true; var receipts = new List<object>();
        var pressed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool Exact(PointerRoutedEventArgs args, bool held)
        {
            var point = args.GetCurrentPoint(root.Content); var client = NativeScreenCapture.ClientBounds(Window);
            var x = client.X + point.Position.X * root.RasterizationScale; var y = client.Y + point.Position.Y * root.RasterizationScale;
            var exact = !args.IsGenerated && args.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse && point.Properties.IsLeftButtonPressed == held && Math.Abs(x - expected.X) < .001 && Math.Abs(y - expected.Y) < .001;
            receipts.Add(new { milliseconds = watch.Elapsed.TotalMilliseconds, args.IsGenerated, x, y, held, exact }); return exact;
        }
        var down = new PointerEventHandler((_, args) => { downs++; downExact &= Exact(args, true); pressed.TrySetResult(); });
        var up = new PointerEventHandler((_, args) => { ups++; upExact &= Exact(args, false); released.TrySetResult(); });
        target.AddHandler(UIElement.PointerPressedEvent, down, true); target.AddHandler(UIElement.PointerReleasedEvent, up, true);
        try
        {
            PointerButton(true);
            try { await pressed.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
            finally { PointerButton(false); }
            await released.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Require(downs == 1 && ups == 1 && downExact && upExact && OwnsCurrentPoint && ReadPointerPosition() == expected, "Current-position click lost exact native press/release delivery.");
        }
        finally
        {
            target.RemoveHandler(UIElement.PointerPressedEvent, down); target.RemoveHandler(UIElement.PointerReleasedEvent, up);
            pointerClicks.Add(new { target = target.Name, expected, currentPosition = true, downs, ups, downExact, upExact, delivered = downs == 1 && ups == 1 && downExact && upExact, receipts });
        }
    }
}
