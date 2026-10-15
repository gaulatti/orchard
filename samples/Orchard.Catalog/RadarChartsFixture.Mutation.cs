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

public sealed partial class RadarChartsFixture
{
    private sealed class MutableRecord : Dictionary<string, object?>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        internal void Change(string key, object? value) { this[key] = value; PropertyChanged?.Invoke(this, new(key)); }
    }
    private async Task VerifyMutationAsync(Grid host, NativeInputSession input, string folder, BleeckerColorScheme scheme)
    {
        var first = new MutableRecord { ["subject"] = "A", ["a"] = 10d }; var second = new MutableRecord { ["subject"] = "B", ["a"] = 20d };
        var data = new ObservableCollection<IReadOnlyDictionary<string, object?>> { first, second, new MutableRecord { ["subject"] = "C", ["a"] = 30d } };
        var series = new BleeckerRadarSeries { Key = "a", Name = "Alpha" }; var definitions = new ObservableCollection<BleeckerRadarSeries> { series };
        var owner = new BleeckerRadarChart { Data = data, Series = definitions, SubjectKey = "subject", Width = 320, Height = 240, ShowLegend = false }; AutomationProperties.SetName(owner, "Live radar consumer");
        var before = new BleeckerButton { Content = "Before radar" }; var after = new BleeckerButton { Content = "After radar" }; var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Top }; stack.Children.Add(before); stack.Children.Add(owner); stack.Children.Add(after); host.Children.Clear(); host.Children.Add(stack);
        await AwaitAsync(() => owner.IsLoaded && after.IsLoaded, "Live Radar did not mount."); UpdateLayout(); var chart = Descendants<BleeckerLineChart>(owner).Single();
        Path[] Shapes(string role) => Part<Canvas>(chart, "PART_Plot").Children.OfType<Path>().Where(p => p.Tag is ValueTuple<string, int> tag && tag.Item1 == role).ToArray();
        await AwaitAsync(() => Shapes("radar-stroke").Length == 1, "Radar did not render consumer values."); Require(ReferenceEquals(chart.Data, data) && ReferenceEquals(owner.Series, definitions), "Radar cloned consumer collections.");
        await input.MoveAsync(before); Require(Part<Button>(before, "PART_Action").Focus(FocusState.Keyboard), "Could not focus preceding Radar action."); input.Key(0x09);
        await AwaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), chart) && Inspection(chart) == "A : 10", "Tab did not enter Radar's single inspection stop.");
        first.Change("a", 15d); await AwaitAsync(() => Inspection(chart) == "A : 15", "Radar record notification did not reach inspection.");
        await input.KeyAsync(0x27, chart); await AwaitAsync(() => Inspection(chart) == "B : 20", "Radar native Right did not select the next subject.");
        first.Change("alt", 17d); second.Change("alt", 22d); series.Key = "alt"; await AwaitAsync(() => Inspection(chart) == "B : 22", "Radar ignored a live series key."); series.Key = "a"; await AwaitAsync(() => Inspection(chart) == "B : 20", "Radar did not restore the original series key.");
        AutomationProperties.SetName(owner, "Renamed radar consumer"); Require(AutomationProperties.GetName(chart) == "Renamed radar consumer", "Radar ignored a live accessible name.");
        var originalFont = owner.FontFamily; owner.FontFamily = new FontFamily("Segoe UI"); await AwaitAsync(() => Part<Canvas>(chart, "PART_Plot").Children.OfType<TextBlock>().All(text => text.FontFamily.Source == "Segoe UI"), "Radar ignored a consumer font family."); owner.FontFamily = originalFont;
        var paint = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)); series.Color = paint; series.Fill = true;
        await AwaitAsync(() => ReferenceEquals(Shapes("radar-stroke")[0].Stroke, paint) && ReferenceEquals(Shapes("radar-fill")[0].Fill, paint) && Math.Abs(Shapes("radar-fill")[0].Opacity - .35) < .000001, "Radar lost live fill or consumer transparent paint.");
        owner.ShowLegend = true; series.Name = "Renamed"; await AwaitAsync(() => Part<Canvas>(chart, "PART_Legend").Children.OfType<Canvas>().SelectMany(c => c.Children.OfType<TextBlock>()).Any(t => t.Text == "Renamed"), "Radar ignored a live legend name.");
        owner.TickFormatter = value => "T" + value; await AwaitAsync(() => Inspection(chart) == "B : T20", "Radar ignored live tooltip formatting.");
        Require(Part<Canvas>(chart, "PART_Plot").Children.OfType<TextBlock>().Where(t => Equals(t.Tag, "radar-radius-tick")).All(t => t.Text.StartsWith('T')), "Radar ignored radius tick formatting."); owner.TickFormatter = null;
        await input.MoveAsync(chart, localPoint: new Point(owner.Width / 2, owner.Height / 2 - 20)); await AwaitAsync(() => Inspection(chart) == "A : 15", "Radar angular hover did not select its first subject.");
        var pointer = input.ReadPointerPosition(); first.Change("a", 25d); await AwaitAsync(() => Inspection(chart) == "A : 25", "Radar stationary hover lost live data."); await FrameAsync(); await FrameAsync(); Require(Inspection(chart) == "A : 25" && input.ReadPointerPosition() == pointer, "Radar did not retain stationary angular inspection.");
        await input.MoveAsync(before); await AwaitAsync(() => Inspection(chart) == "", "Radar exit retained inspection."); await input.KeyAsync(0x25, chart); await AwaitAsync(() => Inspection(chart) == "A : 25", "Radar hover discarded the retained keyboard index.");
        first.Change("a", new object?[] { 5d, 25d }); await AwaitAsync(() => Shapes("radar-base").Length == 1 && Inspection(chart) == "A : 525", "Radar lost a live range value."); first.Change("a", 25d); await AwaitAsync(() => Shapes("radar-base").Length == 0, "Radar retained a removed range boundary.");
        input.Key(0x09); await AwaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Part<Button>(after, "PART_Action")), "Radar interrupted forward Tab."); input.ShiftKey(0x09); await AwaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), chart), "Radar interrupted reverse Tab.");
        owner.IsEnabled = false; UpdateLayout(); Require(!chart.IsEnabled, "Disabled Radar retained enabled inspection."); Require(Part<Button>(before, "PART_Action").Focus(FocusState.Keyboard), "Could not focus before disabled Radar."); input.Key(0x09); await AwaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Part<Button>(after, "PART_Action")), "Disabled Radar did not leave native Tab traversal."); owner.IsEnabled = true;
        stack.Children.Remove(owner); await AwaitAsync(() => !owner.IsLoaded, "Radar did not detach."); await FrameAsync(); await FrameAsync();
        var observed = typeof(BleeckerLineChart).GetField("observedRadarSeries", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!; Require(observed.GetValue(chart) is null, "Detached Radar retained series observers.");
        stack.Children.Insert(1, owner); await AwaitAsync(() => owner.IsLoaded, "Radar did not remount."); stack.Children.Remove(owner); stack.Children.Insert(1, owner); await FrameAsync(); await FrameAsync();
        var oldY = ((PathGeometry)Shapes("radar-stroke")[0].Data).Figures[0].StartPoint.Y; first.Change("a", 18d); await AwaitAsync(() => ReferenceEquals(observed.GetValue(chart), definitions) && ((PathGeometry)Shapes("radar-stroke")[0].Data).Figures[0].StartPoint.Y != oldY, "Rapid Radar remount lost record updates.");
        var template = owner.Template; owner.Template = (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Grid /></ControlTemplate>"); owner.ApplyTemplate(); UpdateLayout(); await AwaitAsync(() => !chart.IsLoaded, "Old Radar template stayed mounted.");
        owner.Template = template; owner.ApplyTemplate(); UpdateLayout(); await AwaitAsync(() => Descendants<BleeckerLineChart>(owner).Any(), "Radar template did not restore."); chart = Descendants<BleeckerLineChart>(owner).Single(); await AwaitAsync(() => Shapes("radar-stroke").Length == 1 && ReferenceEquals(chart.Data, data), "Radar template lost consumer data.");
        var replacement = new BleeckerRadarSeries { Key = "a", Name = "Replacement" }; owner.Series = new() { replacement }; await AwaitAsync(() => Part<Canvas>(chart, "PART_Legend").Children.OfType<Canvas>().SelectMany(c => c.Children.OfType<TextBlock>()).Any(t => t.Text == "Replacement"), "Radar lost replacement series."); series.Name = "Discarded";
        Require(!Part<Canvas>(chart, "PART_Legend").Children.OfType<Canvas>().SelectMany(c => c.Children.OfType<TextBlock>()).Any(t => t.Text == "Discarded"), "Discarded Radar series still drives the chart.");
        owner.Width = 360; owner.Height = 260; await AwaitAsync(() => Math.Abs(((PathGeometry)Shapes("radar-stroke")[0].Data).Figures[0].StartPoint.X - 180) < .001, "Radar ignored live size changes.");
        owner.Data = new(); await AwaitAsync(() => Shapes("radar-stroke").Length == 0, "Empty Radar retained polygon shapes."); data.Clear(); Require(Shapes("radar-stroke").Length == 0, "Discarded Radar data still drives the chart.");
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"radar-chart-lifecycle-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, originalCollections = true, recordNotifications = true, nativeTab = true, nativeArrows = true, angularHover = true, stationaryMutation = true, rangeUpdates = true, fillAndPaint = true, formatters = true, legend = true, realDetach = true, rapidRemount = true, templates = true, replacementCollections = true, resize = true, emptyData = true }));
    }
}
