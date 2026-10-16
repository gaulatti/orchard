using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class PiePointerReferenceTests
{
    [TestMethod]
    public void HoverExitPreservesEveryOriginalCircularKeyboardGate()
    {
        byte[] Fixture(string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures",name));
        string Hash(byte[] b)=>Convert.ToHexString(SHA256.HashData(b)).ToLowerInvariant();
        var oldBytes=Fixture("PieChartLayout.json");using var original=JsonDocument.Parse(oldBytes);using var reference=JsonDocument.Parse(Fixture("PiePointer.json"));var root=reference.RootElement;
        Assert.AreEqual("orchard-pie-pointer-v1",root.GetProperty("format").GetString());Assert.AreEqual("ddefde419f8215d401a8c5f15a054b88682ca18cddf8ab19bad6b40a53a65ce7",Hash(oldBytes));Assert.AreEqual(Hash(oldBytes),root.GetProperty("originalReferenceSha256").GetString());Assert.AreEqual(Hash(Fixture("CapturePiePointer.cjs")),root.GetProperty("scriptSha256").GetString());
        foreach(var field in new[]{"sourceCommit","sourceSha256","donutSha256","react","recharts"})Assert.AreEqual(original.RootElement.GetProperty(field).GetString(),root.GetProperty(field).GetString());
        var cases=root.GetProperty("cases").EnumerateArray().ToArray();Assert.HasCount(84,cases);
        foreach(var sample in cases)
        {
            var old=original.RootElement.GetProperty("cases").EnumerateArray().Single(item=>item.GetProperty("id").GetString()==sample.GetProperty("id").GetString()&&item.GetProperty("scheme").GetString()==sample.GetProperty("scheme").GetString()&&item.GetProperty("props").GetProperty("mode").GetString()==sample.GetProperty("mode").GetString());
            foreach(var phase in new[]{"focus","right","left","enter"})foreach(var field in new[]{"tooltip","tooltipVisible"})Assert.IsTrue(JsonElement.DeepEquals(old.GetProperty(phase).GetProperty(field),sample.GetProperty(phase).GetProperty(field)));
            Assert.AreEqual("",sample.GetProperty("exit").GetProperty("tooltip").GetString());Assert.IsFalse(sample.GetProperty("exit").GetProperty("tooltipVisible").GetBoolean());
            var events=sample.GetProperty("events").EnumerateArray().ToArray();Assert.IsTrue(events.All(item=>item.GetProperty("trusted").GetBoolean()));var moves=events.Where(item=>item.GetProperty("type").GetString()=="pointermove").ToArray();Assert.HasCount(2,moves);
            foreach(var (move,point)in moves.Zip(new[]{sample.GetProperty("point"),sample.GetProperty("outside").GetProperty("point")})){Assert.AreEqual(20+point.GetProperty("x").GetDouble(),move.GetProperty("x").GetDouble());Assert.AreEqual(20+point.GetProperty("y").GetDouble(),move.GetProperty("y").GetDouble());}
            CollectionAssert.AreEqual(new[]{"ArrowRight","ArrowLeft","Enter"},events.Where(item=>item.GetProperty("type").GetString()=="keydown").Select(item=>item.GetProperty("key").GetString()!).ToArray());CollectionAssert.AreEqual(new[]{"ArrowRight","ArrowLeft","Enter"},events.Where(item=>item.GetProperty("type").GetString()=="keyup").Select(item=>item.GetProperty("key").GetString()!).ToArray());
        }
    }
}
