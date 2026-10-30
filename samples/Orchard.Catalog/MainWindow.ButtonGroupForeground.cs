using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunButtonGroupForegroundAsync(string folder)
    {
        var reports = new List<object>();
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            CatalogRoot.RequestedTheme = theme;
            await SelectFixtureAsync(Foundation);
            await AwaitPresentationAsync(ActionGroup);
            var first = (Button)ActionGroup.Children[0];
            var second = (Button)ActionGroup.Children[1];
            var disabled = (Button)ActionGroup.Children[2];
            var firstClicks = 0;
            var secondClicks = 0;
            RoutedEventHandler firstClicked = (_, _) => firstClicks++;
            RoutedEventHandler secondClicked = (_, _) => secondClicks++;
            first.Click += firstClicked;
            second.Click += secondClicked;
            try
            {
                using var input = new NativeInputSession(CatalogRoot.XamlRoot, folder, $"button-group-{theme}");
                await input.AcquireAsync(first);
                await input.ClickAsync(first);
                await WaitForAsync(() => firstClicks == 1 && secondClicks == 0, "Button Group pointer click missed the first consumer action.");
                Require(first.Focus(FocusState.Keyboard), "Button Group first action refused keyboard focus.");
                input.Key(0x09);
                await WaitForAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(CatalogRoot.XamlRoot), second), "Button Group Tab missed the second consumer action.");
                input.Key(0x0D);
                await WaitForAsync(() => firstClicks == 1 && secondClicks == 1, "Button Group Enter missed the second consumer action.");
                input.Key(0x09);
                await WaitForAsync(() => !ReferenceEquals(FocusManager.GetFocusedElement(CatalogRoot.XamlRoot), second), "Button Group Tab did not leave the second action.");
                Require(!ReferenceEquals(FocusManager.GetFocusedElement(CatalogRoot.XamlRoot), disabled) && !disabled.IsEnabled, "Button Group Tab entered its disabled action.");
                await input.ClickAsync(disabled);
                Require(firstClicks == 1 && secondClicks == 1, "Button Group disabled action invoked through pointer input.");
                reports.Add(new { theme = theme.ToString(), pointerFirst = firstClicks, keyboardSecond = secondClicks, disabledSkipped = true, animationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled });
            }
            finally
            {
                first.Click -= firstClicked;
                second.Click -= secondClicked;
            }
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "button-group-foreground.json"), JsonSerializer.Serialize(new { passed = true, reports }, new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "button-group", environment = RuntimeEnvironment() }));
    }

    private static async Task WaitForAsync(Func<bool> condition, string failure)
    {
        for (var attempt = 0; attempt < 60 && !condition(); attempt++) await Task.Delay(16);
        Require(condition(), failure);
    }
}
