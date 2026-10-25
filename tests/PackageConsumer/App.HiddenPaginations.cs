using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Orchard;
using Windows.Foundation;
using Windows.Graphics;

namespace PackageConsumer;

public partial class App
{
    private async void RunHiddenPaginationsAsync(string referenceDirectory)
    {
        var arguments=Environment.GetCommandLineArgs();var output=arguments[Array.IndexOf(arguments,"--verify-output")+1];
        nint handle=0;var frames=new List<object>();var checks=new List<object>();var failures=new List<string>();
        var completed=false;var guards=0;var activationEvents=0;string? failure=null,isolationFailure=null,referenceHash=null;
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);if(isolationFailure is not null||window!.Visible||window.AppWindow.IsVisible||IsWindowVisible(handle)||foreground==Environment.ProcessId)throw new InvalidOperationException(isolationFailure??"Hidden Pagination became visible or owned foreground input.");}
        static void Assert(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        void Check(string id,Action action){Guard();try{action();checks.Add(new{id,passed=true});}catch(Exception error){failures.Add(id+": "+error.Message);checks.Add(new{id,passed=false,error=error.Message});}Guard();}
        static Button[] Actions(BleeckerPagination pagination)=>(FrameworkElementAutomationPeer.CreatePeerForElement(pagination).GetChildren()??[]).Select(peer=>((FrameworkElementAutomationPeer)peer).Owner).OfType<Button>().ToArray();
        try
        {
            window=new Window();handle=WinRT.Interop.WindowNative.GetWindowHandle(window);Guard();window.Activated+=(_,args)=>{if(args.WindowActivationState!=WindowActivationState.Deactivated){activationEvents++;isolationFailure="Hidden Pagination window activated.";}};window.VisibilityChanged+=(_,args)=>{if(args.Visible)isolationFailure="Hidden Pagination window became visible.";};
            window.AppWindow.ResizeClient(new SizeInt32(700,160));var root=new Canvas{Width=700,Height=160,UseLayoutRounding=false};window.Content=root;
            async Task Layout(){for(var i=0;i<3;i++){root.Measure(new Size(700,160));root.Arrange(new Rect(0,0,700,160));root.UpdateLayout();Guard();await Task.Delay(10);}}
            object Box(FrameworkElement element){var b=element.TransformToVisual(root).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));return new{x=b.X,y=b.Y,width=b.Width,height=b.Height};}
            void Mount(BleeckerPagination pagination){Canvas.SetLeft(pagination,20);Canvas.SetTop(pagination,20);root.Children.Add(pagination);}
            await Layout();Assert(root.XamlRoot is not null&&root.XamlRoot.RasterizationScale==1,"Hidden Pagination requires the actual 100% native root.");
            var bytes=await File.ReadAllBytesAsync(Path.Combine(referenceDirectory,"Pagination.json"));referenceHash=Convert.ToHexStringLower(SHA256.HashData(bytes));using var reference=JsonDocument.Parse(bytes);
            Assert(reference.RootElement.GetProperty("sourceSha256").GetString()=="9111a975e78d31dcd8e3c68b588110547b45ee31facad43b81f84bb9e7318834","Unexpected Pagination source.");
            foreach(var source in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                var spec=source.GetProperty("spec");if(spec.TryGetProperty("state",out _))continue;
                root.RequestedTheme=Enum.Parse<ElementTheme>(source.GetProperty("scheme").GetString()!);
                var pagination=new BleeckerPagination{Width=spec.GetProperty("width").GetDouble(),CurrentPage=spec.GetProperty("currentPage").GetDouble(),TotalPages=spec.GetProperty("totalPages").GetDouble(),HasPrevPage=spec.GetProperty("hasPrevPage").GetBoolean(),HasNextPage=spec.GetProperty("hasNextPage").GetBoolean(),FlowDirection=spec.TryGetProperty("rtl",out var rtl)&&rtl.GetBoolean()?FlowDirection.RightToLeft:FlowDirection.LeftToRight};
                Mount(pagination);await Layout();Assert(pagination.IsLoaded,"Package Pagination did not mount.");var visible=pagination.TotalPages>1;
                var nav=HiddenDesc<FrameworkElement>(pagination).Single(element=>element.Name=="PaginationNavigation");
                frames.Add(new{id=source.GetProperty("id").GetString(),scheme=source.GetProperty("scheme").GetString(),measurement=new{visible,nav=visible?Box(nav):null,label=FrameworkElementAutomationPeer.CreatePeerForElement(pagination).GetName(),buttons=Actions(pagination).Select(button=>new{text=button.Name is "PaginationPrevious" or "PaginationNext"?"":AutomationProperties.GetName(button),title=button.Name=="PaginationPrevious"?"Previous page":button.Name=="PaginationNext"?"Next page":"",disabled=!button.IsEnabled,box=Box(button),opacity=button.Opacity,backgroundSizing=button.BackgroundSizing.ToString(),presenterBackgroundSizing=HiddenDesc<ContentPresenter>(button).Single(presenter=>presenter.Name=="ContentPresenter").BackgroundSizing.ToString()}).ToArray()}});
                root.Children.Remove(pagination);await Layout();
            }
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                root.RequestedTheme=theme;var producer=new TextBlock{Tag=4d};var pagination=new BleeckerPagination{Width=600,TotalPages=12,HasPrevPage=true,HasNextPage=true};
                pagination.SetBinding(BleeckerPagination.CurrentPageProperty,new Binding{Source=producer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});var requests=new List<double>();pagination.PageRequested+=(_,args)=>requests.Add(args.Page);Mount(pagination);await Layout();
                var next=Actions(pagination)[^1];var provider=(IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(next).GetPattern(PatternInterface.Invoke);provider.Invoke();await Layout();provider.Invoke();await Layout();
                Check(theme+"-declined-binding",()=>Assert(requests.SequenceEqual(new[]{5d,5})&&pagination.CurrentPage==4&&pagination.GetBindingExpression(BleeckerPagination.CurrentPageProperty)is not null,"Package Pagination mutated its producer or binding."));
                var seven=Actions(pagination).Single(button=>AutomationProperties.GetName(button)=="7");producer.Tag=8d;await Layout();
                Check(theme+"-retained-page",()=>Assert(ReferenceEquals(seven,Actions(pagination).Single(button=>AutomationProperties.GetName(button)=="7"))&&requests.Count==2,"Package Pagination replaced a retained page or emitted a programmatic request."));
                pagination.IsEnabled=false;Check(theme+"-disabled",()=>{var rejected=false;try{provider.Invoke();}catch(ElementNotEnabledException){rejected=true;}Assert(rejected,"Disabled package Pagination remained actionable.");});pagination.IsEnabled=true;
                AutomationProperties.SetName(pagination,"Caller pagination — 東京");producer.Tag=1d;await Layout();
                Check(theme+"-metadata",()=>Assert(FrameworkElementAutomationPeer.CreatePeerForElement(pagination).GetName()=="Caller pagination — 東京"&&requests.Count==2,"Package Pagination replaced caller metadata."));
                var previous=(IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Actions(pagination)[0]).GetPattern(PatternInterface.Invoke);previous.Invoke();await Layout();
                Check(theme+"-availability",()=>Assert(requests.SequenceEqual(new[]{5d,5,0})&&pagination.CurrentPage==1,"Package Pagination inferred availability or assigned the rejected page."));
                pagination.TotalPages=1;await Layout();Check(theme+"-empty",()=>Assert(pagination.ActualHeight==0&&Actions(pagination).Length==0&&!FrameworkElementAutomationPeer.CreatePeerForElement(pagination).IsControlElement(),"Empty package Pagination retained navigation."));pagination.TotalPages=12;await Layout();
                var oldProvider=(IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Actions(pagination)[0]).GetPattern(PatternInterface.Invoke);var template=pagination.Template;pagination.Template=null;await Layout();pagination.Template=template;await Layout();
                Check(theme+"-template",()=>{var rejected=false;try{oldProvider.Invoke();}catch(ElementNotAvailableException){rejected=true;}Assert(rejected&&pagination.GetBindingExpression(BleeckerPagination.CurrentPageProperty)is not null&&requests.Count==3,"Package Pagination lost template binding or enabled a stale peer.");});
                provider=(IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Actions(pagination)[^1]).GetPattern(PatternInterface.Invoke);root.Children.Remove(pagination);await Layout();
                Check(theme+"-detach",()=>{var rejected=false;try{provider.Invoke();}catch(ElementNotAvailableException){rejected=true;}Assert(rejected&&requests.Count==3,"Detached package Pagination remained actionable.");});
                Mount(pagination);await Layout();producer.Tag=6d;await Layout();Check(theme+"-remount",()=>Assert(pagination.CurrentPage==6&&pagination.GetBindingExpression(BleeckerPagination.CurrentPageProperty)is not null&&requests.Count==3,"Package Pagination lost remount binding."));root.Children.Remove(pagination);await Layout();
            }
            Assert(frames.Count==52&&checks.Count==18,"Incomplete package Pagination checks.");completed=failures.Count==0;
        }
        catch(Exception error){failure=error.ToString();}
        finally
        {
            var visible=window?.Visible??false;var nativeVisible=handle!=0&&IsWindowVisible(handle);GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var isolated=!visible&&!nativeVisible&&activationEvents==0&&isolationFailure is null&&foreground!=Environment.ProcessId;var assembly=typeof(BleeckerPagination).Assembly.Location;var librarySha256=Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(assembly)));
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{passed=completed&&isolated,scope="hidden-pagination-contracts",hiddenWindow=true,activateCalled=false,showCalled=false,focusCalled=false,inputSent=false,visible,nativeVisible,activationEvents,guardChecks=guards,isolated,processArchitecture=RuntimeInformation.ProcessArchitecture.ToString(),osArchitecture=RuntimeInformation.OSArchitecture.ToString(),referenceSha256=referenceHash,librarySha256,frames,checks,failures,failure,isolationFailure,visibleRuntimeVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false},new JsonSerializerOptions{WriteIndented=true}));window?.Close();Environment.Exit(completed&&isolated?0:1);
        }
    }
}
