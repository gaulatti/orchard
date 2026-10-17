using System.Reflection;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunTextareaCommandsAsync(string folder)
    {
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Orchard.Catalog.Reference.TextareaCommands.json")??throw new InvalidOperationException("Missing Textarea commands reference.");using var reference=await JsonDocument.ParseAsync(stream);
        var original=Content;var canvas=new Canvas{Height=600,Background=new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)};var page=new Page{Content=canvas};NativeInputSession? input=null;
        var frames=new List<object>();var routeEvents=new List<object>();var mismatches=new List<string>();var phase="mount";var passed=false;
        try
        {
            Content=page;await AwaitPresentationAsync(page);input=new NativeInputSession(page.XamlRoot,folder,"textarea-commands");await input.AcquireAsync(canvas);
            var width=(int)page.XamlRoot.Size.Width;Require(width is 320 or 640,"Textarea commands requires a 320 or 640 client.");canvas.Width=width;
            var bounds=input.ScreenBounds(canvas);var current=input.ReadPointerPosition();var scale=page.XamlRoot.RasterizationScale;await input.MoveAsync(canvas,true,new Point(current.X==(int)(bounds.X+2*scale)&&current.Y==(int)(bounds.Y+2*scale)?3:2,2));
            foreach(var scheme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                page.RequestedTheme=scheme;canvas.Children.Clear();var seed=new DataPackage();seed.SetText("Command clipboard");Clipboard.SetContent(seed);var model=new TextBox{AcceptsReturn=true,Text="First\nSecond"};var editor=new BleeckerTextarea{Name="CommandEditor",Width=width-40};
                editor.SetBinding(BleeckerTextarea.TextProperty,new Binding{Source=model,Path=new PropertyPath("Text"),Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});Canvas.SetLeft(editor,20);Canvas.SetTop(editor,20);canvas.Children.Add(editor);await AwaitPresentationAsync(editor);await input.ClickAsync(editor);editor.TextDocument.Selection.SetRange(0,0);
                var menu=(BleeckerDropdownMenu)editor.SelectionFlyout;var index=0;
                editor.AddHandler(UIElement.PreviewKeyDownEvent,new Microsoft.UI.Xaml.Input.KeyEventHandler((_,e)=>routeEvents.Add(new{phase,kind="preview-down",key=e.Key.ToString(),e.Handled,menu.IsOpen})),true);
                editor.AddHandler(UIElement.PreviewKeyUpEvent,new Microsoft.UI.Xaml.Input.KeyEventHandler((_,e)=>routeEvents.Add(new{phase,kind="preview-up",key=e.Key.ToString(),e.Handled,menu.IsOpen})),true);
                editor.AddHandler(UIElement.ContextRequestedEvent,new TypedEventHandler<UIElement,Microsoft.UI.Xaml.Input.ContextRequestedEventArgs>((_,e)=>routeEvents.Add(new{phase,kind="context-requested",e.Handled,menu.IsOpen})),true);
                menu.Opening+=(_,_)=>routeEvents.Add(new{phase,kind="opening",menu.IsConstrainedToRootBounds});menu.Opened+=(_,_)=>routeEvents.Add(new{phase,kind="opened",menu.IsConstrainedToRootBounds});menu.Closed+=(_,_)=>routeEvents.Add(new{phase,kind="closed"});
                foreach(var source in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c=>c.GetProperty("width").GetInt32()==width&&c.GetProperty("scheme").GetString()==scheme.ToString()))
                {
                    var id=source.GetProperty("id").GetString()!;phase=scheme+"-"+id;await SaveAsync();
                    if(id=="readonly-copy"){editor.IsReadOnly=true;editor.TextDocument.Selection.SetRange(0,editor.Text.Length);}
                    if(id=="readonly-paste"){var rejected=new DataPackage();rejected.SetText("Rejected");Clipboard.SetContent(rejected);}
                    var start=editor.TextDocument.Selection.StartPosition;var end=editor.TextDocument.Selection.EndPosition;
                    if(!ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(page.XamlRoot),editor)){await input.ClickAsync(editor);editor.TextDocument.Selection.SetRange(start,end);}
                    var contextKind=index++%3;var gesture=contextKind==2?"Right click":contextKind==1?"Shift+F10":"Apps";
                    if(contextKind==2)await input.OpenContextPointerAsync(editor,menu,phase,new Point(editor.Padding.Left+4,editor.Padding.Top+6));
                    else await input.OpenContextKeyAsync(editor,menu,phase,contextKind==1);
                    await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,"menu-"+phase+".png"));
                    var label=id.Replace("readonly-","")switch{"select-all"=>"Select all","copy"=>"Copy","cut"=>"Cut","undo"=>"Undo","redo"=>"Redo","paste"=>"Paste",_=>throw new InvalidOperationException("Unknown command.")};
                    var item=menu.Items.OfType<BleeckerMenuContentItem>().Single(i=>i.Text==label);item.Name="TextareaCommand-"+id;var invoked=false;Microsoft.UI.Xaml.RoutedEventHandler handler=(_,_)=>invoked=true;item.Click+=handler;
                    var shouldInvoke=!id.StartsWith("readonly-",StringComparison.Ordinal)||id=="readonly-copy";Require(item.IsEnabled==shouldInvoke,phase+": wrong command availability.");
                    try{await input.ClickMenuItemSourceAsync(item,phase);}
                    catch
                    {
                        var b=input.ScreenBounds(item);var peer=Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(item);
                        await File.WriteAllTextAsync(Path.Combine(folder,phase+"-ownership.json"),JsonSerializer.Serialize(new{bounds=b,automationBounds=peer?.GetBoundingRectangle(),menu.IsOpen,menu.IsConstrainedToRootBounds,menu.ShouldConstrainToRootBounds,point=NativeScreenCapture.DescribePoint(input.Window,(int)(b.X+b.Width/2),(int)(b.Y+b.Height/2))},new JsonSerializerOptions{WriteIndented=true}));
                        await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,phase+"-failure.png"));throw;
                    }
                    finally{item.Click-=handler;}
                    Require(invoked==shouldInvoke,phase+": wrong command invocation.");if(!shouldInvoke)menu.Hide();
                    if(id=="paste"){var task=typeof(BleeckerTextarea).GetField("pasteOperation",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(editor)as Task??throw new InvalidOperationException("Missing paste operation.");await task.WaitAsync(TimeSpan.FromSeconds(2));}
                    await AwaitPresentationAsync(editor);var data=Clipboard.GetContent();var raw=data.Contains(StandardDataFormats.Text)?await data.GetTextAsync():"";var clipboard=raw.Replace("\r\n","\n").Replace('\r','\n');var text=editor.Text;var consumer=model.Text.Replace("\r\n","\n").Replace('\r','\n');
                    var html=data.Contains(StandardDataFormats.Html);var rtf=data.Contains(StandardDataFormats.Rtf);var expected=source.GetProperty("actual").GetProperty("text").GetString();
                    var focus=editor.FocusState.ToString();var matches=text==expected&&consumer==text&&editor.FocusState!=FocusState.Unfocused;if(id is "copy" or "cut" or "readonly-copy")matches&=clipboard=="First\nSecond"&&!html&&!rtf;
                    if(!matches)mismatches.Add(phase);
                    var file=source.GetProperty("file").GetString()!;await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,file));
                    frames.Add(new{id,width,scheme=scheme.ToString(),text,consumerText=consumer,clipboardRaw=raw,clipboardText=clipboard,html,rtf,invoked,shouldInvoke,gesture,focus,menuConstrained=menu.IsConstrainedToRootBounds,selection=new[]{editor.TextDocument.Selection.StartPosition,editor.TextDocument.Selection.EndPosition},editor.IsReadOnly,matches,file});await SaveAsync();
                }
                await VerifyTextareaCommandOwnershipAsync(canvas,page,editor,menu,input,scheme,folder);
                await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,$"catalog-{scheme.ToString().ToLowerInvariant()}.png"));
            }
            Require(mismatches.Count==0,"Textarea commands differ: "+string.Join(", ",mismatches));passed=true;
        }
        finally{input?.Dispose();Content=original;await AwaitPresentationAsync(CatalogRoot);await SaveAsync();}
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed,component="textarea-commands",environment=RuntimeEnvironment()}));
        Task SaveAsync()=>File.WriteAllTextAsync(Path.Combine(folder,"textarea-commands.json"),JsonSerializer.Serialize(new{passed,phase,mismatches,frames,routeEvents},new JsonSerializerOptions{WriteIndented=true}));
    }
}
