using Orchard.Catalog;

namespace Orchard.Tests;

[TestClass]
public sealed class NativeCapturePixelsTests
{
    [TestMethod]
    public void EveryOpaqueChannelValueKeepsItsExactByte()
    {
        var bgra = new byte[256 * 256 * 4]; var rgba = new byte[bgra.Length];
        for (var first = 0; first < 256; first++)
        for (var second = 0; second < 256; second++)
        {
            var at = (first * 256 + second) * 4;
            bgra[at] = (byte)first; bgra[at + 1] = (byte)second;
            bgra[at + 2] = (byte)(first ^ second); bgra[at + 3] = 255;
        }
        Assert.IsTrue(NativeCapturePixels.TryConvertOpaque(bgra, rgba));
        for (var at = 0; at < bgra.Length; at += 4)
        {
            Assert.AreEqual(bgra[at + 2], rgba[at]); Assert.AreEqual(bgra[at + 1], rgba[at + 1]);
            Assert.AreEqual(bgra[at], rgba[at + 2]); Assert.AreEqual((byte)255, rgba[at + 3]);
        }
    }

    [TestMethod]
    public void EveryNonOpaqueAlphaRequiresWindowsConversion()
    {
        for (var alpha = 0; alpha < 255; alpha++)
            Assert.IsFalse(NativeCapturePixels.TryConvertOpaque(new byte[] { 1, 2, 3, 255, 0, 0, 0, (byte)alpha }, new byte[8]));
        Assert.ThrowsExactly<ArgumentException>(() => NativeCapturePixels.TryConvertOpaque(new byte[3], new byte[3]));
        Assert.ThrowsExactly<ArgumentException>(() => NativeCapturePixels.TryConvertOpaque(new byte[4], new byte[8]));
    }
}
