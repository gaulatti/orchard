using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class CommandSpotlightSurfaceTests
{
    [TestMethod]
    public void ClippedFillEncodingMatchesFrozenBrowserPixels()
    {
        using var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CommandSpotlightSurface.json")));
        var cases = reference.RootElement.GetProperty("cases"); Assert.AreEqual(378, cases.GetArrayLength());
        foreach (var c in cases.EnumerateArray())
        {
            var rgb = c.GetProperty("rgb"); var actual = BleeckerOklabFill.Encode(rgb[0].GetByte(), rgb[1].GetByte(), rgb[2].GetByte(), c.GetProperty("sourceAlphaByte").GetByte(), c.GetProperty("opacity").GetDouble());
            byte[] premultiplied = [actual.R, actual.G, actual.B]; var background = c.GetProperty("background"); var expected = c.GetProperty("expected");
            for (var channel = 0; channel < 3; channel++) Assert.AreEqual(expected[channel].GetInt32(), (int)Math.Floor(premultiplied[channel] + background[channel].GetInt32() * (1 - actual.A / 255d) + .5), c.GetProperty("id").GetString() + "/" + channel);
            Assert.AreEqual(255, expected[3].GetInt32());
        }
    }
    [TestMethod]
    public void TransparentEndpointsProduceTransparentPremultipliedBytes()
    {
        foreach (var alpha in new byte[] { 0, 32, 128, 255 }) Assert.AreEqual(((byte)0, (byte)0, (byte)0, (byte)0), BleeckerOklabFill.Encode(44, 87, 132, alpha, 0));
        foreach (var opacity in new[] { 0, .1, .3, .5, 1 }) Assert.AreEqual(((byte)0, (byte)0, (byte)0, (byte)0), BleeckerOklabFill.Encode(44, 87, 132, 0, opacity));
    }
    [TestMethod]
    public void OpaqueColorsRetainTheirByteValues()
    {
        byte[] values = [0, 1, 32, 64, 128, 192, 254, 255];
        foreach (var r in values) foreach (var g in values) foreach (var b in values) Assert.AreEqual((r, g, b, (byte)255), BleeckerOklabFill.Encode(r, g, b, 255, 1));
        for (var value = 0; value < 256; value++) Assert.AreEqual(((byte)value, (byte)value, (byte)value, (byte)255), BleeckerOklabFill.Encode((byte)value, (byte)value, (byte)value, 255, 1));
    }
    [TestMethod]
    public void InvalidOpacityFailsExplicitly()
    {
        foreach (var opacity in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -.01, 1.01 }) Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BleeckerOklabFill.Encode(44, 87, 132, 255, opacity));
    }
}
