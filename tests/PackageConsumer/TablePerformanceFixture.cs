using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Text;
namespace Orchard.Testing;
internal static class TablePerformanceFixture
{
 private delegate bool Visitor(nint handle,nint value);
 [DllImport("user32.dll")]private static extern bool EnumWindows(Visitor visitor,nint value);
 [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
 [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
 [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(nint handle);
 private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var c in Elements(VisualTreeHelper.GetChild(node,i)))yield return c;}
 internal static async Task RunAsync(Window window,string output,string hostAssembly)
 {
  var root=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='1000' Height='760' Background='{ThemeResource BleeckerBackgroundBrush}'/>");window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1000,760));var guards=0;var focus=0;var activations=0;var stage="initial";var snapshots=new List<object>();var measurements=new List<object>();var pngs=new List<object>();
  root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
  void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
  void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Table performance probe left its hidden boundary.");}
  async Task Layout(){for(var i=0;i<3;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Dispatcher refused layout.");await idle.Task;root.Measure(new Size(1000,760));root.Arrange(new Rect(0,0,1000,760));root.UpdateLayout();}}
  async Task Capture(string file){Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),bytes);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});Guard();}
  try{
   root.RequestedTheme=ElementTheme.Light;
   foreach(var count in new[]{16,32,128,512}){
    var warmup=count==16;var trackedRowLoads=0;var trackedRowUnloads=0;var producerCalls=0;var sortRequests=0;var selectionRequests=0;var data=new BleeckerSimpleDataTable{Width=660,Margin=new Thickness(20),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,IsSelectable=true};
    data.Columns.Add(new("Name","Fictional item"));data.Columns.Add(new("Count","Count",alignment:BleeckerTableCellAlignment.Right));data.Columns.Add(new("Status","Status",isSortable:false,cellContentSelector:(r,_)=>{producerCalls++;return ((BenchRow)r).StatusView;}));data.GetRowKey=(r,_)=>((BenchRow)r).Id;data.SortRequested+=(_,_)=>sortRequests++;data.SelectionRequested+=(_,_)=>selectionRequests++;
    root.Children.Add(data);await Layout();Guard();var items=new ObservableCollection<BenchRow>(Enumerable.Range(0,count).Select(i=>new BenchRow(i,count)));var originalItems=items.ToArray();Dictionary<string,BleeckerTableRow>? originalRows=null;Dictionary<string,BleeckerTableCell[]>? originalCells=null;
    BleeckerTableBody Body()=>Elements(data).OfType<BleeckerTableBody>().Single();
    BleeckerTableRow[] Rows()=>Body().Children.OfType<BleeckerTableRow>().ToArray();
    object Verify(string operation){var rows=Rows();var expected=data.EffectiveSort.Field.Length==0?items.ToArray():data.EffectiveSort.Order==BleeckerTableSortOrder.Ascending?items.OrderBy(r=>r.Count).ToArray():items.OrderByDescending(r=>r.Count).ToArray();Require(rows.Length==expected.Length,"Rendered row count changed.");Require(rows.Select(r=>(BenchRow)r.Tag).SequenceEqual(expected),"Rendered row order differs.");Require(rows.All(r=>ReferenceEquals(r.Cells[3].Content,((BenchRow)r.Tag).StatusView)),"Original cell producer content replaced.");Require(rows.All(r=>(r.Cells[1].Content as string)==((BenchRow)r.Tag).Name),"Item notification did not reach the actual cell.");var retained=rows.Where(r=>originalRows?.ContainsKey(((BenchRow)r.Tag).Id)==true).All(r=>{var id=((BenchRow)r.Tag).Id;return ReferenceEquals(r,originalRows![id])&&r.Cells.SequenceEqual(originalCells![id]);});Require(retained,"Reconciliation replaced retained rows/cells.");Require(rows.All(r=>r.IsSelected==data.EffectiveSelectedKeys.Contains(((BenchRow)r.Tag).Id)),"Selection differs from actual rows.");return new{operation,rowCount=rows.Length,rowOrder=expected.Select(r=>r.Id).ToArray(),selectedKeys=data.EffectiveSelectedKeys.ToArray(),retainedRowsAndCells=retained,originalStatusControlsRetained=true,originalItemsRetained=rows.All(r=>originalItems.Any(i=>ReferenceEquals(i,r.Tag))),sortRequests,selectionRequests,producerCalls};}
    async Task Measure(string operation,Action mutation){stage=$"{count}-{operation}";Guard();var process=Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;var memory=process.PrivateMemorySize64;var allocations=GC.GetTotalAllocatedBytes(true);var collections=Enumerable.Range(0,3).Select(GC.CollectionCount).ToArray();var pause=GC.GetTotalPauseDuration();var calls=producerCalls;var loads=trackedRowLoads;var unloads=trackedRowUnloads;var timer=Stopwatch.StartNew();mutation();var mutationMs=timer.Elapsed.TotalMilliseconds;await Layout();timer.Stop();process.Refresh();var measurement=new{rows=count,operation,mutationMilliseconds=mutationMs,settledMilliseconds=timer.Elapsed.TotalMilliseconds,cpuMilliseconds=(process.TotalProcessorTime-cpu).TotalMilliseconds,managedAllocatedBytes=GC.GetTotalAllocatedBytes(true)-allocations,privateMemoryDeltaBytes=process.PrivateMemorySize64-memory,gcCollections=Enumerable.Range(0,3).Select(i=>GC.CollectionCount(i)-collections[i]).ToArray(),gcPauseMilliseconds=(GC.GetTotalPauseDuration()-pause).TotalMilliseconds,producerCalls=producerCalls-calls,trackedRowLoads=trackedRowLoads-loads,trackedRowUnloads=trackedRowUnloads-unloads};Guard();var snapshot=Verify(operation);if(!warmup){measurements.Add(measurement);snapshots.Add(new{rows=count,operation,snapshot});}await File.WriteAllTextAsync(output+".progress.json",JsonSerializer.Serialize(new{stage,completedMeasurements=measurements.Count,measurement}));}
    await Measure("mount",()=>data.ItemsSource=items);originalRows=Rows().ToDictionary(r=>((BenchRow)r.Tag).Id);originalCells=Rows().ToDictionary(r=>((BenchRow)r.Tag).Id,r=>r.Cells.ToArray());foreach(var tracked in Rows()){tracked.Loaded+=(_,_)=>trackedRowLoads++;tracked.Unloaded+=(_,_)=>trackedRowUnloads++;}
    var header=Elements(data).OfType<BleeckerSortableTableHeader>().Single(h=>h.Field=="Count");var sort=(IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement((Button)header.Content).GetPattern(PatternInterface.Invoke);var all=Elements(data).OfType<BleeckerCheckbox>().Single(c=>AutomationProperties.GetName(c)=="Select all");var toggle=(IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(all).GetPattern(PatternInterface.Toggle);
    if(!warmup)await Capture($"table-performance-{count}-mount.png");
    await Measure("sort-ascending",sort.Invoke);await Measure("sort-descending",sort.Invoke);await Measure("select-all",toggle.Toggle);Require(data.EffectiveSelectedKeys.Count==count,"Select all omitted a row.");await Measure("deselect-all",toggle.Toggle);Require(data.EffectiveSelectedKeys.Count==0,"Deselect all retained rows.");await Measure("item-change",()=>items[count/2].Change("Changed fictional item"));await Measure("reverse-source",()=>{items=new ObservableCollection<BenchRow>(items.Reverse());data.ItemsSource=items;});await Measure("release-sort",()=>data.Sort=new(""));
    var removed=items[0];await Measure("remove-one",()=>items.RemoveAt(0));originalRows.Remove(removed.Id);originalCells.Remove(removed.Id);await Measure("append-original",()=>items.Add(removed));
    if(!warmup)await Capture($"table-performance-{count}-final.png");root.Children.Clear();await Layout();Guard();var before=producerCalls;originalItems[0].Change("Unloaded fictional update");Require(producerCalls==before,"Removed table retained item observers.");
   }
   string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-table-performance-native-v1",passed=true,measurements,snapshots,pngs,guards,focus,activations,activateCalled=false,showWindowCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false,publicApisOnly=true,forcedGc=false,timingIncludesThreeDispatcherIdles=true,captureExcludedFromTimings=true,warmupRows=16,processorCount=Environment.ProcessorCount,runtime=Environment.Version.ToString(),os=System.Runtime.InteropServices.RuntimeInformation.OSDescription,textScaleFactor=new Windows.UI.ViewManagement.UISettings().TextScaleFactor,librarySha256=Hash(typeof(BleeckerTable).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
  }catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-table-performance-native-v1",passed=false,stage,error=error.ToString(),measurements,snapshots,guards,focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
 }
 private sealed class BenchRow(int index,int count) : INotifyPropertyChanged
 {
  public string Id{get;}="row-"+index.ToString("D4");public string Name{get;private set;}="Fictional "+index.ToString("D4");public int Count{get;}=count-index;private TextBlock? statusView;public TextBlock StatusView=>statusView??=new TextBlock{Text="Status "+index.ToString("D4"),UseLayoutRounding=false,TextWrapping=TextWrapping.WrapWholeWords};public event PropertyChangedEventHandler? PropertyChanged;internal void Change(string value){Name=value;PropertyChanged?.Invoke(this,new(nameof(Name)));}
 }
}
