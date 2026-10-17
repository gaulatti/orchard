using Orchard.Catalog;
namespace Orchard.Tests;
[TestClass]
public sealed class NativeCaptureValidationTests
{
    [TestMethod]
    public void ExactReferenceFramesRequireEveryChannelAndTheCompleteBuffer()
    {
        byte[] expected = [255, 255, 255, 255, 33, 24, 13, 255];
        NativeCaptureValidation.VerifyExact(expected, expected.ToArray());
        for (var at = 0; at < expected.Length; at++)
        {
            var altered = expected.ToArray(); altered[at] ^= 1;
            Assert.ThrowsExactly<InvalidOperationException>(() => NativeCaptureValidation.VerifyExact(altered, expected));
        }
        Assert.ThrowsExactly<InvalidOperationException>(() => NativeCaptureValidation.VerifyExact(expected[..4], expected));
        Assert.ThrowsExactly<InvalidOperationException>(() => NativeCaptureValidation.VerifyExact([], []));
        Assert.ThrowsExactly<InvalidOperationException>(() => NativeCaptureValidation.VerifyExact([1, 2, 3], [1, 2, 3]));
    }
    [TestMethod]
    public void OrdinaryCapturesStillRejectBlankFrames()
    {
        Assert.ThrowsExactly<InvalidOperationException>(()=>NativeCaptureValidation.Verify(new byte[]{255,255,255,255}));
        Assert.ThrowsExactly<InvalidOperationException>(()=>NativeCaptureValidation.Verify(Array.Empty<byte>()));
    }
    [TestMethod]
    public void EmptyCanvasRequiresEveryPixelToMatchItsExactOpaqueColour()
    {
        NativeCaptureValidation.Verify(new byte[]{33,24,13,255,33,24,13,255},(13,24,33));
        Assert.ThrowsExactly<InvalidOperationException>(()=>NativeCaptureValidation.Verify(new byte[]{33,24,13,255,34,24,13,255},(13,24,33)));
        Assert.ThrowsExactly<InvalidOperationException>(()=>NativeCaptureValidation.Verify(new byte[]{33,24,13,254},(13,24,33)));
        Assert.ThrowsExactly<InvalidOperationException>(()=>NativeCaptureValidation.Verify(new byte[]{0,0,0,255},(13,24,33)));
    }

    [TestMethod]
    public void ColorThresholdStillRequiresNineDistinctNonAlphaValues()
    {
        for (var values = 0; values <= 10; values++)
        {
            var pixels = new byte[values * 4];
            for (var at = 0; at < values; at++)
            {
                pixels[at * 4] = pixels[at * 4 + 1] = pixels[at * 4 + 2] = (byte)at;
                pixels[at * 4 + 3] = (byte)(255 - at);
            }
            Assert.AreEqual(values >= 9, NativeCaptureValidation.HasColorVariation(pixels));
            if (values > 0 && values < 9)
                Assert.ThrowsExactly<InvalidOperationException>(() => NativeCaptureValidation.Verify(pixels));
            else if (values >= 9) NativeCaptureValidation.Verify(pixels);
        }
    }

    [TestMethod]
    public void ColorScanMatchesThePreviousPredicateAcrossEveryBytePalette()
    {
        var random = new Random(7391);
        for (var palette = 1; palette <= 256; palette++)
        {
            var pixels = new byte[(1 + palette * 7) * 4];
            random.NextBytes(pixels);
            for (var at = 0; at < pixels.Length; at++)
                if ((at & 3) != 3) pixels[at] = (byte)(pixels[at] % palette);
            var previous = pixels.Where((_, offset) => offset % 4 != 3).Distinct().Take(9).Count() > 8;
            Assert.AreEqual(previous, NativeCaptureValidation.HasColorVariation(pixels));
        }
    }
}
