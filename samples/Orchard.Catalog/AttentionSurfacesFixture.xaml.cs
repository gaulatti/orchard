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
        await input.AcquireAsync(Heading); await MoveToHeadingAsync(input);
        await VerifyViewportReturnAsync(input, scheme, folder);
        using var stream = CatalogReferenceResources.Open(typeof(AttentionSurfacesFixture).Assembly, "Orchard.Catalog.Reference.AttentionLayout.json")!;
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
            Require(Math.Abs(surface.ActualWidth-control.ActualWidth)<.000001 && Math.Abs(surface.ActualHeight-control.ActualHeight)<.000001,
                $"Attention {name} painted surface is {surface.ActualWidth}x{surface.ActualHeight}, but the consumer chose {control.ActualWidth}x{control.ActualHeight}.");
            if(control.Element==BleeckerAttentionElement.Button)
                Require(Math.Abs(action.ActualWidth-control.ActualWidth)<.000001 && Math.Abs(action.ActualHeight-control.ActualHeight)<.000001,
                    $"Attention {name} native action does not fill the consumer's surface.");
            Require(!control.IsTabStop,"Attention surface invented a wrapper keyboard stop.");
            if(name!="override") Require(ReferenceEquals(control.Background,control.Resources["BleeckerAttentionBackgroundBrush"]) && control.Background is ImageBrush {ImageSource:not null},
                $"Attention {name} did not resolve its instance-owned gradient through the native style.");
            Require(!PeerTree(FrameworkElementAutomationPeer.CreatePeerForElement(control)).Any(peer=>peer.GetName()=="Mg"),"Attention exposed its private font metric probe.");
            measurements.Add(new{name,control.ActualWidth,control.ActualHeight,surfaceWidth=surface.ActualWidth,surfaceHeight=surface.ActualHeight,
                actionWidth=action.ActualWidth,actionHeight=action.ActualHeight,padding=surface.Padding.ToString(),surface.CornerRadius,text=text.Text,
                kind=FrameworkElementAutomationPeer.CreatePeerForElement(control).GetAutomationControlType().ToString(),background=control.Background?.GetType().Name});
        }
        await File.WriteAllTextAsync(Path.Combine(folder,$"attention-measurements-{scheme}.json"),JsonSerializer.Serialize(measurements,new JsonSerializerOptions{WriteIndented=true}));
        var native = NativeAction(Action); var before = command.Count;
        var sizing=new List<object>();var originalWidth=Action.Width;
        try
        {
            foreach(var width in new[]{416d,230d,originalWidth})
            {
                Action.Width=width;UpdateLayout();
                await AwaitAsync(()=>native.ActualWidth==Action.ActualWidth && Surface(Action).ActualWidth==Action.ActualWidth,
                    "Attention's native action or paint did not follow a live consumer width change.");
                sizing.Add(new{ownerWidth=Action.ActualWidth,actionWidth=native.ActualWidth,surfaceWidth=Surface(Action).ActualWidth});
            }
        }
        finally{Action.Width=originalWidth;UpdateLayout();}
        await File.WriteAllTextAsync(Path.Combine(folder,$"attention-consumer-sizing-{scheme}.json"),JsonSerializer.Serialize(sizing));
        ((IInvokeProvider)new ButtonAutomationPeer(native).GetPattern(PatternInterface.Invoke)).Invoke();
        Require(command.Count==before+1 && Equals(command.Last,"consumer"),"Attention native action did not route the consumer command and parameter.");
        before=command.Count;
        await input.MoveAsync(Action,localPoint:new Windows.Foundation.Point(Action.ActualWidth-8,Action.ActualHeight/2));
        await input.PressPointerAsync();
        try{await Task.Delay(30);}finally{input.PointerButton(false);}
        await AwaitAsync(()=>command.Count==before+1,"Clicking the far edge of the consumer-sized attention surface did not activate its native action.");
        Require(Equals(command.Last,"consumer"),"The far-edge attention click changed the consumer's command parameter.");
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
            await MoveToHeadingAsync(input); await AwaitAsync(()=>Lift(specimen)==0,"Attention resting lift did not settle.");
            await input.MoveAsync(specimen);
            if (!new Windows.UI.ViewManagement.UISettings().AnimationsEnabled)
            {
                await Task.Delay(16);
                Require(Math.Abs(Lift(specimen)+1)<.000001,
                    $"Attention {specimen.Name} hover did not reach its reduced-motion endpoint.");
            }
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
        await MoveToHeadingAsync(input);
        var parent=(Panel)VisualTreeHelper.GetParent(InteractiveArticle);var index=parent.Children.IndexOf(InteractiveArticle);
        for(var cycle=0;cycle<12;cycle++){parent.Children.Remove(InteractiveArticle);parent.Children.Insert(index,InteractiveArticle);}
        await Task.Delay(50);InteractiveArticle.UpdateLayout();
        Require(InteractiveArticle.IsLoaded&&InteractiveArticle.Background is ImageBrush {ImageSource:not null},"Rapid attention remove/re-add did not retain its loaded renderer.");
        var content=Slot.Content;var template=Slot.Template;Slot.Template=null;Slot.Template=template;Slot.ApplyTemplate();Slot.UpdateLayout();
        Require(ReferenceEquals(Slot.Content,content)&&ReferenceEquals(VisualTreeHelper.GetParent(ChildAction),((FrameworkElement)VisualTreeHelper.GetChild(Slot,0)).FindName("PART_Content")),
            "Attention template replacement changed or retained the old parent of consumer content.");
        await File.WriteAllTextAsync(Path.Combine(folder,$"attention-actions-{scheme}.json"),JsonSerializer.Serialize(new{passed=true,command.Count,command.Last,inherited,unavailableInvokeRefused=refused}));
    }
    private async Task MoveToHeadingAsync(NativeInputSession input)
    {
        var viewport = CatalogViewport();
        // The heading is the fixture's first row in the catalog content.
        // Set that authoritative viewport explicitly before returning the pointer.
        viewport.ChangeView(null, 0, null, disableAnimation: true);
        await AwaitAsync(() =>
        {
            var origin = Heading.TransformToVisual(viewport).TransformPoint(new Windows.Foundation.Point());
            return viewport.VerticalOffset == 0 && origin.Y >= 0 && origin.Y + Heading.ActualHeight <= viewport.ViewportHeight;
        }, "Attention heading geometry did not reach the requested catalog viewport.");
        await input.MoveAsync(Heading);
    }
    private ScrollViewer CatalogViewport()
    {
        for (DependencyObject? parent = VisualTreeHelper.GetParent(this); parent is not null; parent = VisualTreeHelper.GetParent(parent))
            if (parent is ScrollViewer { Name: "CatalogScroll" } viewport) return viewport;
        throw new InvalidOperationException("Attention fixture requires the registered catalog viewport.");
    }
    private async Task VerifyViewportReturnAsync(NativeInputSession input, BleeckerColorScheme scheme, string folder)
    {
        var viewport = CatalogViewport(); var samples = new List<object>();
        foreach (var keyboard in new[] { false, true })
        {
            await input.ClickAsync(ChildAction);
            if (keyboard) Require(ChildAction.Focus(FocusState.Keyboard), "Attention viewport regression could not focus the distant child action.");
            var before = viewport.VerticalOffset; var beforeBounds = input.ScreenBounds(Heading);
            Require(before > 0 && beforeBounds.Y < input.ScreenBounds(ChildAction).Y, "Attention viewport regression did not traverse the distant child.");
            await MoveToHeadingAsync(input);
            Require(viewport.VerticalOffset == 0 && input.OwnsCurrentPoint, "Attention heading did not return to the actual owned viewport.");
            samples.Add(new { keyboard, before, beforeBounds, after = viewport.VerticalOffset, afterBounds = input.ScreenBounds(Heading), input.OwnsCurrentPoint });
        }
        await File.WriteAllTextAsync(Path.Combine(folder, $"attention-viewport-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, samples }));
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
