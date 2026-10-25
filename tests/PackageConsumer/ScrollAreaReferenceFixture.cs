using System.ComponentModel;
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
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.System;

namespace Orchard.Testing;

// Hidden layout/ownership/rendering diagnostics. Private production methods and
// native visual states receive controlled inputs; no physical input is sent.
internal static class ScrollAreaReferenceFixture
{
    private delegate bool WindowVisitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    [Microsoft.UI.Xaml.Data.Bindable] private sealed class Model : INotifyPropertyChanged
    {
        private double width=520,height=480;private string text="Original";
        public double Width { get=>width;set{width=value;PropertyChanged?.Invoke(this,new(nameof(Width)));} }
        public double Height { get=>height;set{height=value;PropertyChanged?.Invoke(this,new(nameof(Height)));} }
        public string Text { get=>text;set{text=value;PropertyChanged?.Invoke(this,new(nameof(Text)));} }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Selector(DataTemplate template) : DataTemplateSelector
    {
        internal readonly List<object> Items=[];
        protected override DataTemplate SelectTemplateCore(object item){Items.Add(item);return template;}
        protected override DataTemplate SelectTemplateCore(object item,DependencyObject container)=>SelectTemplateCore(item);
    }
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    {
        if(node is FrameworkElement element)yield return element;
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;
    }
    private static double N(JsonElement row,string name,double fallback)=>row.TryGetProperty(name,out var value)?value.GetDouble():fallback;
    private static bool F(JsonElement row,string name)=>row.TryGetProperty(name,out var value)&&value.GetBoolean();
    private static double Maximum(JsonElement spec)
    {
        var has=spec.TryGetProperty("styleMaxHeight",out var value);if(!has)has=spec.TryGetProperty("maxHeight",out value);if(!has)return 240;if(value.ValueKind==JsonValueKind.Null)return double.PositiveInfinity;var text=value.GetString()!;
        if(text.EndsWith("px",StringComparison.Ordinal))return double.Parse(text[..^2],CultureInfo.InvariantCulture);
        if(text.EndsWith("rem",StringComparison.Ordinal))return double.Parse(text[..^3],CultureInfo.InvariantCulture)*16;
        if(text.EndsWith('%'))return double.Parse(text[..^1],CultureInfo.InvariantCulture)/100*N(spec,"parentHeight",0);
        throw new FormatException("Unsupported frozen fixture length.");
    }
    private static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private static void Wheel(BleeckerScrollArea owner,double delta,bool horizontal=false)=>typeof(BleeckerScrollArea).GetMethod("ProcessWheel",Private)!.Invoke(owner,[delta,horizontal]);
    private static void Key(BleeckerScrollArea owner,VirtualKey key)=>typeof(BleeckerScrollArea).GetMethod("ProcessKey",Private)!.Invoke(owner,[key]);
    private static object? Brush(Brush? value)=>value is SolidColorBrush brush?new{color=$"#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}",opacity=brush.Opacity}:null;
    internal static async Task RunAsync(Window window,string referencePath,string output,string hostPath)
    {
        var referenceBytes=await File.ReadAllBytesAsync(referencePath);using var reference=JsonDocument.Parse(referenceBytes);
        var root=new Canvas{Width=800,Height=700,UseLayoutRounding=false};window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(800,700));
        var frames=new List<object>();var histories=new List<object>();var paints=new List<object>();var captures=new List<object>();var checks=new List<object>();var failures=new List<object>();var guards=0;var activations=0;var focus=0;
        window.Activated+=(_,args)=>{if(args.WindowActivationState!=WindowActivationState.Deactivated)activations++;};root.GotFocus+=(_,_)=>focus++;
        void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((handle,_)=>{GetWindowThreadProcessId(handle,out var process);visible|=process==Environment.ProcessId&&IsWindowVisible(handle);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&activations==0&&focus==0,"Hidden ScrollArea acquired visibility, activation or focus.");}
        async Task Layout(){for(var tick=0;tick<6;tick++){root.Measure(new Size(800,700));root.Arrange(new Rect(0,0,800,700));root.UpdateLayout();Guard();var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Could not drain ScrollArea layout.");await idle.Task;await Task.Delay(15);}Require(root.XamlRoot is{IsHostVisible:false,RasterizationScale:1}&&root.XamlRoot.Size==new Size(800,700),"ScrollArea requires its actual hidden unscaled viewport.");Require(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count==0,"ScrollArea introduced a popup.");}
        object Box(FrameworkElement element){var b=element.TransformToVisual(root).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));return new{x=b.X,y=b.Y,width=b.Width,height=b.Height};}
        object Snapshot(BleeckerScrollArea owner,FrameworkElement? content)=>new{area=Box(owner),content=content is null?null:Box(content),client=new{width=owner.ViewportWidth,height=owner.ViewportHeight},scroll=new{width=owner.ExtentWidth,height=owner.ExtentHeight,left=owner.FlowDirection==FlowDirection.RightToLeft?-owner.HorizontalOffset:owner.HorizontalOffset,top=owner.VerticalOffset},originalRetained=ReferenceEquals(owner.Content,content)};
        void Theme(string scheme){root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);root.Background=new SolidColorBrush(scheme=="Light"?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));}
        async Task Case(string id,Func<Task> action){try{root.Children.Clear();await action();Guard();checks.Add(new{id,passed=true});}catch(Exception error){failures.Add(new{id,error=error.ToString()});checks.Add(new{id,passed=false});}}
        async Task Capture(string name)
        {
            var bitmap=new RenderTargetBitmap();var operation=bitmap.RenderAsync(root);var task=operation.AsTask();if(await Task.WhenAny(task,Task.Delay(2000))!=task){operation.Cancel();throw new TimeoutException("ScrollArea complete-client capture exceeded two seconds.");}await task;Guard();Require(bitmap.PixelWidth==800&&bitmap.PixelHeight==700,"ScrollArea capture was rescaled.");
            var buffer=await bitmap.GetPixelsAsync();var pixels=new byte[buffer.Length];using(var reader=Windows.Storage.Streams.DataReader.FromBuffer(buffer))reader.ReadBytes(pixels);
            using var stream=new Windows.Storage.Streams.InMemoryRandomAccessStream();var encoder=await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied,800,700,96,96,pixels);await encoder.FlushAsync();using var pngReader=new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0));await pngReader.LoadAsync((uint)stream.Size);var bytes=new byte[stream.Size];pngReader.ReadBytes(bytes);var file=name+".png";Directory.CreateDirectory(Path.GetDirectoryName(output)!);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),bytes);captures.Add(new{name,file,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes)),width=800,height=700,scope="complete-native-client",completeSourceRasterCompared=false});
        }
        BleeckerScrollArea Mount(JsonElement spec,out Border? content,out Border following)
        {
            var empty=F(spec,"empty");content=empty?null:new Border{Name="OriginalScrollContent",Width=spec.TryGetProperty("contentWidth",out var width)&&width.ValueKind==JsonValueKind.Null?double.NaN:N(spec,"contentWidth",520),Height=N(spec,"contentHeight",480),Background=root.Background};
            var owner=new BleeckerScrollArea{Width=N(spec,"width",256),MaxHeight=Maximum(spec),Content=content,FlowDirection=F(spec,"rtl")?FlowDirection.RightToLeft:FlowDirection.LeftToRight};
            owner.Orientation=spec.TryGetProperty("styleOverflow",out _)?BleeckerScrollAreaOrientation.Both:spec.TryGetProperty("orientation",out var orientation)?Enum.Parse<BleeckerScrollAreaOrientation>(orientation.GetString()!,true):BleeckerScrollAreaOrientation.Vertical;
            if(spec.TryGetProperty("height",out var height))owner.Height=height.GetDouble();if(F(spec,"custom")){owner.Padding=new Thickness(8);owner.BorderThickness=new Thickness(1);owner.BorderBrush=new SolidColorBrush(Microsoft.UI.Colors.Gray);}
            AutomationProperties.SetName(owner,"Fictional scroll area");owner.Tag="original-scroll-area";if(spec.TryGetProperty("tabIndex",out var tab))owner.IsTabStop=tab.GetInt32()>=0;
            var host=new StackPanel{Width=owner.Width,UseLayoutRounding=false};Canvas.SetLeft(host,N(spec,"x",100));Canvas.SetTop(host,N(spec,"y",100));following=new Border{Width=20,Height=20,HorizontalAlignment=F(spec,"rtl")?HorizontalAlignment.Right:HorizontalAlignment.Left,Background=new SolidColorBrush(Microsoft.UI.Colors.Blue)};host.Children.Add(owner);host.Children.Add(following);root.Children.Add(host);return owner;
        }
        await Case("source-layout-matrix",async()=>
        {
            foreach(var row in reference.RootElement.GetProperty("cases").EnumerateArray()){root.Children.Clear();Theme(row.GetProperty("scheme").GetString()!);var owner=Mount(row.GetProperty("spec"),out var content,out var following);await Layout();frames.Add(new{id=row.GetProperty("id").GetString(),scheme=row.GetProperty("scheme").GetString(),actualTheme=owner.ActualTheme.ToString(),measurement=Snapshot(owner,content),following=Box(following),name=AutomationProperties.GetName(owner),tag=owner.Tag});if(row.GetProperty("id").GetString()=="zero-maximum")await Capture(row.GetProperty("scheme").GetString()+"-zero-maximum");}
        });
        await Case("source-scroll-and-live-change-histories",async()=>
        {
            foreach(var row in reference.RootElement.GetProperty("interactions").EnumerateArray().Where(r=>!r.GetProperty("id").GetString()!.StartsWith("thumb",StringComparison.Ordinal))){root.Children.Clear();Theme(row.GetProperty("scheme").GetString()!);using var spec=JsonDocument.Parse(JsonSerializer.Serialize(new{orientation=row.GetProperty("orientation").GetString(),rtl=row.GetProperty("rtl").GetBoolean()}));var owner=Mount(spec.RootElement,out var content,out _);var editor=new TextBox{Text="Original",Width=140,Height=24};content!.Child=editor;var events=0;owner.ViewChanged+=(_,_)=>events++;await Layout();var samples=new List<object>();void Sample(string label)=>samples.Add(new{label,measurement=Snapshot(owner,content)});
                Sample("initial");owner.ScrollTo(80,120);await Layout();Sample("programmatic-positive");owner.ScrollTo(0,120);await Layout();Sample("programmatic-negative");owner.ScrollTo(1e6,1e6);await Layout();Sample("end-positive");owner.ScrollTo(0,1e6);await Layout();Sample("end-negative");
                // Replay source DOM signed requests against native leading-edge offsets.
                if(owner.FlowDirection==FlowDirection.RightToLeft){samples.Clear();owner.ScrollTo(0,0);await Layout();Sample("initial");owner.ScrollTo(0,120);await Layout();Sample("programmatic-positive");owner.ScrollTo(80,120);await Layout();Sample("programmatic-negative");owner.ScrollTo(0,1e6);await Layout();Sample("end-positive");owner.ScrollTo(1e6,1e6);await Layout();Sample("end-negative");}
                owner.ScrollTo(0,0);await Layout();Wheel(owner,-120);await Layout();Sample("vertical-wheel");owner.ScrollTo(0,0);await Layout();Key(owner,VirtualKey.Home);Key(owner,VirtualKey.Down);await Layout();Sample("arrow-down");Key(owner,VirtualKey.End);await Layout();Sample("keyboard-end");editor.Text="Caller edit";editor.Select(2,3);owner.Orientation=BleeckerScrollAreaOrientation.Both;owner.MaxHeight=180;await Layout();var retained=ReferenceEquals(owner.Content,content)&&ReferenceEquals(content.Child,editor)&&editor.Text=="Caller edit"&&editor.SelectionStart==2&&editor.SelectionLength==3;content.Width=120;content.Height=80;await Layout();Require(owner.HorizontalOffset==0&&owner.VerticalOffset==0,"Shrinking original content retained an out-of-range offset.");Require(retained&&events>0,"ScrollArea lost original editing/selection or notifications.");histories.Add(new{id=row.GetProperty("id").GetString(),scheme=row.GetProperty("scheme").GetString(),samples,originalEditorRetained=retained,scrollEventsObserved=events,shrunk=Snapshot(owner,content)});
            }
        });
        await Case("source-scrollbar-visual-states",async()=>
        {
            foreach(var row in reference.RootElement.GetProperty("paints").EnumerateArray()){root.Children.Clear();Theme(row.GetProperty("scheme").GetString()!);using var spec=JsonDocument.Parse(JsonSerializer.Serialize(new{orientation=row.GetProperty("orientation").GetString()}));var owner=Mount(spec.RootElement,out _,out _);await Layout();var thumbs=Elements(owner).OfType<Microsoft.UI.Xaml.Controls.Primitives.Thumb>().Where(t=>t.Visibility==Visibility.Visible&&t.ActualWidth>0&&t.ActualHeight>0).ToArray();var hover=row.GetProperty("hover").GetBoolean();var hoveredName=row.GetProperty("orientation").GetString()=="horizontal"?"HorizontalThumb":"VerticalThumb";foreach(var thumb in thumbs)Require(VisualStateManager.GoToState(thumb,hover&&thumb.Name==hoveredName?"PointerOver":"Normal",false),"Native thumb lacks its production hover state.");await Layout();var name=row.GetProperty("scheme").GetString()+"-"+row.GetProperty("orientation").GetString()+"-"+(hover?"hover":"rest");paints.Add(new{name,scheme=row.GetProperty("scheme").GetString(),orientation=row.GetProperty("orientation").GetString(),hover,thumbs=thumbs.Select(t=>new{name=t.Name,bounds=Box(t),radius=t.CornerRadius.TopLeft,fill=Brush(Elements(t).OfType<Border>().Single(e=>e.Name=="ThumbFill").Background)}).ToArray()});await Capture(name);}
        });
        await Case("original-template-selector-bindings-and-lifetime",async()=>
        {
            Theme("Light");var model=new Model();var template=(DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border Tag='{Binding}' Width='{Binding Width}' Height='{Binding Height}'><TextBox Text='{Binding Text,Mode=TwoWay,UpdateSourceTrigger=PropertyChanged}' FontSize='13'/></Border></DataTemplate>");var selector=new Selector(template);var owner=new BleeckerScrollArea{Width=256,MaxHeight=240,Content=model,ContentTemplateSelector=selector};root.Children.Add(owner);await Layout();var original=Elements(owner).OfType<Border>().Single(e=>ReferenceEquals(e.Tag,model));var editor=Elements(original).OfType<TextBox>().Single();Require(selector.Items.Count>0&&selector.Items.All(v=>ReferenceEquals(v,model)),"Selector lost its original model.");editor.Text="Caller edit";editor.Select(2,3);owner.ScrollTo(80,120);await Layout();owner.Orientation=BleeckerScrollAreaOrientation.Both;owner.MaxHeight=180;owner.FontSize=20;await Layout();Require(Elements(owner).Contains(original)&&editor.SelectionStart==2&&editor.SelectionLength==3&&editor.FontSize==13&&editor.GetBindingExpression(TextBox.TextProperty)is not null,"Layout/paint replaced original template controls or bindings.");model.Width=120;model.Height=80;model.Text="Model edit";await Layout();Require(editor.Text=="Model edit"&&owner.HorizontalOffset==0&&owner.VerticalOffset==0,"Live model layout/value did not reach original controls.");
            var direct=new TextBox{Width=520,Height=480,Text="Direct caller",FontSize=13};owner.Content=null;owner.ContentTemplateSelector=null;owner.Content=direct;await Layout();direct.Select(2,3);var oldView=Elements(owner).OfType<ScrollView>().Single();owner.Template=(ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ContentControl'><Grid x:Name='PART_Layout' UseLayoutRounding='False'/></ControlTemplate>");await Layout();Require(!Elements(owner).Contains(oldView)&&ReferenceEquals(owner.Content,direct)&&direct.SelectionStart==2&&direct.SelectionLength==3,"Template replacement retained its old host or lost original editor selection.");owner.Template=null;await Layout();Require(!Elements(owner).OfType<ScrollView>().Any(),"Null template retained a scrolling host.");owner.ClearValue(Control.TemplateProperty);await Layout();root.Children.Clear();await Layout();root.Children.Add(owner);await Layout();Require(ReferenceEquals(owner.Content,direct)&&direct.SelectionStart==2&&direct.SelectionLength==3,"Unload/remount replaced original editor state.");
        });
        await Case("caller-metadata-range-and-validation",async()=>
        {
            Theme("Light");var owner=new BleeckerScrollArea{Width=256,MaxHeight=240,Content=new Border{Width=520,Height=480},IsTabStop=true,Tag="Caller metadata"};AutomationProperties.SetName(owner,"Caller scroll area");root.Children.Add(owner);await Layout();var peer=FrameworkElementAutomationPeer.CreatePeerForElement(owner);var provider=peer.GetPattern(PatternInterface.Scroll) as IScrollProvider;
            Require(peer.GetName()=="Caller scroll area"&&peer.GetAutomationControlType()==AutomationControlType.Pane&&provider is not null&&owner.IsTabStop&&Equals(owner.Tag,"Caller metadata"),"Caller metadata/scroll pattern was not forwarded.");
            Require(!provider!.HorizontallyScrollable&&provider.VerticallyScrollable&&provider.HorizontalScrollPercent==-1&&provider.VerticalViewSize==50,"Scroll pattern lost its user-axis/range metadata.");
            provider.SetScrollPercent(-1,50);await Layout();Require(owner.VerticalOffset==120,"Scroll pattern percentage did not reach native scrolling.");provider.Scroll(ScrollAmount.NoAmount,ScrollAmount.SmallIncrement);await Layout();Require(owner.VerticalOffset==160,"Scroll pattern increment did not reach native scrolling.");
            owner.Orientation=BleeckerScrollAreaOrientation.Both;await Layout();provider.SetScrollPercent(50,100);await Layout();Require(owner.HorizontalOffset==135&&owner.VerticalOffset==255&&provider.HorizontalScrollPercent==50&&provider.VerticalScrollPercent==100,"Two-axis scroll pattern did not use the actual native range.");owner.Orientation=BleeckerScrollAreaOrientation.Vertical;await Layout();owner.ScrollTo(1e6,1e6);await Layout();Require(owner.HorizontalOffset==270&&owner.VerticalOffset==240,"Programmatic hidden-axis range did not clamp.");
            var rejected=0;try{provider.SetScrollPercent(-1,101);}catch(ArgumentOutOfRangeException){rejected++;}try{owner.ScrollTo(double.NaN,0);}catch(ArgumentOutOfRangeException){rejected++;}try{owner.Orientation=(BleeckerScrollAreaOrientation)42;}catch(ArgumentOutOfRangeException){rejected++;}Require(rejected==3,"Invalid public scroll/orientation/percentage values were accepted.");
            var beforeX=owner.HorizontalOffset;var beforeY=owner.VerticalOffset;owner.IsEnabled=false;await Layout();var disabledX=owner.HorizontalOffset;var disabledY=owner.VerticalOffset;
            Require(disabledX==Math.Min(beforeX,owner.ExtentWidth-owner.ViewportWidth)&&disabledY==Math.Min(beforeY,owner.ExtentHeight-owner.ViewportHeight),"Disabling changed offsets beyond clamping to its actual native viewport.");
            Wheel(owner,120);Key(owner,VirtualKey.Home);await Layout();Require(owner.HorizontalOffset==disabledX&&owner.VerticalOffset==disabledY,"Disabled owner accepted user scrolling.");var disabledRejected=false;try{provider.SetScrollPercent(-1,0);}catch(ElementNotEnabledException){disabledRejected=true;}Require(disabledRejected,"Disabled scroll pattern accepted a user request.");
        });
        root.Children.Clear();await Layout();Directory.CreateDirectory(Path.GetDirectoryName(output)!);string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
        await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{passed=failures.Count==0,scope="hidden-scroll-area-source-renderer-and-contracts",diagnosticOnly=true,stateInjection="private-production-scroll-methods-and-native-visual-states",frames,histories,paints,captures,checks,failures,guards,activations,focus,referenceSha256=Convert.ToHexStringLower(SHA256.HashData(referenceBytes)),librarySha256=Hash(typeof(BleeckerScrollArea).Assembly.Location),hostSha256=Hash(hostPath),processArchitecture=RuntimeInformation.ProcessArchitecture.ToString(),osArchitecture=RuntimeInformation.OSArchitecture.ToString(),activateCalled=false,showCalled=false,focusCalled=false,inputSent=false,sourceRasterParityVerified=false,nativeInputFocusVerified=false,visibleRuntimeVerified=false,nativeComponentAccepted=false},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(failures.Count==0?0:1);
    }
}
