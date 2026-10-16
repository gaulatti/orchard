using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Orchard;

namespace Orchard.Tests;

[TestClass]
public sealed class StepperMotionTests
{
    [TestMethod]
    public void ColorTimelinesMatchTheActualBrowserTransitions()
    {
        using var document = ReadReference(); var root = document.RootElement;
        var script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CaptureStepperMotion.cjs")).Replace("\r\n", "\n");
        Assert.AreEqual(root.GetProperty("scriptSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(script))));
        Assert.AreEqual("a0cea6e7cc8fcc541d0089682d64ed8589fc4983a482095785477cb01d646c29", root.GetProperty("sourceSha256").GetString());
        var cases = 0; var channels = 0;
        foreach (var test in root.GetProperty("transitions").EnumerateArray())
        {
            var reduced = test.GetProperty("reduced").GetBoolean();
            foreach (var animation in test.GetProperty("definition").GetProperty("animations").EnumerateArray())
            {
                var property = animation.GetProperty("property").GetString();
                Assert.IsTrue(property is "color" or "background-color");
                if (!reduced)
                {
                    Assert.AreEqual(190, animation.GetProperty("timing").GetProperty("duration").GetDouble());
                    Assert.AreEqual("cubic-bezier(0.22, 1, 0.36, 1)", animation.GetProperty("timing").GetProperty("easing").GetString());
                }
                else Assert.IsLessThanOrEqualTo(.01, animation.GetProperty("timing").GetProperty("duration").GetDouble());
            }
            foreach (var color in new[] { "background", "foreground" })
            {
                var from = Premultiply(test.GetProperty("before").GetProperty(color));
                var to = Premultiply(test.GetProperty("after").GetProperty(color));
                var motion = new BleeckerCssTransition<(double R, double G, double B, double Alpha)>(BleeckerSrgbColorMix.MixPremultiplied);
                motion.Retarget(from, 0, 190, false); motion.Retarget(to, 0, 190, !reduced);
                foreach (var sample in test.GetProperty("samples").EnumerateArray())
                {
                    // The source's reduced-motion floor is 0.01 ms. Native
                    // reduced motion snaps synchronously; compare presented
                    // samples after that floor, not its unpresented time zero.
                    if (reduced && sample.GetProperty("milliseconds").GetDouble() == 0) continue;
                    var actual = BleeckerSrgbColorMix.EncodePremultiplied(motion.ValueAt(sample.GetProperty("milliseconds").GetDouble()));
                    var expected = sample.GetProperty(color);
                    var values = new double[] { actual.R, actual.G, actual.B, actual.Alpha * 255 };
                    for (var channel = 0; channel < 4; channel++) Assert.AreEqual(expected[channel].GetDouble(), values[channel], channel == 3 ? 1 : 2,
                        $"{test.GetProperty("kind").GetString()} {test.GetProperty("from").GetString()}->{test.GetProperty("to").GetString()} {color} at {sample.GetProperty("milliseconds").GetDouble()}ms channel {channel}");
                    channels += 4;
                }
            }
            cases++;
        }
        Assert.AreEqual(42, cases); Assert.AreEqual(1600, channels);
    }

    [TestMethod]
    public void TrustedSpaceChangesIndividualScaleWithoutATransformTransition()
    {
        using var document = ReadReference(); var count = 0;
        foreach (var test in document.RootElement.GetProperty("presses").EnumerateArray())
        {
            var held = test.GetProperty("held");
            Assert.AreEqual("0.94", held.GetProperty("scale").GetString()); Assert.AreEqual("none", held.GetProperty("transform").GetString());
            Assert.AreEqual("1", test.GetProperty("released").GetProperty("scale").GetString());
            Assert.AreEqual(0, test.GetProperty("heldInvocations").GetInt32()); Assert.AreEqual(1, test.GetProperty("releasedInvocations").GetInt32());
            foreach (var animation in held.GetProperty("animations").EnumerateArray()) Assert.IsFalse(animation.GetProperty("property").GetString() is "scale" or "transform");
            foreach (var input in test.GetProperty("events").EnumerateArray()) Assert.IsTrue(input.GetProperty("trusted").GetBoolean());
            count++;
        }
        Assert.AreEqual(4, count);
        var availability = document.RootElement.GetProperty("availability"); Assert.AreEqual(2, availability.GetArrayLength());
        foreach (var test in availability.EnumerateArray())
        {
            Assert.IsTrue(test.GetProperty("retained").GetBoolean());
            Assert.AreEqual(test.GetProperty("before").GetProperty("background").GetRawText(), test.GetProperty("after").GetProperty("background").GetRawText());
            Assert.AreEqual(test.GetProperty("before").GetProperty("foreground").GetRawText(), test.GetProperty("after").GetProperty("foreground").GetRawText());
        }
    }
    private static JsonDocument ReadReference() => JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "StepperMotion.json")));
    private static (double R, double G, double B, double Alpha) Premultiply(JsonElement rgba) => BleeckerSrgbColorMix.Premultiply(((byte)rgba[0].GetInt32(), (byte)rgba[1].GetInt32(), (byte)rgba[2].GetInt32(), rgba[3].GetDouble() / 255));
}
