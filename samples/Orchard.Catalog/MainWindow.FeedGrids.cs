using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Catalog;
public sealed partial class MainWindow
{
    private async Task RunFeedGridsAsync(string folder)
    {
        using var stream = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.FeedGrid.json") ?? throw new InvalidOperationException("FeedGrid reference is missing.");
        using var reference = await JsonDocument.ParseAsync(stream); var previous = Content; var originalSize = AppWindow.ClientSize;
        var canvas = (Canvas)XamlReader.Load("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' UseLayoutRounding='False'/>");
        var anchor = new Button { Width = 20, Height = 20, Opacity = 0 }; Canvas.SetLeft(anchor, 1480); Canvas.SetTop(anchor, 620); canvas.Children.Add(anchor);
        var frames = new List<object>(); var interactions = new List<object>(); var performance = new List<object>(); var captures = new Dictionary<string, string>(); var rasterMatches = new Dictionary<string, bool>();
        object Box(FrameworkElement node) { var b = node.TransformToVisual(canvas).TransformBounds(new Rect(0, 0, node.ActualWidth, node.ActualHeight)); return new { x = b.X, y = b.Y, width = b.Width, height = b.Height }; }
        var colors = new[] { Windows.UI.Color.FromArgb(255,185,199,209), Windows.UI.Color.FromArgb(255,64,127,163), Windows.UI.Color.FromArgb(255,184,74,80), Windows.UI.Color.FromArgb(255,218,201,159) };
        try
        {
            Content = canvas; AppWindow.ResizeClient(new SizeInt32(1536, 680)); await AwaitPresentationAsync(canvas);
            using var input = new NativeInputSession(canvas.XamlRoot, folder, "feed-grid"); await input.AcquireAsync(canvas); AppWindow.ResizeClient(new SizeInt32(1536, 680)); await AwaitPresentationAsync(canvas); await input.MoveAsync(anchor);
            foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                var spec = row.GetProperty("spec"); canvas.RequestedTheme = Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!);
                var grid = new BleeckerFeedGrid { Width = spec.GetProperty("width").GetDouble() };
                if (spec.TryGetProperty("minimum", out var minimum)) grid.MinColumnWidth = minimum.GetDouble();
                if (spec.TryGetProperty("override", out var custom) && custom.GetBoolean()) { grid.Padding = new Thickness(7); grid.Gap = 5; }
                if (spec.TryGetProperty("rtl", out var rtl) && rtl.GetBoolean()) grid.FlowDirection = FlowDirection.RightToLeft;
                var hidden = spec.TryGetProperty("hidden", out var hiddenValues) ? hiddenValues.EnumerateArray().Select(value => value.GetInt32()).ToArray() : [];
                var bodies = Enumerable.Range(0, spec.GetProperty("count").GetInt32()).Select(index => new PageFrameBody(colors[index % colors.Length]) { Height = 40 + index % 3 * 20, Visibility = hidden.Contains(index) ? Visibility.Collapsed : Visibility.Visible }).ToArray();
                foreach (var body in bodies) grid.Children.Add(body); Canvas.SetLeft(grid, 20); Canvas.SetTop(grid, 20); canvas.Children.Add(grid); await AwaitPresentationAsync(grid);
                frames.Add(new { id = row.GetProperty("id").GetString(), scheme = row.GetProperty("scheme").GetString(), measurement = new { grid = Box(grid), gap = grid.Gap, minimum = grid.MinColumnWidth,
                    padding = new[] { grid.Padding.Left, grid.Padding.Top, grid.Padding.Right, grid.Padding.Bottom }, items = bodies.Select((body, id) => new { id, box = body.Visibility == Visibility.Collapsed ? new { x = 0d, y = 0d, width = 0d, height = 0d } : Box(body) }).ToArray(), passive = !grid.IsTabStop,
                    group = FrameworkElementAutomationPeer.CreatePeerForElement(grid).GetAutomationControlType() == AutomationControlType.Group } });
                if (row.TryGetProperty("file", out var file))
                {
                    await File.WriteAllTextAsync(Path.Combine(folder, "feed-grid-progress.json"), JsonSerializer.Serialize(new { passed = false, frames }, new JsonSerializerOptions { WriteIndented = true }));
                    using var png = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.FeedGrid." + file.GetString()) ?? throw new InvalidOperationException("FeedGrid source PNG missing.");
                    using var bytes = new MemoryStream(); await png.CopyToAsync(bytes); Require(Convert.ToHexStringLower(SHA256.HashData(bytes.ToArray())) == row.GetProperty("pngSha256").GetString(), "FeedGrid source image hash differs.");
                    using var random = new Windows.Storage.Streams.InMemoryRandomAccessStream(); using (var writer = new Windows.Storage.Streams.DataWriter(random)) { writer.WriteBytes(bytes.ToArray()); await writer.StoreAsync(); writer.DetachStream(); } random.Seek(0);
                    var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(random); var pixels = (await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Straight, new Windows.Graphics.Imaging.BitmapTransform(), Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation, Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
                    var capture = Path.Combine(folder, file.GetString()!); try { await NativeScreenCapture.CaptureExactClientAsync(input.Window, capture, pixels); rasterMatches[file.GetString()!] = true; }
                    catch (InvalidOperationException error) when (error.Message == "The composed capture differs from its complete exact reference frame.") { rasterMatches[file.GetString()!] = false; }
                    captures[file.GetString()!] = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(capture)));
                }
                canvas.Children.Remove(grid);
            }
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                canvas.RequestedTheme = theme; var editor = new BleeckerInput { Text = "Consumer text", Height = 40, HorizontalAlignment = HorizontalAlignment.Stretch }; var action = new BleeckerButton { Content = "Consumer action", Height = 40, HorizontalAlignment = HorizontalAlignment.Stretch }; var actions = 0; action.Click += (_, _) => actions++;
                var grid = new BleeckerFeedGrid { Width = 724 }; grid.Children.Add(editor); grid.Children.Add(action); Canvas.SetLeft(grid, 20); Canvas.SetTop(grid, 20); canvas.Children.Add(grid); await AwaitPresentationAsync(grid);
                Require(editor.ActualWidth == 340 && action.ActualWidth == 340, $"FeedGrid did not change to two tracks at its source boundary: editor={editor.ActualWidth}, action={action.ActualWidth}.");
                await input.ClickAsync(editor); await input.KeyAsync(0x41, editor); var retainedText = editor.Text; var focus = FocusManager.GetFocusedElement(canvas.XamlRoot); var unloads = 0; RoutedEventHandler unloaded = (_, _) => unloads++; editor.Unloaded += unloaded;
                grid.Width = 723; await AwaitPresentationAsync(grid); Require(editor.ActualWidth == 691 && action.ActualWidth == 691, "FeedGrid did not return to one column below its boundary.");
                var producer = new TextBlock { Width = 180 }; grid.SetBinding(BleeckerFeedGrid.MinColumnWidthProperty, new Binding { Source = producer, Path = new PropertyPath("Width"), Mode = BindingMode.OneWay }); producer.Width = 200; grid.Gap = 5; grid.Padding = new Thickness(7); await AwaitPresentationAsync(grid);
                Require(grid.MinColumnWidth == 200 && grid.GetBindingExpression(BleeckerFeedGrid.MinColumnWidthProperty) is not null, "FeedGrid replaced or ignored its caller minimum binding.");
                grid.Children.Move(1, 0); await AwaitPresentationAsync(grid); grid.Children.Move(1, 0); await AwaitPresentationAsync(grid); editor.Unloaded -= unloaded;
                Require(editor.Text == retainedText && ReferenceEquals(focus, FocusManager.GetFocusedElement(canvas.XamlRoot)) && unloads == 0, "FeedGrid width/collection changes lost caller input, focus or lifetime.");
                var button = Desc<Button>(action).Single(); await input.ClickAsync(button); await input.KeyAsync(0x20, button); Require(actions == 2, "FeedGrid lost or duplicated a caller action.");
                action.Visibility = Visibility.Collapsed; await AwaitPresentationAsync(grid); action.Visibility = Visibility.Visible; await AwaitPresentationAsync(grid);
                canvas.Children.Remove(grid); await AwaitPresentationAsync(canvas); canvas.Children.Add(grid); await AwaitPresentationAsync(grid); producer.Width = 340; await AwaitPresentationAsync(grid);
                Require(editor.Text == retainedText && ReferenceEquals(grid.Children[0], editor) && grid.MinColumnWidth == 340, "FeedGrid remount lost its child or binding.");
                AutomationProperties.SetName(grid, "Consumer collection"); Require(FrameworkElementAutomationPeer.CreatePeerForElement(grid).GetName() == "Consumer collection", "FeedGrid replaced caller naming metadata.");
                var invalidRejected = false; try { grid.Gap = double.NaN; } catch (ArgumentOutOfRangeException) { invalidRejected = true; } Require(invalidRejected, "FeedGrid accepted an invalid gap.");
                interactions.Add(new { theme = theme.ToString(), passed = true, retainedText, liveBoundary = true, minimumBinding = true, focusRetained = true, moveUnloads = unloads, actions, remount = true, metadataRetained = true, visibility = true, invalidRejected }); canvas.Children.Remove(grid);
            }
            foreach (var count in new[] { 128, 512, 2048 })
            {
                var benchmark = new BleeckerFeedGrid { Width = 800 }; for (var index = 0; index < count; index++) benchmark.Children.Add(new Border { Height = 20 });
                canvas.Children.Add(benchmark); await AwaitPresentationAsync(benchmark); var clock = System.Diagnostics.Stopwatch.StartNew();
                for (var iteration = 0; iteration < 20; iteration++) { benchmark.Width = iteration % 2 == 0 ? 800.375 : 800; benchmark.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); benchmark.Arrange(new Rect(0, 0, benchmark.DesiredSize.Width, benchmark.DesiredSize.Height)); }
                clock.Stop(); Require(benchmark.Children.Count == count && benchmark.ActualWidth == 800, "FeedGrid performance sample did not arrange the real native collection.");
                performance.Add(new { children = count, iterations = 20, totalMilliseconds = clock.Elapsed.TotalMilliseconds, millisecondsPerLayout = clock.Elapsed.TotalMilliseconds / 20, loaded = benchmark.IsLoaded }); canvas.Children.Remove(benchmark);
            }
            await File.WriteAllTextAsync(Path.Combine(folder, "feed-grid.json"), JsonSerializer.Serialize(new { passed = true, frames, interactions, performance, captures, rasterMatches, sourceRasterParityVerified = rasterMatches.Values.All(value => value) }, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "feed-grid", environment = RuntimeEnvironment() }));
        }
        finally { Content = previous; AppWindow.ResizeClient(originalSize); await AwaitPresentationAsync(CatalogRoot); }
    }
}
