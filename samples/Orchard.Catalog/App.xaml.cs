using Microsoft.UI.Xaml;

namespace Orchard.Catalog;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        UnhandledException += (_, args) => ReportSmokeFailure(args.Exception);
        try { InitializeComponent(); }
        catch (Exception error) { ReportSmokeFailure(error); throw; }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception error) { ReportSmokeFailure(error); throw; }
    }

    private static void ReportSmokeFailure(Exception error)
    {
        var arguments = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(arguments, "--smoke-output");
        if (index < 0 || index + 1 >= arguments.Length) return;
        var folder = Path.GetFullPath(arguments[index + 1]);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "result.json"), System.Text.Json.JsonSerializer.Serialize(
            new { passed = false, stage = "startup", error = error.ToString() }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        Environment.Exit(1);
    }
}
