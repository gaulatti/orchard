using System.Diagnostics;
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
    private async Task RunPanelLayoutsAsync(string folder)
    {
        using var stream = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.PanelLayout.json") ?? throw new InvalidOperationException("PanelLayout source reference is missing.");
        using var reference = await JsonDocument.ParseAsync(stream);
        var previous = Content; var originalSize = AppWindow.ClientSize;
        var canvas = (Canvas)XamlReader.Load("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' UseLayoutRounding='False'/>");
        var anchor = new Button { Width = 20, Height = 20, Opacity = 0 }; Canvas.SetLeft(anchor, 840); Canvas.SetTop(anchor, 620); canvas.Children.Add(anchor);
        var frames = new List<object>(); var interactions = new List<object>(); var captures = new Dictionary<string, string>(); var rasterMatches = new Dictionary<string, bool>(); var demoCaptures = new Dictionary<string, string>();
        object Box(FrameworkElement node) { var b = node.TransformToVisual(canvas).TransformBounds(new Rect(0, 0, node.ActualWidth, node.ActualHeight)); return new { x = b.X, y = b.Y, width = b.Width, height = b.Height }; }
        static bool Flag(JsonElement value, string key) => value.TryGetProperty(key, out var result) && result.GetBoolean();
        static double Number(JsonElement value, string key, double defaultValue = double.NaN) => value.TryGetProperty(key, out var result) ? result.GetDouble() : defaultValue;
        var colors = new[] { Windows.UI.Color.FromArgb(255,185,199,209), Windows.UI.Color.FromArgb(255,64,127,163), Windows.UI.Color.FromArgb(255,184,74,80), Windows.UI.Color.FromArgb(255,218,201,159) };
        try
        {
            Content = canvas; AppWindow.ResizeClient(new SizeInt32(900, 680)); await AwaitPresentationAsync(canvas);
            using var input = new NativeInputSession(canvas.XamlRoot, folder, "panel-layout"); await input.AcquireAsync(canvas); AppWindow.ResizeClient(new SizeInt32(900, 680)); await AwaitPresentationAsync(canvas); await input.MoveAsync(anchor);
            foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                var spec = row.GetProperty("spec"); canvas.RequestedTheme = Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!);
                var nodes = new List<(string Id, FrameworkElement Node)>();
                UIElement[] Build(JsonElement items) => items.EnumerateArray().Select((item, index) =>
                {
                    FrameworkElement child;
                    if (item.TryGetProperty("items", out var nested)) { var column = new BleeckerPanelColumn(); foreach (var value in Build(nested)) column.Children.Add(value); child = column; }
                    else
                    {
                        child = new PageFrameBody(colors[index % colors.Length]) { Width = Number(item, "width"), MinWidth = Number(item, "minWidth", 0), Height = Number(item, "height") };
                        nodes.Add((item.GetProperty("id").GetString()!, child));
                    }
                    BleeckerPanelLayout.SetGrow(child, Flag(item, "grow")); if (Flag(item, "hidden")) child.Visibility = Visibility.Collapsed; return (UIElement)child;
                }).ToArray();
                Control layout = Flag(spec, "column") ? new BleeckerPanelColumn() : new BleeckerPanelLayout();
                var collection = layout is BleeckerPanelColumn c ? c.Children : ((BleeckerPanelLayout)layout).Children;
                foreach (var child in Build(spec.GetProperty("items"))) collection.Add(child);
                layout.Width = Number(spec, "width"); layout.Height = Number(spec, "height"); layout.FlowDirection = Flag(spec, "rtl") ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                if (Flag(spec, "padding")) layout.Padding = new Thickness(8);
                Canvas.SetLeft(layout, 20); Canvas.SetTop(layout, 20); canvas.Children.Add(layout); await AwaitPresentationAsync(layout);
                var viewer = Desc<ScrollViewer>(layout).SingleOrDefault();
                frames.Add(new { id = row.GetProperty("id").GetString(), scheme = row.GetProperty("scheme").GetString(), measurement = new { frame = Box(layout),
                    items = nodes.Select(pair => new { id = pair.Id, box = pair.Node.Visibility == Visibility.Collapsed ? new { x = 0d, y = 0d, width = 0d, height = 0d } : Box(pair.Node), wrapper = Box((FrameworkElement)VisualTreeHelper.GetParent(pair.Node)), paint = Box(Desc<Border>(pair.Node).Single()) }).ToArray(),
                    extentWidth = viewer?.ExtentWidth ?? layout.ActualWidth, viewportWidth = viewer?.ViewportWidth ?? layout.ActualWidth,
                    passive = !layout.IsTabStop, group = FrameworkElementAutomationPeer.CreatePeerForElement(layout).GetAutomationControlType() == AutomationControlType.Group } });
                if (row.TryGetProperty("file", out var file))
                {
                    await File.WriteAllTextAsync(Path.Combine(folder, "panel-layout-progress.json"), JsonSerializer.Serialize(new { passed = false, frames }, new JsonSerializerOptions { WriteIndented = true }));
                    using var sourcePng = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.PanelLayout." + file.GetString()) ?? throw new InvalidOperationException("PanelLayout source PNG is missing.");
                    using var bytes = new MemoryStream(); await sourcePng.CopyToAsync(bytes);
                    Require(Convert.ToHexStringLower(SHA256.HashData(bytes.ToArray())) == row.GetProperty("pngSha256").GetString(), "PanelLayout source PNG hash differs.");
                    using var random = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                    using (var writer = new Windows.Storage.Streams.DataWriter(random)) { writer.WriteBytes(bytes.ToArray()); await writer.StoreAsync(); writer.DetachStream(); }
                    random.Seek(0); var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(random);
                    var pixels = (await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Straight, new Windows.Graphics.Imaging.BitmapTransform(), Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation, Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
                    var capture = Path.Combine(folder, file.GetString()!);
                    try { await NativeScreenCapture.CaptureExactClientAsync(input.Window, capture, pixels); rasterMatches[file.GetString()!] = true; }
                    catch (InvalidOperationException error) when (error.Message == "The composed capture differs from its complete exact reference frame.")
                    {
                        // The capture helper saved the complete unmodified failure PNG.
                        // Keep the failed comparison explicit while checking the remaining
                        // geometry/interaction states. It is not visual acceptance.
                        rasterMatches[file.GetString()!] = false;
                    }
                    captures[file.GetString()!] = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(capture)));
                }
                canvas.Children.Remove(layout);
            }
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                canvas.RequestedTheme = theme;
                var editor = new BleeckerInput { Text = "Consumer text" }; var action = new BleeckerButton { Content = "Consumer action" }; var actions = 0; action.Click += (_, _) => actions++;
                var fixedPanel = new BleeckerPanel { Title = "Fixed", Width = 200, Content = editor };
                var top = new BleeckerPanel { Title = "Fixed height", Height = 100 };
                var bottom = new BleeckerPanel { Title = "Growing height", Grow = true, Content = action };
                var column = new BleeckerPanelColumn { Grow = true }; column.Children.Add(top); column.Children.Add(bottom);
                var layout = new BleeckerPanelLayout { Width = 600, Height = 360 }; layout.Children.Add(fixedPanel); layout.Children.Add(column);
                Canvas.SetLeft(layout, 20); Canvas.SetTop(layout, 20); canvas.Children.Add(layout); await AwaitPresentationAsync(layout);
                Require(fixedPanel.ActualWidth == 200 && column.ActualWidth == 400 && top.ActualHeight == 100 && bottom.ActualHeight == 260, "PanelLayout did not consume Panel/Column growth metadata.");
                var demoFile = "panel-layout-demo-" + theme + ".png"; var demoPath = Path.Combine(folder, demoFile);
                await NativeScreenCapture.CaptureClientAsync(input.Window, demoPath); demoCaptures[demoFile] = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(demoPath)));
                await input.ClickAsync(editor); await input.KeyAsync(0x41, editor); var retainedText = editor.Text;
                var editingFocus = FocusManager.GetFocusedElement(canvas.XamlRoot);
                fixedPanel.ClearValue(FrameworkElement.WidthProperty); fixedPanel.Grow = true; await AwaitPresentationAsync(layout);
                Require(fixedPanel.ActualWidth == 300 && column.ActualWidth == 300 && ReferenceEquals(fixedPanel.Content, editor) && editor.Text == retainedText, "Live growth lost identity/value or did not reallocate space.");
                top.Grow = true; top.ClearValue(FrameworkElement.HeightProperty); await AwaitPresentationAsync(layout);
                Require(top.ActualHeight == 180 && bottom.ActualHeight == 180, "Live column growth did not share height.");
                Require(ReferenceEquals(editingFocus, FocusManager.GetFocusedElement(canvas.XamlRoot)), "Live growth replaced the consumer's keyboard focus.");
                var producer = new TextBlock { Tag = false }; column.SetBinding(BleeckerPanelColumn.GrowProperty, new Binding { Source = producer, Path = new PropertyPath("Tag"), Mode = BindingMode.OneWay });
                producer.Tag = true; await AwaitPresentationAsync(layout); Require(column.GetBindingExpression(BleeckerPanelColumn.GrowProperty) is not null && column.Grow && column.ActualWidth == 300, "Column growth binding was lost.");
                var moveUnloads = 0; RoutedEventHandler unloaded = (_, _) => moveUnloads++; fixedPanel.Unloaded += unloaded; column.Unloaded += unloaded;
                layout.Children.Move(1, 0); await AwaitPresentationAsync(layout); layout.Children.Move(1, 0); await AwaitPresentationAsync(layout);
                fixedPanel.Unloaded -= unloaded; column.Unloaded -= unloaded;
                Require(ReferenceEquals(fixedPanel.Content, editor) && editor.Text == retainedText && ReferenceEquals(editingFocus, FocusManager.GetFocusedElement(canvas.XamlRoot)) && moveUnloads == 0, "Moving a child replaced its content, focus or visual lifetime.");
                var button = Desc<Button>(action).Single(); await input.ClickAsync(button); await input.KeyAsync(0x20, button); Require(actions == 2, "Panel layout lost or duplicated a caller action.");
                var originalTemplate = layout.Template; layout.Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><Grid x:Name='PART_Root'/></ControlTemplate>"); await AwaitPresentationAsync(layout);
                layout.Template = originalTemplate; await AwaitPresentationAsync(layout); Require(editor.Text == retainedText && ReferenceEquals(fixedPanel.Content, editor), "Template replacement lost caller state.");
                canvas.Children.Remove(layout); await AwaitPresentationAsync(canvas); canvas.Children.Add(layout); await AwaitPresentationAsync(layout);
                fixedPanel.Width = 700; fixedPanel.Grow = false; column.Grow = false; await AwaitPresentationAsync(layout);
                var viewer = Desc<ScrollViewer>(layout).Single(node => node.Name == "PanelLayoutViewport"); Require(viewer.ScrollableWidth > 0 && !viewer.IsTabStop, "Fixed panels did not produce horizontal overflow or added a stop before editable content.");
                viewer.ChangeView(100, null, null, true); var clock = Stopwatch.StartNew(); while (viewer.HorizontalOffset < 99) { if (clock.ElapsedMilliseconds > 3000) throw new InvalidOperationException("PanelLayout did not scroll horizontally."); await Task.Delay(16); }
                var offset = viewer.HorizontalOffset; fixedPanel.Title = "Retained scrolled child"; await AwaitPresentationAsync(layout); Require(viewer.HorizontalOffset == offset && editor.Text == retainedText, "A header update reset horizontal scrolling or caller text.");
                AutomationProperties.SetName(layout, "Consumer group"); Require(FrameworkElementAutomationPeer.CreatePeerForElement(layout).GetName() == "Consumer group", "PanelLayout replaced caller metadata.");
                layout.Children.Remove(column); await AwaitPresentationAsync(layout); layout.Children.Add(column); await AwaitPresentationAsync(layout); Require(ReferenceEquals(bottom.Content, action), "Collection removal/reinsertion replaced a child.");
                canvas.Children.Remove(layout);
                var plain = new BleeckerPanelLayout { Width = 320, Height = 120 }; plain.Children.Add(new Border { Width = 500 }); plain.Children.Add(new Border { Width = 500 });
                Canvas.SetLeft(plain, 20); Canvas.SetTop(plain, 20); canvas.Children.Add(plain); await AwaitPresentationAsync(plain);
                var plainViewer = Desc<ScrollViewer>(plain).Single(node => node.Name == "PanelLayoutViewport");
                Require(plainViewer.IsTabStop && plainViewer.Focus(FocusState.Keyboard), "A plain overflowing PanelLayout was not keyboard-focusable.");
                await input.KeyAsync(0x27, plainViewer); clock.Restart();
                while (plainViewer.HorizontalOffset <= 0) { if (clock.ElapsedMilliseconds > 3000) throw new InvalidOperationException("PanelLayout did not respond to native Right Arrow."); await Task.Delay(16); }
                var keyboardScroll = plainViewer.HorizontalOffset; canvas.Children.Remove(plain);
                var intrinsic = new BleeckerPanelLayout { Height = 120 }; intrinsic.Children.Add(new Border { Width = 150 }); intrinsic.Children.Add(new Border { Width = 200 });
                var horizontal = new StackPanel { Orientation = Orientation.Horizontal }; horizontal.Children.Add(intrinsic); canvas.Children.Add(horizontal); await AwaitPresentationAsync(intrinsic);
                Require(intrinsic.ActualWidth == 350, "PanelLayout did not measure its intrinsic width in an unbounded parent."); canvas.Children.Remove(horizontal);
                interactions.Add(new { theme = theme.ToString(), passed = true, retainedText, growthMetadata = true, liveGrowth = true, verticalGrowth = true, growthBinding = true, moveRetained = true, moveUnloads, focusRetained = true, actions, templateReplacement = true, remount = true, horizontalScroll = offset, scrollRetained = true, callerMetadata = true, collectionRetained = true, keyboardViewport = true, keyboardScroll, intrinsicWidth = true });
            }
            await File.WriteAllTextAsync(Path.Combine(folder, "panel-layout.json"), JsonSerializer.Serialize(new { passed = true, frames, interactions, captures, demoCaptures, rasterMatches, sourceRasterParityVerified = rasterMatches.Values.All(value => value) }, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "panel-layout", environment = RuntimeEnvironment() }));
        }
        finally { Content = previous; AppWindow.ResizeClient(originalSize); await AwaitPresentationAsync(CatalogRoot); }
    }
}
