using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI.ViewManagement;

namespace Orchard.Catalog;
public sealed partial class MainWindow
{
    private async Task RunRadioContinuityAsync(string folder)
    {
        var enabled=new UISettings().AnimationsEnabled;var original=Content;var canvas=new Canvas{Background=new SolidColorBrush(Microsoft.UI.Colors.White)};var lifecycle=new List<object>();var reversals=new List<object>();var requests=0;
        try
        {
            Content=canvas;await AwaitPresentationAsync(canvas);using var input=new NativeInputSession(canvas.XamlRoot,folder,"radio-continuity");await input.AcquireAsync(canvas);
            var outside=new Border{Width=80,Height=24,Background=new SolidColorBrush(Microsoft.UI.Colors.Transparent)};Canvas.SetLeft(outside,20);Canvas.SetTop(outside,606);
            async Task Wait(Func<bool> condition,string message){var timer=Stopwatch.StartNew();var readiness=new NativeReadiness(DispatcherQueue);while(!condition()&&timer.ElapsedMilliseconds<2000)await readiness.WaitAsync();Require(condition(),message);}
            BleeckerRadioGroup NewGroup(bool last=false){var owner=new BleeckerRadioGroup{Width=280,Options=new(){new("on","Daily","A morning digest."),new("other","Other")}};owner.ValueRequested+=(_,_)=>requests++;if(last)owner.Options.Move(0,1);Canvas.SetLeft(owner,20);Canvas.SetTop(owner,20);return owner;}
            BleeckerRadioGroupOptionBase Target(BleeckerRadioGroup owner)=>(BleeckerRadioGroupOptionBase)owner.OptionButtons[owner.Options.ToList().FindIndex(option=>option.Value=="on")];
            RadioMotionFrame Read(BleeckerRadioGroupOptionBase button){var ring=Desc<Ellipse>(button).Single(shape=>shape.Name=="RadioRing");var dot=Desc<Ellipse>(button).Single(shape=>shape.Name=="RadioDot");var root=Desc<Grid>(button).Single(panel=>panel.Name=="OptionRoot");var border=(SolidColorBrush)ring.Stroke;var fill=(SolidColorBrush)dot.Fill;return new(button.PaintedAt,button.StrokeTiming.Start,button.StrokeTiming.Duration,button.DotTiming.Start,button.DotTiming.Duration,button.StrokeEndpoints.From,button.StrokeEndpoints.To,button.DotEndpoints.From,button.DotEndpoints.To,border.Color.ToString(),border.Opacity,((ScaleTransform)dot.RenderTransform).ScaleX,fill.Color.ToString(),fill.Opacity,root.Opacity,button.IsAnimating);}
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                canvas.RequestedTheme=theme;canvas.Background=new SolidColorBrush(theme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));
                foreach(var kind in new[]{"move-first-to-last","move-last-to-first","same-key-replacement","replace-collection","orientation","remove-reinsert","group-remount"})
                {
                    canvas.Children.Clear();var group=NewGroup(kind=="move-last-to-first");canvas.Children.Add(group);canvas.Children.Add(outside);await AwaitPresentationAsync(group);var button=Target(group);await input.MoveAsync(button);await input.MoveAsync(outside);await Wait(()=>!button.IsAnimating,"Radio continuity pointer baseline did not settle.");group.Value="on";
                    if(enabled)await Wait(()=>button.PaintedAt-button.DotTiming.Start>=47.5&&button.IsAnimating,"Radio continuity missed its initial intermediate paint.");else await AwaitPresentationAsync(group);
                    var before=Read(button);var loads=0;var unloads=0;button.Loaded+=(_,_)=>loads++;button.Unloaded+=(_,_)=>unloads++;
                    if(kind.StartsWith("move-",StringComparison.Ordinal))group.Options.Move(0,1);
                    else if(kind=="same-key-replacement")group.Options[0]=new("on","Updated daily","Changed help");
                    else if(kind=="replace-collection")group.Options=new(group.Options.Select(option=>new BleeckerRadioOption(option.Value,option.Label,option.Description,option.Disabled)));
                    else if(kind=="orientation")group.Orientation=Orientation.Horizontal;
                    else if(kind=="remove-reinsert"){var option=group.Options[0];group.Options.RemoveAt(0);await AwaitPresentationAsync(group);group.Options.Insert(0,option);}
                    else if(kind=="group-remount"){canvas.Children.Remove(group);await AwaitPresentationAsync(canvas);group=NewGroup();group.Value="on";canvas.Children.Add(group);}
                    await AwaitPresentationAsync(group);var current=Target(group);var after=Read(current);var retained=ReferenceEquals(current,button)&&before.StrokeStart==after.StrokeStart&&before.StrokeDuration==after.StrokeDuration&&before.DotStart==after.DotStart&&before.DotDuration==after.DotDuration;
                    var peer=FrameworkElementAutomationPeer.CreatePeerForElement(current);var selected=((ISelectionItemProvider)peer.GetPattern(PatternInterface.SelectionItem)).IsSelected;
                    lifecycle.Add(new{scheme=theme.ToString(),reduced=!enabled,kind,before,after,sameNode=ReferenceEquals(button,current),retained,loads,unloads,order=group.Options.Select(option=>option.Value).ToArray(),label=Desc<TextBlock>(current).Single(text=>text.Name=="RadioLabel").Text,automationName=peer.GetName(),selected});await Wait(()=>!current.IsAnimating,"Radio continuity did not settle.");
                }
                if(enabled)
                {
                    canvas.Children.Clear();var group=NewGroup();canvas.Children.Add(group);canvas.Children.Add(outside);await AwaitPresentationAsync(group);var button=Target(group);await input.MoveAsync(button);await input.MoveAsync(outside);await Wait(()=>!button.IsAnimating,"Radio reversal pointer baseline did not settle.");var before=Read(button);var steps=new List<object>();group.Value="on";
                    foreach(var (value,elapsed)in new (string?,double)[]{(null,47.5),("on",50),(null,25)})
                    {
                        var start=button.DotTiming.Start;await Wait(()=>button.PaintedAt-start>=elapsed&&button.IsAnimating,"Radio reversal missed its live intermediate paint.");var during=Read(button);group.Value=value;var immediate=Read(button);var frames=new List<RadioMotionFrame>();void Frame(object? sender,object args){var sample=Read(button);if(frames.Count==0||frames[^1]!=sample)frames.Add(sample);}CompositionTarget.Rendering+=Frame;
                        try{var target=button.DotTiming.Start;var next=steps.Count==0?50d:steps.Count==1?25d:220d;await Wait(()=>button.PaintedAt-target>=next||!button.IsAnimating,"Radio reversed frames did not advance.");}
                        finally{CompositionTarget.Rendering-=Frame;}
                        steps.Add(new{value,elapsed,during,immediate,frames,after=Read(button)});
                    }
                    await Wait(()=>!button.IsAnimating,"Radio reversal sequence did not settle.");reversals.Add(new{scheme=theme.ToString(),before,steps,after=Read(button)});
                }
                await AwaitPresentationAsync(canvas);await NativeScreenCapture.CaptureClientAsync(input.Window,System.IO.Path.Combine(folder,"catalog-"+theme.ToString().ToLowerInvariant()+".png"));
            }
            await File.WriteAllTextAsync(System.IO.Path.Combine(folder,"radio-continuity.json"),JsonSerializer.Serialize(new{passed=true,enabled,lifecycle,reversals,requests},new JsonSerializerOptions{WriteIndented=true}));await File.WriteAllTextAsync(System.IO.Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="radio-continuity",environment=RuntimeEnvironment()}));
        }
        finally{Content=original;await AwaitPresentationAsync(CatalogRoot);}
    }
}
