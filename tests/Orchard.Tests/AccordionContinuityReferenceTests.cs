using System.Security.Cryptography;
using System.Text.Json;
namespace Orchard.Tests;
[TestClass]
public sealed class AccordionContinuityReferenceTests
{
    private static JsonDocument Reference()=>JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","AccordionContinuity.json")));
    [TestMethod]
    public void SourceCollectionChangesRetainOrResetTheMeasuredRevealTrajectory()
    {
        using var document=Reference();var cases=document.RootElement.GetProperty("lifecycle");Assert.AreEqual(32,cases.GetArrayLength());var samples=0;var retained=0;var reset=0;
        foreach(var row in cases.EnumerateArray()){
            var reduced=row.GetProperty("reduced").GetBoolean();var keepsClock=row.GetProperty("retained").GetBoolean();var before=row.GetProperty("before");var fraction=reduced?1:BleeckerAccordionMotion.Expo(47.5/200);
            Check(before,fraction);samples++;foreach(var sample in row.GetProperty("samples").EnumerateArray()){fraction=reduced||!keepsClock?1:BleeckerAccordionMotion.Expo(sample.GetProperty("milliseconds").GetDouble()/200);Check(sample,fraction);samples++;}
            Assert.AreEqual(row.GetProperty("kind").GetString()=="content-grow"?113d:53d,row.GetProperty("after").GetProperty("naturalHeight").GetDouble());if(!reduced){if(keepsClock)retained++;else reset++;}
        }
        Assert.AreEqual(224,samples);Assert.AreEqual(10,retained);Assert.AreEqual(6,reset);
        static void Check(JsonElement row,double fraction){Assert.AreEqual(row.GetProperty("height").GetDouble(),BleeckerAccordionMotion.RevealHeight(row.GetProperty("naturalHeight").GetDouble(),fraction),.001);Assert.AreEqual(row.GetProperty("angle").GetDouble(),-90*(1-fraction),.0001);}
    }
    [TestMethod]
    public void ContinuityReferenceRetainsItsCollectorClientAndNormalSourceHashes()
    {
        using var document=Reference();foreach(var(file,field)in new[]{("Accordion.json","normalReferenceSha256"),("AccordionClient.tsx","clientSha256"),("CaptureAccordionContinuity.cjs","scriptSha256")})Assert.AreEqual(document.RootElement.GetProperty(field).GetString(),Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures",file)))));
    }
}
