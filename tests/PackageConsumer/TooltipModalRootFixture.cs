using System.ComponentModel;
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
using Orchard;
using Windows.Foundation;

namespace PackageConsumer;

// Public library/native APIs only; the window stays hidden and unactivated.
internal static class TooltipModalRootFixture
{
    private delegate bool WindowVisitor(nint window,nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor,nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window,out uint process);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    private sealed class Model : INotifyPropertyChanged
    {
        private string label="Fictional packaged guidance";
        public string Label {get=>label;set{label=value;PropertyChanged?.Invoke(this,new(nameof(Label)));}}
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Selector(DataTemplate template) : DataTemplateSelector
    {
        internal readonly List<object> Seen=[];
        protected override DataTemplate SelectTemplateCore(object item){if(item is not Model)throw new InvalidOperationException("Tooltip selector did not receive the original model.");Seen.Add(item);return template;}
        protected override DataTemplate SelectTemplateCore(object item,DependencyObject container)=>SelectTemplateCore(item);
    }
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement element)yield return element;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    internal static async Task RunAsync(Window window,string output)
    {
        var root=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}'/>");window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,760));
        var cases=new List<object>();var guards=0;var focus=0;var activations=0;var invokes=0;
        window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};root.GotFocus+=(_,_)=>focus++;
        void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((handle,_)=>{GetWindowThreadProcessId(handle,out var owner);visible|=owner==Environment.ProcessId&&IsWindowVisible(handle);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Package Tooltip acquired visibility, activation or focus.");Require(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count==0,"Hosted package Tooltip opened a native Popup.");}
        async Task Layout(){for(var i=0;i<6;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Package Tooltip could not drain.");await idle.Task;await Task.Delay(12);root.Measure(new Size(900,760));root.Arrange(new Rect(0,0,900,760));root.UpdateLayout();Guard();}}
        bool HasTooltip(AutomationPeer peer)=>peer.GetAutomationControlType()==AutomationControlType.ToolTip||(peer.GetChildren()?.Any(HasTooltip)??false);
        static Border PortalRoot(BleeckerTooltip tip)=>VisualTreeHelper.GetParent(tip) as Border??throw new InvalidOperationException("Tooltip has no private input wrapper.");
        foreach(var scheme in new[]{ElementTheme.Light,ElementTheme.Dark})
        {
            root.RequestedTheme=scheme;var content=new Grid();var target=new Button{Content="Fictional packaged help",Width=180,Height=36,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Margin=new(200,200,0,0)};content.Children.Add(target);var host=new BleeckerModalHost{Content=content};root.Children.Add(host);await Layout();
            var enabled=new ToggleSwitch{IsOn=true};target.SetBinding(Control.IsEnabledProperty,new Binding{Source=enabled,Path=new(nameof(ToggleSwitch.IsOn))});var binding=target.GetBindingExpression(Control.IsEnabledProperty);
            var model=new Model();var template=(DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding Label}' TextWrapping='Wrap' LineHeight='19.5' LineStackingStrategy='BlockLineHeight'/></DataTemplate>");var selector=new Selector(template);var tip=new BleeckerTooltip{Content=model,ContentTemplateSelector=selector};AutomationProperties.SetName(tip,"Fictional packaged help");var opens=0;var closes=0;tip.Opened+=(_,_)=>opens++;tip.Closed+=(_,_)=>closes++;BleeckerTooltipService.SetToolTip(target,tip);tip.IsOpen=true;await Layout();
            Require(tip.IsLoaded&&PortalRoot(tip).IsHitTestVisible&&ReferenceEquals(tip.Content,model)&&ReferenceEquals(tip.ContentTemplateSelector,selector)&&selector.Seen.Count>0&&selector.Seen.All(item=>ReferenceEquals(item,model)),"Package Tooltip replaced the original model/selector.");Require(Elements(tip).OfType<TextBlock>().Any(t=>t.Visibility==Visibility.Visible&&t.Text==model.Label),"Original model template was not rendered.");cases.Add(new{name="original-model-and-selector",scheme=scheme.ToString(),originalModel=true,originalSelector=true,visibleTemplate=true,opens});
            var dialog=new BleeckerAlertDialog{Title="Fictional packaged confirmation",Description="Component ownership demonstration."};dialog.CancelRequested+=(_,_)=>dialog.IsOpen=false;content.Children.Add(dialog);await Layout();dialog.IsOpen=true;await Task.Delay(260);await Layout();var corePeer=FrameworkElementAutomationPeer.CreatePeerForElement(host).GetChildren().Single();
            model.Label="Updated fictional guidance";await Layout();Require(tip.IsOpen&&tip.IsLoaded&&!PortalRoot(tip).IsHitTestVisible&&!HasTooltip(corePeer)&&target.IsEnabled&&ReferenceEquals(binding,target.GetBindingExpression(Control.IsEnabledProperty)),"Newer modal changed Tooltip caller data or left its input/peer active.");cases.Add(new{name="newer-modal-pauses-tooltip",scheme=scheme.ToString(),requestRetained=true,pointerBlocked=true,peerExcluded=true,bindingRetained=true});
            dialog.Cancel();await Task.Delay(180);await Layout();Require(tip.IsOpen&&PortalRoot(tip).IsHitTestVisible&&HasTooltip(corePeer)&&Elements(tip).OfType<TextBlock>().Any(t=>t.Text==model.Label),"Tooltip did not resume original live template content.");cases.Add(new{name="resume-live-template",scheme=scheme.ToString(),originalModel=true,liveValueRendered=true,peerPresent=true});
            BleeckerTooltipService.SetToolTip(target,null);await Layout();Require(!tip.IsOpen&&!tip.IsLoaded&&tip.PlacementTarget is null&&!AutomationProperties.GetDescribedBy(target).Contains(tip)&&opens==1&&closes==1,"Removing the service did not release Tooltip once.");cases.Add(new{name="service-removal",scheme=scheme.ToString(),closedOnce=true,descriptionReleased=true,ownerReleased=true});content.Children.Remove(dialog);content.Children.Remove(target);
            var parent=new BleeckerAlertDialog{Title="Fictional parent component",Description="Owned help demonstration."};content.Children.Add(parent);await Layout();var confirmations=0;parent.ConfirmRequested+=(_,_)=>confirmations++;parent.IsOpen=true;await Task.Delay(260);await Layout();
            var parentAction=Elements(root).OfType<BleeckerButton>().Single(b=>b.Name=="AlertDialogConfirm");var original=new Border{Width=120,Height=39};var owned=new BleeckerTooltip{Content=original,Placement=Microsoft.UI.Xaml.Controls.Primitives.PlacementMode.Right,RequestedTheme=ElementTheme.Light};BleeckerTooltipService.SetToolTip(parentAction,owned);owned.IsOpen=true;await Layout();Require(owned.IsLoaded&&PortalRoot(owned).IsHitTestVisible&&ReferenceEquals(owned.Content,original)&&owned.ActualTheme==ElementTheme.Light&&AutomationProperties.GetDescribedBy(parentAction).Contains(owned),"Public modal owner lost original Tooltip content or explicit theme.");cases.Add(new{name="public-modal-owned-tooltip",scheme=scheme.ToString(),originalContent=true,explicitTheme=true,descriptionLinked=true});
            var upper=new BleeckerAlertDialog{Title="Fictional upper component",Description="Temporary component ownership."};upper.CancelRequested+=(_,_)=>upper.IsOpen=false;content.Children.Add(upper);await Layout();upper.IsOpen=true;await Task.Delay(260);await Layout();Require(owned.IsOpen&&!PortalRoot(owned).IsHitTestVisible&&!HasTooltip(corePeer),"Owned Tooltip remained active below a newer modal.");upper.Cancel();await Task.Delay(180);await Layout();Require(owned.IsOpen&&PortalRoot(owned).IsHitTestVisible&&ReferenceEquals(owned.Content,original),"Owned Tooltip did not resume original content.");cases.Add(new{name="owned-tooltip-suspension",scheme=scheme.ToString(),inactivePointerBlocked=true,inactivePeerExcluded=true,originalContentResumed=true});
            var action=Elements(parentAction).OfType<Button>().Single();((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(action).GetPattern(PatternInterface.Invoke)).Invoke();invokes++;await Task.Delay(180);await Layout();Require(!owned.IsOpen&&!owned.IsLoaded&&confirmations==1&&parent.IsOpen,"Original owner invocation did not close help and retain the caller-controlled parent.");cases.Add(new{name="original-owner-invocation",scheme=scheme.ToString(),nativeInvoke=true,confirmationRequests=confirmations,parentRetained=true,tooltipClosed=true});BleeckerTooltipService.SetToolTip(parentAction,null);parent.IsOpen=false;await Task.Delay(180);await Layout();content.Children.Remove(upper);content.Children.Remove(parent);
            var nestedTarget=new Button{Content="Fictional nested help",Width=160,Height=36};var body=new StackPanel();body.Children.Add(nestedTarget);var popup=new BleeckerPopover{Trigger=new Button{Content="Fictional component settings"},Content=body,Open=true,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Margin=new(100,100,0,0)};content.Children.Add(popup);await Layout();var nested=new BleeckerTooltip{Content="Fictional help inside Popover"};BleeckerTooltipService.SetToolTip(nestedTarget,nested);nested.IsOpen=true;await Layout();Require(popup.IsOpen&&nested.IsLoaded&&PortalRoot(nested).IsHitTestVisible,"Tooltip in original Popover content did not open.");popup.Open=false;await Layout();Require(!nested.IsOpen&&!nested.IsLoaded,"Unloading Popover content retained its Tooltip.");BleeckerTooltipService.SetToolTip(nestedTarget,null);cases.Add(new{name="nested-popover-lifetime",scheme=scheme.ToString(),originalOwner=true,openedWithParent=true,closedWithOwner=true});content.Children.Remove(popup);root.Children.Remove(host);await Layout();
        }
        await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-package-tooltip-modal-root-v1",passed=true,librarySha256=Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(BleeckerTooltip).Assembly.Location))),hostSha256=Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(App).Assembly.Location))),cases,softwareNativeInvokes=invokes,guards,focus,activations,publicApisOnly=true,activateCalled=false,showWindowCalled=false,focusCalled=false,hardwareInputSent=false,nativeInputFocusVerified=false,nativeComponentAccepted=false},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
    }
}
