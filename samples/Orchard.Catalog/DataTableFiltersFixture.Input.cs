using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Orchard.Catalog;

public sealed partial class DataTableFiltersFixture
{
    private async Task<object> PairAsync(NativeInputSession input, ushort key, bool shift)
    {
        var downSource = FocusManager.GetFocusedElement(XamlRoot); var downs = 0; var ups = 0; var shiftDowns = 0; var shiftUps = 0; var downOwned = true; var upOwned = true; var shiftOwned = true; var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var timer = System.Diagnostics.Stopwatch.StartNew(); var order = new List<string>(); double? downMilliseconds = null, upMilliseconds = null;
        var down = new KeyEventHandler((_, args) =>
        {
            if ((ushort)args.Key == key) { order.Add("keydown"); downMilliseconds = timer.Elapsed.TotalMilliseconds; downs++; downOwned &= ReferenceEquals(args.OriginalSource, downSource); }
            else if (args.Key == Windows.System.VirtualKey.Shift) { order.Add("shift-down"); shiftDowns++; shiftOwned &= ReferenceEquals(args.OriginalSource, downSource); }
        });
        var up = new KeyEventHandler((_, args) =>
        {
            if ((ushort)args.Key == key) { order.Add("keyup"); upMilliseconds = timer.Elapsed.TotalMilliseconds; ups++; upOwned &= ReferenceEquals(args.OriginalSource, FocusManager.GetFocusedElement(XamlRoot)); released.TrySetResult(); }
            else if (args.Key == Windows.System.VirtualKey.Shift) { order.Add("shift-up"); shiftUps++; shiftOwned &= ReferenceEquals(args.OriginalSource, FocusManager.GetFocusedElement(XamlRoot)); }
        });
        XamlRoot.Content.AddHandler(UIElement.KeyDownEvent, down, true); XamlRoot.Content.AddHandler(UIElement.KeyUpEvent, up, true);
        try
        {
            if (shift) input.ShiftKey(key); else input.Key(key); await released.Task.WaitAsync(TimeSpan.FromSeconds(2));
            if (shift) await ReadyAsync(() => shiftUps > 0);
            Require(downs == 1 && ups == 1 && downOwned && upOwned, "Native column key lost an exact actual focus owner.");
            Require(order.SequenceEqual(shift ? new[] { "shift-down", "keydown", "keyup", "shift-up" } : new[] { "keydown", "keyup" }), "Native column key order differs.");
            Require(shiftDowns == (shift ? 1 : 0) && shiftUps == (shift ? 1 : 0) && shiftOwned, "Native column Shift modifier lost its exact actual focus owner.");
        }
        finally { XamlRoot.Content.RemoveHandler(UIElement.KeyDownEvent, down); XamlRoot.Content.RemoveHandler(UIElement.KeyUpEvent, up); }
        return new { key, shift, downs, ups, downOwned, upOwned, shiftDowns, shiftUps, shiftOwned, order, downMilliseconds, upMilliseconds };
    }
}
