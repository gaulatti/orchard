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
        tooltipPresentations.Clear();
        StartBringIntoView();
        await MainWindow.AwaitPresentationAsync(this);
        foreach (var tip in new[] { TopTip, BottomTip, LeftTip, RightTip, LongTip, TemplateTip })
        {
            tip.IsOpen = true;
            await AwaitTooltipPresentationAsync(tip, scheme, folder);
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
                await VerifyLiveAppearanceAsync(tip, surface, text, arrow, scheme, folder);
                if (tip == TemplateTip)
                    Require(FrameworkElementAutomationPeer.CreatePeerForElement(tip).GetName() == "Editorial guidance", "Consumer tooltip name was replaced by its data content.");
                if (tip == LongTip || tip == TemplateTip)
                {
                    var original = tip.Content;
                    var longHeight = surface.ActualHeight;
                    tip.Content = "Short update";
                    await AwaitVerificationAsync(() => Require(surface.ActualHeight < longHeight, "Tooltip did not remeasure a live content change."));
                    Require(Descendants<TextBlock>(surface).First().Text == "Short update", "Tooltip template did not receive updated consumer content.");
                    tip.Content = original;
                    await AwaitVerificationAsync(() => Require(surface.ActualHeight == longHeight, "Tooltip did not restore its wrapped content height."));
                }
            }
            finally { tip.IsOpen = false; }
            await AwaitTooltipClosedAsync(tip);
        }
        await VerifyCollisionsAsync(scheme, folder);
        TopTip.IsOpen = true;
        BottomTip.IsOpen = true;
        Require(!TopTip.IsOpen && BottomTip.IsOpen, "Opening another tooltip did not dismiss the previous one.");
        Require(Microsoft.UI.Xaml.Automation.AutomationProperties.GetDescribedBy(BottomTrigger).Contains(BottomTip), "Tooltip owner lost its accessible description relationship.");
        Require(BottomTrigger.Focus(FocusState.Keyboard), "Tooltip trigger did not accept keyboard focus.");
        await Task.Delay(100);
        var triggerPeer = FrameworkElementAutomationPeer.CreatePeerForElement(BottomTrigger);
        ((Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)triggerPeer.GetPattern(PatternInterface.Invoke)).Invoke();
        await Task.Delay(50);
        Require(!BottomTip.IsOpen && ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), BottomTrigger),
            $"Native trigger activation failed: open={BottomTip.IsOpen}, focus={(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot) as FrameworkElement)?.Name}.");
        BottomTip.IsOpen = false;
        await VerifyMotionLifecycleAsync();
        await VerifyWrappedActivationAsync();
    }

    private async Task VerifyWrappedActivationAsync()
    {
        var host = (Grid)XamlRoot.Content;
        var button = new BleeckerButton { Content = "Wrapped activation", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var tooltip = new BleeckerTooltip { Content = "Wrapped button help" };
        BleeckerTooltipService.SetToolTip(button, tooltip);
        host.Children.Add(button);
        try
        {
            host.UpdateLayout();
            await AwaitVerificationAsync(() => Require(Descendants<Button>(button).Any(), "Wrapped tooltip trigger template did not load."));
            var action = Descendants<Button>(button).Single();
            Require(action.Focus(FocusState.Keyboard), "Wrapped tooltip trigger could not focus.");
            await Task.Delay(100);
            Require(tooltip.IsOpen, "Wrapped button focus did not open its tooltip.");
            ((Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(action).GetPattern(PatternInterface.Invoke)).Invoke();
            await Task.Delay(50);
            Require(!tooltip.IsOpen, "Wrapped button activation left its tooltip open.");
        }
        finally
        {
            BleeckerTooltipService.SetToolTip(button, null);
            host.Children.Remove(button);
        }
    }

    private async Task VerifyLiveAppearanceAsync(BleeckerTooltip tip, BleeckerOverlaySurface surface, TextBlock text, Polygon arrow, BleeckerColorScheme scheme, string folder)
    {
        var root = (FrameworkElement)XamlRoot.Content;
        var original = root.RequestedTheme;
        var changed = scheme == BleeckerColorScheme.Light ? BleeckerColorScheme.Dark : BleeckerColorScheme.Light;
        var width = surface.ActualWidth;
        var height = surface.ActualHeight;
        try
        {
            root.RequestedTheme = changed == BleeckerColorScheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
            await AwaitVerificationAsync(() => VerifyPalette(changed));
            Require(tip.IsOpen && surface.ActualWidth == width && surface.ActualHeight == height, "Live appearance changed tooltip visibility or geometry.");
            await MainWindow.CaptureElementAsync(tip, System.IO.Path.Combine(folder, $"tooltip-live-{tip.Name}-{changed}.png"));
        }
        finally { root.RequestedTheme = original; }
        await AwaitVerificationAsync(() => VerifyPalette(scheme));

        void VerifyPalette(BleeckerColorScheme expected)
        {
            var colors = BleeckerTokens.Colors(expected);
            Require(ColorHex((SolidColorBrush)surface.Background).Equals(colors[expected == BleeckerColorScheme.Dark ? "sand" : "deepSea"], StringComparison.OrdinalIgnoreCase)
                && ColorHex((SolidColorBrush)text.Foreground).Equals(expected == BleeckerColorScheme.Dark ? colors["textPrimary"] : "#FFFFFFFF", StringComparison.OrdinalIgnoreCase)
                && ((SolidColorBrush)arrow.Fill).Color == ((SolidColorBrush)surface.Background).Color,
                $"Open tooltip retained its previous appearance: name={tip.Name}, expected={expected}, actual={tip.ActualTheme}, owner={tip.PlacementTarget?.ActualTheme}, root={root.RequestedTheme}/{root.ActualTheme}, background={ColorHex((SolidColorBrush)surface.Background)}, foreground={ColorHex((SolidColorBrush)text.Foreground)}.");
        }
    }

    private async Task VerifyMotionLifecycleAsync()
    {
        TopTip.IsOpen = true;
        await Task.Delay(30);
        var root = Descendants<FrameworkElement>(TopTip).Single(item => item.Name == "PART_MotionRoot");
        Require(root.Opacity == 1 && root.RenderTransform is ScaleTransform { ScaleX: 1, ScaleY: 1 }, "Instant tooltip entry animated or retained an old transform.");
        TopTip.IsOpen = false;
        if (!new Windows.UI.ViewManagement.UISettings().AnimationsEnabled)
            Require(!VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Any(item => ReferenceEquals(item.Child, TopTip)), "Reduced-motion dismissal retained a popup.");
        else
            Require(VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Any(item => ReferenceEquals(item.Child, TopTip)), "Normal-motion dismissal removed the popup before its exit animation.");
        TopTip.IsOpen = true;
        await Task.Delay(250);
        Require(TopTip.IsOpen && VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Count(item => ReferenceEquals(item.Child, TopTip)) == 1,
            "An interrupted exit removed the reopened tooltip or leaked a popup.");
        TopTip.IsOpen = false;
        await AwaitVerificationAsync(() => Require(!VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Any(item => ReferenceEquals(item.Child, TopTip)), $"Completed tooltip exit retained its popup: open={TopTip.IsOpen}, opacity={root.Opacity}."));
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
                await MainWindow.AwaitPresentationAsync(target);
                tip.IsOpen = true;
                await AwaitTooltipPresentationAsync(tip, scheme, folder);
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
                await AwaitTooltipClosedAsync(tip);
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
    private static async Task AwaitVerificationAsync(Action verify)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            try { verify(); return; }
            catch (InvalidOperationException) when (elapsed.Elapsed < TimeSpan.FromSeconds(1)) { await Task.Delay(16); }
        }
    }
}
