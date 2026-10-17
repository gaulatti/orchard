using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class DateTypographyReferenceTests
{
    [TestMethod]
    public void TypographyRetainsEveryFrozenSourceFrameWithoutDomMutations()
    {
        using var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "DateControls.json")));
        using var typography = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "DateTypography.json")));
        var root = typography.RootElement; Assert.AreEqual("orchard-date-typography-v1", root.GetProperty("format").GetString());
        Assert.IsTrue(root.GetProperty("measurementPixelsUnchanged").GetBoolean());
        Assert.AreEqual(reference.RootElement.GetProperty("clientSha256").GetString(), root.GetProperty("clientSha256").GetString());
        Assert.AreEqual(reference.RootElement.GetProperty("scriptSha256").GetString(), root.GetProperty("scriptSha256").GetString());
        var expected = reference.RootElement.GetProperty("cases").EnumerateArray().ToDictionary(frame => frame.GetProperty("file").GetString()!);
        Assert.AreEqual(188, root.GetProperty("cases").GetArrayLength()); var count = 0;
        foreach (var frame in root.GetProperty("cases").EnumerateArray())
        {
            var original = expected[frame.GetProperty("file").GetString()!];
            Assert.AreEqual(original.GetProperty("pngSha256").GetString(), frame.GetProperty("pngSha256").GetString());
            Assert.AreEqual(frame.GetProperty("pngSha256").GetString(), frame.GetProperty("measurementPngSha256").GetString());
            foreach (var text in frame.GetProperty("texts").EnumerateArray())
            {
                var ascent = text.GetProperty("ascent").GetDouble(); var descent = text.GetProperty("descent").GetDouble(); var scale = text.GetProperty("verticalScale").GetDouble();
                Assert.IsTrue(ascent > 0 && descent >= 0 && scale > 0);
                Assert.AreEqual(text.GetProperty("textBox").GetProperty("y").GetDouble() + ascent * scale, text.GetProperty("baseline").GetDouble(), .000001);
                if (text.GetProperty("role").GetString()!.StartsWith("day-", StringComparison.Ordinal)) Assert.AreEqual("tabular-nums", text.GetProperty("numerals").GetString());
                if (text.GetProperty("role").GetString() == "heading") Assert.AreEqual("normal", text.GetProperty("letterSpacing").GetString());
                count++;
            }
        }
        Assert.AreEqual(6076, count);
    }
}
