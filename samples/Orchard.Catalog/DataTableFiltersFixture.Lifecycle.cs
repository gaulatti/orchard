using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class DataTableFiltersFixture
{
    internal async Task VerifyLifecycleAsync(BleeckerColorScheme scheme, string folder)
    {
        var root = (Grid)XamlRoot.Content; var a = new SelectFilterTable(); var b = new SelectFilterTable(true);
        var options = new ObservableCollection<BleeckerSelectOption> { new("Draft", "Draft"), new("Published", "Published") };
        var view = new BleeckerDataTableSelectFilter { Table = a, ColumnId = "status", Options = options, Width = 520, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var host = new Grid { Margin = new Thickness(32), VerticalAlignment = VerticalAlignment.Top }; host.Children.Add(view); Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count); root.Children.Add(host);
        var originalRows = a.Rows.ToArray(); var disabled = false; var stale = false;
        string Display() => ((IValueProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Select(view)).GetPattern(PatternInterface.Value)).Value;
        ISelectionItemProvider Choice(string value) => (ISelectionItemProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Select(view).Items.OfType<Button>().Single(item => item.Tag is BleeckerSelectOption option && option.Value == value)).GetPattern(PatternInterface.SelectionItem);
        try
        {
            await ReadyAsync(() => view.IsLoaded && a.Subscribers == 1); view.ApplyTemplate(); await MainWindow.AwaitPresentationAsync(view);
            var originalColumn = a.GetColumn("status"); Choice("Draft").Select();
            await ReadyAsync(() => a.Requests.Count == 1 && a.VisibleText == "harbor:Harbor:Draft|vale:Vale:Draft" && Display() == "Draft");
            Require(ReferenceEquals(originalColumn, a.Requests[0].Column) && a.Rows.Zip(originalRows).All(pair => ReferenceEquals(pair.First, pair.Second)), "Native lifecycle replaced original column/rows.");
            a.SetExternalFilters([new("status", "Published")]); await ReadyAsync(() => Display() == "Published"); Require(a.Requests.Count == 1, "External filter created a visibility request.");
            Provider(Select(view)).Expand();
            await ReadyAsync(() => FocusManager.GetFocusedElement(XamlRoot) is Button button && ReferenceEquals(button.Tag, options[1]));
            var focusedPublished = (Button)FocusManager.GetFocusedElement(XamlRoot);
            options[1].Label = "Live Published"; await ReadyAsync(() => Display() == "Live Published" && ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), focusedPublished));
            var valueProvider = Choice("Published"); options[1].Value = "Review";
            await ReadyAsync(() => Display() == "" && FocusManager.GetFocusedElement(XamlRoot) is Control { Name: "PART_Listbox" });
            var staleValue = false; try { valueProvider.Select(); } catch (ElementNotAvailableException) { staleValue = true; }
            Require(staleValue && a.Requests.Count == 1, "Changing an original option value retained its stale provider or wrote to the table.");
            options[1].Value = "Published";
            await ReadyAsync(() => Display() == "Live Published" && FocusManager.GetFocusedElement(XamlRoot) is Control { Name: "PART_Listbox" });
            focusedPublished = Select(view).Items.OfType<Button>().Single(item => ReferenceEquals(item.Tag, options[1]));
            Require(focusedPublished.Focus(FocusState.Keyboard), "Live disabled-option regression lost its original focus owner."); options[1].IsDisabled = true;
            await ReadyAsync(() => FocusManager.GetFocusedElement(XamlRoot) is Control { Name: "PART_Listbox" });
            var disabledFocused = false; try { Choice("Published").Select(); } catch (ElementNotEnabledException) { disabledFocused = true; }
            Require(disabledFocused && a.Requests.Count == 1, "Disabling the focused original option retained an enabled mutation path.");
            options[1].IsDisabled = false; Provider(Select(view)).Collapse();
            await ReadyAsync(() => Provider(Select(view)).ExpandCollapseState == ExpandCollapseState.Collapsed);
            view.Placeholder = "Any status"; Require(FrameworkElementAutomationPeer.CreatePeerForElement(Select(view)).GetName() == "Any status", "Live filter placeholder lost its accessible name.");
            var oldChoice = Choice("Draft");
            var originalDraft = Select(view).Items.OfType<Button>().Single(item => ReferenceEquals(item.Tag, options[0]));
            var replacement = new ObservableCollection<BleeckerSelectOption> { new("Draft", "New Draft"), new("Published", "New Published") }; view.Options = replacement;
            await ReadyAsync(() => Display() == "New Published");
            Require(ReferenceEquals(originalDraft, Select(view).Items.OfType<Button>().Single(item => ReferenceEquals(item.Tag, replacement[0]))) && FrameworkElementAutomationPeer.CreatePeerForElement(originalDraft).GetName() == "New Draft", "Stable option keys lost their existing native identity or current metadata.");
            var removedDraft = replacement[0]; replacement.RemoveAt(0);
            try { oldChoice.Select(); } catch (ElementNotAvailableException) { stale = true; } Require(stale && a.Requests.Count == 1, "Removed original option key retained a mutation path.");
            replacement.Insert(0, removedDraft); await ReadyAsync(() => Select(view).Items.Count == 3);
            options[1].Label = "Old source notification"; Require(Display() == "New Published", "Old option metadata changed the replacement view.");
            replacement[0].IsDisabled = true; var blockedOption = false; try { Choice("Draft").Select(); } catch (ElementNotEnabledException) { blockedOption = true; } Require(blockedOption && a.Requests.Count == 1, "Disabled original option accepted a mutation.");
            view.IsEnabled = false; try { Provider(Select(view)).Expand(); } catch (ElementNotEnabledException) { disabled = true; } Require(disabled && a.Requests.Count == 1, "Disabled original filter accepted expansion."); view.IsEnabled = true;
            var oldSelect = Select(view); var oldExpand = Provider(oldSelect); var template = view.Template; oldExpand.Expand(); await ReadyAsync(() => oldExpand.ExpandCollapseState == ExpandCollapseState.Expanded); await MainWindow.AwaitPresentationAsync(view);
            view.Template = null; await ReadyAsync(() => oldExpand.ExpandCollapseState == ExpandCollapseState.Collapsed && !oldSelect.IsLoaded);
            var detached = false; try { oldExpand.Expand(); } catch (ElementNotAvailableException) { detached = true; } Require(detached, "Detached filter template retained its expansion path.");
            view.Template = template; view.ApplyTemplate(); await MainWindow.AwaitPresentationAsync(view); Require(!ReferenceEquals(oldSelect, Select(view)) && Display() == "New Published", "Filter template replacement lost current value.");
            view.Table = b; await ReadyAsync(() => a.Subscribers == 0 && b.Subscribers == 1 && Display() == "Any status");
            a.SetExternalFilters([new("status", "Draft")]); Require(Display() == "Any status" && b.Requests.Count == 0, "Old original table changed the replacement filter.");
            Choice("").Select(); Require(b.Requests.Count == 0, "Selecting current All created a table request.");
            root.Children.Remove(host); await ReadyAsync(() => !view.IsLoaded && b.Subscribers == 0);
            replacement[1].Label = "Changed while detached"; root.Children.Add(host); await ReadyAsync(() => view.IsLoaded && b.Subscribers == 1); await MainWindow.AwaitPresentationAsync(view);
            b.SetExternalFilters([new("status", "Published")]); await ReadyAsync(() => Display() == "Changed while detached");
            await File.WriteAllTextAsync(Path.Combine(folder, $"data-table-filters-lifecycle-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, originalRows = true, originalColumn = true, keyedNativeIdentityRetained = true, staleOptionRejected = stale, liveLabelRetainedFocus = true, liveValueInvalidatesProvider = staleValue, liveDisabledFocusedRejected = disabledFocused, disabledOptionRejected = blockedOption, disabledFilterRejected = disabled, detachedTemplateRejected = detached, templateReplaced = true, tableReplaced = true, remounted = true, originalRequests = a.Requests.Count, replacementRequests = b.Requests.Count }));
        }
        finally { root.Children.Remove(host); }
        await ReadyAsync(() => !view.IsLoaded && a.Subscribers == 0 && b.Subscribers == 0);
        await File.WriteAllTextAsync(Path.Combine(folder, $"data-table-filters-unload-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, finalObservers = a.Subscribers + b.Subscribers }));
    }
}
