using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Orchard.Catalog;

public sealed partial class FieldHelpFixture : UserControl
{
    public FieldHelpFixture() => InitializeComponent();

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
        await Task.Delay(150);
        Require(Help.ActualWidth == 16 && Help.ActualHeight == 16 && EmptyHelp.Visibility == Visibility.Collapsed, "Field help trigger or empty-text geometry differs.");
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(Help);
        Require(peer.GetAutomationControlType() == AutomationControlType.Button && peer.GetName() == Help.Text, "Field help lost its button role or help-text name.");
        var root = (Grid)VisualTreeHelper.GetChild(Help, 0);
        var glyph = (Viewbox)root.FindName("InfoGlyph");
        var circle = (Ellipse)root.FindName("InfoCircle");
        Require(glyph.ActualWidth == 14 && glyph.ActualHeight == 14 && circle.Width == 22 && circle.StrokeThickness == 2, "Lucide info geometry differs.");
        foreach (var state in new[] { "Normal", "PointerOver", "Pressed", "Disabled" })
        {
            Require(VisualStateManager.GoToState(Help, state, false), $"Missing field help state {state}.");
            var expected = BleeckerTokens.Colors(scheme)[state is "PointerOver" or "Pressed" ? "textPrimary" : "textSecondary"];
            var color = ((SolidColorBrush)circle.Stroke).Color;
            Require($"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}" == expected, "Field help state uses an incorrect icon color.");
            var scale = (ScaleTransform)root.RenderTransform;
            var expectedScale = state == "Pressed" ? .9 : 1;
            Require(Math.Abs(scale.ScaleX - expectedScale) < .000001 && Math.Abs(scale.ScaleY - expectedScale) < .000001,
                $"Field help scale differs: {state}/{scale.ScaleX}/{scale.ScaleY}.");
            await MainWindow.CaptureElementAsync(Help, System.IO.Path.Combine(folder, $"field-help-{state}-{scheme}.png"));
        }
        VisualStateManager.GoToState(Help, "Normal", false);
        Require(Help.Focus(FocusState.Keyboard), "Field help did not accept keyboard focus.");
        await Task.Delay(180);
        var tooltip = BleeckerTooltipService.GetToolTip(Help)!;
        Require(tooltip.IsOpen && tooltip.Placement == PlacementMode.Bottom && Equals(tooltip.Content, Help.Text), "Field help did not open bottom help on keyboard focus.");
        Require(((Ellipse)root.FindName("HelpFocus")).Opacity == 1, "Field help keyboard focus ring is absent.");
        await MainWindow.CaptureElementAsync(tooltip, System.IO.Path.Combine(folder, $"field-help-content-{scheme}.png"));
        var activations = 0;
        RoutedEventHandler onClick = (_, _) => activations++;
        Help.Click += onClick;
        try
        {
            ((Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();
            await Task.Delay(50);
            Require(!tooltip.IsOpen && activations == 1 && ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Help),
                "Field help activation did not dismiss while preserving consumer activation and focus.");
        }
        finally { Help.Click -= onClick; }
        tooltip.IsOpen = true;
        var original = Help.Text;
        Help.Text = "Updated help";
        Require(Equals(tooltip.Content, Help.Text) && peer.GetName() == Help.Text, "Changed help did not reach content and accessibility.");
        Help.Text = " \t";
        Require(Help.Visibility == Visibility.Collapsed && !tooltip.IsOpen, "Empty help left its trigger or popup active.");
        Help.Text = original;
        Require(Help.Visibility == Visibility.Visible, "Restored help did not restore its trigger.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
