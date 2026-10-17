using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class MetricReferenceTests
{
    [TestMethod]
    public void FrozenRichMetricRetainsOriginalControlWithoutFocusEvents()
    {
        using var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "MetricRich.json")));
        var root = reference.RootElement;
        Assert.AreEqual("2aeb702dc6a115a9b24ff62842d564a9fcd92f1069925b3e07929532daca47c7", root.GetProperty("sourceSha256").GetString());
        var script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CaptureMetricRich.cjs")).Replace("\r\n", "\n");
        Assert.AreEqual(root.GetProperty("scriptSha256").GetString(), Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(script))));
        Assert.AreEqual(20, root.GetProperty("cases").GetArrayLength());
        foreach (var item in root.GetProperty("cases").EnumerateArray())
        {
            var state = item.GetProperty("state");
            if (item.GetProperty("id").GetString() == "outside-focus") Assert.IsTrue(state.GetProperty("outsideFocusRetained").GetBoolean());
            else
            {
                Assert.IsTrue(state.GetProperty("focused").GetBoolean() && state.GetProperty("sameNode").GetBoolean());
                Assert.AreEqual(0, state.GetProperty("events").GetArrayLength());
                Assert.AreEqual(0, state.GetProperty("lifecycle").GetArrayLength());
            }
        }
    }

    [TestMethod]
    public void MetricCapturePinsSourceToolsAndAllUniqueCases()
    {
        using var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Metric.json")));
        var root = reference.RootElement;
        Assert.AreEqual("2aeb702dc6a115a9b24ff62842d564a9fcd92f1069925b3e07929532daca47c7", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual("e7c2b863087ac5e0c06973e27abbcab597f745b0540cd8698def589a8fe7129c", root.GetProperty("formatterSha256").GetString());
        var script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CaptureMetric.cjs")).Replace("\r\n", "\n");
        Assert.AreEqual(root.GetProperty("scriptSha256").GetString(), Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(script))));
        Assert.AreEqual(680, root.GetProperty("cases").EnumerateArray().Select(item => item.GetProperty("id").GetString() + "/" + item.GetProperty("locale").GetString() + "/" + item.GetProperty("scheme").GetString()).Distinct().Count());
    }

    [TestMethod]
    public void WindowsFormatterMatchesFrozenMetricAcrossLocalesAndFormats()
    {
        using var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Metric.json")));
        var cases = reference.RootElement.GetProperty("cases");
        Assert.AreEqual(680, cases.GetArrayLength());
        foreach (var item in cases.EnumerateArray())
        {
            var value = item.GetProperty("value");
            double? number = value.ValueKind == JsonValueKind.Null ? null : value.ValueKind == JsonValueKind.Number ? value.GetDouble() : value.GetString() switch
            { "NaN" => double.NaN, "Infinity" => double.PositiveInfinity, "-Infinity" => double.NegativeInfinity, "-0" => -0d, _ => throw new InvalidDataException() };
            var format = item.TryGetProperty("format", out var f) ? Enum.Parse<BleeckerMetricFormat>(f.GetString()!, true) : BleeckerMetricFormat.Number;
            var currency = item.TryGetProperty("currency", out var c) ? c.GetString()! : "USD";
            int? decimals = item.TryGetProperty("decimals", out var d) ? d.GetInt32() : null;
            var actual = BleeckerMetricFormatter.Format(number, format, currency, decimals, item.GetProperty("locale").GetString()!);
            var prefix = item.TryGetProperty("prefix", out var p) ? p.GetString() : "";
            var suffix = item.TryGetProperty("suffix", out var s) ? s.GetString() : "";
            Assert.AreEqual(item.GetProperty("state").GetProperty("text").GetString(), prefix + actual + suffix, item.GetProperty("id").GetString() + "/" + item.GetProperty("locale").GetString());
        }
    }

    [TestMethod]
    public void InvalidFormattingFailsAndMissingValuesStayEmpty()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BleeckerMetricFormatter.Format(1, BleeckerMetricFormat.Number, "USD", -1, "en-US"));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BleeckerMetricFormatter.Format(1, BleeckerMetricFormat.Number, "USD", 101, "en-US"));
        Assert.ThrowsExactly<ArgumentException>(() => BleeckerMetricFormatter.Format(1, BleeckerMetricFormat.Currency, "US", null, "en-US"));
        Assert.AreEqual("", BleeckerMetricFormatter.Format(null, BleeckerMetricFormat.Currency, "US", -1, "en-US"));
    }
}
