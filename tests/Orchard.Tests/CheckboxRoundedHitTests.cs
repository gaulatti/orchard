using System.Text.Json;
using Orchard;

namespace Orchard.Tests;

[TestClass]
public sealed class CheckboxRoundedHitTests
{
    [TestMethod]
    public void DisabledHitRegionMatchesEveryCapturedBrowserPixelInBothThemes()
    {
        using var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CheckboxRoundedHit.json")));
        var captures = reference.RootElement.GetProperty("captures");
        Assert.AreEqual(2, captures.GetArrayLength());
        foreach (var capture in captures.EnumerateArray())
        {
            Assert.AreEqual(1, capture.GetProperty("devicePixelRatio").GetDouble());
            foreach (var row in capture.GetProperty("rows").EnumerateArray())
            {
                var width = row.GetProperty("width").GetDouble();
                var height = row.GetProperty("height").GetDouble();
                Assert.AreEqual("4px", row.GetProperty("radius").GetString());
                var mask = row.GetProperty("mask");
                Assert.AreEqual(height, mask.GetArrayLength());
                for (var y = 0; y < mask.GetArrayLength(); y++)
                {
                    var pixels = mask[y].GetString()!;
                    Assert.AreEqual(width, pixels.Length);
                    for (var x = 0; x < pixels.Length; x++) Check(x, y, pixels[x] == '1');
                }
                foreach (var point in row.GetProperty("points").EnumerateArray())
                    Check(point.GetProperty("x").GetDouble(), point.GetProperty("y").GetDouble(), point.GetProperty("input").GetBoolean());

                void Check(double x, double y, bool expected) => Assert.AreEqual(expected,
                    BleeckerRoundedPointerHit.Contains(width, height, 4, 4, 4, 4, x, y),
                    $"{capture.GetProperty("theme").GetString()} {row.GetProperty("id").GetString()} at {x},{y}");
            }
        }
    }
}
