using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Data;
using Windows.Storage.Streams;

namespace Orchard.Catalog;

public sealed partial class CardsFixture : UserControl
{
    public CardsFixture() => InitializeComponent();
    private int clicks;
    private void OnOpen(object sender, RoutedEventArgs args) => Result.Text = $"Workspace opened: {++clicks}";

    internal async Task VerifyAsync(BleeckerColorScheme scheme)
    {
        foreach (var card in Variants.Children.OfType<BleeckerCard>())
        {
            Require(card.CornerRadius.TopLeft == 12 && card.Padding.Left == 24 && card.BorderThickness.Left == 1, "Card geometry differs from Bleecker.");
            Require(card.FontSize == 16 && card.FontFamily.Source.Contains("EncodeSans-Regular.ttf"), "Card inherited Windows typography instead of the reference defaults.");
            var dark = scheme == BleeckerColorScheme.Dark;
            var background = (SolidColorBrush)card.Background;
            var border = (SolidColorBrush)card.BorderBrush;
            var subtle = card.Variant == BleeckerCardVariant.Subtle;
            var transparent = card.Variant == BleeckerCardVariant.Transparent;
            var color = dark ? subtle ? "#FFFFFFFF" : BleeckerTokens.Colors(scheme)["deepSea"] : subtle ? BleeckerTokens.Colors(scheme)["lightSand"] : "#FFFFFFFF";
            Require(Hex(background) == color && Math.Abs(background.Opacity - (transparent ? 0 : subtle ? dark ? .035 : .45 : 1)) < .001, "Card background recipe mismatch.");
            var alpha = card.Variant switch { BleeckerCardVariant.Surface => dark ? .09 : .3, BleeckerCardVariant.Outlined => dark ? .15 : .4, BleeckerCardVariant.Elevated => dark ? .1 : .25, BleeckerCardVariant.Subtle => dark ? .07 : .2, _ => 0 };
            Require(Hex(border) == (dark ? "#FFFFFFFF" : BleeckerTokens.Colors(scheme)["sand"]) && Math.Abs(border.Opacity - alpha) < .001, "Card border recipe mismatch.");
            var root = (Grid)VisualTreeHelper.GetChild(card, 0);
            var host = (Grid)root.Children[1];
            var layers = ElementCompositionPreview.GetElementChildVisual(host) as ContainerVisual;
            Require(card.Variant == BleeckerCardVariant.Elevated ? layers?.Children.Count == 2 : layers is null, "Card elevation layers do not follow variant.");
        }
        foreach (var card in Paddings.Children.OfType<BleeckerCard>())
            Require(card.Padding.Left == (card.PaddingSize switch { BleeckerCardPadding.None => 0, BleeckerCardPadding.Small => 16, BleeckerCardPadding.Medium => 24, _ => 32 }), "Card padding mismatch.");
        var original = Composition.ActualHeight;
        Composition.Width = 240;
        await MainWindow.AwaitPresentationAsync(this);
        Require(Composition.ActualHeight > original, "Card content did not wrap at constrained width.");
        Composition.Width = 320;
        var before = clicks;
        var action = (Button)VisualTreeHelper.GetChild(CardAction, 0);
        ((IInvokeProvider)new ButtonAutomationPeer(action).GetPattern(PatternInterface.Invoke)).Invoke();
        await MainWindow.AwaitPresentationAsync(this);
        Require(clicks == before + 1, "Card intercepted its consumer action.");
        SurfaceCard.Variant = BleeckerCardVariant.Subtle;
        await MainWindow.AwaitPresentationAsync(this);
        Require(Math.Abs(((SolidColorBrush)SurfaceCard.Background).Opacity - (scheme == BleeckerColorScheme.Dark ? .035 : .45)) < .001, "Live card variant did not refresh resources.");
        SurfaceCard.Variant = BleeckerCardVariant.Surface;
        var source = new Border { Background = new SolidColorBrush(Microsoft.UI.Colors.Coral) };
        SurfaceCard.SetBinding(Control.BackgroundProperty, new Binding { Source = source, Path = new PropertyPath("Background") });
        SurfaceCard.Variant = BleeckerCardVariant.Outlined;
        source.Background = new SolidColorBrush(Microsoft.UI.Colors.CornflowerBlue);
        await MainWindow.AwaitPresentationAsync(this);
        Require(SurfaceCard.GetBindingExpression(Control.BackgroundProperty) is not null && Hex((SolidColorBrush)SurfaceCard.Background) == "#FF6495ED", "Changing variant replaced a consumer background binding.");
        SurfaceCard.ClearValue(Control.BackgroundProperty);
        SurfaceCard.Variant = BleeckerCardVariant.Surface;
        var consumerStyle = StyledCard.Style;
        var styleSource = new BleeckerCard { Style = consumerStyle };
        StyledCard.SetBinding(FrameworkElement.StyleProperty, new Binding { Source = styleSource, Path = new PropertyPath("Style") });
        foreach (var variant in Enum.GetValues<BleeckerCardVariant>())
        {
            StyledCard.Variant = variant;
            await MainWindow.AwaitPresentationAsync(this);
            Require(StyledCard.GetBindingExpression(FrameworkElement.StyleProperty) is not null && ReferenceEquals(StyledCard.Style, consumerStyle), "Changing card variant replaced the consumer style binding.");
            Require(StyledCard.CornerRadius.TopLeft == 20 && StyledCard.BorderThickness.Left == 2, "Changing card variant discarded consumer style setters.");
            var expected = variant == BleeckerCardVariant.Transparent ? 0 : variant == BleeckerCardVariant.Subtle ? scheme == BleeckerColorScheme.Dark ? .035 : .45 : 1;
            Require(Math.Abs(((SolidColorBrush)StyledCard.Background).Opacity - expected) < .001, "Consumer styling prevented the card variant from updating.");
        }
        var replacementStyle = new Style { TargetType = typeof(BleeckerCard) };
        replacementStyle.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(16)));
        styleSource.Style = replacementStyle;
        await MainWindow.AwaitPresentationAsync(this);
        Require(StyledCard.CornerRadius.TopLeft == 16, "Consumer style source stopped propagating after variant changes.");
        StyledCard.ClearValue(FrameworkElement.StyleProperty);
        StyledCard.Style = consumerStyle;
        StyledCard.Variant = BleeckerCardVariant.Subtle;
        var elevated = Variants.Children.OfType<BleeckerCard>().Single(card => card.Variant == BleeckerCardVariant.Elevated);
        elevated.CornerRadius = new CornerRadius(20, 8, 4, 16);
        await MainWindow.AwaitPresentationAsync(this);
        var elevatedRoot = (Grid)VisualTreeHelper.GetChild(elevated, 0);
        var mask = (Microsoft.UI.Xaml.Shapes.Path)((Canvas)elevatedRoot.Children[0]).Children[0];
        var outline = ((PathGeometry)mask.Data).Figures.Single();
        Require(outline.StartPoint.X == 20 && outline.Segments.OfType<ArcSegment>().Select(arc => arc.Size.Width).SequenceEqual(new double[] { 8, 4, 16, 20 }), "Changing card corners left the elevation mask stale.");
        elevated.ClearValue(Control.CornerRadiusProperty);
        await VerifyClippingAsync(scheme);
    }
    private async Task VerifyClippingAsync(BleeckerColorScheme scheme)
    {
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(ClipFixture);
        var buffer = await bitmap.GetPixelsAsync();
        var pixels = new byte[checked((int)buffer.Length)];
        using (var reader = DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
        string Pixel(int x, int y)
        {
            var offset = (y * bitmap.PixelWidth + x) * 4;
            return $"#{pixels[offset + 3]:X2}{pixels[offset + 2]:X2}{pixels[offset + 1]:X2}{pixels[offset]:X2}";
        }
        Require(Pixel(50, 50) == BleeckerTokens.Colors(scheme)["primary"], "Overflow fixture did not render its content.");
        Require(Pixel(130, 50) == BleeckerTokens.Colors(scheme)["background"], "Card content overflowed the right edge.");
        Require(Pixel(20, 20) == BleeckerTokens.Colors(scheme)["background"], "Card content ignored rounded corner clipping.");
    }
    private static string Hex(SolidColorBrush brush) => $"#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
