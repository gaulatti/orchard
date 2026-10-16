using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Orchard;

namespace Orchard.Tests;

[TestClass]
public sealed class TimelineContractTests
{
    [TestMethod]
    public void FrozenSourceCoversResponsiveOverflowRtlAndConsumerActions()
    {
        using var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Timeline.json")));
        var root = reference.RootElement;
        Assert.AreEqual("d070c4100c562241a3cc14585c7ddc30fa4be5bd0c96bb5b9ad7303734333f61", root.GetProperty("sourceSha256").GetString());
        var script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CaptureTimeline.cjs")).Replace("\r\n", "\n");
        Assert.AreEqual(root.GetProperty("scriptSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(script))));
        Assert.AreEqual(72, root.GetProperty("cases").GetArrayLength()); Assert.AreEqual(6, root.GetProperty("interactions").GetArrayLength());
        var baselines = 0;
        foreach (var test in root.GetProperty("cases").EnumerateArray())
        {
            var compact = test.GetProperty("viewportWidth").GetInt32() < 640;
            var index = 0;
            foreach (var row in test.GetProperty("normal").GetProperty("rows").EnumerateArray())
            {
                Assert.AreEqual(compact ? "column" : "row", row.GetProperty("headerDirection").GetString());
                foreach (var key in new[] { "title", "timestamp", "description" })
                    if (row.GetProperty(key).ValueKind != JsonValueKind.Null && row.GetProperty(key).GetProperty("firstBaseline").ValueKind != JsonValueKind.Null) baselines++;
                var item = test.GetProperty("props").GetProperty("items")[index++];
                if (row.GetProperty("icon").GetProperty("firstBaseline").ValueKind != JsonValueKind.Null &&
                    (!item.TryGetProperty("icon", out var icon) || icon.ValueKind != JsonValueKind.Object)) baselines++;
            }
            if (test.GetProperty("id").GetString() == "rtl-long")
            {
                Assert.AreEqual(17, test.GetProperty("normal").GetProperty("rows")[0].GetProperty("connector").GetProperty("box").GetProperty("x").GetDouble());
                if (!compact) Assert.IsTrue(test.GetProperty("normal").GetProperty("rows")[1].GetProperty("body").GetProperty("x").GetDouble() < 0);
            }
        }
        Assert.AreEqual(534, baselines);
    }
    [TestMethod]
    public void DefaultStatusFollowsTheCurrentOrdinalAndNeverChangesConsumerStatus()
    {
        var item = new BleeckerTimelineItem("  original  ", "  Raw title  ");
        Assert.AreEqual(BleeckerTimelineStatus.Active, item.ResolveStatus(0)); Assert.AreEqual(BleeckerTimelineStatus.Pending, item.ResolveStatus(1));
        item.Status = BleeckerTimelineStatus.Error;
        Assert.AreEqual(BleeckerTimelineStatus.Error, item.ResolveStatus(0)); Assert.AreEqual(BleeckerTimelineStatus.Error, item.ResolveStatus(7));
        item.Status = null; Assert.AreEqual(BleeckerTimelineStatus.Active, item.ResolveStatus(0));
        Assert.AreEqual("  original  ", item.Id); Assert.AreEqual("  Raw title  ", item.Title);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => item.Status = (BleeckerTimelineStatus)99);
    }
    [TestMethod]
    public void MetadataAndOriginalIconNotifyOnlyTheirActualChanges()
    {
        var icon = new object(); var item = new BleeckerTimelineItem("a", "Title") { Icon = icon };
        var notifications = new List<string?>(); item.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        item.Icon = icon; item.Title = "Updated"; item.Timestamp = "Now"; item.Description = "Summary"; item.Status = BleeckerTimelineStatus.Completed;
        CollectionAssert.AreEqual(new[] { "Title", "Timestamp", "Description", "Status" }, notifications.ToArray()); Assert.AreSame(icon, item.Icon);
        Assert.ThrowsExactly<ArgumentNullException>(() => item.Title = null!); Assert.ThrowsExactly<ArgumentNullException>(() => item.Id = null!);
    }
}
