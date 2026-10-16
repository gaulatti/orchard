using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Orchard;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private readonly Button dataListAction = new() { Content = "Consumer edit" };
    private int dataListActions;
    private void InitializeDataList()
    {
        dataListAction.Click += (_, _) => dataListActions++;
        PackageDataList.Items = [new("owner", "Owner", "Consumer"), new("action", "Action", dataListAction) { Description = "Consumer-owned action" }];
    }
    private async Task VerifyDataListAsync()
    {
        PackageDataList.ApplyTemplate(); Root.UpdateLayout();
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(PackageDataList).GetAutomationControlType() == AutomationControlType.List,
            "Packaged DataList lost native list semantics.");
        var rows = BrandDescendants<Panel>(PackageDataList).Where(row => row.Name == "DataListRow").ToArray();
        Require(rows.Length == 2 && rows.All(row => FrameworkElementAutomationPeer.CreatePeerForElement(row).GetAutomationControlType() == AutomationControlType.ListItem),
            "Packaged DataList lost rows or item semantics.");
        await AcquireConsumerInputAsync();
        Require(dataListAction.Focus(FocusState.Keyboard), "Could not focus packaged DataList content.");
        var count = dataListActions;
        await PaginationConsumerKeyAsync(0x20, dataListAction, dataListAction, () => Require(dataListActions == count, "Packaged DataList invoked before Space release."));
        Require(dataListActions == count + 1 && ReferenceEquals(BrandDescendants<Button>(PackageDataList).Single(), dataListAction),
            "Packaged DataList did not retain its consumer's native action.");
        var first = PackageDataList.Items[0]; first.Label = "Updated owner"; first.Value = 0; PackageDataList.Items.Move(0, 1); Root.UpdateLayout();
        Require(ReferenceEquals(BrandDescendants<Panel>(PackageDataList).Last(row => row.Name == "DataListRow"), rows[0]) &&
            FrameworkElementAutomationPeer.CreatePeerForElement(rows[0]).GetName().Contains("Updated owner"), "Packaged DataList lost keyed live updates.");
        PackageDataList.Items.Move(1, 0); first.Label = "Owner"; first.Value = "Consumer";
    }
}
