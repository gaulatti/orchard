using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard.Catalog;

public sealed partial class ScatterChartsFixture
{
    private sealed class MutableRecord : Dictionary<string,object?>,INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        internal void Change(string key,object? value){this[key]=value;PropertyChanged?.Invoke(this,new(key));}
    }
    private async Task VerifyMutationAsync(Grid host,NativeInputSession input,string folder,BleeckerColorScheme scheme)
    {
        var first=new MutableRecord { ["x"]=10d,["y"]=20d,["z"]=5d };var second=new MutableRecord { ["x"]=20d,["y"]=40d,["z"]=20d };
        var data=new ObservableCollection<IReadOnlyDictionary<string,object?>> { first,second };var independent=new ObservableCollection<IReadOnlyDictionary<string,object?>> { new MutableRecord { ["x"]=40d,["y"]=50d } };
        var primary=new BleeckerScatterSeries { Key="primary",Name="Alpha",XKey="x",YKey="y" };var secondary=new BleeckerScatterSeries { Key="secondary",Name="Beta",XKey="ignored",YKey="alsoIgnored",Data=independent };
        var owner=new BleeckerScatterChart { Data=data,Series=new(){primary,secondary},Width=320,Height=200,ShowLegend=false };AutomationProperties.SetName(owner,"Live scatter consumer");
        var before=new BleeckerButton { Content="Before scatter" };var after=new BleeckerButton { Content="After scatter" };var stack=new StackPanel { VerticalAlignment=VerticalAlignment.Top };stack.Children.Add(before);stack.Children.Add(owner);stack.Children.Add(after);host.Children.Clear();host.Children.Add(stack);
        await AwaitAsync(()=>owner.IsLoaded&&after.IsLoaded,"Live Scatter did not mount.");UpdateLayout();var chart=Descendants<BleeckerLineChart>(owner).Single();Path[] Points()=>Part<Canvas>(chart,"PART_Plot").Children.OfType<Path>().ToArray();
        await AwaitAsync(()=>Points().Length==3,"Per-series data did not render.");Require(ReferenceEquals(chart.Data,data)&&ReferenceEquals(owner.Series[1].Data,independent),"Scatter cloned consumer collections.");
        await input.MoveAsync(before);Require(Part<Button>(before,"PART_Action").Focus(FocusState.Keyboard),"Could not focus preceding Scatter action.");input.Key(0x09);
        await AwaitAsync(()=>ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),chart)&&Inspection(chart)=="x : 10y : 20","Tab did not enter Scatter's single inspection stop.");
        first.Change("y",25d);await AwaitAsync(()=>Inspection(chart)=="x : 10y : 25","Root record notification did not reach Scatter inspection.");
        await input.KeyAsync(0x27,chart);await AwaitAsync(()=>Inspection(chart)=="x : 20y : 40","Scatter Right did not inspect its next point.");
        var point=Points()[2];await input.MoveAsync(chart,localPoint:new Point(Canvas.GetLeft(point),Canvas.GetTop(point)));await AwaitAsync(()=>Inspection(chart)=="x : 40y : 50","Native hover did not inspect independent series data through first-series axis keys.");
        var oldPointY=Canvas.GetTop(point);var pointerBefore=input.ReadPointerPosition();((MutableRecord)independent[0]).Change("y",60d);await AwaitAsync(()=>Canvas.GetTop(Points()[2])!=oldPointY,"Independent record notification did not update point geometry.");await AwaitAsync(()=>Inspection(chart)=="x : 40y : 60","Independent record notification did not reach pointer inspection.");await FrameAsync();await FrameAsync();Require(Inspection(chart)=="x : 40y : 60"&&input.ReadPointerPosition()==pointerBefore,"Scatter lost updated inspection while the native pointer stayed stationary.");
        await input.MoveAsync(before);await AwaitAsync(()=>Inspection(chart)=="","Leaving Scatter retained pointer inspection.");await input.KeyAsync(0x25,chart);await AwaitAsync(()=>Inspection(chart)=="x : 10y : 25","Scatter hover discarded the retained keyboard index.");
        var defaultPaint=((SolidColorBrush)Points()[0].Fill).Color;var paint=new SolidColorBrush(Windows.UI.Color.FromArgb(0,0,0,0));primary.Color=paint;await AwaitAsync(()=>ReferenceEquals(Points()[0].Fill,paint),"Scatter replaced a consumer's transparent brush.");primary.Color=null;await AwaitAsync(()=>Points()[0].Fill is SolidColorBrush restored&&restored.Color==defaultPaint,"Scatter did not restore theme paint after clearing consumer color.");
        owner.Series.Insert(1,primary);await AwaitAsync(()=>Points().Length==5,"Scatter lost a repeated series occurrence.");owner.Series.RemoveAt(1);await AwaitAsync(()=>Points().Length==3,"Scatter retained a removed series occurrence.");
        primary.ZKey="z";await AwaitAsync(()=>Inspection(chart)=="x : 10y : 25z : 5","Live Z key did not reach bubble inspection.");var initialRadius=((ArcSegment)((PathGeometry)Points()[0].Data).Figures[0].Segments[0]).Size.Width;
        first.Change("z",10d);await AwaitAsync(()=>((ArcSegment)((PathGeometry)Points()[0].Data).Figures[0].Segments[0]).Size.Width>initialRadius,"Live bubble value did not update its area.");
        owner.ShowLegend=true;primary.Name="Renamed";await AwaitAsync(()=>Part<Canvas>(chart,"PART_Legend").Children.OfType<Canvas>().SelectMany(c=>c.Children.OfType<TextBlock>()).Any(t=>t.Text=="Renamed"),"Legend ignored its live consumer name.");
        owner.XAxisTickFormatter=value=>"X"+value;owner.YAxisTickFormatter=value=>"Y"+value;owner.TooltipFormatter=value=>"T"+value;await AwaitAsync(()=>Inspection(chart)=="x : T10y : T25z : T10","Scatter ignored live numeric tooltip formatting.");
        Require(Part<Canvas>(chart,"PART_Plot").Children.OfType<TextBlock>().Where(t=>Equals(t.Tag,"scatter-x-tick")).All(t=>t.Text.StartsWith('X')),"Scatter ignored live X formatting.");
        input.Key(0x09);await AwaitAsync(()=>ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),Part<Button>(after,"PART_Action")),"Scatter interrupted forward Tab.");input.ShiftKey(0x09);await AwaitAsync(()=>ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),chart),"Scatter interrupted reverse Tab.");
        owner.IsEnabled=false;UpdateLayout();Require(!chart.IsEnabled,"Disabled Scatter retained enabled inspection.");owner.IsEnabled=true;
        stack.Children.Remove(owner);await AwaitAsync(()=>!owner.IsLoaded,"Scatter did not detach.");await FrameAsync();await FrameAsync();
        var observed=typeof(BleeckerLineChart).GetField("observedScatterSeries",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;Require(observed.GetValue(chart)is null,"Detached Scatter retained per-series observers.");
        stack.Children.Insert(1,owner);await AwaitAsync(()=>owner.IsLoaded,"Scatter did not remount.");stack.Children.Remove(owner);stack.Children.Insert(1,owner);await FrameAsync();await FrameAsync();
        var priorY=Canvas.GetTop(Points()[0]);first.Change("y",35d);await AwaitAsync(()=>ReferenceEquals(observed.GetValue(chart),owner.Series)&&Canvas.GetTop(Points()[0])!=priorY,"Rapid Scatter remount lost its live record updates.");
        var template=owner.Template;owner.Template=(ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Grid /></ControlTemplate>");owner.ApplyTemplate();UpdateLayout();await AwaitAsync(()=>!chart.IsLoaded,"Old Scatter template stayed mounted.");
        owner.Template=template;owner.ApplyTemplate();UpdateLayout();await AwaitAsync(()=>Descendants<BleeckerLineChart>(owner).Any(),"Scatter template did not restore.");chart=Descendants<BleeckerLineChart>(owner).Single();await AwaitAsync(()=>Points().Length==3&&ReferenceEquals(chart.Data,data),"Scatter template lost consumer data.");
        var replacement=new ObservableCollection<IReadOnlyDictionary<string,object?>> { new MutableRecord { ["x"]=5d,["y"]=10d } };secondary.Data=replacement;await AwaitAsync(()=>ReferenceEquals(owner.Series[1].Data,replacement)&&Points().Length==3,"Scatter lost replacement series data.");independent.Clear();Require(Points().Length==3,"Discarded series data still drives Scatter.");
        owner.Data=new();await AwaitAsync(()=>Points().Length==1,"Scatter did not remove cleared root points.");replacement.Clear();await AwaitAsync(()=>Points().Length==0,"Empty Scatter retained point shapes.");
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder,$"scatter-chart-lifecycle-{scheme}.json"),JsonSerializer.Serialize(new{passed=true,originalCollections=true,recordNotifications=true,independentSeries=true,nativeTab=true,nativeArrows=true,nativeHover=true,stationaryHoverMutation=true,consumerColorReset=true,bubbleUpdates=true,formatters=true,legend=true,realDetach=true,rapidRemount=true,templates=true,emptyData=true}));
    }
}
