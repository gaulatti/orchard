using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Hosting;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class CommandSpotlightPaintFixture
{
    private delegate bool Visitor(nint handle,nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor,nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    internal static async Task RunAsync(Window window,string output,string referencePath,string hostAssembly)
    {
        var referenceBytes=await File.ReadAllBytesAsync(referencePath);using var reference=JsonDocument.Parse(referenceBytes);var spec=reference.RootElement;
        var width=spec.GetProperty("width").GetInt32();var height=spec.GetProperty("height").GetInt32();var columns=spec.GetProperty("columns").GetInt32();var cell=spec.GetProperty("cell").GetInt32();var inputs=spec.GetProperty("cases").EnumerateArray().Select(c=>c.Clone()).ToArray();
        if(spec.GetProperty("format").GetString()!="orchard-command-spotlight-paint-source-v1"||!spec.GetProperty("passed").GetBoolean()||inputs.Length!=150||width!=600||height!=400||columns!=15||cell!=40)throw new InvalidOperationException("Unexpected paint reference");
        var root=new Canvas{Width=width,Height=height};var host=new BleeckerModalHost{Content=root,Width=width,Height=height};window.Content=host;window.AppWindow.ResizeClient(new(width,height));var folder=Path.GetDirectoryName(output)!;
        var guards=0;var focus=0;var activations=0;var stage="initial";var cases=new List<object>();var images=new List<object>();var contexts=new List<object>();var liveCases=new List<object>();
        host.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Paint probe left hidden boundary");Require(host.XamlRoot is null||VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot).Count==0,"Paint probe opened native Popup");}
        async Task Layout(){for(var i=0;i<3;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Dispatcher refused layout");await idle.Task;host.Measure(new(width,height));host.Arrange(new(0,0,width,height));host.UpdateLayout();Guard();}await Task.Delay(40);}
        static Windows.UI.Color Color(JsonElement values,byte alpha=255)=>Windows.UI.Color.FromArgb(alpha,values[0].GetByte(),values[1].GetByte(),values[2].GetByte());
        try
        {
            await Layout();foreach(var mode in new[]{"brush-opacity","bitmap-cache","argb-round","argb-floor","element-opacity","element-opacity-cache"})
            {
                stage=mode;await File.WriteAllTextAsync(Path.Combine(folder,"paint-stage.json"),JsonSerializer.Serialize(new{stage,guards}));root.Children.Clear();
                foreach(var item in inputs)
                {
                    var index=item.GetProperty("index").GetInt32();var alpha=item.GetProperty("alpha").GetDouble();var tile=new Grid{Width=cell,Height=cell,Background=new SolidColorBrush(Color(item.GetProperty("background")))};Canvas.SetLeft(tile,index%columns*cell);Canvas.SetTop(tile,index/columns*cell);
                    var color=Color(item.GetProperty("rgb"));var brush=new SolidColorBrush(color){Opacity=alpha};var fill=new Border{Background=brush};
                    if(mode is "bitmap-cache" or "element-opacity-cache")fill.CacheMode=new BitmapCache();
                    if(mode is "argb-round" or "argb-floor"){color.A=(byte)(mode=="argb-round"?Math.Round(alpha*255,MidpointRounding.AwayFromZero):Math.Floor(alpha*255));brush.Color=color;brush.Opacity=1;}
                    if(mode is "element-opacity" or "element-opacity-cache"){brush.Opacity=1;fill.Opacity=alpha;}
                    tile.Children.Add(fill);root.Children.Add(tile);
                }
                await Layout();byte[]? previous=null,bytes=null;var attempts=0;var clock=Stopwatch.StartNew();for(;attempts<6;attempts++){var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(host,width,height);bytes=(await bitmap.GetPixelsAsync()).ToArray();if(previous is not null&&previous.AsSpan().SequenceEqual(bytes))break;previous=bytes;await Layout();}clock.Stop();Require(attempts<6,"Paint frame did not settle");
                var file="paint-"+mode+".png";using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)width,(uint)height,96,96,bytes!);await encoder.FlushAsync();stream.Seek(0);using(var target=File.Create(Path.Combine(folder,file)))await stream.AsStreamForRead().CopyToAsync(target);
                images.Add(new{mode,file,sha256=Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(folder,file)))),attempts=attempts+1,stableFrames=2,captureMilliseconds=clock.Elapsed.TotalMilliseconds});
                foreach(var item in inputs){var x=item.GetProperty("x").GetInt32();var y=item.GetProperty("y").GetInt32();var at=(y*width+x)*4;int[] pixel=[bytes![at+2],bytes[at+1],bytes[at],bytes[at+3]];cases.Add(new{mode,id=item.GetProperty("id").GetString(),index=item.GetProperty("index").GetInt32(),pixel});}Guard();
            }
            root.Children.Clear();await Layout();host.Width=1000;host.Height=960;window.AppWindow.ResizeClient(new(1000,960));
            async Task ContextLayout(){for(var i=0;i<3;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Context dispatcher refused layout");await idle.Task;host.Measure(new(1000,960));host.Arrange(new(0,0,1000,960));host.UpdateLayout();Guard();}await Task.Delay(40);}
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                host.RequestedTheme=theme;var page=new Grid{Background=new SolidColorBrush(theme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33))};var control=new BleeckerCommandSpotlight{Open=true,Margin=new(32),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Actions=new[]{new BleeckerCommandSpotlightAction("original","Fictional original"){Group="Actions"}}};page.Children.Add(control);host.Content=page;await ContextLayout();await Task.Delay(350);await ContextLayout();
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;var backdrop=(Grid)typeof(BleeckerCommandSpotlight).GetField("backdrop",flags)!.GetValue(control)!;var blurHost=(Grid)typeof(BleeckerCommandSpotlight).GetField("blurHost",flags)!.GetValue(control)!;var visual=(Microsoft.UI.Composition.SpriteVisual)typeof(BleeckerCommandSpotlight).GetField("blurVisual",flags)!.GetValue(control)!;var tint=backdrop.Children.OfType<Border>().Single();var original=(SolidColorBrush)tint.Background;var slots=(Dictionary<string,TextBlock>)typeof(BleeckerCommandSpotlight).GetField("palette",flags)!.GetValue(control)!;var slot=slots["Backdrop"];var semantic=(SolidColorBrush)slot.Foreground;
                foreach(var mode in new[]{"original","literal-brush","argb-round","argb-floor","without-blur","literal-without-blur","cached-original"})
                {
                    stage=theme+"/context/"+mode;await File.WriteAllTextAsync(Path.Combine(folder,"paint-stage.json"),JsonSerializer.Serialize(new{stage,guards}));var withoutBlur=mode is "without-blur" or "literal-without-blur";ElementCompositionPreview.SetElementChildVisual(blurHost,withoutBlur?null:visual);tint.CacheMode=mode=="cached-original"?new BitmapCache():null;tint.Background=original;
                    if(mode is "literal-brush" or "literal-without-blur")tint.Background=new SolidColorBrush(semantic.Color){Opacity=.45};
                    if(mode is "argb-round" or "argb-floor"){var color=semantic.Color;color.A=(byte)(mode=="argb-round"?Math.Round(color.A*semantic.Opacity,MidpointRounding.AwayFromZero):Math.Floor(color.A*semantic.Opacity));tint.Background=new SolidColorBrush(color);}
                    await ContextLayout();byte[]? previous=null,bytes=null;var attempts=0;for(;attempts<6;attempts++){var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(host,1000,960);bytes=(await bitmap.GetPixelsAsync()).ToArray();if(previous is not null&&previous.AsSpan().SequenceEqual(bytes))break;previous=bytes;await ContextLayout();}Require(attempts<6,"Context frame did not settle");var at=(900*1000+900)*4;int[] corner=[bytes![at+2],bytes[at+1],bytes[at],bytes[at+3]];
                    var file="context-"+theme+"-"+mode+".png";using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,1000,960,96,96,bytes);await encoder.FlushAsync();stream.Seek(0);using(var target=File.Create(Path.Combine(folder,file)))await stream.AsStreamForRead().CopyToAsync(target);
                    var actual=(SolidColorBrush)tint.Background;contexts.Add(new{theme=theme.ToString(),mode,corner,file,sha256=Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(folder,file)))),attempts=attempts+1,stableFrames=2,sourceOpacity=semantic.Opacity,actualOpacity=actual.Opacity,actualAlpha=actual.Color.A,backdropOpacity=backdrop.Opacity,controlOpacity=control.Opacity,blurVisible=!withoutBlur});
                }
                tint.CacheMode=null;tint.Background=original;var donor=new TextBlock();var bound=new SolidColorBrush(Microsoft.UI.Colors.Black){Opacity=.45};donor.Foreground=bound;slot.SetBinding(TextBlock.ForegroundProperty,new Binding{Source=donor,Path=new("Foreground"),Mode=BindingMode.OneWay});var originalAction=control.Actions.Single();
                async Task Live(string id,double expectedAlpha,SolidColorBrush current)
                {
                    stage=theme+"/live/"+id;await ContextLayout();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(host,1000,960);var bytes=(await bitmap.GetPixelsAsync()).ToArray();var at=(900*1000+900)*4;int[] pixel=[bytes[at+2],bytes[at+1],bytes[at],bytes[at+3]];var rgb=current.Color;int[] background=theme==ElementTheme.Light?[255,255,255]:[13,24,33];var expected=inputs.Single(c=>c.GetProperty("role").GetString()=="primitive"&&c.GetProperty("space").GetString()=="oklab"&&c.GetProperty("alpha").GetDouble()==expectedAlpha&&c.GetProperty("rgb")[0].GetByte()==rgb.R&&c.GetProperty("rgb")[1].GetByte()==rgb.G&&c.GetProperty("rgb")[2].GetByte()==rgb.B&&c.GetProperty("background").EnumerateArray().Select(v=>v.GetInt32()).SequenceEqual(background)).GetProperty("pixel").EnumerateArray().Select(v=>v.GetInt32()).ToArray();
                    Require(pixel.SequenceEqual(expected),"Live isolated backdrop differs from browser reference: "+id);Require(ReferenceEquals(slot.Foreground,current)&&slot.GetBindingExpression(TextBlock.ForegroundProperty)is not null,"Semantic source brush/binding replaced");Require(current.Color.A==255&&ReferenceEquals(control.Actions.Single(),originalAction),"Source alpha/action changed during paint");var rendered=(SolidColorBrush)tint.Background;
                    liveCases.Add(new{theme=theme.ToString(),id,pixel,expected,sourceAlpha=current.Color.A,sourceOpacity=current.Opacity,renderedAlpha=rendered.Color.A,renderedOpacity=rendered.Opacity,sourceAndBindingRetained=true,originalActionRetained=true,passed=true});
                }
                await Live("bound",.45,bound);bound.Color=Windows.UI.Color.FromArgb(255,44,87,132);await Live("color-mutated",.45,bound);bound.Opacity=.2;await Live("opacity-mutated",.2,bound);
                var replacement=new SolidColorBrush(Microsoft.UI.Colors.White){Opacity=.05};donor.Foreground=replacement;await Live("replacement",.05,replacement);bound.Color=Microsoft.UI.Colors.Red;bound.Opacity=.65;await Live("old-source-quiet",.05,replacement);
                var retired=(SolidColorBrush)tint.Background;control.Open=false;await Task.Delay(300);await ContextLayout();var retiredColor=retired.Color;replacement.Color=Windows.UI.Color.FromArgb(255,44,87,132);replacement.Opacity=.65;await ContextLayout();Require(retired.Color==retiredColor,"Detached backdrop still observes source mutations");liveCases.Add(new{theme=theme.ToString(),id="closed-quiet",projectionUnchanged=true,passed=true});
                control.Open=true;await Task.Delay(350);await ContextLayout();backdrop=(Grid)typeof(BleeckerCommandSpotlight).GetField("backdrop",flags)!.GetValue(control)!;tint=backdrop.Children.OfType<Border>().Single();await Live("reopened",.65,replacement);
                host.Content=root;await ContextLayout();
            }
            Guard();string Hash(string p)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-command-spotlight-paint-native-v3",passed=true,referenceSha256=Convert.ToHexStringLower(SHA256.HashData(referenceBytes)),librarySha256=Hash(typeof(BleeckerCommandSpotlight).Assembly.Location),hostSha256=Hash(hostAssembly),width,height,cases,images,contexts,liveCases,guards,focusEvents=focus,activations,hostVisible=window.Visible,hardwareInputSent=false,nativeComponentAccepted=false},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-command-spotlight-paint-native-v1",passed=false,stage,error=error.ToString(),cases,images,guards,focusEvents=focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
}
