using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

// Shape the whole label with DirectWrite, then bend its actual glyph outlines
// onto the source text path. No bitmap or straight-label replacement is used.
internal static class BleeckerCurvedText
{
    internal sealed record Glyph(int CharacterIndex, string Character, Point Start, Point End, double Rotation, PathGeometry? Geometry);
    internal static IReadOnlyList<Glyph> Create(string text, string family, float size, double cx, double cy, double radius, double angle, bool clockwise)
    {
        if (text.Length == 0 || radius <= 0 || !double.IsFinite(radius)) return [];
        var marker = family.IndexOf('#');
        if (marker >= 0 && Uri.TryCreate(family[..marker], UriKind.Absolute, out var uri))
        {
            if (uri.Scheme == "ms-appx" && uri.Host.Length == 0)
            {
                uri = new Uri(System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/'))));
                family = uri.AbsoluteUri + family[marker..];
            }
            using var fonts = new CanvasFontSet(uri);
            if (fonts.Fonts.Count == 0) throw new InvalidOperationException("Curved label cannot load its requested font.");
        }
        using var format = new CanvasTextFormat { FontFamily = family, FontSize = size, FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 } };
        using var layout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(), text, format, float.MaxValue, float.MaxValue); layout.SetPairKerning(0, text.Length, true);
        var start = BleeckerPieGeometry.Polar(cx, cy, radius, angle); var end = BleeckerPieGeometry.Polar(cx, cy, radius, angle + (clockwise ? -359 : 359));
        var measure = new BleeckerSvgCircleMeasure(new((float)start.X, (float)start.Y), new((float)end.X, (float)end.Y), (float)radius, clockwise);
        var renderer = new Renderer(measure); layout.DrawToTextRenderer(renderer, Vector2.Zero);
        return renderer.Glyphs;
    }
    private sealed class Renderer(BleeckerSvgCircleMeasure measure) : ICanvasTextRenderer
    {
        internal List<Glyph> Glyphs { get; } = [];
        private float offset;
        public float Dpi => 96;
        public bool PixelSnappingDisabled => true;
        public Matrix3x2 Transform => Matrix3x2.Identity;
        public void DrawGlyphRun(Vector2 point, CanvasFontFace face, float size, CanvasGlyph[] glyphs, bool sideways, uint bidiLevel, object brush, CanvasTextMeasuringMode measuring, string locale, string text, int[] clusters, uint characterIndex, CanvasGlyphOrientation orientation)
        {
            for (var i = 0; i < glyphs.Length; i++)
            {
                var index = Array.IndexOf(clusters, i); var character = index >= 0 && index < text.Length ? text.Substring(index, 1) : "";
                var width = MathF.Ceiling(glyphs[i].Advance * 64) / 64;
                var distance = offset + width / 2; offset += width;
                if (distance < 0 || distance > measure.Length) { Glyphs.Add(new((int)characterIndex + index, character, new(), new(), 0, null)); continue; }
                var position = measure.At(distance); var center = position.Point; var startPoint = center - position.Tangent * (width / 2); var endPoint = center + position.Tangent * (width / 2);
                var tangent = Math.Atan2(position.Tangent.Y, position.Tangent.X); var start = new Point(startPoint.X, startPoint.Y); var end = new Point(endPoint.X, endPoint.Y); var rotation = tangent * 180 / Math.PI;
                var shift = (Math.Ceiling(face.Ascent * size) - Math.Ceiling(face.Descent * size)) / 2;
                using var outline = CanvasGeometry.CreateGlyphRun(CanvasDevice.GetSharedDevice(), new Vector2((float)-width / 2, (float)shift), face, size, [glyphs[i]], sideways, bidiLevel, measuring, orientation);
                using var transformed = outline.Transform(Matrix3x2.CreateRotation((float)tangent) * Matrix3x2.CreateTranslation((float)center.X, (float)center.Y));
                var receiver = new Receiver(); transformed.SendPathTo(receiver);
                Glyphs.Add(new((int)characterIndex + index, character, start, end, rotation, receiver.Geometry));
            }
        }
        public void DrawInlineObject(Vector2 point, ICanvasTextInlineObject inlineObject, bool sideways, bool rtl, object brush, CanvasGlyphOrientation orientation) => throw new NotSupportedException("Curved values do not contain inline objects.");
        public void DrawUnderline(Vector2 point, float width, float thickness, float offset, float height, CanvasTextDirection direction, object brush, CanvasTextMeasuringMode measuring, string locale, CanvasGlyphOrientation orientation) => throw new NotSupportedException("Curved values do not contain underlines.");
        public void DrawStrikethrough(Vector2 point, float width, float thickness, float offset, CanvasTextDirection direction, object brush, CanvasTextMeasuringMode measuring, string locale, CanvasGlyphOrientation orientation) => throw new NotSupportedException("Curved values do not contain strikethroughs.");
    }
    private sealed class Receiver : ICanvasPathReceiver
    {
        internal PathGeometry Geometry { get; } = new();
        private PathFigure? figure;
        private static Point P(Vector2 p) => new(p.X, p.Y);
        public void BeginFigure(Vector2 start, CanvasFigureFill fill) { figure = new() { StartPoint = P(start), IsFilled = fill == CanvasFigureFill.Default }; Geometry.Figures.Add(figure); }
        public void AddLine(Vector2 end) => figure!.Segments.Add(new LineSegment { Point = P(end) });
        public void AddCubicBezier(Vector2 a, Vector2 b, Vector2 end) => figure!.Segments.Add(new BezierSegment { Point1 = P(a), Point2 = P(b), Point3 = P(end) });
        public void AddQuadraticBezier(Vector2 control, Vector2 end) => figure!.Segments.Add(new QuadraticBezierSegment { Point1 = P(control), Point2 = P(end) });
        public void AddArc(Vector2 end, float rx, float ry, float rotation, CanvasSweepDirection sweep, CanvasArcSize size) => figure!.Segments.Add(new ArcSegment { Point = P(end), Size = new Size(rx, ry), RotationAngle = rotation * 180 / Math.PI, IsLargeArc = size == CanvasArcSize.Large, SweepDirection = sweep == CanvasSweepDirection.Clockwise ? SweepDirection.Clockwise : SweepDirection.Counterclockwise });
        public void EndFigure(CanvasFigureLoop loop) => figure!.IsClosed = loop == CanvasFigureLoop.Closed;
        public void SetFilledRegionDetermination(CanvasFilledRegionDetermination fill) => Geometry.FillRule = fill == CanvasFilledRegionDetermination.Winding ? FillRule.Nonzero : FillRule.EvenOdd;
        public void SetSegmentOptions(CanvasFigureSegmentOptions options) { }
    }
}
