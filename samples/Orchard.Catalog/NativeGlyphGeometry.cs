using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

// Hash every native figure, fill rule and curve coordinate; no rounding,
// excluded glyphs or source-specific tolerance is applied.
internal static class NativeGlyphGeometry
{
    internal static string Hash(IEnumerable<PathGeometry> geometries) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", geometries.Select(Signature))))).ToLowerInvariant();
    internal static string Signature(PathGeometry geometry) => JsonSerializer.Serialize(new { geometry.FillRule, Figures = geometry.Figures.Select(figure => new { figure.StartPoint, figure.IsClosed, figure.IsFilled, Segments = figure.Segments.Select(segment => segment switch { LineSegment line => new[] { line.Point.X, line.Point.Y }, BezierSegment curve => new[] { curve.Point1.X, curve.Point1.Y, curve.Point2.X, curve.Point2.Y, curve.Point3.X, curve.Point3.Y }, QuadraticBezierSegment curve => new[] { curve.Point1.X, curve.Point1.Y, curve.Point2.X, curve.Point2.Y }, _ => throw new InvalidOperationException("Unknown native glyph segment.") }) }) });
}
