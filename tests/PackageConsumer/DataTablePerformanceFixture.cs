using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

// Shared unchanged by the catalog and isolated NuGet consumer. This measures
// paginated Data Table, separately from the existing Simple Data Table workload.
internal static class DataTablePerformanceFixture
{
    private delegate bool Visitor(nint handle,nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor,nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static string Hash(string path)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
    private static string Digest(IEnumerable<string> values)=>Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join('\n',values))));
    private static void Invoke(FrameworkElement element){var button=element as Button??Elements(element).OfType<Button>().Single();var peer=FrameworkElementAutomationPeer.CreatePeerForElement(button);Require(peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider,"Benchmark button has no Invoke provider.");((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();}
    private static void Toggle(BleeckerCheckbox check)=>((IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(check).GetPattern(PatternInterface.Toggle)).Toggle();
    private sealed class Item(int index,int count):INotifyPropertyChanged
    {
        private PropertyChangedEventHandler? changed;
        public int Index{get;}=index;
        public string Id{get;}="fictional-"+index.ToString("D5",CultureInfo.InvariantCulture);
        public string Name{get;private set;}="Group "+index%8+" item "+index.ToString("D5",CultureInfo.InvariantCulture);
        public int Count{get;}=count-index;
        public int Subscribers=>changed?.GetInvocationList().Length??0;
        public event PropertyChangedEventHandler? PropertyChanged{add=>changed+=value;remove=>changed-=value;}
        internal void Change(string suffix){Name="Group "+Index%8+" item "+Index.ToString("D5",CultureInfo.InvariantCulture)+" "+suffix;changed?.Invoke(this,new(nameof(Name)));}
    }
    private sealed class Collection:ObservableCollection<Item>,INotifyCollectionChanged
    {
        public int Subscribers{get;private set;}
        internal Collection(IEnumerable<Item> items){foreach(var item in items)Add(item);}
        event NotifyCollectionChangedEventHandler? INotifyCollectionChanged.CollectionChanged{add{Subscribers++;CollectionChanged+=value;}remove{Subscribers--;CollectionChanged-=value;}}
    }
    private sealed class Events{internal int Unloads;}
    internal static async Task RunAsync(Window window,string output,string hostAssembly,bool pagingOnly=false)
    {
        var root=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='1000' Height='1000' Background='{ThemeResource BleeckerBackgroundBrush}'/>");
        root.Clip=new RectangleGeometry{Rect=new Rect(0,0,1000,1000)};root.RequestedTheme=ElementTheme.Light;window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1000,1000));
        var measurements=new List<object>();var snapshots=new List<object>();var containerDiagnostics=new List<object>();var contracts=new List<object>();var loadingDiagnostics=new List<object>();var pngs=new List<object>();var guards=0;var focus=0;var activations=0;var stage="initial";
        root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,e)=>{if(e.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Data Table performance probe left its hidden boundary.");}
        async Task Layout(){for(var i=0;i<3;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Dispatcher refused benchmark layout.");await idle.Task;root.Measure(new Size(1000,1000));root.Arrange(new Rect(0,0,1000,1000));root.UpdateLayout();Guard();}}
        async Task Capture(string file){Guard();await Task.Delay(TimeSpan.FromMilliseconds(BleeckerTokens.Durations["control"]+32));var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),bytes);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});Guard();}
        var containerContracts = new List<object>();
        async Task VerifyContainerContracts()
        {
            foreach (var mode in new[] { "default", "cell-template", "row-style", "row-context", "cell-context" })
            {
                var originals = new[] { new { Name = "One" }, new { Name = "Two" }, new { Name = "Three" }, new { Name = "Four" } };
                var editors = originals.ToDictionary(item => (object)item, item => new TextBox { Text = item.Name, Tag = item, Width = 120 }, ReferenceEqualityComparer.Instance);
                var data = new BleeckerDataTable { Width = 660, VerticalAlignment = VerticalAlignment.Top };
                if (mode == "cell-template")
                {
                    var template = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBox Text='{Binding Name, Mode=OneWay}' Width='120'/></DataTemplate>");
                    var style = new Style(typeof(BleeckerTableCell)); style.Setters.Add(new Setter(ContentControl.ContentTemplateProperty, template));
                    data.CellStyleSelector = _ => style;
                }
                if (mode == "row-style")
                {
                    var style = new Style(typeof(BleeckerTableRow)); style.Setters.Add(new Setter(Control.FontSizeProperty, 18d)); root.Resources[typeof(BleeckerTableRow)] = style;
                }
                data.Columns.Add(new(id: "value", header: "Original content", cellContentSelector: context => mode == "cell-template" ? context.Row.Original : editors[context.Row.Original]));
                data.Model.SetPageSize(2); data.ItemsSource = originals; root.Children.Add(data); await Layout();
                BleeckerTableRow[] Current() => Elements(data).OfType<BleeckerTableRow>().Where(row => row.Tag is BleeckerDataTableRow).ToArray();
                var before = Current(); Require(before.Length == 2, "Container regression lost initial rows.");
                if (mode == "row-context") foreach (var row in before) row.DataContext = ((BleeckerDataTableRow)row.Tag).Original;
                if (mode == "cell-context") foreach (var row in before) row.Cells[0].DataContext = ((BleeckerDataTableRow)row.Tag).Original;
                var originalEditor = mode == "cell-template" ? Elements(before[0].Cells[0]).OfType<TextBox>().Single() : editors[originals[0]];
                originalEditor.Text = "Original caller draft"; await Layout();
                data.Model.NextPage(); await Layout(); var after = Current();
                Require(after.Length == 2 && ReferenceEquals(((BleeckerDataTableRow)after[0].Tag).Original, originals[2]), "Reused container lost the current original data.");
                var reused = after.Count(row => before.Any(old => ReferenceEquals(old, row)));
                Require(mode == "default" || reused == 0, "A caller-controlled presentation was recycled.");
                Require(!originalEditor.IsLoaded, "Retired caller content remained mounted in a reused container.");
                if (mode == "cell-template")
                {
                    var next = Elements(after[0].Cells[0]).OfType<TextBox>().Single();
                    Require(!ReferenceEquals(next, originalEditor) && ReferenceEquals(next.DataContext, originals[2]) && next.Text == originals[2].Name, "Caller template retained another row's renderer or state.");
                }
                else Require(ReferenceEquals(after[0].Cells[0].Content, editors[originals[2]]) && ReferenceEquals(editors[originals[2]].Tag, originals[2]), "Caller control or original Tag was replaced.");
                data.Model.SetPageIndex(0); await Layout(); var restored = Current();
                if (mode == "cell-template")
                {
                    var next = Elements(restored[0].Cells[0]).OfType<TextBox>().Single();
                    Require(!ReferenceEquals(next, originalEditor) && ReferenceEquals(next.DataContext, originals[0]) && next.Text == originals[0].Name, "Caller template lifecycle changed when returning to a page.");
                }
                else Require(ReferenceEquals(restored[0].Cells[0].Content, originalEditor) && originalEditor.Text == "Original caller draft" && ReferenceEquals(originalEditor.Tag, originals[0]), "Returning to a page lost its original caller editor or local state.");
                containerContracts.Add(new { mode, passed = true, reusedRowContainers = reused, originalDataRetained = true, retiredContentUnloaded = true, returnedContentCorrect = true });
                root.Children.Clear(); await Layout(); if (mode == "row-style") root.Resources.Remove(typeof(BleeckerTableRow));
            }
        }
        try
        {
            await VerifyContainerContracts();
            foreach(var timing in new[]{"none","before-attach","after-layout","unloaded-after-layout"})
            {
                var loaded=0;var unloaded=0;var row=new BleeckerTableRow();row.Cells.Add(new(){Content="Original loading probe"});var body=new BleeckerTableBody();body.Children.Add(row);var table=new BleeckerTable{Width=660,VerticalAlignment=VerticalAlignment.Top};table.Sections.Add(body);
                void Loaded(object sender,RoutedEventArgs args)=>loaded++;
                if(timing=="before-attach")row.Loaded+=Loaded;
                root.Children.Add(table);await Layout();var initial=row.IsLoaded;if(timing=="after-layout")row.Loaded+=Loaded;if(timing=="unloaded-after-layout")row.Unloaded+=(_,_)=>unloaded++;await Task.Delay(64);await Layout();var after=row.IsLoaded;var cell=row.Cells[0];var parent=VisualTreeHelper.GetParent(row) as FrameworkElement;var peer=(IGridProvider)FrameworkElementAutomationPeer.CreatePeerForElement(table).GetPattern(PatternInterface.Grid);
                loadingDiagnostics.Add(new{timing,initial,afterDelay=after,loadedEvents=loaded,rowHasXamlRoot=row.XamlRoot is not null,parentLoaded=parent?.IsLoaded,cellLoaded=cell.IsLoaded,tableLoaded=table.IsLoaded,rootLoaded=root.IsLoaded,gridRows=peer.RowCount,gridColumns=peer.ColumnCount});row.Unloaded+=(_,_)=>unloaded++;root.Children.Clear();await Layout();Require(!row.IsLoaded,"Loading probe row stayed loaded after removal.");
            }
            foreach(var profile in pagingOnly ? new (int Count,int PageSize)[]{(16,10),(16384,100)} : new (int Count,int PageSize)[]{(16,10),(128,10),(128,100),(2048,10),(2048,100),(16384,10),(16384,100)})
            {
                var count=profile.Count;var size=profile.PageSize;var warmup=count==16;var known=Enumerable.Range(0,count).Select(i=>new Item(i,count)).ToArray();var items=new Collection(known);var collections=new List<Collection>{items};var actions=new Dictionary<Item,BleeckerButton>(ReferenceEqualityComparer.Instance);var checks=new Dictionary<string,BleeckerCheckbox>(StringComparer.Ordinal);var rowEvents=new ConditionalWeakTable<BleeckerTableRow,Events>();
                Events EventsFor(BleeckerTableRow row)=>rowEvents.TryGetValue(row,out var events)?events:throw new InvalidOperationException("Native row was not tracked before its next operation.");var invoked=new List<string>();var data=new BleeckerDataTable{Width=660,Margin=new Thickness(20),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,SearchKey="Name"};var model=data.Model;
                var producerCalls=0;var sortInvocations=0;var selectionInvocations=0;var modelChanges=0;var expectedPage=0;bool? descending=null;bool range=false,nameFilter=false,global=false;var selected=new HashSet<string>(StringComparer.Ordinal);Item? removed=null;
                var all=new BleeckerCheckbox{Width=20,Height=20,MinWidth=0,MinHeight=0};AutomationProperties.SetName(all,"Select page");void PageSelection(object sender,RoutedEventArgs args){if(all.IsChecked!=model.AllPageRowsSelected){selectionInvocations++;model.ToggleAllPageRowsSelected(all.IsChecked==true);}}all.Checked+=PageSelection;all.Unchecked+=PageSelection;
                var sort=new BleeckerButton{Variant=BleeckerButtonVariant.Ghost,Content="Count"};sort.Click+=(_,_)=>{sortInvocations++;model.GetColumn("Count")!.ToggleSorting();};
                data.Columns.Add(new(id:"select",headerContentSelector:_=>{all.IsChecked=model.AllPageRowsSelected;return all;},cellContentSelector:c=>{if(!checks.TryGetValue(c.Row.Id,out var check)){var id=c.Row.Id;check=new BleeckerCheckbox{Width=20,Height=20,MinWidth=0,MinHeight=0};AutomationProperties.SetName(check,"Select row "+id);void Change(object sender,RoutedEventArgs args){if(check.IsChecked!=model.IsRowSelected(id))model.CoreRows[int.Parse(id,CultureInfo.InvariantCulture)].ToggleSelected(check.IsChecked==true);}check.Checked+=Change;check.Unchecked+=Change;checks.Add(id,check);}check.IsChecked=c.Row.IsSelected;return check;}));
                data.Columns.Add(new(accessorKey:"Name",header:"Fictional item"));data.Columns.Add(new(accessorKey:"Count",headerContentSelector:_=>sort));
                data.Columns.Add(new(id:"action",header:"Original action",cellContentSelector:c=>{producerCalls++;var item=(Item)c.Row.Original;if(!actions.TryGetValue(item,out var action)){action=new BleeckerButton{Variant=BleeckerButtonVariant.Ghost,Content="Open "+item.Index,Tag=item};action.Click+=(_,_)=>invoked.Add(((Item)action.Tag).Id);actions.Add(item,action);}return action;}));
                model.SetPageSize(size);model.Changed+=(_,_)=>modelChanges++;root.Children.Add(data);await Layout();Guard();
                BleeckerTableRow[] Rows()=>Elements(data).OfType<BleeckerTableBody>().Single().Children.OfType<BleeckerTableRow>().Where(r=>r.Tag is BleeckerDataTableRow).ToArray();
                BleeckerInput Search()=>Elements(data).OfType<BleeckerInput>().Single(e=>e.Name=="PART_Search");
                Item[] Filtered()=>items.Where(i=>(!range||i.Count>=count/4&&i.Count<=count/2)&&(!nameFilter&&!global||i.Name.Contains("Group 3",StringComparison.OrdinalIgnoreCase))).ToArray();
                Item[] Sorted(){var filtered=Filtered();return descending is null?filtered:descending==true?filtered.OrderByDescending(i=>i.Count).ToArray():filtered.OrderBy(i=>i.Count).ToArray();}
                Dictionary<string,WeakReference<BleeckerTableRow>> NativeRows()=>Rows().ToDictionary(r=>((BleeckerDataTableRow)r.Tag).Id,r=>new WeakReference<BleeckerTableRow>(r),StringComparer.Ordinal);
                BleeckerTableRow BeforeRow(WeakReference<BleeckerTableRow> reference)=>reference.TryGetTarget(out var row)?row:throw new InvalidOperationException("Current native row was lost before an operation.");
                object Verify(string operation,Dictionary<string,WeakReference<BleeckerTableRow>> before,Dictionary<string,WeakReference<BleeckerTableCell>[]> beforeCells,Dictionary<string,int> unloadedBefore)
                {
                    var indices=new Dictionary<Item,int>(ReferenceEqualityComparer.Instance);for(var i=0;i<items.Count;i++)indices.Add(items[i],i);var filtered=Filtered();var sorted=Sorted();var page=sorted.Skip(expectedPage*size).Take(size).ToArray();var rows=Rows();Require(model.PageIndex==expectedPage&&model.PageSize==size&&model.FilteredRows.Select(r=>r.Original).SequenceEqual(filtered)&&model.SortedRows.Select(r=>r.Original).SequenceEqual(sorted),"Benchmark model order/filter/page differs from independent expectations.");Require(rows.Length==page.Length&&rows.Length<=size,"Native rows escaped the configured page size.");var retained=0;var retainedUnloads=0;
                    for(var i=0;i<rows.Length;i++){var row=rows[i];var context=(BleeckerDataTableRow)row.Tag;var item=page[i];Require(ReferenceEquals(context.Original,item)&&context.Index==indices[item]&&context.Id==indices[item].ToString(CultureInfo.InvariantCulture),"Visible row lost original/index.");Require(row.Cells.Count==4&&row.Cells[1].Content as string==item.Name&&row.Cells[2].Content as string==item.Count.ToString(CultureInfo.InvariantCulture),"Rendered producer notification/text differs.");Require(Elements(row.Cells[1]).OfType<TextBlock>().Single(t=>t.Name=="PART_Text").Text==item.Name,"Actual displayed name differs.");Require(ReferenceEquals(row.Cells[3].Content,actions[item])&&ReferenceEquals(actions[item].Tag,item)&&actions[item].IsLoaded,"Original action was replaced or unloaded.");Require(((BleeckerCheckbox)row.Cells[0].Content).IsChecked==selected.Contains(context.Id)&&!row.IsSelected,"Native selection differs.");if(before.TryGetValue(context.Id,out var previous)){Require(previous.TryGetTarget(out var previousRow)&&ReferenceEquals(row,previousRow)&&row.Cells.Count==beforeCells[context.Id].Length&&row.Cells.Select((cell,index)=>beforeCells[context.Id][index].TryGetTarget(out var previousCell)&&ReferenceEquals(cell,previousCell)).All(same=>same),"Retained native row/cells were replaced.");retained++;retainedUnloads+=EventsFor(row).Unloads-unloadedBefore[context.Id];}if(!rowEvents.TryGetValue(row,out _)){var events=new Events();rowEvents.Add(row,events);row.Unloaded+=(_,_)=>events.Unloads++;}}
                    Require(retainedUnloads==0,"A retained native row unloaded during reconciliation.");Require(model.SelectedRowIds.SetEquals(selected)&&model.FilteredSelectedRowCount==filtered.Count(i=>selected.Contains(indices[i].ToString(CultureInfo.InvariantCulture))),"Filtered/total selection differs.");var current=new HashSet<Item>(items,ReferenceEqualityComparer.Instance);var wrongItems=known.Count(i=>i.Subscribers!=(current.Contains(i)?1:0));var wrongCollections=collections.Count(c=>c.Subscribers!=(ReferenceEquals(c,items)?1:0));Require(wrongItems==0&&wrongCollections==0,"Producer subscriptions escaped the current source.");
                    return new{operation,sourceCount=items.Count,coreRows=model.CoreRows.Count,filteredRows=model.FilteredRowCount,filteredSelectedRows=model.FilteredSelectedRowCount,pageIndex=model.PageIndex,pageSize=model.PageSize,pageCount=model.PageCount,selectedCount=model.SelectedRowIds.Count,selectedIdsSha256=Digest(model.SelectedRowIds.Order(StringComparer.Ordinal)),sourceOriginalIdsSha256=Digest(items.Select(i=>i.Id)),filteredOriginalIdsSha256=Digest(model.FilteredRows.Select(r=>((Item)r.Original).Id)),sortedOriginalIdsSha256=Digest(model.SortedRows.Select(r=>((Item)r.Original).Id)),rows=rows.Select(r=>{var c=(BleeckerDataTableRow)r.Tag;return new{rowId=c.Id,originalId=((Item)c.Original).Id,name=r.Cells[1].Content as string,count=r.Cells[2].Content as string,selected=((BleeckerCheckbox)r.Cells[0].Content).IsChecked,originalActionRetained=ReferenceEquals(r.Cells[3].Content,actions[(Item)c.Original]),loaded=r.IsLoaded,width=r.ActualWidth,height=r.ActualHeight};}).ToArray(),retainedNativeRows=retained,retainedNativeRowUnloads=retainedUnloads,itemSubscriptions=known.Sum(i=>i.Subscribers),collectionSubscriptions=collections.Sum(c=>c.Subscribers),wrongItemSubscriptions=wrongItems,wrongCollectionSubscriptions=wrongCollections,sortInvocations,selectionInvocations};
                }
                void ObserveContainers(string operation,Dictionary<string,WeakReference<BleeckerTableRow>> before)
                {
                    var previous = new HashSet<BleeckerTableRow>(before.Values.Select(reference => reference.TryGetTarget(out var row) ? row : null).OfType<BleeckerTableRow>(),ReferenceEqualityComparer.Instance);
                    var current = Rows(); var cells = current.SelectMany(row => row.Cells).ToArray();
                    containerDiagnostics.Add(new { rows=count,pageSize=size,operation,reusedRowContainers=current.Count(previous.Contains),visibleRowContainers=current.Length,
                        rowsWithoutCallerStyle=current.Count(row => row.Style is null),rowsWithoutLocalTemplate=current.Count(row => row.ReadLocalValue(Control.TemplateProperty)==DependencyProperty.UnsetValue),
                        cellsWithoutDataTemplates=cells.Count(cell => cell.ContentTemplate is null && cell.ContentTemplateSelector is null),cellsWithoutLocalTemplate=cells.Count(cell => cell.ReadLocalValue(Control.TemplateProperty)==DependencyProperty.UnsetValue) });
                }
                async Task Measure(string operation,Action mutation)
                {
                    stage=$"{count}-{size}-{operation}";Guard();var before=NativeRows();var beforeCells=before.ToDictionary(p=>p.Key,p=>BeforeRow(p.Value).Cells.Select(cell=>new WeakReference<BleeckerTableCell>(cell)).ToArray(),StringComparer.Ordinal);var unloaded=before.ToDictionary(p=>p.Key,p=>EventsFor(BeforeRow(p.Value)).Unloads,StringComparer.Ordinal);var changes=modelChanges;var calls=producerCalls;var process=Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;var memory=process.PrivateMemorySize64;var allocations=GC.GetTotalAllocatedBytes(true);var collectionsBefore=Enumerable.Range(0,3).Select(GC.CollectionCount).ToArray();var pause=GC.GetTotalPauseDuration();var timer=Stopwatch.StartNew();mutation();var mutationMs=timer.Elapsed.TotalMilliseconds;await Layout();timer.Stop();process.Refresh();var measurement=new{rows=count,pageSize=size,operation,mutationMilliseconds=mutationMs,threePassMilliseconds=timer.Elapsed.TotalMilliseconds,cpuMilliseconds=(process.TotalProcessorTime-cpu).TotalMilliseconds,managedAllocatedBytes=GC.GetTotalAllocatedBytes(true)-allocations,privateMemoryDeltaBytes=process.PrivateMemorySize64-memory,gcCollections=Enumerable.Range(0,3).Select(i=>GC.CollectionCount(i)-collectionsBefore[i]).ToArray(),gcPauseMilliseconds=(GC.GetTotalPauseDuration()-pause).TotalMilliseconds,producerCalls=producerCalls-calls,modelNotifications=modelChanges-changes};Guard();var snapshot=Verify(operation,before,beforeCells,unloaded);ObserveContainers(operation,before);if(!warmup){measurements.Add(measurement);snapshots.Add(new{rows=count,pageSize=size,operation,snapshot});}await File.WriteAllTextAsync(output+".progress.json",JsonSerializer.Serialize(new{stage,completedMeasurements=measurements.Count,measurement}));
                }
                await Measure("bind-data",()=>data.ItemsSource=items);if(!warmup&&size==10)await Capture($"data-table-performance-{count}-10-bind-data.png");
                await Measure("sort-descending",()=>{descending=true;expectedPage=0;Invoke(sort);});await Measure("sort-ascending",()=>{descending=false;expectedPage=0;Invoke(sort);});
                await Measure("next-page",()=>{expectedPage=1;Invoke(Elements(data).OfType<Button>().Single(b=>b.Name=="PART_Next"));});
                foreach(var i in Sorted().Skip(expectedPage*size).Take(size))selected.Add(items.IndexOf(i).ToString(CultureInfo.InvariantCulture));await Measure("select-page",()=>Toggle(all));selected.Clear();await Measure("deselect-page",()=>Toggle(all));
                selected.UnionWith(Enumerable.Range(0,items.Count).Select(i=>i.ToString(CultureInfo.InvariantCulture)));await Measure("select-all",()=>model.ToggleAllRowsSelected(true));selected.Clear();await Measure("deselect-all",()=>model.ToggleAllRowsSelected(false));
                var visible=new HashSet<Item>(Sorted().Skip(expectedPage*size).Take(size),ReferenceEqualityComparer.Instance);var hidden=items.First(i=>!visible.Contains(i));await Measure("notify-hidden",()=>{hidden.Change("hidden-update");expectedPage=0;});var first=Sorted()[0];await Measure("notify-visible",()=>{first.Change("visible-update");expectedPage=0;});
                await Measure("range-filter",()=>{range=true;expectedPage=0;model.GetColumn("Count")!.SetFilterValue(new object[]{count/4,count/2});});await Measure("clear-range",()=>{range=false;expectedPage=0;model.ResetColumnFilters();});
                await Measure("search-group",()=>{nameFilter=true;expectedPage=0;Search().Text="Group 3";});await Measure("clear-search",()=>{nameFilter=false;expectedPage=0;Search().Text="";});await Measure("global-group",()=>{global=true;expectedPage=0;model.SetGlobalFilter("Group 3");});await Measure("clear-global",()=>{global=false;expectedPage=0;model.SetGlobalFilter("");});
                // Allocate caller replacement data outside the operation timer.
                var replacement=new Collection(items.Reverse());collections.Add(replacement);await Measure("reverse-source",()=>{items=replacement;expectedPage=0;data.ItemsSource=items;});await Measure("clear-sort",()=>{descending=null;expectedPage=0;model.SetSorting([]);});
                removed=items[0];await Measure("remove-first",()=>{expectedPage=0;items.RemoveAt(0);});await Measure("append-original",()=>{expectedPage=0;items.Add(removed);});
                var action=(BleeckerButton)Rows()[0].Cells[3].Content;Invoke(action);Require(invoked.SequenceEqual(new[]{((Item)action.Tag).Id}),"Original action did not receive its current item.");if(!warmup&&size==10)await Capture($"data-table-performance-{count}-10-append-original.png");
                root.Children.Clear();await Layout();Require(!data.IsLoaded&&known.All(i=>i.Subscribers==0)&&collections.All(c=>c.Subscribers==0),"Unloaded benchmark retained producer subscriptions.");var beforeCalls=producerCalls;known[0].Change("unloaded-update");Require(producerCalls==beforeCalls,"Unloaded item update reached the table.");if(!warmup)contracts.Add(new{rows=count,pageSize=size,unloadedItemSubscriptions=known.Sum(i=>i.Subscribers),unloadedCollectionSubscriptions=collections.Sum(c=>c.Subscribers),unloadedNotificationsIgnored=true,originalActionId=invoked.Single()});
            }
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-data-table-performance-native-v3",performanceScope=pagingOnly?"paging-16384-100":"full",passed=true,measurements,snapshots,containerDiagnostics,containerContracts,contracts,loadingDiagnostics,pngs,guards,focus,activations,activateCalled=false,showWindowCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false,publicApisOnly=true,forcedGc=false,timingIncludesThreeDispatcherIdles=true,timingIncludesMotionSettling=false,unloadObservationOutsideTimings=true,retiredRowsTrackedWeakly=true,operationIdentityReferencesWeak=true,lateLoadedHandlersAvoided=true,captureAndVerificationExcludedFromTimings=true,controlConstructionExcludedFromTimings=true,replacementDataPreparationExcludedFromTimings=true,capturesPageSize10Only=true,warmupRows=16,processorCount=Environment.ProcessorCount,runtime=Environment.Version.ToString(),os=RuntimeInformation.OSDescription,textScaleFactor=new Windows.UI.ViewManagement.UISettings().TextScaleFactor,librarySha256=Hash(typeof(BleeckerDataTable).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-data-table-performance-native-v3",passed=false,stage,error=error.ToString(),measurements,snapshots,contracts,pngs,guards,focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
}
