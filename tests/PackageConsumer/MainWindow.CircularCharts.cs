using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Orchard;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private async Task VerifyCircularChartsAsync()
    {
        foreach (Control owner in new Control[] { PackagePieChart, PackageDonutChart })
        {
            var donut = owner is BleeckerDonutChart;
            var rows = donut ? PackageDonutChart.Data : PackagePieChart.Data;
            var datum = (BleeckerPieChartDatum)rows[0]; datum.Value = 10; datum.Color = null;
            var parent = (Panel)VisualTreeHelper.GetParent(owner); var position = parent.Children.IndexOf(owner);
            parent.Children.Remove(owner); await AwaitAsync(() => !owner.IsLoaded, "Packaged circular chart did not unmount.");
            Root.Children.Add(owner); owner.VerticalAlignment = VerticalAlignment.Top;
            try
            {
                await AwaitAsync(() => owner.IsLoaded, "Packaged circular chart did not remount."); owner.ApplyTemplate(); Root.UpdateLayout();
                var chart = BrandDescendants<BleeckerLineChart>(owner).Single();
                var plot = BrandDescendants<Canvas>(chart).Single(canvas => canvas.Name == "PART_Plot");
                Microsoft.UI.Xaml.Shapes.Path[] Slices() => plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().ToArray();
                await AwaitAsync(() => Slices().Length == 2, "Packaged circular records did not produce sectors.");
                var figure = ((PathGeometry)Slices()[0].Data).Figures[0]; var arcs = figure.Segments.OfType<ArcSegment>().ToArray();
                Require(Math.Abs(figure.StartPoint.X - 236) < .001 && Math.Abs(figure.StartPoint.Y - 100) < .001 && figure.IsClosed && arcs.Length == (donut ? 2 : 1) && (!donut || arcs[1].Size.Width == 57), "Packaged circular source geometry differs.");
                Require(ReferenceEquals(chart.Data, rows) && !owner.IsTabStop && chart.IsTabStop && AutomationProperties.GetName(chart) == AutomationProperties.GetName(owner) && FrameworkElementAutomationPeer.CreatePeerForElement(chart).GetClassName() == (donut ? nameof(BleeckerDonutChart) : nameof(BleeckerPieChart)), "Packaged circular chart lost original records or its single inspection boundary.");
                await AcquireConsumerInputAsync(); Require(chart.Focus(FocusState.Keyboard), "Could not focus packaged circular chart.");
                await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == " : 10", "Packaged circular focus did not inspect its first value.");
                datum.Value = 15; await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == " : 15", "Packaged typed datum notification did not reach inspection.");
                var paint = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)); datum.Color = paint;
                await AwaitAsync(() => ReferenceEquals(Slices()[0].Fill, paint), "Packaged circular chart replaced the consumer's transparent brush.");
                var keys = new[] { new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = 0x27 } } }, new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = 0x27, Flags = 2 } } } };
                Require(SendInput(2, keys, Marshal.SizeOf<ConsumerInput>()) == 2, "Packaged circular Right did not enter native input.");
                await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == " : 20", "Packaged circular Right did not inspect its second value.");
                if (donut)
                {
                    PackageDonutChart.CenterValue = "0"; PackageDonutChart.CenterLabel = null;
                    await AwaitAsync(() => BrandDescendants<Canvas>(owner).Single(canvas => canvas.Name == "PART_Center").Children.OfType<TextBlock>().Single().Text == "0", "Packaged Donut lost zero center text or live label removal.");
                    PackageDonutChart.Data = new(); await AwaitAsync(() => Slices().Length == 0, "Packaged Donut retained empty sectors."); PackageDonutChart.Data = rows;
                }
                else
                {
                    PackagePieChart.InnerRadius = "60%";
                    await AwaitAsync(() => ((PathGeometry)Slices()[0].Data).Figures[0].Segments.OfType<ArcSegment>().Count() == 2, "Packaged Pie ignored a live radius.");
                    PackagePieChart.Data = new(); await AwaitAsync(() => Slices().Length == 0, "Packaged Pie retained empty sectors."); PackagePieChart.Data = rows; PackagePieChart.InnerRadius = 0d;
                }
                await AwaitAsync(() => Slices().Length == 2 && ReferenceEquals(chart.Data, rows), "Packaged circular chart did not restore the original collection.");
            }
            finally { Root.Children.Remove(owner); parent.Children.Insert(position, owner); }
        }
    }
}
