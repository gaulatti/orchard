using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class StatCardsFixture:UserControl
{
    public StatCardsFixture(){InitializeComponent();Specimen.SparklineData=Data(10,30,20,40);ScalarSlots.Footer=0d;}
    private static ObservableCollection<IReadOnlyDictionary<string,object?>> Data(params double[] values)=>new(values.Select(value=>(IReadOnlyDictionary<string,object?>)new Dictionary<string,object?>{{"v",value}}));
    internal async Task VerifyAsync(BleeckerColorScheme scheme,string folder)
    {
        Require(XamlRoot.RasterizationScale==1&&new Windows.UI.ViewManagement.UISettings().TextScaleFactor==1,"Frozen Stat Card comparison requires display/text scale 1; scaling acceptance remains open.");
        using var input=new NativeInputSession(XamlRoot,folder,$"stat-card-{scheme}");await input.AcquireAsync(Specimen);
        var root=(Grid)XamlRoot.Content;var host=new Grid{Margin=new Thickness(20),Background=root.Background};Grid.SetRowSpan(host,root.RowDefinitions.Count);Grid.SetColumnSpan(host,root.ColumnDefinitions.Count);root.Children.Add(host);
        using var reference=JsonDocument.Parse(typeof(StatCardsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.StatCardLayout.json")!);var reports=new List<object>();
        try
        {
            foreach(var item in reference.RootElement.GetProperty("cases").EnumerateArray().Where(x=>x.GetProperty("scheme").GetString()==scheme.ToString()))
            {
                var id=item.GetProperty("id").GetString()!;var props=item.GetProperty("props");var expected=item.GetProperty("layout");
                var card=new BleeckerStatCard{Title=String(props,"title"),Value=props.TryGetProperty("value",out var value)?value.ValueKind==JsonValueKind.Number?value.GetDouble():value.GetString():null,
                    Description=String(props,"description"),Width=expected.GetProperty("card").GetProperty("width").GetDouble(),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,
                    FlowDirection=item.TryGetProperty("rtl",out var rtl)&&rtl.GetBoolean()?FlowDirection.RightToLeft:FlowDirection.LeftToRight};
                if(props.TryGetProperty("trend",out var trend))card.Trend=new BleeckerStatTrend{Value=String(trend,"value"),Label=String(trend,"label"),Direction=Enum.Parse<BleeckerStatTrendDirection>(String(trend,"direction")!,true)};
                if(props.TryGetProperty("icon",out var icon)&&icon.GetBoolean())card.Icon=new Border{Width=12,Height=12,Background=Paint(166,93,87)};
                if(props.TryGetProperty("footer",out var footer)&&footer.GetBoolean())card.Footer=new Border{Height=24,Background=Paint(185,199,209)};
                if(props.TryGetProperty("customValue",out var custom)&&custom.GetBoolean())card.Value=new Border{Width=80,Height=24,Background=Paint(166,93,87),HorizontalAlignment=HorizontalAlignment.Left};
                if(props.TryGetProperty("sparklineData",out var data))card.SparklineData=new(data.EnumerateArray().Select(row=>(IReadOnlyDictionary<string,object?>)row.EnumerateObject().ToDictionary(p=>p.Name,p=>(object?)p.Value.GetDouble())));
                card.SparklineDataKey=String(props,"sparklineDataKey");if(String(props,"sparklineColor") is string color)card.SparklineColor=new SolidColorBrush(Windows.UI.Color.FromArgb(255,Convert.ToByte(color.Substring(1,2),16),Convert.ToByte(color.Substring(3,2),16),Convert.ToByte(color.Substring(5,2),16)));
                host.Children.Clear();host.Children.Add(card);await AwaitAsync(()=>card.IsLoaded,"Stat Card did not mount.");card.ApplyTemplate();UpdateLayout();await FrameAsync();UpdateLayout();
                var names=new[]{("header","PART_Header"),("lead","PART_Lead"),("title","PART_Title"),("value","PART_ValueHost"),("description","PART_Description"),("trend","PART_TrendHost"),("trendIcon","PART_TrendIcon"),("trendValue","PART_TrendValue"),("trendLabel","PART_TrendLabel"),("icon","PART_Icon"),("sparkline","PART_SparklineHost"),("footer","PART_Footer")};
                var actual=new Dictionary<string,object?>();
                foreach(var (key,name) in names){var part=Part<FrameworkElement>(card,name);actual[key]=Bounds(part,card);}
                reports.Add(new{id,height=card.ActualHeight,expectedHeight=expected.GetProperty("card").GetProperty("height").GetDouble(),actual});
                await File.WriteAllTextAsync(System.IO.Path.Combine(folder,$"stat-card-layout-{scheme}.json"),JsonSerializer.Serialize(reports,new JsonSerializerOptions{WriteIndented=true}));
                Near(card.ActualHeight,expected.GetProperty("card").GetProperty("height").GetDouble(),id+" height");
                foreach(var (key,name) in names)
                {
                    var source=expected.GetProperty(key);var part=Part<FrameworkElement>(card,name);
                    if(source.ValueKind==JsonValueKind.Null){if(key is "description" or "trend" or "icon" or "sparkline" or "footer")Require(part.Visibility==Visibility.Collapsed,$"Stat Card {id} retained absent {key}.");continue;}
                    Check(part,card,source,$"{id} {key}",key is not("trendValue" or "trendLabel"),key);
                }
                foreach(var key in new[]{"title","description","trendValue","trendLabel"})
                {
                    var source=expected.GetProperty(key);if(source.ValueKind==JsonValueKind.Null)continue;var text=Part<TextBlock>(card,"PART_"+char.ToUpperInvariant(key[0])+key[1..]);
                    Require(text.Text==source.GetProperty("renderedText").GetString(),$"Stat Card {id} {key} changed rendered text.");CompareColor(text.Foreground,source.GetProperty("colorRgba"),id+" "+key);
                }
                Require(card.Title==String(props,"title")&&card.Description==String(props,"description"),"Stat Card changed consumer strings.");
                var titlePart=Part<TextBlock>(card,"PART_Title");var valuePart=Part<TextBlock>(card,"PART_Value");
                Require(titlePart.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf")&&titlePart.FontSize==11&&titlePart.LineHeight==16.5&&titlePart.CharacterSpacing==90&&valuePart.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf")&&valuePart.FontSize==30&&valuePart.LineHeight==30&&valuePart.CharacterSpacing==-20,"Stat Card changed the source typography contract.");
                Require(!card.IsTabStop&&FrameworkElementAutomationPeer.CreatePeerForElement(card).GetAutomationControlType()==AutomationControlType.Group,"Stat Card added an interactive wrapper.");
                if(card.SparklineData is not null&&!string.IsNullOrEmpty(card.SparklineDataKey))
                {
                    var spark=Descendants<BleeckerSparkline>(card).Single();Require(ReferenceEquals(spark.Data,card.SparklineData)&&spark.Height==48&&spark.ShowArea,"Stat Card changed consumer Sparkline data or source profile.");
                    var chart=Part<BleeckerLineChart>(spark,"PART_Chart");var plot=Part<Canvas>(chart,"PART_Plot");
                    var curve=expected.GetProperty("curve");await AwaitAsync(()=>plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Count()==(curve.ValueKind==JsonValueKind.Null?0:1),"Stat Card Sparkline did not render.");
                    if(curve.ValueKind!=JsonValueKind.Null)
                    {
                        var path=plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Single();LineChartsFixture.CompareCurve((PathGeometry)path.Data,curve.GetString()!,id);
                        if(card.SparklineColor is not null)Require(ReferenceEquals(path.Stroke,card.SparklineColor),"Stat Card did not preserve the explicit history brush.");
                    }
                }
                if(id is "full" or "default" or "narrow" or "rtl" or "long")await MainWindow.CaptureElementAsync(card,System.IO.Path.Combine(folder,$"stat-card-{id}-{scheme}.png"));
                if(id=="narrow")await VerifyComposedOverflowAsync(input,card,folder,scheme);
            }
            await VerifyContentAsync(host,input,folder,scheme);
            await VerifyTypographyAsync(host,input,folder,scheme);
            await MutationAsync(host,input,folder,scheme);
        }
        finally{root.Children.Remove(host);}
    }
    private async Task MutationAsync(Grid host,NativeInputSession input,string folder,BleeckerColorScheme scheme)
    {
        var incomplete=new BleeckerStatTrend{Value="Consumer value"};var rejected=false;
        try{_=incomplete.Direction;}catch(InvalidOperationException){rejected=true;}Require(rejected,"Stat Card invented a direction for an incomplete consumer trend.");
        rejected=false;try{incomplete.Direction=(BleeckerStatTrendDirection)99;}catch(ArgumentOutOfRangeException){rejected=true;}Require(rejected,"Stat Card accepted an unknown direction.");
        var before=new BleeckerButton{Content="Before statistic"};var action=new BleeckerButton{Content="Review locally"};var after=new BleeckerButton{Content="After statistic"};var clicks=0;action.Click+=(_,_)=>clicks++;
        var trend=new BleeckerStatTrend{Value="+2%",Direction=BleeckerStatTrendDirection.Up};var card=new BleeckerStatCard{Width=320,Title="Live value",Value="12",Trend=trend,Description="Suppressed description",Footer=action,SparklineData=Data(10,30,20,40),SparklineDataKey="v"};
        var stack=new StackPanel{VerticalAlignment=VerticalAlignment.Top};host.Children.Clear();stack.Children.Add(before);stack.Children.Add(card);stack.Children.Add(after);host.Children.Add(stack);
        await AwaitAsync(()=>card.IsLoaded&&after.IsLoaded,"Stat Card mutation fixture did not mount.");UpdateLayout();
        var source=new TextBlock{Text="Changed title"};card.SetBinding(BleeckerStatCard.TitleProperty,new Binding{Source=source,Path=new PropertyPath("Text"),Mode=BindingMode.OneWay});source.Text="Bound title";
        await AwaitAsync(()=>Part<TextBlock>(card,"PART_Title").Text=="BOUND TITLE","Stat Card ignored its title binding.");
        trend.Direction=BleeckerStatTrendDirection.Down;trend.Value="-4%";await AwaitAsync(()=>Part<TextBlock>(card,"PART_TrendValue").Text=="-4%","Stat Card ignored a consumer trend mutation.");
        card.Trend=null;UpdateLayout();Require(Part<TextBlock>(card,"PART_Description").Visibility==Visibility.Visible,"Stat Card did not restore its description when trend was removed.");
        card.SparklineDataKey="";UpdateLayout();Require(!Descendants<BleeckerSparkline>(card).Any(),"Stat Card retained an empty-key Sparkline.");card.SparklineDataKey="v";UpdateLayout();
        await input.AcquireAsync(before);await input.MoveAsync(before);Require(Part<Button>(before,"PART_Action").Focus(FocusState.Keyboard),"Could not focus preceding statistic action.");input.Key(0x09);
        var chart=Part<BleeckerLineChart>(Descendants<BleeckerSparkline>(card).Single(),"PART_Chart");
        try
        {
            await AwaitAsync(()=>ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),chart),"Stat Card did not expose only the child's native inspection stop.");
            await AwaitAsync(()=>AutomationProperties.GetHelpText(chart)==" : 10","Stat Card initial focus did not inspect its first consumer value.");
            await input.KeyAsync(0x27,chart);await AwaitAsync(()=>AutomationProperties.GetHelpText(chart)==" : 30","Stat Card history did not inspect consumer data.");
        }
        catch
        {
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var interaction=typeof(BleeckerLineChart).GetField("interaction",flags)!.GetValue(chart)!;
            await File.WriteAllTextAsync(System.IO.Path.Combine(folder,$"stat-card-history-failure-{scheme}.json"),JsonSerializer.Serialize(new{chart.IsLoaded,chart.FocusState,help=AutomationProperties.GetHelpText(chart),data=chart.Data,series=chart.Series.Select(series=>new{series.Key,series.Name}),input.OwnsForeground,focus=(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot) as FrameworkElement)?.GetType().FullName,state=interaction.GetType().GetFields(flags).ToDictionary(field=>field.Name,field=>field.GetValue(interaction))}));
            throw;
        }
        input.Key(0x09);await AwaitAsync(()=>ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),Part<Button>(action,"PART_Action")),"Stat Card skipped its consumer footer action.");input.Key(0x20);await AwaitAsync(()=>clicks==1,"Stat Card consumer action did not invoke through native Space.");input.ShiftKey(0x09);await AwaitAsync(()=>ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),chart),"Stat Card reverse Tab did not restore history focus.");
        card.IsEnabled=false;UpdateLayout();Require(!Part<Button>(action,"PART_Action").IsEnabled&&!chart.IsEnabled,"Disabled Stat Card left its child controls enabled.");card.IsEnabled=true;
        stack.Children.Remove(card);await AwaitAsync(()=>!card.IsLoaded,"Stat Card did not unmount.");card.Trend=trend;trend.Value="-9%";stack.Children.Insert(1,card);await AwaitAsync(()=>card.IsLoaded&&Part<TextBlock>(card,"PART_TrendValue").Text=="-9%","Stat Card lost changed consumer state after remount.");
        Require(ReferenceEquals(card.Footer,action),"Stat Card replaced consumer footer identity.");
        var customValue=new Border{Width=80,Height=24,Background=Paint(166,93,87)};card.Value=customValue;
        await AwaitAsync(()=>Descendants<Border>(card).Any(element=>ReferenceEquals(element,customValue)),"Stat Card replaced consumer value identity.");
        card.ValueTemplate=(DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><TextBlock Text=\"Template-owned value\" Height=\"24\" /></DataTemplate>");
        var templateValue=new OpaqueValue();card.Value=templateValue;Require(ReferenceEquals(Part<ContentPresenter>(card,"PART_RichValue").Content,templateValue),"Stat Card replaced its template-owned data.");
        await AwaitAsync(()=>Descendants<TextBlock>(Part<ContentPresenter>(card,"PART_RichValue")).Any(text=>text.Text=="Template-owned value"),"Stat Card did not apply a consumer value template.");
        var template=card.Template;card.Template=(ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<ControlTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Grid /></ControlTemplate>");card.ApplyTemplate();UpdateLayout();card.Template=template;card.ApplyTemplate();UpdateLayout();
        await AwaitAsync(()=>Descendants<TextBlock>(Part<ContentPresenter>(card,"PART_RichValue")).Any(text=>text.Text=="Template-owned value"),"Stat Card did not restore its consumer template after template replacement.");
        card.Value=.1f;card.ValueTemplate=null;UpdateLayout();Require(Part<TextBlock>(card,"PART_Value").Text=="0.1","Stat Card widened a floating scalar's display precision.");
        card.Footer=0.000m;UpdateLayout();Require(Part<TextBlock>(card,"PART_FooterScalar").Text=="0"&&Part<Border>(card,"PART_Footer").Visibility==Visibility.Collapsed,"Stat Card changed scaled decimal zero content.");
        card.FooterTemplate=(DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><TextBlock Text=\"Template footer\" Tag=\"{Binding}\" Height=\"24\" /></DataTemplate>");card.Footer=false;
        await AwaitAsync(()=>Descendants<TextBlock>(Part<ContentPresenter>(card,"PART_RichFooter")).Any(text=>text.Text=="Template footer"&&Equals(text.Tag,false)),"Stat Card discarded false data owned by a footer template.");
        Require(Part<Border>(card,"PART_Footer").Visibility==Visibility.Visible&&Part<TextBlock>(card,"PART_FooterScalar").Visibility==Visibility.Collapsed,"Stat Card treated template-owned data as a raw scalar.");
        card.IconTemplate=(DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Border Width=\"12\" Height=\"12\" Tag=\"{Binding}\" /></DataTemplate>");card.Icon=0d;
        await AwaitAsync(()=>Descendants<Border>(Part<ContentPresenter>(card,"PART_RichIcon")).Any(border=>Equals(border.Tag,0d)),"Stat Card discarded zero data owned by an icon template.");
        Require(Part<Border>(card,"PART_Icon").Visibility==Visibility.Visible&&Part<TextBlock>(card,"PART_IconScalar").Visibility==Visibility.Collapsed,"Stat Card replaced its consumer icon template with scalar text.");
    }
    private sealed class OpaqueValue{public override string ToString()=>throw new InvalidOperationException("A consumer template owns this value's rendering.");}
    private async Task VerifyComposedOverflowAsync(NativeInputSession input,BleeckerStatCard card,string folder,BleeckerColorScheme scheme)
    {
        var path=System.IO.Path.Combine(folder,$"stat-card-narrow-composed-{scheme}.png");var client=await NativeScreenCapture.CaptureClientAsync(input.Window,path);
        using var stream=new Windows.Storage.Streams.InMemoryRandomAccessStream();
        using(var writer=new Windows.Storage.Streams.DataWriter(stream)){writer.WriteBytes(await File.ReadAllBytesAsync(path));await writer.StoreAsync();writer.DetachStream();}stream.Seek(0);
        var decoder=await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
        var pixels=(await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,Windows.Graphics.Imaging.BitmapAlphaMode.Straight,new Windows.Graphics.Imaging.BitmapTransform(),Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation,Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
        var lead=input.ScreenBounds(Part<StackPanel>(card,"PART_Lead"));var background=((SolidColorBrush)card.Background).Color;
        var reports=new List<object>();
        foreach(var name in new[]{"PART_Value","PART_TrendLabel"})
        {
            var bounds=input.ScreenBounds(Part<TextBlock>(card,name));var count=0;
            for(var y=(int)Math.Ceiling(bounds.Y-client.Y);y<Math.Floor(bounds.Bottom-client.Y);y++)
                for(var x=(int)Math.Ceiling(lead.Right-client.X)+1;x<Math.Floor(bounds.Right-client.X);x++)
                {
                    var index=(y*(int)decoder.PixelWidth+x)*4;
                    if(Math.Max(Math.Abs(pixels[index]-background.B),Math.Max(Math.Abs(pixels[index+1]-background.G),Math.Abs(pixels[index+2]-background.R)))>20)count++;
                }
            reports.Add(new{name,inkPixelsBeyondLead=count,bounds});
            Require(count>0,$"Stat Card {scheme} {name} is clipped at the leading text column.");
        }
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder,$"stat-card-overflow-{scheme}.json"),JsonSerializer.Serialize(new{passed=true,lead,reports}));
    }
    private static string? String(JsonElement props,string key)=>props.TryGetProperty(key,out var value)?value.GetString():null;
    private static SolidColorBrush Paint(byte r,byte g,byte b)=>new(Windows.UI.Color.FromArgb(255,r,g,b));
    private static void CompareColor(Brush brush,JsonElement rgba,string label){var color=((SolidColorBrush)brush).Color;var channels=rgba.EnumerateArray().Select(x=>x.GetInt32()).ToArray();Require(color.R==channels[0]&&color.G==channels[1]&&color.B==channels[2]&&color.A==channels[3],$"Stat Card {label} paint differs.");}
    private static Rect Bounds(FrameworkElement element,FrameworkElement owner)
    {
        var root=(UIElement)owner.XamlRoot.Content;
        // A DOM paragraph's box is the line allocation, while TextBlock's
        // ActualWidth can be its glyph width. Compare the matching allocation.
        var bounds=element is TextBlock?((UIElement)VisualTreeHelper.GetParent(element)).TransformToVisual(root).TransformBounds(Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(element)):
            element.TransformToVisual(root).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));
        var origin=owner.TransformToVisual(root).TransformBounds(new Rect(0,0,owner.ActualWidth,owner.ActualHeight));return new Rect(bounds.X-origin.X,bounds.Y-origin.Y,bounds.Width,bounds.Height);
    }
    private static void Check(FrameworkElement element,FrameworkElement owner,JsonElement expected,string label,bool width,string key)
    {
        var actual=Bounds(element,owner);
        if(owner.FlowDirection==FlowDirection.RightToLeft&&key=="trendValue")Near(actual.Right,expected.GetProperty("x").GetDouble()+expected.GetProperty("width").GetDouble(),label+" trailing anchor");
        else if(owner.FlowDirection==FlowDirection.RightToLeft&&key=="trendLabel")Near(Bounds(Part<TextBlock>(owner,"PART_TrendValue"),owner).Left-actual.Right,8,label+" RTL gap");
        else Near(actual.X,expected.GetProperty("x").GetDouble(),label+" x");
        Near(actual.Y,expected.GetProperty("y").GetDouble(),label+" y");if(width)Near(actual.Width,expected.GetProperty("width").GetDouble(),label+" width");Near(actual.Height,expected.GetProperty("height").GetDouble(),label+" height");
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject owner)where T:DependencyObject{for(var i=0;i<VisualTreeHelper.GetChildrenCount(owner);i++){var child=VisualTreeHelper.GetChild(owner,i);if(child is T match)yield return match;foreach(var nested in Descendants<T>(child))yield return nested;}}
    private static T Part<T>(DependencyObject owner,string name)where T:FrameworkElement=>Descendants<T>(owner).FirstOrDefault(x=>x.Name==name)??throw new InvalidOperationException("Missing Stat Card part "+name);
    private static async Task FrameAsync(){var frame=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);void Presented(object? sender,object args)=>frame.TrySetResult();CompositionTarget.Rendering+=Presented;try{await frame.Task.WaitAsync(TimeSpan.FromSeconds(2));}finally{CompositionTarget.Rendering-=Presented;}}
    private async Task AwaitAsync(Func<bool> ready,string message){var watch=Stopwatch.StartNew();while(!ready()&&watch.Elapsed<TimeSpan.FromSeconds(2)){UpdateLayout();await Task.Delay(16);}Require(ready(),message);}
    private static void Near(double actual,double expected,string label)=>Require(Math.Abs(actual-expected)<.001,$"Stat Card {label} differs: {actual:R}/{expected:R}.");
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
