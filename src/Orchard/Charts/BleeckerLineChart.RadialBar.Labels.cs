using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard;

public sealed partial class BleeckerLineChart
{
    private sealed record RadialLabel(int Index, string Text, string Path, IReadOnlyList<BleeckerCurvedText.Glyph> Glyphs);
    private readonly List<RadialLabel> radialLabels = [];
    private void RenderRadialLabel(RadialBarEntry entry)
    {
        if (entry.Value is null) return;
        var text = ChartReactText(entry.Value); var radius = (entry.Inner + entry.Outer) / 2;
        var delta = Math.Sign(entry.End - entry.Start) * Math.Min(Math.Abs(entry.End - entry.Start), 360); var clockwise = delta <= 0;
        var angle = entry.Start + (delta >= 0 ? 5 : -5); var a = BleeckerPieGeometry.Polar(radialCx, radialCy, radius, angle); var b = BleeckerPieGeometry.Polar(radialCx, radialCy, radius, angle + (clockwise ? -359 : 359));
        var path = $"M{N(a.X)},{N(a.Y)}A{N(radius)},{N(radius)},0,1,{(clockwise ? 1 : 0)},{N(b.X)},{N(b.Y)}";
        var family = FontFamily.Source.EndsWith("/EncodeSans-Regular.ttf#Encode Sans", StringComparison.Ordinal) ? Resource<TextBlock>("<TextBlock FontFamily=\"{StaticResource BleeckerPrimarySemiboldFontFamily}\" />").FontFamily.Source : FontFamily.Source;
        var glyphs = BleeckerCurvedText.Create(text, family, (float)(16 * tooltipSettings.TextScaleFactor), radialCx, radialCy, radius, angle, clockwise);
        radialLabels.Add(new(entry.Index, text, path, glyphs));
        foreach (var glyph in glyphs.Where(glyph => glyph.Geometry is not null))
        {
            var shape = Resource<Path>("<Path Fill=\"{ThemeResource BleeckerTextPrimaryBrush}\" IsHitTestVisible=\"False\" />"); shape.Data = glyph.Geometry; shape.Tag = ("radial-label-glyph", entry.Index, glyph.CharacterIndex); plot!.Children.Add(shape);
        }
    }
}
