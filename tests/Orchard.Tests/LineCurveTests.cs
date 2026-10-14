using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class LineCurveTests
{
    [TestMethod]
    public void NativeMonotoneCommandsMatchTheFrozenRechartsCurveProducer()
    {
        using var source=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","LineCurve.json")));
        Assert.AreEqual("3.2.0",source.RootElement.GetProperty("d3ShapeVersion").GetString());
        Assert.AreEqual("3.8.1",source.RootElement.GetProperty("rechartsVersion").GetString());
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066",source.RootElement.GetProperty("sourceCommit").GetString());
        Assert.AreEqual("33d727f186f98df5917a93687f00d83178e1648a6f3a97afe9b0e797dec488a7",source.RootElement.GetProperty("sourceSha256").GetString());
        foreach(var sample in source.RootElement.GetProperty("samples").EnumerateArray())
        {
            var points=sample.GetProperty("points").EnumerateArray().Select(point=>new BleeckerMonotoneCurve.Point(Number(point[0]),Number(point[1])));
            var actual=BleeckerMonotoneCurve.Create(points);var expected=sample.GetProperty("commands").EnumerateArray().ToArray();
            Assert.AreEqual(expected.Length,actual.Count,sample.GetProperty("points").ToString());
            for(var index=0;index<expected.Length;index++)
            {
                Assert.AreEqual(expected[index].GetProperty("Kind").GetString(),actual[index].Kind);
                var values=expected[index].GetProperty("Values").EnumerateArray().Select(Number).ToArray();
                Assert.AreEqual(values.Length,actual[index].Values.Length);
                for(var coordinate=0;coordinate<values.Length;coordinate++)
                    Assert.AreEqual(values[coordinate],actual[index].Values[coordinate],1e-10);
            }
        }
        static double Number(JsonElement value)=>value.ValueKind==JsonValueKind.Null?double.NaN:value.GetDouble();
    }
}
