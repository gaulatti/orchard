using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard.Catalog;
public sealed partial class MainWindow
{
    private async Task RunAccordionContinuityAsync(string folder)
    {
        var enabled=new UISettings().AnimationsEnabled;var original=Content;var canvas=new Canvas();var lifecycle=new List<object>();
        try
        {
            Content=canvas;await AwaitPresentationAsync(canvas);using var input=new NativeInputSession(canvas.XamlRoot,folder,"accordion-continuity");await input.AcquireAsync(canvas);
            var outside=new Border{Width=80,Height=24,Background=new SolidColorBrush(Microsoft.UI.Colors.Transparent)};Canvas.SetLeft(outside,20);Canvas.SetTop(outside,606);
            BleeckerAccordion NewGroup(bool last=false,bool expanded=false){var group=new BleeckerAccordion{Width=280,Items=AccordionsFixture.Items(),DefaultExpandedId=expanded?"first":null};if(last)group.Items.Move(0,1);Canvas.SetLeft(group,20);Canvas.SetTop(group,20);return group;}
            Button Target(BleeckerAccordion group)=>group.HeaderButtons[group.Items.ToList().FindIndex(item=>item.Id=="first")];
            BleeckerAccordion.MotionFrame Read(BleeckerAccordion group){group.UpdateLayout();return group.MotionFrames.Single(row=>row.Key=="first");}
            IExpandCollapseProvider Expand(Button button)=>(IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.ExpandCollapse);
            async Task Wait(Func<bool> condition,string message){var timer=Stopwatch.StartNew();var readiness=new NativeReadiness(DispatcherQueue);while(!condition()&&timer.ElapsedMilliseconds<2000)await readiness.WaitAsync();Require(condition(),message);}
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                canvas.RequestedTheme=theme;canvas.Background=new SolidColorBrush(theme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));
                foreach(var kind in new[]{"move-first-to-last","move-last-to-first","same-key-replacement","replace-collection","content-grow","default-change","remove-reinsert","group-remount"})
                {
                    canvas.Children.Clear();var group=NewGroup(kind=="move-last-to-first");canvas.Children.Add(group);canvas.Children.Add(outside);await AwaitPresentationAsync(group);var button=Target(group);await input.MoveAsync(button);await Wait(()=>Read(group).Hovered,"Accordion continuity pointer did not enter.");await input.MoveAsync(outside);await Wait(()=>!Read(group).Hovered&&!Read(group).Animating,"Accordion continuity baseline did not settle.");await AwaitPresentationAsync(group);Require(!Read(group).Hovered&&!Read(group).Animating,"Accordion continuity baseline changed after presentation.");Expand(button).Expand();
                    if(enabled)await Wait(()=>Read(group).PaintedAt-Read(group).ExpansionStart>=47.5&&Read(group).Animating,"Accordion continuity missed its intermediate paint.");else await AwaitPresentationAsync(group);
                    var before=Read(group);var loads=0;var unloads=0;button.Loaded+=(_,_)=>loads++;button.Unloaded+=(_,_)=>unloads++;var oldPeer=Expand(button);var staleRejected=false;
                    if(kind.StartsWith("move-",StringComparison.Ordinal))group.Items.Move(0,1);
                    else if(kind=="same-key-replacement"){var item=group.Items[0];group.Items[0]=new(item.Id,"Updated schedule",item.Content){Subtitle="Changed details"};}
                    else if(kind=="replace-collection")group.Items=new(group.Items.Select(item=>new BleeckerAccordionItem(item.Id,item.Title,item.Content){Subtitle=item.Subtitle,Actions=item.Actions}));
                    else if(kind=="content-grow")group.Items[0].Content=AccordionsFixture.Body(string.Join(" ",Enumerable.Repeat("A morning digest.",8)));
                    else if(kind=="default-change")group.DefaultExpandedId="second";
                    else if(kind=="remove-reinsert"){var item=group.Items[0];group.Items.RemoveAt(0);await AwaitPresentationAsync(group);try{oldPeer.Expand();}catch(Microsoft.UI.Xaml.Automation.ElementNotAvailableException){staleRejected=true;}group.Items.Insert(0,item);}
                    else if(kind=="group-remount"){canvas.Children.Remove(group);await AwaitPresentationAsync(canvas);group=NewGroup(expanded:true);canvas.Children.Add(group);}
                    var immediate=Read(group);var frames=new List<BleeckerAccordion.MotionFrame>();void Frame(object? sender,object args){var row=Read(group);if(frames.Count==0||frames[^1]!=row)frames.Add(row);}CompositionTarget.Rendering+=Frame;BleeckerAccordion.MotionFrame after;
                    try{await AwaitPresentationAsync(group);after=Read(group);await Wait(()=>!Read(group).Animating,"Accordion continuity did not settle.");await AwaitPresentationAsync(group);}
                    finally{CompositionTarget.Rendering-=Frame;}
                    var current=Target(group);var peer=FrameworkElementAutomationPeer.CreatePeerForElement(current);var itemNow=group.Items.Single(item=>item.Id=="first");var body=(FrameworkElement)itemNow.Content!;var title=Desc<TextBlock>(current).Single(text=>text.Visibility==Visibility.Visible&&text.Text==itemNow.Title as string).Text;var subtitle=Desc<TextBlock>(current).Single(text=>text.Visibility==Visibility.Visible&&text.Text==itemNow.Subtitle as string).Text;
                    lifecycle.Add(new{scheme=theme.ToString(),reduced=!enabled,kind,before,immediate,after,frames,final=Read(group),sameNode=ReferenceEquals(button,current),retained=ReferenceEquals(button,current)&&before.ExpansionStart==after.ExpansionStart&&before.ExpansionDuration==after.ExpansionDuration,loads,unloads,staleRejected,order=group.Items.Select(item=>item.Id).ToArray(),title,subtitle,content=Desc<TextBlock>(body).Single().Text,automationName=peer.GetName(),expanded=Expand(current).ExpandCollapseState==ExpandCollapseState.Expanded});
                }
                await AwaitPresentationAsync(canvas);await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,"catalog-"+theme.ToString().ToLowerInvariant()+".png"));
            }
            await File.WriteAllTextAsync(Path.Combine(folder,"accordion-continuity.json"),JsonSerializer.Serialize(new{passed=true,enabled,lifecycle},new JsonSerializerOptions{WriteIndented=true}));await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="accordion-continuity",environment=RuntimeEnvironment()}));
        }
        finally{Content=original;await AwaitPresentationAsync(CatalogRoot);}
    }
}
