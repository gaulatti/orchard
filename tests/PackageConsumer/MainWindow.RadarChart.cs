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
    private async Task VerifyRadarChartAsync()
    {
        var owner = PackageRadarChart; owner.Data.Clear(); owner.Series[0].Color = null; owner.Series[0].Fill = true;
        foreach (var (subject, value) in new[] { ("A", 10d), ("B", 20d), ("C", 30d) }) owner.Data.Add(new Dictionary<string, object?> { ["subject"] = subject, ["a"] = value });
        var parent = (Panel)VisualTreeHelper.GetParent(owner); var position = parent.Children.IndexOf(owner);
        parent.Children.Remove(owner); await AwaitAsync(() => !owner.IsLoaded, "Packaged Radar did not unmount."); Root.Children.Add(owner); owner.VerticalAlignment = VerticalAlignment.Top;
        try
        {
            await AwaitAsync(() => owner.IsLoaded, "Packaged Radar did not remount."); owner.ApplyTemplate(); Root.UpdateLayout();
            var chart = BrandDescendants<BleeckerLineChart>(owner).Single(); var plot = BrandDescendants<Canvas>(chart).Single(canvas => canvas.Name == "PART_Plot");
            ShapePath[] Shapes(string role) => plot.Children.OfType<ShapePath>().Where(path => path.Tag is ValueTuple<string, int> tag && tag.Item1 == role).ToArray();
            await AwaitAsync(() => Shapes("radar-stroke").Length == 1, "Packaged Radar did not render its source polygon.");
            // The unchanged pinned browser's 320x240, no-legend consumer case.
            var first = ((PathGeometry)Shapes("radar-stroke")[0].Data).Figures[0].StartPoint;
            Require(Math.Abs(first.X - 160) < .001 && Math.Abs(first.Y - 91.875) < .001 && Math.Abs(Shapes("radar-fill")[0].Opacity - .35) < .000001, "Packaged Radar lost its source geometry or fill.");
            Require(ReferenceEquals(chart.Data, owner.Data) && !owner.IsTabStop && chart.IsTabStop && AutomationProperties.GetName(chart) == "Independent radar" && FrameworkElementAutomationPeer.CreatePeerForElement(chart).GetClassName() == nameof(BleeckerRadarChart), "Packaged Radar lost original data or its single named inspection boundary.");
            await AcquireConsumerInputAsync(); Require(chart.Focus(FocusState.Keyboard), "Could not focus packaged Radar.");
            await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "A;  : 10", "Packaged Radar did not inspect its initial subject.");
            var keys = new[] { new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = 0x27 } } }, new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = 0x27, Flags = 2 } } } };
            Require(SendInput(2, keys, Marshal.SizeOf<ConsumerInput>()) == 2, "Packaged Radar Right did not enter native input.");
            await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "B;  : 20", "Packaged Radar did not inspect through native Right.");
            owner.Data[1] = new Dictionary<string, object?> { ["subject"] = "B", ["a"] = 25d };
            await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "B;  : 25", "Packaged Radar ignored replacement records.");
            var paint = new SolidColorBrush(Microsoft.UI.Colors.Transparent); owner.Series[0].Color = paint; owner.Series[0].Fill = false;
            await AwaitAsync(() => ReferenceEquals(Shapes("radar-stroke")[0].Stroke, paint) && ReferenceEquals(Shapes("radar-fill")[0].Fill, paint) && Shapes("radar-fill")[0].Opacity == 0, "Packaged Radar lost consumer transparent paint or fill changes.");
            owner.TickFormatter = value => "T" + value; await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "B;  : T25", "Packaged Radar ignored live formatting."); owner.TickFormatter = null;
            owner.Data.Clear(); await AwaitAsync(() => Shapes("radar-stroke").Length == 0 && AutomationProperties.GetHelpText(chart) == "", "Packaged Radar retained empty geometry or inspection.");
        }
        finally { Root.Children.Remove(owner); parent.Children.Insert(position, owner); }
    }
}
