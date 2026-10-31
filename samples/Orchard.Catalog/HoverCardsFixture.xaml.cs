using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Text.Json;

namespace Orchard.Catalog;

public sealed partial class HoverCardsFixture : StackPanel
{
    private int contributorActions;

    public HoverCardsFixture() => InitializeComponent();

    private void ContributorClicked(object sender, RoutedEventArgs args) => ActionStatus.Text = $"Contributor actions: {++contributorActions}";

    internal async Task VerifyInputAsync(BleeckerColorScheme scheme, string folder)
    {
        using var input = new NativeInputSession(XamlRoot, folder, $"hover-card-{scheme}");
        await input.AcquireAsync(Contributor);
        await input.MoveAsync(Heading);
        await AwaitAsync(() => !DefaultCard.IsHovered, "Hover Card remained open away from its trigger.");
        Require(Contributor.Focus(FocusState.Keyboard), "Original Hover Card trigger could not take keyboard focus.");
        Require(!DefaultCard.IsHovered, "Keyboard focus opened the pointer-only Hover Card.");
        var initialActions = contributorActions;
        input.Key(0x20);
        await AwaitAsync(() => contributorActions == initialActions + 1, "Space did not invoke the original contributor action.");
        input.Key(0x0D);
        await AwaitAsync(() => contributorActions == initialActions + 2, "Enter did not invoke the original contributor action.");
        input.Key(0x09);
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), CallerNote),
            "Tab did not reach the caller's retained Hover Card content.");
        Require(!DefaultCard.IsHovered, "Tab focus opened the pointer-only Hover Card.");
        await input.MoveAsync(Contributor);
        await AwaitAsync(() => DefaultCard.IsHovered, "Moving the pointer onto the original trigger did not open Hover Card.");
        var surface = Find<FrameworkElement>(DefaultCard, "HoverCardTooltip");
        if (!new Windows.UI.ViewManagement.UISettings().AnimationsEnabled)
        {
            await Task.Delay(32);
            Require(Math.Abs(surface.Opacity - 1) < .000001, "Reduced-motion Hover Card did not reach its open endpoint.");
        }
        await input.MoveAsync(CallerNote);
        Require(DefaultCard.IsHovered, "Moving inside Hover Card dismissed its own surface.");
        await input.MoveAsync(Heading);
        await AwaitAsync(() => !DefaultCard.IsHovered, "Moving away did not dismiss Hover Card.");
        if (!new Windows.UI.ViewManagement.UISettings().AnimationsEnabled)
        {
            await Task.Delay(32);
            Require(Math.Abs(surface.Opacity) < .000001, "Reduced-motion Hover Card did not reach its closed endpoint.");
        }
        await input.MoveAsync(DisabledTrigger);
        await AwaitAsync(() => DisabledCard.IsHovered && !DisabledTrigger.IsEnabled,
            "Disabled original trigger did not retain pointer-only Hover Card behavior.");
        await input.MoveAsync(Heading);
        await File.WriteAllTextAsync(Path.Combine(folder, $"hover-card-input-{scheme}.json"), JsonSerializer.Serialize(new
        {
            passed = true, theme = scheme.ToString(), originalActions = contributorActions - initialActions,
            callerNoteRetained = CallerNote.Text == "Original caller note", pointerOnly = true,
            reducedMotion = !new Windows.UI.ViewManagement.UISettings().AnimationsEnabled,
            hardwareInputSent = true
        }));
    }

    private static T Find<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        if (parent is T item && item.Name == name) return item;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            try { return Find<T>(VisualTreeHelper.GetChild(parent, index), name); }
            catch (InvalidOperationException) { }
        }
        throw new InvalidOperationException($"Hover Card template part {name} is missing.");
    }

    private static async Task AwaitAsync(Func<bool> condition, string message)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (condition()) return;
            await Task.Delay(20);
        }
        throw new InvalidOperationException(message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
