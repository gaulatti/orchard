using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Orchard;

namespace Orchard.Tests;

[TestClass]
public sealed class FilterChipContractTests
{
    [TestMethod]
    public void FrozenReferenceKeepsWrappingOptionalActionsAndConsumerOwnership()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "FilterChip.json")));
        var root = document.RootElement;
        Assert.AreEqual("e80e6fc2932baffa9c0471c5d1ff58d27593f6538070d7128a21ea6a84967525", root.GetProperty("sourceSha256").GetString());
        var script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CaptureFilterChip.cjs")).Replace("\r\n", "\n");
        Assert.AreEqual(root.GetProperty("scriptSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(script))));
        Assert.AreEqual(40, root.GetProperty("cases").GetArrayLength());
        foreach (var test in root.GetProperty("cases").EnumerateArray())
        {
            var id = test.GetProperty("id").GetString();
            var normal = test.GetProperty("normal");
            if (id == "group-empty") { Assert.AreEqual(0, normal.GetProperty("chips").GetArrayLength()); Assert.AreEqual(JsonValueKind.Null, normal.GetProperty("clear").ValueKind); }
            if (id == "empty-value") Assert.AreEqual(1, normal.GetProperty("chips")[0].GetProperty("texts").GetArrayLength());
            if (id == "long" && test.GetProperty("width").GetInt32() == 320) Assert.AreEqual(46, normal.GetProperty("chips")[0].GetProperty("box").GetProperty("height").GetDouble());
            if (id == "group" && test.GetProperty("width").GetInt32() == 320) Assert.AreEqual(64, normal.GetProperty("group").GetProperty("height").GetDouble());
        }
        Assert.AreEqual(4, root.GetProperty("interactions").GetArrayLength());
        foreach (var input in root.GetProperty("interactions").EnumerateArray())
        { Assert.AreEqual(3, input.GetProperty("removeActions").GetInt32()); Assert.AreEqual(1, input.GetProperty("clearActions").GetInt32()); Assert.IsTrue(input.GetProperty("sourceDoesNotMutateConsumerFilters").GetBoolean()); }
    }

    [TestMethod]
    public void ModelRetainsOriginalValuesAndNotifiesHandlerRemoval()
    {
        var filter = new BleeckerFilter("  Status  ", ""); var parameter = new object(); var notifications = new List<string?>(); var calls = 0;
        filter.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        EventHandler handler = (_, _) => calls++;
        filter.RemoveRequested += handler; Assert.IsTrue(filter.HasRemoveHandler);
        filter.RemoveCommandParameter = parameter; filter.NotifyRemove();
        Assert.AreEqual(1, calls); Assert.AreSame(parameter, filter.RemoveCommandParameter); Assert.AreEqual("  Status  ", filter.Label); Assert.AreEqual("", filter.Value);
        filter.RemoveRequested -= handler; filter.NotifyRemove(); Assert.AreEqual(1, calls); Assert.IsFalse(filter.HasRemoveHandler);
        CollectionAssert.AreEqual(new[] { "HasRemoveHandler", "RemoveCommandParameter", "HasRemoveHandler" }, notifications);
        Assert.ThrowsExactly<ArgumentNullException>(() => filter.Label = null!);
    }
}
