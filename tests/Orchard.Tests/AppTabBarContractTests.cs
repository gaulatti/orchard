namespace Orchard.Tests;

[TestClass]
public sealed class AppTabBarContractTests
{
    private sealed record Identity(int Id);
    [TestMethod]
    public void BadgeVisibilityAndCapPreserveTheFullAccessibleCount()
    {
        foreach (var count in new long?[] { null, long.MinValue, -1, 0, 1, 99, 100, long.MaxValue })
        {
            var expected = count is > 0 ? count > 99 ? "99+" : count.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";
            Assert.AreEqual(expected, BleeckerAppTabModel.BadgeText(count));
            Assert.AreEqual(count.HasValue ? count.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + " notifications" : "", BleeckerAppTabModel.BadgeStatus(count));
        }
    }
    [TestMethod]
    public void TabIdentityUsesOriginalValuesAndRejectsEqualDuplicates()
    {
        BleeckerAppTabModel.ValidateValues([null, 1, "1", new Identity(1)]);
        Assert.Throws<ArgumentException>(() => BleeckerAppTabModel.ValidateValues([new Identity(1), new Identity(1)]));
        Assert.Throws<ArgumentException>(() => BleeckerAppTabModel.ValidateValues([null, null]));
    }
    [TestMethod]
    public void ConstrainedTracksRemainEqualAndUnconstrainedTracksUseNaturalWidths()
    {
        Assert.AreEqual(100, BleeckerAppTabModel.TrackWidth(3, 300, 80));
        Assert.AreEqual(80, BleeckerAppTabModel.TrackWidth(3, double.PositiveInfinity, 80));
        Assert.AreEqual(0, BleeckerAppTabModel.TrackWidth(0, 300, 0));
        Assert.AreEqual(0, BleeckerAppTabModel.TrackWidth(3, 0, 80));
        Assert.Throws<ArgumentException>(() => BleeckerAppTabModel.TrackWidth(3, double.NaN, 80));
    }
    [TestMethod]
    public void NativeNavigationSkipsDisabledDestinationsAndKeepsEnds()
    {
        Assert.AreEqual(2, BleeckerAppTabModel.Navigate([true, false, true], 0, 1));
        Assert.AreEqual(0, BleeckerAppTabModel.Navigate([true, false, true], 2, -1));
        Assert.AreEqual(2, BleeckerAppTabModel.Navigate([true, false, true], 2, 1));
        Assert.AreEqual(0, BleeckerAppTabModel.Navigate([true, false, true], 2, 1, home: true));
        Assert.AreEqual(2, BleeckerAppTabModel.Navigate([true, false, true], 0, -1, end: true));
        Assert.AreEqual(-1, BleeckerAppTabModel.Navigate([false, false], 0, 1));
    }
}
