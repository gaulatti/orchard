using System.Security.Cryptography;
using System.Text.Json;
using Orchard.Catalog;

namespace Orchard.Tests;

[TestClass]
public sealed class DataTablePaginationReferenceTests
{
    private static string FilePath(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
    private static string Text(JsonElement value, string name) => value.GetProperty(name).GetString()!;
    private static int Number(JsonElement value, string name, int initial) => value.TryGetProperty(name, out var item) ? item.GetInt32() : initial;
    private static IEnumerable<JsonElement> Nodes(JsonElement node) { yield return node; if (node.TryGetProperty("children", out var children)) foreach (var child in children.EnumerateArray()) foreach (var next in Nodes(child)) yield return next; }
    [TestMethod]
    public void PaginationReferencePinsTheRealProducerAndDependencies()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(FilePath("DataTablePagination.json"))); var root = document.RootElement;
        Assert.AreEqual("orchard-data-table-pagination-v1", Text(root, "format")); Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", Text(root, "sourceCommit"));
        Assert.AreEqual("385a8ae19a8b052be43ae29e7c43b3662ac35ccf892f20c4ef1fc0b893d10897", Text(root, "sourceSha256"));
        foreach (var (field, file) in new[] { ("captureScriptSha256", "CaptureDataTablePagination.cjs"), ("clientSha256", "DataTablePaginationClient.tsx"), ("casesScriptSha256", "DataTablePaginationCases.cjs") })
            Assert.AreEqual(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(FilePath(file)))).ToLowerInvariant(), Text(root, field));
        var cases = root.GetProperty("cases").EnumerateArray().ToArray(); Assert.HasCount(52, cases); Assert.AreEqual(132, cases.Sum(item => item.GetProperty("snapshots").GetArrayLength()));
        foreach (var (name, version) in new[] { ("react", "19.2.4"), ("@tanstack/react-table", "8.21.3"), ("@radix-ui/react-select", "2.3.7") })
            Assert.AreEqual(1, root.GetProperty("bundledRuntime").EnumerateArray().Count(item => Text(item, "name") == name && Text(item, "version") == version));
        foreach (var sample in cases) foreach (var point in sample.GetProperty("snapshots").EnumerateArray())
        {
            Assert.IsGreaterThanOrEqualTo(3, point.GetProperty("presentation").GetProperty("stable").GetInt32()); Assert.IsLessThan(2000, point.GetProperty("presentation").GetProperty("milliseconds").GetDouble());
            var state = point.GetProperty("state"); foreach (var receipt in state.GetProperty("inputEvents").EnumerateArray()) Assert.IsTrue(receipt.GetProperty("trusted").GetBoolean());
            var table = state.GetProperty("table"); Assert.IsTrue(table.GetProperty("sameTable").GetBoolean() && table.GetProperty("sameRows").GetBoolean());
            foreach (var row in table.GetProperty("rows").EnumerateArray()) Assert.IsTrue(row.GetProperty("same").GetBoolean());
        }
    }
    [TestMethod]
    public void OriginalConsumerPagingFilteringSortingAndReplacementMatchEverySourceState()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(FilePath("DataTablePagination.json")));
        foreach (var sample in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var props = sample.GetProperty("props"); var a = new PaginationTable(Number(props, "rows", 47), Number(props, "pageIndex", 0), Number(props, "pageSize", 10), props.TryGetProperty("manualPageCount", out var manual) ? manual.GetInt32() : null, props.TryGetProperty("prefix", out var prefix) ? prefix.GetString()! : "Coverage ");
            var b = new PaginationTable(8, pageSize: 2, prefix: "Desk ", idPrefix: "desk-"); var active = a;
            if (props.TryGetProperty("selection", out var selection)) a.SetSelection(selection.EnumerateObject().Where(item => item.Value.GetBoolean()).Select(item => item.Name));
            if (props.TryGetProperty("filters", out var filters) && filters.GetArrayLength() > 0) a.SetFilter(true);
            foreach (var point in sample.GetProperty("snapshots").EnumerateArray())
            {
                var action = Text(point, "action");
                switch (action)
                {
                    case "first": active.SetPageIndex(0); break; case "previous": active.PreviousPage(); break; case "next": active.NextPage(); break; case "last": active.SetPageIndex(active.PageCount - 1); break;
                    case "sort-desc": active.SetSort(true); break; case "filter-draft": active.SetFilter(true); break; case "clear-filter": active.SetFilter(false); break;
                    case "external-page": active.SetPagination(2, 7); break; case "external-selection": active.SetSelection(["row-003", "row-040"]); break;
                    case "replace-table": active = b; break; case "stale-table": a.SetPageIndex(1); break; case "restore-table": active = a; break;
                    case "keyboard-size-20": active.SetPageSize(20); break; case "keyboard-last": active.SetPageSize(500); break;
                    default: if (action.StartsWith("size-", StringComparison.Ordinal)) active.SetPageSize(int.Parse(action[5..], System.Globalization.CultureInfo.InvariantCulture)); break;
                }
                var expected = point.GetProperty("state").GetProperty("table"); Assert.AreEqual(ReferenceEquals(active, a) ? "A" : "B", Text(expected, "name"));
                Assert.AreEqual(expected.GetProperty("pagination").GetProperty("pageIndex").GetInt32(), active.PageIndex, Text(sample, "id") + "/" + action);
                Assert.AreEqual(expected.GetProperty("pagination").GetProperty("pageSize").GetInt32(), active.PageSize); Assert.AreEqual(expected.GetProperty("pageCount").GetInt32(), active.PageCount);
                Assert.AreEqual(expected.GetProperty("filteredCount").GetInt32(), active.FilteredRowCount); Assert.AreEqual(expected.GetProperty("selectedCount").GetInt32(), active.FilteredSelectedRowCount);
                Assert.AreEqual(expected.GetProperty("canPrevious").GetBoolean(), active.CanPreviousPage); Assert.AreEqual(expected.GetProperty("canNext").GetBoolean(), active.CanNextPage);
                CollectionAssert.AreEqual(expected.GetProperty("rows").EnumerateArray().Select(item => Text(item, "id")).ToArray(), active.VisibleRows.Select(row => row.Id).ToArray());
                Assert.IsTrue(active.VisibleRows.All(row => active.Rows.Any(original => ReferenceEquals(original, row))));
            }
        }
    }
    [TestMethod]
    public void SourceViewportBreakpointAndEmptyManualCountsRemainExplicit()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(FilePath("DataTablePagination.json"))); var root = document.RootElement;
        foreach (var sample in root.GetProperty("cases").EnumerateArray())
        {
            var normal = sample.GetProperty("snapshots")[0].GetProperty("state"); var surface = normal.GetProperty("surface"); var wide = sample.GetProperty("viewportWidth").GetInt32() >= 640;
            Assert.AreEqual(wide ? "row" : "column", Text(root.GetProperty("styles")[surface.GetProperty("styleIndex").GetInt32()], "flexDirection"));
            var buttons = Nodes(surface).Where(item => Text(item, "tag") == "BUTTON" && (!item.GetProperty("attributes").TryGetProperty("role", out var role) || role.GetString() != "combobox")).ToArray(); Assert.HasCount(4, buttons);
            Assert.AreEqual(wide ? "flex" : "none", Text(root.GetProperty("styles")[buttons[0].GetProperty("styleIndex").GetInt32()], "display"));
            if (Text(sample, "id") == "empty") StringAssert.Contains(Text(surface, "text"), "Page 1 of 0");
            if (Text(sample, "id") == "manual-unknown") { StringAssert.Contains(Text(surface, "text"), "Page 4 of -1"); Assert.IsTrue(normal.GetProperty("table").GetProperty("canNext").GetBoolean()); }
        }
    }
}
