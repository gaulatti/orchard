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
internal static class TableCatalogProbe
{
 private delegate bool Visitor(nint handle,nint parameter);
 [DllImport("user32.dll")]private static extern bool EnumWindows(Visitor visitor,nint parameter);
 [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
 [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
 [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(nint handle);
 private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
 internal static async Task RunAsync(Window window,string output)
 {
  var fixture=new TablesFixture();var root=new Grid{Width=900,Height=760};root.Children.Add(fixture);window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,760));var guards=0;var focus=0;var activations=0;var cases=new List<object>();var pngs=new List<object>();var stage="initial";
  root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
  void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
  void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Actual Table catalog left its hidden boundary.");}
  async Task Layout(){for(var i=0;i<8;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Could not drain actual Table fixture.");await idle.Task;root.Measure(new Size(900,760));root.Arrange(new Rect(0,0,900,760));root.UpdateLayout();Guard();await Task.Delay(15);}}
  async Task Capture(string file){Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),bytes);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});Guard();}
  try{
   await Layout();var table=Elements(fixture).OfType<BleeckerTable>().Single();var action=Elements(fixture).OfType<Button>().Single(b=>b.Name=="OriginalAction");var status=Elements(fixture).OfType<TextBlock>().Single(b=>b.Name=="Status");var row=Elements(fixture).OfType<BleeckerTableRow>().Single(r=>r.Name=="OriginalRow");
   foreach(var scheme in new[]{"Light","Dark"}){stage=scheme;root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);await Layout();var footer=table.Sections.OfType<BleeckerTableFooter>().Single().Children.OfType<BleeckerTableRow>().Single();Require(footer.FontWeight==Microsoft.UI.Text.FontWeights.Medium,"Actual footer did not receive library medium weight.");var originalCells=row.Cells.ToArray();var originalContent=row.Cells[2].Content;Require(table.IsLoaded&&action.IsLoaded&&originalCells.Length==3&&ReferenceEquals(originalContent,action),"Actual Table catalog lost original rows or actions.");row.IsSelected=false;await Capture($"table-catalog-{scheme}-initial.png");var invoke=(IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(action).GetPattern(PatternInterface.Invoke);invoke.Invoke();await Layout();Require(row.IsSelected&&status.Text=="Selected: Inspect fictional item","Actual Table action did not update selected paint and visible status.");await Capture($"table-catalog-{scheme}-selected.png");invoke.Invoke();await Layout();Require(!row.IsSelected&&originalCells.SequenceEqual(row.Cells)&&ReferenceEquals(row.Cells[2].Content,originalContent),"Actual Table action replaced original content or failed to toggle state.");cases.Add(new{scheme,passed=true,actualFixtureRetained=ReferenceEquals(root.Children.Single(),fixture),originalCellsAndActionRetained=true,nativeInvokes=2,statusUpdated=true,selectedRowToggledBothWays=true,footerMediumSuppliedByLibrary=true});}
   string Hash(string p)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-table-catalog-v1",passed=true,cases,pngs,guards,focus,activations,activateCalled=false,showWindowCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,nativeComponentAccepted=false,librarySha256=Hash(typeof(BleeckerTable).Assembly.Location),hostSha256=Hash(typeof(App).Assembly.Location)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
  }catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-table-catalog-v1",passed=false,stage,error=error.ToString(),cases,guards,focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
 }
}
