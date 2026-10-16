using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class SparklinePointerReferenceTests
{
    [TestMethod]
    public void RealHoverAndExitPreserveEveryOriginalKeyboardExpectation()
    {
        byte[] Fixture(string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures",name));
        string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var originalBytes=Fixture("SparklineLayout.json");using var original=JsonDocument.Parse(originalBytes);using var reference=JsonDocument.Parse(Fixture("SparklinePointer.json"));var root=reference.RootElement;
        Assert.AreEqual("orchard-sparkline-pointer-v1",root.GetProperty("format").GetString());
        Assert.AreEqual("bcb1a8b4f58995000a4a415a3d907dd6d8d77474856af14759a65d8d80b86ac5",Hash(originalBytes));
        Assert.AreEqual(Hash(originalBytes),root.GetProperty("originalReferenceSha256").GetString());
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066",root.GetProperty("sourceCommit").GetString());
        Assert.AreEqual("7e143cfb557045bfad591be820c28b3bcb29e77469261f51723317f4ffef0ed5",root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(Hash(Fixture("CaptureSparklinePointer.cjs")),root.GetProperty("scriptSha256").GetString());
        Assert.AreEqual("19.2.4",root.GetProperty("react").GetString());Assert.AreEqual("3.8.1",root.GetProperty("recharts").GetString());
        var cases=root.GetProperty("cases").EnumerateArray().ToArray();Assert.HasCount(36,cases);
        foreach(var sample in cases)
        {
            var prior=original.RootElement.GetProperty("cases").EnumerateArray().Single(item=>item.GetProperty("id").GetString()==sample.GetProperty("id").GetString()&&item.GetProperty("scheme").GetString()==sample.GetProperty("scheme").GetString());
            foreach(var phase in new[]{"focus","right"})
            {
                Assert.IsTrue(JsonElement.DeepEquals(prior.GetProperty(phase).GetProperty("dots"),sample.GetProperty(phase).GetProperty("dots")));
                foreach(var field in new[]{"visible","text","box"})Assert.IsTrue(JsonElement.DeepEquals(prior.GetProperty(phase).GetProperty("tooltip").GetProperty(field),sample.GetProperty(phase).GetProperty("tooltip").GetProperty(field)));
            }
            Assert.AreEqual("hidden",sample.GetProperty("exit").GetProperty("tooltip").GetProperty("visible").GetString());Assert.AreEqual("",sample.GetProperty("exit").GetProperty("tooltip").GetProperty("text").GetString());Assert.AreEqual(0,sample.GetProperty("exit").GetProperty("dots").GetArrayLength());
            var events=sample.GetProperty("events").EnumerateArray().ToArray();Assert.IsTrue(events.All(item=>item.GetProperty("trusted").GetBoolean()));
            var moves=events.Where(item=>item.GetProperty("type").GetString()=="pointermove").ToArray();Assert.HasCount(2,moves);
            Assert.AreEqual(20+sample.GetProperty("point").GetProperty("x").GetDouble(),moves[0].GetProperty("x").GetDouble());Assert.AreEqual(20+sample.GetProperty("point").GetProperty("y").GetDouble(),moves[0].GetProperty("y").GetDouble());
            Assert.AreEqual(100,moves[1].GetProperty("x").GetDouble());Assert.AreEqual(56+prior.GetProperty("normal").GetProperty("height").GetDouble(),moves[1].GetProperty("y").GetDouble());
            Assert.HasCount(1,events.Where(item=>item.GetProperty("type").GetString()=="keydown"&&item.GetProperty("key").GetString()=="ArrowRight").ToArray());Assert.HasCount(1,events.Where(item=>item.GetProperty("type").GetString()=="keyup"&&item.GetProperty("key").GetString()=="ArrowRight").ToArray());
        }
    }
}
