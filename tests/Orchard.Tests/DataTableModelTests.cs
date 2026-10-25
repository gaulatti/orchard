using System.Text.Json;
using System.Text.Json.Nodes;

namespace Orchard.Tests;

[TestClass]
public sealed class DataTableModelTests
{
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "DataTable.json")));
    private static object? ConvertValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => null, JsonValueKind.True => true, JsonValueKind.False => false,
        JsonValueKind.String => value.GetString(), JsonValueKind.Number => value.GetDouble(),
        JsonValueKind.Array => value.EnumerateArray().Select(ConvertValue).ToArray(),
        JsonValueKind.Object => value.EnumerateObject().ToDictionary(p => p.Name, p => ConvertValue(p.Value), StringComparer.Ordinal),
        _ => BleeckerDataTableModel.Undefined
    };
    private static object[] DefaultRows() => Enumerable.Range(0,23).Select(index => (object)new Dictionary<string,object?>
    { ["id"]="fictional-"+index.ToString("D2"),["name"]=new[]{"Cedar","Birch","Ash"}[index%3]+" "+(index+1),["count"]=(index*7)%29,["status"]=index%2==0?"Queued":"Ready",["flag"]=index%2==0,["profile"]=new Dictionary<string,object?>{["name"]="Profile "+(index+1)} }).ToArray();
    private sealed class Fixture
    {
        internal readonly BleeckerDataTableModel Model = new();
        internal JsonObject Spec;
        internal object[] Items;
        internal readonly List<object> Events = [];
        internal Fixture(JsonElement spec)
        {
            Spec = (JsonObject)JsonNode.Parse(spec.GetRawText())!;
            Items = spec.TryGetProperty("rows",out var rows) ? rows.EnumerateArray().Select(row=>ConvertValue(row)!).ToArray() : DefaultRows();
            Model.SetColumns(Definitions()); Model.SetData(Items);
            if(spec.TryGetProperty("selected",out var selected))Model.SetRowSelection(selected.EnumerateArray().Select(v=>v.GetString()!));
        }
        private bool Flag(string name) => Spec[name]?.GetValue<bool>()==true;
        internal BleeckerDataTableColumn[] Definitions()
        {
            if(Flag("noColumns"))return [];
            var name=new BleeckerDataTableColumn(accessorKey:Flag("nested")?"profile.name":"name",header:"Fictional item",comparison:Flag("customCompare")?(a,b,id)=>((string)a.GetValue(id)!).Length-((string)b.GetValue(id)!).Length:null);
            var count=new BleeckerDataTableColumn(accessorKey:"count",header:"Count");
            var status=new BleeckerDataTableColumn(accessorKey:"status",header:"Status",headerContentSelector:Flag("blankHeader")?_=>null:null,cellContentSelector:cell=>new Action(()=>Events.Add(new {kind="original-cell-action",row=((Dictionary<string,object?>)cell.Row.Original)["id"],value=cell.GetValue()})));
            var definitions=Flag("groups")?new List<BleeckerDataTableColumn>{new(id:"description",header:"Description",columns:Flag("deepGroups")?[new(id:"identity",header:"Identity",columns:[name]),count]:[name,count]),status}:new List<BleeckerDataTableColumn>{name,count,status};
            if(Flag("boolean"))definitions.Add(new(accessorKey:"flag",header:"Flag"));
            if(Flag("display"))definitions.Add(new(id:"details",header:"Consumer content",cellContentSelector:cell=>"Original "+((Dictionary<string,object?>)cell.Row.Original)["id"]));
            if(!Flag("noSelection"))definitions.Insert(0,new(id:"select",headerContentSelector:_=>new object(),cellContentSelector:_=>new object()));
            return definitions.ToArray();
        }
        internal void PatchColumns(string name,bool value){Spec[name]=value;Model.SetColumns(Definitions());}
        internal void Replace(JsonElement values){Items=values.EnumerateArray().Select(row=>ConvertValue(row)!).ToArray();Model.SetData(Items);}
        internal void Search(string value){var key=Spec["searchKey"]?.GetValue<string>();if(key is not null)Model.GetColumn(key)?.SetFilterValue(value);}
    }
    private static string[] Strings(JsonElement value)=>value.EnumerateArray().Select(v=>v.GetString()!).ToArray();
    private static void JsonEqual(JsonElement expected,object? actual,string message)=>Assert.IsTrue(JsonNode.DeepEquals(JsonNode.Parse(expected.GetRawText()),JsonSerializer.SerializeToNode(actual)),message+" expected="+expected.GetRawText()+" actual="+JsonSerializer.Serialize(actual));
    private static void AssertState(Fixture fixture,JsonElement snapshot)
    {
        var model=fixture.Model;var stage=snapshot.GetProperty("stage").GetString();var state=snapshot.GetProperty("state");
        CollectionAssert.AreEqual(Strings(snapshot.GetProperty("filteredRows")),model.FilteredRows.Select(r=>r.Id).ToArray(),stage+" filtered");
        CollectionAssert.AreEqual(Strings(snapshot.GetProperty("sortedRows")),model.SortedRows.Select(r=>r.Id).ToArray(),stage+" sorted");
        CollectionAssert.AreEqual(snapshot.GetProperty("visibleRows").EnumerateArray().Select(r=>r.GetProperty("id").GetString()).ToArray(),model.PageRows.Select(r=>r.Id).ToArray(),stage+" page");
        CollectionAssert.AreEqual(Strings(snapshot.GetProperty("filteredSelectedRows")),model.FilteredSelectedRows.Select(r=>r.Id).ToArray(),stage+" selected");
        CollectionAssert.AreEquivalent(state.GetProperty("rowSelection").EnumerateObject().Where(p=>p.Value.GetBoolean()).Select(p=>p.Name).ToArray(),model.SelectedRowIds.ToArray(),stage+" retained selection map");
        JsonEqual(state.GetProperty("sorting"),model.Sorting.Select(s=>new{id=s.Id,desc=s.Descending}).ToArray(),stage+" sorting");
        JsonEqual(state.GetProperty("columnFilters"),model.ColumnFilters.Select(f=>new{id=f.Id,value=f.Value}).ToArray(),stage+" filters");
        JsonEqual(state.GetProperty("columnVisibility"),model.ColumnVisibility,stage+" visibility");JsonEqual(state.GetProperty("globalFilter"),model.GlobalFilter,stage+" global");
        Assert.AreEqual(state.GetProperty("pagination").GetProperty("pageIndex").GetInt32(),model.PageIndex);Assert.AreEqual(10,model.PageSize);Assert.AreEqual(snapshot.GetProperty("pageCount").GetInt32(),model.PageCount);Assert.AreEqual(snapshot.GetProperty("canPrevious").GetBoolean(),model.CanPreviousPage);Assert.AreEqual(snapshot.GetProperty("canNext").GetBoolean(),model.CanNextPage);
        var expectedRows=snapshot.GetProperty("visibleRows").EnumerateArray().ToArray();
        for(var i=0;i<expectedRows.Length;i++)
        {
            var row=model.PageRows[i];Assert.AreSame(fixture.Items[row.Index],row.Original,stage+" original row");Assert.AreEqual(expectedRows[i].GetProperty("selected").GetBoolean(),row.IsSelected);
            Assert.AreEqual(expectedRows[i].GetProperty("originalId").GetString(),((Dictionary<string,object?>)row.Original)["id"]);
            var cells=row.GetVisibleCells();var expected=expectedRows[i].GetProperty("values").EnumerateArray().ToArray();Assert.AreEqual(expected.Length,cells.Count);
            for(var j=0;j<expected.Length;j++){var value=cells[j].GetValue();Assert.AreEqual(expected[j].GetProperty("column").GetString(),cells[j].Column.Id);Assert.AreEqual(expected[j].GetProperty("valueType").GetString(),ReferenceEquals(value,BleeckerDataTableModel.Undefined)?"undefined":value is null?"null":value is string?"string":value is bool?"boolean":value is object[]?"array":value is Dictionary<string,object?>?"object":"number");if(!ReferenceEquals(value,BleeckerDataTableModel.Undefined))JsonEqual(expected[j].GetProperty("value"),value,stage+" original cell value");}
        }
        var groups=model.GetHeaderGroups();JsonEqual(snapshot.GetProperty("headerGroups"),groups.Select(g=>new{id=g.Id,headers=g.Headers.Select(h=>new{id=h.Id,column=h.Column.Id,placeholder=h.IsPlaceholder,colSpan=h.ColumnSpan,rowSpan=h.RowSpan}).ToArray()}).ToArray(),stage+" groups");
        JsonEqual(snapshot.GetProperty("events"),fixture.Events,stage+" original actions");
    }
    [TestMethod]
    [DataRow("default")][DataRow("search")][DataRow("missing-search")][DataRow("empty")][DataRow("one-row")][DataRow("exact-page")][DataRow("long-constrained")][DataRow("rtl")][DataRow("grouped")][DataRow("nested")][DataRow("mixed-values")][DataRow("plain-headers")][DataRow("display-column")][DataRow("blank-header")][DataRow("boolean")][DataRow("grouped-empty")][DataRow("deep-groups")][DataRow("caller-error")][DataRow("disabled-original")][DataRow("selected-unhovered")][DataRow("sparse-values")]
    public void OriginalStaticRowModels(string name)
    {
        using var reference=Reference();foreach(var item in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c=>c.GetProperty("name").GetString()==name)){var fixture=new Fixture(item.GetProperty("spec"));AssertState(fixture,item.GetProperty("snapshot"));}
    }
    [TestMethod]
    [DataRow("paging")][DataRow("search-reset")][DataRow("sort-cycle")][DataRow("multi-sort")][DataRow("selection-pages")][DataRow("replace-data")][DataRow("visibility")][DataRow("consumer-filters")][DataRow("numeric-search")][DataRow("boolean-search")][DataRow("original-action")][DataRow("grouped-headers")][DataRow("global-filter")][DataRow("missing-search")][DataRow("custom-compare")][DataRow("empty-columns")][DataRow("short-auto-sort")]
    public void OriginalInteractionPipelines(string action)
    {
        using var reference=Reference();foreach(var history in reference.RootElement.GetProperty("histories").EnumerateArray().Where(h=>h.GetProperty("action").GetString()==action))
        {
            var fixture=new Fixture(history.GetProperty("spec"));var model=fixture.Model;
            foreach(var snapshot in history.GetProperty("snapshots").EnumerateArray())
            {
                var stage=snapshot.GetProperty("stage").GetString()!;
                if(stage!="initial")switch(action)
                {
                    case "paging": if(stage is "next-page" or "last-page")model.NextPage();else model.PreviousPage();break;
                    case "search-reset": if(stage=="page-two")model.NextPage();else fixture.Search(stage=="filter-cedar"?"Cedar":stage=="case-insensitive"?"cEdAr":stage=="no-results"?"no fictional match":"");break;
                    case "sort-cycle": model.GetColumn(stage.StartsWith("name",StringComparison.Ordinal)?"name":"count")!.ToggleSorting();break;
                    case "multi-sort": model.GetColumn(stage=="name-first"?"name":"count")!.ToggleSorting(multi:stage!="name-first");break;
                    case "selection-pages": if(stage=="select-first")model.CoreRows[0].ToggleSelected();else if(stage=="page-two")model.NextPage();else if(stage=="select-second-page")model.CoreRows[10].ToggleSelected();else if(stage is "filter-selected" or "filter-unselected" or "clear-filter")fixture.Search(stage=="filter-selected"?"Cedar":stage=="filter-unselected"?"Ash":"");else if(stage is "select-page" or "unselect-page")model.ToggleAllPageRowsSelected();break;
                    case "replace-data": if(stage=="select-index-zero")model.CoreRows[0].ToggleSelected();else if(stage=="page-two")model.NextPage();else if(stage=="replace-array")fixture.Replace(JsonDocument.Parse("[{\"id\":\"replacement-a\",\"name\":\"Replacement A\",\"count\":1,\"status\":\"Ready\"},{\"id\":\"replacement-b\",\"name\":\"Replacement B\",\"count\":2,\"status\":\"Queued\"}]").RootElement);else fixture.Replace(JsonDocument.Parse("[]").RootElement);break;
                    case "visibility": model.GetColumn(stage.EndsWith("status",StringComparison.Ordinal)?"status":"name")!.ToggleVisibility(stage.StartsWith("show",StringComparison.Ordinal));break;
                    case "consumer-filters": if(stage=="count-range")model.GetColumn("count")!.SetFilterValue(new object[]{5,12});else if(stage=="second-column-filter")model.GetColumn("name")!.SetFilterValue("Cedar");else if(stage=="clear-name")model.GetColumn("name")!.SetFilterValue("");else model.GetColumn("count")!.SetFilterValue(null);break;
                    case "numeric-search":fixture.Search(stage=="numeric-string-25"?"25":stage=="numeric-string-5"?"5":"");break;
                    case "boolean-search":fixture.Search(stage=="boolean-string-true"?"true":"");break;
                    case "missing-search":fixture.Search(stage=="missing-key-edit"?"Cedar":"");break;
                    case "original-action":if(stage=="original-cell")((Action)model.PageRows[0].GetVisibleCells().Single(c=>c.Column.Id=="status").RenderContent()!)();else if(stage=="sort-desc-count")model.GetColumn("count")!.ToggleSorting();else model.NextPage();break;
                    case "grouped-headers":if(stage=="replace-with-flat-columns")fixture.PatchColumns("groups",false);else model.GetColumn("count")!.ToggleVisibility(stage=="restore-count");break;
                    case "global-filter":model.SetGlobalFilter(stage=="global-cedar"?"Cedar":stage=="global-three"?"3":"");break;
                    case "custom-compare":model.GetColumn("name")!.ToggleSorting();break;
                    case "empty-columns":if(stage=="no-columns")fixture.PatchColumns("noColumns",true);else if(stage=="no-columns-no-data")fixture.Replace(JsonDocument.Parse("[]").RootElement);else if(stage=="columns-restored-empty")fixture.PatchColumns("noColumns",false);else{fixture.Items=DefaultRows();model.SetData(fixture.Items);}break;
                    case "short-auto-sort":model.GetColumn(stage=="short-count-first"?"count":"name")!.ToggleSorting();break;
                    default:Assert.Fail("Unknown source action "+action);break;
                }
                AssertState(fixture,snapshot);
            }
        }
    }
}
