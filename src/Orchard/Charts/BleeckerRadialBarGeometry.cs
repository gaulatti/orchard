namespace Orchard;

// Recharts 3.8.1 Sector's rounded tangent-circle construction. Full circles
// use its ordinary 359.999-degree sector path, as in Pie/Donut.
internal static class BleeckerRadialBarGeometry
{
    internal static string? Path(double cx, double cy, double inner, double outer, double start, double end)
    {
        if (outer < inner || start == end) return null;
        var corner = Math.Min(8, (outer - inner) / 2);
        if (corner <= 0 || Math.Abs(end - start) >= 360) return BleeckerPieGeometry.Path(cx, cy, inner, outer, new(start, end, 0));
        var sign = Math.Sign(end - start);
        static (BleeckerPieGeometry.Point Circle, BleeckerPieGeometry.Point Line, double Theta) Tangent(double x, double y, double radius, double angle, double direction, double c, bool external)
        {
            var centerRadius = radius + (external ? c : -c); var theta = Math.Asin(c / centerRadius) * 180 / Math.PI;
            return (BleeckerPieGeometry.Polar(x, y, radius, angle + direction * theta), BleeckerPieGeometry.Polar(x, y, centerRadius * Math.Cos(theta * Math.PI / 180), angle), theta);
        }
        var a = Tangent(cx, cy, outer, start, sign, corner, false); var b = Tangent(cx, cy, outer, end, -sign, corner, false);
        var angle = Math.Abs(end - start) - a.Theta - b.Theta;
        if (angle < 0) return BleeckerPieGeometry.Path(cx, cy, inner, outer, new(start, end, 0));
        var sweep = sign < 0 ? 1 : 0;
        static string N(double value) => (Math.Floor(value * 10000 + .5) / 10000).ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
        static string P(BleeckerPieGeometry.Point p) => N(p.X) + "," + N(p.Y);
        var path = $"M{P(a.Line)}A{N(corner)},{N(corner)},0,0,{sweep},{P(a.Circle)}A{N(outer)},{N(outer)},0,{(angle > 180 ? 1 : 0)},{sweep},{P(b.Circle)}A{N(corner)},{N(corner)},0,0,{sweep},{P(b.Line)}";
        if (inner <= 0) return path + $"L{N(cx)},{N(cy)}Z";
        var c = Tangent(cx, cy, inner, start, sign, corner, true); var d = Tangent(cx, cy, inner, end, -sign, corner, true);
        var innerAngle = Math.Abs(end - start) - c.Theta - d.Theta;
        return path + $"L{P(d.Line)}A{N(corner)},{N(corner)},0,0,{sweep},{P(d.Circle)}A{N(inner)},{N(inner)},0,{(innerAngle > 180 ? 1 : 0)},{1 - sweep},{P(c.Circle)}A{N(corner)},{N(corner)},0,0,{sweep},{P(c.Line)}Z";
    }
}
