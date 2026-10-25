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
using Microsoft.UI.Xaml.Hosting;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

// The source catalog and isolated NuGet consumer compile this same public-API fixture.
internal static partial class DataTableReferenceFixture
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
        using var reference=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(output)!,"source-reference.json")));
        var root=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='900' Height='1000' Background='{ThemeResource BleeckerBackgroundBrush}'/>");
        root.Clip=new RectangleGeometry{Rect=new Rect(0,0,900,1000)};
        window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,1000));var cases=new List<object>();var histories=new List<object>();var pngs=new List<object>();var shadowLifecycleContracts=new List<object>();var shadowUnloadedContexts=0;var guards=0;var focus=0;var activations=0;var stage="initial";
        root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,e)=>{if(e.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Data Table reference probe left its hidden boundary.");}
        async Task Layout(){for(var i=0;i<5;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Dispatcher refused layout.");await idle.Task;root.Measure(new Size(900,1000));root.Arrange(new Rect(0,0,900,1000));root.UpdateLayout();Guard();await Task.Delay(12);}}
        async Task Capture(string file){if(!Environment.GetCommandLineArgs().Contains("--capture-early"))await Task.Delay(TimeSpan.FromMilliseconds(BleeckerTokens.Durations["control"]+32));Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),bytes);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});Guard();}
        Grid[] ShadowHosts(BleeckerDataTable data)=>Elements(data).OfType<Grid>().Where(e=>e.Name is "PART_ShadowNear" or "PART_ShadowFar").ToArray();
        static bool Attached(Grid[] hosts)=>hosts.Length==2&&hosts.All(h=>ElementCompositionPreview.GetElementChildVisual(h) is not null);
        static bool Detached(Grid[] hosts)=>hosts.Length==2&&hosts.All(h=>ElementCompositionPreview.GetElementChildVisual(h) is null);
        async Task Remove(Context context){var hosts=ShadowHosts(context.Data);root.Children.Clear();await Layout();Require(!context.Data.IsLoaded&&Detached(hosts),"Unloaded reference case retained a shadow graph.");shadowUnloadedContexts++;}
        async Task SurfaceContract(string scheme)
        {
            stage=scheme+"-surface-contract";var entry=reference.RootElement.GetProperty("cases").EnumerateArray().First(c=>c.GetProperty("scheme").GetString()==scheme&&c.GetProperty("name").GetString()=="default");var context=new Context(entry.GetProperty("spec"));var data=context.Data;root.Children.Add(data);await Layout();Require(Attached(ShadowHosts(data)),"Default Data Table shadow group missing.");
            data.Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,12,23,34));data.BorderBrush=new SolidColorBrush(Windows.UI.Color.FromArgb(255,56,67,78));data.BorderThickness=new Thickness(2);data.CornerRadius=new CornerRadius(7);await Layout();
            var surface=Elements(data).OfType<Border>().Single(e=>e.Name=="PART_Surface");var mask=Elements(data).OfType<Microsoft.UI.Xaml.Shapes.Path>().Single(e=>e.Name=="PART_MaskNear");var rounded=((PathGeometry)mask.Data).Figures[0].Segments.OfType<ArcSegment>().Select(a=>a.Size.Width).ToArray();
            Require(ReferenceEquals(surface.Background,data.Background)&&ReferenceEquals(surface.BorderBrush,data.BorderBrush)&&surface.BorderThickness.Left==2&&surface.CornerRadius.TopLeft==7&&rounded.SequenceEqual(new double[]{7,7,7,7}),"Caller surface values did not reach paint/mask.");
            var hosts=ShadowHosts(data);root.Children.Clear();await Layout();Require(Detached(hosts),"Unloaded customized surface kept its graph.");root.Children.Add(data);await Layout();Require(Attached(hosts),"Reloaded surface did not restore its graphs.");
            var template=data.Template;data.Template=null;await Layout();Require(Detached(hosts),"Cleared template kept its shadow graph.");data.Template=template;await Layout();Require(Attached(ShadowHosts(data)),"Restored template has no shadow graph.");
            hosts=ShadowHosts(data);data.Template=(ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:o='using:Orchard' TargetType='o:BleeckerDataTable'><StackPanel><TextBlock x:Name='PART_Secondary'/><Grid x:Name='PART_SearchHost'><o:BleeckerInput x:Name='PART_Search'/></Grid><Grid x:Name='PART_TableHost'/><TextBlock x:Name='PART_Summary'/><Button x:Name='PART_Previous'/><Button x:Name='PART_Next'/></StackPanel></ControlTemplate>");await Layout();
            var table=Elements(data).OfType<BleeckerTable>().Single();var rendered=table.Sections.OfType<BleeckerTableBody>().Single().Children.OfType<BleeckerTableRow>().ToArray();Require(Detached(hosts)&&ShadowHosts(data).Length==0&&table.IsLoaded&&rendered.Length==10&&rendered[0].Tag is BleeckerDataTableRow row&&ReferenceEquals(row.Original,context.Items[0]),"Caller template lost original rows or retained default decorations.");
            data.Template=template;await Layout();Require(Attached(ShadowHosts(data)),"Default decorations failed after caller template.");await Remove(context);shadowLifecycleContracts.Add(new{scheme,callerSurfaceBindingsRetained=true,updatedCornerMaskRadii=rounded,unloadDetached=true,reloadAttached=true,templateDetached=true,callerTemplateWithoutDecorationsVerified=true,callerOriginalRowsRetained=true,defaultRestored=true});
        }
        try
        {
            foreach(var scheme in new[]{"Light","Dark"})
            {
                root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);
                foreach(var entry in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c=>c.GetProperty("scheme").GetString()==scheme))
                {
                    var name=entry.GetProperty("name").GetString()!;stage=scheme+"-static-"+name;var context=new Context(entry.GetProperty("spec"));root.Children.Add(context.Data);await Layout();await Capture("data-table-reference-"+scheme+"-"+name+".png");var snapshot=JsonSerializer.SerializeToElement(context.Snapshot(root));cases.Add(new{scheme,name,snapshot});await Remove(context);
                }
                foreach(var history in reference.RootElement.GetProperty("histories").EnumerateArray().Where(h=>h.GetProperty("scheme").GetString()==scheme))
                {
                    var action=history.GetProperty("action").GetString()!;var context=new Context(history.GetProperty("spec"));root.Children.Add(context.Data);await Layout();var snapshots=new List<object>();
                    foreach(var expected in history.GetProperty("snapshots").EnumerateArray())
                    {
                        var label=expected.GetProperty("stage").GetString()!;stage=scheme+"-"+action+"-"+label;if(label!="initial")context.Transition(action,label);await Layout();await Capture("data-table-reference-"+scheme+"-history-"+action+"-"+label+".png");snapshots.Add(new{stage=label,snapshot=JsonSerializer.SerializeToElement(context.Snapshot(root))});
                    }
                    histories.Add(new{scheme,action,snapshots});await Remove(context);
                }
                await SurfaceContract(scheme);
            }
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-data-table-reference-native-v1",passed=true,settledMotionEndpoints=!Environment.GetCommandLineArgs().Contains("--capture-early"),controlDurationMilliseconds=BleeckerTokens.Durations["control"],compositionShadowRasterVerified=false,shadowLifecycleContracts,shadowUnloadedContexts,captureViewportClipped=true,cases,histories,pngs,guards,focus,activations,activateCalled=false,showWindowCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,completeSourceGeometryVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false,publicApisOnly=true,librarySha256=Hash(typeof(BleeckerDataTable).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-data-table-reference-native-v1",passed=false,stage,error=error.ToString(),cases,histories,guards,focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
}
