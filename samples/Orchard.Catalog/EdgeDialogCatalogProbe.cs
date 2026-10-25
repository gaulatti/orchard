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

internal static class EdgeDialogCatalogProbe
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
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((handle,_)=>{GetWindowThreadProcessId(handle,out var owner);visible|=owner==Environment.ProcessId&&IsWindowVisible(handle);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0&&VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot).Count==0,"Actual Drawer/Sheet catalog left its hidden boundary.");}
        async Task Layout(){for(var i=0;i<6;i++){var idle=new TaskCompletionSource();Require(host.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Could not drain catalog layout.");await idle.Task;host.Measure(new Size(900,760));host.Arrange(new Rect(0,0,900,760));host.UpdateLayout();Guard();await Task.Delay(12);}}
        FrameworkElement Surface()=>Elements(host).Where(e=>e.Name is "DrawerSurface" or "SheetSurface").Single(e=>FrameworkElementAutomationPeer.CreatePeerForElement(e).GetPattern(PatternInterface.Window)is IWindowProvider{IsTopmost:true});
        ScrollView View()=>Elements(host).OfType<ScrollView>().Single(v=>v.Name==(Surface().Name=="DrawerSurface"?"DrawerBody":"SheetBody"));
        async Task Invoke(string label){Stage("invoke:"+label);var button=Elements(host).OfType<Button>().Single(b=>FrameworkElementAutomationPeer.CreatePeerForElement(b)?.GetName()==label);((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke)).Invoke();invokes++;await Task.Delay(260);await Layout();Stage("invoked:"+label);}
        async Task Capture(string file){Stage("capture:"+file);var bitmap=new RenderTargetBitmap();var operation=bitmap.RenderAsync(host);var render=operation.AsTask();if(await Task.WhenAny(render,Task.Delay(2000))!=render){operation.Cancel();throw new TimeoutException("Actual Drawer/Sheet catalog capture exceeded two seconds at "+stage);}await render;Guard();var pixels=await bitmap.GetPixelsAsync();var bytes=new byte[checked((int)pixels.Length)];using(var reader=DataReader.FromBuffer(pixels))reader.ReadBytes(bytes);using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,bytes);await encoder.FlushAsync();stream.Seek(0);var encoded=new byte[checked((int)stream.Size)];using(var reader=new DataReader(stream)){await reader.LoadAsync((uint)encoded.Length);reader.ReadBytes(encoded);}await File.WriteAllBytesAsync(Path.Combine(folder,file),encoded);pngs.Add(new{file,sha256=Convert.ToHexStringLower(SHA256.HashData(encoded)),width=bitmap.PixelWidth,height=bitmap.PixelHeight});}
        foreach(var scheme in new[]{"Light","Dark"})
        {
            Stage(scheme+"-fixture-create");host.RequestedTheme=Enum.Parse<ElementTheme>(scheme);var fixture=new EdgeDialogsFixture();background.Children.Add(fixture);await Layout();
            await Invoke("Open bottom drawer");var drawer=(BleeckerDrawer)fixture.FindName("Drawer");var editor=Elements(host).OfType<TextBox>().Single(t=>t.Header?.ToString()=="Fictional local note"&&t.Text=="Original drawer value");editor.Text="Retained fictional catalog edit";var compact=Surface().ActualHeight;
            await Invoke("Expand or shrink drawer content");Require(Surface().ActualHeight==684&&View().ExtentHeight>View().ViewportHeight,"Actual drawer expand handler did not grow its body.");await Capture("catalog-edge-"+scheme+"-drawer-expanded.png");
            await Invoke("Expand or shrink drawer content");Require(Surface().ActualHeight==compact&&editor.IsLoaded&&editor.Text=="Retained fictional catalog edit"&&ReferenceEquals(editor,Elements(host).OfType<TextBox>().Single(t=>t.Text=="Retained fictional catalog edit")),"Actual drawer shrink lost height or original edit.");await Capture("catalog-edge-"+scheme+"-drawer-shrunk.png");
            await Invoke("Edit owned fictional settings");var ownedPopover=(BleeckerPopover)fixture.FindName("DrawerPopover");var ownedEditor=Elements(host).OfType<TextBox>().Single(t=>t.Header?.ToString()=="Retained local setting");ownedEditor.Text="Retained fictional owned setting";Require(ownedPopover.IsOpen&&ownedEditor.IsLoaded,"Actual owned Popover trigger did not open.");await Capture("catalog-edge-"+scheme+"-owned-popover.png");
            await Invoke("Open nested sheet");var nested=(BleeckerSheet)fixture.FindName("Nested");Require(nested.IsOpen&&Surface().Name=="SheetSurface","Actual nested Sheet did not open above Drawer.");await Invoke("Close");Require(!nested.IsOpen&&drawer.IsOpen&&Surface().Name=="DrawerSurface","Nested catalog close lost its parent.");Require(ownedPopover.IsOpen&&ownedEditor.IsLoaded&&ownedEditor.Text=="Retained fictional owned setting","Actual nested composition lost the owned editor.");cases.Add(new{scheme,name="drawer-owned-popover",passed=true,actualFixture=true,originalEditorRetained=true});
            ((IWindowProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Surface()).GetPattern(PatternInterface.Window)).Close();invokes++;await Task.Delay(240);await Layout();Require(!drawer.IsOpen,"Actual Drawer close handler did not accept the request.");cases.Add(new{scheme,name="drawer-live-content-and-nested-sheet",passed=true,originalEditorRetained=true,actualFixture=true});
            foreach(var side in new[]{"Left","Right","Top","Bottom"})
            {
                await Invoke(side+" sheet");var sheet=(BleeckerSheet)fixture.FindName(side=="Right"?"Sheet":side+"Sheet");Require(sheet.IsOpen&&sheet.Side==Enum.Parse<BleeckerSheetSide>(side),"Actual Sheet side handler selected the wrong component.");
                BleeckerTooltip? tip=null;
                if(side=="Right")
                {
                    var select=(BleeckerSelect)fixture.FindName("SheetSelect");var chooser=Elements(select).OfType<ItemsControl>().Single(e=>FrameworkElementAutomationPeer.CreatePeerForElement(e).GetAutomationControlType()==AutomationControlType.ComboBox);((IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(chooser).GetPattern(PatternInterface.ExpandCollapse)).Expand();await Layout();await Invoke("Fictional Beta");Require(select.Value=="b"&&((TextBlock)fixture.FindName("Status")).Text=="Caller accepted fictional choice: b","Actual owned Select handler did not accept the original option request.");tip=(BleeckerTooltip)fixture.FindName("SheetTooltip");tip.IsOpen=true;await Task.Delay(220);await Layout();Require(tip.IsLoaded&&tip.IsOpen,"Actual attached Tooltip did not open inside Sheet.");
                }
                await Capture("catalog-edge-"+scheme+"-sheet-"+side+".png");await Invoke("Close");Require(!sheet.IsOpen,"Actual Sheet close handler did not accept the request.");cases.Add(new{scheme,name="sheet-"+side,passed=true,actualFixture=true});
                if(tip is not null){Require(!tip.IsOpen&&!tip.IsLoaded,"Actual Sheet close retained its Tooltip.");cases.Add(new{scheme,name="sheet-owned-select-tooltip",passed=true,actualFixture=true,selectionAccepted=true,tooltipClosedWithOwner=true});}
            }
            await Invoke("Open sheet with its own scroll control");var own=(BleeckerSheet)fixture.FindName("OwnScroll");var original=(ScrollView)own.Content;original.ScrollTo(0,200,new ScrollingScrollOptions(ScrollingAnimationMode.Disabled,ScrollingSnapPointsMode.Ignore));await Task.Delay(80);await Layout();Require(!own.ScrollContent&&original.VerticalOffset==200&&View().VerticalOffset==0,"Original caller ScrollView did not own scrolling.");await Invoke("Close");Require(!own.IsOpen,"Scroll-owning Sheet close was refused by its actual handler.");cases.Add(new{scheme,name="caller-owned-scroll",passed=true,actualFixture=true});
            background.Children.Remove(fixture);await Layout();
        }

        string Hash(string p)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-edge-dialog-catalog-v1",passed=true,cases,pngs,guards,focus,activations,softwareNativeInvokes=invokes,librarySha256=Hash(typeof(BleeckerDrawer).Assembly.Location),hostSha256=Hash(typeof(App).Assembly.Location),activateCalled=false,showCalled=false,focusCalled=false,hardwareInputSent=false,nativeComponentAccepted=false},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
    }
}
