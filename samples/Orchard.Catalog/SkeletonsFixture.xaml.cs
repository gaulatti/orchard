using System.Text.Json;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard.Catalog;

public sealed partial class SkeletonsFixture : UserControl
{
    public SkeletonsFixture() => InitializeComponent();

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        var samples = new List<object>();
        foreach (var (shape, radius) in new[] { (Rectangle, 7d), (Circle, 20d), (Capsule, 20d) })
        {
            var surface = Surface(shape);
            Require(surface.CornerRadius == new CornerRadius(radius), "Skeleton corner shape differs from the reference.");
            VerifyShape(shape, scheme);
            samples.Add(new { shape.Name, shape.ActualWidth, shape.ActualHeight, radius, brush = Brush(surface.Background) });
        }
        foreach (var card in new[] { DefaultCard, ZeroCard, TallCard })
        {
            VerifyCard(card, scheme);
            samples.Add(new { card.Name, card.Rows, card.ActualWidth, card.ActualHeight });
        }
        foreach (var table in new[] { DefaultTable, HeaderTable, EmptyColumns })
        {
            VerifyTable(table, scheme);
            foreach (var shape in Descendants<BleeckerSkeleton>(table)) VerifyShape(shape, scheme);
            samples.Add(new { table.Name, table.Rows, table.Columns, table.ActualWidth, table.ActualHeight });
        }
        foreach (var owner in new Control[] { Rectangle, DefaultCard, DefaultTable })
        {
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(owner);
            Require(!owner.IsTabStop && !peer.IsControlElement() && !peer.IsContentElement() &&
                peer.GetPattern(PatternInterface.Invoke) is null, "Decorative loading shapes acquired interactive semantics.");
        }
        DefaultCard.Width = DefaultTable.Width = 200;
        await SettleAsync();
        VerifyCard(DefaultCard, scheme); VerifyTable(DefaultTable, scheme);
        Require(DefaultTable.ActualHeight == 272, "Constrained table changed its row heights.");
        DefaultCard.Width = DefaultTable.Width = 320;
        HeaderTable.Rows = 2;
        await SettleAsync();
        Require(ZeroCard.Rows == 2, "Consumer-owned count binding did not propagate.");
        VerifyCard(ZeroCard, scheme); VerifyTable(HeaderTable, scheme);
        HeaderTable.Rows = -2; HeaderTable.Columns = -1;
        await SettleAsync();
        Require(ZeroCard.Rows == -2 && HeaderTable.Rows == -2 && HeaderTable.Columns == -1,
            "Skeleton count normalization rewrote consumer values.");
        VerifyCard(ZeroCard, scheme); VerifyTable(HeaderTable, scheme);
        HeaderTable.Rows = 0; HeaderTable.Columns = 2;
        Capsule.Height = 24;
        await SettleAsync();
        Require(Surface(Capsule).CornerRadius == new CornerRadius(12), "Resized capsule did not normalize its radius.");
        Capsule.Circle = false;
        Require(Surface(Capsule).CornerRadius == new CornerRadius(7), "Live circle property did not restore rectangular corners.");
        Capsule.Circle = true; Capsule.Height = 40;
        await SettleAsync();

