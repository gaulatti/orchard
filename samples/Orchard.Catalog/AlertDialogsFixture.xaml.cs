using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Text.Json;

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

    internal async Task VerifyInputAsync(BleeckerColorScheme scheme, string folder)
    {
        static T? Find<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            if (parent is T item && item.Name == name) return item;
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                if (Find<T>(VisualTreeHelper.GetChild(parent, index), name) is { } found) return found;
            }
            return null;
        }
        static async Task AwaitAsync(Func<bool> condition, string message)
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                if (condition()) return;
                await Task.Delay(20);
            }
            throw new InvalidOperationException(message);
        }
        static bool Within(DependencyObject? node, DependencyObject owner)
        {
            for (; node is not null; node = VisualTreeHelper.GetParent(node))
                if (ReferenceEquals(node, owner)) return true;
            return false;
        }
        using var input = new NativeInputSession(XamlRoot, folder, $"alert-dialog-{scheme}");
        await input.AcquireAsync(OpenDefaultButton);
        var beforeConfirm = confirmed;
        var beforeCancel = canceled;
        await input.ClickAsync(OpenDefaultButton);
        await AwaitAsync(() => Default.IsOpen, "Pointer did not open Alert Dialog.");
        FrameworkElement? surface = null;
        BleeckerButton? confirm = null;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            surface = Find<FrameworkElement>(XamlRoot.Content, "AlertDialogSurface");
            confirm = Find<BleeckerButton>(XamlRoot.Content, "AlertDialogConfirm");
            if (surface?.IsLoaded == true && confirm?.IsLoaded == true) break;
            await Task.Delay(20);
        }
        if (surface?.IsLoaded != true || confirm?.IsLoaded != true)
            throw new InvalidOperationException("Alert Dialog did not present its actions.");
        var reducedMotion = !new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
        if (reducedMotion)
        {
            await Task.Delay(32);
            if (Math.Abs(surface.Opacity - 1) > .000001)
                throw new InvalidOperationException("Reduced-motion Alert Dialog did not reach its open endpoint.");
        }
        await input.ClickAsync(confirm);
        await AwaitAsync(() => !Default.IsOpen && confirmed == beforeConfirm + 1,
            "Pointer confirmation did not reach the caller once.");
        await AwaitAsync(() => Within(FocusManager.GetFocusedElement(XamlRoot) as DependencyObject, OpenDefaultButton),
            "Alert Dialog did not return focus to its opener.");
        input.Key(0x20);
        await AwaitAsync(() => Default.IsOpen, "Space did not reopen Alert Dialog from its opener.");
        input.Key(0x1B);
        await AwaitAsync(() => !Default.IsOpen && canceled == beforeCancel + 1,
            "Escape did not request cancellation once.");
        await AwaitAsync(() => Within(FocusManager.GetFocusedElement(XamlRoot) as DependencyObject, OpenDefaultButton),
            "Escape did not restore opener focus.");
        await File.WriteAllTextAsync(Path.Combine(folder, $"alert-dialog-input-{scheme}.json"), JsonSerializer.Serialize(new
        {
            passed = true, theme = scheme.ToString(), pointerConfirmRequests = confirmed - beforeConfirm,
            escapeCancelRequests = canceled - beforeCancel, focusRestored = true,
            reducedMotion, hardwareInputSent = true
        }));
    }
}
