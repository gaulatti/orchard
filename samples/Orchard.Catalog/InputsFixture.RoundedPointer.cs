using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class InputsFixture
{
    private async Task VerifyRoundedDisabledPointerAsync(BleeckerColorScheme scheme, string folder)
    {
        using var input = new NativeInputSession(XamlRoot, folder, $"input-rounded-{scheme}");
        var samples = new List<object>();
        var windowCaptures = new List<object>();
        var passed = false;
        string? error = null;
        var controls = new[] { SmallInput, MediumInput, LargeInput };
        var originals = controls.Select(control => (control.IsEnabled, control.IsError)).ToArray();
        try
        {
            await input.AcquireAsync((FrameworkElement)((StackPanel)Content).Children[0]);
            ReadOnlyInput.Focus(FocusState.Keyboard);
            foreach (var control in controls)
            {
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
                        await AwaitAsync(() => NativeInputSession.CurrentVisibleCursor() == expected &&
                            Hex(Find<Border>(control, "BorderElement").BorderBrush) == border &&
                            editor.Opacity == opacity && control.FocusState == FocusState.Unfocused,
                            $"{control.InputSize}, invalid={invalid}, {corner} {local.X},{local.Y}: rounded hover/cursor mismatch.",
                            () => $"cursor={NativeInputSession.CurrentVisibleCursor()}, expected={expected}; border={Hex(Find<Border>(control, "BorderElement").BorderBrush)}, expected={border}; opacity={editor.Opacity:R}, expected={opacity:R}; focus={control.FocusState}");
                        samples.Add(new { size = control.InputSize.ToString(), invalid, corner, point, local.Hover,
                            cursor = NativeInputSession.CurrentVisibleCursor().ToInt64(), border, editor.Opacity,
                            focus = control.FocusState.ToString(), width = control.ActualWidth, height = control.ActualHeight });
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
            await AwaitAsync(() => NativeInputSession.CurrentVisibleCursor() == SystemCursor(IntPtr.Zero, new IntPtr(32512)),
                "Leaving all rounded disabled inputs retained the override cursor.");
            passed = true;
        }
        catch (Exception exception) { error = exception.ToString(); throw; }
        finally
        {
            for (var index = 0; index < controls.Length; index++)
            {
                controls[index].IsEnabled = originals[index].IsEnabled;
                controls[index].IsError = originals[index].IsError;
            }
            UpdateLayout();
            await File.WriteAllTextAsync(Path.Combine(folder, $"input-rounded-pointer-{scheme}.json"),
                JsonSerializer.Serialize(new { passed, scheme, error, scale = XamlRoot.RasterizationScale, samples, windowCaptures },
                    new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
