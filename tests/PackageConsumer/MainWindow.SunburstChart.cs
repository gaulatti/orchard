using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Orchard;
using ShapePath = Microsoft.UI.Xaml.Shapes.Path;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private static BleeckerSunburstNode SunburstData() => new()
    {
        Name = "Root", Children = new()
        {
            new BleeckerSunburstNode { Name = "Harbor", Value = 40, Children = new() { new BleeckerSunburstNode { Name = "A", Value = 25 }, new BleeckerSunburstNode { Name = "B", Value = 15 } } },
            new BleeckerSunburstNode { Name = "Terrace", Value = 20 }
        }
    };
    private async Task VerifySunburstChartAsync()
    {
        var owner = PackageSunburstChart; var tree = SunburstData(); owner.Data = tree; owner.DataKey = "value"; owner.NameKey = "name";
        var parent = (Panel)VisualTreeHelper.GetParent(owner); var position = parent.Children.IndexOf(owner); parent.Children.Remove(owner); await AwaitAsync(() => !owner.IsLoaded, "Packaged Sunburst did not unmount."); Root.Children.Add(owner); owner.VerticalAlignment = VerticalAlignment.Top;
        try
        {
            await AwaitAsync(() => owner.IsLoaded, "Packaged Sunburst did not remount."); owner.ApplyTemplate(); Root.UpdateLayout(); var plot = BrandDescendants<Canvas>(owner).Single(canvas => canvas.Name == "PART_Plot");
            ShapePath[] Shapes(string kind) => plot.Children.OfType<ShapePath>().Where(path => path.Tag is ValueTuple<string, int> tag && tag.Item1 == kind).ToArray();
            await AwaitAsync(() => Shapes("sunburst-sector").Length == 4 && Shapes("sunburst-label").Length == 4, "Packaged Sunburst lost recursive sectors or glyphs.");
            var figure = ((PathGeometry)Shapes("sunburst-sector")[0].Data).Figures.Single(); var arc = (ArcSegment)figure.Segments[0];
            Require(Math.Abs(figure.StartPoint.X - 233.3333) < .001 && Math.Abs(figure.StartPoint.Y - 120) < .001 && Math.Abs(arc.Size.Width - 73.3333) < .001 && Math.Abs(arc.Size.Height - 73.3333) < .001 && figure.IsClosed, "Packaged Sunburst differs from the unchanged 320x240 source geometry.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(owner);
            Require(ReferenceEquals(owner.Data, tree) && !owner.IsTabStop && peer.GetAutomationControlType() == AutomationControlType.Image && !peer.IsKeyboardFocusable() && peer.IsContentElement() && peer.GetName() == "Independent Sunburst hierarchy" && peer.GetHelpText().Contains("Root > Harbor > A: 25", StringComparison.Ordinal), "Packaged Sunburst lost original ownership or hierarchical image semantics.");
            Require(Shapes("sunburst-label-outline").Length == 4 && Shapes("sunburst-label-outline").All(path => path.StrokeThickness == 1) && !BrandDescendants<ToolTip>(owner).Any(), "Packaged Sunburst lost outlined values or added inspection.");
            var shared = (BleeckerSunburstNode)((BleeckerSunburstNode)tree.Children[0]).Children[0]; var last = (BleeckerSunburstNode)tree.Children[1]; last.Children.Add(shared);
            await AwaitAsync(() => Shapes("sunburst-sector").Length == 5, "Packaged Sunburst ignored child additions."); shared.Value = 30; shared.Name = "Updated";
            await AwaitAsync(() => peer.GetHelpText().Contains("Root > Harbor > Updated: 30", StringComparison.Ordinal) && peer.GetHelpText().Contains("Root > Terrace > Updated: 30", StringComparison.Ordinal), "Packaged Sunburst lost shared record updates.");
            var paint = new SolidColorBrush(Microsoft.UI.Colors.Transparent); shared.Fill = paint; await AwaitAsync(() => Shapes("sunburst-sector").Count(path => ReferenceEquals(path.Fill, paint)) == 2, "Packaged Sunburst cloned original paint.");
            owner.DataKey = "missing"; await AwaitAsync(() => Shapes("sunburst-sector").Length == 0 && Shapes("sunburst-label").Length == 0, "Packaged Sunburst invented a missing-key fallback."); owner.DataKey = "value"; await AwaitAsync(() => Shapes("sunburst-sector").Length == 5, "Packaged Sunburst did not restore its data key.");
            tree.Children.Clear(); await AwaitAsync(() => Shapes("sunburst-sector").Length == 0 && Shapes("sunburst-label").Length == 0 && peer.GetHelpText() == "", "Packaged Sunburst retained reset children.");
        }
        finally { Root.Children.Remove(owner); parent.Children.Insert(position, owner); }
    }
}
