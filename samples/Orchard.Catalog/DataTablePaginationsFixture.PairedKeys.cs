using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Orchard.Catalog;

public sealed partial class DataTablePaginationsFixture
{
    private async Task<object> PaginationPairedKeyAsync(NativeInputSession input, JsonElement point, Func<object?, string> describe, List<string> errors)
    {
        var from = FocusManager.GetFocusedElement(XamlRoot) as Control ?? throw new InvalidOperationException("Paired keyboard phase has no focused native control.");
        var key = point.GetProperty("key").GetString()!;
        var code = key switch { "Tab" or "Shift+Tab" => 0x09, "ArrowDown" => 0x28, "ArrowUp" => 0x26, "Home" => 0x24, "End" => 0x23, "Enter" => 0x0D, "Space" => 0x20, "Escape" => 0x1B, _ => throw new InvalidOperationException("Unknown original paired key: " + key) };
        var sourceKey = key switch { "Shift+Tab" => "Tab", "Space" => " ", _ => key };
        var source = point.GetProperty("state").GetProperty("inputEvents").EnumerateArray().Where(item => item.GetProperty("key").ValueKind == JsonValueKind.String && item.GetProperty("key").GetString() == sourceKey && item.GetProperty("type").GetString() is "keydown" or "keyup").TakeLast(2).ToArray();
        Require(source.Length == 2 && source[0].GetProperty("type").GetString() == "keydown" && source[1].GetProperty("type").GetString() == "keyup" && source.All(item => item.GetProperty("trusted").GetBoolean()), "Frozen source does not contain the original paired key receipts.");
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var elapsed = System.Diagnostics.Stopwatch.StartNew(); var order = new List<string>(); double? downMilliseconds = null, upMilliseconds = null;
        var downs = 0; var ups = 0; var shiftDowns = 0; var shiftUps = 0; var downOwned = true; var releaseFocusedOwner = true; var shiftOwned = true;
        var downTarget = ""; var upTarget = ""; var upFocus = "";
        var down = new KeyEventHandler((_, args) =>
        {
            if ((ushort)args.Key == code) { order.Add("keydown"); downMilliseconds = elapsed.Elapsed.TotalMilliseconds; downs++; downOwned &= ReferenceEquals(args.OriginalSource, from); downTarget = describe(args.OriginalSource); }
            else if (args.Key == Windows.System.VirtualKey.Shift) { order.Add("shift-down"); shiftDowns++; shiftOwned &= ReferenceEquals(args.OriginalSource, from); }
        });
        var up = new KeyEventHandler((_, args) =>
        {
            var focused = FocusManager.GetFocusedElement(XamlRoot);
            if ((ushort)args.Key == code) { order.Add("keyup"); upMilliseconds = elapsed.Elapsed.TotalMilliseconds; ups++; releaseFocusedOwner &= ReferenceEquals(args.OriginalSource, focused); upTarget = describe(args.OriginalSource); upFocus = describe(focused); released.TrySetResult(); }
            else if (args.Key == Windows.System.VirtualKey.Shift) { order.Add("shift-up"); shiftUps++; shiftOwned &= ReferenceEquals(args.OriginalSource, focused); }
        });
        // Observe one actual route. Sending the native batch below never waits
        // for focus, layout, presentation or a dispatcher callback between keys.
        XamlRoot.Content.AddHandler(UIElement.KeyDownEvent, down, true); XamlRoot.Content.AddHandler(UIElement.KeyUpEvent, up, true);
        try
        {
            if (key == "Shift+Tab") input.ShiftKey((ushort)code); else input.Key((ushort)code);
            try { await released.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (TimeoutException) { errors.Add("Paired input lost its actual native key release within two seconds."); }
            if (key == "Shift+Tab") await ReadyAsync(() => shiftUps > 0);
            if (downs != 1 || ups != 1 || !downOwned || !releaseFocusedOwner) errors.Add("Paired input did not retain exactly one initial down owner and one actual focused release owner.");
            if (!order.SequenceEqual(key == "Shift+Tab" ? new[] { "shift-down", "keydown", "keyup", "shift-up" } : new[] { "keydown", "keyup" })) errors.Add("Paired native input order differs.");
            if (key == "Shift+Tab" && (shiftDowns != 1 || shiftUps != 1 || !shiftOwned)) errors.Add("Paired Shift modifier lost its actual focus owner.");
        }
        finally { XamlRoot.Content.RemoveHandler(UIElement.KeyDownEvent, down); XamlRoot.Content.RemoveHandler(UIElement.KeyUpEvent, up); }
        return new { key, paired = true, nativeInputBatchSize = key == "Shift+Tab" ? 4 : 2, downs, ups, downOwned, releaseFocusedOwner, shiftDowns, shiftUps, shiftOwned, from = describe(from), downTarget, upTarget, upFocus, order, downMilliseconds, upMilliseconds, source };
    }
}
