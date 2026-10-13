using Microsoft.UI.Xaml;

namespace Orchard.Catalog;

public partial class App : Application
{
    private Window? _window;
    private static readonly List<string> ResourceErrors = [];
    private static readonly List<object> AvatarStartupErrors = [];

    public App()
    {
        UnhandledException += (_, args) => ReportSmokeFailure(args.Exception);
        if (Environment.GetCommandLineArgs().Contains("--smoke-output", StringComparer.Ordinal))
        {
            System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
            DebugSettings.IsXamlResourceReferenceTracingEnabled = true;
            DebugSettings.XamlResourceReferenceFailed += (_, args) => ResourceErrors.Add(args.Message);
            // XAML can replace a managed setter error with a generic parse
            // failure. Retain the original control/metadata error in smoke mode.
            AppDomain.CurrentDomain.FirstChanceException += (_, args) =>
            {
                var stack = args.Exception.StackTrace;
                if (AvatarStartupErrors.Count < 8 && stack?.Contains("BleeckerAvatar", StringComparison.Ordinal) == true)
                    AvatarStartupErrors.Add(new { type = args.Exception.GetType().FullName, args.Exception.Message, stack });
            };
        }
        try { InitializeComponent(); }
        catch (Exception error) { ReportSmokeFailure(error); throw; }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            foreach (var name in new[] { "Heading", "Body", "PrimaryButton", "SecondaryButton", "TextBox", "ComboBox" })
            {
                var key = $"Bleecker{name}Style";
                try { _ = FindResource(Resources, key); }
                catch (Exception error) { throw new InvalidOperationException($"Could not load Orchard resource {key}.", error); }
            }
            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception error) { ReportSmokeFailure(error); throw; }
    }

    private static object FindResource(ResourceDictionary dictionary, string key)
    {
        if (dictionary.TryGetValue(key, out var value)) return value;
        foreach (var merged in dictionary.MergedDictionaries.Reverse())
        {
            try { return FindResource(merged, key); }
            catch (KeyNotFoundException) { }
        }
        throw new KeyNotFoundException(key);
    }

    private static object DescribeResources(ResourceDictionary dictionary) => new
    {
        source = dictionary.Source?.ToString(),
        count = dictionary.Count,
        merged = dictionary.MergedDictionaries.Select(DescribeResources).ToArray()
    };

    private void ReportSmokeFailure(Exception error)
    {
        var arguments = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(arguments, "--smoke-output");
        if (index < 0 || index + 1 >= arguments.Length) return;
        var folder = Path.GetFullPath(arguments[index + 1]);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "result.json"), System.Text.Json.JsonSerializer.Serialize(
            new { passed = false, stage = "startup", error = error.ToString(), resourceErrors = ResourceErrors,
                avatarStartupErrors = AvatarStartupErrors, resources = DescribeResources(Resources) }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        Environment.Exit(1);
    }
}
