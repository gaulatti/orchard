using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class SearchInputReferenceTests
{
    [TestMethod]
    public void FrozenBrowserKeepsOptionalClearAndNativeSearchEscapeContract()
    {
        using var document=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","SearchInput.json")));var root=document.RootElement;
        Assert.AreEqual("0a9e5eafdbafdeffdb2e60859ecd5cd1eea4af3598eac712e03f5a6d01a2a038",root.GetProperty("sourceSha256").GetString());
        var script=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","CaptureSearchInput.cjs")).Replace("\r\n","\n");
        Assert.AreEqual(root.GetProperty("scriptSha256").GetString(),Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(script))));
        Assert.AreEqual(40,root.GetProperty("cases").GetArrayLength());
        Assert.IsTrue(root.GetProperty("freshMountPerCase").GetBoolean());
        Assert.AreEqual("01bce586edd4c1ad4aed30415b5aa9cbc6e620a874e4751cf7299bfa54ccc192",root.GetProperty("inputSourceSha256").GetString());
        foreach(var test in root.GetProperty("cases").EnumerateArray())
        {
            var id=test.GetProperty("id").GetString();var normal=test.GetProperty("normal");
            if(id is "empty" or "no-callback")Assert.AreEqual(JsonValueKind.Null,normal.GetProperty("clear").ValueKind);
            else{Assert.AreEqual(18,normal.GetProperty("clear").GetProperty("box").GetProperty("width").GetDouble());Assert.IsFalse(normal.GetProperty("clear").GetProperty("disabled").GetBoolean());}
            Assert.AreEqual(16,normal.GetProperty("search").GetProperty("width").GetDouble());
            Assert.AreEqual("32px",normal.GetProperty("editor").GetProperty("paint").GetProperty("padding")[1].GetString());
            Assert.AreEqual("40px",normal.GetProperty("editor").GetProperty("paint").GetProperty("padding")[3].GetString());
            Assert.AreEqual(id=="disabled"?"0.6":"1",normal.GetProperty("editor").GetProperty("paint").GetProperty("opacity").GetString(),"Paused transitions must not retain a prior case's enabled opacity.");
        }
        Assert.AreEqual(4,root.GetProperty("interactions").GetArrayLength());
        foreach(var input in root.GetProperty("interactions").EnumerateArray())
        {
            Assert.IsTrue(input.GetProperty("clearRetainsConsumerText").GetBoolean());Assert.IsTrue(input.GetProperty("disabledEditorAllowsClear").GetBoolean());Assert.IsTrue(input.GetProperty("readOnlyEditorAllowsClear").GetBoolean());
            var escape=input.GetProperty("escape");Assert.AreEqual("Culture",escape.GetProperty("value").GetString());Assert.AreEqual(1,escape.GetProperty("actions").GetArrayLength());Assert.AreEqual("change",escape.GetProperty("actions")[0].GetProperty("kind").GetString());Assert.AreEqual("",escape.GetProperty("actions")[0].GetProperty("text").GetString());
        }
    }
}
