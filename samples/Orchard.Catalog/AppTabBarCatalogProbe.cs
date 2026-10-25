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

internal static class AppTabBarCatalogProbe
{
    private delegate bool Visitor(nint handle,nint parameter);
    [DllImport("user32.dll")]private static extern bool EnumWindows(Visitor visitor,nint parameter);
    [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    internal static async Task RunAsync(Window window,string output)
    {
        var folder=Path.GetDirectoryName(output)!;Directory.CreateDirectory(folder);var fixture=new AppTabBarsFixture();var root=new Grid{Width=900,Height=760,Padding=new Thickness(40)};root.Children.Add(fixture);window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,760));var guards=0;var focus=0;var activations=0;var invokes=0;var selections=0;var cases=new List<object>();var pngs=new List<object>();var stage="initial";
        root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Require(bool valid,string message){if(!valid)throw new InvalidOperationException(message);}
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0&&VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count==0,"Actual App Tab Bar catalog left its hidden boundary.");}
        async Task Layout(){for(var i=0;i<5;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Could not drain catalog layout.");await idle.Task;root.Measure(new Size(900,760));root.Arrange(new Rect(0,0,900,760));root.UpdateLayout();Guard();await Task.Delay(12);}}
        Button[] Buttons(BleeckerAppTabBar group)=>Elements(group).OfType<Button>().Where(b=>b.DataContext is BleeckerAppTab).ToArray();
        async Task Invoke(Button target){Guard();((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(target).GetPattern(PatternInterface.Invoke)).Invoke();invokes++;await Layout();Guard();}
        async Task Select(Button target){Guard();((ISelectionItemProvider)FrameworkElementAutomationPeer.CreatePeerForElement(target).GetPattern(PatternInterface.SelectionItem)).Select();selections++;await Layout();Guard();}
        async Task Capture(string file){Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(fixture);Guard();var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);await File.WriteAllBytesAsync(Path.Combine(folder,file),bytes);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});Guard();}
        try
        {
            foreach(var scheme in new[]{"Light","Dark"})
            {
                stage=scheme;root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);await Layout();var groups=Elements(fixture).OfType<BleeckerAppTabBar>().ToArray();Require(groups.Length==5&&groups.All(group=>group.ActualTheme==root.RequestedTheme),"Actual catalog lost controls or theme.");var sample=groups.Single(group=>group.Name=="Sample");var typed=groups.Single(group=>group.Name=="TypedSample");var narrow=groups.Single(group=>group.Name=="NarrowSample");var disabled=groups.Single(group=>group.Name=="DisabledSample");var large=groups.Single(group=>group.Name=="LargeSample");var status=Elements(fixture).OfType<TextBlock>().Single(text=>text.Name=="Status");Require(Equals(sample.SelectedValue,"overview")&&status.Text=="Selected: overview"&&typed.SelectedValue?.GetType().IsEnum==true&&narrow.Width==160&&Equals(narrow.SelectedValue,1)&&!disabled.IsEnabled&&Buttons(disabled).All(button=>!button.IsEnabled)&&large.FontSize==20,"Actual catalog typed, disabled, constrained or enlarged defaults failed.");var tracks=groups.Select(group=>{var items=Buttons(group);foreach(var item in items){var bounds=item.TransformToVisual(root).TransformBounds(new Rect(0,0,item.ActualWidth,item.ActualHeight));var icon=Elements(item).OfType<FontIcon>().Single();var glyph=icon.TransformToVisual(root).TransformBounds(new Rect(0,0,icon.ActualWidth,icon.ActualHeight));Require(Math.Abs(bounds.Width-group.ActualWidth/items.Length)<.001&&Math.Abs(glyph.X+glyph.Width/2-bounds.X-bounds.Width/2)<.001,"Actual catalog destination did not fill or center in its equal track.");}return new{name=group.Name,width=group.ActualWidth,count=items.Length,trackWidth=items[0].ActualWidth,iconsCentered=true};}).ToArray();await Capture($"catalog-app-tab-bar-{scheme}-initial.png");var buttons=Buttons(sample);await Invoke(buttons[1]);Require(Equals(sample.SelectedValue,"queue")&&status.Text=="Selected: queue","Actual native activation did not update the catalog status.");await Invoke(buttons[1]);Require(Equals(sample.SelectedValue,"queue")&&status.Text=="Selected: queue","Actual catalog reselect cleared or changed the value.");await Select(Buttons(typed)[0]);Require(typed.SelectedValue?.GetType().IsEnum==true&&Equals(typed.SelectedValue,typed.Tabs[0].Value),"Actual typed catalog selection lost its enum type.");await Capture($"catalog-app-tab-bar-{scheme}-changed.png");await Select(buttons[0]);Require(Equals(sample.SelectedValue,"overview")&&status.Text=="Selected: overview","Actual SelectionItem action did not update the catalog status.");cases.Add(new{scheme,passed=true,actualFixtureControls=5,tracks,statusTracksNativeActivation=true,reselectPreservesValue=true,statusTracksSelectionProvider=true,typedEnumPreserved=true,disabledAndConstrainedAndLargeExamples=true,originalFixtureRetained=ReferenceEquals(root.Children.Single(),fixture)});
            }
            string Hash(string p)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-app-tab-bar-catalog-v1",passed=true,cases,pngs,guards,focus,activations,invokes,selections,activateCalled=false,showCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,nativeComponentAccepted=false,librarySha256=Hash(typeof(BleeckerAppTabBar).Assembly.Location),hostSha256=Hash(typeof(App).Assembly.Location)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-app-tab-bar-catalog-v1",passed=false,stage,error=error.ToString(),cases,guards,focus,activations,invokes,selections},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
}
