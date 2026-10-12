using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Orchard.Catalog;

public sealed partial class StatusBadgesFixture : UserControl
{
    public StatusBadgesFixture() => InitializeComponent();
    internal async Task VerifyAsync(BleeckerColorScheme scheme)
    {
        var longRoot = (Grid)VisualTreeHelper.GetChild(LongLabel, 0);
        var longSurface = (Border)longRoot.Children[0];
        var longDescription = (TextBlock)longRoot.Children[1];
        var descriptionSlot = Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(longDescription).Width - longDescription.Margin.Left - longDescription.Margin.Right;
        var longDot = (Ellipse)((Grid)longSurface.Child).Children[0];
        Require(Math.Abs(longDot.Width - 3.6875) < .2 && longDot.Height == 6, "Constrained status dot did not follow reference flex shrinking.");
        Require(longSurface.ActualHeight == 39 && Math.Abs(longSurface.ActualWidth - 190.203125) < 3 && Math.Abs(descriptionSlot - 121.796875) < 3,
            $"Long status label did not share constrained width with its description: badge={longSurface.ActualWidth}x{longSurface.ActualHeight}, description={longDescription.ActualWidth}x{longDescription.ActualHeight}, root={longRoot.ActualWidth}, column={longRoot.ColumnDefinitions[0].Width}.");
        LongLabel.Width = 640;
        await Task.Delay(80);
        Require(longSurface.ActualHeight == 24 && longDescription.ActualHeight <= 17, "Widening status content did not restore single-line layout.");
        LongLabel.Width = 320;
        await Task.Delay(80);
        Require(longSurface.ActualHeight > 24 && longDescription.ActualHeight > 17, "Narrowing status content did not restore wrapping.");
        foreach (var badge in Variants.Children.OfType<BleeckerStatusBadge>())
        {
            var root = (Grid)VisualTreeHelper.GetChild(badge, 0);
            var surface = (Border)root.Children[0];
            var content = (Grid)surface.Child;
            var dot = (Ellipse)content.Children[0];
            var label = (TextBlock)content.Children[1];
            Require(surface.ActualHeight == 24 && surface.Padding.Left == 10 && surface.Padding.Top == 2 && surface.CornerRadius.TopLeft == 6 && surface.BorderThickness.Left == 1, "Status badge geometry differs from the reference.");
            Require(dot.ActualWidth == 6 && dot.ActualHeight == 6 && content.ColumnSpacing == 6, "Status dot geometry mismatch.");
            Require(label.FontSize == 11 && label.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf") && label.LineHeight == 16.5, "Status typography inherited Windows defaults.");
            var dark = scheme == BleeckerColorScheme.Dark;
            var colors = BleeckerTokens.Colors(scheme);
            var tint = badge.Variant switch { BleeckerStatusBadgeVariant.Live => dark ? "#FF00D492" : "#FF009966", BleeckerStatusBadgeVariant.Offline => colors["terracotta"], BleeckerStatusBadgeVariant.Warning => colors["desert"], BleeckerStatusBadgeVariant.Info => colors[dark ? "accentBlue" : "sea"], _ => colors["sand"] };
            var foreground = badge.Variant switch { BleeckerStatusBadgeVariant.Live => dark ? "#FF5EE9B5" : "#FF007A55", BleeckerStatusBadgeVariant.Offline => dark ? colors["terracotta"] : "#FF874641", BleeckerStatusBadgeVariant.Warning => colors["accentText"], BleeckerStatusBadgeVariant.Info => tint, _ => colors["textSecondary"] };
            var dotColor = badge.Variant == BleeckerStatusBadgeVariant.Live ? "#FF00BC7D" : badge.Variant == BleeckerStatusBadgeVariant.Default ? colors["textSecondary"] : tint;
            Require(Hex(badge.Background) == tint && Hex(badge.BorderBrush) == tint && Hex(badge.Foreground) == foreground && Hex(dot.Fill) == dotColor, "Status palette differs from the pinned recipe.");
            Require(Math.Abs(((SolidColorBrush)badge.Background).Opacity - (badge.Variant == BleeckerStatusBadgeVariant.Live && !dark ? .08 : badge.Variant == BleeckerStatusBadgeVariant.Default && !dark ? .2 : .1)) < .001, "Status background alpha mismatch.");
            Require(Math.Abs(((SolidColorBrush)badge.BorderBrush).Opacity - (badge.Variant == BleeckerStatusBadgeVariant.Default && !dark ? .3 : .2)) < .001, "Status border alpha mismatch.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(badge);
            Require(!badge.IsTabStop && peer.GetName() == badge.Label && peer.GetHelpText() == badge.Description && peer.GetAutomationControlType() == AutomationControlType.Text, "Status accessibility contract mismatch.");
        }
        var originalHeight = Constrained.ActualHeight;
        Constrained.Width = 160;
        await Task.Delay(80);
        var constrainedSurface = (Border)((Grid)VisualTreeHelper.GetChild(Constrained, 0)).Children[0];
        var constrainedLabel = (TextBlock)((Grid)constrainedSurface.Child).Children[1];
        Require(constrainedLabel.ActualWidth >= 11, "International status label was squeezed below a readable glyph width.");
        Require(Constrained.ActualHeight > originalHeight, "Constrained status description did not wrap.");
        Constrained.Width = 240;
        var source = new TextBlock { Text = "UPDATED" };
        Changing.SetBinding(BleeckerStatusBadge.LabelProperty, new Binding { Source = source, Path = new PropertyPath("Text") });
        Changing.Variant = BleeckerStatusBadgeVariant.Offline;
        source.Text = "RESTORED";
        Changing.Description = "";
        await Task.Delay(80);
        Require(Changing.Label == "RESTORED" && Changing.GetBindingExpression(BleeckerStatusBadge.LabelProperty) is not null, "Status change discarded consumer label binding.");
        Require(((Grid)VisualTreeHelper.GetChild(Changing, 0)).Children[1].Visibility == Visibility.Collapsed, "Empty status description retained a visible text element.");
        Require(((SolidColorBrush)Changing.Foreground).Color == ((SolidColorBrush)Variants.Children.OfType<BleeckerStatusBadge>().Single(b => b.Variant == BleeckerStatusBadgeVariant.Offline).Foreground).Color, "Live status variant did not update its palette.");
        Changing.ClearValue(BleeckerStatusBadge.LabelProperty);
        Changing.Label = "LOCAL"; Changing.Description = "Consumer-owned status"; Changing.Variant = BleeckerStatusBadgeVariant.Info;
    }
    private static string Hex(Brush value) { var c = ((SolidColorBrush)value).Color; return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}"; }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
