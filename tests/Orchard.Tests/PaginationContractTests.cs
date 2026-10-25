using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class PaginationContractTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
    private static JsonDocument Read() => JsonDocument.Parse(File.ReadAllBytes(Fixture("Pagination.json")));
    private static string Hash(string name) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(name))));

    [TestMethod]
    public void CollectorAndCompleteImagesRemainBoundToOriginalComponent()
    {
        using var reference = Read(); var root = reference.RootElement;
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("commit").GetString());
        Assert.AreEqual("9111a975e78d31dcd8e3c68b588110547b45ee31facad43b81f84bb9e7318834", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(root.GetProperty("scriptSha256").GetString(), Hash("CapturePagination.cjs"));
        Assert.AreEqual(root.GetProperty("clientSha256").GetString(), Hash("PaginationClient.tsx"));
        var cases = root.GetProperty("cases").EnumerateArray().ToArray(); Assert.AreEqual(66, cases.Length);
        Assert.AreEqual(66, cases.Select(row => row.GetProperty("file").GetString()).Distinct().Count());
        foreach (var row in cases) Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Hash(Path.Combine("Pagination", row.GetProperty("file").GetString()!)));
    }

    [TestMethod]
    public void PageWindowsMatchActualSourceIncludingFractionalAndOutsideValues()
    {
        using var reference = Read();
        foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec = row.GetProperty("spec"); var actual = row.GetProperty("measurement"); var total = spec.GetProperty("totalPages").GetDouble();
            Assert.AreEqual(actual.GetProperty("visible").GetBoolean(), BleeckerPaginationModel.IsVisible(total));
            var expected = actual.GetProperty("buttons").EnumerateArray().Where(button => button.GetProperty("title").GetString() == "")
                .Select(button => double.Parse(button.GetProperty("text").GetString()!, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            CollectionAssert.AreEqual(expected, BleeckerPaginationModel.Pages(spec.GetProperty("currentPage").GetDouble(), total), row.GetProperty("id").GetString());
        }
    }

    [TestMethod]
    public void SourceFlexShrinkKeepsNumberWidthsAndArrowMinimum()
    {
        using var reference = Read();
        foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray().Where(row => !row.GetProperty("spec").TryGetProperty("state", out _)))
        {
            var actual = row.GetProperty("measurement"); if (!actual.GetProperty("visible").GetBoolean()) continue;
            var buttons = actual.GetProperty("buttons").EnumerateArray().ToArray();
            var widths = buttons.Skip(1).SkipLast(1).Select(button => button.GetProperty("box").GetProperty("width").GetDouble()).ToArray();
            Assert.AreEqual(buttons[0].GetProperty("box").GetProperty("width").GetDouble(), BleeckerPaginationModel.ArrowWidth(actual.GetProperty("nav").GetProperty("width").GetDouble(), widths));
            var start = Math.Min(buttons[0].GetProperty("box").GetProperty("x").GetDouble(), buttons[^1].GetProperty("box").GetProperty("x").GetDouble()) - actual.GetProperty("nav").GetProperty("x").GetDouble();
            Assert.AreEqual(start, BleeckerPaginationModel.Start(actual.GetProperty("nav").GetProperty("width").GetDouble(), widths));
        }
    }

    [TestMethod]
    public void SourceRequestsRemainControlledAndAvailabilityIsAuthoritative()
    {
        using var reference = Read(); var interactions = reference.RootElement.GetProperty("interactions").EnumerateArray().ToArray(); Assert.AreEqual(6, interactions.Length);
        foreach (var row in interactions)
        {
            if (row.TryGetProperty("contradictoryAvailability", out _)) { CollectionAssert.AreEqual(new[] { 0d }, row.GetProperty("requests").EnumerateArray().Select(value => value.GetDouble()).ToArray()); continue; }
            var accepted = row.GetProperty("accept").GetBoolean(); var index = 0;
            foreach (var sample in row.GetProperty("samples").EnumerateArray())
            {
                index++; Assert.AreEqual(accepted ? 4 + index : 4, sample.GetProperty("currentPage").GetDouble());
                CollectionAssert.AreEqual(Enumerable.Range(1, index).Select(value => accepted ? 4d + value : 5d).ToArray(), sample.GetProperty("requests").EnumerateArray().Select(value => value.GetDouble()).ToArray());
            }
        }
    }

    [TestMethod]
    public void NonFiniteNumbersKeepSourceComparisonAndArrayLengthRules()
    {
        Assert.IsTrue(BleeckerPaginationModel.IsVisible(double.NaN)); Assert.HasCount(0, BleeckerPaginationModel.Pages(4, double.NaN));
        CollectionAssert.AreEqual(Enumerable.Range(1, 7).Select(value => (double)value).ToArray(), BleeckerPaginationModel.Pages(4, double.PositiveInfinity));
        Assert.HasCount(0, BleeckerPaginationModel.Pages(4, double.NegativeInfinity));
        Assert.IsTrue(BleeckerPaginationModel.Pages(double.NaN, 12).All(double.IsNaN));
    }
}
