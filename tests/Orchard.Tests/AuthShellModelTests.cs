using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class AuthShellModelTests
{
    [TestMethod]
    public void NativeTracksAndInsetsMatchEveryFrozenOriginalState()
    {
        using var doc = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "AuthShell.json")));
        foreach (var c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            var s = c.GetProperty("spec"); var m = c.GetProperty("measurement"); var width = s.GetProperty("width").GetDouble(); var rem = s.TryGetProperty("rootFont", out var font) ? font.GetDouble() / 16 : 1;
            var hasAside = m.GetProperty("aside").ValueKind != JsonValueKind.Null;
            var side = s.TryGetProperty("side", out var v) && v.GetString() == "start" ? BleeckerAuthShellSide.Start : BleeckerAuthShellSide.End;
            var tracks = BleeckerAuthShellModel.Columns(width, width, rem, hasAside, side); var access = m.GetProperty("access").GetProperty("box");
            var rtl = s.TryGetProperty("rtl", out var direction) && direction.GetBoolean(); var x = rtl ? width - tracks.AccessX - tracks.AccessWidth : tracks.AccessX;
            Assert.AreEqual(x, access.GetProperty("x").GetDouble(), 1d / 64, c.GetProperty("id").GetString()); Assert.AreEqual(tracks.AccessWidth, access.GetProperty("width").GetDouble(), 1d / 64);
            var spacing = BleeckerAuthShellModel.Insets(width, rem); var padding = m.GetProperty("access").GetProperty("paint").GetProperty("padding");
            Assert.AreEqual(spacing.Horizontal, padding[0].GetDouble()); Assert.AreEqual(spacing.Vertical, padding[1].GetDouble());
            Assert.AreEqual(spacing.ContentVertical, m.GetProperty("content").GetProperty("paint").GetProperty("padding")[1].GetDouble());
            Assert.AreEqual(Math.Min(spacing.MaximumContent, Math.Max(0, tracks.AccessWidth - spacing.Horizontal * 2)), m.GetProperty("inner").GetProperty("box").GetProperty("width").GetDouble(), 1d / 64);
        }
    }
    [TestMethod]
    public void BreakpointsFollowViewportWhileColumnWidthsFollowContainer()
    {
        Assert.IsFalse(BleeckerAuthShellModel.Columns(1440, 1023, 2, true, BleeckerAuthShellSide.Start).AsideVisible);
        var narrow = BleeckerAuthShellModel.Columns(300, 1440, 2, true, BleeckerAuthShellSide.Start);
        Assert.IsTrue(narrow.AsideVisible); Assert.AreEqual(768, narrow.AsideWidth); Assert.AreEqual(256, narrow.AccessWidth);
        var absent = BleeckerAuthShellModel.Columns(300, 1440, 1, false, BleeckerAuthShellSide.End); Assert.AreEqual(300, absent.AccessWidth); Assert.IsFalse(absent.AsideVisible);
    }
    [TestMethod]
    public void InvalidDimensionsAndUnknownSidesFailExplicitly()
    {
        foreach (var value in new[] { double.NaN, double.PositiveInfinity, -1d }) Assert.Throws<ArgumentOutOfRangeException>(() => BleeckerAuthShellModel.Columns(value, 1024, 1, true, BleeckerAuthShellSide.End));
        foreach (var value in new[] { double.NaN, double.PositiveInfinity, -1d, 0d }) Assert.Throws<ArgumentOutOfRangeException>(() => BleeckerAuthShellModel.Insets(1024, value));
        Assert.Throws<ArgumentOutOfRangeException>(() => BleeckerAuthShellModel.Columns(1024, 1024, 1, true, (BleeckerAuthShellSide)99));
    }
}
