using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;

namespace Orchard.Tests;

[TestClass]
public sealed class CollectionFiltersContractTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllBytes(Fixture("CollectionFilters.json")));
    private static string DateLabel(DateTimeOffset value) => value.LocalDateTime.ToString("MMM d, yyyy", CultureInfo.GetCultureInfo("en-US"));
    private static BleeckerCollectionFilterOption[] Options(JsonElement spec) => spec.GetProperty("filterOptions").EnumerateArray().Select(option => new BleeckerCollectionFilterOption(option.GetProperty("field").GetString()!, option.GetProperty("label").GetString()!, Enum.Parse<BleeckerCollectionFilterType>(option.GetProperty("type").GetString()!, true), option.TryGetProperty("options", out var values) ? values.EnumerateArray().Select(value => new BleeckerSelectOption(value.GetProperty("value").GetString()!, value.GetProperty("label").GetString()!)).ToArray() : null)).ToArray();
    private static Dictionary<string, BleeckerCollectionFilterValue> Values(JsonElement values) => values.EnumerateObject().ToDictionary(value => value.Name, value => value.Value.ValueKind is JsonValueKind.True or JsonValueKind.False ? new BleeckerCollectionFilterValue(value.Value.GetBoolean()) : new(value.Value.GetString()!));
    private static void Same(IReadOnlyDictionary<string, BleeckerCollectionFilterValue> actual, JsonElement wanted)
    {
        CollectionAssert.AreEqual(wanted.EnumerateObject().Select(value => value.Name).ToArray(), actual.Keys.ToArray());
        foreach (var (field, value) in Values(wanted)) Assert.AreEqual(value, actual[field], field);
    }
    [TestMethod]
    public void FrozenReferenceBindsSourceCollectorClientAndEveryRawPng()
    {
        using var reference = Reference(); var root = reference.RootElement;
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("commit").GetString());
        Assert.AreEqual("9b220e110dde6293e081ff555c6b69c9a0e34fee352e5b46888646b418774d69", root.GetProperty("sourceSha256").GetString());
        foreach (var (file, field) in new[] { ("CaptureCollectionFilters.cjs", "scriptSha256"), ("CollectionFiltersClient.tsx", "clientSha256") }) Assert.AreEqual(root.GetProperty(field).GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));
        var cases = root.GetProperty("cases").EnumerateArray().ToArray(); Assert.AreEqual(50, cases.Length);
        foreach (var row in cases) Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture("CollectionFilters/" + row.GetProperty("file").GetString())))));
    }
    [TestMethod]
    public void BrowserTokenSwatchesBindTheOwnedPaletteWithoutFractionalOpacity()
    {
        using var reference = JsonDocument.Parse(File.ReadAllBytes(Fixture("CollectionFiltersPaint.json")));
        var root = reference.RootElement;
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("commit").GetString());
        Assert.AreEqual(Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture("CollectionFilters.json")))), root.GetProperty("referenceSha256").GetString());
        Assert.AreEqual(Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture("CaptureCollectionFiltersPaint.cjs")))), root.GetProperty("scriptSha256").GetString());
        Assert.IsFalse(root.GetProperty("sourceScenePixelsPainted").GetBoolean());
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml/presentation", x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var palette = XDocument.Load(Fixture("CollectionFiltersPalette.g.xaml")); var brushes = 0; var swatches = 0;
        foreach (var theme in root.GetProperty("themes").EnumerateArray())
        {
            var key = theme.GetProperty("theme").GetString() == "Dark" ? "Default" : "Light";
            var dictionary = palette.Descendants(xaml + "ResourceDictionary").Single(element => (string?)element.Attribute(x + "Key") == key);
            foreach (var paint in theme.GetProperty("paints").EnumerateArray())
            {
                Assert.AreEqual(paint.GetProperty("pngSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture("CollectionFiltersPaint/" + paint.GetProperty("file").GetString()))))); swatches++;
            }
            foreach (var brush in dictionary.Elements(xaml + "SolidColorBrush"))
            {
                var name = ((string)brush.Attribute(x + "Key")!)["Collection".Length..]; name = char.ToLowerInvariant(name[0]) + name[1..];
                var paint = theme.GetProperty("paints").EnumerateArray().Single(paint => paint.GetProperty("key").GetString() == name);
                Assert.AreEqual(paint.GetProperty("domArgb").GetString()!.ToUpperInvariant(), (string?)brush.Attribute("Color"));
                Assert.IsNull(brush.Attribute("Opacity")); brushes++;
            }
        }
        Assert.AreEqual(18, brushes); Assert.AreEqual(22, swatches);
    }
    [TestMethod]
    public void OriginalFalseEmptyUnknownAndDateValuesReachTheSourceChipText()
    {
        using var reference = Reference(); var chips = 0;
        foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec = row.GetProperty("spec"); var options = Options(spec); var current = Values(spec.GetProperty("currentFilters")); var measurement = row.GetProperty("measurement");
            Assert.AreEqual(options.Length != 0 || spec.GetProperty("sortOptions").GetArrayLength() != 0, measurement.GetProperty("visible").GetBoolean());
            foreach (var chip in measurement.GetProperty("chips").EnumerateArray())
            {
                var label = chip.GetProperty("label").GetString(); var field = current.Keys.First(field => (options.FirstOrDefault(option => option.Field == field)?.Label ?? field) == label); var option = options.FirstOrDefault(option => option.Field == field);
                Assert.AreEqual(chip.GetProperty("value").GetString(), BleeckerCollectionFiltersModel.Display(option, current[field], DateLabel)); chips++;
            }
        }
        Assert.IsTrue(chips > 60);
    }
    [TestMethod]
    public void RequestsMatchAcceptedAndDeclinedBrowserHistoriesWithoutAssigningTheProducer()
    {
        using var reference = Reference(); var root = reference.RootElement; var initial = root.GetProperty("cases").EnumerateArray().First(row => row.GetProperty("id").GetString() == "active-expanded").GetProperty("spec"); var steps = 0;
        foreach (var interaction in root.GetProperty("interactions").EnumerateArray())
        {
            var accepted = interaction.GetProperty("accept").GetBoolean(); var current = Values(initial.GetProperty("currentFilters")); var sort = new BleeckerSortState("createdAt", BleeckerSortOrder.Descending); var requests = 0;
            foreach (var sample in interaction.GetProperty("samples").EnumerateArray())
            {
                var action = sample.GetProperty("action").GetString(); var emitted = sample.GetProperty("requests").GetArrayLength() > requests;
                if (action == "programmatic-default-ignored") current = new() { ["featured"] = false, ["missing"] = "Original" };
                if (emitted)
                {
                    var request = sample.GetProperty("requests").EnumerateArray().Last();
                    if (request.GetProperty("kind").GetString() == "filters")
                    {
                        IReadOnlyDictionary<string, BleeckerCollectionFilterValue> next = action switch { "remove-false" => BleeckerCollectionFiltersModel.Change(current, "featured", null), "select" => BleeckerCollectionFiltersModel.Change(current, "status", new("available")), "boolean-false" => BleeckerCollectionFiltersModel.Change(current, "featured", new(false)), "clear" => new Dictionary<string, BleeckerCollectionFilterValue>(), _ => throw new InvalidOperationException(action) };
                        Same(next, request.GetProperty("value")); if (accepted) current = new(next);
                    }
                    else
                    {
                        var next = action == "sort-field" ? BleeckerCollectionFiltersModel.Field(sort, "title") : BleeckerCollectionFiltersModel.Toggle(sort); var wanted = request.GetProperty("value");
                        Assert.AreEqual(wanted.GetProperty("field").GetString(), next.Field); Assert.AreEqual(wanted.GetProperty("order").GetString(), next.Order == BleeckerSortOrder.Ascending ? "asc" : "desc"); if (accepted) sort = next;
                    }
                }
                if (action is "collapse" or "programmatic-default-ignored") Assert.IsFalse(emitted);
                requests = sample.GetProperty("requests").GetArrayLength(); Same(current, sample.GetProperty("currentFilters")); Assert.AreEqual(sample.GetProperty("currentSort").GetProperty("field").GetString(), sort.Field); steps++;
            }
        }
        Assert.AreEqual(32, steps);
    }
    [TestMethod]
    public void LocalDateRolloverAndFalsyParsingMatchTheActualBrowser()
    {
        using var reference = Reference(); var rows = 0;
        foreach (var sample in reference.RootElement.GetProperty("dateValues").EnumerateArray())
        {
            var value = sample.GetProperty("value").GetString()!; var date = BleeckerCollectionFiltersModel.ParseDate(value);
            Assert.AreEqual(sample.GetProperty("chip").GetString(), date is null ? value : DateLabel(date.Value));
            Assert.AreEqual(sample.GetProperty("date").GetString(), date is null ? "Choose added after" : DateLabel(date.Value));
            if (date is not null) Assert.AreEqual(date.Value.LocalDateTime.Date, BleeckerCollectionFiltersModel.ParseDate(BleeckerCollectionFiltersModel.SerializeDate(date))!.Value.LocalDateTime.Date); rows++;
        }
        Assert.AreEqual(16, rows);
        Assert.AreEqual("12-01-02", BleeckerCollectionFiltersModel.SerializeDate(BleeckerDates.Midnight(new DateTime(12, 1, 2))));
    }
    [TestMethod]
    public void ViewportBreakpointsFollowTheSourceEvenInAConstrainedContainer()
    {
        using var reference = Reference();
        foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec = row.GetProperty("spec"); var fields = row.GetProperty("measurement").GetProperty("fields").EnumerateArray().ToArray(); if (fields.Length == 0) continue;
            var columns = BleeckerCollectionFiltersModel.Columns(spec.GetProperty("viewportWidth").GetDouble()); var firstTop = fields[0].GetProperty("box").GetProperty("y").GetDouble();
            Assert.AreEqual(Math.Min(columns, fields.Length), fields.Count(field => field.GetProperty("box").GetProperty("y").GetDouble() == firstTop));
            var left = fields.Min(field => field.GetProperty("box").GetProperty("x").GetDouble()); var inset = BleeckerCollectionFiltersModel.HorizontalPadding(spec.GetProperty("viewportWidth").GetDouble()); Assert.AreEqual(20 + 1 + inset, left, .016);
        }
    }
    [TestMethod]
    public void NumericFieldRequestsMatchBrowserOrderWithoutReorderingTheOriginalDictionary()
    {
        using var reference = Reference(); var root = reference.RootElement;
        var spec = root.GetProperty("cases").EnumerateArray().Single(row => row.GetProperty("scheme").GetString() == "Light" && row.GetProperty("id").GetString() == "numeric-keys").GetProperty("spec");
        var count = 0;
        foreach (var history in root.GetProperty("keyOrderInteractions").EnumerateArray())
        {
            var original = spec.GetProperty("filterEntries").EnumerateArray().ToDictionary(pair => pair[0].GetString()!, pair => pair[1].ValueKind is JsonValueKind.True or JsonValueKind.False ? new BleeckerCollectionFilterValue(pair[1].GetBoolean()) : new(pair[1].GetString()!));
            var snapshot = original.ToArray(); IReadOnlyDictionary<string, BleeckerCollectionFilterValue> current = original;
            foreach (var sample in history.GetProperty("samples").EnumerateArray().Skip(1))
            {
                var next = BleeckerCollectionFiltersModel.Change(current, "2", sample.GetProperty("action").GetString() == "remove-index" ? null : new(true));
                var expected = sample.GetProperty("requestKeys").EnumerateArray().Last().EnumerateArray().Select(key => key.GetString()!).ToArray();
                CollectionAssert.AreEqual(expected, next.Keys.ToArray());
                Same(next, sample.GetProperty("requests").EnumerateArray().Last().GetProperty("value"));
                Assert.AreNotSame(original, next); if (history.GetProperty("accept").GetBoolean()) current = next; count++;
            }
            CollectionAssert.AreEqual(snapshot, original.ToArray());
        }
        Assert.AreEqual(8, count);
    }
    [TestMethod]
    public void ARequestedChangePreservesOriginalFalseValuesAndUnrelatedInsertionOrder()
    {
        var original = new Dictionary<string, BleeckerCollectionFilterValue> { ["unknown"] = "kept", ["featured"] = false, ["status"] = "available" };
        var next = BleeckerCollectionFiltersModel.Change(original, "status", new(""));
        Assert.AreEqual(3, original.Count); Assert.AreEqual(new BleeckerCollectionFilterValue("available"), original["status"]); Assert.AreEqual(new BleeckerCollectionFilterValue(false), next["featured"]); CollectionAssert.AreEqual(new[] { "unknown", "featured" }, next.Keys.ToArray());
    }
}
