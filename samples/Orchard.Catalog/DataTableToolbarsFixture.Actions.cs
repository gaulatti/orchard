using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Orchard.Catalog;

public sealed partial class DataTableToolbarsFixture
{
    private async Task VerifyActionsAsync(NativeInputSession input, BleeckerColorScheme scheme, string folder)
    {
        await input.ClickAsync(Filter);
        await AwaitAsync(() => table.VisibleText == "Harbor|Vale" && Results.Text == table.VisibleText,
            "Original filter action did not update its consumer rows.");
        await input.ClickAsync(Action);
        Require(Results.Text == "Consumer action invoked; Harbor|Vale", "Original trailing action was not invoked.");
        var search = Descendants<BleeckerInput>(Preview).Single();
        var reset = Descendants<Button>(Preview).Single(button => button.Name == "PART_Reset");
        await FocusAsync(search);
        var transfers = new List<object>();
        foreach (var (from, to) in new (Control, Control)[] { (search, Filter), (Filter, reset), (reset, Action), (Action, Outside) })
        {
            var downs = 0; var ups = 0; var downOwned = true; var upOwned = true;
            var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var down = new KeyEventHandler((_, args) =>
            {
                if (args.Key != Windows.System.VirtualKey.Tab) return;
                downs++; downOwned &= ReferenceEquals(args.OriginalSource, from);
            });
            var up = new KeyEventHandler((_, args) =>
            {
                if (args.Key != Windows.System.VirtualKey.Tab) return;
                ups++; upOwned &= ReferenceEquals(args.OriginalSource, to); released.TrySetResult();
            });
            XamlRoot.Content.AddHandler(UIElement.KeyDownEvent, down, true);
            XamlRoot.Content.AddHandler(UIElement.KeyUpEvent, up, true);
            try
            {
                input.Key(0x09);
                await released.Task.WaitAsync(TimeSpan.FromSeconds(2));
                Require(downs == 1 && ups == 1 && downOwned && upOwned && ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), to),
                    "Toolbar Tab did not retain its exact press/release focus transfer.");
            }
            finally
            {
                XamlRoot.Content.RemoveHandler(UIElement.KeyDownEvent, down);
                XamlRoot.Content.RemoveHandler(UIElement.KeyUpEvent, up);
                transfers.Add(new { from = from.Name, to = to.Name, downs, ups, downOwned, upOwned });
                await File.WriteAllTextAsync(Path.Combine(folder, $"data-table-toolbar-tab-{scheme}.json"), JsonSerializer.Serialize(transfers));
            }
        }
        Preview.IsEnabled = false;
        await MainWindow.AwaitPresentationAsync(Preview);
        Require(!search.IsEnabled && !reset.IsEnabled && !Filter.IsEnabled && !Action.IsEnabled &&
            !FrameworkElementAutomationPeer.CreatePeerForElement(search).IsEnabled() &&
            !FrameworkElementAutomationPeer.CreatePeerForElement(reset).IsEnabled(), "Toolbar disabled state did not reach its original controls and peers.");
        table.GetColumn("title")!.SetFilterValue("Vale");
        await AwaitAsync(() => search.Text == "Vale" && table.VisibleText == "Vale", "Disabled toolbar lost external consumer state.");
        Preview.IsEnabled = true;
        table.SetFilters([]);
        await MainWindow.AwaitPresentationAsync(Preview);
        Require(search.IsEnabled && Filter.IsEnabled && Action.IsEnabled && table.VisibleRows.Count == 3,
            "Toolbar did not restore enabled controls and consumer rows.");
        await File.WriteAllTextAsync(Path.Combine(folder, $"data-table-toolbar-actions-{scheme}.json"),
            JsonSerializer.Serialize(new { passed = true, originalFilter = true, originalAction = true, disabledInheritance = true, externalDisabledUpdate = true, transfers = transfers.Count }));
    }
}
