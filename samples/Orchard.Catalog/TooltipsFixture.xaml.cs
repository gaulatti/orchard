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
        await VerifyCollisionsAsync(scheme, folder);
        TopTip.IsOpen = true;
        BottomTip.IsOpen = true;
        Require(!TopTip.IsOpen && BottomTip.IsOpen, "Opening another tooltip did not dismiss the previous one.");
        Require(Microsoft.UI.Xaml.Automation.AutomationProperties.GetDescribedBy(BottomTrigger).Contains(BottomTip), "Tooltip owner lost its accessible description relationship.");
        BottomTip.IsOpen = false;
    }

    private async Task VerifyCollisionsAsync(BleeckerColorScheme scheme, string folder)
    {
        var root = (Grid)XamlRoot.Content;
        var layer = new Canvas();
        var target = new Button { Content = "Target", Width = 80, Height = 40, Style = TopTrigger.Style };
        var tip = new BleeckerTooltip { Content = "Edge tooltip", PlacementTarget = target };
        BleeckerTooltipService.SetToolTip(target, tip);
        layer.Children.Add(target);
        root.Children.Add(layer);
        try
        {
            foreach (var side in new[] { PlacementMode.Top, PlacementMode.Bottom, PlacementMode.Left, PlacementMode.Right })
            foreach (var edge in new[] { false, true })
            {
                var x = edge && side == PlacementMode.Left ? 10 : edge && side == PlacementMode.Right ? root.ActualWidth - 90 : (root.ActualWidth - 80) / 2;
                var y = edge && side == PlacementMode.Top ? 10 : edge && side == PlacementMode.Bottom ? root.ActualHeight - 50 : (root.ActualHeight - 40) / 2;
                Canvas.SetLeft(target, x);
                Canvas.SetTop(target, y);
                tip.Placement = side;
                await Task.Delay(80);
                tip.IsOpen = true;
                await Task.Delay(180);
                var opposite = side switch { PlacementMode.Top => PlacementMode.Bottom, PlacementMode.Bottom => PlacementMode.Top, PlacementMode.Left => PlacementMode.Right, _ => PlacementMode.Left };
                Require(tip.ActualPlacement == (edge ? opposite : side), "Tooltip did not resolve the expected collision side.");
                var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Single(item => ReferenceEquals(item.Child, tip));
                Require(popup.IsConstrainedToRootBounds, "Tooltip still uses an unconstrained native popup window.");
                var arrow = Descendants<Polygon>(tip).Single();
                var position = arrow.TransformToVisual(target).TransformPoint(new Windows.Foundation.Point());
                var gap = tip.ActualPlacement switch
                {
                    PlacementMode.Top => -position.Y - arrow.ActualHeight,
                    PlacementMode.Bottom => position.Y - target.ActualHeight,
                    PlacementMode.Left => -position.X - arrow.ActualWidth,
                    _ => position.X - target.ActualWidth
                };
                Require(Math.Abs(gap - 7) <= .5, $"Tooltip arrow clearance differs from reference: {side}/{edge}/{gap}.");
                await MainWindow.CaptureElementAsync(tip, System.IO.Path.Combine(folder, $"tooltip-collision-{side}-{edge}-{scheme}.png"));
                tip.IsOpen = false;
                await Task.Delay(80);
            }
        }
        finally
        {
            tip.IsOpen = false;
            BleeckerTooltipService.SetToolTip(target, null);
            root.Children.Remove(layer);
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
