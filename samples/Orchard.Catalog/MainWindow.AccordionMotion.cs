using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard.Catalog;
public sealed partial class MainWindow
{
    private async Task RunAccordionMotionAsync(string folder)
    {
        using var resource=typeof(MainWindow).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.AccordionMotion.json")??throw new InvalidOperationException("Accordion motion reference missing.");using var source=await JsonDocument.ParseAsync(resource);
        var original=Content;var canvas=new Canvas();var enabled=new UISettings().AnimationsEnabled;var reports=new List<object>();var reversals=new List<object>();
        try
        {
            Content=canvas;await AwaitPresentationAsync(canvas);using var input=new NativeInputSession(canvas.XamlRoot,folder,"accordion-motion");await input.AcquireAsync(canvas);var outside=new Border{Width=80,Height=24,Background=new SolidColorBrush(Microsoft.UI.Colors.Transparent)};Canvas.SetLeft(outside,20);Canvas.SetTop(outside,606);
            BleeckerAccordion? group=null;
            BleeckerAccordion.MotionFrame[] Read(){group!.UpdateLayout();return group.MotionFrames.ToArray();}
            async Task Wait(Func<bool> condition,string message){var watch=Stopwatch.StartNew();var readiness=new NativeReadiness(DispatcherQueue);while(!condition()&&watch.ElapsedMilliseconds<2000)await readiness.WaitAsync();Require(condition(),message);}
            async Task Hover(string value){if(value=="none")await input.MoveAsync(outside);else await input.MoveAsync(group!.HeaderButtons[value=="first"?0:1]);}
            void Change(string value){var index=value=="second"?1:0;var header=group!.HeaderButtons[index];var peer=(IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(header).GetPattern(PatternInterface.ExpandCollapse);if(value=="none")((IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(group.HeaderButtons[group.ExpandedId=="second"?1:0]).GetPattern(PatternInterface.ExpandCollapse)).Collapse();else peer.Expand();}
            async Task Baseline(ElementTheme theme,string value="none",string hover="none"){
                canvas.Children.Clear();canvas.RequestedTheme=theme;canvas.Background=new SolidColorBrush(theme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));group=new BleeckerAccordion{Width=280,Items=AccordionsFixture.Items(),DefaultExpandedId=value=="none"?null:value};Canvas.SetLeft(group,20);Canvas.SetTop(group,20);canvas.Children.Add(group);canvas.Children.Add(outside);await AwaitPresentationAsync(group);await input.MoveAsync(group.HeaderButtons[0]);await Wait(()=>Read()[0].Hovered,"Accordion baseline pointer did not enter the header.");await Hover(hover);await Wait(()=>Read().All(row=>row.Hovered==(row.Key==hover)&&!row.Animating),"Accordion motion baseline did not settle.");await AwaitPresentationAsync(group);Require(Read().All(row=>row.Hovered==(row.Key==hover)&&!row.Animating),"Accordion baseline changed after presentation.");
            }
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                foreach(var spec in source.RootElement.GetProperty("transitions").EnumerateArray().Where(row=>row.GetProperty("scheme").GetString()==theme.ToString()&&row.GetProperty("reduced").GetBoolean()!=enabled)){
                    var kind=spec.GetProperty("kind").GetString()!;var from=spec.GetProperty("from").GetString()!;var to=spec.GetProperty("to").GetString()!;await Baseline(theme,kind=="expansion"?from:"none",kind=="expansion"?"none":from);var before=Read();var frames=new List<BleeckerAccordion.MotionFrame[]>();void Frame(object? sender,object args){var sample=Read();if(frames.Count==0||!frames[^1].SequenceEqual(sample))frames.Add(sample);}CompositionTarget.Rendering+=Frame;BleeckerAccordion.MotionFrame[] immediate;
                    try{if(kind=="expansion")Change(to);else if(kind=="hover")await Hover(to);else canvas.RequestedTheme=theme==ElementTheme.Light?ElementTheme.Dark:ElementTheme.Light;immediate=Read();await AwaitPresentationAsync(group!);await Wait(()=>Read().All(row=>!row.Animating),"Accordion motion did not finish.");await AwaitPresentationAsync(group!);}
                    finally{CompositionTarget.Rendering-=Frame;}
                    reports.Add(new{scheme=theme.ToString(),reduced=!enabled,kind,from,to,before,immediate,frames,after=Read()});
                }
                if(enabled){
                    await Baseline(theme);var before=Read();var steps=new List<object>();Change("first");
                    foreach(var(target,elapsed)in new[]{("none",47.5),("first",50d),("none",25d)}){
                        var start=Read()[0].ExpansionStart;await Wait(()=>Read()[0].PaintedAt-start>=elapsed&&Read()[0].Animating,"Accordion reversal missed intermediate paint.");var during=Read();Change(target);var immediate=Read();var frames=new List<BleeckerAccordion.MotionFrame[]>();void Frame(object? sender,object args){var sample=Read();if(frames.Count==0||!frames[^1].SequenceEqual(sample))frames.Add(sample);}CompositionTarget.Rendering+=Frame;
                        try{var at=immediate[0].ExpansionStart;var next=steps.Count==0?50d:steps.Count==1?25d:220d;await Wait(()=>Read()[0].PaintedAt-at>=next||!Read()[0].Animating,"Accordion reversal frames did not advance.");}
                        finally{CompositionTarget.Rendering-=Frame;}
                        steps.Add(new{target,elapsed,during,immediate,frames,after=Read()});
                    }
                    await Wait(()=>Read().All(row=>!row.Animating),"Accordion reversal did not settle.");reversals.Add(new{scheme=theme.ToString(),before,steps,after=Read()});
                }
                await AwaitPresentationAsync(canvas);await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,"catalog-"+theme.ToString().ToLowerInvariant()+".png"));
            }
            await File.WriteAllTextAsync(Path.Combine(folder,"accordion-motion.json"),JsonSerializer.Serialize(new{passed=true,enabled,reports,reversals},new JsonSerializerOptions{WriteIndented=true}));await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="accordion-motion",environment=RuntimeEnvironment()}));
        }
        finally{Content=original;await AwaitPresentationAsync(CatalogRoot);}
    }
}
