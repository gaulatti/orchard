using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using System.Text.Json;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunTextareaFontProbeAsync(string folder)
    {
        var original=Content;var canvas=new Canvas{Background=new SolidColorBrush(Microsoft.UI.Colors.White)};var page=new Page{Content=canvas,RequestedTheme=ElementTheme.Light};
        var reports=new List<object>();NativeInputSession? input=null;
        try
        {
            Content=page;await AwaitPresentationAsync(page);input=new NativeInputSession(page.XamlRoot,folder,"textarea-font");await input.AcquireAsync(canvas);
            foreach(var id in new[]{"plain-modern","plain-legacy","custom","plain-system","text-box"})
            {
                canvas.Children.Clear();Control editor=id=="text-box"?new TextBox{Text="First line\nSecond line",AcceptsReturn=true}:id=="custom"?new BleeckerTextarea{Text="First line\nSecond line"}:new RichEditBox();
                editor.Width=280;editor.Height=112;editor.FontSize=14;editor.FontFamily=new FontFamily(id=="plain-system"?"Segoe UI":"ms-appx:///Orchard/Assets/Fonts/EncodeSans-Regular.ttf#Encode Sans");editor.Foreground=new SolidColorBrush(Windows.UI.Color.FromArgb(255,26,55,77));editor.Padding=new Thickness(16,12,16,12);
                Canvas.SetLeft(editor,20);Canvas.SetTop(editor,20);canvas.Children.Add(editor);await AwaitPresentationAsync(editor);
                if(editor is RichEditBox rich&&id!="custom")
                {
                    if(id=="plain-legacy")rich.Document.SetText(TextSetOptions.None,"First line\nSecond line");
                    else rich.TextDocument.SetText(TextSetOptions.None,"First line\nSecond line");
                }
                await AwaitPresentationAsync(editor);
                reports.Add(new{id,font=editor.FontFamily.Source,format=editor is RichEditBox box?box.TextDocument.GetRange(0,0).CharacterFormat.Name:null});
                await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,id+".png"));
            }
            await File.WriteAllTextAsync(Path.Combine(folder,"textarea-font.json"),JsonSerializer.Serialize(reports,new JsonSerializerOptions{WriteIndented=true}));
        }
        finally{input?.Dispose();Content=original;await AwaitPresentationAsync(CatalogRoot);}
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="textarea-font",diagnosticOnly=true,environment=RuntimeEnvironment()}));
    }
}
