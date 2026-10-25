using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using System.Numerics;
using Windows.Foundation;

namespace Orchard;

internal static class BleeckerShadowGeometry
{
    internal static CanvasGeometry CanvasMask(CanvasDevice device, double width, double height, CornerRadius corners, double inset)
    {
        var contour = RoundedMask(width, height, corners, inset).Figures[0];
        using var builder = new CanvasPathBuilder(device);
        builder.BeginFigure(new Vector2((float)contour.StartPoint.X, (float)contour.StartPoint.Y));
        foreach (var segment in contour.Segments)
        {
            if (segment is LineSegment line) builder.AddLine(new Vector2((float)line.Point.X, (float)line.Point.Y));
            else if (segment is ArcSegment arc) builder.AddArc(new Vector2((float)arc.Point.X, (float)arc.Point.Y), (float)arc.Size.Width, (float)arc.Size.Height, 0, CanvasSweepDirection.Clockwise, CanvasArcSize.Small);
        }
        builder.EndFigure(CanvasFigureLoop.Closed);
        return CanvasGeometry.CreatePath(builder);
    }
    internal static PathGeometry RoundedMask(double width, double height, CornerRadius corners, double inset)
    {
        var radii = new[] { corners.TopLeft, corners.TopRight, corners.BottomRight, corners.BottomLeft }
            .Select(radius => Math.Max(0, radius - inset)).ToArray();
        // Normalize overlapping corners together, preserving their relative sizes.
        var scale = 1d;
        foreach (var (length, total) in new[] { (width, radii[0] + radii[1]), (height, radii[1] + radii[2]), (width, radii[2] + radii[3]), (height, radii[3] + radii[0]) })
            if (total > 0) scale = Math.Min(scale, length / total);
        for (var i = 0; i < radii.Length; i++) radii[i] *= scale;
        var figure = new PathFigure { StartPoint = new Point(radii[0], 0), IsClosed = true, IsFilled = true };
        void Line(double x, double y) => figure.Segments.Add(new LineSegment { Point = new Point(x, y) });
        void Corner(double radius, double x, double y)
        {
            if (radius == 0) Line(x, y);
            else figure.Segments.Add(new ArcSegment { Point = new Point(x, y), Size = new Size(radius, radius), SweepDirection = SweepDirection.Clockwise });
        }
        Line(width - radii[1], 0); Corner(radii[1], width, radii[1]);
        Line(width, height - radii[2]); Corner(radii[2], width - radii[2], height);
        Line(radii[3], height); Corner(radii[3], 0, height - radii[3]);
        Line(0, radii[0]); Corner(radii[0], radii[0], 0);
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }
}
