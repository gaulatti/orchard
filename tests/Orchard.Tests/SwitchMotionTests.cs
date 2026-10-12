using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class SwitchMotionTests
{
    [TestMethod]
    public void PremiumCurveMatchesTheRenderedPinnedSwitchTimeline()
    {
        // The audit freezes the source CSS transitions at 95ms. Browser output:
        // 190ms track alpha=.971037 (off=.25, on=1), 220ms thumb translate=14.9522.
        Assert.AreEqual((.971037 - .25) / .75, BleeckerColorMix.Premium(95d / 190), .00001);
        Assert.AreEqual(14.9522 / 16, BleeckerColorMix.Premium(95d / 220), .00001);
    }

    [TestMethod]
    public void TrackColorMatchesTheBrowserRasterAt95Milliseconds()
    {
        var sample = BleeckerColorMix.Mix((230, 213, 184, .25), (44, 87, 132, 1), BleeckerColorMix.Premium(.5));
        // Source CSS sampled into a 1px sRGB canvas: RGBA 46,88,133,248.
        // One channel step allows the canvas's alpha premultiplication round-trip.
        Assert.AreEqual(46, sample.R, 1);
        Assert.AreEqual(88, sample.G, 1);
        Assert.AreEqual(133, sample.B, 1);
        Assert.AreEqual(248, Math.Round(sample.Alpha * 255), 0);
    }

    [TestMethod]
    public void RepeatedReversalsAndPaletteChangesMatchFrozenBrowserColors()
    {
        var off = BleeckerColorMix.Premultiply((230, 213, 184, .25));
        var on = BleeckerColorMix.Premultiply((44, 87, 132, 1));
        var dark = BleeckerColorMix.Premultiply((91, 163, 245, 1));
        var transition = new BleeckerCssTransition<(double L, double A, double B, double Alpha)>(BleeckerColorMix.MixPremultiplied);
        using var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "SwitchTransitions.json")));
        transition.Retarget(off, 0, 190, false);
        transition.Retarget(on, 0, 190, true);
        Raster("on95", 95);
        transition.Retarget(off, 95, 190, true);
        // CSS effect durations observed in the isolated reference-property audit.
        Duration("reverse50", 95);
        Raster("reverse50", 145);
        transition.Retarget(on, 145, 190, true);
        Duration("reverseAgain25", 145);
        Raster("reverseAgain25", 170);
        transition.Retarget(dark, 170, 190, true);
        Raster("dark95", 265);
        Assert.IsTrue(transition.IsRunning(359));
        Assert.IsFalse(transition.IsRunning(360));

        JsonElement Sample(string name) => reference.RootElement.EnumerateArray().Single(sample => sample.GetProperty("name").GetString() == name);
        void Duration(string name, double time)
        {
            var duration = Sample(name).GetProperty("duration")[0].GetDouble();
            Assert.IsTrue(transition.IsRunning(time + duration - .001));
            Assert.IsFalse(transition.IsRunning(time + duration + .001));
        }
        void Raster(string name, double time)
        {
            var expected = Sample(name).GetProperty("rgba");
            var color = BleeckerColorMix.EncodePremultiplied(transition.ValueAt(time));
            Assert.AreEqual(expected[0].GetInt32(), color.R, 1); Assert.AreEqual(expected[1].GetInt32(), color.G, 1);
            Assert.AreEqual(expected[2].GetInt32(), color.B, 1); Assert.AreEqual(expected[3].GetInt32(), Math.Round(color.Alpha * 255), 1);
        }
    }

    [TestMethod]
    public void AnUnchangedDestinationKeepsItsClockAndReducedMotionSettlesIt()
    {
        var thumb = new BleeckerCssTransition<double>((from, to, progress) => from + (to - from) * progress);
        thumb.Retarget(0, 0, 220, false);
        thumb.Retarget(16, 0, 220, true);
        Assert.AreEqual(14.9522, thumb.ValueAt(95), .0001);
        // A palette notification at 95ms must not restart the thumb's 220ms path.
        thumb.Retarget(16, 95, 220, true);
        Assert.IsTrue(thumb.IsRunning(219));
        Assert.IsFalse(thumb.IsRunning(220));
        Assert.AreEqual(16, thumb.ValueAt(220), 0);
        thumb.Retarget(0, 220, 220, true);
        thumb.Retarget(0, 230, 220, false);
        Assert.IsFalse(thumb.IsRunning(230));
        Assert.AreEqual(0, thumb.ValueAt(230), 0);
    }
}
