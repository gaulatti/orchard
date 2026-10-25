using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Orchard.Tests;

[TestClass]
public sealed class DataTableFilterPlanningTests
{
    private static object? Value(JsonElement value)=>value.ValueKind switch{JsonValueKind.True=>true,JsonValueKind.False=>false,JsonValueKind.String=>value.GetString(),JsonValueKind.Number=>value.GetDouble(),JsonValueKind.Null=>null,_=>throw new InvalidOperationException("Unexpected source fixture value.")};
    [TestMethod]
    [DataRow("no-columns")][DataRow("boolean-only")][DataRow("boolean-column-filter")][DataRow("hidden-eligible")]
    public void CapturedEligibilityPipelinesReachOriginalRows(string name)
    {
        using var reference=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","DataTableFilterPlan.json")));
        foreach(var entry in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c=>c.GetProperty("name").GetString()==name))
        {
            var model=new BleeckerDataTableModel();var definitions=new List<BleeckerDataTableColumn>{new(id:"select"),new(accessorKey:"name"),new(accessorKey:"count"),new(accessorKey:"status")};if(entry.GetProperty("spec").TryGetProperty("boolean",out var boolean)&&boolean.GetBoolean())definitions.Add(new(accessorKey:"flag"));model.SetColumns(definitions);
            var rows=entry.GetProperty("spec").GetProperty("rows").EnumerateArray().Select(r=>(object)r.EnumerateObject().ToDictionary(p=>p.Name,p=>Value(p.Value),StringComparer.Ordinal)).ToArray();model.SetData(rows);
            foreach(var step in entry.GetProperty("snapshots").EnumerateArray())
            {
                var stage=step.GetProperty("stage").GetString();if(stage=="prepared"){if(name=="no-columns")model.SetColumns([]);else if(name=="boolean-column-filter")model.GetColumn("flag")!.SetFilterValue(true);else if(name=="hidden-eligible")foreach(var id in new[]{"name","count","status"})model.GetColumn(id)!.ToggleVisibility(false);}if(stage=="global")model.SetGlobalFilter(entry.GetProperty("query").GetString());
                var expected=step.GetProperty("snapshot");CollectionAssert.AreEqual(expected.GetProperty("filteredIds").EnumerateArray().Select(i=>i.GetString()).ToArray(),model.FilteredRows.Select(r=>r.Id).ToArray(),name+"/"+stage);CollectionAssert.AreEqual(expected.GetProperty("visibleOriginalIds").EnumerateArray().Select(i=>i.GetString()).ToArray(),model.PageRows.Select(r=>((Dictionary<string,object?>)r.Original)["id"] as string).ToArray());Assert.AreEqual(expected.GetProperty("pageCount").GetInt32(),model.PageCount);foreach(var row in model.CoreRows)Assert.AreSame(rows[row.Index],row.Original);Assert.IsTrue(JsonNode.DeepEquals(JsonNode.Parse(expected.GetProperty("state").GetProperty("columnFilters").GetRawText()),JsonSerializer.SerializeToNode(model.ColumnFilters.Select(f=>new{id=f.Id,value=f.Value}))));
            }
        }
    }
    private sealed class Range:IEnumerable<object?>
    {
        internal int Enumerations,Minimum=5,Maximum=12;
        public IEnumerator<object?> GetEnumerator(){Enumerations++;yield return Minimum;yield return Maximum;}
        IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    [TestMethod]
    public void NumericBoundsResolveOncePerPassAndRefreshReadsCurrentBounds()
    {
        var model=new BleeckerDataTableModel();model.SetColumns([new(id:"count",accessor:(_,index)=>index)]);model.SetData(Enumerable.Range(0,1024).Select(_=>new object()));var range=new Range();model.GetColumn("count")!.SetFilterValue(range);
        Assert.AreEqual(2,range.Enumerations,"Removal validation and one filter-pass resolution must not repeat per original row.");CollectionAssert.AreEqual(Enumerable.Range(5,8).ToArray(),model.FilteredRows.Select(r=>r.Index).ToArray());range.Minimum=8;range.Maximum=10;model.RefreshData();Assert.AreEqual(3,range.Enumerations);CollectionAssert.AreEqual(new[]{8,9,10},model.FilteredRows.Select(r=>r.Index).ToArray());
    }
    [TestMethod]
    public void EveryCallerPredicateRunsInSourceOrderWithoutProbingUnusedAccessors()
    {
        var calls=new List<string>();var accessorCalls=0;var model=new BleeckerDataTableModel();object? Access(object item,int index){accessorCalls++;return index;}
        model.SetColumns([new(id:"first",accessor:Access,filter:(row,id,value)=>{calls.Add(id+"/"+row.Index);return false;}),new(id:"second",accessor:Access,filter:(row,id,value)=>{calls.Add(id+"/"+row.Index);return true;})]);model.SetData(Enumerable.Range(0,3).Select(_=>new object()));model.GetColumn("first")!.SetFilterValue("active");model.GetColumn("second")!.SetFilterValue("active");calls.Clear();accessorCalls=0;model.RefreshData();CollectionAssert.AreEqual(new[]{"first/0","second/0","first/1","second/1","first/2","second/2"},calls);Assert.AreEqual(0,accessorCalls);Assert.AreEqual(0,model.FilteredRowCount);
    }
    [TestMethod]
    public void DataAndColumnChangesReevaluateGlobalEligibility()
    {
        static object Row(object? value)=>new Dictionary<string,object?>{{"value",value}};
        var model=new BleeckerDataTableModel();model.SetColumns([new(accessorKey:"value")]);model.SetData(new[]{Row(true),Row(false)});model.SetGlobalFilter("true");Assert.AreEqual(2,model.FilteredRowCount);
        model.SetData(new[]{Row("true"),Row("false")});Assert.AreEqual(1,model.FilteredRowCount);Assert.AreEqual("0",model.PageRows.Single().Id);model.SetColumns([new(accessorKey:"value",enableGlobalFilter:false)]);Assert.AreEqual(2,model.FilteredRowCount);model.SetColumns([new(accessorKey:"value")]);model.SetData(new[]{Row(null),Row("true")});Assert.AreEqual(2,model.FilteredRowCount,"Source eligibility is determined by the first core row.");
    }
}
