using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement;

namespace Orchard.Testing;

internal static class SkeletonContentFixture
{
    private delegate bool Visitor(nint handle,nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor,nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement element)yield return element;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static object Box(FrameworkElement e,UIElement root){var b=e.TransformToVisual(root).TransformBounds(new Rect(0,0,e.ActualWidth,e.ActualHeight));return new{x=b.X,y=b.Y,width=b.Width,height=b.Height};}
    private static object? BrushValue(Brush? brush)=>brush is null?null:brush is SolidColorBrush solid?new{color=$"#{solid.Color.A:X2}{solid.Color.R:X2}{solid.Color.G:X2}{solid.Color.B:X2}",opacity=solid.Opacity}:throw new InvalidOperationException("Expected a source solid brush.");
    private sealed record Model(string Name);
    private sealed class Selector(DataTemplate template):DataTemplateSelector
    {
        internal int Calls;
        protected override DataTemplate SelectTemplateCore(object item){Calls++;return template;}
        protected override DataTemplate SelectTemplateCore(object item,DependencyObject container){Calls++;return template;}
    }
    internal static async Task RunAsync(Window window,string output,string hostAssembly)
    {
        var states=new List<object>();var contracts=new List<object>();var captures=new List<object>();var stage="initial";var guards=0;var focus=0;var activations=0;
        try
        {
            void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
            Require(!new UISettings().AnimationsEnabled,"This fixture qualifies the current reduced-motion state; normal pulse timing remains separate.");
            object probe=new BleeckerSkeleton();Require(probe is ContentControl,"Pinned Skeleton accepts child content, but this native library has no content slot.");
            var referencePath=Path.Combine(Path.GetDirectoryName(output)!,"source-reference.json");using var reference=JsonDocument.Parse(await File.ReadAllTextAsync(referencePath));
            var root=(Canvas)XamlReader.Load("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='420' Height='620' Background='{ThemeResource BleeckerBackgroundBrush}'/>");
            window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(420,620));
            root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
            void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Skeleton fixture left its hidden boundary.");}
            async Task Layout(){for(var i=0;i<3;i++){root.Measure(new Size(420,620));root.Arrange(new Rect(0,0,420,620));root.UpdateLayout();Guard();await Task.Delay(16);}await NativeRenderReadiness.RenderAsync(root);Guard();}
            async Task Capture(string file){Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),bytes);captures.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});Guard();}
            foreach(var scheme in new[]{ElementTheme.Light,ElementTheme.Dark})foreach(var flow in new[]{FlowDirection.LeftToRight,FlowDirection.RightToLeft})
            {
                root.RequestedTheme=scheme;var prefix=scheme+"/"+flow;
                foreach(var spec in reference.RootElement.GetProperty("cases").EnumerateArray())
                {
                    var id=spec.GetProperty("id").GetString()!;var kind=spec.GetProperty("kind").GetString()!;stage=prefix+"/"+id;Control control;
                    if(kind=="shape")control=new BleeckerSkeleton{Width=spec.GetProperty("width").GetDouble(),Height=spec.GetProperty("height").GetDouble(),Circle=spec.GetProperty("circle").GetBoolean()};
                    else if(kind=="card"){var card=new BleeckerSkeletonCard{Width=spec.GetProperty("width").GetDouble()};if(!spec.GetProperty("useDefaults").GetBoolean())card.Rows=spec.GetProperty("rows").GetInt32();control=card;}
                    else if(kind=="table"){var table=new BleeckerSkeletonTable{Width=spec.GetProperty("width").GetDouble()};if(!spec.GetProperty("useDefaults").GetBoolean()){table.Rows=spec.GetProperty("rows").GetInt32();table.Columns=spec.GetProperty("columns").GetInt32();}control=table;}
                    else throw new InvalidOperationException("Unknown pinned Skeleton case.");
                    control.FlowDirection=flow;Canvas.SetLeft(control,24);Canvas.SetTop(control,24);root.Children.Add(control);await Layout();
                    var peer=FrameworkElementAutomationPeer.CreatePeerForElement(control);var shapes=Elements(control).OfType<BleeckerSkeleton>().ToArray();
                    var shapeValues=shapes.Select(shape=>{var surface=Elements(shape).OfType<Border>().Single(b=>b.Name=="PART_Surface");return new{box=Box(shape,root),opacity=shape.Opacity,brush=BrushValue(surface.Background),radii=new[]{surface.CornerRadius.TopLeft,surface.CornerRadius.TopRight,surface.CornerRadius.BottomRight,surface.CornerRadius.BottomLeft}};}).ToArray();
                    var file="skeleton-"+scheme+"-"+flow+"-"+id+".png";await Capture(file);
                    states.Add(new{scheme=scheme.ToString(),flow=flow.ToString(),id,kind,box=Box(control,root),rows=control is BleeckerSkeletonCard c?c.Rows:control is BleeckerSkeletonTable t?t.Rows:(int?)null,columns=control is BleeckerSkeletonTable tableControl?tableControl.Columns:(int?)null,background=BrushValue(control.Background),shapes=shapeValues,decorative=!control.IsTabStop&&!peer.IsControlElement()&&!peer.IsContentElement()&&peer.GetPattern(PatternInterface.Invoke) is null,capture=file});
                    root.Children.Clear();await Layout();Require(!control.IsLoaded,"Retired default Skeleton stayed loaded.");
                }
                stage=prefix+"/caller-content";
                var editor=new TextBox{Text="Original draft",Width=160,MinWidth=0};var button=new Button{Content="Keep draft",Width=160};var patch=new Border{Width=60,Height=12,Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,51,102,153))};
                var panel=new StackPanel{Width=160,Spacing=6};panel.Children.Add(patch);panel.Children.Add(editor);panel.Children.Add(button);var producer=new TextBlock{Tag=panel};var opacityProducer=new TextBlock{Tag=.75};var clicks=0;button.Click+=(_,_)=>clicks++;AutomationProperties.SetName(button,"Keep draft");
                var skeleton=new BleeckerSkeleton{Width=260,Height=140,Padding=new Thickness(8),FlowDirection=flow};var slot=(ContentControl)(object)skeleton;
                slot.SetBinding(ContentControl.ContentProperty,new Binding{Source=producer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});
                skeleton.SetBinding(UIElement.OpacityProperty,new Binding{Source=opacityProducer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});Canvas.SetLeft(skeleton,24);Canvas.SetTop(skeleton,24);root.Children.Add(skeleton);await Layout();editor.Text="Edited caller draft";
                void Invoke(){var peer=FrameworkElementAutomationPeer.CreatePeerForElement(button);Require(peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider,"Caller action lost native Invoke.");((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();}
                Invoke();await Layout();var originalRetained=ReferenceEquals(slot.Content,panel)&&editor.IsLoaded&&button.IsLoaded&&editor.Text=="Edited caller draft"&&clicks==1&&slot.GetBindingExpression(ContentControl.ContentProperty)is not null;
                IEnumerable<AutomationPeer> Peers(AutomationPeer peer){yield return peer;foreach(var child in peer.GetChildren()??[])foreach(var nested in Peers(child))yield return nested;}
                var childInPeerTree=Peers(FrameworkElementAutomationPeer.CreatePeerForElement(skeleton)).Any(p=>p.GetAutomationControlType()==AutomationControlType.Button&&p.GetName()=="Keep draft");
                Require(originalRetained&&childInPeerTree,"Skeleton replaced caller content, bindings, edit state, action or child peer.");contracts.Add(new{scheme=scheme.ToString(),flow=flow.ToString(),id="original-content",passed=true,originalRetained,clicks,childInPeerTree,childControlElement=FrameworkElementAutomationPeer.CreatePeerForElement(button).IsControlElement()});
                var originalTemplate=skeleton.Template;skeleton.Template=null;await Layout();var contentDuringClear=ReferenceEquals(slot.Content,panel);var bindingDuringClear=slot.GetBindingExpression(ContentControl.ContentProperty)is not null;var loadedDuringClear=panel.IsLoaded;
                Require(contentDuringClear&&bindingDuringClear,$"Clearing template changed caller ownership: content={contentDuringClear}, binding={bindingDuringClear}, loaded={loadedDuringClear}.");skeleton.Template=originalTemplate;await Layout();
                Require(panel.IsLoaded&&editor.Text=="Edited caller draft"&&ReferenceEquals(slot.Content,panel),"Template restoration replaced the original editor.");contracts.Add(new{scheme=scheme.ToString(),flow=flow.ToString(),id="template-restoration",passed=true,contentRetained=ReferenceEquals(slot.Content,panel),editRetained=editor.Text=="Edited caller draft",bindingRetained=slot.GetBindingExpression(ContentControl.ContentProperty)is not null,loadedDuringClear});
                var dataTemplate=(DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding Name}'/></DataTemplate>");var templateProducer=new TextBlock{Tag=dataTemplate};slot.SetBinding(ContentControl.ContentTemplateProperty,new Binding{Source=templateProducer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});var model=new Model("Fictional first");producer.Tag=model;await Layout();var rendered=Elements(skeleton).OfType<TextBlock>().Single(t=>t.Text==model.Name);var modelRetained=ReferenceEquals(rendered.DataContext,model)&&!panel.IsLoaded&&slot.GetBindingExpression(ContentControl.ContentTemplateProperty)is not null;
                slot.ClearValue(ContentControl.ContentTemplateProperty);var selector=new Selector(dataTemplate);slot.ContentTemplateSelector=selector;var selectedModel=new Model("Fictional second");producer.Tag=selectedModel;await Layout();var selected=Elements(skeleton).OfType<TextBlock>().Single(t=>t.Text==selectedModel.Name);var selectorModelRetained=ReferenceEquals(selected.DataContext,selectedModel);Require(modelRetained&&selectorModelRetained&&selector.Calls>0,"Caller template/selector lost original model context.");
                slot.ContentTemplateSelector=null;producer.Tag=panel;await Layout();Require(ReferenceEquals(slot.Content,panel)&&editor.Text=="Edited caller draft"&&panel.IsLoaded,"Returning original content after model templates lost state.");contracts.Add(new{scheme=scheme.ToString(),flow=flow.ToString(),id="caller-templates",passed=true,modelRetained,selectorModelRetained,selectorCalls=selector.Calls,originalRestored=ReferenceEquals(slot.Content,panel)&&panel.IsLoaded});
                var fileName="skeleton-"+scheme+"-"+flow+"-caller-content.png";await Capture(fileName);var patchBox=Box(patch,root);root.Children.Clear();await Layout();Require(!panel.IsLoaded,"Unloaded Skeleton retained loaded caller content.");opacityProducer.Tag=.25;await Layout();Require(skeleton.Opacity==.25&&skeleton.GetBindingExpression(UIElement.OpacityProperty)is not null,"Unloaded Skeleton lost caller opacity binding.");root.Children.Add(skeleton);await Layout();Invoke();await Layout();Require(clicks==2&&panel.IsLoaded&&editor.Text=="Edited caller draft"&&ReferenceEquals(slot.Content,panel)&&skeleton.Opacity==.25,"Remount changed caller state or duplicated actions.");
                contracts.Add(new{scheme=scheme.ToString(),flow=flow.ToString(),id="opacity-and-remount",passed=true,contentRetained=ReferenceEquals(slot.Content,panel),editRetained=editor.Text=="Edited caller draft",clicks,opacity=skeleton.Opacity,opacityBindingRetained=skeleton.GetBindingExpression(UIElement.OpacityProperty)is not null,capture=fileName,capturedOpacity=.75,patchBox});root.Children.Clear();await Layout();
            }
            Guard();Require(!new UISettings().AnimationsEnabled,"Animation preference changed during reduced-motion verification.");
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-skeleton-content-native-v1",passed=true,states,contracts,captures,guards,focus,activations,animationsEnabled=false,reducedMotionVerified=true,normalPulseVerified=false,osSettingsChanged=false,physicalInputVerified=false,nativeComponentAccepted=false,nativeARM64RuntimeVerified=false,compositionShadowRasterVerified=false,sourceRasterParityVerified=false,publicApisOnly=true,rootWidth=420,rootHeight=620,referenceSha256=Hash(referencePath),librarySha256=Hash(typeof(BleeckerSkeleton).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){Console.Error.WriteLine(error);try{await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-skeleton-content-native-v1",passed=false,stage,error=error.ToString(),states,contracts,guards,focus,activations,librarySha256=Hash(typeof(BleeckerSkeleton).Assembly.Location),hostSha256=Hash(hostAssembly)}));}finally{Environment.Exit(1);}}
    }
}
