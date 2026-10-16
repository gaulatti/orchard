using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class LoadingOverlayReferenceTests
{
    [TestMethod]
    public void FrozenPortalReferenceKeepsVisibilityLayoutAndActualInputSemantics()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LoadingOverlay.json")));
        var root = document.RootElement;
        Assert.AreEqual("9b4721d0cb08b99284bf0b59dc16ba2448cf8b065a926882f37b1099cea033da", root.GetProperty("sourceSha256").GetString());
        var script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CaptureLoadingOverlay.cjs")).Replace("\r\n", "\n");
        Assert.AreEqual(root.GetProperty("scriptSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(script))));
        Assert.AreEqual(60, root.GetProperty("cases").GetArrayLength());
        foreach (var test in root.GetProperty("cases").EnumerateArray())
        {
            var id = test.GetProperty("id").GetString(); var overlays = test.GetProperty("normal").GetProperty("overlays");
            Assert.AreEqual(id == "hidden" ? 0 : id == "stacked" ? 2 : 1, overlays.GetArrayLength());
            foreach (var overlay in overlays.EnumerateArray())
            {
                Assert.AreEqual(0, overlay.GetProperty("box").GetProperty("x").GetDouble()); Assert.AreEqual(0, overlay.GetProperty("box").GetProperty("y").GetDouble());
                Assert.AreEqual(test.GetProperty("width").GetDouble(), overlay.GetProperty("box").GetProperty("width").GetDouble());
                Assert.AreEqual(test.GetProperty("height").GetDouble(), overlay.GetProperty("box").GetProperty("height").GetDouble());
                Assert.AreEqual("blur(2px)", overlay.GetProperty("blur").GetString());
                Assert.AreEqual(48, overlay.GetProperty("spinner").GetProperty("box").GetProperty("width").GetDouble());
                var label = overlay.GetProperty("label");
                if (id is "empty" or "null") Assert.AreEqual(JsonValueKind.Null, label.ValueKind);
                if (id == "space") { Assert.AreEqual(110, overlay.GetProperty("card").GetProperty("box").GetProperty("height").GetDouble()); Assert.AreEqual(0, label.GetProperty("box").GetProperty("height").GetDouble()); }
                if (id == "unbroken" && test.GetProperty("width").GetInt32() == 320) Assert.IsTrue(overlay.GetProperty("card").GetProperty("box").GetProperty("x").GetDouble() < 0);
            }
        }
        Assert.AreEqual(6, root.GetProperty("interactions").GetArrayLength());
        foreach (var interaction in root.GetProperty("interactions").EnumerateArray())
        {
            Assert.AreEqual("underlying", interaction.GetProperty("focusOnOpen").GetString());
            Assert.AreEqual(2, interaction.GetProperty("keyboardActivations").GetInt32());
            Assert.IsTrue(interaction.GetProperty("pointerBlocked").GetBoolean()); Assert.IsTrue(interaction.GetProperty("escapeDoesNotDismiss").GetBoolean());
            Assert.AreEqual("BODY", interaction.GetProperty("focusAfterPointer").GetProperty("tag").GetString());
            Assert.AreEqual("underlying", interaction.GetProperty("focusAfterTab").GetString()); Assert.AreEqual("underlying", interaction.GetProperty("focusAfterHide").GetString());
        }
    }
}
