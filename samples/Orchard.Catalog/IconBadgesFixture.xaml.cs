using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Orchard.Catalog;

public sealed partial class IconBadgesFixture : UserControl
{
    public IconBadgesFixture()
    {
        InitializeComponent();
        foreach (var variant in Enum.GetValues<BleeckerIconBadgeVariant>())
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            foreach (var size in Enum.GetValues<BleeckerIconBadgeSize>())
            {
                var badge = new BleeckerIconBadge { Size = size, Variant = variant };
                badge.Content = Vector(badge);
                row.Children.Add(badge);
            }
            Matrix.Children.Add(row);
        }
    }
    internal static Grid Vector(BleeckerIconBadge badge)
    {
        var grid = new Grid { Width = 24, Height = 24, UseLayoutRounding = false };
        var shape = new Rectangle { Width = 16, Height = 14, StrokeThickness = 2, UseLayoutRounding = false };
        // SVG strokes straddle their path; Rectangle's stroke is inside its bounds.
        shape.Width = 18; shape.Height = 16;
        shape.SetBinding(Shape.StrokeProperty, new Binding { Source = badge, Path = new PropertyPath("Foreground") });
        grid.Children.Add(shape);
        AutomationProperties.SetAccessibilityView(grid, AccessibilityView.Raw);
        return grid;
    }
    internal async Task VerifyAsync(BleeckerColorScheme scheme)
    {
        foreach (var badge in Matrix.Children.Cast<StackPanel>().SelectMany(row => row.Children.Cast<BleeckerIconBadge>()))
        {
            var extent = badge.Size == BleeckerIconBadgeSize.Medium ? 56 : 64;
            Require(badge.ActualWidth == extent && badge.ActualHeight == extent, "IconBadge size matrix changed.");
            Require(!badge.IsTabStop && badge.BorderThickness == new Thickness(1) && badge.CornerRadius == new CornerRadius(10), "IconBadge passive geometry changed.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(badge);
            Require(peer.GetPattern(PatternInterface.Invoke) is null && !peer.IsKeyboardFocusable(), "IconBadge acquired button semantics.");
            var expected = new ResourceDictionary { Source = new Uri($"ms-appx:///Orchard/Themes/IconBadges/{badge.Variant}.g.xaml") };
            var colors = (ResourceDictionary)expected.ThemeDictionaries[scheme == BleeckerColorScheme.Light ? "Light" : "Default"];
            foreach (var (key, value) in new[] { ("Background", badge.Background), ("BorderBrush", badge.BorderBrush), ("Foreground", badge.Foreground) })
            {
                var actual = (SolidColorBrush)value; var source = (SolidColorBrush)colors["IconBadge" + key];
                Require(actual.Color == source.Color && actual.Opacity == source.Opacity, "IconBadge palette changed: " + key);
            }
        }
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(Named).GetName() == "Consumer icon marker", "Named badge group lost its name.");
        Require(((Button)Interactive.Content).Content.Equals("A"), "Badge child was replaced.");
        await MainWindow.AwaitPresentationAsync(this);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
