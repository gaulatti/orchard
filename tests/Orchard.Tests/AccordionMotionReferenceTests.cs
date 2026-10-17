using System.Security.Cryptography;
using System.Text.Json;
namespace Orchard.Tests;
[TestClass]
public sealed class AccordionMotionReferenceTests
{
    private static JsonDocument Reference()=>JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","AccordionMotion.json")));
    [TestMethod]
    public void BrowserExpansionSamplesUseTheExactCurveAndLayoutUnits()
    {
        using var document=Reference();var timelines=document.RootElement.GetProperty("transitions");Assert.AreEqual(48,timelines.GetArrayLength());var samples=0;
        foreach(var timeline in timelines.EnumerateArray().Where(row=>row.GetProperty("kind").GetString()=="expansion"))
        {
            var before=timeline.GetProperty("before").GetProperty("rows");var after=timeline.GetProperty("after").GetProperty("rows");var definition=timeline.GetProperty("definition").GetProperty("rows");
            foreach(var sample in timeline.GetProperty("samples").EnumerateArray())for(var index=0;index<2;index++){
                var from=before[index].GetProperty("expanded").GetBoolean()?1d:0;var to=after[index].GetProperty("expanded").GetBoolean()?1d:0;var animation=definition[index].GetProperty("animations").EnumerateArray().FirstOrDefault(row=>row.GetProperty("target").GetString()=="grid");var fraction=animation.ValueKind==JsonValueKind.Undefined?to:from+(to-from)*BleeckerAccordionMotion.Expo(sample.GetProperty("milliseconds").GetDouble()/animation.GetProperty("timing").GetProperty("duration").GetDouble());var actual=sample.GetProperty("rows")[index];
                Assert.AreEqual(actual.GetProperty("height").GetDouble(),BleeckerAccordionMotion.RevealHeight(actual.GetProperty("naturalHeight").GetDouble(),fraction),.001);Assert.AreEqual(actual.GetProperty("angle").GetDouble(),-90*(1-fraction),.0001);samples++;
            }
        }
        Assert.AreEqual(384,samples);
    }
    [TestMethod]
    public void ReversalsRetainTheBrowsersShortenedClocksAndHeightSamples()
    {
        using var document=Reference();var cases=document.RootElement.GetProperty("reversals");Assert.AreEqual(2,cases.GetArrayLength());var samples=0;
        foreach(var sequence in cases.EnumerateArray()){
            var clock=new BleeckerCssTransition<double>((a,b,t)=>a+(b-a)*t,BleeckerAccordionMotion.Expo);clock.Retarget(0,0,200,false);clock.Retarget(1,0,200,true);var at=0d;
            foreach(var step in sequence.GetProperty("steps").EnumerateArray()){
                at+=step.GetProperty("elapsed").GetDouble();clock.Retarget(step.GetProperty("target").GetString()=="none"?0:1,at,200,true);var definition=step.GetProperty("definition").GetProperty("rows")[0];foreach(var animation in definition.GetProperty("animations").EnumerateArray().Where(row=>row.GetProperty("target").GetString() is "grid" or "chevron"))Assert.AreEqual(animation.GetProperty("timing").GetProperty("duration").GetDouble(),clock.Timing.Duration,.001);
                foreach(var sample in step.GetProperty("samples").EnumerateArray()){
                    var fraction=clock.ValueAt(at+sample.GetProperty("milliseconds").GetDouble());var row=sample.GetProperty("rows")[0];Assert.AreEqual(row.GetProperty("height").GetDouble(),BleeckerAccordionMotion.RevealHeight(row.GetProperty("naturalHeight").GetDouble(),fraction),.001);Assert.AreEqual(row.GetProperty("angle").GetDouble(),-90*(1-fraction),.0001);samples++;
                }
            }
        }
        Assert.AreEqual(42,samples);
    }
    [TestMethod]
    public void MotionReferenceRetainsCollectorClientAndNormalSourceProvenance()
    {
        using var document=Reference();foreach(var(file,field)in new[]{("Accordion.json","normalReferenceSha256"),("AccordionClient.tsx","clientSha256"),("CaptureAccordionMotion.cjs","scriptSha256")})Assert.AreEqual(document.RootElement.GetProperty(field).GetString(),Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures",file)))));
    }
}
