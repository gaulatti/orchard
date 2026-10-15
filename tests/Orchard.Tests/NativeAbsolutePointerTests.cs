using Orchard.Catalog;

namespace Orchard.Tests;

[TestClass]
public sealed class NativeAbsolutePointerTests
{
    [TestMethod]
    public void EveryPixelSurvivesTheWindowsAbsoluteCoordinateMapping()
    {
        foreach (var extent in new[] { 2, 1280, 1920, 2560, 3440, 3840, 7680, 15360, 32000 })
        foreach (var origin in new[] { 0, -1920, -32768 })
        for (var offset = 0; offset < extent; offset++)
        {
            var normalized = NativeAbsolutePointer.Normalize(origin + offset, origin, extent);
            Assert.IsTrue(normalized is >= 0 and <= 65535);
            Assert.AreEqual(offset, (int)((long)normalized * extent / 65536));
        }
    }
    [TestMethod]
    public void InvalidAndUnrepresentableTargetsFailWithoutMovingElsewhere()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NativeAbsolutePointer.Normalize(0, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => NativeAbsolutePointer.Normalize(-1921, -1920, 3840));
        Assert.Throws<ArgumentOutOfRangeException>(() => NativeAbsolutePointer.Normalize(1920, -1920, 3840));
        Assert.Throws<InvalidOperationException>(() => NativeAbsolutePointer.Normalize(1, 0, 131072));
    }
}
