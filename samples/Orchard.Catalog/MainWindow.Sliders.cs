using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Catalog;
public sealed partial class MainWindow
{
    private async Task RunSlidersAsync(string folder)
    {
        using var stream=CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.Slider.json")??throw new InvalidOperationException("Slider source reference is missing.");
        using var reference=await JsonDocument.ParseAsync(stream);var previous=Content;var originalSize=AppWindow.ClientSize;
        var canvas=(Canvas)XamlReader.Load("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' UseLayoutRounding='False'/>");
        var anchor=new Button{Width=20,Height=20,Opacity=0};Canvas.SetLeft(anchor,840);Canvas.SetTop(anchor,440);canvas.Children.Add(anchor);
        var frames=new List<object>();var keys=new List<object>();var history=new List<object>();var interactions=new List<object>();var captures=new Dictionary<string,string>();var rasterMatches=new Dictionary<string,bool>();
        object Box(FrameworkElement node){var b=node.TransformToVisual(canvas).TransformBounds(new Rect(0,0,node.ActualWidth,node.ActualHeight));return new{x=b.X,y=b.Y,width=b.Width,height=b.Height};}
        static double Number(JsonElement spec,string key,double otherwise)=>spec.TryGetProperty(key+"Kind",out var kind)?kind.GetString()switch{"nan"=>double.NaN,"infinity"=>double.PositiveInfinity,"negative-infinity"=>double.NegativeInfinity,_=>throw new InvalidOperationException("Invalid Slider number kind.")}:spec.TryGetProperty(key,out var value)?value.GetDouble():otherwise;
        static bool Flag(JsonElement spec,string key)=>spec.TryGetProperty(key,out var value)&&value.GetBoolean();
        static BleeckerSlider Create(JsonElement spec)=>new(){Width=Number(spec,"width",320),Min=Number(spec,"min",0),Max=Number(spec,"max",100),Step=Number(spec,"step",1),Value=Number(spec,"value",0),Label=spec.TryGetProperty("label",out var label)?label.GetString()!:"",ShowValue=Flag(spec,"showValue"),IsEnabled=!Flag(spec,"disabled"),FlowDirection=Flag(spec,"rtl")?FlowDirection.RightToLeft:FlowDirection.LeftToRight};
        static IRangeValueProvider Provider(BleeckerSlider slider)=>(IRangeValueProvider)FrameworkElementAutomationPeer.CreatePeerForElement(slider).GetPattern(PatternInterface.RangeValue);
        void Mount(BleeckerSlider slider){Canvas.SetLeft(slider,24);Canvas.SetTop(slider,24);canvas.Children.Add(slider);}
        FrameworkElement Part(BleeckerSlider slider,string name)=>Desc<FrameworkElement>(slider).Single(node=>node.Name==name);
        try
        {
            Content=canvas;AppWindow.ResizeClient(new SizeInt32(900,500));await AwaitPresentationAsync(canvas);using var input=new NativeInputSession(canvas.XamlRoot,folder,"slider");await input.AcquireAsync(canvas);AppWindow.ResizeClient(new SizeInt32(900,500));await AwaitPresentationAsync(canvas);await input.MoveAsync(anchor);
            foreach(var row in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                canvas.RequestedTheme=Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!);var spec=row.GetProperty("spec");var slider=Create(spec);AutomationProperties.SetName(slider,spec.TryGetProperty("name",out var name)?name.GetString()!:"Consumer range");Mount(slider);await AwaitPresentationAsync(slider);
                var header=Part(slider,"SliderHeader");var thumb=Part(slider,"SliderThumb");var peer=FrameworkElementAutomationPeer.CreatePeerForElement(slider);var provider=Provider(slider);
                frames.Add(new{id=row.GetProperty("id").GetString(),scheme=row.GetProperty("scheme").GetString(),measurement=new{outer=Box(slider),row=Box(Part(slider,"SliderInput")),track=Box(Part(slider,"SliderTrack")),fill=Box(Part(slider,"SliderFill")),header=header.Visibility==Visibility.Visible?Box(header):null,
                    label=header.Visibility==Visibility.Visible?new{box=Box(Part(slider,"SliderLabel")),text=slider.Label}:null,display=slider.ShowValue?new{box=Box(Part(slider,"SliderValue")),text=Desc<TextBlock>(slider).Single(node=>node.Name=="SliderValueText").Text}:null,
                    actualValue=provider.Value,minimum=provider.Minimum,maximum=provider.Maximum,smallChange=provider.SmallChange,disabled=!slider.IsEnabled,name=peer.GetName(),role=peer.GetAutomationControlType().ToString(),readOnly=provider.IsReadOnly,tabStop=slider.IsTabStop,thumb=new{box=Box(thumb),opacity=thumb.Opacity}}});
                if(row.TryGetProperty("file",out var file))
                {
                    await File.WriteAllTextAsync(Path.Combine(folder,"slider-progress.json"),JsonSerializer.Serialize(new{passed=false,frames},new JsonSerializerOptions{WriteIndented=true}));
                    using var png=CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.Slider."+file.GetString())??throw new InvalidOperationException("Slider source PNG missing.");using var bytes=new MemoryStream();await png.CopyToAsync(bytes);Require(Convert.ToHexStringLower(SHA256.HashData(bytes.ToArray()))==row.GetProperty("pngSha256").GetString(),"Slider source PNG hash differs.");
                    using var random=new Windows.Storage.Streams.InMemoryRandomAccessStream();using(var writer=new Windows.Storage.Streams.DataWriter(random)){writer.WriteBytes(bytes.ToArray());await writer.StoreAsync();writer.DetachStream();}random.Seek(0);var decoder=await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(random);var pixels=(await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,Windows.Graphics.Imaging.BitmapAlphaMode.Straight,new Windows.Graphics.Imaging.BitmapTransform(),Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation,Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
                    var capture=Path.Combine(folder,file.GetString()!);try{await NativeScreenCapture.CaptureExactClientAsync(input.Window,capture,pixels);rasterMatches[file.GetString()!]=true;}catch(InvalidOperationException error)when(error.Message=="The composed capture differs from its complete exact reference frame."){rasterMatches[file.GetString()!]=false;}captures[file.GetString()!]=Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(capture)));
                }
                canvas.Children.Remove(slider);
            }
            foreach(var row in reference.RootElement.GetProperty("interactions").EnumerateArray())
            {
                canvas.RequestedTheme=Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!);var spec=row.GetProperty("spec");var slider=Create(spec);var requests=new List<double>();var frozen=Flag(spec,"freeze");slider.ValueRequested+=(_,args)=>{requests.Add(args.Value);if(!frozen)slider.Value=args.Value;};Mount(slider);await AwaitPresentationAsync(slider);Require(slider.Focus(FocusState.Keyboard),"Slider could not receive keyboard focus.");var before=Provider(slider).Value;var samples=new List<object>();
                foreach(var sample in row.GetProperty("samples").EnumerateArray())
                {
                    var key=sample.GetProperty("key").GetString();var code=key switch{"ArrowLeft"=>0x25,"ArrowRight"=>0x27,"ArrowUp"=>0x26,"ArrowDown"=>0x28,"PageUp"=>0x21,"PageDown"=>0x22,"Home"=>0x24,"End"=>0x23,_=>throw new InvalidOperationException("Unknown source range key.")};await input.KeyAsync((ushort)code,slider);await AwaitPresentationAsync(slider);samples.Add(new{key,value=Provider(slider).Value});
                }
                keys.Add(new{id=row.GetProperty("id").GetString(),scheme=row.GetProperty("scheme").GetString(),before,samples,requests,focusRetained=ReferenceEquals(FocusManager.GetFocusedElement(canvas.XamlRoot),slider)});canvas.Children.Remove(slider);
            }
            foreach(var scheme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                canvas.RequestedTheme=scheme;BleeckerSlider? slider=null;
                foreach(var row in reference.RootElement.GetProperty("history").EnumerateArray().Where(row=>row.GetProperty("scheme").GetString()==scheme.ToString()))
                {
                    var spec=row.GetProperty("spec");if(slider is null){slider=Create(spec);Mount(slider);}else{slider.Min=Number(spec,"min",0);slider.Max=Number(spec,"max",100);slider.Step=Number(spec,"step",1);slider.Value=Number(spec,"value",0);slider.Width=Number(spec,"width",320);}await AwaitPresentationAsync(slider);history.Add(new{id=row.GetProperty("id").GetString(),scheme=scheme.ToString(),actualValue=Provider(slider).Value,fillWidth=Part(slider,"SliderFill").ActualWidth});
                }
                if(slider is not null)canvas.Children.Remove(slider);
                var producer=new TextBlock{Tag=25d};slider=new BleeckerSlider{Width=320,Label="Bound range",ShowValue=true};slider.SetBinding(BleeckerSlider.ValueProperty,new Binding{Source=producer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});var requests=new List<double>();slider.ValueRequested+=(_,args)=>requests.Add(args.Value);Mount(slider);await AwaitPresentationAsync(slider);
                await input.ClickAsync(Part(slider,"SliderTrack"),localPoint:new Point(240,4));Require(requests.Count==1&&slider.Value==25&&Provider(slider).Value==25,"Slider mutated its controlled value or lost a track request.");
                var last=requests[^1];producer.Tag=last;await AwaitPresentationAsync(slider);Require(slider.Value==last&&slider.GetBindingExpression(BleeckerSlider.ValueProperty)is not null,"Slider replaced its consumer binding.");
                var thumb=Part(slider,"SliderThumb");await input.ClickAsync(thumb);Require(slider.Focus(FocusState.Keyboard),"Slider lost keyboard focus after pointer input.");await input.KeyAsync(0x25,slider);Require(requests.Count==2,"Slider did not emit one physical key request.");
                var provider=Provider(slider);var count=requests.Count;provider.SetValue(50);Require(requests.Count==count+1&&slider.Value==last,"Slider automation mutated the caller's controlled value.");
                slider.IsEnabled=false;await AwaitPresentationAsync(slider);var disabledRejected=false;try{provider.SetValue(60);}catch(ElementNotEnabledException){disabledRejected=true;}Require(disabledRejected&&requests.Count==count+1,"Disabled Slider accepted an automation request.");slider.IsEnabled=true;
                var template=slider.Template;slider.Template=null;await AwaitPresentationAsync(canvas);slider.Template=template;await AwaitPresentationAsync(slider);producer.Tag=70d;await AwaitPresentationAsync(slider);Require(slider.Value==70&&slider.GetBindingExpression(BleeckerSlider.ValueProperty)is not null,"Slider template replacement lost its binding.");
                AutomationProperties.SetName(slider,"Caller name");Require(FrameworkElementAutomationPeer.CreatePeerForElement(slider).GetName()=="Caller name","Slider replaced caller naming metadata.");AutomationProperties.SetName(slider,"");Require(FrameworkElementAutomationPeer.CreatePeerForElement(slider).GetName()=="","Slider ignored an explicit empty name.");slider.ClearValue(AutomationProperties.NameProperty);Require(FrameworkElementAutomationPeer.CreatePeerForElement(slider).GetName()=="Bound range","Slider did not associate its visible label.");
                var originalStyle=slider.Style;var namingBase=new Style(typeof(BleeckerSlider)){BasedOn=originalStyle};namingBase.Setters.Add(new Setter(AutomationProperties.NameProperty,""));var namingStyle=new Style(typeof(BleeckerSlider)){BasedOn=namingBase};var styleProducer=new TextBlock{Tag=namingStyle};slider.SetBinding(FrameworkElement.StyleProperty,new Binding{Source=styleProducer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});await AwaitPresentationAsync(slider);Require(FrameworkElementAutomationPeer.CreatePeerForElement(slider).GetName()=="","Slider replaced an empty name from the caller's BasedOn style.");styleProducer.Tag=originalStyle;await AwaitPresentationAsync(slider);Require(FrameworkElementAutomationPeer.CreatePeerForElement(slider).GetName()=="Bound range"&&slider.GetBindingExpression(FrameworkElement.StyleProperty)is not null,"Slider style removal lost its label or caller binding.");
                var stale=Provider(slider);canvas.Children.Remove(slider);await AwaitPresentationAsync(canvas);var staleRejected=false;try{stale.SetValue(60);}catch(ElementNotAvailableException){staleRejected=true;}Require(staleRejected,"Detached Slider provider remained actionable.");canvas.Children.Add(slider);await AwaitPresentationAsync(slider);producer.Tag=80d;await AwaitPresentationAsync(slider);Require(slider.Value==80&&Provider(slider).Value==80,"Slider remount lost live data.");
                canvas.Children.Remove(slider);
                var dragSlider=new BleeckerSlider{Width=320,Value=25,Label="Drag range",ShowValue=true};var dragRequests=new List<double>();var acceptDrag=true;dragSlider.ValueRequested+=(_,args)=>{dragRequests.Add(args.Value);if(acceptDrag)dragSlider.Value=args.Value;};Mount(dragSlider);await AwaitPresentationAsync(dragSlider);
                async Task Drag()
                {
                    var target=Part(dragSlider,"SliderThumb");var bounds=target.TransformToVisual(canvas).TransformBounds(new Rect(0,0,target.ActualWidth,target.ActualHeight));var from=new Point(bounds.X+10,bounds.Y+10);await input.MoveAsync(canvas,localPoint:from);await input.PressPointerAsync();
                    try{for(var index=1;index<=4;index++)await input.MoveAsync(canvas,localPoint:new Point(from.X+index*20,from.Y));}finally{input.PointerButton(false);}await AwaitPresentationAsync(dragSlider);
                }
                await Drag();Require(dragRequests.Count>0&&dragSlider.Value>25,"Slider did not deliver real native thumb dragging.");var acceptedDrag=dragSlider.Value;acceptDrag=false;dragSlider.Value=25;dragRequests.Clear();await AwaitPresentationAsync(dragSlider);await Drag();Require(dragRequests.Count>0&&dragSlider.Value==25&&Provider(dragSlider).Value==25,"Slider dragging did not preserve a rejected controlled value.");canvas.Children.Remove(dragSlider);
                interactions.Add(new{scheme=scheme.ToString(),passed=true,trackRequest=true,keyRequest=true,automationRequest=true,bindingRetained=true,disabledRejected,templateReplacement=true,metadataRetained=true,staleRejected,remount=true,acceptedDrag,frozenDrag=true});
            }
            await File.WriteAllTextAsync(Path.Combine(folder,"slider.json"),JsonSerializer.Serialize(new{passed=true,frames,keys,history,interactions,captures,rasterMatches,sourceRasterParityVerified=rasterMatches.Values.All(value=>value)},new JsonSerializerOptions{WriteIndented=true}));await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="slider",environment=RuntimeEnvironment()}));
        }
        finally{Content=previous;AppWindow.ResizeClient(originalSize);await AwaitPresentationAsync(CatalogRoot);}
    }
}
