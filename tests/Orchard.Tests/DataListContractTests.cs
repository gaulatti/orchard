using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Orchard;

namespace Orchard.Tests;

[TestClass]
public sealed class DataListContractTests
{
    [TestMethod]
    public void PinnedBrowserCoversBothSidesAndExactResponsiveBoundary()
    {
        using var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "DataList.json")));
        var root = reference.RootElement;
        Assert.AreEqual("23da195177b59a57fb1031ba4c8fdd900f7484febc4d73d13b96770cc55bde5a", root.GetProperty("sourceSha256").GetString());
        var script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CaptureDataList.cjs")).Replace("\r\n", "\n");
        Assert.AreEqual(root.GetProperty("scriptSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(script))));
        Assert.AreEqual(72, root.GetProperty("cases").GetArrayLength());
        foreach (var point in root.GetProperty("cases").EnumerateArray())
        {
            var viewport = point.GetProperty("viewportWidth").GetInt32();
            Assert.IsTrue(viewport is 639 or 640 or 1024);
            foreach (var row in point.GetProperty("normal").GetProperty("rows").EnumerateArray())
                Assert.AreEqual(viewport < 640 ? "flex" : "grid", row.GetProperty("display").GetString());
        }
        Assert.AreEqual(6, root.GetProperty("interactions").GetArrayLength());
    }
    [TestMethod]
    public void NumericZeroAndWhitespaceRetainTheirDistinctSourceBoundaries()
    {
        using var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "DataList.json")));
        foreach (var point in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("id").GetString() == "raw"))
        {
            var rows = point.GetProperty("normal").GetProperty("rows");
            Assert.AreEqual("00", rows[0].GetProperty("value").GetProperty("text").GetString());
            Assert.AreEqual(JsonValueKind.Null, rows[0].GetProperty("description").ValueKind);
            Assert.AreEqual(0, rows[1].GetProperty("label").GetProperty("box").GetProperty("height").GetDouble());
            Assert.AreEqual(0, rows[3].GetProperty("description").GetProperty("box").GetProperty("height").GetDouble());
            Assert.AreEqual("73", rows[4].GetProperty("value").GetProperty("text").GetString());
        }
    }
    [TestMethod]
    public void ConsumerModelPreservesContentObjectsAndNotifiesLiveReplacement()
    {
        var content = new object(); var item = new BleeckerDataListItem("  original  ", "  Label  ", content);
        var notifications = new List<string?>(); item.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        Assert.AreEqual("  original  ", item.Id); Assert.AreSame(content, item.Value);
        item.Value = content; item.Description = 0; item.Label = "Updated"; item.Id = "other";
        CollectionAssert.AreEqual(new[] { "Description", "Label", "Id" }, notifications.ToArray());
        Assert.ThrowsExactly<ArgumentNullException>(() => item.Id = null!);
        Assert.AreEqual("other", item.Id);
    }
}
