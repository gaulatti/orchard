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
        var reports = root.GetProperty("reports").EnumerateArray().ToArray(); Assert.HasCount(36,reports);
        foreach (var scheme in new[] { "Light","Dark" })
        {
            var table = new PaginationTable(50,pageSize:10);
            var points = reports.Where(item=>item.GetProperty("scheme").GetString()==scheme).ToArray();
            CollectionAssert.AreEqual(new[] { "opened","end","home","type-5","type-50","home-after-type","hover-down-step","hover-bottom","hover-top","viewport-next","hover-down-again","hover-top-again","left-indicator","shrink-open","closed","restored-closed","closed-type-5","closed-type-50" },points.Select(item=>item.GetProperty("phase").GetString()).ToArray());
            foreach (var point in points)
            {
                var state = point.GetProperty("state"); var original = state.GetProperty("table"); var phase = point.GetProperty("phase").GetString();
                Assert.IsTrue(original.GetProperty("sameTable").GetBoolean()&&original.GetProperty("sameRows").GetBoolean());
                Assert.IsTrue(original.GetProperty("rows").EnumerateArray().All(row=>row.GetProperty("same").GetBoolean()));
                if (phase=="closed-type-5") table.SetPageSize(50);
                Assert.AreEqual(table.VisibleText,state.GetProperty("visibleRows").GetString()); Assert.AreEqual(table.Requests.Count,state.GetProperty("events").GetArrayLength());
                Assert.AreEqual(table.PageIndex,original.GetProperty("pagination").GetProperty("pageIndex").GetInt32()); Assert.AreEqual(table.PageSize,original.GetProperty("pagination").GetProperty("pageSize").GetInt32());
                Assert.IsTrue(state.GetProperty("receipts").EnumerateArray().All(receipt=>receipt.GetProperty("trusted").GetBoolean()));
                Assert.IsGreaterThanOrEqualTo(3,point.GetProperty("presentation").GetProperty("stable").GetInt32()); Assert.IsLessThan(2000,point.GetProperty("presentation").GetProperty("milliseconds").GetDouble());
                if (!state.GetProperty("popupOpen").GetBoolean()) { Assert.AreEqual("combobox",state.GetProperty("focused").GetString()); continue; }
                Assert.AreEqual(phase is "hover-down-step" or "hover-down-again"?346:phase=="shrink-open"?110:318,state.GetProperty("popup").GetProperty("height").GetDouble());
                Assert.AreEqual(phase is "hover-down-step" or "hover-down-again"?36:phase is "end" or "hover-bottom" or "type-50"?188:0,state.GetProperty("viewport").GetProperty("scrollTop").GetDouble());
                foreach (var name in new[] { "up","down" }) if (state.GetProperty(name) is var indicator && indicator.ValueKind!=JsonValueKind.Null) { Assert.AreEqual("true",indicator.GetProperty("ariaHidden").GetString()); Assert.AreEqual(28,indicator.GetProperty("box").GetProperty("height").GetDouble()); }
            }
        }
    }
}
