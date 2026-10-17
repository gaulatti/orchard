using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunSearchInputsAsync(string folder)
    {
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Orchard.Catalog.Reference.SearchInput.json")??throw new InvalidOperationException("Missing SearchInput reference.");
        using var source=await JsonDocument.ParseAsync(stream);
        var original=Content;var page=new Page();var canvas=new Canvas();var scope=new ScrollViewer{Content=canvas,IsTabStop=false,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled};AutomationProperties.SetAutomationId(scope,"SearchA11y-Scope");page.Content=scope;
        var reports=new List<object>();var inputs=new List<object>();var accessibility=new List<object>();var phase="mount";var passed=false;NativeInputSession? input=null;
        try
        {
            Content=page;await AwaitPresentationAsync(page);input=new NativeInputSession(page.XamlRoot,folder,"search-input");await input.AcquireAsync(canvas);
            var width=(int)page.XamlRoot.Size.Width;Require(width is 320 or 640,"SearchInput reference requires a 320 or 640 client.");
            foreach(var scheme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                page.RequestedTheme=scheme;canvas.Background=new SolidColorBrush(scheme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));
                foreach(var reference in source.RootElement.GetProperty("cases").EnumerateArray().Where(item=>item.GetProperty("width").GetInt32()==width&&item.GetProperty("scheme").GetString()==scheme.ToString()))
                {
                    phase=reference.GetProperty("id").GetString()!;var spec=reference.GetProperty("props");canvas.Children.Clear();
                    var search=new BleeckerSearchInput{Width=width-40,Text=spec.GetProperty("value").GetString()!,PlaceholderText=spec.TryGetProperty("placeholder",out var placeholder)?placeholder.GetString()!:"",InputSize=spec.TryGetProperty("inputSize",out var size)?size.GetString()=="sm"?BleeckerInputSize.Small:BleeckerInputSize.Large:BleeckerInputSize.Medium,IsError=Flag(spec,"error"),IsEditorEnabled=!Flag(spec,"disabled"),IsReadOnly=Flag(spec,"readOnly"),FlowDirection=spec.TryGetProperty("dir",out _)?FlowDirection.RightToLeft:FlowDirection.LeftToRight};
                    if(Flag(spec,"clear"))search.ClearRequested+=(_,_)=>{};
                    AutomationProperties.SetName(search,"Search catalogue");Canvas.SetLeft(search,20);Canvas.SetTop(search,20);canvas.Children.Add(search);
                    await input.MoveAsync(canvas,true,new Point(width-10,400));await AwaitPresentationAsync(page);
                    var editor=search.Editor;var clear=Children<Button>(search).SingleOrDefault(button=>button.Visibility==Visibility.Visible);var icon=(FrameworkElement)editor.StartIcon!;
                    reports.Add(new{id=phase,width,scheme=scheme.ToString(),editor=new{box=Box(editor),value=editor.Text,editor.IsEnabled,editor.IsReadOnly,editor.Padding,editor.FontSize,name=FrameworkElementAutomationPeer.CreatePeerForElement(editor).GetName(),role=FrameworkElementAutomationPeer.CreatePeerForElement(editor).GetAutomationControlType().ToString()},search=Box(icon),clear=clear is null?null:new{box=Box(clear),name=FrameworkElementAutomationPeer.CreatePeerForElement(clear).GetName(),clear.IsEnabled,role=FrameworkElementAutomationPeer.CreatePeerForElement(clear).GetAutomationControlType().ToString()}});
                    await SaveAsync();await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,reference.GetProperty("file").GetString()!));
                    if(phase=="value")await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,$"catalog-{scheme.ToString().ToLowerInvariant()}.png"));
                }
                phase="input-"+scheme;canvas.Children.Clear();var consumer=new TextBlock{Text="Culture"};var bound=new BleeckerSearchInput{Width=width-40};Canvas.SetLeft(bound,20);Canvas.SetTop(bound,20);
                bound.SetBinding(BleeckerSearchInput.TextProperty,new Binding{Source=consumer,Path=new PropertyPath("Text"),Mode=BindingMode.TwoWay});
                AutomationProperties.SetName(bound,"Search catalogue");var clears=0;EventHandler callback=(_,_)=>clears++;bound.ClearRequested+=callback;canvas.Children.Add(bound);await AwaitPresentationAsync(page);
                var clearButton=Children<Button>(bound).Single();var editorInput=bound.Editor;var editorPeer=FrameworkElementAutomationPeer.CreatePeerForElement(editorInput);
                Require(editorPeer.GetAutomationControlType()==AutomationControlType.Edit,"Search editor lost its native Edit role.");
                await CheckAccessibilityAsync("default");
                Require(clearButton.Focus(FocusState.Keyboard),"Search clear refused focus.");await input.KeyAsync(0x20,clearButton);await input.KeyAsync(0x0D,clearButton);await input.ClickAsync(clearButton);
                Require(clears==3&&bound.Text=="Culture"&&consumer.Text=="Culture","Clear changed consumer-owned text or invoked incorrectly.");
                Require(editorInput.Focus(FocusState.Keyboard),"Search editor refused focus before Tab traversal.");await input.HeldKeyAsync(0x09,editorInput,()=>clearButton,()=>Task.CompletedTask);Require(ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot),clearButton),"Search Tab did not reach its separate clear action.");
                bound.IsEditorEnabled=false;await AwaitPresentationAsync(page);Require(!editorInput.IsEnabled&&clearButton.IsEnabled,"Editor-only disablement disabled its sibling clear.");await CheckAccessibilityAsync("disabled");await input.ClickAsync(clearButton);Require(clears==4,"Disabled editor blocked clear.");
                bound.IsEditorEnabled=true;bound.IsReadOnly=true;await AwaitPresentationAsync(page);await CheckAccessibilityAsync("readonly");await input.ClickAsync(clearButton);Require(clears==5&&bound.Text=="Culture","Read-only editor blocked clear or changed text.");
                Require(editorInput.Focus(FocusState.Keyboard),"Read-only search refused focus.");await input.KeyAsync(0x1B,editorInput);Require(bound.Text=="Culture","Escape edited read-only text.");
                bound.IsReadOnly=false;await input.KeyAsync(0x1B,editorInput);await AwaitPresentationAsync(page);Require(bound.Text==""&&consumer.Text==""&&clearButton.Visibility==Visibility.Collapsed&&clears==5,"Escape failed native edit/binding or invoked clear callback.");
                await CheckAccessibilityAsync("empty");
                await input.KeyAsync(0x41,editorInput);await AwaitPresentationAsync(page);Require(bound.Text.Equals("a",StringComparison.OrdinalIgnoreCase)&&consumer.Text==bound.Text,"Native typing failed two-way consumer binding.");
                editorInput.SelectAll();await input.KeyAsync(0x42,editorInput);await AwaitPresentationAsync(page);Require(bound.Text.Equals("b",StringComparison.OrdinalIgnoreCase)&&consumer.Text==bound.Text,"Native selection replacement failed.");
                await input.KeyAsync(0x08,editorInput);await AwaitPresentationAsync(page);Require(bound.Text==""&&consumer.Text=="","Native Backspace did not reach consumer.");
                consumer.Text="External update";await AwaitPresentationAsync(page);Require(editorInput.Text==consumer.Text&&bound.Text==consumer.Text&&clearButton.Visibility==Visibility.Visible,"Consumer update failed after native edits.");
                editorInput.SelectAll();await input.ModifiedKeyAsync(0x11,0x43,editorInput);await input.KeyAsync(0x1B,editorInput);await AwaitPresentationAsync(page);Require(consumer.Text=="","Native clipboard preparation Escape did not edit text.");
                await input.ModifiedKeyAsync(0x11,0x56,editorInput);await AwaitPresentationAsync(page);Require(editorInput.Text=="External update"&&bound.Text==editorInput.Text&&consumer.Text==editorInput.Text,"Native clipboard paste did not traverse editor and consumer binding.");
                bound.MaxLength=3;editorInput.SelectAll();foreach(var key in new ushort[]{0x43,0x44,0x45,0x46})await input.KeyAsync(key,editorInput);await AwaitPresentationAsync(page);Require(bound.Text.Equals("cde",StringComparison.OrdinalIgnoreCase)&&consumer.Text==bound.Text,"Native MaxLength did not constrain edits through consumer binding.");bound.MaxLength=0;consumer.Text="External update";await AwaitPresentationAsync(page);
                bound.ClearLabel="Limpar pesquisa";await AwaitPresentationAsync(page);Require(FrameworkElementAutomationPeer.CreatePeerForElement(clearButton).GetName()=="Limpar pesquisa","Clear label did not update native peer.");
                AutomationProperties.SetName(bound,"Updated search name");await AwaitPresentationAsync(page);Require(editorPeer.GetName()=="Updated search name","Editor did not receive live accessible name.");
                await CheckAccessibilityAsync("updated");
                var parameter=new object();var executions=0;var command=new FilterFixtureCommand(value=>{Require(ReferenceEquals(value,parameter),"Search command replaced consumer parameter.");executions++;});bound.ClearCommand=command;bound.ClearCommandParameter=parameter;command.Allowed=false;command.Notify();await AwaitPresentationAsync(page);Require(!clearButton.IsEnabled,"CanExecute rejection left clear enabled.");
                command.Allowed=true;command.Notify();await AwaitPresentationAsync(page);await input.ClickAsync(clearButton);Require(clears==6&&executions==1&&consumer.Text=="External update","Clear command/event boundary failed.");
                bound.IsEnabled=false;await AwaitPresentationAsync(page);Require(!editorInput.IsEnabled&&!clearButton.IsEnabled,"Compound disablement did not reach both controls.");bound.IsEnabled=true;await AwaitPresentationAsync(page);
                foreach(var inputSize in new[]{BleeckerInputSize.Small,BleeckerInputSize.Large,BleeckerInputSize.Medium}){bound.InputSize=inputSize;await AwaitPresentationAsync(page);Require(editorInput.Padding==new Thickness(40,0,32,0),"Input size reset search padding.");}
                var template=bound.Template;bound.Template=null;await AwaitPresentationAsync(page);Require(!clearButton.IsLoaded&&!editorInput.IsLoaded,"Discarded template retained loaded native controls.");
                bound.Template=template;bound.ApplyTemplate();await AwaitPresentationAsync(page);Require(ReferenceEquals(bound.Editor,editorInput)&&editorInput.IsLoaded&&editorInput.Text==consumer.Text,"Template replacement lost native editor or binding.");var replacement=Children<Button>(bound).Single();Require(!ReferenceEquals(replacement,clearButton),"Template replacement retained stale clear action.");clearButton=replacement;
                canvas.Children.Remove(bound);await AwaitPresentationAsync(page);consumer.Text="Remounted text";canvas.Children.Add(bound);await AwaitPresentationAsync(page);Require(editorInput.Text==consumer.Text&&clearButton.IsEnabled,"Remount failed to reconnect editor/clear.");
                await CheckAccessibilityAsync("remounted");
                bound.ClearCommand=null;bound.ClearRequested-=callback;await AwaitPresentationAsync(page);Require(clearButton.Visibility==Visibility.Collapsed,"No callback/command left a visible clear action.");
                EventHandler consumerClear=(_,_)=>bound.Text="";bound.ClearRequested+=consumerClear;await AwaitPresentationAsync(page);await input.ClickAsync(clearButton);await AwaitPresentationAsync(page);Require(bound.Text==""&&consumer.Text==""&&clearButton.Visibility==Visibility.Collapsed,"Consumer clear failed retained outer two-way binding.");
                inputs.Add(new{scheme=scheme.ToString(),clears,executions,consumerOwnership=true,tabOrder=true,disabledEditorClear=true,readOnlyClear=true,readOnlyEscape=true,escapeChange=true,nativeTyping=true,selectionReplacement=true,backspace=true,clipboard=true,maxLength=true,twoWayBinding=true,liveNames=true,commandGuard=true,inheritedDisabled=true,sizePadding=true,templateReplacement=true,unloadRemount=true,optionalClear=true,consumerClear=true});await SaveAsync();
                async Task CheckAccessibilityAsync(string id)
                {
                    var tree=await ProbeSearchInputAsync(folder,input.Window,scheme+"-"+id);var editors=tree.GetProperty("editors");var buttons=tree.GetProperty("buttons");Require(editors.GetArrayLength()==1,"External UIA search editor count differs.");var edit=editors[0];
                    Require(tree.GetProperty("process").GetInt32()==Environment.ProcessId&&tree.GetProperty("clientProcess").GetInt32()!=Environment.ProcessId,"Search UIA did not cross the client boundary.");
                    Require(edit.GetProperty("hasValue").GetBoolean()&&edit.GetProperty("hasText").GetBoolean()&&edit.GetProperty("value").GetString()==bound.Text&&edit.GetProperty("readOnly").GetBoolean()==bound.IsReadOnly,"External native editor value/text/read-only contract differs.");
                    Require(edit.GetProperty("enabled").GetBoolean()==editorInput.IsEnabled&&edit.GetProperty("name").GetString()==AutomationProperties.GetName(bound),"External search name/enabled state differs.");
                    var visible=clearButton.Visibility==Visibility.Visible;Require(buttons.GetArrayLength()==(visible?1:0),"External optional clear count differs.");if(visible)Require(buttons[0].GetProperty("hasInvoke").GetBoolean()&&buttons[0].GetProperty("name").GetString()==bound.ClearLabel&&buttons[0].GetProperty("enabled").GetBoolean()==clearButton.IsEnabled,"External native clear contract differs.");
                    accessibility.Add(new{scheme=scheme.ToString(),id,tree});await SaveAsync();
                }
            }
            passed=true;
        }
        finally{input?.Dispose();Content=original;await AwaitPresentationAsync(CatalogRoot);await SaveAsync();}
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="search-input",environment=RuntimeEnvironment()}));
        static bool Flag(JsonElement value,string key)=>value.TryGetProperty(key,out var flag)&&flag.GetBoolean();
        object Box(FrameworkElement element){var bounds=element.TransformToVisual(page).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));return new{x=bounds.X,y=bounds.Y,width=bounds.Width,height=bounds.Height};}
        Task SaveAsync()=>File.WriteAllTextAsync(Path.Combine(folder,"search-input.json"),JsonSerializer.Serialize(new{passed,phase,reports,inputs,accessibility,originalContentRestored=ReferenceEquals(Content,original)},new JsonSerializerOptions{WriteIndented=true}));
        static IEnumerable<T> Children<T>(DependencyObject parent)where T:DependencyObject
        {for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T item)yield return item;foreach(var nested in Children<T>(child))yield return nested;}}
    }
    private static async Task<JsonElement> ProbeSearchInputAsync(string folder,IntPtr window,string id)
    {
        var executable=Path.Combine(folder,"SearchInputUiaProbe.exe");
        if(!File.Exists(executable))
        {
            var framework=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"Microsoft.NET/Framework64/v4.0.30319");var compile=new ProcessStartInfo(Path.Combine(framework,"csc.exe")){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true};
            foreach(var argument in new[]{"/nologo","/target:exe","/out:"+executable,"/reference:"+Path.Combine(framework,"WPF/UIAutomationClient.dll"),"/reference:"+Path.Combine(framework,"WPF/UIAutomationTypes.dll"),"/reference:"+Path.Combine(framework,"WPF/WindowsBase.dll"),"/reference:"+Path.Combine(framework,"System.Runtime.Serialization.dll"),Path.Combine(AppContext.BaseDirectory,"SearchInputUiaProbe.cs")})compile.ArgumentList.Add(argument);
            using var compiler=Process.Start(compile)??throw new InvalidOperationException("Could not start SearchInput UIA compiler.");var messages=compiler.StandardOutput.ReadToEndAsync();var errors=compiler.StandardError.ReadToEndAsync();await compiler.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));Require(compiler.ExitCode==0,"SearchInput UIA compiler failed: "+await messages+await errors);
        }
        var output=Path.Combine(folder,"uia-"+id+".json");var start=new ProcessStartInfo(executable){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true};foreach(var argument in new[]{window.ToInt64().ToString(),Environment.ProcessId.ToString(),output})start.ArgumentList.Add(argument);
        using var process=Process.Start(start)??throw new InvalidOperationException("Could not start external SearchInput UIA client.");var stderr=process.StandardError.ReadToEndAsync();await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));Require(process.ExitCode==0,"External SearchInput UIA failed: "+await stderr);using var tree=JsonDocument.Parse(await File.ReadAllTextAsync(output));return tree.RootElement.Clone();
    }
}
