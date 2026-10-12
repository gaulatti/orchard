using Microsoft.UI.Xaml;
using System.Text.Json;

namespace PackageConsumer;

public partial class App : Application
{
    private Window? window;

    public App()
    {
        UnhandledException += (_, args) => Report(false, args.Exception.ToString());
        try { InitializeComponent(); }
        catch (Exception error) { Report(false, error.ToString()); throw; }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try { window = new MainWindow(); window.Activate(); }
        catch (Exception error) { Report(false, error.ToString()); throw; }
    }

    internal static void Report(bool passed, string message)
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, "--verify-output");
        if (index < 0) return;
        var path = args[index + 1];
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new { passed, message,
            assembly = typeof(Orchard.BleeckerTokens).Assembly.Location,
            architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            osArchitecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
            osDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription },
            new JsonSerializerOptions { WriteIndented = true }));
        Environment.Exit(passed ? 0 : 1);
    }
}
