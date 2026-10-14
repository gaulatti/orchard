using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Path=Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard.Catalog;

public sealed partial class SparklinesFixture:UserControl
{
    public SparklinesFixture(){InitializeComponent();Specimen.Data=Data(10,30,20,40);Area.Data=Data(10,30,20,40);Explicit.Data=Data(10,30,20,40);}
    private static ObservableCollection<IReadOnlyDictionary<string,object?>> Data(params double[] values)=>new(values.Select(value=>(IReadOnlyDictionary<string,object?>)new Dictionary<string,object?>{{"v",value}}));
    internal async Task VerifyAsync(BleeckerColorScheme scheme,string folder)
    {
        Require(XamlRoot.RasterizationScale==1&&new Windows.UI.ViewManagement.UISettings().TextScaleFactor==1,"Frozen Sparkline comparison requires display/text scale 1; scaling acceptance remains open.");
        using var input=new NativeInputSession(XamlRoot,folder,$"sparkline-{scheme}");await input.AcquireAsync(Specimen);
        var root=(Grid)XamlRoot.Content;var host=new Grid{Margin=new Thickness(20),Background=root.Background};Grid.SetRowSpan(host,root.RowDefinitions.Count);Grid.SetColumnSpan(host,root.ColumnDefinitions.Count);root.Children.Add(host);
        using var reference=JsonDocument.Parse(typeof(SparklinesFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.SparklineLayout.json")!);
        var reports=new List<object>();
        try
        {
            foreach(var item in reference.RootElement.GetProperty("cases").EnumerateArray().Where(x=>x.GetProperty("scheme").GetString()==scheme.ToString()))
            {
                var id=item.GetProperty("id").GetString()!;var props=item.GetProperty("props");var expected=item.GetProperty("normal");
                var data=new ObservableCollection<IReadOnlyDictionary<string,object?>>(props.GetProperty("data").EnumerateArray().Select(row=>(IReadOnlyDictionary<string,object?>)Read(row)!));
                var spark=new BleeckerSparkline{Data=data,DataKey=props.GetProperty("dataKey").GetString(),Width=expected.GetProperty("width").GetDouble(),Height=expected.GetProperty("height").GetDouble(),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,
                    ShowArea=props.TryGetProperty("showArea",out var area)&&area.GetBoolean()};
                if(props.TryGetProperty("color",out var color))spark.Color=new SolidColorBrush(Paint(color.GetString()!));
                AutomationProperties.SetName(spark,"Consumer compact series");host.Children.Clear();host.Children.Add(spark);
                await AwaitAsync(()=>spark.IsLoaded,"Sparkline did not mount.");UpdateLayout();await FrameAsync();UpdateLayout();
                var chart=Part<BleeckerLineChart>(spark,"PART_Chart");var plot=Part<Canvas>(chart,"PART_Plot");var legend=Part<Canvas>(chart,"PART_Legend");
                var pointerTrace=new List<object>();
                chart.PointerMoved+=(_,args)=>
                {
                    var point=args.GetCurrentPoint(chart);
                    pointerTrace.Add(new{position=point.Position,point.Timestamp,device=args.Pointer.PointerDeviceType.ToString(),screen=input.ReadPointerPosition(),bounds=input.ScreenBounds(chart),original=(args.OriginalSource as FrameworkElement)?.Name});
                };
                var sourceCurves=expected.GetProperty("lines").EnumerateArray().ToArray();
                await AwaitAsync(()=>plot.Children.OfType<Path>().Count()==sourceCurves.Length,"Sparkline curve count did not settle.");
                var paths=plot.Children.OfType<Path>().ToArray();
                Require(ReferenceEquals(chart.Data,data)&&chart.Series.Single().Key==spark.DataKey,"Sparkline did not consume the original records and key.");
                Require(!spark.IsTabStop&&chart.IsTabStop&&AutomationProperties.GetName(chart)=="Consumer compact series"&&FrameworkElementAutomationPeer.CreatePeerForElement(chart).GetClassName()==nameof(BleeckerSparkline),"Sparkline did not preserve its native inspection/naming boundary.");
                Require(!plot.Children.OfType<TextBlock>().Any()&&legend.Children.Count==0,"Sparkline rendered axes or legend.");
                for(var i=0;i<paths.Length;i++)
                {
                    LineChartsFixture.CompareCurve((PathGeometry)paths[i].Data,sourceCurves[i].GetProperty("d").GetString()!,id);
                    Require(paths[i].StrokeThickness==2&&paths[i].Fill is null&&sourceCurves[i].GetProperty("fill").GetString()=="none","Sparkline changed the pinned source's unfilled 2px line.");
                    ComparePaint(paths[i].Stroke,sourceCurves[i].GetProperty("stroke").GetString()!,id);
                }
                if(id is "default" or "area" or "explicit" or "gaps" or "negative" or "narrow")await MainWindow.CaptureElementAsync(host,System.IO.Path.Combine(folder,$"sparkline-{id}-{scheme}.png"));
                try
                {
                    Require(chart.Focus(FocusState.Keyboard),"Could not focus Sparkline inspection.");
                    await VerifyInspectionAsync(chart,item.GetProperty("focus"),id+" focus");
                    await input.KeyAsync(0x27,chart);
                    await VerifyInspectionAsync(chart,item.GetProperty("right"),id+" right");
                }
                catch
                {
                    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                    var state=typeof(BleeckerLineChart).GetField("interaction",flags)!.GetValue(chart)!;
                    await File.WriteAllTextAsync(System.IO.Path.Combine(folder,$"sparkline-input-failure-{id}-{scheme}.json"),JsonSerializer.Serialize(new
                    {
                        id,chart.IsLoaded,chart.FocusState,input.OwnsForeground,help=AutomationProperties.GetHelpText(chart),data=chart.Data,
                        chart.IsEnabled,chart.ActualWidth,chart.ActualHeight,
                        targetFocused=ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),chart),
                        tooltip=Part<Border>(chart,"PART_Tooltip").Visibility.ToString(),
                        focus=(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot) as FrameworkElement)?.GetType().FullName,
                        items=Part<StackPanel>(chart,"PART_TooltipItems").Children.OfType<TextBlock>().Select(text=>text.Text),
                        interaction=state.GetType().GetFields(flags).ToDictionary(field=>field.Name,field=>field.GetValue(state)),
                        sparklineMode=typeof(BleeckerLineChart).GetField("sparklineMode",flags)!.GetValue(chart),
                        barMode=typeof(BleeckerLineChart).GetField("barMode",flags)?.GetValue(chart),
                        pointerTrace,screenPointer=input.ReadPointerPosition(),screenBounds=input.ScreenBounds(chart)
                    }));
                    throw;
                }
                reports.Add(new{id,height=spark.ActualHeight,width=spark.ActualWidth,curves=paths.Length,showArea=spark.ShowArea,tooltip=Part<StackPanel>(chart,"PART_TooltipItems").Children.OfType<TextBlock>().Select(x=>x.Text).ToArray()});
                await File.WriteAllTextAsync(System.IO.Path.Combine(folder,$"sparkline-{scheme}.json"),JsonSerializer.Serialize(reports,new JsonSerializerOptions{WriteIndented=true}));
            }
            await VerifyNegativeAxesAsync(host);
            await MutationAsync(host,input);
        }
        finally{root.Children.Remove(host);}
    }
    private async Task VerifyInspectionAsync(BleeckerLineChart chart,JsonElement expected,string label)
    {
        var tooltip=Part<Border>(chart,"PART_Tooltip");var container=Part<Grid>(chart,"PART_TooltipContainer");var items=Part<StackPanel>(chart,"PART_TooltipItems");var active=Part<Canvas>(chart,"PART_Active");
        var source=expected.GetProperty("tooltip");var visible=source.ValueKind!=JsonValueKind.Null&&source.GetProperty("visible").GetString()=="visible";
        await AwaitAsync(()=>tooltip.Visibility==(visible?Visibility.Visible:Visibility.Collapsed)&&string.Concat(items.Children.OfType<TextBlock>().Select(x=>x.Text))==(visible?source.GetProperty("text").GetString():""),$"Sparkline {label} inspection did not match source text/visibility.");
        var dots=expected.GetProperty("dots").EnumerateArray().ToArray();
        await AwaitAsync(()=>
        {
            var drawn=active.Children.OfType<Ellipse>().ToArray();
            return drawn.Length==dots.Length&&drawn.Select((dot,i)=>Math.Abs(Canvas.GetLeft(dot)+3-double.Parse(dots[i].GetProperty("cx").GetString()!,System.Globalization.CultureInfo.InvariantCulture))<.001).All(match=>match);
        },$"Sparkline {label} active coordinate did not settle.");
        UpdateLayout();
        if(visible)
        {
            var box=source.GetProperty("box");await AwaitAsync(()=>Math.Abs(tooltip.ActualHeight-box.GetProperty("height").GetDouble())<.001,$"Sparkline {label} tooltip height differs.");
            Near(container.Margin.Top,box.GetProperty("y").GetDouble(),label+" tooltip y");
            var sourceX=box.GetProperty("x").GetDouble();var sourceWidth=box.GetProperty("width").GetDouble();
            var cursor=active.Children.OfType<Line>().Single();
            if(Math.Abs(sourceX-cursor.X1-10)<.001)Near(container.Margin.Left,sourceX,label+" tooltip x");
            else if(Math.Abs(sourceX-2)<.001)Near(container.Margin.Left,2,label+" tooltip start clamp");
            else Near(container.Margin.Left+tooltip.ActualWidth,sourceX+sourceWidth,label+" tooltip trailing edge");
        }
        var native=active.Children.OfType<Ellipse>().ToArray();for(var i=0;i<native.Length;i++)
        {
            Near(native[i].Width,6,label+" active diameter");Near(native[i].StrokeThickness,0,label+" active stroke");
            Near(Canvas.GetLeft(native[i])+3,double.Parse(dots[i].GetProperty("cx").GetString()!,System.Globalization.CultureInfo.InvariantCulture),label+" active x");
            Near(Canvas.GetTop(native[i])+3,double.Parse(dots[i].GetProperty("cy").GetString()!,System.Globalization.CultureInfo.InvariantCulture),label+" active y");
        }
    }
    private async Task VerifyNegativeAxesAsync(Grid host)
    {
        using var reference=JsonDocument.Parse(typeof(SparklinesFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.NegativeChartDomains.json")!);
        foreach(var sample in reference.RootElement.GetProperty("cases").EnumerateArray())
        {
            var chart=new BleeckerLineChart{Width=640,Height=300,XAxisKey="label",HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top};
            chart.Series.Add(new BleeckerChartSeries{Key="v",Name="Value"});var index=0;
            foreach(var value in sample.GetProperty("values").EnumerateArray())chart.Data.Add(new Dictionary<string,object?>{{"label",(index++).ToString(System.Globalization.CultureInfo.InvariantCulture)},{"v",value.GetDouble()}});
            host.Children.Clear();host.Children.Add(chart);await AwaitAsync(()=>chart.IsLoaded,"Negative-domain chart did not mount.");UpdateLayout();
            var expected=sample.GetProperty("ticks").EnumerateArray().Select(x=>x.GetString()).ToArray();
            await AwaitAsync(()=>Part<Canvas>(chart,"PART_Plot").Children.OfType<TextBlock>().Where(text=>Math.Abs(Canvas.GetLeft(text)+text.DesiredSize.Width-52)<.001).Select(text=>text.Text).SequenceEqual(expected),"Native negative axis labels differ from the rendered pinned producer.");
        }
    }
    private async Task MutationAsync(Grid host,NativeInputSession input)
    {
        var before=new BleeckerButton{Content="Before sparkline"};var after=new BleeckerButton{Content="After sparkline"};var spark=new BleeckerSparkline{Data=Data(10,30,20),DataKey="v",Width=320,Height=40};
        var stack=new StackPanel{VerticalAlignment=VerticalAlignment.Top};host.Children.Clear();stack.Children.Add(before);stack.Children.Add(spark);stack.Children.Add(after);host.Children.Add(stack);
        await AwaitAsync(()=>after.IsLoaded,"Sparkline keyboard fixture did not mount.");UpdateLayout();await input.AcquireAsync(before);
        var chart=Part<BleeckerLineChart>(spark,"PART_Chart");var first=Part<Button>(before,"PART_Action");var last=Part<Button>(after,"PART_Action");
        Require(first.Focus(FocusState.Keyboard),"Could not focus preceding action.");input.Key(0x09);await AwaitAsync(()=>ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),chart),"Tab did not enter Sparkline inspection.");
        input.Key(0x27);await AwaitAsync(()=>AutomationProperties.GetHelpText(chart)==" : 30","Sparkline did not inspect consumer data through physical Right.");
        input.Key(0x09);await AwaitAsync(()=>ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),last),"Tab did not leave Sparkline.");input.ShiftKey(0x09);await AwaitAsync(()=>ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),chart),"Reverse Tab skipped Sparkline.");
        await input.MoveAsync(chart,localPoint:new Point(318,20));await AwaitAsync(()=>AutomationProperties.GetHelpText(chart)==" : 20","Sparkline did not inspect its last value through the physical pointer.");
        await input.MoveAsync(after);await AwaitAsync(()=>Part<Border>(chart,"PART_Tooltip").Visibility==Visibility.Collapsed,"Sparkline pointer exit did not dismiss inspection.");
        var original=spark.Data;var plot=Part<Canvas>(chart,"PART_Plot");var path=plot.Children.OfType<Path>().Single();spark.Data[1]=new Dictionary<string,object?>{{"v",50d}};
        await AwaitAsync(()=>!ReferenceEquals(plot.Children.OfType<Path>().SingleOrDefault(),path),"Sparkline ignored a changed consumer record.");
        spark.Data=new();await AwaitAsync(()=>plot.Children.OfType<Path>().Count()==0&&Part<Border>(chart,"PART_Tooltip").Visibility==Visibility.Collapsed,"Sparkline did not clear empty data/inspection.");
        spark.Data=original;await AwaitAsync(()=>plot.Children.OfType<Path>().Count()==1,"Sparkline did not restore data.");
        stack.Children.Remove(spark);await AwaitAsync(()=>!spark.IsLoaded,"Sparkline did not unmount.");original.Add(new Dictionary<string,object?>{{"v",25d}});stack.Children.Insert(1,spark);await AwaitAsync(()=>spark.IsLoaded,"Sparkline did not remount.");
        await AwaitAsync(()=>Part<Canvas>(chart,"PART_Plot").Children.OfType<Path>().SingleOrDefault()?.Data is PathGeometry geometry&&geometry.Figures.Single().Segments.Count==3,"Sparkline lost changed data after remount.");
        var keySource=new TextBlock{Tag="v"};spark.SetBinding(BleeckerSparkline.DataKeyProperty,new Binding{Source=keySource,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});
        keySource.Tag="missing";await AwaitAsync(()=>plot.Children.OfType<Path>().Count()==0,"Sparkline ignored its bound data key.");keySource.Tag="v";await AwaitAsync(()=>plot.Children.OfType<Path>().Count()==1,"Sparkline did not restore its bound data key.");
    }
    private static object? Read(JsonElement value)=>value.ValueKind switch{JsonValueKind.Object=>value.EnumerateObject().ToDictionary(p=>p.Name,p=>Read(p.Value)),JsonValueKind.Array=>value.EnumerateArray().Select(Read).ToArray(),JsonValueKind.String=>value.GetString(),JsonValueKind.Number=>value.GetDouble(),JsonValueKind.True=>true,JsonValueKind.False=>false,_=>null};
    private static Windows.UI.Color Paint(string value)=>value=="transparent"?Windows.UI.Color.FromArgb(0,0,0,0):Windows.UI.Color.FromArgb(255,Convert.ToByte(value.Substring(1,2),16),Convert.ToByte(value.Substring(3,2),16),Convert.ToByte(value.Substring(5,2),16));
    private static void ComparePaint(Brush brush,string css,string label)
    {
        var values=System.Text.RegularExpressions.Regex.Matches(css,"[0-9.]+").Select(m=>double.Parse(m.Value,System.Globalization.CultureInfo.InvariantCulture)).ToArray();var c=((SolidColorBrush)brush).Color;var alpha=values.Length==4?values[3]:1;
        Require(Math.Abs(c.A/255d*brush.Opacity-alpha)<1e-9&&(alpha==0||c.R==values[0]&&c.G==values[1]&&c.B==values[2]),$"Sparkline {label} paint differs.");
    }
    private static T Part<T>(DependencyObject owner,string name)where T:FrameworkElement{if(owner is T match&&match.Name==name)return match;for(var i=0;i<VisualTreeHelper.GetChildrenCount(owner);i++){try{return Part<T>(VisualTreeHelper.GetChild(owner,i),name);}catch(InvalidOperationException){}}throw new InvalidOperationException($"Missing Sparkline part {name}.");}
    private static async Task FrameAsync(){var frame=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);void Presented(object? sender,object args)=>frame.TrySetResult();CompositionTarget.Rendering+=Presented;try{await frame.Task.WaitAsync(TimeSpan.FromSeconds(2));}finally{CompositionTarget.Rendering-=Presented;}}
    private async Task AwaitAsync(Func<bool> ready,string message){var watch=Stopwatch.StartNew();while(!ready()&&watch.Elapsed<TimeSpan.FromSeconds(2)){UpdateLayout();await Task.Delay(16);}Require(ready(),message);}
    private static void Near(double actual,double expected,string label)=>Require(Math.Abs(actual-expected)<.001,$"Sparkline {label} differs: {actual:R}/{expected:R}.");
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
