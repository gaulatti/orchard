using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;
public sealed partial class MainWindow
{
    private async Task RunAccordionsAsync(string folder)
    {
        using var stream=CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.Accordion.json")??throw new InvalidOperationException("Accordion reference missing.");using var document=await JsonDocument.ParseAsync(stream);
        var original=Content;var canvas=new Canvas();var frames=new List<object>();
        try
        {
            Content=canvas;await AwaitPresentationAsync(canvas);using var input=new NativeInputSession(canvas.XamlRoot,folder,"accordion-reference");await input.AcquireAsync(canvas);var width=(int)Math.Round(canvas.XamlRoot.Size.Width);
            object Box(FrameworkElement element){var point=element.TransformToVisual(canvas).TransformPoint(new Point());return new{x=point.X,y=point.Y,width=element.ActualWidth,height=element.ActualHeight};}
            object? Paint(Brush brush)=>brush is SolidColorBrush solid?new{color=solid.Color.ToString(),opacity=solid.Opacity}:null;
            Button ActionButton(string label,double buttonWidth,double height,double fontSize)=>new(){Content=new TextBlock{Text=label,FontFamily=(FontFamily)Application.Current.Resources["BleeckerPrimaryFontFamily"],FontSize=fontSize,LineHeight=20,LineStackingStrategy=LineStackingStrategy.BlockLineHeight,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center},Width=buttonWidth,Height=height,Style=(Style)Application.Current.Resources["BleeckerAccordionHeaderStyle"]};
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                canvas.RequestedTheme=theme;canvas.Background=new SolidColorBrush(theme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));
                foreach(var frame in document.RootElement.GetProperty("cases").EnumerateArray().Where(frame=>frame.GetProperty("width").GetInt32()==width&&frame.GetProperty("scheme").GetString()==theme.ToString()))
                {
                    var id=frame.GetProperty("id").GetString()!;var spec=frame.GetProperty("spec");var group=new BleeckerAccordion{Width=width-40,Items=AccordionsFixture.Items()};if(spec.TryGetProperty("expanded",out var expanded))group.DefaultExpandedId=expanded.GetString();
                    if(id=="empty")group.Items.Clear();
                    if(id=="long")group.Items[0].Title="A long delivery schedule title that wraps in a constrained header";
                    if(id=="zero-subtitle")group.Items[0].Subtitle=0;
                    if(id is "actions" or "rich")group.Items[0].Actions=ActionButton("Edit",48,24,12);
                    if(id=="rich"){
                        var title=new TextBlock{FontFamily=(FontFamily)Application.Current.Resources["BleeckerPrimaryMediumFontFamily"],FontSize=14,FontWeight=Microsoft.UI.Text.FontWeights.Medium,LineHeight=20,LineStackingStrategy=LineStackingStrategy.BlockLineHeight,TextWrapping=TextWrapping.Wrap};title.Inlines.Add(new Run{Text="Reader "});title.Inlines.Add(new Run{Text="preferences",FontFamily=(FontFamily)Application.Current.Resources["BleeckerPrimarySemiboldFontFamily"],FontWeight=Microsoft.UI.Text.FontWeights.Bold});
                        var subtitle=new TextBlock{FontFamily=(FontFamily)Application.Current.Resources["BleeckerSecondaryFontFamily"],FontSize=12,LineHeight=16,LineStackingStrategy=LineStackingStrategy.BlockLineHeight,TextWrapping=TextWrapping.Wrap};subtitle.Inlines.Add(new Run{Text="Local "});subtitle.Inlines.Add(new Run{Text="choices",FontStyle=Windows.UI.Text.FontStyle.Italic});
                        group.Items[0].Title=title;group.Items[0].Subtitle=subtitle;group.Items[0].Content=new Border{Padding=new Thickness(16),Child=ActionButton("Apply choices",110,28,14)};
                    }
                    canvas.Children.Clear();Canvas.SetLeft(group,20);Canvas.SetTop(group,20);canvas.Children.Add(group);var outside=new Border{Width=20,Height=20,Background=new SolidColorBrush(Microsoft.UI.Colors.Transparent)};Canvas.SetLeft(outside,0);Canvas.SetTop(outside,600);canvas.Children.Add(outside);await AwaitPresentationAsync(group);await input.MoveAsync(outside);if(id=="hover")await input.MoveAsync(group.HeaderButtons[0]);if(id=="focus")Require(group.HeaderButtons[0].Focus(FocusState.Keyboard),"Accordion reference header refused focus.");await AwaitPresentationAsync(group);
                    var rows=Desc<StackPanel>(group).Where(panel=>panel.Name=="AccordionRow").ToArray();var reportRows=new List<object>();
                    for(var index=0;index<rows.Length;index++){
                        var button=group.HeaderButtons[index];var row=rows[index];var header=(FrameworkElement)VisualTreeHelper.GetParent(button);var title=Desc<FrameworkElement>(button).Single(element=>element.Name=="AccordionTitle");var subtitle=Desc<FrameworkElement>(button).Single(element=>element.Name=="AccordionSubtitle");var actions=Desc<FrameworkElement>(header).Single(element=>element.Name=="AccordionActions");var reveal=(FrameworkElement)row.Children[1];var peer=FrameworkElementAutomationPeer.CreatePeerForElement(button);var arrow=Desc<Microsoft.UI.Xaml.Shapes.Path>(button).Single(element=>element.Name=="AccordionChevron");
                        object Text(FrameworkElement slot){var text=Desc<TextBlock>(slot).First(element=>element.Visibility==Visibility.Visible);return new{box=Box(slot),text=text.Text,fontSize=text.FontSize,lineHeight=text.LineHeight,fontFamily=text.FontFamily.Source,foreground=Paint(text.Foreground)};}
                        reportRows.Add(new{box=Box(row),header=Box(header),button=Box(button),expanded=((IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse)).ExpandCollapseState==ExpandCollapseState.Expanded,name=peer.GetName(),title=Text(title),subtitle=subtitle.Visibility==Visibility.Collapsed?null:Text(subtitle),actions=actions.Visibility==Visibility.Collapsed?null:Box(actions),body=Box(reveal),angle=((RotateTransform)arrow.RenderTransform).Angle});
                    }
                    frames.Add(new{id,width,scheme=theme.ToString(),box=Box(group),background=Paint(group.Background),border=Paint(group.BorderBrush),rows=reportRows});await File.WriteAllTextAsync(Path.Combine(folder,"accordion-reference.json"),JsonSerializer.Serialize(new{passed=false,frames}));await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,frame.GetProperty("file").GetString()!));
                }
                await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,"catalog-"+theme.ToString().ToLowerInvariant()+".png"));
            }
            await File.WriteAllTextAsync(Path.Combine(folder,"accordion-reference.json"),JsonSerializer.Serialize(new{passed=true,frames}));await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="accordion-reference",environment=RuntimeEnvironment()}));
        }
        finally{Content=original;await AwaitPresentationAsync(CatalogRoot);}
    }
}
