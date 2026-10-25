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
using Shape = Microsoft.UI.Xaml.Shapes.Shape;

namespace Orchard.Testing;

// Actual owned captions and vectors, shared by catalog and package consumers.
internal static class TableButtonContentForegroundFixture
{
    private delegate bool Visitor(nint handle,nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor,nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    private static ContentPresenter Producer(FrameworkElement node,Button button)
    {
        for(var parent=VisualTreeHelper.GetParent(node);parent is not null&&!ReferenceEquals(parent,button);parent=VisualTreeHelper.GetParent(parent))if(parent is ContentPresenter p)return p;
        throw new InvalidOperationException("Owned button content has no rendered presenter.");
    }
    private static object Paint(Brush brush){Require(brush is SolidColorBrush,"Expected solid foreground.");var b=(SolidColorBrush)brush;return new{rgba=new[]{(int)b.Color.R,b.Color.G,b.Color.B,b.Color.A},opacity=b.Opacity};}
    private static bool Same(Brush a,Brush b)=>a is SolidColorBrush x&&b is SolidColorBrush y&&x.Color==y.Color&&x.Opacity==y.Opacity;
    internal static async Task RunAsync(Window window,string output,string hostAssembly)
    {
        var root=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='1000' Height='240' Background='{ThemeResource BleeckerBackgroundBrush}'/>");
        window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1000,240));
        var states=new List<object>();var pngs=new List<object>();var failures=new List<string>();var guards=0;var focus=0;var activations=0;var stage="initial";
        root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,e)=>{if(e.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var process);visible|=process==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Hidden foreground/input guard failed.");}
        async Task Layout(){for(var i=0;i<3;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Dispatcher refused layout.");await idle.Task;root.Measure(new Size(1000,240));root.Arrange(new Rect(0,0,1000,240));root.UpdateLayout();Guard();}}
        object Box(FrameworkElement e){var r=e.TransformToVisual(root).TransformBounds(new Rect(0,0,e.ActualWidth,e.ActualHeight));return new{x=r.X,y=r.Y,width=r.Width,height=r.Height};}
        async Task Capture(string file){Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),bytes);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});Guard();}
        try
        {
            foreach(var scheme in new[]{"Light","Dark"})foreach(var kind in new[]{"toolbar","column-toggle"})
            {
                root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);
                var model=new BleeckerDataTableModel();model.SetColumns([new BleeckerDataTableColumn(accessorKey:"name",header:"Name")]);model.SetData(new[]{new Dictionary<string,object?>{{"name","Fictional row"}}});model.GetColumn("name")!.SetFilterValue("Fictional");
                Control control=kind=="toolbar"?new BleeckerDataTableToolbar{Table=model,SearchColumn="name"}:new BleeckerDataTableColumnToggle{Table=model};
                control.Width=660;control.Margin=new Thickness(20);control.HorizontalAlignment=HorizontalAlignment.Left;control.VerticalAlignment=VerticalAlignment.Top;root.Children.Add(control);await Layout();
                var button=Elements(control).OfType<Button>().Single(b=>b.Name==(kind=="toolbar"?"PART_Reset":"PART_Trigger"));
                var content=(FrameworkElement)button.Content;var caption=Elements(content).OfType<TextBlock>().Single(t=>t.Text==(kind=="toolbar"?"Reset":"Columns"));var glyph=Elements(content).OfType<Viewbox>().Single();var shapes=Elements(glyph).OfType<Shape>().ToArray();
                Require(shapes.Length==(kind=="toolbar"?1:3)&&caption.Text.Length>0,"Owned content missing.");
                async Task Record(string name)
                {
                    stage=scheme+"-"+kind+"-"+name;await Layout();await Task.Delay(TimeSpan.FromMilliseconds(BleeckerTokens.Durations["control"]+32));Guard();
                    Require(ReferenceEquals(button.Content,content)&&button.IsLoaded&&caption.IsLoaded&&shapes.All(s=>s.IsLoaded)&&button.IsEnabled==control.IsEnabled,"Native button availability/ownership changed.");
                    var presenter=Producer(caption,button);Require(ReferenceEquals(presenter,Producer(glyph,button)),"Caption/vector producer differs.");
                    var observations=new List<object>();
                    void Observe(string role,Brush actual){var same=Same(actual,presenter.Foreground);if(!same)failures.Add(stage+"/"+role+" foreground");observations.Add(new{role,foreground=Paint(presenter.Foreground),actual=actual is null?null:Paint(actual),brushMatches=same});}
                    Observe("caption",caption.Foreground);for(var i=0;i<shapes.Length;i++)Observe("glyph-"+i,shapes[i].Stroke);
                    Require(caption.GetBindingExpression(TextBlock.FontFamilyProperty)is not null,"Owned font binding lost.");
                    var file="table-button-content-foreground-"+stage+".png";await Capture(file);
                    states.Add(new{scheme,kind,name,loaded=true,actualTheme=root.ActualTheme.ToString(),enabled=button.IsEnabled,presenterName=presenter.Name,caption=caption.Text,captionBox=Box(caption),glyph=Box(glyph),button=Box(button),observations,fontBindingRetained=true,file});
                }
                await Record("normal");control.IsEnabled=false;await Record("disabled");control.IsEnabled=true;await Record("reenabled");
                var originalTemplate=button.Template;button.Template=(ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'><Border Background='{TemplateBinding Background}' Padding='{TemplateBinding Padding}'><ContentPresenter x:Name='CallerPresenter' Content='{TemplateBinding Content}' Foreground='#FF9170B8' HorizontalContentAlignment='{TemplateBinding HorizontalContentAlignment}' VerticalContentAlignment='{TemplateBinding VerticalContentAlignment}'/></Border></ControlTemplate>");var callerTemplate=button.Template;for(var cycle=0;cycle<3;cycle++){button.Template=originalTemplate;await Layout();button.Template=callerTemplate;await Layout();}await Record("caller-presenter");
                button.Template=originalTemplate;await Record("template-restored");root.Children.Remove(control);await Layout();
                var released=caption.GetBindingExpression(TextBlock.ForegroundProperty)is null&&shapes.All(s=>s.GetBindingExpression(Shape.StrokeProperty)is null);
                if(!released)failures.Add(scheme+"-"+kind+" unloaded foreground binding");states.Add(new{scheme,kind,name="unloaded",loaded=false,retiredContentUnloaded=!caption.IsLoaded&&shapes.All(s=>!s.IsLoaded),ownedForegroundBindingsReleased=released});
            }
            Guard();var passed=failures.Count==0;await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-table-button-content-foreground-native-v1",passed,executionCompleted=true,templateStressCycles=3,states,pngs,failures,guards,focus,activations,activateCalled=false,showWindowCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false,publicApisOnly=true,librarySha256=Hash(typeof(BleeckerDataTable).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(passed?0:1);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-table-button-content-foreground-native-v1",passed=false,executionCompleted=false,stage,error=error.ToString(),states,pngs,failures,guards,focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
}
