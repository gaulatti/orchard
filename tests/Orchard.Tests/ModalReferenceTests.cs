using System.Text.Json;
using Orchard;
namespace Orchard.Tests;

[TestClass]
public sealed class ModalReferenceTests
{
    [TestMethod]
    public void Modal_flex_surface_and_scroll_geometry_match_forty_rendered_source_states()
    {
        using var source=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","Modal.json")));var count=0;
        foreach(var row in source.RootElement.GetProperty("cases").EnumerateArray())
        {
            var m=row.GetProperty("measurement");var v=row.GetProperty("viewport");var kind=row.GetProperty("spec").GetProperty("kind").GetString();var intrinsic=m.GetProperty("titleIntrinsic");double N(JsonElement e,string key)=>e.GetProperty(key).GetDouble();
            var l=BleeckerModalModel.Measure(N(v,"width"),N(v,"height"),N(intrinsic,"maximum"),N(intrinsic,"minimum"),_=>N(m.GetProperty("title").GetProperty("box"),"height"),_=>kind=="empty"?0:kind=="long"?1000:80,maximumWidth:kind=="custom"?672:576);
            void Compare(string key,BleeckerModalModel.Rect b){var e=m.GetProperty(key).GetProperty("box");Assert.AreEqual(N(e,"x"),b.X,$"{count}:{key}.x");Assert.AreEqual(N(e,"y"),b.Y,$"{count}:{key}.y");Assert.AreEqual(N(e,"width"),b.Width,$"{count}:{key}.width");Assert.AreEqual(N(e,"height"),b.Height,$"{count}:{key}.height");}
            Compare("dialog",l.Surface);Compare("header",l.Header);Compare("body",l.Body);Compare("title",l.Title);Compare("close",l.Close);if(kind!="empty")Compare("consumer",l.Content);
            var scroll=m.GetProperty("scroll");Assert.AreEqual(N(scroll,"clientWidth"),l.ViewportWidth);Assert.AreEqual(N(scroll,"clientHeight"),l.ViewportHeight);Assert.AreEqual(N(scroll,"scrollHeight"),l.ExtentHeight);count++;
        }
        Assert.AreEqual(40,count);
    }
    [TestMethod]
    public void Invalid_modal_metrics_fail_and_rtl_preserves_the_same_physical_surface()
    {
        foreach(var value in new[]{-1d,double.NaN,double.PositiveInfinity})Assert.Throws<ArgumentOutOfRangeException>(()=>BleeckerModalModel.Measure(value,600,300,50,_=>28,_=>80));
        Assert.Throws<ArgumentOutOfRangeException>(()=>BleeckerModalModel.Measure(900,760,300,50,_=>double.NaN,_=>80));
        var left=BleeckerModalModel.Measure(900,760,300,50,_=>28,_=>1000);var right=BleeckerModalModel.Measure(900,760,300,50,_=>28,_=>1000,rtl:true);
        Assert.AreEqual(left.Surface,right.Surface);Assert.AreEqual(left.Close.X-left.Inner.X,right.Inner.X+right.Inner.Width-right.Close.X-right.Close.Width);Assert.AreEqual(left.ExtentHeight,right.ExtentHeight);
    }
}
