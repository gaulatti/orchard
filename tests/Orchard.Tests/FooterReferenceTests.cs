using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class FooterReferenceTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllBytes(Fixture("Footer.json")));

    [TestMethod]
    public void FrozenFooterBindsSourceCaptureAndEveryFullClientImage()
    {
        using var reference = Reference(); var root = reference.RootElement;
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("commit").GetString());
        Assert.AreEqual("77756e4546713842cf511b7521fe1a5b61f39d378797d37a4b77511486f7d082", root.GetProperty("sourceSha256").GetString());
        foreach (var (file, field) in new[] { ("CaptureFooter.cjs", "scriptSha256"), ("FooterClient.tsx", "clientSha256") }) Assert.AreEqual(root.GetProperty(field).GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));
        Assert.AreEqual(64, root.GetProperty("cases").GetArrayLength());
        foreach (var row in root.GetProperty("cases").EnumerateArray()) Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture("Footer/" + row.GetProperty("file").GetString())))));
        Assert.AreEqual(4, root.GetProperty("interactions").GetArrayLength());
        foreach (var history in root.GetProperty("interactions").EnumerateArray())
        {
            var samples = history.GetProperty("samples"); Assert.AreEqual(4, samples.GetArrayLength());
            foreach (var sample in samples.EnumerateArray().Take(2)) { Assert.AreEqual("Caller text", sample.GetProperty("input").GetString()); Assert.IsTrue(sample.GetProperty("inputFocused").GetBoolean()); CollectionAssert.AreEqual(new[] { 2, 5 }, sample.GetProperty("selection").EnumerateArray().Select(value => value.GetInt32()).ToArray()); }
            if (history.GetProperty("custom").GetBoolean()) Assert.IsTrue(samples[2].GetProperty("requests")[0].GetProperty("original").GetBoolean());
        }
    }

    [TestMethod]
    public void ExpandedFooterCoversLongEmptyMultirowAndLiveMutationEndpoints()
    {
        using var reference = Reference(); var cases = reference.RootElement.GetProperty("cases").EnumerateArray().ToArray();
        foreach (var id in new[] { "long-links-320", "long-links-768", "empty-values", "long-description-320", "long-bottom-768", "six-sections", "unicode-narrow", "mutable-layout" })
            CollectionAssert.AreEquivalent(new[] { "Light", "Dark" }, cases.Where(row => row.GetProperty("id").GetString() == id).Select(row => row.GetProperty("scheme").GetString()).ToArray());
        foreach (var row in cases.Where(row => row.GetProperty("id").GetString() == "mutable-layout"))
        {
            var initial = row.GetProperty("initialSpec"); var final = row.GetProperty("spec");
            Assert.AreEqual(4, final.GetProperty("mutations").GetArrayLength());
            Assert.AreNotEqual(initial.GetProperty("brand").GetProperty("description").GetString(), final.GetProperty("brand").GetProperty("description").GetString());
            Assert.AreEqual("An updated long fictional resources heading", final.GetProperty("sections")[0].GetProperty("title").GetString());
            Assert.AreEqual(initial.GetProperty("sections")[1].GetProperty("items")[1].GetProperty("marker").GetString(), final.GetProperty("sections")[0].GetProperty("items")[1].GetProperty("marker").GetString());
        }
    }

    [TestMethod]
    public void OriginalFooterDistinguishesNullishContentAndViewportBreakpoints()
    {
        using var reference = Reference(); var rows = reference.RootElement.GetProperty("cases").EnumerateArray().Where(row => row.GetProperty("scheme").GetString() == "Light").ToDictionary(row => row.GetProperty("id").GetString()!);
        string Right(string id) => rows[id].GetProperty("measurement").GetProperty("bottomRight").GetProperty("text").GetString()!;
        Assert.AreEqual("View source on GitHub", Right("wide")); Assert.AreEqual(Right("wide"), Right("null-right"));
        Assert.IsTrue(BleeckerFooterModel.DefaultSource(null)); Assert.IsFalse(BleeckerFooterModel.DefaultSource("")); Assert.IsFalse(BleeckerFooterModel.DefaultSource(false)); Assert.IsFalse(BleeckerFooterModel.DefaultSource(0));
        Assert.AreEqual("", Right("empty-right")); Assert.AreEqual("", Right("false-right")); Assert.AreEqual("0", Right("zero-right")); Assert.AreEqual("", Right("zero-sections"));
        string Paint(string id, string node, string field) => rows[id].GetProperty("measurement").GetProperty("paint").GetProperty(node).GetProperty(field).GetString()!;
        Assert.AreEqual("column", Paint("viewport-767", "bottom", "flexDirection")); Assert.AreEqual("row", Paint("viewport-768", "bottom", "flexDirection"));
        Assert.AreEqual(1, Paint("viewport-767", "grid", "gridTemplateColumns").Split(' ').Length); Assert.AreEqual(2, Paint("viewport-768", "grid", "gridTemplateColumns").Split(' ').Length);
        Assert.AreEqual(2, Paint("viewport-1023", "grid", "gridTemplateColumns").Split(' ').Length); Assert.AreEqual(4, Paint("viewport-1024", "grid", "gridTemplateColumns").Split(' ').Length);
        Assert.AreEqual(4, Paint("constrained-host", "grid", "gridTemplateColumns").Split(' ').Length);
        Assert.AreEqual("span 2 / span 2", Paint("one-section", "brand", "gridColumn")); Assert.AreEqual("auto", Paint("three-sections", "brand", "gridColumn"));
        Assert.AreEqual(JsonValueKind.Null, rows["no-accent"].GetProperty("measurement").GetProperty("accent").ValueKind);
        foreach (var row in rows.Values)
        {
            var spec = row.GetProperty("spec"); var viewport = spec.GetProperty("viewportWidth").GetDouble(); var measurement = row.GetProperty("measurement");
            Assert.AreEqual(measurement.GetProperty("container").GetProperty("width").GetDouble(), BleeckerFooterModel.Container(viewport, measurement.GetProperty("host").GetProperty("width").GetDouble()), .001);
            Assert.AreEqual(measurement.GetProperty("paint").GetProperty("grid").GetProperty("gridTemplateColumns").GetString()!.Split(' ').Length, BleeckerFooterModel.Columns(viewport));
            Assert.AreEqual(measurement.GetProperty("paint").GetProperty("brand").GetProperty("gridColumn").GetString() == "span 2 / span 2" ? 2 : 1, BleeckerFooterModel.BrandSpan(viewport, spec.GetProperty("sections").GetArrayLength()));
        }
    }
}
