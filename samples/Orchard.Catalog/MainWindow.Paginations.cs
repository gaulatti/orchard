using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunPaginationsAsync(string folder)
    {
        using var referenceStream = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.Pagination.json") ?? throw new InvalidOperationException("Pagination reference missing.");
        using var referenceBytes = new MemoryStream(); await referenceStream.CopyToAsync(referenceBytes);
        using var reference = JsonDocument.Parse(referenceBytes.ToArray());
        var previousContent = Content; var previousSize = AppWindow.ClientSize;
        var canvas = (Canvas)Microsoft.UI.Xaml.Markup.XamlReader.Load("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' UseLayoutRounding='False'/>");
        var anchor = new Button { Width = 2, Height = 2, Opacity = 0 }; Canvas.SetLeft(anchor, 680); Canvas.SetTop(anchor, 140); canvas.Children.Add(anchor);
        var frames = new List<object>(); var interactions = new List<object>(); var captures = new Dictionary<string, string>(); var matches = new Dictionary<string, bool>();
        object Box(FrameworkElement element) { var bounds = element.TransformToVisual(canvas).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight)); return new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height }; }
        static IEnumerable<FrameworkElement> Nodes(DependencyObject parent)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) { var child = VisualTreeHelper.GetChild(parent, i); if (child is FrameworkElement element) yield return element; foreach (var nested in Nodes(child)) yield return nested; }
        }
        Button[] Actions(BleeckerPagination pagination) => ((FrameworkElementAutomationPeer.CreatePeerForElement(pagination).GetChildren() ?? []).Select(peer => ((FrameworkElementAutomationPeer)peer).Owner).OfType<Button>()).ToArray();
        static object Paint(Brush brush) => brush is SolidColorBrush solid ? new { color = $"#{solid.Color.A:x2}{solid.Color.R:x2}{solid.Color.G:x2}{solid.Color.B:x2}", opacity = solid.Opacity } : throw new InvalidOperationException("Pagination source paint requires a solid native brush.");
        object ActionSnapshot(Button button)
        {
            var arrow = button.Name is "PaginationPrevious" or "PaginationNext";
            var presenter = Nodes(button).OfType<ContentPresenter>().Single(node => node.Name == "ContentPresenter");
            var foreground = arrow ? Nodes(button).OfType<Microsoft.UI.Xaml.Shapes.Path>().Single().Stroke : Nodes(button).OfType<TextBlock>().Single(node => node.Name == "PaginationPageTextText").Foreground;
            return new { text = arrow ? "" : AutomationProperties.GetName(button), title = button.Name == "PaginationPrevious" ? "Previous page" : button.Name == "PaginationNext" ? "Next page" : "", disabled = !button.IsEnabled, box = Box(button), focused = ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(canvas.XamlRoot), button), opacity = button.Opacity, paint = new { foreground = Paint(foreground), border = Paint(presenter.BorderBrush), background = Paint(presenter.Background) } };
        }
        void Mount(BleeckerPagination pagination) { Canvas.SetLeft(pagination, 20); Canvas.SetTop(pagination, 20); canvas.Children.Add(pagination); }
        try
        {
            Content = canvas; AppWindow.ResizeClient(new SizeInt32(700, 160)); await AwaitPresentationAsync(canvas);
            Require(canvas.XamlRoot.RasterizationScale == 1, "Pagination source probe requires unchanged 100% scale.");
            using var input = new NativeInputSession(canvas.XamlRoot, folder, "pagination"); await input.AcquireAsync(canvas);
            foreach (var source in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                canvas.RequestedTheme = Enum.Parse<ElementTheme>(source.GetProperty("scheme").GetString()!);
                var spec = source.GetProperty("spec");
                var pagination = new BleeckerPagination { Width = spec.GetProperty("width").GetDouble(), CurrentPage = spec.GetProperty("currentPage").GetDouble(), TotalPages = spec.GetProperty("totalPages").GetDouble(), HasPrevPage = spec.GetProperty("hasPrevPage").GetBoolean(), HasNextPage = spec.GetProperty("hasNextPage").GetBoolean(), FlowDirection = spec.TryGetProperty("rtl", out var rtl) && rtl.GetBoolean() ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                Mount(pagination); await AwaitPresentationAsync(pagination); await input.MoveAsync(anchor);
                var state = spec.TryGetProperty("state", out var stateValue) ? stateValue.GetString() : "normal";
                var actions = Actions(pagination);
                var target = spec.TryGetProperty("target", out var targetValue) ? targetValue.GetString() : "page";
                if (state is "hover" or "pressed") await input.MoveAsync(actions[target == "previous" ? 0 : target == "next" ? actions.Length - 1 : 1]);
                if (state == "focus") Require(actions[1].Focus(FocusState.Keyboard), "Pagination could not focus.");
                if (state == "pressed") await input.PressPointerAsync();
                try
                {
                    await AwaitPresentationAsync(pagination); var visible = pagination.TotalPages > 1;
                    var nav = Nodes(pagination).Single(element => element.Name == "PaginationNavigation");
                    var measurement = new { visible, nav = visible ? Box(nav) : null, label = FrameworkElementAutomationPeer.CreatePeerForElement(pagination).GetName(), buttons = actions.Select(ActionSnapshot).ToArray() };
                    var file = source.GetProperty("file").GetString()!;
                    using var sourcePng = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.Pagination." + file) ?? throw new InvalidOperationException("Pagination source PNG missing.");
                    using var bytes = new MemoryStream(); await sourcePng.CopyToAsync(bytes); Require(Convert.ToHexStringLower(SHA256.HashData(bytes.ToArray())) == source.GetProperty("pngSha256").GetString(), "Pagination source PNG hash differs.");
                    using var random = new Windows.Storage.Streams.InMemoryRandomAccessStream(); using (var writer = new Windows.Storage.Streams.DataWriter(random)) { writer.WriteBytes(bytes.ToArray()); await writer.StoreAsync(); writer.DetachStream(); }
                    random.Seek(0); var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(random); var pixels = (await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Straight, new Windows.Graphics.Imaging.BitmapTransform(), Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation, Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
                    try { await NativeScreenCapture.CaptureExactClientAsync(input.Window, Path.Combine(folder, file), pixels); matches[file] = true; }
                    catch (InvalidOperationException error) when (error.Message == "The composed capture differs from its complete exact reference frame.") { matches[file] = false; }
                    captures[file] = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(folder, file))));
                    frames.Add(new { id = source.GetProperty("id").GetString(), scheme = source.GetProperty("scheme").GetString(), measurement });
                }
                finally { if (state == "pressed") input.PointerButton(false); canvas.Children.Remove(pagination); }
            }
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark }) foreach (var accept in new[] { false, true })
            {
                canvas.RequestedTheme = theme; var producer = new TextBlock { Tag = 4d };
                var pagination = new BleeckerPagination { Width = 600, TotalPages = 12, HasPrevPage = true, HasNextPage = true };
                pagination.SetBinding(BleeckerPagination.CurrentPageProperty, new Binding { Source = producer, Path = new PropertyPath("Tag"), Mode = BindingMode.OneWay });
                var requests = new List<double>(); pagination.PageRequested += (_, args) => { requests.Add(args.Page); if (accept) producer.Tag = args.Page; };
                Mount(pagination); await AwaitPresentationAsync(pagination); var samples = new List<object>();
                foreach (var action in new[] { "click", "Space", "Enter" })
                {
                    var next = Actions(pagination)[^1]; if (action == "click") await input.ClickAsync(next); else { Require(next.Focus(FocusState.Keyboard), "Pagination next action could not focus."); await input.KeyAsync(action == "Space" ? (ushort)0x20 : (ushort)0x0D, next); }
                    await AwaitPresentationAsync(pagination); samples.Add(new { action, currentPage = pagination.CurrentPage, requests = requests.ToArray() });
                }
                Require(requests.SequenceEqual(accept ? new[] { 5d, 6, 7 } : new[] { 5d, 5, 5 }) && pagination.CurrentPage == (accept ? 7 : 4) && pagination.GetBindingExpression(BleeckerPagination.CurrentPageProperty) is not null, "Pagination changed the consumer binding or requests.");
                var seven = Actions(pagination).Single(button => AutomationProperties.GetName(button) == "7"); Require(seven.Focus(FocusState.Keyboard), "Pagination page could not focus."); producer.Tag = 8d; await AwaitPresentationAsync(pagination);
                Require(ReferenceEquals(seven, Actions(pagination).Single(button => AutomationProperties.GetName(button) == "7")) && ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(canvas.XamlRoot), seven) && requests.Count == 3, "Pagination lost retained page focus or emitted a programmatic request.");
                var provider = (IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Actions(pagination)[^1]).GetPattern(PatternInterface.Invoke);
                pagination.IsEnabled = false; var disabledRejected = false; try { provider.Invoke(); } catch (ElementNotEnabledException) { disabledRejected = true; } Require(disabledRejected, "Disabled pagination remained actionable."); pagination.IsEnabled = true;
                canvas.Children.Remove(pagination); await AwaitPresentationAsync(canvas); var detachedRejected = false; try { provider.Invoke(); } catch (ElementNotAvailableException) { detachedRejected = true; } Require(detachedRejected, "Detached pagination remained actionable.");
                Mount(pagination); await AwaitPresentationAsync(pagination); pagination.TotalPages = 1; await AwaitPresentationAsync(pagination); Require(pagination.ActualHeight == 0 && Actions(pagination).Length == 0, "Single page retained navigation space or actions."); pagination.TotalPages = 12; await AwaitPresentationAsync(pagination);
                var template = pagination.Template; pagination.Template = null; await AwaitPresentationAsync(canvas); pagination.Template = template; await AwaitPresentationAsync(pagination); producer.Tag = 1d; await AwaitPresentationAsync(pagination);
                Require(pagination.GetBindingExpression(BleeckerPagination.CurrentPageProperty) is not null && pagination.CurrentPage == 1 && requests.Count == 3, "Pagination lost its binding through remount/template replacement.");
                await input.ClickAsync(Actions(pagination)[0]); await AwaitPresentationAsync(pagination); Require(requests[^1] == 0, "Pagination inferred previous availability from the page instead of its producer.");
                interactions.Add(new { scheme = theme.ToString(), accept, samples, bindingRetained = true, focusedButtonRetained = true, disabledRejected, detachedRejected, emptySpaceRemoved = true, remount = true, templateReplacement = true, contradictoryAvailabilityRequest = requests[^1] }); canvas.Children.Remove(pagination);
            }
            await File.WriteAllTextAsync(Path.Combine(folder, "pagination.json"), JsonSerializer.Serialize(new { passed = true, scope = "foreground-pagination-source-capture", referenceSha256 = Convert.ToHexStringLower(SHA256.HashData(referenceBytes.ToArray())), librarySha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(BleeckerPagination).Assembly.Location))), catalogSha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(MainWindow).Assembly.Location))), frames, interactions, captures, rasterMatches = matches, rawClientFrames = true, foregroundCapture = true, sourcePixelsPainted = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "pagination", environment = RuntimeEnvironment() }));
        }
        finally { Content = previousContent; AppWindow.ResizeClient(previousSize); await AwaitPresentationAsync(CatalogRoot); }
    }
}
