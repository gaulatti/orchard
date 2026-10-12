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
}
