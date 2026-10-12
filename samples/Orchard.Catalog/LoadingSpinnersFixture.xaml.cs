using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard.Catalog;

public sealed partial class LoadingSpinnersFixture : UserControl
{
    public LoadingSpinnersFixture() => InitializeComponent();
    internal bool AnimationsEnabled { get; private set; }

    internal async Task VerifyAsync(BleeckerColorScheme scheme)
    {
        foreach (var spinner in Sizes.Children.OfType<BleeckerLoadingSpinner>())
        {
            var diameter = spinner.Size switch { BleeckerLoadingSize.Small => 16, BleeckerLoadingSize.Medium => 32, _ => 48 };
            Require(spinner.ActualWidth == diameter && spinner.ActualHeight == diameter, "Spinner dimensions differ from Bleecker.");
            var canvas = (Canvas)VisualTreeHelper.GetChild(spinner, 0);
            var arc = (Microsoft.UI.Xaml.Shapes.Path)canvas.Children.Single();
            Require(arc.StrokeThickness == (diameter == 48 ? 3 : 2) && arc.StrokeStartLineCap == PenLineCap.Flat && arc.StrokeEndLineCap == PenLineCap.Flat, "Spinner border differs from Bleecker.");
            var segment = (ArcSegment)((PathGeometry)arc.Data).Figures.Single().Segments.Single();
            Require(segment.IsLargeArc && segment.SweepDirection == SweepDirection.Clockwise && Math.Abs(segment.Size.Width - (diameter - arc.StrokeThickness) / 2) < 0.001, "Spinner lost its three-quarter ring geometry.");
            var brush = (SolidColorBrush)arc.Stroke;
            Require($"#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}" == BleeckerTokens.Colors(scheme)["primary"], "Spinner does not use its theme color.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(spinner);
            Require(peer.GetAutomationControlType() == AutomationControlType.ProgressBar && !string.IsNullOrEmpty(peer.GetName()) && !spinner.IsTabStop, "Spinner accessibility contract failed.");
        }
        var rotation = (RotateTransform)((Canvas)VisualTreeHelper.GetChild(DynamicSpinner, 0)).RenderTransform;
        AnimationsEnabled = new UISettings().AnimationsEnabled;
        var angle = rotation.Angle;
        await Task.Delay(180);
        Require(AnimationsEnabled ? Math.Abs(rotation.Angle - angle) > 1 : rotation.Angle == 0, "Spinner motion does not respect Windows preferences.");
        DynamicSpinner.Visibility = Visibility.Collapsed;
        await Task.Delay(60);
        Require(rotation.Angle == 0, "Hidden spinner did not stop and reset.");
        DynamicSpinner.Visibility = Visibility.Visible;
        Sizes.Children.Remove(DynamicSpinner);
        await Task.Delay(60);
        Require(rotation.Angle == 0, "Unloaded spinner did not stop.");
        Sizes.Children.Insert(1, DynamicSpinner);
        DynamicSpinner.Size = BleeckerLoadingSize.Large;
        await Task.Delay(100);
        Require(DynamicSpinner.ActualWidth == 48, "Bound spinner size did not update layout.");
        DynamicSpinner.Size = BleeckerLoadingSize.Medium;
        await Task.Delay(100);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
