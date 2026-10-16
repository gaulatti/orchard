using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class LoadingOverlaysFixture : UserControl
{
    public LoadingOverlaysFixture() => InitializeComponent();
    internal async Task VerifyCatalogAsync(string folder, BleeckerColorScheme scheme)
    {
        using var input = new NativeInputSession(XamlRoot, folder, "loading-overlay-catalog-" + scheme); await input.AcquireAsync(DemoLabel);
        Require(DemoLabel.Focus(FocusState.Keyboard), "Loading overlay demo could not focus its original editor.");
        DemoOverlay.Label = DemoLabel.Text; DemoOverlay.IsVisible = true;
        try
        {
            await MainWindow.AwaitPresentationAsync(DemoOverlay);
            var portal = (FrameworkElement)VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Single(p => p.Child is FrameworkElement child && child.Name == "PART_Portal").Child;
            var origin = portal.TransformToVisual(XamlRoot.Content).TransformPoint(new Point());
            await File.WriteAllTextAsync(Path.Combine(folder, $"loading-overlay-preview-{scheme}.json"), System.Text.Json.JsonSerializer.Serialize(new
            {
                origin, portal = new { portal.ActualWidth, portal.ActualHeight }, viewport = XamlRoot.Size,
                owner = DemoOverlay.TransformToVisual(XamlRoot.Content).TransformPoint(new Point()),
                ownerSize = new { DemoOverlay.ActualWidth, DemoOverlay.ActualHeight }
            }));
            Require(Math.Abs(origin.X) < .05 && Math.Abs(origin.Y) < .05 && Math.Abs(portal.ActualWidth - XamlRoot.Size.Width) < .05 && Math.Abs(portal.ActualHeight - XamlRoot.Size.Height) < .05,
                $"Loading overlay demo portal does not cover its window: origin={origin.X},{origin.Y}; portal={portal.ActualWidth},{portal.ActualHeight}; root={XamlRoot.Size.Width},{XamlRoot.Size.Height}.");
            Require(ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), DemoLabel), "Loading overlay demo stole editor focus.");
            await input.KeyAsync(0x27, DemoLabel);
            await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"loading-overlay-preview-{scheme}.png"));
        }
        finally { DemoOverlay.IsVisible = false; }
        await MainWindow.AwaitPresentationAsync(DemoOverlay);
        Require(!VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Any(p => p.Child is FrameworkElement child && child.Name == "PART_Portal"), "Loading overlay demo did not close.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private async void ShowOverlay(object sender, RoutedEventArgs args)
    {
        DemoOverlay.Label = DemoLabel.Text; DemoOverlay.IsVisible = true; DemoStatus.Text = "The caller will hide the overlay after 3 seconds.";
        try { await Task.Delay(TimeSpan.FromSeconds(3)); }
        finally { DemoOverlay.IsVisible = false; DemoStatus.Text = "The overlay is hidden."; }
    }
}
