namespace Orchard;

/// <summary>Intersects a browser-sized pointer sample with rounded control bounds.</summary>
internal static class BleeckerRoundedPointerHit
{
    internal static bool Contains(double width, double height, double topLeft, double topRight,
        double bottomRight, double bottomLeft, double x, double y)
    {
        if (width <= 0 || height <= 0 || x + 1 <= 0 || y + 1 <= 0 || x >= width || y >= height) return false;
        // CSS scales all corner radii together when adjacent corners overlap.
        var scale = Math.Min(1, Math.Min(Math.Min(Ratio(width, topLeft + topRight), Ratio(width, bottomLeft + bottomRight)),
            Math.Min(Ratio(height, topLeft + bottomLeft), Ratio(height, topRight + bottomRight))));
        topLeft *= scale; topRight *= scale; bottomRight *= scale; bottomLeft *= scale;
        // Blink tests the 1x1 CSS-pixel quad anchored at the pointer against the
        // rounded border, rather than testing a dimensionless point. This keeps
        // the adjacent corner pixels inside the pinned Checkbox's hit region.
        return !(x < topLeft && y < topLeft && Outside(topLeft, topLeft, topLeft)) &&
            !(x + 1 > width - topRight && y < topRight && Outside(width - topRight, topRight, topRight)) &&
            !(x + 1 > width - bottomRight && y + 1 > height - bottomRight && Outside(width - bottomRight, height - bottomRight, bottomRight)) &&
            !(x < bottomLeft && y + 1 > height - bottomLeft && Outside(bottomLeft, height - bottomLeft, bottomLeft));

        bool Outside(double centerX, double centerY, double radius)
        {
            var dx = centerX - Math.Clamp(centerX, x, x + 1);
            var dy = centerY - Math.Clamp(centerY, y, y + 1);
            return dx * dx + dy * dy > radius * radius;
        }
        static double Ratio(double size, double radii) => radii == 0 ? 1 : size / radii;
    }
}
