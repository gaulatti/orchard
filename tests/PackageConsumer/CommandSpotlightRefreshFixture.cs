using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Testing;

internal static class CommandSpotlightRefreshFixture
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private delegate bool Visitor(nint handle,nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor,nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    internal static async Task RunAsync(Window window,string output,string hostAssembly)
    {
        var root=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}'/>");
        var host=new BleeckerModalHost{Content=root,Width=1000,Height=960};window.Content=host;window.AppWindow.ResizeClient(new(1000,960));
        var folder=Path.GetDirectoryName(output)!;var guards=0;var focus=0;var activations=0;var stage="initial";var measurements=new List<object>();var profiles=new List<object>();var fontCases=new List<object>();var cases=new List<object>();
        host.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Require(bool value,string label){if(!value)throw new InvalidOperationException(label);}
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Refresh probe left hidden boundary");Require(host.XamlRoot is null||VisualTreeHelper.GetOpenPopupsForXamlRoot(host.XamlRoot).Count==0,"Refresh probe opened a native Popup");}
        void Stage(string value){stage=value;File.WriteAllText(Path.Combine(folder,"refresh-stage.json"),JsonSerializer.Serialize(new{stage,guards}));}
        async Task Layout(){for(var i=0;i<3;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Dispatcher refused layout");await idle.Task;host.Measure(new(1000,960));host.Arrange(new(0,0,1000,960));host.UpdateLayout();Guard();}}
        Button[] Rows()=>Elements(host).OfType<Button>().Where(b=>b.Name=="SpotlightAction").ToArray();
        async Task Measure(List<object> target,string theme,string grouping,string phase,int sample,int iterations,Action operation)
        {
            Stage($"{theme}/{grouping}/{phase}/{sample}");var allocated=GC.GetAllocatedBytesForCurrentThread();var clock=Stopwatch.StartNew();operation();var synchronousMilliseconds=clock.Elapsed.TotalMilliseconds;var synchronousAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-allocated;await Layout();clock.Stop();target.Add(new{theme,grouping,count=1000,phase,sample,iterations,synchronousMilliseconds,synchronousAllocatedBytes,settledMilliseconds=clock.Elapsed.TotalMilliseconds});
        }
        try
        {
            await Layout();
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})foreach(var grouping in new[]{"single","unique"})
            {
                Stage($"{theme}/{grouping}/setup");root.Children.Clear();await Layout();host.RequestedTheme=theme;
                var originals=Enumerable.Range(0,1000).Select(i=>new BleeckerCommandSpotlightAction(i.ToString(),$"Fictional command {i:D6}"){Group=grouping=="single"?"Group":$"Group {i:D6}"}).ToArray();
                var control=new BleeckerCommandSpotlight{Open=true,Actions=originals};root.Children.Add(control);await Layout();await Task.Delay(350);await Layout();var before=Rows();Require(before.Length==1000,"Native collection missing");
                for(var sample=0;sample<3;sample++)await Measure(measurements,theme.ToString(),grouping,"label-refresh",sample,1,()=>control.TriggerLabel="Measured label "+sample);
                var after=Rows();Require(before.Zip(after).All(p=>ReferenceEquals(p.First,p.Second))&&after.Select((r,i)=>ReferenceEquals(r.Tag,originals[i])).All(v=>v),"Refresh replaced caller rows or original actions");
                cases.Add(new{theme=theme.ToString(),grouping,count=1000,originalRowsAndActionsRetained=true,passed=true});
                if(theme==ElementTheme.Light&&grouping=="single")
                {
                    var fontProperty=typeof(BleeckerCommandSpotlight).GetProperty("HasCallerFontFamily",Private)!;var body=typeof(BleeckerCommandSpotlight).GetField("body",Private)!.GetValue(control)!;
                    var bodyUpdate=body.GetType().GetMethod("Update",Private)!;var inputUpdate=body.GetType().GetMethod("UpdateInput",Private)!;var rowSet=before[0].GetType().GetMethod("Set",Private,null,[typeof(BleeckerCommandSpotlightAction)],null)!;
                    var layouts=before.Select(r=>r.GetType().GetField("layout",Private)!.GetValue(r)!).ToArray();var contentUpdate=layouts[0].GetType().GetMethod("Update",Private,null,[typeof(BleeckerCommandSpotlightAction),typeof(Brush)],null)!;
                    var hints=before.Select(r=>Elements(r).OfType<BleeckerKbd>().Single(h=>h.Name=="SpotlightActionHint")).ToArray();var updateHint=typeof(BleeckerCommandSpotlight).GetMethod("UpdateHint",Private)!;
                    var titles=before.Select(r=>Elements(r).OfType<TextBlock>().Single(t=>t.Name=="SpotlightActionTitle")).ToArray();var descriptions=before.Select(r=>Elements(r).OfType<TextBlock>().Single(t=>t.Name=="SpotlightActionDescription")).ToArray();var setText=typeof(BleeckerCommandSpotlight).GetMethod("SetText",Private,null,[typeof(TextBlock),typeof(double),typeof(double),typeof(Brush)],null)!;var secondary=(Brush)typeof(BleeckerCommandSpotlight).GetMethod("Brush",Private)!.Invoke(control,["Secondary"])!;
                    for(var sample=0;sample<3;sample++)
                    {
                        await Measure(profiles,"Light","single","font-style-lookup",sample,2000,()=>{for(var i=0;i<2000;i++)_ = fontProperty.GetValue(control);});
                        await Measure(profiles,"Light","single","row-direct-set",sample,1000,()=>{for(var i=0;i<1000;i++)rowSet.Invoke(before[i],[originals[i]]);});
                        await Measure(profiles,"Light","single","row-content-update",sample,1000,()=>{for(var i=0;i<1000;i++)contentUpdate.Invoke(layouts[i],[originals[i],before[i].Foreground]);});
                        await Measure(profiles,"Light","single","text-style-update",sample,2000,()=>{for(var i=0;i<1000;i++){setText.Invoke(control,[titles[i],14d,20d,before[i].Foreground]);setText.Invoke(control,[descriptions[i],12d,16d,secondary]);}});
                        await Measure(profiles,"Light","single","hint-update",sample,1000,()=>{for(var i=0;i<1000;i++)updateHint.Invoke(control,[hints[i]]);});
                        await Measure(profiles,"Light","single","body-update",sample,1,()=>bodyUpdate.Invoke(body,null));
                        await Measure(profiles,"Light","single","input-update",sample,1,()=>inputUpdate.Invoke(body,null));
                    }
                }
            }
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                Stage($"{theme}/fonts/setup");root.Children.Clear();await Layout();host.RequestedTheme=theme;
                string[] shortcut=["Ctrl","Q"];var original=new BleeckerCommandSpotlightAction("original","Fictional original"){Description="Original description",Group="Original group",Shortcut=shortcut};var icon=new TextBlock();icon.SetBinding(TextBlock.TextProperty,new Binding{Source=original,Path=new("Title"),Mode=BindingMode.OneWay});original.Icon=icon;
                var control=new BleeckerCommandSpotlight{Open=true,Actions=new[]{original}};root.Children.Add(control);await Layout();await Task.Delay(350);await Layout();var row=Rows().Single();var originalStyle=control.Style;
                TextBlock Text(string name)=>Elements(host).OfType<TextBlock>().Single(t=>t.Name==name);
                var initialTitle=Text("SpotlightActionTitle").FontFamily.Source;var initialDescription=Text("SpotlightActionDescription").FontFamily.Source;var initialGroup=Text("SpotlightGroupLabel").FontFamily.Source;
                async Task Fonts(string id,string titleFamily,string descriptionFamily,string groupFamily,double size)
                {
                    Stage($"{theme}/fonts/{id}");await Layout();var title=Text("SpotlightActionTitle");var description=Text("SpotlightActionDescription");var group=Text("SpotlightGroupLabel");var editor=Elements(host).OfType<TextBox>().Single(t=>t.Name=="SpotlightSearch");
                    var currentRow=Rows().Single();var sameRow=ReferenceEquals(row,currentRow);var fontOnly=id is "local" or "binding-first" or "binding-second" or "cleared";Require(ReferenceEquals(currentRow.Tag,original)&&(!fontOnly||sameRow),"Font-only refresh replaced original row/action");row=currentRow;Require(title.FontFamily.Source==titleFamily&&description.FontFamily.Source==descriptionFamily&&group.FontFamily.Source==groupFamily,"Live font ownership changed");Require(title.FontSize==14*size/16&&description.FontSize==12*size/16&&editor.FontSize==size,"Live scaled fonts changed");Require(title.FlowDirection==control.FlowDirection&&description.FlowDirection==control.FlowDirection&&title.Language==control.Language&&title.IsTextScaleFactorEnabled==control.IsTextScaleFactorEnabled,"Live text settings stale");Require(ReferenceEquals(original.Icon,icon)&&icon.GetBindingExpression(TextBlock.TextProperty)is not null&&icon.Text==original.Title,"Caller icon binding lost");
                    var hint=Elements(currentRow).OfType<BleeckerKbd>().Single(h=>h.Name=="SpotlightActionHint");var caps=Elements(hint).OfType<BleeckerKbd>().Where(h=>h.Keys is null).ToArray();var keys=original.Shortcut?.ToArray()??[];Require(hint.Keys!.SequenceEqual(keys)&&hint.Height==20*size/16&&hint.MinHeight==20*size/16&&hint.MinWidth==20*size/16&&caps.Length==keys.Length&&caps.All(c=>c.Height==20*size/16&&c.MinHeight==20*size/16&&c.MinWidth==20*size/16),"Shortcut replacement/template/scaling dimensions stale");
                    fontCases.Add(new{theme=theme.ToString(),id,titleFamily=title.FontFamily.Source,descriptionFamily=description.FontFamily.Source,groupFamily=group.FontFamily.Source,fontSize=size,direction=control.FlowDirection.ToString(),language=control.Language,textScaling=control.IsTextScaleFactorEnabled,originalActionRetained=true,fontOnlyRowRetained=!fontOnly||sameRow,sameRowAsPrevious=sameRow,iconBindingRetained=true,hintDimensionsVerified=true,ownerFontBindingRetained=id.StartsWith("binding")?control.GetBindingExpression(Control.FontFamilyProperty)is not null:true,passed=true});
                }
                await Fonts("default",initialTitle,initialDescription,initialGroup,16);
                var parent=new Style{TargetType=typeof(BleeckerCommandSpotlight),BasedOn=originalStyle};parent.Setters.Add(new Setter(Control.FontFamilyProperty,new FontFamily("Consolas")));var inherited=new Style{TargetType=typeof(BleeckerCommandSpotlight),BasedOn=parent};inherited.Setters.Add(new Setter(Control.FontSizeProperty,24d));control.Style=inherited;await Fonts("inherited","Consolas","Consolas","Consolas",24);
                control.FontFamily=new("Courier New");control.FlowDirection=FlowDirection.RightToLeft;control.Language="tr-TR";control.IsTextScaleFactorEnabled=false;await Fonts("local","Courier New","Courier New","Courier New",24);
                shortcut[0]="Shift";var donor=new TextBlock{FontFamily=new("Arial")};control.SetBinding(Control.FontFamilyProperty,new Binding{Source=donor,Path=new("FontFamily"),Mode=BindingMode.OneWay});await Fonts("binding-first","Arial","Arial","Arial",24);
                donor.FontFamily=new("Tahoma");await Fonts("binding-second","Tahoma","Tahoma","Tahoma",24);
                control.ClearValue(Control.FontFamilyProperty);await Fonts("cleared","Consolas","Consolas","Consolas",24);
                var replacement=new Style{TargetType=typeof(BleeckerCommandSpotlight),BasedOn=originalStyle};replacement.Setters.Add(new Setter(Control.FontFamilyProperty,new FontFamily("Segoe UI")));replacement.Setters.Add(new Setter(Control.FontSizeProperty,20d));control.Style=replacement;original.Title="Updated original";original.Description="Updated description";await Fonts("replaced","Segoe UI","Segoe UI","Segoe UI",20);Require(Text("SpotlightActionTitle").Text==original.Title&&Text("SpotlightActionDescription").Text==original.Description,"Live action text stale");
                control.Style=originalStyle;control.FlowDirection=FlowDirection.LeftToRight;control.Language="en-US";control.IsTextScaleFactorEnabled=true;original.Shortcut=[];await Fonts("restored",initialTitle,initialDescription,initialGroup,16);
            }
            root.Children.Clear();await Layout();Guard();string Hash(string p)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-command-spotlight-refresh-native-v2",passed=true,librarySha256=Hash(typeof(BleeckerCommandSpotlight).Assembly.Location),hostSha256=Hash(hostAssembly),cases,measurements,profiles,fontCases,guards,focusEvents=focus,activations,hostVisible=window.Visible,hardwareInputSent=false,latencyAcceptanceClaimed=false,nativeComponentAccepted=false},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-command-spotlight-refresh-native-v1",passed=false,stage,error=error.ToString(),cases,measurements,profiles,fontCases,guards,focusEvents=focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
}
