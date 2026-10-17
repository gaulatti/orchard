using Orchard.Catalog;
namespace Orchard.Tests;
[TestClass]
public sealed class NativeCaptureValidationTests
{
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
}
