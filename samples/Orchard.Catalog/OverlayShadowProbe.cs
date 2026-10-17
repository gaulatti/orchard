using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

internal static class OverlayShadowProbe
{
    internal sealed record Layer(float BlurRadius, float sigma, double x, double y, float Opacity, byte R, byte G, byte B, byte A, bool mask, float width, float height, double insetX, double insetY);
    internal sealed record Snapshot(double ActualWidth, double ActualHeight, double radius, Layer[] layers);

    internal static Snapshot Read(BleeckerOverlaySurface surface)
    {
        var host = Descendants<Grid>(surface).Single(item => item.Name == "PART_ShadowHost");
        var container = ElementCompositionPreview.GetElementChildVisual(host) as ContainerVisual;
        Require(surface.IsLoaded && !surface.IsTabStop && host.Visibility == Visibility.Visible && container?.Children.Count == 2, "Overlay lost its loaded native shadow surface/layers.");
        var layers = container!.Children.Cast<SpriteVisual>().Select(sprite =>
        {
            var brush = sprite.Brush as CompositionEffectBrush ?? throw new InvalidOperationException("Overlay lost a native Gaussian effect layer.");
            var sigmaStatus = brush.Properties.TryGetScalar("cssBlur.BlurAmount", out var sigma);
            var colorStatus = brush.Properties.TryGetColor("shadowTint.Color", out var color);
            Require(sigmaStatus == CompositionGetValueStatus.Succeeded && colorStatus == CompositionGetValueStatus.Succeeded, $"Native Gaussian properties are unavailable: {sigmaStatus}/{colorStatus}.");
            var input = (CompositionSurfaceBrush)brush.GetSourceParameter("mask"); var source = (CompositionVisualSurface)input.Surface; var caster = (SpriteVisual)source.SourceVisual;
            var insetX = (surface.ActualWidth - caster.Size.X) / 2; var insetY = (surface.ActualHeight - caster.Size.Y) / 2;
            var extent = MathF.Ceiling(sigma * 3);
            Require(source.SourceOffset.X == -extent && source.SourceOffset.Y == -extent && source.SourceSize == caster.Size + new System.Numerics.Vector2(extent * 2) && sprite.Size == source.SourceSize, "Overlay clipped its three-sigma Gaussian kernel.");
            return new Layer(sigma * 2, sigma, sprite.Offset.X - source.SourceOffset.X - insetX, sprite.Offset.Y - source.SourceOffset.Y - insetY, sprite.Opacity, color.R, color.G, color.B, color.A, caster.Brush is not null, caster.Size.X, caster.Size.Y, insetX, insetY);
        }).ToArray();
        // VisualCollection enumerates bottom to top; CSS's first recipe is
        // foremost. Inspect this before sorting the report by recipe identity.
        Require(layers.Select(layer => layer.BlurRadius).SequenceEqual(new[] { 14f, 64f }), "Overlay reversed the source's front-to-back shadow order.");
        layers = layers.OrderByDescending(item => item.BlurRadius).ToArray();
        // Independently assert the pinned public recipe, without accessing library internals.
        (float X, float Y, float Blur, float Spread, float Opacity, byte R, byte G, byte B)[] expectedLayers = [(0,24,64,-20,.28f,26,55,77),(0,4,14,0,.06f,26,55,77)];
        foreach (var (actual, expected) in layers.Zip(expectedLayers))
            Require(actual.BlurRadius == expected.Blur && actual.sigma == expected.Blur / 2 && actual.x == expected.X && actual.y == expected.Y && Math.Abs(actual.Opacity - expected.Opacity) < .000001 && actual.R == expected.R && actual.G == expected.G && actual.B == expected.B && actual.A == 255 && actual.mask && Math.Abs(actual.insetX + expected.Spread) < .01 && Math.Abs(actual.insetY + expected.Spread) < .01, "Overlay differs from the canonical CSS shadow recipe.");
        return new(surface.ActualWidth, surface.ActualHeight, surface.CornerRadius.TopLeft, layers);
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i); if (child is T value) yield return value;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
