using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class TooltipPlacementTests
{
    [TestMethod]
    public void NativePlacementMatchesTheFrozenRendererIncludingOversizedSurfaces()
    {
        using var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "TooltipPlacement.json")));
        Assert.AreEqual("3.8.1", reference.RootElement.GetProperty("rechartsVersion").GetString());
        foreach (var sample in reference.RootElement.GetProperty("samples").EnumerateArray())
        {
            var actual = BleeckerTooltipPlacement.Position(sample.GetProperty("coordinate").GetDouble(), sample.GetProperty("dimension").GetDouble(), sample.GetProperty("start").GetDouble(), sample.GetProperty("end").GetDouble());
            Assert.AreEqual(sample.GetProperty("expected").GetDouble(), actual, 1e-10, sample.ToString());
        }
    }
}
