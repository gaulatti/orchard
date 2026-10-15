using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Orchard;

namespace Orchard.Tests;

[TestClass]
public sealed class SankeyGeometryTests
{
    [TestMethod]
    public void UnsupportedRecordTypesDoNotSilentlyChangeGraphRelationshipsOrValues()
    {
        IReadOnlyDictionary<string, object?>[] nodes = [new Dictionary<string, object?> { ["name"] = "A" }, new Dictionary<string, object?> { ["name"] = "B" }];
        foreach (var key in new[] { "source", "target", "value" })
        {
            var flow = new Dictionary<string, object?> { ["source"] = 0, ["target"] = 1, ["value"] = 10d }; flow[key] = "1";
            Assert.ThrowsExactly<InvalidOperationException>(() => BleeckerSankeyGeometry.Compute(nodes, [flow], 320, 240));
        }
    }
    [TestMethod]
    public void OriginalNodeAndFlowRecordsReachTheCapturedBrowserGeometry()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "SankeyChartLayout.json")));
        var count = 0;
        foreach (var sample in document.RootElement.GetProperty("cases").EnumerateArray().Concat(document.RootElement.GetProperty("errorCases").EnumerateArray()))
        {
            var props = sample.GetProperty("props"); var data = props.GetProperty("data");
            var nodes = Rows(data.GetProperty("nodes")); var links = Rows(data.GetProperty("links")); var normal = sample.GetProperty("normal");
            double Option(string key, double initial) => props.TryGetProperty(key, out var value) ? value.GetDouble() : initial;
            BleeckerSankeyGeometry.Layout Compute() => BleeckerSankeyGeometry.Compute(nodes, links, Math.Round(normal.GetProperty("width").GetDouble(), MidpointRounding.AwayFromZero), Math.Round(normal.GetProperty("height").GetDouble(), MidpointRounding.AwayFromZero), Option("nodeWidth", 12), Option("linkCurvature", .5));
            var id = sample.GetProperty("id").GetString()!;
            if (id == "rooted-cycle") { Assert.ThrowsExactly<InvalidOperationException>(() => Compute()); continue; }
            var actual = Compute(); var expected = normal.GetProperty("nodes").EnumerateArray().ToArray();
            Assert.AreEqual(nodes.Length, actual.Nodes.Length); Assert.AreEqual(links.Length, actual.Links.Length);
            for (var i = 0; i < expected.Length; i++)
            {
                var attrs = expected[i].GetProperty("attributes"); var node = actual.Nodes[i];
                Near(node.X, Attr(attrs, "x"), id); Near(node.Y, Attr(attrs, "y"), id); Near(node.Width, Attr(attrs, "width"), id); Near(node.Height, Attr(attrs, "height"), id);
            }
            var paths = normal.GetProperty("links").EnumerateArray().Select(path => path.GetProperty("attributes").GetProperty("d").GetString()!).Where(path => !Regex.IsMatch(path, "NaN|Infinity")).ToArray();
            var computed = actual.Links.Select(link => link.Path).OfType<string>().ToArray(); Assert.AreEqual(paths.Length, computed.Length, id);
            for (var i = 0; i < paths.Length; i++)
            {
                var a = Numbers(computed[i]); var b = Numbers(paths[i]); Assert.AreEqual(b.Length, a.Length, id);
                for (var j = 0; j < a.Length; j++) Near(a[j], b[j], id);
            }
            // The layout must never write computed positions/values into the
            // original records, including records whose SVG coordinates fail.
            Assert.IsFalse(nodes.Any(row => row.ContainsKey("x") || row.ContainsKey("depth") || row.ContainsKey("value")));
            Assert.IsFalse(links.Any(row => row.ContainsKey("sy") || row.ContainsKey("dy")));
            count++;
        }
        Assert.AreEqual(80, count);
    }
    private static double Attr(JsonElement value, string key) => value.TryGetProperty(key, out var attr) && double.TryParse(attr.GetString(), CultureInfo.InvariantCulture, out var result) ? result : double.NaN;
    private static void Near(double actual, double expected, string id) { if (double.IsNaN(expected)) Assert.IsTrue(double.IsNaN(actual), id); else if (double.IsInfinity(expected)) Assert.AreEqual(expected, actual, id); else Assert.IsLessThan(.001, Math.Abs(actual - expected), id + $" {actual:R}/{expected:R}"); }
    private static double[] Numbers(string value) => Regex.Matches(value, @"[-+]?(?:\d*\.)?\d+(?:[eE][-+]?\d+)?").Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
    private static IReadOnlyDictionary<string, object?>[] Rows(JsonElement value) => value.EnumerateArray().Select(row => (IReadOnlyDictionary<string, object?>)Read(row)!).ToArray();
    private static object? Read(JsonElement value) => value.ValueKind switch { JsonValueKind.Object => value.EnumerateObject().ToDictionary(p => p.Name, p => Read(p.Value)), JsonValueKind.Number => value.GetDouble(), JsonValueKind.String => value.GetString(), _ => throw new InvalidOperationException("Unknown Sankey source value.") };
}
