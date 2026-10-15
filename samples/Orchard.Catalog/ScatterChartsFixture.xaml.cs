using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard.Catalog;

public sealed partial class ScatterChartsFixture : UserControl
{
    public ScatterChartsFixture()
    {
        InitializeComponent();
        foreach (var row in new[] { (X:10d,Y:20d,Z:5d),(X:20d,Y:40d,Z:10d),(X:30d,Y:25d,Z:20d) }) Specimen.Data.Add(new Dictionary<string,object?> { ["x"]=row.X,["y"]=row.Y,["z"]=row.Z });
    }
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        Require(XamlRoot.RasterizationScale == 1 && new Windows.UI.ViewManagement.UISettings().TextScaleFactor == 1, "Frozen Scatter checks require display/text scale 1.");
        using var input = new NativeInputSession(XamlRoot, folder, $"scatter-chart-{scheme}"); await input.AcquireAsync(Specimen);
        var root = (Grid)XamlRoot.Content; var host = new Grid { Margin = new Thickness(20), Background = root.Background }; Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count); root.Children.Add(host);
        using var reference = JsonDocument.Parse(typeof(ScatterChartsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.ScatterChartLayout.json")!);
        var reports = new List<object>(); var failures = new List<string>();
        try
        {
            foreach (var sample in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("scheme").GetString() == scheme.ToString()))
            {
                var id=sample.GetProperty("id").GetString()!;var props=sample.GetProperty("props");var expected=sample.GetProperty("normal");
                var rows=Records(props.GetProperty("data"));var owner=new BleeckerScatterChart { Data=rows, Width=expected.GetProperty("width").GetDouble(),Height=expected.GetProperty("height").GetDouble(),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,
                    ShowGrid=!props.TryGetProperty("showGrid",out var grid)||grid.GetBoolean(),ShowLegend=!props.TryGetProperty("showLegend",out var legendProp)||legendProp.GetBoolean(),XAxisLabel=props.TryGetProperty("xAxisLabel",out var xl)?xl.GetString():null,YAxisLabel=props.TryGetProperty("yAxisLabel",out var yl)?yl.GetString():null };
                foreach(var item in props.GetProperty("series").EnumerateArray()) owner.Series.Add(new BleeckerScatterSeries { Key=item.GetProperty("key").GetString()!,Name=item.GetProperty("name").GetString()!,XKey=item.GetProperty("xKey").GetString()!,YKey=item.GetProperty("yKey").GetString()!,ZKey=item.TryGetProperty("zKey",out var zk)?zk.GetString():null,Data=item.TryGetProperty("data",out var data)?Records(data):null,Color=item.TryGetProperty("color",out var color)?new SolidColorBrush(Paint(color.GetString()!)):null });
                if(props.TryGetProperty("format",out var format)&&format.GetBoolean()){owner.XAxisTickFormatter=value=>"X"+Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture);owner.YAxisTickFormatter=value=>"Y"+Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture);owner.TooltipFormatter=value=>"T"+value.ToString(System.Globalization.CultureInfo.InvariantCulture);}
                AutomationProperties.SetName(owner,"Consumer scatter comparison");host.Children.Clear();host.Children.Add(owner);await AwaitAsync(()=>owner.IsLoaded,"Scatter did not mount.");owner.ApplyTemplate();UpdateLayout();await FrameAsync();UpdateLayout();
                var chart=Descendants<BleeckerLineChart>(owner).Single();var plot=Part<Canvas>(chart,"PART_Plot");var errors=new List<string>();void Check(Action action){try{action();}catch(InvalidOperationException error){errors.Add(error.Message);}}
                var paths=plot.Children.OfType<Path>().ToArray();var points=expected.GetProperty("points").EnumerateArray().ToArray();
                Check(()=>Require(paths.Length==points.Length,$"points {paths.Length}/{points.Length}"));
                for(var i=0;i<Math.Min(paths.Length,points.Length);i++){var n=i;Check(()=>{Near(Canvas.GetLeft(paths[n]),points[n].GetProperty("cx").GetDouble());Near(Canvas.GetTop(paths[n]),points[n].GetProperty("cy").GetDouble());CompareCircle((PathGeometry)paths[n].Data,points[n].GetProperty("path").GetString()!);ComparePaint(paths[n].Fill,points[n].GetProperty("fill").GetString()!);Require(paths[n].Stroke is null,"Scatter manufactured a point stroke.");});}
                foreach(var axis in new[]{"x","y"})Check(()=>{
                    var ticks=plot.Children.OfType<TextBlock>().Where(text=>Equals(text.Tag,"scatter-"+axis+"-tick")).ToArray();var source=expected.GetProperty(axis+"Ticks").EnumerateArray().ToArray();Require(ticks.Select(t=>t.Text).SequenceEqual(source.Select(t=>t.GetProperty("text").GetString())),axis+" tick text differs");
                    for(var i=0;i<ticks.Length;i++){var x=double.Parse(source[i].GetProperty("x").GetString()!,System.Globalization.CultureInfo.InvariantCulture);var y=double.Parse(source[i].GetProperty("y").GetString()!,System.Globalization.CultureInfo.InvariantCulture);Near(Canvas.GetLeft(ticks[i])+(axis=="x"?ticks[i].DesiredSize.Width/2:ticks[i].DesiredSize.Width),x);Near(Canvas.GetTop(ticks[i])+ticks[i].BaselineOffset,y+(axis=="x"?8.52:4.26));ComparePaint(ticks[i].Foreground,source[i].GetProperty("fill").GetString()!);}
                });
                Check(()=>{var actual=plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Line>().ToArray();var source=expected.GetProperty("grid").EnumerateArray().ToArray();Require(actual.Length==source.Length,$"grid {actual.Length}/{source.Length}");for(var i=0;i<actual.Length;i++){Near(actual[i].X1,source[i].GetProperty("x1").GetDouble());Near(actual[i].Y1,source[i].GetProperty("y1").GetDouble());Near(actual[i].X2,source[i].GetProperty("x2").GetDouble());Near(actual[i].Y2,source[i].GetProperty("y2").GetDouble());}});
                Check(()=>{var labels=Part<Canvas>(chart,"PART_Legend").Children.OfType<Canvas>().SelectMany(item=>item.Children.OfType<TextBlock>()).ToArray();var source=expected.GetProperty("legend").EnumerateArray().ToArray();Require(labels.Select(t=>t.Text).SequenceEqual(source.Select(t=>t.GetProperty("text").GetString())),"legend text differs");for(var i=0;i<labels.Length;i++){ComparePaint(labels[i].Foreground,source[i].GetProperty("color").GetString()!);var parent=(Canvas)VisualTreeHelper.GetParent(labels[i]);Near(Canvas.GetTop(parent)+Canvas.GetTop(labels[i])+labels[i].BaselineOffset,source[i].GetProperty("box").GetProperty("y").GetDouble()+16);}});
                Check(()=>{var source=expected.GetProperty("axisLabels").EnumerateArray().ToArray();var labels=plot.Children.OfType<TextBlock>().Where(t=>Equals(t.Tag,"scatter-x-label")||Equals(t.Tag,"scatter-y-label")).ToArray();Require(labels.Select(t=>t.Text).SequenceEqual(source.Select(t=>t.GetProperty("text").GetString())),"axis labels differ");for(var i=0;i<labels.Length;i++){var text=labels[i];var point=text.TransformToVisual(plot).TransformPoint(new Point(Equals(text.Tag,"scatter-x-label")?text.DesiredSize.Width/2:0,text.BaselineOffset));var sx=double.Parse(source[i].GetProperty("x").GetString()!,System.Globalization.CultureInfo.InvariantCulture);var sy=double.Parse(source[i].GetProperty("y").GetString()!,System.Globalization.CultureInfo.InvariantCulture);Near(point.X,sx+(Equals(text.Tag,"scatter-y-label")?4.26:0));Near(point.Y,sy);}});
                Check(()=>Require(ReferenceEquals(chart.Data,rows)&&!owner.IsTabStop&&chart.IsTabStop&&AutomationProperties.GetName(chart)=="Consumer scatter comparison"&&FrameworkElementAutomationPeer.CreatePeerForElement(chart).GetClassName()==nameof(BleeckerScatterChart),"Scatter lost original data or its single named inspection boundary."));
                if(id is "default" or "narrow" or "bubble" or "labels" or "multiple-data" or "negative")await NativeScreenCapture.CaptureClientAsync(input.Window,System.IO.Path.Combine(folder,$"scatter-chart-{id}-{scheme}.png"));
                Require(chart.Focus(FocusState.Keyboard),"Could not focus Scatter inspection.");
                foreach(var state in sample.GetProperty("states").EnumerateArray().Where(state=>state.GetProperty("action").GetString()!="hoverLast"))
                {
                    var action=state.GetProperty("action").GetString();if(action!="focus")await input.KeyAsync(action=="ArrowRight"?(ushort)0x27:action=="ArrowLeft"?(ushort)0x25:(ushort)0x0D,chart);
                    var value=state.GetProperty("state");await AwaitAsync(()=>Inspection(chart)==value.GetProperty("tooltip").GetString(),$"Scatter {id}/{action} payload did not settle: {Inspection(chart)}");
                    if(value.GetProperty("tooltipVisible").GetBoolean())await AwaitAsync(()=>Math.Abs(Part<Border>(chart,"PART_Tooltip").ActualHeight-value.GetProperty("tooltipBox").GetProperty("height").GetDouble())<.001,"Scatter tooltip rows did not arrange to the source height.");
                    Check(()=>Require((Part<Border>(chart,"PART_Tooltip").Visibility==Visibility.Visible)==value.GetProperty("tooltipVisible").GetBoolean(),"tooltip visibility differs"));
                    Check(()=>{var cursor=Part<Canvas>(chart,"PART_Active").Children.OfType<Path>().ToArray();if(value.GetProperty("cursor").ValueKind==JsonValueKind.Null){Require(cursor.Length==0,"Scatter retained a hidden cursor.");return;}Require(cursor.Length==1,"Scatter lost its crosshair.");var d=Regex.Match(value.GetProperty("cursor").GetString()!," d=\"([^\"]+)\"").Groups[1].Value;var source=(PathGeometry)((Path)XamlReader.Load("<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Data='"+d+"' />")).Data;var actual=(PathGeometry)cursor[0].Data;Require(actual.Figures.Count==source.Figures.Count,"crosshair figures differ");for(var i=0;i<actual.Figures.Count;i++){Near(actual.Figures[i].StartPoint.X,source.Figures[i].StartPoint.X);Near(actual.Figures[i].StartPoint.Y,source.Figures[i].StartPoint.Y);Near(((LineSegment)actual.Figures[i].Segments.Single()).Point.X,((LineSegment)source.Figures[i].Segments.Single()).Point.X);Near(((LineSegment)actual.Figures[i].Segments.Single()).Point.Y,((LineSegment)source.Figures[i].Segments.Single()).Point.Y);}Require(cursor[0].StrokeDashArray.SequenceEqual(new[]{3d,3d}),"crosshair dashes differ");if(value.GetProperty("tooltipVisible").GetBoolean())Near(Part<Border>(chart,"PART_Tooltip").ActualHeight,value.GetProperty("tooltipBox").GetProperty("height").GetDouble());});
                }
                reports.Add(new{id,errors,points=paths.Length});failures.AddRange(errors.Select(error=>id+": "+error));await File.WriteAllTextAsync(System.IO.Path.Combine(folder,$"scatter-chart-{scheme}.json"),JsonSerializer.Serialize(new{passed=failures.Count==0,reports},new JsonSerializerOptions{WriteIndented=true}));
            }
            Require(failures.Count==0,"Scatter mismatches: "+string.Join(" | ",failures));
            await VerifyMutationAsync(host,input,folder,scheme);
        }
        finally{root.Children.Remove(host);}
    }
    private static string Inspection(BleeckerLineChart chart)=>string.Concat(Part<StackPanel>(chart,"PART_TooltipItems").Children.OfType<TextBlock>().Select(text=>text.Text));
    private static ObservableCollection<IReadOnlyDictionary<string,object?>> Records(JsonElement data)=>new(data.EnumerateArray().Select(row=>(IReadOnlyDictionary<string,object?>)Read(row)!));
    private static object? Read(JsonElement value)=>value.ValueKind switch{JsonValueKind.Object=>value.EnumerateObject().ToDictionary(p=>p.Name,p=>Read(p.Value)),JsonValueKind.Array=>value.EnumerateArray().Select(Read).ToArray(),JsonValueKind.Number=>value.GetDouble(),JsonValueKind.String=>value.GetString(),JsonValueKind.True=>true,JsonValueKind.False=>false,JsonValueKind.Null=>null,_=>throw new InvalidOperationException("Unknown frozen value.")};
    private static Windows.UI.Color Paint(string value)=>value=="transparent"?Windows.UI.Color.FromArgb(0,0,0,0):Windows.UI.Color.FromArgb(255,Convert.ToByte(value.Substring(1,2),16),Convert.ToByte(value.Substring(3,2),16),Convert.ToByte(value.Substring(5,2),16));
    private static void ComparePaint(Brush paint,string value){var channels=Regex.Matches(value,@"[\d.]+").Select(m=>double.Parse(m.Value,System.Globalization.CultureInfo.InvariantCulture)).ToArray();var c=((SolidColorBrush)paint).Color;Require(c.R==channels[0]&&c.G==channels[1]&&c.B==channels[2]&&c.A==(channels.Length==3?255:channels[3]*255),"paint differs from "+value);}
    private static void CompareCircle(PathGeometry actual,string source){var expected=(PathGeometry)((Path)XamlReader.Load("<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Data='"+source+"' />")).Data;var a=actual.Figures.Single();var e=expected.Figures.Single();Near(a.StartPoint.X,e.StartPoint.X);Near(a.StartPoint.Y,e.StartPoint.Y);Require(a.IsFilled&&a.Segments.Count==e.Segments.Count,"circle segments differ");for(var i=0;i<a.Segments.Count;i++){var arc=(ArcSegment)a.Segments[i];var other=(ArcSegment)e.Segments[i];Near(arc.Point.X,other.Point.X);Near(arc.Point.Y,other.Point.Y);Near(arc.Size.Width,other.Size.Width);Near(arc.Size.Height,other.Size.Height);Require(arc.IsLargeArc==other.IsLargeArc&&arc.SweepDirection==other.SweepDirection,"circle arc differs");}}
    private static void Near(double actual,double expected)=>Require(Math.Abs(actual-expected)<.001,$"coordinate {actual:R}/{expected:R}");
    private static IEnumerable<T> Descendants<T>(DependencyObject owner)where T:DependencyObject{for(var i=0;i<VisualTreeHelper.GetChildrenCount(owner);i++){var child=VisualTreeHelper.GetChild(owner,i);if(child is T item)yield return item;foreach(var nested in Descendants<T>(child))yield return nested;}}
    private static T Part<T>(DependencyObject owner,string name)where T:FrameworkElement=>Descendants<T>(owner).FirstOrDefault(item=>item.Name==name)??throw new InvalidOperationException("Missing Scatter part "+name);
    private async Task AwaitAsync(Func<bool> condition,string message){var watch=Stopwatch.StartNew();while(!condition()&&watch.Elapsed<TimeSpan.FromSeconds(2)){UpdateLayout();await Task.Delay(16);}Require(condition(),message);}
    private static async Task FrameAsync(){var ready=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);void Frame(object? sender,object args)=>ready.TrySetResult();CompositionTarget.Rendering+=Frame;try{await ready.Task.WaitAsync(TimeSpan.FromSeconds(2));}finally{CompositionTarget.Rendering-=Frame;}}
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
