using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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
    private static T? Find<T>(DependencyObject root,string name) where T:FrameworkElement {if(root is T match&&match.Name==name)return match;for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)if(Find<T>(VisualTreeHelper.GetChild(root,i),name)is{ } found)return found;return null;}
    internal static async Task RunAsync(Window window,string output,string hostAssembly)
    {
        var observations=new List<object>();var frames=new List<object>();var toggleObservations=new List<object>();var toggleFrames=new List<object>();var stage="initial";var guards=0;var focus=0;var activations=0;
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
            const string switchLabel="Enable fictional editorial notifications while reviewing a long draft";
            var scaledSwitch=new BleeckerSwitch{Width=320,Label=switchLabel,IsOn=true};var toggles=0;scaledSwitch.Toggled+=(_,_)=>toggles++;Canvas.SetLeft(scaledSwitch,24);Canvas.SetTop(scaledSwitch,104);root.Children.Add(scaledSwitch);
            async Task Layout(){for(var i=0;i<3;i++){root.Measure(new Size(400,160));root.Arrange(new Rect(0,0,400,160));root.UpdateLayout();Guard();await Task.Delay(16);}await NativeRenderReadiness.RenderAsync(root);Guard();}
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})foreach(var scale in new[]{1f,1.25f,1.5f,2f})
            {
                stage=$"{theme}/{scale}";root.RequestedTheme=theme;island.SiteBridge.OverrideScale=scale;var width=(int)Math.Round(400*scale);var height=(int)Math.Round(160*scale);window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(width,height));island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,width,height));await Layout();
                var actual=root.XamlRoot.RasterizationScale;var peer=FrameworkElementAutomationPeer.CreatePeerForElement(badge);
                var badgeSurface=Find<Border>(badge,"PART_Badge")??throw new InvalidOperationException("Scaled Status Badge surface is missing.");
                var badgeLabel=Find<TextBlock>(badge,"PART_Label")??throw new InvalidOperationException("Scaled Status Badge label is missing.");
                var badgeDescription=Find<TextBlock>(badge,"PART_Description")??throw new InvalidOperationException("Scaled Status Badge description is missing.");
                var badgeDot=Find<Microsoft.UI.Xaml.Shapes.Ellipse>(badge,"PART_Dot")??throw new InvalidOperationException("Scaled Status Badge dot is missing.");
                var badgeBounds=badge.TransformToVisual(root).TransformBounds(new Rect(0,0,badge.ActualWidth,badge.ActualHeight));
                var badgeSurfaceBounds=badgeSurface.TransformToVisual(root).TransformBounds(new Rect(0,0,badgeSurface.ActualWidth,badgeSurface.ActualHeight));
                var switchTrack=Find<Border>(scaledSwitch,"PART_Track")??throw new InvalidOperationException("Scaled Switch track is missing.");
                var switchThumb=Find<Microsoft.UI.Xaml.Shapes.Ellipse>(scaledSwitch,"PART_Thumb")??throw new InvalidOperationException("Scaled Switch thumb is missing.");
                var switchText=Find<TextBlock>(scaledSwitch,"PART_Label")??throw new InvalidOperationException("Scaled Switch label is missing.");
                var switchName=FrameworkElementAutomationPeer.CreatePeerForElement(scaledSwitch).GetName();
                var trackBounds=switchTrack.TransformToVisual(root).TransformBounds(new Rect(0,0,switchTrack.ActualWidth,switchTrack.ActualHeight));
                var labelBounds=switchText.TransformToVisual(root).TransformBounds(new Rect(0,0,switchText.ActualWidth,switchText.ActualHeight));
                var bindingsRetained=rounded.GetBindingExpression(FrameworkElement.UseLayoutRoundingProperty)is not null&&roundedVertical.GetBindingExpression(FrameworkElement.UseLayoutRoundingProperty)is not null;
                observations.Add(new{theme=theme.ToString(),requestedScale=scale,actualRasterizationScale=actual,islandScale=island.SiteBridge.OverrideScale,loaded=root.IsLoaded,separatorWidth=separator.ActualWidth,separatorHeight=separator.ActualHeight,separatorLayoutRounding=separator.UseLayoutRounding,verticalWidth=vertical.ActualWidth,verticalHeight=vertical.ActualHeight,verticalLayoutRounding=vertical.UseLayoutRounding,callerWidth=rounded.ActualWidth,callerHeight=rounded.ActualHeight,callerVerticalWidth=roundedVertical.ActualWidth,callerVerticalHeight=roundedVertical.ActualHeight,callerLayoutRounding=rounded.UseLayoutRounding,callerVerticalLayoutRounding=roundedVertical.UseLayoutRounding,bindingsRetained,badgeWidth=badge.ActualWidth,badgeHeight=badge.ActualHeight,badgeBounds=new{x=badgeBounds.X,y=badgeBounds.Y,width=badgeBounds.Width,height=badgeBounds.Height},badgeSurfaceBounds=new{x=badgeSurfaceBounds.X,y=badgeSurfaceBounds.Y,width=badgeSurfaceBounds.Width,height=badgeSurfaceBounds.Height},badgeLabel=badgeLabel.Text,badgeLabelHeight=badgeLabel.ActualHeight,badgeDescription=badgeDescription.Text,badgeDescriptionHeight=badgeDescription.ActualHeight,badgeDotWidth=badgeDot.ActualWidth,badgeDotHeight=badgeDot.ActualHeight,badgeTabStop=badge.IsTabStop,name=peer.GetName(),help=peer.GetHelpText(),switchWidth=scaledSwitch.ActualWidth,switchHeight=scaledSwitch.ActualHeight,switchTrackWidth=switchTrack.ActualWidth,switchTrackHeight=switchTrack.ActualHeight,switchThumbWidth=switchThumb.ActualWidth,switchThumbHeight=switchThumb.ActualHeight,switchLabel=switchText.Text,switchName,switchLabelBounds=new{x=labelBounds.X,y=labelBounds.Y,width=labelBounds.Width,height=labelBounds.Height},switchTrackBounds=new{x=trackBounds.X,y=trackBounds.Y,width=trackBounds.Width,height=trackBounds.Height},switchToggles=toggles});
                Require(Math.Abs(actual-scale)<.0001&&root.IsLoaded,"Owned XAML island did not apply the requested native rasterization scale.");Require(peer.GetName()==""&&peer.GetHelpText()=="","Scaled native island lost caller metadata.");
                Require(badge.IsLoaded&&!badge.IsTabStop&&badgeLabel.Text=="LOCAL"&&badgeDescription.Text=="Fictional local status"&&badgeLabel.ActualHeight>0&&badgeDescription.ActualHeight>0&&badgeDot.ActualWidth==6&&badgeDot.ActualHeight==6&&badgeSurface.ActualHeight==24&&badgeBounds.X>=24&&badgeBounds.Y>=48&&badgeBounds.Right<=400&&badgeBounds.Bottom<=104,"Scaled Status Badge lost its visible content, geometry or display-only semantics.");
                Require(!separator.UseLayoutRounding&&!vertical.UseLayoutRounding&&rounded.UseLayoutRounding&&roundedVertical.UseLayoutRounding&&bindingsRetained,"Separator defaults replaced caller pixel-snapping bindings or retained rounded defaults.");
                Require(separator.ActualWidth==320&&separator.ActualHeight==1&&vertical.ActualWidth==1&&vertical.ActualHeight==64,"Scaled Separator lost its logical dimensions.");
                Require(scaledSwitch.ActualWidth==320&&switchTrack.ActualWidth==36&&switchTrack.ActualHeight==20&&switchThumb.ActualWidth==16&&switchThumb.ActualHeight==16&&switchText.Text==switchLabel&&switchName==switchLabel&&toggles==0,"Scaled Switch lost its geometry, label, name or toggle isolation.");
                Require(labelBounds.X>=24&&labelBounds.Y>=104&&labelBounds.Right<=344&&labelBounds.Bottom<=160&&labelBounds.Height>=20,"Scaled Switch label escaped its available row or clipped the island.");
                Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var bytes=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());var file=$"content-scale-{theme}-{scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}.png";
                using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,bytes);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream);await reader.LoadAsync((uint)stream.Size);var png=new byte[(int)stream.Size];reader.ReadBytes(png);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),png);frames.Add(new{theme=theme.ToString(),requestedScale=scale,width=bitmap.PixelWidth,height=bitmap.PixelHeight,file,sha256=Convert.ToHexStringLower(SHA256.HashData(png))});
                Require(bitmap.PixelWidth==width&&bitmap.PixelHeight==height,"Native island capture did not use its actual rasterization scale.");
            }
            root.Children.Clear();
            var styleNames=new[]{"BleeckerSmallToggleStyle","BleeckerToggleStyle","BleeckerLargeToggleStyle","BleeckerSmallOutlineToggleStyle","BleeckerOutlineToggleStyle","BleeckerLargeOutlineToggleStyle"};
            var labels=new[]{"Small","Medium","Large","S outline","M outline","L outline"};
            var scaledToggles=new ToggleButton[styleNames.Length];var toggleChanges=0;
            for(var i=0;i<styleNames.Length;i++)
            {
                var toggle=new ToggleButton{Width=108,Content=labels[i],IsChecked=true,Style=(Style)Application.Current.Resources[styleNames[i]]};
                AutomationProperties.SetName(toggle,$"Fictional {labels[i]} toggle");
                toggle.Checked+=(_,_)=>toggleChanges++;toggle.Unchecked+=(_,_)=>toggleChanges++;
                Canvas.SetLeft(toggle,24+(i%3)*122);Canvas.SetTop(toggle,12+(i/3)*68);root.Children.Add(toggle);scaledToggles[i]=toggle;
            }
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})foreach(var scale in new[]{1f,1.25f,1.5f,2f})
            {
                stage=$"toggle/{theme}/{scale}";root.RequestedTheme=theme;island.SiteBridge.OverrideScale=scale;var width=(int)Math.Round(400*scale);var height=(int)Math.Round(160*scale);window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(width,height));island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,width,height));await Layout();
                var states=scaledToggles.Select((toggle,i)=>
                {
                    var peer=FrameworkElementAutomationPeer.CreatePeerForElement(toggle);var bounds=toggle.TransformToVisual(root).TransformBounds(new Rect(0,0,toggle.ActualWidth,toggle.ActualHeight));
                    return new{id=styleNames[i],label=labels[i],name=peer.GetName(),width=toggle.ActualWidth,height=toggle.ActualHeight,bounds=new{x=bounds.X,y=bounds.Y,width=bounds.Width,height=bounds.Height},checkedState=toggle.IsChecked,loaded=toggle.IsLoaded,tabStop=toggle.IsTabStop};
                }).ToArray();
                Require(states.All((state)=>state.loaded&&state.checkedState==true&&state.tabStop&&state.width==108&&state.height>=32&&state.bounds.x>=24&&state.bounds.y>=12&&state.bounds.x+state.bounds.width<=400&&state.bounds.y+state.bounds.height<=160),"Scaled native Toggle style lost geometry or selection.");
                Require(toggleChanges==0,"Theme or rasterization scale changed a caller Toggle selection.");
                toggleObservations.Add(new{theme=theme.ToString(),requestedScale=scale,actualRasterizationScale=root.XamlRoot.RasterizationScale,states,toggleChanges});
                Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var bytes=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());var file=$"toggle-scale-{theme}-{scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}.png";
                using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,bytes);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream);await reader.LoadAsync((uint)stream.Size);var png=new byte[(int)stream.Size];reader.ReadBytes(png);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),png);toggleFrames.Add(new{theme=theme.ToString(),requestedScale=scale,width=bitmap.PixelWidth,height=bitmap.PixelHeight,file,sha256=Convert.ToHexStringLower(SHA256.HashData(png))});
                Require(bitmap.PixelWidth==width&&bitmap.PixelHeight==height&&Math.Abs(root.XamlRoot.RasterizationScale-scale)<.0001,"Toggle capture did not use the requested native rasterization scale.");
            }
            Guard();GC.KeepAlive(island);await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-content-scale-native-v3",passed=true,scope="owned hidden DesktopWindowXamlSource SiteBridge.OverrideScale for Separator, Status Badge, Switch and native Toggle styles; no physical monitor DPI or system text-scale change",observations,frames,toggleObservations,toggleFrames,guards,focus,activations,isolatedRasterizationScaleVerified=true,isolatedStatusBadgeRasterizationVerified=true,isolatedSwitchRasterizationVerified=true,isolatedToggleRasterizationVerified=true,displayScalingVerified=false,systemTextScalingVerified=false,osSettingsChanged=false,hardwareInputSent=false,nativeARM64RuntimeVerified=false,librarySha256=Hash(typeof(BleeckerSeparator).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-content-scale-native-v3",passed=false,stage,error=error.ToString(),observations,frames,toggleObservations,toggleFrames,guards,focus,activations,librarySha256=Hash(typeof(BleeckerSeparator).Assembly.Location),hostSha256=Hash(hostAssembly)}));Environment.Exit(1);}
    }
}
