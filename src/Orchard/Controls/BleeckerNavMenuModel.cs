namespace Orchard;

public enum BleeckerNavDirection { Horizontal, Vertical }

internal static class BleeckerNavMenuModel
{
    internal static double[] Widths(IReadOnlyList<double> natural, IReadOnlyList<double> minimum, double available)
    {
        if (natural.Count != minimum.Count) throw new ArgumentException("Each navigation item requires its own minimum width.");
        var result = natural.ToArray(); var room = available - Math.Max(0, natural.Count - 1) * 32;
        if (!double.IsFinite(room) || result.Sum() <= room) return result;
        var frozen = new bool[result.Length];
        while (true)
        {
            var fixedWidth = Enumerable.Range(0, result.Length).Where(index => frozen[index]).Sum(index => result[index]);
            var basis = Enumerable.Range(0, result.Length).Where(index => !frozen[index]).Sum(index => natural[index]);
            if (basis == 0) break;
            var free = room - fixedWidth - basis; var clamped = false;
            for (var index = 0; index < result.Length; index++)
            {
                if (frozen[index]) continue;
                result[index] = natural[index] + free * natural[index] / basis;
                if (result[index] < minimum[index]) { result[index] = minimum[index]; frozen[index] = true; clamped = true; }
            }
            if (!clamped || frozen.All(value => value)) break;
        }
        return result.Select(value => Math.Round(value * 64, MidpointRounding.AwayFromZero) / 64).ToArray();
    }
    internal static string Key(string href, string label) => href + ":" + label;
}
