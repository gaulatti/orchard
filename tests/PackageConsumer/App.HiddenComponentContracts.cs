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
    private async void RunHiddenComponentContractsAsync(string referenceDirectory)
    {
        var args=Environment.GetCommandLineArgs();var outputIndex=Array.IndexOf(args,"--verify-output");
        if(outputIndex<0||outputIndex+1>=args.Length){Environment.Exit(2);return;}
        var output=args[outputIndex+1];var foregroundBefore=GetForegroundWindow();var handle=nint.Zero;
        var frames=new List<object>();var checks=new List<object>();var failures=new List<string>();
        var guardChecks=0;var activationEvents=0;string? failure=null;string? isolationFailure=null;string? referenceHash=null;var completed=false;
        void Guard()
        {
            guardChecks++;GetWindowThreadProcessId(GetForegroundWindow(),out var process);
            if(isolationFailure is not null||window!.Visible||window.AppWindow.IsVisible||IsWindowVisible(handle)||process==Environment.ProcessId)
                throw new InvalidOperationException(isolationFailure??"Hidden component test became visible or owned foreground input.");
        }
        void Check(string id,Action assertion)
        {
            Guard();try{assertion();checks.Add(new{id,passed=true});}
            catch(Exception error){failures.Add(id+": "+error.Message);checks.Add(new{id,passed=false,error=error.Message});}Guard();
        }
        static void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        try
        {
            // This branch never constructs MainWindow, activates/shows a window,
            // calls Focus, invokes an enabled button, sends input or changes OS settings.
            window=new Window();handle=WinRT.Interop.WindowNative.GetWindowHandle(window);Guard();
            window.Activated+=(_,eventArgs)=>{if(eventArgs.WindowActivationState!=WindowActivationState.Deactivated){activationEvents++;isolationFailure="Hidden component test activated its window.";}};
            window.VisibilityChanged+=(_,eventArgs)=>{if(eventArgs.Visible)isolationFailure="Hidden component test showed its window.";};
            var root=new Canvas{Width=240,Height=160,UseLayoutRounding=false};window.Content=root;Guard();
            async Task Layout(FrameworkElement control)
            {
                for(var i=0;i<3;i++){root.Measure(new Size(240,160));root.Arrange(new Rect(0,0,240,160));root.UpdateLayout();if(control is Control templated)templated.ApplyTemplate();Guard();await Task.Delay(10);}
                Assert(control.IsLoaded,"Hidden component did not enter the real XAML tree.");Guard();
            }
            object Box(FrameworkElement node){var r=node.TransformToVisual(root).TransformBounds(new Rect(0,0,node.ActualWidth,node.ActualHeight));return new{x=r.X,y=r.Y,width=r.Width,height=r.Height};}
            void Mount(FrameworkElement node){Canvas.SetLeft(node,24);Canvas.SetTop(node,24);root.Children.Add(node);}
            static string Label(BleeckerThemeMode mode)=>mode switch{BleeckerThemeMode.Light=>"Switch to dark theme",BleeckerThemeMode.Dark=>"Switch to system theme",_=>"Switch to light theme"};
            static string? Tip(BleeckerThemeToggle toggle)=>ToolTipService.GetToolTip(toggle) is ToolTip tip?tip.Content as string:ToolTipService.GetToolTip(toggle) as string;
            var bytes=await File.ReadAllBytesAsync(Path.Combine(referenceDirectory,"ThemeToggle.json"));referenceHash=Convert.ToHexStringLower(SHA256.HashData(bytes));using var source=JsonDocument.Parse(bytes);
            Assert(source.RootElement.GetProperty("sourceSha256").GetString()=="57e3f4181d2b50fc0aa7dcd56205ef62e94955d4067d5e20c58254c34bd638d9","Unexpected Theme Toggle source reference.");
            foreach(var row in source.RootElement.GetProperty("cases").EnumerateArray().Where(row=>row.GetProperty("spec").GetProperty("state").GetString()=="normal"))
            {
                root.RequestedTheme=Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!);var spec=row.GetProperty("spec");var toggle=new BleeckerThemeToggle();
                if(spec.TryGetProperty("mode",out var mode))toggle.Mode=Enum.Parse<BleeckerThemeMode>(mode.GetString()!,true);
                if(spec.TryGetProperty("size",out var size))toggle.Size=size.GetString()switch{"sm"=>BleeckerIconButtonSize.Small,"lg"=>BleeckerIconButtonSize.Large,_=>BleeckerIconButtonSize.Medium};
                if(spec.TryGetProperty("variant",out var variant))toggle.Variant=Enum.Parse<BleeckerIconButtonVariant>(variant.GetString()!,true);
                if(spec.TryGetProperty("disabled",out var disabled))toggle.IsEnabled=!disabled.GetBoolean();
                if(spec.TryGetProperty("rtl",out var rtl)&&rtl.GetBoolean())toggle.FlowDirection=FlowDirection.RightToLeft;
                if(spec.TryGetProperty("name",out var name))AutomationProperties.SetName(toggle,name.GetString()!);
                if(spec.TryGetProperty("title",out var title))ToolTipService.SetToolTip(toggle,title.GetString());
                root.Children.Clear();Mount(toggle);await Layout(toggle);
                var peer=FrameworkElementAutomationPeer.CreatePeerForElement(toggle);var icon=HiddenDesc<ContentControl>(toggle).Single(node=>node.Name=="ThemeIcon");var vector=HiddenDesc<Viewbox>(icon).Single();var shapes=HiddenDesc<Microsoft.UI.Xaml.Shapes.Path>(icon).ToArray();var paint=(SolidColorBrush)shapes[0].Stroke;
                frames.Add(new{id=row.GetProperty("id").GetString(),scheme=row.GetProperty("scheme").GetString(),measurement=new{mode=toggle.Mode.ToString().ToLowerInvariant(),label=peer.GetName(),title=Tip(toggle),disabled=!toggle.IsEnabled,box=Box(toggle),icon=Box(vector),strokeRgba=new[]{(int)paint.Color.R,paint.Color.G,paint.Color.B,paint.Color.A},strokeOpacity=paint.Opacity,strokeWidth=shapes[0].StrokeThickness,iconOpacity=icon.Opacity,paths=shapes.Length,tabStop=toggle.IsTabStop,role=peer.GetAutomationControlType().ToString(),loaded=toggle.IsLoaded}});Guard();
            }
            Assert(frames.Count==116,"Incomplete hidden Theme Toggle normal-state matrix.");
            foreach(var scheme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                root.RequestedTheme=scheme;root.Children.Clear();var producer=new TextBlock{Tag=BleeckerThemeMode.System};var toggle=new BleeckerThemeToggle();
                toggle.SetBinding(BleeckerThemeToggle.ModeProperty,new Binding{Source=producer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});Mount(toggle);await Layout(toggle);
                var peer=FrameworkElementAutomationPeer.CreatePeerForElement(toggle);var invoke=(IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);var defaultStyle=toggle.Style;var requestCount=0;toggle.ThemeRequested+=(_,_)=>requestCount++;
                var prefix=scheme+"/";
                Check(prefix+"default-name-tip",()=>Assert(peer.GetName()==Label(BleeckerThemeMode.System)&&Tip(toggle)==Label(BleeckerThemeMode.System),"Default mode metadata differs."));
                var callerBaseStyle=new Style(typeof(BleeckerThemeToggle)){BasedOn=defaultStyle};callerBaseStyle.Setters.Add(new Setter(AutomationProperties.NameProperty,""));callerBaseStyle.Setters.Add(new Setter(ToolTipService.ToolTipProperty,"Styled tooltip"));var callerStyle=new Style(typeof(BleeckerThemeToggle)){BasedOn=callerBaseStyle};
                var styleProducer=new TextBlock{Tag=callerStyle};toggle.SetBinding(FrameworkElement.StyleProperty,new Binding{Source=styleProducer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});await Layout(toggle);peer.InvalidatePeer();
                Check(prefix+"style-empty-name",()=>Assert(peer.GetName()=="","Caller style's explicit empty name was replaced."));
                Check(prefix+"style-tooltip",()=>Assert(Tip(toggle)=="Styled tooltip","Default tooltip hid the caller style's tooltip."));
                producer.Tag=BleeckerThemeMode.Dark;await Layout(toggle);
                Check(prefix+"style-mode-update",()=>Assert(peer.GetName()==""&&Tip(toggle)=="Styled tooltip"&&toggle.GetBindingExpression(BleeckerThemeToggle.ModeProperty)is not null&&toggle.GetBindingExpression(FrameworkElement.StyleProperty)is not null,"Mode update replaced caller metadata or bindings."));
                var nameProducer=new TextBlock{Tag="Bound caller name"};toggle.SetBinding(AutomationProperties.NameProperty,new Binding{Source=nameProducer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});var tipProducer=new TextBlock{Tag="Bound caller tooltip"};toggle.SetBinding(ToolTipService.ToolTipProperty,new Binding{Source=tipProducer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});producer.Tag=BleeckerThemeMode.Light;await Layout(toggle);
                Check(prefix+"local-bindings-win",()=>Assert(peer.GetName()=="Bound caller name"&&Tip(toggle)=="Bound caller tooltip"&&toggle.GetBindingExpression(AutomationProperties.NameProperty)is not null&&toggle.GetBindingExpression(ToolTipService.ToolTipProperty)is not null,"Mode update replaced local metadata bindings."));
                nameProducer.Tag="";tipProducer.Tag="";await Layout(toggle);Check(prefix+"bound-empty-values",()=>Assert(peer.GetName()==""&&Tip(toggle)=="","Empty consumer binding was replaced."));
                toggle.ClearValue(AutomationProperties.NameProperty);toggle.ClearValue(ToolTipService.ToolTipProperty);await Layout(toggle);Check(prefix+"clear-restores-style",()=>Assert(peer.GetName()==""&&Tip(toggle)=="Styled tooltip","Clearing local metadata did not reveal caller style."));
                var emptyTipStyle=new Style(typeof(BleeckerThemeToggle)){BasedOn=callerStyle};emptyTipStyle.Setters.Add(new Setter(ToolTipService.ToolTipProperty,""));styleProducer.Tag=emptyTipStyle;await Layout(toggle);Check(prefix+"style-empty-tooltip",()=>Assert(peer.GetName()==""&&string.IsNullOrEmpty(Tip(toggle)),"Empty styled tooltip did not remain absent."));
                var nullStyleRejected=false;try{_ = new Setter(ToolTipService.ToolTipProperty,null);}catch(ArgumentException){nullStyleRejected=true;}Check(prefix+"invalid-null-style-setter",()=>Assert(nullStyleRejected,"WinUI unexpectedly accepted an unsupported null Setter constructor."));
                styleProducer.Tag=defaultStyle;await Layout(toggle);Check(prefix+"style-removal-restores-default",()=>Assert(peer.GetName()==Label(BleeckerThemeMode.Light)&&Tip(toggle)==Label(BleeckerThemeMode.Light),"Removing caller style did not restore mode metadata."));
                ToolTipService.SetToolTip(toggle,null);producer.Tag=BleeckerThemeMode.Dark;await Layout(toggle);Check(prefix+"local-null-tooltip",()=>Assert(ToolTipService.GetToolTip(toggle)is null,"Explicit local null tooltip was replaced."));toggle.ClearValue(ToolTipService.ToolTipProperty);await Layout(toggle);Check(prefix+"clear-restores-default-tip",()=>Assert(Tip(toggle)==Label(BleeckerThemeMode.Dark),"Clearing a local tooltip did not restore the default."));
                var label=new TextBlock{Text="Caller label"};root.Children.Add(label);AutomationProperties.SetLabeledBy(toggle,label);await Layout(toggle);Check(prefix+"caller-label",()=>Assert(peer.GetName()=="Caller label","Caller LabeledBy did not name the button."));toggle.ClearValue(AutomationProperties.LabeledByProperty);
                var sibling=new BleeckerThemeToggle{Style=defaultStyle,Mode=BleeckerThemeMode.Light};Mount(sibling);await Layout(sibling);sibling.Mode=BleeckerThemeMode.System;await Layout(sibling);Check(prefix+"shared-style-metadata",()=>Assert(Tip(toggle)==Label(BleeckerThemeMode.Dark)&&Tip(sibling)==Label(BleeckerThemeMode.System)&&!ReferenceEquals(ToolTipService.GetToolTip(toggle),ToolTipService.GetToolTip(sibling)),"A shared style reused another control's default tooltip."));root.Children.Remove(sibling);
                var originalContent=toggle.Content;var contentProducer=new TextBlock{Tag="Consumer-owned content"};toggle.SetBinding(ContentControl.ContentProperty,new Binding{Source=contentProducer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});producer.Tag=BleeckerThemeMode.Light;await Layout(toggle);Check(prefix+"content-binding",()=>Assert(toggle.Content as string=="Consumer-owned content"&&toggle.GetBindingExpression(ContentControl.ContentProperty)is not null&&peer.GetName()==Label(BleeckerThemeMode.Light),"Mode update replaced consumer content or default action metadata."));
                AutomationProperties.SetName(toggle,"");await Layout(toggle);Check(prefix+"empty-name-with-content",()=>Assert(peer.GetName()=="","Consumer content replaced an explicit empty name."));toggle.ClearValue(AutomationProperties.NameProperty);styleProducer.Tag=callerStyle;await Layout(toggle);Check(prefix+"style-empty-name-with-content",()=>Assert(peer.GetName()=="","Consumer content replaced a styled empty name."));styleProducer.Tag=defaultStyle;toggle.ClearValue(ContentControl.ContentProperty);toggle.Content=originalContent;await Layout(toggle);
                toggle.IsEnabled=false;var disabledRejected=false;try{invoke.Invoke();}catch(ElementNotEnabledException){disabledRejected=true;}Check(prefix+"disabled-invoke",()=>Assert(disabledRejected&&requestCount==0,"Disabled hidden button emitted a request."));toggle.IsEnabled=true;
                var template=toggle.Template;toggle.Template=null;await Layout(toggle);toggle.Template=template;producer.Tag=BleeckerThemeMode.System;await Layout(toggle);Check(prefix+"template-binding",()=>Assert(toggle.Mode==BleeckerThemeMode.System&&toggle.GetBindingExpression(BleeckerThemeToggle.ModeProperty)is not null,"Template replacement lost the consumer mode."));
                root.Children.Remove(toggle);root.UpdateLayout();await Task.Delay(20);Guard();var detachedRejected=false;try{invoke.Invoke();}catch(ElementNotAvailableException){detachedRejected=true;}Check(prefix+"detached-invoke",()=>Assert(detachedRejected&&requestCount==0,"Detached hidden button emitted a request."));
                Mount(toggle);producer.Tag=BleeckerThemeMode.Dark;await Layout(toggle);Check(prefix+"remount-binding",()=>Assert(toggle.Mode==BleeckerThemeMode.Dark&&peer.GetName()==Label(BleeckerThemeMode.Dark)&&Tip(toggle)==Label(BleeckerThemeMode.Dark)&&requestCount==0,"Remount lost binding, metadata or request boundaries."));root.Children.Clear();Guard();
            }
            completed=failures.Count==0;
        }
        catch(Exception error){failure=error.ToString();}
        finally
        {
            var visible=window?.Visible??false;var nativeVisible=handle!=nint.Zero&&IsWindowVisible(handle);GetWindowThreadProcessId(GetForegroundWindow(),out var foregroundProcess);
            var isolated=!visible&&!nativeVisible&&activationEvents==0&&isolationFailure is null&&foregroundProcess!=Environment.ProcessId;
            var libraryAssembly=typeof(BleeckerThemeToggle).Assembly.Location;var librarySha256=Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(libraryAssembly)));
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{passed=completed&&isolated,scope="hidden-component-contracts",hiddenWindow=true,activateCalled=false,showCalled=false,focusCalled=false,inputSent=false,enabledButtonInvoked=false,visible,nativeVisible,activationEvents,guardChecks,isolated,foregroundBefore=(long)foregroundBefore,foregroundAfter=(long)GetForegroundWindow(),processArchitecture=RuntimeInformation.ProcessArchitecture.ToString(),osArchitecture=RuntimeInformation.OSArchitecture.ToString(),displayScale=handle==nint.Zero?0:GetDpiForWindow(handle)/96d,referenceSha256=referenceHash,libraryAssembly,librarySha256,frames,checks,failures,failure,isolationFailure,visibleRuntimeVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false},new JsonSerializerOptions{WriteIndented=true}));
            window?.Close();Environment.Exit(completed&&isolated?0:1);
        }
    }
}
