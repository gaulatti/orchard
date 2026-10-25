using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunFooterAsync(string folder)
    {
        using var stream = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.Footer.json") ?? throw new InvalidOperationException("Footer reference missing.");
        using var bytes = new MemoryStream(); await stream.CopyToAsync(bytes); using var reference = JsonDocument.Parse(bytes.ToArray());
        var previous = Content; var size = AppWindow.ClientSize; var canvas = (Canvas)Microsoft.UI.Xaml.Markup.XamlReader.Load("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' UseLayoutRounding='False'/>"); var frames = new List<object>(); var captures = new Dictionary<string, string>();
        void Stage(string stage, JsonElement source) => File.AppendAllText(Path.Combine(folder, "footer-stages.jsonl"), JsonSerializer.Serialize(new { stage, id = source.GetProperty("id").GetString(), scheme = source.GetProperty("scheme").GetString(), at = DateTime.UtcNow }) + "\n");
        static IEnumerable<T> Nodes<T>(DependencyObject root) where T : FrameworkElement { for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++) { var child = VisualTreeHelper.GetChild(root, index); if (child is T target) yield return target; foreach (var nested in Nodes<T>(child)) yield return nested; } }
        object Box(FrameworkElement element) { var box = element.TransformToVisual(canvas).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight)); return new { x = box.X, y = box.Y, width = box.Width, height = box.Height }; }
        static object Paint(Brush brush) { var solid = brush as SolidColorBrush ?? throw new InvalidOperationException("Expected solid Footer paint."); return new { color = solid.Color.ToString(), opacity = solid.Opacity }; }
        static BleeckerFooter Create(JsonElement spec) => FooterReferenceFactory.Create(spec);
        try
        {
            Content = canvas; AppWindow.ResizeClient(new SizeInt32(1280, 900)); await AwaitPresentationAsync(canvas); Require(canvas.XamlRoot.RasterizationScale == 1, "Footer requires the unchanged 100% root.");
            using var input = new NativeInputSession(canvas.XamlRoot, folder, "footer"); await input.AcquireAsync(canvas);
            foreach (var source in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                Stage("mount", source); var spec = source.GetProperty("spec"); AppWindow.ResizeClient(new SizeInt32(spec.GetProperty("viewportWidth").GetInt32(), spec.GetProperty("viewportHeight").GetInt32())); canvas.RequestedTheme = Enum.Parse<ElementTheme>(source.GetProperty("scheme").GetString()!); var control = Create(source.TryGetProperty("initialSpec", out var initialSpec) ? initialSpec : spec); var original = control.Sections.SelectMany(section => section.Items).ToArray(); canvas.Children.Add(control); await AwaitPresentationAsync(control); if (spec.TryGetProperty("mutations", out var mutations)) foreach (var mutation in mutations.EnumerateArray()) { FooterReferenceFactory.Mutate(control, mutation); await AwaitPresentationAsync(control); } await input.MoveAsync(canvas, localPoint: new Point(2, 880)); await AwaitPresentationAsync(control); Stage("layout", source);
                FrameworkElement Part(string name) => Nodes<FrameworkElement>(control).Single(node => node.Name == name);
                var sectionFrames = Nodes<Panel>(control).Where(node => node.Name == "FooterSection").Select(section => new { title = AutomationProperties.GetName(Nodes<FrameworkElement>(section).Single(node => node.Name == "FooterSectionHeading")), box = Box(section), heading = Box(Nodes<FrameworkElement>(section).Single(node => node.Name == "FooterSectionHeading")) }).ToArray();
                var rows = Nodes<HyperlinkButton>(control).Where(link => link.Name is "FooterBrandLink" or "FooterSectionLink" or "FooterSourceLink" or "CustomFooterLink").Select(link =>
                {
                    var context = (BleeckerNavLinkContext)link.DataContext; var text = Nodes<TextBlock>(link).FirstOrDefault(text => text.Name is "FooterLinkTextText" or "FooterSourceTextText"); var peer = FrameworkElementAutomationPeer.CreatePeerForElement(link);
                    return new { href = context.Href, label = context.Label, text = text?.Text ?? control.Brand.BrandName ?? "", external = context.External, marker = (context.Item as FooterReferenceItem)?.Marker, originalItem = original.Any(item => ReferenceEquals(item, context.Item)), box = Box(link), name = peer.GetName(), role = peer.GetAutomationControlType().ToString(), paint = text is null ? null : Paint(text.Foreground) };
                }).ToArray();
                var file = source.GetProperty("file").GetString()!; await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, file)); captures[file] = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(folder, file))));
                frames.Add(new { id = source.GetProperty("id").GetString(), scheme = source.GetProperty("scheme").GetString(), host = Box(control), footer = Box(control), container = Box(Part("FooterContainer")), grid = Box(Part("FooterGrid")), brand = Box(Part("FooterBrandColumn")), description = string.IsNullOrEmpty(control.Brand.Description) ? null : Box(Part("FooterDescription")), bottom = Box(Part("FooterBottom")), bottomLeft = new { box = Box(Part("FooterBottomLeft")) }, bottomRight = new { box = Box(Part("FooterBottomRight")) }, topRule = Box(Part("FooterTopRule")), accent = control.ShowBottomAccent ? Box(Part("FooterAccent")) : null, sections = sectionFrames, links = rows, paint = new { footer = Paint(control.Background), topRule = Paint(((Border)Part("FooterTopRule")).Background), bottomRule = Paint(((Border)Part("FooterBottomRule")).Background), accent = Paint(((Border)Part("FooterAccent")).Background) } });
                Stage("snapshot", source); await File.WriteAllTextAsync(Path.Combine(folder, "footer-partial.json"), JsonSerializer.Serialize(new { diagnosticOnly = true, completed = false, frames, captures })); Stage("capture", source); canvas.Children.Remove(control); await AwaitPresentationAsync(canvas); Stage("unmount", source);
            }
            await File.WriteAllTextAsync(Path.Combine(folder, "footer.json"), JsonSerializer.Serialize(new { passed = true, diagnosticOnly = true, scope = "foreground-footer-source-capture", referenceSha256 = Convert.ToHexStringLower(SHA256.HashData(bytes.ToArray())), librarySha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(BleeckerFooter).Assembly.Location))), catalogSha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(MainWindow).Assembly.Location))), frames, captures, rawClientFrames = true, sourcePixelsPainted = false, inputSequencesVerified = false, sourceRasterParityVerified = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "footer", environment = RuntimeEnvironment() }));
        }
        finally { Content = previous; AppWindow.ResizeClient(size); await AwaitPresentationAsync(CatalogRoot); }
    }
}
