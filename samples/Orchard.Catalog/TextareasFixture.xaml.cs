using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class TextareasFixture : UserControl
{
    public TextareasFixture() => InitializeComponent();

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder, Control focusTarget)
    {
        var phases=new List<object>();var phase="mount";var passed=false;
        var originalText=LiveNotes.Text;var originalTheme=RequestedTheme;
        var model=new TextBox{AcceptsReturn=true,Text="Bound notes\nSecond line"};
        using var input=new NativeInputSession(XamlRoot,folder,$"textarea-catalog-{scheme}");
        try
        {
            StartBringIntoView(new BringIntoViewOptions{AnimationDesired=false});
            await MainWindow.AwaitPresentationAsync(this);await input.AcquireAsync(Heading);await input.MoveAsync(Heading);
            LiveNotes.SetBinding(BleeckerTextarea.TextProperty,new Binding{Source=model,Path=new PropertyPath("Text"),Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged});
            foreach(var(size,rows,height)in new[]{(BleeckerInputSize.Small,1,96d),(BleeckerInputSize.Small,4,98d),(BleeckerInputSize.Large,10,266d),(BleeckerInputSize.Medium,10,226d),(BleeckerInputSize.Medium,4,112d)})
            {
                phase=$"geometry-{size}-{rows}";LiveNotes.InputSize=size;LiveNotes.Rows=rows;await MainWindow.AwaitPresentationAsync(LiveNotes);
                var paragraph=LiveNotes.TextDocument.GetRange(0,0).ParagraphFormat;
                Require(LiveNotes.ActualHeight==height&&LiveNotes.LineHeight==(size==BleeckerInputSize.Large?24:20),"Live Textarea rows/size did not reach the source geometry.");
                Require(paragraph.LineSpacingRule==LineSpacingRule.Exactly&&paragraph.LineSpacing==LiveNotes.LineHeight*.75,"Live Textarea retained stale native line spacing.");
                VerifyText("Bound notes\nSecond line");phases.Add(new{phase,size=size.ToString(),rows,height=LiveNotes.ActualHeight,lineHeight=LiveNotes.LineHeight,nativeLineSpacing=paragraph.LineSpacing});
            }
            phase="native-edit";await input.ClickAsync(LiveNotes);LiveNotes.TextDocument.Selection.SetRange(LiveNotes.Text.Length,LiveNotes.Text.Length);
            await input.KeyAsync(0x0D,LiveNotes);await input.KeyAsync(0x41,LiveNotes);await MainWindow.AwaitPresentationAsync(LiveNotes);
            VerifyText("Bound notes\nSecond line\na");phases.Add(new{phase,text=LiveNotes.Text,consumer=Normalize(model.Text)});
            phase="readonly-consumer";LiveNotes.IsReadOnly=true;model.Text="Read-only live update\n";await MainWindow.AwaitPresentationAsync(LiveNotes);
            await input.KeyAsync(0x42,LiveNotes);await MainWindow.AwaitPresentationAsync(LiveNotes);VerifyText("Read-only live update\n");
            Require(((RichEditBox)LiveNotes).IsReadOnly,"Read-only consumer update released its native guard.");phases.Add(new{phase,text=LiveNotes.Text,nativeReadOnly=((RichEditBox)LiveNotes).IsReadOnly});
            LiveNotes.IsReadOnly=false;await input.ClickAsync(focusTarget);await input.MoveAsync(Heading);
            foreach(var theme in new[]{ElementTheme.Dark,ElementTheme.Light})
            {
                phase="live-theme-"+theme;RequestedTheme=theme;await MainWindow.AwaitPresentationAsync(LiveNotes);
                var border=FindBorder();var expected=theme==ElementTheme.Light?"#FFE6D5B8":"#FFFFFFFF";
                // WinUI stores brush opacity at single precision, including
                // values returned through the projected double property.
                var expectedOpacity=theme==ElementTheme.Light?.4f:.15f;
                try{await UntilAsync(()=>Hex(border.BorderBrush)==expected&&border.BorderBrush.Opacity==expectedOpacity&&Hex(border.Background)==(theme==ElementTheme.Light?"#FFFFFFFF":"#FF182533")&&border.Background.Opacity==1,"Live owner theme did not reach Textarea chrome.");}
                catch{phases.Add(new{phase,requested=theme.ToString(),actualTheme=LiveNotes.ActualTheme.ToString(),border=Hex(border.BorderBrush),borderOpacity=border.BorderBrush.Opacity,background=Hex(border.Background),backgroundOpacity=border.Background.Opacity,focus=LiveNotes.FocusState.ToString()});throw;}
                VerifyText("Read-only live update\n");phases.Add(new{phase,border=Hex(border.BorderBrush),borderOpacity=border.BorderBrush.Opacity,background=Hex(border.Background),backgroundOpacity=border.Background.Opacity,actualTheme=LiveNotes.ActualTheme.ToString()});
            }
            phase="validity-override";LiveNotes.IsError=true;LiveNotes.IsInvalid=false;await MainWindow.AwaitPresentationAsync(LiveNotes);
            Require(AutomationProperties.GetIsDataValidForForm(LiveNotes),"Explicit validity override was lost on a live error change.");
            await UntilAsync(()=>Hex(FindBorder().BorderBrush)=="#FFA65D57","Live error did not reach the source rest border.");
            LiveNotes.IsInvalid=null;Require(!AutomationProperties.GetIsDataValidForForm(LiveNotes),"Clearing validity did not restore error-derived accessibility.");
            phases.Add(new{phase,explicitFalseValid=true,clearedErrorInvalid=true,border=Hex(FindBorder().BorderBrush)});
            passed=true;
        }
        finally
        {
            LiveNotes.IsReadOnly=false;LiveNotes.IsError=false;LiveNotes.IsInvalid=null;LiveNotes.InputSize=BleeckerInputSize.Medium;LiveNotes.Rows=4;
            LiveNotes.ClearValue(BleeckerTextarea.TextProperty);LiveNotes.Text=originalText;RequestedTheme=originalTheme;await MainWindow.AwaitPresentationAsync(this);
            await File.WriteAllTextAsync(Path.Combine(folder,$"textarea-catalog-{scheme}.json"),JsonSerializer.Serialize(new{passed,phase,phases,restored=LiveNotes.Text==originalText&&RequestedTheme==originalTheme},new JsonSerializerOptions{WriteIndented=true}));
        }
        await MainWindow.CaptureElementAsync(this,Path.Combine(folder,$"textareas-{scheme}.png"));

        void VerifyText(string expected)
        {
            LiveNotes.TextDocument.GetText(TextGetOptions.AllowFinalEop|TextGetOptions.UseLf,out var native);
            Require(LiveNotes.Text==expected&&Normalize(model.Text)==expected&&native==expected+"\n"&&LiveNotes.GetBindingExpression(BleeckerTextarea.TextProperty)is not null,"Live Textarea update lost its native document or consumer binding.");
        }
        Border FindBorder()=>Descendants(LiveNotes).OfType<Border>().Single(e=>e.Name=="BorderElement");
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject owner)
    {
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(owner);i++){var child=VisualTreeHelper.GetChild(owner,i);yield return child;foreach(var nested in Descendants(child))yield return nested;}
    }
    private static async Task UntilAsync(Func<bool> ready,string message)
    {
        var clock=Stopwatch.StartNew();while(!ready()&&clock.Elapsed<TimeSpan.FromSeconds(2))await Task.Delay(16);Require(ready(),message);
    }
    private static string Hex(Brush value)
    {
        var color=((SolidColorBrush)value).Color;return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }
    private static string Normalize(string value)=>value.Replace("\r\n","\n").Replace('\r','\n');
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
