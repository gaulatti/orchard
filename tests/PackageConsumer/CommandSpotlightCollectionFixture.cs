using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Testing;

internal static class CommandSpotlightCollectionFixture
{
    internal static async Task VerifyCatalogCycle(FrameworkElement fixture,BleeckerModalHost host,Func<int,Task> layout,Action<bool,string> require)
    {
        var cycle=Elements(fixture).OfType<Button>().Single(b=>b.Name=="CollectionCycle");var control=Elements(fixture).OfType<BleeckerCommandSpotlight>().Single(c=>c.Name=="Collection");var originals=control.Actions.ToArray();
        void Invoke()=>((Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(cycle).GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)).Invoke();
        Invoke();await layout(40);require(control.Actions.Count()==1000&&originals.Select((a,i)=>ReferenceEquals(a,control.Actions.ElementAt(i))).All(v=>v),"Catalog collection expansion replaced originals");
        control.Show();await layout(350);require(Elements(host).OfType<Button>().Count(b=>b.Name=="SpotlightAction")==1000,"Catalog large collection did not reach native rows");control.Close();await layout(220);
        Invoke();await layout(40);require(control.Actions.Count()==32&&originals.Zip(control.Actions).All(p=>ReferenceEquals(p.First,p.Second)),"Catalog collection reset did not retain originals");
    }
    private delegate bool Visitor(nint handle,nint value);
    [DllImport("user32.dll")]private static extern bool EnumWindows(Visitor visitor,nint value);
    [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement element)yield return element;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    internal static async Task RunAsync(Window window,string output,string hostAssembly)
    {
        var folder=Path.GetDirectoryName(output)!;var root=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}'/>");
        var host=new BleeckerModalHost{Content=root,Width=1000,Height=960};window.Content=host;window.AppWindow.ResizeClient(new(1000,960));
        var focus=0;var activations=0;var guards=0;var stage="initial";var cases=new List<object>();var measurements=new List<object>();
        host.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Require(bool value,string label){if(!value)throw new InvalidOperationException(label);}
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Collection probe left its hidden boundary");Require(host.XamlRoot is null||VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot).Count==0,"Collection probe opened a native Popup");}
        async Task Layout(){for(var i=0;i<3;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Dispatcher refused layout");await idle.Task;host.Measure(new(1000,960));host.Arrange(new(0,0,1000,960));host.UpdateLayout();Guard();}}
        async Task Measure(string theme,string grouping,int count,string phase,int sample,Action operation)
        {
            stage=$"{theme}/{grouping}/{count}/{phase}/{sample}";File.WriteAllText(Path.Combine(folder,"collections-stage.json"),JsonSerializer.Serialize(new{stage,cases=cases.Count,guards}));
            var allocated=GC.GetAllocatedBytesForCurrentThread();var clock=Stopwatch.StartNew();operation();var sync=clock.Elapsed.TotalMilliseconds;var bytes=GC.GetAllocatedBytesForCurrentThread()-allocated;await Layout();clock.Stop();
            measurements.Add(new{theme,grouping,count,phase,sample,synchronousMilliseconds=sync,settledMilliseconds=clock.Elapsed.TotalMilliseconds,synchronousAllocatedBytes=bytes});
        }
        Dictionary<string,Button> Rows()=>Elements(host).OfType<Button>().Where(b=>b.Name=="SpotlightAction").ToDictionary(b=>((BleeckerCommandSpotlightAction)b.Tag).Id);
        try
        {
            await Layout();
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})foreach(var grouping in new[]{"single","unique"})foreach(var count in new[]{100,500,1000})
            {
                root.Children.Clear();await Layout();host.RequestedTheme=theme;var loaded=0;var unloaded=0;
                var originals=Enumerable.Range(0,count).Select(i=>new BleeckerCommandSpotlightAction(i.ToString(),$"Fictional item {i:D6}"){Group=grouping=="unique"?$"Group {i:D6}":"Group",Disabled=i%17==0}).ToArray();
                var icons=new Dictionary<string,TextBlock>();foreach(var action in originals.Where((_,i)=>i%100==42)){var icon=new TextBlock{Tag=action};icon.SetBinding(TextBlock.TextProperty,new Binding{Source=action,Path=new("Title"),Mode=BindingMode.OneWay});icon.Loaded+=(_,_)=>loaded++;icon.Unloaded+=(_,_)=>unloaded++;action.Icon=icon;icons[action.Id]=icon;}
                var control=new BleeckerCommandSpotlight{Open=true,Margin=new(32),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top};root.Children.Add(control);await Layout();await Task.Delay(350);await Layout();
                await Measure(theme.ToString(),grouping,count,"set",0,()=>control.Actions=originals);
                var initial=Rows();Require(initial.Count==count&&originals.All(a=>ReferenceEquals(initial[a.Id].Tag,a)),"Original collection reaches native rows");
                var beforeLoaded=loaded;var beforeUnloaded=unloaded;
                for(var sample=0;sample<3;sample++)await Measure(theme.ToString(),grouping,count,"refresh",sample,()=>control.TriggerLabel="Measured refresh "+sample);
                var afterRefresh=Rows();var refreshRetained=originals.All(a=>ReferenceEquals(initial[a.Id],afterRefresh[a.Id]));Require(refreshRetained,"Refresh replaced caller rows");
                var refreshLoaded=loaded-beforeLoaded;var refreshUnloaded=unloaded-beforeUnloaded;
                beforeLoaded=loaded;beforeUnloaded=unloaded;
                await Measure(theme.ToString(),grouping,count,"reverse",0,()=>control.Actions=originals.Reverse().ToArray());
                var afterReverse=Rows();var reverseRetained=originals.All(a=>ReferenceEquals(initial[a.Id],afterReverse[a.Id]));Require(reverseRetained,"Reorder replaced original row objects");
                var reverseLoaded=loaded-beforeLoaded;var reverseUnloaded=unloaded-beforeUnloaded;
                var order=Elements(host).OfType<Button>().Where(b=>b.Name=="SpotlightAction").Select(b=>((BleeckerCommandSpotlightAction)b.Tag).Id).ToArray();Require(order.SequenceEqual(originals.Reverse().Select(a=>a.Id)),"Native order differs from caller order");
                var editor=Elements(host).OfType<TextBox>().Single(e=>e.Name=="SpotlightSearch");await Measure(theme.ToString(),grouping,count,"filter",0,()=>editor.Text="000042");
                Require(Rows().Count==1&&ReferenceEquals(control.SelectedAction,originals[42]),"Editor query did not retain the matching original");
                await Measure(theme.ToString(),grouping,count,"clear",0,()=>editor.Text="");Require(Rows().Count==count,"Clearing query did not restore collection");
                originals[42].Title="Updated original fictional item";await Layout();var boundIcons=icons.All(p=>ReferenceEquals(p.Value.Tag,originals[int.Parse(p.Key)])&&p.Value.Text==originals[int.Parse(p.Key)].Title&&p.Value.GetBindingExpression(TextBlock.TextProperty)is not null&&ReferenceEquals(p.Value,originals[int.Parse(p.Key)].Icon));Require(boundIcons,"Caller icon model/binding changed");
                cases.Add(new{theme=theme.ToString(),grouping,count,refreshRetained,reverseRetained,refreshLoaded,refreshUnloaded,reverseLoaded,reverseUnloaded,originalIconsAndBindingsRetained=boundIcons,callerOrderRetained=true,queryBoundaryVerified=true,passed=true});
            }
            root.Children.Clear();await Layout();Guard();
            string Hash(string p)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-command-spotlight-collections-native-v1",passed=true,librarySha256=Hash(typeof(BleeckerCommandSpotlight).Assembly.Location),hostSha256=Hash(hostAssembly),cases,measurements,guards,focusEvents=focus,activations,hostVisible=window.Visible,hardwareInputSent=false,latencyAcceptanceClaimed=false,nativeComponentAccepted=false},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-command-spotlight-collections-native-v1",passed=false,stage,error=error.ToString(),cases,measurements,guards,focusEvents=focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
}
