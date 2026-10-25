using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;

namespace Orchard.Testing;

// An implementation probe for the pinned Header's fixed positioning: its
// physical header is 73 DIPs high while its following sibling starts at zero.
// This verifies WinUI layout behavior before selecting the production design.
internal static class HeaderPlacementProbe
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    private sealed class OverflowPanel : Panel
    {
        internal bool ReserveHeight;
        internal readonly Border Surface = new() { Width = 800, Height = 73, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 210, 20, 30)) };
        internal readonly Border Menu = new() { Width = 800, Height = 261, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 170, 70)) };
        internal OverflowPanel() { Children.Add(Surface); Children.Add(Menu); UseLayoutRounding = false; }
        protected override Size MeasureOverride(Size available) { Surface.Measure(new(800,73)); Menu.Measure(new(800,261)); return new(800,ReserveHeight?73:0); }
        protected override Size ArrangeOverride(Size final) { Surface.Arrange(new(0,0,800,73)); Menu.Arrange(new(0,72,800,261)); return new(800,73); }
    }
    private sealed class ZeroFlowControl : Control
    {
        internal readonly OverflowPanel Surface = new();
        internal readonly TranslateTransform Shift = new();
        private Grid? root;
        internal ZeroFlowControl()
        {
            UseLayoutRounding = false; IsTabStop = false;
            Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Control'><Grid x:Name='PART_Root' UseLayoutRounding='False'/></ControlTemplate>");
        }
        protected override void OnApplyTemplate() { base.OnApplyTemplate(); root = (Grid)GetTemplateChild("PART_Root"); root.RenderTransform = Shift; root.Children.Add(Surface); }
        protected override Size MeasureOverride(Size available) { base.MeasureOverride(new(800,73)); return new(800,0); }
        protected override Size ArrangeOverride(Size final) { base.ArrangeOverride(new(800,73)); return new(800,73); }
    }
    private sealed class MarginFlowControl : Control
    {
        internal readonly OverflowPanel Surface = new() { ReserveHeight=true };
        internal readonly TranslateTransform Shift = new();
        internal MarginFlowControl()
        {
            UseLayoutRounding=false;IsTabStop=false;
            Template=(ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Control'><Grid x:Name='PART_Root' Width='800' Height='73' Margin='0,0,-400,-73' HorizontalAlignment='Left' VerticalAlignment='Top' UseLayoutRounding='False'/></ControlTemplate>");
        }
        protected override void OnApplyTemplate(){base.OnApplyTemplate();var root=(Grid)GetTemplateChild("PART_Root");root.RenderTransform=Shift;root.Children.Add(Surface);}
    }
    internal static async Task RunAsync(Window window,string reportPath)
    {
        var rejectedDesign=Environment.GetCommandLineArgs().Contains("--probe-header-zero-flow-clip");
        var handle=WinRT.Interop.WindowNative.GetWindowHandle(window);var activations=0;var focus=0;
        window.Activated+=(_,args)=>{if(args.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        var root=new Canvas{Width=1280,Height=900,Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,250,250,250))};root.GotFocus+=(_,_)=>focus++;window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1280,900));
        var flow=new StackPanel{Width=400};Canvas.SetLeft(flow,80);Canvas.SetTop(flow,120);
        var panel=new OverflowPanel();var following=new Border{Height=500,Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,20,40,180))};flow.Children.Add(panel);flow.Children.Add(following);root.Children.Add(flow);
        string? failure=null;var guardChecks=0;var scenarios=new List<object>();var checks=new List<object>();
        void Guard(){guardChecks++;GetWindowThreadProcessId(GetForegroundWindow(),out var owner);if(window.Visible||window.AppWindow.IsVisible||IsWindowVisible(handle)||owner==Environment.ProcessId||activations!=0||focus!=0)throw new InvalidOperationException("Placement probe acquired visibility, activation or focus.");}
        var failedChecks=new List<string>();
        void Check(string name,bool passed,object actual){checks.Add(new{name,passed,actual});if(!passed)failedChecks.Add(name);}
        async Task Settle(){for(var index=0;index<8;index++){root.Measure(new(1280,900));root.Arrange(new(0,0,1280,900));root.UpdateLayout();Guard();await Task.Delay(20);}}
        object Box(FrameworkElement item){var rect=item.TransformToVisual(root).TransformBounds(new Rect(0,0,item.ActualWidth,item.ActualHeight));return new{x=rect.X,y=rect.Y,width=rect.Width,height=rect.Height};}
        async Task Capture(string name,FrameworkElement target,OverflowPanel content,double y)
        {
            await Settle();
            Check(name+" actual hidden viewport",root.XamlRoot is {IsHostVisible:false,RasterizationScale:1}&&root.XamlRoot.Size.Width==1280&&root.XamlRoot.Size.Height==900,new{root.XamlRoot?.IsHostVisible,root.XamlRoot?.RasterizationScale,root.XamlRoot?.Size});
            var top=content.Surface.TransformToVisual(root).TransformPoint(new Point());
            var next=following.TransformToVisual(root).TransformPoint(new Point());
            Check(name+" zero flow",target.DesiredSize.Height==0&&next.Y==120,new{desired=target.DesiredSize.Height,followingY=next.Y});
            Check(name+" physical size",content.Surface.ActualWidth==800&&content.Surface.ActualHeight==73&&top.X==80&&top.Y==y,new{top.X,top.Y,content.Surface.ActualWidth,content.Surface.ActualHeight});
            // Render the entire ancestor, so clipping at the zero-height child,
            // template root or narrow containing block cannot escape detection.
            var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);var buffer=await bitmap.GetPixelsAsync();var data=new byte[buffer.Length];using var reader=Windows.Storage.Streams.DataReader.FromBuffer(buffer);reader.ReadBytes(data);
            Check(name+" unscaled complete raster",bitmap.PixelWidth==1280&&bitmap.PixelHeight==900&&data.Length==1280*900*4,new{bitmap.PixelWidth,bitmap.PixelHeight,data.Length});
            string Pixel(int x,int row){var offset=(row*bitmap.PixelWidth+x)*4;return Convert.ToHexStringLower(data.AsSpan(offset,4));}
            var headerPixel=Pixel(800,(int)y+20);var menuPixel=Pixel(800,(int)y+160);var bodyPixel=Pixel(100,500);
            Check(name+" ancestor header paint",headerPixel=="1e14d2ff",headerPixel);
            Check(name+" ancestor menu paint",menuPixel=="46aa14ff",menuPixel);
            Check(name+" following body paint",bodyPixel=="b42814ff",bodyPixel);
            var headerHits=VisualTreeHelper.FindElementsInHostCoordinates(new Point(800,y+20),root).ToArray();
            var menuHits=VisualTreeHelper.FindElementsInHostCoordinates(new Point(800,y+160),root).ToArray();
            Check(name+" header hit testing",headerHits.Contains(content.Surface),headerHits.Select(hit=>hit.GetType().Name).ToArray());
            Check(name+" menu hit testing",menuHits.Contains(content.Menu),menuHits.Select(hit=>hit.GetType().Name).ToArray());
            using var stream=new Windows.Storage.Streams.InMemoryRandomAccessStream();var encoder=await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId,stream);
            encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,data);await encoder.FlushAsync();
            using var pngReader=new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0));await pngReader.LoadAsync((uint)stream.Size);var png=new byte[stream.Size];pngReader.ReadBytes(png);
            var file=name+".png";await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(reportPath)!,file),png);
            scenarios.Add(new{name,root=Box(root),flow=Box(flow),scale=root.XamlRoot?.RasterizationScale,target=Box(target),surface=Box(content.Surface),menu=Box(content.Menu),following=Box(following),desired=new{target.DesiredSize.Width,target.DesiredSize.Height},raster=new{file,pngSha256=Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(png)),width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(data)),headerPixel,menuPixel,bodyPixel}});
            Guard();
        }
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            if(rejectedDesign)
            {
                Canvas.SetZIndex(panel,50);await Capture("panel",panel,panel,120);
                flow.Children.Remove(panel);var control=new ZeroFlowControl();Canvas.SetZIndex(control,50);flow.Children.Insert(0,control);
                await Capture("control",control,control.Surface,120);control.Shift.Y=-120;await Capture("control-fixed",control,control.Surface,0);
            }
            else
            {
                flow.Children.Remove(panel);var control=new MarginFlowControl();Canvas.SetZIndex(control,50);flow.Children.Insert(0,control);
                await Capture("margin-control",control,control.Surface,120);
                control.Shift.Y=-120;await Capture("margin-control-fixed",control,control.Surface,0);
            }
            if(failedChecks.Count>0)throw new InvalidOperationException("Placement contracts failed: "+string.Join(", ",failedChecks));
        }
        catch(Exception error){failure=error.ToString();}
        string Hash(string file)=>Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(file)));
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);await File.WriteAllTextAsync(reportPath,JsonSerializer.Serialize(new{passed=failure is null,diagnosticOnly=true,scope=rejectedDesign?"hidden-header-rejected-zero-flow-design-v1":"hidden-header-negative-margin-placement-probe-v1",failure,scenarios,checks,guardChecks,activations,focus,activateCalled=false,showCalled=false,focusCalled=false,inputSent=false,nativeHeaderImplemented=false,sourceRasterParityVerified=false,nativeComponentAccepted=false,visibleRuntimeVerified=false,processArchitecture=RuntimeInformation.ProcessArchitecture.ToString(),osArchitecture=RuntimeInformation.OSArchitecture.ToString(),hostSha256=Hash(typeof(HeaderPlacementProbe).Assembly.Location),librarySha256=Hash(typeof(Orchard.BleeckerFooter).Assembly.Location)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(failure is null?0:1);
    }
}
