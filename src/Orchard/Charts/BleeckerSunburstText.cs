using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

// Actual shaped glyph outlines allow SVG's stroke-before-fill paint order.
internal static class BleeckerSunburstText
{
    internal sealed record Label(string Text, double X, double Baseline, double Advance, PathGeometry Geometry, PathGeometry OutlineGeometry);
    internal static Label Create(string text, string family, float size, ushort weight, double x, double baseline)
    {
        var marker = family.IndexOf('#');
        if (marker >= 0 && Uri.TryCreate(family[..marker], UriKind.Absolute, out var uri))
        {
            if (uri.Scheme == "ms-appx" && uri.Host.Length == 0) { uri = new Uri(System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/')))); family = uri.AbsoluteUri + family[marker..]; }
            using var fonts = new CanvasFontSet(uri); if (fonts.Fonts.Count == 0) throw new InvalidOperationException("Sunburst cannot load its requested font.");
        }
        using var format = new CanvasTextFormat { FontFamily = family, FontSize = size, FontWeight = new Windows.UI.Text.FontWeight { Weight = weight } };
        // SVG's default whitespace processing removes the outer breaking spaces
        // after Recharts has joined its words. Keep the original DOM label text.
        var paintedText = text.Trim(' ');
        using var layout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(), paintedText, format, float.MaxValue, float.MaxValue); if (paintedText.Length > 0) layout.SetPairKerning(0, paintedText.Length, true);
        var advance = Math.Ceiling(layout.LayoutBounds.Width * 64) / 64;
        using var outline = CanvasGeometry.CreateText(layout);
        var nativeBaseline = layout.LineMetrics.Length == 0 ? 0 : layout.LineMetrics[0].Baseline;
        using var positioned = outline.Transform(Matrix3x2.CreateTranslation((float)(x - advance / 2), (float)(baseline - nativeBaseline)));
        // XAML geometry instances have a single owner. Both paints use the same
        // shaped outline, with independent native geometry for each Path.
        var receiver = new Receiver(); positioned.SendPathTo(receiver);
        var strokeReceiver = new Receiver(); positioned.SendPathTo(strokeReceiver);
        return new(text, x, baseline, advance, receiver.Geometry, strokeReceiver.Geometry);
    }
    private sealed class Receiver : ICanvasPathReceiver
    {
        internal PathGeometry Geometry { get; } = new(); private PathFigure? figure;
        private static Point P(Vector2 point) => new(point.X, point.Y);
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
