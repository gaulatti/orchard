using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class LineDataTests
{
    [TestMethod]
    public void NativeFiveTickDomainsMatchTheFrozenProducer()
    {
        using var reference = Reference();
        foreach (var sample in reference.RootElement.GetProperty("scale").EnumerateArray())
        {
            var input = sample.GetProperty("values").EnumerateArray().Select(item => item.GetDouble()).ToArray();
            var expected = sample.GetProperty("ticks").EnumerateArray().Select(item => item.GetDouble()).ToArray();
            var actual = BleeckerChartScale.Ticks(input); Assert.AreEqual(expected.Length, actual.Length);
            for (var index = 0; index < expected.Length; index++) Assert.AreEqual(expected[index], actual[index], Math.Max(1e-12, Math.Abs(expected[index]) * 1e-12), sample.ToString());
        }
    }
    [TestMethod]
    public void NativeDataKeysPreserveLiteralPrecedenceAndFrozenPathGrammar()
    {
        using var reference = Reference();
        foreach (var sample in reference.RootElement.GetProperty("records").EnumerateArray())
        {
            var record = (IReadOnlyDictionary<string, object?>)Value(sample.GetProperty("record"))!;
            Assert.AreEqual(Value(sample.GetProperty("value")), BleeckerChartData.Read(record, sample.GetProperty("key").GetString()!), sample.ToString());
        }
    }
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "LineData.json")));
    [TestMethod]
    public void NativeNumericDomainValuesMatchTheFrozenSelector()
    {
        using var reference = Reference();
        foreach (var sample in reference.RootElement.GetProperty("numbers").EnumerateArray())
            Assert.AreEqual(Value(sample.GetProperty("value")), BleeckerChartData.Number(Value(sample.GetProperty("input"))), sample.ToString());
    }
    private static object? Value(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => value.EnumerateObject().ToDictionary(item => item.Name, item => Value(item.Value)),
        JsonValueKind.Array => value.EnumerateArray().Select(Value).ToArray(),
        JsonValueKind.Number => value.GetDouble(),
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Null => null,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => throw new InvalidOperationException("Unknown frozen data value.")
    };
}
