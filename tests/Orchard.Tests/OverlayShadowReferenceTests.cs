using System.Security.Cryptography;
using System.Text.Json;
namespace Orchard.Tests;

[TestClass]
public sealed class OverlayShadowReferenceTests
{
    [TestMethod]
    public void FrozenOuterShadowIsIndependentOfBackgroundAlphaAndKind()
    {
        using var reference=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","OverlayShadow.json")));var r=reference.RootElement;
        Assert.AreEqual("orchard-overlay-shadow-source-v1",r.GetProperty("format").GetString());Assert.AreEqual("155.0.4283.62",r.GetProperty("browser").GetString());Assert.AreEqual("12b264b46566b389eff73d6b21e7f32feb465cdc52f2c86ab8379555a49a075e",r.GetProperty("referenceSha256").GetString());
        var cases=r.GetProperty("cases").EnumerateArray().ToArray();Assert.HasCount(16,cases);
        foreach(var theme in new[]{"Light","Dark"})
        {
            var rows=cases.Where(c=>c.GetProperty("theme").GetString()==theme).ToArray();Assert.HasCount(8,rows);var outside=rows[0].GetProperty("outside").EnumerateArray().Select(v=>v.GetInt32()).ToArray();
            foreach(var c in rows){Assert.AreEqual(0,c.GetProperty("outsideDifferencesFromOpaque").GetInt32());CollectionAssert.AreEqual(outside,c.GetProperty("outside").EnumerateArray().Select(v=>v.GetInt32()).ToArray());if(c.GetProperty("id").GetString() is "transparent" or "null")CollectionAssert.AreEqual(theme=="Light"?new[]{255,255,255,255}:new[]{13,24,33,255},c.GetProperty("inside").EnumerateArray().Select(v=>v.GetInt32()).ToArray());}
        }
    }
    [TestMethod]
    public void FrozenShadowPngBytesRemainUnchanged()
    {
        using var reference=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","OverlayShadow.json")));
        foreach(var c in reference.RootElement.GetProperty("cases").EnumerateArray()){var name=c.GetProperty("file").GetString()!;Assert.AreEqual(Path.GetFileName(name),name);var bytes=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","OverlayShadow",name));Assert.AreEqual(c.GetProperty("sha256").GetString(),Convert.ToHexStringLower(SHA256.HashData(bytes)));Assert.AreEqual(2,c.GetProperty("stableFrames").GetInt32());}
    }
}
