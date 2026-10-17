using System.Reflection;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.ApplicationModel.DataTransfer;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunTextareasAsync(string folder)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Orchard.Catalog.Reference.Textarea.json") ?? throw new InvalidOperationException("Missing Textarea reference.");
        using var reference = await JsonDocument.ParseAsync(stream);
        var original = Content;
        var canvas = new Canvas { Height = 600 };
        var page = new Page { Content = canvas };
        var frames = new List<object>(); var interactions = new List<object>(); var phase = "mount"; var passed = false;
        NativeInputSession? input = null;
        try
        {
            Content = page; await AwaitPresentationAsync(page);
            input = new NativeInputSession(page.XamlRoot, folder, "textarea");
            await input.AcquireAsync(canvas);
            var width = (int)page.XamlRoot.Size.Width;
            Require(width is 320 or 640, "Textarea reference requires a 320 or 640 client.");
            canvas.Width = width;
            foreach (var scheme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                page.RequestedTheme = scheme;
                page.Background = new SolidColorBrush(scheme == ElementTheme.Light ? Windows.UI.Color.FromArgb(255,255,255,255) : Windows.UI.Color.FromArgb(255,13,24,33));
                canvas.Background = page.Background;
                foreach (var source in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("scheme").GetString() == scheme.ToString() && c.GetProperty("width").GetInt32() == width))
                {
                    phase = source.GetProperty("file").GetString()!;
                    var spec = source.GetProperty("props"); var state = source.GetProperty("state").GetString();
                    canvas.Children.Clear();
                    var editor = new BleeckerTextarea { Name = "TextareaSourceEditor", Width = source.GetProperty("width").GetDouble() - 40,
                        InputSize = spec.TryGetProperty("textareaSize", out var size) ? size.GetString() == "sm" ? BleeckerInputSize.Small : BleeckerInputSize.Large : BleeckerInputSize.Medium,
                        Rows = spec.TryGetProperty("rows", out var rows) ? rows.GetInt32() : 4,
                        Text = spec.TryGetProperty("defaultValue", out var text) ? text.GetString()! : "",
                        PlaceholderText = spec.TryGetProperty("placeholder", out var placeholder) ? placeholder.GetString()! : "",
                        IsReadOnly = Flag(spec,"readOnly"), IsEnabled = !Flag(spec,"disabled"), IsError = Flag(spec,"error"),
                        FlowDirection = spec.TryGetProperty("dir", out _) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                    if (spec.TryGetProperty("aria-invalid", out var invalid)) editor.IsInvalid = invalid.GetBoolean();
                    AutomationProperties.SetName(editor, "Editorial notes"); AutomationProperties.SetAutomationId(editor,"TextareaA11y-Editor");
                    Canvas.SetLeft(editor,20); Canvas.SetTop(editor,20); canvas.Children.Add(editor);
                    await input.MoveAsync(canvas, true, new Point(width-10,450)); await AwaitPresentationAsync(page);
                    if (state == "hover") await input.MoveAsync(editor);
                    if (state == "focus") { editor.Focus(FocusState.Keyboard); await AwaitPresentationAsync(editor); }
                    await AwaitPresentationAsync(page);
                    Require(editor.ActualHeight == source.GetProperty("box").GetProperty("height").GetDouble() && editor.ActualWidth == source.GetProperty("box").GetProperty("width").GetDouble(), $"Textarea rows/size geometry differs: {phase}, actual {editor.ActualWidth}x{editor.ActualHeight}.");
                    Require(editor.Text == source.GetProperty("value").GetString(), "Textarea changed consumer text.");
                    Require(AutomationProperties.GetIsDataValidForForm(editor) == !(editor.IsInvalid ?? editor.IsError), "Textarea accessibility validity override differs.");
                    var paragraph = editor.TextDocument.GetRange(0,0).ParagraphFormat;
                    Require(paragraph.LineSpacingRule == LineSpacingRule.Exactly && Math.Abs(paragraph.LineSpacing * 4 / 3 - editor.LineHeight) < .001, "Native multiline line spacing differs.");
                    var chrome = Descendants<Border>(editor).Single(e => e.Name == "BorderElement");
                    var viewport = Descendants<ScrollViewer>(editor).Single(e => e.Name == "ContentElement");
                    var paint = (SolidColorBrush)chrome.BorderBrush;
                    var nativeFont=editor.TextDocument.GetRange(0,0).CharacterFormat;
                    editor.TextDocument.GetRange(0,Math.Max(1,editor.Text.IndexOf('\n'))).GetRect(PointOptions.ClientCoordinates,out var firstLineRect,out _);
                    frames.Add(new { file=phase,scheme=scheme.ToString(),id=source.GetProperty("id").GetString(),state,width=editor.ActualWidth,height=editor.ActualHeight,editor.Rows,editor.LineHeight,text=editor.Text,fontFamily=editor.FontFamily.Source,nativeFont=nativeFont.Name,nativeFontSize=nativeFont.Size,firstLineRect,border=ColorHex(paint.Color),borderOpacity=paint.Opacity,editorOpacity=Descendants<Grid>(editor).Single(e=>e.Name=="Editor").Opacity,scrollHeight=viewport.ExtentHeight,viewportHeight=viewport.ViewportHeight,lineSpacing=paragraph.LineSpacing,lineRule=paragraph.LineSpacingRule.ToString() });
                    await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,phase)); await SaveAsync();
                }
                phase = scheme + "-native-input"; canvas.Children.Clear();
                var model = new TextBox { AcceptsReturn = true, Text = "First" };
                var bound = new BleeckerTextarea { Name="TextareaBoundEditor",Width=280 };
                AutomationProperties.SetName(bound,"Editorial notes");AutomationProperties.SetAutomationId(bound,"TextareaA11y-Editor");
                bound.SetBinding(BleeckerTextarea.TextProperty,new Binding {Source=model,Path=new PropertyPath("Text"),Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});
                Canvas.SetLeft(bound,20);Canvas.SetTop(bound,20);canvas.Children.Add(bound); await AwaitPresentationAsync(page);
                await input.ClickAsync(bound); bound.TextDocument.Selection.SetRange(5,5);
                await input.KeyAsync(0x0D,bound);await AwaitPresentationAsync(bound);
                Require(bound.Text=="First\n"&&Normalize(model.Text)=="First\n","Native Enter lost the user-owned trailing newline.");
                await input.KeyAsync(0x41,bound); await AwaitPresentationAsync(bound);
                Require(bound.Text=="First\na"&&Normalize(model.Text)==bound.Text,"Native multiline edit did not cross consumer two-way binding: "+JsonSerializer.Serialize(new{bound.Text,consumer=model.Text,bindingRetained=bound.GetBindingExpression(BleeckerTextarea.TextProperty)is not null}));
                var editorAfterEnter=bound.Text;var nativeConsumerAfterEnter=model.Text;
                foreach(var shortcut in new ushort[]{0x42,0x49,0x55}){phase=scheme+"-format-shortcut-"+shortcut;await SaveAsync();await input.ModifiedPreviewKeyAsync(0x11,shortcut,bound);}
                var insertion=bound.TextDocument.Selection.CharacterFormat;
                Require(insertion.Bold==FormatEffect.Off&&insertion.Italic==FormatEffect.Off&&insertion.Underline==UnderlineType.None,"Rich formatting shortcuts escaped the plain-text editor: "+JsonSerializer.Serialize(new{insertion.Bold,insertion.Italic,insertion.Underline}));
                Require(bound.ContextFlyout is null&&bound.SelectionFlyout is BleeckerDropdownMenu plainMenu&&plainMenu.Items.Count==6,"Plain-text command surfaces expose the rich editor toolbar.");
                phase=scheme+"-clipboard-payload";await SaveAsync();
                var clipboard=new DataPackage();clipboard.SetText("Pasted\nnotes");clipboard.SetHtmlFormat(HtmlFormatHelper.CreateHtmlFormat("<b>Pasted<br>notes</b>"));Clipboard.SetContent(clipboard);
                phase=scheme+"-clipboard-select";await SaveAsync();await input.ModifiedKeyAsync(0x11,0x41,bound);
                phase=scheme+"-clipboard-paste";await SaveAsync();await input.ModifiedKeyAsync(0x11,0x56,bound);await AwaitPresentationAsync(bound);
                Require(bound.Text=="Pasted\nnotes"&&Normalize(model.Text)==bound.Text&&bound.TextDocument.GetRange(0,6).CharacterFormat.Bold==FormatEffect.Off,"Rich clipboard data escaped Unicode plain-text paste or binding.");
                Require(bound.TextDocument.Selection.StartPosition==bound.Text.Length&&bound.TextDocument.Selection.EndPosition==bound.Text.Length,"Plain-text paste did not collapse its caret after the inserted text.");
                phase=scheme+"-clipboard-undo";await SaveAsync();await input.ModifiedKeyAsync(0x11,0x5A,bound);await AwaitPresentationAsync(bound);Require(bound.Text=="First\na","Plain-text paste did not retain one native undo transaction.");
                await input.ModifiedKeyAsync(0x11,0x41,bound); await input.KeyAsync(0x42,bound); await AwaitPresentationAsync(bound);
                Require(bound.Text=="b"&&model.Text=="b","Native selection replacement failed.");
                await input.ModifiedKeyAsync(0x11,0x5A,bound); await AwaitPresentationAsync(bound);
                Require(bound.Text=="First\na"&&Normalize(model.Text)==bound.Text,"Native undo did not restore both lines.");
                var editableUia=await ProbeTextareaAsync(folder,input.Window,scheme+"-editable");
                Require(editableUia.GetProperty("hasText").GetBoolean()&&!editableUia.GetProperty("readOnly").GetBoolean()&&editableUia.GetProperty("name").GetString()=="Editorial notes","External editable text provider differs.");
                Require(editableUia.GetProperty("resizeName").GetString()=="Resize text area"&&editableUia.GetProperty("resizeType").GetString()=="ControlType.Thumb"&&editableUia.GetProperty("resizeFocusable").GetBoolean(),"Resize grip lost its named, focusable external automation provider.");
                bound.IsReadOnly=true; await input.ModifiedKeyAsync(0x11,0x41,bound); await input.KeyAsync(0x43,bound); await AwaitPresentationAsync(bound);
                Require(bound.Text=="First\na"&&bound.TextDocument.Selection.Length>0,"Native read-only selection/editing semantics differ.");
                var readonlyUia=await ProbeTextareaAsync(folder,input.Window,scheme+"-readonly");
                Require(readonlyUia.GetProperty("hasText").GetBoolean()&&readonlyUia.GetProperty("readOnly").GetBoolean()&&readonlyUia.GetProperty("selectionCount").GetInt32()==1,"External read-only text provider differs.");
                bound.IsReadOnly=false;
                var grip=Descendants<Control>(bound).Single(e=>e.Name=="TextareaResizeGrip");
                grip.Focus(FocusState.Keyboard); await input.KeyAsync(0x28,grip); await AwaitPresentationAsync(bound);
                Require(bound.ActualHeight==132&&bound.ActualWidth==280,"Keyboard vertical resize changed width or wrong height.");
                await input.KeyAsync(0x26,grip); await AwaitPresentationAsync(bound); Require(bound.ActualHeight==112,"Resize minimum differs.");
                var template=bound.Template;bound.Template=null;bound.ApplyTemplate();bound.Template=template;bound.ApplyTemplate();await AwaitPresentationAsync(bound);
                Require(bound.Text=="First\na"&&Normalize(model.Text)==bound.Text,"Template replacement reset consumer text.");
                canvas.Children.Remove(bound);await AwaitPresentationAsync(page);canvas.Children.Add(bound);await AwaitPresentationAsync(bound);
                model.Text="Consumer update\n";await AwaitPresentationAsync(bound);Require(Normalize(bound.Text)=="Consumer update\n"&&Normalize(model.Text)=="Consumer update\n","Remount lost binding or the requested trailing newline.");
                bound.IsReadOnly=true;model.Text="Read-only consumer update\n";await AwaitPresentationAsync(bound);
                bound.TextDocument.GetText(TextGetOptions.AllowFinalEop|TextGetOptions.UseLf,out var readonlyDocument);
                Require(Normalize(bound.Text)=="Read-only consumer update\n"&&readonlyDocument=="Read-only consumer update\n\n"&&((RichEditBox)bound).IsReadOnly,"Remounted read-only document lost its consumer update, final newline or native guard.");
                interactions.Add(new{scheme=scheme.ToString(),nativeEnter=true,selectionReplacement=true,undo=true,readOnlySelection=true,twoWayBinding=true,keyboardResize=true,templateReplacement=true,unloadRemount=true,trailingNewline=true,formattingShortcutsBlocked=true,plainTextCommandSurfaces=true,richClipboardRejected=true,pasteUndo=true,editorAfterEnter,nativeConsumerAfterEnter,editableUia,readonlyUia}); await SaveAsync();
                await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,$"catalog-{scheme.ToString().ToLowerInvariant()}.png"));
            }
            passed=true;
        }
        finally { input?.Dispose();Content=original;await AwaitPresentationAsync(CatalogRoot);await SaveAsync(); }
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="textarea",environment=RuntimeEnvironment()}));
        Task SaveAsync()=>File.WriteAllTextAsync(Path.Combine(folder,"textarea.json"),JsonSerializer.Serialize(new{passed,phase,ownerProcess=Environment.ProcessId,originalContentRestored=ReferenceEquals(Content,original),frames,interactions},new JsonSerializerOptions{WriteIndented=true}));
        static bool Flag(JsonElement element,string key)=>element.TryGetProperty(key,out var value)&&value.GetBoolean();
        static string Normalize(string value)=>value.Replace("\r\n","\n").Replace('\r','\n');
        static IEnumerable<T> Descendants<T>(DependencyObject parent)where T:DependencyObject
        { for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++){var child=VisualTreeHelper.GetChild(parent,i);if(child is T value)yield return value;foreach(var nested in Descendants<T>(child))yield return nested;} }
    }

    private static async Task<JsonElement> ProbeTextareaAsync(string folder,IntPtr window,string id)
    {
        var executable=Path.Combine(folder,"TextareaUiaProbe.exe");
        if(!File.Exists(executable))
        {
            var framework=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"Microsoft.NET/Framework64/v4.0.30319");
            var compile=new ProcessStartInfo(Path.Combine(framework,"csc.exe")){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true};
            foreach(var argument in new[]{"/nologo","/target:exe","/out:"+executable,"/reference:"+Path.Combine(framework,"WPF/UIAutomationClient.dll"),"/reference:"+Path.Combine(framework,"WPF/UIAutomationTypes.dll"),"/reference:"+Path.Combine(framework,"WPF/WindowsBase.dll"),"/reference:"+Path.Combine(framework,"System.Runtime.Serialization.dll"),Path.Combine(AppContext.BaseDirectory,"TextareaUiaProbe.cs")})compile.ArgumentList.Add(argument);
            using var compiler=Process.Start(compile)??throw new InvalidOperationException("Could not start Textarea UIA compiler.");
            var messages=compiler.StandardOutput.ReadToEndAsync();var errors=compiler.StandardError.ReadToEndAsync();await compiler.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Require(compiler.ExitCode==0,"Textarea UIA compiler failed: "+await messages+await errors);
        }
        var output=Path.Combine(folder,"uia-"+id+".json");
        var start=new ProcessStartInfo(executable){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true};
        foreach(var argument in new[]{window.ToInt64().ToString(),Environment.ProcessId.ToString(),output})start.ArgumentList.Add(argument);
        using var process=Process.Start(start)??throw new InvalidOperationException("Could not start external Textarea UIA client.");
        var stderr=process.StandardError.ReadToEndAsync();await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        Require(process.ExitCode==0,"External Textarea UIA failed: "+await stderr);
        using var tree=JsonDocument.Parse(await File.ReadAllTextAsync(output));
        Require(tree.RootElement.GetProperty("process").GetInt32()==Environment.ProcessId&&tree.RootElement.GetProperty("clientProcess").GetInt32()!=Environment.ProcessId,"Textarea UIA did not cross the owned process boundary.");
        return tree.RootElement.Clone();
    }
}
