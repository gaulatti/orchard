using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.ViewManagement;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    // Diagnostic only: an opaque source scene can compare native text APIs.
    // This does not replace library text or claim arbitrary-background support.
    private async Task RunAdminShellTextDiagnosticAsync(string folder)
    {
        using var stream = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.AdminShell.json") ?? throw new InvalidOperationException("AdminShell reference is missing.");
        using var bytes = new MemoryStream(); await stream.CopyToAsync(bytes); var referenceBytes = bytes.ToArray();
        using var reference = JsonDocument.Parse(referenceBytes);
        var previous = Content; var previousSize = AppWindow.ClientSize;
        var canvas = (Canvas)XamlReader.Load("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' UseLayoutRounding='False'/>");
        var anchor = new Button { Width = 2, Height = 2, Opacity = 0 }; Canvas.SetZIndex(anchor, 1000); canvas.Children.Add(anchor);
        var frames = new List<object>(); var variants = new List<object>(); var captures = new Dictionary<string, string>(); var rasterMatches = new Dictionary<string, bool>();
        static FrameworkElement Part(BleeckerAdminShell shell, string name) => AdminDescendants(shell).Single(element => element.Name == name);
        Rect Bounds(FrameworkElement element) => element.TransformToVisual(canvas).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        object Box(FrameworkElement element) { var bounds = Bounds(element); return new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height }; }
        object Snapshot(BleeckerAdminShell shell) => new { shell = Box(shell), row = Box(Part(shell, "AdminShellRow")), main = Box(Part(shell, "AdminShellMain")), content = Box(Part(shell, "AdminShellContent")), footer = Box(Part(shell, "AdminShellFooter")), header = (object?)null, sidebar = Box((FrameworkElement)shell.Sidebar!), body = Box((FrameworkElement)shell.Content!), footerBody = Box((FrameworkElement)shell.Footer!) };
        var modes = new[] {
            ("auto-default", CanvasTextAntialiasing.Auto, CanvasTextRenderingMode.Default, CanvasTextGridFit.Default),
            ("cleartype-default", CanvasTextAntialiasing.ClearType, CanvasTextRenderingMode.Default, CanvasTextGridFit.Default),
            ("cleartype-natural", CanvasTextAntialiasing.ClearType, CanvasTextRenderingMode.Natural, CanvasTextGridFit.Default),
            ("cleartype-symmetric", CanvasTextAntialiasing.ClearType, CanvasTextRenderingMode.NaturalSymmetric, CanvasTextGridFit.Default),
            ("cleartype-gdi-natural", CanvasTextAntialiasing.ClearType, CanvasTextRenderingMode.GdiNatural, CanvasTextGridFit.Default),
            ("cleartype-no-grid", CanvasTextAntialiasing.ClearType, CanvasTextRenderingMode.NaturalSymmetric, CanvasTextGridFit.Disable),
            ("grayscale-symmetric", CanvasTextAntialiasing.Grayscale, CanvasTextRenderingMode.NaturalSymmetric, CanvasTextGridFit.Default) }.Select(mode => (mode.Item1, mode.Item2, mode.Item3, mode.Item4, (float?)null, (float?)null)).ToList();
        foreach (var gamma in new[] { 1f, 1.4f, 1.8f, 2.2f }) foreach (var contrast in new[] { 0f, .5f, 1f }) foreach (var mode in new[] { CanvasTextRenderingMode.Natural, CanvasTextRenderingMode.NaturalSymmetric })
            modes.Add(($"custom-g{gamma.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}-c{contrast.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}-{mode}", CanvasTextAntialiasing.ClearType, mode, CanvasTextGridFit.Default, gamma, contrast));
        foreach (var order in new[] { "rgb", "bgr" }) modes.Add(($"coverage-{order}565", CanvasTextAntialiasing.ClearType, CanvasTextRenderingMode.NaturalSymmetric, CanvasTextGridFit.Default, 1f, 0f));
        try
        {
            Content = canvas; await AwaitPresentationAsync(canvas);
            Require(canvas.XamlRoot.RasterizationScale == 1 && new UISettings().TextScaleFactor == 1, "Text diagnostic requires unchanged 100% display/text scaling.");
            using var input = new NativeInputSession(canvas.XamlRoot, folder, "admin-shell-text-diagnostic"); await input.AcquireAsync(canvas);
            foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray().Where(row => row.GetProperty("id").GetString() == "header-text"))
            {
                var spec = row.GetProperty("spec"); var viewport = spec.GetProperty("viewport"); var width = viewport.GetProperty("width").GetInt32(); var height = viewport.GetProperty("height").GetInt32();
                canvas.RequestedTheme = Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!); AppWindow.ResizeClient(new SizeInt32(width, height));
                var clock = Stopwatch.StartNew();
                while (canvas.XamlRoot.Size.Width != width || canvas.XamlRoot.Size.Height != height) { Require(clock.ElapsedMilliseconds < 5000, "Text diagnostic viewport did not settle."); await Task.Delay(10); }
                Canvas.SetLeft(anchor, width - 4); Canvas.SetTop(anchor, height - 4); await AwaitPresentationAsync(canvas); await input.MoveAsync(anchor);
                var shell = AdminShellFromReference(spec); Canvas.SetLeft(shell, 20); Canvas.SetTop(shell, 20); canvas.Children.Add(shell); await AwaitPresentationAsync(shell);
                var target = AdminDescendants(shell).OfType<TextBlock>().Single(text => text.Name == "AdminShellHeaderText");
                var foreground = target.Foreground; var background = canvas.Background as SolidColorBrush ?? throw new InvalidOperationException("The opaque text probe requires the existing solid canvas.");
                var color = foreground as SolidColorBrush ?? throw new InvalidOperationException("The opaque text probe requires the existing solid text brush.");
                Require(background.Color.A == 255 && background.Opacity == 1 && color.Color.A == 255 && color.Opacity == 1, "The diagnostic supports only its pinned opaque scene.");
                var family = target.FontFamily.Source; var marker = family.IndexOf('#');
                Require(marker > 0 && Uri.TryCreate(family[..marker], UriKind.Absolute, out var fontUri) && fontUri.Scheme == "ms-appx", "The diagnostic requires the original bundled font URI.");
                var fontPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, new Uri(family[..marker]).AbsolutePath.TrimStart('/')));
                var fontHash = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(fontPath)));
                var pinnedFont = reference.RootElement.GetProperty("fonts").EnumerateArray().Single(font => font.GetProperty("name").GetString() == Path.GetFileName(fontPath));
                Require(fontHash == pinnedFont.GetProperty("sha256").GetString(), "The diagnostic font differs from the frozen source.");
                var resolvedFamily = new Uri(fontPath).AbsoluteUri + family[marker..]; var nativeBounds = Bounds(target); var baseline = nativeBounds.Y + target.BaselineOffset;
                async Task Capture(string variant, object? rendering = null)
                {
                    await AwaitPresentationAsync(shell); var sourceFile = row.GetProperty("file").GetString()!; var file = variant + "-" + sourceFile;
                    using var source = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.AdminShell." + sourceFile) ?? throw new InvalidOperationException("Source PNG missing.");
                    using var sourceBuffer = new MemoryStream(); await source.CopyToAsync(sourceBuffer); var sourcePixels = sourceBuffer.ToArray();
                    Require(Convert.ToHexStringLower(SHA256.HashData(sourcePixels)) == row.GetProperty("pngSha256").GetString(), "Source PNG hash differs.");
                    using var random = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                    using (var writer = new Windows.Storage.Streams.DataWriter(random)) { writer.WriteBytes(sourcePixels); await writer.StoreAsync(); writer.DetachStream(); }
                    random.Seek(0); var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(random);
                    Require(decoder.PixelWidth == width && decoder.PixelHeight == height, "Text diagnostic source dimensions differ.");
                    var pixels = (await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Straight, new Windows.Graphics.Imaging.BitmapTransform(), Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation, Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
                    try { await NativeScreenCapture.CaptureExactClientAsync(input.Window, Path.Combine(folder, file), pixels); rasterMatches[file] = true; }
                    catch (InvalidOperationException error) when (error.Message == "The composed capture differs from its complete exact reference frame.") { rasterMatches[file] = false; }
                    captures[file] = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(folder, file))));
                    variants.Add(new { variant, id = "header-text", scheme = row.GetProperty("scheme").GetString(), file, sourceFile, measurement = Snapshot(shell), text = target.Text, fontFamily = family, resolvedFamily, fontHash, target.FontSize, target.FontWeight, target.FontStyle, target.FontStretch, target.Language, target.CharacterSpacing, nativeBounds = new { x = nativeBounds.X, y = nativeBounds.Y, width = nativeBounds.Width, height = nativeBounds.Height }, baseline, rendering });
                }
                try
                {
                    await Capture("native"); target.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
                    foreach (var mode in modes)
                    {
                        var left = Math.Floor(nativeBounds.Left); var top = Math.Floor(nativeBounds.Top); var imageWidth = Math.Ceiling(nativeBounds.Right) - left; var imageHeight = Math.Ceiling(nativeBounds.Bottom) - top;
                        using var surface = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), (float)imageWidth, (float)imageHeight, 96, Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized, CanvasAlphaMode.Ignore);
                        using var format = new CanvasTextFormat { FontFamily = resolvedFamily, FontSize = (float)target.FontSize, FontWeight = target.FontWeight, FontStyle = target.FontStyle, FontStretch = target.FontStretch, LocaleName = target.Language, WordWrapping = CanvasWordWrapping.NoWrap, Direction = target.FlowDirection == FlowDirection.RightToLeft ? CanvasTextDirection.RightToLeftThenTopToBottom : CanvasTextDirection.LeftToRightThenTopToBottom };
                        using var layout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(), target.Text, format, float.MaxValue, float.MaxValue);
                        layout.SetPairKerning(0, target.Text.Length, true); layout.SetCharacterSpacing(0, target.Text.Length, 0, (float)(target.CharacterSpacing * target.FontSize / 1000), 0);
                        var draw = new Vector2((float)(nativeBounds.Left - left), (float)(baseline - top - layout.LineMetrics[0].Baseline));
                        CatalogTextRenderingParameters.Settings applied;
                        var coverage = mode.Item1.StartsWith("coverage-", StringComparison.Ordinal);
                        using (var drawing = surface.CreateDrawingSession())
                        {
                            drawing.Clear(coverage ? Windows.UI.Color.FromArgb(255, 0, 0, 0) : background.Color); drawing.TextAntialiasing = mode.Item2;
                            drawing.TextRenderingParameters = mode.Item5 is float gamma ? CatalogTextRenderingParameters.Create(gamma, mode.Item6!.Value, (uint)mode.Item3) : new CanvasTextRenderingParameters(mode.Item3, mode.Item4);
                            applied = CatalogTextRenderingParameters.Read(drawing.TextRenderingParameters);
                            if (mode.Item5 is float requestedGamma) Require(applied.Gamma == requestedGamma && applied.EnhancedContrast == mode.Item6 && applied.ClearTypeLevel == 1 && applied.PixelGeometry == 1 && applied.RenderingMode == (uint)mode.Item3, "Native per-draw parameters were not retained by Win2D.");
                            drawing.DrawTextLayout(layout, draw, coverage ? Windows.UI.Color.FromArgb(255, 255, 255, 255) : color.Color);
                        }
                        if (coverage)
                        {
                            // This is freshly rasterized font coverage, never
                            // reference-image pixels or a comparison mask.
                            var coveragePixels = surface.GetPixelBytes(); var bgr = mode.Item1 == "coverage-bgr565";
                            static int Expand5(byte value) { var q = value >> 3; return (q << 3) | (q >> 2); }
                            static int Expand6(byte value) { var q = value >> 2; return (q << 2) | (q >> 4); }
                            static byte Blend(byte foreground, byte backdrop, int alpha) => (byte)((foreground * alpha + backdrop * (255 - alpha) + 127) / 255);
                            for (var i = 0; i < coveragePixels.Length; i += 4)
                            {
                                var r = Expand5(coveragePixels[i + (bgr ? 0 : 2)]); var g = Expand6(coveragePixels[i + 1]); var b = Expand5(coveragePixels[i + (bgr ? 2 : 0)]);
                                coveragePixels[i] = Blend(color.Color.B, background.Color.B, b); coveragePixels[i + 1] = Blend(color.Color.G, background.Color.G, g); coveragePixels[i + 2] = Blend(color.Color.R, background.Color.R, r); coveragePixels[i + 3] = 255;
                            }
                            surface.SetPixelBytes(coveragePixels);
                        }
                        using var pixelStream = new Windows.Storage.Streams.InMemoryRandomAccessStream(); await surface.SaveAsync(pixelStream, CanvasBitmapFileFormat.Png); pixelStream.Seek(0);
                        var bitmap = new BitmapImage(); await bitmap.SetSourceAsync(pixelStream);
                        var image = new Image { Source = bitmap, Width = imageWidth, Height = imageHeight, Stretch = Stretch.None, UseLayoutRounding = false, IsHitTestVisible = false }; Canvas.SetLeft(image, left); Canvas.SetTop(image, top); Canvas.SetZIndex(image, 900); canvas.Children.Add(image);
                        try { await Capture(mode.Item1, new { antialiasing = mode.Item2.ToString(), mode = mode.Item3.ToString(), gridFit = mode.Item4.ToString(), requestedGamma = mode.Item5, requestedContrast = mode.Item6, applied, coverageFormat = coverage ? (mode.Item1 == "coverage-bgr565" ? "BGR565" : "RGB565") : "None", alpha = surface.AlphaMode.ToString(), dpi = surface.Dpi, draw = new { x = draw.X, y = draw.Y }, layoutBaseline = layout.LineMetrics[0].Baseline }); }
                        finally { canvas.Children.Remove(image); image.Source = null; }
                    }
                }
                finally { target.Foreground = foreground; canvas.Children.Remove(shell); }
                frames.Add(new { id = "header-text", scheme = row.GetProperty("scheme").GetString() });
            }
            Require(frames.Count == 2 && variants.Count == 68 && captures.Count == 68, "Text diagnostic is incomplete.");
            await File.WriteAllTextAsync(Path.Combine(folder, "admin-shell-text-diagnostic.json"), JsonSerializer.Serialize(new { passed = true, scope = "diagnostic-admin-shell-text-rendering", diagnosticOnly = true, referenceSha256 = Convert.ToHexStringLower(SHA256.HashData(referenceBytes)), librarySha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(BleeckerAdminShell).Assembly.Location))), catalogSha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(MainWindow).Assembly.Location))), frames, variants, captures, rasterMatches, rawClientFrames = true, foregroundCapture = true, opaqueTextProbe = true, sourcePixelsPainted = false, productionLibraryChanged = false, osSettingsChanged = false, narratorLaunched = false, sourceRasterParityVerified = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "admin-shell-text-diagnostic", environment = new { processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), displayScale = canvas.XamlRoot.RasterizationScale, textScale = new UISettings().TextScaleFactor } }));
        }
        finally { Content = previous; AppWindow.ResizeClient(previousSize); await AwaitPresentationAsync(CatalogRoot); }
    }
}
