using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class DataTableColumnTogglesFixture
{
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        var root = (Grid)XamlRoot.Content;
        var a = new ColumnVisibilityTable(); var b = new ColumnVisibilityTable(second: true);
        var rows = a.Rows.ToArray(); var columns = a.Columns.ToArray();
        var disabled = false;
        var view = new BleeckerDataTableColumnToggle { Table = a, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
        var host = new Grid { Width = 520, Margin = new Thickness(32), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Background = root.Background };
        host.Children.Add(view); Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count); root.Children.Add(host);
        Popup Popup() => VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Single(item => item.Child is FrameworkElement { Name: "PART_Dialog" });
        async Task OpenAsync()
        {
            Provider(Trigger(view)).Expand(); await ReadyAsync(() => IsOpen(view) && Descendants<BleeckerCheckbox>(Popup().Child).Any());
            await MainWindow.AwaitPresentationAsync((FrameworkElement)Popup().Child);
        }
        static bool Unavailable(Action action)
        {
            try { action(); return false; } catch (ElementNotAvailableException) { return true; }
        }
        try
        {
            using var input = new NativeInputSession(XamlRoot, folder, $"column-toggle-lifecycle-{scheme}");
            await ReadyAsync(() => view.IsLoaded && a.Subscribers == 1); view.ApplyTemplate(); await MainWindow.AwaitPresentationAsync(view);
            var originalTemplate = view.Template; var oldTrigger = Trigger(view); var oldProvider = Provider(oldTrigger);
            await OpenAsync();
            var first = Descendants<BleeckerCheckbox>(Popup().Child).First();
            ((IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(first).GetPattern(PatternInterface.Toggle)).Toggle();
            Require(a.Requests.Count == 1 && ReferenceEquals(a.Requests[0].Column, columns[1]) && !columns[1].IsVisible, "Native column action lost its original consumer boundary.");
            Require(a.Rows.Zip(rows).All(pair => ReferenceEquals(pair.First, pair.Second)) && a.Columns.Zip(columns).All(pair => ReferenceEquals(pair.First, pair.Second)), "Native picker replaced original consumer records.");
            await MainWindow.AwaitPresentationAsync((FrameworkElement)Popup().Child);
            await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"data-table-column-toggle-{scheme}.png"));
            view.IsEnabled = false; await ReadyAsync(() => oldProvider.ExpandCollapseState == ExpandCollapseState.Collapsed);
            try { oldProvider.Expand(); } catch (ElementNotEnabledException) { disabled = true; }
            Require(disabled && a.Requests.Count == 1, "Disabled column picker accepted its native action."); view.IsEnabled = true;
            view.Table = b; await ReadyAsync(() => a.Subscribers == 0 && b.Subscribers == 1);
            var unchanged = b.VisibleCells; a.ExternalVisibility(new Dictionary<string, bool> { ["title"] = false });
            Require(b.VisibleCells == unchanged && b.Requests.Count == 0, "Stale source changed the replacement consumer.");
            await OpenAsync(); view.Template = null; await ReadyAsync(() => !VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Any(item => item.Child is FrameworkElement { Name: "PART_Dialog" }));
            Require(Unavailable(oldProvider.Expand), "Detached template trigger retained its native expansion path.");
            view.Template = originalTemplate; view.ApplyTemplate(); await MainWindow.AwaitPresentationAsync(view); await OpenAsync();
            Require(!ReferenceEquals(Trigger(view), oldTrigger) && b.Requests.Count == 0, "Template replacement reused a stale native trigger or emitted a visibility request.");
            root.Children.Remove(host); await ReadyAsync(() => !view.IsLoaded && b.Subscribers == 0);
            Require(!VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Any(item => item.Child is FrameworkElement { Name: "PART_Dialog" }), "Unmounted column picker retained an open popup.");
            root.Children.Add(host); await ReadyAsync(() => view.IsLoaded && b.Subscribers == 1); await OpenAsync();
            Provider(Trigger(view)).Collapse(); await ReadyAsync(() => !IsOpen(view));
        }
        finally { root.Children.Remove(host); }
        await ReadyAsync(() => !view.IsLoaded && a.Subscribers == 0 && b.Subscribers == 0);
        await File.WriteAllTextAsync(Path.Combine(folder, $"data-table-column-toggle-lifecycle-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, originalColumns = true, originalRows = true, disabledRejected = disabled, staleTemplateRejected = true, templateReplaced = true, remounted = true, visibilityRequests = a.Requests.Count, replacementRequests = b.Requests.Count, finalObservers = a.Subscribers + b.Subscribers }));
    }
}