        var animations = new UISettings().AnimationsEnabled;
        var surfaceBefore = Surface(Rectangle);
        var motion = new List<object>();
        var levels = new List<double>();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (var sample = 0; sample < 21; sample++)
        {
            levels.Add(surfaceBefore.Opacity);
            motion.Add(new { milliseconds = watch.Elapsed.TotalMilliseconds, opacity = surfaceBefore.Opacity });
            await Task.Delay(110);
        }
        Require(animations ? levels.Max() - levels.Min() > .35 && levels.All(level => level >= .5 && level <= 1) : levels.All(level => level == 1),
            "Skeleton pulse did not follow the Windows animation preference.");
        Rectangle.Opacity = .75;
        Rectangle.Visibility = Visibility.Collapsed;
        await SettleAsync();
        Require(surfaceBefore.Opacity == 1 && Rectangle.Opacity == .75, "Hidden pulse did not reset or replaced consumer opacity.");
        Rectangle.Visibility = Visibility.Visible;
        Shapes.Children.Remove(Rectangle);
        await SettleAsync();
        Require(!Rectangle.IsLoaded && surfaceBefore.Opacity == 1, "Unloaded skeleton retained its pulse.");
        Shapes.Children.Insert(0, Rectangle);
        Rectangle.Opacity = 1;
        await SettleAsync();
        var shapeTemplate = Rectangle.Template;
        Shapes.Children.Remove(Rectangle);
        await SettleAsync();
        var oldSurface = Surface(Rectangle);
        Rectangle.Template = null;
        Require(oldSurface.Opacity == 1 && VisualTreeHelper.GetChildrenCount(Rectangle) == 0,
            "Detached Skeleton template retained visuals or an active pulse.");
        Rectangle.Template = shapeTemplate;
        Shapes.Children.Insert(0, Rectangle);
        Rectangle.ApplyTemplate();
        await SettleAsync();
        Require(!ReferenceEquals(Surface(Rectangle), oldSurface) && Surface(Rectangle).CornerRadius == new CornerRadius(7),
            "Restored skeleton template did not replace its old surface or recover geometry.");

