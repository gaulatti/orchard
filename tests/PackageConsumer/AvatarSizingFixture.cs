using System.Collections.ObjectModel;
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

namespace Orchard.Testing;

internal static class AvatarSizingFixture
{
    private delegate bool Visitor(nint handle,nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor,nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement element)yield return element;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    private static T Part<T>(DependencyObject root,string name)where T:FrameworkElement=>Elements(root).OfType<T>().Single(e=>e.Name==name);
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static Canvas StyledHost()=> (Canvas)XamlReader.Load("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:orchard='using:Orchard' Width='520' Height='240'><Canvas.Resources><Style TargetType='orchard:BleeckerAvatar'><Setter Property='Width' Value='72'/><Setter Property='Height' Value='36'/><Setter Property='FontSize' Value='19'/></Style></Canvas.Resources></Canvas>");
    internal static async Task RunAsync(Window window,string output,string hostAssembly)
    {
        var states=new List<object>();var contracts=new List<object>();var stage="initial";var guards=0;var focus=0;var activations=0;
        try
        {
            void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
            var root=new Canvas{Width=520,Height=240,Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,68,85,102))};window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(520,240));root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
            void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Avatar fixture left its hidden boundary.");}
            async Task Layout(){for(var i=0;i<3;i++){root.Measure(new Size(520,240));root.Arrange(new Rect(0,0,520,240));root.UpdateLayout();Guard();await Task.Delay(16);}await NativeRenderReadiness.RenderAsync(root);Guard();}
            async Task Capture(string name){Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Require(bitmap.PixelWidth==520&&bitmap.PixelHeight==240,"Avatar capture dimensions changed.");var bytes=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,520,240,96,96,bytes);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream);await reader.LoadAsync((uint)stream.Size);var png=new byte[(int)stream.Size];reader.ReadBytes(png);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,name),png);}
            var sizes=Enum.GetValues<BleeckerAvatarSize>();var edges=new[]{24d,32,40,48,64};var fonts=new[]{10d,12,14,16,18};var overflowFonts=new[]{10d,12,12,14,16};
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})foreach(var flow in new[]{FlowDirection.LeftToRight,FlowDirection.RightToLeft})
            {
                root.RequestedTheme=theme;
                foreach(var empty in new[]{false,true})for(var index=0;index<sizes.Length;index++)
                {
                    stage=$"{theme}/{flow}/default/{empty}/{sizes[index]}";var avatar=new BleeckerAvatar{AvatarSize=sizes[index],Fallback=empty?null:"Fictional Harbor",FlowDirection=flow};Canvas.SetLeft(avatar,24);Canvas.SetTop(avatar,16);root.Children.Add(avatar);await Layout();var person=Part<Viewbox>(avatar,"PART_Person");var text=Part<TextBlock>(avatar,"PART_Initials");
                    Require(avatar.ActualWidth==edges[index]&&avatar.ActualHeight==edges[index]&&avatar.FontSize==fonts[index]&&text.FontSize==fonts[index],"Avatar default metrics changed.");Require(Math.Abs(person.Width-edges[index]*.6)<.001&&Math.Abs(person.Height-edges[index]*.6)<.001,"Default avatar silhouette dimensions changed.");var file=$"avatar-{theme}-{flow}-default-{(empty?"empty":"initials")}-{sizes[index]}.png";await Capture(file);states.Add(new{theme=theme.ToString(),flow=flow.ToString(),id="default",size=sizes[index].ToString(),empty,avatar.ActualWidth,avatar.ActualHeight,avatar.FontSize,capture=file,captureSha256=Hash(Path.Combine(Path.GetDirectoryName(output)!,file)),passed=true});root.Children.Clear();await Layout();
                }
                foreach(var mode in new[]{"bound-custom","bound-same","local-same","style","based-style","implicit-style"})
                {
                    stage=$"{theme}/{flow}/{mode}/initial";var host=mode=="implicit-style"?StyledHost():new Canvas{Width=520,Height=240};root.Children.Add(host);var avatar=new BleeckerAvatar{Fallback=null,FlowDirection=flow};Canvas.SetLeft(avatar,24);Canvas.SetTop(avatar,16);var bound=mode.StartsWith("bound",StringComparison.Ordinal);var w=mode=="bound-custom"?74d:mode is "style" or "implicit-style"?72d:mode=="based-style"?70d:40d;var h=mode=="bound-custom"?42d:mode is "style" or "implicit-style"?36d:mode=="based-style"?38d:40d;var f=mode=="bound-custom"?17d:mode is "style" or "implicit-style"?19d:mode=="based-style"?21d:14d;var producer=new TextBlock{Width=w,Height=h,FontSize=f};
                    if(bound)foreach(var(property,name)in new[]{(FrameworkElement.WidthProperty,"Width"),(FrameworkElement.HeightProperty,"Height"),(Control.FontSizeProperty,"FontSize")})avatar.SetBinding(property,new Binding{Source=producer,Path=new PropertyPath(name)});
                    else if(mode=="local-same"){avatar.Width=w;avatar.Height=h;avatar.FontSize=f;}
                    else if(mode is "style" or "based-style"){var style=new Style{TargetType=typeof(BleeckerAvatar)};style.Setters.Add(new Setter(FrameworkElement.WidthProperty,w));style.Setters.Add(new Setter(FrameworkElement.HeightProperty,h));style.Setters.Add(new Setter(Control.FontSizeProperty,mode=="based-style"?18d:f));if(mode=="based-style"){var derived=new Style{TargetType=typeof(BleeckerAvatar),BasedOn=style};derived.Setters.Add(new Setter(Control.FontSizeProperty,f));style=derived;}avatar.Style=style;}
                    host.Children.Add(avatar);await Layout();
                    void Check(string id)
                    {
                        var bindings=new[]{FrameworkElement.WidthProperty,FrameworkElement.HeightProperty,Control.FontSizeProperty}.Select(p=>avatar.GetBindingExpression(p)is not null).ToArray();var person=Part<Viewbox>(avatar,"PART_Person");var matches=avatar.Width==w&&avatar.Height==h&&avatar.FontSize==f&&Math.Abs(avatar.ActualWidth-w)<.001&&Math.Abs(avatar.ActualHeight-h)<.001&&Part<TextBlock>(avatar,"PART_Initials").FontSize==f&&bindings.All(b=>b==bound);states.Add(new{theme=theme.ToString(),flow=flow.ToString(),id,mode,size=avatar.AvatarSize.ToString(),expectedWidth=w,expectedHeight=h,expectedFont=f,avatar.Width,avatar.Height,avatar.FontSize,avatar.ActualWidth,avatar.ActualHeight,bindings,personWidth=person.Width,personHeight=person.Height,passed=matches});Require(matches,"Avatar template or size update replaced caller dimensions, font or bindings.");Require(Math.Abs(person.Width-w*.6)<.001&&Math.Abs(person.Height-h*.6)<.001,"Avatar silhouette does not follow60 percent of caller dimensions.");
                    }
                    Check("caller-initial");if(mode=="bound-custom")await Capture($"avatar-{theme}-{flow}-bound-custom.png");foreach(var size in sizes){stage=$"{theme}/{flow}/{mode}/{size}";avatar.AvatarSize=size;await Layout();Check("caller-size");}
                    if(bound){producer.Width=w+=8;producer.Height=h+=4;producer.FontSize=f+=1;await Layout();Check("caller-update");}
                    var saved=avatar.Template;avatar.Template=null;await Layout();avatar.Template=saved;await Layout();Check("caller-retemplate");host.Children.Remove(avatar);await Layout();Require(!avatar.IsLoaded,"Avatar did not unload.");if(bound){producer.Width=w+=2;producer.Height=h+=2;}avatar.AvatarSize=BleeckerAvatarSize.ExtraSmall;host.Children.Add(avatar);await Layout();Check("caller-remount");
                    if(mode!="implicit-style")
                    {
                        foreach(var property in new[]{FrameworkElement.WidthProperty,FrameworkElement.HeightProperty,Control.FontSizeProperty})avatar.ClearValue(property);await Layout();var styled=mode is "style" or "based-style";Require(avatar.Width==(styled?w:24)&&avatar.Height==(styled?h:24)&&avatar.FontSize==(styled?f:10),"Clearing avatar overrides did not restore style or current size defaults.");if(styled){avatar.Style=null;await Layout();Require(avatar.Width==24&&avatar.Height==24&&avatar.FontSize==10,"Removing avatar style did not restore current size defaults.");}
                    }
                    contracts.Add(new{theme=theme.ToString(),flow=flow.ToString(),id="caller-lifecycle",mode,passed=true});root.Children.Clear();await Layout();
                }
                foreach(var styled in new[]{false,true})
                {
                    var host=styled?StyledHost():new Canvas{Width=520,Height=240};root.Children.Add(host);var group=new BleeckerAvatarGroup{Max=2,FlowDirection=flow,Avatars=new ObservableCollection<BleeckerAvatarData>{new(){Fallback="Fictional Harbor"},new(){Fallback="Sample Journal"},new(){Fallback="Local News"},new(){Fallback="Demo Desk"}}};host.Children.Add(group);Canvas.SetLeft(group,24);Canvas.SetTop(group,16);BleeckerAvatar[]? original=null;
                    for(var index=0;index<sizes.Length;index++){stage=$"{theme}/{flow}/group/{styled}/{sizes[index]}";group.AvatarSize=sizes[index];await Layout();var children=Elements(group).OfType<BleeckerAvatar>().ToArray();Require(children.Length==3,"Avatar group lost its two original positions or overflow.");if(original is not null)Require(children.SequenceEqual(original),"Size change replaced retained group children.");original=children;var width=styled?72:edges[index];var height=styled?36:edges[index];Require(children.All(a=>a.ActualWidth==width&&a.ActualHeight==height)&&children.Take(2).All(a=>a.FontSize==(styled?19:fonts[index]))&&children[2].FontSize==(styled?19:overflowFonts[index])&&Math.Abs(group.ActualWidth-(3*width-16))<.001,"Avatar group lost default/implicit-style dimensions, font or overlap.");states.Add(new{theme=theme.ToString(),flow=flow.ToString(),id="group-size",styled,size=sizes[index].ToString(),children=children.Length,width,height,groupWidth=group.ActualWidth,passed=true});}contracts.Add(new{theme=theme.ToString(),flow=flow.ToString(),id="group-retention",styled,passed=true});root.Children.Clear();await Layout();
                }
            }
            Guard();await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-avatar-sizing-v1",passed=true,states,contracts,guards,focus,activations,publicApisOnly=true,osSettingsChanged=false,physicalInputVerified=false,nativeARM64RuntimeVerified=false,displayScalingVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false,librarySha256=Hash(typeof(BleeckerAvatar).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){Console.Error.WriteLine(error);try{await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-avatar-sizing-v1",passed=false,stage,error=error.ToString(),states,contracts,guards,focus,activations,librarySha256=Hash(typeof(BleeckerAvatar).Assembly.Location),hostSha256=Hash(hostAssembly)}));}finally{Environment.Exit(1);}}
    }
}
