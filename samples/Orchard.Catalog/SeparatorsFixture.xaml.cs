using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class SeparatorsFixture : UserControl
{
    public SeparatorsFixture() => InitializeComponent();
    internal async Task VerifyAsync(BleeckerColorScheme scheme)
    {
        Require(Horizontal.ActualWidth == 320 && Horizontal.ActualHeight == 1, "Horizontal separator geometry differs from reference.");
        Require(Vertical.ActualWidth == 1 && Vertical.ActualHeight == 48, "Vertical separator geometry differs from reference.");
        foreach (var separator in new[] { Horizontal, Vertical, Semantic, Changing })
        {
            var brush = (SolidColorBrush)separator.Background;
            Require(brush.Color == (scheme == BleeckerColorScheme.Dark ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Black)
                && Math.Abs(brush.Opacity - (scheme == BleeckerColorScheme.Dark ? .1 : .05)) < .001, "Separator theme recipe mismatch.");
            Require(!separator.IsTabStop, "Separator became a keyboard stop.");
            Require(ReferenceEquals(((Border)VisualTreeHelper.GetChild(separator, 0)).Background, brush), "Separator template did not render its brush.");
        }
        var decorative = FrameworkElementAutomationPeer.CreatePeerForElement(Horizontal);
        var semantic = FrameworkElementAutomationPeer.CreatePeerForElement(Semantic);
        var vertical = FrameworkElementAutomationPeer.CreatePeerForElement(Vertical);
        Require(!decorative.IsControlElement() && !decorative.IsContentElement(), "Decorative separator exposed unnecessary semantics.");
        Require(semantic.IsControlElement() && semantic.GetAutomationControlType() == AutomationControlType.Separator
            && semantic.GetName() == "Workspace sections" && vertical.GetOrientation() == AutomationOrientation.Vertical, "Semantic separator lost its role, name or orientation.");
        Horizontal.Decorative = false;
        Require(decorative.IsControlElement(), "Live separator semantics did not update.");
        Horizontal.Decorative = true;
        Constrained.Width = 137;
        Changing.Orientation = Orientation.Vertical;
        await Task.Delay(80);
        Require(Horizontal.ActualWidth == 137 && Horizontal.ActualHeight == 1, "Constrained separator did not resize.");
        Require(Changing.ActualWidth == 1 && Changing.ActualHeight == 48, "Live vertical orientation did not remeasure.");
        Changing.Orientation = Orientation.Horizontal;
        Constrained.Width = 320;
        await Task.Delay(80);
        Require(Changing.ActualWidth == 240 && Changing.ActualHeight == 1, "Live horizontal orientation did not restore geometry.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
