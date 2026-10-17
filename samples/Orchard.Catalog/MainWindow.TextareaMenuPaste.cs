using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Windows.ApplicationModel.DataTransfer;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task VerifyTextareaMenuPasteAsync(Canvas canvas,NativeInputSession input,ElementTheme scheme,int width,string folder)
    {
        canvas.Children.Clear();var model=new TextBox{AcceptsReturn=true,Text="ab"};
        var editor=new BleeckerTextarea{Name="MenuLengthEditor",Width=width-40,MaxLength=3};
        editor.SetBinding(BleeckerTextarea.TextProperty,new Binding{Source=model,Path=new PropertyPath("Text"),Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});
        Canvas.SetLeft(editor,20);Canvas.SetTop(editor,20);canvas.Children.Add(editor);await AwaitPresentationAsync(editor);
        await input.ClickAsync(editor);editor.TextDocument.Selection.SetRange(2,2);
        var data=new DataPackage();data.SetText("cde");Clipboard.SetContent(data);
        var menu=(BleeckerDropdownMenu)editor.SelectionFlyout;var paste=menu.Items.OfType<BleeckerMenuContentItem>().Single(item=>item.Text=="Paste");
        paste.Name="TextareaMenuPaste";var clicked=false;paste.Click+=(_,_)=>clicked=true;
        menu.ShowAt(editor);await AwaitPresentationAsync(paste);Require(paste.IsEnabled,"Editable menu paste is unavailable.");
        await input.ClickMenuItemSourceAsync(paste,scheme.ToString());
        var operation=typeof(BleeckerTextarea).GetField("pasteOperation",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.GetValue(editor)as Task??throw new InvalidOperationException("Missing menu paste task.");
        await operation.WaitAsync(TimeSpan.FromSeconds(2));await AwaitPresentationAsync(editor);
        var text=editor.Text;var consumer=model.Text;
        Require(clicked&&text=="abc"&&consumer==text&&!menu.IsOpen,"Real menu paste bypassed the limit, binding or menu closure.");
        await input.ClickAsync(editor);await input.ModifiedKeyAsync(0x11,0x5A,editor);await AwaitPresentationAsync(editor);
        Require(editor.Text=="ab"&&model.Text=="ab","Menu paste lost its single undo transaction.");
        editor.IsReadOnly=true;editor.TextDocument.Selection.SetRange(0,2);menu.ShowAt(editor);await AwaitPresentationAsync(paste);
        var readonlyDisabled=menu.Items.OfType<BleeckerMenuContentItem>().Where(item=>item.Text is "Undo" or "Redo" or "Cut" or "Paste").All(item=>!item.IsEnabled);
        var readonlyCopy=menu.Items.OfType<BleeckerMenuContentItem>().Single(item=>item.Text=="Copy").IsEnabled;
        Require(readonlyDisabled&&readonlyCopy,"Read-only menu does not distinguish editing from copying.");menu.Hide();await AwaitPresentationAsync(editor);
        await File.WriteAllTextAsync(Path.Combine(folder,$"textarea-menu-limits-{scheme}.json"),JsonSerializer.Serialize(new{passed=true,clicked,text,consumerText=consumer,undo=editor.Text,readOnlyPasteDisabled=readonlyDisabled,readOnlyCopyEnabled=readonlyCopy}));
    }
}
