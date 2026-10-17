using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunFilterChipAccessibilitySessionAsync(string folder)
    {
        var original=Content;var page=new Page{IsTabStop=false};var host=new StackPanel{Spacing=16,Margin=new Thickness(20)};
        var scope=new ScrollViewer{Content=host,IsTabStop=false,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
        AutomationProperties.SetAutomationId(scope,"FilterSession-Scope");page.Content=scope;
        var reports=new List<object>();var checks=new List<object>();var phase="mount";var passed=false;NativeInputSession? input=null;
        Process? client=null;Task<string>? stderr=null;
        try
        {
            Content=page;await AwaitPresentationAsync(page);input=new NativeInputSession(page.XamlRoot,folder,"filter-chip-accessibility-session");
            var executable=await CompileFilterSessionClientAsync(folder);
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                page.RequestedTheme=theme;page.Background=new SolidColorBrush(theme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));host.Children.Clear();
                var reference=new Button{Content="Native reference",HorizontalAlignment=HorizontalAlignment.Left};AutomationProperties.SetAutomationId(reference,"SessionReference");
                var single=new BleeckerFilterChip{Label="Status",Value="Draft",HorizontalAlignment=HorizontalAlignment.Left};var group=new BleeckerFilterGroup{Width=600};
                var singleCalls=0;var commandCalls=0;var clearCalls=0;var parameter=new object();var eventFloor=0;single.RemoveRequested+=(_,_)=>singleCalls++;
                var command=new FilterFixtureCommand(value=>{Require(ReferenceEquals(value,parameter),"Retained UIA command replaced its parameter.");commandCalls++;});
                var model=new BleeckerFilter("Desk","Culture"){RemoveCommand=command,RemoveCommandParameter=parameter};group.Filters.Add(model);group.Filters.Add(new("Read-only","Fictional"));group.ClearAllRequested+=(_,_)=>clearCalls++;
                var caption=new TextBlock{Text="Linked name"};host.Children.Add(reference);host.Children.Add(caption);host.Children.Add(single);host.Children.Add(group);await AwaitPresentationAsync(page);await input.AcquireAsync(reference);AssignIds();
                var start=new ProcessStartInfo(executable){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
                start.ArgumentList.Add(input.Window.ToInt64().ToString());start.ArgumentList.Add(Environment.ProcessId.ToString());client=Process.Start(start)??throw new InvalidOperationException("Could not start persistent UIA client.");stderr=client.StandardError.ReadToEndAsync();
                await ReadAsync("ready");
                foreach(var (slot,id)in new[]{("reference","SessionReference"),("single-old","SessionSingle"),("group-old","SessionGroup"),("clear-old","SessionClear")})
                {await RequestAsync("retain\t"+slot+"\t"+id);await RequestAsync("watch\t"+slot);}
                AutomationProperties.SetName(reference,"Native reference updated");
                (FrameworkElementAutomationPeer.FromElement(reference)??FrameworkElementAutomationPeer.CreatePeerForElement(reference)).RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty,"Native reference","Native reference updated");
                await ExpectEventAsync("native-name-health","reference","name","Native reference updated");
                single.Label="Updated label";await ExpectEventAsync("label-name","single-old","name","Remove Updated label filter");
                AutomationProperties.SetName(single,"Consumer remove name");await ExpectEventAsync("explicit-name","single-old","name","Consumer remove name");
                var nameSource=new TextBlock{Text="Bound remove"};single.SetBinding(AutomationProperties.NameProperty,new Binding{Source=nameSource,Path=new PropertyPath(nameof(TextBlock.Text)),Mode=BindingMode.OneWay});await ExpectEventAsync("bound-name","single-old","name",nameSource.Text);
                nameSource.Text="Bound remove updated";await ExpectEventAsync("live-bound-name","single-old","name",nameSource.Text);
                single.ClearValue(AutomationProperties.NameProperty);AutomationProperties.SetLabeledBy(single,caption);await ExpectEventAsync("linked-name","single-old","name",caption.Text);
                caption.Text="Linked name updated";await ExpectEventAsync("live-linked-name","single-old","name",caption.Text);
                AutomationProperties.SetName(caption,"Linked automation name");await ExpectEventAsync("linked-metadata-name","single-old","name","Linked automation name");
                AutomationProperties.SetName(single,"Consumer remove name");await ExpectEventAsync("name-priority","single-old","name","Consumer remove name");
                await InvokeAsync("single-live","single-old",()=>singleCalls,1);
                command.Allowed=false;command.Notify();await ExpectEventAsync("command-disabled","group-old","enabled","False");await InvokeAsync("disabled","group-old",()=>commandCalls,0);
                command.Allowed=true;command.Notify();await ExpectEventAsync("command-enabled","group-old","enabled","True");await InvokeAsync("command-live","group-old",()=>commandCalls,1);
                group.IsEnabled=false;await ExpectEventAsync("group-disabled","group-old","enabled","False");await InvokeAsync("disabled-parent","group-old",()=>commandCalls,1);
                group.IsEnabled=true;await ExpectEventAsync("group-enabled","group-old","enabled","True");
                group.ClearAllText="Clear chosen filters";await ExpectEventAsync("clear-name","clear-old","name","Clear chosen filters");
                var template=single.Template;single.Template=null;await AwaitPresentationAsync(page);await InvokeAsync("template-empty","single-old",()=>singleCalls,1);
                single.Template=template;single.ApplyTemplate();await AwaitPresentationAsync(page);AssignIds();await RequestAsync("retain\tsingle-new\tSessionSingle");
                await InvokeAsync("template-old","single-old",()=>singleCalls,1);await InvokeAsync("template-new","single-new",()=>singleCalls,2);
                group.Filters.Remove(model);await AwaitPresentationAsync(page);await InvokeAsync("removed-model","group-old",()=>commandCalls,1);
                group.Filters.Clear();await AwaitPresentationAsync(page);await InvokeAsync("empty-clear","clear-old",()=>clearCalls,0);
                host.Children.Remove(single);await AwaitPresentationAsync(page);await InvokeAsync("unloaded","single-new",()=>singleCalls,2);
                host.Children.Add(single);await AwaitPresentationAsync(page);AssignIds();await RequestAsync("retain\tsingle-remounted\tSessionSingle");await InvokeAsync("remounted","single-remounted",()=>singleCalls,3);
                checks.Add(new{id="final-consumers",scheme=theme.ToString(),singleCalls,commandCalls,clearCalls,filters=group.Filters.Count});
                await RequestAsync("quit");client.StandardInput.Close();await client.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3));Require(client.ExitCode==0,"UIA session failed: "+await stderr);client.Dispose();client=null;
                await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,$"catalog-{theme.ToString().ToLowerInvariant()}.png"));

                void AssignIds()
                {
                    AutomationProperties.SetAutomationId(FilterTypographyDescendants<Button>(single).Single(),"SessionSingle");
                    foreach(var button in FilterTypographyDescendants<Button>(group))
                    {if(button.Name=="ClearAllFilters")AutomationProperties.SetAutomationId(button,"SessionClear");else if(AutomationProperties.GetName(button)=="Remove Desk filter")AutomationProperties.SetAutomationId(button,"SessionGroup");}
                }
                async Task<JsonElement> RequestAsync(string commandLine)
                {phase=theme+"/"+commandLine.Split('\t')[0];await client!.StandardInput.WriteLineAsync(commandLine);return await ReadAsync(commandLine);}
                async Task<JsonElement> ReadAsync(string request)
                {
                    var line=await client!.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(3));
                    Require(line is not null,"UIA session closed unexpectedly: "+(client.HasExited?await stderr!:"still running"));
                    using var json=JsonDocument.Parse(line!);var response=json.RootElement.Clone();reports.Add(new{scheme=theme.ToString(),request,response});await SaveAsync();return response;
                }
                async Task ExpectEventAsync(string id,string slot,string property,string value)
                {
                    phase=theme+"/"+id;var elapsed=Stopwatch.StartNew();var found=false;var since=eventFloor;var sequence=0;
                    while(elapsed.Elapsed<TimeSpan.FromSeconds(2))
                    {
                        var response=await RequestAsync("events");var match=response.GetProperty("events").EnumerateArray().FirstOrDefault(item=>item.GetProperty("sequence").GetInt32()>since&&item.GetProperty("slot").GetString()==slot&&item.GetProperty("property").GetString()==property&&item.GetProperty("value").GetString()==value);found=match.ValueKind!=JsonValueKind.Undefined;if(found){sequence=match.GetProperty("sequence").GetInt32();eventFloor=sequence;}
                        if(found)break;await Task.Delay(25);
                    }
                    checks.Add(new{id,scheme=theme.ToString(),slot,property,value,delivered=found,since,sequence,milliseconds=elapsed.Elapsed.TotalMilliseconds});await SaveAsync();Require(found,"External UIA property event was not delivered: "+theme+"/"+id);
                }
                async Task InvokeAsync(string id,string slot,Func<int> counter,int expected)
                {
                    var response=await RequestAsync("invoke\t"+slot);await AwaitPresentationAsync(page);var actual=counter();
                    checks.Add(new{id,scheme=theme.ToString(),slot,outcome=response.GetProperty("outcome").GetString(),expected,actual});await SaveAsync();Require(actual==expected,"Retained UIA pattern invoked an inactive consumer or missed its current consumer: "+id);
                }
            }
            passed=true;
        }
        finally
        {
            if(client is not null)
            {
                client.StandardInput.Close();try{await client.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2));}catch(TimeoutException){client.Kill();await client.WaitForExitAsync();}client.Dispose();
            }
            input?.Dispose();Content=original;await AwaitPresentationAsync(CatalogRoot);await SaveAsync();
        }
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="filter-chip-accessibility-session",environment=RuntimeEnvironment()}));
        Task SaveAsync()=>File.WriteAllTextAsync(Path.Combine(folder,"filter-chip-accessibility-session.json"),JsonSerializer.Serialize(new{passed,phase,reports,checks,originalContentRestored=ReferenceEquals(Content,original)},new JsonSerializerOptions{WriteIndented=true}));
    }
    private static async Task<string> CompileFilterSessionClientAsync(string folder)
    {
        var executable=Path.Combine(folder,"FilterChipUiaSession.exe");var framework=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"Microsoft.NET/Framework64/v4.0.30319");
        var start=new ProcessStartInfo(Path.Combine(framework,"csc.exe")){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true};
        foreach(var argument in new[]{"/nologo","/target:exe","/out:"+executable,"/reference:"+Path.Combine(framework,"WPF/UIAutomationClient.dll"),"/reference:"+Path.Combine(framework,"WPF/UIAutomationTypes.dll"),"/reference:"+Path.Combine(framework,"WPF/WindowsBase.dll"),"/reference:"+Path.Combine(framework,"System.Runtime.Serialization.dll"),Path.Combine(AppContext.BaseDirectory,"FilterChipUiaSession.cs")})start.ArgumentList.Add(argument);
        using var compiler=Process.Start(start)??throw new InvalidOperationException("Could not compile persistent UIA client.");var output=compiler.StandardOutput.ReadToEndAsync();var errors=compiler.StandardError.ReadToEndAsync();await compiler.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));Require(compiler.ExitCode==0,"UIA session compilation failed: "+await output+await errors);return executable;
    }
}
