using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class TextareaReferenceTests
{
    [TestMethod]
    public void TrustedResizeRetainsDisabledReadonlyDirectionMinimumAndPointerAnchor()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "TextareaResize.json")));
        var root = document.RootElement;
        Assert.AreEqual("b523ba0962a02492a5e5954957eede4e1ad683ee7ce7ac21c04097325eb82082", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("sourceCommit").GetString());
        var script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CaptureTextareaResize.cjs")).Replace("\r\n", "\n");
        Assert.AreEqual(root.GetProperty("scriptSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(script))));
        var cases = root.GetProperty("cases").EnumerateArray().ToArray();
        Assert.AreEqual(72, cases.Length);
        Assert.AreEqual(72, cases.Select(c => c.GetProperty("file").GetString()).Distinct().Count());
        Assert.AreEqual(24, cases.Count(c => c.GetProperty("mode").GetString() == "disabled"));
        Assert.AreEqual(24, cases.Count(c => c.GetProperty("mode").GetString() == "readonly"));
        foreach (var frame in cases)
        {
            var initial = frame.GetProperty("initial"); var enlarged = frame.GetProperty("enlargedHeld");
            Assert.AreEqual(initial.GetProperty("height").GetDouble() + 48, enlarged.GetProperty("height").GetDouble());
            foreach (var state in new[] { "initial", "enlargedHeld", "minimumHeld", "reverseAboveMinimum" })
            {
                var actual = frame.GetProperty(state);
                Assert.AreEqual(initial.GetProperty("width").GetDouble(), actual.GetProperty("width").GetDouble());
                Assert.AreEqual(initial.GetProperty("value").GetString(), actual.GetProperty("value").GetString());
                Assert.AreEqual(frame.GetProperty("dir").GetString(), actual.GetProperty("direction").GetString());
                Assert.IsFalse(actual.GetProperty("focused").GetBoolean());
                Assert.AreEqual(frame.GetProperty("mode").GetString() == "disabled", actual.GetProperty("disabled").GetBoolean());
                Assert.AreEqual(frame.GetProperty("mode").GetString() == "readonly", actual.GetProperty("readOnly").GetBoolean());
            }
            Assert.AreEqual(initial.GetProperty("minHeight").GetDouble(), frame.GetProperty("minimumHeld").GetProperty("height").GetDouble());
            Assert.AreEqual(initial.GetProperty("minHeight").GetDouble(), frame.GetProperty("reverseAboveMinimum").GetProperty("height").GetDouble());
            Assert.IsTrue(frame.GetProperty("events").EnumerateArray().All(e => e.GetProperty("trusted").GetBoolean()));
        }
    }

    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Textarea.json")));

    [TestMethod]
    public void FrozenTextareaReferenceRetainsSourceAndCollectorProvenance()
    {
        using var document = Reference();
        var root = document.RootElement;
        Assert.AreEqual("b523ba0962a02492a5e5954957eede4e1ad683ee7ce7ac21c04097325eb82082", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("sourceCommit").GetString());
        var script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CaptureTextarea.cjs")).Replace("\r\n", "\n");
        Assert.AreEqual(root.GetProperty("scriptSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(script))));
        Assert.IsTrue(root.GetProperty("freshMountPerState").GetBoolean());
        Assert.IsTrue(root.GetProperty("animationsFinishedForEndpointMeasurements").GetBoolean());
        Assert.AreEqual(172, root.GetProperty("cases").GetArrayLength());
        Assert.AreEqual(172, root.GetProperty("cases").EnumerateArray().Select(c => c.GetProperty("file").GetString()).Distinct().Count());
        foreach (var frame in root.GetProperty("cases").EnumerateArray())
            Assert.AreEqual(64, frame.GetProperty("pngSha256").GetString()!.Length);
    }

    [TestMethod]
    public void ActualTextareaRowsAndCascadeOverrideLiteralRecipeAssumptions()
    {
        using var document = Reference();
        foreach (var frame in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var id = frame.GetProperty("id").GetString();
            var paint = frame.GetProperty("paint");
            var expectedHeight = id switch { "small" => 98d, "large" => 128d, "rows-ten" => 226d, "small-rows-one" => 96d, "large-rows-ten" => 266d, _ => 112d };
            Assert.AreEqual(expectedHeight, frame.GetProperty("box").GetProperty("height").GetDouble(), id);
            Assert.AreEqual(frame.GetProperty("width").GetDouble() - 40, frame.GetProperty("box").GetProperty("width").GetDouble());
            Assert.AreEqual(id is "large" or "large-rows-ten" ? "24px" : "20px", paint.GetProperty("lineHeight").GetString());
            Assert.AreEqual("vertical", paint.GetProperty("resize").GetString());
            Assert.AreEqual(id is "disabled" or "disabled-error" ? "0.6" : "1", paint.GetProperty("opacity").GetString());
            Assert.AreEqual(id == "error-aria-override" ? "false" : id is "error" or "disabled-error" ? "true" : null, frame.GetProperty("ariaInvalid").GetString());
            if (frame.GetProperty("scheme").GetString() == "Dark" && id is not ("disabled" or "disabled-error"))
            {
                Assert.AreEqual("rgb(24, 37, 51)", paint.GetProperty("backgroundColor").GetString(), "Resolve the actual deep-sea token in the active theme.");
                if (frame.GetProperty("state").GetString() == "focus")
                    Assert.AreEqual("rgb(91, 163, 245)", paint.GetProperty("borderColor").GetString(), "The dark focus selector wins even for error=true.");
            }
        }
    }

    [TestMethod]
    public void TrustedBrowserInputProvesMultilineUndoReadonlySelectionAndVerticalResize()
    {
        using var document = Reference();
        var interactions = document.RootElement.GetProperty("interactions");
        Assert.AreEqual(4, interactions.GetArrayLength());
        foreach (var interaction in interactions.EnumerateArray())
        {
            Assert.IsTrue(interaction.GetProperty("multilineEditing").GetBoolean());
            Assert.IsTrue(interaction.GetProperty("undoRestoresBothLines").GetBoolean());
            Assert.IsTrue(interaction.GetProperty("readOnlyRejectsEditing").GetBoolean());
            var selection = interaction.GetProperty("readonlySelection");
            Assert.IsTrue(selection.GetProperty("end").GetInt32() > selection.GetProperty("start").GetInt32());
            var resize = interaction.GetProperty("resize");
            Assert.AreEqual(resize.GetProperty("before").GetProperty("width").GetDouble(), resize.GetProperty("after").GetProperty("width").GetDouble());
            Assert.AreEqual(112d, resize.GetProperty("minimum").GetProperty("height").GetDouble());
            Assert.IsTrue(resize.GetProperty("after").GetProperty("height").GetDouble() >= 152);
            var events = interaction.GetProperty("inputEvents").EnumerateArray().ToArray();
            Assert.IsTrue(events.Length > 40 && events.All(e => e.GetProperty("trusted").GetBoolean()));
            Assert.IsTrue(events.Any(e => e.GetProperty("key").GetString() == "Enter"));
            Assert.IsTrue(events.Any(e => e.GetProperty("type").GetString() == "input"));
        }
    }
}
