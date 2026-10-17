using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Orchard;

namespace Orchard.Tests;

[TestClass]
public sealed class InputPaintMotionTests
{
    [TestMethod]
    public void InputColorAndOpacityTimelinesMatchFrozenBrowserStates()
    {
        using var document=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","InputPaintMotion.json")));var root=document.RootElement;
        Assert.AreEqual("01bce586edd4c1ad4aed30415b5aa9cbc6e620a874e4751cf7299bfa54ccc192",root.GetProperty("sourceSha256").GetString());
        var script=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","CaptureInputPaintMotion.cjs")).Replace("\r\n","\n");Assert.AreEqual(root.GetProperty("scriptSha256").GetString(),Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(script))));
        var cases=0;var samples=0;
        foreach(var test in root.GetProperty("transitions").EnumerateArray())
        {
            var reduced=test.GetProperty("reduced").GetBoolean();var before=test.GetProperty("before");var after=test.GetProperty("after");
            foreach(var animation in test.GetProperty("definition").GetProperty("animations").EnumerateArray())
            {
                var timing=animation.GetProperty("timing");if(!reduced){Assert.AreEqual(190,timing.GetProperty("duration").GetDouble());Assert.AreEqual("cubic-bezier(0.22, 1, 0.36, 1)",timing.GetProperty("easing").GetString());}
                else Assert.IsLessThanOrEqualTo(.01,timing.GetProperty("duration").GetDouble());
            }
            foreach(var(property,raw)in new[]{("background","rawBackground"),("border","rawBorder")})
            {
                var motion=new BleeckerCssTransition<(double L,double A,double B,double Alpha)>(BleeckerColorMix.MixPremultiplied);
                motion.Retarget(BleeckerColorMix.Premultiply(Read(before.GetProperty(raw).GetString()!)),0,190,false);motion.Retarget(BleeckerColorMix.Premultiply(Read(after.GetProperty(raw).GetString()!)),0,190,!reduced);
                foreach(var sample in test.GetProperty("samples").EnumerateArray())
                {
                    var time=sample.GetProperty("milliseconds").GetDouble();if(reduced&&time==0)continue;
                    var actual=BleeckerColorMix.EncodePremultiplied(motion.ValueAt(time));var expected=Read(sample.GetProperty(raw).GetString()!);var channels=new double[]{actual.R,actual.G,actual.B};var expectedChannels=new double[]{expected.R,expected.G,expected.B};
                    // Computed CSS preserves alpha and OKLab coordinates. Canvas
                    // getImageData unpremultiplication introduces extra rounding
                    // at low alpha and is retained only as a capture diagnostic.
                    for(var channel=0;channel<3;channel++)Assert.AreEqual(expectedChannels[channel],channels[channel],2,$"{test.GetProperty("scheme")} {test.GetProperty("from")}->{test.GetProperty("to")} {property} {time}ms channel {channel}");
                    Assert.AreEqual(Read(sample.GetProperty(raw).GetString()!).Alpha,actual.Alpha,.000001,"Input paint must preserve fractional alpha throughout the browser transition.");samples++;
                }
            }
            var opacity=new BleeckerCssTransition<double>((from,to,t)=>from+(to-from)*t);opacity.Retarget(before.GetProperty("opacity").GetDouble(),0,190,false);opacity.Retarget(after.GetProperty("opacity").GetDouble(),0,190,!reduced);
            foreach(var sample in test.GetProperty("samples").EnumerateArray()){var time=sample.GetProperty("milliseconds").GetDouble();if(reduced&&time==0)continue;Assert.AreEqual(sample.GetProperty("opacity").GetDouble(),opacity.ValueAt(time),.000001);}
            cases++;
        }
        Assert.AreEqual(40,cases);Assert.AreEqual(360,samples);
    }
    private static (byte R,byte G,byte B,double Alpha) Read(string color)
    {
        var values=Regex.Matches(color,@"[-+]?(?:\d*\.)?\d+(?:[eE][-+]?\d+)?").Select(match=>double.Parse(match.Value,CultureInfo.InvariantCulture)).ToArray();
        if(color.StartsWith("oklab(",StringComparison.Ordinal)){var alpha=values.Length==4?values[3]:1;return BleeckerColorMix.EncodePremultiplied((values[0]*alpha,values[1]*alpha,values[2]*alpha,alpha));}
        if(!color.StartsWith("rgb",StringComparison.Ordinal))throw new InvalidOperationException("Unsupported browser color: "+color);
        return((byte)values[0],(byte)values[1],(byte)values[2],values.Length==4?values[3]:1);
    }
}
