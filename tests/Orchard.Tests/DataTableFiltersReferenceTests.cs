using System.Security.Cryptography;
using System.Text.Json;
using Orchard.Catalog;

namespace Orchard.Tests;

[TestClass]
public sealed class DataTableFiltersReferenceTests
{
    private static string Fixture(string file) => Path.Combine(AppContext.BaseDirectory, "Fixtures", file);
    [TestMethod]
    public void ReferencePinsRealSelectProducerAndCompletePhysicalCaptures()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Fixture("DataTableFilters.json"))); var root = document.RootElement;
        Assert.AreEqual("orchard-data-table-filters-v1", root.GetProperty("format").GetString());
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("sourceCommit").GetString());
        Assert.AreEqual("83a7e5de0d5b0a63d42591e3d45b230243d0ee97d9797a2f9b6b87d4369334bb", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual("0b6113dd13d74b1023208cbf96e080915f3b47a2", root.GetProperty("sourceLockGitBlob").GetString());
        foreach (var (field, file) in new[] { ("scriptSha256", "CaptureDataTableFilters.cjs"), ("clientSha256", "DataTableFiltersClient.tsx") }) Assert.AreEqual(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Fixture(file)))).ToLowerInvariant(), root.GetProperty(field).GetString());
        var cases = root.GetProperty("cases").EnumerateArray().ToArray(); Assert.HasCount(56, cases); Assert.AreEqual(276, cases.Sum(sample => sample.GetProperty("snapshots").GetArrayLength()));
        foreach (var sample in cases)
        foreach (var point in sample.GetProperty("snapshots").EnumerateArray())
        {
            var state = point.GetProperty("state"); var table = state.GetProperty("table");
            Assert.IsTrue(table.GetProperty("sameTable").GetBoolean() && table.GetProperty("sameRows").GetBoolean());
            Assert.IsTrue(table.GetProperty("columns").EnumerateArray().All(item => item.GetProperty("same").GetBoolean()));
            Assert.IsTrue(table.GetProperty("rows").EnumerateArray().All(item => item.GetProperty("same").GetBoolean()));
            Assert.IsTrue(state.GetProperty("inputEvents").EnumerateArray().All(item => item.GetProperty("trusted").GetBoolean()));
            Assert.IsGreaterThanOrEqualTo(3, point.GetProperty("presentation").GetProperty("stable").GetInt32()); Assert.IsLessThan(2000, point.GetProperty("presentation").GetProperty("milliseconds").GetDouble());
            if (state.GetProperty("trigger").ValueKind == JsonValueKind.Null) { Assert.IsFalse(table.GetProperty("columnPresent").GetBoolean()); Assert.IsFalse(state.GetProperty("popupOpen").GetBoolean()); }
            else
            {
                var width = sample.GetProperty("viewportWidth").GetInt32() >= 640 ? 176 : sample.GetProperty("hostWidth").GetDouble();
                Assert.AreEqual(width, state.GetProperty("trigger").GetProperty("box").GetProperty("width").GetDouble()); Assert.AreEqual(40, state.GetProperty("trigger").GetProperty("box").GetProperty("height").GetDouble());
            }
        }
    }
    [TestMethod]
    public void LiveReferencePreservesKeyedFocusAndNeverWritesDuringOptionUpdates()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Fixture("DataTableFiltersLive.json"))); var root = document.RootElement;
        Assert.AreEqual("orchard-data-table-filters-live-v1", root.GetProperty("format").GetString());
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("sourceCommit").GetString());
        Assert.AreEqual("83a7e5de0d5b0a63d42591e3d45b230243d0ee97d9797a2f9b6b87d4369334bb", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Fixture("CaptureDataTableFiltersLive.cjs")))).ToLowerInvariant(), root.GetProperty("scriptSha256").GetString());
        Assert.AreEqual(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Fixture("DataTableFiltersClient.tsx")))).ToLowerInvariant(), root.GetProperty("clientSha256").GetString());
        var cases = root.GetProperty("cases").EnumerateArray().ToArray(); Assert.HasCount(32, cases); Assert.AreEqual(140, cases.Sum(sample => sample.GetProperty("snapshots").GetArrayLength()));
        var retained = 0; var viewport = 0;
        foreach (var sample in cases)
        foreach (var point in sample.GetProperty("snapshots").EnumerateArray())
        {
            var state = point.GetProperty("state");
            Assert.IsTrue(state.GetProperty("inputEvents").EnumerateArray().All(item => item.GetProperty("trusted").GetBoolean()));
            Assert.IsGreaterThanOrEqualTo(3, point.GetProperty("presentation").GetProperty("stable").GetInt32());
            Assert.IsLessThan(2000, point.GetProperty("presentation").GetProperty("milliseconds").GetDouble());
            if (!sample.GetProperty("updates").TryGetProperty(point.GetProperty("phase").GetString()!, out _)) continue;
            Assert.AreEqual(0, state.GetProperty("events").GetArrayLength());
            if (sample.GetProperty("id").GetString() is "remove-focused" or "disable-focused" or "change-focused-value")
            { Assert.AreEqual("listbox", state.GetProperty("focused").GetString()); Assert.IsFalse(point.GetProperty("focusIdentity").GetProperty("retained").GetBoolean()); viewport++; }
            else { Assert.IsTrue(point.GetProperty("focusIdentity").GetProperty("retained").GetBoolean()); retained++; }
            if (sample.GetProperty("id").GetString() == "long-reorder") Assert.AreEqual(400, state.GetProperty("viewport").GetProperty("scrollTop").GetDouble());
        }
        Assert.AreEqual(26, retained); Assert.AreEqual(6, viewport);
    }
    [TestMethod]
    [DataRow("DataTableFilters.json")]
    [DataRow("DataTableFiltersLive.json")]
    public void NativeSelectValuesPreserveSourcePlaceholderAndDuplicatePaint(string referenceFile)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Fixture(referenceFile)));
        foreach (var sample in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var props = sample.GetProperty("props"); var placeholder = props.TryGetProperty("placeholder", out var label) ? label.GetString()! : "All";
            JsonElement? values = props.TryGetProperty("options", out var supplied) ? supplied : null;
            foreach (var point in sample.GetProperty("snapshots").EnumerateArray())
            {
                if (sample.TryGetProperty("updates", out var updates) && updates.TryGetProperty(point.GetProperty("phase").GetString()!, out var update))
                { if (update.TryGetProperty("placeholder", out var nextLabel)) placeholder = nextLabel.GetString()!; if (update.TryGetProperty("options", out var nextOptions)) values = nextOptions; }
                var options = new[] { new BleeckerSelectOption("", placeholder) }.Concat(values is { } choices ? choices.EnumerateArray().Select(option => new BleeckerSelectOption(option.GetProperty("value").GetString()!, option.GetProperty("label").GetString()!, option.TryGetProperty("disabled", out var disabled) && disabled.GetBoolean())) : new[] { new BleeckerSelectOption("Draft", "Draft"), new BleeckerSelectOption("Published", "Published") }).ToArray();
                var state = point.GetProperty("state"); if (!state.GetProperty("table").GetProperty("columnPresent").GetBoolean()) continue;
                var raw = state.GetProperty("table").GetProperty("filterValue"); var value = BleeckerSelectFilterValue.Internal(raw.ValueKind == JsonValueKind.Object ? null : Decode(raw));
                Assert.AreEqual(state.GetProperty("trigger").GetProperty("text").GetString(), BleeckerSelectFilterValue.Display(value, placeholder, options));
                Assert.AreEqual(state.GetProperty("trigger").GetProperty("placeholder").GetBoolean(), value is null);
                if (state.GetProperty("popupOpen").GetBoolean()) Assert.AreEqual(state.GetProperty("options").EnumerateArray().Count(option => option.GetProperty("state").GetString() == "checked"), options.Count(option => BleeckerSelectFilterValue.Matches(value, option)));
            }
        }
    }
    [TestMethod]
    [DataRow("DataTableFilters.json")]
    [DataRow("DataTableFiltersLive.json")]
    public void SourceActionsReachOriginalConsumerFiltersAndVisibleRows(string referenceFile)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Fixture(referenceFile)));
        foreach (var sample in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var props = sample.GetProperty("props");
            var a = new SelectFilterTable(initial: props.TryGetProperty("filters", out var initial) ? Filters(initial) : null);
            var b = new SelectFilterTable(true, props.TryGetProperty("secondFilters", out var second) ? Filters(second) : null); var active = a;
            var rowsA = a.Rows.ToArray(); var rowsB = b.Rows.ToArray(); var columnId = props.TryGetProperty("columnId", out var id) ? id.GetString()! : "status";
            foreach (var point in sample.GetProperty("snapshots").EnumerateArray())
            {
                var phase = point.GetProperty("phase").GetString(); var state = point.GetProperty("state");
                switch (phase)
                {
                    case "replace": active = b; break;
                    case "restore-column": columnId = "status"; break;
                    case "remove-column": columnId = "missing"; break;
                    case "title-column": columnId = "title"; break;
                    case "external-draft": active.SetExternalFilters([new("status", "Draft")]); break;
                    case "external-published": active.SetExternalFilters([new("status", "Published")]); break;
                    case "old-notification": a.SetExternalFilters([new("status", "Published")]); break;
                }
                var events = state.GetProperty("events").EnumerateArray().ToArray();
                foreach (var change in events.Skip(a.Requests.Count + b.Requests.Count))
                {
                    Assert.AreEqual(change.GetProperty("table").GetString(), active.Name);
                    var next = Filters(change.GetProperty("next")).ToDictionary(item => item.Key, item => item.Value);
                    var column = active.GetColumn(columnId)!; column.SetFilterValue(next.GetValueOrDefault(columnId));
                    Assert.AreSame(column, active.Requests[^1].Column);
                }
                Assert.AreEqual(events.Length, a.Requests.Count + b.Requests.Count); Assert.AreEqual(state.GetProperty("visibleRows").GetString(), active.VisibleText);
                Assert.AreEqual(state.GetProperty("table").GetProperty("columnPresent").GetBoolean(), active.GetColumn(columnId) is not null);
                Assert.IsTrue(a.Rows.Zip(rowsA).All(pair => ReferenceEquals(pair.First, pair.Second)) && b.Rows.Zip(rowsB).All(pair => ReferenceEquals(pair.First, pair.Second)));
                var expected = Filters(state.GetProperty("table").GetProperty("filters")).ToArray(); Assert.AreEqual(expected.Length, active.Filters.Count);
                foreach (var pair in expected) Assert.AreEqual(JsonSerializer.Serialize(pair.Value), JsonSerializer.Serialize(active.Filters[pair.Key]));
            }
        }
    }
    private static IEnumerable<KeyValuePair<string, object?>> Filters(JsonElement value) => value.EnumerateArray().Select(item => new KeyValuePair<string, object?>(item.GetProperty("id").GetString()!, Decode(item.GetProperty("value"))));
    private static object? Decode(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => null, JsonValueKind.String => value.GetString(), JsonValueKind.True => true, JsonValueKind.False => false,
        JsonValueKind.Number => value.GetDouble(), JsonValueKind.Array => value.EnumerateArray().Select(Decode).ToArray(),
        _ => throw new InvalidOperationException("Unknown frozen filter value descriptor.")
    };
}
