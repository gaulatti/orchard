using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class SelectsFixture
{
    internal async Task VerifyForegroundAsync(string folder, BleeckerColorScheme scheme)
    {
        Status.Text = "No value requested.";
        Medium.Value = "a";
        Header.Value = "";
        await MainWindow.AwaitPresentationAsync(Medium);
        using var input = new NativeInputSession(XamlRoot, folder, $"select-foreground-{scheme}");
        await input.AcquireAsync(Status);

        static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
        {
            yield return parent;
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
                foreach (var child in Descendants(VisualTreeHelper.GetChild(parent, index))) yield return child;
        }
        static BleeckerSelectSurface Surface(BleeckerSelect select) => Descendants(select).OfType<BleeckerSelectSurface>().Single();
        static BleeckerSelectItem Option(BleeckerSelectSurface surface, string value) => surface.Items.OfType<BleeckerSelectItem>()
            .Single(item => item.Tag is BleeckerSelectOption option && option.Value == value);
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

        var medium = Surface(Medium);
        await input.ClickAsync(medium);
        await AwaitAsync(() => medium.IsDropDownOpen && ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), Option(medium, "a")),
            "Pointer opening Select did not focus its selected option.");
        await MainWindow.AwaitPresentationAsync(Option(medium, "a"));
        await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"select-foreground-medium-open-{scheme}.png"));
        await input.ClickAsync(Option(medium, "b"));
        await AwaitAsync(() => !medium.IsDropDownOpen && Medium.Value == "b" && Status.Text == "Consumer accepted: b",
            "Pointer option selection did not reach the original consumer value request.");
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), medium),
            "Pointer selection did not restore trigger focus.");

        input.Key(0x20); // Space reopens the focused Select.
        await AwaitAsync(() => medium.IsDropDownOpen && ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), Option(medium, "b")),
            "Select Space did not reopen with its current option focused.");
        input.Key(0x24); // Home reaches the first enabled option.
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), Option(medium, "a")),
            "Select Home did not focus the first option.");
        input.Key(0x09); // Tab stays within the open chooser.
        await AwaitAsync(() => medium.IsDropDownOpen && ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), Option(medium, "a")),
            "Select Tab left the open chooser.");
        input.Key(0x23); // End skips the disabled option.
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), Option(medium, "b")),
            "Select End focused a disabled option or missed the last enabled option.");
        input.Key(0x26); // Up returns to the first option.
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), Option(medium, "a")),
            "Select Up did not focus the previous option.");
        input.Key(0x0D);
        await AwaitAsync(() => !medium.IsDropDownOpen && Medium.Value == "a" && Status.Text == "Consumer accepted: a",
            "Select keyboard choice did not reach the original consumer.");
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), medium),
            "Select keyboard selection did not restore trigger focus.");
        input.Key(0x0D);
        await AwaitAsync(() => medium.IsDropDownOpen, "Select did not reopen for Escape dismissal.");
        input.Key(0x1B);
        await AwaitAsync(() => !medium.IsDropDownOpen && Medium.Value == "a" && Status.Text == "Consumer accepted: a",
            "Select Escape changed the caller value or left the menu open.");
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), medium),
            "Select Escape did not restore trigger focus before typeahead.");
        input.Key(0x4C); // L cycles to the next label with the same prefix while closed.
        await AwaitAsync(() => !medium.IsDropDownOpen && Medium.Value == "b" && Status.Text == "Consumer accepted: b",
            "Closed Select typeahead did not request the matching original option.");
        input.Key(0x0D);
        await AwaitAsync(() => medium.IsDropDownOpen && ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), Option(medium, "b")),
            "Select did not reopen on its typeahead-selected option.");
        input.Key(0x4C); // L cycles focus to the next matching label without committing.
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), Option(medium, "a")) && Medium.Value == "b",
            "Open Select typeahead changed the value or missed its matching option.");
        await input.ClickAsync(Status);
        await AwaitAsync(() => !medium.IsDropDownOpen && Medium.Value == "b" && Status.Text == "Consumer accepted: b",
            "Select outside click did not dismiss the menu while preserving the caller value.");

        var header = Surface(Header);
        Require(header.Focus(FocusState.Keyboard), "Header Select trigger rejected keyboard focus.");
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), header),
            "Header Select trigger did not receive keyboard focus.");
        var reducedMotion = !new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
        if (reducedMotion)
        {
            var ring = Descendants(header).OfType<Border>().Single(item => item.Name == "Ring");
            await Task.Delay(32);
            Require(Math.Abs(ring.Opacity - 1) < .000001,
                $"Reduced-motion Header Select focus ring did not reach its endpoint: opacity={ring.Opacity}, focus={header.FocusState}.");
        }
        input.Key(0x0D); // Enter opens the original option surface.
        await AwaitAsync(() => header.IsDropDownOpen && ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), Option(header, "")),
            "Keyboard opening Header Select did not focus its placeholder option.");
        await MainWindow.AwaitPresentationAsync(Option(header, ""));
        await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"select-foreground-header-open-{scheme}.png"));
        input.Key(0x28); // Down reaches the first caller-owned option.
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), Option(header, "a")),
            "Header Select Down did not focus the first original option.");
        input.Key(0x0D);
        await AwaitAsync(() => !header.IsDropDownOpen && Header.Value == "a" && Status.Text == "Consumer accepted: a",
            "Header Select Enter did not reach the original consumer value request.");
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), header),
            "Header Select selection did not restore trigger focus.");
        input.Key(0x0D);
        await AwaitAsync(() => header.IsDropDownOpen, "Header Select did not reopen for Escape dismissal.");
        input.Key(0x1B);
        await AwaitAsync(() => !header.IsDropDownOpen && Header.Value == "a" && Status.Text == "Consumer accepted: a",
            "Header Select Escape changed the caller value or left the menu open.");

        await input.ClickAsync(header);
        await AwaitAsync(() => header.IsDropDownOpen && ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), Option(header, "a")),
            "Pointer opening Header Select did not focus its current option.");
        await input.ClickAsync(Option(header, "b"));
        await AwaitAsync(() => !header.IsDropDownOpen && Header.Value == "b" && Status.Text == "Consumer accepted: b",
            "Header Select pointer choice did not reach the original consumer.");
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), header),
            "Header Select pointer selection did not restore trigger focus.");

        Require(header.Focus(FocusState.Keyboard), "Header Select rejected focus before typeahead.");
        input.Key(0x4C); // The injected Local suite placeholder follows Beta in cyclic order.
        await AwaitAsync(() => !header.IsDropDownOpen && Header.Value == "" && Status.Text == "Consumer accepted: ",
            "Closed Header Select typeahead did not request its matching placeholder option.");
        input.Key(0x4C); // Repeating the prefix cycles to the original Alpha option.
        await AwaitAsync(() => !header.IsDropDownOpen && Header.Value == "a" && Status.Text == "Consumer accepted: a",
            "Closed Header Select typeahead did not cycle to the next matching caller option.");
        input.Key(0x0D);
        await AwaitAsync(() => header.IsDropDownOpen && ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), Option(header, "a")),
            "Header Select did not reopen on its typeahead-selected option.");
        input.Key(0x4C); // L moves focus to Beta without committing.
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), Option(header, "b")) && Header.Value == "a",
            "Open Header Select typeahead changed the caller value or missed its matching option.");
        await MainWindow.AwaitPresentationAsync(Option(header, "b"));
        await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"select-foreground-header-typeahead-{scheme}.png"));
        await input.ClickAsync(Status);
        await AwaitAsync(() => !header.IsDropDownOpen && Header.Value == "a" && Status.Text == "Consumer accepted: a",
            "Header Select outside click changed the caller value or left the menu open.");

        await File.WriteAllTextAsync(Path.Combine(folder, $"select-foreground-{scheme}.json"), JsonSerializer.Serialize(new
        {
            passed = true, scheme, pointerSelectValue = "b", pointerFocusedSelectedOption = true,
            pointerRestoredFocus = true, selectKeyboardValue = "a", selectHomeEndUpAndTab = true,
            selectKeyboardRestoredFocus = true, selectEscapePreservedValue = true,
            selectClosedTypeaheadValue = "b", selectOpenTypeaheadFocusedValue = "a", selectOutsideClickPreservedValue = true,
            headerKeyboardValue = "a", headerInitiallyFocusedPlaceholder = true,
            headerDownFocusedOriginalOption = true, headerRestoredFocus = true, escapePreservedValue = true,
            headerPointerValue = "b", headerPointerRestoredFocus = true,
            headerClosedTypeaheadValues = new[] { "", "a" }, headerOpenTypeaheadFocusedValue = "b", headerOutsideClickPreservedValue = true,
            headerReducedMotion = reducedMotion
        }));
    }
}
