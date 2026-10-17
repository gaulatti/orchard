using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Text;
using Orchard;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private async Task VerifyTextareaAsync()
    {
        PackageTextarea.IsReadOnly=false;PackageTextarea.InputSize=BleeckerInputSize.Medium;PackageTextarea.Rows=4;PackageTextarea.ClearValue(FrameworkElement.HeightProperty);
        PackageTextarea.Text="Independent notes\nSecond line";Root.UpdateLayout();
        await AwaitAsync(()=>PackageTextarea.ActualHeight==112,"Packaged Textarea default rows sizing differs.");
        var format=PackageTextarea.TextDocument.GetRange(0,0);
        Require(format.ParagraphFormat.LineSpacingRule==LineSpacingRule.Exactly&&format.ParagraphFormat.LineSpacing==15,"Packaged multiline line height differs.");
        Require(format.CharacterFormat.Name==PackageTextarea.FontFamily.Source&&format.CharacterFormat.Name.Contains("EncodeSans-Regular.ttf#Encode Sans"),"Packaged Textarea lost the private font asset binding.");
        PackageTextarea.IsReadOnly=true;PackageTextarea.Text="Read-only consumer update\n";
        await AwaitAsync(()=>PackageTextarea.Text.EndsWith('\n'),"Packaged read-only consumer update failed.");
        PackageTextarea.TextDocument.GetText(TextGetOptions.None,out var nativeText);
        Require(nativeText.Replace("\r\n","\n").Replace('\r','\n')==PackageTextarea.Text+"\n"&&((RichEditBox)PackageTextarea).IsReadOnly,"Packaged read-only document or native guard differs.");
        PackageTextarea.InputSize=BleeckerInputSize.Large;PackageTextarea.Rows=10;Root.UpdateLayout();
        await AwaitAsync(()=>PackageTextarea.ActualHeight==266,"Packaged large ten-row sizing differs.");
        PackageTextarea.IsReadOnly=false;PackageTextarea.InputSize=BleeckerInputSize.Medium;PackageTextarea.Rows=4;
        var consumer=new TextBlock{Text="Consumer notes"};
        PackageTextarea.SetBinding(BleeckerTextarea.TextProperty,new Binding{Source=consumer,Path=new PropertyPath("Text"),Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});
        PackageTextarea.StartBringIntoView(new BringIntoViewOptions{AnimationDesired=false});await AwaitPaginationPresentationAsync(PackageTextarea);
        Require(PackageTextarea.Focus(FocusState.Keyboard),"Packaged Textarea refused native focus.");
        PackageTextarea.TextDocument.Selection.SetRange(0,PackageTextarea.Text.Length);
        await PaginationConsumerKeyAsync(0x41,PackageTextarea,PackageTextarea,()=>{});
        await AwaitAsync(()=>PackageTextarea.Text=="a"&&consumer.Text=="a","Packaged native editing lost consumer two-way binding.");
        PackageTextarea.ClearValue(BleeckerTextarea.TextProperty);PackageTextarea.Text="Independent notes\nSecond line";
    }
}
