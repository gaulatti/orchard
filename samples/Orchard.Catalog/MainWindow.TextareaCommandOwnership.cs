using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task VerifyTextareaCommandOwnershipAsync(Canvas canvas,Page page,BleeckerTextarea editor,BleeckerDropdownMenu menu,NativeInputSession input,ElementTheme scheme,string folder)
    {
        var phases=new List<string>();var phase="local-null";var passed=false;
        try
        {
            editor.ContextFlyout=null;await input.OpenContextKeyAsync(editor,menu,scheme+"-local-null",false,expectOpen:false);
            Require(!VisualTreeHelper.GetOpenPopupsForXamlRoot(page.XamlRoot).Any(),"Local null did not disable the default context menu.");phases.Add(phase);
            phase="reset-null";editor.ClearValue(UIElement.ContextFlyoutProperty);
            Require(editor.ContextFlyout is null&&editor.ReadLocalValue(UIElement.ContextFlyoutProperty)==DependencyProperty.UnsetValue,"Clearing local null did not restore default context ownership.");phases.Add(phase);
            editor.FlowDirection=FlowDirection.RightToLeft;await AwaitPresentationAsync(editor);editor.TextDocument.Selection.SetRange(0,editor.Text.Length);
            phase="rtl-copy";await input.OpenContextKeyAsync(editor,menu,scheme+"-rtl-copy",true);
            var copy=menu.Items.OfType<BleeckerMenuContentItem>().Single(i=>i.Text=="Copy");await input.ClickMenuItemSourceAsync(copy,scheme+"-rtl-copy");await AwaitPresentationAsync(editor);
            var clipboard=Clipboard.GetContent();var copied=(await clipboard.GetTextAsync()).Replace("\r\n","\n").Replace('\r','\n');
            Require(copied==editor.Text&&!clipboard.Contains(StandardDataFormats.Rtf)&&!clipboard.Contains(StandardDataFormats.Html),"RTL copy changed text or exported rich formatting.");phases.Add(phase);
            phase="consumer-binding";var supplied=new MenuFlyout();supplied.Items.Add(new MenuFlyoutItem{Text="Consumer action"});var model=new Border{Tag=supplied};
            editor.SetBinding(UIElement.ContextFlyoutProperty,new Binding{Source=model,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});
            await input.OpenContextKeyAsync(editor,supplied,scheme+"-consumer-binding",false,expectConstrained:false);
            Require(ReferenceEquals(editor.ContextFlyout,supplied)&&editor.GetBindingExpression(UIElement.ContextFlyoutProperty)is not null&&!menu.IsOpen,"Default menu replaced the consumer's bound flyout.");supplied.Hide();await AwaitPresentationAsync(editor);phases.Add(phase);
            phase="reset-binding";editor.ClearValue(UIElement.ContextFlyoutProperty);
            Require(editor.ContextFlyout is null&&editor.GetBindingExpression(UIElement.ContextFlyoutProperty)is null&&editor.ReadLocalValue(UIElement.ContextFlyoutProperty)==DependencyProperty.UnsetValue,"Clearing the consumer binding did not restore default context ownership.");phases.Add(phase);
            phase="template-close";await input.OpenContextKeyAsync(editor,menu,scheme+"-template",false);
            var template=editor.Template;editor.Template=null;editor.ApplyTemplate();editor.Template=template;editor.ApplyTemplate();await AwaitPresentationAsync(editor);
            Require(!menu.IsOpen,"Template replacement kept the old context menu open.");phases.Add(phase);
            phase="unload-close";await input.OpenContextKeyAsync(editor,menu,scheme+"-unload",false);canvas.Children.Remove(editor);await AwaitPresentationAsync(page);
            Require(!menu.IsOpen,"Unloading the owner kept its context menu open.");canvas.Children.Add(editor);await AwaitPresentationAsync(editor);phases.Add(phase);
            passed=true;
        }
        finally{await File.WriteAllTextAsync(Path.Combine(folder,$"textarea-command-ownership-{scheme}.json"),JsonSerializer.Serialize(new{passed,phase,phases},new JsonSerializerOptions{WriteIndented=true}));}
    }
}
