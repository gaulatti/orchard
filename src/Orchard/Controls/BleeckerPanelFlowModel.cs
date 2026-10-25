namespace Orchard;

internal static class BleeckerPanelFlowModel
{
    internal static double[] Tracks(double available, IReadOnlyList<double> natural, IReadOnlyList<bool> growing)
    {
        if (double.IsNaN(available) || available < 0 || natural.Count != growing.Count || natural.Any(n => !double.IsFinite(n) || n < 0))
            throw new ArgumentOutOfRangeException(nameof(available));
        if (double.IsPositiveInfinity(available)) return natural.ToArray();
        var fixedSize = natural.Where((_, index) => !growing[index]).Sum();
        var count = growing.Count(value => value); var fraction = count == 0 ? 0 : (float)(Math.Max(0, available - fixedSize) / count);
        var result = new double[natural.Count]; var index = 0; double allocated = 0;
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = !growing[i] ? natural[i] : index == count - 1 ? Math.Max(0, available - fixedSize) - allocated
                : Math.Floor((double)fraction * (index + 1) * 64) / 64 - Math.Floor((double)fraction * index * 64) / 64;
            if (growing[i]) { index++; allocated += result[i]; }
        }
        return result;
    }
}
