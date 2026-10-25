using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Testing;

// Runs identically in the catalog and an independently restored package consumer.
internal static class ModalContentFixture
{
    private sealed class Model:INotifyPropertyChanged
    {
        private string text="Fictional short note";private double height=20;
        public string Text{get=>text;set{text=value;PropertyChanged?.Invoke(this,new(nameof(Text)));}}
        public double Height{get=>height;set{height=value;PropertyChanged?.Invoke(this,new(nameof(Height)));}}
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private delegate bool Visitor(nint window,nint parameter);
    [DllImport("user32.dll")]private static extern bool EnumWindows(Visitor visitor,nint parameter);
    [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint window,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(nint window);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement element)yield return element;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    private static IEnumerable<DependencyObject> Parents(DependencyObject child){for(var parent=VisualTreeHelper.GetParent(child);parent is not null;parent=VisualTreeHelper.GetParent(parent))yield return parent;}
    internal static async Task RunAsync(Window window,string output,string hostPath)
    {
        var folder=Path.GetDirectoryName(output)!;Directory.CreateDirectory(folder);var background=new Grid();var host=new BleeckerModalHost{Content=background,Width=900,Height=760};window.Content=host;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,760));
        var guards=0;var focus=0;var activations=0;var invokes=0;var isolationFailed=false;var cases=new List<object>();var failures=new List<object>();var stage="initial";
        host.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((handle,_)=>{GetWindowThreadProcessId(handle,out var owner);visible|=owner==Environment.ProcessId&&IsWindowVisible(handle);return true;},0);isolationFailed|=visible||window.Visible||window.AppWindow.IsVisible||foreground==Environment.ProcessId||focus>0||activations>0||host.XamlRoot is not null&&VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot).Count>0;Require(!isolationFailed,"Modal content fixture left its hidden, unactivated, popup-free boundary.");}
        async Task Layout(){for(var i=0;i<6;i++){var idle=new TaskCompletionSource();Require(host.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Could not drain Modal content.");await idle.Task;host.Measure(new Size(900,760));host.Arrange(new Rect(0,0,900,760));host.UpdateLayout();Guard();await Task.Delay(12);}}
        async Task<BleeckerModal> Open(object content,DataTemplate? template=null){var modal=new BleeckerModal{Title="Fictional content contract",Content=content,ContentTemplate=template};background.Children.Add(modal);await Layout();modal.IsOpen=true;await Task.Delay(260);await Layout();return modal;}
        FrameworkElement Surface()=>Elements(host).Single(e=>e.Name=="ModalSurface"&&FrameworkElementAutomationPeer.CreatePeerForElement(e).GetPattern(PatternInterface.Window)is IWindowProvider{IsTopmost:true});
        ScrollView View()=>Elements(host).OfType<ScrollView>().Single(v=>v.Name=="ModalViewport");
        object Snapshot()=>new{surfaceHeight=Surface().ActualHeight,extent=View().ExtentHeight,viewport=View().ViewportHeight,offset=View().VerticalOffset};
        async Task Invoke(Button button){((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke)).Invoke();invokes++;await Layout();}
        async Task Clear(){foreach(var modal in background.Children.OfType<BleeckerModal>().ToArray())modal.IsOpen=false;await Task.Delay(180);background.Children.Clear();await Layout();}
        async Task Case(string scheme,string name,Func<Task<object>> action)
        {
            stage=scheme+"-"+name;File.WriteAllText(Path.Combine(folder,"stage.json"),JsonSerializer.Serialize(new{stage,guards,cases=cases.Count}));
            try{await Clear();var evidence=await action();Guard();cases.Add(new{scheme,name,passed=true,evidence});}
            catch(Exception error){failures.Add(new{scheme,name,error=error.ToString()});cases.Add(new{scheme,name,passed=false});if(isolationFailed)throw;}
            finally{if(!isolationFailed)await Clear();}
        }
        try
        {
            foreach(var scheme in new[]{"Light","Dark"})
            {
                host.RequestedTheme=Enum.Parse<ElementTheme>(scheme);
                await Case(scheme,"automatic-root-child-height",async()=>{var child=new Border{Height=20};var body=new StackPanel();body.Children.Add(child);var modal=await Open(body);var compact=Surface().ActualHeight;child.Height=1000;await Layout();var grown=Snapshot();Require(Surface().ActualHeight==728&&View().ExtentHeight==1133,"Automatic parent did not grow with its original child: "+JsonSerializer.Serialize(grown));child.Height=20;await Layout();Require(Surface().ActualHeight==compact&&ReferenceEquals(modal.Content,body)&&ReferenceEquals(body.Children[0],child),"Automatic child shrink replaced content or retained stale bounds: "+JsonSerializer.Serialize(Snapshot())+" expected compact="+compact+".");return new{grown,compact,restored=Snapshot(),originalChildRetained=true};});
                await Case(scheme,"automatic-root-add-remove",async()=>{var body=new StackPanel();body.Children.Add(new Border{Height=20});var modal=await Open(body);var compact=Surface().ActualHeight;var added=new Border{Height=1000};body.Children.Add(added);await Layout();var grown=Snapshot();Require(Surface().ActualHeight==728&&View().ExtentHeight==1153,"Adding an original child did not resize: "+JsonSerializer.Serialize(grown));body.Children.Remove(added);await Layout();Require(Surface().ActualHeight==compact&&ReferenceEquals(modal.Content,body),"Removing an original child retained stale geometry: "+JsonSerializer.Serialize(Snapshot())+" expected compact="+compact+".");return new{grown,compact,restored=Snapshot(),originalRootRetained=true};});
                await Case(scheme,"templated-model-text-reflow",async()=>{var model=new Model();var template=(DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Tag='{Binding}' Text='{Binding Text}' FontFamily='{StaticResource BleeckerPrimaryFontFamily}' FontSize='16' LineHeight='24' LineStackingStrategy='BlockLineHeight' TextWrapping='Wrap'/></DataTemplate>");var modal=await Open(model,template);var original=Elements(host).OfType<TextBlock>().Single(t=>ReferenceEquals(t.Tag,model));var compact=Surface().ActualHeight;model.Text=string.Join('\n',Enumerable.Repeat("Fictional original model paragraph",60));await Layout();var grown=Snapshot();Require(original.Text==model.Text&&Surface().ActualHeight==728&&View().ExtentHeight>1400,"Original model text did not reflow its Modal: "+JsonSerializer.Serialize(grown));model.Text="Fictional short note";await Layout();Require(Surface().ActualHeight==compact&&ReferenceEquals(modal.Content,model)&&original.IsLoaded,"Model text shrink replaced its template or retained stale bounds: "+JsonSerializer.Serialize(Snapshot())+" expected compact="+compact+".");return new{grown,compact,restored=Snapshot(),originalTemplateRetained=true};});
                await Case(scheme,"templated-root-size-binding",async()=>{var model=new Model();var template=(DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border Tag='{Binding}' Height='{Binding Height}'/></DataTemplate>");var modal=await Open(model,template);var original=Elements(host).OfType<Border>().Single(b=>ReferenceEquals(b.Tag,model));var compact=Surface().ActualHeight;model.Height=1000;await Layout();var grown=Snapshot();Require(Surface().ActualHeight==728&&View().ExtentHeight==1133&&original.Height==1000,"Bound rendered-root height did not resize.");model.Height=20;await Layout();Require(Surface().ActualHeight==compact&&original.IsLoaded&&ReferenceEquals(modal.Content,model),"Root model shrink lost its original template.");return new{grown,compact,restored=Snapshot(),bindingRetained=original.GetBindingExpression(FrameworkElement.HeightProperty)is not null};});
                await Case(scheme,"plain-content-live-lines",async()=>{var modal=await Open("Fictional short note");var compact=Surface().ActualHeight;modal.Content=string.Join('\n',Enumerable.Repeat("Fictional original plain content",200));await Layout();var grown=Snapshot();Require(Surface().ActualHeight==728&&View().ExtentHeight>1400,"Plain source text did not reflow.");modal.Content="Fictional short note";await Layout();Require(Surface().ActualHeight==compact,"Plain source text did not shrink.");return new{grown,compact,restored=Snapshot()};});
                await Case(scheme,"owned-popover-suspension",async()=>{var editor=new TextBox{Text="Retained fictional popup edit"};var body=new StackPanel{Width=240};body.Children.Add(editor);var popup=new BleeckerPopover{Trigger=new Button{Content="Fictional settings"},Content=body,Open=true};var modal=await Open(popup);var frame=Parents(editor).OfType<Border>().First(b=>b.Child is ContentControl);Require(popup.IsOpen&&editor.IsLoaded&&frame.IsHitTestVisible,"Owned Popover did not mount.");var upper=await Open(new Border{Height=30});Require(!frame.IsHitTestVisible&&popup.IsOpen,"Newer Modal did not suspend the owned Popover.");upper.IsOpen=false;await Task.Delay(180);await Layout();Require(frame.IsHitTestVisible&&editor.Text=="Retained fictional popup edit"&&modal.IsOpen,"Owned Popover did not resume original content.");return new{owned=true,suspended=true,resumed=true,originalEditorRetained=true};});
                await Case(scheme,"owned-tooltip-lifetime",async()=>{var target=new Button{Content="Fictional help trigger",Width=180,Height=36};var body=new StackPanel();body.Children.Add(target);var modal=await Open(body);var tip=new BleeckerTooltip{Content="Fictional owned guidance"};BleeckerTooltipService.SetToolTip(target,tip);tip.IsOpen=true;await Layout();var frame=VisualTreeHelper.GetParent(tip)as Border??throw new InvalidOperationException("Missing Tooltip input frame.");Require(tip.IsLoaded&&frame.IsHitTestVisible,"Modal-owned Tooltip did not mount.");var upper=await Open(new Border{Height=30});Require(frame.IsHitTestVisible==false&&tip.IsOpen,"Newer Modal did not suspend its owned Tooltip.");upper.IsOpen=false;await Task.Delay(180);await Layout();Require(frame.IsHitTestVisible&&tip.IsOpen,"Tooltip did not resume.");modal.IsOpen=false;await Task.Delay(180);await Layout();Require(!tip.IsOpen&&!tip.IsLoaded,"Closing the original Modal retained its Tooltip.");BleeckerTooltipService.SetToolTip(target,null);return new{owned=true,suspended=true,resumed=true,closedWithOwner=true};});
                await Case(scheme,"owned-select-controlled-request",async()=>{var options=new[]{new BleeckerSelectOption("a","Fictional Alpha"),new BleeckerSelectOption("b","Fictional Beta")};var value=new TextBox{Text="a"};var select=new BleeckerSelect{Options=options,Width=240};select.SetBinding(BleeckerSelect.ValueProperty,new Binding{Source=value,Path=new PropertyPath(nameof(TextBox.Text))});var requests=new List<BleeckerSelectRequestedEventArgs>();select.ValueRequested+=(_,a)=>requests.Add(a);var modal=await Open(select);var chooser=Elements(select).OfType<ItemsControl>().Single(c=>FrameworkElementAutomationPeer.CreatePeerForElement(c).GetAutomationControlType()==AutomationControlType.ComboBox);var peer=(IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(chooser).GetPattern(PatternInterface.ExpandCollapse);peer.Expand();await Layout();var option=chooser.Items.OfType<Button>().Single(i=>ReferenceEquals(i.Tag,options[1]));var popup=Elements(host).OfType<Border>().Single(b=>b.Name=="PopupBorder");var frame=(Border)VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(popup));var upper=await Open(new Border{Height=30});Require(!frame.IsHitTestVisible,"Newer Modal did not suspend Select.");await Invoke(option);Require(requests.Count==0,"Suspended Select emitted a request.");upper.IsOpen=false;await Task.Delay(180);await Layout();await Invoke(option);Require(requests.Count==1&&ReferenceEquals(requests[0].Option,options[1])&&requests[0].Value=="b"&&select.Value=="a"&&value.Text=="a"&&modal.IsOpen,"Owned Select rewrote caller data or lost its original producer.");return new{owned=true,suspendedInvokeIgnored=true,originalOptionRetained=true,callerValueRetained=true,parentRetained=true};});
            }
            await Clear();Guard();
        }
        catch(Exception error){failures.Add(new{stage,error=error.ToString()});}
        string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
        await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-modal-content-v1",passed=failures.Count==0,cases,failures,guards,focus,activations,softwareNativeInvokes=invokes,publicApisOnly=true,librarySha256=Hash(typeof(BleeckerModal).Assembly.Location),hostSha256=Hash(hostPath),architecture=RuntimeInformation.ProcessArchitecture.ToString(),activateCalled=false,showCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,nativeComponentAccepted=false},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(failures.Count==0?0:1);
    }
}
