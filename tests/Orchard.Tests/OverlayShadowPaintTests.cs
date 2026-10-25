using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class OverlayShadowPaintTests
{
    [TestMethod]
    public void EncodedTokenColorMatchesIndependentUnblurredSourcePaint()
    {
        using var reference = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","OverlayShadowPaint.json")));
        var source = reference.RootElement;
        Assert.AreEqual("155.0.4283.62",source.GetProperty("browser").GetString());
        Assert.AreEqual(8,source.GetProperty("retainedSourceRecaptures").GetArrayLength());
        foreach (var sample in source.GetProperty("paintSamples").EnumerateArray())
        {
            var opacity = sample.GetProperty("layer").GetString() == "far" ? .28f : .06f;
            var actual = BleeckerShadowPaint.EncodeColor(opacity,26,55,77);
            var premul = new[]{actual.X,actual.Y,actual.Z}.Select(c=>(int)MathF.Round(c*actual.W*255,MidpointRounding.AwayFromZero)).Append((int)MathF.Round(actual.W*255,MidpointRounding.AwayFromZero)).ToArray();
            CollectionAssert.AreEqual(sample.GetProperty("premulRgba").EnumerateArray().Select(v=>v.GetInt32()).ToArray(),premul);
            Assert.IsTrue(sample.GetProperty("fullCoveragePixels").GetInt32()>=512);
        }
    }

    [TestMethod]
    public void SourcePaintPngsAndCaptureProducerRemainFrozen()
    {
        using var reference = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","OverlayShadowPaint.json")));
        var source = reference.RootElement;
        foreach(var c in source.GetProperty("cases").EnumerateArray())
        {
            var name=c.GetProperty("file").GetString()!;
            Assert.AreEqual(Path.GetFileName(name),name);
            Assert.AreEqual(c.GetProperty("sha256").GetString(),Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","OverlayShadowPaint",name)))));
        }
        Assert.AreEqual(source.GetProperty("scriptSha256").GetString(),Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","CaptureOverlayShadowPaint.mjs")))));
    }

    [TestMethod]
    public void TransparentPaintIsFiniteAndInvalidOpacityIsRejected()
    {
        Assert.AreEqual(Vector4.Zero,BleeckerShadowPaint.EncodeColor(0,26,55,77));
        foreach(var opacity in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-.01f,1.01f})
            Assert.Throws<ArgumentOutOfRangeException>(()=>BleeckerShadowPaint.EncodeColor(opacity,26,55,77));
    }
}
