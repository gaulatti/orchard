using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Catalog;

internal static class ContextMenuCatalogProbe
{
    private delegate bool Visitor(nint handle,nint parameter);
    [DllImport("user32.dll")]private static extern bool EnumWindows(Visitor visitor,nint parameter);
    [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    {
        if(node is FrameworkElement e)yield return e;
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;
    }
    internal static async Task RunAsync(Window window,string output)
    {
        var folder=Path.GetDirectoryName(output)!;var fixture=new ContextMenusFixture();var root=new Grid{Width=900,Height=760,Padding=new Thickness(40)};root.Children.Add(fixture);window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,760));var guards=0;var focus=0;var activations=0;var invokes=0;var cases=new List<object>();var pngs=new List<object>();var stage="initial";BleeckerContextMenu? active=null;
        root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Require(bool valid,string message){if(!valid)throw new InvalidOperationException(message);}
        void Guard()
        {
            guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);
            Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Context Menu catalog left its hidden boundary.");
        }
        async Task Layout()
        {
            for(var i=0;i<6;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Could not drain context catalog layout.");await idle.Task;root.Measure(new Size(900,760));root.Arrange(new Rect(0,0,900,760));root.UpdateLayout();Guard();await Task.Delay(12);}
        }
        async Task Capture(string file,FrameworkElement element)
        {
            Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(element);Guard();var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);await File.WriteAllBytesAsync(Path.Combine(folder,file),bytes);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});Guard();
        }
        try
        {
            foreach(var scheme in new[]{"Light","Dark"})foreach(var modal in new[]{true,false})
            {
                stage=$"{scheme}-{modal}";root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);await Layout();var target=Elements(fixture).OfType<Button>().Single(b=>b.Name=="Target");var status=Elements(fixture).OfType<TextBlock>().Single(b=>b.Name=="Status");var menu=(BleeckerContextMenu)target.ContextFlyout;active=menu;var mode=Elements(fixture).OfType<ToggleSwitch>().Single(s=>s.Name=="ModalMode");mode.IsOn=modal;await Layout();Require(menu.IsModal==modal,"Actual catalog modality switch lost its public binding.");var original=menu.Items.Cast<MenuFlyoutItem>().ToArray();var composed=original.OfType<BleeckerMenuContentItem>().ToDictionary(i=>i,i=>i.Content);Require(original.Length==5&&composed.Count==2,"Catalog lost its ordinary, inset or composed actions.");status.Text="No fictional context action yet";
                foreach(var item in original.Where(i=>i.IsEnabled))
                {
                    ((FlyoutBase)menu).ShowAt(target,new FlyoutShowOptions{Position=new Point(80,20),ShowMode=FlyoutShowMode.Transient});await Layout();Require(menu.IsOpen&&original.All(i=>i.IsLoaded)&&original.SequenceEqual(menu.Items.Cast<MenuFlyoutItem>()),"Catalog replaced original native menu actions.");var peer=FrameworkElementAutomationPeer.CreatePeerForElement(item);Require(peer.GetName()==item.Text,"Composed content lost its native accessible text.");if(item==original[0]){var openElements=VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).SelectMany(p=>Elements(p.Child)).ToArray();Require(openElements.Any(e=>e.Name=="BleeckerContextMenuPortal")==!modal,"Actual catalog did not use the selected native projection mode.");await Capture($"context-catalog-{scheme}-{modal}-open.png",openElements.OfType<BleeckerOverlaySurface>().Single());}
                    ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();invokes++;await Layout();Require(!menu.IsOpen&&status.Text=="Selected: "+item.Text,"Actual catalog action did not dismiss and update its visible status.");Require(composed.All(p=>ReferenceEquals(p.Key.Content,p.Value)),"Native invocation replaced caller composed content.");
                }
                await Capture($"context-catalog-{scheme}-{modal}-changed.png",root);cases.Add(new{scheme,modal,modalitySwitchBindingVerified=true,selectedProjectionVerified=true,passed=true,actualFixtureActions=5,enabledActionsInvoked=4,composedContentRetained=true,nativeActionsUpdateStatus=true,nativeDismissalVerified=true,originalFixtureRetained=ReferenceEquals(root.Children.Single(),fixture),finalStatus=status.Text});active=null;
            }
            string Hash(string p)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-context-menu-catalog-v2",passed=true,cases,pngs,guards,focus,activations,invokes,activateCalled=false,showWindowCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,nativeComponentAccepted=false,librarySha256=Hash(typeof(BleeckerContextMenu).Assembly.Location),hostSha256=Hash(typeof(App).Assembly.Location)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){active?.Hide();await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-context-menu-catalog-v2",passed=false,stage,error=error.ToString(),cases,guards,focus,activations,invokes},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
}
