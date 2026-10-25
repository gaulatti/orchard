using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Orchard.Tests;

[TestClass]
public sealed class TableContractTests
{
    [TestMethod]
    public void CallerComparerCannotChangeSourceKeysOrDefaultPropertyLookup()
    {
        var owner = new HashSet<string>(new[] { "CEDAR", "i", "é" }, StringComparer.OrdinalIgnoreCase);
        var model = new BleeckerTableState(); var rows = new object[] { Row("cedar", "Lower", 1, ""), Row("Cedar", "Title", 2, ""), Row("i", "Latin", 3, ""), Row("ı", "Dotless", 4, ""), Row("é", "Composed", 5, ""), Row("e\u0301", "Decomposed", 6, "") };
        var projected = BleeckerTableState.Project(rows, (row, _) => Id(row), model.Selection(owner));
        CollectionAssert.AreEqual(new[] { "i", "é" }, projected.Where(row => row.IsSelected).Select(row => row.Key).ToArray());
        var requested = model.RequestRow(model.Selection(owner), "cedar", true);
        CollectionAssert.AreEqual(new[] { "CEDAR", "i", "é", "cedar" }, requested.ToArray());
        CollectionAssert.AreEqual(new[] { "CEDAR", "i", "é" }, owner.ToArray());
        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["NAME"] = "Upper-only" };
        Assert.IsNull(BleeckerTableState.CellValue(dictionary, new("name", "Name")));
        Assert.AreEqual("Upper-only", BleeckerTableState.CellValue(dictionary, new("NAME", "Name")));
        Assert.AreEqual("Upper-only", BleeckerTableState.CellValue(dictionary, new("computed", "Computed", valueSelector: row => ((Dictionary<string, object?>)row)["name"])));
    }
    private static object Row(string id, string name, object? count, string status) => new Dictionary<string, object?> { ["id"] = id, ["name"] = name, ["count"] = count, ["status"] = status };
    private static object[] Rows() => [Row("cedar", "Cedar bulletin", 12, "Queued"), Row("birch", "Birch update", 3, "Ready"), Row("ash", "Ash brief", 25, "Draft")];
    private static object[] Mixed() => [Row("null", "Null count", null, "Unset"), Row("boolean", "Boolean count", false, "False"), Row("array", "Array count", new[] { "2", "3" }, "Array"), Row("object", "Object count", new object(), "Object")];
    private static string Id(object row) => (string)BleeckerTableState.Value(row, "id")!;

    [TestMethod]
    [DataRow("internal-sort")]
    [DataRow("controlled-sort-reject-release")]
    [DataRow("controlled-sort-accept")]
    [DataRow("internal-selection")]
    [DataRow("controlled-selection-reject-release")]
    [DataRow("controlled-selection-accept")]
    [DataRow("collection-update")]
    [DataRow("custom-compare")]
    [DataRow("coercion-sort")]
    [DataRow("index-keys")]
    public void NativeStateReplaysTheActualSourceRequestsAndVisibleProjection(string action)
    {
        var prior = CultureInfo.CurrentCulture; CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        try
        {
            using var source = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Table.json")));
            foreach (var history in source.RootElement.GetProperty("histories").EnumerateArray().Where(h => h.GetProperty("action").GetString() == action))
            {
                IReadOnlyList<object> rows = action == "coercion-sort" ? Mixed() : Rows();
                var state = new BleeckerTableState();
                BleeckerTableSort? controlledSort = action.StartsWith("controlled-sort", StringComparison.Ordinal) ? new("count", BleeckerTableSortOrder.Descending) : null;
                IReadOnlySet<string>? controlledSelection = action.StartsWith("controlled-selection", StringComparison.Ordinal) ? new BleeckerTableSelection(["outside"]) : null;
                var columns = new BleeckerTableColumn[] { new("name", "Fictional item", comparison: action == "custom-compare" ? (a, b) => ((string)BleeckerTableState.Value(b, "name")!).Length.CompareTo(((string)BleeckerTableState.Value(a, "name")!).Length) : null), new("count", "Count"), new("status", "Status", isSortable: false) };
                var events = new List<object>();
                IReadOnlyList<object> Sorted() => BleeckerTableState.Sorted(rows, columns, state.Sort(controlledSort));
                IReadOnlyList<BleeckerTableProjectedRow> Project() => BleeckerTableState.Project(Sorted(), (row, index) => action == "index-keys" ? index.ToString(CultureInfo.InvariantCulture) : Id(row), state.Selection(controlledSelection));
                void Sort(string field)
                {
                    var requested = state.RequestSort(field, state.Sort(controlledSort));
                    events.Add(new { kind = "sort", value = new { field = requested.Field, order = requested.Order == BleeckerTableSortOrder.Ascending ? "asc" : "desc" } });
                    if (action == "controlled-sort-accept") controlledSort = requested;
                }
                void SelectRow(int index)
                {
                    var row = Project()[index]; var requested = state.RequestRow(state.Selection(controlledSelection), row.Key, !row.IsSelected);
                    events.Add(new { kind = "selection", value = requested.ToArray() });
                    if (action == "controlled-selection-accept") controlledSelection = requested;
                }
                void SelectAll()
                {
                    var keys = Project().Select(r => r.Key).ToArray(); var flags = BleeckerTableState.SelectionFlags(keys, state.Selection(controlledSelection));
                    var requested = state.RequestAll(keys, !flags.All); events.Add(new { kind = "selection", value = requested.ToArray() });
                    if (action == "controlled-selection-accept") controlledSelection = requested;
                }
                foreach (var snapshot in history.GetProperty("snapshots").EnumerateArray())
                {
                    var stage = snapshot.GetProperty("stage").GetString();
                    switch (stage)
                    {
                        case "initial": break;
                        case "name-asc": case "name-desc": case "custom-asc": case "custom-desc": Sort("name"); break;
                        case "count-asc": case "count-desc": case "accepted-asc": case "accepted-desc": case "coercion-asc": case "coercion-desc": case "sort-rebinds-index-key": Sort("count"); break;
                        case "rejected-request": if (action.StartsWith("controlled-sort", StringComparison.Ordinal)) Sort("count"); else SelectRow(0); break;
                        case "released-control": if (action.StartsWith("controlled-sort", StringComparison.Ordinal)) controlledSort = null; else controlledSelection = null; break;
                        case "one-selected": case "accepted-one": case "first-index-selected": SelectRow(0); break;
                        case "all-selected": case "none-selected": case "accepted-all": case "all-replaces-outside-key": SelectAll(); break;
                        case "accepted-row-off": SelectRow(1); break;
                        case "original-records-replaced": rows = [Row("cedar", "Cedar revised", 1, "Ready"), Row("ash", "Ash revised", 2, "Queued")]; break;
                        default: Assert.Fail("Unimplemented source transition: " + stage); break;
                    }
                    var actual = Project(); var expected = snapshot.GetProperty("rows").EnumerateArray().ToArray();
                    CollectionAssert.AreEqual(expected.Select(r => r.GetProperty("id").GetString()).ToArray(), actual.Select(r => Id(r.Item)).ToArray(), stage);
                    CollectionAssert.AreEqual(expected.Where(r => r.GetProperty("selected").GetString() == "true").Select(r => r.GetProperty("id").GetString()).ToArray(), actual.Where(r => r.IsSelected).Select(r => Id(r.Item)).ToArray(), stage);
                    CollectionAssert.AreEqual(expected.Select(r => r.GetProperty("index").GetString()).ToArray(), actual.Select(r => r.Index.ToString(CultureInfo.InvariantCulture)).ToArray(), stage);
                    Assert.AreEqual(snapshot.GetProperty("footerOrder").GetString(), string.Join(',', Sorted().Select(Id)), stage);
                    foreach (var projected in actual) Assert.IsTrue(rows.Any(original => ReferenceEquals(original, projected.Item)), "Projection replaced an original row.");
                    Assert.IsTrue(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(events), JsonNode.Parse(snapshot.GetProperty("events").GetRawText())), stage + ": original callback requests differ.");
                    var checks = snapshot.GetProperty("checkboxes").EnumerateArray().ToArray(); var flags = BleeckerTableState.SelectionFlags(actual.Select(r => r.Key), state.Selection(controlledSelection));
                    Assert.AreEqual(checks[0].GetProperty("checked").GetBoolean(), flags.All); Assert.AreEqual(checks[0].GetProperty("indeterminate").GetBoolean(), flags.Some);
                }
            }
        }
        finally { CultureInfo.CurrentCulture = prior; }
    }

    [TestMethod]
    public void SourceMixedCellTextAndEmptySelectionAreRetained()
    {
        using var source = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Table.json")));
        var cells = source.RootElement.GetProperty("cases").EnumerateArray().First(c => c.GetProperty("scheme").GetString() == "Light" && c.GetProperty("name").GetString() == "mixed-values").GetProperty("snapshot").GetProperty("body").EnumerateArray().Where((_, index) => index % 4 == 2).Select(c => c.GetProperty("text").GetString()).ToArray();
        CollectionAssert.AreEqual(cells, Mixed().Select(row => BleeckerTableState.CellText(BleeckerTableState.Value(row, "count"))).ToArray());
        Assert.AreEqual((false, false), BleeckerTableState.SelectionFlags([], new BleeckerTableSelection(["outside"])));
    }

    [TestMethod]
    public void UnsortedRowsAndTypedAccessorResultsStayOriginalAndTiesAreStable()
    {
        var rows = Rows(); Assert.AreSame(rows, BleeckerTableState.Sorted(rows, [], new("")));
        var columns = new[] { new BleeckerTableColumn("computed", "Computed", valueSelector: _ => 1) };
        var sorted = BleeckerTableState.Sorted(rows, columns, new("computed", BleeckerTableSortOrder.Descending));
        for (var index = 0; index < rows.Length; index++) Assert.AreSame(rows[index], sorted[index]);
        var original = new HashSet<string>(["outside"], StringComparer.Ordinal); var state = new BleeckerTableState();
        var requested = state.RequestRow(original, "cedar", true); Assert.IsFalse(original.Contains("cedar"));
        CollectionAssert.AreEqual(new[] { "outside", "cedar" }, requested.ToArray());
        Assert.AreEqual(0, BleeckerTableState.DefaultCompare(double.NaN, 1));
    }
}
