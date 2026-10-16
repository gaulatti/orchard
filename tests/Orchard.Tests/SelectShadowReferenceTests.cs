using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class SelectShadowReferenceTests
{
    [TestMethod]
    public void RenderedShadowOutsideClickRemainsModalAndPreservesOriginalConsumer()
    {
        string Fixture(string file) => Path.Combine(AppContext.BaseDirectory,"Fixtures",file);
        using var document = JsonDocument.Parse(File.ReadAllBytes(Fixture("SelectShadowPointer.json"))); var root = document.RootElement;
        Assert.AreEqual("orchard-select-shadow-pointer-v1",root.GetProperty("format").GetString());
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066",root.GetProperty("sourceCommit").GetString());
        Assert.AreEqual("83a7e5de0d5b0a63d42591e3d45b230243d0ee97d9797a2f9b6b87d4369334bb",root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Fixture("CaptureSelectShadowPointer.cjs")))).ToLowerInvariant(),root.GetProperty("scriptSha256").GetString());
        var cases = root.GetProperty("cases").EnumerateArray().ToArray(); Assert.HasCount(2,cases);
        foreach (var sample in cases)
        {
            var points = sample.GetProperty("snapshots").EnumerateArray().ToArray(); Assert.HasCount(3,points);
            var state = points[^1].GetProperty("state"); Assert.AreEqual("outside",points[^1].GetProperty("phase").GetString());
            Assert.IsFalse(state.GetProperty("popupOpen").GetBoolean()); Assert.AreEqual("trigger",state.GetProperty("focused").GetString()); Assert.AreEqual(0,state.GetProperty("outsideClicks").GetInt32()); Assert.AreEqual(0,state.GetProperty("events").GetArrayLength());
            var events = state.GetProperty("shadowPointerEvents").EnumerateArray().ToArray(); Assert.HasCount(4,events);
            CollectionAssert.AreEqual(new[]{"pointermove","pointerdown","pointerup","click"},events.Select(item=>item.GetProperty("type").GetString()!).ToArray());
            CollectionAssert.AreEqual(new[]{"HTML","HTML","BUTTON","HTML"},events.Select(item=>item.GetProperty("tag").GetString()!).ToArray());
            foreach (var item in events) { Assert.IsTrue(item.GetProperty("trusted").GetBoolean()); Assert.AreEqual(200,item.GetProperty("x").GetDouble()); Assert.AreEqual(186,item.GetProperty("y").GetDouble()); }
            foreach (var point in points) { var table=point.GetProperty("state").GetProperty("table"); Assert.IsTrue(table.GetProperty("sameTable").GetBoolean()&&table.GetProperty("sameRows").GetBoolean()); }
        }
    }
}
