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

internal static class ModalCatalogProbe
{
    private delegate bool Visitor(nint handle,nint parameter);
    [DllImport("user32.dll")]private static extern bool EnumWindows(Visitor visitor,nint parameter);
    [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    internal static async Task RunAsync(Window window,string output)
    {
        var folder=Path.GetDirectoryName(output)!;Directory.CreateDirectory(folder);var background=new Grid();var host=new BleeckerModalHost{Content=background,Width=900,Height=760};window.Content=host;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,760));var guards=0;var focus=0;var activations=0;var invokes=0;var cases=new List<object>();var pngs=new List<object>();
        var stage="initial";void Stage(string value){stage=value;File.WriteAllText(Path.Combine(folder,"stage.json"),JsonSerializer.Serialize(new{stage,guards,invokes,cases=cases.Count}));}Stage("window-created");
        host.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Require(bool valid,string message){if(!valid)throw new InvalidOperationException(message);}
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((handle,_)=>{GetWindowThreadProcessId(handle,out var owner);visible|=owner==Environment.ProcessId&&IsWindowVisible(handle);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0&&VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot).Count==0,"Actual Modal catalog left its hidden boundary.");}
        async Task Layout(){for(var i=0;i<6;i++){var idle=new TaskCompletionSource();Require(host.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Could not drain catalog layout.");await idle.Task;host.Measure(new Size(900,760));host.Arrange(new Rect(0,0,900,760));host.UpdateLayout();Guard();await Task.Delay(12);}}
        FrameworkElement Surface()=>Elements(host).Single(e=>e.Name=="ModalSurface");
        ScrollView View()=>Elements(host).OfType<ScrollView>().Single(v=>v.Name=="ModalViewport");
        async Task Invoke(string label){Stage("invoke:"+label);var button=Elements(host).OfType<Button>().Single(b=>FrameworkElementAutomationPeer.CreatePeerForElement(b)?.GetName()==label);((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke)).Invoke();invokes++;await Task.Delay(260);await Layout();Stage("invoked:"+label);}
        async Task Capture(string file){Stage("capture:"+file);var bitmap=new RenderTargetBitmap();var operation=bitmap.RenderAsync(host);var render=operation.AsTask();if(await Task.WhenAny(render,Task.Delay(2000))!=render){operation.Cancel();throw new TimeoutException("Actual Modal catalog capture exceeded two seconds at "+stage);}await render;Guard();var pixels=await bitmap.GetPixelsAsync();var bytes=new byte[checked((int)pixels.Length)];using(var reader=DataReader.FromBuffer(pixels))reader.ReadBytes(bytes);using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,bytes);await encoder.FlushAsync();stream.Seek(0);var encoded=new byte[checked((int)stream.Size)];using(var reader=new DataReader(stream)){await reader.LoadAsync((uint)encoded.Length);reader.ReadBytes(encoded);}await File.WriteAllBytesAsync(Path.Combine(folder,file),encoded);pngs.Add(new{file,sha256=Convert.ToHexStringLower(SHA256.HashData(encoded)),width=bitmap.PixelWidth,height=bitmap.PixelHeight});}
        foreach(var scheme in new[]{"Light","Dark"})
        {
            Stage(scheme+"-fixture-create");host.RequestedTheme=Enum.Parse<ElementTheme>(scheme);var fixture=new ModalsFixture();Stage(scheme+"-fixture-created");background.Children.Add(fixture);await Layout();Stage(scheme+"-fixture-loaded");await Invoke("Open component example");var dialog=(BleeckerModal)fixture.FindName("Default");var editor=Elements(host).OfType<TextBox>().Single(t=>t.Header?.ToString()=="Fictional note");editor.Text="Retained fictional catalog edit";var compact=Surface().ActualHeight;await Invoke("Expand fictional content");var expanded=Surface().ActualHeight;Require(expanded==728&&View().ExtentHeight>View().ViewportHeight&&dialog.IsOpen,"Actual expand handler did not grow Modal.");await Capture("catalog-modal-"+scheme+"-expanded.png");await Invoke("Shrink fictional content");Require(Surface().ActualHeight==compact&&editor.Text=="Retained fictional catalog edit"&&ReferenceEquals(editor,Elements(host).OfType<TextBox>().Single(t=>t.Header?.ToString()=="Fictional note")),"Actual shrink handler retained stale height or replaced the editor.");await Capture("catalog-modal-"+scheme+"-shrunk.png");cases.Add(new{scheme,passed=true,compactHeight=compact,expandedHeight=expanded,restoredHeight=Surface().ActualHeight,originalEditorRetained=true,actualFixture=true});await Invoke("Close");Require(!dialog.IsOpen,"Actual catalog close handler did not accept the request.");background.Children.Remove(fixture);await Layout();
        }
        string Hash(string p)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-modal-catalog-v1",passed=true,cases,pngs,guards,focus,activations,softwareNativeInvokes=invokes,librarySha256=Hash(typeof(BleeckerModal).Assembly.Location),hostSha256=Hash(typeof(App).Assembly.Location),activateCalled=false,showCalled=false,focusCalled=false,hardwareInputSent=false,nativeComponentAccepted=false},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
    }
}
