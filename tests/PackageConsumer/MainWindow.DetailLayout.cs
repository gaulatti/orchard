using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Orchard;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private void VerifyPackageDetailLayout()
    {
        Require(ReferenceEquals(PackageDetailLayout.Content, PackageDetailMain) && ReferenceEquals(PackageDetailLayout.SideRail, PackageDetailRail) &&
            PackageDetailMain.Text == "Independent main" && PackageDetailRail.Text == "Independent rail" && !PackageDetailLayout.IsTabStop,
            "Packaged DetailLayout lost consumer content, input values or passive behavior.");
        PackageDetailLayout.Side = BleeckerDetailLayoutSide.Start;
        PackageDetailLayout.Ratio = BleeckerDetailLayoutRatio.Balanced;
        PackageDetailLayout.Sticky = true;
        Root.UpdateLayout();
        var rail = BrandDescendants<ContentControl>(PackageDetailLayout).Single(node => node.Name == "DetailSideRail");
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(rail);
        Require(peer.GetName() == "Consumer related details" && peer.GetItemType() == "complementary" &&
            peer.GetLandmarkType() == AutomationLandmarkType.Custom && !rail.IsTabStop,
            "Packaged DetailLayout lost its named complementary group.");
        var template = PackageDetailLayout.Template;
        PackageDetailLayout.Template = null; Root.UpdateLayout();
        PackageDetailLayout.Template = template; Root.UpdateLayout();
        Require(ReferenceEquals(PackageDetailLayout.Content, PackageDetailMain) && ReferenceEquals(PackageDetailLayout.SideRail, PackageDetailRail) &&
            PackageDetailMain.Text == "Independent main" && PackageDetailRail.Text == "Independent rail",
            "Packaged DetailLayout template replacement lost consumer inputs.");
        PackageDetailLayout.SideRail = null; Root.UpdateLayout();
        Require(BrandDescendants<ContentControl>(PackageDetailLayout).Single(node => node.Name == "DetailSideRail").Visibility == Visibility.Collapsed,
            "Packaged DetailLayout retained an empty rail.");
        PackageDetailLayout.SideRail = PackageDetailRail;
        var dataTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding}'/></DataTemplate>");
        PackageDetailLayout.ContentTemplate = dataTemplate; PackageDetailLayout.SideRailTemplate = dataTemplate;
        PackageDetailLayout.Content = 42; PackageDetailLayout.SideRail = false; Root.UpdateLayout();
        var presenters = BrandDescendants<ContentPresenter>(PackageDetailLayout).ToArray();
        Require(presenters.Any(node => node.Content is int number && number == 42) && presenters.Any(node => node.Content is bool flag && !flag),
            "Packaged DetailLayout converted numeric/Boolean models before its consumer templates received them.");
        PackageDetailLayout.ContentTemplate = null; PackageDetailLayout.SideRailTemplate = null;
        PackageDetailLayout.Content = PackageDetailMain; PackageDetailLayout.SideRail = PackageDetailRail;
        PackageDetailLayout.Side = BleeckerDetailLayoutSide.End;
        PackageDetailLayout.Ratio = BleeckerDetailLayoutRatio.Primary;
        PackageDetailLayout.Sticky = false; Root.UpdateLayout();
    }
}
