using System.Text.Json;
using Microsoft.UI.Xaml;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunMenubarForegroundAsync(string folder)
    {
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            CatalogRoot.RequestedTheme = theme;
            await SelectFixtureAsync(Menubars);
            await Menubars.VerifyForegroundAsync(folder, theme);
            await CaptureFixtureAsync(Menubars, Path.Combine(folder, $"menubar-catalog-{theme}.png"));
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new
        {
            passed = true, component = "menubar-foreground", environment = RuntimeEnvironment()
        }));
    }
}
