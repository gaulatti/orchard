namespace Orchard.Tests;

[TestClass]
public sealed class SankeyInteractionTests
{
    [TestMethod]
    public void OriginalFlowInspectionRetainsItsKindAcrossNodeAndFlowChanges()
    {
        var state = new BleeckerSankeyInteraction(); state.Focus(); state.Pointer(true, 0);
        Assert.AreEqual(3, state.Index(3, 2)); Assert.AreEqual(4, state.Index(4, 2)); Assert.AreEqual(3, state.Index(3, 2));
        Assert.IsNull(state.Index(0, 0)); Assert.AreEqual(3, state.Index(3, 2));
        state.Key(0, 3, 2); Assert.IsNull(state.Index(3, 2));
        state.Key(0, 3, 2); Assert.AreEqual(3, state.Index(3, 2)); Assert.AreEqual(4, state.Index(4, 2));
        state.Exit(); state.Blur(); Assert.IsNull(state.Index(3, 2));
    }
    [TestMethod]
    public void NativeKeyboardBoundsAndPointerTakeoverRemainIndependent()
    {
        var state = new BleeckerSankeyInteraction(); state.Focus(); state.Pointer(true, 0);
        state.Key(1, 3, 2); Assert.AreEqual(1, state.Index(3, 2));
        for (var i = 0; i < 8; i++) state.Key(1, 3, 2); Assert.AreEqual(4, state.Index(3, 2));
        for (var i = 0; i < 8; i++) state.Key(-1, 3, 2); Assert.AreEqual(0, state.Index(3, 2));
        state.Pointer(false, 2); Assert.AreEqual(2, state.Index(3, 2)); state.Exit(); Assert.IsNull(state.Index(3, 2));
        state.Focus(); Assert.AreEqual(0, state.Index(3, 2)); Assert.IsNull(state.Index(0, 0));
    }
}
