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
}
