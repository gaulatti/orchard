using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class KbdContentFixture
{
    private delegate bool Visitor(nint handle,nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor,nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement element)yield return element;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static object Box(Rect r)=>new{x=r.X,y=r.Y,width=r.Width,height=r.Height};
    private sealed record Model(string Name);
    private sealed class Selector(DataTemplate template):DataTemplateSelector
    {
        internal int Calls;
        protected override DataTemplate SelectTemplateCore(object item){Calls++;return template;}
        protected override DataTemplate SelectTemplateCore(object item,DependencyObject container){Calls++;return template;}
    }
    internal static async Task RunAsync(Window window,string output,string hostAssembly,Func<UserControl>? catalogFactory=null)
    {
        var states=new List<object>();var contracts=new List<object>();var scaleFrames=new List<object>();var metadataContracts=new List<object>();var catalogs=new List<object>();var stage="initial";var guards=0;var focus=0;var activations=0;
        try
        {
            void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
            var root=new Canvas{Width=180,Height=160,Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,68,85,102))};window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(180,160));
            root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
            void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Keycap content fixture left its hidden boundary.");}
            async Task Layout(){for(var i=0;i<3;i++){root.Measure(new Size(180,160));root.Arrange(new Rect(0,0,180,160));root.UpdateLayout();Guard();await Task.Delay(16);}await NativeRenderReadiness.RenderAsync(root);Guard();}
            async Task Capture(string name)
            {
                Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);var scale=root.XamlRoot.RasterizationScale;Require(bitmap.PixelWidth==(int)Math.Round(180*scale)&&bitmap.PixelHeight==(int)Math.Round(160*scale),"Keycap content capture dimensions changed.");var bytes=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());
                using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,bytes);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream);await reader.LoadAsync((uint)stream.Size);var png=new byte[(int)stream.Size];reader.ReadBytes(png);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,name),png);
            }
            foreach(var scheme in new[]{ElementTheme.Light,ElementTheme.Dark})foreach(var flow in new[]{FlowDirection.LeftToRight,FlowDirection.RightToLeft})
            {
                root.RequestedTheme=scheme;var horizontal=new Border{Tag=HorizontalAlignment.Center};var vertical=new Border{Tag=VerticalAlignment.Center};
                var original=new Border{Width=12,Height=8,Background=new SolidColorBrush(Microsoft.UI.Colors.Coral),Tag="original-state"};var producer=new Border{Tag=original};var templateProducer=new Border();
                var control=new BleeckerKbd{Width=120,Height=52,Padding=new Thickness(8,4,10,6),FlowDirection=flow};
                foreach(var(property,source)in new[]{(ContentControl.ContentProperty,producer),(Control.HorizontalContentAlignmentProperty,horizontal),(Control.VerticalContentAlignmentProperty,vertical),(ContentControl.ContentTemplateProperty,templateProducer)})control.SetBinding(property,new Binding{Source=source,Path=new PropertyPath("Tag")});
                var referenceChild=new Border{Width=12,Height=8,Background=new SolidColorBrush(Microsoft.UI.Colors.Coral)};var reference=new Border{Width=120,Height=52,Padding=control.Padding,BorderThickness=control.BorderThickness,Child=referenceChild,FlowDirection=flow};
                root.Children.Add(control);Canvas.SetLeft(control,24);Canvas.SetTop(control,12);root.Children.Add(reference);Canvas.SetLeft(reference,24);Canvas.SetTop(reference,88);await Layout();reference.BorderThickness=control.BorderThickness;
                foreach(var h in new[]{HorizontalAlignment.Center,HorizontalAlignment.Left,HorizontalAlignment.Right,HorizontalAlignment.Stretch})foreach(var v in new[]{VerticalAlignment.Center,VerticalAlignment.Top,VerticalAlignment.Bottom,VerticalAlignment.Stretch})
                {
                    stage=$"{scheme}/{flow}/{h}/{v}";horizontal.Tag=h;vertical.Tag=v;referenceChild.HorizontalAlignment=h;referenceChild.VerticalAlignment=v;await Layout();
                    var actual=original.TransformToVisual(control).TransformBounds(new Rect(0,0,original.ActualWidth,original.ActualHeight));var expected=referenceChild.TransformToVisual(reference).TransformBounds(new Rect(0,0,referenceChild.ActualWidth,referenceChild.ActualHeight));var file=$"kbd-content-{scheme}-{flow}-{h}-{v}.png";await Capture(file);
                    var matches=Math.Abs(actual.X-expected.X)<.001&&Math.Abs(actual.Y-expected.Y)<.001&&Math.Abs(actual.Width-expected.Width)<.001&&Math.Abs(actual.Height-expected.Height)<.001;
                    states.Add(new{scheme=scheme.ToString(),flow=flow.ToString(),horizontal=h.ToString(),vertical=v.ToString(),actual=Box(actual),expected=Box(expected),passed=matches,capture=file,captureSha256=Hash(Path.Combine(Path.GetDirectoryName(output)!,file))});
                    Require(matches,"Caller keycap content alignment does not match native Border layout.");Require(ReferenceEquals(control.Content,original)&&original.Tag as string=="original-state"&&control.GetBindingExpression(Control.HorizontalContentAlignmentProperty)is not null&&control.GetBindingExpression(Control.VerticalContentAlignmentProperty)is not null,"Keycap replaced caller content, state or alignment bindings.");
                }
                stage=$"{scheme}/{flow}/combination";horizontal.Tag=HorizontalAlignment.Left;vertical.Tag=VerticalAlignment.Top;control.Keys=new[]{"Ctrl","S"};await Layout();var children=Elements(control).OfType<BleeckerKbd>().Where(c=>!ReferenceEquals(c,control)).ToArray();
                Require(children.Length==2&&children.All(c=>c.HorizontalContentAlignment==HorizontalAlignment.Left&&c.VerticalContentAlignment==VerticalAlignment.Top),"Combination keycaps do not forward caller content alignment.");horizontal.Tag=HorizontalAlignment.Right;vertical.Tag=VerticalAlignment.Bottom;await Layout();
                Require(children.All(c=>c.HorizontalContentAlignment==HorizontalAlignment.Right&&c.VerticalContentAlignment==VerticalAlignment.Bottom&&c.GetBindingExpression(Control.HorizontalContentAlignmentProperty)is not null&&c.GetBindingExpression(Control.VerticalContentAlignmentProperty)is not null)&&children.All(c=>Elements(control).Contains(c)),"Live alignment changes replaced combination keys or lost their bindings.");
                contracts.Add(new{scheme=scheme.ToString(),flow=flow.ToString(),id="combination-bindings",passed=true,originalChildrenRetained=true,children=2});control.Keys=null;
                stage=$"{scheme}/{flow}/templates";var template=(DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border Width='12' Height='8' Background='Coral' Tag='{Binding}'/></DataTemplate>");var model=new Model("Fictional shortcut");producer.Tag=model;templateProducer.Tag=template;await Layout();Require(Elements(control).OfType<Border>().Any(b=>ReferenceEquals(b.Tag,model)&&ReferenceEquals(b.DataContext,model)),"Keycap template replaced the original model.");contracts.Add(new{scheme=scheme.ToString(),flow=flow.ToString(),id="content-template",passed=true,originalModelRetained=true});
                templateProducer.Tag=null;var selector=new Selector(template);control.ContentTemplateSelector=selector;var selectedModel=new Model("Fictional selected shortcut");producer.Tag=selectedModel;await Layout();Require(selector.Calls>0&&Elements(control).OfType<Border>().Any(b=>ReferenceEquals(b.Tag,selectedModel)&&ReferenceEquals(b.DataContext,selectedModel))&&control.GetBindingExpression(ContentControl.ContentTemplateProperty)is not null,"Keycap selector lost original model or template binding.");contracts.Add(new{scheme=scheme.ToString(),flow=flow.ToString(),id="content-selector",passed=true,originalModelRetained=true,templateBindingRetained=true});
                control.ContentTemplateSelector=null;producer.Tag=original;var savedTemplate=control.Template;control.Template=null;await Layout();control.Template=savedTemplate;await Layout();root.Children.Remove(control);await Layout();Require(!control.IsLoaded,"Keycap content did not unload.");horizontal.Tag=HorizontalAlignment.Left;vertical.Tag=VerticalAlignment.Top;root.Children.Add(control);await Layout();Require(ReferenceEquals(control.Content,original)&&original.Tag as string=="original-state"&&original.IsLoaded&&control.HorizontalContentAlignment==HorizontalAlignment.Left&&control.VerticalContentAlignment==VerticalAlignment.Top&&control.GetBindingExpression(ContentControl.ContentProperty)is not null,"Keycap remount lost caller content or bindings.");contracts.Add(new{scheme=scheme.ToString(),flow=flow.ToString(),id="template-and-remount",passed=true,originalContentRetained=true,bindingsRetained=true});root.Children.Clear();await Layout();
            }
            window.Content=null;using var island=new DesktopWindowXamlSource();island.Initialize(window.AppWindow.Id);island.Content=root;
            foreach(var scheme in new[]{ElementTheme.Light,ElementTheme.Dark})foreach(var scale in new[]{1f,1.25f,1.5f,2f})
            {
                stage=$"{scheme}/scale/{scale}";root.RequestedTheme=scheme;root.Children.Clear();island.SiteBridge.OverrideScale=scale;var width=(int)Math.Round(180*scale);var height=(int)Math.Round(160*scale);window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(width,height));island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,width,height));
                var original=new Border{Width=12,Height=8,Background=new SolidColorBrush(Microsoft.UI.Colors.Coral)};var producer=new Border{Tag=original};var control=new BleeckerKbd{Width=120,Height=52,Padding=new Thickness(8,4,10,6)};control.SetBinding(ContentControl.ContentProperty,new Binding{Source=producer,Path=new PropertyPath("Tag")});root.Children.Add(control);Canvas.SetLeft(control,24);Canvas.SetTop(control,12);
                var expectedChild=new Border{Width=12,Height=8,Background=new SolidColorBrush(Microsoft.UI.Colors.Coral),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};var reference=new Border{Width=120,Height=52,Padding=control.Padding,BorderThickness=control.BorderThickness,Child=expectedChild};root.Children.Add(reference);Canvas.SetLeft(reference,24);Canvas.SetTop(reference,88);
                var single=new BleeckerKbd{Content="Esc"};root.Children.Add(single);Canvas.SetLeft(single,24);Canvas.SetTop(single,132);var combination=new BleeckerKbd{Keys=new[]{"Ctrl","K"}};root.Children.Add(combination);Canvas.SetLeft(combination,84);Canvas.SetTop(combination,132);await Layout();reference.BorderThickness=control.BorderThickness;await Layout();
                var actual=original.TransformToVisual(control).TransformBounds(new Rect(0,0,original.ActualWidth,original.ActualHeight));var expected=expectedChild.TransformToVisual(reference).TransformBounds(new Rect(0,0,expectedChild.ActualWidth,expectedChild.ActualHeight));var singlePeer=FrameworkElementAutomationPeer.CreatePeerForElement(single);var combinationPeer=FrameworkElementAutomationPeer.CreatePeerForElement(combination);var file=$"kbd-scale-{scheme}-{scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}.png";await Capture(file);
                var matches=Math.Abs(actual.X-expected.X)<.001&&Math.Abs(actual.Y-expected.Y)<.001&&Math.Abs(actual.Width-expected.Width)<.001&&Math.Abs(actual.Height-expected.Height)<.001;
                var retained=ReferenceEquals(control.Content,original)&&control.GetBindingExpression(ContentControl.ContentProperty)is not null;
                scaleFrames.Add(new{scheme=scheme.ToString(),requestedScale=scale,actualRasterizationScale=root.XamlRoot.RasterizationScale,width,height,actual=Box(actual),expected=Box(expected),matches,retained,singleWidth=single.ActualWidth,singleHeight=single.ActualHeight,combinationWidth=combination.ActualWidth,combinationHeight=combination.ActualHeight,singleName=singlePeer.GetName(),combinationName=combinationPeer.GetName(),capture=file,captureSha256=Hash(Path.Combine(Path.GetDirectoryName(output)!,file))});
                Require(matches&&retained&&Math.Abs(root.XamlRoot.RasterizationScale-scale)<.0001&&single.ActualHeight==20&&combination.ActualHeight==20&&!single.IsTabStop&&!combination.IsTabStop&&singlePeer.GetName()=="Esc"&&combinationPeer.GetName()=="Ctrl+K","Scaled keycap content, metadata or caller ownership differs.");
            }
            island.SiteBridge.OverrideScale=1;island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,180,160));window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(180,160));root.Children.Clear();
            foreach(var scheme in new[]{ElementTheme.Light,ElementTheme.Dark})foreach(var mode in new[]{"single","combination"})
            {
                root.RequestedTheme=scheme;var producer=new Border{Tag=mode=="single"?(object)"Esc":new[]{"Ctrl","K"}};var control=new BleeckerKbd();var contentProperty=mode=="single"?ContentControl.ContentProperty:BleeckerKbd.KeysProperty;control.SetBinding(contentProperty,new Binding{Source=producer,Path=new PropertyPath("Tag")});root.Children.Add(control);await Layout();var peer=FrameworkElementAutomationPeer.CreatePeerForElement(control);
                void Check(string id,string expected){stage=$"{scheme}/{mode}/{id}";peer.InvalidatePeer();var actual=peer.GetName();var retained=control.GetBindingExpression(contentProperty)is not null&&ReferenceEquals(mode=="single"?control.Content:control.Keys,producer.Tag);metadataContracts.Add(new{scheme=scheme.ToString(),mode,id,expected,actual,retained,passed=actual==expected&&retained});Require(actual==expected&&retained,$"Caller keycap metadata differs at {stage}: '{actual}'");}
                Check("default",mode=="single"?"Esc":"Ctrl+K");AutomationProperties.SetName(control,"");Check("local-empty","");producer.Tag=mode=="single"?(object)"Updated":new[]{"Alt","K"};await Layout();Check("empty-after-content-update","");
                var nameProducer=new Border{Tag=""};control.SetBinding(AutomationProperties.NameProperty,new Binding{Source=nameProducer,Path=new PropertyPath("Tag")});await Layout();Check("bound-empty","");nameProducer.Tag="Caller shortcut";await Layout();Check("bound-name","Caller shortcut");nameProducer.Tag="";await Layout();Check("bound-empty-again","");control.ClearValue(AutomationProperties.NameProperty);var fallback=mode=="single"?"Updated":"Alt+K";Check("clear-restores-default",fallback);
                var standard=control.Style;var baseStyle=new Style{TargetType=typeof(BleeckerKbd),BasedOn=standard};baseStyle.Setters.Add(new Setter(AutomationProperties.NameProperty,""));control.Style=new Style{TargetType=typeof(BleeckerKbd),BasedOn=baseStyle};await Layout();Check("basedon-empty","");AutomationProperties.SetName(control,"Local shortcut");Check("local-over-style","Local shortcut");control.ClearValue(AutomationProperties.NameProperty);Check("clear-reveals-style","");
                root.Children.Remove(control);await Layout();control.ClearValue(FrameworkElement.StyleProperty);var implicitHost=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:local='using:Orchard'><Grid.Resources><Style x:Key='MetadataBase' TargetType='local:BleeckerKbd'><Setter Property='AutomationProperties.Name' Value=''/></Style><Style TargetType='local:BleeckerKbd' BasedOn='{StaticResource MetadataBase}'><Setter Property='Tag' Value='implicit-style-applied'/></Style></Grid.Resources></Grid>");implicitHost.Children.Add(control);root.Children.Add(implicitHost);await Layout();Require(control.Tag as string=="implicit-style-applied","Keycap implicit style was not applied.");Check("ancestor-implicit-empty","");implicitHost.Children.Remove(control);root.Children.Remove(implicitHost);control.ClearValue(FrameworkElement.StyleProperty);root.Children.Add(control);await Layout();Check("remove-style-restores-default",fallback);
                var label=new TextBlock();root.Children.Add(label);AutomationProperties.SetLabeledBy(control,label);await Layout();Check("empty-label","");label.Text="Caller shortcut label";await Layout();FrameworkElementAutomationPeer.CreatePeerForElement(label).InvalidatePeer();Check("caller-label","Caller shortcut label");control.ClearValue(AutomationProperties.LabeledByProperty);Check("remove-label-restores-default",fallback);
                control.SetBinding(AutomationProperties.NameProperty,new Binding{Source=nameProducer,Path=new PropertyPath("Tag")});var template=control.Template;control.Template=null;await Layout();control.Template=template;await Layout();root.Children.Remove(control);await Layout();root.Children.Add(control);await Layout();Check("template-remount-empty","");root.Children.Clear();await Layout();
            }
            if(catalogFactory is not null)foreach(var scheme in new[]{ElementTheme.Light,ElementTheme.Dark}){root.RequestedTheme=scheme;var fixture=catalogFactory();root.Children.Add(fixture);await Layout();var caller=Elements(fixture).OfType<BleeckerKbd>().Single(c=>c.Name=="CallerOwnedName");Require(caller.IsLoaded&&FrameworkElementAutomationPeer.CreatePeerForElement(caller).GetName()=="","Registered keycap fixture lost its caller-empty name.");catalogs.Add(new{scheme=scheme.ToString(),callerEmptyName=true,passed=true});root.Children.Clear();await Layout();}
            Guard();GC.KeepAlive(island);await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-kbd-content-v1",passed=true,states,contracts,scaleFrames,metadataContracts,catalogs,guards,focus,activations,publicApisOnly=true,osSettingsChanged=false,physicalInputVerified=false,nativeARM64RuntimeVerified=false,displayScalingVerified=false,isolatedRasterizationScaleVerified=true,sourceRasterParityVerified=false,nativeComponentAccepted=false,librarySha256=Hash(typeof(BleeckerKbd).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){Console.Error.WriteLine(error);try{await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-kbd-content-v1",passed=false,stage,error=error.ToString(),states,contracts,scaleFrames,metadataContracts,guards,focus,activations,librarySha256=Hash(typeof(BleeckerKbd).Assembly.Location),hostSha256=Hash(hostAssembly)}));}finally{Environment.Exit(1);}}
    }
}
