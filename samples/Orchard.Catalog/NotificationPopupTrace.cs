using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

// Catalog evidence only: observe native popup lifetime without changing it.
internal sealed class NotificationPopupTrace : IDisposable
{
    private readonly List<object> events = [];
    private readonly List<Action> unsubscribe = [];
    private readonly Stopwatch watch = Stopwatch.StartNew();
    private readonly string path;

    internal NotificationPopupTrace(XamlRoot root, NativeInputSession input, string folder,
        BleeckerColorScheme scheme, params (string Id, BleeckerNotificationBadge Owner)[] popups)
    {
        path = Path.Combine(folder, $"notifications-{scheme}-lifecycle.json");
        foreach (var (id, owner) in popups)
        {
            var popup = (Flyout)((BleeckerIconButton)owner.FindName("Trigger")).Flyout;
            void Record(string phase)
            {
                var ancestry = new List<string>();
                for (DependencyObject? element = popup.Content; element is not null; element = VisualTreeHelper.GetParent(element))
                    ancestry.Add(element.GetType().Name);
                events.Add(new { id, phase, milliseconds = watch.Elapsed.TotalMilliseconds,
                    popup.IsOpen, owner.IsLoaded, owner.Count, unread = owner.Items.Count(item => item.IsUnread),
                    contentLoaded = (popup.Content as FrameworkElement)?.IsLoaded,
                    focused = FocusManager.GetFocusedElement(root)?.GetType().Name,
                    foreground = input.OwnsForeground, foreignForeground = input.HasForeignForeground, ancestry });
            }
            void Opening(object? sender, object args) => Record("opening");
            void Opened(object? sender, object args) => Record("opened");
            void Closing(object? sender, FlyoutBaseClosingEventArgs args) => Record("closing");
            void Closed(object? sender, object args) => Record("closed");
            void Loaded(object sender, RoutedEventArgs args) => Record("owner-loaded");
            void Unloaded(object sender, RoutedEventArgs args) => Record("owner-unloaded");
            popup.Opening += Opening; popup.Opened += Opened; popup.Closing += Closing; popup.Closed += Closed;
            owner.Loaded += Loaded; owner.Unloaded += Unloaded;
            unsubscribe.Add(() =>
            {
                popup.Opening -= Opening; popup.Opened -= Opened; popup.Closing -= Closing; popup.Closed -= Closed;
                owner.Loaded -= Loaded; owner.Unloaded -= Unloaded;
            });
            Record("observed");
        }
    }
    public void Dispose()
    {
        foreach (var remove in unsubscribe) remove();
        File.WriteAllText(path, JsonSerializer.Serialize(events));
    }
}
