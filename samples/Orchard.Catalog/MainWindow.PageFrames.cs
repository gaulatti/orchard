using System.Diagnostics;
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
    private async Task RunPageFramesAsync(string folder)
    {
        using var stream = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.PageFrame.json") ?? throw new InvalidOperationException("PageFrame reference is missing.");
        using var reference = await JsonDocument.ParseAsync(stream);
        var previous = Content; var previousSize = AppWindow.ClientSize;
        var canvas = (Canvas)XamlReader.Load("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' UseLayoutRounding='False'/>");
        var host = new Grid { UseLayoutRounding = false }; Canvas.SetLeft(host, 20); canvas.Children.Add(host);
        var anchor = new Button { Width = 20, Height = 20, Opacity = 0 }; Canvas.SetTop(anchor, 320); canvas.Children.Add(anchor);
        var frames = new List<object>(); var interactions = new List<object>();
        object Box(FrameworkElement node)
        {
            var box = node.TransformToVisual(canvas).TransformBounds(new Rect(0, 0, node.ActualWidth, node.ActualHeight));
            return new { x = box.X, y = box.Y, width = box.Width, height = box.Height };
        }
        async Task Viewport(int width)
        {
            if (AppWindow.ClientSize.Width != width || AppWindow.ClientSize.Height != 360) AppWindow.ResizeClient(new SizeInt32(width, 360));
            var clock = Stopwatch.StartNew();
            while (canvas.XamlRoot is null || Math.Abs(canvas.XamlRoot.Size.Width - width) >= .001 || Math.Abs(canvas.XamlRoot.Size.Height - 360) >= .001)
            { if (clock.ElapsedMilliseconds > 5000) throw new InvalidOperationException("PageFrame viewport did not reach the requested client size."); await Task.Delay(10); }
            Canvas.SetLeft(anchor, width - 40); await AwaitPresentationAsync(canvas);
        }
        static T Option<T>(JsonElement spec, string name, T fallback) where T : struct, Enum => spec.TryGetProperty(name, out var value) ? Enum.Parse<T>(value.GetString()!, true) : fallback;
        try
        {
            Content = canvas; await AwaitPresentationAsync(canvas);
            using var input = new NativeInputSession(canvas.XamlRoot, folder, "page-frame"); await input.AcquireAsync(canvas); await input.MoveAsync(anchor);
            foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                var spec = row.GetProperty("spec"); var theme = Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!);
                canvas.RequestedTheme = theme; await Viewport(spec.GetProperty("viewport").GetInt32());
                host.Width = spec.GetProperty("hostWidth").GetDouble();
                var body = new PageFrameBody();
                var empty = spec.TryGetProperty("empty", out var emptyValue) && emptyValue.GetBoolean();
                var frame = new BleeckerPageFrame { Content = empty ? null : body,
                    Element = Option(spec, "element", BleeckerPageFrameElement.Div), FrameWidth = Option(spec, "width", BleeckerPageFrameWidth.Content),
                    Gutter = Option(spec, "gutter", BleeckerPageFrameGutter.Comfortable), VerticalSpacing = Option(spec, "verticalSpacing", BleeckerPageFrameVerticalSpacing.Comfortable) };
                if (spec.TryGetProperty("name", out var name)) AutomationProperties.SetName(frame, name.GetString());
                if (spec.TryGetProperty("style", out var style))
                {
                    if (style.TryGetProperty("maxWidth", out var maximum)) frame.MaxWidth = maximum.GetDouble();
                    if (style.TryGetProperty("width", out var width)) frame.Width = width.GetDouble();
                    if (style.TryGetProperty("padding", out _)) frame.Padding = new Thickness(7, 3, 7, 3);
                }
                host.Children.Add(frame); await AwaitPresentationAsync(frame);
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(frame);
                frames.Add(new { id = row.GetProperty("id").GetString(), scheme = theme.ToString(), viewport = canvas.XamlRoot.Size.Width,
                    measurement = new { frame = Box(frame), body = empty ? null : Box(body), padding = new[] { frame.Padding.Left, frame.Padding.Top, frame.Padding.Right, frame.Padding.Bottom },
                        element = frame.Element.ToString().ToLowerInvariant(), name = peer.GetName(), maximum = double.IsPositiveInfinity(frame.MaxWidth) ? (double?)null : frame.MaxWidth,
                        passive = !frame.IsTabStop, group = peer.GetAutomationControlType() == AutomationControlType.Group,
                        landmark = peer.GetLandmarkType().ToString(), itemType = peer.GetItemType(), contentRetained = empty ? frame.Content is null : ReferenceEquals(frame.Content, body) } });
                if (row.TryGetProperty("file", out var file))
                {
                    await File.WriteAllTextAsync(Path.Combine(folder, "page-frame-progress.json"), JsonSerializer.Serialize(new { passed = false, frames }, new JsonSerializerOptions { WriteIndented = true }));
                    using var png = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.PageFrame." + file.GetString()) ?? throw new InvalidOperationException("PageFrame source PNG is missing.");
                    using var bytes = new MemoryStream(); await png.CopyToAsync(bytes);
                    Require(Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes.ToArray())) == row.GetProperty("pngSha256").GetString(), "PageFrame source PNG changed.");
                    using var random = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                    using (var writer = new Windows.Storage.Streams.DataWriter(random)) { writer.WriteBytes(bytes.ToArray()); await writer.StoreAsync(); writer.DetachStream(); }
                    random.Seek(0);
                    var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(random);
                    var pixels = (await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Straight,
                        new Windows.Graphics.Imaging.BitmapTransform(), Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation, Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
                    await NativeScreenCapture.CaptureExactClientAsync(input.Window, Path.Combine(folder, file.GetString()!), pixels);
                }
                host.Children.Remove(frame);
                if (frames.Count % 64 == 0) await File.WriteAllTextAsync(Path.Combine(folder, "page-frame-progress.json"), JsonSerializer.Serialize(new { passed = false, frames }, new JsonSerializerOptions { WriteIndented = true }));
            }
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                canvas.RequestedTheme = theme; await Viewport(1536); host.Width = 1000;
                var editor = new BleeckerInput { Text = "Consumer text" };
                var frame = new BleeckerPageFrame { Content = editor, Element = BleeckerPageFrameElement.Main, FrameWidth = BleeckerPageFrameWidth.Reading };
                host.Children.Add(frame); await AwaitPresentationAsync(frame);
                Require(frame.MaxWidth == 768 && frame.ActualWidth == 768 && frame.Padding == new Thickness(40, 64, 40, 64), "PageFrame did not apply its initial viewport/width recipe.");
                await input.ClickAsync(editor); await input.KeyAsync(0x41, editor); var retained = editor.Text;
                frame.Gutter = BleeckerPageFrameGutter.Spacious; frame.VerticalSpacing = BleeckerPageFrameVerticalSpacing.None; frame.FrameWidth = BleeckerPageFrameWidth.Full;
                await AwaitPresentationAsync(frame);
                Require(ReferenceEquals(frame.Content, editor) && editor.Text == retained && frame.ActualWidth == 1000 && frame.Padding == new Thickness(64, 0, 64, 0), "PageFrame recipe changes lost child value/identity or viewport spacing.");
                host.Width = 240;
                foreach (var (viewport, gutter) in new[] { (639, 24d), (640, 40d), (1023, 40d), (1024, 64d) })
                {
                    await Viewport(viewport); await AwaitPresentationAsync(frame);
                    Require(frame.Padding == new Thickness(gutter, 0, gutter, 0) && ReferenceEquals(frame.Content, editor) && editor.Text == retained,
                        "PageFrame did not update an existing narrow frame from the actual window viewport.");
                }
                await Viewport(1536); host.Width = 1000; await AwaitPresentationAsync(frame);
                var producer = new TextBlock { Width = 560, Tag = new Thickness(7, 3, 7, 3) };
                frame.SetBinding(Control.PaddingProperty, new Binding { Source = producer, Path = new PropertyPath("Tag"), Mode = BindingMode.OneWay });
                frame.SetBinding(FrameworkElement.MaxWidthProperty, new Binding { Source = producer, Path = new PropertyPath("Width"), Mode = BindingMode.OneWay });
                frame.FrameWidth = BleeckerPageFrameWidth.Reading; frame.Gutter = BleeckerPageFrameGutter.Compact; await AwaitPresentationAsync(frame);
                Require(frame.Padding == new Thickness(7, 3, 7, 3) && frame.ActualWidth == 560 && frame.GetBindingExpression(Control.PaddingProperty) is not null && frame.GetBindingExpression(FrameworkElement.MaxWidthProperty) is not null,
                    "PageFrame replaced consumer padding/maximum bindings.");
                producer.Tag = new Thickness(4, 8, 4, 8); producer.Width = 520; await AwaitPresentationAsync(frame);
                Require(frame.ActualWidth == 520 && frame.Padding == new Thickness(4, 8, 4, 8), "PageFrame stopped following its consumer binding producers.");
                frame.ClearValue(Control.PaddingProperty); frame.ClearValue(FrameworkElement.MaxWidthProperty); await AwaitPresentationAsync(frame);
                Require(frame.ActualWidth == 768 && frame.Padding == new Thickness(20, 0, 20, 0), "Clearing PageFrame overrides did not restore its current recipes.");
                var originalStyle = frame.Style;
                var style = new Style(typeof(BleeckerPageFrame)) { BasedOn = originalStyle }; style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(5))); style.Setters.Add(new Setter(FrameworkElement.MaxWidthProperty, 480d));
                frame.Style = style; frame.Gutter = BleeckerPageFrameGutter.Spacious; await AwaitPresentationAsync(frame);
                Require(frame.Padding == new Thickness(5) && frame.ActualWidth == 480 && ReferenceEquals(frame.Style, style), "PageFrame replaced consumer style setters.");
                frame.Style = originalStyle; await AwaitPresentationAsync(frame);
                Require(frame.ActualWidth == 768 && frame.Padding == new Thickness(64, 0, 64, 0), "PageFrame did not restore defaults after a caller style was removed.");
                var template = frame.Template; frame.Template = null; await AwaitPresentationAsync(host); frame.Template = template; await AwaitPresentationAsync(frame);
                Require(ReferenceEquals(frame.Content, editor) && editor.Text == retained, "PageFrame template replacement changed the consumer input.");
                host.Children.Remove(frame); await AwaitPresentationAsync(host); host.Children.Add(frame); await AwaitPresentationAsync(frame);
                Require(ReferenceEquals(frame.Content, editor) && editor.Text == retained, "PageFrame remount lost its content/value.");
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(frame);
                Require(peer.GetLandmarkType() == AutomationLandmarkType.Main && !frame.IsTabStop, "PageFrame lost its main landmark or added a tab stop.");
                AutomationProperties.SetLandmarkType(frame, AutomationLandmarkType.None); Require(peer.GetLandmarkType() == AutomationLandmarkType.None, "PageFrame replaced explicit caller landmark metadata.");
                frame.Content = "Consumer template";
                frame.ContentTemplate = (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding}' FontSize='12' LineHeight='20'/></DataTemplate>");
                await AwaitPresentationAsync(frame); var rendered = Desc<TextBlock>(frame).Single(text => text.Text == "Consumer template");
                frame.FrameWidth = BleeckerPageFrameWidth.Full; await AwaitPresentationAsync(frame);
                Require(ReferenceEquals(rendered, Desc<TextBlock>(frame).Single(text => text.Text == "Consumer template")), "PageFrame rebuilt an unchanged consumer data template.");
                var selector = new PageFrameSelector(frame.ContentTemplate); frame.ContentTemplate = null; frame.ContentTemplateSelector = selector; frame.Content = "Selected template";
                await AwaitPresentationAsync(frame); var selected = Desc<TextBlock>(frame).Single(text => text.Text == "Selected template");
                frame.Gutter = BleeckerPageFrameGutter.None; await AwaitPresentationAsync(frame);
                Require(selector.Selections > 0 && ReferenceEquals(frame.ContentTemplateSelector, selector) && ReferenceEquals(selected, Desc<TextBlock>(frame).Single(text => text.Text == "Selected template")),
                    "PageFrame replaced the consumer template selector or rebuilt its unchanged content.");
                frame.ContentTemplateSelector = null;
                var action = new BleeckerButton { Content = "Consumer action" }; var actions = 0; action.Click += (_, _) => actions++; frame.Content = action;
                await AwaitPresentationAsync(frame); var button = Desc<Button>(action).Single();
                await input.ClickAsync(button); await input.KeyAsync(0x20, button);
                Require(actions == 2, "PageFrame lost or duplicated a consumer's native action.");
                var originalElement = frame.Element;
                try { frame.Element = (BleeckerPageFrameElement)999; throw new InvalidOperationException("PageFrame accepted an unknown element."); }
                catch (ArgumentOutOfRangeException) { } finally { frame.Element = originalElement; }
                interactions.Add(new { theme = theme.ToString(), passed = true, retainedText = retained, childRetained = true, nativeEdit = retained != "Consumer text", boundOverridesRetained = true,
                    liveBindingProducer = true, liveViewport = true, clearRestoresRecipe = true, stylePrecedence = true, templateReplacement = true, remount = true, mainLandmark = true, callerMetadataRetained = true,
                    dataTemplateRetained = true, templateSelectorRetained = true, nativeActions = actions, invalidRejected = true });
                host.Children.Remove(frame);
            }
            await File.WriteAllTextAsync(Path.Combine(folder, "page-frame.json"), JsonSerializer.Serialize(new { passed = true, frames, interactions }, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "page-frame", environment = RuntimeEnvironment() }));
        }
        finally { Content = previous; AppWindow.ResizeClient(previousSize); await AwaitPresentationAsync(CatalogRoot); }
    }
    private sealed class PageFrameSelector(DataTemplate template) : DataTemplateSelector
    {
        internal int Selections { get; private set; }
        protected override DataTemplate SelectTemplateCore(object item) { Selections++; return template; }
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) { Selections++; return template; }
    }
    // CSS reports a fractional logical box but device-snaps its opaque flat
    // background. Keep the logical specimen Grid and its native paint part
    // separate; PageFrame never changes consumer rendering or geometry.
    private sealed partial class PageFrameBody : Grid
    {
        private readonly Border paint = new() { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            UseLayoutRounding = false, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 185, 199, 209)) };
        private readonly TranslateTransform shift = new();
        private Rect previous;
        private double previousXBasis;
        internal PageFrameBody(Windows.UI.Color? color = null)
        {
            if (color is { } provided) paint.Background = new SolidColorBrush(provided);
            Height = 40; UseLayoutRounding = false; paint.RenderTransform = shift;
            // Paint must not contribute its snapped width to the specimen's
            // intrinsic size: that would make a ContentPresenter clip the
            // fractional logical box back through its opaque edge.
            var canvas = new Canvas { UseLayoutRounding = false }; canvas.Children.Add(paint); Children.Add(canvas);
            Loaded += (_, _) => { LayoutUpdated += UpdatePaint; UpdatePaint(null, EventArgs.Empty); };
            Unloaded += (_, _) => LayoutUpdated -= UpdatePaint;
        }
        private void UpdatePaint(object? sender, object args)
        {
            if (XamlRoot is null || !IsLoaded) return;
            var transform = TransformToVisual(XamlRoot.Content);
            var bounds = transform.TransformBounds(new Rect(0, 0, ActualWidth, ActualHeight));
            var origin = transform.TransformPoint(new Point()); var xBasis = transform.TransformPoint(new Point(1, 0)).X - origin.X;
            if (bounds == previous && xBasis == previousXBasis) return; previous = bounds; previousXBasis = xBasis;
            var scale = XamlRoot.RasterizationScale;
            double Snap(double value) => Math.Floor(value * scale + .5) / scale;
            paint.Width = Math.Max(0, Snap(bounds.Right) - Snap(bounds.Left)) / Math.Abs(xBasis); paint.Height = Math.Max(0, Snap(bounds.Bottom) - Snap(bounds.Top));
            shift.X = ((xBasis < 0 ? Snap(bounds.Right) : Snap(bounds.Left)) - origin.X) / xBasis; shift.Y = Snap(bounds.Top) - bounds.Top;
        }
    }
}
