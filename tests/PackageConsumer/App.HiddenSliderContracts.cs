using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Orchard;
using Windows.Foundation;

namespace PackageConsumer;

public partial class App
{
    private async void RunHiddenSliderContractsAsync(string referenceDirectory)
    {
        var arguments=Environment.GetCommandLineArgs();var outputIndex=Array.IndexOf(arguments,"--verify-output");
        if(outputIndex<0||outputIndex+1>=arguments.Length){Environment.Exit(2);return;}
        var output=arguments[outputIndex+1];var before=GetForegroundWindow();var handle=nint.Zero;var frames=new List<object>();var history=new List<object>();var checks=new List<object>();var failures=new List<string>();
        var completed=false;var guardChecks=0;var activationEvents=0;var enabledRangeRequests=0;string? failure=null;string? isolationFailure=null;string? referenceHash=null;
        void Guard()
        {
            guardChecks++;GetWindowThreadProcessId(GetForegroundWindow(),out var process);
            if(isolationFailure is not null||window!.Visible||window.AppWindow.IsVisible||IsWindowVisible(handle)||process==Environment.ProcessId)
                throw new InvalidOperationException(isolationFailure??"Hidden Slider test became visible or owned foreground input.");
        }
        static void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        void Check(string id,Action action){Guard();try{action();checks.Add(new{id,passed=true});}catch(Exception error){failures.Add(id+": "+error.Message);checks.Add(new{id,passed=false,error=error.Message});}Guard();}
        static double Number(JsonElement spec,string key,double otherwise)=>spec.TryGetProperty(key+"Kind",out var kind)?kind.GetString()switch{"nan"=>double.NaN,"infinity"=>double.PositiveInfinity,"negative-infinity"=>double.NegativeInfinity,_=>throw new InvalidOperationException("Invalid Slider number kind.")}:spec.TryGetProperty(key,out var value)?value.GetDouble():otherwise;
        static bool Flag(JsonElement spec,string key)=>spec.TryGetProperty(key,out var value)&&value.GetBoolean();
        static BleeckerSlider Create(JsonElement spec)=>new(){Width=Number(spec,"width",320),Min=Number(spec,"min",0),Max=Number(spec,"max",100),Step=Number(spec,"step",1),Value=Number(spec,"value",0),Label=spec.TryGetProperty("label",out var label)?label.GetString()!:"",ShowValue=Flag(spec,"showValue"),IsEnabled=!Flag(spec,"disabled"),FlowDirection=Flag(spec,"rtl")?FlowDirection.RightToLeft:FlowDirection.LeftToRight};
        static IRangeValueProvider Range(BleeckerSlider slider)=>(IRangeValueProvider)FrameworkElementAutomationPeer.CreatePeerForElement(slider).GetPattern(PatternInterface.RangeValue);
        static object? Field(BleeckerSlider slider,string name)=>typeof(BleeckerSlider).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(slider);
        static string Raw(double value)=>value.ToString("R",CultureInfo.InvariantCulture);
        try
        {
            // Only this invisible window is created. No MainWindow, Activate,
            // Show, Focus, pointer/key sender or OS-setting mutation participates.
            window=new Window();handle=WinRT.Interop.WindowNative.GetWindowHandle(window);Guard();
            window.Activated+=(_,args)=>{if(args.WindowActivationState!=WindowActivationState.Deactivated){activationEvents++;isolationFailure="Hidden Slider window activated.";}};
            window.VisibilityChanged+=(_,args)=>{if(args.Visible)isolationFailure="Hidden Slider window became visible.";};
            var root=new Canvas{Width=900,Height=500,UseLayoutRounding=false};window.Content=root;Guard();
            async Task Layout(FrameworkElement element)
            {
                for(var i=0;i<3;i++){root.Measure(new Size(900,500));root.Arrange(new Rect(0,0,900,500));root.UpdateLayout();if(element is Control control)control.ApplyTemplate();Guard();await Task.Delay(10);}
                Assert(element.IsLoaded,"Hidden Slider did not enter the real XAML tree.");Guard();
            }
            void Mount(BleeckerSlider slider){Canvas.SetLeft(slider,24);Canvas.SetTop(slider,24);root.Children.Add(slider);}
            FrameworkElement Part(BleeckerSlider slider,string name)=>HiddenDesc<FrameworkElement>(slider).Single(element=>element.Name==name);
            object Box(FrameworkElement element){var box=element.TransformToVisual(root).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));return new{x=box.X,y=box.Y,width=box.Width,height=box.Height};}
            object Paint(Brush? brush)=>brush is SolidColorBrush color?new{r=(int)color.Color.R,g=(int)color.Color.G,b=(int)color.Color.B,a=(int)color.Color.A,opacity=color.Opacity}:throw new InvalidOperationException("Slider test expected a solid brush.");
            object Snapshot(BleeckerSlider slider)
            {
                var header=Part(slider,"SliderHeader");var thumb=Part(slider,"SliderThumb");var peer=FrameworkElementAutomationPeer.CreatePeerForElement(slider);var range=Range(slider);var track=(Border)Part(slider,"SliderTrack");var fill=(Border)Part(slider,"SliderFill");var border=(Border)Part(slider,"SliderThumbBorder");
                return new{outer=Box(slider),row=Box(Part(slider,"SliderInput")),track=Box(track),fill=Box(fill),header=header.Visibility==Visibility.Visible?Box(header):null,
                    label=header.Visibility==Visibility.Visible?new{box=Box(Part(slider,"SliderLabel")),text=slider.Label}:null,
                    display=slider.ShowValue?new{box=Box(Part(slider,"SliderValue")),text=HiddenDesc<TextBlock>(slider).Single(text=>text.Name=="SliderValueText").Text}:null,
                    thumb=new{box=Box(thumb),opacity=thumb.Opacity},actualValue=range.Value,minimum=range.Minimum,maximum=range.Maximum,smallChange=range.SmallChange,largeChange=range.LargeChange,rawValue=Raw(slider.Value),disabled=!slider.IsEnabled,name=peer.GetName(),role=peer.GetAutomationControlType().ToString(),readOnly=range.IsReadOnly,tabStop=slider.IsTabStop,loaded=slider.IsLoaded,hostVisible=slider.XamlRoot.IsHostVisible,
                    trackPaint=Paint(track.Background),fillPaint=Paint(fill.Background),thumbPaint=Paint(border.Background),thumbBorderPaint=Paint(border.BorderBrush),thumbBorderThickness=border.BorderThickness.Left,renderingActive=(bool)Field(slider,"rendering")!};
            }
            var bytes=await File.ReadAllBytesAsync(Path.Combine(referenceDirectory,"Slider.json"));referenceHash=Convert.ToHexStringLower(SHA256.HashData(bytes));using var reference=JsonDocument.Parse(bytes);
            Assert(reference.RootElement.GetProperty("sourceSha256").GetString()=="b5ab4cb0ed7af06317843f42934f640a90da62eed20c0fc59a166e071faf6b6a","Unexpected Slider source reference.");
            foreach(var row in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                root.RequestedTheme=Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!);var spec=row.GetProperty("spec");var slider=Create(spec);AutomationProperties.SetName(slider,spec.TryGetProperty("name",out var name)?name.GetString()!:"Consumer range");root.Children.Clear();Mount(slider);await Layout(slider);
                var measurement=Snapshot(slider);object? lateMeasurement=null;
                if(Math.Abs(Part(slider,"SliderFill").ActualWidth-row.GetProperty("measurement").GetProperty("fill").GetProperty("width").GetDouble())>=.001){await Task.Delay(220);Guard();await Layout(slider);lateMeasurement=Snapshot(slider);}
                frames.Add(new{id=row.GetProperty("id").GetString(),scheme=row.GetProperty("scheme").GetString(),measurement,lateMeasurement});root.Children.Remove(slider);root.UpdateLayout();await Task.Delay(1);Guard();
            }
            Assert(frames.Count==182,"Incomplete hidden Slider matrix.");
            foreach(var scheme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                root.RequestedTheme=scheme;root.Children.Clear();BleeckerSlider? historySlider=null;
                foreach(var row in reference.RootElement.GetProperty("history").EnumerateArray().Where(row=>row.GetProperty("scheme").GetString()==scheme.ToString()))
                {
                    var spec=row.GetProperty("spec");if(historySlider is null){historySlider=Create(spec);Mount(historySlider);}else{historySlider.Min=Number(spec,"min",0);historySlider.Max=Number(spec,"max",100);historySlider.Step=Number(spec,"step",1);historySlider.Value=Number(spec,"value",0);historySlider.Width=Number(spec,"width",320);}await Layout(historySlider);await Task.Delay(220);Guard();await Layout(historySlider);
                    history.Add(new{id=row.GetProperty("id").GetString(),scheme=scheme.ToString(),actualValue=Range(historySlider).Value,fillWidth=Part(historySlider,"SliderFill").ActualWidth,hostVisible=historySlider.XamlRoot.IsHostVisible,renderingActive=(bool)Field(historySlider,"rendering")!});
                }
                root.Children.Clear();
                var producer=new TextBlock{Tag=25.3};var minimum=new TextBlock{Tag=0d};var maximum=new TextBlock{Tag=100d};var step=new TextBlock{Tag=10d};var enabled=new TextBlock{Tag=true};var slider=new BleeckerSlider{Width=320,Label="Bound range",ShowValue=true};
                foreach(var(property,source)in new[]{(BleeckerSlider.ValueProperty,producer),(BleeckerSlider.MinProperty,minimum),(BleeckerSlider.MaxProperty,maximum),(BleeckerSlider.StepProperty,step),(Control.IsEnabledProperty,enabled)})slider.SetBinding(property,new Binding{Source=source,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});
                var requests=new List<double>();var accept=false;slider.ValueRequested+=(_,args)=>{requests.Add(args.Value);if(accept)producer.Tag=args.Value;};Mount(slider);await Layout(slider);var peer=FrameworkElementAutomationPeer.CreatePeerForElement(slider);var range=Range(slider);var prefix=scheme+"/";
                Check(prefix+"original-and-normalized-values",()=>Assert(slider.Value==25.3&&range.Value==30&&range.Minimum==0&&range.Maximum==100&&range.SmallChange==10&&HiddenDesc<TextBlock>(slider).Single(text=>text.Name=="SliderValueText").Text=="25.3","Original and normalized values were conflated."));
                range.SetValue(30);Check(prefix+"same-normalized-value",()=>Assert(requests.Count==0&&slider.Value==25.3,"Equivalent native value emitted a request."));
                range.SetValue(60);enabledRangeRequests++;await Layout(slider);Check(prefix+"rejected-request",()=>Assert(requests.SequenceEqual(new[]{60d})&&slider.Value==25.3&&range.Value==30&&slider.GetBindingExpression(BleeckerSlider.ValueProperty)is not null,"Rejected range request mutated the consumer's binding."));
                accept=true;range.SetValue(70);enabledRangeRequests++;await Layout(slider);Check(prefix+"accepted-request",()=>Assert(requests.SequenceEqual(new[]{60d,70d})&&slider.Value==70&&range.Value==70,"Accepted range request did not reach the consumer model."));accept=false;
                maximum.Tag=50d;step.Tag=4d;await Layout(slider);Check(prefix+"live-bounds-and-step",()=>Assert(slider.Value==70&&range.Value==48&&range.Maximum==50&&range.SmallChange==4&&range.LargeChange==5&&requests.Count==2,"Live bounds rewrote original data or stale metadata remained."));
                minimum.Tag=-20d;maximum.Tag=40d;step.Tag=7d;await Layout(slider);Check(prefix+"bound-constraints",()=>Assert(range.Minimum==-20&&range.Maximum==40&&range.SmallChange==7&&range.Value==36&&range.LargeChange==7&&slider.Value==70,"Bound range constraints did not remain authoritative."));
                var invalidRejected=0;foreach(var value in new[]{double.NaN,double.PositiveInfinity,-21d,41d}){try{range.SetValue(value);}catch(ArgumentOutOfRangeException){invalidRejected++;}}Check(prefix+"invalid-requests",()=>Assert(invalidRejected==4&&requests.Count==2&&slider.Value==70,"Invalid automation request crossed the consumer boundary."));
                var nameProducer=new TextBlock{Tag="Caller range"};slider.SetBinding(AutomationProperties.NameProperty,new Binding{Source=nameProducer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});slider.Label="Changed range label";await Layout(slider);Check(prefix+"name-binding",()=>Assert(peer.GetName()=="Caller range"&&slider.GetBindingExpression(AutomationProperties.NameProperty)is not null,"Label update replaced caller name binding."));
                nameProducer.Tag="";await Layout(slider);Check(prefix+"empty-name-binding",()=>Assert(peer.GetName()=="","Explicit empty name binding was replaced."));slider.ClearValue(AutomationProperties.NameProperty);
                var originalStyle=slider.Style;var callerBase=new Style(typeof(BleeckerSlider)){BasedOn=originalStyle};callerBase.Setters.Add(new Setter(AutomationProperties.NameProperty,""));var callerStyle=new Style(typeof(BleeckerSlider)){BasedOn=callerBase};var styleProducer=new TextBlock{Tag=callerStyle};slider.SetBinding(FrameworkElement.StyleProperty,new Binding{Source=styleProducer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});await Layout(slider);peer.InvalidatePeer();Check(prefix+"empty-style-name",()=>Assert(peer.GetName()=="","Explicit empty styled name was replaced by the visible label."));
                styleProducer.Tag=originalStyle;await Layout(slider);Check(prefix+"style-removal-name",()=>Assert(peer.GetName()=="Changed range label"&&slider.GetBindingExpression(FrameworkElement.StyleProperty)is not null,"Style removal lost name defaults or caller style binding."));
                var label=new TextBlock{Text="External range label"};root.Children.Add(label);AutomationProperties.SetLabeledBy(slider,label);await Layout(slider);Check(prefix+"caller-label",()=>Assert(peer.GetName()=="External range label","Caller LabeledBy did not name the range."));slider.ClearValue(AutomationProperties.LabeledByProperty);
                var template=slider.Template;slider.Template=null;await Layout(slider);Check(prefix+"template-detach-resources",()=>Assert(Field(slider,"input")is null&&Field(slider,"fillClip")is null&&Field(slider,"clipGeometry")is null&&((ICollection)Field(slider,"shadows")!).Count==0&&!(bool)Field(slider,"rendering")!,"Template detach retained input, clips, shadows or rendering callbacks."));
                slider.Template=template;producer.Tag=22d;await Layout(slider);Check(prefix+"template-binding",()=>Assert(slider.Value==22&&Range(slider).Value==22&&slider.GetBindingExpression(BleeckerSlider.ValueProperty)is not null&&requests.Count==2,"Template replacement lost the original consumer value."));
                enabled.Tag=false;producer.Tag=29d;await Layout(slider);var disabledRejected=false;try{range.SetValue(8);}catch(ElementNotEnabledException){disabledRejected=true;}Check(prefix+"disabled-request",()=>Assert(disabledRejected&&requests.Count==2&&slider.Value==29&&!slider.IsEnabled&&Part(slider,"SliderThumb").Opacity==.5,"Disabled range accepted a request or lost its displayed state."));enabled.Tag=true;await Layout(slider);
                root.Children.Remove(slider);root.UpdateLayout();await Task.Delay(20);Guard();var staleRejected=false;try{range.SetValue(8);}catch(ElementNotAvailableException){staleRejected=true;}Check(prefix+"detached-request-and-observers",()=>Assert(staleRejected&&requests.Count==2&&!(bool)Field(slider,"rendering")!&&((ICollection)Field(slider,"observers")!).Count==0,"Detached range retained requests, observers or active timelines."));
                minimum.Tag=0d;maximum.Tag=100d;step.Tag=5d;producer.Tag=42d;Mount(slider);await Layout(slider);Check(prefix+"remount-binding",()=>Assert(slider.Value==42&&Range(slider).Value==40&&slider.IsEnabled&&requests.Count==2&&new[]{BleeckerSlider.ValueProperty,BleeckerSlider.MinProperty,BleeckerSlider.MaxProperty,BleeckerSlider.StepProperty,Control.IsEnabledProperty}.All(property=>slider.GetBindingExpression(property)is not null),"Remount lost consumer values or bindings."));
                slider.Visibility=Visibility.Collapsed;await Task.Delay(10);Guard();Check(prefix+"collapsed-timelines",()=>Assert(!(bool)Field(slider,"rendering")!,"Collapsed range retained its rendering callback."));slider.Visibility=Visibility.Visible;await Layout(slider);root.Children.Clear();Guard();
            }
            completed=failures.Count==0;
        }
        catch(Exception error){failure=error.ToString();}
        finally
        {
            var visible=window?.Visible??false;var nativeVisible=handle!=nint.Zero&&IsWindowVisible(handle);GetWindowThreadProcessId(GetForegroundWindow(),out var foregroundProcess);var isolated=!visible&&!nativeVisible&&activationEvents==0&&isolationFailure is null&&foregroundProcess!=Environment.ProcessId;
            var assembly=typeof(BleeckerSlider).Assembly.Location;var librarySha256=Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(assembly)));
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{passed=completed&&isolated,scope="hidden-slider-contracts",hiddenWindow=true,activateCalled=false,showCalled=false,focusCalled=false,inputSent=false,enabledRangeRequests,visible,nativeVisible,activationEvents,guardChecks,isolated,foregroundBefore=(long)before,foregroundAfter=(long)GetForegroundWindow(),processArchitecture=RuntimeInformation.ProcessArchitecture.ToString(),osArchitecture=RuntimeInformation.OSArchitecture.ToString(),displayScale=handle==nint.Zero?0:GetDpiForWindow(handle)/96d,referenceSha256=referenceHash,librarySha256,frames,history,checks,failures,failure,isolationFailure,visibleRuntimeVerified=false,physicalInputVerified=false,sourceRasterParityVerified=false,normalMotionVerified=false,nativeComponentAccepted=false},new JsonSerializerOptions{WriteIndented=true}));
            window?.Close();Environment.Exit(completed&&isolated?0:1);
        }
    }
}
