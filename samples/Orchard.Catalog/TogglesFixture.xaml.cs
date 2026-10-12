using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class TogglesFixture : UserControl
{
    public TogglesFixture()
    {
        InitializeComponent();
        DefaultToggle.Checked += OnChanged;
        DefaultToggle.Unchecked += OnChanged;
    }

    private void OnChanged(object sender, RoutedEventArgs e) =>
        ToggleResult.Text = $"Follow updates: {(DefaultToggle.IsChecked == true ? "on" : "off")}";

    internal async Task VerifyAsync(BleeckerColorScheme scheme)
    {
        DefaultToggle.IsChecked = false;
        var peer = new ToggleButtonAutomationPeer(DefaultToggle);
        var provider = (IToggleProvider)peer.GetPattern(PatternInterface.Toggle);
        Require(provider.ToggleState == ToggleState.Off, "Toggle automation did not expose Off.");
        provider.Toggle();
        await Task.Delay(100);
        Require(DefaultToggle.IsChecked == true && provider.ToggleState == ToggleState.On && ToggleResult.Text == "Follow updates: on",
            "Native toggle did not propagate selection to the consumer.");
        provider.Toggle();
        await Task.Delay(100);
        Require(DefaultToggle.IsChecked == false && ToggleResult.Text == "Follow updates: off", "Native toggle did not deselect.");
        Require(!DisabledToggle.IsEnabled, "Disabled toggle was enabled.");

        foreach (var toggle in new[] { DefaultToggle, OutlineToggle })
        {
            toggle.ApplyTemplate();
            var presenter = VisualTreeHelper.GetChild(toggle, 0) as ContentPresenter
                ?? throw new InvalidOperationException("Native toggle template lost its presenter.");
            foreach (var state in new[] { "Normal", "PointerOver", "Pressed", "Disabled", "Checked", "CheckedPointerOver", "CheckedPressed", "CheckedDisabled" })
            {
                Require(VisualStateManager.GoToState(toggle, state, false), $"Missing toggle state {state}.");
                await Task.Delay(120);
                var selected = state.StartsWith("Checked", StringComparison.Ordinal);
                var expected = BleeckerTokens.Colors(scheme)[selected ? "primary" : state is "PointerOver" or "Pressed" ? "textPrimary" : "textSecondary"];
                Require(presenter.Foreground is SolidColorBrush brush &&
                    $"#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}" == expected,
                    $"{scheme} {state}: toggle retained a system foreground instead of {expected}.");
                Require(presenter.Background is SolidColorBrush background && background.Opacity < 0.3,
                    $"{scheme} {state}: toggle lost its transparent/tinted surface.");
            }
            VisualStateManager.GoToState(toggle, toggle.IsChecked == true ? "Checked" : "Normal", false);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
