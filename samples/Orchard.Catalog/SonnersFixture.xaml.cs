using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class SonnersFixture : UserControl
{
    private string? latest;
    private readonly ICommand originalCommand;
    public SonnersFixture()
    {
        originalCommand = new ActionCommand(parameter => Result.Text = "Original fictional action: " + parameter + "; the notification remains mounted.");
        InitializeComponent();
    }
    private void PositionChanged(object sender, SelectionChangedEventArgs args)
    { if (Host is not null && sender is ComboBox { SelectedIndex: >= 0 } position) Host.Position = (BleeckerSonnerPosition)position.SelectedIndex; }
    private void PersistentChanged(object sender, RoutedEventArgs args)
    { if (Host is not null) Host.Duration = ((CheckBox)sender).IsChecked == true ? 0 : 4000; }
    private void ShowVariant(object sender, RoutedEventArgs args)
    {
        var variant = Enum.Parse<BleeckerSonnerVariant>((string)((FrameworkElement)sender).Tag);
        latest = Host.Show(new BleeckerSonnerToast("Fictional " + variant + " notification") { Variant = variant, Description = "Caller-owned local content." });
    }
    private void ShowAction(object sender, RoutedEventArgs args)
    {
        var variant = Enum.Parse<BleeckerButtonVariant>((string)((FrameworkElement)sender).Tag);
        var action = new BleeckerSonnerAction("Continue", originalCommand, "original " + variant + " action", variant);
        latest = Host.Show(new BleeckerSonnerToast("Ready to continue") { Description = "This action preserves its original command and parameter.", Duration = 0, Action = action });
    }
    private void ShowLong(object sender, RoutedEventArgs args) => latest = Host.Show(new BleeckerSonnerToast("A deliberately long fictional notification — Straße 東京 é العربية") { Description = "Fictional 日本語 descrição — שלום. This description wraps and retains its complete original content.", Duration = 0 });
    private void ShowStack(object sender, RoutedEventArgs args)
    { for (var index = 0; index < 12; index++) latest = Host.Show(new BleeckerSonnerToast("Original fictional item " + index) { Duration = 0 }); }
    private void DismissLatest(object sender, RoutedEventArgs args) { if (latest is not null) Host.Dismiss(latest); }
    private void ShowGlobal(object sender, RoutedEventArgs args)
    {
        var options = new BleeckerSonnerOptions() { Description = "Fictional imperative API examples.", Duration = 0 };
        BleeckerToast.Show(new BleeckerSonnerToast("Global default") { Duration = 0 }); BleeckerToast.Success("Global success", options); BleeckerToast.Error("Global error", options);
        BleeckerToast.Warning("Global warning", options); latest = BleeckerToast.Info("Global info", options); BleeckerToast.Dismiss("fictional-nonexistent-id");
    }
    private sealed class ActionCommand(Action<object?> execute) : ICommand
    { public event EventHandler? CanExecuteChanged { add { } remove { } } public bool CanExecute(object? parameter) => true; public void Execute(object? parameter) => execute(parameter); }
}
