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
    private async Task VerifyRadialBarChartAsync()
    {
        var owner = PackageRadialBarChart; var a = new BleeckerRadialBarChartDatum { Name = "A", Value = 10 }; var b = new BleeckerRadialBarChartDatum { Name = "B", Value = 20 };
        owner.Data = new() { a, b, new BleeckerRadialBarChartDatum { Name = "C", Value = 30 } }; owner.ShowLegend = false; owner.InnerRadius = "10%"; owner.OuterRadius = "80%"; owner.StartAngle = 90; owner.EndAngle = -270;
        var parent = (Panel)VisualTreeHelper.GetParent(owner); var position = parent.Children.IndexOf(owner); parent.Children.Remove(owner); await AwaitAsync(() => !owner.IsLoaded, "Packaged Radial Bar did not unmount."); Root.Children.Add(owner); owner.VerticalAlignment = VerticalAlignment.Top;
        try
        {
            await AwaitAsync(() => owner.IsLoaded, "Packaged Radial Bar did not remount."); owner.ApplyTemplate(); Root.UpdateLayout();
            var chart = BrandDescendants<BleeckerLineChart>(owner).Single(); var plot = BrandDescendants<Canvas>(chart).Single(canvas => canvas.Name == "PART_Plot");
            ShapePath[] Bars() => plot.Children.OfType<ShapePath>().Where(path => path.Tag is ValueTuple<string, int> tag && tag.Item1 == "radial-foreground").ToArray();
            ShapePath[] Glyphs() => plot.Children.OfType<ShapePath>().Where(path => path.Tag is ValueTuple<string, int, int> tag && tag.Item1 == "radial-label-glyph").ToArray();
            await AwaitAsync(() => Bars().Length == 3 && Glyphs().Length == 6 && Glyphs().All(glyph => ((PathGeometry)glyph.Data).Figures.Count > 0), "Packaged Radial Bar lost its sectors or actual curved font outlines.");
            var point = ((PathGeometry)Bars()[0].Data).Figures[0].StartPoint; Require(Math.Abs(point.X - 160) < .001 && Math.Abs(point.Y - 94.0205) < .001, "Packaged Radial Bar lost the unchanged source consumer geometry.");
            Require(ReferenceEquals(chart.Data, owner.Data) && !owner.IsTabStop && chart.IsTabStop && AutomationProperties.GetName(chart) == "Independent radial bars" && FrameworkElementAutomationPeer.CreatePeerForElement(chart).GetClassName() == nameof(BleeckerRadialBarChart), "Packaged Radial Bar lost original data or its single named inspection boundary.");
            await AcquireConsumerInputAsync(); Require(chart.Focus(FocusState.Keyboard), "Could not focus packaged Radial Bar."); await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "0;  : 10", "Packaged Radial Bar did not inspect its initial record.");
            void Key(ushort value)
            {
                var input = new[] { new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = value } } }, new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = value, Flags = 2 } } } };
                Require(SendInput(2, input, Marshal.SizeOf<ConsumerInput>()) == 2, "Packaged Radial Bar key did not enter native input.");
            }
            Key(0x27); await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "1;  : 20", "Packaged Radial Bar did not inspect through native Right."); b.Value = 25; await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "1;  : 25", "Packaged Radial Bar ignored typed value notifications.");
            var paint = new SolidColorBrush(Microsoft.UI.Colors.Transparent); a.Fill = paint; await AwaitAsync(() => ReferenceEquals(Bars()[0].Fill, paint), "Packaged Radial Bar replaced a transparent consumer fill.");
            var oldPoint = ((PathGeometry)Bars()[0].Data).Figures[0].StartPoint; owner.InnerRadius = 30d; owner.OuterRadius = "90%"; await AwaitAsync(() => ((PathGeometry)Bars()[0].Data).Figures[0].StartPoint != oldPoint, "Packaged Radial Bar ignored native radius values.");
            owner.ShowLegend = true; a.Name = "Updated"; await AwaitAsync(() => BrandDescendants<TextBlock>(chart).Any(text => text.Text == "Updated"), "Packaged Radial Bar ignored a live legend.");
            owner.Data.Clear(); await AwaitAsync(() => Bars().Length == 0 && Glyphs().Length == 0 && AutomationProperties.GetHelpText(chart) == "", "Packaged Radial Bar retained empty bars, glyphs or inspection.");
        }
        finally { Root.Children.Remove(owner); parent.Children.Insert(position, owner); }
    }
}
