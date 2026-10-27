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

        var header = Surface(Header);
        Require(header.Focus(FocusState.Keyboard), "Header Select trigger rejected keyboard focus.");
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), header),
            "Header Select trigger did not receive keyboard focus.");
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

        await File.WriteAllTextAsync(Path.Combine(folder, $"select-foreground-{scheme}.json"), JsonSerializer.Serialize(new
        {
            passed = true, scheme, pointerSelectValue = "b", pointerFocusedSelectedOption = true,
            pointerRestoredFocus = true, headerKeyboardValue = "a", headerInitiallyFocusedPlaceholder = true,
            headerDownFocusedOriginalOption = true, headerRestoredFocus = true, escapePreservedValue = true
        }));
    }
}
