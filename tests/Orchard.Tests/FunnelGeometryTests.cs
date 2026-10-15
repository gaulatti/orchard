using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Orchard;

namespace Orchard.Tests;

[TestClass]
public sealed class FunnelGeometryTests
{
    [TestMethod]
    public void ConsumerRecordsReachTheCapturedBrowserTrapezoids()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "FunnelChartLayout.json")));
        var count = 0;
        foreach (var sample in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var normal = sample.GetProperty("normal"); var data = sample.GetProperty("props").GetProperty("data").EnumerateArray().Select(row => (IReadOnlyDictionary<string, object?>)Read(row)!).ToArray();
            var entries = BleeckerFunnelGeometry.Compute(data, Math.Round(normal.GetProperty("width").GetDouble(), MidpointRounding.AwayFromZero), Math.Round(normal.GetProperty("height").GetDouble(), MidpointRounding.AwayFromZero));
            var expected = normal.GetProperty("paths").EnumerateArray().Select(path => path.GetProperty("attributes").GetProperty("d").GetString()!).Where(path => !Regex.IsMatch(path, "NaN|Infinity")).ToArray(); var actual = entries.Select(entry => entry.Path).OfType<string>().ToArray();
            Assert.AreEqual(data.Length, entries.Length, "Geometry must retain records even when their source path paints no ink.");
            Assert.AreEqual(expected.Length, actual.Length, sample.GetProperty("id").GetString());
            for (var i = 0; i < actual.Length; i++)
            {
                var a = Numbers(actual[i]); var b = Numbers(expected[i]); Assert.AreEqual(b.Length, a.Length);
                for (var j = 0; j < a.Length; j++) Assert.IsLessThan(.001, Math.Abs(a[j] - b[j]), sample.GetProperty("id").GetString());
            }
            count++;
        }
        Assert.AreEqual(54, count);
    }
    private static double[] Numbers(string value) => Regex.Matches(value, @"[-+]?(?:\d*\.)?\d+(?:[eE][-+]?\d+)?").Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
    private static object? Read(JsonElement value) => value.ValueKind switch { JsonValueKind.Object => value.EnumerateObject().ToDictionary(property => property.Name, property => Read(property.Value)), JsonValueKind.Array => value.EnumerateArray().Select(Read).ToArray(), JsonValueKind.Number => value.GetDouble(), JsonValueKind.String => value.GetString(), JsonValueKind.True => true, JsonValueKind.False => false, JsonValueKind.Null => null, _ => throw new InvalidOperationException("Unknown source value.") };
}
