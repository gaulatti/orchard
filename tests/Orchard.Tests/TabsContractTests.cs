using System.Text.Json;
using System.Security.Cryptography;

namespace Orchard.Tests;

[TestClass]
public sealed class TabsContractTests
{
    [TestMethod]
    public void EveryRenderedReferenceRetainsItsOriginalPng()
    {
        using var reference = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Tabs.json")));
        var frames = 0;
        foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray())
            if (row.TryGetProperty("file", out var file))
            {
                Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(
                    File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Tabs", file.GetString()!)))));
                frames++;
            }
        Assert.AreEqual(36, frames);
    }

    [TestMethod]
    public void RenderedSourceKeepsAcceptedAndDeclinedRequestsDistinct()
    {
        using var reference = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Tabs.json")));
        var root = reference.RootElement;
        Assert.AreEqual("812294042eecf36f03ccfe890bac6f1542a6573c1fd8c24b42e963bb560c6827", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(44, root.GetProperty("cases").GetArrayLength());
        Assert.AreEqual(4, root.GetProperty("interactions").GetArrayLength());
        foreach (var sequence in root.GetProperty("interactions").EnumerateArray())
        {
            var accepted = sequence.GetProperty("accept").GetBoolean();
            CollectionAssert.AreEqual(new[] { "activity", "settings" }, sequence.GetProperty("requests").EnumerateArray().Select(value => value.GetString()!).ToArray());
            foreach (var (field, expected) in new[] { ("clicked", accepted ? "Activity" : "Overview"), ("ended", accepted ? "Settings" : "Overview") })
            {
                var selected = sequence.GetProperty(field).GetProperty("items").EnumerateArray().Single(item => item.GetProperty("selected").GetString() == "true");
                Assert.AreEqual(expected, selected.GetProperty("text").GetString());
            }
        }
        foreach (var (filename, field) in new[] { ("CaptureTabs.cjs", "scriptSha256"), ("TabsClient.tsx", "clientSha256") })
            Assert.AreEqual(root.GetProperty(field).GetString(), Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", filename)))).ToLowerInvariant());
    }

    [TestMethod]
    public void StretchAllocationMatchesEveryFrozenBrowserTrack()
    {
        using var reference = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Tabs.json")));
        var cases = reference.RootElement.GetProperty("cases").EnumerateArray().ToArray();
        var checkedCases = 0;
        foreach (var row in cases.Where(row => row.GetProperty("spec").TryGetProperty("stretch", out var stretch) && stretch.GetBoolean()))
        {
            var spec = row.GetProperty("spec"); var prefix = spec.GetProperty("id").GetString()![..^4];
            var natural = cases.Single(candidate => candidate.GetProperty("scheme").GetString() == row.GetProperty("scheme").GetString() && candidate.GetProperty("spec").GetProperty("id").GetString() == prefix + "false");
            var minimums = natural.GetProperty("measurement").GetProperty("items").EnumerateArray().Select(item => item.GetProperty("box").GetProperty("width").GetDouble()).ToArray();
            var expected = row.GetProperty("measurement").GetProperty("items").EnumerateArray().Select(item => item.GetProperty("box").GetProperty("width").GetDouble()).ToArray();
            CollectionAssert.AreEqual(expected, BleeckerTabsModel.Widths(minimums, expected.Sum(), true), spec.GetProperty("id").GetString());
            checkedCases++;
        }
        Assert.AreEqual(18, checkedCases);
    }

    [TestMethod]
    public void TabIdentifiersAndDestinationsRemainConsumerOwned()
    {
        BleeckerTabsModel.Validate(new[] { new BleeckerTabItem("", "Empty"), new BleeckerTabItem("A", "Upper"), new BleeckerTabItem("a", "Lower") });
        Assert.ThrowsExactly<ArgumentException>(() => BleeckerTabsModel.Validate(new[] { new BleeckerTabItem("same", "First"), new BleeckerTabItem("same", "Second") }));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BleeckerTabItem(null!, "Invalid"));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BleeckerTabItem("id", null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => BleeckerTabsModel.Validate(new BleeckerTabItem[] { null! }));
        var item = new BleeckerTabItem("consumer-id", "日本語", "../consumer/path?value=1#part", external: true, panelId: "consumer-panel");
        Assert.AreEqual("../consumer/path?value=1#part", item.Href);
        Assert.IsTrue(item.External);
        Assert.AreEqual("consumer-panel", item.PanelId);
    }

    [TestMethod]
    public void StretchTracksKeepLongLabelsAndDistributeOnlyAvailableSpace()
    {
        CollectionAssert.AreEqual(new[] { 200d, 200d, 200d }, BleeckerTabsModel.Widths(new[] { 100d, 200d, 50d }, 600, true));
        CollectionAssert.AreEqual(new[] { 100d, 200d, 100d }, BleeckerTabsModel.Widths(new[] { 100d, 200d, 50d }, 400, true));
        CollectionAssert.AreEqual(new[] { 100d, 200d, 50d }, BleeckerTabsModel.Widths(new[] { 100d, 200d, 50d }, 200, true));
        CollectionAssert.AreEqual(new[] { 100d, 200d, 50d }, BleeckerTabsModel.Widths(new[] { 100d, 200d, 50d }, 600, false));
        Assert.IsEmpty(BleeckerTabsModel.Widths(Array.Empty<double>(), 600, true));
    }
}
