using System.Text.Json;
using System.Security.Cryptography;

namespace Orchard.Tests;
[TestClass]
public sealed class RadioGroupContractTests
{
    [TestMethod]
    public void SourceRequestsPreserveConsumerControlInBothThemesAndWidths()
    {
        using var reference=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","RadioGroup.json")));var root=reference.RootElement;
        Assert.AreEqual("1af7eb200200683a11a1cae62291f462a711f89bf0a7bfaf67d9f7d2f1fb33fb",root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(56,root.GetProperty("cases").GetArrayLength());Assert.AreEqual(8,root.GetProperty("interactions").GetArrayLength());
        foreach(var sequence in root.GetProperty("interactions").EnumerateArray())
        {
            Assert.AreEqual("weekly",sequence.GetProperty("requests")[0].GetString());Assert.AreEqual(1,sequence.GetProperty("requests").GetArrayLength());
            var selected=sequence.GetProperty("measurement").GetProperty("options").EnumerateArray().Single(option=>option.GetProperty("checked").GetBoolean());
            Assert.AreEqual(sequence.GetProperty("accept").GetBoolean()?"weekly":"daily",selected.GetProperty("value").GetString());
        }
    }
    [TestMethod]
    public void ValuesAreOrdinalUniqueAndEmptyValuesRemainSupported()
    {
        BleeckerRadioModel.Validate(new[]{new BleeckerRadioOption("","Empty"),new BleeckerRadioOption("A","Upper"),new BleeckerRadioOption("a","Lower",Disabled:true)});
        Assert.ThrowsExactly<ArgumentException>(()=>BleeckerRadioModel.Validate(new[]{new BleeckerRadioOption("a","First"),new BleeckerRadioOption("a","Duplicate")}));
        Assert.ThrowsExactly<ArgumentNullException>(()=>new BleeckerRadioOption(null!,"Invalid"));
        Assert.ThrowsExactly<ArgumentNullException>(()=>BleeckerRadioModel.Validate(new BleeckerRadioOption[]{null!}));
    }
    [TestMethod]
    public void CollectorAndClientMatchFrozenProvenance()
    {
        using var source=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","RadioGroup.json")));
        foreach(var (name,field)in new[]{("CaptureRadioGroup.cjs","scriptSha256"),("RadioGroupClient.tsx","clientSha256")})
            Assert.AreEqual(source.RootElement.GetProperty(field).GetString(),Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures",name)))).ToLowerInvariant());
    }
}
