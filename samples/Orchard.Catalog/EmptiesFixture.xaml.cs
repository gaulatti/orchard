using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;
public sealed partial class EmptiesFixture : UserControl
{
    public EmptiesFixture() => InitializeComponent();
    internal async Task VerifyAsync()
    {
        Require(DefaultEmpty.ActualWidth == 360 && DefaultEmpty.ActualHeight == 230, "Empty default surface geometry changed.");
        foreach (var empty in new[] { DefaultEmpty, ActionEmpty, CustomEmpty })
        {
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(empty);
            Require(!empty.IsTabStop && peer.GetAutomationControlType() == AutomationControlType.Group && peer.GetPattern(PatternInterface.Invoke) is null, "Empty surface gained its own command or Tab stop.");
        }
        var heading = Descendants(DefaultEmpty).OfType<TextBlock>().Single(text => text.Text == "No items");
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(heading).GetHeadingLevel() == AutomationHeadingLevel.Level3, "Empty title lost its heading level.");
        Require(ActionEmpty.Action is Button && CustomEmpty.Icon is Border, "Empty replaced consumer content.");
        await MainWindow.AwaitPresentationAsync(this);
    }
    private static IEnumerable<Microsoft.UI.Xaml.DependencyObject> Descendants(Microsoft.UI.Xaml.DependencyObject root)
    {
        for (var index=0;index<VisualTreeHelper.GetChildrenCount(root);index++)
        {
            var child=VisualTreeHelper.GetChild(root,index);yield return child;
            foreach(var nested in Descendants(child))yield return nested;
        }
    }
    private static void Require(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
}
