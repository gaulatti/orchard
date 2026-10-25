using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class FieldContractTests
{
    private static JsonDocument Read() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Field.json")));
    [TestMethod]
    public void ReferenceRetainsThePinnedSourceAndEveryOriginalPng()
    {
        using var document = Read(); var root = document.RootElement;
        Assert.AreEqual("552df290321007582f3605c1bc4538ad5664ed267dee1bd22cb45bac203ec11f", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(44, root.GetProperty("cases").GetArrayLength());
        foreach (var (name, field) in new[] { ("CaptureField.cjs", "scriptSha256"), ("FieldClient.tsx", "clientSha256") })
            Assert.AreEqual(root.GetProperty(field).GetString(), Hash(Path.Combine(AppContext.BaseDirectory, "Fixtures", name)));
        foreach (var row in root.GetProperty("cases").EnumerateArray())
            Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Hash(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Field", row.GetProperty("file").GetString()!)));
    }
    [TestMethod]
    public void RichSpecimenUsesTheBrowsersActualResolvedPinnedFace()
    {
        using var document = Read(); var rows = document.RootElement.GetProperty("cases").EnumerateArray().Where(row => row.GetProperty("id").GetString() == "rich-label").ToArray();
        Assert.AreEqual(2, rows.Length);
        foreach (var row in rows)
        {
            var fonts = row.GetProperty("strongFonts"); Assert.AreEqual(1, fonts.GetArrayLength());
            Assert.AreEqual("EncodeSans-SmBold", fonts[0].GetProperty("postScriptName").GetString());
            Assert.IsTrue(fonts[0].GetProperty("isCustomFont").GetBoolean()); Assert.AreEqual(7, fonts[0].GetProperty("glyphCount").GetInt32());
        }
        Assert.IsTrue(document.RootElement.GetProperty("fonts").EnumerateArray().Any(font => font.GetProperty("name").GetString() == "EncodeSans-SemiBold"));
    }
    [TestMethod]
    public void FlowMatchesEveryFrozenHeaderControlAndParagraphPosition()
    {
        using var document = Read();
        foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var m = row.GetProperty("measurement"); var root = m.GetProperty("root").GetProperty("box"); var origin = root.GetProperty("y").GetDouble();
            var description = m.GetProperty("description"); var error = m.GetProperty("error");
            static double Height(JsonElement node) => node.ValueKind == JsonValueKind.Null ? 0 : node.GetProperty("box").GetProperty("height").GetDouble();
            var flow = BleeckerFieldModel.Place(Height(m.GetProperty("header")), Height(m.GetProperty("control")), description.ValueKind != JsonValueKind.Null, Height(description), error.ValueKind != JsonValueKind.Null, Height(error));
            Assert.AreEqual(m.GetProperty("control").GetProperty("box").GetProperty("y").GetDouble() - origin, flow.ControlY);
            if (description.ValueKind != JsonValueKind.Null) Assert.AreEqual(description.GetProperty("box").GetProperty("y").GetDouble() - origin, flow.DescriptionY);
            if (error.ValueKind != JsonValueKind.Null) Assert.AreEqual(error.GetProperty("box").GetProperty("y").GetDouble() - origin, flow.ErrorY);
            Assert.AreEqual(root.GetProperty("height").GetDouble(), flow.Height, row.GetProperty("id").GetString());
        }
    }
    [TestMethod]
    public void LiveSourceKeepsTheChildAndItsValueWhileMessagesChange()
    {
        using var document = Read();
        foreach (var row in document.RootElement.GetProperty("interactions").EnumerateArray())
        {
            Assert.IsTrue(row.GetProperty("labelFocused").GetBoolean()); Assert.IsTrue(row.GetProperty("identityRetained").GetBoolean());
            var initial = row.GetProperty("initial");
            foreach (var phase in new[] { "added", "removedError", "removedMessages" })
            {
                var state = row.GetProperty(phase); Assert.AreEqual(initial.GetProperty("controlId").GetString(), state.GetProperty("controlId").GetString());
                Assert.AreEqual("Consumer edit", state.GetProperty("value").GetString()); Assert.IsFalse(state.GetProperty("nativeRequired").GetBoolean());
            }
            CollectionAssert.AreEqual(new[] { "external-help", "description", "error" }, Strings(row.GetProperty("added").GetProperty("describedBy")));
            Assert.AreEqual("true", row.GetProperty("added").GetProperty("invalid").GetString());
            Assert.AreEqual("false", row.GetProperty("removedError").GetProperty("invalid").GetString());
            CollectionAssert.AreEqual(new[] { "external-help" }, Strings(row.GetProperty("removedMessages").GetProperty("describedBy")));
            CollectionAssert.AreEqual(new[] { "Consumer edit", "action" }, Strings(row.GetProperty("events")));
        }
    }
    [TestMethod]
    public void ScalarPresencePreservesReactNodeBoundaries()
    {
        foreach (var value in new object?[] { null, false, "", 0, -0d, double.NaN }) Assert.IsFalse(BleeckerFieldModel.Present(value));
        foreach (var value in new object[] { true, " ", 1, -1, double.PositiveInfinity }) Assert.IsTrue(BleeckerFieldModel.Present(value));
        Assert.IsNull(BleeckerFieldModel.Display(false)); Assert.IsNull(BleeckerFieldModel.Display(true));
        Assert.AreEqual("0", BleeckerFieldModel.Display(-0d)); Assert.AreEqual("1e+21", BleeckerFieldModel.Display(1e21));
    }
    private static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static string[] Strings(JsonElement array) => array.EnumerateArray().Select(item => item.GetString()!).ToArray();
}
