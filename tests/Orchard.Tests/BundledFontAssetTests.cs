namespace Orchard.Tests;

[TestClass]
public sealed class BundledFontAssetTests
{
    [TestMethod]
    public void NestedLayoutResolvesTheSameAssetDuringValidation()
    {
        var uri = new Uri("file:///C:/Orchard/EncodeSans-Regular.ttf");
        var calls = 0;
        BleeckerBundledFontAsset asset = null!;
        asset = new(uri, requested =>
        {
            calls++;
            Assert.AreEqual(uri, requested);
            Assert.AreEqual(uri.AbsoluteUri, asset.Resolve());
        });
        Assert.AreEqual(uri.AbsoluteUri, asset.Resolve());
        Assert.AreEqual(uri.AbsoluteUri, asset.Resolve());
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public void FailedValidationRemainsAnErrorAfterNestedResolution()
    {
        var uri = new Uri("file:///C:/Orchard/invalid.ttf");
        var calls = 0;
        var failure = new InvalidOperationException("No font faces.");
        BleeckerBundledFontAsset asset = null!;
        asset = new(uri, _ => { calls++; asset.Resolve(); throw failure; });
        Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(() => asset.Resolve()));
        Assert.AreSame(failure, Assert.ThrowsExactly<InvalidOperationException>(() => asset.Resolve()));
        Assert.AreEqual(1, calls);
    }
}
