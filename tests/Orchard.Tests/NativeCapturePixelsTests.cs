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

    [TestMethod]
    public void UnalignedVectorBoundariesAndTailsPreserveOnlyTheDestinationSlice()
    {
        foreach (var count in new[] { 1, 3, 4, 5, 7, 8, 9, 15, 16, 17, 257 })
        for (var offset = 0; offset < 16; offset++)
        {
            var source = Enumerable.Repeat((byte)0xa5, offset + count * 4 + 17).ToArray();
            var destination = Enumerable.Repeat((byte)0x5a, source.Length).ToArray();
            for (var pixel = 0; pixel < count; pixel++)
            {
                var at = offset + pixel * 4;
                source[at] = (byte)pixel; source[at + 1] = (byte)(pixel * 37);
                source[at + 2] = (byte)(pixel * 11); source[at + 3] = 255;
            }
            var original = source.ToArray();
            Assert.IsTrue(NativeCapturePixels.TryConvertOpaque(source.AsSpan(offset, count * 4), destination.AsSpan(offset, count * 4)));
            CollectionAssert.AreEqual(original, source);
            for (var at = 0; at < destination.Length; at++)
            {
                if (at < offset || at >= offset + count * 4) Assert.AreEqual((byte)0x5a, destination[at]);
                else
                {
                    var channel = (at - offset) % 4;
                    var sourceAt = at + (channel == 0 ? 2 : channel == 2 ? -2 : 0);
                    Assert.AreEqual(source[sourceAt], destination[at]);
                }
            }
        }
    }

    [TestMethod]
    public void TransparencyInEveryVectorLaneAndTailRequiresWindowsConversion()
    {
        for (var pixel = 0; pixel < 17; pixel++)
        for (var alpha = 0; alpha < 255; alpha++)
        {
            var source = Enumerable.Repeat((byte)255, 17 * 4).ToArray();
            source[pixel * 4 + 3] = (byte)alpha;
            Assert.IsFalse(NativeCapturePixels.TryConvertOpaque(source, new byte[source.Length]));
        }
    }
}
