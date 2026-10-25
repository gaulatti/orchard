using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Shape = Microsoft.UI.Xaml.Shapes.Shape;
using ShapePath = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard.Testing;

// Public-control regression shared by the catalog and independent consumer.
internal static class TableNavigationForegroundFixture
{
    private delegate bool Visitor(nint handle,nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor,nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    private static (FrameworkElement Source,Brush Foreground) ForegroundSource(ShapePath path,Button owner)
    {
        for(var parent=VisualTreeHelper.GetParent(path);parent is not null&&!ReferenceEquals(parent,owner);parent=VisualTreeHelper.GetParent(parent))if(parent is ContentPresenter p)return(p,p.Foreground);
        Require(Elements(owner).Any(e=>ReferenceEquals(e,path)),"Navigation glyph lost its native button.");return(owner,owner.Foreground);
    }
    private static object Paint(Brush brush){Require(brush is SolidColorBrush,"Navigation foreground is not a solid brush.");var b=(SolidColorBrush)brush;return new{rgba=new[]{(int)b.Color.R,b.Color.G,b.Color.B,b.Color.A},opacity=b.Opacity};}
    private static bool PaintEquals(Brush a,Brush b)=>a is SolidColorBrush x&&b is SolidColorBrush y&&x.Color==y.Color&&x.Opacity==y.Opacity;
    internal static async Task RunAsync(Window window,string output,string hostAssembly)
    {
        var root=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='1000' Height='1100' Background='{ThemeResource BleeckerBackgroundBrush}'/>");
        window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1000,1100));
        var states=new List<object>();var pngs=new List<object>();var failures=new List<string>();var guards=0;var focus=0;var activations=0;var stage="initial";
        root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,e)=>{if(e.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var id);visible|=id==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Navigation foreground fixture left its hidden boundary.");}
        async Task Layout(){for(var i=0;i<6;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Dispatcher refused navigation layout.");await idle.Task;root.Measure(new Size(1000,1100));root.Arrange(new Rect(0,0,1000,1100));root.UpdateLayout();Guard();await Task.Delay(12);}}
        object Box(FrameworkElement e){var b=e.TransformToVisual(root).TransformBounds(new Rect(0,0,e.ActualWidth,e.ActualHeight));return new{x=b.X,y=b.Y,width=b.Width,height=b.Height};}
        async Task Capture(string file){var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());GC.KeepAlive(bitmap);using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),bytes);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});}
        try
        {
            foreach(var scheme in new[]{"Light","Dark"})foreach(var kind in new[]{"data-table","pagination"})
            {
                root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);
                var records=Enumerable.Range(0,25).Select(i=>new Dictionary<string,object?>{{"id",i},{"name","Fictional row "+i}}).ToArray();
                var definition=new BleeckerDataTableColumn(accessorKey:"name",header:"Name");BleeckerDataTableModel model;Control control;Action<Dictionary<string,object?>[]> setData;
                if(kind=="data-table"){var data=new BleeckerDataTable();data.Columns.Add(definition);data.ItemsSource=records;model=data.Model;control=data;setData=items=>data.ItemsSource=items;}
                else{model=new BleeckerDataTableModel();model.SetColumns([definition]);model.SetData(records);control=new BleeckerDataTablePagination{Table=model};setData=items=>model.SetData(items);}
                control.Width=660;control.Margin=new Thickness(20);control.HorizontalAlignment=HorizontalAlignment.Left;control.VerticalAlignment=VerticalAlignment.Top;root.Children.Add(control);await Layout();model.SetPageSize(5);await Layout();
                string[] Names()=>kind=="data-table"?["PART_Previous","PART_Next"]:["PART_First","PART_Previous","PART_Next","PART_Last"];
                Button[] Buttons()=>Names().Select(name=>Elements(control).OfType<Button>().Single(b=>b.Name==name)).ToArray();
                ShapePath Glyph(Button b)=>Elements(b).OfType<ShapePath>().Single();
                async Task Record(string name,bool callerTemplate=false,bool callerBindingExpected=false,bool directContentTemplate=false)
                {
                    stage=scheme+"-"+kind+"-"+name;await Layout();await Task.Delay(TimeSpan.FromMilliseconds(BleeckerTokens.Durations["control"]+32));Guard();
                    var buttons=Buttons();Require(buttons.Length==Names().Length&&buttons.All(b=>b.IsLoaded&&b.Visibility==Visibility.Visible),"Navigation buttons were not mounted.");
                    var observations=buttons.Select(button=>{var path=Glyph(button);var producer=ForegroundSource(path,button);var view=Elements(button).OfType<Viewbox>().Single();var expectedEnabled=control.IsEnabled&&(button.Name is "PART_Previous" or "PART_First"?model.CanPreviousPage:model.CanNextPage);Require(button.IsEnabled==expectedEnabled,"Public source availability did not reach native navigation.");var same=PaintEquals(path.Stroke,producer.Foreground);if(!same)failures.Add(stage+"/"+button.Name+" foreground");return new{name=button.Name,enabled=button.IsEnabled,expectedEnabled,brushMatches=same,brushReferenceMatches=ReferenceEquals(path.Stroke,producer.Foreground),foreground=Paint(producer.Foreground),stroke=Paint(path.Stroke),glyph=Box(view),button=Box(button),presenterName=producer.Source.Name,producerKind=producer.Source is ContentPresenter?"content-presenter":"button",callerForegroundBindingRetained=button.GetBindingExpression(Control.ForegroundProperty)is not null};}).ToArray();
                    if(callerBindingExpected)Require(observations[0].callerForegroundBindingRetained,"Caller foreground binding was removed.");
                    var file="table-navigation-foreground-"+stage+".png";await Capture(file);states.Add(new{scheme,kind,name,actualTheme=root.ActualTheme.ToString(),pageIndex=model.PageIndex,pageCount=model.PageCount,hostEnabled=control.IsEnabled,callerTemplate,callerBindingExpected,directContentTemplate,loaded=true,buttons=observations,file});
                }
                async Task Detached(string name,ShapePath[] retired)
                {
                    stage=scheme+"-"+kind+"-"+name;await Layout();var released=retired.All(p=>!p.IsLoaded&&p.GetBindingExpression(Shape.StrokeProperty)is null);if(!released)failures.Add(stage+" obsolete presenter binding");states.Add(new{scheme,kind,name,loaded=false,retiredGlyphs=retired.Length,retiredGlyphsUnloaded=retired.All(p=>!p.IsLoaded),ownedBindingsReleased=released});
                }
                model.SetPageIndex(0);await Record("first");model.SetPageIndex(1);await Record("middle");model.SetPageIndex(model.PageCount-1);await Record("last");
                setData(records.Take(3).ToArray());await Record("single-page");setData(records);model.SetPageIndex(1);await Record("source-restored");
                control.IsEnabled=false;await Record("host-disabled");control.IsEnabled=true;await Record("host-enabled");
                root.RequestedTheme=scheme=="Light"?ElementTheme.Dark:ElementTheme.Light;await Record("live-theme");root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);await Record("theme-restored");
                var retired=Buttons().Select(Glyph).ToArray();root.Children.Remove(control);await Detached("unloaded",retired);root.Children.Add(control);await Layout();model.SetPageIndex(1);await Record("reloaded");
                retired=Buttons().Select(Glyph).ToArray();var originalTemplate=control.Template;control.Template=null;await Detached("template-cleared",retired);control.Template=originalTemplate;await Layout();model.SetPageIndex(1);await Record("template-restored");
                var first=Buttons()[0];var buttonTemplate=first.Template;
                first.Template=(ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'><ContentPresenter x:Name='CallerPresenter' Content='{TemplateBinding Content}' Foreground='#FF336699' HorizontalContentAlignment='Center' VerticalContentAlignment='Center'/></ControlTemplate>");await Record("caller-template",callerTemplate:true);
                var oldPresenter=(ContentPresenter)ForegroundSource(Glyph(first),first).Source;oldPresenter.Foreground=new SolidColorBrush(Windows.UI.Color.FromArgb(255,220,77,55));await Record("caller-presenter-change",callerTemplate:true);
                first.Template=buttonTemplate;await Record("button-template-restored");oldPresenter.Foreground=new SolidColorBrush(Windows.UI.Color.FromArgb(255,255,0,255));await Record("retired-presenter-change");
                var direct=(Viewbox)first.Content;first.Content=null;first.Template=(ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'><Border x:Name='DirectHost'/></ControlTemplate>");await Layout();var directHost=Elements(first).OfType<Border>().Single(b=>b.Name=="DirectHost");directHost.Child=direct;await Record("direct-content-template",callerTemplate:true,directContentTemplate:true);
                first.Foreground=new SolidColorBrush(Windows.UI.Color.FromArgb(255,55,99,144));await Record("direct-button-change",callerTemplate:true,directContentTemplate:true);
                directHost.Child=null;first.Template=buttonTemplate;first.Content=direct;await Record("direct-template-restored");
                var producer=new Border{Tag=new SolidColorBrush(Windows.UI.Color.FromArgb(255,71,111,151))};first.SetBinding(Control.ForegroundProperty,new Binding{Source=producer,Path=new PropertyPath("Tag")});await Record("caller-foreground",callerBindingExpected:true);
                producer.Tag=new SolidColorBrush(Windows.UI.Color.FromArgb(255,90,125,160));await Record("caller-foreground-change",callerBindingExpected:true);
                control.IsEnabled=false;await Record("caller-bound-disabled",callerBindingExpected:true);control.IsEnabled=true;await Record("caller-bound-restored",callerBindingExpected:true);
                root.Children.Remove(control);await Layout();
            }
            Guard();var passed=failures.Count==0;await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-table-navigation-foreground-native-v1",passed,executionCompleted=true,states,pngs,failures,guards,focus,activations,activateCalled=false,showWindowCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false,publicApisOnly=true,librarySha256=Hash(typeof(BleeckerDataTable).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(passed?0:1);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-table-navigation-foreground-native-v1",passed=false,executionCompleted=false,stage,error=error.ToString(),states,pngs,failures,guards,focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
}
