using System.Security.Cryptography;
using System.Text.Json;
using Orchard.Catalog;

namespace Orchard.Tests;

[TestClass]
public sealed class DataTablePaginationKeyboardReferenceTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
    private static string Text(JsonElement element, string key) => element.GetProperty(key).GetString()!;
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllBytes(Fixture("DataTablePaginationKeyboard.json")));
    [TestMethod]
    public void KeyboardReferencePinsTheOriginalRuntimeAndEveryPhysicalSourceKey()
    {
        using var document = Reference(); var root = document.RootElement;
        Assert.AreEqual("orchard-data-table-pagination-keyboard-v1", Text(root, "format"));
        foreach (var (field, file) in new[] { ("baselineSha256", "DataTablePagination.json"), ("scriptSha256", "CaptureDataTablePaginationKeyboard.cjs"), ("clientSha256", "DataTablePaginationClient.tsx") })
            Assert.AreEqual(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Fixture(file)))).ToLowerInvariant(), Text(root, field));
        var cases = root.GetProperty("cases").EnumerateArray().ToArray(); Assert.HasCount(16, cases); Assert.AreEqual(136, cases.Sum(item => item.GetProperty("snapshots").GetArrayLength()));
        var keys = 0;
        foreach (var sample in cases)
        {
            var previousEvents = 0;
            foreach (var point in sample.GetProperty("snapshots").EnumerateArray())
            {
                var presentation = point.GetProperty("presentation"); Assert.IsGreaterThanOrEqualTo(3, presentation.GetProperty("stable").GetInt32()); Assert.IsLessThan(2000, presentation.GetProperty("milliseconds").GetDouble());
                var state = point.GetProperty("state"); var table = state.GetProperty("table"); Assert.IsTrue(table.GetProperty("sameTable").GetBoolean() && table.GetProperty("sameRows").GetBoolean());
                Assert.IsTrue(table.GetProperty("rows").EnumerateArray().All(row => row.GetProperty("same").GetBoolean()) && table.GetProperty("columns").EnumerateArray().All(column => column.GetProperty("same").GetBoolean()));
                var events = state.GetProperty("inputEvents").EnumerateArray().ToArray(); Assert.IsTrue(events.All(item => item.GetProperty("trusted").GetBoolean()));
                if (point.GetProperty("key").ValueKind == JsonValueKind.String)
                {
                    keys++; var key = Text(point, "key") switch { "Shift+Tab" => "Tab", "Space" => " ", var value => value };
                    var pair = events.Skip(previousEvents).Where(item => item.GetProperty("key").ValueKind == JsonValueKind.String && Text(item, "key") == key).ToArray();
                    CollectionAssert.AreEqual(new[] { "keydown", "keyup" }, pair.Select(item => Text(item, "type")).ToArray(), Text(sample, "id") + "/" + Text(point, "phase"));
                }
                previousEvents = events.Length;
            }
        }
        Assert.AreEqual(120, keys);
    }
    [TestMethod]
    public void HighlightCancelAndCommitCrossTheOriginalConsumerBoundary()
    {
        using var document = Reference();
        foreach (var sample in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var props = sample.GetProperty("props"); var table = new PaginationTable(pageIndex: props.TryGetProperty("pageIndex", out var index) ? index.GetInt32() : 0, pageSize: props.TryGetProperty("pageSize", out var size) ? size.GetInt32() : 10);
            foreach (var point in sample.GetProperty("snapshots").EnumerateArray())
            {
                var state = point.GetProperty("state"); var expected = state.GetProperty("table"); var pagination = expected.GetProperty("pagination");
                if (Text(point, "phase") == "committed" && Text(point, "key") != "Escape") table.SetPageSize(pagination.GetProperty("pageSize").GetInt32());
                Assert.AreEqual(pagination.GetProperty("pageIndex").GetInt32(), table.PageIndex); Assert.AreEqual(pagination.GetProperty("pageSize").GetInt32(), table.PageSize); Assert.AreEqual(expected.GetProperty("pageCount").GetInt32(), table.PageCount);
                Assert.AreEqual(state.GetProperty("events").GetArrayLength(), table.Requests.Count); Assert.AreEqual(Text(state, "visibleRows"), table.VisibleText);
                Assert.IsTrue(table.VisibleRows.All(row => table.Rows.Any(original => ReferenceEquals(row, original))));
                if (Text(point, "phase") is "opened" or "popup-tab" or "highlighted") { Assert.IsTrue(state.GetProperty("popupOpen").GetBoolean()); Assert.AreEqual(0, table.Requests.Count); }
            }
        }
    }
    [TestMethod]
    public void SourceTabTraversalSkipsOnlyUnavailableOrResponsiveActions()
    {
        using var document = Reference();
        var expected = new Dictionary<string, string[]> { ["first"] = ["next", "last", "after"], ["middle"] = ["first", "previous", "next", "last", "after"], ["last"] = ["first", "previous", "after"], ["long-end"] = ["after"], ["long-home"] = ["first", "previous", "next", "last", "after"], ["narrow"] = ["next", "after"], ["rtl"] = ["next", "last", "after"], ["cancel"] = ["next", "last", "after"] };
        foreach (var sample in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var points = sample.GetProperty("snapshots").EnumerateArray().ToArray();
            CollectionAssert.AreEqual(expected[Text(sample, "id")], points.Where(point => Text(point, "phase").StartsWith("tab-", StringComparison.Ordinal)).Select(point => Text(point.GetProperty("state"), "focused")).ToArray());
            if (Text(sample, "id") == "first") Assert.AreEqual("combo", Text(points.Single(point => Text(point, "phase") == "reverse-tab").GetProperty("state"), "focused"));
            var committed = points.Single(point => Text(point, "phase") == "committed").GetProperty("state"); Assert.IsFalse(committed.GetProperty("popupOpen").GetBoolean()); Assert.AreEqual("combo", Text(committed, "focused"));
        }
    }
}
