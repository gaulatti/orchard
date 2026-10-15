using System.Collections;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class DataTableToolbarsFixture
{
    private sealed class ToolbarScenario:IDisposable
    {
        private readonly DataTableToolbarsFixture owner;
        internal readonly ToolbarFilterTable A=new(),B=new(second:true);
        internal ToolbarFilterTable Active;
        internal string ActiveName=>ReferenceEquals(Active,A)?"A":"B";
        internal readonly Grid Host;
        internal readonly BleeckerDataTableToolbar Bar;
        internal readonly Button Before=new(){Name="Before",Content="Before toolbar",Height=24,MinHeight=0,HorizontalAlignment=HorizontalAlignment.Left};
        internal readonly Button After=new(){Name="After",Content="After toolbar",Height=24,MinHeight=0,HorizontalAlignment=HorizontalAlignment.Left};
        // A visible native host control represents leaving the toolbar's test
        // document. The producer leaves its document for browser chrome here.
        internal readonly Button Boundary=new(){Name="OutsideToolbar",Content="Outside toolbar",Height=24,MinHeight=0,HorizontalAlignment=HorizontalAlignment.Left};
        private readonly TextBlock probe=new();
        private readonly HashSet<Button> registered=[];
        private readonly HashSet<Button> suppliedNodes=[];
        private readonly Dictionary<ToolbarFilterTable,Dictionary<string,IBleeckerDataTableFilterColumn>> originalColumns;
        internal sealed record SlotCall(string Role,int Index,string Table,bool OriginalTable,bool OriginalRows,bool OriginalNode);
        internal readonly List<SlotCall> Calls=[];
        internal BleeckerInput Search=>Descendants<BleeckerInput>(Bar).Single();
        internal Button Reset=>Descendants<Button>(Bar).Single(button=>button.Name=="PART_Reset");
        internal ToolbarScenario(DataTableToolbarsFixture owner,JsonElement sample,BleeckerColorScheme scheme)
        {
            this.owner=owner;Active=A;var props=sample.GetProperty("props");
            A.SetInitial(Filters(props,"initialColumnFilters"));A.SetFilters(Filters(props,"columnFilters"));if(props.TryGetProperty("globalFilter",out var global))A.SetGlobal(Value(global));
            B.SetInitial(Filters(props,"secondInitialColumnFilters"));B.SetFilters(Filters(props,"secondColumnFilters"));if(props.TryGetProperty("secondGlobalFilter",out var secondGlobal))B.SetGlobal(Value(secondGlobal));
            originalColumns=new(){[A]=new[]{"title","status"}.ToDictionary(id=>id,id=>A.GetColumn(id)!),[B]=new[]{"category","status"}.ToDictionary(id=>id,id=>B.GetColumn(id)!)};
            A.Changed+=Changed;B.Changed+=Changed;
            Host=new Grid{Margin=new Thickness(16),Background=((Grid)owner.XamlRoot.Content).Background,RequestedTheme=scheme==BleeckerColorScheme.Light?ElementTheme.Light:ElementTheme.Dark,TabFocusNavigation=KeyboardNavigationMode.Cycle};
            Grid.SetRowSpan(Host,((Grid)owner.XamlRoot.Content).RowDefinitions.Count);Grid.SetColumnSpan(Host,((Grid)owner.XamlRoot.Content).ColumnDefinitions.Count);
            for(var row=0;row<5;row++)Host.RowDefinitions.Add(new(){Height=GridLength.Auto});
            Bar=new BleeckerDataTableToolbar{Table=A,Width=sample.GetProperty("hostWidth").GetDouble(),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,FlowDirection=sample.GetProperty("direction").GetString()=="rtl"?FlowDirection.RightToLeft:FlowDirection.LeftToRight};
            if(props.TryGetProperty("searchColumn",out var column))Bar.SearchColumn=column.GetString();if(props.TryGetProperty("searchPlaceholder",out var placeholder))Bar.SearchPlaceholder=placeholder.GetString()!;
            Bar.FilterRenderers=props.TryGetProperty("filters",out var filters)?Content(filters,"filters"):null;
            Bar.Actions=props.TryGetProperty("actions",out var actions)?Content(actions,"actions"):null;
            var children=new FrameworkElement[]{Before,Bar,After,Boundary,probe};for(var row=0;row<children.Length;row++){Grid.SetRow(children[row],row);Host.Children.Add(children[row]);}
            probe.Text=A.VisibleText;
        }
        private void Changed(object? sender,EventArgs args){if(ReferenceEquals(sender,Active))probe.Text=Active.VisibleText;}
        private object? Content(JsonElement value,string role)
        {
            var content=owner.SourceContent(value,role,Bar,Active);
            if(content is IEnumerable elements)foreach(var button in elements.Cast<object?>().OfType<Button>())suppliedNodes.Add(button);
            else if(content is Button button)suppliedNodes.Add(button);
            return content;
        }
        internal void SelectTable(bool second){Active=second?B:A;Bar.Table=Active;probe.Text=Active.VisibleText;}
        internal Button Slot(string role)=>Descendants<Button>(Bar).First(button=>button.Tag is string tag&&tag.StartsWith(role+":",StringComparison.Ordinal));
        internal void RegisterSlots()
        {
            foreach(var button in Descendants<Button>(Bar).Where(button=>button.Tag is string tag&&(tag.StartsWith("filters:",StringComparison.Ordinal)||tag.StartsWith("actions:",StringComparison.Ordinal))))
            {
                if(!registered.Add(button))continue;var model=Active;var original=button;var parts=((string)button.Tag).Split(':');var role=parts[0];var index=int.Parse(parts[1],System.Globalization.CultureInfo.InvariantCulture);
                button.Click+=(_,_)=>Calls.Add(new(role,index,ActiveName,ReferenceEquals(model,Active)&&ReferenceEquals(Bar.Table,Active),ReferenceEquals(model.Rows,ToolbarFilterTable.OriginalRows),suppliedNodes.Contains(original)&&Descendants<Button>(Bar).Any(candidate=>ReferenceEquals(candidate,original))));
            }
        }
        internal void ReplaceSlots()
        {
            using var props=JsonDocument.Parse("{\"filters\":{\"kind\":\"buttons\",\"labels\":[\"Replacement filter\"],\"width\":180},\"actions\":{\"kind\":\"buttons\",\"labels\":[\"Replacement action\"],\"width\":150}}");
            Bar.FilterRenderers=Content(props.RootElement.GetProperty("filters"),"filters");Bar.Actions=Content(props.RootElement.GetProperty("actions"),"actions");
        }
        internal Control FocusTarget(string role)=>role switch{"INPUT"=>Search,"BUTTON"=>Reset,"filters"=>Slot("filters"),"actions"=>Slot("actions"),"before"=>Before,"after"=>After,"body"=>Boundary,_=>throw new InvalidOperationException("Unknown source focus role: "+role)};
        internal string DescribeFocus()
        {
            var focused=FocusManager.GetFocusedElement(Host.XamlRoot);if(focused is null||ReferenceEquals(focused,Boundary))return "body";
            if(ReferenceEquals(focused,Before))return "before";if(ReferenceEquals(focused,After))return "after";
            if(focused is BleeckerInput)return "INPUT";if(focused is Button button){if(button.Name=="PART_Reset")return "BUTTON";if(button.Tag is string tag)return tag.Split(':')[0];}
            return "outside:"+focused.GetType().Name;
        }
        internal object FocusEvidence()
        {
            var focused=FocusManager.GetFocusedElement(Host.XamlRoot);
            return new{type=focused?.GetType().Name,name=(focused as FrameworkElement)?.Name,nativeHostBoundary=ReferenceEquals(focused,Boundary),role=DescribeFocus()};
        }
        internal void VerifyConsumer(JsonElement expected)
        {
            var table=expected.GetProperty("table");Require(ActiveName==table.GetProperty("name").GetString()&&ReferenceEquals(Bar.Table,Active),"Original active table differs.");
            Require(Active.VisibleText==expected.GetProperty("visibleRows").GetString()&&probe.Text==Active.VisibleText&&probe.IsLoaded,"Consumer row projection differs.");
            var sourceFilters=table.GetProperty("state").GetProperty("columnFilters").EnumerateArray().ToArray();
            Require(Active.CurrentFilters.Count==sourceFilters.Length&&sourceFilters.All(filter=>Active.CurrentFilters.TryGetValue(filter.GetProperty("id").GetString()!,out var value)&&SameValue(value,Value(filter.GetProperty("value")))),"Original column filter state differs.");
            Require(SameValue(Active.GlobalFilter,Value(table.GetProperty("state").GetProperty("globalFilter"))),"Original global filtering differs.");
            foreach(var column in table.GetProperty("columns").EnumerateArray())
            {var id=column.GetProperty("id").GetString()!;Require(ReferenceEquals(Active.GetColumn(id),originalColumns[Active][id])&&SameValue(Active.GetColumn(id)!.GetFilterValue(),Value(column.GetProperty("value"))),"Original column identity/value differs.");}
            var sourceRows=table.GetProperty("rows").EnumerateArray().ToArray();Require(sourceRows.Length==Active.VisibleRows.Count,"Original visible row count differs.");
            for(var index=0;index<sourceRows.Length;index++)Require(Active.VisibleRows[index].Id==sourceRows[index].GetProperty("id").GetString()&&ToolbarFilterTable.OriginalRows.Any(row=>ReferenceEquals(row,Active.VisibleRows[index])),"Original row identity/order differs.");
        }
        private static bool SameValue(object? first,object? second)
        {
            if(first is IDictionary a&&second is IDictionary b)return a.Count==b.Count&&a.Keys.Cast<object>().All(key=>b.Contains(key)&&SameValue(a[key],b[key]));
            if(first is IEnumerable aItems&&first is not string&&second is IEnumerable bItems&&second is not string)
            {var aValues=aItems.Cast<object?>().ToArray();var bValues=bItems.Cast<object?>().ToArray();return aValues.Length==bValues.Length&&aValues.Zip(bValues).All(pair=>SameValue(pair.First,pair.Second));}
            return Equals(first,second);
        }
        public void Dispose(){A.Changed-=Changed;B.Changed-=Changed;}
    }
}
