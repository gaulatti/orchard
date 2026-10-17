using System.Globalization;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;
public sealed partial class MainWindow
{
    private async Task RunAccordionScalarsAsync(string folder)
    {
        using var stream=typeof(MainWindow).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.AccordionScalars.json")??throw new InvalidOperationException("Accordion scalar reference missing.");using var document=await JsonDocument.ParseAsync(stream);
        var original=Content;var canvas=new Canvas();var frames=new List<object>();
        try{
            Content=canvas;await AwaitPresentationAsync(canvas);using var input=new NativeInputSession(canvas.XamlRoot,folder,"accordion-scalars");await input.AcquireAsync(canvas);var width=(int)Math.Round(canvas.XamlRoot.Size.Width);
            object Box(FrameworkElement element){var point=element.TransformToVisual(canvas).TransformPoint(new Point());return new{x=point.X,y=point.Y,width=element.ActualWidth,height=element.ActualHeight};}
            string Text(FrameworkElement element)=>string.Concat(Desc<TextBlock>(element).Where(text=>text.Visibility==Visibility.Visible).Select(text=>text.Text));
            var outside=new Border{Width=10,Height=10,Background=new SolidColorBrush(Microsoft.UI.Colors.Transparent)};Canvas.SetTop(outside,600);
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark}){
                canvas.RequestedTheme=theme;canvas.Background=new SolidColorBrush(theme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));
                foreach(var reference in document.RootElement.GetProperty("cases").EnumerateArray().Where(frame=>frame.GetProperty("width").GetInt32()==width&&frame.GetProperty("scheme").GetString()==theme.ToString())){
                    var scalar=reference.GetProperty("scalar");object? value=scalar.GetProperty("kind").GetString() switch{"null"=>null,"boolean"=>scalar.GetProperty("value").GetString()=="true","number"=>double.Parse(scalar.GetProperty("value").GetString()!,CultureInfo.InvariantCulture),"string"=>scalar.GetProperty("value").GetString(),_=>throw new InvalidOperationException("Unknown scalar kind.")};var item=new BleeckerAccordionItem("first","Scalar sample",AccordionsFixture.Body("Consumer body")){Subtitle="Consumer text"};
                    switch(reference.GetProperty("slot").GetString()){case "title":item.Title=value;break;case "subtitle":item.Subtitle=value;break;case "actions":item.Actions=value;break;case "content":item.Content=value;break;default:throw new InvalidOperationException("Unknown scalar slot.");}
                    var group=new BleeckerAccordion{Width=width-40,Items=[item],DefaultExpandedId="first"};canvas.Children.Clear();Canvas.SetLeft(group,20);Canvas.SetTop(group,20);canvas.Children.Add(group);canvas.Children.Add(outside);await AwaitPresentationAsync(group);await input.MoveAsync(outside);await AwaitPresentationAsync(group);
                    var row=Desc<StackPanel>(group).Single(panel=>panel.Name=="AccordionRow");var button=group.HeaderButtons[0];var header=(FrameworkElement)VisualTreeHelper.GetParent(button);FrameworkElement Slot(string name)=>Desc<FrameworkElement>(group).Single(element=>element.Name==name);var title=Slot("AccordionTitle");var subtitle=Slot("AccordionSubtitle");var actions=Slot("AccordionActions");var body=Slot("AccordionContent");var peer=FrameworkElementAutomationPeer.CreatePeerForElement(button);
                    frames.Add(new{id=reference.GetProperty("id").GetString(),width,scheme=theme.ToString(),measurement=new{box=Box(group),row=Box(row),header=Box(header),button=Box(button),title=new{box=Box(title),text=Text(title)},subtitle=subtitle.Visibility==Visibility.Collapsed?null:new{box=Box(subtitle),text=Text(subtitle)},actions=actions.Visibility==Visibility.Collapsed?null:new{box=Box(actions),text=Text(actions)},body=new{box=Box((FrameworkElement)row.Children[1]),text=Text(body)},name=peer.GetName(),expanded=((IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse)).ExpandCollapseState==Microsoft.UI.Xaml.Automation.ExpandCollapseState.Expanded}});
                    if(reference.GetProperty("file").ValueKind!=JsonValueKind.Null)await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,reference.GetProperty("file").GetString()!));
                }
                await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,"catalog-"+theme.ToString().ToLowerInvariant()+".png"));
            }
            await File.WriteAllTextAsync(Path.Combine(folder,"accordion-scalars.json"),JsonSerializer.Serialize(new{passed=true,frames},new JsonSerializerOptions{WriteIndented=true}));await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="accordion-scalars",environment=RuntimeEnvironment()}));
        }finally{Content=original;await AwaitPresentationAsync(CatalogRoot);}
    }
}
