using System.Security.Cryptography;
using System.Text.Json;
using Orchard.Catalog;

namespace Orchard.Tests;

[TestClass]
public sealed class DataTablePaginationScrollTests
{
    [TestMethod]
    public void SourceScrollControlsPreserveOriginalRowsAndControlledPagination()
    {
        string Fixture(string name) => Path.Combine(AppContext.BaseDirectory,"Fixtures",name);
        using var document = JsonDocument.Parse(File.ReadAllBytes(Fixture("DataTablePaginationScroll.json"))); var root = document.RootElement;
        Assert.AreEqual("orchard-data-table-pagination-scroll-v1",root.GetProperty("format").GetString());
        foreach (var (field,file) in new[] { ("scriptSha256","CaptureDataTablePaginationScroll.cjs"),("keyboardReferenceSha256","DataTablePaginationKeyboard.json") })
            Assert.AreEqual(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Fixture(file)))).ToLowerInvariant(),root.GetProperty(field).GetString());
        var reports = root.GetProperty("reports").EnumerateArray().ToArray(); Assert.HasCount(18,reports);
        foreach (var scheme in new[] { "Light","Dark" })
        {
            var table = new PaginationTable(50,pageSize:10);
            var points = reports.Where(item=>item.GetProperty("scheme").GetString()==scheme).ToArray();
            CollectionAssert.AreEqual(new[] { "opened","end","home","hover-down-step","hover-bottom","hover-top","left-indicator","shrink-open","closed" },points.Select(item=>item.GetProperty("phase").GetString()).ToArray());
            foreach (var point in points)
            {
                var state = point.GetProperty("state"); var original = state.GetProperty("table"); var phase = point.GetProperty("phase").GetString();
                Assert.IsTrue(original.GetProperty("sameTable").GetBoolean()&&original.GetProperty("sameRows").GetBoolean());
                Assert.IsTrue(original.GetProperty("rows").EnumerateArray().All(row=>row.GetProperty("same").GetBoolean()));
                Assert.AreEqual(table.VisibleText,state.GetProperty("visibleRows").GetString()); Assert.HasCount(0,table.Requests); Assert.AreEqual(0,state.GetProperty("events").GetArrayLength());
                Assert.AreEqual(0,original.GetProperty("pagination").GetProperty("pageIndex").GetInt32()); Assert.AreEqual(10,original.GetProperty("pagination").GetProperty("pageSize").GetInt32());
                Assert.IsTrue(state.GetProperty("receipts").EnumerateArray().All(receipt=>receipt.GetProperty("trusted").GetBoolean()));
                Assert.IsGreaterThanOrEqualTo(3,point.GetProperty("presentation").GetProperty("stable").GetInt32()); Assert.IsLessThan(2000,point.GetProperty("presentation").GetProperty("milliseconds").GetDouble());
                if (phase=="closed") { Assert.IsFalse(state.GetProperty("popupOpen").GetBoolean()); Assert.AreEqual("combobox",state.GetProperty("focused").GetString()); continue; }
                Assert.AreEqual(phase=="hover-down-step"?346:phase=="shrink-open"?110:318,state.GetProperty("popup").GetProperty("height").GetDouble());
                Assert.AreEqual(phase=="hover-down-step"?36:phase is "end" or "hover-bottom"?188:0,state.GetProperty("viewport").GetProperty("scrollTop").GetDouble());
                foreach (var name in new[] { "up","down" }) if (state.GetProperty(name) is var indicator && indicator.ValueKind!=JsonValueKind.Null) { Assert.AreEqual("true",indicator.GetProperty("ariaHidden").GetString()); Assert.AreEqual(28,indicator.GetProperty("box").GetProperty("height").GetDouble()); }
            }
        }
    }
}
