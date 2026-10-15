using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Orchard.Catalog;

// An actual consumer filtering model: original rows/columns, independently
// owned initial/current filters and a derived view. No toolbar API is mocked.
internal sealed class ToolbarFilterTable:IBleeckerDataTableFilterSource
{
    internal sealed record Row(string Id,string Title,string Status);
    internal sealed class Column(ToolbarFilterTable owner,string id):IBleeckerDataTableFilterColumn
    {
        internal string Id=>id;
        public object? GetFilterValue()=>owner.filters.GetValueOrDefault(id);
        public void SetFilterValue(object? value){owner.Writes.Add((this,value));if(value is string empty&&empty.Length==0||value is null)owner.filters.Remove(id);else owner.filters[id]=value;owner.Publish();}
    }
    internal static readonly Row[] OriginalRows=[new("harbor","Harbor","Draft"),new("reed","Reed","Published"),new("vale","Vale","Draft")];
    private readonly Dictionary<string,object?> filters=new(StringComparer.Ordinal),initial=new(StringComparer.Ordinal);
    private readonly Dictionary<string,Column> columns;
    private EventHandler? changed;
    public event EventHandler? Changed{add{changed+=value;Subscribers++;}remove{changed-=value;Subscribers--;}}
    internal int Subscribers{get;private set;}
    internal int Resets{get;private set;}
    internal List<(Column Column,object? Value)> Writes{get;}=[];
    internal ObservableCollection<Row> VisibleRows{get;}=[];
    internal string VisibleText=>string.Join('|',VisibleRows.Select(row=>row.Title));
    public int ColumnFilterCount=>filters.Count;
    public object? GlobalFilter{get;private set;}
    internal ToolbarFilterTable(bool second=false)
    {columns=new(StringComparer.Ordinal){[second?"category":"title"]=new(this,second?"category":"title"),["status"]=new(this,"status")};Publish();}
    public IBleeckerDataTableFilterColumn? GetColumn(string id)=>columns.GetValueOrDefault(id);
    public void ResetColumnFilters(){Resets++;filters.Clear();foreach(var item in initial)filters.Add(item.Key,item.Value);Publish();}
    internal void SetFilters(IEnumerable<KeyValuePair<string,object?>> value){filters.Clear();foreach(var item in value)filters.Add(item.Key,item.Value);Publish();}
    internal void SetInitial(IEnumerable<KeyValuePair<string,object?>> value){initial.Clear();foreach(var item in value)initial.Add(item.Key,item.Value);}
    internal void SetGlobal(object? value){GlobalFilter=value;Publish();}
    private void Publish()
    {
        var rows=OriginalRows.Where(row=>filters.All(pair=>Cell(row,pair.Key).Contains(pair.Value is null?"undefined":Text(pair.Value),StringComparison.OrdinalIgnoreCase))&&(!Truthy(GlobalFilter)||new[]{row.Title,row.Status}.Any(cell=>cell.Contains(Text(GlobalFilter),StringComparison.OrdinalIgnoreCase)))).ToArray();
        VisibleRows.Clear();foreach(var row in rows)VisibleRows.Add(row);changed?.Invoke(this,EventArgs.Empty);
    }
    private static string Cell(Row row,string id)=>id is "title" or "category"?row.Title:id=="status"?row.Status:throw new ArgumentException("Unknown consumer column.");
    private static bool Truthy(object? value)=>value switch{null=>false,bool flag=>flag,string text=>text.Length>0,double number=>number!=0&&!double.IsNaN(number),int number=>number!=0,_=>true};
    private static string Text(object? value)=>value switch{null=>"",string text=>text,bool flag=>flag?"true":"false",IDictionary=>"[object Object]",IEnumerable array=>string.Join(',',array.Cast<object?>().Select(Text)),IFormattable number=>number.ToString(null,CultureInfo.InvariantCulture),_=>"[object Object]"};
}
