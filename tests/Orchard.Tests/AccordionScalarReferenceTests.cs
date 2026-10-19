using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
namespace Orchard.Tests;
[TestClass]
public sealed class AccordionScalarReferenceTests
{
    private static JsonDocument Reference()=>JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","AccordionScalars.json")));
    [TestMethod]
    public void NumberTextMatchesTheActualReactTitleAndAccessibleName()
    {
        using var document=Reference();var frames=document.RootElement.GetProperty("cases");Assert.AreEqual(292,frames.GetArrayLength());var count=0;
        foreach(var frame in frames.EnumerateArray().Where(row=>row.GetProperty("width").GetInt32()==320&&row.GetProperty("scheme").GetString()=="Light"&&row.GetProperty("slot").GetString()=="title"&&row.GetProperty("scalar").GetProperty("kind").GetString()=="number")){
            var number=double.Parse(frame.GetProperty("scalar").GetProperty("value").GetString()!,CultureInfo.InvariantCulture);var text=BleeckerNumberText.Format(number);var measurement=frame.GetProperty("measurement");Assert.AreEqual(measurement.GetProperty("title").GetProperty("text").GetString(),text);Assert.AreEqual(measurement.GetProperty("name").GetString(),text+" Consumer text");count++;
        }
        Assert.AreEqual(13,count);
    }
    [TestMethod]
    public void ScalarReferenceRetainsUnmodifiedSourceAndCollectorProvenance()
    {
        using var document=Reference();foreach(var(file,field)in new[]{("Accordion.json","normalReferenceSha256"),("AccordionScalarsClient.tsx","clientSha256"),("CaptureAccordionScalars.cjs","scriptSha256")})Assert.AreEqual(document.RootElement.GetProperty(field).GetString(),Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures",file)))));
        var pngs=0;foreach(var frame in document.RootElement.GetProperty("cases").EnumerateArray().Where(row=>row.GetProperty("file").ValueKind!=JsonValueKind.Null)){Assert.AreEqual(frame.GetProperty("pngSha256").GetString(),Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","AccordionScalars",frame.GetProperty("file").GetString()!)))));pngs++;}Assert.AreEqual(36,pngs);
    }
}
