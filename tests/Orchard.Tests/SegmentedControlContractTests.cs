namespace Orchard.Tests;

[TestClass]
public sealed class SegmentedControlContractTests
{
    private sealed record Identity(int Id);
    [TestMethod]
    public void ValuesRetainTheirTypesAndNullIsAnOrdinaryIdentity()
    {
        var record = new Identity(7); var option = new BleeckerSegmentOption(record, "Fictional");
        Assert.AreSame(record, option.Value);
        BleeckerSegmentedModel.Validate([option, new(7, "Number"), new("7", "String"), new(null, "None")]);
    }
    [TestMethod]
    public void DuplicateValueEqualityIsRejectedRatherThanConvertedToLabels()
    {
        Assert.Throws<ArgumentException>(() => BleeckerSegmentedModel.Validate([new(new Identity(7), "First"), new(new Identity(7), "Second")]));
        Assert.Throws<ArgumentException>(() => BleeckerSegmentedModel.Validate([new(null, "First"), new(null, "Second")]));
    }
    [TestMethod]
    public void OptionDataIsExplicitAndImmutable()
    {
        Assert.Throws<ArgumentNullException>(() => new BleeckerSegmentOption(1, null!));
        Assert.Throws<ArgumentNullException>(() => BleeckerSegmentedModel.Validate([null!]));
        Assert.IsTrue(new BleeckerSegmentOption(1, "", true).IsDisabled);
    }
    [TestMethod]
    public void NaturalWidthsAreKeptWithoutEqualWidthFill()
    {
        CollectionAssert.AreEqual(new[] { 30d, 90d, 60d }, BleeckerSegmentedModel.Widths([30, 90, 60], 600));
        CollectionAssert.AreEqual(new[] { 30d, 90d, 60d }, BleeckerSegmentedModel.Widths([30, 90, 60], double.PositiveInfinity));
    }
    [TestMethod]
    public void ShortLabelsRetainTheirWidthWhileLongLabelsShareTheConstraint()
    {
        CollectionAssert.AreEqual(new[] { 30d, 60d, 60d }, BleeckerSegmentedModel.Widths([30, 90, 90], 150));
        CollectionAssert.AreEqual(new[] { 60d, 30d, 60d }, BleeckerSegmentedModel.Widths([90, 30, 90], 150));
    }
    [TestMethod]
    public void ExtremelyNarrowProposalsRetainSourcePaddingAndEmptyOptionsStayEmpty()
    {
        CollectionAssert.AreEqual(new[] { 24d, 24d }, BleeckerSegmentedModel.Widths([50, 50], 0));
        Assert.AreEqual(0, BleeckerSegmentedModel.Widths([], 0).Length);
    }
    [TestMethod]
    public void InvalidNativeMeasurementsFailExplicitly()
    {
        Assert.Throws<ArgumentException>(() => BleeckerSegmentedModel.Widths([double.NaN], 100));
        Assert.Throws<ArgumentException>(() => BleeckerSegmentedModel.Widths([double.PositiveInfinity], 100));
        Assert.Throws<ArgumentException>(() => BleeckerSegmentedModel.Widths([20], 100));
        Assert.Throws<ArgumentException>(() => BleeckerSegmentedModel.Widths([30], double.NaN));
    }
    [TestMethod]
    public void KeyboardPlansSkipDisabledOptionsAndKeepNativeEnds()
    {
        bool[] enabled = [true, false, true];
        Assert.AreEqual(2, BleeckerSegmentedModel.Navigate(enabled, 0, 1));
        Assert.AreEqual(0, BleeckerSegmentedModel.Navigate(enabled, 2, -1));
        Assert.AreEqual(2, BleeckerSegmentedModel.Navigate(enabled, 2, 1));
        Assert.AreEqual(0, BleeckerSegmentedModel.Navigate(enabled, 0, -1));
        Assert.AreEqual(0, BleeckerSegmentedModel.Navigate(enabled, 2, 1, home: true));
        Assert.AreEqual(2, BleeckerSegmentedModel.Navigate(enabled, 0, -1, end: true));
    }
    [TestMethod]
    public void EmptyAndDisabledKeyboardPlansNeverInventASelection()
    {
        Assert.AreEqual(-1, BleeckerSegmentedModel.Navigate([], 0, 1));
        Assert.AreEqual(-1, BleeckerSegmentedModel.Navigate([false, false], 0, 1));
        Assert.AreEqual(1, BleeckerSegmentedModel.Navigate([false, true], -1, -1));
    }
}
