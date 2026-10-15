using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard.Catalog;

public sealed partial class RadialBarChartsFixture
{
    private async Task VerifyMutationAsync(Grid host, NativeInputSession input, string folder, BleeckerColorScheme scheme)
    {
        var a = new BleeckerRadialBarChartDatum { Name = "A", Value = 10 }; var b = new BleeckerRadialBarChartDatum { Name = "B", Value = 20 };
        var data = new ObservableCollection<IReadOnlyDictionary<string, object?>> { a, b, new BleeckerRadialBarChartDatum { Name = "C", Value = 30 } };
        var owner = new BleeckerRadialBarChart { Data = data, Width = 320, Height = 240, ShowLegend = false }; AutomationProperties.SetName(owner, "Live radial consumer");
        var before = new BleeckerButton { Content = "Before radial chart" }; var after = new BleeckerButton { Content = "After radial chart" }; var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Top }; stack.Children.Add(before); stack.Children.Add(owner); stack.Children.Add(after); host.Children.Clear(); host.Children.Add(stack);
        await AwaitAsync(() => owner.IsLoaded && after.IsLoaded, "Live Radial Bar did not mount."); UpdateLayout(); var chart = Descendants<BleeckerLineChart>(owner).Single();
        Path[] Bars() => Part<Canvas>(chart, "PART_Plot").Children.OfType<Path>().Where(path => path.Tag is ValueTuple<string, int> tag && tag.Item1 == "radial-foreground").ToArray();
        Point GlyphStart()
        {
            var labels = ((IEnumerable)typeof(BleeckerLineChart).GetField("radialLabels", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(chart)!).Cast<object>();
            var glyphs = ((IEnumerable)Get<object>(labels.First(), "Glyphs")).Cast<object>(); return Get<Point>(glyphs.First(), "Start");
        }
        await AwaitAsync(() => Bars().Length == 3, "Radial Bar lost live consumer records."); Require(ReferenceEquals(chart.Data, data), "Radial Bar cloned consumer data.");
        await input.MoveAsync(before); Require(Part<Button>(before, "PART_Action").Focus(FocusState.Keyboard), "Could not focus before Radial Bar."); input.Key(0x09);
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), chart) && Inspection(chart) == "0 : 10", "Tab did not enter one Radial Bar inspection stop.");
        a.Value = 15; await AwaitAsync(() => Inspection(chart) == "0 : 15", "Radial Bar ignored typed record notifications.");
        await input.KeyAsync(0x27, chart); await AwaitAsync(() => Inspection(chart) == "1 : 20", "Radial Bar native Right did not select the next item.");
        await input.KeyAsync(0x0D, chart); await AwaitAsync(() => Inspection(chart) == "", "Radial Bar Enter did not hide inspection."); await input.KeyAsync(0x0D, chart); await AwaitAsync(() => Inspection(chart) == "1 : 20", "Radial Bar Enter did not restore its keyboard index.");
        var paint = new SolidColorBrush(Microsoft.UI.Colors.Transparent); a.Fill = paint; await AwaitAsync(() => ReferenceEquals(Bars()[0].Fill, paint), "Radial Bar lost an explicit transparent consumer brush.");
        owner.ShowLegend = true; a.Name = "Renamed"; await AwaitAsync(() => Part<Canvas>(chart, "PART_Legend").Children.OfType<Canvas>().SelectMany(canvas => canvas.Children.OfType<TextBlock>()).Any(text => text.Text == "Renamed"), "Radial Bar ignored a live legend name.");
        var family = owner.FontFamily; var oldStart = GlyphStart(); owner.FontFamily = new FontFamily("Segoe UI"); await AwaitAsync(() => GlyphStart() != oldStart, "Radial Bar ignored its consumer font family in curved glyphs."); owner.FontFamily = family;
        AutomationProperties.SetName(owner, "Renamed radial consumer"); Require(AutomationProperties.GetName(chart) == "Renamed radial consumer", "Radial Bar ignored a live accessible name.");
        await input.MoveAsync(chart, localPoint: new Point(185, 120)); await AwaitAsync(() => Inspection(chart) == "0 : 15", "Radial Bar radius hover selected the wrong record.");
        var pointer = input.ReadPointerPosition(); a.Value = 18; await AwaitAsync(() => Inspection(chart) == "0 : 18", "Radial Bar stationary hover lost record changes."); await FrameAsync(); await FrameAsync(); Require(Inspection(chart) == "0 : 18" && input.ReadPointerPosition() == pointer, "Radial Bar did not retain stationary radius inspection.");
        await input.MoveAsync(before); await AwaitAsync(() => Inspection(chart) == "", "Radial Bar exit retained inspection."); await input.KeyAsync(0x25, chart); await AwaitAsync(() => Inspection(chart) == "0 : 18", "Radial Bar hover discarded keyboard selection.");
        var originalPoint = ((PathGeometry)Bars()[0].Data).Figures[0].StartPoint; owner.StartAngle = 180; owner.EndAngle = 0; await AwaitAsync(() => ((PathGeometry)Bars()[0].Data).Figures[0].StartPoint != originalPoint, "Radial Bar ignored angle changes.");
        originalPoint = ((PathGeometry)Bars()[0].Data).Figures[0].StartPoint; owner.InnerRadius = "30%"; owner.OuterRadius = 100d; await AwaitAsync(() => ((PathGeometry)Bars()[0].Data).Figures[0].StartPoint != originalPoint, "Radial Bar ignored radius changes.");
        input.Key(0x09); await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), Part<Button>(after, "PART_Action")), "Radial Bar interrupted forward Tab."); input.ShiftKey(0x09); await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), chart), "Radial Bar interrupted reverse Tab.");
        owner.IsEnabled = false; UpdateLayout(); Require(!chart.IsEnabled, "Disabled Radial Bar retained enabled inspection."); Require(Part<Button>(before, "PART_Action").Focus(FocusState.Keyboard), "Could not focus before disabled Radial Bar."); input.Key(0x09); await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), Part<Button>(after, "PART_Action")), "Disabled Radial Bar did not leave Tab traversal."); owner.IsEnabled = true;
        var observed = typeof(BleeckerLineChart).GetField("observedData", BindingFlags.Instance | BindingFlags.NonPublic)!; stack.Children.Remove(owner); await AwaitAsync(() => !owner.IsLoaded, "Radial Bar did not detach."); await FrameAsync(); await FrameAsync(); Require(observed.GetValue(chart) is null, "Detached Radial Bar retained record observers.");
        stack.Children.Insert(1, owner); await AwaitAsync(() => owner.IsLoaded, "Radial Bar did not remount."); stack.Children.Remove(owner); stack.Children.Insert(1, owner); await FrameAsync(); await FrameAsync();
        var originalEnd = ((PathGeometry)Bars()[0].Data).Figures[0].Segments.OfType<ArcSegment>().Skip(1).First().Point; a.Value = 12; await AwaitAsync(() => ReferenceEquals(observed.GetValue(chart), data) && ((PathGeometry)Bars()[0].Data).Figures[0].Segments.OfType<ArcSegment>().Skip(1).First().Point != originalEnd, "Rapid Radial Bar remount lost record updates.");
        var template = owner.Template; owner.Template = (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Grid /></ControlTemplate>"); owner.ApplyTemplate(); UpdateLayout(); await AwaitAsync(() => !chart.IsLoaded, "Old Radial Bar template stayed mounted."); owner.Template = template; owner.ApplyTemplate(); UpdateLayout(); await AwaitAsync(() => Descendants<BleeckerLineChart>(owner).Any(), "Radial Bar template did not restore."); chart = Descendants<BleeckerLineChart>(owner).Single(); await AwaitAsync(() => Bars().Length == 3 && ReferenceEquals(chart.Data, data), "Radial Bar template lost original data.");
        originalPoint = ((PathGeometry)Bars()[0].Data).Figures[0].StartPoint; owner.Width = 360; owner.Height = 260; await AwaitAsync(() => Math.Abs(chart.ActualWidth - 360) < .001 && Math.Abs(chart.ActualHeight - 260) < .001 && ((PathGeometry)Bars()[0].Data).Figures[0].StartPoint != originalPoint, "Radial Bar ignored live size changes.");
        owner.Data = new() { new Dictionary<string, object?> { ["name"] = "Range", ["value"] = new object?[] { 5d, 20d } } }; await AwaitAsync(() => Bars().Length == 1 && !ReferenceEquals(chart.Data, data), "Radial Bar lost replacement range data."); data.Clear(); a.Value = 999; Require(Bars().Length == 1, "Discarded Radial Bar data changed the live chart."); owner.Data.Clear(); await AwaitAsync(() => Bars().Length == 0 && Inspection(chart) == "", "Empty Radial Bar retained shapes or inspection.");
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"radial-bar-chart-lifecycle-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, originalData = true, typedNotifications = true, nativeTab = true, nativeArrows = true, enter = true, radiusHover = true, stationaryMutation = true, consumerPaint = true, legend = true, fontOverride = true, liveAutomationName = true, angles = true, radii = true, disabledTab = true, realDetach = true, rapidRemount = true, templateReplacement = true, replacementCollections = true, rangeData = true, resize = true, emptyData = true }));
    }
}
