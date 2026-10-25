using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;

namespace Orchard.Testing;
internal static partial class DataTableReferenceFixture
{
    private static object? ConvertValue(JsonElement value)=>value.ValueKind switch{JsonValueKind.Null=>null,JsonValueKind.True=>true,JsonValueKind.False=>false,JsonValueKind.String=>value.GetString(),JsonValueKind.Number=>value.GetDouble(),JsonValueKind.Array=>value.EnumerateArray().Select(ConvertValue).ToArray(),JsonValueKind.Object=>value.EnumerateObject().ToDictionary(p=>p.Name,p=>ConvertValue(p.Value),StringComparer.Ordinal),_=>BleeckerDataTableModel.Undefined};
    private static object[] DefaultRows()=>Enumerable.Range(0,23).Select(i=>(object)new Dictionary<string,object?>{["id"]="fictional-"+i.ToString("D2"),["name"]=new[]{"Cedar","Birch","Ash"}[i%3]+" "+(i+1),["count"]=(i*7)%29,["status"]=i%2==0?"Queued":"Ready",["flag"]=i%2==0,["profile"]=new Dictionary<string,object?>{["name"]="Profile "+(i+1)}}).ToArray();
    private sealed class Context
    {
        internal readonly BleeckerDataTable Data=new(){Margin=new Thickness(20),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top};
        internal BleeckerDataTableModel Model=>Data.Model;
        private BleeckerTable Table=>Elements(Data).OfType<BleeckerTable>().Single();
        private BleeckerInput Search=>Elements(Data).OfType<BleeckerInput>().Single(e=>e.Name=="PART_Search");
        internal JsonObject Spec;
        internal object[] Items;
        private readonly List<object> events=[];
        private readonly Dictionary<string,BleeckerButton> headings=new(StringComparer.Ordinal);
        private readonly Dictionary<string,BleeckerCheckbox> checks=new(StringComparer.Ordinal);
        private readonly Dictionary<object,BleeckerButton> actions=new(ReferenceEqualityComparer.Instance);
        private BleeckerCheckbox? all;
        private bool multi;
        internal Context(JsonElement spec)
        {
            Spec=(JsonObject)JsonNode.Parse(spec.GetRawText())!;Data.Width=Spec["width"]?.GetValue<double>()??660;Data.FlowDirection=Flag("rtl")?FlowDirection.RightToLeft:FlowDirection.LeftToRight;Data.SearchKey=Spec["searchKey"]?.GetValue<string>();
            Items=spec.TryGetProperty("rows",out var values)?values.EnumerateArray().Select(v=>ConvertValue(v)!).ToArray():DefaultRows();SetColumns();Data.ItemsSource=Items;
            if(spec.TryGetProperty("selected",out var selected))Model.SetRowSelection(selected.EnumerateArray().Select(v=>v.GetString()!));
        }
        private bool Flag(string name)=>Spec[name]?.GetValue<bool>()==true;
        private object Heading(BleeckerDataTableHeader context)
        {
            if(Flag("plainHeaders"))return context.Column.Header!;
            if(!headings.TryGetValue(context.Column.Id,out var button)){var id=context.Column.Id;button=new BleeckerButton{Variant=BleeckerButtonVariant.Ghost,Content=context.Column.Header};AutomationProperties.SetName(button,"Sort "+id);button.Click+=(_,_)=>Model.GetColumn(id)!.ToggleSorting(multi:multi);headings.Add(id,button);}return button;
        }
        private object HeaderSelection(BleeckerDataTableHeader context)
        {
            if(all is null){all=new BleeckerCheckbox{Width=20,Height=20,MinWidth=0,MinHeight=0};AutomationProperties.SetName(all,"Select page");void Changed(object sender,RoutedEventArgs args){if(all.IsChecked!=Model.AllPageRowsSelected)Model.ToggleAllPageRowsSelected(all.IsChecked==true);}all.Checked+=Changed;all.Unchecked+=Changed;}
            all.IsChecked=context.Table.AllPageRowsSelected;return all;
        }
        private object Selection(BleeckerDataTableCell cell)
        {
            var id=cell.Row.Id;if(!checks.TryGetValue(id,out var check)){check=new BleeckerCheckbox{Width=20,Height=20,MinWidth=0,MinHeight=0};AutomationProperties.SetName(check,"Select row "+id);void Changed(object sender,RoutedEventArgs args){var row=Model.CoreRows.FirstOrDefault(r=>r.Id==id);if(row is not null&&check.IsChecked!=row.IsSelected)row.ToggleSelected(check.IsChecked==true);}check.Checked+=Changed;check.Unchecked+=Changed;checks.Add(id,check);}check.IsChecked=cell.Row.IsSelected;return check;
        }
        private object Original(BleeckerDataTableCell cell)
        {
            if(!actions.TryGetValue(cell.Row.Original,out var button)){button=new BleeckerButton{Variant=BleeckerButtonVariant.Ghost};button.Click+=(_,_)=>{var current=(BleeckerDataTableCell)button.Tag;events.Add(new{kind="original-cell-action",row=((Dictionary<string,object?>)current.Row.Original)["id"],value=current.GetValue()});};actions.Add(cell.Row.Original,button);}button.Tag=cell;button.Content=BleeckerDataTableModel.CellText(cell.GetValue());button.IsEnabled=!Flag("disabledStatus");return button;
        }
        private void SetColumns()
        {
            Data.Columns.Clear();if(Flag("noColumns"))return;
            var name=new BleeckerDataTableColumn(accessorKey:Flag("nested")?"profile.name":"name",header:"Fictional item",headerContentSelector:Heading,comparison:Flag("customCompare")?(a,b,id)=>((string)a.GetValue(id)!).Length-((string)b.GetValue(id)!).Length:null);
            var count=new BleeckerDataTableColumn(accessorKey:"count",header:"Count",headerContentSelector:Heading);
            var status=new BleeckerDataTableColumn(accessorKey:"status",header:"Status",headerContentSelector:Flag("blankHeader")?_=>null:null,cellContentSelector:Original);
            var definitions=Flag("groups")?new List<BleeckerDataTableColumn>{new(id:"description",header:"Description",columns:Flag("deepGroups")?[new(id:"identity",header:"Identity",columns:[name]),count]:[name,count]),status}:new List<BleeckerDataTableColumn>{name,count,status};
            if(Flag("boolean"))definitions.Add(new(accessorKey:"flag",header:"Flag",headerContentSelector:Heading));if(Flag("display"))definitions.Add(new(id:"details",header:"Consumer content",cellContentSelector:c=>"Original "+((Dictionary<string,object?>)c.Row.Original)["id"]));if(!Flag("noSelection"))definitions.Insert(0,new(id:"select",headerContentSelector:HeaderSelection,cellContentSelector:Selection));foreach(var column in definitions)Data.Columns.Add(column);
        }
        private void Sort(string id,bool additive=false){multi=additive;try{Invoke(headings[id]);}finally{multi=false;}}
        private void Page(bool next)=>Invoke(Elements(Data).OfType<Button>().Single(b=>b.Name==(next?"PART_Next":"PART_Previous")));
        private void Replace(string json){Items=JsonDocument.Parse(json).RootElement.EnumerateArray().Select(v=>ConvertValue(v)!).ToArray();Data.ItemsSource=Items;}
        private void PatchColumns(string key,bool value){Spec[key]=value;SetColumns();}
        internal void Transition(string action,string label)
        {
            switch(action)
            {
                case "paging":Page(label is "next-page" or "last-page");break;
                case "search-reset":if(label=="page-two")Page(true);else Search.Text=label=="filter-cedar"?"Cedar":label=="case-insensitive"?"cEdAr":label=="no-results"?"no fictional match":"";break;
                case "sort-cycle":Sort(label.StartsWith("name",StringComparison.Ordinal)?"name":"count");break;
                case "multi-sort":Sort(label=="name-first"?"name":"count",label!="name-first");break;
                case "selection-pages":if(label=="select-first")Toggle(checks["0"]);else if(label=="page-two")Page(true);else if(label=="select-second-page")Toggle(checks["10"]);else if(label is "filter-selected" or "filter-unselected" or "clear-filter")Search.Text=label=="filter-selected"?"Cedar":label=="filter-unselected"?"Ash":"";else if(label is "select-page" or "unselect-page")Toggle(all!);else Require(label=="leave-selected","Unknown selection transition.");break;
                case "replace-data":if(label=="select-index-zero")Toggle(checks["0"]);else if(label=="page-two")Page(true);else Replace(label=="replace-array"?"[{\"id\":\"replacement-a\",\"name\":\"Replacement A\",\"count\":1,\"status\":\"Ready\"},{\"id\":\"replacement-b\",\"name\":\"Replacement B\",\"count\":2,\"status\":\"Queued\"}]":"[]");break;
                case "visibility":Model.GetColumn(label.EndsWith("status",StringComparison.Ordinal)?"status":"name")!.ToggleVisibility(label.StartsWith("show",StringComparison.Ordinal));break;
                case "consumer-filters":if(label=="count-range")Model.GetColumn("count")!.SetFilterValue(new object[]{5,12});else if(label=="second-column-filter")Model.GetColumn("name")!.SetFilterValue("Cedar");else if(label=="clear-name")Model.GetColumn("name")!.SetFilterValue("");else Model.GetColumn("count")!.SetFilterValue(null);break;
                case "numeric-search":Search.Text=label=="numeric-string-25"?"25":label=="numeric-string-5"?"5":"";break;
                case "boolean-search":Search.Text=label=="boolean-string-true"?"true":"";break;
                case "missing-search":Search.Text=label=="missing-key-edit"?"Cedar":"";break;
                case "original-action":if(label=="original-cell")Invoke(actions[Model.PageRows[0].Original]);else if(label=="sort-desc-count")Sort("count");else Page(true);break;
                case "grouped-headers":if(label=="replace-with-flat-columns")PatchColumns("groups",false);else Model.GetColumn("count")!.ToggleVisibility(label=="restore-count");break;
                case "global-filter":Model.SetGlobalFilter(label=="global-cedar"?"Cedar":label=="global-three"?"3":"");break;
                case "custom-compare":Sort("name");break;
                case "empty-columns":if(label=="no-columns")PatchColumns("noColumns",true);else if(label=="no-columns-no-data")Replace("[]");else if(label=="columns-restored-empty")PatchColumns("noColumns",false);else{Items=DefaultRows();Data.ItemsSource=Items;}break;
                case "short-auto-sort":Sort(label=="short-count-first"?"count":"name");break;
                default:throw new InvalidOperationException("Unknown source transition "+action+"/"+label);
            }
        }
        private static string Text(object? value)=>value is BleeckerCheckbox?"":value is BleeckerButton button?button.Content?.ToString()??"":value?.ToString()??"";
        internal object Snapshot(UIElement root)
        {
            var native=Table;var header=native.Sections.OfType<BleeckerTableHeader>().Single();var body=native.Sections.OfType<BleeckerTableBody>().Single();var rows=body.Children.OfType<BleeckerTableRow>().ToArray();var heads=header.Children.OfType<BleeckerTableRow>().ToArray();
            Require(rows.Where(r=>r.Tag is BleeckerDataTableRow).Select(r=>((BleeckerDataTableRow)r.Tag).Id).SequenceEqual(Model.PageRows.Select(r=>r.Id)),"Rendered order differs from original row contexts.");
            foreach(var row in rows)
            {
                if(row.Tag is BleeckerDataTableRow context){Require(ReferenceEquals(context.Original,Items[context.Index]),"Rendered row lost original object.");Require(row.Cells.Count==context.GetVisibleCells().Count,"Rendered visible cells differ.");if(row.Cells.Count==0)Require(row.ActualHeight==0,"Empty source row must have zero height.");foreach(var cell in row.Cells){var c=(BleeckerDataTableCell)cell.Tag;Require(ReferenceEquals(c.Row,context),"Rendered cell lost its original row.");if(c.Column.Id=="status")Require(ReferenceEquals(cell.Content,actions[context.Original])&&((BleeckerButton)cell.Content).IsLoaded,"Original native action was replaced/unloaded: row="+context.Id+", reference="+ReferenceEquals(cell.Content,actions[context.Original])+", loaded="+((BleeckerButton)cell.Content).IsLoaded+", cellLoaded="+cell.IsLoaded+", parent="+VisualTreeHelper.GetParent((BleeckerButton)cell.Content)?.GetType().Name+", width="+((BleeckerButton)cell.Content).ActualWidth);else if(c.Column.Id=="select")Require(cell.Content is BleeckerCheckbox check&&check.IsChecked==context.IsSelected,"Native checkbox differs from original row.");else Require(Equals(cell.Content,c.RenderContent()),"Rendered producer value differs.");}}
                else Require(rows.Length==1&&row.Cells.Single().Content?.ToString()=="No results."&&row.ActualHeight==96&&row.Cells[0].ColumnSpan==Math.Max(1,Data.Columns.Count),"Original empty-result contract differs.");
                Require(!row.IsSelected,"Source does not set the primitive selected-fill prop.");
            }
            var groups=Model.GetHeaderGroups();Require(heads.SelectMany(r=>r.Cells).Select(c=>((BleeckerDataTableHeader)c.Tag).Id).SequenceEqual(groups.SelectMany(g=>g.Headers).Select(h=>h.Id)),"Rendered header group order differs.");
            var previous=Elements(Data).OfType<Button>().Single(b=>b.Name=="PART_Previous");var next=Elements(Data).OfType<Button>().Single(b=>b.Name=="PART_Next");var summary=Elements(Data).OfType<TextBlock>().Single(e=>e.Name=="PART_Summary");Require(previous.IsEnabled==Model.CanPreviousPage&&next.IsEnabled==Model.CanNextPage,"Native page limits differ.");
            object Value(BleeckerDataTableCell c){var v=c.GetValue();return new{column=c.Column.Id,value=ReferenceEquals(v,BleeckerDataTableModel.Undefined)?null:v,valueType=ReferenceEquals(v,BleeckerDataTableModel.Undefined)?"undefined":v is null?"null":v is string?"string":v is bool?"boolean":v is object[]?"array":v is Dictionary<string,object?>?"object":"number"};}
            object? ButtonPresentation(BleeckerTableCell c)
            {
                if(c.Content is not BleeckerButton button)return null;
                var action=Elements(button).OfType<Button>().Single();var presenter=Elements(button).OfType<ContentPresenter>().Single(p=>p.Name=="PART_Content");
                return new{wrapperSpacing=button.CharacterSpacing,actionSpacing=action.CharacterSpacing,presenterSpacing=presenter.CharacterSpacing,actionFontSize=action.FontSize,actionFontFamily=action.FontFamily.Source,actionFontWeight=action.FontWeight.Weight,buttonWidth=button.ActualWidth,text=Elements(button).OfType<TextBlock>().Where(t=>t.Visibility==Visibility.Visible&&t.IsLoaded&&t.ActualWidth>0).Select(t=>new{t.Text,t.CharacterSpacing,t.FontSize,fontFamily=t.FontFamily.Source,fontWeight=t.FontWeight.Weight,box=Box(t,root)}).ToArray()};
            }
            object Cell(BleeckerTableCell c)=>new{text=Text(c.Content),renderKind=c.Content is BleeckerCheckbox?"checkbox":c.Content is BleeckerButton?"button":"text",renderedText=string.Concat(Elements(c).OfType<TextBlock>().Where(t=>t.Visibility==Visibility.Visible&&t.IsLoaded&&t.ActualWidth>0).Select(t=>t.Text)),buttonPresentation=ButtonPresentation(c),box=Box(c,root),colSpan=c.ColumnSpan};
            object Brush(Brush brush){var s=(SolidColorBrush)brush;return new{r=s.Color.R,g=s.Color.G,b=s.Color.B,a=s.Color.A/255d*s.Opacity};}
            object CheckboxPaint(BleeckerCheckbox c){var elements=Elements(c).ToArray();var box=elements.OfType<Border>().Single(e=>e.Name=="PART_Box");var glyph=elements.OfType<Viewbox>().Single(e=>e.Name=="PART_Glyph");var chrome=elements.OfType<Grid>().Single(e=>e.Name=="PART_InputChrome");return new{glyphOpacity=glyph.Opacity,chromeOpacity=chrome.Opacity,fill=Brush(box.Background),stroke=Brush(box.BorderBrush),targetFill=Brush(elements.OfType<TextBlock>().Single(e=>e.Name==(c.IsChecked==true?"PART_OnFill":"PART_OffFill")).Foreground),targetStroke=Brush(elements.OfType<TextBlock>().Single(e=>e.Name==(c.IsChecked==true?"PART_OnStroke":"PART_OffStroke")).Foreground)};}
            var surface=Elements(Data).OfType<Border>().Single(e=>e.Name=="PART_Surface");var shadowHosts=Elements(Data).OfType<Grid>().Where(e=>e.Name is "PART_ShadowNear" or "PART_ShadowFar").ToArray();
            var paint=new{surface=new{box=Box(surface,root),fill=Brush(surface.Background),stroke=Brush(surface.BorderBrush),border=new[]{surface.BorderThickness.Left,surface.BorderThickness.Top,surface.BorderThickness.Right,surface.BorderThickness.Bottom},radii=new[]{surface.CornerRadius.TopLeft,surface.CornerRadius.TopRight,surface.CornerRadius.BottomRight,surface.CornerRadius.BottomLeft}},headerFill=Brush(header.Background),shadowLayers=shadowHosts.Select(h=>{var v=ElementCompositionPreview.GetElementChildVisual(h);var mask=(v as Microsoft.UI.Composition.SpriteVisual)?.Brush as Microsoft.UI.Composition.CompositionMaskBrush;var color=(mask?.Source as Microsoft.UI.Composition.CompositionColorBrush)?.Color;return new{name=h.Name,attached=v is not null,opacity=v?.Opacity,width=v?.Size.X,height=v?.Size.Y,x=v?.Offset.X,y=v?.Offset.Y,masked=mask?.Mask is not null,color=color.HasValue?new[]{(int)color.Value.R,color.Value.G,color.Value.B,(int)color.Value.A}:null};}).ToArray()};
            return new{paint,fixture=Box(Data,root),table=Box(native,root),scrollClientWidth=native.ActualWidth,scrollWidth=native.ScrollableWidth+native.ActualWidth,state=new{sorting=Model.Sorting.Select(s=>new{id=s.Id,desc=s.Descending}).ToArray(),columnFilters=Model.ColumnFilters.Select(f=>new{id=f.Id,value=f.Value}).ToArray(),rowSelection=Model.SelectedRowIds.ToDictionary(id=>id,_=>true),pagination=new{pageIndex=Model.PageIndex,pageSize=Model.PageSize},columnVisibility=Model.ColumnVisibility,globalFilter=Model.GlobalFilter},pageCount=Model.PageCount,canPrevious=previous.IsEnabled,canNext=next.IsEnabled,filteredRows=Model.FilteredRows.Select(r=>r.Id).ToArray(),filteredSelectedRows=Model.FilteredSelectedRows.Select(r=>r.Id).ToArray(),sortedRows=Model.SortedRows.Select(r=>r.Id).ToArray(),visibleRows=rows.Where(r=>r.Tag is BleeckerDataTableRow).Select(r=>{var c=(BleeckerDataTableRow)r.Tag;return new{id=c.Id,originalId=((Dictionary<string,object?>)c.Original)["id"],selected=c.IsSelected,values=r.Cells.Select(cell=>Value((BleeckerDataTableCell)cell.Tag)).ToArray()};}).ToArray(),headerGroups=groups.Select(g=>new{id=g.Id,headers=g.Headers.Select(h=>new{id=h.Id,column=h.Column.Id,placeholder=h.IsPlaceholder,colSpan=h.ColumnSpan,rowSpan=h.RowSpan}).ToArray()}).ToArray(),header=heads.SelectMany(r=>r.Cells).Select(Cell).ToArray(),body=rows.SelectMany(r=>r.Cells).Select(Cell).ToArray(),rows=rows.Select(r=>new{state=r.Tag is BleeckerDataTableRow nativeRow?nativeRow.IsSelected?"true":"false":null,selected=(string?)null,primitiveSelected=r.IsSelected,box=Box(r,root)}).ToArray(),search=Data.SearchKey is null?Array.Empty<object>():new[]{new{value=Search.Text,placeholder=Search.PlaceholderText,box=Box(Search,root)}},checkboxes=heads.Concat(rows).SelectMany(r=>r.Cells).Select(c=>c.Content).OfType<BleeckerCheckbox>().Select(c=>new{name=AutomationProperties.GetName(c),@checked=c.IsChecked==true,indeterminate=!c.IsChecked.HasValue,paint=CheckboxPaint(c),box=Box(c,root)}).ToArray(),originalActions=rows.SelectMany(r=>r.Cells).Where(c=>c.Tag is BleeckerDataTableCell context&&context.Column.Id=="status").Select(c=>{var context=(BleeckerDataTableCell)c.Tag;return new{row=((Dictionary<string,object?>)context.Row.Original)["id"],disabled=!((BleeckerButton)c.Content).IsEnabled,text=Text(c.Content)};}).ToArray(),footer=new{text=summary.Text,buttons=new[]{new{disabled=!previous.IsEnabled,box=Box(previous,root)},new{disabled=!next.IsEnabled,box=Box(next,root)}}},events=events.ToArray(),originalNativeContentVerified=true};
        }
    }
}
