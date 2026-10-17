using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Orchard;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private async Task VerifyTextareaAsync()
    {
        var parent=PackageTextarea.Parent as Panel??throw new InvalidOperationException("Packaged Textarea lost its consumer parent.");
        var position=parent.Children.IndexOf(PackageTextarea);
        parent.Children.Remove(PackageTextarea);
        await AwaitAsync(()=>!PackageTextarea.IsLoaded,"Packaged Textarea did not unmount before visible verification.");
        var host=new Border{Width=320,Margin=new Thickness(32),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Background=Root.Background,Child=PackageTextarea};
        Root.Children.Add(host);
        try
        {
            await AwaitTextareaPresentationAsync();
            await VerifyVisibleTextareaAsync();
        }
        finally
        {
            Root.Children.Remove(host);
            await AwaitAsync(()=>!PackageTextarea.IsLoaded,"Visible Textarea did not unmount before restoration.");
            host.Child=null;parent.Children.Insert(position,PackageTextarea);
            await AwaitAsync(()=>PackageTextarea.IsLoaded,"Packaged Textarea did not restore its consumer parent.");
        }
    }

    private async Task VerifyVisibleTextareaAsync()
    {
        PackageTextarea.IsReadOnly=false;PackageTextarea.InputSize=BleeckerInputSize.Medium;PackageTextarea.Rows=4;PackageTextarea.ClearValue(FrameworkElement.HeightProperty);
        PackageTextarea.Text="Independent notes\nSecond line";Root.UpdateLayout();
        await AwaitAsync(()=>PackageTextarea.ActualHeight==112,"Packaged Textarea default rows sizing differs.");
        var format=PackageTextarea.TextDocument.GetRange(0,0);
        Require(format.ParagraphFormat.LineSpacingRule==LineSpacingRule.Exactly&&format.ParagraphFormat.LineSpacing==15,"Packaged multiline line height differs.");
        Require(format.CharacterFormat.Name==PackageTextarea.FontFamily.Source&&format.CharacterFormat.Name.Contains("EncodeSans-Regular.ttf#Encode Sans"),"Packaged Textarea lost the private font asset binding.");
        PackageTextarea.IsReadOnly=true;PackageTextarea.Text="Read-only consumer update\n";
        try { await AwaitAsync(()=>
        {
            PackageTextarea.TextDocument.GetText(TextGetOptions.AllowFinalEop|TextGetOptions.UseLf,out var value);
            return value=="Read-only consumer update\n\n"&&PackageTextarea.Text=="Read-only consumer update\n"&&((RichEditBox)PackageTextarea).IsReadOnly;
        },"Packaged read-only document did not receive the exact requested value and trailing newline."); }
        catch(Exception error)
        {
            PackageTextarea.TextDocument.GetText(TextGetOptions.AllowFinalEop|TextGetOptions.UseLf,out var actual);
            object? State(string name)=>typeof(BleeckerTextarea).GetField(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.GetValue(PackageTextarea)??typeof(BleeckerTextarea).GetProperty(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.GetValue(PackageTextarea);
            throw new InvalidOperationException("Packaged Textarea document state: "+System.Text.Json.JsonSerializer.Serialize(new{PackageTextarea.Text,actual,PackageTextarea.IsLoaded,logicalReadOnly=PackageTextarea.IsReadOnly,nativeReadOnly=((RichEditBox)PackageTextarea).IsReadOnly,editorReady=State("editorReady"),synchronizingText=State("synchronizingText"),documentEditDepth=State("documentEditDepth"),contentVersion=State("contentVersion")}),error);
        }
        PackageTextarea.TextDocument.GetText(TextGetOptions.AllowFinalEop|TextGetOptions.UseLf,out var nativeText);
        PackageTextarea.TextDocument.GetText(TextGetOptions.AllowFinalEop,out var nativeWithFinal);
        Require(nativeText=="Read-only consumer update\n\n"&&PackageTextarea.Text=="Read-only consumer update\n"&&((RichEditBox)PackageTextarea).IsReadOnly,"Packaged read-only document or native guard differs: "+System.Text.Json.JsonSerializer.Serialize(new{PackageTextarea.Text,nativeText,nativeWithFinal,logicalReadOnly=PackageTextarea.IsReadOnly,nativeReadOnly=((RichEditBox)PackageTextarea).IsReadOnly}));
        PackageTextarea.InputSize=BleeckerInputSize.Large;PackageTextarea.Rows=10;Root.UpdateLayout();
        await AwaitAsync(()=>PackageTextarea.ActualHeight==266,"Packaged large ten-row sizing differs.");
        PackageTextarea.IsReadOnly=false;PackageTextarea.InputSize=BleeckerInputSize.Medium;PackageTextarea.Rows=4;
        var consumer=new TextBlock{Text="Consumer notes"};
        PackageTextarea.SetBinding(BleeckerTextarea.TextProperty,new Binding{Source=consumer,Path=new PropertyPath("Text"),Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});
        await AwaitTextareaPresentationAsync();await AcquireConsumerInputAsync();
        Require(PackageTextarea.Focus(FocusState.Keyboard),"Packaged Textarea refused native focus.");
        PackageTextarea.TextDocument.Selection.SetRange(0,PackageTextarea.Text.Length);
        await PaginationConsumerKeyAsync(0x41,PackageTextarea,PackageTextarea,()=>{});
        await AwaitAsync(()=>PackageTextarea.Text=="a"&&consumer.Text=="a","Packaged native editing lost consumer two-way binding.");
        await VerifyTextareaContextAsync();
        PackageTextarea.ClearValue(BleeckerTextarea.TextProperty);PackageTextarea.Text="Independent notes\nSecond line";
    }

    private async Task AwaitTextareaPresentationAsync()
    {
        var completion=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);Rect? previous=null;var stable=0;
        void Rendering(object? sender,object args)
        {
            var box=PackageTextarea.TransformToVisual(Root).TransformBounds(new Rect(0,0,PackageTextarea.ActualWidth,PackageTextarea.ActualHeight));
            var visible=PackageTextarea.IsLoaded&&box.Width>0&&box.Height>0&&box.Left>=0&&box.Top>=0&&box.Right<=Root.ActualWidth&&box.Bottom<=Root.ActualHeight;
            stable=visible&&previous==box?stable+1:0;previous=box;if(stable>=2)completion.TrySetResult();
        }
        CompositionTarget.Rendering+=Rendering;
        try{await completion.Task.WaitAsync(TimeSpan.FromSeconds(2));}
        finally{CompositionTarget.Rendering-=Rendering;}
    }
}
