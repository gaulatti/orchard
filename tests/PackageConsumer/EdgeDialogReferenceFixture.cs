using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Input;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
namespace Orchard.Testing;

// Also compiled by an independent package consumer. No internal library APIs.
internal static class EdgeDialogReferenceFixture
{
    private sealed class Model:INotifyPropertyChanged
    {
        private double height=20;private string text="Fictional original model";
        public double Height{get=>height;set{height=value;PropertyChanged?.Invoke(this,new(nameof(Height)));}}
        public string Text{get=>text;set{text=value;PropertyChanged?.Invoke(this,new(nameof(Text)));}}
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Selector(object model,DataTemplate template):DataTemplateSelector
    {
        internal int Calls;
        protected override DataTemplate SelectTemplateCore(object item){if(!ReferenceEquals(item,model))throw new InvalidOperationException("Edge selector lost original model.");Calls++;return template;}
        protected override DataTemplate SelectTemplateCore(object item,DependencyObject container)=>SelectTemplateCore(item);
    }
    private delegate bool Visitor(nint window,nint parameter);
    [DllImport("user32.dll")]private static extern bool EnumWindows(Visitor visitor,nint parameter);
    [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint window,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(nint window);
    private sealed class Command:ICommand
    {
        private EventHandler? changed;internal bool Enabled=true;internal int Count,Subscriptions;internal object? Last;
        public event EventHandler? CanExecuteChanged{add{changed+=value;Subscriptions++;}remove{changed-=value;Subscriptions--;}}
        public bool CanExecute(object? p)=>Enabled;public void Execute(object? p){Count++;Last=p;}internal void Raise()=>changed?.Invoke(this,EventArgs.Empty);
    }
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var c in Elements(VisualTreeHelper.GetChild(node,i)))yield return c;}
    private static string Hash(string p)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));
    internal static async Task RunAsync(Window window,string reference,string output,string hostPath)
    {
        var folder=Path.GetDirectoryName(output)!;Directory.CreateDirectory(folder);using var source=JsonDocument.Parse(File.ReadAllText(reference));var background=new Grid();var host=new BleeckerModalHost{Content=background,Width=900,Height=760};window.Content=host;
        var width=900d;var height=760d;var guards=0;var focus=0;var activations=0;var invokes=0;var frames=new List<object>();var cases=new List<object>();var pngs=new List<object>();var stage="initial";
        host.GotFocus+=(_,_)=>focus++;window.Activated+=(_,args)=>{if(args.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((handle,_)=>{GetWindowThreadProcessId(handle,out var owner);visible|=owner==Environment.ProcessId&&IsWindowVisible(handle);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Edge fixture acquired visibility, activation or focus.");Require(host.XamlRoot is null||VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot).Count==0,"Edge fixture opened a native Popup.");}
        async Task Layout(){var idle=new TaskCompletionSource();Require(host.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Could not drain edge layout.");await idle.Task;for(var i=0;i<4;i++){host.Measure(new Size(width,height));host.Arrange(new Rect(0,0,width,height));host.UpdateLayout();Guard();await Task.Delay(12);}}
        async Task Wait(int ms){await Task.Delay(ms);Guard();}
        FrameworkElement Find(string name)=>Elements(host).Single(e=>e.Name==name);
        FrameworkElement? Active()=>Elements(host).Where(e=>e.Name is "DrawerSurface" or "SheetSurface" or "ModalSurface").SingleOrDefault(e=>FrameworkElementAutomationPeer.CreatePeerForElement(e).GetPattern(PatternInterface.Window)is IWindowProvider{IsTopmost:true});
        object Box(FrameworkElement e){var p=e.TransformToVisual(host).TransformPoint(new Point());return new{x=p.X,y=p.Y,width=e.ActualWidth,height=e.ActualHeight};}
        void Stage(string value){stage=value;File.WriteAllText(Path.Combine(folder,"stage.json"),JsonSerializer.Serialize(new{stage,guards,frames=frames.Count,cases=cases.Count}));}
        async Task Capture(string file){var bitmap=new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();await bitmap.RenderAsync(host);Guard();var pixels=await bitmap.GetPixelsAsync();var bytes=new byte[checked((int)pixels.Length)];using(var reader=DataReader.FromBuffer(pixels))reader.ReadBytes(bytes);using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,bytes);await encoder.FlushAsync();var encoded=new byte[checked((int)stream.Size)];stream.Seek(0);using(var reader=new DataReader(stream)){await reader.LoadAsync((uint)encoded.Length);reader.ReadBytes(encoded);}await File.WriteAllBytesAsync(Path.Combine(folder,file),encoded);pngs.Add(new{file,sha256=Hash(Path.Combine(folder,file)),width=bitmap.PixelWidth,height=bitmap.PixelHeight});}
        try
        {
            var index=0;
            foreach(var row in source.RootElement.GetProperty("cases").EnumerateArray())
            {
                var scheme=row.GetProperty("scheme").GetString()!;var spec=row.GetProperty("spec");var shape=spec.GetProperty("shape").GetString()!;var drawer=spec.GetProperty("kind").GetString()=="drawer";var prefix=drawer?"Drawer":"Sheet";var side=drawer?BleeckerSheetSide.Bottom:Enum.Parse<BleeckerSheetSide>(spec.GetProperty("side").GetString()!,true);var v=row.GetProperty("viewport");width=v.GetProperty("width").GetDouble();height=v.GetProperty("height").GetDouble();Stage($"{index}-{scheme}-{prefix}-{side}-{shape}");host.Width=width;host.Height=height;host.RequestedTheme=Enum.Parse<ElementTheme>(scheme);window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)width,(int)height));
                var ch=spec.TryGetProperty("height",out var supplied)?supplied.GetDouble():shape is "long" or "no-scroll" or "fill"?1000:80;FrameworkElement? original=shape=="empty"?null:shape=="fill"?new ScrollView{Content=new Grid{Height=1000},HorizontalScrollMode=ScrollingScrollMode.Disabled}:new Grid{Height=ch};if(original is Grid grid)grid.Children.Add(new TextBox{Text="Fictional original value",VerticalAlignment=VerticalAlignment.Top});
                BleeckerEdgeDialog dialog=drawer?new BleeckerDrawer():new BleeckerSheet{Side=side,ScrollContent=shape is not("no-scroll" or "fill")};dialog.Title=shape is "no-heading" or "description-only"?null:shape=="title-wrap"?"Fictional component heading with deliberately long wrapping words across several lines":"Fictional component panel";dialog.Description=shape=="no-heading"?null:"Fictional local component guidance for the original caller content.";dialog.Content=original;
                if(shape=="custom"){var style=new Style(typeof(BleeckerOverlaySurface)){BasedOn=(Style)Application.Current.Resources[drawer?"BleeckerDrawerSurfaceStyle":"BleeckerSheet"+side+"SurfaceStyle"]};style.Setters.Add(new Setter(drawer?FrameworkElement.MaxHeightProperty:FrameworkElement.MaxWidthProperty,drawer?height*.7:512d));style.Setters.Add(new Setter(Control.CornerRadiusProperty,new CornerRadius(0)));dialog.SurfaceStyle=style;}
                background.Children.Add(dialog);await Layout();dialog.IsOpen=true;await Wait(280);await Layout();var view=(ScrollView)Find(prefix+"Body");var surface=Find(prefix+"Surface");var peer=FrameworkElementAutomationPeer.CreatePeerForElement(surface);var provider=(IWindowProvider)peer.GetPattern(PatternInterface.Window);Require(ReferenceEquals(dialog.Content,original)&&provider.IsModal&&provider.IsTopmost,"Edge dialog lost content or modal metadata.");
                frames.Add(new{index,scheme,dialog=Box(surface),inner=Box(Find(prefix+"Inner")),handle=drawer?Box(Find("DrawerHandle")):null,header=drawer&&shape=="no-heading"?null:Box(Find(prefix+"Header")),title=dialog.Title is null?null:Box(Find(prefix+"Title")),description=dialog.Description is null?null:Box(Find(prefix+"Description")),close=drawer?null:Box(Find("SheetClose")),body=Box(view),content=shape=="empty"?null:Box(Find(prefix+"Content")),scroll=new{clientWidth=view.ViewportWidth,clientHeight=view.ViewportHeight,extentHeight=view.ExtentHeight,offset=view.VerticalOffset},name=peer.GetName(),help=peer.GetHelpText(),originalContentRetained=true,originalHeightRetained=original is not Grid originalGrid||originalGrid.Height==ch});
                if(width==900&&shape=="fixed")await Capture($"edge-{scheme}-{prefix}-{side}.png");
                dialog.IsOpen=false;await Wait(210);background.Children.Remove(dialog);await Layout();Require(Active() is null,"Edge state leaked its layer.");index++;
            }
            width=900;height=760;host.Width=width;host.Height=height;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,760));
            foreach(var scheme in new[]{"Light","Dark"})foreach(var drawer in new[]{true,false})
            {
                var prefix=drawer?"Drawer":"Sheet";host.RequestedTheme=Enum.Parse<ElementTheme>(scheme);Stage(scheme+prefix+"-contracts");var editor=new TextBox{Text="Fictional original value"};var content=new StackPanel();content.Children.Add(editor);var command=new Command();var parameter=new object();var open=new ToggleSwitch{IsOn=true};BleeckerEdgeDialog dialog=drawer?new BleeckerDrawer():new BleeckerSheet();dialog.Content=content;dialog.Title="Fictional component panel";dialog.Description="Original description";dialog.CloseCommand=command;dialog.CloseCommandParameter=parameter;dialog.SetBinding(BleeckerEdgeDialog.IsOpenProperty,new Binding{Source=open,Path=new PropertyPath(nameof(ToggleSwitch.IsOn))});var requests=0;dialog.CloseRequested+=(_,_)=>requests++;background.Children.Add(dialog);await Layout();await Wait(280);await Layout();
                void Case(string name,bool valid,object? evidence=null){Stage(scheme+prefix+"-"+name);cases.Add(new{scheme,kind=prefix,name,passed=valid,evidence});Require(valid,name+": "+JsonSerializer.Serialize(evidence));Guard();}
                bool Bound()=>open.IsOn&&dialog.IsOpen&&dialog.GetBindingExpression(BleeckerEdgeDialog.IsOpenProperty)is not null;
                var surface=Active()!;((IWindowProvider)FrameworkElementAutomationPeer.CreatePeerForElement(surface).GetPattern(PatternInterface.Window)).Close();invokes++;Case("controlled-window-close",requests==1&&command.Count==1&&ReferenceEquals(command.Last,parameter)&&Bound()&&ReferenceEquals(dialog.Content,content));
                editor.Text="Retained fictional edit";dialog.Title="Changed title";dialog.Description="Changed description";await Layout();Case("live-metadata-retains-editor",editor.IsLoaded&&editor.Text=="Retained fictional edit"&&FrameworkElementAutomationPeer.CreatePeerForElement(surface).GetHelpText()==dialog.Description);
                command.Enabled=false;command.Raise();await Layout();dialog.RequestClose();Case("disabled-command",requests==1&&(drawer||!((Control)Find("SheetClose")).IsEnabled));command.Enabled=true;command.Raise();await Layout();
                if(!drawer){((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Find("SheetClose")).GetPattern(PatternInterface.Invoke)).Invoke();invokes++;Case("sheet-close-button-controlled",requests==2&&Bound());}
                var nested=new BleeckerModal{Title="Nested fictional component",Content=new TextBox{Text="Nested original value"}};content.Children.Add(nested);await Layout();nested.IsOpen=true;await Wait(280);await Layout();var before=requests;dialog.RequestClose();Case("nested-modal-suspends-parent",!ReferenceEquals(Active(),surface)&&requests==before&&Bound());nested.IsOpen=false;await Wait(210);await Layout();Case("nested-close-restores-parent",ReferenceEquals(Active(),surface)&&editor.IsLoaded);content.Children.Remove(nested);
                var compact=surface.ActualHeight;var compactExtent=((ScrollView)Find(prefix+"Body")).ExtentHeight;content.Height=1000;await Layout();var body=(ScrollView)Find(prefix+"Body");var headerY=Find(prefix+"Header").TransformToVisual(host).TransformPoint(new Point()).Y;body.ScrollTo(0,200,new ScrollingScrollOptions(ScrollingAnimationMode.Disabled,ScrollingSnapPointsMode.Ignore));await Wait(80);await Layout();Case("body-scroll-keeps-header",body.VerticalOffset==200&&Find(prefix+"Header").TransformToVisual(host).TransformPoint(new Point()).Y==headerY&&editor.Text=="Retained fictional edit",new{offset=body.VerticalOffset,headerY,after=Find(prefix+"Header").TransformToVisual(host).TransformPoint(new Point()).Y});
                content.Height=double.NaN;await Layout();Case("automatic-content-shrinks",surface.ActualHeight==compact&&body.ExtentHeight==compactExtent&&body.VerticalOffset==0&&editor.IsLoaded,new{compact,restored=surface.ActualHeight,compactExtent,restoredExtent=body.ExtentHeight});
                var model=new Model();var template=(DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Grid Height='{Binding Height}'><TextBox Text='{Binding Text,Mode=TwoWay}' VerticalAlignment='Top'/></Grid></DataTemplate>");var selector=new Selector(model,template);dialog.ContentTemplateSelector=selector;dialog.Content=model;await Layout();var templateEditor=Elements(host).OfType<TextBox>().Single(e=>e.Text==model.Text);Case("selector-receives-original-model",selector.Calls>0&&ReferenceEquals(dialog.Content,model)&&ReferenceEquals(dialog.ContentTemplateSelector,selector));
                var templateCompact=surface.ActualHeight;var templateExtent=body.ExtentHeight;templateEditor.Text="Retained templated edit";model.Height=1000;await Layout();Require(body.ExtentHeight>body.ViewportHeight,"Bound model did not create real overflow.");model.Height=20;await Layout();Case("bound-template-resizes-and-retains-editor",surface.ActualHeight==templateCompact&&body.ExtentHeight==templateExtent&&templateEditor.IsLoaded&&templateEditor.Text=="Retained templated edit"&&model.Text==templateEditor.Text,new{compact=templateCompact,restored=surface.ActualHeight,extent=body.ExtentHeight,templateExtent});
                open.IsOn=false;await Wait(210);background.Children.Remove(dialog);await Layout();Case("caller-close-cleans-up",Active() is null&&command.Subscriptions==0);
            }
            Guard();await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-edge-dialog-native-v1",passed=true,frames,cases,pngs,guards,focus,activations,softwareNativeInvokes=invokes,sourceSha256=Hash(reference),librarySha256=Hash(typeof(BleeckerDrawer).Assembly.Location),hostSha256=Hash(hostPath),publicApisOnly=true,architecture=RuntimeInformation.ProcessArchitecture.ToString(),activateCalled=false,showCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-edge-dialog-native-v1",passed=false,stage,error=error.ToString(),frames,cases,guards,focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
}
