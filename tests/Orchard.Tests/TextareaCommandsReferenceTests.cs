using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class TextareaCommandsReferenceTests
{
    [TestMethod]
    public void FrozenCommandsRetainTrustedClipboardAndUndoRedoContract()
    {
        using var document=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","TextareaCommands.json")));
        var root=document.RootElement;
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066",root.GetProperty("sourceCommit").GetString());
        Assert.AreEqual("b523ba0962a02492a5e5954957eede4e1ad683ee7ce7ac21c04097325eb82082",root.GetProperty("sourceSha256").GetString());
        var script=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","CaptureTextareaCommands.cjs")).Replace("\r\n","\n");
        Assert.AreEqual(root.GetProperty("scriptSha256").GetString(),Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(script))));
        Assert.IsTrue(root.GetProperty("realClipboard").GetBoolean());var cases=root.GetProperty("cases").EnumerateArray().ToArray();Assert.AreEqual(36,cases.Length);
        Assert.AreEqual(36,cases.Select(c=>c.GetProperty("file").GetString()).Distinct().Count());
        foreach(var frame in cases)
        {
            var id=frame.GetProperty("id").GetString()!;var actual=frame.GetProperty("actual");Assert.AreEqual(id is "cut" or "redo"?"":"First\nSecond",actual.GetProperty("text").GetString());
            Assert.AreEqual(id.StartsWith("readonly-",StringComparison.Ordinal),actual.GetProperty("readOnly").GetBoolean());
            Assert.IsTrue(frame.GetProperty("events").EnumerateArray().All(e=>e.GetProperty("trusted").GetBoolean()));
            if(id is "copy" or "cut" or "readonly-copy")
            {
                var clipboard=frame.GetProperty("clipboard");Assert.AreEqual("First\nSecond",clipboard.GetProperty("normalizedText").GetString());
                CollectionAssert.AreEqual(new[]{"text/plain"},clipboard.GetProperty("types").EnumerateArray().Select(t=>t.GetString()!).ToArray());
            }
        }
    }
}
