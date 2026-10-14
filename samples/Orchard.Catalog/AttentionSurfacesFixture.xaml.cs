using System.Text.Json;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class AttentionSurfacesFixture : UserControl
{
    private readonly Dictionary<string,BleeckerAttentionSurface> cases = [];
    private readonly CounterCommand command = new();
    public AttentionSurfacesFixture()
    {
        InitializeComponent();
        foreach (var element in Enum.GetValues<BleeckerAttentionElement>())
        foreach (var density in new[] { BleeckerAttentionDensity.Comfortable,BleeckerAttentionDensity.Compact })
        foreach (var interactive in new bool?[] { null,false,true })
        {
            var name = $"{SourceElement(element)}-{density.ToString().ToLowerInvariant()}-{(interactive is null ? "undefined" : interactive.Value.ToString().ToLowerInvariant())}";
            var control = new BleeckerAttentionSurface { Element=element,Density=density,Interactive=interactive,Hue=210,Intensity=5,Width=320,HorizontalAlignment=HorizontalAlignment.Left,Content="Fictional attention surface" };
            cases.Add(name,control); Matrix.Children.Add(control);
        }
        foreach (var (width,height) in new[] { (300,120),(120,300),(192,192) }) foreach(var intensity in new[]{0,5,10})
        {
            var name = $"gradient-{width}x{height}-{intensity}";
            var control = new BleeckerAttentionSurface { Hue=210,Intensity=intensity,Width=width,Height=height,HorizontalAlignment=HorizontalAlignment.Left,Content="" };
            cases.Add(name,control); Gradients.Children.Add(control);
        }
        cases.Add("disabled-button",DisabledAction); cases.Add("empty",Empty); cases.Add("long",Long); cases.Add("override",Styled);
        Action.Command = PassiveAction.Command = command;
        Action.CommandParameter = "consumer"; PassiveAction.CommandParameter = "passive";
        var actions = 0; Action.Click += (_,_) => Outcome.Text=$"Consumer action {++actions}";
    }
    internal async Task VerifyAsync(BleeckerColorScheme scheme,string folder)
    {
        using var input = new NativeInputSession(XamlRoot,folder,$"attention-{scheme}");
        await input.AcquireAsync(Heading); await input.MoveAsync(Heading);
        using var stream = typeof(AttentionSurfacesFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.AttentionLayout.json")!;
        using var reference = JsonDocument.Parse(stream);
        var source = reference.RootElement.GetProperty("schemes").EnumerateArray().Single(value=>value.GetProperty("scheme").GetString()==scheme.ToString());
        var measurements = new List<object>();
        foreach (var sample in source.GetProperty("samples").EnumerateArray())
        {
            var name = sample.GetProperty("id").GetString()!; var control = cases[name]; control.UpdateLayout();
            var expected = sample.GetProperty("normal"); var root = (FrameworkElement)VisualTreeHelper.GetChild(control,0);
            var surface = (Border)root.FindName("PART_Surface");
            var action = (Button)root.FindName("PART_Action");
            var text = (TextBlock)root.FindName("PART_String");
            Require(control.ActualWidth==expected.GetProperty("width").GetDouble() && Math.Abs(control.ActualHeight-expected.GetProperty("height").GetDouble())<.000001,
                $"Attention source geometry differs for {name}: {control.ActualWidth}x{control.ActualHeight}, expected {expected.GetProperty("width")}x{expected.GetProperty("height")}.");
            Require(control.Element==BleeckerAttentionElement.Button ? action.Visibility==Visibility.Visible && ReferenceEquals(action.Content,surface)
                : action.Visibility==Visibility.Collapsed && action.Content is null,"Attention element did not retain its actual button/non-button structure.");
            Require(!control.IsTabStop,"Attention surface invented a wrapper keyboard stop.");
            if(name!="override") Require(ReferenceEquals(control.Background,control.Resources["BleeckerAttentionBackgroundBrush"]) && control.Background is ImageBrush {ImageSource:not null},
                $"Attention {name} did not resolve its instance-owned gradient through the native style.");
            Require(!PeerTree(FrameworkElementAutomationPeer.CreatePeerForElement(control)).Any(peer=>peer.GetName()=="Mg"),"Attention exposed its private font metric probe.");
            measurements.Add(new{name,control.ActualWidth,control.ActualHeight,padding=surface.Padding.ToString(),surface.CornerRadius,text=text.Text,
                kind=FrameworkElementAutomationPeer.CreatePeerForElement(control).GetAutomationControlType().ToString(),background=control.Background?.GetType().Name});
        }
        await File.WriteAllTextAsync(Path.Combine(folder,$"attention-measurements-{scheme}.json"),JsonSerializer.Serialize(measurements,new JsonSerializerOptions{WriteIndented=true}));
        var native = NativeAction(Action); var before = command.Count;
        ((IInvokeProvider)new ButtonAutomationPeer(native).GetPattern(PatternInterface.Invoke)).Invoke();
        Require(command.Count==before+1 && Equals(command.Last,"consumer"),"Attention native action did not route the consumer command and parameter.");
        var passive = NativeAction(PassiveAction); before = command.Count;
        ((IInvokeProvider)new ButtonAutomationPeer(passive).GetPattern(PatternInterface.Invoke)).Invoke();
        Require(command.Count==before+1 && Equals(command.Last,"passive"),"Interactive=false removed native button activation.");
        command.SetAvailable(false);
        await AwaitAsync(()=>!native.IsEnabled&&!passive.IsEnabled,"Attention native actions did not follow consumer command availability.");
        var unavailableCount=command.Count;
        var refused=false;
        try { ((IInvokeProvider)new ButtonAutomationPeer(native).GetPattern(PatternInterface.Invoke)).Invoke(); }
        catch(System.Runtime.InteropServices.COMException){refused=true;}
        catch(InvalidOperationException){refused=true;}
        Require(command.Count==unavailableCount,"An unavailable attention action executed its consumer command.");
        command.SetAvailable(true);
        await AwaitAsync(()=>native.IsEnabled&&passive.IsEnabled,"Attention native actions did not recover consumer command availability.");
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(InteractiveArticle).GetPattern(PatternInterface.Invoke) is null,
            "A visual interactive override invented non-button Invoke semantics.");
        var originalBackground = Styled.Background; var originalBorder = Styled.BorderBrush;
        Styled.Hue=12; Styled.Intensity=9; Styled.Density=BleeckerAttentionDensity.Compact;
        Require(ReferenceEquals(Styled.Background,originalBackground) && ReferenceEquals(Styled.BorderBrush,originalBorder) && Surface(Styled).Padding==new Thickness(7),
            "Attention recipe changes overwrote consumer style authority.");
        Styled.Hue=210; Styled.Intensity=5; Styled.Density=BleeckerAttentionDensity.Comfortable;
        var inherited = 0; Microsoft.UI.Xaml.Input.TappedEventHandler onTap=(_,_)=>inherited++;
        Slot.Tapped += onTap;
        try { await input.ClickAsync(ChildAction); Require(inherited==1,"Attention swallowed its consumer-owned child action."); }
        finally { Slot.Tapped -= onTap; }
        foreach(var specimen in new[]{Action,InteractiveArticle,DisabledAction})
        {
            await input.MoveAsync(Heading); await AwaitAsync(()=>Lift(specimen)==0,"Attention resting lift did not settle.");
            await input.MoveAsync(specimen);
            try { await AwaitAsync(()=>Math.Abs(Lift(specimen)+1)<.000001,$"Attention {specimen.Name} hover did not lift the actual surface."); }
            catch
            {
                var sourcePoint=input.DescribeCurrentPoint();
                var element=(FrameworkElement)VisualTreeHelper.GetChild(specimen,0);
                var type=typeof(BleeckerAttentionSurface);
                var ownerPoint=type.GetField("pointerOver",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(specimen);
                var rootPoint=element.TransformToVisual(XamlRoot.Content).TransformPoint(new Windows.Foundation.Point());
                await File.WriteAllTextAsync(Path.Combine(folder,$"attention-hover-failed-{scheme}.json"),JsonSerializer.Serialize(new{specimen.Name,ownerPoint,lift=Lift(specimen),rootPoint,element.ActualWidth,element.ActualHeight,sourcePoint}));
                throw;
            }
            Require(specimen!=DisabledAction || !NativeAction(specimen).IsEnabled,"Hover styling enabled a disabled native action.");
            await input.RequireForegroundAsync();
            await MainWindow.CaptureElementAsync(specimen,Path.Combine(folder,$"attention-hover-{specimen.Name}-{scheme}.png"));
        }
        await input.MoveAsync(PassiveAction); await AwaitAsync(()=>Lift(PassiveAction)==0,"Passive button gained an interactive hover lift.");
        Require(native.Focus(FocusState.Keyboard),"Attention native button refused keyboard focus.");
        await input.RequireForegroundAsync(); input.Key(0x20); await Task.Delay(50);
        Require(command.Count>before+1,"Attention physical Space did not activate its consumer command.");
        await input.MoveAsync(Heading);
        var parent=(Panel)VisualTreeHelper.GetParent(InteractiveArticle);var index=parent.Children.IndexOf(InteractiveArticle);
        for(var cycle=0;cycle<12;cycle++){parent.Children.Remove(InteractiveArticle);parent.Children.Insert(index,InteractiveArticle);}
        await Task.Delay(50);InteractiveArticle.UpdateLayout();
        Require(InteractiveArticle.IsLoaded&&InteractiveArticle.Background is ImageBrush {ImageSource:not null},"Rapid attention remove/re-add did not retain its loaded renderer.");
        var content=Slot.Content;var template=Slot.Template;Slot.Template=null;Slot.Template=template;Slot.ApplyTemplate();Slot.UpdateLayout();
        Require(ReferenceEquals(Slot.Content,content)&&ReferenceEquals(VisualTreeHelper.GetParent(ChildAction),((FrameworkElement)VisualTreeHelper.GetChild(Slot,0)).FindName("PART_Content")),
            "Attention template replacement changed or retained the old parent of consumer content.");
        await CaptureGradientPixelsAsync(input,source,scheme,folder);
        await File.WriteAllTextAsync(Path.Combine(folder,$"attention-actions-{scheme}.json"),JsonSerializer.Serialize(new{passed=true,command.Count,command.Last,inherited,unavailableInvokeRefused=refused}));
    }
    private async Task CaptureGradientPixelsAsync(NativeInputSession input,JsonElement source,BleeckerColorScheme scheme,string folder)
    {
        Require(XamlRoot.RasterizationScale==1,"The frozen Attention pixel reference requires display scale 1; scaled acceptance uses separate evidence.");
        var samples=new List<object>(); var captures=new List<object>(); var maximum=0;
        foreach(var reference in source.GetProperty("samples").EnumerateArray())
        {
            var name=reference.GetProperty("id").GetString()!; if(!name.StartsWith("gradient-",StringComparison.Ordinal))continue;
            var control=cases[name];await input.MoveAsync(control);await input.RequireForegroundAsync();
            var file=Path.Combine(folder,$"attention-{scheme}-{name}-window.png");
            await NativeScreenCapture.CaptureClientAsync(input.Window,file);
            using var stream=File.OpenRead(file);
            using var random=stream.AsRandomAccessStream();
            var decoder=await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(random);
            var pixels=(await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,Windows.Graphics.Imaging.BitmapAlphaMode.Ignore,
                new Windows.Graphics.Imaging.BitmapTransform(),Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation,Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
            var client=NativeScreenCapture.ClientBounds(input.Window);var bounds=input.ScreenBounds(control);
            captures.Add(new{name,file=Path.GetFileName(file),radius=Surface(control).CornerRadius.TopLeft,x=(int)Math.Round(bounds.X-client.X),y=(int)Math.Round(bounds.Y-client.Y),
                width=(int)Math.Round(bounds.Width),height=(int)Math.Round(bounds.Height)});
            foreach(var point in reference.GetProperty("pixels").EnumerateArray())
            {
                var x=(int)Math.Round(bounds.X-client.X)+point.GetProperty("x").GetInt32();
                var y=(int)Math.Round(bounds.Y-client.Y)+point.GetProperty("y").GetInt32();
                Require(x>=0&&y>=0&&x<decoder.PixelWidth&&y<decoder.PixelHeight,"Attention reference sample is outside the captured client.");
                var offset=checked((int)((y*decoder.PixelWidth+x)*4));
                var actual=new[]{(int)pixels[offset+2],(int)pixels[offset+1],(int)pixels[offset]};
                var expected=point.GetProperty("rgba").EnumerateArray().Take(3).Select(value=>value.GetInt32()).ToArray();
                var delta=actual.Select((value,index)=>Math.Abs(value-expected[index])).Max();maximum=Math.Max(maximum,delta);
                samples.Add(new{name,x=point.GetProperty("x").GetInt32(),y=point.GetProperty("y").GetInt32(),actual,expected,delta});
            }
        }
        // Retain observed differences without declaring them accepted. Full
        // border, radius, gradient and shadow raster parity remains open.
        await File.WriteAllTextAsync(Path.Combine(folder,$"attention-pixels-{scheme}.json"),JsonSerializer.Serialize(new{maximum,captures,samples},new JsonSerializerOptions{WriteIndented=true}));
        Require(maximum<=1,$"Attention interior source samples exceed the single-channel BGRA encoding precision bound: {maximum}.");
    }
    private static Button NativeAction(BleeckerAttentionSurface owner)=>(Button)((FrameworkElement)VisualTreeHelper.GetChild(owner,0)).FindName("PART_Action");
    private static Border Surface(BleeckerAttentionSurface owner)=>(Border)((FrameworkElement)VisualTreeHelper.GetChild(owner,0)).FindName("PART_Surface");
    private static double Lift(BleeckerAttentionSurface owner)=>((TranslateTransform)((FrameworkElement)VisualTreeHelper.GetChild(owner,0)).RenderTransform).Y;
    private static IEnumerable<AutomationPeer> PeerTree(AutomationPeer peer)
    {
        yield return peer;
        foreach(var child in peer.GetChildren()??[]) foreach(var descendant in PeerTree(child)) yield return descendant;
    }
    private static string SourceElement(BleeckerAttentionElement value)=>value switch{BleeckerAttentionElement.ListItem=>"li",_=>value.ToString().ToLowerInvariant()};
    private static async Task AwaitAsync(Func<bool> condition,string message)
    {
        var clock=System.Diagnostics.Stopwatch.StartNew();while(!condition()&&clock.Elapsed<TimeSpan.FromSeconds(2))await Task.Delay(16);
        Require(condition(),message);
    }
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private sealed class CounterCommand : System.Windows.Input.ICommand
    {
        internal int Count; internal object? Last;
        private bool available=true;
        internal void SetAvailable(bool value){available=value;CanExecuteChanged?.Invoke(this,EventArgs.Empty);}
        public bool CanExecute(object? parameter)=>available;
        public void Execute(object? parameter){Count++;Last=parameter;}
        public event EventHandler? CanExecuteChanged;
    }
}
