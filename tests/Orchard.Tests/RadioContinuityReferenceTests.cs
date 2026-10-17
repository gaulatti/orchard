using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Orchard.Tests;
[TestClass]
public sealed class RadioContinuityReferenceTests
{
    [TestMethod]
    public void PinnedKeyedMovesRetainOnlyTheOptionsThatStayInTheSourceTree()
    {
        using var document=Reference();var rows=document.RootElement.GetProperty("lifecycle");Assert.AreEqual(28,rows.GetArrayLength());
        foreach(var row in rows.EnumerateArray())
        {
            var kind=row.GetProperty("kind").GetString();var fresh=kind is "remove-reinsert" or "group-remount";Assert.AreEqual(!fresh,row.GetProperty("sameNode").GetBoolean());
            Assert.AreEqual(!fresh&&(row.GetProperty("reduced").GetBoolean()||kind!="move-first-to-last"),row.GetProperty("retained").GetBoolean());
            if(!row.GetProperty("reduced").GetBoolean()&&row.GetProperty("retained").GetBoolean())Assert.AreEqual(row.GetProperty("before").GetProperty("scale").GetDouble(),row.GetProperty("after").GetProperty("scale").GetDouble(),0);
            else Assert.AreEqual(1,row.GetProperty("after").GetProperty("scale").GetDouble(),0);
        }
    }
    [TestMethod]
    public void RepeatedReversalsMatchTheBrowsersShortenedClocksAndRenderedChannels()
    {
        using var document=Reference();var cases=document.RootElement.GetProperty("reversals");Assert.AreEqual(2,cases.GetArrayLength());var samples=0;
        foreach(var sequence in cases.EnumerateArray())
        {
            var border=new BleeckerCssTransition<BleeckerCssColor>(BleeckerCssColor.Mix);var dot=new BleeckerCssTransition<double>((from,to,progress)=>from+(to-from)*progress);var before=sequence.GetProperty("before");var steps=sequence.GetProperty("steps");border.Retarget(Colour(before.GetProperty("border").GetString()!),0,190,false);dot.Retarget(0,0,220,false);
            var first=Animation(steps[0].GetProperty("during"),"border-top-color");border.Retarget(Colour(first.GetProperty("frames")[1].GetProperty("borderTopColor").GetString()!),0,190,true);dot.Retarget(1,0,220,true);var time=0d;
            foreach(var step in steps.EnumerateArray())
            {
                time+=step.GetProperty("elapsed").GetDouble();var definition=step.GetProperty("definition");var stroke=Animation(definition,"border-top-color");var scale=Animation(definition,"scale");var destination=Colour(stroke.GetProperty("frames")[1].GetProperty("borderTopColor").GetString()!);border.Retarget(destination,time,190,true);dot.Retarget(step.GetProperty("checked").GetBoolean()?1:0,time,220,true);
                Assert.AreEqual(stroke.GetProperty("timing").GetProperty("duration").GetDouble(),border.Timing.Duration,.001);Assert.AreEqual(scale.GetProperty("timing").GetProperty("duration").GetDouble(),dot.Timing.Duration,.001);
                foreach(var sample in step.GetProperty("samples").EnumerateArray())
                {
                    var at=time+sample.GetProperty("milliseconds").GetDouble();Assert.AreEqual(sample.GetProperty("scale").GetDouble(),dot.ValueAt(at),.00001);var expected=Colour(sample.GetProperty("border").GetString()!).Encode();var actual=border.ValueAt(at).Encode();Assert.AreEqual(expected.R,actual.R);Assert.AreEqual(expected.G,actual.G);Assert.AreEqual(expected.B,actual.B);Assert.AreEqual(expected.Alpha,actual.Alpha,.000001);samples++;
                }
            }
        }
        Assert.AreEqual(36,samples);
    }
    [TestMethod]
    public void ContinuityCollectorRetainsItsSourceAndHarnessProvenance()
    {
        using var document=Reference();foreach(var(file,field)in new[]{("RadioGroup.json","normalReferenceSha256"),("CaptureRadioContinuity.cjs","scriptSha256"),("RadioContinuityClient.tsx","clientSha256")})Assert.AreEqual(document.RootElement.GetProperty(field).GetString(),Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures",file)))).ToLowerInvariant());
    }
    private static JsonElement Animation(JsonElement frame,string property)=>frame.GetProperty("animations").EnumerateArray().Single(animation=>animation.GetProperty("property").GetString()==property);
    private static JsonDocument Reference()=>JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","RadioContinuity.json")));
    private static BleeckerCssColor Colour(string css)
    {
        var values=Regex.Matches(css,@"[-+]?(?:\d*\.)?\d+(?:[eE][-+]?\d+)?").Select(match=>double.Parse(match.Value,CultureInfo.InvariantCulture)).ToArray();if(css.StartsWith("oklab",StringComparison.Ordinal)){var alpha=values.Length==4?values[3]:1;return new(false,values[0]*alpha,values[1]*alpha,values[2]*alpha,alpha);}return BleeckerCssColor.FromRgba(((byte)values[0],(byte)values[1],(byte)values[2],values.Length==4?values[3]:1),true);
    }
}
