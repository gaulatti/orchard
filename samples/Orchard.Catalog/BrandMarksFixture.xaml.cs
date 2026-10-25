using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Orchard.Catalog;

public sealed partial class BrandMarksFixture : UserControl
{
    private SolidColorBrush? callerBrush;
    private bool seaTint;
    public BrandMarksFixture() => InitializeComponent();
    private void ResizePreview(object sender, RoutedEventArgs args)
    {
        Live.Size = Live.Size == 64 ? 96 : 64;
        Outcome.Text = $"Live mark: {Live.Size:0} DIPs";
    }
    private void TintPreview(object sender, RoutedEventArgs args)
    {
        callerBrush ??= new SolidColorBrush(((SolidColorBrush)Live.Foreground).Color);
        Live.Foreground = callerBrush;
        seaTint = !seaTint;
        var scheme = ActualTheme == ElementTheme.Dark ? BleeckerColorScheme.Dark : BleeckerColorScheme.Light;
        var hex = BleeckerTokens.Colors(scheme)[seaTint ? "sea" : "textPrimary"].TrimStart('#');
        callerBrush.Color = Color.FromArgb(Convert.ToByte(hex[..2],16),Convert.ToByte(hex[2..4],16),Convert.ToByte(hex[4..6],16),Convert.ToByte(hex[6..8],16));
        Outcome.Text = seaTint ? "Caller brush changed to the sea token" : "Caller brush changed to the primary text token";
    }
}
