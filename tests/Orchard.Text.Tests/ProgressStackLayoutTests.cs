using System.Globalization;
using System.Text.Json;
using Orchard;

namespace Orchard.Text.Tests;

public sealed partial class ProgressStackTextTests
{
    // Only the source spec is an input to allocation. Expected rectangles and
    // DOM line counts are read after production layout has produced its result.
    [TestMethod]
    public void NativeDataThroughShapingAndFlexAllocationMatchesAll212SourceLayouts()
    {
        var count=0;
        foreach(var name in new[]{"ProgressStack.json","ProgressStackText.json"})
        {
            using var document=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures",name)));
            foreach(var row in document.RootElement.GetProperty("cases").EnumerateArray())
            {
                count++;var spec=row.GetProperty("spec");var rtl=spec.TryGetProperty("rtl",out var direction)&&direction.GetBoolean();
                var width=spec.TryGetProperty("width",out var declared)?declared.GetDouble():280;
                var trackHeight=spec.TryGetProperty("custom",out _)?20d:12d;
                var showLegend=!spec.TryGetProperty("showLegend",out var show)||show.GetBoolean();
                var items=spec.GetProperty("items").EnumerateArray().Select(item=>new
                {
                    Label=Inline(BleeckerInlineText.Normalize(item.GetProperty("label").GetString()!),rtl,false),
                    Value=Inline(BleeckerNumberText.Format(double.Parse(item.GetProperty("value").GetString()!,CultureInfo.InvariantCulture)),rtl,true)
                }).ToArray();
                var inlineWidths=new BleeckerProgressStackLayout.InlineWidths[items.Length];
                var labelHeights=new double[items.Length];var valueHeights=new double[items.Length];
                var limits=items.Select(item=>(
                    BleeckerProgressStackLayout.ItemWidth(item.Label.MaximumWidth,item.Value.MaximumWidth),
                    BleeckerProgressStackLayout.ItemWidth(item.Label.MinimumWidth,item.Value.MinimumWidth))).ToArray();
                var legend=BleeckerProgressStackLayout.Place(width,showLegend?limits:[],(index,itemWidth)=>
                {
                    var item=items[index];var sizes=BleeckerProgressStackLayout.Allocate(itemWidth,
                        new(item.Label.MaximumWidth,item.Label.MinimumWidth),new(item.Value.MaximumWidth,item.Value.MinimumWidth));
                    inlineWidths[index]=sizes;labelHeights[index]=item.Label.Wrap(sizes.Label).Count*16;valueHeights[index]=item.Value.Wrap(sizes.Value).Count*16;
                    return Math.Max(10,Math.Max(labelHeights[index],valueHeights[index]));
                });
                var expected=row.GetProperty("measurement");var id=$"{name}/{row.GetProperty("id").GetString()}/{row.GetProperty("scheme").GetString()}/{row.GetProperty("motion").GetString()}";
                static double Number(JsonElement box,string key)=>box.GetProperty(key).GetDouble();
                void Near(double actual,double wanted,string field)=>Assert.AreEqual(wanted,actual,$"{id}/{field}");
                void Rect(double x,double y,double w,double h,JsonElement box,string field)
                {
                    Near(x,Number(box,"x"),field+"/x");Near(y,Number(box,"y"),field+"/y");Near(w,Number(box,"width"),field+"/width");Near(h,Number(box,"height"),field+"/height");
                }
                Rect(20,20,width,trackHeight+(showLegend?12+legend.Height:0),expected.GetProperty("root").GetProperty("box"),"root");
                Assert.AreEqual(showLegend?items.Length:0,expected.GetProperty("items").GetArrayLength(),id+"/items");
                if(!showLegend)continue;
                Rect(20,20+trackHeight+12,width,legend.Height,expected.GetProperty("legend").GetProperty("box"),"legend");
                for(var i=0;i<items.Length;i++)
                {
                    var item=legend.Items[i];var target=expected.GetProperty("items")[i];var sizes=inlineWidths[i];
                    var x=20+(rtl?width-item.X-item.Width:item.X);var y=20+trackHeight+12+item.Y;
                    Rect(x,y,item.Width,item.Height,target.GetProperty("box"),$"item-{i}");
                    double Left(double start,double size)=>x+(rtl?item.Width-start-size:start);
                    Rect(Left(0,sizes.Swatch),y+BleeckerProgressStackLayout.Center(item.Height,10),sizes.Swatch,10,target.GetProperty("swatch").GetProperty("box"),$"swatch-{i}");
                    Rect(Left(sizes.Swatch+6,sizes.Label),y+BleeckerProgressStackLayout.Center(item.Height,labelHeights[i]),sizes.Label,labelHeights[i],target.GetProperty("label").GetProperty("box"),$"label-{i}");
                    Rect(Left(sizes.Swatch+sizes.Label+12,sizes.Value),y+BleeckerProgressStackLayout.Center(item.Height,valueHeights[i]),sizes.Value,valueHeights[i],target.GetProperty("value").GetProperty("box"),$"value-{i}");
                }
            }
        }
        Assert.AreEqual(212,count);
    }
}
