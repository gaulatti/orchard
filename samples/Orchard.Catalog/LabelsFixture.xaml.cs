using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class LabelsFixture : UserControl
{
    public LabelsFixture() => InitializeComponent();

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
        await Task.Delay(150);
        UpdateLayout();
        var root = (Grid)VisualTreeHelper.GetChild(EmailLabel, 0);
        var text = (TextBlock)root.FindName("PART_Text");
        Require(text.FontFamily.Source.Contains("EncodeSans-Medium.ttf") && text.FontSize == 13 && text.FontWeight.Weight == 500 &&
            text.CharacterSpacing == 0 && text.LineHeight == 13 && EmailLabel.ActualHeight == 13, $"Label typography or line box differs: height={EmailLabel.ActualHeight}.");
        var color = ((SolidColorBrush)text.Foreground).Color;
        Require($"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}" == BleeckerTokens.Colors(scheme)["textPrimary"], "Label uses the wrong theme color.");
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(EmailLabel);
        Require(peer.GetName() == "Email Address" && peer.GetAutomationControlType() == AutomationControlType.Text && !EmailLabel.IsTabStop,
            "Label lost its name, text role or non-tabbable contract.");
        Require(ReferenceEquals(AutomationProperties.GetLabeledBy(EmailInput), EmailLabel), "Label did not associate with its input.");
        EmailLabel.Content = "Updated email";
        Require(peer.GetName() == "Updated email", "Label automation name did not follow consumer content.");
        EmailLabel.Content = "Email Address";
        var disabledRoot = (Grid)VisualTreeHelper.GetChild(DisabledLabel, 0);
        Require(Math.Abs(disabledRoot.Opacity - .6) < .000001 && LongLabel.ActualHeight > 13 && LongLabel.ActualWidth == 140,
            $"Disabled or wrapped label geometry differs: opacity={disabledRoot.Opacity}, width={LongLabel.ActualWidth}, height={LongLabel.ActualHeight}.");
        EmailLabel.Target = null;
        Require(!ReferenceEquals(AutomationProperties.GetLabeledBy(EmailInput), EmailLabel), "Detached label retained its input relationship.");
        EmailLabel.Target = EmailInput;
        AutomationProperties.SetName(EmailInput, "Consumer override");
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(EmailInput).GetName() == "Consumer override", "Label replaced an explicit consumer accessibility name.");
        EmailInput.ClearValue(AutomationProperties.NameProperty);
        await MainWindow.CaptureElementAsync(this, System.IO.Path.Combine(folder, $"labels-{scheme}.png"));
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
