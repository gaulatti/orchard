using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class SelectPortalPlacementTests
{
    [TestMethod]
    public void SharedPlacementMatchesAllFrozenOpenSelectContentPositions()
    {
        using var source = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Select.json")));
        var count = 0;
        foreach (var row in source.RootElement.GetProperty("openCases").EnumerateArray())
        {
            var m = row.GetProperty("measurement"); var anchor = m.GetProperty("trigger"); var popup = m.GetProperty("popup").GetProperty("box");
            double N(JsonElement node, string key) => node.GetProperty(key).GetDouble();
            var actual = Orchard.BleeckerPopoverModel.Place(new(N(anchor,"x"),N(anchor,"y"),N(anchor,"width"),N(anchor,"height")),N(popup,"width"),N(popup,"height"),900,760,Orchard.BleeckerPopoverSide.Bottom,Orchard.BleeckerPopoverAlign.Start,4,false,1,8);
            Assert.AreEqual(N(popup,"x"),actual.X,1d/64,row.GetProperty("id").GetString()); Assert.AreEqual(N(popup,"y"),actual.Y,1d/64,row.GetProperty("id").GetString()); count++;
        }
        Assert.AreEqual(32,count);
    }
    [TestMethod]
    public void SelectEightPixelCollisionBoundaryIsExplicitAndValidated()
    {
        var select = Orchard.BleeckerPopoverModel.Place(new(2,20,200,40),200,100,900,760,Orchard.BleeckerPopoverSide.Bottom,Orchard.BleeckerPopoverAlign.Start,4,false,1,8);
        var popover = Orchard.BleeckerPopoverModel.Place(new(2,20,200,40),200,100,900,760,Orchard.BleeckerPopoverSide.Bottom,Orchard.BleeckerPopoverAlign.Start,4,false);
        Assert.AreEqual(8d,select.X); Assert.AreEqual(12d,popover.X);
        foreach(var padding in new[]{-1d,double.NaN,double.PositiveInfinity}) Assert.ThrowsExactly<ArgumentOutOfRangeException>(()=>Orchard.BleeckerPopoverModel.Place(new(2,20,200,40),200,100,900,760,Orchard.BleeckerPopoverSide.Bottom,Orchard.BleeckerPopoverAlign.Start,4,false,1,padding));
    }
}
