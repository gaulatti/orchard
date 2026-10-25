using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Catalog;
internal static class SimpleDataTableCatalogProbe
{
 private delegate bool Visitor(nint handle,nint parameter);
 [DllImport("user32.dll")]private static extern bool EnumWindows(Visitor visitor,nint parameter);
 [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
 [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
 [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(nint handle);
 private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
 internal static async Task RunAsync(Window window,string output)
 {
  var fixture=new SimpleDataTablesFixture();var root=new Grid{Width=900,Height=760};root.Children.Add(fixture);window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,760));var guards=0;var focus=0;var activations=0;var cases=new List<object>();var pngs=new List<object>();var stage="initial";
  root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
  void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
  void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Actual Table catalog left its hidden boundary.");}
  async Task Layout(){for(var i=0;i<8;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Could not drain actual Table fixture.");await idle.Task;root.Measure(new Size(900,760));root.Arrange(new Rect(0,0,900,760));root.UpdateLayout();Guard();await Task.Delay(15);}}
  async Task Capture(string file){Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),bytes);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});Guard();}
  try{
   await Layout();var data=Elements(fixture).OfType<BleeckerSimpleDataTable>().Single();var table=Elements(data).OfType<BleeckerTable>().Single();var status=Elements(fixture).OfType<TextBlock>().Single(b=>b.Name=="Status");var standalone=Elements(fixture).OfType<BleeckerSortableTableHeader>().Single(h=>h.Name=="StandaloneHeader");
   void Invoke(Button b)=>((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(b).GetPattern(PatternInterface.Invoke)).Invoke();
   foreach(var scheme in new[]{"Light","Dark"}){
    stage=scheme;root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);data.Sort=null;data.SelectedKeys=null;await Layout();var rows=table.Sections.OfType<BleeckerTableBody>().Single();var originals=rows.Children.OfType<BleeckerTableRow>().SelectMany(r=>r.Cells).Select(c=>c.Content).OfType<Button>().ToArray();Require(originals.Length==3,"Actual composition lost its original action controls.");
    await Capture($"simple-table-catalog-{scheme}-initial.png");
    var count=Elements(data).OfType<BleeckerSortableTableHeader>().Single(h=>h.Field=="Count");data.Sort=new("Count",BleeckerTableSortOrder.Descending);await Layout();Invoke((Button)count.Content);await Layout();Require(data.EffectiveSort==new BleeckerTableSort("Count",BleeckerTableSortOrder.Descending),"Rejected controlled sort changed visible state.");data.Sort=null;await Layout();Require(data.EffectiveSort==new BleeckerTableSort("Count")&&status.Text=="Sort request: Count Ascending","Actual header did not emit its request or update visible status.");
    var first=rows.Children.OfType<BleeckerTableRow>().First();var checkbox=(BleeckerCheckbox)first.Cells[0].Content;data.SelectedKeys=new HashSet<string>(StringComparer.Ordinal);await Layout();((IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(checkbox).GetPattern(PatternInterface.Toggle)).Toggle();await Layout();Require(data.EffectiveSelectedKeys.Count==0,"Rejected controlled selection changed visible state.");data.SelectedKeys=null;await Layout();Require(status.Text=="Selected: birch"&&data.EffectiveSelectedKeys.SetEquals(new[]{"birch"}),"Actual checkbox did not request visible selection.");
    var action=(Button)first.Cells.Last().Content;Invoke(action);await Layout();Require(status.Text=="Original action: birch"&&originals.Contains(action),"Actual original cell action or visible status failed.");
    standalone.CurrentSort=new("");Invoke((Button)standalone.Content);await Layout();Require(standalone.CurrentSort==new BleeckerTableSort("name")&&status.Text=="Standalone sort: Ascending","Standalone original header request failed.");Invoke((Button)standalone.Content);await Layout();Require(standalone.CurrentSort==new BleeckerTableSort("name",BleeckerTableSortOrder.Descending),"Standalone descending request failed.");
    await Capture($"simple-table-catalog-{scheme}-actions.png");Require(originals.All(b=>b.IsLoaded)&&ReferenceEquals(Elements(data).OfType<BleeckerTable>().Single(),table),"Actual composition replaced original controls.");cases.Add(new{scheme,passed=true,actualFixtureRetained=ReferenceEquals(root.Children.Single(),fixture),originalCellsAndActionRetained=true,statusUpdated=true,nativeSortAndSelectionVerified=true,standaloneHeaderVerified=true,nativeActions=5});
   }
   string Hash(string p)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-simple-table-catalog-v1",passed=true,cases,pngs,guards,focus,activations,activateCalled=false,showWindowCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,nativeComponentAccepted=false,librarySha256=Hash(typeof(BleeckerTable).Assembly.Location),hostSha256=Hash(typeof(App).Assembly.Location)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
  }catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-simple-table-catalog-v1",passed=false,stage,error=error.ToString(),cases,guards,focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
 }
}
