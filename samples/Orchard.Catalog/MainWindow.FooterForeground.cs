using System.Text.Json;
using Microsoft.UI.Xaml;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunFooterForegroundAsync(string folder)
    {
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            CatalogRoot.RequestedTheme = theme;
            await SelectFixtureAsync(Footers);
            await Footers.VerifyForegroundAsync(folder, theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark);
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new
        {
            passed = true, component = "footer-foreground", environment = RuntimeEnvironment()
        }));
    }
}
