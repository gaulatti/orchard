using System.Globalization;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;
public sealed partial class MainWindow
{
    private async Task RunEmptiesAsync(string folder)
    {
        using var stream=CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.Empty.json")??throw new InvalidOperationException("Empty reference missing.");
        using var document=await JsonDocument.ParseAsync(stream);
        var original=Content;var canvas=new Canvas{UseLayoutRounding=false};var frames=new List<object>();var lifecycle=new List<object>();var completed=false;
        try
        {
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark}){CatalogRoot.RequestedTheme=theme;await SelectFixtureAsync(Empties);await Empties.VerifyAsync();}
            Content=canvas;await AwaitPresentationAsync(canvas);
            using var input=new NativeInputSession(canvas.XamlRoot,folder,"empty");await input.AcquireAsync(canvas);
            object Box(FrameworkElement element){var rect=element.TransformToVisual(canvas).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));return new{x=rect.X,y=rect.Y,width=rect.Width,height=rect.Height};}
            object Paint(Brush value){var brush=(SolidColorBrush)value;return new{r=brush.Color.R,g=brush.Color.G,b=brush.Color.B,a=brush.Color.A,opacity=brush.Opacity};}
            FrameworkElement Part(BleeckerEmpty empty,string name)=>Desc<FrameworkElement>(empty).Single(element=>element.Name==name);
            object? Scalar(JsonElement spec,string prefix)=>spec.GetProperty(prefix+"Kind").GetString() switch{"boolean"=>spec.GetProperty(prefix+"Value").GetString()=="true","number"=>double.Parse(spec.GetProperty(prefix+"Value").GetString()!,CultureInfo.InvariantCulture),"text"=>spec.GetProperty(prefix+"Value").GetString(),_=>throw new InvalidOperationException("Unknown Empty scalar kind.")};
            var outside=new Border{Width=10,Height=10,Background=new SolidColorBrush(Microsoft.UI.Colors.Transparent)};Canvas.SetLeft(outside,620);Canvas.SetTop(outside,600);
            foreach(var reference in document.RootElement.GetProperty("cases").EnumerateArray())
            {
                var theme=Enum.Parse<ElementTheme>(reference.GetProperty("scheme").GetString()!);canvas.RequestedTheme=theme;
                canvas.Background=new SolidColorBrush(theme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));
                var spec=reference.GetProperty("spec");var empty=new BleeckerEmpty{Title=spec.TryGetProperty("title",out var caption)?caption.GetString():"No items",Description=spec.TryGetProperty("description",out var description)?description.GetString():null};
                var block=new EmptyBlock{Width=spec.TryGetProperty("parentWidth",out var parent)?parent.GetDouble():280};block.Children.Add(empty);
                if(spec.TryGetProperty("rtl",out _))empty.FlowDirection=FlowDirection.RightToLeft;
                FrameworkElement? suppliedIcon=null;
                if(spec.TryGetProperty("icon",out var iconKind))switch(iconKind.GetString())
                {
                    case "scalar":empty.Icon=Scalar(spec,"icon");break;
                    case "custom":suppliedIcon=new Border{Width=24,Height=24,Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,180,55,90)),UseLayoutRounding=false};empty.Icon=suppliedIcon;break;
                    case "large":suppliedIcon=new Border{Width=100,Height=80,UseLayoutRounding=false};empty.Icon=suppliedIcon;break;
                    case "null":break;
                    default:throw new InvalidOperationException("Unknown Empty icon kind.");
                }
                if(spec.TryGetProperty("action",out var actionKind))switch(actionKind.GetString())
                {
                    case "scalar":empty.Action=Scalar(spec,"action");break;
                    case "rich":
                        var family=(FontFamily)Application.Current.Resources["BleeckerPrimaryFontFamily"];var label=new TextBlock{FontFamily=family,FontSize=16,FontWeight=Microsoft.UI.Text.FontWeights.Normal};
                        var row=new StackPanel{Orientation=Orientation.Horizontal,UseLayoutRounding=false};row.Children.Add(new Button{Content="Create",Width=new BleeckerReferenceText.IntrinsicWidth().Measure(label,"Create"),FontFamily=family,FontSize=16,FontWeight=Microsoft.UI.Text.FontWeights.Normal,UseLayoutRounding=false,Height=24,MinHeight=0,MinWidth=0,Padding=new(0),BorderThickness=new(0)});row.Children.Add(new TextBox{Text="Retained",FontFamily=family,FontSize=16,UseLayoutRounding=false,Width=80,Height=24,MinHeight=0,MinWidth=0,Padding=new(0),BorderThickness=new(0)});empty.Action=row;break;
                    default:throw new InvalidOperationException("Unknown Empty action kind.");
                }
                if(spec.TryGetProperty("custom",out _)){empty.Padding=new(12,20,12,20);empty.BorderThickness=new(2);empty.CornerRadius=new(6);empty.Spacing=12;empty.Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,245,237,220));}
                canvas.Children.Clear();Canvas.SetLeft(block,20);Canvas.SetTop(block,20);canvas.Children.Add(block);canvas.Children.Add(outside);
                await AwaitPresentationAsync(empty);
                var icon=(BleeckerIconBadge)Part(empty,"EmptyIcon");
                if(spec.TryGetProperty("icon",out var kind)&&kind.GetString()=="large"){suppliedIcon!.SetBinding(Border.BackgroundProperty,new Binding{Source=icon,Path=new PropertyPath("Foreground")});await AwaitPresentationAsync(empty);}
                await input.MoveAsync(outside);await AwaitPresentationAsync(empty);
                var titlePart=Part(empty,"EmptyTitle");var titleText=Desc<TextBlock>(titlePart).Single();var descriptionPart=Part(empty,"EmptyDescription");var actionPart=Part(empty,"EmptyAction");
                var originalIcon=empty.Icon is null?Part(empty,"EmptyDefaultIcon"):empty.Icon as FrameworkElement;
                var peer=FrameworkElementAutomationPeer.CreatePeerForElement(empty);
                frames.Add(new{id=reference.GetProperty("id").GetString(),scheme=theme.ToString(),motion=reference.GetProperty("motion").GetString(),measurement=new{
                    root=new{box=Box(empty),background=Paint(empty.Background),border=Paint(empty.BorderBrush),radius=empty.CornerRadius.TopLeft,borderWidth=empty.BorderThickness.Left},
                    icon=new{box=Box(icon),child=originalIcon is null?null:Box(originalIcon),text=string.Concat(Desc<TextBlock>(icon).Where(text=>text.Visibility==Visibility.Visible).Select(text=>text.Text.Replace('\n',' '))),background=Paint(icon.Background),border=Paint(icon.BorderBrush),color=Paint(icon.Foreground)},
                    body=new{box=Box(Part(empty,"EmptyBody"))},title=new{box=Box(titlePart),text=titleText.Text.Replace('\n',' '),fontSize=titleText.FontSize,lineHeight=titleText.LineHeight,weight=titleText.FontWeight.Weight,heading=AutomationProperties.GetHeadingLevel(titleText).ToString()},
                    description=descriptionPart.Visibility==Visibility.Collapsed?null:new{box=Box(descriptionPart),text=Desc<TextBlock>(descriptionPart).Single().Text.Replace('\n',' ')},action=actionPart.Visibility==Visibility.Collapsed?null:new{box=Box(actionPart)},empty.IsTabStop,name=peer.GetName(),hasInvoke=peer.GetPattern(PatternInterface.Invoke)is not null}});
                await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,reference.GetProperty("file").GetString()!));
            }
            await File.WriteAllTextAsync(Path.Combine(folder,"empty.json"),JsonSerializer.Serialize(new{passed=false,frames,lifecycle},new JsonSerializerOptions{WriteIndented=true}));
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                canvas.RequestedTheme=theme;canvas.Children.Clear();var row=new StackPanel{Orientation=Orientation.Horizontal};var button=new Button{Content="Create",Height=32};var editor=new TextBox{Text="Retained",Width=120,Height=32};row.Children.Add(button);row.Children.Add(editor);
                var empty=new BleeckerEmpty{Width=400,Title="No items",Description="Consumer description.",Action=row};Canvas.SetLeft(empty,20);Canvas.SetTop(empty,20);canvas.Children.Add(empty);canvas.Children.Add(outside);var clicks=0;button.Click+=(_,_)=>clicks++;
                await AwaitPresentationAsync(empty);await input.ClickAsync(button);await input.HeldKeyAsync(0x09,button,()=>editor,()=>AwaitPresentationAsync(empty));Require(ReferenceEquals(FocusManager.GetFocusedElement(canvas.XamlRoot),editor),"Empty interrupted consumer keyboard order.");editor.Text="Typed state";
                foreach(var description in new string?[]{"Updated description.","",null})
                {
                    empty.Title="Updated title";empty.Description=description;empty.Icon=new Border{Width=24,Height=24,Background=new SolidColorBrush(Microsoft.UI.Colors.Red)};await AwaitPresentationAsync(empty);
                    Require(ReferenceEquals(empty.Action,row)&&editor.Text=="Typed state"&&ReferenceEquals(FocusManager.GetFocusedElement(canvas.XamlRoot),editor),"Empty update replaced consumer input or focus.");lifecycle.Add(new{scheme=theme.ToString(),phase="live-content",description,retained=true});
                }
                canvas.Children.Remove(empty);canvas.Children.Add(empty);await AwaitPresentationAsync(empty);await input.ClickAsync(button);Require(clicks==2&&editor.Text=="Typed state","Empty detach lost consumer state or commands.");
                lifecycle.Add(new{scheme=theme.ToString(),phase="detach-input",retained=true});
                empty.MinWidth=200;empty.Padding=new(12,20,12,20);empty.BorderThickness=new(2);await AwaitPresentationAsync(empty);Require(empty.MinWidth==200&&empty.MinHeight==44,"Empty overwrote a consumer minimum or lost its padding minimum.");
                empty.ClearValue(FrameworkElement.MinWidthProperty);Require(empty.MinWidth==28,"Empty did not restore its automatic padding minimum.");
                var style=new Style(typeof(BleeckerEmpty));style.Setters.Add(new Setter(FrameworkElement.MinWidthProperty,180d));empty.Style=style;await AwaitPresentationAsync(empty);Require(empty.MinWidth==180&&ReferenceEquals(empty.Style,style),"Empty overrode a consumer minimum style.");empty.Style=null;
                lifecycle.Add(new{scheme=theme.ToString(),phase="minimum-ownership",retained=true});
                var template=(DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding Label}'/></DataTemplate>");var model=new EmptyModel("Consumer model");empty.Action=null;empty.ActionTemplate=template;empty.Action=model;empty.Icon=null;empty.IconTemplateSelector=new EmptySelector(template);empty.Icon=model;
                await AwaitPresentationAsync(empty);Require(ReferenceEquals(empty.Action,model)&&ReferenceEquals(empty.Icon,model)&&Desc<TextBlock>(empty).Count(text=>text.Text==model.Label)==2,"Empty templates lost their original consumer models.");
                lifecycle.Add(new{scheme=theme.ToString(),phase="templates-models",retained=true});await input.MoveAsync(outside);await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,"catalog-"+theme.ToString().ToLowerInvariant()+".png"));
            }
            await File.WriteAllTextAsync(Path.Combine(folder,"empty.json"),JsonSerializer.Serialize(new{passed=true,frames,lifecycle},new JsonSerializerOptions{WriteIndented=true}));
            await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="empty",environment=RuntimeEnvironment()}));
            completed=true;
        }
        finally{if(!completed)await File.WriteAllTextAsync(Path.Combine(folder,"empty.json"),JsonSerializer.Serialize(new{passed=false,frames,lifecycle},new JsonSerializerOptions{WriteIndented=true}));Content=original;await AwaitPresentationAsync(CatalogRoot);}
    }
    private sealed record EmptyModel(string Label);
    private sealed class EmptySelector(DataTemplate template):DataTemplateSelector
    {
        protected override DataTemplate SelectTemplateCore(object item)=>template;
        protected override DataTemplate SelectTemplateCore(object item,DependencyObject container)=>template;
    }
    // CSS constrains the block parent; its child may keep a larger border box.
    // Giving the child Width=0 instead creates a WinUI layout clip, even when
    // the child's measured ActualWidth correctly includes its padding.
    private sealed class EmptyBlock:Panel
    {
        protected override Size MeasureOverride(Size available)
        {
            var child=Children.Single();child.Measure(new(Width,double.PositiveInfinity));return new(Width,child.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size final)
        {
            var child=(FrameworkElement)Children.Single();child.Arrange(new(0,0,Math.Max(child.MinWidth,child.DesiredSize.Width),Math.Max(child.MinHeight,child.DesiredSize.Height)));return final;
        }
    }
}
