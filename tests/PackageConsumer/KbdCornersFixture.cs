using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class KbdCornersFixture
{
    private delegate bool Visitor(nint handle,nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor,nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement element)yield return element;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    internal static async Task RunAsync(Window window,string output,string hostAssembly)
    {
        var states=new List<object>();var lifecycle=new List<object>();var stage="initial";var guards=0;var focus=0;var activations=0;
        try
        {
            void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
            var canvasColor=Windows.UI.Color.FromArgb(255,68,85,102);
            var root=new Canvas{Width=180,Height=80,Background=new SolidColorBrush(canvasColor)};
            window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(180,80));
            root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
            void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Keycap fixture left its hidden boundary.");}
            async Task Layout(){for(var i=0;i<3;i++){root.Measure(new Size(180,80));root.Arrange(new Rect(0,0,180,80));root.UpdateLayout();Guard();await Task.Delay(16);}await NativeRenderReadiness.RenderAsync(root);Guard();}
            async Task<byte[]> Capture(string name)
            {
                Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Require(bitmap.PixelWidth==180&&bitmap.PixelHeight==80,"Keycap capture dimensions changed.");
                var bytes=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,180,80,96,96,bytes);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream);await reader.LoadAsync((uint)stream.Size);var png=new byte[(int)stream.Size];reader.ReadBytes(png);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,name),png);return bytes;
            }
            var cases=new[]{
                (id:"default",corners:new CornerRadius(4),width:80d,height:28d),
                (id:"square",corners:new CornerRadius(0),width:80d,height:28d),
                (id:"small",corners:new CornerRadius(2),width:80d,height:28d),
                (id:"rounded",corners:new CornerRadius(8),width:80d,height:28d),
                (id:"square-left",corners:new CornerRadius(3,7,8,0),width:80d,height:28d),
                (id:"square-right",corners:new CornerRadius(7,3,0,9),width:80d,height:28d),
                (id:"overlapping",corners:new CornerRadius(50),width:80d,height:28d),
                (id:"tiny-square",corners:new CornerRadius(0),width:6d,height:6d)};
            foreach(var scheme in new[]{ElementTheme.Light,ElementTheme.Dark})foreach(var flow in new[]{FlowDirection.LeftToRight,FlowDirection.RightToLeft})
            {
                root.RequestedTheme=scheme;
                var producer=new Border{Tag=new CornerRadius(4)};var original=new TextBlock{Text="K"};
                var control=new BleeckerKbd{Content=original,Width=80,Height=28,MinWidth=0,FlowDirection=flow};control.SetBinding(Control.CornerRadiusProperty,new Binding{Source=producer,Path=new PropertyPath("Tag")});Canvas.SetLeft(control,24);Canvas.SetTop(control,16);root.Children.Add(control);
                foreach(var item in cases)
                {
                    stage=$"{scheme}/{flow}/{item.id}";control.Width=item.width;control.Height=item.height;producer.Tag=item.corners;await Layout();var name=$"kbd-{scheme}-{flow}-{item.id}.png";var bytes=await Capture(name);
                    Require(ReferenceEquals(control.Content,original)&&control.GetBindingExpression(Control.CornerRadiusProperty)is not null,"Keycap lost caller content or radius binding.");
                    var path=Elements(control).OfType<Microsoft.UI.Xaml.Shapes.Path>().Single(e=>e.Name=="PART_Shadow");var brush=(SolidColorBrush)path.Fill;var squareSamples=new List<object>();
                    foreach(var x in new[]{.5,item.width-.5})
                    {
                        var corner=x<item.width/2?item.corners.BottomLeft:item.corners.BottomRight;if(corner!=0)continue;
                        var point=control.TransformToVisual(root).TransformPoint(new Point(x,item.height+.5));var px=(int)Math.Floor(point.X);var py=(int)Math.Floor(point.Y);var offset=(py*180+px)*4;
                        var expected=new[]{brush.Color.B,brush.Color.G,brush.Color.R}.Zip(new[]{canvasColor.B,canvasColor.G,canvasColor.R},(foreground,background)=>(int)Math.Round(foreground*brush.Opacity+background*(1-brush.Opacity))).ToArray();var actual=bytes.Skip(offset).Take(4).Select(v=>(int)v).ToArray();
                        Require(actual[3]==255&&actual.Take(3).Zip(expected).All(pair=>Math.Abs(pair.First-pair.Second)<=1),"Square keycap corner has no matching full-width one-pixel shadow.");squareSamples.Add(new{x=px,y=py,expectedBgr=expected,actualBgra=actual});
                    }
                    var data=path.Data as PathGeometry;Require(data is not null&&data.Figures.Count==1,"Keycap has no shadow geometry.");var arcs=data!.Figures[0].Segments.OfType<ArcSegment>().Select(a=>a.Size.Width).ToArray();var c=item.corners;
                    var scale=Math.Min(1,Math.Min(Math.Min(Ratio(item.width,c.TopLeft+c.TopRight),Ratio(item.width,c.BottomLeft+c.BottomRight)),Math.Min(Ratio(item.height,c.TopLeft+c.BottomLeft),Ratio(item.height,c.TopRight+c.BottomRight))));var expectedArcs=new[]{c.BottomLeft,c.BottomRight,c.BottomRight,c.BottomLeft}.Where(r=>r>0).Select(r=>r*scale).ToArray();
                    Require(arcs.Length==expectedArcs.Length&&arcs.Zip(expectedArcs).All(pair=>Math.Abs(pair.First-pair.Second)<.000001),"Keycap shadow corners do not follow the live caller radius.");
                    states.Add(new{scheme=scheme.ToString(),flow=flow.ToString(),item.id,item.width,item.height,corners=new[]{c.TopLeft,c.TopRight,c.BottomRight,c.BottomLeft},arcs,squareSamples,capture=name,captureSha256=Hash(Path.Combine(Path.GetDirectoryName(output)!,name)),passed=true});
                }
                control.Width=80;control.Height=28;producer.Tag=new CornerRadius(0);control.Keys=new[]{"Ctrl","S"};await Layout();var children=Elements(control).OfType<BleeckerKbd>().Where(e=>!ReferenceEquals(e,control)).ToArray();
                bool SquareShadow(BleeckerKbd e)=>((PathGeometry)Elements(e).OfType<Microsoft.UI.Xaml.Shapes.Path>().Single(p=>p.Name=="PART_Shadow").Data).Figures[0].Segments.OfType<ArcSegment>().Count()==0;
                Require(children.Length==2&&children.All(e=>e.CornerRadius==new CornerRadius(0)&&SquareShadow(e)),"Combination keycap shadows retained rounded corners.");
                control.Keys=null;var template=control.Template;control.Template=null;await Layout();producer.Tag=new CornerRadius(2);control.Template=template;await Layout();root.Children.Remove(control);await Layout();Require(!control.IsLoaded,"Keycap did not unload.");producer.Tag=new CornerRadius(0);root.Children.Add(control);await Layout();
                Require(ReferenceEquals(control.Content,original)&&control.CornerRadius==new CornerRadius(0)&&control.GetBindingExpression(Control.CornerRadiusProperty)is not null&&SquareShadow(control),"Retemplate/remount changed caller state or shadow corners.");lifecycle.Add(new{scheme=scheme.ToString(),flow=flow.ToString(),combinationCaps=children.Length,bindingRetained=true,contentRetained=true,retemplateVerified=true,remountVerified=true,passed=true});root.Children.Clear();await Layout();
            }
            Guard();await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-kbd-corners-v1",passed=true,states,lifecycle,guards,focus,activations,publicApisOnly=true,osSettingsChanged=false,physicalInputVerified=false,nativeARM64RuntimeVerified=false,displayScalingVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false,librarySha256=Hash(typeof(BleeckerKbd).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){Console.Error.WriteLine(error);try{await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-kbd-corners-v1",passed=false,stage,error=error.ToString(),states,lifecycle,guards,focus,activations,librarySha256=Hash(typeof(BleeckerKbd).Assembly.Location),hostSha256=Hash(hostAssembly)}));}finally{Environment.Exit(1);}}
        static double Ratio(double size,double radii)=>radii==0?1:size/radii;
    }
}
