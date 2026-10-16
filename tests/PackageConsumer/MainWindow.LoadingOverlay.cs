using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Orchard;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private async Task VerifyLoadingOverlayAsync()
    {
        await AcquireConsumerInputAsync(); Require(timelineAction.Focus(FocusState.Keyboard), "Could not focus the original action before the packaged loading portal.");
        PackageLoadingOverlay.IsVisible = true; Root.UpdateLayout();
        await AwaitAsync(() => VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot).Any(p => p.Child is FrameworkElement child && child.Name == "PART_Portal"), "Packaged loading portal did not open.");
        try
        {
            var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot).Single(p => p.Child is FrameworkElement child && child.Name == "PART_Portal");
            var portal = (FrameworkElement)popup.Child;
            Require(Math.Abs(portal.ActualWidth - Root.XamlRoot.Size.Width) < .05 && Math.Abs(portal.ActualHeight - Root.XamlRoot.Size.Height) < .05,
                "Packaged loading portal does not cover its window.");
            Require(ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(Root.XamlRoot), timelineAction), "Packaged loading portal stole opening focus.");
            var spinner = BrandDescendants<BleeckerLoadingSpinner>(portal).Single();
            Require(spinner.Size == BleeckerLoadingSize.Large && spinner.ActualWidth == 48 && spinner.ActualHeight == 48 &&
                FrameworkElementAutomationPeer.CreatePeerForElement(spinner).GetName() == "Preparing consumer data", "Packaged loading spinner lost its geometry or consumer label.");
            var count = timelineActions;
            await PaginationConsumerKeyAsync(0x20, timelineAction, timelineAction, () => Require(timelineActions == count, "Packaged underlying action fired before Space release."));
            Require(timelineActions == count + 1, "Packaged portal blocked the existing keyboard action.");
            PackageLoadingOverlay.Label = ""; Root.UpdateLayout();
            await AwaitAsync(() => BrandDescendants<BleeckerOverlaySurface>(portal).Single().ActualHeight == 98, "Packaged portal retained the omitted label gap.");
        }
        finally { PackageLoadingOverlay.IsVisible = false; PackageLoadingOverlay.Label = "Preparing consumer data"; Root.UpdateLayout(); }
        Require(!VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot).Any(p => p.Child is FrameworkElement child && child.Name == "PART_Portal"), "Packaged loading portal did not close.");
    }
}
