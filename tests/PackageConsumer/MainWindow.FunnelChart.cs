using System.Runtime.InteropServices;
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
    private async Task VerifyFunnelChartAsync()
    {
        var owner = PackageFunnelChart; var a = new BleeckerFunnelChartDatum { Name = "A", Value = 30 }; var b = new BleeckerFunnelChartDatum { Name = "B", Value = 20 };
        owner.Data = new() { a, b, new BleeckerFunnelChartDatum { Name = "C", Value = 10 } }; owner.ShowLabels = true;
        var parent = (Panel)VisualTreeHelper.GetParent(owner); var position = parent.Children.IndexOf(owner); parent.Children.Remove(owner); await AwaitAsync(() => !owner.IsLoaded, "Packaged Funnel did not unmount."); Root.Children.Add(owner); owner.VerticalAlignment = VerticalAlignment.Top;
        try
        {
            await AwaitAsync(() => owner.IsLoaded, "Packaged Funnel did not remount."); owner.ApplyTemplate(); Root.UpdateLayout();
            var chart = BrandDescendants<BleeckerLineChart>(owner).Single(); var plot = BrandDescendants<Canvas>(chart).Single(canvas => canvas.Name == "PART_Plot");
            ShapePath[] Bars() => plot.Children.OfType<ShapePath>().Where(path => path.Tag is ValueTuple<string, int> tag && tag.Item1 == "funnel").ToArray();
            TextBlock[] Labels() => plot.Children.OfType<TextBlock>().Where(label => label.Tag is ValueTuple<string, int, int> tag && tag.Item1 == "funnel-label").ToArray();
            await AwaitAsync(() => Bars().Length == 3 && Labels().Length == 3, "Packaged Funnel lost its trapezoids or source name-only labels.");
            var figure = ((PathGeometry)Bars()[0].Data).Figures[0]; var point = ((LineSegment)figure.Segments[1]).Point;
            Require(figure.IsClosed && figure.StartPoint == new Windows.Foundation.Point(5, 5) && Math.Abs(point.X - 263.3333) < .001 && Math.Abs(point.Y - 81.6667) < .001 && Labels()[0].Text == "A: ", "Packaged Funnel lost the unchanged source consumer geometry or label text.");
            Require(ReferenceEquals(chart.Data, owner.Data) && !owner.IsTabStop && chart.IsTabStop && AutomationProperties.GetName(chart) == "Independent funnel counts" && FrameworkElementAutomationPeer.CreatePeerForElement(chart).GetClassName() == nameof(BleeckerFunnelChart), "Packaged Funnel lost original data or its single named inspection stop.");
            await AcquireConsumerInputAsync(); Require(chart.Focus(FocusState.Keyboard), "Could not focus packaged Funnel."); await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == " : 30", "Packaged Funnel did not inspect its initial record.");
            void Key(ushort value)
            {
                var input = new[] { new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = value } } }, new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = value, Flags = 2 } } } };
                Require(SendInput(2, input, Marshal.SizeOf<ConsumerInput>()) == 2, "Packaged Funnel key did not enter native input.");
            }
            Key(0x27); await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == " : 30", "Packaged Funnel Right changed the pinned source selection."); a.Value = 35; await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == " : 35", "Packaged Funnel ignored typed value notifications.");
            owner.ValueFormatter = value => "Consumer " + value.ToString("0", System.Globalization.CultureInfo.InvariantCulture); await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == " : Consumer 35", "Packaged Funnel ignored its numeric formatter.");
            var paint = new SolidColorBrush(Microsoft.UI.Colors.Transparent); a.Fill = paint; await AwaitAsync(() => ReferenceEquals(Bars()[0].Fill, paint), "Packaged Funnel replaced a transparent consumer brush."); a.Name = "Updated"; await AwaitAsync(() => Labels().Any(label => label.Text == "Updated: "), "Packaged Funnel ignored a live name.");
            owner.ShowLabels = false; await AwaitAsync(() => Labels().Length == 0, "Packaged Funnel did not hide labels."); owner.ShowLabels = true; await AwaitAsync(() => Labels().Length == 3, "Packaged Funnel did not restore labels.");
            owner.Data.Clear(); await AwaitAsync(() => Bars().Length == 0 && Labels().Length == 0 && AutomationProperties.GetHelpText(chart) == "", "Packaged Funnel retained empty shapes, labels or inspection.");
        }
        finally { Root.Children.Remove(owner); parent.Children.Insert(position, owner); }
    }
}
