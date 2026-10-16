using System.Security.Cryptography;
using System.Text.Json;
using Orchard.Catalog;

namespace Orchard.Tests;

[TestClass]
public sealed class DataTableColumnToggleReferenceTests
{
    private static string Fixture(string file) => Path.Combine(AppContext.BaseDirectory, "Fixtures", file);
    [TestMethod]
    public void ReferencePinsOriginalPopoverAndColumnVisibilityRuntime()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Fixture("DataTableColumnToggle.json"))); var root = document.RootElement;
        Assert.AreEqual("orchard-data-table-column-toggle-v1", root.GetProperty("format").GetString());
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("sourceCommit").GetString());
        Assert.AreEqual("9082ca90691d40083e9bb4b94f687daa56a8e3a064e356ab5dd9afa67ed89df7", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual("0b6113dd13d74b1023208cbf96e080915f3b47a2", root.GetProperty("sourceLockGitBlob").GetString());
        foreach (var (field, file) in new[] { ("scriptSha256", "CaptureDataTableColumnToggle.cjs"), ("clientSha256", "DataTableColumnToggleClient.tsx") }) Assert.AreEqual(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Fixture(file)))).ToLowerInvariant(), root.GetProperty(field).GetString());
        var cases = root.GetProperty("cases").EnumerateArray().ToArray(); Assert.HasCount(28, cases); Assert.AreEqual(132, cases.Sum(sample => sample.GetProperty("snapshots").GetArrayLength()));
        foreach (var point in cases.SelectMany(sample => sample.GetProperty("snapshots").EnumerateArray()))
        {
            Assert.IsTrue(point.GetProperty("state").GetProperty("inputEvents").EnumerateArray().All(item => item.GetProperty("trusted").GetBoolean()));
            Assert.IsGreaterThanOrEqualTo(3, point.GetProperty("presentation").GetProperty("stable").GetInt32()); Assert.IsLessThan(2000, point.GetProperty("presentation").GetProperty("milliseconds").GetDouble());
            if (point.GetProperty("state").GetProperty("popupOpen").GetBoolean()) Assert.AreEqual(224, point.GetProperty("state").GetProperty("popup").GetProperty("box").GetProperty("width").GetDouble());
        }
    }
    [TestMethod]
    public void VisibilityChangesReachOriginalConsumerColumnsAndRows()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Fixture("DataTableColumnToggle.json")));
        foreach (var sample in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var props = sample.GetProperty("props"); var initial = props.TryGetProperty("visibility", out var values) ? values.EnumerateObject().ToDictionary(pair => pair.Name, pair => pair.Value.GetBoolean()) : null;
            var a = new ColumnVisibilityTable(props.TryGetProperty("kind", out var kind) ? kind.GetString()! : "basic", emptyRows: props.TryGetProperty("rows", out var count) && count.GetInt32() == 0, initial: initial); var b = new ColumnVisibilityTable(second: true); var active = a;
            foreach (var point in sample.GetProperty("snapshots").EnumerateArray())
            {
                var state = point.GetProperty("state"); var phase = point.GetProperty("phase").GetString();
                switch (phase)
                {
                    case "replace": active = b; break;
                    case "click-first": active.Eligible.First().ToggleVisibility(!active.Eligible.First().IsVisible); break;
                    case "space": var target = active.Eligible.ElementAt(sample.GetProperty("id").GetString() == "long" ? 19 : 0); target.ToggleVisibility(!target.IsVisible); break;
                    case "external-status-off": var next = new Dictionary<string, bool>(active.Visibility) { ["status"] = false }; active.ExternalVisibility(next); break;
                    case "old-notification": a.ExternalVisibility(new Dictionary<string, bool> { ["title"] = false, ["status"] = false }); break;
                    case "hide-eligibility": active.EnableHiding = false; break;
                    case "restore-eligibility": active.EnableHiding = true; break;
                }
                Assert.AreEqual(state.GetProperty("visibleCells").GetString(), active.VisibleCells); Assert.AreEqual(state.GetProperty("events").GetArrayLength(), a.Requests.Count + b.Requests.Count);
                var table = state.GetProperty("table"); Assert.AreEqual(table.GetProperty("name").GetString(), active.Name); Assert.IsTrue(table.GetProperty("sameTable").GetBoolean() && table.GetProperty("sameRows").GetBoolean());
                CollectionAssert.AreEqual(table.GetProperty("columns").EnumerateArray().Select(column => column.GetProperty("id").GetString()).ToArray(), active.Columns.Select(column => column.Id).ToArray());
                foreach (var request in active.Requests) Assert.IsTrue(active.Columns.Any(column => ReferenceEquals(column, request.Column)));
                if (state.GetProperty("popupOpen").GetBoolean()) CollectionAssert.AreEqual(state.GetProperty("options").EnumerateArray().Select(option => option.GetProperty("id").GetString()).ToArray(), active.Eligible.Select(column => column.Id).ToArray());
            }
        }
    }
}
