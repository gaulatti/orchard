using System.Globalization;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class NegativeChartDomainTests
{
    [TestMethod]
    public void NegativeDomainsMatchTheActualRenderedSourceAxes()
    {
        using var reference=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","NegativeChartDomains.json")));
        foreach(var sample in reference.RootElement.GetProperty("cases").EnumerateArray())
        {
            var values=sample.GetProperty("values").EnumerateArray().Select(x=>x.GetDouble());
            var actual=BleeckerChartScale.Ticks(values);var expected=sample.GetProperty("ticks").EnumerateArray().Select(x=>double.Parse(x.GetString()!,CultureInfo.InvariantCulture)).ToArray();
            Assert.AreEqual(expected.Length,actual.Length);
            for(var i=0;i<actual.Length;i++)Assert.AreEqual(expected[i],actual[i],1e-12,sample.ToString());
            CollectionAssert.AreEqual(sample.GetProperty("ticks").EnumerateArray().Select(x=>x.GetString()).ToArray(),actual.Select(BleeckerChartScale.TickLabel).ToArray());
        }
    }
}
