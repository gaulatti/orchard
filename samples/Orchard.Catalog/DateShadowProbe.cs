using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

// Read the live compositor graph, independently of the library's recipe fields.
internal static class DateShadowProbe
{
    internal sealed record Snapshot(double ActualWidth, double ActualHeight, double radius, float BlurRadius, float sigma,
        double x, double y, float Opacity, byte R, byte G, byte B, byte A, float width, float height, double insetX, double insetY,
        bool mask, bool exteriorMask, bool paddedKernel);
    internal static Snapshot? Read(Control control)
    {
        var host = Descendants<Grid>(control).Single(item => item.Name == "DateShadowHost");
        var visual = ElementCompositionPreview.GetElementChildVisual(host);
        if (visual is null) return null;
        var sprite = visual as SpriteVisual ?? throw new InvalidOperationException("Date shadow lost its native sprite.");
        var outputMask = sprite.Brush as CompositionMaskBrush ?? throw new InvalidOperationException("Date shadow lost its output mask.");
        var color = (outputMask.Source as CompositionColorBrush ?? throw new InvalidOperationException("Date shadow lost its output colour.")).Color;
        var filteredSource = ((CompositionSurfaceBrush)outputMask.Mask).Surface as CompositionVisualSurface ?? throw new InvalidOperationException("Date shadow lost its filtered mask surface.");
        var filtered = (SpriteVisual)filteredSource.SourceVisual;
        var brush = filtered.Brush as CompositionEffectBrush ?? throw new InvalidOperationException("Date shadow lost its Gaussian effect.");
        Require(brush.Properties.TryGetScalar("cssBlur.BlurAmount", out var sigma) == CompositionGetValueStatus.Succeeded, "Date shadow lost its live blur property.");
        var input = (CompositionSurfaceBrush)brush.GetSourceParameter("mask"); var source = (CompositionVisualSurface)input.Surface; var caster = (SpriteVisual)source.SourceVisual;
        var insetX = (host.ActualWidth - caster.Size.X) / 2; var insetY = (host.ActualHeight - caster.Size.Y) / 2; var extent = MathF.Ceiling(sigma * 3);
        var padded = source.SourceOffset == new System.Numerics.Vector2(-extent) && source.SourceSize == caster.Size + new System.Numerics.Vector2(extent * 2) && sprite.Size == source.SourceSize;
        var exterior = Descendants<Microsoft.UI.Xaml.Shapes.Path>(control).Single(item => item.Name == "DateShadowExteriorMask");
        var exteriorMask = brush.GetSourceParameter("exterior") is CompositionSurfaceBrush && exterior.Data is PathGeometry { FillRule: FillRule.EvenOdd } hole && hole.Figures.Count == 2 &&
            hole.Transform is TranslateTransform shift && Math.Abs(shift.X + sprite.Offset.X) < .000001 && Math.Abs(shift.Y + sprite.Offset.Y) < .000001;
        Require(padded && exteriorMask && caster.Brush is not null && filtered.Size == sprite.Size && filteredSource.SourceSize == sprite.Size, "Date shadow clipped its kernel or included its rounded border box.");
        return new(host.ActualWidth, host.ActualHeight, control.CornerRadius.TopLeft, sigma * 2, sigma,
            sprite.Offset.X - source.SourceOffset.X - insetX, sprite.Offset.Y - source.SourceOffset.Y - insetY, color.A / 255f * sprite.Opacity, color.R, color.G, color.B, color.A,
            caster.Size.X, caster.Size.Y, insetX, insetY, caster.Brush is not null, exteriorMask, padded);
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        { var child = VisualTreeHelper.GetChild(parent, index); if (child is T value) yield return value; foreach (var nested in Descendants<T>(child)) yield return nested; }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
