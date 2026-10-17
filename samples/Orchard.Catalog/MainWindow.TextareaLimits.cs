using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunTextareaLimitsAsync(string folder)
    {
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Orchard.Catalog.Reference.TextareaLimits.json")??throw new InvalidOperationException("Missing Textarea limits reference.");
        using var reference=await JsonDocument.ParseAsync(stream);
        var original=Content;var canvas=new Canvas{Height=600};var page=new Page{Content=canvas};
        NativeInputSession? input=null;var frames=new List<object>();var mismatches=new List<string>();var phase="mount";var passed=false;
        try
        {
            Content=page;await AwaitPresentationAsync(page);input=new NativeInputSession(page.XamlRoot,folder,"textarea-limits");await input.AcquireAsync(canvas);
            var width=(int)page.XamlRoot.Size.Width;Require(width is 320 or 640,"Textarea limits requires a 320 or 640 client.");canvas.Width=width;
            canvas.Background=new SolidColorBrush(Microsoft.UI.Colors.White);
            // The prior session can restore the pointer to our first editor's
            // exact center. Establish a real owned move before that first click;
            // Windows need not emit PointerMoved for an unchanged screen point.
            var bounds=input.ScreenBounds(canvas);var pointer=input.ReadPointerPosition();var scale=page.XamlRoot.RasterizationScale;
            var aside=new Windows.Foundation.Point(2,2);
            if(pointer.X==(int)(bounds.X+2*scale)&&pointer.Y==(int)(bounds.Y+2*scale))aside=new Windows.Foundation.Point(3,2);
            await input.MoveAsync(canvas,true,aside);
            foreach(var source in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c=>c.GetProperty("width").GetInt32()==width))
            {
                var id=source.GetProperty("id").GetString()!;var spec=source.GetProperty("spec");var expected=source.GetProperty("actual");
                var scheme=Enum.Parse<ElementTheme>(source.GetProperty("scheme").GetString()!);phase=scheme+"-"+id;await SaveAsync();
                canvas.Children.Clear();page.RequestedTheme=scheme;canvas.Background=new SolidColorBrush(scheme==ElementTheme.Dark?Windows.UI.Color.FromArgb(255,13,24,33):Microsoft.UI.Colors.White);
                var model=new TextBox{AcceptsReturn=true,Text=spec.GetProperty("initial").GetString()!};
                var editor=new BleeckerTextarea{Name="LengthEditor",Width=width-40,MaxLength=spec.GetProperty("limit").ValueKind==JsonValueKind.Null?0:spec.GetProperty("limit").GetInt32(),IsReadOnly=spec.TryGetProperty("readOnly",out var readOnly)&&readOnly.GetBoolean()};
                editor.SetBinding(BleeckerTextarea.TextProperty,new Binding{Source=model,Path=new PropertyPath("Text"),Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});
                Canvas.SetLeft(editor,20);Canvas.SetTop(editor,20);canvas.Children.Add(editor);await AwaitPresentationAsync(editor);
                var initial=editor.Text;Require(initial==spec.GetProperty("initial").GetString(),phase+": programmatic initial value was truncated.");
                await input.ClickAsync(editor);
                var selection=spec.TryGetProperty("selection",out var range)?range.EnumerateArray().Select(v=>v.GetInt32()).ToArray():new[]{editor.Text.Length,editor.Text.Length};
                editor.TextDocument.Selection.SetRange(selection[0],selection[1]);
                if(spec.TryGetProperty("selection",out _)&&selection[0]==0&&selection[1]==editor.Text.Length)
                    await input.ModifiedKeyAsync(0x11,0x41,editor);
                var nativeSelection=new[]{editor.TextDocument.Selection.StartPosition,editor.TextDocument.Selection.EndPosition};
                if(spec.TryGetProperty("liveLimit",out var live))editor.MaxLength=live.GetInt32();
                var pasteEvents=0;editor.Paste+=(_,_)=>pasteEvents++;
                if(spec.TryGetProperty("keys",out var keys))foreach(var key in keys.EnumerateArray())await input.KeyAsync(key.GetString()=="Enter"?(ushort)0x0D:(ushort)char.ToUpperInvariant(key.GetString()![0]),editor);
                else
                {
                    var clipboard=new DataPackage();clipboard.SetText(spec.GetProperty("paste").GetString()!);Clipboard.SetContent(clipboard);
                    await input.ModifiedKeyAsync(0x11,0x56,editor);
                }
                var watch=Stopwatch.StartNew();var stable=0;
                while(stable<2&&watch.Elapsed<TimeSpan.FromSeconds(2)){await AwaitPresentationAsync(editor);stable=editor.Text==expected.GetProperty("text").GetString()?stable+1:0;}
                var actualText=editor.Text;var actualSelection=new[]{editor.TextDocument.Selection.StartPosition,editor.TextDocument.Selection.EndPosition};
                var modelRaw=model.Text;var consumer=modelRaw.Replace("\r\n","\n").Replace('\r','\n');
                var matches=actualText==expected.GetProperty("text").GetString()&&consumer==actualText&&actualSelection.SequenceEqual(expected.GetProperty("selection").EnumerateArray().Select(v=>v.GetInt32()));
                if(!matches)mismatches.Add(phase);
                var file=source.GetProperty("file").GetString()!;await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,file));
                string? undo=null;string? undoConsumer=null;
                if(spec.TryGetProperty("paste",out _))
                {
                    await input.ModifiedKeyAsync(0x11,0x5A,editor);await AwaitPresentationAsync(editor);
                    undo=editor.Text;undoConsumer=model.Text.Replace("\r\n","\n").Replace('\r','\n');
                    if(undo!=source.GetProperty("undo").GetString()||undoConsumer!=undo){matches=false;mismatches.Add(phase+"-undo");}
                }
                frames.Add(new{id,width,scheme=scheme.ToString(),initial,nativeSelection,text=actualText,length=actualText.Length,selection=actualSelection,consumerText=consumer,modelRaw,editor.MaxLength,editor.IsReadOnly,editor.IsError,editor.IsInvalid,dataValid=AutomationProperties.GetIsDataValidForForm(editor),pasteEvents,matches,undo,undoConsumer,file});await SaveAsync();
                if(id=="readonly")
                {
                    await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,$"catalog-{scheme.ToString().ToLowerInvariant()}.png"));
                    await VerifyDeferredTextareaPasteAsync(canvas,page,input,scheme,width,folder);
                    await VerifyTextareaMenuPasteAsync(canvas,input,scheme,width,folder);
                }
            }
            Require(mismatches.Count==0,"Textarea length/binding/selection differs from frozen source: "+string.Join(", ",mismatches));passed=true;
        }
        finally{input?.Dispose();Content=original;await AwaitPresentationAsync(CatalogRoot);await SaveAsync();}
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed,component="textarea-limits",environment=RuntimeEnvironment()}));
        Task SaveAsync()=>File.WriteAllTextAsync(Path.Combine(folder,"textarea-limits.json"),JsonSerializer.Serialize(new{passed,phase,mismatches,frames},new JsonSerializerOptions{WriteIndented=true}));
    }
}
