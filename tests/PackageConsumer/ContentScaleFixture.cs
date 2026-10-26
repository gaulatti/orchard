using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class ContentScaleFixture
{
    private delegate bool Visitor(nint handle,nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor,nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint handle);
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    internal static async Task RunAsync(Window window,string output,string hostAssembly)
    {
        var observations=new List<object>();var frames=new List<object>();var stage="initial";var guards=0;var focus=0;var activations=0;
        void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        try
        {
            var root=new Canvas{Width=400,Height=160,Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,68,85,102))};
            root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,e)=>{if(e.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
            void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Content scale fixture left its hidden boundary.");}
            Guard();using var island=new DesktopWindowXamlSource();island.Initialize(window.AppWindow.Id);island.Content=root;Guard();
            var separator=new BleeckerSeparator{Width=320};Canvas.SetLeft(separator,24);Canvas.SetTop(separator,24);root.Children.Add(separator);
            var roundingProducer=new Border{Tag=true};
            var rounded=new BleeckerSeparator{Width=320};rounded.SetBinding(FrameworkElement.UseLayoutRoundingProperty,new Binding{Source=roundingProducer,Path=new PropertyPath("Tag")});Canvas.SetLeft(rounded,24);Canvas.SetTop(rounded,32);root.Children.Add(rounded);
            var vertical=new BleeckerSeparator{Orientation=Orientation.Vertical,Height=64};Canvas.SetLeft(vertical,368);Canvas.SetTop(vertical,24);root.Children.Add(vertical);
            var roundedVertical=new BleeckerSeparator{Orientation=Orientation.Vertical,Height=64};roundedVertical.SetBinding(FrameworkElement.UseLayoutRoundingProperty,new Binding{Source=roundingProducer,Path=new PropertyPath("Tag")});Canvas.SetLeft(roundedVertical,376);Canvas.SetTop(roundedVertical,24);root.Children.Add(roundedVertical);
            var badge=new BleeckerStatusBadge{Label="LOCAL",Description="Fictional local status",Variant=BleeckerStatusBadgeVariant.Live};AutomationProperties.SetName(badge,"");AutomationProperties.SetHelpText(badge,"");Canvas.SetLeft(badge,24);Canvas.SetTop(badge,48);root.Children.Add(badge);
            async Task Layout(){for(var i=0;i<3;i++){root.Measure(new Size(400,160));root.Arrange(new Rect(0,0,400,160));root.UpdateLayout();Guard();await Task.Delay(16);}await NativeRenderReadiness.RenderAsync(root);Guard();}
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})foreach(var scale in new[]{1f,1.25f,1.5f,2f})
            {
                stage=$"{theme}/{scale}";root.RequestedTheme=theme;island.SiteBridge.OverrideScale=scale;var width=(int)Math.Round(400*scale);var height=(int)Math.Round(160*scale);window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(width,height));island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,width,height));await Layout();
                var actual=root.XamlRoot.RasterizationScale;var peer=FrameworkElementAutomationPeer.CreatePeerForElement(badge);
                var bindingsRetained=rounded.GetBindingExpression(FrameworkElement.UseLayoutRoundingProperty)is not null&&roundedVertical.GetBindingExpression(FrameworkElement.UseLayoutRoundingProperty)is not null;
                observations.Add(new{theme=theme.ToString(),requestedScale=scale,actualRasterizationScale=actual,islandScale=island.SiteBridge.OverrideScale,loaded=root.IsLoaded,separatorWidth=separator.ActualWidth,separatorHeight=separator.ActualHeight,separatorLayoutRounding=separator.UseLayoutRounding,verticalWidth=vertical.ActualWidth,verticalHeight=vertical.ActualHeight,verticalLayoutRounding=vertical.UseLayoutRounding,callerWidth=rounded.ActualWidth,callerHeight=rounded.ActualHeight,callerVerticalWidth=roundedVertical.ActualWidth,callerVerticalHeight=roundedVertical.ActualHeight,callerLayoutRounding=rounded.UseLayoutRounding,callerVerticalLayoutRounding=roundedVertical.UseLayoutRounding,bindingsRetained,badgeWidth=badge.ActualWidth,badgeHeight=badge.ActualHeight,name=peer.GetName(),help=peer.GetHelpText()});
                Require(Math.Abs(actual-scale)<.0001&&root.IsLoaded,"Owned XAML island did not apply the requested native rasterization scale.");Require(peer.GetName()==""&&peer.GetHelpText()=="","Scaled native island lost caller metadata.");
                Require(!separator.UseLayoutRounding&&!vertical.UseLayoutRounding&&rounded.UseLayoutRounding&&roundedVertical.UseLayoutRounding&&bindingsRetained,"Separator defaults replaced caller pixel-snapping bindings or retained rounded defaults.");
                Require(separator.ActualWidth==320&&separator.ActualHeight==1&&vertical.ActualWidth==1&&vertical.ActualHeight==64,"Scaled Separator lost its logical dimensions.");
                Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var bytes=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());var file=$"content-scale-{theme}-{scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}.png";
                using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,bytes);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream);await reader.LoadAsync((uint)stream.Size);var png=new byte[(int)stream.Size];reader.ReadBytes(png);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),png);frames.Add(new{theme=theme.ToString(),requestedScale=scale,width=bitmap.PixelWidth,height=bitmap.PixelHeight,file,sha256=Convert.ToHexStringLower(SHA256.HashData(png))});
                Require(bitmap.PixelWidth==width&&bitmap.PixelHeight==height,"Native island capture did not use its actual rasterization scale.");
            }
            Guard();GC.KeepAlive(island);await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-content-scale-native-v1",passed=true,scope="owned hidden DesktopWindowXamlSource SiteBridge.OverrideScale; no physical monitor DPI change",observations,frames,guards,focus,activations,isolatedRasterizationScaleVerified=true,displayScalingVerified=false,osSettingsChanged=false,hardwareInputSent=false,nativeARM64RuntimeVerified=false,librarySha256=Hash(typeof(BleeckerSeparator).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-content-scale-native-v1",passed=false,stage,error=error.ToString(),observations,frames,guards,focus,activations,librarySha256=Hash(typeof(BleeckerSeparator).Assembly.Location),hostSha256=Hash(hostAssembly)}));Environment.Exit(1);}
    }
}
