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

internal static class SecureFieldCatalogProbe
{
    private delegate bool Visitor(nint handle,nint parameter);
    [DllImport("user32.dll")]private static extern bool EnumWindows(Visitor visitor,nint parameter);
    [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    internal static async Task RunAsync(Window window,string output)
    {
        var folder=Path.GetDirectoryName(output)!;Directory.CreateDirectory(folder);var fixture=new SecureFieldsFixture();var root=new Grid{Width=720,Height=760,Padding=new Thickness(40)};root.Children.Add(fixture);window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(720,760));var guards=0;var focus=0;var activations=0;var invokes=0;var cases=new List<object>();var pngs=new List<object>();var stage="initial";
        root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0&&VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count==0,"Actual Secure Field catalog left its hidden boundary.");}
        async Task Layout(){for(var i=0;i<5;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Could not drain actual catalog.");await idle.Task;root.Measure(new Size(720,760));root.Arrange(new Rect(0,0,720,760));root.UpdateLayout();Guard();await Task.Delay(12);}}
        async Task Invoke(FrameworkElement target)
        {
            Guard();
            // BleeckerButton is a composite; invoke its real native action.
            if(target is BleeckerButton)target=Elements(target).OfType<Button>().Single(button=>button.Name=="PART_Action");
            var peer=FrameworkElementAutomationPeer.CreatePeerForElement(target);
            Require(peer?.GetPattern(PatternInterface.Invoke)is IInvokeProvider,"Actual catalog action has no native Invoke provider.");
            ((IInvokeProvider)peer!.GetPattern(PatternInterface.Invoke)).Invoke();invokes++;await Layout();Guard();
        }
        async Task Capture(string file){Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(fixture);Guard();var bytes=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,bytes);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var encoded=new byte[(int)stream.Size];reader.ReadBytes(encoded);await File.WriteAllBytesAsync(Path.Combine(folder,file),encoded);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(encoded))});Guard();}
        try
        {
            foreach(var scheme in new[]{"Light","Dark"})
            {
                stage=scheme;root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);await Layout();var fields=Elements(fixture).OfType<BleeckerSecureField>().ToArray();Require(fields.Length==4&&fields.All(f=>f.ActualTheme==root.RequestedTheme),"Actual fixture lost controls or theme.");var sample=fields.Single(f=>f.Name=="Sample");var editor=Elements(sample).OfType<PasswordBox>().Single();var status=Elements(fixture).OfType<TextBlock>().Single(t=>t.Name=="Status");var reveal=Elements(sample).OfType<BleeckerIconButton>().Single();var clear=Elements(fixture).OfType<BleeckerButton>().Single(b=>Equals(b.Content,"Clear sample"));Require(sample.Password.Length==0&&status.Text=="Value length: 0"&&!sample.IsRevealed&&fields.Count(f=>!f.IsEnabled)==1&&fields.Any(f=>f.Width==160)&&fields.Any(f=>f.FontSize==28&&f.ActualHeight>=40)&&AutomationProperties.GetLabeledBy(editor)is not null,"Actual catalog defaults, scaling or label association failed.");await Capture($"catalog-secure-{scheme}-initial.png");editor.Password="Fictional catalog value";await Layout();var length=editor.Password.Length;Require(length>0&&sample.Password==editor.Password&&status.Text==$"Value length: {length}","Actual catalog counter did not track the native value.");await Invoke(reveal);Require(sample.IsRevealed&&editor.PasswordRevealMode==PasswordRevealMode.Visible&&status.Text==$"Value length: {length}","Actual reveal changed the counter or failed to reveal.");await Capture($"catalog-secure-{scheme}-revealed.png");await Invoke(clear);Require(sample.Password.Length==0&&editor.Password.Length==0&&sample.IsRevealed&&status.Text=="Value length: 0","Actual clear did not clear the native value/counter while retaining reveal state.");await Invoke(reveal);Require(!sample.IsRevealed&&editor.PasswordRevealMode==PasswordRevealMode.Hidden,"Actual final conceal failed.");cases.Add(new{scheme,passed=true,actualFixtureControls=4,originalFixtureRetained=ReferenceEquals(root.Children.Single(),fixture),counterTracksNativeValue=true,revealPreservesCounter=true,clearPreservesRevealState=true,finalConcealed=true,labelForwarded=true,disabledAndConstrainedAndLargeExamples=true});
            }
            string Hash(string p)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-secure-field-catalog-v1",passed=true,cases,pngs,guards,focus,activations,invokes,passwordContentsLogged=false,activateCalled=false,showCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,nativeComponentAccepted=false,librarySha256=Hash(typeof(BleeckerSecureField).Assembly.Location),hostSha256=Hash(typeof(App).Assembly.Location)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-secure-field-catalog-v1",passed=false,stage,error=error.GetType().Name+": "+error.Message,cases,guards,focus,activations,invokes},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
}
