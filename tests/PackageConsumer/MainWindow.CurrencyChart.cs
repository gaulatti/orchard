using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Orchard;
using ShapePath = Microsoft.UI.Xaml.Shapes.Path;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private static System.Collections.ObjectModel.ObservableCollection<object?> CurrencyData() => new()
    {
        new Dictionary<string, object?> { ["name"] = "Harbor", ["total"] = 100d }, new Dictionary<string, object?> { ["name"] = "Reed", ["total"] = 220d },
        new Dictionary<string, object?> { ["name"] = "Vale", ["total"] = 140d }, new Dictionary<string, object?> { ["name"] = "North", ["total"] = 310d }
    };
    private async Task VerifyCurrencyChartAsync()
    {
        var owner = PackageCurrencyChart; var data = CurrencyData(); owner.Data = data;
        var parent = (Panel)VisualTreeHelper.GetParent(owner); var position = parent.Children.IndexOf(owner); parent.Children.Remove(owner); await AwaitAsync(() => !owner.IsLoaded, "Packaged Chart did not unmount."); Root.Children.Add(owner); owner.VerticalAlignment = VerticalAlignment.Top;
        try
        {
            await AwaitAsync(() => owner.IsLoaded, "Packaged Chart did not remount."); owner.ApplyTemplate(); Root.UpdateLayout(); var chart = BrandDescendants<BleeckerLineChart>(owner).Single(); var plot = BrandDescendants<Canvas>(owner).Single(canvas => canvas.Name == "PART_Plot");
            await AwaitAsync(() => plot.Children.OfType<ShapePath>().Count() == 1, "Packaged Chart did not render its currency curve.");
            var figure = ((PathGeometry)plot.Children.OfType<ShapePath>().Single().Data).Figures.Single();
            Require(Math.Abs(figure.StartPoint.X - 60) < .001 && Math.Abs(figure.StartPoint.Y - 115) < .001 && figure.Segments.Count == 3 && figure.Segments.All(segment => segment is BezierSegment), "Packaged Chart lost the source's 320x200 currency layout.");
            Require(ReferenceEquals(owner.Data, data) && !owner.IsTabStop && chart.IsTabStop && FrameworkElementAutomationPeer.CreatePeerForElement(chart).GetClassName() == nameof(BleeckerChart) && AutomationProperties.GetName(chart) == "Independent currency totals" && BrandDescendants<Canvas>(chart).Single(canvas => canvas.Name == "PART_Legend").Children.Count == 0, "Packaged Chart lost original records, identity or fixed legend behavior.");
            Require(plot.Children.OfType<TextBlock>().Any(label => label.Text == "$320") && plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Line>().Count() == 5, "Packaged Chart lost currency ticks or grid.");
            await AcquireConsumerInputAsync(); Require(chart.Focus(FocusState.Keyboard), "Packaged Chart refused focus."); await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "Harbor; Total : $100", "Packaged Chart did not inspect its original first record.");
            var active = BrandDescendants<Canvas>(chart).Single(canvas => canvas.Name == "PART_Active"); var dot = active.Children.OfType<Microsoft.UI.Xaml.Shapes.Ellipse>().Single(); Require((dot.Width - dot.StrokeThickness) / 2 == 6 && (dot.Height - dot.StrokeThickness) / 2 == 6 && dot.StrokeThickness == 2, "Packaged Chart lost its six-unit active radius.");
            data[0] = new Dictionary<string, object?> { ["name"] = "Raw value", ["total"] = "10.00" }; await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "Raw value; Total : $10.00", "Packaged Chart normalized the raw currency string.");
            owner.Data = new() { null, new Dictionary<string, object?> { ["name"] = "B", ["total"] = 20d }, false }; await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "B; Total : $20", "Packaged Chart lost null/primitive source records.");
            owner.Data.Clear(); await AwaitAsync(() => !plot.Children.OfType<ShapePath>().Any() && AutomationProperties.GetHelpText(chart) == "" && !active.Children.Any(), "Packaged Chart retained empty data output.");
        }
        finally { Root.Children.Remove(owner); parent.Children.Insert(position, owner); }
    }
}
