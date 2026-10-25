using System.Runtime.InteropServices;
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
internal static class TableTextFixture
{
 private delegate bool Visitor(nint handle,nint value);
 [DllImport("user32.dll")]private static extern bool EnumWindows(Visitor visitor,nint value);
 [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
 [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
 [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(nint handle);
 private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
 internal static async Task RunAsync(Window window,string output,string hostAssembly)
 {
  using var source=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(output)!,"source-reference.json")));
  var root=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='900' Height='760' Background='{ThemeResource BleeckerBackgroundBrush}'/>");window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,760));var guards=0;var focus=0;var activations=0;var stage="initial";var cases=new List<object>();var histories=new List<object>();var pngs=new List<object>();
  root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
  void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
  void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Table text probe left its hidden boundary.");}
  async Task Layout(){for(var i=0;i<6;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Dispatcher refused layout.");await idle.Task;root.Measure(new Size(900,760));root.Arrange(new Rect(0,0,900,760));root.UpdateLayout();Guard();await Task.Delay(12);}}
  async Task Capture(string file){Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),bytes);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});Guard();}
  try{
   foreach(var scheme in new[]{"Light","Dark"}){
    root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);
    foreach(var sample in source.RootElement.GetProperty("cases").EnumerateArray().Where(c=>c.GetProperty("scheme").GetString()==scheme)){
     var name=sample.GetProperty("name").GetString()!;var spec=sample.GetProperty("spec");stage=scheme+"-"+name;root.Language=spec.GetProperty("language").GetString()!;var label=spec.GetProperty("label").GetString()!;var table=Headings(label);root.Children.Add(table);await Layout();var snapshot=HeadingSnapshot(table);await Capture($"table-text-{scheme}-{name}.png");cases.Add(new{scheme,name,passed=true,snapshot});root.Children.Clear();await Layout();
    }
    foreach(var history in source.RootElement.GetProperty("histories").EnumerateArray().Where(h=>h.GetProperty("scheme").GetString()==scheme)){
     var name=history.GetProperty("name").GetString()!;stage=scheme+"-"+name;var spec=history.GetProperty("spec");root.Language=spec.GetProperty("language").GetString()!;var snapshots=new List<object>();
     if(name=="live-language"){
      var table=Headings(spec.GetProperty("label").GetString()!);root.Children.Add(table);await Layout();var originals=table.Sections.OfType<BleeckerTableHeader>().Single().Children.OfType<BleeckerTableRow>().Single().Cells.ToArray();
      foreach(var expected in history.GetProperty("snapshots").EnumerateArray()){var label=expected.GetProperty("stage").GetString()!;root.Language=expected.GetProperty("language").GetString()!;await Layout();Require(originals.SequenceEqual(table.Sections.OfType<BleeckerTableHeader>().Single().Children.OfType<BleeckerTableRow>().Single().Cells),"Language changes replaced original headings.");snapshots.Add(new{stage=label,snapshot=HeadingSnapshot(table)});}root.Children.Clear();await Layout();
     }else{
      var data=new BleeckerSimpleDataTable{Width=660,Margin=new Thickness(20),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,IsSelectable=true};data.GetRowKey=(row,_)=>((Dictionary<string,object?>)row)["id"]!.ToString()!;data.Columns.Add(new("name","Fictional item"));data.Columns.Add(new("count","Count",alignment:BleeckerTableCellAlignment.Right));
      var originals=spec.GetProperty("data").EnumerateArray().Select(row=>row.EnumerateObject().ToDictionary(p=>p.Name,p=>(object?)(p.Value.ValueKind==JsonValueKind.Number?p.Value.GetInt32():p.Value.GetString()),StringComparer.OrdinalIgnoreCase)).ToArray();data.ItemsSource=originals;
      var controlled=new HashSet<string>(spec.GetProperty("keys").EnumerateArray().Select(k=>k.GetString()!),StringComparer.OrdinalIgnoreCase);data.SelectedKeys=controlled;var events=new List<object>();data.SelectionRequested+=(_,keys)=>events.Add(new{kind="selection",keys=keys.ToArray()});root.Children.Add(data);await Layout();
      foreach(var expected in history.GetProperty("snapshots").EnumerateArray()){
       var label=expected.GetProperty("stage").GetString()!;
       void Toggle(string target){var checkbox=Elements(data).OfType<BleeckerCheckbox>().Single(c=>AutomationProperties.GetName(c)==target);((IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(checkbox).GetPattern(PatternInterface.Toggle)).Toggle();}
       switch(label){case "initial":break;case "rejected-request":Toggle("Select row cedar");break;case "released-control":data.SelectedKeys=null;break;case "all-selected":case "none-selected":Toggle("Select all");break;default:throw new InvalidOperationException("Unknown key transition "+label);}
       await Layout();Require(controlled.SequenceEqual(spec.GetProperty("keys").EnumerateArray().Select(k=>k.GetString()!)),"Control changed caller set values.");snapshots.Add(new{stage=label,snapshot=DataSnapshot(data,events),callerSetUnchanged=true,originalItemsRetained=Elements(data).OfType<BleeckerTableBody>().Single().Children.OfType<BleeckerTableRow>().All(r=>originals.Any(item=>ReferenceEquals(item,r.Tag)))});
      }root.Children.Clear();await Layout();
     }
     histories.Add(new{scheme,name,passed=true,retainedNativeInstance=true,snapshots});
    }
   }
   string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-table-text-native-v1",passed=true,cases,histories,pngs,guards,focus,activations,activateCalled=false,showWindowCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false,publicApisOnly=true,librarySha256=Hash(typeof(BleeckerTable).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
  }catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-table-text-native-v1",passed=false,stage,error=error.ToString(),cases,histories,guards,focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
 }
 private static BleeckerTable Headings(string label){var table=new BleeckerTable{Width=660,Margin=new Thickness(20),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top};var section=new BleeckerTableHeader();var row=new BleeckerTableRow();row.Cells.Add(new BleeckerTableHead{Content=label});row.Cells.Add(new BleeckerSortableTableHeader{Field="label",Label=label});row.Cells.Add(new BleeckerSortableTableHeader{Field="plain",Label=label,IsSortable=false});section.Children.Add(row);table.Sections.Add(section);return table;}
 private static object HeadingSnapshot(BleeckerTable table)=>new{language=table.Language,header=table.Sections.OfType<BleeckerTableHeader>().Single().Children.OfType<BleeckerTableRow>().Single().Cells.Select(cell=>new{raw=cell is BleeckerSortableTableHeader sort?sort.Label:cell.Content as string,display=cell is BleeckerSortableTableHeader{IsSortable:true} sortable?((Button)sortable.Content).Content as string:Elements(cell).OfType<TextBlock>().Single(t=>t.Name=="PART_Text").Text,originalAccessibleLabel=cell is BleeckerSortableTableHeader{IsSortable:true} heading?AutomationProperties.GetName((Button)heading.Content):FrameworkElementAutomationPeer.CreatePeerForElement(cell).GetName()}).ToArray()};
 private static object DataSnapshot(BleeckerSimpleDataTable data,List<object> events)=>new{body=Elements(data).OfType<BleeckerTableBody>().Single().Children.OfType<BleeckerTableRow>().Select(row=>new{selected=row.IsSelected?"true":"false",cells=row.Cells.Select(cell=>cell.Content is BleeckerCheckbox?"":cell.Content as string??"").ToArray()}).ToArray(),checkboxes=Elements(data).OfType<BleeckerCheckbox>().Select(c=>new{name=AutomationProperties.GetName(c),@checked=c.IsChecked==true,indeterminate=!c.IsChecked.HasValue}).ToArray(),events=events.ToArray()};
}
