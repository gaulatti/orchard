using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Orchard.Tests;

[TestClass]
public sealed class CurrencyChartLayoutTests
{
    [TestMethod]
    public void OriginalRecordsCrossTheFrozenCategoryDomainCurveAndInspectionBoundaries()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "ChartLayout.json")));
        var count = 0;
        foreach (var sample in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var normal = sample.GetProperty("normal"); var id = sample.GetProperty("id").GetString()!;
            var records = (object?[])Read(sample.GetProperty("props").GetProperty("data"))!; var original = JsonSerializer.Serialize(records);
            var layout = BleeckerCurrencyChartLayout.Compute(records, normal.GetProperty("width").GetDouble(), normal.GetProperty("height").GetDouble());
            Assert.AreSame(records, layout.Records, id);
            var clip = normal.GetProperty("clip")[0]; Near(layout.Left, Number(clip, "x"), id); Near(layout.Top, Number(clip, "y"), id); Near(layout.Right - layout.Left, Number(clip, "width"), id); Near(layout.Bottom - layout.Top, Number(clip, "height"), id);
            var curves = normal.GetProperty("curves").EnumerateArray().ToArray(); Assert.AreEqual(curves.Length > 0, layout.HasCurve, id);
            var commands = BleeckerMonotoneCurve.Create(layout.Points);
            if (curves.Length > 0)
            {
                var expected = curves[0].GetProperty("attributes").TryGetProperty("d", out var d) ? d.GetString()! : "";
                var actualKinds = string.Concat(commands.Select(c => c.Kind)); var expectedKinds = string.Concat(Regex.Matches(expected, "[MLCZ]").Select(m => m.Value)); Assert.AreEqual(expectedKinds, actualKinds, id);
                var values = commands.SelectMany(c => c.Values).ToArray(); var numbers = Numbers(expected); Assert.AreEqual(numbers.Length, values.Length, id);
                for (var i = 0; i < values.Length; i++) Near(values[i], numbers[i], id);
            }
            foreach (var tick in normal.GetProperty("yTicks").EnumerateArray()) Assert.IsTrue(layout.Ticks.Any(value => "$" + BleeckerChartScale.TickLabel(value) == tick.GetProperty("text").GetString()), id);
            foreach (var tick in normal.GetProperty("xTicks").EnumerateArray()) Assert.IsTrue(Enumerable.Range(0, layout.Categories.Length).Any(index => layout.AxisText(index) == tick.GetProperty("text").GetString()), id + " category text");
            var interaction = new BleeckerChartInteraction(); interaction.Focus();
            foreach (var state in sample.GetProperty("states").EnumerateArray().Take(5))
            {
                var action = state.GetProperty("action").GetString(); if (action != "Tab") interaction.Key(action == "Enter" ? 0 : action == "ArrowRight" ? 1 : -1, layout.InspectionCount);
                var expected = state.GetProperty("state"); var selected = interaction.Index(layout.InspectionCount);
                var label = selected is int index ? layout.CategoryText(index) : "";
                var payload = selected is int pick ? layout.PayloadIndex(pick) : -1;
                var raw = payload >= 0 ? BleeckerCurrencyChartLayout.Read(records[payload], "total") : null;
                var content = selected is null ? "" : label + (raw is null ? "" : "Total : $" + BleeckerCurrencyChartLayout.StringValue(raw));
                Assert.AreEqual(expected.GetProperty("tooltip").GetProperty("text").GetString(), content, id + " " + action);
                var dot = selected is int active ? layout.ActivePoint(active) : null; var sourceDots = expected.GetProperty("dots").EnumerateArray().ToArray();
                Assert.AreEqual(sourceDots.Length, dot is null ? 0 : 1, id + " " + action);
                if (dot is { } point) { Near(point.X, Number(sourceDots[0].GetProperty("attributes"), "cx"), id); Near(point.Y, Number(sourceDots[0].GetProperty("attributes"), "cy"), id); }
            }
            Assert.AreEqual(original, JsonSerializer.Serialize(records), "Layout mutated consumer records: " + id); count++;
        }
        Assert.AreEqual(94, count);
    }
    private static double Number(JsonElement element, string key) => double.Parse(element.GetProperty(key).GetString()!, CultureInfo.InvariantCulture);
    private static double[] Numbers(string value) => Regex.Matches(value, @"[-+]?(?:\d*\.)?\d+(?:[eE][-+]?\d+)?").Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
    private static void Near(double actual, double expected, string id) => Assert.IsLessThan(.001, Math.Abs(actual - expected), id + $" {actual:R}/{expected:R}");
    private static object? Read(JsonElement value) => value.ValueKind switch { JsonValueKind.Object => value.EnumerateObject().ToDictionary(p => p.Name, p => Read(p.Value)), JsonValueKind.Array => value.EnumerateArray().Select(Read).ToArray(), JsonValueKind.Number => value.GetDouble(), JsonValueKind.String => value.GetString(), JsonValueKind.Null => null, JsonValueKind.True => true, JsonValueKind.False => false, _ => throw new InvalidOperationException("Unknown source value.") };
}
