using System.Text.Json;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
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
        Rectangle.Height = 8;
        await SettleAsync();
        Require(Surface(Rectangle).CornerRadius == new CornerRadius(4) && Rectangle.CornerRadius == new CornerRadius(7),
            "Thin Skeleton did not normalize its painted corners or rewrote consumer radius.");
        Rectangle.Height = 16;
        await SettleAsync();

        var animations = new UISettings().AnimationsEnabled;
        var surfaceBefore = Surface(Rectangle);
        var motion = new List<object>();
        var levels = new List<double>();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (var sample = 0; sample < 21; sample++)
        {
            levels.Add(Rectangle.Opacity);
            motion.Add(new { milliseconds = watch.Elapsed.TotalMilliseconds, opacity = Rectangle.Opacity });
            await Task.Delay(110);
        }
        Require(animations ? levels.Max() - levels.Min() > .35 && levels.All(level => level >= .5 && level <= 1) : levels.All(level => level == 1),
            "Skeleton pulse did not follow the Windows animation preference.");
        Rectangle.SetBinding(OpacityProperty, new Binding { Source = OpacitySource, Path = new PropertyPath(nameof(Opacity)) });
        var boundMotion = new List<object>();
        foreach (var baseOpacity in new[] { .75, .25 })
        {
            OpacitySource.Opacity = baseOpacity;
            await SettleAsync();
            var observed = new List<double>();
            for (var sample = 0; sample < 21; sample++) { observed.Add(Rectangle.Opacity); await Task.Delay(110); }
            Require(Convert.ToDouble(Rectangle.GetAnimationBaseValue(OpacityProperty)) == baseOpacity && Rectangle.GetBindingExpression(OpacityProperty) is not null,
                "Skeleton pulse replaced its consumer opacity binding.");
            Require(animations ? observed.Min() >= Math.Min(baseOpacity, .5) && observed.Max() <= Math.Max(baseOpacity, .5) &&
                observed.Max() - observed.Min() > .17 : observed.All(value => value == baseOpacity),
                "Bound opacity pulse differs from the reference's consumer endpoint and 50% keyframe.");
            boundMotion.Add(new { baseOpacity, minimum = observed.Min(), maximum = observed.Max(), bindingPreserved = true });
        }
        OpacitySource.Opacity = .75;
        Shapes.Visibility = Visibility.Collapsed;
        await SettleAsync();
        Require(Rectangle.Opacity == .75 && Circle.Opacity == 1 && Capsule.Opacity == 1,
            "Skeleton descendants retained animation under a hidden ancestor.");
        Shapes.Visibility = Visibility.Visible;
        Rectangle.Visibility = Visibility.Collapsed;
        await SettleAsync();
        Require(surfaceBefore.Opacity == 1 && Rectangle.Opacity == .75 && Rectangle.GetBindingExpression(OpacityProperty) is not null,
            "Hidden pulse did not restore consumer opacity or replaced its binding.");
        Rectangle.Visibility = Visibility.Visible;
        Shapes.Children.Remove(Rectangle);
        await SettleAsync();
        Require(!Rectangle.IsLoaded && surfaceBefore.Opacity == 1 && Rectangle.Opacity == .75, "Unloaded skeleton retained its pulse.");
        OpacitySource.Opacity = .25;
        await SettleAsync();
        Require(Rectangle.Opacity == .25 && Rectangle.GetBindingExpression(OpacityProperty) is not null,
            "Unloaded Skeleton lost consumer binding propagation.");
        Rectangle.ClearValue(OpacityProperty);
        Shapes.Children.Insert(0, Rectangle);
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
        var paintSamples = new List<object>();
        var passed = false;
        var stage = "element-captures";
        string? error = null;
        try
        {
            foreach (var (owner, name) in new (FrameworkElement, string)[] { (Shapes, "shapes"), (DefaultCard, "card"), (ZeroCard, "card-zero"),
                (TallCard, "card-six"), (DefaultTable, "table"), (HeaderTable, "table-header") })
            {
                stage = name;
                var capture = $"skeleton-{name}-{scheme}.png";
                if (owner is BleeckerSkeletonCard) await MainWindow.CaptureElementAsync(owner, Path.Combine(folder, capture));
                else await CaptureMonochromeAsync(owner, Path.Combine(folder, capture), animations, paintSamples);
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
                var bounds = native.ScreenBounds(target);
                var emptyPixelsVerified = target == EmptyColumns
                    ? await VerifyEmptyPixelsAsync(Path.Combine(folder, capture), client, bounds, scheme) : (int?)null;
                windowCaptures.Add(new { capture, client, target = bounds, emptyPixelsVerified });
            }
            passed = true;
            stage = "complete";
        }
        catch (Exception failure) { error = failure.ToString(); throw; }
        finally
        {
            await File.WriteAllTextAsync(Path.Combine(folder, $"skeleton-measurements-{scheme}.json"),
                JsonSerializer.Serialize(new { passed, checksPassed = true, stage, error, scheme, animationsEnabled = animations, motion, boundMotion,
                    decorative = true, boundCounts = true, negativeCountsPreserved = true, samples, paintSamples, elementCaptures, windowCaptures },
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
            Math.Abs(brush.opacity - (scheme == BleeckerColorScheme.Dark ? .1 : .2)) < .000001 && Convert.ToDouble(shape.GetAnimationBaseValue(OpacityProperty)) == 1,
            "Skeleton rendered a Windows default or incorrect background alpha.");
        if (!shape.Circle)
            Require(surface.CornerRadius == new CornerRadius(shape.ActualHeight == 12 ? 6 : 7),
                "Skeleton painted overlapping 7px corners without CSS normalization.");
    }
    private async Task SettleAsync() { UpdateLayout(); await Task.Delay(80); UpdateLayout(); }
    private static async Task CaptureMonochromeAsync(FrameworkElement owner, string path, bool animations, List<object> samples)
    {
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(owner);
        Require(bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0, "Skeleton shapes did not render.");
        var buffer = await bitmap.GetPixelsAsync();
        var pixels = new byte[checked((int)buffer.Length)];
        using (var reader = DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
        var shapes = Descendants<BleeckerSkeleton>(owner).ToArray();
        Require(shapes.Length > 0, "Monochrome capture requires actual loading shapes.");
        foreach (var shape in shapes)
        {
            var center = shape.TransformToVisual(owner).TransformPoint(new Point(shape.ActualWidth / 2, shape.ActualHeight / 2));
            var x = (int)Math.Floor(center.X * owner.XamlRoot.RasterizationScale);
            var y = (int)Math.Floor(center.Y * owner.XamlRoot.RasterizationScale);
            Require(x >= 0 && y >= 0 && x < bitmap.PixelWidth && y < bitmap.PixelHeight,
                "A Skeleton shape was outside its captured raster.");
            var index = (y * bitmap.PixelWidth + x) * 4;
            var brush = (SolidColorBrush)Surface(shape).Background;
            var alpha = pixels[index + 3];
            var baseAlpha = brush.Color.A * brush.Opacity;
            Require(alpha >= Math.Round(baseAlpha * (animations ? .5 : 1)) - 1 && alpha <= Math.Round(baseAlpha) + 1,
                "Rendered Skeleton alpha differs from its branded brush and pulse range.");
            // Alpha and premultiplied color channels quantize independently;
            // their combined half-unit rounding error is at most one byte.
            Require(Math.Abs(pixels[index] - brush.Color.B * alpha / 255d) <= 1 &&
                Math.Abs(pixels[index + 1] - brush.Color.G * alpha / 255d) <= 1 &&
                Math.Abs(pixels[index + 2] - brush.Color.R * alpha / 255d) <= 1 && alpha > 0,
                "Rendered Skeleton pixels do not use the branded color.");
            samples.Add(new { capture = Path.GetFileName(path), shape.Name, x, y, alpha,
                blue = pixels[index], green = pixels[index + 1], red = pixels[index + 2], brush = Brush(brush) });
        }
        await MainWindow.SavePngAsync(path, pixels, bitmap.PixelWidth, bitmap.PixelHeight);
    }
    private static async Task<int> VerifyEmptyPixelsAsync(string path, Rect client, Rect target, BleeckerColorScheme scheme)
    {
        var file = await StorageFile.GetFileFromPathAsync(path);
        using var stream = await file.OpenReadAsync();
        var decoder = await BitmapDecoder.CreateAsync(stream);
        var provider = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, new BitmapTransform(),
            ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
        var pixels = provider.DetachPixelData();
        var x = (int)Math.Round(target.X - client.X); var y = (int)Math.Round(target.Y - client.Y);
        var width = (int)Math.Round(target.Width); var height = (int)Math.Round(target.Height);
        Require(width > 0 && height > 0 && x >= 0 && y >= 0 && x + width <= decoder.PixelWidth && y + height <= decoder.PixelHeight,
            "Empty-column table was outside its composited capture.");
        var color = Convert.ToUInt32(BleeckerTokens.Colors(scheme)["background"].TrimStart('#'), 16);
        var red = (byte)(color >> 16); var green = (byte)(color >> 8); var blue = (byte)color;
        for (var row = y; row < y + height; row++)
        for (var column = x; column < x + width; column++)
        {
            var index = checked((int)((row * decoder.PixelWidth + column) * 4));
            Require(pixels[index] == blue && pixels[index + 1] == green && pixels[index + 2] == red && pixels[index + 3] == 255,
                "Zero-column SkeletonTable painted content instead of retaining transparent layout gaps.");
        }
        return width * height;
    }
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
