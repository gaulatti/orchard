using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI.Content;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class CheckboxesFixture
{
    private async Task VerifyDisabledPointerAsync(NativeInputSession input, BleeckerColorScheme scheme, string folder)
    {
        var samples = new List<object>();
        var passed = false;
        var enabled = DisabledOff.IsEnabled;
        var direction = DisabledOff.FlowDirection;
        var hitTestVisible = DisabledOff.IsHitTestVisible;
        var visibility = DisabledOff.Visibility;
        var template = DisabledOff.Template;
        var index = Rows.Children.IndexOf(DisabledOff);
        var host = new Grid { Height = DisabledOff.ActualHeight };
        var overlay = new Border { Width = 20, Height = 20, HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top, Background = new SolidColorBrush(Microsoft.UI.Colors.Black), Visibility = Visibility.Collapsed };
        var editor = new BleeckerInput { Name = "CheckboxPointerEditor", Width = 240, Text = "Shared cursor ownership", IsEnabled = false };
        ScrollViewer? viewport = null;
        var scrollOffset = 0d;
        try
        {
            await input.MoveAsync(Find<TextBlock>(Off, "PART_Label"));
            await WaitAsync(() => CurrentCheckboxCursor() == CheckboxSystemCursor(IntPtr.Zero, new IntPtr(32512)) &&
                Math.Abs(Find<Border>(Off, "PART_Box").BorderBrush.Opacity - .5) < 1e-9,
                "Enabled label hover lost its associated-input border or arrow cursor.");
            samples.Add(new { phase = "enabled-label-hover", cursor = CurrentCheckboxCursor().ToInt64() });
            foreach (var control in new[] { DisabledOff, DisabledOn })
            {
                await input.MoveAsync(Find<Border>(control, "PART_Box"));
                await Check(control, "disabled-box", true, 32648);
                await MainWindow.CaptureElementAsync(control, Path.Combine(folder, $"checkbox-{control.Name}-box-hover-{scheme}.png"));
                await input.MoveAsync(Find<TextBlock>(control, "PART_Label"));
                await Check(control, "disabled-label", false, 32512);
                await input.MoveAsync(Find<TextBlock>(control, "PART_Description"));
                await Check(control, "disabled-description", false, 32512);
                await MainWindow.CaptureElementAsync(control, Path.Combine(folder, $"checkbox-{control.Name}-label-hover-{scheme}.png"));
            }

            await input.MoveAsync(Find<Border>(DisabledOff, "PART_Box"));
            await Check(DisabledOff, "before-enable", true, 32648);
            var anchor = PointerPosition();
            DisabledOff.IsEnabled = true;
            await Check(DisabledOff, "enabled-under-pointer", true, 32512, anchor);
            DisabledOff.IsEnabled = false;
            await Check(DisabledOff, "disabled-under-pointer", true, 32648, anchor);
            DisabledOff.Template = null;
            Require(CurrentCheckboxCursor() != CheckboxSystemCursor(IntPtr.Zero, new IntPtr(32648)),
                "Template detachment retained the disabled box cursor.");
            samples.Add(new { phase = "template-detached", cursor = CurrentCheckboxCursor().ToInt64() });
            DisabledOff.Template = template;
            DisabledOff.ApplyTemplate(); UpdateLayout();
            await Check(DisabledOff, "template-restored", true, 32648, anchor);

            Rows.Children.Remove(DisabledOff);
            host.Children.Add(DisabledOff); host.Children.Add(overlay);
            Rows.Children.Insert(index, host); UpdateLayout();
            await WaitAsync(() => DisabledOff.IsLoaded, "Disabled pointer host did not load its checkbox.");
            await input.MoveAsync(Find<Border>(DisabledOff, "PART_Box"));
            anchor = PointerPosition();
            await Check(DisabledOff, "remounted-in-host", true, 32648, anchor);
            for (var round = 0; round < 3; round++)
            {
                host.Children.Remove(DisabledOff);
                Rows.Children.Insert(index, DisabledOff);
                Rows.Children.Remove(DisabledOff);
                host.Children.Insert(0, DisabledOff); UpdateLayout();
                await Check(DisabledOff, $"rapid-remount-{round}", true, 32648, anchor);
            }
            overlay.Visibility = Visibility.Visible; UpdateLayout();
            await Check(DisabledOff, "occluded", false, 32512, anchor);
            overlay.Visibility = Visibility.Collapsed; UpdateLayout();
            await Check(DisabledOff, "uncovered", true, 32648, anchor);
            host.Clip = new RectangleGeometry { Rect = new Rect(15, 0, host.ActualWidth - 15, host.ActualHeight) };
            UpdateLayout();
            await Check(DisabledOff, "clipped", false, 32512, anchor);
            host.Clip = null; UpdateLayout();
            await Check(DisabledOff, "unclipped", true, 32648, anchor);
            host.Margin = new Thickness(40, 0, 0, 0); UpdateLayout();
            await Check(DisabledOff, "moved-away", false, 32512, anchor);
            host.Margin = new Thickness(0); UpdateLayout();
            await Check(DisabledOff, "moved-back", true, 32648, anchor);
            DisabledOff.IsHitTestVisible = false; UpdateLayout();
            await Check(DisabledOff, "hit-testing-disabled", false, 32512, anchor);
            DisabledOff.IsHitTestVisible = true; UpdateLayout();
            await Check(DisabledOff, "hit-testing-restored", true, 32648, anchor);
            DisabledOff.Visibility = Visibility.Collapsed; UpdateLayout();
            await WaitAsync(() => CurrentCheckboxCursor() == CheckboxSystemCursor(IntPtr.Zero, new IntPtr(32512)),
                "Hidden checkbox retained its blocked cursor.");
            samples.Add(new { phase = "hidden", cursor = CurrentCheckboxCursor().ToInt64() });
            DisabledOff.Visibility = Visibility.Visible; UpdateLayout();
            await Check(DisabledOff, "shown", true, 32648, anchor);
            host.Children.Remove(DisabledOff); UpdateLayout();
            await WaitAsync(() => !DisabledOff.IsLoaded && CurrentCheckboxCursor() == CheckboxSystemCursor(IntPtr.Zero, new IntPtr(32512)),
                "Unloaded checkbox retained its blocked cursor.");
            samples.Add(new { phase = "unloaded", cursor = CurrentCheckboxCursor().ToInt64() });
            host.Children.Insert(0, DisabledOff); UpdateLayout();
            await Check(DisabledOff, "reloaded", true, 32648, anchor);
            for (DependencyObject? ancestor = host; ancestor is not null; ancestor = VisualTreeHelper.GetParent(ancestor))
                if (ancestor is ScrollViewer scroll) { viewport = scroll; break; }
            Require(viewport is not null, "Disabled pointer fixture has no scroll viewport.");
            scrollOffset = viewport!.VerticalOffset;
            Require(scrollOffset + 25 <= viewport.ScrollableHeight, "Disabled pointer fixture has insufficient scroll range.");
            viewport.ChangeView(null, scrollOffset + 25, null, true);
            await WaitAsync(() => Math.Abs(viewport.VerticalOffset - scrollOffset - 25) < 1, "Disabled pointer viewport did not scroll.");
            await Check(DisabledOff, "scrolled-away", false, 32512, anchor);
            viewport.ChangeView(null, scrollOffset, null, true);
            await WaitAsync(() => Math.Abs(viewport.VerticalOffset - scrollOffset) < 1, "Disabled pointer viewport did not restore its offset.");
            await Check(DisabledOff, "scrolled-back", true, 32648, anchor);

            DisabledOff.FlowDirection = FlowDirection.RightToLeft; UpdateLayout();
            await input.MoveAsync(Find<Border>(DisabledOff, "PART_Box"));
            await Check(DisabledOff, "rtl-box", true, 32648);
            await input.MoveAsync(Find<TextBlock>(DisabledOff, "PART_Label"));
            await Check(DisabledOff, "rtl-label", false, 32512);
            DisabledOff.FlowDirection = direction; UpdateLayout();

            Rows.Children.Insert(index, editor); UpdateLayout();
            await input.MoveAsync(editor);
            await WaitAsync(() => CurrentCheckboxCursor() == CheckboxSystemCursor(IntPtr.Zero, new IntPtr(32648)),
                "Disabled editor did not select its blocked cursor.");
            samples.Add(new { phase = "input-blocked", cursor = CurrentCheckboxCursor().ToInt64() });
            await input.MoveAsync(Find<Border>(DisabledOff, "PART_Box"));
            await Check(DisabledOff, "input-to-checkbox", true, 32648);
            editor.IsEnabled = true;
            await Check(DisabledOff, "other-owner-enabled", true, 32648);
            samples.Add(new { phase = "editor-enabled-away", cursor = DescribeEditorCursor(input, editor) });
            await input.MoveAsync(editor);
            samples.Add(new { phase = "editor-entered", cursor = DescribeEditorCursor(input, editor) });
            try
            {
                await WaitAsync(() => CurrentCheckboxCursor() == CheckboxSystemCursor(IntPtr.Zero, new IntPtr(32513)),
                    "Enabled editor lost its text cursor after leaving a disabled checkbox.");
            }
            catch
            {
                samples.Add(new { phase = "editor-cursor-failed", cursor = DescribeEditorCursor(input, editor),
                    previousOwner = DescribeDisabledPointer(input, DisabledOff) });
                throw;
            }
            samples.Add(new { phase = "checkbox-to-input", cursor = CurrentCheckboxCursor().ToInt64() });
            editor.IsEnabled = false;
            await WaitAsync(() => CurrentCheckboxCursor() == CheckboxSystemCursor(IntPtr.Zero, new IntPtr(32648)),
                "Stationary disabling did not restore the editor's blocked cursor.");
            samples.Add(new { phase = "input-disabled-under-pointer", cursor = CurrentCheckboxCursor().ToInt64() });
            var editorAnchor = PointerPosition();
            for (var round = 0; round < 3; round++)
            {
                Rows.Children.Remove(editor); Rows.Children.Insert(index, editor); UpdateLayout();
                await WaitAsync(() => editor.IsLoaded && CurrentCheckboxCursor() == CheckboxSystemCursor(IntPtr.Zero, new IntPtr(32648)) &&
                    Hex(Find<Border>(editor, "BorderElement").BorderBrush) == (scheme == BleeckerColorScheme.Dark ? "#40FFFFFF" : "#B3E6D5B8"),
                    $"Rapid Input remount {round} lost its disabled hover border/cursor.");
                var point = PointerPosition();
                Require(point.X == editorAnchor.X && point.Y == editorAnchor.Y, "Pointer moved during rapid Input remount.");
                samples.Add(new { phase = "input-rapid-remount", round, cursor = CurrentCheckboxCursor().ToInt64(),
                    border = Hex(Find<Border>(editor, "BorderElement").BorderBrush) });
            }
            await input.MoveAsync(CheckboxHeading);
            await WaitAsync(() => CurrentCheckboxCursor() == CheckboxSystemCursor(IntPtr.Zero, new IntPtr(32512)),
                "Leaving both disabled controls retained an override cursor.");
            samples.Add(new { phase = "all-owners-exited", cursor = CurrentCheckboxCursor().ToInt64() });
            passed = true;

            async Task Check(BleeckerCheckbox control, string phase, bool hover, int cursor, PointerPoint? stationary = null)
            {
                var alpha = (float)(control.IsChecked == true ? 1 : hover ? .5 : scheme == BleeckerColorScheme.Light ? .6 : .2);
                try
                {
                    await WaitAsync(() => CurrentCheckboxCursor() == CheckboxSystemCursor(IntPtr.Zero, new IntPtr(cursor)) &&
                        Math.Abs(Find<Border>(control, "PART_Box").BorderBrush.Opacity - alpha) < 1e-9 &&
                        Find<Grid>(control, "PART_InputChrome").Opacity == (control.IsEnabled ? 1 : .5),
                        $"Disabled pointer phase {phase} did not reach its expected border, opacity and cursor.");
                }
                catch
                {
                    var failedBrush = Find<Border>(control, "PART_Box").BorderBrush;
                    samples.Add(new { phase, failed = true, control.Name, control.IsEnabled, control.IsChecked,
                        border = Hex(failedBrush), failedBrush.Opacity, expectedAlpha = alpha,
                        inputOpacity = Find<Grid>(control, "PART_InputChrome").Opacity,
                        cursor = CurrentCheckboxCursor().ToInt64(), expectedCursor = cursor,
                        expectedCursorHandle = CheckboxSystemCursor(IntPtr.Zero, new IntPtr(cursor)).ToInt64(),
                        pointer = DescribeDisabledPointer(input, control) });
                    throw;
                }
                var position = PointerPosition();
                if (stationary is PointerPoint expected) Require(position.X == expected.X && position.Y == expected.Y,
                    $"Stationary pointer moved during {phase}.");
                Require(Find<TextBlock>(control, "PART_Label").Opacity == 1 && Find<TextBlock>(control, "PART_Description").Opacity == 1 &&
                    Find<Viewbox>(control, "PART_Glyph").Opacity == (control.IsChecked == true ? 1 : 0),
                    $"Disabled pointer phase {phase} altered text or glyph opacity.");
                var brush = Find<Border>(control, "PART_Box").BorderBrush;
                var expectedColor = control.IsChecked == true || hover
                    ? BleeckerTokens.Colors(scheme)[scheme == BleeckerColorScheme.Light ? "sea" : "accentBlue"]
                    : scheme == BleeckerColorScheme.Light ? "#FFE6D5B8" : "#FFFFFFFF";
                Require(Hex(brush) == expectedColor, $"Disabled pointer phase {phase} selected the wrong theme color.");
                if (!control.IsEnabled) Require(!control.Focus(FocusState.Keyboard), $"Disabled pointer phase {phase} accepted keyboard focus.");
                samples.Add(new { phase, control.Name, control.IsEnabled, control.IsChecked, hover,
                    direction = control.FlowDirection.ToString(), border = Hex(brush), brush.Opacity,
                    inputOpacity = Find<Grid>(control, "PART_InputChrome").Opacity, cursor = CurrentCheckboxCursor().ToInt64(),
                    expectedCursor = cursor, position.X, position.Y });
            }
        }
        finally
        {
            if (viewport is not null) viewport.ChangeView(null, scrollOffset, null, true);
            Rows.Children.Remove(editor);
            host.Children.Remove(DisabledOff);
            Rows.Children.Remove(host);
            if (!Rows.Children.Contains(DisabledOff)) Rows.Children.Insert(Math.Min(index, Rows.Children.Count), DisabledOff);
            DisabledOff.Template = template; DisabledOff.IsEnabled = enabled; DisabledOff.FlowDirection = direction;
            DisabledOff.IsHitTestVisible = hitTestVisible; DisabledOff.Visibility = visibility;
            UpdateLayout();
            await File.WriteAllTextAsync(Path.Combine(folder, $"checkbox-disabled-pointer-{scheme}.json"),
                JsonSerializer.Serialize(new { passed, scheme, displayScale = XamlRoot.RasterizationScale, samples }, new JsonSerializerOptions { WriteIndented = true }));
        }
        physicalInputSteps.Add("Disabled box-only hover/cursor, stationary lifecycle, clipping/scrolling/RTL and shared Input cursor ownership");
    }

    private object DescribeEditorCursor(NativeInputSession input, BleeckerInput editor)
    {
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
        var tracker = typeof(BleeckerInput).GetField("disabledPointer", flags)?.GetValue(editor);
        var protectedCursor = typeof(UIElement).GetProperty("ProtectedCursor", flags)?.GetValue(editor) as InputCursor;
        var island = ContentIsland.FindAllForCurrentThread().Single(item => item.Environment.Equals(editor.XamlRoot.ContentIslandEnvironment));
        var sourceCursor = InputPointerSource.GetForIsland(island).Cursor;
        var screen = PointerPosition();
        var client = NativeScreenCapture.ClientBounds(input.Window);
        var position = new Point((screen.X - client.X) / editor.XamlRoot.RasterizationScale,
            (screen.Y - client.Y) / editor.XamlRoot.RasterizationScale);
        return new { editor.IsEnabled, editor.IsLoaded, focus = editor.FocusState.ToString(),
            native = CurrentCheckboxCursor().ToInt64(), expectedText = CheckboxSystemCursor(IntPtr.Zero, new IntPtr(32513)).ToInt64(),
            protectedCursor = Cursor(protectedCursor), sourceCursor = Cursor(sourceCursor),
            registered = tracker?.GetType().GetField("observer", flags)?.GetValue(tracker) is not null,
            implementationHover = tracker?.GetType().GetProperty("IsOver", flags)?.GetValue(tracker),
            pointerOver = typeof(BleeckerInput).GetField("pointerOver", flags)?.GetValue(editor),
            hits = VisualTreeHelper.FindElementsInHostCoordinates(position, editor.XamlRoot.Content).Take(8)
                .Select(item => new { type = item.GetType().FullName, name = (item as FrameworkElement)?.Name }).ToArray() };

        static object Cursor(InputCursor? value) => new { type = value?.GetType().FullName,
            shape = (value as InputSystemCursor)?.CursorShape.ToString() };
    }

    private static PointerPoint PointerPosition() { Require(ReadCheckboxPointer(out var point), "Could not read physical checkbox pointer."); return point; }
    private object DescribeDisabledPointer(NativeInputSession input, BleeckerCheckbox control)
    {
        var screen = PointerPosition();
        var client = NativeScreenCapture.ClientBounds(input.Window);
        var position = new Point((screen.X - client.X) / XamlRoot.RasterizationScale,
            (screen.Y - client.Y) / XamlRoot.RasterizationScale);
        var root = XamlRoot.Content;
        var hits = VisualTreeHelper.FindElementsInHostCoordinates(position, root).Take(8).ToArray();
        var ancestors = new List<object>();
        for (DependencyObject? current = Find<Border>(control, "PART_Box"); current is not null; current = VisualTreeHelper.GetParent(current))
            ancestors.Add(Describe(current));
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var tracker = typeof(BleeckerCheckbox).GetField("disabledPointer", flags)?.GetValue(control);
        var observer = tracker?.GetType().GetField("observer", flags)?.GetValue(tracker);
        // Internal fields are diagnostics only; acceptance uses rendered output
        // and physical cursor handles rather than the implementation's booleans.
        return new { control.IsLoaded, control.IsHitTestVisible, screen.X, screen.Y, rootPosition = position,
            templateAssigned = control.Template is not null, boundBoxIsNull = typeof(BleeckerCheckbox).GetField("box", flags)?.GetValue(control) is null,
            boxBounds = input.ScreenBounds(Find<Border>(control, "PART_Box")), registered = observer is not null,
            implementationHover = tracker?.GetType().GetProperty("IsOver", flags)?.GetValue(tracker),
            ancestors, hits = hits.Select(Describe).ToArray() };

        object Describe(DependencyObject item)
        {
            var element = item as FrameworkElement;
            var ui = item as UIElement;
            var clip = ui?.Clip as RectangleGeometry;
            return new { type = item.GetType().FullName, element?.Name, element?.IsLoaded, ui?.IsHitTestVisible,
                visibility = ui?.Visibility.ToString(), element?.ActualWidth, element?.ActualHeight,
                position = element?.TransformToVisual(root).TransformPoint(new Point()), clip = clip?.Rect };
        }
    }
    [StructLayout(LayoutKind.Sequential)] private struct PointerPoint { public int X; public int Y; }
    [DllImport("user32.dll", EntryPoint = "GetCursorPos")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ReadCheckboxPointer(out PointerPoint point);
    [DllImport("user32.dll", EntryPoint = "GetCursor")] private static extern IntPtr CurrentCheckboxCursor();
    [DllImport("user32.dll", EntryPoint = "LoadCursorW")] private static extern IntPtr CheckboxSystemCursor(IntPtr instance, IntPtr name);
}
