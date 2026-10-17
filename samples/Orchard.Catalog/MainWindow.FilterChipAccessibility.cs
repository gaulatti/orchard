using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunFilterChipAccessibilityAsync(string folder)
    {
        var original=Content;var page=new Page{IsTabStop=false};
        var host=new StackPanel{Spacing=16,Margin=new Thickness(20)};
        var scope=new ScrollViewer{Content=host,IsTabStop=false,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
        AutomationProperties.SetAutomationId(scope,"FilterA11y-Scope");page.Content=scope;
        var reports=new List<object>();var tabs=new List<object>();var phase="mount";var passed=false;NativeInputSession? input=null;
        try
        {
            Content=page;await AwaitPresentationAsync(page);input=new NativeInputSession(page.XamlRoot,folder,"filter-chip-accessibility");
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                page.RequestedTheme=theme;page.Background=new SolidColorBrush(theme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));host.Children.Clear();
                var before=new Button{Content="Before filters"};var after=new Button{Content="After filters"};
                var caption=new TextBlock{Text="Caption scope"};var single=new BleeckerFilterChip{Label="Region",Value="Europe",HorizontalAlignment=HorizontalAlignment.Left};
                var readOnly=new BleeckerFilterChip{Label="Language",Value="Português",HorizontalAlignment=HorizontalAlignment.Left};
                var group=new BleeckerFilterGroup{Width=600,HorizontalAlignment=HorizontalAlignment.Left};
                var singleCalls=0;var statusCalls=0;var commandCalls=0;var clearCalls=0;var parameter=new object();
                single.RemoveRequested+=(_,_)=>singleCalls++;
                var status=new BleeckerFilter("Status","Draft");status.RemoveRequested+=(_,_)=>statusCalls++;
                var command=new FilterFixtureCommand(value=>{Require(ReferenceEquals(value,parameter),"External UIA command changed the consumer parameter.");commandCalls++;});
                var desk=new BleeckerFilter("Desk","Culture"){RemoveCommand=command,RemoveCommandParameter=parameter};
                group.Filters.Add(status);group.Filters.Add(desk);group.Filters.Add(new("Read-only","Fictional"));group.ClearAllRequested+=(_,_)=>clearCalls++;
                foreach(var item in new FrameworkElement[]{before,caption,single,readOnly,group,after})host.Children.Add(item);
                AutomationProperties.SetAutomationId(before,"FilterA11y-Before");AutomationProperties.SetAutomationId(after,"FilterA11y-After");
                await AwaitPresentationAsync(page);await input.AcquireAsync(before);
                var singleName="Remove Region filter";var showSingle=true;var replacement=false;
                await CheckAsync("default");
                await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,$"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                var actions=Actions();Require(before.Focus(FocusState.Keyboard),"Could not focus pre-filter anchor.");
                foreach(var target in new[]{actions.Single,actions.Status,actions.Desk,actions.Clear,after})await TabAsync(target);
                await CheckAsync("tab-order");
                await TabAsync(actions.Clear,true);await TabAsync(actions.Desk,true);
                Require(actions.Status.Focus(FocusState.Keyboard),"Could not focus retained filter before reorder.");group.Filters.Move(0,1);await AwaitPresentationAsync(page);
                Require(ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot),actions.Status),"Filter reorder lost original action focus.");await CheckAsync("reordered");
                Require(before.Focus(FocusState.Keyboard),"Could not reset reorder traversal.");foreach(var target in new[]{actions.Single,actions.Desk,actions.Status,actions.Clear,after})await TabAsync(target);
                single.Label="Révision 日本語";singleName="Remove Révision 日本語 filter";await CheckAsync("live-label");
                AutomationProperties.SetName(single,"Remove regional scope");singleName="Remove regional scope";await CheckAsync("explicit-name");
                var source=new TextBlock{Text="Remove selected desk"};single.SetBinding(AutomationProperties.NameProperty,new Binding{Source=source,Path=new PropertyPath(nameof(TextBlock.Text)),Mode=BindingMode.OneWay});singleName=source.Text;await CheckAsync("bound-name");
                source.Text="Remove selected owner";singleName=source.Text;await CheckAsync("live-bound-name");
                single.ClearValue(AutomationProperties.NameProperty);AutomationProperties.SetLabeledBy(single,caption);singleName=caption.Text;await CheckAsync("linked-caption");
                caption.Text="Updated caption scope";singleName=caption.Text;await CheckAsync("live-linked-caption");
                AutomationProperties.SetName(single,"Explicit remove priority");singleName="Explicit remove priority";await CheckAsync("name-priority");
                var template=single.Template;single.Template=null;await AwaitPresentationAsync(page);showSingle=false;await CheckAsync("template-empty");
                single.Template=template;single.ApplyTemplate();showSingle=true;await CheckAsync("template-replaced");
                group.ClearAllText="Clear filters";await CheckAsync("localized-clear");
                command.Allowed=false;command.Notify();await CheckAsync("command-disabled","FilterA11y-Desk");Require(commandCalls==0,"Disabled external UIA command executed.");
                actions=Actions();Require(actions.Single.Focus(FocusState.Keyboard),"Could not focus before disabled action traversal.");await TabAsync(actions.Status);
                group.IsEnabled=false;before.IsEnabled=false;await CheckAsync("group-disabled","FilterA11y-Status");Require(statusCalls==0,"Disabled group executed its event.");
                await CheckAsync("native-disabled","FilterA11y-Before");Require(actions.Single.Focus(FocusState.Keyboard),"Standalone action did not remain enabled.");await TabAsync(after);
                group.IsEnabled=true;before.IsEnabled=true;command.Allowed=true;command.Notify();await CheckAsync("invoke-command","FilterA11y-Desk");Require(commandCalls==1&&group.Filters.Count==3,"UIA command did not reach the original consumer once.");
                await CheckAsync("invoke-single","FilterA11y-Single");Require(singleCalls==1,"UIA remove event did not reach original standalone consumer once.");
                await CheckAsync("invoke-clear","FilterA11y-Clear");Require(clearCalls==1&&group.Filters.Count==3,"UIA Clear all mutated caller filters or missed its callback.");
                var previous=group.Filters;var owner=new BleeckerFilter("Owner","Fictional");owner.RemoveRequested+=(_,_)=>statusCalls++;
                group.Filters=[owner];replacement=true;previous.Add(new("Detached","Should stay absent"));await CheckAsync("collection-replaced");
                group.Filters.Clear();await CheckAsync("empty");Require(group.ActualHeight==0,"Empty group retained height.");
                host.Children.Remove(single);showSingle=false;await CheckAsync("unloaded");
                host.Children.Insert(2,single);showSingle=true;await CheckAsync("remounted");
                reports.Add(new{id="consumer-callbacks",scheme=theme.ToString(),singleCalls,commandCalls,clearCalls,statusCalls,callerOwnedFilters=true});
                await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,$"after-lifecycle-{theme}.png"));

                (Button Single,Button Status,Button Desk,Button Clear) Actions()=>
                    (FilterTypographyDescendants<Button>(single).Single(),FilterTypographyDescendants<Button>(group).Single(button=>AutomationProperties.GetName(button)=="Remove Status filter"),FilterTypographyDescendants<Button>(group).Single(button=>AutomationProperties.GetName(button)=="Remove Desk filter"),FilterTypographyDescendants<Button>(group).Single(button=>button.Name=="ClearAllFilters"));
                async Task CheckAsync(string id,string? invokeId=null)
                {
                    phase=theme+"/"+id;await AwaitPresentationAsync(page);
                    var expected=new Dictionary<string,string>{{"FilterA11y-Before","Before filters"},{"FilterA11y-After","After filters"}};
                    if(showSingle){var button=FilterTypographyDescendants<Button>(single).Single();AutomationProperties.SetAutomationId(button,"FilterA11y-Single");expected.Add("FilterA11y-Single",singleName);}
                    if(group.Filters.Count>0)
                    {
                        foreach(var chip in FilterTypographyDescendants<BleeckerFilterChip>(group))
                        {
                            if(chip.Label=="Read-only")continue;var button=FilterTypographyDescendants<Button>(chip).Single();var key="FilterA11y-"+chip.Label;
                            AutomationProperties.SetAutomationId(button,key);expected.Add(key,"Remove "+chip.Label+" filter");
                        }
                        var clear=FilterTypographyDescendants<Button>(group).Single(button=>button.Name=="ClearAllFilters");AutomationProperties.SetAutomationId(clear,"FilterA11y-Clear");expected.Add("FilterA11y-Clear",group.ClearAllText);
                    }
                    var external=await ProbeFilterChipAsync(folder,input!.Window,id+"-"+theme,invokeId);await AwaitPresentationAsync(page);
                    var buttons=external.GetProperty("buttons").EnumerateArray().ToArray();
                    reports.Add(new{id,scheme=theme.ToString(),expected,invokeId,invokeOutcome=external.GetProperty("invokeOutcome").GetString(),buttonOrder=buttons.Select(button=>button.GetProperty("id").GetString()).ToArray(),groupEnabled=group.IsEnabled,commandEnabled=command.Allowed,beforeEnabled=before.IsEnabled,showSingle,replacement});await SaveAsync();
                    Require(buttons.Length==expected.Count,"External UIA button count differs: "+phase);
                    foreach(var button in buttons)
                    {
                        var key=button.GetProperty("id").GetString()!;Require(expected.TryGetValue(key,out var name)&&button.GetProperty("name").GetString()==name,"External UIA name differs: "+phase+"/"+key);
                        var enabled=key=="FilterA11y-Before"?before.IsEnabled:key is "FilterA11y-Single" or "FilterA11y-After"?true:group.IsEnabled&&(key!="FilterA11y-Desk"||command.Allowed);
                        Require(button.GetProperty("enabled").GetBoolean()==enabled&&button.GetProperty("hasInvoke").GetBoolean()&&!button.GetProperty("offscreen").GetBoolean()&&button.GetProperty("controlType").GetString()=="ControlType.Button","External UIA native role/state differs: "+phase+"/"+key);
                    }
                    if(invokeId is not null)Require(external.GetProperty("invokeOutcome").GetString()==(id.Contains("disabled")?"element-not-enabled":"invoked"),"UIA Invoke result differs: "+phase);
                    var texts=external.GetProperty("texts").EnumerateArray().Select(text=>text.GetString()).ToArray();Require(texts.Contains("Language")&&texts.Contains("Português")&&!texts.Contains("Detached"),"Read-only content or collection ownership differs in external UIA.");
                }
                async Task TabAsync(Control to,bool reverse=false)
                {
                    var from=FocusManager.GetFocusedElement(page.XamlRoot)as Control??throw new InvalidOperationException("Tab source is not a native control.");
                    var downs=0;var ups=0;var downOwned=true;var upOwned=true;var released=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var down=new KeyEventHandler((_,args)=>{if((ushort)args.Key==0x09){downs++;downOwned&=ReferenceEquals(args.OriginalSource,from);}});
                    var up=new KeyEventHandler((_,args)=>{if((ushort)args.Key==0x09){ups++;upOwned&=ReferenceEquals(args.OriginalSource,to);released.TrySetResult();}});
                    page.AddHandler(UIElement.KeyDownEvent,down,true);page.AddHandler(UIElement.KeyUpEvent,up,true);
                    try{if(reverse)input!.ShiftKey(0x09);else input!.Key(0x09);await released.Task.WaitAsync(TimeSpan.FromSeconds(2));Require(downs==1&&ups==1&&downOwned&&upOwned&&ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot),to),"Native filter Tab order differs: "+AutomationProperties.GetAutomationId(from)+" -> "+AutomationProperties.GetAutomationId(to));}
                    finally{page.RemoveHandler(UIElement.KeyDownEvent,down);page.RemoveHandler(UIElement.KeyUpEvent,up);tabs.Add(new{scheme=theme.ToString(),from=AutomationProperties.GetAutomationId(from),to=AutomationProperties.GetAutomationId(to),reverse,downs,ups,downOwned,upOwned});await SaveAsync();}
                }
            }
            passed=true;
        }
        finally{input?.Dispose();Content=original;await AwaitPresentationAsync(CatalogRoot);await SaveAsync();}
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="filter-chip-accessibility",environment=RuntimeEnvironment()}));
        Task SaveAsync()=>File.WriteAllTextAsync(Path.Combine(folder,"filter-chip-accessibility.json"),JsonSerializer.Serialize(new{passed,phase,reports,tabs,originalContentRestored=ReferenceEquals(Content,original)},new JsonSerializerOptions{WriteIndented=true}));
    }

    private static async Task<JsonElement> ProbeFilterChipAsync(string folder,IntPtr window,string id,string? invokeId)
    {
        var executable=Path.Combine(folder,"FilterChipUiaProbe.exe");
        if(!File.Exists(executable))
        {
            var framework=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"Microsoft.NET/Framework64/v4.0.30319");
            var compile=new ProcessStartInfo(Path.Combine(framework,"csc.exe")){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true};
            foreach(var argument in new[]{"/nologo","/target:exe","/out:"+executable,"/reference:"+Path.Combine(framework,"WPF/UIAutomationClient.dll"),"/reference:"+Path.Combine(framework,"WPF/UIAutomationTypes.dll"),"/reference:"+Path.Combine(framework,"WPF/WindowsBase.dll"),"/reference:"+Path.Combine(framework,"System.Runtime.Serialization.dll"),Path.Combine(AppContext.BaseDirectory,"FilterChipUiaProbe.cs")})compile.ArgumentList.Add(argument);
            using var compiler=Process.Start(compile)??throw new InvalidOperationException("Could not start FilterChip UIA compiler.");
            var messages=compiler.StandardOutput.ReadToEndAsync();var errors=compiler.StandardError.ReadToEndAsync();await compiler.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Require(compiler.ExitCode==0,"FilterChip UIA compiler failed: "+await messages+await errors);
        }
        var output=Path.Combine(folder,"uia-"+id+".json");var start=new ProcessStartInfo(executable){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true};
        foreach(var argument in new[]{window.ToInt64().ToString(),Environment.ProcessId.ToString(),output})start.ArgumentList.Add(argument);if(invokeId is not null)start.ArgumentList.Add(invokeId);
        using var process=Process.Start(start)??throw new InvalidOperationException("Could not start FilterChip external UIA probe.");var stderr=process.StandardError.ReadToEndAsync();await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));Require(process.ExitCode==0,"FilterChip external UIA failed: "+await stderr);
        using var tree=JsonDocument.Parse(await File.ReadAllTextAsync(output));return tree.RootElement.Clone();
    }
}
