using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Windows.UI.ViewManagement;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    // The clock is controlled only in this catalog diagnostic. Targets still
    // come from real input and production palette/state handling, never CSS
    // samples. This does not claim the normal OS-preference event-clock path.
    private async Task RunPaginationMotionAsync(string folder)
    {
        using var stream = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.PaginationMotion.json") ?? throw new InvalidOperationException("Pagination motion reference missing.");
        using var bytes = new MemoryStream(); await stream.CopyToAsync(bytes); using var reference = JsonDocument.Parse(bytes.ToArray());
        var previousContent = Content; var previousSize = AppWindow.ClientSize;
        var canvas = (Canvas)Microsoft.UI.Xaml.Markup.XamlReader.Load("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' UseLayoutRounding='False'/>");
        var anchor = new Button { Width = 2, Height = 2, Opacity = 0 }; Canvas.SetLeft(anchor, 680); Canvas.SetTop(anchor, 140); canvas.Children.Add(anchor);
        var frames = new List<object>(); var captures = new Dictionary<string, string>(); var lifecycle = new List<object>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var buttonType = typeof(BleeckerPagination).GetNestedType("ActionButton", BindingFlags.NonPublic)!;
        var modelField = buttonType.GetField("paintMotion", flags)!; var renderingField = buttonType.GetField("rendering", flags)!;
        var stop = buttonType.GetMethod("StopRendering", flags)!; var paint = buttonType.GetMethod("PaintFrame", flags)!; var request = buttonType.GetMethod("RequestPaint", flags)!;
        var modelType = modelField.FieldType; var at = modelType.GetMethod("At", flags)!; var retarget = modelType.GetMethod("Retarget", flags)!;
        static IEnumerable<T> Desc<T>(DependencyObject root) where T : DependencyObject
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++) { var child = VisualTreeHelper.GetChild(root, index); if (child is T target) yield return target; foreach (var nested in Desc<T>(child)) yield return nested; }
        }
        static Button[] Actions(BleeckerPagination pagination) => (FrameworkElementAutomationPeer.CreatePeerForElement(pagination).GetChildren() ?? []).Select(peer => ((FrameworkElementAutomationPeer)peer).Owner).OfType<Button>().ToArray();
        static object BrushValue(Brush brush) => brush is SolidColorBrush solid ? new { color = $"#{solid.Color.A:x2}{solid.Color.R:x2}{solid.Color.G:x2}{solid.Color.B:x2}", opacity = solid.Opacity } : throw new InvalidOperationException("Motion did not paint a solid native brush.");
        object Snapshot(Button button)
        {
            var presenter = Desc<ContentPresenter>(button).Single(element => element.Name == "ContentPresenter");
            var foreground = button.Name is "PaginationPrevious" or "PaginationNext" ? Desc<Microsoft.UI.Xaml.Shapes.Path>(button).Single().Stroke : Desc<TextBlock>(button).Single(element => element.Name == "PaginationPageTextText").Foreground;
            var scale = (ScaleTransform)button.RenderTransform;
            return new { foreground = BrushValue(foreground), background = BrushValue(presenter.Background), border = BrushValue(presenter.BorderBrush), opacity = button.Opacity, scale = new[] { scale.ScaleX, scale.ScaleY }, rendering = (bool)renderingField.GetValue(button)! };
        }
        try
        {
            Content = canvas; AppWindow.ResizeClient(new SizeInt32(700, 160)); await AwaitPresentationAsync(canvas);
            Require(canvas.XamlRoot.RasterizationScale == 1, "Motion diagnostic requires unchanged 100% display scale.");
            using var input = new NativeInputSession(canvas.XamlRoot, folder, "pagination-motion"); await input.AcquireAsync(canvas);
            foreach (var row in reference.RootElement.GetProperty("transitions").EnumerateArray())
            {
                var theme = row.GetProperty("scheme").GetString()!; var role = row.GetProperty("role").GetString()!; var kind = row.GetProperty("kind").GetString()!; var reduced = row.GetProperty("reduced").GetBoolean();
                canvas.RequestedTheme = Enum.Parse<ElementTheme>(theme);
                var pagination = new BleeckerPagination { Width = 600, CurrentPage = kind == "deselect" ? 1 : 4, TotalPages = 12, HasPrevPage = kind != "enable", HasNextPage = true };
                Canvas.SetLeft(pagination, 20); Canvas.SetTop(pagination, 20); canvas.Children.Add(pagination); await AwaitPresentationAsync(pagination); await input.MoveAsync(anchor);
                var target = role == "previous" ? Actions(pagination)[0] : Actions(pagination).Single(button => AutomationProperties.GetName(button) == (role == "page" ? "1" : "4"));
                var pressed = false;
                try
                {
                    if (kind is "leave" or "press" or "release") await input.MoveAsync(target);
                    if (kind == "release") { await input.PressPointerAsync(); pressed = true; }
                    await AwaitPresentationAsync(pagination); stop.Invoke(target, null); var model = modelField.GetValue(target)!; var before = at.Invoke(model, [double.PositiveInfinity])!;
                    switch (kind)
                    {
                        case "hover": await input.MoveAsync(target); break;
                        case "leave": await input.MoveAsync(anchor); break;
                        case "press": await input.PressPointerAsync(); pressed = true; break;
                        case "release": input.PointerButton(false); pressed = false; break;
                        case "disable": pagination.HasPrevPage = false; break;
                        case "enable": pagination.HasPrevPage = true; break;
                        case "select": pagination.CurrentPage = 1; break;
                        case "deselect": pagination.CurrentPage = 4; break;
                        default: throw new InvalidOperationException("Unknown source motion action.");
                    }
                    await AwaitPresentationAsync(pagination); stop.Invoke(target, null); var after = at.Invoke(model, [double.PositiveInfinity])!;
                    retarget.Invoke(model, [before, 0d, false]); retarget.Invoke(model, [after, 0d, !reduced]);
                    var samples = new List<object>();
                    foreach (var sample in row.GetProperty("samples").EnumerateArray())
                    {
                        var milliseconds = sample.GetProperty("milliseconds").GetDouble(); if (reduced && milliseconds == 0) continue;
                        paint.Invoke(target, [milliseconds]); await AwaitPresentationAsync(canvas);
                        samples.Add(new { milliseconds, paint = Snapshot(target) });
                        if (!reduced && role == "page" && kind == "hover" && milliseconds is 95 or 190)
                        {
                            var file = $"pagination-motion-{theme}-{milliseconds}.png"; await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, file)); captures[file] = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(folder, file))));
                        }
                    }
                    frames.Add(new { scheme = theme, role, kind, reduced, samples }); request.Invoke(target, [false]);
                }
                finally
                {
                    if (pressed) input.PointerButton(false);
                    var buttons = Actions(pagination); canvas.Children.Remove(pagination); await AwaitPresentationAsync(canvas);
                    var released = buttons.All(button => !(bool)renderingField.GetValue(button)! && ((System.Collections.ICollection)buttonType.GetField("brushTokens", flags)!.GetValue(button)!).Count == 0 && ((System.Collections.ICollection)buttonType.GetField("hoverTokens", flags)!.GetValue(button)!).Count == 0);
                    Require(released, "Detached Pagination retained paint callbacks."); lifecycle.Add(new { scheme = theme, role, kind, reduced, detachedSubscriptionsReleased = released });
                }
            }
            Require(frames.Count == 64 && lifecycle.Count == 64 && captures.Count == 4, "Incomplete native controlled-time motion diagnostic.");
            await File.WriteAllTextAsync(Path.Combine(folder, "pagination-motion.json"), JsonSerializer.Serialize(new { passed = true, scope = "controlled-time-native-pagination-paint", diagnosticOnly = true, referenceSha256 = Convert.ToHexStringLower(SHA256.HashData(bytes.ToArray())), librarySha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(BleeckerPagination).Assembly.Location))), catalogSha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(MainWindow).Assembly.Location))), frames, lifecycle, captures, targetsFromProductionInput = true, sourcePixelsPainted = false, osSettingsChanged = false, systemAnimationsEnabled = new UISettings().AnimationsEnabled, normalRealtimeMotionVerified = false, sourceRasterParityVerified = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "pagination-motion", environment = RuntimeEnvironment() }));
        }
        finally { Content = previousContent; AppWindow.ResizeClient(previousSize); await AwaitPresentationAsync(CatalogRoot); }
    }
}
