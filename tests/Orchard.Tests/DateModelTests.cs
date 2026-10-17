using System.Globalization;
using Orchard;

namespace Orchard.Tests;

[TestClass]
public sealed class DateModelTests
{
    [TestMethod]
    public void CalendarTracksRetainFrozenBrowserLayoutUnitDistribution()
    {
        CollectionAssert.AreEqual(new[] { 40d, 40, 40, 40, 40, 40, 40 }, BleeckerDates.Columns(280));
        CollectionAssert.AreEqual(new[] { 85.703125, 85.71875, 85.71875, 85.703125, 85.71875, 85.71875, 85.71875 }, BleeckerDates.Columns(600));
        CollectionAssert.AreEqual(new[] { 36.28125, 36.28125, 36.28125, 36.296875, 36.28125, 36.28125, 36.28125 }, BleeckerDates.Columns(254));
    }

    [TestMethod]
    public void SourceDateConstructionKeepsOverflowAndSmallYearSemantics()
    {
        Assert.AreEqual("2026-03-02", BleeckerDates.Serialize(BleeckerDates.Parse("2026-02-30")));
        Assert.AreEqual("1999-01-01", BleeckerDates.Serialize(BleeckerDates.Parse("0099-01-01")));
        Assert.AreEqual("2025-12-05", BleeckerDates.Serialize(BleeckerDates.Parse("2026-00-05")));
        Assert.AreEqual("2026-09-30", BleeckerDates.Serialize(BleeckerDates.Parse("2026-10-00")));
        var now = BleeckerDates.Midnight(new DateTime(2026, 10, 17)).AddHours(12);
        Assert.AreEqual("2026-10-14", BleeckerDates.Preset(now, 2.5).From);
        Assert.AreEqual("2026-10-19", BleeckerDates.Preset(now, -2.5).From);
        Assert.AreEqual("2026-10-16", BleeckerDates.Preset(now, .5).From);
    }

    [TestMethod]
    public void RangeKeepsDateOnlyFieldsAndPresetsKeepTheOtherEndpoint()
    {
        var original = new BleeckerDateRange("2026-10-10", "2026-10-20");
        Assert.AreEqual(new BleeckerDateRange("2026-10-15", "2026-10-20"), original with { From = "2026-10-15" });
        Assert.AreEqual("2026-10-10", original.From);
        Assert.IsNull(BleeckerDates.Parse(null)); Assert.IsNull(BleeckerDates.Parse(""));
        var date = BleeckerDates.Parse("2024-02-29");
        Assert.AreEqual("2024-02-29", BleeckerDates.Serialize(date));
        Assert.AreEqual("Feb 29, 2024", BleeckerDates.Display(date!.Value));
    }

    [TestMethod]
    public void DateRangePresetsUseLocalCalendarDatesAndPreserveZeroAndNegativeDays()
    {
        var noon = BleeckerDates.Midnight(new DateTime(2026, 10, 17)).AddHours(12);
        Assert.AreEqual(new BleeckerDateRange("2026-10-10", "2026-10-17"), BleeckerDates.Preset(noon, 7));
        Assert.AreEqual(new BleeckerDateRange("2026-10-17", "2026-10-17"), BleeckerDates.Preset(noon, 0));
        Assert.AreEqual(new BleeckerDateRange("2026-10-20", "2026-10-17"), BleeckerDates.Preset(noon, -3));
        var spring = BleeckerDates.Midnight(new DateTime(2026, 3, 9)).AddMinutes(30);
        Assert.AreEqual(new BleeckerDateRange("2026-03-02", "2026-03-09"), BleeckerDates.Preset(spring, 7));
    }

    [TestMethod]
    public void CalendarCellMidnightRetainsTimestampBounds()
    {
        var cell = BleeckerDates.Parse("2026-10-17")!.Value;
        Assert.IsTrue(cell < cell.AddHours(12));
        Assert.AreEqual(TimeSpan.Zero, cell.TimeOfDay);
        Assert.AreEqual(TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 10, 17)), cell.Offset);
        Assert.AreEqual(new DateTime(2026, 10, 17), BleeckerDates.LocalDate(cell));
    }
}
