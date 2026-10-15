using System.Globalization;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class DataTableToolbarsFixture
{
    internal async Task<bool> VerifyPressAsync(BleeckerColorScheme scheme, string folder)
    {
        using var layout = JsonDocument.Parse(typeof(DataTableToolbarsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.DataTableToolbarLayout.json")!);
        using var stream = typeof(DataTableToolbarsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.DataTableToolbarPress.json")!;
        using var buffer = new MemoryStream(); await stream.CopyToAsync(buffer); var referenceBytes = buffer.ToArray(); using var reference = JsonDocument.Parse(referenceBytes);
        var sample = layout.RootElement.GetProperty("cases").EnumerateArray().Single(item => item.GetProperty("id").GetString() == "global-filter" && item.GetProperty("scheme").GetString() == scheme.ToString());
        var root = (Grid)XamlRoot.Content; var reports = new List<object>(); var transfers = new List<object>(); var failures = new List<string>();
        using var input = new NativeInputSession(XamlRoot, folder, $"data-table-toolbar-press-{scheme}"); await input.AcquireAsync(Outside);
        foreach (var test in reference.RootElement.GetProperty("results").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == scheme.ToString() && item.GetProperty("reducedMotion").GetString() == "reduce"))
        {
            var id = test.GetProperty("id").GetString()!; using var scenario = new ToolbarScenario(this, sample, scheme); root.Children.Add(scenario.Host);
            try
            {
                await AwaitAsync(() => scenario.Bar.IsLoaded, "Press specimen did not load."); scenario.Bar.ApplyTemplate(); UpdateLayout(); await MainWindow.AwaitPresentationAsync(scenario.Bar); scenario.RegisterSlots();
                await input.ClickAsync(scenario.Search);
                await TransferToolbarTabAsync(input, scenario, transfers, id, 0, "filters");
                await TransferToolbarTabAsync(input, scenario, transfers, id, 0, "BUTTON");
                var reset = scenario.Reset; var step = 0;
                async Task Capture(string phase)
                {
                    var expected = test.GetProperty("states")[step].GetProperty("state"); Require(expected.GetProperty("phase").GetString() == phase, "Press phase ordering differs."); step++;
                    await MainWindow.AwaitPresentationAsync(scenario.Bar);
                    var errors = new List<string>(); void Check(string name, Action verify) { try { verify(); } catch (InvalidOperationException error) { errors.Add(name + ": " + error.Message); } }
                    var transform = (CompositeTransform)reset.RenderTransform;
                    var expectedScale = double.Parse(expected.GetProperty("scale").GetString()!, CultureInfo.InvariantCulture);
                    var expectedShift = expected.GetProperty("translate").GetString() == "0px" ? 0d : 1d;
                    var bounds = Bounds(reset, scenario.Bar); var surface = Descendants<ContentPresenter>(reset).Single(item => item.Name == "ContentPresenter"); var brush = (SolidColorBrush)surface.Background;
                    var pointerHit = PressPointerOwnsReset(reset, input); var hover = expected.GetProperty("pointerOwnsReset").GetBoolean();
                    Check("consumer", () => scenario.VerifyConsumer(expected));
                    Check("callbacks", () => Require(scenario.A.Resets == expected.GetProperty("clicks").GetArrayLength(), "Reset callback timing/count differs."));
                    // WinUI stores these transform values as floats. Use the
                    // existing numeric transform bound, never a pixel mask.
                    Check("transform", () => Require(Math.Abs(transform.ScaleX - expectedScale) < .000001 && Math.Abs(transform.ScaleY - expectedScale) < .000001 && Math.Abs(transform.TranslateY - expectedShift) < .000001, "Actual held transform differs from source."));
                    Check("geometry", () => Compare(bounds, expected.GetProperty("box")));
                    Check("pointer", () => Require(pointerHit == hover && input.OwnsCurrentPoint, "Actual owned pointer hit differs from source."));
                    var focusRole = expected.GetProperty("focusRole").GetString()!; if (focusRole == "reset") focusRole = "BUTTON"; if (focusRole == "search") focusRole = "INPUT";
                    Check("focus", () => Require(scenario.DescribeFocus() == focusRole, "Native focus differs from source."));
                    Check("paint", () => Require(hover ? brush.Color.A == (scheme == BleeckerColorScheme.Light ? 31 : 255) && brush.Color.R == (scheme == BleeckerColorScheme.Light ? 230 : 255) && brush.Color.G == (scheme == BleeckerColorScheme.Light ? 213 : 255) && brush.Color.B == (scheme == BleeckerColorScheme.Light ? 184 : 255) && Math.Abs(brush.Opacity - (scheme == BleeckerColorScheme.Light ? 1 : .06)) < .000001 : brush.Color.A * brush.Opacity == 0, "Held background does not follow source hover paint."));
                    var file = $"data-table-toolbar-press-{id}-{phase}-{scheme}.png"; await MainWindow.CaptureElementAsync(root, Path.Combine(folder, file));
                    var barBounds = Bounds(scenario.Bar, root);
                    reports.Add(new { id, phase, step, file, errors, surface = new { x = barBounds.X, y = barBounds.Y, width = barBounds.Width, height = barBounds.Height }, bounds = new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height }, scaleX = transform.ScaleX, scaleY = transform.ScaleY, translateY = transform.TranslateY, reset.IsPressed, reset.IsPointerOver, pointerHit, focusRole = scenario.DescribeFocus(), callbacks = scenario.A.Resets, paint = DescribeBrush(brush) });
                    failures.AddRange(errors.Select(error => id + "/" + phase + ": " + error));
                }
                if (id == "space-hover" || id.StartsWith("drag-", StringComparison.Ordinal)) await input.MoveAsync(reset);
                await Capture("normal");
                if (id.StartsWith("drag-", StringComparison.Ordinal))
                {
                    await input.PressPointerAsync();
                    try
                    {
                        await Capture("held"); await input.MoveAsync(scenario.Search); await Capture("outside");
                        if (id == "drag-return") { await input.MoveAsync(reset); await Capture("returned"); }
                    }
                    finally { await ReleaseToolbarPointerAsync(input, $"press-{id}-{scheme}", folder); }
                    await Capture("released");
                }
                else
                {
                    Control releaseTarget = reset;
                    await input.HeldKeyAsync(id == "enter-outside" ? (ushort)0x0D : (ushort)0x20, reset, () => releaseTarget, async () =>
                    {
                        await Capture("held");
                        if (id == "space-tab-cancel")
                        {
                            var destination = scenario.FocusTarget("filters"); await ReversePressTabAsync(input, reset, destination, transfers, id); releaseTarget = destination; await Capture("blurred");
                        }
                    });
                    await Capture("released");
                }
                if (id == "enter-outside")
                {
                    await input.HeldKeyAsync(0x20, reset, () => reset, () => Capture("space-held")); await Capture("space-released");
                    await input.MoveAsync(reset); await Capture("mouse-hover"); await input.PressPointerAsync();
                    try { await Capture("mouse-held"); }
                    finally { await ReleaseToolbarPointerAsync(input, $"press-{id}-restored-{scheme}", folder); }
                    await Capture("mouse-released");
                }
                Require(step == test.GetProperty("states").GetArrayLength(), "Press specimen omitted a source state.");
            }
            finally { root.Children.Remove(scenario.Host); }
        }
        var passed = failures.Count == 0;
        await File.WriteAllTextAsync(Path.Combine(folder, $"data-table-toolbar-press-{scheme}.json"), JsonSerializer.Serialize(new { format = "orchard-data-table-toolbar-native-press-v1", scheme = scheme.ToString(), passed, nativeComponentAccepted = false, referenceSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(referenceBytes)).ToLowerInvariant(), failures, reports, transfers }, new JsonSerializerOptions { WriteIndented = true }));
        return passed;
    }

    private static bool PressPointerOwnsReset(Button reset, NativeInputSession input)
    {
        var screen = input.ReadPointerPosition(); var client = NativeScreenCapture.ClientBounds(input.Window); var root = reset.XamlRoot.Content;
        var logical = new Point((screen.X - client.X) / reset.XamlRoot.RasterizationScale, (screen.Y - client.Y) / reset.XamlRoot.RasterizationScale);
        var hit = VisualTreeHelper.FindElementsInHostCoordinates(logical, root).FirstOrDefault();
        for (var node = hit as DependencyObject; node is not null; node = VisualTreeHelper.GetParent(node)) if (ReferenceEquals(node, reset)) return true;
        return false;
    }
    private async Task ReversePressTabAsync(NativeInputSession input, Control from, Control to, List<object> reports, string id)
    {
        var downs = 0; var ups = 0; var shiftDowns = 0; var shiftUps = 0; var downOwned = true; var upOwned = true;
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var down = new KeyEventHandler((_, args) => { if (args.Key is not (Windows.System.VirtualKey.Tab or Windows.System.VirtualKey.Shift)) return; if (args.Key == Windows.System.VirtualKey.Tab) downs++; else shiftDowns++; downOwned &= ReferenceEquals(args.OriginalSource, from); });
        var up = new KeyEventHandler((_, args) => { if (args.Key is not (Windows.System.VirtualKey.Tab or Windows.System.VirtualKey.Shift)) return; if (args.Key == Windows.System.VirtualKey.Tab) ups++; else { shiftUps++; released.TrySetResult(); } upOwned &= ReferenceEquals(args.OriginalSource, to); });
        XamlRoot.Content.AddHandler(UIElement.KeyDownEvent, down, true); XamlRoot.Content.AddHandler(UIElement.KeyUpEvent, up, true);
        try { input.ShiftKey(0x09); await released.Task.WaitAsync(TimeSpan.FromSeconds(2)); Require(downs == 1 && ups == 1 && shiftDowns == 1 && shiftUps == 1 && downOwned && upOwned && ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), to), "Held Space cancellation lost its exact reverse focus transfer."); }
        finally { XamlRoot.Content.RemoveHandler(UIElement.KeyDownEvent, down); XamlRoot.Content.RemoveHandler(UIElement.KeyUpEvent, up); reports.Add(new { id, reverse = true, downs, ups, shiftDowns, shiftUps, downOwned, upOwned }); }
    }
}
