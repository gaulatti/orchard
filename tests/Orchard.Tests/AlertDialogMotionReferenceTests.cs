using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class AlertDialogMotionReferenceTests
{
    private static string Fixture(string file) => Path.Combine(AppContext.BaseDirectory, "Fixtures", file);
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllBytes(Fixture("AlertDialogMotion.json")));
    private static double N(JsonElement row, string name) => row.GetProperty(name).GetDouble();
    private static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file))));

    [TestMethod]
    public void ReversalsBindOriginalComponentCssAndTheExactDriver()
    {
        using var d = Reference(); var r = d.RootElement;
        Assert.AreEqual("orchard-alert-dialog-reversal-reference-v1", r.GetProperty("format").GetString());
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", r.GetProperty("commit").GetString());
        Assert.AreEqual(Hash("AlertDialog.json"), r.GetProperty("sourceReferenceSha256").GetString());
        Assert.AreEqual(Hash("CaptureAlertDialogMotion.cjs"), r.GetProperty("scriptSha256").GetString());
        Assert.AreEqual(Hash("AlertDialogClient.tsx"), r.GetProperty("clientSha256").GetString());
        var client = r.GetProperty("inputFiles").EnumerateArray().Single(x => x.GetProperty("path").GetString()!.Replace('\\', '/').EndsWith("/alert-dialog-motion-client.tsx", StringComparison.Ordinal));
        Assert.AreEqual(Hash("AlertDialogMotionClient.tsx"), client.GetProperty("sha256").GetString());
        Assert.IsTrue(r.GetProperty("headless").GetBoolean());
        Assert.AreEqual(18, r.GetProperty("histories").GetArrayLength());
        Assert.AreEqual(90, r.GetProperty("histories").EnumerateArray().Sum(h => h.GetProperty("stages").GetArrayLength()));
        Assert.AreEqual(234, r.GetProperty("histories").EnumerateArray().Sum(h => h.GetProperty("stages").EnumerateArray().Sum(s => s.GetProperty("samples").GetArrayLength())));
    }

    [TestMethod]
    public void ChangingSurfaceEasingPreservesPresentationAndIndependentReversalDurations()
    {
        using var d = Reference();
        foreach (var history in d.RootElement.GetProperty("histories").EnumerateArray())
        {
            var motion = new BleeckerAlertDialogMotion();
            foreach (var stage in history.GetProperty("stages").EnumerateArray())
            {
                var at = N(stage, "logicalTime"); var entering = stage.GetProperty("entering").GetBoolean();
                motion.SetVisible(entering, at);
                foreach (var animation in stage.GetProperty("animations").EnumerateArray())
                {
                    var surface = animation.GetProperty("target").GetString() == "surface";
                    Assert.AreEqual(N(animation, "duration"), surface ? motion.Durations.Surface : motion.Durations.Backdrop, .01, "Original effective CSS duration");
                }
                foreach (var sample in stage.GetProperty("samples").EnumerateArray())
                {
                    var actual = motion.SampleAt(at + N(sample, "milliseconds"));
                    Assert.AreEqual(N(sample, "opacity"), actual.Opacity, .00001, "surface opacity");
                    Assert.AreEqual(N(sample, "backdropOpacity"), actual.BackdropOpacity, .00001, "backdrop opacity");
                    Assert.AreEqual(N(sample, "scale"), actual.Scale, .000001, "surface scale");
                    Assert.AreEqual(N(sample, "translateY"), actual.TranslateY, .00002, "surface translation");
                    var box = BleeckerAlertDialogModel.Transform(new(176, 187.25, 448, 225.5), actual); var expected = sample.GetProperty("surface");
                    Assert.AreEqual(N(expected, "x"), box.X, .0001); Assert.AreEqual(N(expected, "y"), box.Y, .0001);
                    Assert.AreEqual(N(expected, "width"), box.Width, .0001); Assert.AreEqual(N(expected, "height"), box.Height, .0001);
                    Assert.AreEqual(!entering, sample.GetProperty("inert").GetBoolean()); Assert.AreEqual(entering, sample.GetProperty("rootInert").GetBoolean());
                }
            }
        }
    }

    [TestMethod]
    public void ReducedMotionAndUnchangedDestinationRetainTheSourceClockContract()
    {
        var motion = new BleeckerAlertDialogMotion(); motion.SetVisible(true, 0, reduced: true);
        Assert.AreEqual(.01, motion.Durations.Surface); Assert.AreEqual(1d, motion.SampleAt(.01).Opacity);
        motion.SetVisible(false, 1, reduced: true); Assert.AreEqual(0d, motion.SampleAt(1.01).Opacity);
        var transition = new BleeckerCssTransition<double>((a, b, t) => a + (b - a) * t, x => x);
        transition.Retarget(0, 0, 0, false); transition.Retarget(1, 0, 100, true);
        transition.Retarget(1, 25, 10, true, x => x * x);
        Assert.AreEqual(.5, transition.ValueAt(50), .000001, "Timing-only changes do not restart a running CSS transition.");
    }
}
