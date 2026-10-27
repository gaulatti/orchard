using System.Text.Json;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunInputPointerForegroundAsync(string folder)
    {
        await SelectFixtureAsync(Inputs);
        await Inputs.VerifyPointerForegroundAsync(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new
        {
            passed = true, component = "input-pointer-foreground", environment = RuntimeEnvironment()
        }));
    }
}
