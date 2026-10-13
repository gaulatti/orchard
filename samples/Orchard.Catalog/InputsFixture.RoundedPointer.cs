using System.Text.Json;
using Microsoft.UI.Content;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class InputsFixture
{
    private async Task VerifyRoundedDisabledPointerAsync(BleeckerColorScheme scheme, string folder)
    {
        using var input = new NativeInputSession(XamlRoot, folder, $"input-rounded-{scheme}");
        var samples = new List<object>();
        var windowCaptures = new List<object>();
        var alignments = new List<object>();
        var passed = false;
        string? error = null;
        BleeckerInput? activeControl = null;
        object? failedCursor = null;
        var cursorTrace = new List<object>();
        var controls = new[] { SmallInput, MediumInput, LargeInput };
        var originals = controls.Select(control => (control.IsEnabled, control.IsError, Transform: control.ReadLocalValue(UIElement.RenderTransformProperty))).ToArray();
        try
        {
            await input.AcquireAsync((FrameworkElement)((StackPanel)Content).Children[0]);
            ReadOnlyInput.Focus(FocusState.Keyboard);
            foreach (var control in controls)
            {
                activeControl = control;
                control.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false, VerticalAlignmentRatio = .5 });
                await Task.Delay(100);
                var before = input.ScreenBounds(control);
                // The fixed browser samples use a one-pixel pointer square at
                // an integer origin. A .5px centered catalog column otherwise
                // makes the physical (0,0) sample fall outside the control.
                // Translate only this specimen; preserve its size and restore
                // its original transform after the boundary matrix.
                var shift = new TranslateTransform
                {
                    X = (Math.Ceiling(before.X) - before.X) / XamlRoot.RasterizationScale,
                    Y = (Math.Ceiling(before.Y) - before.Y) / XamlRoot.RasterizationScale
                };
                var alignedTransform = new TransformGroup();
                if (control.RenderTransform is Transform previous) alignedTransform.Children.Add(previous);
                alignedTransform.Children.Add(shift);
                control.RenderTransform = alignedTransform;
                UpdateLayout();
                var aligned = input.ScreenBounds(control);
                Require(Math.Abs(aligned.X - Math.Round(aligned.X)) < .0001 && Math.Abs(aligned.Y - Math.Round(aligned.Y)) < .0001 &&
                    Math.Abs(aligned.Width - before.Width) < .0001 && Math.Abs(aligned.Height - before.Height) < .0001,
                    "Pointer specimen did not retain its dimensions at a physical pixel origin.");
                alignments.Add(new { control.Name, before, aligned, shift.X, shift.Y });
                control.IsEnabled = false;
                UpdateLayout();
                Require(control.CornerRadius == new CornerRadius(7), "Input pointer fixture lost the pinned rounded geometry.");
                foreach (var invalid in new[] { false, true })
                {
                    control.IsError = invalid;
                    foreach (var corner in new[] { "top-left", "top-right", "bottom-left", "bottom-right" })
                    foreach (var local in new[] { (X: 0d, Y: 0d, Hover: false), (X: 2d, Y: 0d, Hover: false),
                        (X: 3d, Y: 0d, Hover: true), (X: 0d, Y: 3d, Hover: true) })
                    {
                        var point = new Point(corner.EndsWith("right") ? control.ActualWidth - 1 - local.X : local.X,
                            corner.StartsWith("bottom") ? control.ActualHeight - 1 - local.Y : local.Y);
                        await input.MoveAsync(control, localPoint: point);
                        var expected = SystemCursor(IntPtr.Zero, new IntPtr(local.Hover ? 32648 : 32512));
                        var border = local.Hover ? scheme == BleeckerColorScheme.Dark ? "#40FFFFFF" : "#B3E6D5B8"
                            : invalid ? scheme == BleeckerColorScheme.Dark ? "#FFD47B75" : "#FFA65D57"
                            : scheme == BleeckerColorScheme.Dark ? "#26FFFFFF" : "#66E6D5B8";
                        var editor = Find<Grid>(control, "Editor");
                        // UIElement opacity is stored as a native float. Await
                        // its exact 60% destination together with the border.
                        var opacity = (double)(float).6;
                        cursorTrace.Clear();
                        var dispatch = System.Diagnostics.Stopwatch.StartNew();
                        await AwaitOwnedAsync(() =>
                        {
                            var visible = NativeInputSession.CurrentVisibleCursor();
                            if (visible != expected) cursorTrace.Add(new { milliseconds = dispatch.Elapsed.TotalMilliseconds,
                                cursor = NativeInputSession.DescribeCursor(), publication=NativeCursorDiagnostics.Describe(control) });
                            return visible == expected && Hex(Find<Border>(control, "BorderElement").BorderBrush) == border &&
                                editor.Opacity == opacity && control.FocusState == FocusState.Unfocused;
                        },
                            $"{control.InputSize}, invalid={invalid}, {corner} {local.X},{local.Y}: rounded hover/cursor mismatch.",
                            () => $"cursor={NativeInputSession.CurrentVisibleCursor()}, expected={expected}; border={Hex(Find<Border>(control, "BorderElement").BorderBrush)}, expected={border}; opacity={editor.Opacity:R}, expected={opacity:R}; focus={control.FocusState}; publisher={JsonSerializer.Serialize(DescribeRoundedCursor(control))}");
                        samples.Add(new { size = control.InputSize.ToString(), invalid, corner, point, local.Hover,
                            cursor = NativeInputSession.CurrentVisibleCursor().ToInt64(), border, editor.Opacity,
                            focus = control.FocusState.ToString(), width = control.ActualWidth, height = control.ActualHeight,
                            publisher = DescribeRoundedCursor(control) });
                        if (corner == "top-left" && local.Y == 0 && local.X is 0 or 3)
                        {
                            await MainWindow.CaptureElementAsync(control, Path.Combine(folder,
                                $"input-rounded-{scheme}-{control.InputSize}-{(invalid ? "invalid" : "valid")}-{(local.Hover ? "hover" : "rest")}.png"));
                            var capture = $"input-rounded-window-{scheme}-{control.InputSize}-{(invalid ? "invalid" : "valid")}-{(local.Hover ? "hover" : "rest")}.png";
                            var client = await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, capture));
                            windowCaptures.Add(new { capture, client, target = input.ScreenBounds(control), point, local.Hover });
                        }
                    }
                }
                control.IsEnabled = originals[Array.IndexOf(controls, control)].IsEnabled;
                control.IsError = originals[Array.IndexOf(controls, control)].IsError;
            }
            await input.MoveAsync((FrameworkElement)((StackPanel)Content).Children[0]);
            await AwaitOwnedAsync(() => NativeInputSession.CurrentVisibleCursor() == SystemCursor(IntPtr.Zero, new IntPtr(32512)),
                "Leaving all rounded disabled inputs retained the override cursor.");
            passed = true;
        }
        catch (Exception exception)
        {
            error = exception.ToString();
            if (activeControl is not null) failedCursor = DescribeRoundedCursor(activeControl);
            throw;
        }
        finally
        {
            for (var index = 0; index < controls.Length; index++)
            {
                controls[index].IsEnabled = originals[index].IsEnabled;
                controls[index].IsError = originals[index].IsError;
                if (originals[index].Transform == DependencyProperty.UnsetValue) controls[index].ClearValue(UIElement.RenderTransformProperty);
                else controls[index].SetValue(UIElement.RenderTransformProperty, originals[index].Transform);
            }
            UpdateLayout();
            await File.WriteAllTextAsync(Path.Combine(folder, $"input-rounded-pointer-{scheme}.json"),
                JsonSerializer.Serialize(new { passed, scheme, error, failedCursor, cursorTrace, scale = XamlRoot.RasterizationScale, alignments, samples, windowCaptures },
                    new JsonSerializerOptions { WriteIndented = true }));
        }

        async Task AwaitOwnedAsync(Func<bool> condition, string message, Func<string>? details = null)
        {
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                // The runner's setup console can take foreground after a move
                // has already completed. Reuse the session's single bounded
                // recovery, then keep the original displayed-cursor gate.
                await input.RequireForegroundAsync();
                if (condition() && input.OwnsForeground) return;
                if (elapsed.Elapsed >= TimeSpan.FromSeconds(1)) break;
                UpdateLayout(); await Task.Delay(16);
            }
            await input.RequireForegroundAsync();
            Require(condition() && input.OwnsForeground, $"{message} {details?.Invoke()}");
        }
    }

    private object DescribeRoundedCursor(BleeckerInput control)
    {
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
        var tracker = typeof(BleeckerInput).GetField("disabledPointer", flags)?.GetValue(control);
        var island = ContentIsland.FindAllForCurrentThread().Single(item => item.Environment.Equals(XamlRoot.ContentIslandEnvironment));
        var cursor = InputPointerSource.GetForIsland(island).Cursor;
        var positioned = ReadPointer(out var screen);
        var window = Microsoft.UI.Win32Interop.GetWindowFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
        var client = NativeScreenCapture.ClientBounds(window);
        var position = new Point((screen.X - client.X) / XamlRoot.RasterizationScale, (screen.Y - client.Y) / XamlRoot.RasterizationScale);
        // Publisher and tracker state explain a failure; the acceptance gate
        // still requires the actual global displayed cursor and rendered state.
        return new { control.Name, control.IsEnabled, control.IsLoaded, positioned, screen.X, screen.Y,
            nativeCursor = NativeInputSession.DescribeCursor(),
            publication=NativeCursorDiagnostics.Describe(control),
            nativeWindow = NativeScreenCapture.DescribePoint(window, screen.X, screen.Y),
            rootPosition = position, selectedCursor = (cursor as InputSystemCursor)?.CursorShape.ToString() ?? cursor?.GetType().Name,
            registered = tracker?.GetType().GetField("observer", flags)?.GetValue(tracker) is not null,
            implementationHover = tracker?.GetType().GetProperty("IsOver", flags)?.GetValue(tracker),
            ownsCursor = tracker?.GetType().GetProperty("OwnsCursor", flags)?.GetValue(tracker),
            hits = VisualTreeHelper.FindElementsInHostCoordinates(position, XamlRoot.Content).Take(8)
                .Select(item => new { type = item.GetType().FullName, name = (item as FrameworkElement)?.Name }).ToArray() };
    }
}
