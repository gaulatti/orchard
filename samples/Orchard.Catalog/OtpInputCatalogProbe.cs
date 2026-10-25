using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
namespace Orchard.Catalog;

internal static class OtpInputCatalogProbe
{
    private delegate bool Visitor(nint handle,nint parameter);
    [DllImport("user32.dll")]private static extern bool EnumWindows(Visitor visitor,nint parameter);
    [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    internal static async Task RunAsync(Window window,string output)
    {
        var folder=Path.GetDirectoryName(output)!;Directory.CreateDirectory(folder);var fixture=new OtpInputsFixture();var root=new Grid{Width=900,Height=760,Padding=new Thickness(40)};root.Children.Add(fixture);window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,760));var guards=0;var focus=0;var activations=0;var edits=0;var cases=new List<object>();var pngs=new List<object>();var stage="initial";
        root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Require(bool valid,string message){if(!valid)throw new InvalidOperationException(message);}
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0&&VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count==0,"OTP catalog left its hidden boundary.");}
        async Task Layout(){for(var i=0;i<5;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Could not drain catalog layout.");await idle.Task;root.Measure(new Size(900,760));root.Arrange(new Rect(0,0,900,760));root.UpdateLayout();Guard();await Task.Delay(12);}}
        Control[] Editors(BleeckerOtpInput input)=>Elements(input).OfType<Control>().Where(c=>c is TextBox or PasswordBox).ToArray();
        async Task Edit(TextBox editor,string text){Guard();editor.Text=text;edits++;await Layout();Guard();}
        async Task Capture(string file){Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);await File.WriteAllBytesAsync(Path.Combine(folder,file),bytes);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});Guard();}
        try
        {
            foreach(var scheme in new[]{"Light","Dark"})
            {
                stage=scheme;root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);await Layout();var inputs=Elements(fixture).OfType<BleeckerOtpInput>().ToArray();Require(inputs.Length==7&&inputs.All(i=>i.ActualTheme==root.RequestedTheme),"Catalog lost original OTP controls or theme.");var sample=inputs.Single(i=>i.Name=="Sample");var rejected=inputs.Single(i=>i.Name=="RejectedSample");var four=inputs.Single(i=>i.Name=="FourSample");var secure=inputs.Single(i=>i.Name=="SecureSample");var narrow=inputs.Single(i=>i.Name=="NarrowSample");var disabled=inputs.Single(i=>i.Name=="DisabledSample");var invalid=inputs.Single(i=>i.Name=="InvalidSample");var status=Elements(fixture).OfType<TextBlock>().Single(t=>t.Name=="Status");Require(sample.Value=="482"&&rejected.Value=="482"&&Editors(four).Length==4&&secure.IsSecure&&Editors(secure).All(e=>e is PasswordBox&&FrameworkElementAutomationPeer.CreatePeerForElement(e).IsPassword())&&narrow.Width==160&&!disabled.IsEnabled&&Editors(disabled).All(e=>!e.IsEnabled)&&!AutomationProperties.GetIsDataValidForForm(invalid),"Catalog caller, length, secure, constrained, disabled or composed error defaults failed.");four.Value="27";await Layout();Require(((TextBox)Editors(four)[1]).Text=="7","Caller reset did not reach the four-slot example.");await Capture($"catalog-otp-{scheme}-initial.png");await Edit((TextBox)Editors(sample)[1],"7");Require(sample.Value=="472"&&status.Text=="Accepted fictional value: 472","Actual catalog did not accept the native value request or update its visible status.");await Edit((TextBox)Editors(rejected)[1],"7");Require(rejected.Value=="482"&&((TextBox)Editors(rejected)[1]).Text=="8","Actual no-handler example did not restore caller value.");await Edit((TextBox)Editors(four)[1],"9");Require(four.Value=="29","Four-slot example did not accept a native request.");await Capture($"catalog-otp-{scheme}-changed.png");await Edit((TextBox)Editors(sample)[1],"8");Require(sample.Value=="482"&&status.Text=="Accepted fictional value: 482","Catalog status did not track the restored caller value.");cases.Add(new{scheme,passed=true,actualFixtureControls=7,nativeEditorUpdatesStatus=true,noHandlerRestoresCallerValue=true,fourSlotValueUpdates=true,secureNativeEditors=true,disabledAndConstrainedExamples=true,composedErrorMetadata=true,originalFixtureRetained=ReferenceEquals(root.Children.Single(),fixture)});
            }
            string Hash(string p)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-otp-input-catalog-v1",passed=true,cases,pngs,guards,focus,activations,edits,activateCalled=false,showCalled=false,focusCalled=false,hardwareInputSent=false,osClipboardUsed=false,physicalInputVerified=false,nativeComponentAccepted=false,librarySha256=Hash(typeof(BleeckerOtpInput).Assembly.Location),hostSha256=Hash(typeof(App).Assembly.Location)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-otp-input-catalog-v1",passed=false,stage,error=error.ToString(),cases,guards,focus,activations,edits},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
}
