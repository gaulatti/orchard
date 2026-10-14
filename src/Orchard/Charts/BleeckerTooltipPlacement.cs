namespace Orchard;

// Frozen Recharts 3.8.1 defaults: offset 10, no escape or reverse direction.
internal static class BleeckerTooltipPlacement
{
    internal static double Position(double coordinate, double dimension, double start, double end)
    {
        var positive = coordinate + 10;
        return Math.Max(positive + dimension > end ? coordinate - dimension - 10 : positive, start);
    }
}
