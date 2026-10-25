using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Orchard;
using Windows.Foundation;
using Windows.Graphics;
namespace PackageConsumer;
public partial class App
{
    private async void RunHiddenAppShellsAsync(string referenceDirectory)
    {
        var arguments=Environment.GetCommandLineArgs();var output=arguments[Array.IndexOf(arguments,"--verify-output")+1];
        nint handle=0;var frames=new List<object>();var checks=new List<object>();var failures=new List<string>();var diagnostics=new List<string>();
        var completed=false;var guards=0;var activationEvents=0;string? failure=null,isolationFailure=null,referenceHash=null;
        DebugSettings.IsXamlResourceReferenceTracingEnabled=true;DebugSettings.XamlResourceReferenceFailed+=(_,args)=>diagnostics.Add(args.Message);
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);if(isolationFailure is not null||window!.Visible||window.AppWindow.IsVisible||IsWindowVisible(handle)||foreground==Environment.ProcessId)throw new InvalidOperationException(isolationFailure??"Hidden AppShell became visible or owned foreground input.");}
        static void Assert(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        void Check(string id,Action action){Guard();try{action();checks.Add(new{id,passed=true});}catch(Exception error){failures.Add(id+": "+error.Message);checks.Add(new{id,passed=false,error=error.Message});}Guard();}
        static object? Field(BleeckerAppShell shell,string name)=>typeof(BleeckerAppShell).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(shell);
        static bool Flag(JsonElement spec,string key)=>spec.TryGetProperty(key,out var value)&&value.ValueKind==JsonValueKind.True;
        static Binding Bind(object producer)=>new(){Source=producer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay};
        static IEnumerable<AutomationPeer> PeerChildren(AutomationPeer parent){foreach(var child in parent.GetChildren()??[]){yield return child;foreach(var descendant in PeerChildren(child))yield return descendant;}}
        try
        {
            window=new Window();handle=WinRT.Interop.WindowNative.GetWindowHandle(window);Guard();window.Activated+=(_,args)=>{if(args.WindowActivationState!=WindowActivationState.Deactivated){activationEvents++;isolationFailure="Hidden AppShell window activated.";}};window.VisibilityChanged+=(_,args)=>{if(args.Visible)isolationFailure="Hidden AppShell window became visible.";};
            var root=new Canvas{Width=900,Height=640,UseLayoutRounding=false};window.Content=root;Guard();
            for(var i=0;i<3;i++){root.Measure(new Size(900,640));root.Arrange(new Rect(0,0,900,640));root.UpdateLayout();Guard();await Task.Delay(10);}
            Assert(root.XamlRoot is not null,"Hidden window did not create its native XAML root.");
            async Task Viewport(double width,double height)
            {
                var scale=root.XamlRoot!.RasterizationScale;window.AppWindow.ResizeClient(new SizeInt32((int)Math.Round(width*scale),(int)Math.Round(height*scale)));root.Width=width;root.Height=height;
                for(var i=0;i<3;i++){root.Measure(new Size(width,height));root.Arrange(new Rect(0,0,width,height));root.UpdateLayout();Guard();await Task.Delay(10);}
                Assert(Math.Abs(root.XamlRoot.Size.Width-width)<.001&&Math.Abs(root.XamlRoot.Size.Height-height)<.001,$"Native viewport differs: {root.XamlRoot.Size} from {width}x{height}.");
            }
            async Task Layout(BleeckerAppShell shell)
            {
                for(var i=0;i<3;i++){root.Measure(new Size(root.Width,root.Height));root.Arrange(new Rect(0,0,root.Width,root.Height));root.UpdateLayout();shell.ApplyTemplate();Guard();await Task.Delay(10);}Assert(shell.IsLoaded,"AppShell did not enter the real XAML tree.");Guard();
            }
            FrameworkElement Part(BleeckerAppShell shell,string name)=>HiddenDesc<FrameworkElement>(shell).Single(element=>element.Name==name);
            object Box(FrameworkElement element){var b=element.TransformToVisual(root).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));return new{x=b.X,y=b.Y,width=b.Width,height=b.Height};}
            static object Paint(Brush? brush)=>brush is SolidColorBrush solid?new{color=$"#{solid.Color.A:x2}{solid.Color.R:x2}{solid.Color.G:x2}{solid.Color.B:x2}",opacity=solid.Opacity}:throw new InvalidOperationException("Expected solid AppShell paint.");
            object Snapshot(BleeckerAppShell shell)
            {
                object? Original(string name){var element=HiddenDesc<FrameworkElement>(shell).SingleOrDefault(element=>element.Name==name);return element is null?null:Box(element);}
                var main=(Panel)Part(shell,"AppShellMain");var peer=FrameworkElementAutomationPeer.CreatePeerForElement(main);var insets=(Thickness)typeof(BleeckerAppShell).GetProperty("Insets",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(shell)!;
                return new{shell=Box(shell),main=Box(main),content=Box(Part(shell,"AppShellContent")),header=Original("ReferenceHeader"),footer=Original("ReferenceFooter"),body=Original("ReferenceBody"),paint=new{background=Paint(shell.Background),foreground=Paint(shell.Foreground),contentBackground=Paint(main.Background)},padding=new[]{shell.Padding.Left,shell.Padding.Top,shell.Padding.Right,shell.Padding.Bottom},contentPadding=new[]{insets.Left,insets.Top,insets.Right,insets.Bottom},minimum=shell.MinHeight,text=string.Concat(new[]{"AppShellHeaderText","AppShellBodyText","AppShellFooterText"}.Select(name=>HiddenDesc<TextBlock>(shell).Single(text=>text.Name==name).Text)),landmark=peer.GetLandmarkType().ToString(),tabStop=shell.IsTabStop,loaded=shell.IsLoaded,hostVisible=shell.XamlRoot.IsHostVisible,viewport=new{width=shell.XamlRoot.Size.Width,height=shell.XamlRoot.Size.Height}};
            }
            static object? Slot(JsonElement spec,string name,string height,byte r,byte g,byte b)
            {
                if(spec.TryGetProperty(name,out var value))return value.ValueKind switch{JsonValueKind.String=>value.GetString(),JsonValueKind.Number=>value.GetDouble(),JsonValueKind.True=>true,JsonValueKind.False=>false,_=>null};
                return spec.TryGetProperty(height,out var h)?new Border{Name="Reference"+char.ToUpperInvariant(name[0])+name[1..],Height=h.GetDouble(),Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,r,g,b)),UseLayoutRounding=false}:null;
            }
            var bytes=await File.ReadAllBytesAsync(Path.Combine(referenceDirectory,"AppShell.json"));referenceHash=Convert.ToHexStringLower(SHA256.HashData(bytes));using var reference=JsonDocument.Parse(bytes);Assert(reference.RootElement.GetProperty("sourceSha256").GetString()=="21cbb676b184bcad28ffb5577b19a1ccc34d18ab420221b08cf936a23919c3e1","Unexpected AppShell source.");
            foreach(var row in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                root.Children.Clear();root.UpdateLayout();var spec=row.GetProperty("spec");var viewport=spec.GetProperty("viewport");await Viewport(viewport.GetProperty("width").GetDouble(),viewport.GetProperty("height").GetDouble());root.RequestedTheme=Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!);
                await File.WriteAllTextAsync(output+".progress.json",JsonSerializer.Serialize(new{scheme=row.GetProperty("scheme").GetString(),id=row.GetProperty("id").GetString(),frames=frames.Count,checks=checks.Count}));
                var shell=new BleeckerAppShell{Width=spec.GetProperty("width").GetDouble(),Header=Slot(spec,"header","headerHeight",141,182,156),Content=Slot(spec,"body","bodyHeight",230,199,167),Footer=Slot(spec,"footer","footerHeight",185,199,209),OffsetHeader=Flag(spec,"offset"),FlowDirection=Flag(spec,"rtl")?FlowDirection.RightToLeft:FlowDirection.LeftToRight};
                if(spec.TryGetProperty("height",out var height))shell.Height=height.GetDouble();if(spec.TryGetProperty("minimum",out var minimum))shell.MinHeight=minimum.GetDouble();if(spec.TryGetProperty("padding",out var padding))shell.Padding=new Thickness(padding.GetDouble());if(spec.TryGetProperty("contentPadding",out var contentPadding))shell.ContentPadding=new Thickness(contentPadding.GetDouble());if(spec.TryGetProperty("headerToken",out var token))shell.HeaderHeight=token.GetDouble();
                root.Children.Add(shell);Canvas.SetLeft(shell,20);Canvas.SetTop(shell,20);await Layout(shell);frames.Add(new{id=row.GetProperty("id").GetString(),scheme=row.GetProperty("scheme").GetString(),measurement=Snapshot(shell)});root.Children.Remove(shell);root.UpdateLayout();Guard();
            }
            Assert(frames.Count==168,"Incomplete AppShell native matrix.");
            foreach(var scheme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                root.RequestedTheme=scheme;await Viewport(900,640);var prefix=scheme+"/";var headerInput=new TextBox{Text="Original header input"};var header=new Grid{Height=48};header.Children.Add(headerInput);var input=new TextBox{Text="Original body input"};var body=new Grid{Height=120};body.Children.Add(input);var footer=new Button{Content="Original local action",Height=32};var headerProducer=new TextBlock{Tag=header};var bodyProducer=new TextBlock{Tag=body};var footerProducer=new TextBlock{Tag=footer};var shell=new BleeckerAppShell{Width=480};
                foreach(var(property,producer)in new[]{(BleeckerAppShell.HeaderProperty,headerProducer),(ContentControl.ContentProperty,bodyProducer),(BleeckerAppShell.FooterProperty,footerProducer)})shell.SetBinding(property,Bind(producer));root.Children.Add(shell);await Layout(shell);var originalMain=Part(shell,"AppShellMain");var unloads=0;input.Unloaded+=(_,_)=>unloads++;
                Check(prefix+"original-slots",()=>Assert(ReferenceEquals(shell.Header,header)&&ReferenceEquals(shell.Content,body)&&ReferenceEquals(shell.Footer,footer)&&new[]{BleeckerAppShell.HeaderProperty,ContentControl.ContentProperty,BleeckerAppShell.FooterProperty}.All(property=>shell.GetBindingExpression(property)is not null),"AppShell replaced original slots or bindings."));
                Check(prefix+"native-landmark",()=>Assert(!shell.IsTabStop&&FrameworkElementAutomationPeer.CreatePeerForElement(originalMain).GetLandmarkType()==AutomationLandmarkType.Main,"AppShell did not expose its passive Main landmark."));
                input.Text="Edited body input";headerInput.Text="Edited header input";header.Height=80;shell.Width=240;shell.OffsetHeader=false;await Layout(shell);
                Check(prefix+"retained-edits-and-controls",()=>Assert(input.Text=="Edited body input"&&headerInput.Text=="Edited header input"&&unloads==0&&ReferenceEquals(Part(shell,"AppShellMain"),originalMain)&&ReferenceEquals(shell.Content,body),"Slot/width updates replaced caller controls, edits or the main region."));
                await Viewport(640,360);await Layout(shell);Check(prefix+"actual-viewport",()=>Assert(shell.MinHeight==360&&shell.ActualHeight==360&&input.Text=="Edited body input"&&unloads==0,$"Actual native viewport resize differs: minimum={shell.MinHeight}, height={shell.ActualHeight}, unloads={unloads}, input={input.Text}."));
                shell.OffsetHeader=true;var tokenProducer=new TextBlock{Tag=96.5};shell.SetBinding(BleeckerAppShell.HeaderHeightProperty,Bind(tokenProducer));await Layout(shell);Check(prefix+"token-binding",()=>Assert(shell.HeaderHeight==96.5&&shell.GetBindingExpression(BleeckerAppShell.HeaderHeightProperty)is not null,"Source header token binding was replaced."));
                var paddingProducer=new TextBlock{Tag=new Thickness(12)};shell.SetBinding(BleeckerAppShell.ContentPaddingProperty,Bind(paddingProducer));await Layout(shell);paddingProducer.Tag=new Thickness(0);await Layout(shell);Check(prefix+"zero-padding-binding",()=>Assert(shell.ContentPadding==new Thickness(0)&&shell.GetBindingExpression(BleeckerAppShell.ContentPaddingProperty)is not null&&Part(shell,"AppShellContent").TransformToVisual(originalMain).TransformPoint(new()).Y==0,"Explicit zero main padding lost its binding or recipe override."));shell.ClearValue(BleeckerAppShell.ContentPaddingProperty);await Layout(shell);Check(prefix+"clear-padding-restores-recipe",()=>Assert(Part(shell,"AppShellContent").TransformToVisual(originalMain).TransformPoint(new()).Y==96.5,"Clearing main padding did not restore the source token recipe."));
                var minimumProducer=new TextBlock{Tag=100d};shell.SetBinding(FrameworkElement.MinHeightProperty,Bind(minimumProducer));await Layout(shell);Check(prefix+"minimum-binding",()=>Assert(shell.MinHeight==100&&shell.GetBindingExpression(FrameworkElement.MinHeightProperty)is not null,"Native minimum binding was replaced."));minimumProducer.Tag=500d;await Layout(shell);Check(prefix+"live-minimum-binding",()=>Assert(shell.MinHeight==500&&shell.ActualHeight==500,"Native minimum producer did not resize the shell."));shell.ClearValue(FrameworkElement.MinHeightProperty);await Layout(shell);Assert(shell.MinHeight==360,"Clearing a nonzero caller minimum did not restore the viewport.");shell.MinHeight=0;await Layout(shell);shell.ClearValue(FrameworkElement.MinHeightProperty);await Layout(shell);Check(prefix+"clear-minimum-restores-viewport",()=>Assert(shell.MinHeight==360,"Clearing caller minimum did not restore viewport sizing."));
                var originalStyle=shell.Style;var baseStyle=new Style(typeof(BleeckerAppShell)){BasedOn=originalStyle};baseStyle.Setters.Add(new Setter(FrameworkElement.MinHeightProperty,140d));shell.Style=new Style(typeof(BleeckerAppShell)){BasedOn=baseStyle};await Layout(shell);Check(prefix+"minimum-style",()=>Assert(shell.MinHeight==140,"Inherited caller minimum setter was replaced by viewport sizing."));shell.Style=originalStyle;await Layout(shell);Check(prefix+"clear-style-restores-viewport",()=>Assert(shell.MinHeight==360,"Removing the caller minimum style did not restore the viewport recipe."));
                var nameProducer=new TextBlock{Tag="Caller shell"};shell.SetBinding(AutomationProperties.NameProperty,Bind(nameProducer));await Layout(shell);var peer=FrameworkElementAutomationPeer.CreatePeerForElement(shell);Check(prefix+"caller-metadata",()=>Assert(peer.GetName()=="Caller shell"&&shell.GetBindingExpression(AutomationProperties.NameProperty)is not null,"Caller shell metadata was replaced."));nameProducer.Tag="";await Layout(shell);peer.InvalidatePeer();Check(prefix+"empty-name-binding",()=>Assert(peer.GetName()=="","Empty caller name was replaced."));
                var model=new AppShellConsumerModel{Value="Original shell model"};var template=(DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding Value}' Height='24'/></DataTemplate>");shell.HeaderTemplate=template;shell.ContentTemplate=template;shell.FooterTemplate=template;headerProducer.Tag=model;bodyProducer.Tag=model;footerProducer.Tag=model;await Layout(shell);Check(prefix+"original-template-models",()=>Assert(HiddenDesc<ContentPresenter>(shell).Count(presenter=>ReferenceEquals(presenter.Content,model))==3&&HiddenDesc<TextBlock>(shell).Count(text=>text.Text=="Original shell model")==3&&ReferenceEquals(shell.Header,model)&&ReferenceEquals(shell.Content,model)&&ReferenceEquals(shell.Footer,model),"Slot templates did not receive original models."));model.Value="Changed shell model";await Layout(shell);Check(prefix+"live-template-models",()=>Assert(HiddenDesc<TextBlock>(shell).Count(text=>text.Text=="Changed shell model")==3,"Slot template model bindings were lost."));
                var selector=new AppShellConsumerSelector(template);shell.HeaderTemplateSelector=selector;shell.ContentTemplateSelector=selector;shell.FooterTemplateSelector=selector;shell.HeaderTemplate=null;shell.ContentTemplate=null;shell.FooterTemplate=null;await Layout(shell);Check(prefix+"template-selectors",()=>Assert(selector.Models.Count>=3&&selector.Models.All(item=>ReferenceEquals(item,model)),"Selectors did not receive original model objects."));
                headerProducer.Tag=header;bodyProducer.Tag=body;footerProducer.Tag=footer;shell.HeaderTemplateSelector=null;shell.ContentTemplateSelector=null;shell.FooterTemplateSelector=null;await Layout(shell);var originalTemplate=shell.Template;shell.Template=null;root.UpdateLayout();await Task.Delay(20);Guard();Check(prefix+"template-detach",()=>Assert(Field(shell,"panel")is null&&Field(shell,"header")is null&&Field(shell,"body")is null&&Field(shell,"footer")is null,"Removed template retained owned parts."));shell.Template=originalTemplate;await Layout(shell);Check(prefix+"template-reapply",()=>Assert(!ReferenceEquals(Part(shell,"AppShellMain"),originalMain)&&ReferenceEquals(shell.Content,body)&&ReferenceEquals(shell.Header,header)&&ReferenceEquals(shell.Footer,footer)&&input.Text=="Edited body input"&&shell.GetBindingExpression(ContentControl.ContentProperty)is not null,"Template reapply lost original state."));
                root.Children.Remove(shell);root.UpdateLayout();await Task.Delay(20);Guard();Check(prefix+"unload-observers",()=>Assert(Field(shell,"observedRoot")is null,"Unloaded shell retained its viewport subscription."));root.Children.Add(shell);await Layout(shell);Check(prefix+"remount-state",()=>Assert(ReferenceEquals(shell.Content,body)&&input.Text=="Edited body input"&&headerInput.Text=="Edited header input"&&shell.MinHeight==360,"Remount lost original caller state or viewport recipe."));root.Children.Clear();root.UpdateLayout();Guard();
                var plain=new BleeckerAppShell{Width=240,MinHeight=100,Header="Caller header",Content="Caller body",Footer="Caller footer"};root.Children.Add(plain);await Layout(plain);
                Check(prefix+"plain-text-metadata",()=>{var children=PeerChildren(FrameworkElementAutomationPeer.CreatePeerForElement(plain)).ToArray();foreach(var name in new[]{"Caller header","Caller body","Caller footer"})Assert(children.Any(peer=>peer.GetName()==name&&peer.GetAutomationControlType()==AutomationControlType.Text&&peer.IsContentElement()),"Original scalar text was absent from the native content tree: "+name);});root.Children.Clear();root.UpdateLayout();Guard();
            }
            completed=failures.Count==0;
        }
        catch(Exception error){failure=error.ToString();}
        finally
        {
            var visible=window?.Visible??false;var nativeVisible=handle!=0&&IsWindowVisible(handle);GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var isolated=!visible&&!nativeVisible&&activationEvents==0&&isolationFailure is null&&foreground!=Environment.ProcessId;var assembly=typeof(BleeckerAppShell).Assembly.Location;var librarySha256=Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(assembly)));
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{passed=completed&&isolated,scope="hidden-app-shell-contracts",hiddenWindow=true,activateCalled=false,showCalled=false,focusCalled=false,inputSent=false,visible,nativeVisible,activationEvents,guardChecks=guards,isolated,processArchitecture=RuntimeInformation.ProcessArchitecture.ToString(),osArchitecture=RuntimeInformation.OSArchitecture.ToString(),referenceSha256=referenceHash,librarySha256,frames,checks,failures,failure,diagnostics,isolationFailure,visibleRuntimeVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false},new JsonSerializerOptions{WriteIndented=true}));window?.Close();Environment.Exit(completed&&isolated?0:1);
        }
    }
    [Bindable]
    private sealed class AppShellConsumerModel:System.ComponentModel.INotifyPropertyChanged
    {
        private string value="";public string Value{get=>value;set{this.value=value;PropertyChanged?.Invoke(this,new(nameof(Value)));}}public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class AppShellConsumerSelector(DataTemplate template):DataTemplateSelector
    {
        internal List<object> Models{get;}=[];
        protected override DataTemplate SelectTemplateCore(object item,DependencyObject container){if(item is not null)Models.Add(item);return template;}
    }
}
