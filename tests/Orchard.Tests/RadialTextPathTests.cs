using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Orchard;

namespace Orchard.Tests;

[TestClass]
public sealed class RadialTextPathTests
{
    [TestMethod]
    public void SvgPathDistancesReachTheCapturedBrowserGlyphPositions()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "RadialBarChartLayout.json")));
        var checkedGlyphs = 0;
        foreach (var sample in document.RootElement.GetProperty("cases").EnumerateArray())
        foreach (var label in sample.GetProperty("normal").GetProperty("labels").EnumerateArray())
        {
            var values = Regex.Matches(label.GetProperty("path").GetString()!, @"[-+]?(?:\d*\.)?\d+(?:[eE][-+]?\d+)?").Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
            if (values[2] <= 0) continue;
            var measure = new BleeckerSvgCircleMeasure(new((float)values[0], (float)values[1]), new((float)values[7], (float)values[8]), (float)values[2], values[6] == 1); var offset = 0f;
            foreach (var glyph in label.GetProperty("characters").EnumerateArray())
            {
                if (glyph.TryGetProperty("error", out _)) continue;
                static Vector2 Point(JsonElement value) => new(value.GetProperty("x").GetSingle(), value.GetProperty("y").GetSingle());
                var start = Point(glyph.GetProperty("start")); var end = Point(glyph.GetProperty("end")); var width = MathF.Round(Vector2.Distance(start, end) * 64) / 64; if (width == 0) continue;
                var position = measure.At(offset + width / 2); offset += width; var actualStart = position.Point - position.Tangent * (width / 2); var actualEnd = position.Point + position.Tangent * (width / 2);
                var context = sample.GetProperty("id").GetString() + "/" + label.GetProperty("text").GetString();
                Assert.IsLessThan(.001f, MathF.Abs(actualStart.X - start.X), context); Assert.IsLessThan(.001f, MathF.Abs(actualStart.Y - start.Y), context);
                Assert.IsLessThan(.001f, MathF.Abs(actualEnd.X - end.X), context); Assert.IsLessThan(.001f, MathF.Abs(actualEnd.Y - end.Y), context); checkedGlyphs++;
            }
        }
        Assert.AreEqual(550, checkedGlyphs, "The paired source glyph boundary changed.");
    }
}
