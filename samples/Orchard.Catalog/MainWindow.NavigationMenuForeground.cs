using System.Text.Json;
using Microsoft.UI.Xaml;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunNavigationMenuForegroundAsync(string folder)
    {
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            CatalogRoot.RequestedTheme = theme;
            await SelectFixtureAsync(NavigationMenus);
            await NavigationMenus.VerifyForegroundAsync(folder, theme);
            await CaptureFixtureAsync(NavigationMenus, Path.Combine(folder, $"navigation-menu-catalog-{theme}.png"));
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new
        {
            passed = true, component = "navigation-menu-foreground", environment = RuntimeEnvironment()
        }));
    }
}
