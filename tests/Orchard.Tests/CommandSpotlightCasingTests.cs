using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class CommandSpotlightCasingTests
{
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CommandSpotlightHotkeys.json")));
    private static string Units(JsonElement array) => new(array.EnumerateArray().Select(v => (char)v.GetInt32()).ToArray());
    [TestMethod]
    public void NativeUpperMatchesEverySourceUnicodeScalar()
    {
        using var data = Reference();
        var map = data.RootElement.GetProperty("casing").GetProperty("upper").EnumerateArray().ToDictionary(v => v[0].GetInt32(), v => v[1].GetString()!);
        for (var cp = 0; cp <= 0x10ffff; cp++)
        {
            if (cp is >= 0xd800 and <= 0xdfff) continue;
            var text = char.ConvertFromUtf32(cp);
            Assert.AreEqual(map.GetValueOrDefault(cp, text), BleeckerCommandSpotlightCasing.Upper(text), $"U+{cp:X}");
        }
        foreach (var item in data.RootElement.GetProperty("casing").GetProperty("contexts").EnumerateArray())
            Assert.AreEqual(Units(item.GetProperty("upperUnits")), BleeckerCommandSpotlightCasing.Upper(Units(item.GetProperty("textUnits"))));
    }
    [TestMethod]
    public void NativeLowerMatchesEverySourceUnicodeScalar()
    {
        using var data = Reference();
        var map = data.RootElement.GetProperty("casing").GetProperty("lower").EnumerateArray().ToDictionary(v => v[0].GetInt32(), v => v[1].GetString()!);
        var failures = new List<string>(); var differences = 0;
        for (var cp = 0; cp <= 0x10ffff; cp++)
        {
            if (cp is >= 0xd800 and <= 0xdfff) continue;
            var text = char.ConvertFromUtf32(cp); var expected = map.GetValueOrDefault(cp, text);
            var actual = BleeckerCommandSpotlightState<string>.Lower(text);
            if (actual == expected) continue;
            differences++; if (failures.Count < 20) failures.Add($"U+{cp:X}: expected {string.Join(' ', expected.Select(c => ((int)c).ToString("X4")))}, actual {string.Join(' ', actual.Select(c => ((int)c).ToString("X4")))}");
        }
        Assert.AreEqual(0, differences, string.Join("; ", failures));
    }
    [TestMethod]
    public void NativeLowerPreservesSourceContextAndUtf16Values()
    {
        using var data = Reference(); var failures = new List<string>();
        foreach (var item in data.RootElement.GetProperty("casing").GetProperty("contexts").EnumerateArray())
        {
            var text = Units(item.GetProperty("textUnits")); var expected = Units(item.GetProperty("lowerUnits"));
            if (expected != BleeckerCommandSpotlightState<string>.Lower(text)) failures.Add(string.Join(' ', text.Select(c => ((int)c).ToString("X4"))));
        }
        Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
    }
    [TestMethod]
    public void FinalSigmaMatchesTheSourceContextClassesForEveryScalar()
    {
        using var data = Reference(); var casing = data.RootElement.GetProperty("casing");
        var cased = casing.GetProperty("effectiveCasedRanges").EnumerateArray().Select(v => (v[0].GetInt32(),v[1].GetInt32())).ToArray();
        var ignored = casing.GetProperty("caseIgnorableRanges").EnumerateArray().Select(v => (v[0].GetInt32(),v[1].GetInt32())).ToArray();
        bool Contains((int,int)[] ranges,int cp) => ranges.Any(r => cp>=r.Item1&&cp<=r.Item2);
        for(var cp=0;cp<=0x10ffff;cp++)
        {
            if(cp is >=0xd800 and <=0xdfff)continue;var text=char.ConvertFromUtf32(cp);
            var isCased=Contains(cased,cp);var significant=isCased||Contains(ignored,cp);
            Assert.AreEqual(isCased,BleeckerCommandSpotlightState<string>.Lower(text+"Σ").EndsWith('ς'),$"before/U+{cp:X}");
            Assert.AreEqual(significant,BleeckerCommandSpotlightState<string>.Lower("A"+text+"Σ").EndsWith('ς'),$"ignored/U+{cp:X}");
            Assert.AreEqual(significant,BleeckerCommandSpotlightState<string>.Lower("AΣ"+text+"A").StartsWith("aσ",StringComparison.Ordinal),$"after/U+{cp:X}");
        }
    }
}
