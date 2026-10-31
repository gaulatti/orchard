using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Text.Json;
namespace Orchard.Catalog;
public sealed partial class AppShellsFixture:UserControl
{
    public AppShellsFixture(){InitializeComponent();Suite.Options=[new("monitor","Local monitoring"),new("editorial","Local editorial")];}
    private void SelectSuite(object? sender,BleeckerSelectRequestedEventArgs args){Suite.Value=args.Value;Selection.Text=args.Value=="monitor"?"Fictional monitoring suite":"Fictional editorial suite";}
    private void ClearNote(object sender,RoutedEventArgs args)=>Note.Text="";

    internal async Task VerifyInputAsync(BleeckerColorScheme scheme, string folder)
    {
        using var input = new NativeInputSession(XamlRoot, folder, $"app-shell-{scheme}");
        var root = CatalogFixtureRoot.From(XamlRoot);
        var host = new Grid { Margin = new Thickness(16), Background = root.Background };
        Grid.SetRowSpan(host, root.RowDefinitions.Count);
        Grid.SetColumnSpan(host, root.ColumnDefinitions.Count);
        root.Children.Add(host);
        var header = new Button { Content = "Fictional header action", HorizontalAlignment = HorizontalAlignment.Left };
        var editor = new TextBox { Text = "original", Width = 280, HorizontalAlignment = HorizontalAlignment.Left };
        var action = new Button { Content = "Fictional body action", HorizontalAlignment = HorizontalAlignment.Left };
        var headerClicks = 0;
        var bodyClicks = 0;
        header.Click += (_, _) => headerClicks++;
        action.Click += (_, _) => bodyClicks++;
        var body = new StackPanel { Spacing = 16 };
        body.Children.Add(editor);
        body.Children.Add(action);
        var shell = new BleeckerAppShell { Header = header, Content = body, Footer = "Fictional local footer" };
        host.Children.Add(shell);
        try
        {
            await AwaitAsync(() => shell.IsLoaded && header.IsLoaded && action.IsLoaded, "App Shell did not mount.");
            UpdateLayout();
            await input.AcquireAsync(header);
            Require(header.Focus(FocusState.Keyboard), "App Shell header action could not take focus.");
            input.Key(0x09);
            await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), editor), "Tab did not reach the caller editor.");
            input.Key(0x09);
            await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), action), "Tab did not reach the caller body action.");
            input.Key(0x20);
            await AwaitAsync(() => bodyClicks == 1, "Space did not invoke the caller body action.");
            await input.ClickAsync(action);
            await AwaitAsync(() => bodyClicks == 2, "Pointer did not invoke the caller body action.");
            await input.ClickAsync(header);
            await AwaitAsync(() => headerClicks == 1, "Pointer did not invoke the caller header action.");
            shell.OffsetHeader = false;
            UpdateLayout();
            Require(ReferenceEquals(shell.Header, header) && ReferenceEquals(shell.Content, body) && editor.Text == "original", "Layout change replaced caller controls.");
            await File.WriteAllTextAsync(Path.Combine(folder, $"app-shell-input-{scheme}.json"), JsonSerializer.Serialize(new
            {
                passed = true, theme = scheme.ToString(), bodyKeyboardInvocations = 1,
                bodyPointerInvocations = 1, headerPointerInvocations = 1,
                callerControlsRetained = true, shellTabStop = shell.IsTabStop, hardwareInputSent = true
            }));
        }
        finally { root.Children.Remove(host); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task AwaitAsync(Func<bool> predicate, string message)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (predicate()) return;
            await Task.Delay(20);
        }
        throw new InvalidOperationException(message);
    }
}
