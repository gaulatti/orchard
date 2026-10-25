using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Path=System.IO.Path;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunSearchInputStatesAsync(string folder)
    {
        using var stream=CatalogReferenceResources.Open(Assembly.GetExecutingAssembly(), "Orchard.Catalog.Reference.SearchInputStates.json")??throw new InvalidOperationException("Missing SearchInputStates reference.");using var source=await JsonDocument.ParseAsync(stream);
        var original=Content;var page=new Page{IsTabStop=true};var canvas=new Canvas{Name="SearchInputStateSurface"};page.Content=canvas;NativeInputSession? input=null;var reports=new List<object>();var paletteChecks=new List<object>();var paintGates=new List<object>();var phase="mount";var passed=false;
        try
        {
            Content=page;await AwaitPresentationAsync(page);input=new NativeInputSession(page.XamlRoot,folder,"search-input-states");await input.AcquireAsync(canvas);var width=(int)page.XamlRoot.Size.Width;Require(width is 320 or 640,"Search Input states require a 320 or 640 client.");
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                page.RequestedTheme=theme;canvas.Background=new SolidColorBrush(theme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));
                foreach(var reference in source.RootElement.GetProperty("cases").EnumerateArray().Where(item=>item.GetProperty("width").GetInt32()==width&&item.GetProperty("scheme").GetString()==theme.ToString()))
                {
                    phase=reference.GetProperty("id").GetString()!;Require(page.Focus(FocusState.Programmatic),"State fixture could not clear prior focus.");canvas.Children.Clear();var props=reference.GetProperty("props");var control=new BleeckerSearchInput{Width=width-40,Text="Culture",IsError=Flag(props,"error"),IsEditorEnabled=!Flag(props,"disabled"),IsReadOnly=Flag(props,"readOnly")};var actions=0;control.ClearRequested+=(_,_)=>actions++;AutomationProperties.SetName(control,"Search catalogue");Canvas.SetLeft(control,20);Canvas.SetTop(control,20);canvas.Children.Add(control);await input.MoveAsync(canvas,true,new Point(width-10,400));await AwaitPresentationAsync(page);
                    var clear=Children<Button>(control).Single();var editor=control.Editor;
                    if(phase.EndsWith("hover",StringComparison.Ordinal))await input.MoveAsync(phase.StartsWith("clear",StringComparison.Ordinal)?clear:editor);
                    else if(phase.EndsWith("focus",StringComparison.Ordinal)){Require(editor.Focus(FocusState.Keyboard),"State editor refused focus.");if(phase=="clear-focus")await TabToClearAsync();}
                    object? state=null;var hold=props.TryGetProperty("hold",out var held)?held.GetString():null;
                    if(hold is "pointer" or "edge")await input.HeldPointerAsync(clear,CaptureAsync,hold=="edge"?new Point(0,9):null);
                    else if(hold is not null){Require(editor.Focus(FocusState.Keyboard),"Held-key editor refused focus.");await TabToClearAsync();await input.HeldKeyAsync(hold=="Space"?(ushort)0x20:(ushort)0x0D,clear,()=>clear,CaptureAsync);}
                    else await CaptureAsync();
                    await AwaitPresentationAsync(page);Require(control.Text=="Culture","State activation changed consumer-owned text.");reports.Add(new{id=phase,width,scheme=theme.ToString(),state,released=new{clearActions=actions,value=control.Text}});await SaveAsync();
                    if(phase=="clear-hover")await CheckLivePaletteAsync();
                    async Task CheckLivePaletteAsync()
                    {
                        var part=Children<TextBlock>(control).Single(item=>item.Name=="PART_HoverBackground");var originalBrush=(SolidColorBrush)part.Foreground;var originalColor=originalBrush.Color;var originalOpacity=originalBrush.Opacity;var localForeground=part.ReadLocalValue(TextBlock.ForegroundProperty);var localOpacity=part.ReadLocalValue(UIElement.OpacityProperty);var defaultAlpha=theme==ElementTheme.Light?.1:.15;
                        var surface=((Grid)clear.Content).Children.OfType<Border>().Single(item=>item.Name=="InlineActionChrome");
                        try
                        {
                            originalBrush.Color=Microsoft.UI.Colors.Magenta;originalBrush.Opacity=.5;await CheckAsync(Microsoft.UI.Colors.Magenta,defaultAlpha*.5);
                            part.Foreground=new SolidColorBrush(Microsoft.UI.Colors.Lime){Opacity=.8};await CheckAsync(Microsoft.UI.Colors.Lime,defaultAlpha*.8);
                            part.Opacity=.25;await CheckAsync(Microsoft.UI.Colors.Lime,.2);
                        }
                        finally{originalBrush.Color=originalColor;originalBrush.Opacity=originalOpacity;Restore(part,TextBlock.ForegroundProperty,localForeground);Restore(part,UIElement.OpacityProperty,localOpacity);await AwaitPresentationAsync(page);}
                        Require(ReferenceEquals(part.Foreground,originalBrush),"Hover palette did not restore the original shared brush.");
                        var alternate=theme==ElementTheme.Light?ElementTheme.Dark:ElementTheme.Light;
                        try{page.RequestedTheme=alternate;await AwaitPresentationAsync(page);var expected=BleeckerTokens.Colors(alternate==ElementTheme.Dark?BleeckerColorScheme.Dark:BleeckerColorScheme.Light)["sand"];Require(ColorHex(((SolidColorBrush)part.Foreground).Color)==expected,"Hover palette did not follow its shared token across theme changes.");await CheckAsync(((SolidColorBrush)part.Foreground).Color,alternate==ElementTheme.Dark?.15:.1);}
                        finally{page.RequestedTheme=theme;await AwaitPresentationAsync(page);}
                        await CheckAsync(originalColor,defaultAlpha*originalOpacity);paletteChecks.Add(new{scheme=theme.ToString(),sourceMutation=true,brushReplacement=true,opacityMutation=true,sourceBrushRestored=true,themeRoundtrip=true});await SaveAsync();
                        async Task CheckAsync(Windows.UI.Color color,double alpha)=>await AwaitPaintAsync(()=>surface.Background is SolidColorBrush painted&&painted.Color.R==color.R&&painted.Color.G==color.G&&painted.Color.B==color.B&&Math.Abs(painted.Color.A/255d*painted.Opacity-alpha)<.000001,"palette");
                        static void Restore(DependencyObject owner,DependencyProperty property,object local){if(ReferenceEquals(local,DependencyProperty.UnsetValue))owner.ClearValue(property);else owner.SetValue(property,local);}
                    }
                    async Task TabToClearAsync()=>await input.HeldKeyAsync(0x09,editor,()=>clear,()=>Task.CompletedTask);
                    async Task CaptureAsync()
                    {
                        await AwaitPresentationAsync(page);var chrome=Children<Border>(editor).Single(item=>item.Name=="BorderElement");var layer=Children<Grid>(editor).Single(item=>item.Name=="Editor");var ring=Children<Border>(editor).Single(item=>item.Name=="InputRing");var clearChrome=((Grid)clear.Content).Children.OfType<Border>().Single(item=>item.Name=="InlineActionChrome");var focusCue=((Grid)clear.Content).Children.OfType<Border>().Single(item=>item.Name=="InlineActionFocus");var clearIcon=(Canvas)clearChrome.Child;var line=clearIcon.Children.OfType<Line>().First();
                        var target=Children<Border>(editor).Single(item=>item.Name=="InputPalette");var ringTarget=Children<Border>(editor).Single(item=>item.Name=="InputRingPalette");var hovered=phase is "clear-hover" or "clear-pointer-pressed" or "clear-edge-pressed";var foregroundTarget=Children<TextBlock>(control).Single(item=>item.Name==(hovered?"PART_Hover":"PART_Secondary"));var backgroundTarget=Children<TextBlock>(control).Single(item=>item.Name=="PART_HoverBackground");var pressed=reference.GetProperty("state").GetProperty("clear").GetProperty("paint").GetProperty("scale").GetString()=="0.92";var expectedActions=reference.GetProperty("state").GetProperty("clearActions").GetInt32();
                        await AwaitPaintAsync(()=>Same(chrome.Background,target.Background)&&Same(chrome.BorderBrush,target.BorderBrush)&&Math.Abs(layer.Opacity-target.Opacity)<.000001&&Math.Abs(ring.Opacity-ringTarget.Opacity)<.000001&&Same(line.Stroke,foregroundTarget.Foreground)&&Same(clearChrome.Background,backgroundTarget.Foreground,hovered?backgroundTarget.Opacity:0)&&clear.IsPressed==pressed&&actions==expectedActions,"capture");
                        state=new{editor=new{box=Box(editor),background=Paint(chrome.Background),border=Paint(chrome.BorderBrush),editorOpacity=layer.Opacity,focus=editor.FocusState.ToString(),ring=new{box=Box(ring),paint=Paint(ring.BorderBrush),ring.Opacity}},clear=new{box=Box(clear),paintedBox=Box(clearChrome),icon=Box(clearIcon),background=Paint(clearChrome.Background),foreground=Paint(line.Stroke),clear.IsPressed,clear.IsEnabled,focus=clear.FocusState.ToString(),clear.UseSystemFocusVisuals,focusCue=new{box=Box(focusCue),visible=focusCue.Visibility==Visibility.Visible,paint=Paint(focusCue.BorderBrush)},focusPrimary=Paint(clear.FocusVisualPrimaryBrush),focusSecondary=Paint(clear.FocusVisualSecondaryBrush)},clearActions=actions};
                        var frame=Path.Combine(folder,reference.GetProperty("file").GetString()!);await NativeScreenCapture.CaptureClientAsync(input.Window,frame);if(phase=="clear-hover")File.Copy(frame,Path.Combine(folder,$"catalog-{theme.ToString().ToLowerInvariant()}.png"),true);await SaveAsync();
                    }
                }
            }
            passed=true;
        }
        finally{input?.Dispose();Content=original;await AwaitPresentationAsync(CatalogRoot);await SaveAsync();}
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="search-input-states",environment=RuntimeEnvironment()}));
        static bool Flag(JsonElement value,string key)=>value.TryGetProperty(key,out var flag)&&flag.GetBoolean();
        object Box(FrameworkElement element){var b=element.TransformToVisual(page).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));return new{x=b.X,y=b.Y,width=b.Width,height=b.Height};}
        static object? Paint(Brush? brush){if(brush is null)return null;var solid=(SolidColorBrush)brush;return new{color=ColorHex(solid.Color),opacity=solid.Opacity};}
        Task SaveAsync()=>File.WriteAllTextAsync(Path.Combine(folder,"search-input-states.json"),JsonSerializer.Serialize(new{passed,phase,reports,paletteChecks,paintGates,originalContentRestored=ReferenceEquals(Content,original)},new JsonSerializerOptions{WriteIndented=true}));
        async Task AwaitPaintAsync(Func<bool> condition,string kind)
        {
            var ready=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var timer=Stopwatch.StartNew();var stable=0;var frames=new List<object>();
            void Rendered(object? sender,object args){var matches=condition();stable=matches?stable+1:0;frames.Add(new{milliseconds=timer.Elapsed.TotalMilliseconds,matches,stable});if(stable>=2)ready.TrySetResult();}
            CompositionTarget.Rendering+=Rendered;
            try{page.InvalidateArrange();await ready.Task.WaitAsync(TimeSpan.FromSeconds(2));}
            finally{CompositionTarget.Rendering-=Rendered;paintGates.Add(new{phase,theme=page.RequestedTheme.ToString(),kind,passed=ready.Task.IsCompletedSuccessfully,stable,milliseconds=timer.Elapsed.TotalMilliseconds,frames});}
        }
        static bool Same(Brush actual,Brush expected,double multiplier=1){var a=(SolidColorBrush)actual;var e=(SolidColorBrush)expected;var alpha=e.Color.A/255d*e.Opacity*multiplier;return Math.Abs(a.Color.A/255d*a.Opacity-alpha)<.000001&&(alpha==0||a.Color.R==e.Color.R&&a.Color.G==e.Color.G&&a.Color.B==e.Color.B);}
        static IEnumerable<T> Children<T>(DependencyObject parent)where T:DependencyObject{for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T item)yield return item;foreach(var nested in Children<T>(child))yield return nested;}}
    }
}
