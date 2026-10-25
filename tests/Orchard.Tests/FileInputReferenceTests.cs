using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class FileInputReferenceTests
{
    private static string Fixture(string file) => Path.Combine(AppContext.BaseDirectory, "Fixtures", file);
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllBytes(Fixture("FileInput.json")));
    private static JsonElement History(JsonElement root, string id, string theme) => root.GetProperty("histories").EnumerateArray().Single(h => h.GetProperty("id").GetString() == id && h.GetProperty("scheme").GetString() == theme);
    private static JsonElement Step(JsonElement history, string label) => history.GetProperty("steps").EnumerateArray().Single(s => s.GetProperty("label").GetString() == label).GetProperty("value");
    private static void Count(JsonElement value, int files, int events)
    {
        Assert.AreEqual(files, value.GetProperty("input").GetProperty("files").GetArrayLength());
        Assert.AreEqual(events, value.GetProperty("events").GetArrayLength());
    }

    [TestMethod]
    public void FrozenSourceIncludesUnmodifiedInputsCssAndEverySettledScreenshot()
    {
        var bytes = File.ReadAllBytes(Fixture("FileInput.json"));
        Assert.AreEqual("bc824df8aef0b200cc74bc96a1aac183fe8e75e028f171f8c6abee633b2c7b8b", Convert.ToHexStringLower(SHA256.HashData(bytes)));
        using var reference = JsonDocument.Parse(bytes); var r = reference.RootElement;
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", r.GetProperty("commit").GetString());
        Assert.AreEqual(68, r.GetProperty("cases").GetArrayLength()); Assert.AreEqual(22, r.GetProperty("histories").GetArrayLength());
        Assert.AreEqual(122, r.GetProperty("cssInputs").GetArrayLength()); Assert.AreEqual(15, r.GetProperty("bundledFiles").GetArrayLength());
        foreach (var (file, field) in new[] { ("CaptureFileInput.cjs", "scriptSha256"), ("FileInputClient.tsx", "clientSha256"), ("FileInput/source.css", "cssSha256"), ("FileInput/bundle.js", "bundleSha256") })
            Assert.AreEqual(r.GetProperty(field).GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));
        foreach (var field in new[] { "dependencies", "cssInputs" })
            foreach (var input in r.GetProperty(field).EnumerateArray())
                Assert.AreEqual(input.GetProperty("sha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture("FileInput/Source/" + input.GetProperty("path").GetString())))));
        foreach (var c in r.GetProperty("cases").EnumerateArray())
        {
            Assert.AreEqual(2, c.GetProperty("stableFrames").GetInt32());
            Assert.AreEqual(c.GetProperty("pngSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture("FileInput/" + c.GetProperty("file").GetString())))));
        }
        Assert.IsFalse(r.GetProperty("nativeSystemDialogTested").GetBoolean()); Assert.IsFalse(r.GetProperty("nativeImplemented").GetBoolean());
    }

    [TestMethod]
    public void BrowserChooserResultsKeepOriginalFilenamesBytesAndCallerCallbacks()
    {
        using var reference = Reference();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var h = History(reference.RootElement, "choose-replace-clear", theme);
            var selected = Step(h, "selected"); Count(selected, 1, 2);
            var original = selected.GetProperty("input").GetProperty("files")[0];
            Assert.AreEqual("fictional-note.txt", original.GetProperty("name").GetString()); Assert.AreEqual("Original fictional bytes", original.GetProperty("text").GetString());
            Assert.AreEqual(24, original.GetProperty("size").GetInt32()); Assert.AreEqual("text/plain", original.GetProperty("type").GetString());
            foreach (var e in selected.GetProperty("events").EnumerateArray())
            {
                Assert.IsTrue(e.GetProperty("sameRef").GetBoolean()); Assert.IsFalse(e.GetProperty("trusted").GetBoolean());
                Assert.AreEqual(original.GetProperty("lastModified").GetInt64(), e.GetProperty("files")[0].GetProperty("lastModified").GetInt64());
            }
            var replaced = Step(h, "replaced"); Count(replaced, 1, 4);
            Assert.AreEqual("#EXTM3U", replaced.GetProperty("input").GetProperty("files")[0].GetProperty("text").GetString());
        }
    }

    [TestMethod]
    public void ProgrammaticClearAndFormResetDoNotInventNativeChangeEvents()
    {
        using var reference = Reference();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            Count(Step(History(reference.RootElement, "choose-replace-clear", theme), "cleared-programmatically"), 0, 4);
            Count(Step(History(reference.RootElement, "form-reset", theme), "reset"), 0, 2);
            Count(Step(History(reference.RootElement, "selection-cleared-with-files-api", theme), "empty"), 0, 4);
            var rejected = History(reference.RootElement, "invalid-nonempty-value", theme).GetProperty("assignment");
            Assert.IsTrue(rejected.GetProperty("threw").GetBoolean()); Assert.AreEqual("InvalidStateError", rejected.GetProperty("name").GetString());
        }
    }

    [TestMethod]
    public void LivePropertiesRetainOriginalInputAndSelectedFileWithoutTruncation()
    {
        using var reference = Reference();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var h = History(reference.RootElement, "live-props-retain-selection", theme);
            Assert.IsTrue(h.GetProperty("sameNode").GetBoolean()); Assert.IsTrue(h.GetProperty("sameFile").GetBoolean()); Count(Step(h, "updated"), 1, 2);
            var multiple = History(reference.RootElement, "multiple-selection-live-single", theme);
            Count(Step(multiple, "multiple"), 2, 2); var single = Step(multiple, "now-single-keeps-files"); Count(single, 2, 2);
            Assert.IsFalse(single.GetProperty("input").GetProperty("multiple").GetBoolean());
            var remount = History(reference.RootElement, "unmount-remount", theme); Assert.IsFalse(remount.GetProperty("sameNode").GetBoolean());
            Assert.AreEqual(JsonValueKind.Null, Step(remount, "unmounted").GetProperty("input").ValueKind); Count(Step(remount, "remounted"), 0, 2);
        }
    }

    [TestMethod]
    public void RequiredValidityAndDisabledTabBehaviorCrossTheOriginalNativeInput()
    {
        using var reference = Reference();
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var validity = History(reference.RootElement, "required-validity", theme);
            foreach (var label in new[] { "missing", "present", "missing-again" })
            {
                var input = Step(validity, label).GetProperty("input"); var hasFile = label == "present";
                Assert.AreEqual(hasFile, input.GetProperty("valid").GetBoolean()); Assert.AreEqual(!hasFile, input.GetProperty("valueMissing").GetBoolean());
            }
            var tab = History(reference.RootElement, "tab-order-disabled", theme);
            Assert.AreEqual("original-file", Step(tab, "enabled").GetProperty("focus").GetString()); Assert.AreEqual("after", Step(tab, "disabled-skipped").GetProperty("focus").GetString());
        }
    }
}
