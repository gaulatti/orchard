using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard.Catalog;

public sealed partial class AreaChartsFixture
{
    private async Task VerifyMutationAsync(Grid host, NativeInputSession input, string folder, BleeckerColorScheme scheme)
    {
        var row = new ObservableRow(); row.Set("label", "First"); row.Set("v", 10d);
        var rows = new ObservableCollection<IReadOnlyDictionary<string, object?>> { row, new Dictionary<string, object?> { ["label"] = "Second", ["v"] = 20d } };
        var series = new BleeckerChartSeries { Key = "v", Name = "Consumer" };
        var chart = new BleeckerAreaChart { Data = rows, XAxisKey = "label", Width = 320, Height = 200, ShowLegend = false };
        chart.Series.Add(series); AutomationProperties.SetName(chart, "Live consumer comparison");
        var before = new BleeckerButton { Content = "Before areas" }; var after = new BleeckerButton { Content = "After areas" };
        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Top }; stack.Children.Add(before); stack.Children.Add(chart); stack.Children.Add(after);
        host.Children.Clear(); host.Children.Add(stack); await AwaitAsync(() => chart.IsLoaded && after.IsLoaded, "Live Area Chart did not mount."); UpdateLayout();
        var inner = Part<BleeckerLineChart>(chart, "PART_Chart");
        Path[] Areas() => Part<Canvas>(inner, "PART_Plot").Children.OfType<Path>().ToArray();
        await AwaitAsync(() => Areas().Length == 2, "Live Area Chart did not render consumer records.");
        var paint = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 25, 115, 145)); series.Color = paint;
        await AwaitAsync(() => Areas().All(path => ReferenceEquals(path.Fill ?? path.Stroke, paint)), "Area Chart ignored a live series brush.");
        paint.Color = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        Require(Areas().All(path => ReferenceEquals(path.Fill ?? path.Stroke, paint)), "Area Chart replaced transparent consumer paint.");
        await input.MoveAsync(before); Require(Part<Button>(before, "PART_Action").Focus(FocusState.Keyboard), "Could not focus action before Area Chart.");
        // Tab's release belongs to the newly focused element. Check its real
        // destination instead of applying the same-target arrow-key invariant.
        input.Key(0x09);
        await AwaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), inner) && AutomationProperties.GetHelpText(inner) == "First;  : 10", "Physical Tab did not enter the Area Chart's single inspection stop.");
        row.Set("v", 15d);
        await AwaitAsync(() => AutomationProperties.GetHelpText(inner) == "First;  : 15", "Observable record change did not reach Area Chart inspection.");
        await input.KeyAsync(0x27, inner);
        await AwaitAsync(() => AutomationProperties.GetHelpText(inner) == "Second;  : 20", "Physical Right did not inspect the next category.");
        await input.MoveAsync(inner, localPoint: new Windows.Foundation.Point(inner.ActualWidth * .2, inner.ActualHeight * .5));
        await AwaitAsync(() => AutomationProperties.GetHelpText(inner) == "First;  : 15", "Area Chart pointer hover did not inspect the category.");
        await input.MoveAsync(before);
        await AwaitAsync(() => AutomationProperties.GetHelpText(inner) == "", "Pointer exit retained Area Chart inspection.");
        await input.KeyAsync(0x25, inner);
        await AwaitAsync(() => AutomationProperties.GetHelpText(inner) == "First;  : 15", "Pointer inspection discarded the retained keyboard index.");
        chart.Stacked = true;
        var second = new BleeckerChartSeries { Key = "v", Name = "Second series" }; chart.Series.Add(second);
        double StartY(int path) => ((PathGeometry)Areas()[path].Data).Figures[0].StartPoint.Y;
        await AwaitAsync(() => Areas().Length == 4 && StartY(3) < StartY(1), "Live stacked upper boundary did not accumulate the consumer values.");
        chart.Stacked = false;
        await AwaitAsync(() => Areas().Length == 4 && StartY(3) == StartY(1), "Live unstacked boundaries did not return to overlapping values.");
        chart.Stacked = true;
        await AwaitAsync(() => Areas().Length == 4 && StartY(3) < StartY(1), "Live restacking did not restore the accumulated boundary.");
        chart.Series.Remove(second);
        await AwaitAsync(() => Areas().Length == 2 && AutomationProperties.GetHelpText(inner) == "First;  : 15", "Removing a series left stale geometry or inspection payload.");
        await input.KeyAsync(0x27, inner);
        await AwaitAsync(() => AutomationProperties.GetHelpText(inner) == "Second;  : 20", "Area Chart Right navigation did not advance inspection.");
        await input.KeyAsync(0x0D, inner);
        await AwaitAsync(() => AutomationProperties.GetHelpText(inner) == "", "Enter did not hide Area Chart inspection.");
        await input.KeyAsync(0x0D, inner);
        await AwaitAsync(() => AutomationProperties.GetHelpText(inner) == "Second;  : 20", "Enter did not restore the retained category.");
        input.Key(0x09);
        await AwaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Part<Button>(after, "PART_Action")), "Area Chart interrupted forward Tab.");
        input.ShiftKey(0x09);
        await AwaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), inner), "Area Chart interrupted reverse Tab.");
        chart.ShowGrid = false;
        await AwaitAsync(() => !Part<Canvas>(inner, "PART_Plot").Children.OfType<Microsoft.UI.Xaml.Shapes.Line>().Any(), "Area Chart ignored grid visibility.");
        chart.ShowLegend = true;
        await AwaitAsync(() => Part<Canvas>(inner, "PART_Legend").Children.Count == 1, "Area Chart ignored legend visibility.");
        chart.IsEnabled = false; UpdateLayout(); Require(!inner.IsEnabled, "Disabled Area Chart retained an enabled inspection child."); chart.IsEnabled = true;
        stack.Children.Remove(chart); await AwaitAsync(() => !chart.IsLoaded, "Area Chart did not unmount.");
        await FrameAsync(); await FrameAsync();
        var observedField = typeof(BleeckerLineChart).GetField("observedData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        Require(!inner.IsLoaded && observedField.GetValue(inner) is null, "Detached Area Chart retained data observers.");
        row.Set("v", 18d);
        stack.Children.Insert(1, chart); await AwaitAsync(() => chart.IsLoaded && Areas().Length == 2, "Area Chart did not remount changed records.");
        stack.Children.Remove(chart); stack.Children.Insert(1, chart);
        await FrameAsync(); await FrameAsync();
        var priorY = StartY(1);
        row.Set("v", 19d);
        await AwaitAsync(() => StartY(1) < priorY && ReferenceEquals(observedField.GetValue(inner), rows), "A delayed unload detached the remounted Area Chart's live records.");
        var original = chart.Template;
        chart.Template = (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Grid /></ControlTemplate>"); chart.ApplyTemplate(); UpdateLayout();
        await AwaitAsync(() => !inner.IsLoaded, "Detached Area Chart renderer remained mounted.");
        chart.Template = original; chart.ApplyTemplate(); UpdateLayout(); inner = Part<BleeckerLineChart>(chart, "PART_Chart");
        await AwaitAsync(() => inner.IsLoaded && Areas().Length == 2, "Area Chart did not restore its consumer collections after template replacement.");
        Require(ReferenceEquals(inner.Data, rows) && ReferenceEquals(inner.Series, chart.Series) && AutomationProperties.GetName(inner) == "Live consumer comparison", "Template restoration lost consumer identity or naming.");
        var replacement = new ObservableCollection<IReadOnlyDictionary<string, object?>> { new Dictionary<string, object?> { ["label"] = "Changed", ["v"] = 12d } }; chart.Data = replacement;
        await AwaitAsync(() => Areas().Length == 0 && Part<Canvas>(inner, "PART_Plot").Children.OfType<Microsoft.UI.Xaml.Shapes.Ellipse>().Count() == 1, "Replacement Area Chart data did not reach rendered bars."); rows.Clear();
        Require(ReferenceEquals(inner.Data, replacement) && replacement.Count == 1, "Area Chart mutated replacement consumer records.");
        chart.Data = new() { new Dictionary<string, object?> { ["label"] = "Range", ["v"] = new object[] { 10d, 30d } } };
        await AwaitAsync(() => Areas().Length == 0 && Part<Canvas>(inner, "PART_Plot").Children.OfType<Microsoft.UI.Xaml.Shapes.Ellipse>().Count() == 1, "Area Chart omitted consumer range data.");
        Require(inner.Focus(FocusState.Keyboard), "Could not focus restored range inspection.");
        await AwaitAsync(() => AutomationProperties.GetHelpText(inner) == "Range;  : 1030", "Area Chart range tooltip differs from the rendered source.");
        chart.Series.Clear(); await AwaitAsync(() => Areas().Length == 0, "Empty Area Chart series retained live bars.");
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"area-chart-lifecycle-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, physicalTab = true, physicalArrows = true, pointerCategories = true, retainedKeyboard = true, recordNotifications = true, liveStacking = true, templates = true, collectionIdentity = true, transparentPaint = true }));
    }
    private sealed class ObservableRow : Dictionary<string, object?>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        internal void Set(string key, object? value) { this[key] = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key)); }
    }
}
