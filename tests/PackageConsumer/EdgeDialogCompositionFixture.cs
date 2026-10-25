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
using Windows.Foundation;

namespace Orchard.Testing;

// Public APIs and real popup owners, also compiled against a fresh NuGet restore.
internal static class EdgeDialogCompositionFixture
{
    private delegate bool Visitor(nint window,nint parameter);
    [DllImport("user32.dll")]private static extern bool EnumWindows(Visitor visitor,nint parameter);
    [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint window,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(nint window);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var c in Elements(VisualTreeHelper.GetChild(node,i)))yield return c;}
    private static IEnumerable<DependencyObject> Parents(DependencyObject child){for(var p=VisualTreeHelper.GetParent(child);p is not null;p=VisualTreeHelper.GetParent(p))yield return p;}
    internal static async Task RunAsync(Window window,string output,string hostPath)
    {
        var folder=Path.GetDirectoryName(output)!;Directory.CreateDirectory(folder);var background=new Grid();var host=new BleeckerModalHost{Content=background,Width=900,Height=760};window.Content=host;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,760));
        var guards=0;var focus=0;var activations=0;var invokes=0;var isolationFailed=false;var cases=new List<object>();var failures=new List<object>();var stage="initial";var owner="drawer";
        host.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((handle,_)=>{GetWindowThreadProcessId(handle,out var process);visible|=process==Environment.ProcessId&&IsWindowVisible(handle);return true;},0);isolationFailed|=visible||window.Visible||window.AppWindow.IsVisible||foreground==Environment.ProcessId||focus>0||activations>0||host.XamlRoot is not null&&VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot).Count>0;Require(!isolationFailed,"Edge composition left its hidden, unactivated, popup-free boundary.");}
        async Task Layout(){for(var i=0;i<6;i++){var idle=new TaskCompletionSource();Require(host.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Could not drain edge composition.");await idle.Task;host.Measure(new Size(900,760));host.Arrange(new Rect(0,0,900,760));host.UpdateLayout();Guard();await Task.Delay(12);}}
        async Task<BleeckerEdgeDialog> Open(object content){BleeckerEdgeDialog dialog=owner=="drawer"?new BleeckerDrawer():new BleeckerSheet{Side=Enum.Parse<BleeckerSheetSide>(owner,true)};dialog.Title="Fictional composition owner";dialog.Content=content;background.Children.Add(dialog);await Layout();dialog.IsOpen=true;await Task.Delay(280);await Layout();return dialog;}
        async Task Close(BleeckerEdgeDialog dialog){dialog.IsOpen=false;await Task.Delay(220);await Layout();}
        async Task Invoke(Button button){((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke)).Invoke();invokes++;await Layout();}
        object Bounds(FrameworkElement e){var p=e.TransformToVisual(host).TransformPoint(new Point());Require(double.IsFinite(p.X)&&double.IsFinite(p.Y)&&e.ActualWidth>0&&e.ActualHeight>0&&p.X>=0&&p.Y>=0&&p.X+e.ActualWidth<=900&&p.Y+e.ActualHeight<=760,"Owned popup escaped its actual XamlRoot viewport.");return new{x=p.X,y=p.Y,width=e.ActualWidth,height=e.ActualHeight};}
        async Task Clear(){foreach(var d in background.Children.OfType<BleeckerEdgeDialog>().ToArray())d.IsOpen=false;await Task.Delay(220);background.Children.Clear();await Layout();Require(!Elements(host).Any(e=>e.Name is "DrawerSurface" or "SheetSurface" or "PopupBorder"),"Owned edge presentation leaked after removal.");}
        async Task Case(string scheme,string name,Func<Task<object>> action){stage=scheme+"-"+owner+"-"+name;File.WriteAllText(Path.Combine(folder,"stage.json"),JsonSerializer.Serialize(new{stage,guards,cases=cases.Count}));try{await Clear();var evidence=await action();Guard();cases.Add(new{scheme,owner,name,passed=true,evidence});}catch(Exception error){failures.Add(new{scheme,owner,name,error=error.ToString()});cases.Add(new{scheme,owner,name,passed=false});if(isolationFailed)throw;}finally{if(!isolationFailed)await Clear();}}
        try
        {
            foreach(var scheme in new[]{"Light","Dark"})foreach(var kind in new[]{"drawer","left","right","top","bottom"})
            {
                owner=kind;host.RequestedTheme=Enum.Parse<ElementTheme>(scheme);
                await Case(scheme,"owned-popover",async()=>{
                    var editor=new TextBox{Text="Retained fictional edge edit"};var body=new StackPanel{Width=240};body.Children.Add(editor);var popup=new BleeckerPopover{Trigger=new Button{Content="Fictional settings",Width=180,Height=36},Content=body,Open=true};var dialog=await Open(popup);var frame=Parents(editor).OfType<Border>().First(b=>b.Child is ContentControl);Require(editor.IsLoaded&&frame.IsHitTestVisible&&popup.Open==true,"Owned Popover did not mount original content.");var bounds=Bounds(frame);var upper=await Open(new Border{Height=30});Require(!frame.IsHitTestVisible&&popup.Open==true&&ReferenceEquals(popup.Content,body),"Newer owner did not suspend the controlled Popover.");await Close(upper);Require(frame.IsHitTestVisible&&editor.IsLoaded&&editor.Text=="Retained fictional edge edit","Owned Popover did not resume its original editor.");await Close(dialog);Require(!editor.IsLoaded&&popup.Open==true&&ReferenceEquals(popup.Content,body),"Closing owner retained presentation or rewrote controlled data.");dialog.IsOpen=true;await Task.Delay(280);await Layout();Require(editor.IsLoaded&&editor.Text=="Retained fictional edge edit"&&ReferenceEquals(popup.Content,body),"Reopening owner lost original Popover content.");return new{bounds,owned=true,suspended=true,resumed=true,closedWithOwner=true,reopenedWithOriginalContent=true,callerOpenRetained=true};
                });
                await Case(scheme,"owned-tooltip",async()=>{
                    var target=new Button{Content="Fictional help trigger",Width=180,Height=36};var body=new StackPanel();body.Children.Add(target);var dialog=await Open(body);var tip=new BleeckerTooltip{Content="Fictional owned edge guidance"};BleeckerTooltipService.SetToolTip(target,tip);tip.IsOpen=true;await Layout();var frame=VisualTreeHelper.GetParent(tip)as Border??throw new InvalidOperationException("Missing owned Tooltip input frame.");Require(tip.IsLoaded&&frame.IsHitTestVisible,"Owned Tooltip did not mount.");var bounds=Bounds(frame);var upper=await Open(new Border{Height=30});Require(!frame.IsHitTestVisible&&tip.IsOpen,"Newer owner did not suspend Tooltip.");await Close(upper);Require(frame.IsHitTestVisible&&tip.IsOpen,"Owned Tooltip did not resume.");await Close(dialog);Require(!tip.IsOpen&&!tip.IsLoaded,"Closing owner retained Tooltip.");dialog.IsOpen=true;await Task.Delay(280);await Layout();Require(!tip.IsOpen,"Reopening owner invented a Tooltip open request.");tip.IsOpen=true;await Layout();Require(tip.IsLoaded&&ReferenceEquals(BleeckerTooltipService.GetToolTip(target),tip),"Tooltip service lost original data after owner remount.");tip.IsOpen=false;BleeckerTooltipService.SetToolTip(target,null);return new{bounds,owned=true,suspended=true,resumed=true,closedWithOwner=true,reopenedOnlyOnRequest=true,originalTooltipRetained=true};
                });
                await Case(scheme,"owned-select",async()=>{
                    var options=new[]{new BleeckerSelectOption("a","Fictional Alpha"),new BleeckerSelectOption("b","Fictional Beta")};var value=new TextBox{Text="a"};var select=new BleeckerSelect{Options=options,Width=240};select.SetBinding(BleeckerSelect.ValueProperty,new Binding{Source=value,Path=new PropertyPath(nameof(TextBox.Text))});var requests=new List<BleeckerSelectRequestedEventArgs>();select.ValueRequested+=(_,e)=>requests.Add(e);var dialog=await Open(select);var chooser=Elements(select).OfType<ItemsControl>().Single(c=>FrameworkElementAutomationPeer.CreatePeerForElement(c).GetAutomationControlType()==AutomationControlType.ComboBox);var peer=(IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(chooser).GetPattern(PatternInterface.ExpandCollapse);peer.Expand();await Layout();var option=chooser.Items.OfType<Button>().Single(i=>ReferenceEquals(i.Tag,options[1]));var popup=Elements(host).OfType<Border>().Single(b=>b.Name=="PopupBorder");var frame=(Border)VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(popup));var bounds=Bounds(popup);var upper=await Open(new Border{Height=30});Require(!frame.IsHitTestVisible,"Newer owner did not suspend Select.");await Invoke(option);Require(requests.Count==0,"Suspended Select emitted a request.");await Close(upper);await Invoke(option);Require(requests.Count==1&&ReferenceEquals(requests[0].Option,options[1])&&requests[0].Value=="b"&&select.Value=="a"&&value.Text=="a"&&dialog.IsOpen,"Owned Select rewrote caller value or lost original option.");peer.Expand();await Layout();await Close(dialog);Require(peer.ExpandCollapseState==ExpandCollapseState.Collapsed&&select.GetBindingExpression(BleeckerSelect.ValueProperty)is not null&&ReferenceEquals(select.Options,options),"Closing owner retained dropdown or rewrote its data binding.");return new{bounds,owned=true,suspendedInvokeIgnored=true,originalOptionRetained=true,callerValueRetained=true,bindingRetained=true,closedWithOwner=true};
                });
            }
            await Clear();Guard();
        }catch(Exception error){failures.Add(new{stage,error=error.ToString()});}
        string Hash(string p)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));
        await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-edge-composition-v1",passed=failures.Count==0,cases,failures,guards,focus,activations,softwareNativeInvokes=invokes,publicApisOnly=true,librarySha256=Hash(typeof(BleeckerDrawer).Assembly.Location),hostSha256=Hash(hostPath),architecture=RuntimeInformation.ProcessArchitecture.ToString(),activateCalled=false,showCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,nativeComponentAccepted=false},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(failures.Count==0?0:1);
    }
}
