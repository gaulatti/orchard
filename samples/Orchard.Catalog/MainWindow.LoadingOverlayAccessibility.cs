using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunLoadingOverlayAccessibilityAsync(string folder)
    {
        var original = Content; var page = new Page(); var host = new StackPanel { Spacing = 16, Margin = new Thickness(20) }; page.Content = host;
        var action = new Button { Name = "OriginalAction", Content = "Original focus", Width = 140, Height = 32, HorizontalAlignment = HorizontalAlignment.Left };
        var caption = new TextBlock { Text = "Linked operation" }; var overlay = new BleeckerLoadingOverlay();
        host.Children.Add(action); host.Children.Add(caption); host.Children.Add(overlay);
        var reports = new List<object>(); var phase = "mount"; var passed = false; NativeInputSession? input = null;
        try
        {
            Content = page; await AwaitPresentationAsync(page); input = new NativeInputSession(page.XamlRoot, folder, "loading-overlay-accessibility"); await input.AcquireAsync(action);
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                page.RequestedTheme = theme; Require(action.Focus(FocusState.Keyboard), "Could not focus original accessibility action.");
                overlay.Label = "Loading..."; overlay.ClearValue(AutomationProperties.NameProperty); AutomationProperties.SetLabeledBy(overlay, null);
                overlay.IsVisible = true; await SettleAsync(); await CheckAsync("default", "Loading...");
                overlay.Label = "Preparing visible content"; await SettleAsync(); await CheckAsync("live-label", overlay.Label);
                AutomationProperties.SetName(overlay, "Preparing independent content"); await SettleAsync(); await CheckAsync("explicit-name", "Preparing independent content");
                overlay.Label = null; await SettleAsync(); await CheckAsync("hidden-label", "Preparing independent content");
                var nameSource = new TextBlock { Text = "Préparer le dossier 日本語" };
                overlay.SetBinding(AutomationProperties.NameProperty, new Binding { Source = nameSource, Path = new PropertyPath(nameof(TextBlock.Text)), Mode = BindingMode.OneWay });
                await SettleAsync(); await CheckAsync("bound-name", nameSource.Text);
                nameSource.Text = "Live consumer operation"; await SettleAsync(); await CheckAsync("live-bound-name", nameSource.Text);
                await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                overlay.ClearValue(AutomationProperties.NameProperty); AutomationProperties.SetLabeledBy(overlay, caption); await SettleAsync(); await CheckAsync("linked-caption", caption.Text);
                caption.Text = "Updated linked operation"; await SettleAsync(); await CheckAsync("live-linked-caption", caption.Text);
                AutomationProperties.SetName(overlay, "Explicit priority"); await SettleAsync(); await CheckAsync("name-priority", "Explicit priority");
                var template = overlay.Template; overlay.Template = null; overlay.ApplyTemplate(); await SettleAsync(); overlay.Template = template; overlay.ApplyTemplate(); await SettleAsync();
                await CheckAsync("template-replacement", "Explicit priority");
                overlay.IsVisible = false; await SettleAsync(); await ProbeAsync("hidden-" + theme, "", 0);
                reports.Add(new { id = "hidden", scheme = theme.ToString(), progressPeers = 0 });
                async Task CheckAsync(string id, string expected)
                {
                    phase = id; var portal = Portal(); var spinner = Descendants<BleeckerLoadingSpinner>(portal).Single(); var peer = FrameworkElementAutomationPeer.CreatePeerForElement(spinner);
                    var actual = peer.GetName(); var role = peer.GetAutomationControlType().ToString(); var focusPreserved = ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), action);
                    reports.Add(new { id, scheme = theme.ToString(), expected, actual, role, spinner.IsTabStop, focusPreserved, hasRangeValue = peer.GetPattern(PatternInterface.RangeValue) is not null });
                    await SaveAsync(); Require(actual == expected && role == "ProgressBar" && !spinner.IsTabStop && focusPreserved && peer.GetPattern(PatternInterface.RangeValue) is null, "Loading overlay accessibility differs: " + id + $" name='{actual}', expected='{expected}'.");
                    await ProbeAsync(id + "-" + theme, expected, 1);
                }
            }
            passed = true;
        }
        finally { overlay.IsVisible = false; input?.Dispose(); Content = original; await AwaitPresentationAsync(CatalogRoot); await SaveAsync(); }
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "loading-overlay-accessibility", environment = RuntimeEnvironment() }));
        FrameworkElement Portal() => (FrameworkElement)VisualTreeHelper.GetOpenPopupsForXamlRoot(page.XamlRoot).Single(p => p.Child is FrameworkElement child && child.Name == "PART_Portal").Child;
        Task SettleAsync() { page.UpdateLayout(); return AwaitPresentationAsync(page); }
        Task SaveAsync() => File.WriteAllTextAsync(Path.Combine(folder, "loading-overlay-accessibility.json"), JsonSerializer.Serialize(new { passed, phase, reports, originalContentRestored = ReferenceEquals(Content, original) }, new JsonSerializerOptions { WriteIndented = true }));
        async Task ProbeAsync(string id, string expected, int count)
        {
            phase = id; var output = Path.Combine(folder, "uia-" + id + ".json");
            var executable = Path.Combine(folder, "LoadingOverlayUiaProbe.exe");
            if (!File.Exists(executable))
            {
                var framework = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET/Framework64/v4.0.30319");
                var compile = new ProcessStartInfo(Path.Combine(framework, "csc.exe")) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (var argument in new[] { "/nologo", "/target:exe", "/out:" + executable, "/reference:" + Path.Combine(framework, "WPF/UIAutomationClient.dll"), "/reference:" + Path.Combine(framework, "WPF/UIAutomationTypes.dll"), "/reference:" + Path.Combine(framework, "WPF/WindowsBase.dll"), "/reference:" + Path.Combine(framework, "System.Runtime.Serialization.dll"), Path.Combine(AppContext.BaseDirectory, "LoadingOverlayUiaProbe.cs") }) compile.ArgumentList.Add(argument);
                using var compiler = Process.Start(compile) ?? throw new InvalidOperationException("Could not start the UIA client compiler.");
                var messages = compiler.StandardOutput.ReadToEndAsync(); var errors = compiler.StandardError.ReadToEndAsync(); await compiler.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Require(compiler.ExitCode == 0, "UIA client compilation failed: " + await messages + await errors);
            }
            var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var argument in new[] { input!.Window.ToInt64().ToString(), Environment.ProcessId.ToString(), output }) start.ArgumentList.Add(argument);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the owned UIA probe.");
            var stderr = process.StandardError.ReadToEndAsync(); await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Require(process.ExitCode == 0, "External UIA probe failed: " + await stderr);
            using var tree = JsonDocument.Parse(await File.ReadAllTextAsync(output)); var peers = tree.RootElement.GetProperty("progress");
            Require(peers.GetArrayLength() == count, "External UIA progress count differs.");
            if (count == 1) Require(peers[0].GetProperty("name").GetString() == expected && !peers[0].GetProperty("isOffscreen").GetBoolean() && !peers[0].GetProperty("isKeyboardFocusable").GetBoolean() && !peers[0].GetProperty("hasRangeValuePattern").GetBoolean(), "External UIA did not expose the named indeterminate progress indicator.");
        }
        static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            { var child = VisualTreeHelper.GetChild(parent, index); if (child is T match) yield return match; foreach (var nested in Descendants<T>(child)) yield return nested; }
        }
    }
}
