using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

// Public APIs only: this fixture also runs against the packed independent consumer.
internal static class ModalReferenceFixture
{
    private delegate bool Visitor(nint window,nint parameter);
    [DllImport("user32.dll")]private static extern bool EnumWindows(Visitor visitor,nint parameter);
    [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint window,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(nint window);
    private sealed class Command: ICommand
    {
        private EventHandler? changed;internal bool Enabled=true;internal int Count,Subscriptions;internal object? Last;
        public event EventHandler? CanExecuteChanged{add{changed+=value;Subscriptions++;}remove{changed-=value;Subscriptions--;}}
        public bool CanExecute(object? value)=>Enabled;public void Execute(object? value){Count++;Last=value;}internal void Raise()=>changed?.Invoke(this,EventArgs.Empty);
    }
    private sealed class Selector(object original,DataTemplate template):DataTemplateSelector
    {
        internal int Calls;
        protected override DataTemplate SelectTemplateCore(object item){if(!ReferenceEquals(item,original))throw new InvalidOperationException("Selector lost original caller model.");Calls++;return template;}
        protected override DataTemplate SelectTemplateCore(object item,DependencyObject container)=>SelectTemplateCore(item);
    }
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    {
        if(node is FrameworkElement element)yield return element;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;
    }
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    internal static async Task RunAsync(Window window,string reference,string output,string hostPath)
    {
        var folder=Path.GetDirectoryName(output)!;Directory.CreateDirectory(folder);using var source=JsonDocument.Parse(File.ReadAllText(reference));var background=new Grid();var host=new BleeckerModalHost{Content=background,Width=900,Height=760};window.Content=host;
        var width=900d;var height=760d;var guards=0;var focus=0;var activations=0;var invokes=0;var frames=new List<object>();var cases=new List<object>();var pngs=new List<object>();var stage="initial";
        host.GotFocus+=(_,_)=>focus++;window.Activated+=(_,args)=>{if(args.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((handle,_)=>{GetWindowThreadProcessId(handle,out var owner);visible|=owner==Environment.ProcessId&&IsWindowVisible(handle);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Modal fixture acquired visibility, activation or focus.");Require(host.XamlRoot is null||VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot).Count==0,"Modal opened a native Popup.");}
        async Task Layout(){var idle=new TaskCompletionSource();Require(host.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Could not drain Modal layout.");await idle.Task;for(var i=0;i<4;i++){host.Measure(new Size(width,height));host.Arrange(new Rect(0,0,width,height));host.UpdateLayout();Guard();await Task.Delay(12);}}
        async Task Wait(int milliseconds){await Task.Delay(milliseconds);Guard();}
        FrameworkElement Find(string name)=>Elements(host).Single(e=>e.Name==name);
        FrameworkElement? ActiveSurface()=>Elements(host).Where(e=>e.Name is "ModalSurface" or "AlertDialogSurface").SingleOrDefault(e=>FrameworkElementAutomationPeer.CreatePeerForElement(e).GetPattern(PatternInterface.Window)is IWindowProvider{IsTopmost:true});
        object Box(FrameworkElement element){var p=element.TransformToVisual(host).TransformPoint(new Point());return new{x=p.X,y=p.Y,width=element.ActualWidth,height=element.ActualHeight};}
        async Task InvokeClose(){var button=Find("ModalClose");((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke)).Invoke();invokes++;await Wait(20);}
        void Stage(string value){stage=value;File.WriteAllText(Path.Combine(folder,"stage.json"),JsonSerializer.Serialize(new{stage,guards,frames=frames.Count,cases=cases.Count}));}
        try
        {
            foreach(var row in source.RootElement.GetProperty("cases").EnumerateArray())
            {
                var scheme=row.GetProperty("scheme").GetString()!;var kind=row.GetProperty("spec").GetProperty("kind").GetString()!;var v=row.GetProperty("viewport");width=v.GetProperty("width").GetDouble();height=v.GetProperty("height").GetDouble();Stage(scheme+"-"+width+"-"+kind);host.Width=width;host.Height=height;host.RequestedTheme=Enum.Parse<ElementTheme>(scheme);window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)width,(int)height));
                var original=kind=="empty"?null:new Grid{Height=kind=="long"?1000:80};original?.Children.Add(new TextBox{Text="Fictional original value",VerticalAlignment=VerticalAlignment.Top});var dialog=new BleeckerModal{Title=kind=="title-wrap"?"Fictional component heading with deliberately long wrapping text across several lines":"Fictional component demonstration",Content=original};
                if(kind=="custom"){var style=new Style(typeof(BleeckerOverlaySurface)){BasedOn=(Style)Application.Current.Resources["BleeckerModalSurfaceStyle"]};style.Setters.Add(new Setter(FrameworkElement.MaxWidthProperty,672d));style.Setters.Add(new Setter(Control.CornerRadiusProperty,new CornerRadius(0)));dialog.SurfaceStyle=style;}
                background.Children.Add(dialog);await Layout();dialog.IsOpen=true;await Wait(280);await Layout();var view=(ScrollView)Find("ModalViewport");var surface=Find("ModalSurface");var peer=FrameworkElementAutomationPeer.CreatePeerForElement(surface);var provider=(IWindowProvider)peer.GetPattern(PatternInterface.Window);
                Require(ReferenceEquals(dialog.Content,original)&&provider.IsModal&&provider.IsTopmost&&peer.GetName()==dialog.Title,"Modal lost original content or dialog metadata.");
                frames.Add(new{scheme,kind,viewport=new{width,height},surface=Box(surface),header=Box(Find("ModalHeader")),title=Box(Find("ModalTitle")),close=Box(Find("ModalClose")),content=Box(Find("ModalContent")),scroll=new{clientWidth=view.ViewportWidth,clientHeight=view.ViewportHeight,extentWidth=view.ExtentWidth,extentHeight=view.ExtentHeight},originalContentRetained=true,modal=true,label=peer.GetName()});
                if(width==900&&kind is "fixed" or "long" or "custom")
                {
                    var file=$"modal-{scheme}-{kind}.png";var bitmap=new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();await bitmap.RenderAsync(host);Guard();var pixels=await bitmap.GetPixelsAsync();var bytes=new byte[checked((int)pixels.Length)];using(var reader=DataReader.FromBuffer(pixels))reader.ReadBytes(bytes);using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,bytes);await encoder.FlushAsync();var encoded=new byte[checked((int)stream.Size)];stream.Seek(0);using(var reader=new DataReader(stream)) {await reader.LoadAsync((uint)encoded.Length);reader.ReadBytes(encoded);}await File.WriteAllBytesAsync(Path.Combine(folder,file),encoded);pngs.Add(new{file,sha256=Hash(Path.Combine(folder,file)),width=bitmap.PixelWidth,height=bitmap.PixelHeight});
                }
                dialog.IsOpen=false;await Wait(180);background.Children.Remove(dialog);await Layout();Require(ActiveSurface() is null,"Modal layout leaked its layer.");
            }
            width=900;height=760;host.Width=width;host.Height=height;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,760));
            foreach(var scheme in new[]{"Light","Dark"})
            {
                Stage(scheme+"-contracts");host.RequestedTheme=Enum.Parse<ElementTheme>(scheme);var editor=new TextBox{Text="Original fictional edit"};var content=new StackPanel();content.Children.Add(editor);var command=new Command();var parameter=new object();var sourceOpen=new ToggleSwitch{IsOn=true};var dialog=new BleeckerModal{Title="Fictional controlled Modal",Content=content,CloseCommand=command,CloseCommandParameter=parameter};dialog.SetBinding(BleeckerModal.IsOpenProperty,new Binding{Source=sourceOpen,Path=new PropertyPath(nameof(ToggleSwitch.IsOn))});var requests=0;dialog.CloseRequested+=(_,_)=>requests++;background.Children.Add(dialog);await Layout();await Wait(280);await Layout();
                void Case(string name,bool valid,object? evidence=null){Stage(scheme+"-"+name);Guard();cases.Add(new{scheme,name,passed=valid,evidence});Require(valid,name+": "+JsonSerializer.Serialize(evidence));}
                bool Bound()=>sourceOpen.IsOn&&dialog.IsOpen&&dialog.GetBindingExpression(BleeckerModal.IsOpenProperty)is not null;
                await InvokeClose();Case("native-close-is-caller-controlled",requests==1&&command.Count==1&&ReferenceEquals(command.Last,parameter)&&Bound()&&ReferenceEquals(dialog.Content,content));
                editor.Text="Retained after refused close";dialog.Title="Updated fictional title";await Layout();Case("live-title-retains-original-editor",editor.IsLoaded&&editor.Text=="Retained after refused close"&&ReferenceEquals(dialog.Content,content));
                command.Enabled=false;command.Raise();await Layout();dialog.RequestClose();Case("disabled-command-rejects-request",!((Control)Find("ModalClose")).IsEnabled&&requests==1);command.Enabled=true;command.Raise();await Layout();
                ((IWindowProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Find("ModalSurface")).GetPattern(PatternInterface.Window)).Close();Case("window-close-remains-controlled",requests==2&&Bound());
                var child=new BleeckerModal{Title="Fictional child",Content=new TextBlock{Text="Original nested content"}};content.Children.Add(child);await Layout();var parentSurface=ActiveSurface();child.IsOpen=true;await Wait(280);await Layout();dialog.RequestClose();Case("nested-owner-blocks-parent-request",!ReferenceEquals(ActiveSurface(),parentSurface)&&requests==2&&Bound());child.IsOpen=false;await Wait(180);await Layout();Case("nested-close-restores-parent",ReferenceEquals(ActiveSurface(),parentSurface)&&editor.IsLoaded);content.Children.Remove(child);
                var compactHeightBefore=Find("ModalSurface").ActualHeight;content.Height=1000;await Layout();var view=(ScrollView)Find("ModalViewport");var headerBefore=Find("ModalHeader").TransformToVisual(host).TransformPoint(new Point()).Y;view.ScrollTo(0,300,new ScrollingScrollOptions(ScrollingAnimationMode.Disabled,ScrollingSnapPointsMode.Ignore));await Wait(100);await Layout();var headerAfter=Find("ModalHeader").TransformToVisual(host).TransformPoint(new Point()).Y;Case("scroll-moves-header-and-retains-original-editor",view.VerticalOffset==300&&headerAfter==headerBefore-300&&editor.Text=="Retained after refused close"&&editor.IsLoaded&&Bound(),new{offset=view.VerticalOffset,headerBefore,headerAfter,originalEditorRetained=editor.IsLoaded,extent=view.ExtentHeight,viewport=view.ViewportHeight});dialog.RequestClose();var retainedOffset=view.VerticalOffset;content.Height=double.NaN;await Layout();var compactHeightAfter=Find("ModalSurface").ActualHeight;Case("refused-close-retains-scroll-position",retainedOffset==300&&requests==3&&Bound()&&view.VerticalOffset==0&&compactHeightAfter==compactHeightBefore,new{offset=retainedOffset,requests,callerOpenRetained=Bound(),compactHeightBefore,compactHeightAfter,shrunkOffset=view.VerticalOffset});
                var oldSurface=ActiveSurface();dialog.Template=(ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><Grid x:Name='PART_Layout'/></ControlTemplate>");dialog.ApplyTemplate();await Layout();await Wait(280);await Layout();Case("template-remount-retains-original-content",Bound()&&!ReferenceEquals(oldSurface,ActiveSurface())&&editor.IsLoaded&&editor.Text=="Retained after refused close");
                background.Children.Remove(dialog);await Layout();Case("unload-releases-command-and-layer",command.Subscriptions==0&&ActiveSurface() is null&&dialog.IsOpen);background.Children.Add(dialog);await Layout();await Wait(280);await Layout();Case("reload-mounts-retained-caller-state",Bound()&&command.Subscriptions==1&&editor.IsLoaded);
                var model=new object();var template=(DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='Fictional original model template'/></DataTemplate>");var selector=new Selector(model,template);dialog.Content=model;dialog.ContentTemplateSelector=selector;await Layout();Case("selector-receives-original-model",selector.Calls>0&&Elements(host).OfType<TextBlock>().Any(t=>t.Text=="Fictional original model template")&&ReferenceEquals(dialog.Content,model));
                sourceOpen.IsOn=false;await Wait(180);background.Children.Remove(dialog);await Layout();Case("caller-close-cleans-up",ActiveSurface() is null&&command.Subscriptions==0&&!dialog.IsOpen);
            }
            Guard();await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-modal-native-v1",passed=true,frames,cases,pngs,guards,focus,activations,softwareNativeInvokes=invokes,sourceSha256=Hash(reference),librarySha256=Hash(typeof(BleeckerModal).Assembly.Location),hostSha256=Hash(hostPath),publicApisOnly=true,architecture=RuntimeInformation.ProcessArchitecture.ToString(),activateCalled=false,showCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-modal-native-v1",passed=false,stage,error=error.ToString(),frames,cases,guards,focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
}
