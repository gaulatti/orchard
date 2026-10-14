using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard.Catalog;

public sealed partial class BarChartsFixture
{
    private async Task VerifyMutationAsync(Grid host, NativeInputSession input, string folder, BleeckerColorScheme scheme)
    {
        var row = new ObservableRow(); row.Set("label", "First"); row.Set("v", 10d);
        var rows = new ObservableCollection<IReadOnlyDictionary<string, object?>> { row, new Dictionary<string, object?> { ["label"] = "Second", ["v"] = 20d } };
        var series = new BleeckerChartSeries { Key = "v", Name = "Consumer" };
        var chart = new BleeckerBarChart { Data = rows, XAxisKey = "label", Width = 320, Height = 200, ShowLegend = false };
        chart.Series.Add(series); AutomationProperties.SetName(chart, "Live consumer comparison");
        var before = new BleeckerButton { Content = "Before bars" }; var after = new BleeckerButton { Content = "After bars" };
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Top }; stack.Children.Add(before); stack.Children.Add(chart); stack.Children.Add(after);
        host.Children.Clear(); host.Children.Add(stack); await AwaitAsync(() => chart.IsLoaded && after.IsLoaded, "Live Bar Chart did not mount."); UpdateLayout();
        var inner = Part<BleeckerLineChart>(chart, "PART_Chart");
        Path[] Bars() => Part<Canvas>(inner, "PART_Plot").Children.OfType<Path>().ToArray();
        await AwaitAsync(() => Bars().Length == 2, "Live Bar Chart did not render consumer records.");
        var paint = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 25, 115, 145)); series.Color = paint;
        await AwaitAsync(() => Bars().All(bar => ReferenceEquals(bar.Fill, paint)), "Bar Chart ignored a live series brush.");
        paint.Color = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        Require(Bars().All(bar => ReferenceEquals(bar.Fill, paint)), "Bar Chart replaced transparent consumer paint.");
        await input.MoveAsync(before); Require(Part<Button>(before, "PART_Action").Focus(FocusState.Keyboard), "Could not focus action before Bar Chart.");
        // Tab's release belongs to the newly focused element. Check its real
        // destination instead of applying the same-target arrow-key invariant.
        input.Key(0x09);
        await AwaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), inner) && AutomationProperties.GetHelpText(inner) == "First;  : 10", "Physical Tab did not enter the Bar Chart's single inspection stop.");
        row.Set("v", 15d);
        await AwaitAsync(() => AutomationProperties.GetHelpText(inner) == "First;  : 15", "Observable record change did not reach Bar Chart inspection.");
        await input.KeyAsync(0x27, inner);
        await AwaitAsync(() => AutomationProperties.GetHelpText(inner) == "Second;  : 20", "Physical Right did not inspect the next category.");
        await input.MoveAsync(inner);
        await AwaitAsync(() => AutomationProperties.GetHelpText(inner) == "First;  : 15", "Bar Chart pointer hover did not inspect the category band.");
        await input.MoveAsync(before);
        await AwaitAsync(() => AutomationProperties.GetHelpText(inner) == "", "Pointer exit retained Bar Chart inspection.");
        await input.KeyAsync(0x25, inner);
        await AwaitAsync(() => AutomationProperties.GetHelpText(inner) == "First;  : 15", "Pointer inspection discarded the retained keyboard index.");
        chart.Layout = BleeckerBarChartLayout.Vertical;
        await AwaitAsync(() => Bars().Length == 2 && ((PathGeometry)Bars()[0].Data).Figures[0].StartPoint.X == 60, "Live orientation change did not produce horizontal bars.");
        await input.KeyAsync(0x25, inner);
        await AwaitAsync(() => AutomationProperties.GetHelpText(inner) == "Second;  : 20", "Vertical source Left navigation did not advance inspection.");
        await input.KeyAsync(0x0D, inner);
        await AwaitAsync(() => AutomationProperties.GetHelpText(inner) == "", "Enter did not hide Bar Chart inspection.");
        await input.KeyAsync(0x0D, inner);
        await AwaitAsync(() => AutomationProperties.GetHelpText(inner) == "Second;  : 20", "Enter did not restore the retained category.");
        input.Key(0x09);
        await AwaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Part<Button>(after, "PART_Action")), "Bar Chart interrupted forward Tab.");
        input.ShiftKey(0x09);
        await AwaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), inner), "Bar Chart interrupted reverse Tab.");
        chart.ShowGrid = false;
        await AwaitAsync(() => !Part<Canvas>(inner, "PART_Plot").Children.OfType<Microsoft.UI.Xaml.Shapes.Line>().Any(), "Bar Chart ignored grid visibility.");
        chart.ShowLegend = true;
        await AwaitAsync(() => Part<Canvas>(inner, "PART_Legend").Children.Count == 1, "Bar Chart ignored legend visibility.");
        chart.IsEnabled = false; UpdateLayout(); Require(!inner.IsEnabled, "Disabled Bar Chart retained an enabled inspection child."); chart.IsEnabled = true;
        stack.Children.Remove(chart); await AwaitAsync(() => !chart.IsLoaded, "Bar Chart did not unmount."); row.Set("v", 18d);
        stack.Children.Insert(1, chart); await AwaitAsync(() => chart.IsLoaded && Bars().Length == 2, "Bar Chart did not remount changed records.");
        var original = chart.Template;
        chart.Template = (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Grid /></ControlTemplate>"); chart.ApplyTemplate(); UpdateLayout();
        await AwaitAsync(() => !inner.IsLoaded, "Detached Bar Chart renderer remained mounted.");
        chart.Template = original; chart.ApplyTemplate(); UpdateLayout(); inner = Part<BleeckerLineChart>(chart, "PART_Chart");
        await AwaitAsync(() => inner.IsLoaded && Bars().Length == 2, "Bar Chart did not restore its consumer collections after template replacement.");
        Require(ReferenceEquals(inner.Data, rows) && ReferenceEquals(inner.Series, chart.Series) && AutomationProperties.GetName(inner) == "Live consumer comparison", "Template restoration lost consumer identity or naming.");
        var replacement = new ObservableCollection<IReadOnlyDictionary<string, object?>> { new Dictionary<string, object?> { ["label"] = "Changed", ["v"] = 12d } }; chart.Data = replacement;
        await AwaitAsync(() => Bars().Length == 1, "Replacement Bar Chart data did not reach rendered bars."); rows.Clear();
        Require(ReferenceEquals(inner.Data, replacement) && replacement.Count == 1, "Bar Chart mutated replacement consumer records.");
        chart.Data = new() { new Dictionary<string, object?> { ["label"] = "Range", ["v"] = new object[] { 10d, 30d } } };
        await AwaitAsync(() => Bars().Length == 1, "Bar Chart omitted consumer range data.");
        Require(inner.Focus(FocusState.Keyboard), "Could not focus restored range inspection.");
        await AwaitAsync(() => AutomationProperties.GetHelpText(inner) == "Range;  : 1030", "Bar Chart range tooltip differs from the rendered source.");
        chart.Series.Clear(); await AwaitAsync(() => Bars().Length == 0, "Empty Bar Chart series retained live bars.");
        var rejected = false; try { chart.Layout = (BleeckerBarChartLayout)99; } catch (ArgumentOutOfRangeException) { rejected = true; }
        Require(rejected && chart.Layout == BleeckerBarChartLayout.Vertical, "Bar Chart accepted an unknown layout or changed the consumer's previous value.");
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"bar-chart-lifecycle-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, physicalTab = true, physicalArrows = true, pointerBands = true, retainedKeyboard = true, recordNotifications = true, liveLayout = true, templates = true, collectionIdentity = true, transparentPaint = true }));
    }
    private sealed class ObservableRow : Dictionary<string, object?>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        internal void Set(string key, object? value) { this[key] = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key)); }
    }
}
