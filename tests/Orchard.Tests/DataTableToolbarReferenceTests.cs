using System.Text.Json;

namespace Orchard.Tests;

// Browser producer evidence. These tests do not accept a native Toolbar;
// its consumer/table boundary, catalog and Windows runtime are still pending.
[TestClass]
public sealed class DataTableToolbarReferenceTests
{
    [TestMethod]
    public void FrozenToolbarUsesTheActualPinnedTableRuntimeAndCompleteMatrix()
    {
        using var r=new Reference();
        Assert.AreEqual("orchard-data-table-toolbar-reference-v1",Text(r.Root,"format"));
        Assert.AreEqual(2,r.Root.GetProperty("captureVersion").GetInt32());
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066",Text(r.Root,"sourceCommit"));
        Assert.AreEqual("a2beb795287fd027857d9f4d12df9d0e76406b0fa0703a9fcf20aaa23064a0ef",Text(r.Root,"sourceSha256"));
        Assert.HasCount(118,r.Cases);Assert.AreEqual(118,r.Cases.Select(c=>Text(c,"id")+Text(c,"scheme")).Distinct().Count());
        Assert.AreEqual(376,r.Cases.Sum(c=>c.GetProperty("states").GetArrayLength()));
        Assert.AreEqual(14,r.Cases.Sum(c=>c.GetProperty("screenshots").GetArrayLength()));
        Assert.HasCount(8,r.Root.GetProperty("bundledRuntime").EnumerateArray().ToArray());
        foreach(var(name,version)in new[]{("react","19.2.4"),("react-dom","19.2.4"),("@tanstack/react-table","8.21.3"),("@tanstack/table-core","8.21.3")})
        {var dependency=r.Root.GetProperty("bundledRuntime").EnumerateArray().Single(p=>Text(p,"name")==name);Assert.AreEqual(version,Text(dependency,"version"));StringAssert.StartsWith(Text(dependency,"integrity"),"sha512-");}
        Assert.HasCount(4,r.Root.GetProperty("dependencies").EnumerateArray().ToArray());
        Assert.AreEqual(64,Text(r.Root,"fullCaptureSha256").Length);
        foreach(var sample in r.Cases){Assert.AreEqual(0,sample.GetProperty("errors").GetArrayLength());Assert.AreEqual(0,sample.GetProperty("consoleMessages").GetArrayLength());foreach(var screenshot in sample.GetProperty("screenshots").EnumerateArray())Assert.IsTrue(screenshot.GetProperty("normalMatchesFullCapture").GetBoolean());}
    }
    [TestMethod]
    public void ViewportBreakpointAndInputWrapperRemainDistinctFromInputCap()
    {
        using var r=new Reference();foreach(var theme in Themes)
        {
            var normal=r.Normal("default",theme);Box(normal.GetProperty("surface"),0,0,800,36);
            var left=normal.GetProperty("surface").GetProperty("children")[0];Box(left,0,0,684,36);Box(left.GetProperty("children")[0],0,0,496,36);
            Box(Input(normal),0,0,320,36);Box(Slot(normal,"filters"),508,0,176,36);Box(Slot(normal,"actions"),696,0,104,36);
            var narrow=r.Normal("below-breakpoint",theme);Box(narrow.GetProperty("surface"),0,0,600,132);Box(Input(narrow),0,0,600,36);Box(Slot(narrow,"filters"),0,48,176,36);Box(Slot(narrow,"actions"),0,96,104,36);
            var at=r.Normal("at-breakpoint",theme);Box(at.GetProperty("surface"),0,0,600,36);Box(Input(at),0,0,296,36);Assert.AreEqual("row",Text(r.Style(at.GetProperty("surface")),"flexDirection"));
            Box(Input(r.Normal("constrained-host",theme)),0,0,56,36);
            foreach(var id in new[]{"tiny-host","zero-host"}){var tiny=r.Normal(id,theme);Box(Input(tiny),0,0,30,36);Box(Slot(tiny,"actions"),230,0,104,36);}
            var rtl=r.Normal("rtl",theme);Box(Input(rtl),480,0,320,36);Box(Slot(rtl,"filters"),116,0,176,36);Box(Slot(rtl,"actions"),0,0,104,36);
            Assert.AreEqual(0d,Number(r.Normal("empty",theme).GetProperty("surface").GetProperty("box"),"height"));
        }
    }
    [TestMethod]
    public void SearchWritesOnlyItsActualColumnAndReachesFilteredConsumerRows()
    {
        using var r=new Reference();foreach(var theme in Themes)
        {
            var sample=r.Case("default",theme);var typed=State(sample,"search-fill");Assert.AreEqual("Harbor",Text(Input(typed),"value"));Assert.AreEqual("Harbor",Text(typed,"visibleRows"));
            var filter=typed.GetProperty("table").GetProperty("state").GetProperty("columnFilters").EnumerateArray().Single();Assert.AreEqual("title",Text(filter,"id"));Assert.AreEqual("Harbor",Text(filter,"value"));
            var cleared=State(sample,"search-clear");Assert.AreEqual(0,cleared.GetProperty("table").GetProperty("state").GetProperty("columnFilters").GetArrayLength());Assert.AreEqual("Harbor|Reed|Vale",Text(cleared,"visibleRows"));
            Assert.AreEqual("Reed",Text(State(sample,"search-keyboard"),"visibleRows"));
            var missing=State(r.Case("missing-column",theme),"search-fill");Assert.AreEqual("",Text(Input(missing),"value"));Assert.AreEqual(0,missing.GetProperty("table").GetProperty("state").GetProperty("columnFilters").GetArrayLength());Assert.AreEqual("Harbor|Reed|Vale",Text(missing,"visibleRows"));
            Assert.IsFalse(Nodes(r.Normal("empty-search-column",theme).GetProperty("surface")).Any(n=>Text(n,"tag")=="INPUT"));
            foreach(var(id,value)in new[]{("column-zero","0"),("column-false","false"),("column-number","42"),("column-array","Harbor,Reed"),("column-object","[object Object]"),("column-null",""),("column-undefined",""),("column-nan","NaN")})Assert.AreEqual(value,Text(Input(r.Normal(id,theme)),"value"));
        }
    }
    [TestMethod]
    public void ResetRestoresInitialColumnFiltersAndNeverClearsGlobalState()
    {
        using var r=new Reference();foreach(var theme in Themes)
        {
            var global=r.Case("global-filter",theme);foreach(var action in new[]{"reset-release","reset-keyboard"})
            {var after=State(global,action);Assert.AreEqual("Harbor",Text(after.GetProperty("table").GetProperty("state"),"globalFilter"));Assert.AreEqual(0,after.GetProperty("table").GetProperty("state").GetProperty("columnFilters").GetArrayLength());Assert.AreEqual("Harbor",Text(after,"visibleRows"));Assert.IsTrue(HasReset(after));}
            var initial=State(r.Case("initial-filter",theme),"reset-release");Assert.AreEqual("Harbor",Text(Input(initial),"value"));Assert.AreEqual("Harbor",Text(initial,"visibleRows"));Assert.IsTrue(HasReset(initial));
            foreach(var id in new[]{"undefined","null","false","zero","negative-zero","nan","empty"})Assert.IsFalse(HasReset(r.Normal("global-"+id,theme)),id);
            foreach(var id in new[]{"infinity","space","string-zero","number","true","array","object"})
            {var sample=r.Case("global-"+id,theme);Assert.IsTrue(HasReset(sample.GetProperty("normal")),id);Assert.IsTrue(HasReset(State(sample,"reset-click")),id);}
            Assert.IsTrue(HasReset(r.Normal("empty-column-value",theme)));
        }
    }
    [TestMethod]
    public void ConsumerSlotsKeepIdentityAndSourceNodeTruthiness()
    {
        using var r=new Reference();foreach(var theme in Themes)
        {
            var normal=r.Normal("zero-slots",theme);Assert.AreEqual(24d,Number(normal.GetProperty("surface").GetProperty("box"),"height"));Assert.AreEqual(2,Nodes(normal.GetProperty("surface")).Count(n=>Text(n,"tag")=="#text"&&Text(n,"text")=="0"));
            foreach(var id in new[]{"false-slots","true-slots","empty-string-slots"})Assert.AreEqual(0d,Number(r.Normal(id,theme).GetProperty("surface").GetProperty("box"),"height"));
            var disabled=r.Normal("disabled-slots",theme);Assert.IsTrue(Slot(disabled,"filters").GetProperty("disabled").GetBoolean());Assert.IsTrue(Slot(disabled,"actions").GetProperty("disabled").GetBoolean());Assert.IsFalse(Reset(disabled).GetProperty("disabled").GetBoolean());
            Assert.AreEqual(64d,Number(r.Normal("tall-slots",theme).GetProperty("surface").GetProperty("box"),"height"));
        }
        var callbacks=0;foreach(var sample in r.Cases)foreach(var state in States(sample))
        {var table=state.GetProperty("table");Assert.IsTrue(table.GetProperty("sameTable").GetBoolean());foreach(var column in table.GetProperty("columns").EnumerateArray())Assert.IsTrue(column.GetProperty("same").GetBoolean());foreach(var row in table.GetProperty("rows").EnumerateArray())Assert.IsTrue(row.GetProperty("same").GetBoolean());Assert.AreEqual(string.Join('|',table.GetProperty("rows").EnumerateArray().Select(row=>Text(row,"title"))),Text(state,"visibleRows"));foreach(var callback in state.GetProperty("events").EnumerateArray().Where(e=>Text(e,"kind")=="slot")){callbacks++;Assert.IsTrue(callback.GetProperty("sameTable").GetBoolean());Assert.IsTrue(callback.GetProperty("sameRows").GetBoolean());}}
        Assert.IsGreaterThan(10,callbacks);
    }
    [TestMethod]
    public void InputAndResetUseSourceTypographyIconAndSettledPhysicalStates()
    {
        using var r=new Reference();foreach(var theme in Themes)
        {
            var sample=r.Case("column-filter",theme);var normal=sample.GetProperty("normal");var input=r.Style(Input(normal));Assert.AreEqual("14px",Text(input,"fontSize"));Assert.AreEqual("20px",Text(input,"lineHeight"));Assert.AreEqual("0px 14px",Text(input,"padding"));Assert.AreEqual("7px",Text(input,"borderRadius"));
            var reset=Reset(normal);var style=r.Style(reset);Assert.AreEqual("13px",Text(style,"fontSize"));Assert.AreEqual("500",Text(style,"fontWeight"));Assert.AreEqual("8px",Text(style,"gap"));Near(Number(reset.GetProperty("box"),"width"),84.96875);
            var icon=Nodes(reset).Single(n=>Text(n,"tag")=="svg");Near(Number(icon.GetProperty("box"),"width"),14);Near(Number(icon.GetProperty("box"),"height"),14);
            var held=r.Style(Reset(State(sample,"reset-held")));Assert.AreEqual("0px 1px",Text(held,"translate"));Assert.AreEqual("0.99",Text(held,"scale"));
            var hover=r.Style(Reset(State(sample,"reset-hover")));StringAssert.Contains(Text(hover,"backgroundColor"),theme=="Light"?"/ 0.12":"/ 0.06");
        }
        var receipts=0;var distinctReceipts=0;foreach(var sample in r.Cases)
        {
            distinctReceipts+=States(sample).Max(state=>state.GetProperty("inputEvents").GetArrayLength());
            foreach(var gate in new[]{sample.GetProperty("normalPresentation")}.Concat(sample.GetProperty("states").EnumerateArray().Select(s=>s.GetProperty("presentation"))))
            {Assert.IsTrue(gate.GetProperty("stable").GetInt32()>=3);Assert.IsTrue(gate.GetProperty("frames").GetInt32()>=4);Assert.IsTrue(gate.GetProperty("milliseconds").GetDouble()<2000);}
            foreach(var state in States(sample))foreach(var receipt in state.GetProperty("inputEvents").EnumerateArray()){receipts++;Assert.IsTrue(receipt.GetProperty("trusted").GetBoolean());}
            foreach(var state in States(sample))foreach(var node in Nodes(state.GetProperty("surface")).Where(n=>n.TryGetProperty("styleIndex",out _)))Assert.IsTrue(node.GetProperty("styleIndex").GetInt32()<r.Root.GetProperty("styles").GetArrayLength());
        }
        Assert.IsGreaterThan(1000,receipts);
        Assert.AreEqual(1246,distinctReceipts);
    }
    [TestMethod]
    public void ReplacingSearchColumnAndOriginalTablePreservesEachOwnersState()
    {
        using var r=new Reference();foreach(var theme in Themes)
        {
            var sample=r.Case("live-source",theme);Assert.AreEqual("Vale",Text(Input(State(sample,"external-column-filter")),"value"));Assert.AreEqual("Vale",Text(State(sample,"external-global-filter"),"visibleRows"));
            var changed=State(sample,"search-column-change");Assert.AreEqual("Status search",Text(Input(changed),"placeholder"));Assert.AreEqual("",Text(Input(changed),"value"));Assert.IsFalse(Nodes(State(sample,"search-column-hidden").GetProperty("surface")).Any(n=>Text(n,"tag")=="INPUT"));
            Assert.AreEqual("B",Text(State(sample,"replace-table").GetProperty("table"),"name"));Assert.AreEqual("Reed",Text(State(sample,"second-table-search"),"visibleRows"));
            var restored=State(sample,"restore-table");Assert.AreEqual("A",Text(restored.GetProperty("table"),"name"));Assert.AreEqual("Vale",Text(Input(restored),"value"));Assert.AreEqual("Vale",Text(restored,"visibleRows"));
            Assert.AreEqual("Replacement filter",Text(Slot(State(sample,"replace-slots"),"filters"),"text"));
        }
    }
    private static readonly string[] Themes=["Light","Dark"];
    private static IEnumerable<JsonElement> States(JsonElement sample)=>new[]{sample.GetProperty("normal")}.Concat(sample.GetProperty("states").EnumerateArray().Select(s=>s.GetProperty("state")));
    private static JsonElement State(JsonElement sample,string action)=>sample.GetProperty("states").EnumerateArray().Single(s=>Text(s,"action")==action).GetProperty("state");
    private static IEnumerable<JsonElement> Nodes(JsonElement node){yield return node;if(node.TryGetProperty("children",out var children))foreach(var child in children.EnumerateArray())foreach(var nested in Nodes(child))yield return nested;}
    private static JsonElement Input(JsonElement state)=>Nodes(state.GetProperty("surface")).Single(n=>Text(n,"tag")=="INPUT");
    private static JsonElement Slot(JsonElement state,string slot)=>Nodes(state.GetProperty("surface")).First(n=>n.TryGetProperty("attributes",out var attrs)&&attrs.TryGetProperty("data-slot",out var value)&&value.GetString()==slot);
    private static JsonElement Reset(JsonElement state)=>Nodes(state.GetProperty("surface")).Single(n=>Text(n,"tag")=="BUTTON"&&Text(n,"text")=="Reset");
    private static bool HasReset(JsonElement state)=>Nodes(state.GetProperty("surface")).Any(n=>Text(n,"tag")=="BUTTON"&&Text(n,"text")=="Reset");
    private static string Text(JsonElement value,string name)=>value.GetProperty(name).GetString()!;
    private static double Number(JsonElement value,string name)=>value.GetProperty(name).GetDouble();
    private static void Near(double actual,double expected)=>Assert.IsTrue(Math.Abs(actual-expected)<.000001,$"{actual:R}/{expected:R}");
    private static void Box(JsonElement node,double x,double y,double width,double height){var box=node.GetProperty("box");Near(Number(box,"x"),x);Near(Number(box,"y"),y);Near(Number(box,"width"),width);Near(Number(box,"height"),height);}
    private sealed class Reference:IDisposable
    {
        private readonly JsonDocument document=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","DataTableToolbarLayout.json")));
        internal JsonElement Root=>document.RootElement;
        internal JsonElement[] Cases=>Root.GetProperty("cases").EnumerateArray().ToArray();
        internal JsonElement Case(string id,string scheme)=>Cases.Single(c=>Text(c,"id")==id&&Text(c,"scheme")==scheme);
        internal JsonElement Normal(string id,string scheme)=>Case(id,scheme).GetProperty("normal");
        internal JsonElement Style(JsonElement node)=>Root.GetProperty("styles")[node.GetProperty("styleIndex").GetInt32()];
        public void Dispose()=>document.Dispose();
    }
}
