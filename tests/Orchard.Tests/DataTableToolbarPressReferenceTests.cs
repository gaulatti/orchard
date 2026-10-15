using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

// Frozen producer behavior, separately exercised by the native press fixture.
// These source checks alone do not accept the Windows component.
[TestClass]
public sealed class DataTableToolbarPressReferenceTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
    private static string Text(JsonElement item, string name) => item.GetProperty(name).GetString()!;
    private static JsonElement State(JsonElement item, string phase) => item.GetProperty("states").EnumerateArray().Single(point => Text(point.GetProperty("state"), "phase") == phase).GetProperty("state");
    private static int Clicks(JsonElement state) => state.GetProperty("clicks").GetArrayLength();

    [TestMethod]
    public void PressReferencePinsTheOriginalProducerAndTrustedInputs()
    {
        using var original = JsonDocument.Parse(File.ReadAllBytes(Fixture("DataTableToolbarLayout.json")));
        using var document = JsonDocument.Parse(File.ReadAllBytes(Fixture("DataTableToolbarPress.json"))); var root = document.RootElement;
        Assert.AreEqual("orchard-data-table-toolbar-press-v1", Text(root, "format"));
        foreach (var field in new[] { "sourceCommit", "sourceSha256", "compiledCssSha256", "browser" }) Assert.AreEqual(Text(original.RootElement, field), Text(root, field));
        Assert.AreEqual(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Fixture("DataTableToolbarLayout.json")))).ToLowerInvariant(), Text(root, "referenceSha256"));
        Assert.AreEqual(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Fixture("CaptureDataTableToolbarPress.cjs")))).ToLowerInvariant(), Text(root, "scriptSha256"));
        Assert.AreEqual("initial", Text(root, "caret")); Assert.AreEqual("allow", Text(root, "animations")); Assert.AreEqual(0, root.GetProperty("masks").GetInt32()); Assert.AreEqual(0, root.GetProperty("tolerance").GetInt32());
        var cases = root.GetProperty("results").EnumerateArray().ToArray(); Assert.HasCount(24, cases);
        Assert.AreEqual(24, cases.Select(item => (Text(item, "id"), Text(item, "scheme"), Text(item, "reducedMotion"))).Distinct().Count());
        Assert.AreEqual(88, cases.Sum(item => item.GetProperty("states").GetArrayLength()));
        foreach (var item in cases)
        {
            Assert.AreEqual(0, item.GetProperty("errors").GetArrayLength());
            foreach (var receipt in item.GetProperty("receipts").EnumerateArray()) Assert.IsTrue(receipt.GetProperty("trusted").GetBoolean());
            foreach (var point in item.GetProperty("states").EnumerateArray())
            {
                Assert.IsGreaterThanOrEqualTo(3, point.GetProperty("presentation").GetProperty("stable").GetInt32());
                Assert.IsLessThan(2000, point.GetProperty("presentation").GetProperty("milliseconds").GetDouble());
                Assert.AreEqual(64, Text(point, "sha256").Length);
                var table = point.GetProperty("state").GetProperty("table"); Assert.IsTrue(table.GetProperty("sameTable").GetBoolean());
                Assert.AreEqual("Harbor", Text(table.GetProperty("state"), "globalFilter")); Assert.AreEqual(0, table.GetProperty("state").GetProperty("columnFilters").GetArrayLength());
                Assert.AreEqual("Harbor", Text(point.GetProperty("state"), "visibleRows"));
            }
        }
    }

    [TestMethod]
    public void SourcePressTransformHoverPaintAndCancellationRemainIndependent()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Fixture("DataTableToolbarPress.json")));
        foreach (var item in document.RootElement.GetProperty("results").EnumerateArray())
        {
            var id = Text(item, "id"); var held = State(item, "held"); var released = State(item, "released");
            Assert.AreEqual(id == "enter-outside" ? "1" : "0.99", Text(held, "scale"));
            Assert.AreEqual(id == "enter-outside" ? "0px" : "0px 1px", Text(held, "translate"));
            Assert.AreEqual("1", Text(released, "scale")); Assert.AreEqual("0px", Text(released, "translate"));
            Assert.AreEqual(id == "enter-outside" ? 1 : 0, Clicks(held));
            Assert.AreEqual(id is "drag-out" or "space-tab-cancel" ? 0 : 1, Clicks(released));
            if (id is "space-outside" or "enter-outside" or "space-tab-cancel")
            { Assert.AreEqual("rgba(0, 0, 0, 0)", Text(held, "background")); Assert.IsFalse(held.GetProperty("pointerOwnsReset").GetBoolean()); }
            if (id.StartsWith("drag-", StringComparison.Ordinal))
            {
                var outside = State(item, "outside"); Assert.AreEqual("0.99", Text(outside, "scale")); Assert.AreEqual("0px 1px", Text(outside, "translate"));
                Assert.AreEqual("rgba(0, 0, 0, 0)", Text(outside, "background")); Assert.AreEqual(0, Clicks(outside)); Assert.IsTrue(outside.GetProperty("focused").GetBoolean());
            }
            if (id == "space-tab-cancel")
            {
                var blurred = State(item, "blurred"); Assert.AreEqual("1", Text(blurred, "scale")); Assert.AreEqual("filters", Text(blurred, "focusRole")); Assert.AreEqual(0, Clicks(blurred)); Assert.IsFalse(blurred.GetProperty("focused").GetBoolean());
            }
        }
    }
}
