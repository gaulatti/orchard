using System.Text.Json;
using Orchard;

namespace Orchard.Tests;

[TestClass]
public sealed class TooltipPlacementReferenceTests
{
    [TestMethod]
    public void Placement_and_arrows_match_all_forty_rendered_source_states()
    {
        using var source=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","Tooltip.json")));
        var count=0;
        foreach(var item in source.RootElement.GetProperty("cases").EnumerateArray())
        {
            var actual=item.GetProperty("measurement");var anchor=actual.GetProperty("trigger");var tooltip=actual.GetProperty("tooltip");var body=tooltip.GetProperty("box");var arrow=tooltip.GetProperty("arrow").GetProperty("box");
            double N(JsonElement element,string name)=>element.GetProperty(name).GetDouble();
            var model=BleeckerTooltipPlacementModel.Place(new(N(anchor,"x"),N(anchor,"y"),N(anchor,"x")+N(anchor,"width"),N(anchor,"y")+N(anchor,"height")),N(body,"width"),N(body,"height"),900,760,Enum.Parse<BleeckerTooltipSide>(item.GetProperty("spec").GetProperty("side").GetString()!,true),1);
            Assert.AreEqual(tooltip.GetProperty("side").GetString(),model.Side.ToString().ToLowerInvariant());Assert.AreEqual(N(body,"x"),model.X);Assert.AreEqual(N(body,"y"),model.Y);Assert.IsTrue(model.ArrowVisible);
            var actualArrowX=model.Side switch {BleeckerTooltipSide.Left=>model.X+N(body,"width"),BleeckerTooltipSide.Right=>model.X-5,_=>model.X+model.ArrowOffset};
            var actualArrowY=model.Side switch {BleeckerTooltipSide.Top=>model.Y+N(body,"height"),BleeckerTooltipSide.Bottom=>model.Y-5,_=>model.Y+model.ArrowOffset};
            Assert.AreEqual(N(arrow,"x"),actualArrowX);Assert.AreEqual(N(arrow,"y"),actualArrowY);count++;
        }
        Assert.AreEqual(40,count);
    }
    [TestMethod]
    public void Invalid_bounds_and_scale_fail_explicitly()
    {
        var anchor=new TooltipBounds(390,360,510,396);
        foreach(var scale in new[]{0d,-1,double.NaN,double.PositiveInfinity})Assert.Throws<ArgumentOutOfRangeException>(()=>BleeckerTooltipPlacementModel.Place(anchor,144,55,900,760,BleeckerTooltipSide.Top,scale));
        Assert.Throws<ArgumentException>(()=>BleeckerTooltipPlacementModel.Place(new(1,1,0,0),144,55,900,760,BleeckerTooltipSide.Top,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>BleeckerTooltipPlacementModel.Place(anchor,144,55,900,760,(BleeckerTooltipSide)99,1));
    }
}
