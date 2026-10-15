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
    private async Task VerifySankeyChartAsync()
    {
        var owner = PackageSankeyChart; var a = new BleeckerSankeyNode { Name = "A" }; var b = new BleeckerSankeyNode { Name = "B" }; var c = new BleeckerSankeyNode { Name = "C" };
        var first = new BleeckerSankeyLink { Source = 0, Target = 1, Value = 30 }; var second = new BleeckerSankeyLink { Source = 1, Target = 2, Value = 30 };
        owner.Data = new() { Nodes = new() { a, b, c }, Links = new() { first, second } }; owner.NodeColors = null; owner.NodeWidth = 12; owner.LinkCurvature = .5; owner.LinkOpacity = .35; owner.NodeRadius = 4;
        var parent = (Panel)VisualTreeHelper.GetParent(owner); var position = parent.Children.IndexOf(owner); parent.Children.Remove(owner); await AwaitAsync(() => !owner.IsLoaded, "Packaged Sankey did not unmount."); Root.Children.Add(owner); owner.VerticalAlignment = VerticalAlignment.Top;
        try
        {
            await AwaitAsync(() => owner.IsLoaded, "Packaged Sankey did not remount."); owner.ApplyTemplate(); Root.UpdateLayout(); var chart = BrandDescendants<BleeckerLineChart>(owner).Single(); var plot = BrandDescendants<Canvas>(chart).Single(canvas => canvas.Name == "PART_Plot");
            ShapePath[] Shapes(string kind) => plot.Children.OfType<ShapePath>().Where(path => path.Tag is ValueTuple<string, int> tag && tag.Item1 == kind).ToArray();
            double FlowHeight() { var flow = ((PathGeometry)Shapes("sankey-link")[0].Data).Figures[0]; return ((LineSegment)flow.Segments[1]).Point.Y - ((BezierSegment)flow.Segments[0]).Point3.Y; }
            await AwaitAsync(() => Shapes("sankey-node").Length == 3 && Shapes("sankey-link").Length == 2, "Packaged Sankey lost original nodes or flows.");
            var figure = ((PathGeometry)Shapes("sankey-link")[0].Data).Figures[0]; var cubic = (BezierSegment)figure.Segments[0];
            Require(figure.IsClosed && figure.StartPoint == new Windows.Foundation.Point(32, 120) && cubic.Point1 == new Windows.Foundation.Point(93, 120) && cubic.Point2 == new Windows.Foundation.Point(93, 120) && cubic.Point3 == new Windows.Foundation.Point(154, 120), "Packaged Sankey differs from the unchanged 320x240 source chain.");
            Require(ReferenceEquals(chart.Data, owner.Data.Nodes) && !owner.IsTabStop && chart.IsTabStop && AutomationProperties.GetName(chart) == "Independent Sankey flows" && FrameworkElementAutomationPeer.CreatePeerForElement(chart).GetClassName() == nameof(BleeckerSankeyChart), "Packaged Sankey lost original data or its single named group.");
            await AcquireConsumerInputAsync(); Require(chart.Focus(FocusState.Keyboard), "Could not focus packaged Sankey."); await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "A : 30", "Packaged Sankey did not inspect its first node.");
            void Key(ushort value)
            {
                var input = new[] { new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = value } } }, new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = value, Flags = 2 } } } };
                Require(SendInput(2, input, Marshal.SizeOf<ConsumerInput>()) == 2, "Packaged Sankey key did not enter native input.");
            }
            Key(0x27); await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "B : 30", "Packaged Sankey Right did not inspect its next node."); Key(0x27); await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "C : 30", "Packaged Sankey lost its last node."); Key(0x27); await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "A - B : 30", "Packaged Sankey lost original flow inspection.");
            first.Value = 20; await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "A - B : 20", "Packaged Sankey ignored typed flow updates."); a.Name = "Updated"; await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "Updated - B : 20", "Packaged Sankey ignored original names.");
            var extra = new BleeckerSankeyNode { Name = "Unused" }; owner.Data.Nodes.Add(extra); await AwaitAsync(() => Math.Abs(FlowHeight() - 136) < .001 && AutomationProperties.GetHelpText(chart) == "Updated - B : 20", "Packaged Sankey changed flow inspection kind when a node was added."); owner.Data.Nodes.Remove(extra); await AwaitAsync(() => Math.Abs(FlowHeight() - 146.66666666666666) < .001 && AutomationProperties.GetHelpText(chart) == "Updated - B : 20", "Packaged Sankey lost flow inspection after node removal.");
            var paint = new SolidColorBrush(Microsoft.UI.Colors.Transparent); a.Fill = paint; await AwaitAsync(() => ReferenceEquals(Shapes("sankey-link")[0].Fill, paint) && ReferenceEquals(Shapes("sankey-node")[0].Fill, paint), "Packaged Sankey replaced consumer paint.");
            owner.NodeColors = new() { new SolidColorBrush(Microsoft.UI.Colors.CadetBlue) }; a.Fill = null; await AwaitAsync(() => ReferenceEquals(Shapes("sankey-link")[0].Fill, owner.NodeColors[0]), "Packaged Sankey ignored its original palette.");
            owner.Data.Links = new() { new BleeckerSankeyLink { Source = 0, Target = 2, Value = 12 } }; await AwaitAsync(() => Shapes("sankey-link").Length == 1 && AutomationProperties.GetHelpText(chart) == "Updated - C : 12", "Packaged Sankey ignored root link replacement.");
            owner.Data = new(); await AwaitAsync(() => Shapes("sankey-link").Length == 0 && Shapes("sankey-node").Length == 0 && AutomationProperties.GetHelpText(chart) == "", "Packaged Sankey retained an empty graph or inspection.");
        }
        finally { Root.Children.Remove(owner); parent.Children.Insert(position, owner); }
    }
}
