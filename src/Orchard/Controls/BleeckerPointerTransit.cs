namespace Orchard;

internal readonly record struct TooltipPoint(double X, double Y);
internal readonly record struct TooltipBounds(double Left, double Top, double Right, double Bottom);

// Pointer-grace contract from @radix-ui/react-tooltip 1.2.16 (MIT, WorkOS).
// See licenses/LICENSE-Radix.txt. Geometry is independent of the Windows input adapter.
internal static class BleeckerPointerTransit
{
    internal static TooltipPoint[] Create(TooltipPoint exit, TooltipBounds leaving, TooltipBounds destination)
    {
        var distances = new[] { Math.Abs(exit.X - leaving.Left), Math.Abs(exit.X - leaving.Right), Math.Abs(exit.Y - leaving.Top), Math.Abs(exit.Y - leaving.Bottom) };
        var side = Array.IndexOf(distances, distances.Min());
        var center = side switch
        {
            0 => new TooltipPoint(exit.X + 5, exit.Y),
            1 => new TooltipPoint(exit.X - 5, exit.Y),
            2 => new TooltipPoint(exit.X, exit.Y + 5),
            _ => new TooltipPoint(exit.X, exit.Y - 5)
        };
        var delta = side < 2 ? new TooltipPoint(0, 5) : new TooltipPoint(5, 0);
        var points = new[]
        {
            new TooltipPoint(center.X - delta.X, center.Y - delta.Y),
            new TooltipPoint(center.X + delta.X, center.Y + delta.Y),
            new TooltipPoint(destination.Left, destination.Top), new TooltipPoint(destination.Right, destination.Top),
            new TooltipPoint(destination.Right, destination.Bottom), new TooltipPoint(destination.Left, destination.Bottom)
        }.Distinct().OrderBy(point => point.X).ThenBy(point => point.Y).ToArray();
        var hull = new List<TooltipPoint>();
        foreach (var point in points) Append(point, 0);
        var lowerCount = hull.Count;
        for (var i = points.Length - 2; i >= 0; i--) Append(points[i], lowerCount - 1);
        hull.RemoveAt(hull.Count - 1);
        return hull.ToArray();

        void Append(TooltipPoint point, int floor)
        {
            while (hull.Count >= floor + 2 && Cross(hull[^2], hull[^1], point) <= 0) hull.RemoveAt(hull.Count - 1);
            hull.Add(point);
        }
    }

    internal static bool Contains(IReadOnlyList<TooltipPoint> polygon, TooltipPoint point)
    {
        var inside = false;
        for (var i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];
            if ((a.Y > point.Y) != (b.Y > point.Y) && point.X < a.X + (point.Y - a.Y) * (b.X - a.X) / (b.Y - a.Y)) inside = !inside;
        }
        return inside;
    }

    private static double Cross(TooltipPoint a, TooltipPoint b, TooltipPoint c) => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
}
