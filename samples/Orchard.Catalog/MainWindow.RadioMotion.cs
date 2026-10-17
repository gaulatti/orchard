using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI.ViewManagement;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private sealed record RadioMotionFrame(double PaintedAt,double StrokeStart,double StrokeDuration,double DotStart,double DotDuration,BleeckerCssColor StrokeFrom,BleeckerCssColor StrokeTarget,double DotFrom,double DotTarget,string Border,double BorderOpacity,double Scale,string Dot,double DotOpacity,double Opacity,bool Animating);
    private async Task RunRadioMotionAsync(string folder)
    {
        using var resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("Orchard.Catalog.Reference.RadioMotion.json")??throw new InvalidOperationException("Missing radio motion reference.");using var source=await JsonDocument.ParseAsync(resource);
        var enabled=new UISettings().AnimationsEnabled;var original=Content;var canvas=new Canvas{Background=new SolidColorBrush(Microsoft.UI.Colors.White)};var reports=new List<object>();var availability=new List<object>();
        try
        {
            Content=canvas;await AwaitPresentationAsync(canvas);using var input=new NativeInputSession(canvas.XamlRoot,folder,"radio-motion");await input.AcquireAsync(canvas);
            var outside=new Border{Width=80,Height=24,Background=new SolidColorBrush(Microsoft.UI.Colors.Transparent)};Canvas.SetLeft(outside,20);Canvas.SetTop(outside,606);
            var group=new BleeckerRadioGroup{Width=280,Options=new(){new("on","Daily","A morning digest.")}};Canvas.SetLeft(group,20);Canvas.SetTop(group,20);canvas.Children.Add(group);canvas.Children.Add(outside);await AwaitPresentationAsync(group);await input.MoveAsync(group.OptionButtons[0]);await input.MoveAsync(outside);
            var button=(BleeckerRadioGroupOptionBase)group.OptionButtons[0];var ring=Desc<Ellipse>(button).Single(shape=>shape.Name=="RadioRing");var dot=Desc<Ellipse>(button).Single(shape=>shape.Name=="RadioDot");var optionRoot=Desc<Grid>(button).Single(panel=>panel.Name=="OptionRoot");
            RadioMotionFrame Read(){var border=(SolidColorBrush)ring.Stroke;var fill=(SolidColorBrush)dot.Fill;return new(button.PaintedAt,button.StrokeTiming.Start,button.StrokeTiming.Duration,button.DotTiming.Start,button.DotTiming.Duration,button.StrokeEndpoints.From,button.StrokeEndpoints.To,button.DotEndpoints.From,button.DotEndpoints.To,border.Color.ToString(),border.Opacity,((ScaleTransform)dot.RenderTransform).ScaleX,fill.Color.ToString(),fill.Opacity,optionRoot.Opacity,button.IsAnimating);}
            async Task Wait(Func<bool> condition,string message){var watch=Stopwatch.StartNew();var readiness=new NativeReadiness(DispatcherQueue);while(!condition()&&watch.ElapsedMilliseconds<2000)await readiness.WaitAsync();Require(condition(),message);}
            async Task State(string state){group.Value=state=="on"?"on":null;group.IsEnabled=!state.StartsWith("disabled",StringComparison.Ordinal);if(state.Contains("hover",StringComparison.Ordinal))await input.MoveAsync(button);else await input.MoveAsync(outside);}
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                foreach(var spec in source.RootElement.GetProperty("transitions").EnumerateArray().Where(row=>row.GetProperty("scheme").GetString()==theme.ToString()&&row.GetProperty("reduced").GetBoolean()!=enabled))
                {
                    canvas.RequestedTheme=theme;canvas.Background=new SolidColorBrush(theme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));var kind=spec.GetProperty("kind").GetString()!;var from=spec.GetProperty("from").GetString()!;var to=spec.GetProperty("to").GetString()!;
                    await State(from);await Wait(()=>!button.IsAnimating,"Radio initial motion did not settle.");await AwaitPresentationAsync(group);var before=Read();var frames=new List<RadioMotionFrame>();void Frame(object? sender,object args){var frame=Read();if(frames.Count==0||frames[^1]!=frame)frames.Add(frame);}
                    CompositionTarget.Rendering+=Frame;RadioMotionFrame immediate;
                    try{if(kind=="theme")canvas.RequestedTheme=theme==ElementTheme.Light?ElementTheme.Dark:ElementTheme.Light;else await State(to);immediate=Read();await AwaitPresentationAsync(group);await Wait(()=>!button.IsAnimating,"Radio transition exceeded its settling deadline.");await AwaitPresentationAsync(group);}
                    finally{CompositionTarget.Rendering-=Frame;}
                    reports.Add(new{kind,scheme=theme.ToString(),reduced=!enabled,from,to,before,immediate,frames,after=Read()});
                }
                if(enabled)
                {
                    canvas.RequestedTheme=theme;await State("off");await Wait(()=>!button.IsAnimating,"Radio availability baseline did not settle.");group.Value="on";await Wait(()=>button.PaintedAt-button.DotTiming.Start>=47.5,"Radio availability test missed intermediate frames.");var before=Read();group.IsEnabled=false;var after=Read();var retained=before.DotStart==after.DotStart&&before.DotDuration==after.DotDuration&&before.StrokeStart==after.StrokeStart&&before.StrokeDuration==after.StrokeDuration&&after.Animating;availability.Add(new{scheme=theme.ToString(),before,after,retained});await Wait(()=>!button.IsAnimating,"Disabled radio transition did not settle.");
                }
                canvas.RequestedTheme=theme;canvas.Background=new SolidColorBrush(theme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));await Wait(()=>!button.IsAnimating,"Radio final theme did not settle.");await AwaitPresentationAsync(group);await NativeScreenCapture.CaptureClientAsync(input.Window,System.IO.Path.Combine(folder,"catalog-"+theme.ToString().ToLowerInvariant()+".png"));
            }
            await File.WriteAllTextAsync(System.IO.Path.Combine(folder,"radio-motion.json"),JsonSerializer.Serialize(new{passed=true,enabled,reports,availability},new JsonSerializerOptions{WriteIndented=true}));await File.WriteAllTextAsync(System.IO.Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="radio-motion",environment=RuntimeEnvironment()}));
        }
        finally{Content=original;await AwaitPresentationAsync(CatalogRoot);}
    }
}
