using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Orchard;
using Windows.Foundation;
namespace PackageConsumer;

public partial class App
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint hwnd,out uint process);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(nint hwnd);

    private async void RunHiddenProgressStacksAsync(string referenceDirectory)
    {
        var arguments=Environment.GetCommandLineArgs();var outputIndex=Array.IndexOf(arguments,"--verify-output");
        if(outputIndex<0||outputIndex+1>=arguments.Length){Environment.Exit(2);return;}
        var output=arguments[outputIndex+1];
        var before=GetForegroundWindow();var handle=nint.Zero;var frames=new List<object>();var references=new List<object>();var fontTransitions=new List<object>();var collectionUpdates=new List<object>();var paintFrames=new List<object>();
        var completed=false;string? failure=null;var guardChecks=0;var activationEvents=0;
        void Guard()
        {
            guardChecks++;GetWindowThreadProcessId(GetForegroundWindow(),out var owner);
            if(window!.Visible||window.AppWindow.IsVisible||IsWindowVisible(handle)||owner==Environment.ProcessId)throw new InvalidOperationException("Hidden layout verification became visible or owned foreground input.");
        }
        try
        {
            // No MainWindow, Activate, Show, focus operation, input sender or
            // desktop-setting mutation participates in this diagnostic.
            window=new Window();handle=WinRT.Interop.WindowNative.GetWindowHandle(window);Guard();
            window.Activated+=(_,args)=>{if(args.WindowActivationState!=WindowActivationState.Deactivated){activationEvents++;Report(false,"Hidden layout window was activated.");}};
            window.VisibilityChanged+=(_,args)=>{if(args.Visible)Report(false,"Hidden layout window became visible.");};
            var root=new Canvas{Width=640,Height=400,UseLayoutRounding=false};window.Content=root;Guard();
            object Box(FrameworkElement element){var rect=element.TransformToVisual(root).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));return new{x=rect.X,y=rect.Y,width=rect.Width,height=rect.Height};}
            static object Paint(Brush? brush)=>brush is SolidColorBrush color?new{r=color.Color.R,g=color.Color.G,b=color.Color.B,a=color.Color.A,opacity=color.Opacity}:new{r=(byte)0,g=(byte)0,b=(byte)0,a=(byte)0,opacity=1d};
            object Text(FrameworkElement element){var text=HiddenDesc<TextBlock>(element).Single();return new{box=Box(element),text=text.Text.Replace('\n',' '),renderedText=text.Text,color=Paint(text.Foreground),weight=text.FontWeight.Weight,fontSize=text.FontSize,lineHeight=text.LineHeight};}
            object Snapshot(BleeckerProgressStack control)
            {
                FrameworkElement Part(string part)=>HiddenDesc<FrameworkElement>(control).Single(element=>element.Name==part);
                var bar=(Border)Part("ProgressStackTrack");var peer=FrameworkElementAutomationPeer.CreatePeerForElement(control);
                return new
                {
                    root=new{box=Box(control)},bar=new{box=Box(bar),background=Paint(bar.Background)},
                    segments=HiddenDesc<Border>(control).Where(part=>part.Name=="ProgressStackSegment").Select(part=>new{box=Box(part),background=Paint(part.Background),title=ToolTipService.GetToolTip(part)?.ToString()}).ToArray(),
                    legend=control.ShowLegend?new{box=Box(Part("ProgressStackLegend"))}:null,
                    items=control.ShowLegend?HiddenDesc<Panel>(control).Where(part=>part.Name=="ProgressStackItem").Select(part=>new{box=Box(part),swatch=new{box=Box(HiddenDesc<Border>(part).Single()),background=Paint(HiddenDesc<Border>(part).Single().Background)},label=Text(HiddenDesc<FrameworkElement>(part).Single(child=>child.Name=="ProgressStackLabel")),value=Text(HiddenDesc<FrameworkElement>(part).Single(child=>child.Name=="ProgressStackValue"))}).ToArray():[],
                    control.IsTabStop,name=peer.GetName(),hasInvoke=peer.GetPattern(PatternInterface.Invoke)is not null,hasRange=peer.GetPattern(PatternInterface.RangeValue)is not null
                };
            }
            async Task LayoutAsync(FrameworkElement control)
            {
                for(var i=0;i<3;i++){root.Measure(new Size(640,400));root.Arrange(new Rect(0,0,640,400));root.UpdateLayout();if(control is Control templated)templated.ApplyTemplate();Guard();await Task.Delay(10);}
                if(!control.IsLoaded)throw new InvalidOperationException("Hidden control did not enter the real XAML tree.");
            }
            foreach(var name in new[]{"ProgressStack.json","ProgressStackText.json"})
            {
                var bytes=await File.ReadAllBytesAsync(Path.Combine(referenceDirectory,name));references.Add(new{name,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});using var document=JsonDocument.Parse(bytes);
                if(document.RootElement.GetProperty("sourceSha256").GetString()!="101df66952162d978a45935604e468e7b465e8a57afdf873a48e0867a46b6254")throw new InvalidOperationException("Unexpected ProgressStack source reference.");
                foreach(var row in document.RootElement.GetProperty("cases").EnumerateArray())
                {
                    var theme=Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!);root.RequestedTheme=theme;root.Background=new SolidColorBrush(theme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));
                    var spec=row.GetProperty("spec");var control=new BleeckerProgressStack{Width=spec.TryGetProperty("width",out var width)?width.GetDouble():280,UseLayoutRounding=false};
                    foreach(var item in spec.GetProperty("items").EnumerateArray())control.Items.Add(new(){Label=item.GetProperty("label").GetString()!,Value=double.Parse(item.GetProperty("value").GetString()!,CultureInfo.InvariantCulture),Color=HiddenColor(item)});
                    if(spec.TryGetProperty("max",out var maximum))control.Max=double.Parse(maximum.GetString()!,CultureInfo.InvariantCulture);
                    if(spec.TryGetProperty("showLegend",out var show))control.ShowLegend=show.GetBoolean();
                    if(spec.TryGetProperty("rtl",out _))control.FlowDirection=FlowDirection.RightToLeft;
                    if(spec.TryGetProperty("custom",out _)){control.TrackHeight=20;control.CornerRadius=new(3);control.Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,245,237,220));}
                    root.Children.Clear();Canvas.SetLeft(control,20);Canvas.SetTop(control,20);root.Children.Add(control);await LayoutAsync(control);
                    var measurement=Snapshot(control);
                    var capture=await CaptureHiddenProgressStackAsync(root,Path.GetDirectoryName(output)!,row.GetProperty("file").GetString()!,Guard);
                    frames.Add(new{reference=name,id=row.GetProperty("id").GetString(),scheme=theme.ToString(),motion=row.GetProperty("motion").GetString(),labels=control.Items.Select(item=>item.Label).ToArray(),measurement,capture});Guard();
                }
            }
            if(frames.Count!=212)throw new InvalidOperationException("Incomplete hidden ProgressStack matrix.");
            collectionUpdates.AddRange(await VerifyHiddenProgressStackUpdatesAsync(root,referenceDirectory,LayoutAsync,Snapshot,Guard));
            paintFrames.AddRange(await CaptureHiddenProgressStackPaintAsync(root,referenceDirectory,Path.GetDirectoryName(output)!,LayoutAsync,Guard));
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})foreach(var kind in new[]{"progress-stack","empty","icon-badge"})
            {
                const string content="日本語 العربية";root.RequestedTheme=theme;
                Control styled=kind switch{"progress-stack"=>new BleeckerProgressStack{Width=400,Items=new(){new(){Label=content,Value=6}}},"empty"=>new BleeckerEmpty{Width=400,Title=content},"icon-badge"=>new BleeckerIconBadge{Content=content},_=>throw new InvalidOperationException()};
                root.Children.Clear();root.Children.Add(styled);await LayoutAsync(styled);var originalFamily=styled.FontFamily;
                TextBlock Caption()=>HiddenDesc<TextBlock>(styled).Single(text=>text.Text.Replace('\n',' ')==content);
                object FontState(string phase){var caption=Caption();return new{kind,scheme=theme.ToString(),phase,ownerFamily=styled.FontFamily.Source,textFamily=caption.FontFamily.Source,Text=caption.Text.Replace('\n',' '),renderedText=caption.Text,caption.FontSize,caption.LineHeight,explicitRunFamilies=caption.Inlines.OfType<Run>().Where(run=>run.ReadLocalValue(TextElement.FontFamilyProperty)!=DependencyProperty.UnsetValue).Select(run=>run.FontFamily.Source).ToArray()};}
                fontTransitions.Add(FontState("original"));styled.FontFamily=new FontFamily("Arial");await LayoutAsync(styled);fontTransitions.Add(FontState("custom"));
                if(Caption().Inlines.OfType<Run>().Any(run=>run.ReadLocalValue(TextElement.FontFamilyProperty)!=DependencyProperty.UnsetValue))throw new InvalidOperationException("Custom font retained Orchard's script-specific face overrides: "+kind);
                styled.FontFamily=originalFamily;await LayoutAsync(styled);fontTransitions.Add(FontState("restored"));
                if(!Caption().Inlines.OfType<Run>().Any(run=>run.ReadLocalValue(TextElement.FontFamilyProperty)!=DependencyProperty.UnsetValue))throw new InvalidOperationException("Restored Orchard font lost script-specific faces: "+kind);
            }
            completed=true;
        }
        catch(Exception error){failure=error.ToString();}
        finally
        {
            var after=GetForegroundWindow();var visible=window?.Visible??false;var nativeVisible=handle!=nint.Zero&&IsWindowVisible(handle);
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{passed=completed,scope="hidden-progress-stack-layout-and-raster",hiddenWindow=true,activateCalled=false,inputSent=false,visible,nativeVisible,activationEvents,guardChecks,foregroundBefore=(long)before,foregroundAfter=(long)after,displayScale=handle==nint.Zero?0:GetDpiForWindow(handle)/96d,textScale=new Windows.UI.ViewManagement.UISettings().TextScaleFactor,highContrast=new Windows.UI.ViewManagement.AccessibilitySettings().HighContrast,processArchitecture=RuntimeInformation.ProcessArchitecture.ToString(),osArchitecture=RuntimeInformation.OSArchitecture.ToString(),references,frames,fontTransitions,collectionUpdates,paintFrames,failure,nativeComponentAccepted=false,visibleRuntimeVerified=false},new JsonSerializerOptions{WriteIndented=true}));
            window?.Close();Environment.Exit(completed?0:1);
        }
    }
    private static Brush? HiddenColor(JsonElement item)
    {
        if(!item.TryGetProperty("color",out var field))return null;var value=field.GetString()!;
        if(value.StartsWith('#'))return new SolidColorBrush(Windows.UI.Color.FromArgb(255,byte.Parse(value[1..3],NumberStyles.HexNumber),byte.Parse(value[3..5],NumberStyles.HexNumber),byte.Parse(value[5..7],NumberStyles.HexNumber)));
        var parts=Regex.Matches(value,"[0-9.]+").Select(match=>double.Parse(match.Value,CultureInfo.InvariantCulture)).ToArray();if(parts.Length!=4)throw new InvalidOperationException("Unknown source brush.");return new SolidColorBrush(Windows.UI.Color.FromArgb(255,(byte)parts[0],(byte)parts[1],(byte)parts[2])){Opacity=parts[3]};
    }
    private static IEnumerable<T> HiddenDesc<T>(DependencyObject root)where T:DependencyObject
    {
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T match)yield return match;foreach(var nested in HiddenDesc<T>(child))yield return nested;}
    }
}
