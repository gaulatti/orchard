using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Orchard;

namespace PackageConsumer;
public sealed partial class MainWindow
{
    private async Task VerifyPackagePanelLayoutAsync()
    {
        Root.UpdateLayout();
        Require(BleeckerPanelLayout.GetGrow(PackageAttachedGrowth) && PackageAttachedGrowth.ActualWidth == 380, "Packaged XAML attached growth metadata did not allocate its track.");
        var growthProducer = new TextBlock { Tag = false };
        PackageAttachedGrowth.SetBinding(BleeckerPanelLayout.GrowProperty, new Binding { Source = growthProducer, Path = new PropertyPath("Tag"), Mode = BindingMode.OneWay }); Root.UpdateLayout();
        Require(!BleeckerPanelLayout.GetGrow(PackageAttachedGrowth) && PackageAttachedGrowth.ActualWidth == 0, "Packaged attached growth binding did not remove flexible allocation.");
        growthProducer.Tag = true; Root.UpdateLayout(); Require(PackageAttachedGrowth.ActualWidth == 380 && PackageAttachedGrowth.GetBindingExpression(BleeckerPanelLayout.GrowProperty) is not null, "Packaged attached growth binding was replaced or not observed.");
        Require(PackagePanelFixed.ActualWidth == 200 && PackagePanelColumn.ActualWidth == 400 && PackagePanelTop.ActualHeight == 100 && PackagePanelBottom.ActualHeight == 260,
            "Packaged panel layout did not arrange fixed/growing rows and columns.");
        PackageLayoutInput.Text = "Edited package layout"; PackagePanelFixed.ClearValue(FrameworkElement.WidthProperty); PackagePanelFixed.Grow = true; Root.UpdateLayout();
        Require(PackagePanelFixed.ActualWidth == 300 && PackagePanelColumn.ActualWidth == 300 && ReferenceEquals(PackagePanelFixed.Content, PackageLayoutInput), "Packaged panel layout lost its child or live growth.");
        PackagePanelLayout.Children.Move(1, 0); Root.UpdateLayout();
        var actions = 0; RoutedEventHandler clicked = (_, _) => actions++; PackageLayoutAction.Click += clicked;
        var button = BrandDescendants<Button>(PackageLayoutAction).Single(); ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke)).Invoke();
        await AwaitAsync(() => actions == 1, "Packaged panel layout did not retain its caller action."); PackageLayoutAction.Click -= clicked;
        var template = PackagePanelLayout.Template; PackagePanelLayout.Template = null; Root.UpdateLayout(); PackagePanelLayout.Template = template; Root.UpdateLayout();
        Require(PackageLayoutInput.Text == "Edited package layout" && ReferenceEquals(PackagePanelBottom.Content, PackageLayoutAction), "Packaged panel layout template replacement lost caller state.");
        PackagePanelLayout.Children.Move(0, 1); PackagePanelFixed.Grow = false; PackagePanelFixed.Width = 200; PackageLayoutInput.Text = "Independent layout"; Root.UpdateLayout();
    }
}
