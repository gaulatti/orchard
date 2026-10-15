using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Orchard.Tests;

[TestClass]
public sealed class SunburstGeometryTests
{
    [TestMethod]
    public void OriginalTreesReachTheCapturedSectorsAndValueBaselines()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "SunburstChartLayout.json")));
        var count = 0;
        foreach (var sample in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var props = sample.GetProperty("props"); var normal = sample.GetProperty("normal"); var data = (IReadOnlyDictionary<string, object?>)Read(props.GetProperty("data"))!;
            var key = props.TryGetProperty("dataKey", out var dataKey) ? dataKey.GetString()! : "value"; var original = JsonSerializer.Serialize(data);
            var actual = BleeckerSunburstGeometry.Compute(data, key, Math.Round(normal.GetProperty("width").GetDouble(), MidpointRounding.AwayFromZero), Math.Round(normal.GetProperty("height").GetDouble(), MidpointRounding.AwayFromZero));
            var id = sample.GetProperty("id").GetString()!;
            var expected = normal.GetProperty("paths").EnumerateArray().Select(p => p.GetProperty("attributes").GetProperty("d").GetString()!).Where(p => !Regex.IsMatch(p, "NaN|Infinity")).ToArray();
            var paths = actual.Entries.Select(entry => entry.Path(actual.CenterX, actual.CenterY)).OfType<string>().ToArray(); Assert.AreEqual(expected.Length, paths.Length, id);
            for (var i = 0; i < paths.Length; i++) { var a = Numbers(paths[i]); var b = Numbers(expected[i]); Assert.AreEqual(b.Length, a.Length, id); for (var j = 0; j < a.Length; j++) Near(a[j], b[j], id); }
            var labels = actual.Entries.Where(entry => entry.HasLabel).ToArray(); var sourceLabels = normal.GetProperty("labels").EnumerateArray().ToArray(); Assert.AreEqual(sourceLabels.Length, labels.Length, id);
            for (var i = 0; i < labels.Length; i++) { Assert.AreEqual(sourceLabels[i].GetProperty("text").GetString(), labels[i].Label, id); var attrs = sourceLabels[i].GetProperty("attributes"); Near(labels[i].LabelX, double.Parse(attrs.GetProperty("x").GetString()!, CultureInfo.InvariantCulture), id); Near(labels[i].LabelY, double.Parse(attrs.GetProperty("y").GetString()!, CultureInfo.InvariantCulture), id); }
            Assert.AreEqual(original, JsonSerializer.Serialize(data), "Layout mutated original tree records: " + id);
            count++;
        }
        Assert.AreEqual(94, count);
    }
    [TestMethod]
    public void SharedRecordsKeepTheirIdentityAtEveryTreePath()
    {
        var shared = new Dictionary<string, object?> { ["name"] = "Shared", ["value"] = 30d };
        var branches = new[] { new Dictionary<string, object?> { ["name"] = "A", ["value"] = 30d, ["children"] = new[] { shared } }, new Dictionary<string, object?> { ["name"] = "B", ["value"] = 30d, ["children"] = new[] { shared } } };
        var root = new Dictionary<string, object?> { ["name"] = "Root", ["children"] = branches };
        var layout = BleeckerSunburstGeometry.Compute(root, "value", 320, 240);
        Assert.AreSame(root, layout.Root.Record); Assert.AreSame(branches, layout.Root.ChildSource);
        Assert.AreSame(shared, layout.Entries[1].Node.Record); Assert.AreSame(shared, layout.Entries[3].Node.Record);
        CollectionAssert.AreEqual(new[] { 0, 0 }, layout.Entries[1].Node.Path); CollectionAssert.AreEqual(new[] { 1, 0 }, layout.Entries[3].Node.Path);
        Assert.AreEqual(60d, layout.RootValue); Assert.IsFalse(root.ContainsKey("value")); Assert.IsFalse(shared.ContainsKey("fill"));
        shared["value"] = 20d;
        var changed = BleeckerSunburstGeometry.Compute(root, "value", 320, 240);
        Assert.AreEqual(40d, changed.RootValue); Assert.AreEqual("20", changed.Entries[1].Label); Assert.AreEqual("20", changed.Entries[3].Label);
    }
    [TestMethod]
    public void InvalidRecursiveInputFailsBeforeRenderingAndNeverBecomesEmptyData()
    {
        Assert.Throws<ArgumentNullException>(() => BleeckerSunburstGeometry.Compute(null!, "value", 320, 240));
        var cyclic = new Dictionary<string, object?>(); cyclic["children"] = new[] { cyclic };
        Assert.Throws<InvalidOperationException>(() => BleeckerSunburstGeometry.Compute(cyclic, "value", 320, 240));
        foreach (var children in new object[] { "invalid", 42, new object?[] { null }, new object[] { 42 } })
            Assert.Throws<InvalidOperationException>(() => BleeckerSunburstGeometry.Compute(new Dictionary<string, object?> { ["children"] = children }, "value", 320, 240));
        Assert.AreSame(cyclic, ((IReadOnlyDictionary<string, object?>[])cyclic["children"]!)[0]);
    }
    private static void Near(double actual, double expected, string id) => Assert.IsLessThan(.001, Math.Abs(actual - expected), id + $" {actual:R}/{expected:R}");
    private static double[] Numbers(string value) => Regex.Matches(value, @"[-+]?(?:\d*\.)?\d+(?:[eE][-+]?\d+)?").Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
    private static object? Read(JsonElement value) => value.ValueKind switch { JsonValueKind.Object => value.EnumerateObject().ToDictionary(p => p.Name, p => Read(p.Value)), JsonValueKind.Array => value.EnumerateArray().Select(Read).ToArray(), JsonValueKind.Number => value.GetDouble(), JsonValueKind.String => value.GetString(), JsonValueKind.Null => null, JsonValueKind.True => true, JsonValueKind.False => false, _ => throw new InvalidOperationException("Unknown source value.") };
}
