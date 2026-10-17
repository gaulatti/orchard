using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Windows.ApplicationModel.DataTransfer;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task VerifyDeferredTextareaPasteAsync(Canvas canvas,Page page,NativeInputSession input,ElementTheme scheme,int width,string folder)
    {
        var cases=new List<object>();var phase="start";var passed=false;
        try
        {
            foreach(var action in new[]{"limit-decrease","limit-increase","content","selection","readonly","disabled","template","remount","rapid-remount"})
            {
                phase=action;canvas.Children.Clear();
                var model=new TextBox{AcceptsReturn=true,Text="ab"};
                var editor=new BleeckerTextarea{Name="DeferredLengthEditor",Width=width-40,MaxLength=action=="limit-increase"?3:5};
                editor.SetBinding(BleeckerTextarea.TextProperty,new Binding{Source=model,Path=new PropertyPath("Text"),Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});
                Canvas.SetLeft(editor,20);Canvas.SetTop(editor,20);canvas.Children.Add(editor);await AwaitPresentationAsync(editor);
                await input.ClickAsync(editor);editor.TextDocument.Selection.SetRange(2,2);
                var requested=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                DataProviderRequest? pending=null;DataProviderDeferral? deferral=null;
                var data=new DataPackage();data.SetDataProvider(StandardDataFormats.Text,request=>{pending=request;deferral=request.GetDeferral();requested.TrySetResult();});Clipboard.SetContent(data);
                await input.ModifiedKeyAsync(0x11,0x56,editor);await requested.Task.WaitAsync(TimeSpan.FromSeconds(2));
                Require(pending is not null&&deferral is not null,"Deferred clipboard provider was not requested.");
                var operation=typeof(BleeckerTextarea).GetField("pasteOperation",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.GetValue(editor)as Task??throw new InvalidOperationException("Missing pending paste task.");
                Require(!operation.IsCompleted,"Deferred paste completed before its data provider.");
                var expected=action=="limit-decrease"?"abc":action=="limit-increase"?"abcde":action=="content"?"Replacement":"ab";
                try
                {
                    switch(action)
                    {
                        case "limit-decrease":editor.MaxLength=3;break;
                        case "limit-increase":editor.MaxLength=5;break;
                        case "content":model.Text="Replacement";break;
                        case "selection":editor.TextDocument.Selection.SetRange(0,0);break;
                        case "readonly":editor.IsReadOnly=true;break;
                        case "disabled":editor.IsEnabled=false;break;
                        case "template":var template=editor.Template;editor.Template=null;editor.ApplyTemplate();editor.Template=template;editor.ApplyTemplate();break;
                        case "remount":canvas.Children.Remove(editor);await AwaitPresentationAsync(page);canvas.Children.Add(editor);await AwaitPresentationAsync(editor);break;
                        case "rapid-remount":canvas.Children.Remove(editor);canvas.Children.Add(editor);await AwaitPresentationAsync(editor);break;
                    }
                    await AwaitPresentationAsync(page);
                }
                finally{pending!.SetData("cde");deferral!.Complete();}
                // Observe the task started by the real routed paste, including
                // cancellation where unchanged text alone proves nothing.
                await operation.WaitAsync(TimeSpan.FromSeconds(2));
                var clock=Stopwatch.StartNew();var stable=0;
                while(stable<2&&clock.Elapsed<TimeSpan.FromSeconds(2)){await AwaitPresentationAsync(page);stable=editor.Text==expected?stable+1:0;}
                var actual=editor.Text;var consumer=model.Text.Replace("\r\n","\n").Replace('\r','\n');
                Require(stable==2&&actual==expected&&consumer==expected,scheme+" deferred "+action+" changed the wrong content or ignored the current limit: "+actual);
                if(action=="selection")Require(editor.TextDocument.Selection.StartPosition==0,"Canceled clipboard moved the replacement selection.");
                if(action=="readonly")Require(editor.IsReadOnly&&((RichEditBox)editor).IsReadOnly,"Clipboard cancellation released the read-only guard.");
                if(action=="disabled")Require(!editor.IsEnabled,"Clipboard cancellation enabled the editor.");
                cases.Add(new{action,expected,text=actual,consumerText=consumer,editor.MaxLength,editor.IsReadOnly,editor.IsEnabled,providerRequested=requested.Task.IsCompletedSuccessfully,operationCompleted=operation.IsCompletedSuccessfully});
            }
            passed=true;
        }
        finally{await File.WriteAllTextAsync(Path.Combine(folder,$"textarea-deferred-{scheme}.json"),JsonSerializer.Serialize(new{passed,phase,cases},new JsonSerializerOptions{WriteIndented=true}));}
    }
}
