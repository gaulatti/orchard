using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Orchard;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private readonly Button timelineAction = new() { Content = "Edit", Width = 24, Height = 24, MinWidth = 0, MinHeight = 0, Padding = new Thickness() };
    private int timelineActions;
    private void InitializeTimeline()
    {
        timelineAction.Click += (_, _) => timelineActions++;
        PackageTimeline.Items = [new("created", "Record created") { Timestamp = "09:15", Description = "Consumer-owned history", Icon = timelineAction }, new("review", "Editorial review")];
    }
    private async Task VerifyTimelineAsync()
    {
        PackageTimeline.ApplyTemplate(); Root.UpdateLayout();
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(PackageTimeline).GetAutomationControlType() == AutomationControlType.List, "Packaged Timeline lost list semantics.");
        var rows = BrandDescendants<Panel>(PackageTimeline).Where(row => row.Name == "TimelineRow").ToArray();
        Require(rows.Length == 2 && AutomationProperties.GetItemStatus(rows[0]) == "Active" && AutomationProperties.GetItemStatus(rows[1]) == "Pending", "Packaged Timeline default statuses differ.");
        await AcquireConsumerInputAsync(); Require(timelineAction.Focus(FocusState.Keyboard), "Could not focus original packaged Timeline icon.");
        var count = timelineActions;
        await PaginationConsumerKeyAsync(0x20, timelineAction, timelineAction, () => Require(timelineActions == count, "Packaged Timeline invoked before Space release."));
        Require(timelineActions == count + 1 && ReferenceEquals(BrandDescendants<Button>(PackageTimeline).Single(), timelineAction), "Packaged Timeline lost its consumer action.");
        var first = PackageTimeline.Items[0]; first.Title = "Updated history"; PackageTimeline.Items.Move(0, 1); Root.UpdateLayout();
        Require(ReferenceEquals(BrandDescendants<Panel>(PackageTimeline).Last(row => row.Name == "TimelineRow"), rows[0]) && AutomationProperties.GetItemStatus(rows[0]) == "Pending" &&
            FrameworkElementAutomationPeer.CreatePeerForElement(rows[0]).GetName().Contains("Updated history"), "Packaged Timeline lost keyed metadata or ordinal status.");
        PackageTimeline.Items.Move(1, 0); first.Title = "Record created";
    }
}
