using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class AlertDialogsFixture : StackPanel
{
    private int canceled, confirmed;
    private DispatcherQueueTimer? loading;
    public AlertDialogsFixture() { InitializeComponent(); Unloaded += (_, _) => { loading?.Stop(); loading = null; Default.IsOpen = Destructive.IsOpen = Wide.IsOpen = false; }; }
    private void OpenDefault(object sender, RoutedEventArgs args) { Default.ConfirmLoading = false; Default.IsOpen = true; }
    private void OpenDestructive(object sender, RoutedEventArgs args) => Destructive.IsOpen = true;
    private void OpenWide(object sender, RoutedEventArgs args) => Wide.IsOpen = true;
    private void OpenLoading(object sender, RoutedEventArgs args)
    {
        Default.ConfirmLoading = true; Default.IsOpen = true; loading?.Stop(); loading = DispatcherQueue.CreateTimer(); loading.IsRepeating = false; loading.Interval = TimeSpan.FromMilliseconds(1200);
        loading.Tick += (_, _) => { Default.ConfirmLoading = false; Status.Text = "Fictional loading finished; either action can now request a change."; loading = null; }; loading.Start();
    }
    private void Canceled(object sender, RoutedEventArgs args) { canceled++; ((BleeckerAlertDialog)sender).IsOpen = false; Status.Text = $"Cancel requests: {canceled}; confirm requests: {confirmed}"; }
    private void Confirmed(object sender, RoutedEventArgs args) { confirmed++; ((BleeckerAlertDialog)sender).IsOpen = false; Status.Text = $"Cancel requests: {canceled}; confirm requests: {confirmed}"; }
}
