using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class TextareaLimitsReferenceTests
{
    [TestMethod]
    public void NativeFinalParagraphDoesNotIncreaseUserInsertionCapacity()
    {
        Assert.AreEqual("abc",BleeckerTextareaInsertion.Prepare("abcdef","ABCDE",0,6,3));
        Assert.AreEqual("",BleeckerTextareaInsertion.Prepare("x","ABCDE",5,6,5));
    }

    [TestMethod]
    public void ClipboardInsertionMatchesTheFrozenUserBoundary()
    {
        using var document=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","TextareaLimits.json")));
        foreach(var frame in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec=frame.GetProperty("spec");
            if(!spec.TryGetProperty("paste",out var paste)||spec.TryGetProperty("readOnly",out var readOnly)&&readOnly.GetBoolean())continue;
            var initial=spec.GetProperty("initial").GetString()!;
            var selection=spec.TryGetProperty("selection",out var range)?range.EnumerateArray().Select(v=>v.GetInt32()).ToArray():new[]{initial.Length,initial.Length};
            var limit=spec.TryGetProperty("liveLimit",out var live)?live.GetInt32():spec.GetProperty("limit").ValueKind==JsonValueKind.Null?0:spec.GetProperty("limit").GetInt32();
            var insertion=BleeckerTextareaInsertion.Prepare(paste.GetString()!,initial,selection[0],selection[1],limit);
            var result=initial[..selection[0]]+insertion+initial[selection[1]..];
            Assert.AreEqual(frame.GetProperty("actual").GetProperty("text").GetString(),result,frame.GetProperty("id").GetString());
        }
    }

    [TestMethod]
    public void FrozenLengthCasesUseRealClipboardAndTrustedKeysWithPinnedProvenance()
    {
        using var document=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","TextareaLimits.json")));
        var root=document.RootElement;
        Assert.AreEqual("b523ba0962a02492a5e5954957eede4e1ad683ee7ce7ac21c04097325eb82082",root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066",root.GetProperty("sourceCommit").GetString());
        var script=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","CaptureTextareaLimits.cjs")).Replace("\r\n","\n");
        Assert.AreEqual(root.GetProperty("scriptSha256").GetString(),Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(script))));
        Assert.IsTrue(root.GetProperty("realClipboard").GetBoolean());Assert.IsTrue(root.GetProperty("freshMountPerCase").GetBoolean());
        var cases=root.GetProperty("cases").EnumerateArray().ToArray();Assert.AreEqual(56,cases.Length);
        Assert.AreEqual(56,cases.Select(c=>c.GetProperty("file").GetString()).Distinct().Count());
        var expected=new Dictionary<string,string>{["typing"]="abcde",["newline"]="ab\ncd",["paste"]="abcde",["paste-crlf"]="ab\ncd",
            ["surrogate-fit"]="A😀",["surrogate-boundary"]="A",["selection"]="AuvwE",["full"]="abc",["programmatic-overflow"]="ABCDE",
            ["overflow-replacement"]="uvw",["zero-capacity-selection"]="ABC",["live-limit"]="abc",["unlimited"]="Unlimited\nnotes 😀",["readonly"]="Read-only"};
        foreach(var frame in cases)
        {
            var id=frame.GetProperty("id").GetString()!;var actual=frame.GetProperty("actual");var text=actual.GetProperty("text").GetString()!;
            Assert.AreEqual(expected[id],text,id);Assert.AreEqual(text.Length,actual.GetProperty("length").GetInt32());
            Assert.IsNull(actual.GetProperty("ariaInvalid").GetString(),"HTML constraint validity does not implicitly set aria-invalid.");
            var events=frame.GetProperty("events").EnumerateArray().ToArray();Assert.IsTrue(events.All(e=>e.GetProperty("trusted").GetBoolean()));
            if(frame.GetProperty("spec").TryGetProperty("paste",out _))
            {
                Assert.IsTrue(events.Any(e=>e.GetProperty("type").GetString()=="paste"));
                Assert.AreEqual(frame.GetProperty("spec").GetProperty("initial").GetString(),frame.GetProperty("undo").GetString(),id+": paste must undo as one edit, and a blocked paste must not create an edit.");
            }
        }
    }
}
