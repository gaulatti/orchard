using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Orchard;
namespace PackageConsumer;
public sealed partial class MainWindow
{
    private async Task VerifyPackageFeedGridAsync()
    {
        Root.UpdateLayout(); Require(PackageFeedInput.ActualWidth == 340 && PackageFeedAction.ActualWidth == 340, "Packaged FeedGrid did not retain its two source tracks.");
        PackageFeedInput.Text = "Edited independent grid"; PackageFeedGrid.Width = 723; Root.UpdateLayout(); Require(PackageFeedInput.ActualWidth == 691 && ReferenceEquals(PackageFeedGrid.Children[0], PackageFeedInput), "Packaged FeedGrid did not observe its width boundary.");
        var producer = new TextBlock { Width = 180 }; PackageFeedGrid.SetBinding(BleeckerFeedGrid.MinColumnWidthProperty, new Binding { Source = producer, Path = new PropertyPath("Width"), Mode = BindingMode.OneWay }); producer.Width = 200; Root.UpdateLayout(); Require(PackageFeedGrid.MinColumnWidth == 200 && PackageFeedInput.ActualWidth < 340 && PackageFeedGrid.GetBindingExpression(BleeckerFeedGrid.MinColumnWidthProperty) is not null, "Packaged FeedGrid lost its minimum binding.");
        PackageFeedGrid.Children.Move(1, 0); Root.UpdateLayout(); PackageFeedGrid.Children.Move(1, 0); Root.UpdateLayout(); Require(PackageFeedInput.Text == "Edited independent grid", "Packaged FeedGrid replaced caller input during collection moves.");
        var actions = 0; RoutedEventHandler clicked = (_, _) => actions++; PackageFeedAction.Click += clicked; var button = BrandDescendants<Button>(PackageFeedAction).Single(); ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke)).Invoke(); await AwaitAsync(() => actions == 1, "Packaged FeedGrid lost its consumer action."); PackageFeedAction.Click -= clicked;
        PackageFeedGrid.ClearValue(BleeckerFeedGrid.MinColumnWidthProperty); PackageFeedGrid.Width = 724; PackageFeedInput.Text = "Independent feed grid"; Root.UpdateLayout();
    }
}
