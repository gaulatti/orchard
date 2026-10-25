using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;
namespace Orchard.Testing;

// Observes actual public WinUI properties under real timers. It does not inject
// a model clock, change system settings, activate a window or claim pixel parity.
internal static class EdgeMotionFixture
{
    private delegate bool Visitor(nint window,nint parameter);
    [DllImport("user32.dll")]private static extern bool EnumWindows(Visitor visitor,nint parameter);
    [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint window,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(nint window);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject n){if(n is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(n);i++)foreach(var c in Elements(VisualTreeHelper.GetChild(n,i)))yield return c;}
    private sealed record Sample(string Phase,double Milliseconds,double Surface,double Backdrop,double Axis,double OtherAxis,double Dimension,double SurfaceAge,double BackdropAge);
    internal static async Task RunAsync(Window window,string output,string hostPath)
    {
        var folder=Path.GetDirectoryName(output)!;Directory.CreateDirectory(folder);var background=new Grid();var host=new BleeckerModalHost{Width=900,Height=760,Content=background};window.Content=host;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,760));var clock=Stopwatch.StartNew();var animations=new UISettings().AnimationsEnabled;var guards=0;var focus=0;var activations=0;var cases=new List<object>();var stage="initial";
        host.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0&&VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot).Count==0,"Motion properties probe acquired visibility, activation, focus or a native Popup.");}
        async Task Layout(){var idle=new TaskCompletionSource();Require(host.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Could not drain native motion.");await idle.Task;host.Measure(new Size(900,760));host.Arrange(new Rect(0,0,900,760));host.UpdateLayout();Guard();}
        try
        {
            foreach(var scheme in new[]{"Light","Dark"})foreach(var side in new[]{"drawer","left","right","top","bottom"})
            {
                stage=scheme+"-"+side;File.WriteAllText(Path.Combine(folder,"stage.json"),JsonSerializer.Serialize(new{stage,guards,cases=cases.Count}));host.RequestedTheme=Enum.Parse<ElementTheme>(scheme);var drawer=side=="drawer";var vertical=side is "left" or "right";var sign=side is "left" or "top"?-1:1;var prefix=drawer?"Drawer":"Sheet";var original=new Grid{Height=80};BleeckerEdgeDialog dialog=drawer?new BleeckerDrawer():new BleeckerSheet{Side=Enum.Parse<BleeckerSheetSide>(side,true)};dialog.Title="Fictional component panel";dialog.Content=original;background.Children.Add(dialog);await Layout();dialog.IsOpen=true;FrameworkElement? mounted=null;for(var attempt=0;attempt<80;attempt++){await Layout();mounted=Elements(host).SingleOrDefault(e=>e.Name==prefix+"Surface");if(mounted is not null)break;await Task.Delay(2);}var surface=mounted??throw new InvalidOperationException("Native edge surface did not mount within the probe boundary.");var backdrop=Elements(host).Single(e=>e.Name==prefix+"Backdrop");var snapshots=new List<Sample>();
                var actualTransform=(TranslateTransform)surface.RenderTransform;var surfacePublished=clock.Elapsed.TotalMilliseconds;var backdropPublished=surfacePublished;var transformProperty=vertical?TranslateTransform.XProperty:TranslateTransform.YProperty;var transformToken=actualTransform.RegisterPropertyChangedCallback(transformProperty,(_,_)=>surfacePublished=clock.Elapsed.TotalMilliseconds);var backdropToken=backdrop.RegisterPropertyChangedCallback(UIElement.OpacityProperty,(_,_)=>backdropPublished=clock.Elapsed.TotalMilliseconds);
                Sample Take(string phase){Require(surface.RenderTransform is TranslateTransform,"Missing actual translate presentation.");var t=(TranslateTransform)surface.RenderTransform;var dimension=vertical?surface.ActualWidth:surface.ActualHeight;var axis=vertical?t.X:t.Y;var other=vertical?t.Y:t.X;Require(dimension>0&&axis*sign>=-.00000001&&Math.Abs(other)<.00000001,"Incorrect native translation axis or sign.");var value=1-axis*sign/dimension;Require(value>=-.00000001&&value<=1.00000001&&backdrop.Opacity>=0&&backdrop.Opacity<=1,"Native motion escaped normalized endpoints.");var time=clock.Elapsed.TotalMilliseconds;var s=new Sample(phase,time,value,backdrop.Opacity,axis,other,dimension,Math.Max(0,time-surfacePublished),Math.Max(0,time-backdropPublished));snapshots.Add(s);return s;}
                async Task Samples(string phase,int count){for(var i=0;i<count;i++){await Task.Delay(8);await Layout();Take(phase);}}
                void Continuous(Sample a,Sample b){var elapsed=b.Milliseconds-a.Milliseconds;var bound=.03+.03*(elapsed+Math.Max(a.SurfaceAge,a.BackdropAge));Require(Math.Abs(a.Surface-b.Surface)<=bound&&Math.Abs(a.Backdrop-b.Backdrop)<=bound,"An interrupted native transition snapped instead of retaining its current presentation.");}
                Take("initial");await Samples("opening",6);var opening=snapshots.Where(s=>s.Phase=="opening").ToArray();if(animations)Require(opening.Any(s=>s.Surface>.01&&s.Surface<.99),"Normal entry had no intermediate native presentation.");Require(opening.All(s=>Math.Abs(s.Surface-s.Backdrop)<.00000001),"Independent opening properties disagree with the common source curve.");
                var beforeClose=Take("before-close");dialog.IsOpen=false;var afterClose=Take("after-close");if(animations)Continuous(beforeClose,afterClose);Require(!((IWindowProvider)FrameworkElementAutomationPeer.CreatePeerForElement(surface).GetPattern(PatternInterface.Window)).IsTopmost,"Closing native layer remained active.");await Samples("closing",6);var closing=snapshots.Where(s=>s.Phase=="closing").ToArray();if(animations){if(drawer)Require(closing.Any(s=>s.Surface-s.Backdrop>.02)&&closing.All(s=>s.Surface>=s.Backdrop-.00000001),"Drawer did not present the source's independent exit curves.");else Require(closing.All(s=>Math.Abs(s.Surface-s.Backdrop)<.00000001),"Sheet exit properties disagree with their common source curve.");}
                var beforeReopen=Take("before-reopen");dialog.IsOpen=true;var afterReopen=Take("after-reopen");if(animations)Continuous(beforeReopen,afterReopen);await Samples("reopening",6);Require(ReferenceEquals(surface,Elements(host).Single(e=>e.Name==prefix+"Surface"))&&ReferenceEquals(dialog.Content,original),"Interrupted reentry replaced the surface or original content.");await Task.Delay(300);await Layout();var settled=Take("settled");Require(settled.Surface==1&&settled.Backdrop==1,"Reopened native presentation did not reach its exact endpoints.");dialog.IsOpen=false;await Task.Delay(220);await Layout();Require(!Elements(host).Any(e=>e.Name==prefix+"Surface"),"Closing native presentation was retained beyond its presence interval.");actualTransform.UnregisterPropertyChangedCallback(transformProperty,transformToken);backdrop.UnregisterPropertyChangedCallback(UIElement.OpacityProperty,backdropToken);background.Children.Remove(dialog);await Layout();cases.Add(new{scheme,side,passed=true,normalAnimationPreference=animations,closingDeactivated=true,originalSurfaceRetained=true,originalContentRetained=true,settled=true,closedPresentationReleased=true,samples=snapshots});
            }
            string Hash(string p)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-edge-motion-properties-v1",passed=true,cases,guards,focus,activations,animationsEnabled=animations,systemSettingsReadOnly=true,publicApisOnly=true,modelClockInjected=false,activateCalled=false,showCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,nativePixelMotionVerified=false,nativeComponentAccepted=false,librarySha256=Hash(typeof(BleeckerDrawer).Assembly.Location),hostSha256=Hash(hostPath)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-edge-motion-properties-v1",passed=false,stage,error=error.ToString(),cases,guards,focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
}
