using System.Text.Json;
using Microsoft.UI.Xaml;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunPopoverForegroundAsync(string folder)
    {
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            CatalogRoot.RequestedTheme = theme;
            await SelectFixtureAsync(Popovers);
            await Popovers.VerifyForegroundAsync(folder, theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark);
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new
        {
            passed = true, component = "popover-foreground", environment = RuntimeEnvironment()
        }));
    }
}
