using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static partial class DataTableReferenceFixture
{
    internal static async Task RunFilterPlansAsync(Window window,string output,string hostAssembly)
    {
        using var source=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(output)!,"filter-source-reference.json")));
        var root=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='900' Height='1000' Background='{ThemeResource BleeckerBackgroundBrush}'/>");root.Clip=new RectangleGeometry{Rect=new Rect(0,0,900,1000)};window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,1000));
        var states=new List<object>();var pngs=new List<object>();var guards=0;var focus=0;var activations=0;var stage="initial";
        root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,e)=>{if(e.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Data Table filter-plan probe left its hidden boundary.");}
        async Task Layout(){for(var i=0;i<5;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Dispatcher refused filter-plan layout.");await idle.Task;root.Measure(new Size(900,1000));root.Arrange(new Rect(0,0,900,1000));root.UpdateLayout();Guard();await Task.Delay(12);}}
        async Task Capture(string file){await Task.Delay(TimeSpan.FromMilliseconds(BleeckerTokens.Durations["control"]+32));Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),bytes);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});Guard();}
        try
        {
            foreach(var entry in source.RootElement.GetProperty("cases").EnumerateArray())
            {
                var scheme=entry.GetProperty("scheme").GetString()!;var name=entry.GetProperty("name").GetString()!;root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);var context=new Context(entry.GetProperty("spec"));root.Children.Add(context.Data);
                foreach(var label in new[]{"initial","prepared","global"})
                {
                    stage=scheme+"-"+name+"-"+label;
                    if(label=="prepared"){if(name=="no-columns")context.Data.Columns.Clear();else if(name=="boolean-column-filter")context.Model.GetColumn("flag")!.SetFilterValue(true);else if(name=="hidden-eligible")foreach(var id in new[]{"name","count","status"})context.Model.GetColumn(id)!.ToggleVisibility(false);}
                    if(label=="global")context.Model.SetGlobalFilter(entry.GetProperty("query").GetString());
                    await Layout();var file="data-table-filter-plan-"+stage+".png";await Capture(file);states.Add(new{scheme,name,stage=label,snapshot=JsonSerializer.SerializeToElement(context.Snapshot(root)),file});
                }
                root.Children.Clear();await Layout();Require(!context.Data.IsLoaded,"Removed filter-plan component stayed loaded.");
            }
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-data-table-filter-plan-native-v1",passed=true,states,pngs,guards,focus,activations,activateCalled=false,showWindowCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false,publicApisOnly=true,librarySha256=Hash(typeof(BleeckerDataTable).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-data-table-filter-plan-native-v1",passed=false,stage,error=error.ToString(),states,pngs,guards,focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
}
