using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Orchard.Tests;

[TestClass]
public sealed class AttentionColorTests
{
    [TestMethod]
    public void FrozenProducerNormalizationAndBrowserColorsMatch()
    {
        using var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "AttentionColors.json")));
        foreach (var sample in reference.RootElement.GetProperty("cases").EnumerateArray())
        {
            var hue = Number(sample.GetProperty("inputHue").GetString()!); var intensity = Number(sample.GetProperty("inputIntensity").GetString()!);
            var actual = BleeckerAttentionColor.Create(hue, intensity); var label = $"hue={hue}, intensity={intensity}";
            var sourceHue = Regex.Match(sample.GetProperty("accent").GetString()!, @"hsl\((\d+)");
            Assert.AreEqual(int.Parse(sourceHue.Groups[1].Value, CultureInfo.InvariantCulture), actual.Hue, label + " normalized hue");
            Assert.AreEqual(sample.GetProperty("intensity").GetDouble(), actual.Intensity, 0, label);
            Assert.AreEqual(sample.GetProperty("redMix").GetInt32(), actual.RedMix, label);
            Assert.AreEqual(sample.GetProperty("coverage").GetDouble(), actual.Coverage, .0000000001, label);
            Check(sample.GetProperty("computedAccent").GetString()!, actual.Accent, label + " accent");
            Check(sample.GetProperty("computedBorder").GetString()!, actual.Border, label + " border");
            var background = sample.GetProperty("computedBackground").GetString()!;
            var stops = Regex.Matches(background, @"oklab\([^)]+\)");
            Check(stops[0].Value, actual.Start, label + " start"); Check(stops[1].Value, actual.Middle, label + " middle");
            var percentages = Regex.Matches(background, @"\)\s+([\d.]+)%");
            Assert.AreEqual(double.Parse(percentages[1].Groups[1].Value, CultureInfo.InvariantCulture) / 100, actual.MiddlePosition, 0, label);
            Assert.AreEqual(double.Parse(percentages[2].Groups[1].Value, CultureInfo.InvariantCulture) / 100, actual.EndPosition, 0, label);
        }
    }
    private static double Number(string text) => text switch { "NaN" => double.NaN, "Infinity" => double.PositiveInfinity, "-Infinity" => double.NegativeInfinity, _ => double.Parse(text, CultureInfo.InvariantCulture) };
    private static void Check(string css, (double L,double A,double B,double Alpha) actual, string label)
    {
        var values = Regex.Matches(css, @"[-+]?\d*\.?\d+(?:[eE][-+]?\d+)?").Select(value => double.Parse(value.Value, CultureInfo.InvariantCulture)).ToArray();
        var alpha = values.Length == 4 ? values[3] : 1;
        Assert.AreEqual(alpha, actual.Alpha, .000002, label + " alpha");
        Assert.AreEqual(values[0] * alpha, actual.L, .00005, label + " L");
        Assert.AreEqual(values[1] * alpha, actual.A, .00005, label + " A");
        Assert.AreEqual(values[2] * alpha, actual.B, .00005, label + " B");
    }
}
