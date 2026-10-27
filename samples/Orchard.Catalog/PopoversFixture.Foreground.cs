using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Orchard.Catalog;

public sealed partial class PopoversFixture
{
    internal async Task VerifyForegroundAsync(string folder, BleeckerColorScheme scheme)
    {
        Status.Text = "No open request";
        Controlled.Open = false;
        var defaultTrigger = (FrameworkElement)Default.Trigger!;
        var controlledTrigger = (Button)Controlled.Trigger!;
        var callerInput = ((StackPanel)Default.Content).Children.OfType<TextBox>().Single();
        var controlledClose = ((StackPanel)Controlled.Content).Children.OfType<Button>().Single();
        await MainWindow.AwaitPresentationAsync(defaultTrigger);
        using var input = new NativeInputSession(XamlRoot, folder, $"popover-foreground-{scheme}");
        await input.AcquireAsync(Status);

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        async Task AwaitAsync(Func<bool> condition, string message)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (!condition() && watch.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(16);
            Require(condition(), message);
        }

        await input.ClickAsync(defaultTrigger);
        await AwaitAsync(() => Default.IsOpen && callerInput.IsLoaded && Status.Text == "Requested open",
            $"Pointer opening Popover did not present the original caller input: open={Default.IsOpen}, loaded={callerInput.IsLoaded}, status={Status.Text}.");
        await MainWindow.AwaitPresentationAsync(callerInput);
        await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"popover-foreground-open-{scheme}.png"));
        var defaultAction = MainWindow.Desc<Button>(defaultTrigger).Single(button => button.Name == "PART_Action");
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), callerInput),
            "Pointer opening Popover did not focus the original caller input.");
        input.Key(0x1B); // Escape dismisses the topmost original surface.
        await AwaitAsync(() => !Default.IsOpen && Status.Text == "Requested closed", "Escape did not request Popover close.");
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), defaultAction),
            "Escape did not restore Popover trigger focus.");
        Require(callerInput.Text == "Original caller input", "Popover replaced or changed its caller input.");

        Require(controlledTrigger.Focus(FocusState.Keyboard), "Controlled Popover trigger rejected keyboard focus.");
        input.Key(0x0D);
        await AwaitAsync(() => Controlled.IsOpen && Controlled.Open == true && Status.Text == "Host accepted open" &&
            ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), controlledClose),
            "Keyboard opening Controlled Popover did not reach the caller or focus its content.");
        input.Key(0x20);
        await AwaitAsync(() => !Controlled.IsOpen && Controlled.Open == false && Status.Text == "Host accepted closed",
            "Keyboard content action did not request Controlled Popover close.");

        await input.ClickAsync(defaultTrigger);
        await AwaitAsync(() => Default.IsOpen && ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), callerInput),
            "Popover did not reopen with the same caller input.");
        await input.ClickAsync(Status);
        await AwaitAsync(() => !Default.IsOpen && Status.Text == "Requested closed",
            "Physical outside click did not request Popover close.");
        Require(callerInput.Text == "Original caller input", "Outside dismissal changed the caller input.");

        var parentTrigger = (Button)NestedParent.Trigger!;
        var childTrigger = (Button)NestedChild.Trigger!;
        var childInput = (TextBox)NestedChild.Content;
        await input.ClickAsync(parentTrigger);
        await AwaitAsync(() => NestedParent.IsOpen && childTrigger.IsLoaded && Status.Text == "Parent requested open",
            "Physical pointer did not open the nested parent Popover.");
        await input.ClickAsync(childTrigger);
        await AwaitAsync(() => NestedChild.IsOpen && childInput.IsLoaded && Status.Text == "Child requested open" &&
            ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), childInput),
            "Physical pointer did not open and focus the nested child Popover.");
        await MainWindow.AwaitPresentationAsync(childInput);
        await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"popover-foreground-nested-{scheme}.png"));
        input.Key(0x1B);
        await AwaitAsync(() => !NestedChild.IsOpen && NestedParent.IsOpen && Status.Text == "Child requested closed" &&
            ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), childTrigger),
            "Escape did not close only the nested child and restore its trigger focus.");
        input.Key(0x1B);
        await AwaitAsync(() => !NestedParent.IsOpen && Status.Text == "Parent requested closed" &&
            ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), parentTrigger),
            "Second Escape did not close the parent and restore its trigger focus.");
        Require(childInput.Text == "Original child input", "Nested Popover changed the caller's original input.");

        await File.WriteAllTextAsync(Path.Combine(folder, $"popover-foreground-{scheme}.json"), JsonSerializer.Serialize(new
        {
            passed = true, scheme, pointerOpenedOriginalInput = true, escapeClosed = true,
            escapeRestoredTriggerFocus = true, controlledKeyboardRequest = true,
            controlledContentClosed = true, outsidePointerClosed = true, originalInputRetained = true,
            nestedPointerOpenedAndFocused = true, nestedEscapeClosedInOrder = true
        }));
    }
}
