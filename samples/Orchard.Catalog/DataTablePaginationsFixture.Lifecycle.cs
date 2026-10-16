using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class DataTablePaginationsFixture
{
    private async Task VerifyLifecycleAsync(BleeckerColorScheme scheme, string folder)
    {
        var root = (Grid)XamlRoot.Content;
        var a = new PaginationTable(); var b = new PaginationTable(8, pageSize: 2);
        var original = new TrackedPageSizes([10, 20, 30]); var replacement = new TrackedPageSizes([2, 4]);
        var bar = new BleeckerDataTablePagination { Table = a, PageSizeOptions = original, Width = 800, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var host = new Grid { Margin = new Thickness(16), Background = root.Background }; Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count); host.Children.Add(bar); root.Children.Add(host);
        try
        {
            await ReadyAsync(() => bar.IsLoaded); bar.ApplyTemplate(); await MainWindow.AwaitPresentationAsync(bar);
            ItemsControl Size() => Chooser(bar);
            string Page() => Descendants<TextBlock>(bar).Single(item => item.Name == "PART_Page").Text;
            Require(a.Subscribers == 1 && original.Subscribers == 1, "Mounted pagination did not observe its original sources exactly once.");
            var defaults = new BleeckerDataTablePagination(); var otherDefaults = new BleeckerDataTablePagination();
            Require(!ReferenceEquals(defaults.PageSizeOptions, otherDefaults.PageSizeOptions), "Page-size defaults leaked across control instances.");
            original.Add(40); await MainWindow.AwaitPresentationAsync(bar); Require(Size().Items.Count == 4, "Live original options did not reach the selector.");
            bar.Table = b; bar.PageSizeOptions = replacement; await MainWindow.AwaitPresentationAsync(bar);
            Require(a.Subscribers == 0 && b.Subscribers == 1 && original.Subscribers == 0 && replacement.Subscribers == 1, "Replacement retained stale table/options observers.");
            a.SetPagination(3, 10); original.Clear(); original.Add(999); await MainWindow.AwaitPresentationAsync(bar);
            Require(Page() == "Page 1 of 4" && Size().Items.Count == 2 && (int)SelectedOption(Size())!.Tag == 2, "Stale sources changed the active view.");
            replacement.Add(8); b.SetPagination(1, 4); await MainWindow.AwaitPresentationAsync(bar);
            Require(Page() == "Page 2 of 2" && Size().Items.Count == 3 && (int)SelectedOption(Size())!.Tag == 4, "Replacement updates did not reach the original view.");
            bar.IsEnabled = false; await MainWindow.AwaitPresentationAsync(bar);
            Require(Descendants<Button>(bar).All(button => !button.IsEnabled) && !Size().IsEnabled, "Disabled pagination retained an enabled action.");
            var writes = b.Requests.Count; var disabledRejected = false;
            try { SelectSize(Size(),8); } catch (ElementNotEnabledException) { disabledRejected = true; }
            Require(disabledRejected,"Disabled page-size automation accepted a selection.");
            Require(b.Requests.Count == writes, "Disabled selector forwarded a consumer mutation.");
            bar.IsEnabled = true; b.SetPagination(0, 2); await MainWindow.AwaitPresentationAsync(bar);
            var oldSize = Size(); var template = bar.Template; bar.Template = null; b.SetPagination(1, 4); bar.Template = template; bar.ApplyTemplate(); await MainWindow.AwaitPresentationAsync(bar);
            Require(!ReferenceEquals(oldSize, Size()) && Page() == "Page 2 of 2" && b.Subscribers == 1 && replacement.Subscribers == 1, "Template replacement lost current state or duplicated observers.");
            writes = b.Requests.Count; var detachedRejected = false;
            try { SelectSize(oldSize,8); } catch (ElementNotAvailableException) { detachedRejected = true; }
            Require(detachedRejected,"Detached page-size automation accepted a selection.");
            Require(b.Requests.Count == writes, "Detached template selector retained a mutation handler.");
            Expand(Size(), true); await ReadyAsync(() => ChooserIsOpen(Size()));
            string DisplayedSize() { var value = Descendants<ContentPresenter>(Size()).Single(item => item.Name == "ContentPresenter").Content; return value is TextBlock text ? text.Text : value?.ToString() ?? ""; }
            var beforeExternal = b.Requests.Count; b.SetPagination(0, 8); await MainWindow.AwaitPresentationAsync(bar);
            Require(ChooserIsOpen(Size()) && DisplayedSize() == "8" && b.Requests.Count == beforeExternal, "An external page size did not update the open chooser without a consumer request.");
            replacement.Remove(8); await MainWindow.AwaitPresentationAsync(bar);
            Require(ChooserIsOpen(Size()) && DisplayedSize() == "" && b.PageSize == 8 && b.Requests.Count == beforeExternal, $"An unlisted controlled size did not clear the open display without changing the producer: open={ChooserIsOpen(Size())}, display={DisplayedSize()}, size={b.PageSize}, requests={b.Requests.Count}/{beforeExternal}, items={string.Join(',', Size().Items.OfType<Button>().Select(item => item.Tag))}, selected={SelectedOption(Size())?.Tag}.");
            replacement.Add(8); b.SetPagination(1, 4); await MainWindow.AwaitPresentationAsync(bar);
            var removedSize = Size(); host.Children.Remove(bar); await ReadyAsync(() => !bar.IsLoaded && b.Subscribers == 0 && replacement.Subscribers == 0);
            Require(!ChooserIsOpen(removedSize), "Unloaded pagination retained an open selector popup.");
            b.SetPagination(0, 4); replacement.Add(16); host.Children.Add(bar); await ReadyAsync(() => bar.IsLoaded); await MainWindow.AwaitPresentationAsync(bar);
            Require(Page() == "Page 1 of 2" && Size().Items.Count == 4 && b.Subscribers == 1 && replacement.Subscribers == 1, "Remount did not reconnect to current consumer state exactly once.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(bar);
            Require(peer.GetAutomationControlType() == AutomationControlType.Group, "Remounted pagination lost its native group role.");
            Require(AutomationProperties.GetName(Size()) == "Rows per page" && FrameworkElementAutomationPeer.CreatePeerForElement(Size()).GetAutomationControlType() == AutomationControlType.ComboBox, "Page size lost its native role/name.");
            foreach (var (name, caption) in new[] { ("PART_First", "Go to first page"), ("PART_Previous", "Go to previous page"), ("PART_Next", "Go to next page"), ("PART_Last", "Go to last page") })
                Require(AutomationProperties.GetName(Descendants<Button>(bar).Single(item => item.Name == name)) == caption, "Navigation lost its accessible name.");
        }
        finally { root.Children.Remove(host); }
        await ReadyAsync(() => !bar.IsLoaded && a.Subscribers == 0 && b.Subscribers == 0 && original.Subscribers == 0 && replacement.Subscribers == 0);
        Require(a.Subscribers == 0 && b.Subscribers == 0 && original.Subscribers == 0 && replacement.Subscribers == 0, "Final removal retained source observers.");
        await File.WriteAllTextAsync(Path.Combine(folder, $"data-table-pagination-lifecycle-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, perInstanceDefaults = true, tableReplacement = true, optionReplacement = true, staleSourcesDetached = true, disabledMutationSuppressed = true, templateReplacement = true, oldTemplateDetached = true, openExternalSize = true, openUnlistedSize = true, openProducerRequests = 0, popupClosedOnUnload = true, reparent = true, nativeRolesAndNames = true, finalObservers = 0 }));
    }

    private sealed class TrackedPageSizes(IEnumerable<int> values) : ObservableCollection<int>(values)
    {
        internal int Subscribers { get; private set; }
        public override event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add { base.CollectionChanged += value; Subscribers++; }
            remove { base.CollectionChanged -= value; Subscribers--; }
        }
    }
}
