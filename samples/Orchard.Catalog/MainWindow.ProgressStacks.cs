using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
namespace Orchard.Catalog;
public sealed partial class MainWindow
{
 private async Task RunProgressStacksAsync(string folder)
 {
  using var stream=typeof(MainWindow).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.ProgressStack.json")??throw new InvalidOperationException("ProgressStack reference missing.");using var reference=await JsonDocument.ParseAsync(stream);
  var original=Content;var canvas=new Canvas{UseLayoutRounding=false};var frames=new List<object>();var lifecycle=new List<object>();var completed=false;
  object Box(FrameworkElement element){var rect=element.TransformToVisual(canvas).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));return new{x=rect.X,y=rect.Y,width=rect.Width,height=rect.Height};}
  object Paint(Brush? brush)=>brush is SolidColorBrush color?new{r=color.Color.R,g=color.Color.G,b=color.Color.B,a=color.Color.A,opacity=color.Opacity}:new{r=(byte)0,g=(byte)0,b=(byte)0,a=(byte)0,opacity=1d};
  FrameworkElement Part(BleeckerProgressStack control,string name)=>Desc<FrameworkElement>(control).Single(element=>element.Name==name);
  object Text(FrameworkElement element){var text=Desc<TextBlock>(element).Single();return new{box=Box(element),text=text.Text.Replace('\n',' '),color=Paint(text.Foreground),weight=text.FontWeight.Weight,fontSize=text.FontSize,lineHeight=text.LineHeight};}
  object Snapshot(BleeckerProgressStack control)
  {
   var bar=(Border)Part(control,"ProgressStackTrack");var peer=FrameworkElementAutomationPeer.CreatePeerForElement(control);
   return new{root=new{box=Box(control)},bar=new{box=Box(bar),background=Paint(bar.Background)},segments=Desc<Border>(control).Where(part=>part.Name=="ProgressStackSegment").Select(part=>new{box=Box(part),background=Paint(part.Background),title=ToolTipService.GetToolTip(part)?.ToString()}).ToArray(),legend=control.ShowLegend?new{box=Box(Part(control,"ProgressStackLegend"))}:null,items=control.ShowLegend?Desc<Panel>(control).Where(part=>part.Name=="ProgressStackItem").Select(part=>new{box=Box(part),swatch=new{box=Box(Desc<Border>(part).Single()),background=Paint(Desc<Border>(part).Single().Background)},label=Text(Desc<FrameworkElement>(part).Single(child=>child.Name=="ProgressStackLabel")),value=Text(Desc<FrameworkElement>(part).Single(child=>child.Name=="ProgressStackValue"))}).ToArray():[],control.IsTabStop,name=peer.GetName(),hasInvoke=peer.GetPattern(PatternInterface.Invoke)is not null,hasRange=peer.GetPattern(PatternInterface.RangeValue)is not null};
  }
  static Brush? Color(JsonElement item)
  {
   if(!item.TryGetProperty("color",out var field))return null;var value=field.GetString()!;
   if(value.StartsWith('#'))return new SolidColorBrush(Windows.UI.Color.FromArgb(255,byte.Parse(value[1..3],NumberStyles.HexNumber),byte.Parse(value[3..5],NumberStyles.HexNumber),byte.Parse(value[5..7],NumberStyles.HexNumber)));
   var parts=Regex.Matches(value,"[0-9.]+").Select(match=>double.Parse(match.Value,CultureInfo.InvariantCulture)).ToArray();if(parts.Length!=4)throw new InvalidOperationException("Unknown source brush.");return new SolidColorBrush(Windows.UI.Color.FromArgb(255,(byte)parts[0],(byte)parts[1],(byte)parts[2])){Opacity=parts[3]};
  }
  try
  {
   foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark}){CatalogRoot.RequestedTheme=theme;await SelectFixtureAsync(ProgressStacks);await ProgressStacks.VerifyAsync();}
   Content=canvas;await AwaitPresentationAsync(canvas);using var input=new NativeInputSession(canvas.XamlRoot,folder,"progress-stack");await input.AcquireAsync(canvas);
   var outside=new Border{Width=10,Height=10,Background=new SolidColorBrush(Microsoft.UI.Colors.Transparent)};Canvas.SetLeft(outside,620);Canvas.SetTop(outside,380);
   foreach(var row in reference.RootElement.GetProperty("cases").EnumerateArray())
   {
    var theme=Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!);canvas.RequestedTheme=theme;canvas.Background=new SolidColorBrush(theme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));var spec=row.GetProperty("spec");var control=new BleeckerProgressStack{Width=spec.TryGetProperty("width",out var width)?width.GetDouble():280};
    foreach(var item in spec.GetProperty("items").EnumerateArray())control.Items.Add(new(){Label=item.GetProperty("label").GetString()!,Value=double.Parse(item.GetProperty("value").GetString()!,CultureInfo.InvariantCulture),Color=Color(item)});
    if(spec.TryGetProperty("max",out var max))control.Max=double.Parse(max.GetString()!,CultureInfo.InvariantCulture);if(spec.TryGetProperty("showLegend",out var show))control.ShowLegend=show.GetBoolean();if(spec.TryGetProperty("rtl",out _))control.FlowDirection=FlowDirection.RightToLeft;if(spec.TryGetProperty("custom",out _)){control.TrackHeight=20;control.CornerRadius=new(3);control.Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,245,237,220));}
    canvas.Children.Clear();Canvas.SetLeft(control,20);Canvas.SetTop(control,20);canvas.Children.Add(control);canvas.Children.Add(outside);await AwaitPresentationAsync(control);await input.MoveAsync(outside);await AwaitPresentationAsync(control);
    if(row.GetProperty("id").GetString()=="nonbreaking-space"&&theme==ElementTheme.Light&&row.GetProperty("motion").GetString()=="reduce")
    {
     var target=Desc<TextBlock>(Desc<FrameworkElement>(control).First(part=>part.Name=="ProgressStackLabel")).Single();var probes=new List<object>();
     foreach(var content in new[]{"Published"," ","\u00a0"," Published ","\u00a0Published\u00a0"}){var text=new TextBlock{FontFamily=target.FontFamily,FontSize=target.FontSize,FontWeight=target.FontWeight,CharacterSpacing=target.CharacterSpacing,Language=target.Language,IsTextScaleFactorEnabled=target.IsTextScaleFactorEnabled};BleeckerReferenceText.Set(text,content);var metric=new BleeckerReferenceText.IntrinsicWidth(joinWhitespace:true);var measuredSpan=metric.Measure(text,content);probes.Add(new{content,width=measuredSpan,metric.WhitespaceJoinedWidth,metric.RawWidth,metric.RegionWidths,metric.GlyphRuns});}
     await File.WriteAllTextAsync(Path.Combine(folder,"font-probes.json"),JsonSerializer.Serialize(probes,new JsonSerializerOptions{WriteIndented=true}));
    }
    frames.Add(new{id=row.GetProperty("id").GetString(),scheme=theme.ToString(),motion=row.GetProperty("motion").GetString(),measurement=Snapshot(control)});await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,row.GetProperty("file").GetString()!));
   }
   foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
   {
    canvas.RequestedTheme=theme;canvas.Children.Clear();var control=new BleeckerProgressStack{Width=280};foreach(var item in ProgressStacksFixture.Sample())control.Items.Add(item);Canvas.SetLeft(control,20);Canvas.SetTop(control,20);canvas.Children.Add(control);var editor=new TextBox{Width=120,Height=32,Text="Retained"};Canvas.SetTop(editor,200);canvas.Children.Add(editor);var tail=new Button{Content="Next",Width=80,Height=32};Canvas.SetTop(tail,250);canvas.Children.Add(tail);await AwaitPresentationAsync(control);await input.ClickAsync(editor);await input.HeldKeyAsync(0x09,editor,()=>tail,()=>AwaitPresentationAsync(control));Require(ReferenceEquals(FocusManager.GetFocusedElement(canvas.XamlRoot),tail),"ProgressStack entered keyboard order.");
    editor.Focus(FocusState.Keyboard);var retained=control.Items[0];var brush=new SolidColorBrush(Microsoft.UI.Colors.Red);retained.Label="Updated";retained.Value=8;retained.Color=brush;await AwaitPresentationAsync(control);Require(ReferenceEquals(control.Items[0],retained)&&ReferenceEquals(Desc<Border>(control).First(part=>part.Name=="ProgressStackSegment").Background,brush)&&ReferenceEquals(FocusManager.GetFocusedElement(canvas.XamlRoot),editor),"ProgressStack live data lost ownership or focus.");lifecycle.Add(new{scheme=theme.ToString(),phase="live-item",retained=true});
    var old=control.Items;control.Items=new ObservableCollection<BleeckerProgressStackItem>(old.Reverse());await AwaitPresentationAsync(control);old.Clear();retained.Value=9;await AwaitPresentationAsync(control);Require(control.Items.Count==3&&ReferenceEquals(control.Items[^1],retained),"ProgressStack observed a replaced collection or lost an item.");retained.Value=0;retained.Value=-0d;Require(BitConverter.DoubleToInt64Bits(retained.Value)==long.MinValue,"ProgressStack replaced the original negative zero.");lifecycle.Add(new{scheme=theme.ToString(),phase="replace-reorder",retained=true});
    control.ShowLegend=false;await AwaitPresentationAsync(control);Require(control.ActualHeight==12,"ProgressStack retained a hidden legend gap.");control.Items.Clear();control.ShowLegend=true;await AwaitPresentationAsync(control);Require(control.ActualHeight==24,"ProgressStack lost its empty legend spacing.");lifecycle.Add(new{scheme=theme.ToString(),phase="hide-clear",retained=true});
    control.Items.Add(retained);canvas.Children.Remove(control);retained.Value=12;canvas.Children.Add(control);await AwaitPresentationAsync(control);Require(control.Items.Single().Value==12&&editor.Text=="Retained","ProgressStack remount lost data.");lifecycle.Add(new{scheme=theme.ToString(),phase="detach-remount",retained=true});
    var template=control.Template;control.Template=null;await AwaitPresentationAsync(canvas);control.Template=template;await AwaitPresentationAsync(control);Require(Desc<Border>(control).Count(part=>part.Name=="ProgressStackSegment")==1,"ProgressStack retemplate duplicated segments.");lifecycle.Add(new{scheme=theme.ToString(),phase="retemplate",retained=true});
    AutomationProperties.SetName(control,"Consumer distribution");control.Max=24;await AwaitPresentationAsync(control);Require(FrameworkElementAutomationPeer.CreatePeerForElement(control).GetName()=="Consumer distribution"&&Part(control,"ProgressStackTrack").ActualWidth==280&&Desc<Border>(control).Single(part=>part.Name=="ProgressStackSegment").ActualWidth==140,"ProgressStack lost its explicit name or maximum update.");var declarative=(BleeckerProgressStack)XamlReader.Load("<orchard:BleeckerProgressStack xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:orchard='using:Orchard' Max='20' />");Require(declarative.Max==20,"ProgressStack XAML maximum was not numeric.");lifecycle.Add(new{scheme=theme.ToString(),phase="name-max",retained=true});await input.MoveAsync(tail);await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,"catalog-"+theme.ToString().ToLowerInvariant()+".png"));
   }
   completed=true;await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="progress-stack",environment=RuntimeEnvironment()}));
  }
  finally{await File.WriteAllTextAsync(Path.Combine(folder,"progress-stack.json"),JsonSerializer.Serialize(new{passed=completed,frames,lifecycle},new JsonSerializerOptions{WriteIndented=true}));Content=original;await AwaitPresentationAsync(CatalogRoot);}
 }
}
