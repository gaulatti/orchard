using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard.Catalog;

public sealed partial class PieChartsFixture
{
    private async Task VerifyMutationAsync(Grid host, NativeInputSession input, string folder, BleeckerColorScheme scheme)
    {
        foreach (var donut in new[] { false, true })
        {
            var first = new BleeckerPieChartDatum { Name = "First", Value = 10 };
            var second = new BleeckerPieChartDatum { Name = "Second", Value = 20 };
            var rows = new ObservableCollection<IReadOnlyDictionary<string, object?>> { first, second };
            Control owner = donut ? new BleeckerDonutChart { Data = rows, CenterValue = "30", CenterLabel = "Consumer", ShowLegend = false } : new BleeckerPieChart { Data = rows, ShowLegend = false };
            owner.Width = 320; owner.Height = 200; AutomationProperties.SetName(owner, "Live circular consumer");
            var before = new BleeckerButton { Content = "Before circular chart" }; var after = new BleeckerButton { Content = "After circular chart" };
            var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Top }; stack.Children.Add(before); stack.Children.Add(owner); stack.Children.Add(after); host.Children.Clear(); host.Children.Add(stack);
            await AwaitAsync(() => owner.IsLoaded && after.IsLoaded, "Live circular chart did not mount."); UpdateLayout();
            var chart = Descendants<BleeckerLineChart>(owner).Single(); Path[] Slices() => Part<Canvas>(chart, "PART_Plot").Children.OfType<Path>().ToArray();
            await AwaitAsync(() => Slices().Length == 2, "Live circular records did not render.");
            Require(ReferenceEquals(chart.Data, rows) && ReferenceEquals(rows[0], first), "Circular chart cloned consumer data.");
            await input.MoveAsync(before); Require(Part<Button>(before, "PART_Action").Focus(FocusState.Keyboard), "Could not focus preceding action."); input.Key(0x09);
            await AwaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), chart) && AutomationProperties.GetHelpText(chart) == " : 10", "Tab did not enter the single circular inspection stop.");
            first.Value = 15; await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == " : 15", "Typed datum change did not reach circular inspection.");
            await input.KeyAsync(0x27, chart); await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == " : 20", "Right did not inspect the second slice.");
            await input.KeyAsync(0x0D, chart); await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "", "Enter did not hide circular inspection.");
            await input.KeyAsync(0x0D, chart); await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == " : 20", "Enter did not restore circular inspection.");
            var angle = ((15d / 35 * 356) / 2) * Math.PI / 180; var radial = donut ? 66.5 : 38;
            await input.MoveAsync(chart, localPoint: new Point(160 + radial * Math.Cos(angle), 100 - radial * Math.Sin(angle)));
            await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == " : 15", "Native slice hover did not inspect the consumer value.");
            await input.MoveAsync(before); await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "", "Leaving a slice retained pointer inspection.");
            await input.KeyAsync(0x25, chart); await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == " : 15", "Pointer hover discarded the retained keyboard index.");
            var paint = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 25, 115, 145)); first.Color = paint;
            await AwaitAsync(() => ReferenceEquals(Slices()[0].Fill, paint), "Live datum brush did not reach the slice."); paint.Color = Windows.UI.Color.FromArgb(0, 0, 0, 0); Require(ReferenceEquals(Slices()[0].Fill, paint), "Transparent datum brush was replaced.");
            first.Name = "Renamed"; await AwaitAsync(() => Part<Canvas>(chart, "PART_Plot").Children.OfType<TextBlock>().First().Text.StartsWith("Renamed:", StringComparison.Ordinal), "Datum name did not reach its label.");
            if (owner is BleeckerDonutChart ring) { ring.CenterValue = "0"; ring.CenterLabel = null; await AwaitAsync(() => Part<Canvas>(ring, "PART_Center").Children.OfType<TextBlock>().Single().Text == "0", "Donut center ignored string zero or label removal."); ring.ShowLegend = true; }
            else { var pie = (BleeckerPieChart)owner; pie.InnerRadius = "60%"; pie.ShowLegend = true; await AwaitAsync(() => ((PathGeometry)Slices()[0].Data).Figures[0].Segments.OfType<ArcSegment>().Count() == 2, "Live radius did not produce a ring."); }
            await AwaitAsync(() => Part<Canvas>(chart, "PART_Legend").Children.Count == 2, "Legend toggle did not reach circular data.");
            input.Key(0x09); await AwaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Part<Button>(after, "PART_Action")), "Circular chart interrupted forward Tab."); input.ShiftKey(0x09); await AwaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), chart), "Circular chart interrupted reverse Tab.");
            owner.IsEnabled = false; UpdateLayout(); Require(!chart.IsEnabled, "Disabled circular chart retained enabled inspection."); owner.IsEnabled = true;
            stack.Children.Remove(owner); await AwaitAsync(() => !owner.IsLoaded, "Circular chart did not detach."); await FrameAsync(); await FrameAsync();
            var observed = typeof(BleeckerLineChart).GetField("observedData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            Require(!chart.IsLoaded && observed.GetValue(chart) is null, "Detached circular chart retained observers.");
            stack.Children.Insert(1, owner); await AwaitAsync(() => owner.IsLoaded, "Circular chart did not remount."); stack.Children.Remove(owner); stack.Children.Insert(1, owner); await FrameAsync(); await FrameAsync();
            second.Value = 25; await AwaitAsync(() => ReferenceEquals(observed.GetValue(chart), rows) && Part<Canvas>(chart, "PART_Plot").Children.OfType<TextBlock>().Any(text => text.Text.Contains("63%", StringComparison.Ordinal)), "Rapid circular remount lost its live datum updates.");
            var template = owner.Template; owner.Template = (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Grid /></ControlTemplate>"); owner.ApplyTemplate(); UpdateLayout(); await AwaitAsync(() => !chart.IsLoaded, "Old circular template remained mounted.");
            owner.Template = template; owner.ApplyTemplate(); UpdateLayout(); await AwaitAsync(() => Descendants<BleeckerLineChart>(owner).Any(), "Circular template did not restore."); chart = Descendants<BleeckerLineChart>(owner).Single();
            await AwaitAsync(() => ReferenceEquals(chart.Data, rows) && Slices().Length == 2, "Circular template lost original consumer records.");
            var replacement = new ObservableCollection<IReadOnlyDictionary<string, object?>> { new BleeckerPieChartDatum { Name = "Changed", Value = 12 } };
            if (owner is BleeckerDonutChart d) d.Data = replacement; else ((BleeckerPieChart)owner).Data = replacement;
            await AwaitAsync(() => Slices().Length == 1 && ReferenceEquals(chart.Data, replacement), "Circular collection replacement did not reach geometry."); rows.Clear();
            replacement.Clear(); await AwaitAsync(() => Slices().Length == 0, "Empty circular data retained slices.");
            if (owner is BleeckerDonutChart emptyRing)
            {
                Require(Part<Canvas>(emptyRing, "PART_Center").Children.OfType<TextBlock>().Single().Text == "0", "Empty donut data erased independent center text.");
                emptyRing.CenterLabel = "Consumer font"; emptyRing.FontFamily = new FontFamily("Consolas"); UpdateLayout();
                Require(Part<Canvas>(emptyRing, "PART_Center").Children.OfType<TextBlock>().All(text => text.FontFamily.Source == "Consolas"), "Donut center replaced the consumer font override.");
            }
        }
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"pie-chart-lifecycle-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, typedData = true, nativeTab = true, nativeArrowsEnter = true, nativeHover = true, liveRadius = true, liveCenter = true, transparentBrush = true, realDetach = true, rapidRemount = true, templates = true, originalCollections = true }));
    }
}
