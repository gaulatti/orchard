using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.ViewManagement;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunAdminShellsAsync(string folder, bool diagnosticOnly = false)
    {
        using var stream = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.AdminShell.json") ?? throw new InvalidOperationException("AdminShell reference is missing.");
        using var sourceBytes = new MemoryStream(); await stream.CopyToAsync(sourceBytes);
        var referenceSha256 = Convert.ToHexStringLower(SHA256.HashData(sourceBytes.ToArray()));
        using var reference = JsonDocument.Parse(sourceBytes.ToArray());
        var previous = Content; var previousSize = AppWindow.ClientSize;
        var canvas = (Canvas)XamlReader.Load("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' UseLayoutRounding='False'/>");
        var anchor = new Button { Width = 2, Height = 2, Opacity = 0 }; Canvas.SetZIndex(anchor, 1000); canvas.Children.Add(anchor);
        var frames = new List<object>(); var variants = new List<object>();
        var captures = new Dictionary<string, string>(); var rasterMatches = new Dictionary<string, bool>();
        object Box(FrameworkElement element)
        {
            var bounds = element.TransformToVisual(canvas).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
            return new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height };
        }
        static object? Paint(Brush? brush) => brush is null ? null : brush is SolidColorBrush solid ? new { type = brush.GetType().FullName, a = solid.Color.A, r = solid.Color.R, g = solid.Color.G, b = solid.Color.B, brush.Opacity } : throw new InvalidOperationException("The focused AdminShell probe expects its existing solid brushes.");
        static FrameworkElement Part(BleeckerAdminShell shell, string name) => AdminDescendants(shell).Single(element => element.Name == name);
        object ClipBox(BleeckerAdminShell shell)
        {
            var main = Part(shell, "AdminShellMain");
            var clip = main.Clip as RectangleGeometry ?? throw new InvalidOperationException("The AdminShell main clip is missing.");
            var bounds = main.TransformToVisual(canvas).TransformBounds(clip.Rect);
            return new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height };
        }
        object Snapshot(BleeckerAdminShell shell) => new
        {
            shell = Box(shell), row = Box(Part(shell, "AdminShellRow")), main = Box(Part(shell, "AdminShellMain")), mainClip = ClipBox(shell), content = Box(Part(shell, "AdminShellContent")),
            footer = shell.FooterTemplate is not null || shell.FooterTemplateSelector is not null || AdminFooterPresent(shell.Footer) ? Box(Part(shell, "AdminShellFooter")) : null,
            header = shell.Header is FrameworkElement h ? Box(h) : null, sidebar = shell.Sidebar is FrameworkElement s ? Box(s) : null,
            body = shell.Content is FrameworkElement b ? Box(b) : null, footerBody = shell.Footer is FrameworkElement f ? Box(f) : null,
            viewport = new { width = canvas.XamlRoot.Size.Width, height = canvas.XamlRoot.Size.Height },
            scroll = new { viewportWidth = shell.Viewport!.ViewportWidth, extentWidth = shell.Viewport.ExtentWidth, viewportHeight = shell.Viewport.ViewportHeight, extentHeight = shell.Viewport.ExtentHeight, horizontalOffset = shell.Viewport.HorizontalOffset, offset = shell.Viewport.VerticalOffset },
            footerDecorationRoundingEnabled = Part(shell, "AdminShellFooterDecoration").UseLayoutRounding && !Part(shell, "AdminShellFooter").UseLayoutRounding,
            mainLandmark = FrameworkElementAutomationPeer.CreatePeerForElement(Part(shell, "AdminShellMain")).GetLandmarkType().ToString(), shell.IsLoaded, minimum = shell.MinHeight
        };
        object LayoutTrace(FrameworkElement start)
        {
            var trace = new List<object>();
            for (DependencyObject? node = start; node is not null; node = VisualTreeHelper.GetParent(node))
            {
                if (node is not FrameworkElement element) continue;
                var offset = element.ActualOffset; var slot = Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(element);
                trace.Add(new { type = element.GetType().FullName, element.Name, element.UseLayoutRounding, bounds = Box(element), offset = new { x = offset.X, y = offset.Y, z = offset.Z }, slot = new { x = slot.X, y = slot.Y, width = slot.Width, height = slot.Height }, clip = element.Clip is RectangleGeometry clip ? new { x = clip.Rect.X, y = clip.Rect.Y, width = clip.Rect.Width, height = clip.Rect.Height } : null });
            }
            return trace;
        }
        static object CompositionTrace(FrameworkElement start)
        {
            var trace = new List<object>();
            for (Microsoft.UI.Composition.Visual? visual = ElementCompositionPreview.GetElementVisual(start); visual is not null; visual = visual.Parent)
            {
                var offset = visual.Offset; var size = visual.Size; var scale = visual.Scale; var matrix = visual.TransformMatrix;
                trace.Add(new { type = visual.GetType().FullName, offset = new { x = offset.X, y = offset.Y, z = offset.Z }, size = new { width = size.X, height = size.Y }, scale = new { x = scale.X, y = scale.Y, z = scale.Z }, visual.IsPixelSnappingEnabled, clip = visual.Clip?.GetType().FullName, matrix = new[] { matrix.M11, matrix.M12, matrix.M13, matrix.M14, matrix.M21, matrix.M22, matrix.M23, matrix.M24, matrix.M31, matrix.M32, matrix.M33, matrix.M34, matrix.M41, matrix.M42, matrix.M43, matrix.M44 } });
            }
            return trace;
        }
        try
        {
            Content = canvas; await AwaitPresentationAsync(canvas);
            Require(canvas.XamlRoot.RasterizationScale == 1 && new UISettings().TextScaleFactor == 1, "AdminShell source capture requires unchanged 100% display and text scaling.");
            using var input = new NativeInputSession(canvas.XamlRoot, folder, "admin-shell"); await input.AcquireAsync(canvas);
            async Task Viewport(int width, int height)
            {
                if (AppWindow.ClientSize.Width != width || AppWindow.ClientSize.Height != height) AppWindow.ResizeClient(new SizeInt32(width, height));
                var clock = Stopwatch.StartNew();
                while (Math.Abs(canvas.XamlRoot.Size.Width - width) >= .001 || Math.Abs(canvas.XamlRoot.Size.Height - height) >= .001)
                { if (clock.ElapsedMilliseconds > 5000) throw new InvalidOperationException("AdminShell client did not reach its source viewport."); await Task.Delay(10); }
                Canvas.SetLeft(anchor, width - 4); Canvas.SetTop(anchor, height - 4); await AwaitPresentationAsync(canvas); await input.MoveAsync(anchor);
            }
            async Task<string> Capture(JsonElement row, string fileKey, string hashKey, string prefix = "")
            {
                var sourceFile = row.GetProperty(fileKey).GetString()!; var file = prefix + sourceFile;
                using var png = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.AdminShell." + sourceFile) ?? throw new InvalidOperationException("AdminShell source PNG is missing.");
                using var bytes = new MemoryStream(); await png.CopyToAsync(bytes);
                Require(Convert.ToHexStringLower(SHA256.HashData(bytes.ToArray())) == row.GetProperty(hashKey).GetString(), "AdminShell source PNG hash differs.");
                using var random = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                using (var writer = new Windows.Storage.Streams.DataWriter(random)) { writer.WriteBytes(bytes.ToArray()); await writer.StoreAsync(); writer.DetachStream(); }
                random.Seek(0); var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(random);
                Require(decoder.PixelWidth == (uint)AppWindow.ClientSize.Width && decoder.PixelHeight == (uint)AppWindow.ClientSize.Height, "AdminShell source/client image dimensions differ.");
                var pixels = (await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Straight, new Windows.Graphics.Imaging.BitmapTransform(), Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation, Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
                var output = Path.Combine(folder, file);
                try { await NativeScreenCapture.CaptureExactClientAsync(input.Window, output, pixels); rasterMatches[file] = true; }
                catch (InvalidOperationException error) when (error.Message == "The composed capture differs from its complete exact reference frame.") { rasterMatches[file] = false; }
                captures[file] = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(output))); return file;
            }
            foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                if (diagnosticOnly && row.GetProperty("id").GetString() != "fractional") continue;
                var spec = row.GetProperty("spec"); var viewport = spec.GetProperty("viewport");
                canvas.RequestedTheme = Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!); await Viewport(viewport.GetProperty("width").GetInt32(), viewport.GetProperty("height").GetInt32());
                var shell = AdminShellFromReference(spec);
                Canvas.SetLeft(shell, 20); Canvas.SetTop(shell, 20); canvas.Children.Add(shell); await AwaitPresentationAsync(shell);
                Require(Part(shell, "AdminShellFooterDecoration").UseLayoutRounding && !Part(shell, "AdminShellFooter").UseLayoutRounding, "AdminShell must round only its owned footer decoration.");
                if (diagnosticOnly) { await Task.Delay(250); await AwaitPresentationAsync(shell); }
                var measurement = Snapshot(shell); if (row.TryGetProperty("file", out _)) await Capture(row, "file", "pngSha256");
                if (diagnosticOnly)
                {
                    var footerPart = Part(shell, "AdminShellFooterDecoration"); var presenter = AdminDescendants(shell.Viewport!).OfType<ScrollContentPresenter>().Single();
                    object Paints() => new { canvas = Paint(canvas.Background), shell = Paint(shell.Background), main = Paint(((Panel)Part(shell, "AdminShellMain")).Background), viewport = Paint(shell.Viewport!.Background), footer = Paint(((Border)footerPart).BorderBrush) };
                    variants.Add(new { variant = "baseline", id = row.GetProperty("id").GetString(), scheme = row.GetProperty("scheme").GetString(), file = row.GetProperty("file").GetString(), measurement, layout = LayoutTrace(footerPart), paint = Paints(), composition = (object?)null });
                    _ = CompositionTrace(footerPart);
                    async Task FreshShell()
                    {
                        canvas.Children.Remove(shell); await AwaitPresentationAsync(canvas);
                        shell = AdminShellFromReference(spec); Canvas.SetLeft(shell, 20); Canvas.SetTop(shell, 20); canvas.Children.Add(shell);
                        await AwaitPresentationAsync(shell); await Task.Delay(250); await AwaitPresentationAsync(shell);
                        footerPart = Part(shell, "AdminShellFooterDecoration"); presenter = AdminDescendants(shell.Viewport!).OfType<ScrollContentPresenter>().Single();
                        _ = CompositionTrace(footerPart); await AwaitPresentationAsync(shell);
                    }
                    async Task Probe(string name)
                    {
                        await Task.Delay(250); await AwaitPresentationAsync(shell);
                        var measured = Snapshot(shell); var composition = CompositionTrace(footerPart); var file = await Capture(row, "file", "pngSha256", name + "-");
                        variants.Add(new { variant = name, id = row.GetProperty("id").GetString(), scheme = row.GetProperty("scheme").GetString(), file, measurement = measured, layout = LayoutTrace(footerPart), paint = Paints(), composition });
                    }
                    await Probe("traced");
                    await FreshShell(); presenter.UseLayoutRounding = false; await Probe("presenter-rounding-off");
                    await FreshShell(); shell.ContentBackground = null; await Probe("main-background-null");
                    await FreshShell(); presenter.UseLayoutRounding = false; shell.ContentBackground = null; await Probe("combined");
                    async Task<List<Microsoft.UI.Composition.Visual>> OwnedSnappers()
                    {
                        var flowVisual = ElementCompositionPreview.GetElementVisual(Part(shell, "AdminShellScrollContent")); var viewportVisual = ElementCompositionPreview.GetElementVisual(shell.Viewport!); await AwaitPresentationAsync(shell);
                        var owned = new List<Microsoft.UI.Composition.Visual>(); var boundaryFound = false;
                        for (Microsoft.UI.Composition.Visual? visual = flowVisual; visual is not null; visual = visual.Parent)
                        { if (ReferenceEquals(visual, viewportVisual)) { boundaryFound = true; break; } if (visual.IsPixelSnappingEnabled) owned.Add(visual); }
                        Require(boundaryFound, "The focused probe did not find its own viewport boundary."); return owned;
                    }
                    await FreshShell(); foreach (var visual in await OwnedSnappers()) visual.IsPixelSnappingEnabled = false; await Probe("scroll-snap-off");
                    await FreshShell(); ((Panel)Part(shell, "AdminShellMain")).Background = null; shell.Viewport!.Background = shell.ContentBackground; await Probe("viewport-background");
                    await FreshShell(); ((Panel)Part(shell, "AdminShellMain")).Background = null; shell.Viewport!.Background = shell.ContentBackground; foreach (var visual in await OwnedSnappers()) visual.IsPixelSnappingEnabled = false; await Probe("viewport-background-snap-off");
                    await FreshShell(); presenter.CanContentRenderOutsideBounds = true; await Probe("presenter-outside-bounds");
                    await FreshShell(); presenter.CanContentRenderOutsideBounds = true; foreach (var visual in await OwnedSnappers()) visual.IsPixelSnappingEnabled = false; await Probe("outside-bounds-snap-off");
                    await FreshShell(); presenter.CanContentRenderOutsideBounds = true;
                    var clippedMain = (Panel)Part(shell, "AdminShellMain"); clippedMain.Clip = new RectangleGeometry { Rect = new Rect(0, 0, clippedMain.ActualWidth, clippedMain.ActualHeight) }; await Probe("outside-main-clip");
                    await FreshShell(); presenter.CanContentRenderOutsideBounds = true; clippedMain = (Panel)Part(shell, "AdminShellMain");
                    var mainBounds = clippedMain.TransformToVisual(canvas).TransformBounds(new Rect(0, 0, clippedMain.ActualWidth, clippedMain.ActualHeight));
                    var scale = canvas.XamlRoot.RasterizationScale; var left = Math.Floor(mainBounds.Left * scale + .5) / scale; var top = Math.Floor(mainBounds.Top * scale + .5) / scale;
                    var right = Math.Floor(mainBounds.Right * scale + .5) / scale; var bottom = Math.Floor(mainBounds.Bottom * scale + .5) / scale;
                    clippedMain.Clip = new RectangleGeometry { Rect = new Rect(left - mainBounds.Left, top - mainBounds.Top, right - left, bottom - top) }; await Probe("outside-main-clip-pixels");
                    await FreshShell(); footerPart.UseLayoutRounding = true; await Probe("footer-layout-rounding");
                    await FreshShell(); ElementCompositionPreview.GetElementVisual(footerPart).IsPixelSnappingEnabled = true; await Probe("footer-pixel-snapping");
                    await FreshShell(); foreach (var visual in await OwnedSnappers()) visual.IsPixelSnappingEnabled = false;
                    ElementCompositionPreview.GetElementVisual(footerPart).IsPixelSnappingEnabled = true; await Probe("footer-pixel-snapping-ancestors-off");
                    await FreshShell();
                    var footerBorder = (Border)footerPart; var footerBrush = (SolidColorBrush)footerBorder.BorderBrush;
                    var footerVisual = ElementCompositionPreview.GetElementVisual(footerPart);
                    var borderVisual = footerVisual.Compositor.CreateSpriteVisual();
                    borderVisual.Size = new System.Numerics.Vector2((float)footerPart.ActualWidth, 1);
                    borderVisual.Brush = footerVisual.Compositor.CreateColorBrush(footerBrush.Color);
                    borderVisual.Opacity = (float)footerBrush.Opacity; borderVisual.IsPixelSnappingEnabled = true; borderVisual.IsHitTestVisible = false;
                    footerBorder.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
                    ElementCompositionPreview.SetElementChildVisual(footerPart, borderVisual); await Probe("footer-composition-border");
                }
                object? scrolled = null;
                if (row.TryGetProperty("scrolled", out _))
                {
                    shell.Viewport!.ChangeView(200, null, null, true); await AwaitPresentationAsync(shell);
                    // The pinned template contracts its indicator after two
                    // seconds. Compare its resting state and retain v1's
                    // immediate captures as separate diagnostic evidence.
                    await Task.Delay(3000); await AwaitPresentationAsync(shell); scrolled = Snapshot(shell); await Capture(row, "scrolledFile", "scrolledPngSha256");
                }
                frames.Add(new { id = row.GetProperty("id").GetString(), scheme = row.GetProperty("scheme").GetString(), measurement, scrolled }); canvas.Children.Remove(shell);
                if (frames.Count % 16 == 0) await File.WriteAllTextAsync(Path.Combine(folder, "admin-shell-progress.json"), JsonSerializer.Serialize(new { passed = false, frames = frames.Count, captures = captures.Count }));
            }
            Require(frames.Count == (diagnosticOnly ? 2 : 312) && captures.Count == (diagnosticOnly ? 32 : 88), "AdminShell capture is incomplete.");
            var librarySha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(BleeckerAdminShell).Assembly.Location)));
            var catalogSha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(MainWindow).Assembly.Location)));
            var component = diagnosticOnly ? "admin-shell-diagnostic" : "admin-shell";
            await File.WriteAllTextAsync(Path.Combine(folder, component + ".json"), JsonSerializer.Serialize(new { passed = true, scope = diagnosticOnly ? "diagnostic-admin-shell-source-capture" : "foreground-admin-shell-source-capture", diagnosticOnly, referenceSha256, librarySha256, catalogSha256, frames, captures, rasterMatches, variants, diagnosticRestMilliseconds = diagnosticOnly ? (int?)250 : null, sourceRasterParityVerified = !diagnosticOnly && rasterMatches.Values.All(value => value), rawClientFrames = true, foregroundCapture = true, scrolledRestMilliseconds = diagnosticOnly ? (int?)null : 3000, sourcePixelsPainted = false, osSettingsChanged = false, narratorLaunched = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component, environment = new { processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), displayScale = canvas.XamlRoot.RasterizationScale, textScale = new UISettings().TextScaleFactor } }));
        }
        finally { Content = previous; AppWindow.ResizeClient(previousSize); await AwaitPresentationAsync(CatalogRoot); }
    }
    private static BleeckerAdminShell AdminShellFromReference(JsonElement spec)
    {
        var shell = new BleeckerAdminShell { Width = spec.GetProperty("width").GetDouble() };
        if (spec.TryGetProperty("height", out var height)) shell.Height = height.GetDouble();
        if (spec.TryGetProperty("minimum", out var minimum)) shell.MinHeight = minimum.GetDouble();
        if (spec.TryGetProperty("padding", out var padding)) shell.Padding = new Thickness(padding.GetDouble());
        if (spec.TryGetProperty("contentPadding", out var cp)) shell.ContentPadding = new Thickness(cp.GetDouble());
        if (spec.TryGetProperty("rtl", out var rtl) && rtl.GetBoolean()) shell.FlowDirection = FlowDirection.RightToLeft;
        object? Slot(string name, string heightName)
        {
            if (spec.TryGetProperty(name, out var scalar)) return scalar.ValueKind switch { JsonValueKind.Null => null, JsonValueKind.True => true, JsonValueKind.False => false, JsonValueKind.String => scalar.GetString(), JsonValueKind.Number => scalar.GetDouble(), _ => throw new InvalidOperationException("Unknown AdminShell source slot.") };
            if (!spec.TryGetProperty(heightName, out var value)) return null;
            var element = new Border { Height = value.GetDouble(), UseLayoutRounding = false }; if (spec.TryGetProperty(name + "Width", out var width)) element.Width = width.GetDouble(); return element;
        }
        shell.Header = Slot("header", "headerHeight"); shell.Sidebar = Slot("sidebar", "sidebarHeight"); shell.Content = Slot("body", "bodyHeight"); shell.Footer = Slot("footer", "footerHeight"); return shell;
    }
    private static bool AdminFooterPresent(object? value) => value switch { null => false, bool flag => flag, string text => text.Length > 0, double d => d != 0 && !double.IsNaN(d), _ => true };
    private static IEnumerable<FrameworkElement> AdminDescendants(DependencyObject owner)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++) { var child = VisualTreeHelper.GetChild(owner, i); if (child is FrameworkElement element) yield return element; foreach (var nested in AdminDescendants(child)) yield return nested; }
    }
}
