using System.Text.Json;

namespace Orchard.Catalog;

public sealed partial class InputsFixture
{
    internal async Task VerifyPointerForegroundAsync(string folder)
    {
        await MainWindow.AwaitPresentationAsync(PointerCheckButton);
        using var input = new NativeInputSession(XamlRoot, folder, "input-pointer-foreground");
        await input.AcquireAsync(PointerCheckButton);
        var started = DateTime.UtcNow;
        await input.MoveAsync(PointerCheckButton);
        input.PointerButton(true);
        input.PointerButton(false);
        await AwaitAsync(() => PointerCheckStatus.Text.StartsWith("Move the pointer", StringComparison.Ordinal),
            "The physical pointer did not start the Input lifecycle action.", TimeSpan.FromSeconds(2));
        await input.MoveAsync(DisabledPointerHost);
        await AwaitAsync(() => PointerCheckStatus.Text.StartsWith("Pointer lifecycle passed", StringComparison.Ordinal) ||
            PointerCheckStatus.Text.StartsWith("Pointer check failed", StringComparison.Ordinal),
            "The Input pointer lifecycle action did not finish.", TimeSpan.FromSeconds(90));
        var source = Path.Combine(Path.GetTempPath(), "orchard-input-pointer-result.json");
        if (!File.Exists(source) || File.GetLastWriteTimeUtc(source) < started)
            throw new InvalidOperationException("Input pointer lifecycle did not write a fresh result.");
        var destination = Path.Combine(folder, "input-pointer-foreground-result.json");
        File.Copy(source, destination);
        using var result = JsonDocument.Parse(await File.ReadAllTextAsync(destination));
        if (!result.RootElement.GetProperty("passed").GetBoolean())
            throw new InvalidOperationException("Input pointer lifecycle failed: " + result.RootElement.GetProperty("error").GetString());

        static async Task AwaitAsync(Func<bool> condition, string message, TimeSpan limit)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (!condition() && watch.Elapsed < limit) await Task.Delay(16);
            if (!condition()) throw new InvalidOperationException(message);
        }
    }
}
