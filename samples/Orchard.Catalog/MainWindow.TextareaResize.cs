using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunTextareaResizeAsync(string folder)
    {
        using var stream = CatalogReferenceResources.Open(Assembly.GetExecutingAssembly(), "Orchard.Catalog.Reference.TextareaResize.json") ?? throw new InvalidOperationException("Missing Textarea resize reference.");
        using var reference = await JsonDocument.ParseAsync(stream);
        var original = Content;
        var canvas = new Canvas { Height = 600 };
        var page = new Page { Content = canvas };
        var frames = new List<object>(); var phase = "mount"; var passed = false;
        NativeInputSession? input = null;
        try
        {
            Content = page; await AwaitPresentationAsync(page);
            input = new NativeInputSession(page.XamlRoot, folder, "textarea-resize");
            await input.AcquireAsync(canvas);
            var width = (int)page.XamlRoot.Size.Width;
            Require(width is 320 or 640, "Textarea resize requires a 320 or 640 client.");
            canvas.Width = width;
            foreach (var source in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("width").GetInt32() == width))
            {
                var id = source.GetProperty("id").GetString()!;
                var scheme = Enum.Parse<ElementTheme>(source.GetProperty("scheme").GetString()!);
                phase = scheme + "-" + id; await SaveAsync();
                canvas.Children.Clear(); page.RequestedTheme = scheme;
                canvas.Background = new SolidColorBrush(scheme == ElementTheme.Dark ? Windows.UI.Color.FromArgb(255, 13, 24, 33) : Microsoft.UI.Colors.White);
                var editor = new BleeckerTextarea { Name = "ResizeEditor", Width = width - 40,
                    Text = source.GetProperty("initial").GetProperty("value").GetString()!,
                    InputSize = source.GetProperty("size").GetString() switch { "sm" => BleeckerInputSize.Small, "lg" => BleeckerInputSize.Large, _ => BleeckerInputSize.Medium },
                    IsReadOnly = source.GetProperty("mode").GetString() == "readonly",
                    IsEnabled = source.GetProperty("mode").GetString() != "disabled",
                    FlowDirection = source.GetProperty("dir").GetString() == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                // Translate the browser geometry down so the overshoot stays
                // inside our actual owned HWND. Raster capture restores y=20.
                Canvas.SetLeft(editor, 20); Canvas.SetTop(editor, 280); canvas.Children.Add(editor);
                await AwaitPresentationAsync(editor);
                var expectedText = editor.Text;
                Require(editor.ActualHeight == Number("initial", "height"), phase + ": initial height differs.");
                var grip = ResizeDescendants<Control>(editor).Single(c => c.Name == "TextareaResizeGrip");
                var gripBounds = grip.TransformToVisual(canvas).TransformBounds(new Rect(0, 0, grip.ActualWidth, grip.ActualHeight));
                var rtl = editor.FlowDirection == FlowDirection.RightToLeft;
                Require(Math.Abs(gripBounds.X - (rtl ? 20 : 20 + editor.ActualWidth - 12)) < .001, phase + ": grip is on the wrong physical edge: " + gripBounds);
                Point GripPoint() => new(20 + (rtl ? 3 : editor.ActualWidth - 3), 280 + editor.ActualHeight - 3);
                var receipts = new List<object>(); var captured = false;
                var handler = new PointerEventHandler((_, e) => receipts.Add(new { e.IsGenerated, id = e.Pointer.PointerId,
                    position = e.GetCurrentPoint(canvas).Position, pressed = e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed }));
                page.AddHandler(UIElement.PointerPressedEvent, handler, true);
                page.AddHandler(UIElement.PointerMovedEvent, handler, true);
                page.AddHandler(UIElement.PointerReleasedEvent, handler, true);
                object enlarged; object minimum; object reversed;
                try
                {
                    var origin = GripPoint(); await input.MoveAsync(canvas, true, origin);
                    await CursorAsync();
                    await input.PressPointerAsync(); await AwaitPresentationAsync(editor);
                    captured = page.PointerCaptures?.Count == 1;
                    Require(captured, phase + ": enabled root did not capture the real pointer.");
                    // Keep horizontal movement inside this HWND; either sign
                    // must preserve width for a vertical-only resizer.
                    try { await MoveByAsync(origin, rtl ? 40 : -40, 48, 8); await CursorAsync(); }
                    finally { input.PointerButton(false); }
                    await AwaitPresentationAsync(editor);
                    enlarged = Snapshot(); Require(editor.ActualHeight == Number("enlargedHeld", "height"), phase + ": enlarged height differs.");
                    Require(editor.FocusState == FocusState.Unfocused, phase + ": resizing focused the editor.");
                    origin = GripPoint(); await input.MoveAsync(canvas, true, origin); await input.PressPointerAsync();
                    try
                    {
                        await MoveByAsync(origin, 0, -240, 8); minimum = Snapshot();
                        await CursorAsync();
                        Require(editor.ActualHeight == Number("minimumHeld", "height"), phase + ": minimum differs.");
                        await MoveByAsync(new Point(origin.X, origin.Y - 240), 0, 20, 4); reversed = Snapshot();
                        await CursorAsync();
                        Require(editor.ActualHeight == Number("reverseAboveMinimum", "height"), phase + ": minimum reversal lost the original anchor.");
                    }
                    finally { input.PointerButton(false); }
                    await AwaitPresentationAsync(editor);
                    Require(page.PointerCaptures?.Count is null or 0, phase + ": pointer capture leaked after release.");
                    Require(editor.Text == expectedText && editor.ActualWidth == width - 40 &&
                        editor.IsEnabled == (source.GetProperty("mode").GetString() != "disabled") && editor.FocusState == FocusState.Unfocused,
                        phase + ": resizing changed text, width, disabled semantics or focus.");
                    await File.WriteAllTextAsync(Path.Combine(folder, phase + "-receipts.json"), JsonSerializer.Serialize(receipts));
                    Require(receipts.Count(r => !JsonSerializer.SerializeToElement(r).GetProperty("IsGenerated").GetBoolean()) > 10, phase + ": missing physical pointer receipts.");
                }
                finally
                {
                    page.RemoveHandler(UIElement.PointerPressedEvent, handler);
                    page.RemoveHandler(UIElement.PointerMovedEvent, handler);
                    page.RemoveHandler(UIElement.PointerReleasedEvent, handler);
                }
                var lifecycleCancellation = false;
                if (id == "lg-disabled-rtl")
                {
                    foreach (var action in new[] { "capture-loss", "template", "unload" })
                    {
                        var origin = GripPoint(); var before = editor.ActualHeight;
                        await input.MoveAsync(canvas, true, origin); await input.PressPointerAsync(); await AwaitPresentationAsync(editor);
                        Require(page.PointerCaptures?.Count == 1, phase + ": cancellation precondition has no capture.");
                        try
                        {
                            if (action == "capture-loss") page.ReleasePointerCaptures();
                            else if (action == "template") { var template = editor.Template; editor.Template = null; editor.ApplyTemplate(); editor.Template = template; editor.ApplyTemplate(); }
                            else canvas.Children.Remove(editor);
                            await AwaitPresentationAsync(page);
                            Require(page.PointerCaptures?.Count is null or 0, phase + ": " + action + " leaked pointer capture.");
                            if (action == "unload") { canvas.Children.Add(editor); await AwaitPresentationAsync(editor); }
                            await input.MoveAsync(canvas, true, new Point(origin.X, origin.Y + 20)); await AwaitPresentationAsync(editor);
                            Require(editor.ActualHeight == before && editor.Text == expectedText && !editor.IsEnabled, phase + ": stale drag survived " + action);
                        }
                        finally { input.PointerButton(false); }
                    }
                    lifecycleCancellation = true;
                }
                Canvas.SetTop(editor, 20); await input.MoveAsync(canvas, true, new Point(2, 2)); await AwaitPresentationAsync(editor);
                var file = $"textarea-resize-{id}-{width}-{scheme}.png";
                await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, file));
                if (id == "lg-disabled-rtl") await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"catalog-{scheme.ToString().ToLowerInvariant()}.png"));
                frames.Add(new { id, width, scheme = scheme.ToString(), enlarged, minimum, reversed, captured, gripBounds, lifecycleCancellation, file, receipts, editor.Text, editor.IsEnabled, editor.IsReadOnly, focus = editor.FocusState.ToString() });
                await SaveAsync();
                double Number(string state, string property) => source.GetProperty(state).GetProperty(property).GetDouble();
                object Snapshot() => new { width = editor.ActualWidth, height = editor.ActualHeight, text = editor.Text, enabled = editor.IsEnabled, focus = editor.FocusState.ToString() };
                async Task MoveByAsync(Point from, double dx, double dy, int steps)
                {
                    for (var step = 1; step <= steps; step++) await input.MoveAsync(canvas, true, new Point(from.X + dx * step / steps, from.Y + dy * step / steps));
                    await AwaitPresentationAsync(editor);
                }
                async Task CursorAsync()
                {
                    var watch = Stopwatch.StartNew(); var stable = 0;
                    while (stable < 2 && watch.Elapsed < TimeSpan.FromSeconds(2))
                    {
                        await AwaitPresentationAsync(editor);
                        stable = NativeInputSession.CurrentVisibleCursor() == ResizeStockCursor(IntPtr.Zero, new IntPtr(32645)) ? stable + 1 : 0;
                    }
                    if (stable != 2)
                    {
                        var point = GripPoint();
                        await File.WriteAllTextAsync(Path.Combine(folder, phase + "-cursor-failure.json"), JsonSerializer.Serialize(new { point,
                            editorPoint = canvas.TransformToVisual(editor).TransformPoint(point), gripPoint = canvas.TransformToVisual(grip).TransformPoint(point),
                            gripInCanvas = grip.TransformToVisual(canvas).TransformBounds(new Rect(0,0,grip.ActualWidth,grip.ActualHeight)),
                            gripBounds, editorBounds = editor.TransformToVisual(canvas).TransformBounds(new Rect(0,0,editor.ActualWidth,editor.ActualHeight)),
                            cursor = NativeInputSession.DescribeCursor() }, new JsonSerializerOptions { WriteIndented = true }));
                        await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, phase + "-cursor-failure.png"));
                    }
                    Require(stable == 2, phase + ": native cursor is not SizeNorthSouth: " + JsonSerializer.Serialize(NativeInputSession.DescribeCursor()));
                }
            }
            passed = true;
        }
        finally { input?.Dispose(); Content = original; await AwaitPresentationAsync(CatalogRoot); await SaveAsync(); }
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed, component = "textarea-resize", environment = RuntimeEnvironment() }));
        Task SaveAsync() => File.WriteAllTextAsync(Path.Combine(folder, "textarea-resize.json"), JsonSerializer.Serialize(new { passed, phase, frames }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static IEnumerable<T> ResizeDescendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T value) yield return value;
            foreach (var nested in ResizeDescendants<T>(child)) yield return nested;
        }
    }
    [DllImport("user32.dll", EntryPoint = "LoadCursorW")] private static extern IntPtr ResizeStockCursor(IntPtr instance, IntPtr name);
}
