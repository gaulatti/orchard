using System.Text.Json;
using Microsoft.UI.Xaml;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunTextareaCatalogAsync(string folder)
    {
        foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
        {
            ThemeSelector.SelectedIndex=theme==ElementTheme.Light?1:2;await SelectFixtureAsync(Textareas);
            await Textareas.VerifyAsync(theme==ElementTheme.Light?BleeckerColorScheme.Light:BleeckerColorScheme.Dark,folder,ComponentSearch);
            await CaptureAsync(Path.Combine(folder,$"catalog-{theme.ToString().ToLowerInvariant()}.png"));
        }
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="textarea-catalog",environment=RuntimeEnvironment()}));
    }
}
