using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Text.Json;
namespace Orchard.Catalog;
public sealed partial class AdminShellsFixture : UserControl
{
    private int actions;
    private bool wide;
    public AdminShellsFixture() { InitializeComponent(); }
    private void OnFooterAction(object sender, RoutedEventArgs args) => Result.Text = $"Local footer action {++actions}";
    private void OnToggleWide(object sender, RoutedEventArgs args) { wide = !wide; BodyContent.Width = wide ? 1200 : double.NaN; }
    private void OnFarEdgeAction(object sender, RoutedEventArgs args) => Result.Text = $"Far edge action {++actions}";

    internal async Task VerifyInputAsync(BleeckerColorScheme scheme, string folder)
    {
        using var input = new NativeInputSession(XamlRoot, folder, $"admin-shell-{scheme}");
        var root = CatalogFixtureRoot.From(XamlRoot);
        var host = new Grid { Margin = new Thickness(16), Background = root.Background };
        Grid.SetRowSpan(host, root.RowDefinitions.Count);
        Grid.SetColumnSpan(host, root.ColumnDefinitions.Count);
        root.Children.Add(host);
        var header = new Button { Content = "Fictional header action", HorizontalAlignment = HorizontalAlignment.Left };
        var railEditor = new TextBox { Text = "original rail", Width = 150 };
        var railAction = new Button { Content = "Rail action" };
        var bodyEditor = new TextBox { Text = "original body", Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
        var bodyAction = new Button { Content = "Body action", HorizontalAlignment = HorizontalAlignment.Left };
        var footer = new Button { Content = "Footer action", HorizontalAlignment = HorizontalAlignment.Left };
        var railClicks = 0; var bodyClicks = 0; var headerClicks = 0; var footerClicks = 0;
        railAction.Click += (_, _) => railClicks++;
        bodyAction.Click += (_, _) => bodyClicks++;
        header.Click += (_, _) => headerClicks++;
        footer.Click += (_, _) => footerClicks++;
        var rail = new StackPanel { Width = 160, Spacing = 12 };
        rail.Children.Add(railEditor); rail.Children.Add(railAction);
        var body = new StackPanel { Spacing = 12 };
        body.Children.Add(bodyEditor); body.Children.Add(bodyAction);
        var shell = new BleeckerAdminShell { Height = 540, MinHeight = 0, Header = header, Sidebar = rail, Content = body, Footer = footer };
        host.Children.Add(shell);
        try
        {
            await AwaitAsync(() => shell.IsLoaded && footer.IsLoaded, "Admin Shell did not mount.");
            UpdateLayout();
            await input.AcquireAsync(header);
            Require(header.Focus(FocusState.Keyboard), "Admin Shell header could not take focus.");
            input.Key(0x09);
            await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), railEditor), "Tab did not reach the caller rail editor.");
            input.Key(0x09);
            await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), railAction), "Tab did not reach the caller rail action.");
            input.Key(0x20);
            await AwaitAsync(() => railClicks == 1, "Space did not invoke the caller rail action.");
            await input.ClickAsync(bodyAction);
            await AwaitAsync(() => bodyClicks == 1, "Pointer did not invoke the caller body action.");
            await input.ClickAsync(header);
            await AwaitAsync(() => headerClicks == 1, "Pointer did not invoke the caller header action.");
            await input.ClickAsync(footer);
            await AwaitAsync(() => footerClicks == 1, "Pointer did not invoke the caller footer action.");
            shell.ContentPadding = new Thickness(0);
            UpdateLayout();
            Require(ReferenceEquals(shell.Header, header) && ReferenceEquals(shell.Sidebar, rail) && ReferenceEquals(shell.Content, body) && ReferenceEquals(shell.Footer, footer) && railEditor.Text == "original rail" && bodyEditor.Text == "original body", "Admin Shell layout change replaced caller controls.");
            await File.WriteAllTextAsync(Path.Combine(folder, $"admin-shell-input-{scheme}.json"), JsonSerializer.Serialize(new
            {
                passed = true, theme = scheme.ToString(), railKeyboardInvocations = 1,
                bodyPointerInvocations = 1, headerPointerInvocations = 1, footerPointerInvocations = 1,
                callerControlsRetained = true, hardwareInputSent = true
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