        var windowCaptures = new List<object>();
        var elementCaptures = new List<string>();
        var passed = false;
        var stage = "element-captures";
        string? error = null;
        try
        {
            foreach (var (owner, name) in new (UIElement, string)[] { (Shapes, "shapes"), (DefaultCard, "card"), (ZeroCard, "card-zero"),
                (TallCard, "card-six"), (DefaultTable, "table"), (HeaderTable, "table-header") })
            {
                stage = name;
                var capture = $"skeleton-{name}-{scheme}.png";
                await MainWindow.CaptureElementAsync(owner, Path.Combine(folder, capture));
                elementCaptures.Add(capture);
            }
            // The zero-column producer is wholly transparent. Preserve that
            // contract and capture its allocated region through the compositor;
            // RenderTargetBitmap has no visual content to render for this case.
            using var native = new NativeInputSession(XamlRoot, folder, $"skeleton-{scheme}");
            stage = "acquire-composited-captures";
            await native.AcquireAsync(Rectangle);
            foreach (var (target, name) in new (FrameworkElement, string)[] { (Shapes, "shapes"), (DefaultCard, "card"),
                (DefaultTable, "table"), (EmptyColumns, "table-empty-columns") })
            {
                stage = $"window-{name}";
                await native.MoveAsync(target);
                var capture = $"skeleton-window-{name}-{scheme}.png";
                var client = await NativeScreenCapture.CaptureClientAsync(native.Window, Path.Combine(folder, capture));
                windowCaptures.Add(new { capture, client, target = native.ScreenBounds(target) });
            }
            passed = true;
            stage = "complete";
        }
        catch (Exception failure) { error = failure.ToString(); throw; }
        finally
        {
            await File.WriteAllTextAsync(Path.Combine(folder, $"skeleton-measurements-{scheme}.json"),
                JsonSerializer.Serialize(new { passed, checksPassed = true, stage, error, scheme, animationsEnabled = animations, motion,
                    decorative = true, boundCounts = true, negativeCountsPreserved = true, samples, elementCaptures, windowCaptures },
                    new JsonSerializerOptions { WriteIndented = true }));
        }
    }
    private void VerifyCard(BleeckerSkeletonCard card, BleeckerColorScheme scheme)
    {
        Require(Math.Abs(card.ActualHeight - (88 + Math.Max(0, card.Rows) * 24)) < .001 && card.Padding == new Thickness(24) &&
            card.CornerRadius == new CornerRadius(12), "SkeletonCard layout differs from the reference.");
        var root = (Grid)VisualTreeHelper.GetChild(card, 0);
        var body = (StackPanel)root.FindName("PART_Body");
        var text = (Grid)root.FindName("PART_TextColumn");
        var headline = (BleeckerSkeleton)root.FindName("PART_Headline");
        var subtitle = (BleeckerSkeleton)root.FindName("PART_Subtitle");
        var ring = (Border)root.FindName("PART_Ring");
        Require(body.Children.Count == Math.Max(0, card.Rows) && body.Spacing == 12 &&
            Math.Abs(text.ActualWidth - (card.ActualWidth - 100)) < .001 &&
            Math.Abs(headline.ActualWidth - text.ActualWidth / 3) < .001 && Math.Abs(subtitle.ActualWidth - text.ActualWidth / 2) < .001 &&
            headline.ActualHeight == 16 && subtitle.ActualHeight == 12, "SkeletonCard child geometry differs from the reference.");
        Require(Brush(card.Background).color == BleeckerTokens.Colors(scheme)["card"] && Brush(card.Background).opacity == 1 &&
            Brush(ring.BorderBrush).color == (scheme == BleeckerColorScheme.Dark ? "#FFFFFFFF" : "#FF000000") &&
            Math.Abs(Brush(ring.BorderBrush).opacity - .08) < .000001 && ring.Margin == new Thickness(-1) && ring.CornerRadius == new CornerRadius(13),
            "SkeletonCard surface or exterior ring differs from the reference.");
        var shadows = ElementCompositionPreview.GetElementChildVisual((Grid)root.FindName("PART_ShadowHost")) as ContainerVisual;
        Require(shadows?.Children.Count == 2, "SkeletonCard did not render both shadow layers.");
        var layers = shadows!.Children.Cast<SpriteVisual>().Select(sprite => (DropShadow)sprite.Shadow).ToArray();
        Require(layers.Select(layer => layer.BlurRadius).Order().SequenceEqual(new[] { 2f, 3f }) &&
            layers.All(layer => layer.Offset == new System.Numerics.Vector3(0, 1, 0) && layer.Opacity == .1f && layer.Color == Microsoft.UI.Colors.Black),
            "SkeletonCard rendered a stock elevation recipe.");
        foreach (var shape in Descendants<BleeckerSkeleton>(card)) VerifyShape(shape, scheme);
    }
    private static void VerifyTable(BleeckerSkeletonTable table, BleeckerColorScheme scheme)
    {
        var host = (StackPanel)VisualTreeHelper.GetChild(table, 0);
        var rows = Math.Max(0, table.Rows); var columns = Math.Max(0, table.Columns);
        Require(host.Children.Count == rows + 1 && Math.Abs(table.ActualHeight - (columns == 0 ? rows * 8 : 32 + rows * 48)) < .001,
            "SkeletonTable lost header, row sizes or empty-column gaps.");
        for (var index = 0; index < host.Children.Count; index++)
        {
            var row = (Grid)host.Children[index];
            Require(row.Children.Count == columns && row.ColumnSpacing == 8, "SkeletonTable column count or gaps differ from the reference.");
            foreach (var shape in row.Children.OfType<BleeckerSkeleton>())
                Require(Math.Abs(shape.ActualWidth - (table.ActualWidth - Math.Max(0, columns - 1) * 8) / columns) < .001 &&
                    shape.ActualHeight == (index == 0 ? 32 : 40), "SkeletonTable equal column allocation differs from the reference.");
        }
    }
    private static void VerifyShape(BleeckerSkeleton shape, BleeckerColorScheme scheme)
    {
        var surface = Surface(shape); var brush = Brush(surface.Background);
        Require(brush.color == (scheme == BleeckerColorScheme.Dark ? "#FFFFFFFF" : BleeckerTokens.Colors(scheme)["sand"]) &&
            Math.Abs(brush.opacity - (scheme == BleeckerColorScheme.Dark ? .1 : .2)) < .000001 && shape.Opacity == 1,
            "Skeleton rendered a Windows default or incorrect background alpha.");
    }
    private async Task SettleAsync() { UpdateLayout(); await Task.Delay(80); UpdateLayout(); }
    private static Border Surface(BleeckerSkeleton owner) => (Border)VisualTreeHelper.GetChild(owner, 0);
    private static (string color, double opacity) Brush(Brush brush)
    {
        var solid = (SolidColorBrush)brush;
        return ($"#{solid.Color.A:X2}{solid.Color.R:X2}{solid.Color.G:X2}{solid.Color.B:X2}", solid.Opacity);
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject owner) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(owner); index++)
        {
            var child = VisualTreeHelper.GetChild(owner, index);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
