using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunPresentationSmokeAsync(string folder)
    {
        var checks = new (FrameworkElement Fixture, Func<BleeckerColorScheme, Task> Verify)[]
        {
            (Toggles, scheme => Toggles.VerifyAsync(scheme)),
            (Buttons, async scheme =>
            {
                using var input = new NativeInputSession(CatalogRoot.XamlRoot, folder, $"buttons-rest-{scheme}");
                var heading = (FrameworkElement)((Microsoft.UI.Xaml.Controls.StackPanel)Buttons.Content).Children[0];
                await input.AcquireAsync(heading); await input.MoveAsync(heading); await Buttons.VerifyAsync(scheme);
            }),
            (IconButtons, scheme => IconButtons.VerifyAsync(scheme)), (IconBadges, scheme => IconBadges.VerifyAsync(scheme)), (Empties, _ => Empties.VerifyAsync()), (ProgressStacks, _ => ProgressStacks.VerifyAsync()), (ToggleGroups, _ => ToggleGroups.VerifyAsync()),
            (Cards, scheme => Cards.VerifyAsync(scheme)), (Separators, scheme => Separators.VerifyAsync(scheme)),
            (StatusBadges, scheme => StatusBadges.VerifyAsync(scheme)), (Menus, scheme => Menus.VerifyAsync(scheme, folder)),
            (Notifications, scheme => Notifications.VerifyAsync(scheme, folder, CatalogRoot)), (ActivityItems, scheme => ActivityItems.VerifyAsync(scheme, folder, CatalogRoot)),
            (Tooltips, scheme => Tooltips.VerifyAsync(scheme, folder, CatalogRoot)), (FieldHelp, scheme => FieldHelp.VerifyAsync(scheme, folder)),
            (Labels, scheme => Labels.VerifyAsync(scheme, folder)), (Inputs, scheme => Inputs.VerifyAsync(scheme, folder)),
            (Keyboard, scheme => Keyboard.VerifyAsync(scheme, folder)), (Eyebrows, scheme => Eyebrows.VerifyAsync(scheme, folder)),
            (Progress, scheme => Progress.VerifyAsync(scheme, folder)), (Switches, scheme => Switches.VerifyAsync(scheme, folder))
        };
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            CatalogRoot.RequestedTheme = theme; var scheme = theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark;
            foreach (var check in checks) { await SelectFixtureAsync(check.Fixture); await check.Verify(scheme); }
            await CaptureFixtureAsync(Switches, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), System.Text.Json.JsonSerializer.Serialize(new { passed = true, component = "presentation", environment = RuntimeEnvironment() }));
    }

    // Nonanimated state checks need actual presentation, not a fixed sleep.
    internal static async Task AwaitPresentationAsync(FrameworkElement element)
    {
        element.UpdateLayout();
        var presented = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var frames = 0;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        void Rendering(object? sender, object args) { if (++frames == 2) presented.TrySetResult(); }
        CompositionTarget.Rendering += Rendering;
        try { await presented.Task.WaitAsync(deadline.Token); }
        finally { CompositionTarget.Rendering -= Rendering; }
        await ElementCompositionPreview.GetElementVisual(element).Compositor.RequestCommitAsync().AsTask().WaitAsync(deadline.Token);
    }
}
