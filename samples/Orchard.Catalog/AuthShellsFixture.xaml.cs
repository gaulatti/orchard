using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Text.Json;

namespace Orchard.Catalog;

public sealed partial class AuthShellsFixture : UserControl
{
    public AuthShellsFixture() => InitializeComponent();
    private void Change(object sender, RoutedEventArgs args)
    {
        if (Primary is null || Split is null || Start is null || Rtl is null || TextScale is null) return;
        Primary.Layout = Split.IsChecked == true ? BleeckerAuthShellLayout.Split : BleeckerAuthShellLayout.Centered;
        Primary.Side = Start.IsChecked == true ? BleeckerAuthShellSide.Start : BleeckerAuthShellSide.End;
        Primary.FlowDirection = Rtl.IsChecked == true ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        Primary.FontSize = TextScale.IsChecked == true ? 32 : 16;
    }
    private void LocalAction(object sender, RoutedEventArgs args) => Result.Text = "Local fictional action invoked; caller-owned form values retained";

    internal async Task VerifyInputAsync(BleeckerColorScheme scheme, string folder)
    {
        using var input = new NativeInputSession(XamlRoot, folder, $"auth-shell-{scheme}");
        var root = CatalogFixtureRoot.From(XamlRoot);
        var host = new Grid { Margin = new Thickness(16), Background = root.Background };
        Grid.SetRowSpan(host, root.RowDefinitions.Count);
        Grid.SetColumnSpan(host, root.ColumnDefinitions.Count);
        root.Children.Add(host);
        var entry = new TextBox { Header = "Fictional member", Text = "original", Width = 280, HorizontalAlignment = HorizontalAlignment.Left };
        var action = new Button { Content = "Try local action" };
        var clicks = 0;
        action.Click += (_, _) => clicks++;
        var body = new StackPanel { Spacing = 20, VerticalAlignment = VerticalAlignment.Center };
        body.Children.Add(entry);
        body.Children.Add(action);
        var shell = new BleeckerAuthShell { Brand = "Fictional Orchard member access", Footer = "Fictional local component demo", Content = body };
        host.Children.Add(shell);
        try
        {
            await AwaitAsync(() => shell.IsLoaded && action.IsLoaded && entry.IsLoaded, "Auth Shell did not mount.");
            UpdateLayout();
            await input.AcquireAsync(entry);
            Require(entry.Focus(FocusState.Keyboard), "Auth Shell entry could not take focus.");
            input.Key(0x09);
            await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), action), "Tab did not reach the caller action.");
            input.Key(0x20);
            await AwaitAsync(() => clicks == 1, "Space did not invoke the caller action.");
            await input.ClickAsync(action);
            await AwaitAsync(() => clicks == 2, "Pointer did not invoke the caller action.");
            shell.Layout = BleeckerAuthShellLayout.Split;
            shell.Aside = "Fictional collection note";
            UpdateLayout();
            Require(ReferenceEquals(shell.Content, body) && ReferenceEquals(body.Children[0], entry) && entry.Text == "original", "Layout change replaced caller content.");
            await File.WriteAllTextAsync(Path.Combine(folder, $"auth-shell-input-{scheme}.json"), JsonSerializer.Serialize(new
            {
                passed = true, theme = scheme.ToString(), keyboardSpaceInvocations = 1, pointerInvocations = 1,
                callerInputRetained = true, callerContentRetained = true, shellTabStop = shell.IsTabStop,
                hardwareInputSent = true
            }));
        }
        finally { root.Children.Remove(host); }
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
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
