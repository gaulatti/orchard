using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
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

// The source catalog and isolated NuGet consumer compile this same public-API fixture.
internal static class DataTableCatalogProbe
{
    private delegate bool Visitor(nint handle,nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor,nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement element)yield return element;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private static object Box(FrameworkElement e,UIElement root){var b=e.TransformToVisual(root).TransformBounds(new Rect(0,0,e.ActualWidth,e.ActualHeight));return new{x=b.X,y=b.Y,width=b.Width,height=b.Height};}
    private static string Hash(string path)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
    private static void Invoke(FrameworkElement button){var action=button as Button??Elements(button).OfType<Button>().Single();var peer=FrameworkElementAutomationPeer.CreatePeerForElement(action);Require(peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider,"Native button has no Invoke provider.");((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();}
    private static void Toggle(BleeckerCheckbox checkbox)=>((IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(checkbox).GetPattern(PatternInterface.Toggle)).Toggle();
    internal static async Task RunAsync(Window window,string output,string hostAssembly)
    {
        var root=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='900' Height='1300' Background='{ThemeResource BleeckerBackgroundBrush}'/>");window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,1300));var states=new List<object>();var pngs=new List<object>();var stage="initial";var guards=0;var focus=0;var activations=0;
        // Reserve an ink gutter around the real fixture in the offscreen viewport.
        root.Padding=new Thickness(16);
        root.Clip=new RectangleGeometry{Rect=new Rect(0,0,900,1300)};
        root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,e)=>{if(e.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Actual catalog probe left its hidden boundary.");}
        async Task Layout(){for(var i=0;i<5;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Dispatcher refused catalog layout.");await idle.Task;root.Measure(new Size(900,1300));root.Arrange(new Rect(0,0,900,1300));root.UpdateLayout();Guard();await Task.Delay(12);}}
        async Task Capture(string file){Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),bytes);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});Guard();}
        try
        {
            foreach(var scheme in new[]{"Light","Dark"})
            {
                root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);var fixture=new Orchard.Catalog.DataTablesFixture();root.Children.Add(fixture);await Layout();var data=(BleeckerDataTable)fixture.FindName("Data");var status=(TextBlock)fixture.FindName("Status");
                BleeckerTable Table()=>Elements(data).OfType<BleeckerTable>().Single();
                BleeckerInput Search()=>Elements(data).OfType<BleeckerInput>().Single(e=>e.Name=="PART_Search");
                BleeckerCheckbox Check(string name)=>Elements(data).OfType<BleeckerCheckbox>().Single(c=>AutomationProperties.GetName(c)==name);
                async Task Record(string label)
                {
                    stage=scheme+"-"+label;await Layout();await Capture("data-table-catalog-"+scheme+"-"+label+".png");var rows=Table().Sections.OfType<BleeckerTableBody>().Single().Children.OfType<BleeckerTableRow>().ToArray();
                    Require(rows.Where(r=>r.Tag is BleeckerDataTableRow).Select(r=>((BleeckerDataTableRow)r.Tag).Id).SequenceEqual(data.Model.PageRows.Select(r=>r.Id)),"Actual catalog model/row order differs.");
                    foreach(var cell in rows.SelectMany(r=>r.Cells).Where(c=>c.Tag is BleeckerDataTableCell context&&context.Column.Id=="Status")){var context=(BleeckerDataTableCell)cell.Tag;Require(ReferenceEquals(cell.Content,context.RenderContent())&&cell.Content is BleeckerButton{IsLoaded:true},"Catalog replaced its original item action.");}
                    states.Add(new{scheme,stage=label,fixtureType=fixture.GetType().FullName,rows=rows.Where(r=>r.Tag is BleeckerDataTableRow).Select(r=>new{id=((BleeckerDataTableRow)r.Tag).Id,name=((BleeckerDataTableRow)r.Tag).GetValue("Name"),count=((BleeckerDataTableRow)r.Tag).GetValue("Count")}).ToArray(),pageIndex=data.Model.PageIndex,pageCount=data.Model.PageCount,filtered=data.Model.FilteredRowCount,filteredSelected=data.Model.FilteredSelectedRowCount,selected=data.Model.SelectedRowIds.OrderBy(id=>id,StringComparer.Ordinal).ToArray(),search=Search().Text,status=status.Text,emptyText=rows.SingleOrDefault(r=>r.Tag is null)?.Cells.Single().Content?.ToString(),originalActionsRetained=true,box=Box(data,root)});
                }
                await Record("initial");Toggle(Check("Select row 0"));await Record("selected-first");Invoke(Elements(data).OfType<Button>().Single(b=>b.Name=="PART_Next"));await Record("page-two");Toggle(Check("Select row 10"));await Record("selected-second-page");Search().Text="Cedar";await Record("filtered");
                var action=(BleeckerButton)Table().Sections.OfType<BleeckerTableBody>().Single().Children.OfType<BleeckerTableRow>().First().Cells[3].Content;Invoke(action);await Record("original-action");Require(status.Text=="Original action: fictional-00","Actual catalog action did not retain its original ID.");
                Search().Text="no fictional match";await Record("empty");Search().Text="";await Record("cleared");var count=Elements(data).OfType<BleeckerButton>().Single(b=>b.Content as string=="Count");Invoke(count);await Record("count-descending");Invoke(count);await Record("count-ascending");Toggle(Check("Select page"));await Record("select-page");Toggle(Check("Select page"));await Record("unselect-page");root.Children.Clear();await Layout();Require(!data.IsLoaded&&!action.IsLoaded,"Removed actual catalog fixture retained loaded content.");
            }
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-data-table-catalog-native-v1",passed=true,states,pngs,guards,focus,activations,activateCalled=false,showWindowCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false,publicApisOnly=true,librarySha256=Hash(typeof(BleeckerDataTable).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-data-table-catalog-native-v1",passed=false,stage,error=error.ToString(),states,guards,focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
}
