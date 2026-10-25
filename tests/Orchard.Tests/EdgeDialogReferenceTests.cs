using System.Text.Json;
using Orchard;
namespace Orchard.Tests;

[TestClass]
public sealed class EdgeDialogReferenceTests
{
    [TestMethod]
    public void Interrupted_panel_and_backdrop_clocks_match_all_390_source_samples()
    {
        using var source=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","EdgeDialogInterruptions.json")));var samples=0;var stages=0;
        foreach(var h in source.RootElement.GetProperty("histories").EnumerateArray())
        {
            var spec=h.GetProperty("spec");var drawer=spec.GetProperty("kind").GetString()=="drawer";var side=drawer?"bottom":spec.GetProperty("side").GetString();var vertical=side is "left" or "right";var sign=side is "left" or "top"?-1:1;var motion=new BleeckerEdgeDialogMotion(drawer);
            foreach(var stage in h.GetProperty("stages").EnumerateArray())
            {
                var time=stage.GetProperty("logicalTime").GetDouble();motion.SetVisible(stage.GetProperty("entering").GetBoolean(),time);
                foreach(var animation in stage.GetProperty("animations").EnumerateArray()){var expected=animation.GetProperty("duration").GetDouble();Assert.AreEqual(expected,animation.GetProperty("target").GetString()=="surface"?motion.Durations.Surface:motion.Durations.Backdrop,.005,$"{h.GetProperty("scheme")}:{side}:{stages}:duration");}
                foreach(var p in stage.GetProperty("samples").EnumerateArray())
                {
                    var value=motion.Sample(time+p.GetProperty("milliseconds").GetDouble());var b=p.GetProperty("box");var dimension=b.GetProperty(vertical?"width":"height").GetDouble();var offset=(1-value.Surface)*dimension*sign;Assert.AreEqual(p.GetProperty("translateX").GetDouble(),vertical?offset:0,.0002,$"{side}:{stages}:x");Assert.AreEqual(p.GetProperty("translateY").GetDouble(),vertical?0:offset,.0002,$"{side}:{stages}:y");Assert.AreEqual(p.GetProperty("backdropOpacity").GetDouble(),value.Backdrop,.000002,$"{side}:{stages}:opacity");samples++;
                }
                stages++;
            }
        }
        Assert.AreEqual(150,stages);Assert.AreEqual(390,samples);
    }
    [TestMethod]
    public void Source_Drawer_and_Sheet_geometry_matches_all_194_rendered_states()
    {
        using var source=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","EdgeDialog.json")));var count=0;
        foreach(var row in source.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec=row.GetProperty("spec");var shape=spec.GetProperty("shape").GetString();var drawer=spec.GetProperty("kind").GetString()=="drawer";var m=row.GetProperty("measurement");var v=row.GetProperty("viewport");double N(JsonElement e,string name)=>e.GetProperty(name).GetDouble();var width=N(v,"width");var height=N(v,"height");var title=m.GetProperty("title").ValueKind!=JsonValueKind.Null;var description=m.GetProperty("description").ValueKind!=JsonValueKind.Null;
            BleeckerEdgeDialogModel.TextMetrics Text(double _) =>new(title?N(m.GetProperty("title").GetProperty("box"),"height"):0,description?N(m.GetProperty("description").GetProperty("box"),"height"):0);
            double Content(double _) =>spec.TryGetProperty("height",out var supplied)?supplied.GetDouble():shape=="empty"?0:shape is "long" or "no-scroll" or "fill"?1000:80;
            var l=drawer?BleeckerEdgeDialogModel.Drawer(width,height,Text,Content,title,description,shape=="custom"?height*.7:null):BleeckerEdgeDialogModel.Sheet(width,height,Enum.Parse<BleeckerEdgeSide>(spec.GetProperty("side").GetString()!,true),Text,Content,title,description,shape is not("no-scroll" or "fill"),shape=="fill",shape=="custom"?512:null);
            void Box(string key,BleeckerEdgeDialogModel.Rect? box){var expected=m.GetProperty(key);Assert.AreEqual(expected.ValueKind==JsonValueKind.Null,box is null,$"{count}:{key}:presence");if(box is not{} b)return;var e=expected.GetProperty("box");Assert.AreEqual(N(e,"x"),b.X,$"{count}:{key}.x");Assert.AreEqual(N(e,"y"),b.Y,$"{count}:{key}.y");Assert.AreEqual(N(e,"width"),b.Width,$"{count}:{key}.width");Assert.AreEqual(N(e,"height"),b.Height,$"{count}:{key}.height");}
            Box("dialog",l.Surface);Box("handle",l.Handle);Box("header",l.Header);Box("title",l.Title);Box("description",l.Description);Box("close",l.Close);Box("body",l.Body);if(shape!="empty")Box("consumer",l.Content);count++;
        }
        Assert.AreEqual(194,count);
    }
    [TestMethod]
    public void Drawer_dismiss_threshold_matches_source_touch_histories_and_resets()
    {
        using var source=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","EdgeDialog.json")));var gesture=new BleeckerDrawerDismissGesture();var count=0;
        foreach(var row in source.RootElement.GetProperty("gestures").EnumerateArray()){gesture.Start(100);var request=gesture.End(100+row.GetProperty("delta").GetDouble());Assert.AreEqual(row.GetProperty("after").GetProperty("events").GetArrayLength()==1,request);Assert.IsFalse(gesture.End(200));count++;}Assert.AreEqual(10,count);gesture.Start(0);gesture.Reset();Assert.IsFalse(gesture.End(100));Assert.Throws<ArgumentOutOfRangeException>(()=>gesture.Start(double.NaN));
    }
    [TestMethod]
    public void Independent_panel_and_backdrop_clocks_match_all_source_motion_samples()
    {
        using var source=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","EdgeDialogMotion.json")));var count=0;
        foreach(var row in source.RootElement.GetProperty("timelines").EnumerateArray())
        {
            var spec=row.GetProperty("spec");var drawer=spec.GetProperty("kind").GetString()=="drawer";var side=drawer?"bottom":spec.GetProperty("side").GetString();var motion=new BleeckerEdgeDialogMotion(drawer);motion.SetVisible(true,0);
            foreach(var direction in new[]{"opening","closing"})
            {
                if(direction=="closing")motion.SetVisible(false,240);
                foreach(var p in row.GetProperty(direction).GetProperty("points").EnumerateArray())
                {
                    var sample=motion.Sample((direction=="closing"?240:0)+p.GetProperty("ms").GetDouble());var b=p.GetProperty("box");var dimension=b.GetProperty(side is "left" or "right"?"width":"height").GetDouble();var offset=(1-sample.Surface)*dimension*(side is "left" or "top"?-1:1);
                    Assert.AreEqual(p.GetProperty("translateX").GetDouble(),side is "left" or "right"?offset:0,.0002);Assert.AreEqual(p.GetProperty("translateY").GetDouble(),side is "left" or "right"?0:offset,.0002);Assert.AreEqual(p.GetProperty("backdropOpacity").GetDouble(),sample.Backdrop,.000002);count++;
                }
            }
        }
        Assert.AreEqual(100,count);
    }
    [TestMethod]
    public void Edge_presence_retains_inert_closing_paint_for_180ms_and_cancels_removal_on_reopen()
    {
        var p=new BleeckerEdgeDialogPresence();p.SetOpen(true,0);Assert.IsTrue(p.Present);Assert.IsFalse(p.Visible);p.Advance(19);Assert.IsFalse(p.Visible);p.Advance(20);Assert.IsTrue(p.Visible);p.SetOpen(false,50);Assert.IsFalse(p.DesiredOpen);Assert.IsFalse(p.Visible);p.Advance(229);Assert.IsTrue(p.Present);p.Advance(230);Assert.IsFalse(p.Present);p.SetOpen(true,240);p.Advance(260);p.SetOpen(false,270);p.SetOpen(true,300);p.Advance(450);Assert.IsTrue(p.Present&&p.Visible);p.Dispose();Assert.IsFalse(p.Present||p.DesiredOpen||p.Visible);
    }
    [TestMethod]
    public void Edge_presence_matches_all_normal_and_reduced_motion_browser_clock_histories()
    {
        using var source=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","EdgeDialogPresence.json")));var count=0;
        foreach(var trace in source.RootElement.GetProperty("traces").EnumerateArray())
        {
            var p=new BleeckerEdgeDialogPresence();foreach(var step in trace.GetProperty("steps").EnumerateArray())
            {
                var time=step.GetProperty("time").GetDouble();if(step.GetProperty("action").GetString()=="set")p.SetOpen(step.GetProperty("open").GetBoolean(),time);else{p.Advance(time);Assert.AreEqual(step.GetProperty("present").GetBoolean(),p.Present);Assert.AreEqual(step.GetProperty("visible").GetBoolean(),p.Visible);if(p.Present){Assert.AreEqual(step.GetProperty("hidden").GetBoolean(),!p.DesiredOpen);Assert.AreEqual(step.GetProperty("inert").GetBoolean(),!p.DesiredOpen);}count++;}
            }
        }
        Assert.AreEqual(104,count);
    }
}
