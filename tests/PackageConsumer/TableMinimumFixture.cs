using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
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

// Shared by the source catalog and independent package consumer. These cases
// exercise the public minimum-width boundary, including caller layout code.
internal static class TableMinimumFixture
{
    private delegate bool Visitor(nint handle,nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor,nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    {
        if(node is FrameworkElement element)yield return element;
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;
    }
    internal static async Task RunAsync(Window window,string output,string hostAssembly)
    {
        var root=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='1000' Height='760' Background='{ThemeResource BleeckerBackgroundBrush}'/>");
        window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1000,760));
        var states=new List<object>();var pngs=new List<object>();var guards=0;var focus=0;var activations=0;var stage="initial";
        root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,e)=>{if(e.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Guard()
        {
            guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;
            EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var process);visible|=process==Environment.ProcessId&&IsWindowVisible(h);return true;},0);
            Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Minimum-width fixture left its hidden boundary.");
        }
        async Task Layout()
        {
            for(var i=0;i<6;i++)
            {
                var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Dispatcher refused minimum-width layout.");await idle.Task;
                root.Measure(new Size(1000,760));root.Arrange(new Rect(0,0,1000,760));root.UpdateLayout();Guard();await Task.Delay(12);
            }
        }
        async Task Capture(string file)
        {
            var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();
            var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());
            using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);
            using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);
            await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),bytes);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});
        }
        try
        {
            foreach(var scheme in new[]{"Light","Dark"})
            {
                root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);
                async Task<(BleeckerTable Table,BleeckerTableCell Cell)> Mount(FrameworkElement content,ControlTemplate? template=null)
                {
                    root.Children.Clear();await Layout();var cell=new BleeckerTableCell{Content=content};if(template is not null)cell.Template=template;
                    var row=new BleeckerTableRow();row.Cells.Add(cell);var body=new BleeckerTableBody();body.Children.Add(row);
                    var table=new BleeckerTable{Width=240,Margin=new Thickness(20),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top};table.Sections.Add(body);root.Children.Add(table);await Layout();
                    return(table,cell);
                }
                async Task Record(string name,BleeckerTable table,BleeckerTableCell cell,FrameworkElement content,double expectedWidth,ScrollViewer? viewport=null,WidthSensitivePanel? caller=null,ControlTemplate? template=null)
                {
                    stage=scheme+"-"+name;await Layout();Require(Math.Abs(cell.ActualWidth-expectedWidth)<=.05,"Shared cell minimum differs: "+stage+" "+cell.ActualWidth+" vs "+expectedWidth);
                    var overflow=expectedWidth>240;Require(overflow?table.ScrollableWidth>expectedWidth-241:table.ScrollableWidth<=.05,"Outer overflow differs: "+stage);
                    Require(ReferenceEquals(cell.Content,content)&&Elements(cell).Any(e=>ReferenceEquals(e,content))&&content.IsLoaded,"Caller content was replaced or detached.");
                    if(viewport is not null)Require(viewport.ScrollableWidth>0&&viewport.Content is TextBlock,"Scroll viewport stopped containing its original overflowing text.");
                    if(caller is not null)Require(caller.ZeroWidthMeasures>0&&caller.MinWidth==caller.RequestedMinimum,"Caller zero-width measure contract was lost.");
                    if(template is not null)Require(ReferenceEquals(cell.Template,template),"Caller cell template was replaced.");
                    var file="table-minimum-"+scheme+"-"+name+".png";await Capture(file);
                    var types=Elements(cell).GroupBy(e=>e.GetType().FullName!).OrderBy(g=>g.Key,StringComparer.Ordinal).Select(g=>new{type=g.Key,count=g.Count()}).ToArray();
                    states.Add(new{scheme,name,expectedWidth,cellWidth=cell.ActualWidth,scrollableWidth=table.ScrollableWidth,expectedOverflow=overflow,originalContentRetained=ReferenceEquals(cell.Content,content),originalContentLoaded=content.IsLoaded,viewportScrollableWidth=viewport?.ScrollableWidth,callerMinimum=caller?.MinWidth,callerZeroMeasures=caller?.ZeroWidthMeasures,callerTemplateRetained=template is null||ReferenceEquals(cell.Template,template),types,file});
                }
                var label=new TextBlock{Text="Harbor Reed",TextWrapping=TextWrapping.WrapWholeWords};var plain=await Mount(label);await Record("plain-rich",plain.Table,plain.Cell,label,240);
                var clicks=0;var action=new BleeckerButton{Variant=BleeckerButtonVariant.Ghost,Content="Open"};action.Click+=(_,_)=>clicks++;var button=await Mount(action);
                var nativeButton=Elements(action).OfType<Button>().Single();((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(nativeButton).GetPattern(PatternInterface.Invoke)).Invoke();Require(clicks==1,"Original action stopped invoking.");await Record("original-action",button.Table,button.Cell,action,240);
                var checkbox=new BleeckerCheckbox{Width=20,Height=20};var check=await Mount(checkbox);await Record("fixed-checkbox",check.Table,check.Cell,checkbox,240);
                var text=new TextBlock{Text=new string('M',160),TextWrapping=TextWrapping.NoWrap};
                var scroll=new ScrollViewer{Content=text,HorizontalScrollMode=ScrollMode.Enabled,HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,VerticalScrollMode=ScrollMode.Disabled,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled};
                var scrolling=await Mount(scroll);await Record("scroll-auto",scrolling.Table,scrolling.Cell,scroll,240,scroll);
                scroll.Width=140;await Record("scroll-fixed",scrolling.Table,scrolling.Cell,scroll,240,scroll);
                scroll.Width=double.NaN;await Record("scroll-restored-auto",scrolling.Table,scrolling.Cell,scroll,240,scroll);
                scroll.MinWidth=360;await Record("scroll-minimum",scrolling.Table,scrolling.Cell,scroll,392,scroll);
                scroll.MinWidth=0;await Record("scroll-restored-minimum",scrolling.Table,scrolling.Cell,scroll,240,scroll);
                var caller=new WidthSensitivePanel();var owned=await Mount(caller);await Record("caller-override-initial",owned.Table,owned.Cell,caller,372,caller:caller);
                caller.Reset(420);await Record("caller-override-grown",owned.Table,owned.Cell,caller,452,caller:caller);
                caller.Reset(160);await Record("caller-override-shrunk",owned.Table,owned.Cell,caller,240,caller:caller);
                var template=(ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:local='using:Orchard' TargetType='local:BleeckerTableCell'><Border Padding='3'><ContentPresenter Content='{TemplateBinding Content}'/></Border></ControlTemplate>");
                var replacement=new WidthSensitivePanel();var custom=await Mount(replacement,template);await Record("caller-template",custom.Table,custom.Cell,replacement,346,caller:replacement,template:template);
                root.Children.Clear();await Layout();Require(!replacement.IsLoaded&&!custom.Table.IsLoaded,"Retired caller presentation remained loaded.");
            }
            Guard();await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-table-minimum-native-v1",passed=true,states,pngs,guards,focus,activations,activateCalled=false,showWindowCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false,publicApisOnly=true,librarySha256=Hash(typeof(BleeckerTable).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-table-minimum-native-v1",passed=false,stage,error=error.ToString(),states,pngs,guards,focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
    private sealed class WidthSensitivePanel : Panel
    {
        internal int ZeroWidthMeasures{get;private set;}
        internal double RequestedMinimum{get;private set;}=340;
        internal void Reset(double minimum){RequestedMinimum=minimum;MinWidth=0;InvalidateMeasure();}
        protected override Size MeasureOverride(Size available)
        {
            if(available.Width==0){ZeroWidthMeasures++;MinWidth=RequestedMinimum;}
            return new Size(40,24);
        }
        protected override Size ArrangeOverride(Size final)=>final;
    }
}
