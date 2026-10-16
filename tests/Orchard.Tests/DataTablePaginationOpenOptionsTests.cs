using System.Security.Cryptography;
using System.Text.Json;
using Orchard.Catalog;

namespace Orchard.Tests;

[TestClass]
public sealed class DataTablePaginationOpenOptionsTests
{
    [TestMethod]
    public void OpenOptionChangesPreserveOriginalRowsAndAnUnlistedControlledValue()
    {
        string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
        using var document = JsonDocument.Parse(File.ReadAllBytes(Fixture("DataTablePaginationOpenOptions.json"))); var root = document.RootElement;
        Assert.AreEqual("orchard-data-table-pagination-open-options-v1", root.GetProperty("format").GetString());
        foreach (var (field, name) in new[] { ("scriptSha256", "CaptureDataTablePaginationOpenOptions.cjs"), ("keyboardReferenceSha256", "DataTablePaginationKeyboard.json") })
            Assert.AreEqual(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Fixture(name)))).ToLowerInvariant(), root.GetProperty(field).GetString());
        var reports = root.GetProperty("reports").EnumerateArray().ToArray(); Assert.HasCount(8, reports);
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var table = new PaginationTable(8, pageIndex: 1, pageSize: 4);
            foreach (var report in reports.Where(item => item.GetProperty("scheme").GetString() == theme))
            {
                var phase = report.GetProperty("phase").GetString(); if (phase == "external-size") table.SetPagination(0, 8); if (phase == "restore-current") table.SetPagination(1, 4);
                var state = report.GetProperty("state"); var original = state.GetProperty("table"); var pagination = original.GetProperty("pagination");
                Assert.IsTrue(state.GetProperty("popupOpen").GetBoolean()); Assert.AreEqual(0, state.GetProperty("events").GetArrayLength()); Assert.HasCount(0, table.Requests);
                Assert.AreEqual(table.PageIndex, pagination.GetProperty("pageIndex").GetInt32()); Assert.AreEqual(table.PageSize, pagination.GetProperty("pageSize").GetInt32());
                Assert.AreEqual(table.VisibleText, state.GetProperty("visibleRows").GetString()); Assert.IsTrue(table.VisibleRows.All(row => table.Rows.Any(item => ReferenceEquals(item, row))));
                Assert.IsTrue(original.GetProperty("sameTable").GetBoolean() && original.GetProperty("sameRows").GetBoolean());
                Assert.IsTrue(original.GetProperty("rows").EnumerateArray().All(row => row.GetProperty("same").GetBoolean()));
                Assert.IsTrue(state.GetProperty("inputEvents").EnumerateArray().All(item => item.GetProperty("trusted").GetBoolean()));
                Assert.IsGreaterThanOrEqualTo(3, report.GetProperty("presentation").GetProperty("stable").GetInt32()); Assert.IsLessThan(2000, report.GetProperty("presentation").GetProperty("milliseconds").GetDouble());
                Assert.AreEqual(phase == "remove-current" ? "" : table.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture), state.GetProperty("displayed").GetString());
            }
        }
    }
}
