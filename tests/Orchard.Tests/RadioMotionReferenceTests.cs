using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Orchard.Tests;
[TestClass]
public sealed class RadioMotionReferenceTests
{
    [TestMethod]
    public void FrozenBrowserTimelinesMatchTheNativeTransitionClock()
    {
        using var document=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","RadioMotion.json")));var root=document.RootElement;Assert.AreEqual(52,root.GetProperty("transitions").GetArrayLength());
        var checkedSamples=0;
        foreach(var transition in root.GetProperty("transitions").EnumerateArray().Where(row=>!row.GetProperty("reduced").GetBoolean()))
        {
            var border=new BleeckerCssTransition<BleeckerCssColor>(BleeckerCssColor.Mix);var dot=new BleeckerCssTransition<double>((a,b,t)=>a+(b-a)*t);
            var before=transition.GetProperty("before");var after=transition.GetProperty("after");var borderFrames=transition.GetProperty("definition").GetProperty("animations").EnumerateArray().FirstOrDefault(animation=>animation.GetProperty("property").GetString()=="border-top-color");var start=borderFrames.ValueKind==JsonValueKind.Undefined?before.GetProperty("border").GetString()!:borderFrames.GetProperty("frames")[0].GetProperty("borderTopColor").GetString()!;var end=borderFrames.ValueKind==JsonValueKind.Undefined?after.GetProperty("border").GetString()!:borderFrames.GetProperty("frames")[1].GetProperty("borderTopColor").GetString()!;border.Retarget(Colour(start),0,190,false);dot.Retarget(before.GetProperty("scale").GetDouble(),0,220,false);
            border.Retarget(Colour(end),0,190,true);dot.Retarget(after.GetProperty("scale").GetDouble(),0,220,true);
            foreach(var animation in transition.GetProperty("definition").GetProperty("animations").EnumerateArray())
            {
                var property=animation.GetProperty("property").GetString();if(property is not ("border-top-color" or "scale"))continue;
                Assert.AreEqual(property=="scale"?220d:190d,animation.GetProperty("timing").GetProperty("duration").GetDouble());Assert.AreEqual("cubic-bezier(0.22, 1, 0.36, 1)",animation.GetProperty("timing").GetProperty("easing").GetString());
            }
            foreach(var sample in transition.GetProperty("samples").EnumerateArray())
            {
                var time=sample.GetProperty("milliseconds").GetDouble();Assert.AreEqual(sample.GetProperty("scale").GetDouble(),dot.ValueAt(time),.00001);
                var expected=Colour(sample.GetProperty("border").GetString()!).Encode();var actual=border.ValueAt(time).Encode();var context=$"{transition.GetProperty("scheme").GetString()}/{transition.GetProperty("kind").GetString()}/{transition.GetProperty("from").GetString()}->{transition.GetProperty("to").GetString()}/{time}";Assert.AreEqual(expected.R,actual.R,context);Assert.AreEqual(expected.G,actual.G,context);Assert.AreEqual(expected.B,actual.B,context);Assert.AreEqual(expected.Alpha,actual.Alpha,.000001,context);checkedSamples++;
            }
        }
        Assert.AreEqual(156,checkedSamples);foreach(var availability in root.GetProperty("availability").EnumerateArray())Assert.IsTrue(availability.GetProperty("retained").GetBoolean());
    }
    [TestMethod]
    public void MixedColoursUseThePinnedBrowsersIccAndChromaticAdaptationPath()
    {
        using var document=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","RadioMotion.json")));
        foreach(var scheme in new[]{"Light","Dark"})foreach(var state in new[]{"off","hover"})
        {
            var frame=document.RootElement.GetProperty("transitions").EnumerateArray().First(row=>!row.GetProperty("reduced").GetBoolean()&&row.GetProperty("scheme").GetString()==scheme&&row.GetProperty("kind").GetString()=="theme"&&row.GetProperty("from").GetString()==state);
            var expected=Colour(frame.GetProperty("before").GetProperty("border").GetString()!);
            var rgb=state=="hover"?(scheme=="Light"?((byte)44,(byte)87,(byte)132):((byte)91,(byte)163,(byte)245)):(scheme=="Light"?((byte)230,(byte)213,(byte)184):((byte)26,(byte)35,(byte)50));
            var actual=BleeckerCssColor.FromRgba((rgb.Item1,rgb.Item2,rgb.Item3,expected.Alpha),false);Assert.AreEqual(expected.First,actual.First,.000001);Assert.AreEqual(expected.Second,actual.Second,.000001);Assert.AreEqual(expected.Third,actual.Third,.000001);var encoded=actual.Encode();Assert.AreEqual(rgb.Item1,encoded.R);Assert.AreEqual(rgb.Item2,encoded.G);Assert.AreEqual(rgb.Item3,encoded.B);
        }
    }
    [TestMethod]
    public void MotionCollectorAndNormalReferenceRetainTheirFrozenProvenance()
    {
        using var document=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","RadioMotion.json")));var root=document.RootElement;
        foreach(var (name,field)in new[]{("RadioGroup.json","normalReferenceSha256"),("CaptureRadioMotion.cjs","scriptSha256")})Assert.AreEqual(root.GetProperty(field).GetString(),Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures",name)))).ToLowerInvariant());
    }
    private static BleeckerCssColor Colour(string css)
    {
        var values=Regex.Matches(css,@"[-+]?(?:\d*\.)?\d+(?:[eE][-+]?\d+)?").Select(match=>double.Parse(match.Value,CultureInfo.InvariantCulture)).ToArray();
        if(css.StartsWith("oklab",StringComparison.Ordinal)){var alpha=values.Length==4?values[3]:1;return new(false,values[0]*alpha,values[1]*alpha,values[2]*alpha,alpha);}
        return BleeckerCssColor.FromRgba(((byte)values[0],(byte)values[1],(byte)values[2],values.Length==4?values[3]:1),true);
    }
}
