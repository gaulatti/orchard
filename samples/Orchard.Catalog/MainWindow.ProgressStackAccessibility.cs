using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunProgressStackAccessibilityAsync(string folder)
    {
        using var stream=CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.ProgressStackText.json")??throw new InvalidOperationException("ProgressStack text reference missing.");
        using var reference=await JsonDocument.ParseAsync(stream);
        var original=Content; var canvas=new Canvas { UseLayoutRounding=false }; var snapshots=new List<object>();
        var window=WinRT.Interop.WindowNative.GetWindowHandle(this); var completed=false;
        try {
            Content=canvas; await AwaitPresentationAsync(canvas);
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark }) {
                canvas.RequestedTheme=theme;
                canvas.Background=new SolidColorBrush(theme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));
                foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray().Where(row=>row.GetProperty("scheme").GetString()==theme.ToString()&&row.GetProperty("motion").GetString()=="reduce")) {
                    var spec=row.GetProperty("spec"); var control=new BleeckerProgressStack { Width=spec.GetProperty("width").GetDouble() };
                    if (spec.TryGetProperty("rtl",out _)) control.FlowDirection=FlowDirection.RightToLeft;
                    foreach (var item in spec.GetProperty("items").EnumerateArray()) control.Items.Add(new() { Label=item.GetProperty("label").GetString()!, Value=double.Parse(item.GetProperty("value").GetString()!,CultureInfo.InvariantCulture), Color=new SolidColorBrush(Microsoft.UI.Colors.Blue) });
                    AutomationProperties.SetAutomationId(control,"ProgressStackA11y-Scope"); AutomationProperties.SetName(control,"Consumer distribution");
                    Canvas.SetLeft(control,20); Canvas.SetTop(control,20); canvas.Children.Clear(); canvas.Children.Add(control); await AwaitPresentationAsync(control);
                    var id=row.GetProperty("id").GetString()+"-"+theme;
                    var tree=await ProbeProgressStackAsync(folder,window,id);
                    snapshots.Add(new { id=row.GetProperty("id").GetString(), scheme=theme.ToString(), labels=control.Items.Select(item=>item.Label).ToArray(), values=control.Items.Select(item=>BleeckerNumberText.Format(item.Value)).ToArray(), painted=Desc<Panel>(control).Where(part=>part.Name is "ProgressStackLabel" or "ProgressStackValue").Select(part=>Desc<TextBlock>(part).Single().Text).ToArray(), tree });
                }
                await RunProgressStackAccessibilityUpdatesAsync(folder,canvas,window,theme);
                await NativeScreenCapture.CaptureClientAsync(window,Path.Combine(folder,"catalog-"+theme.ToString().ToLowerInvariant()+".png"));
            }
            completed=true;
        } finally {
            Content=original; await AwaitPresentationAsync(CatalogRoot);
            await File.WriteAllTextAsync(Path.Combine(folder,"progress-stack-accessibility.json"),JsonSerializer.Serialize(new { passed=completed, originalContentRestored=ReferenceEquals(Content,original), snapshots },new JsonSerializerOptions { WriteIndented=true }));
        }
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new { passed=true, component="progress-stack-accessibility", environment=RuntimeEnvironment() }));
    }

    private static async Task RunProgressStackAccessibilityUpdatesAsync(string folder,Canvas canvas,IntPtr window,ElementTheme theme)
    {
        var control=new BleeckerProgressStack { Width=100 };
        var item=new BleeckerProgressStackItem { Label="publi\u00adcation\u00adworkflow",Value=6,Color=new SolidColorBrush(Microsoft.UI.Colors.Blue) };
        control.Items.Add(item);control.Items.Add(new() {Label="Draft",Value=3});
        AutomationProperties.SetAutomationId(control,"ProgressStackA11y-Scope");AutomationProperties.SetName(control,"Consumer distribution");
        Canvas.SetLeft(control,20);Canvas.SetTop(control,20);canvas.Children.Clear();canvas.Children.Add(control);await AwaitPresentationAsync(control);
        var start=new ProcessStartInfo(Path.Combine(folder,"ProgressStackUiaProbe.exe")) { UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=System.Text.Encoding.UTF8 };
        start.ArgumentList.Add(window.ToInt64().ToString());start.ArgumentList.Add(Environment.ProcessId.ToString());
        using var process=Process.Start(start)??throw new InvalidOperationException("Could not start ProgressStack UIA session.");
        var stderr=process.StandardError.ReadToEndAsync();var states=new List<object>();
        async Task<JsonElement> Response(string phase,bool send=true)
        {
            if(send){await process.StandardInput.WriteLineAsync("state");await process.StandardInput.FlushAsync();}
            var line=await process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10));Require(line is not null,"ProgressStack UIA session ended: "+(process.HasExited?await stderr:""));
            using var parsed=JsonDocument.Parse(line!);var tree=parsed.RootElement.Clone();
            states.Add(new {phase,labels=control.ShowLegend?control.Items.Select(item=>item.Label).ToArray():[],values=control.ShowLegend?control.Items.Select(item=>BleeckerNumberText.Format(item.Value)).ToArray():[],tree});return tree;
        }
        try {
            var first=await Response("initial",send:false);var identity=first.GetProperty("retained").GetProperty("runtimeId").GetRawText();
            item.Label="日本語の公開ワークフロー";item.Value=8;await AwaitPresentationAsync(control);
            var clock=Stopwatch.StartNew();JsonElement changed;
            do {await Task.Delay(30);changed=await Response("live-name-event");} while(!changed.GetProperty("nameEvents").EnumerateArray().Any(value=>value.GetString()==item.Label)&&clock.Elapsed<TimeSpan.FromSeconds(3));
            Require(changed.GetProperty("retained").GetProperty("runtimeId").GetRawText()==identity,"ProgressStack replaced the retained label provider.");
            Require(changed.GetProperty("retained").GetProperty("name").GetString()==item.Label&&changed.GetProperty("nameEvents").EnumerateArray().Any(value=>value.GetString()==item.Label),"ProgressStack did not expose the live logical label/name event.");
            control.Width=280;await AwaitPresentationAsync(control);await Response("widen");
            control.Items.Move(0,1);await AwaitPresentationAsync(control);await Response("reorder");
            control.ShowLegend=false;await AwaitPresentationAsync(control);await Response("hide");
            control.ShowLegend=true;await AwaitPresentationAsync(control);await Response("show");
            canvas.Children.Remove(control);await AwaitPresentationAsync(canvas);canvas.Children.Add(control);await AwaitPresentationAsync(control);await Response("remount");
            var template=control.Template;control.Template=null;await AwaitPresentationAsync(canvas);control.Template=template;await AwaitPresentationAsync(control);await Response("retemplate");
            control.Items.Clear();await AwaitPresentationAsync(control);await Response("clear");
            await process.StandardInput.WriteLineAsync("quit");await process.StandardInput.FlushAsync();await process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10));process.StandardInput.Close();await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3));Require(process.ExitCode==0,"ProgressStack UIA session failed: "+await stderr);
        } finally {
            if(!process.HasExited){process.StandardInput.Close();await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3));}
            await File.WriteAllTextAsync(Path.Combine(folder,"uia-updates-"+theme+".json"),JsonSerializer.Serialize(new {scheme=theme.ToString(),states},new JsonSerializerOptions {WriteIndented=true}));
        }
    }

    private static async Task<JsonElement> ProbeProgressStackAsync(string folder,IntPtr window,string id)
    {
        var executable=Path.Combine(folder,"ProgressStackUiaProbe.exe");
        if (!File.Exists(executable)) {
            var framework=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"Microsoft.NET/Framework64/v4.0.30319");
            var compile=new ProcessStartInfo(Path.Combine(framework,"csc.exe")) { UseShellExecute=false, CreateNoWindow=true, WindowStyle=ProcessWindowStyle.Hidden, RedirectStandardOutput=true, RedirectStandardError=true };
            foreach (var argument in new[] { "/nologo", "/target:exe", "/out:"+executable, "/reference:"+Path.Combine(framework,"WPF/UIAutomationClient.dll"), "/reference:"+Path.Combine(framework,"WPF/UIAutomationTypes.dll"), "/reference:"+Path.Combine(framework,"WPF/WindowsBase.dll"), "/reference:"+Path.Combine(framework,"System.Runtime.Serialization.dll"), Path.Combine(AppContext.BaseDirectory,"ProgressStackUiaProbe.cs") }) compile.ArgumentList.Add(argument);
            using var compiler=Process.Start(compile)??throw new InvalidOperationException("Could not start ProgressStack UIA compiler.");
            var messages=compiler.StandardOutput.ReadToEndAsync(); var errors=compiler.StandardError.ReadToEndAsync();
            await compiler.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)); Require(compiler.ExitCode==0,"ProgressStack UIA compiler failed: "+await messages+await errors);
        }
        var output=Path.Combine(folder,"uia-"+id+".json");
        var start=new ProcessStartInfo(executable) { UseShellExecute=false, CreateNoWindow=true, WindowStyle=ProcessWindowStyle.Hidden, RedirectStandardOutput=true, RedirectStandardError=true };
        foreach (var argument in new[] { window.ToInt64().ToString(),Environment.ProcessId.ToString(),output }) start.ArgumentList.Add(argument);
        using var process=Process.Start(start)??throw new InvalidOperationException("Could not start external ProgressStack UIA client.");
        var stderr=process.StandardError.ReadToEndAsync(); await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)); Require(process.ExitCode==0,"External ProgressStack UIA failed: "+await stderr);
        using var tree=JsonDocument.Parse(await File.ReadAllTextAsync(output)); return tree.RootElement.Clone();
    }
}
