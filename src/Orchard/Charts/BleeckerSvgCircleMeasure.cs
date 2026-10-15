// Circular SVG arc construction and conic distance interpolation follow
// Chromium 155's Skia (29ed1e87), Copyright 2018 Google LLC, BSD-3-Clause.
// See licenses/LICENSE-Skia.txt and the Radial Bar reference documentation.
using System.Numerics;

namespace Orchard;

internal sealed class BleeckerSvgCircleMeasure
{
    private sealed record Conic(Vector2 A, Vector2 B, Vector2 C, float Weight)
    {
        internal Vector2 Point(float t)
        {
            var weighted = B * Weight; var a = A - weighted * 2 + C; var b = (weighted - A) * 2;
            var denominator = (2 * (1 - Weight) * t + 2 * (Weight - 1)) * t + 1;
            return ((a * t + b) * t + A) / denominator;
        }
        internal Vector2 Tangent(float t)
        {
            var p20 = C - A; var c = (B - A) * Weight; var a = p20 * Weight - p20; var b = p20 - c - c;
            return Vector2.Normalize((a * t + b) * t + c);
        }
    }
    private sealed record Segment(Conic Curve, float T, float Distance);
    private readonly List<Segment> segments = [];
    internal float Length => segments.Count == 0 ? 0 : segments[^1].Distance;
    internal BleeckerSvgCircleMeasure(Vector2 start, Vector2 end, float radius, bool clockwise)
    {
        if (radius <= 0 || start == end) return;
        var inverseRadius = 1f / radius; var unit0 = start * inverseRadius; var unit1 = end * inverseRadius; var delta = unit1 - unit0; var d = Vector2.Dot(delta, delta);
        var scale = MathF.Sqrt(MathF.Max(1 / d - .25f, 0)); if (clockwise) scale = -scale;
        delta *= scale; var center = (unit0 + unit1) * .5f + new Vector2(-delta.Y, delta.X);
        unit0 -= center; unit1 -= center;
        var theta = MathF.Atan2(unit0.Y, unit0.X); var arc = MathF.Atan2(unit1.Y, unit1.X) - theta;
        if (arc < 0 && clockwise) arc += MathF.PI * 2; else if (arc > 0 && !clockwise) arc -= MathF.PI * 2;
        var count = (int)MathF.Ceiling(MathF.Abs(arc / (2 * MathF.PI / 3))); var width = arc / count;
        var tangent = MathF.Tan(width * .5f); var weight = MathF.Sqrt(.5f + MathF.Cos(width) * .5f); var previous = start;
        for (var i = 0; i < count; i++)
        {
            var stop = theta + width; var sin = MathF.Sin(stop); var cos = MathF.Cos(stop);
            var unitEnd = new Vector2(cos, sin) + center; var control = unitEnd + new Vector2(tangent * sin, -tangent * cos);
            var next = i == count - 1 ? end : unitEnd * radius;
            var conic = new Conic(previous, control * radius, next, weight); Add(conic, 0, previous, 0x3FFFFFFF, next, 0); previous = next; theta = stop;
        }
    }
    private void Add(Conic curve, int min, Vector2 a, int max, Vector2 b, int depth)
    {
        var half = (min + max) >> 1; var middle = curve.Point(half * (1f / 0x3FFFFFFF)); var deviation = middle - (a + b) * .5f;
        if (depth < 8 && (max - min) >> 10 != 0 && MathF.Max(MathF.Abs(deviation.X), MathF.Abs(deviation.Y)) > .5f)
        {
            Add(curve, min, a, half, middle, depth + 1); Add(curve, half, middle, max, b, depth + 1);
        }
        else
        {
            var previous = Length; var distance = previous + Vector2.Distance(a, b);
            if (distance > previous) segments.Add(new(curve, max * (1f / 0x3FFFFFFF), distance));
        }
    }
    internal (Vector2 Point, Vector2 Tangent) At(float distance)
    {
        if (segments.Count == 0) throw new InvalidOperationException("A measured SVG circle requires a nonempty arc.");
        distance = Math.Clamp(distance, 0, Length); var index = segments.FindIndex(segment => segment.Distance >= distance); var current = segments[index];
        var startDistance = index == 0 ? 0 : segments[index - 1].Distance; var startT = index > 0 && ReferenceEquals(segments[index - 1].Curve, current.Curve) ? segments[index - 1].T : 0;
        var t = startT + (current.T - startT) * (distance - startDistance) / (current.Distance - startDistance);
        return (current.Curve.Point(t), current.Curve.Tangent(t));
    }
}
