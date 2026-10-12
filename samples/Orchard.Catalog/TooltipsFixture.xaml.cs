using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Orchard.Catalog;

public sealed partial class TooltipsFixture : UserControl
{
    public TooltipsFixture() => InitializeComponent();

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        StartBringIntoView();
        await Task.Delay(150);
        foreach (var tip in new[] { TopTip, BottomTip, LeftTip, RightTip, LongTip })
        {
            tip.IsOpen = true;
            await Task.Delay(180);
            tip.UpdateLayout();
            try
            {
                Require(tip.IsOpen, "Native tooltip did not open.");
                var surface = Descendants<BleeckerOverlaySurface>(tip).Single();
                var arrow = Descendants<Polygon>(tip).Single();
                var text = Descendants<TextBlock>(surface).First();
                Require(surface.ActualWidth <= 224 && surface.Padding == new Thickness(12, 8, 12, 8)
                    && surface.CornerRadius == new CornerRadius(6) && surface.BorderThickness == new Thickness(0), "Tooltip body geometry differs from reference.");
                Require(text.FontSize == 12 && text.FontFamily.Source.Contains("Encode Sans") && text.LineHeight == 19.5
                    && text.FontWeight.Weight == 400, "Tooltip typography differs from reference.");
                var vertical = tip.Placement is PlacementMode.Top or PlacementMode.Bottom;
                Require(arrow.ActualWidth == (vertical ? 10 : 5) && arrow.ActualHeight == (vertical ? 5 : 10), "Tooltip arrow geometry differs from reference.");
                var colors = BleeckerTokens.Colors(scheme);
                var expectedBackground = colors[scheme == BleeckerColorScheme.Dark ? "sand" : "deepSea"];
                var expectedForeground = scheme == BleeckerColorScheme.Dark ? colors["textPrimary"] : "#FFFFFFFF";
                Require(ColorHex((SolidColorBrush)surface.Background).Equals(expectedBackground, StringComparison.OrdinalIgnoreCase)
                    && ColorHex((SolidColorBrush)text.Foreground).Equals(expectedForeground, StringComparison.OrdinalIgnoreCase), "Tooltip palette differs from reference.");
                Require(((SolidColorBrush)arrow.Fill).Color == ((SolidColorBrush)surface.Background).Color, "Tooltip arrow lost its surface color.");
                Require(!tip.IsTabStop && FrameworkElementAutomationPeer.CreatePeerForElement(tip).GetAutomationControlType() == AutomationControlType.ToolTip,
                    "Tooltip lost its native role or became a keyboard stop.");
                Require(!text.IsTextTrimmed && (tip != LongTip || text.ActualHeight > 19.5), "Long tooltip text did not wrap.");
                await MainWindow.CaptureElementAsync(tip, System.IO.Path.Combine(folder, $"tooltip-{tip.Name}-{scheme.ToString().ToLowerInvariant()}.png"));
                if (tip == LongTip)
                {
                    var original = tip.Content;
                    var longHeight = surface.ActualHeight;
                    tip.Content = "Short update";
                    await Task.Delay(80);
                    Require(surface.ActualHeight < longHeight, "Tooltip did not remeasure a live content change.");
                    tip.Content = original;
                    await Task.Delay(80);
                    Require(surface.ActualHeight == longHeight, "Tooltip did not restore its wrapped content height.");
                }
            }
            finally { tip.IsOpen = false; }
            await Task.Delay(80);
        }
    }

    private static string ColorHex(SolidColorBrush brush) => $"#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
