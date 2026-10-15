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

public sealed partial class FunnelChartsFixture
{
    private async Task VerifyMutationAsync(Grid host, NativeInputSession input, string folder, BleeckerColorScheme scheme)
    {
        var a = new BleeckerFunnelChartDatum { Name = "A", Value = 30 }; var b = new BleeckerFunnelChartDatum { Name = "B", Value = 20 };
        var data = new ObservableCollection<IReadOnlyDictionary<string, object?>> { a, b, new BleeckerFunnelChartDatum { Name = "C", Value = 10 } };
        var owner = new BleeckerFunnelChart { Data = data, Width = 320, Height = 240 }; AutomationProperties.SetName(owner, "Live funnel consumer");
        var before = new BleeckerButton { Content = "Before funnel" }; var after = new BleeckerButton { Content = "After funnel" }; var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Top }; stack.Children.Add(before); stack.Children.Add(owner); stack.Children.Add(after); host.Children.Clear(); host.Children.Add(stack);
        await AwaitAsync(() => owner.IsLoaded && after.IsLoaded, "Live Funnel did not mount."); UpdateLayout(); var chart = Descendants<BleeckerLineChart>(owner).Single();
        Path[] Bars() => Part<Canvas>(chart, "PART_Plot").Children.OfType<Path>().Where(path => path.Tag is ValueTuple<string, int> tag && tag.Item1 == "funnel").ToArray();
        TextBlock[] Labels() => Part<Canvas>(chart, "PART_Plot").Children.OfType<TextBlock>().Where(label => label.Tag is ValueTuple<string, int, int> tag && tag.Item1 == "funnel-label").ToArray();
        Point Edge(int index) => ((LineSegment)((PathGeometry)Bars()[index].Data).Figures[0].Segments[0]).Point;
        string Inspection() => AutomationProperties.GetHelpText(chart);
        static Button Action(BleeckerButton owner) => (Button)((FrameworkElement)VisualTreeHelper.GetChild(owner, 0)).FindName("PART_Action");
        await AwaitAsync(() => Bars().Length == 3 && Labels().Length == 3, "Funnel lost live records or labels."); Require(ReferenceEquals(chart.Data, data), "Funnel cloned its original collection.");
        await input.MoveAsync(before); Require(Action(before).Focus(FocusState.Keyboard), "Could not anchor native Funnel Tab entry."); input.Key(0x09); await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), chart) && Inspection() == " : 30", "Native Tab did not reach one Funnel inspection stop.");
        foreach (ushort key in new ushort[] { 0x27, 0x25, 0x28, 0x26 }) { await input.KeyAsync(key, chart); Require(Inspection() == " : 30", "Funnel arrows changed the pinned producer's initial selection."); }
        await input.KeyAsync(0x0D, chart); await AwaitAsync(() => Inspection() == "", "Funnel Enter did not hide inspection."); await input.KeyAsync(0x0D, chart); await AwaitAsync(() => Inspection() == " : 30", "Funnel Enter did not restore inspection.");
        await input.MoveAsync(chart, localPoint: new Point(160, 110)); await AwaitAsync(() => Inspection() == " : 20", "Funnel hover did not inspect the actual second trapezoid.");
        var pointer = input.ReadPointerPosition(); b.Value = 15; await AwaitAsync(() => Inspection() == " : 15", "Funnel ignored typed value notifications under stationary hover."); await FrameAsync(); await FrameAsync(); Require(Inspection() == " : 15" && pointer == input.ReadPointerPosition(), "Funnel lost stationary inspection after repaint.");
        var paint = new SolidColorBrush(Microsoft.UI.Colors.Transparent); a.Fill = paint; await AwaitAsync(() => ReferenceEquals(Bars()[0].Fill, paint), "Funnel replaced a transparent consumer brush.");
        a.Name = "Updated"; await AwaitAsync(() => Labels().Any(label => label.Text == "Updated: "), "Funnel ignored a live consumer name."); owner.ShowLabels = false; await AwaitAsync(() => Labels().Length == 0, "Funnel did not hide labels."); owner.ShowLabels = true; await AwaitAsync(() => Labels().Length == 3, "Funnel did not restore labels.");
        var family = owner.FontFamily; owner.FontFamily = new FontFamily("Segoe UI"); await AwaitAsync(() => Labels().All(label => label.FontFamily.Source == "Segoe UI"), "Funnel ignored its consumer font family."); owner.FontFamily = family;
        AutomationProperties.SetName(owner, "Renamed funnel consumer"); Require(AutomationProperties.GetName(chart) == "Renamed funnel consumer", "Funnel ignored a live automation name.");
        owner.ValueFormatter = value => "Consumer " + value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture); await AwaitAsync(() => Inspection() == " : Consumer 15.0", "Funnel ignored a live value formatter."); Require(Labels().Any(label => label.Text == "Updated: "), "Funnel formatter changed source name-only labels."); owner.ValueFormatter = null; await AwaitAsync(() => Inspection() == " : 15", "Funnel did not restore unformatted inspection.");
        await input.MoveAsync(before); await AwaitAsync(() => Inspection() == "", "Funnel leave retained mouse inspection."); input.Key(0x09); await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), Action(after)), "Funnel interrupted forward Tab."); input.ShiftKey(0x09); await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), chart), "Funnel interrupted reverse Tab.");
        owner.IsEnabled = false; UpdateLayout(); Require(!chart.IsEnabled, "Disabled Funnel retained enabled input."); Require(Action(before).Focus(FocusState.Keyboard), "Could not anchor disabled Funnel traversal."); input.Key(0x09); await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), Action(after)), "Disabled Funnel did not leave Tab traversal."); owner.IsEnabled = true;
        var observed = typeof(BleeckerLineChart).GetField("observedData", BindingFlags.Instance | BindingFlags.NonPublic)!; stack.Children.Remove(owner); await AwaitAsync(() => !owner.IsLoaded, "Funnel did not detach."); await FrameAsync(); await FrameAsync(); Require(observed.GetValue(chart) is null, "Detached Funnel retained record observers.");
        stack.Children.Insert(1, owner); await AwaitAsync(() => owner.IsLoaded, "Funnel did not remount."); stack.Children.Remove(owner); stack.Children.Insert(1, owner); await FrameAsync(); await FrameAsync(); var old = Edge(1); b.Value = 18; await AwaitAsync(() => ReferenceEquals(observed.GetValue(chart), data) && Edge(1) != old, "Rapid Funnel remount lost live record geometry.");
        var template = owner.Template; owner.Template = (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Grid /></ControlTemplate>"); owner.ApplyTemplate(); UpdateLayout(); await AwaitAsync(() => !chart.IsLoaded, "Old Funnel template remained mounted."); owner.Template = template; owner.ApplyTemplate(); UpdateLayout(); await AwaitAsync(() => Descendants<BleeckerLineChart>(owner).Any(), "Funnel template did not restore."); chart = Descendants<BleeckerLineChart>(owner).Single(); await AwaitAsync(() => Bars().Length == 3 && ReferenceEquals(chart.Data, data), "Funnel template lost original records.");
        old = Edge(0); owner.Width = 360; owner.Height = 260; await AwaitAsync(() => Math.Abs(chart.ActualWidth - 360) < .001 && Math.Abs(chart.ActualHeight - 260) < .001 && Edge(0) != old, "Funnel ignored live size changes.");
        owner.Data = new() { new BleeckerFunnelChartDatum { Name = "Replacement", Value = 12 } }; await AwaitAsync(() => Bars().Length == 1 && !ReferenceEquals(chart.Data, data), "Funnel lost collection replacement."); data.Clear(); b.Value = 999; Require(Bars().Length == 1, "Discarded Funnel records affected the current chart."); owner.Data.Clear(); await AwaitAsync(() => Bars().Length == 0 && Labels().Length == 0 && Inspection() == "", "Empty Funnel retained shapes, labels or inspection.");
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"funnel-chart-lifecycle-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, originalData = true, typedNotifications = true, nativeTab = true, nativeArrows = true, enter = true, actualShapeHover = true, stationaryMutation = true, consumerPaint = true, liveName = true, labelVisibility = true, fontOverride = true, liveAutomationName = true, formatter = true, disabledTab = true, realDetach = true, rapidRemount = true, templateReplacement = true, replacementCollections = true, resize = true, emptyData = true }));
    }
}
