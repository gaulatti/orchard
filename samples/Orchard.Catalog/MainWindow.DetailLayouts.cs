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
    private async Task RunDetailLayoutsAsync(string folder)
    {
        using var stream = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.DetailLayout.json") ?? throw new InvalidOperationException("DetailLayout reference is missing.");
        using var reference = await JsonDocument.ParseAsync(stream);
        var previous = Content; var originalSize = AppWindow.ClientSize;
        var canvas = (Canvas)XamlReader.Load("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' UseLayoutRounding='False'/>");
        // The source host's top margin collapses outside the themed body.
        // Its first 20 pixels are the browser's white canvas in both themes.
        var marginCanvas = new Border { Height = 20, Background = new SolidColorBrush(Microsoft.UI.Colors.White), UseLayoutRounding = false }; canvas.Children.Add(marginCanvas);
        var anchor = new Button { Width = 20, Height = 20, Opacity = 0 }; Canvas.SetTop(anchor, 640); canvas.Children.Add(anchor);
        var frames = new List<object>(); var interactions = new List<object>();
        object Box(FrameworkElement node)
        {
            var r = node.TransformToVisual(canvas).TransformBounds(new Rect(0, 0, node.ActualWidth, node.ActualHeight));
            return new { x = r.X, y = r.Y, width = r.Width, height = r.Height };
        }
        async Task Viewport(int width)
        {
            if (AppWindow.ClientSize.Width != width || AppWindow.ClientSize.Height != 680) AppWindow.ResizeClient(new SizeInt32(width, 680));
            var clock = Stopwatch.StartNew();
            while (canvas.XamlRoot is null || Math.Abs(canvas.XamlRoot.Size.Width - width) >= .001 || Math.Abs(canvas.XamlRoot.Size.Height - 680) >= .001)
            { if (clock.ElapsedMilliseconds > 5000) throw new InvalidOperationException("DetailLayout viewport did not resize."); await Task.Delay(10); }
            marginCanvas.Width = width; Canvas.SetLeft(anchor, width - 40); await AwaitPresentationAsync(canvas);
        }
        async Task Scroll(ScrollViewer viewer, double offset)
        {
            viewer.ChangeView(null, offset, null, true); var expected = Math.Min(offset, viewer.ScrollableHeight); var clock = Stopwatch.StartNew();
            while (Math.Abs(viewer.VerticalOffset - expected) >= .001)
            { if (clock.ElapsedMilliseconds > 3000) throw new InvalidOperationException("DetailLayout scroll position did not settle."); await Task.Delay(10); }
            await AwaitPresentationAsync(viewer);
        }
        static T Option<T>(JsonElement spec, string name, T fallback) where T : struct, Enum => spec.TryGetProperty(name, out var value) ? Enum.Parse<T>(value.GetString()!, true) : fallback;
        static double Number(JsonElement spec, string name, double fallback) => spec.TryGetProperty(name, out var value) ? value.GetDouble() : fallback;
        static object? Scalar(JsonElement value) => value.ValueKind switch { JsonValueKind.Null => null, JsonValueKind.String => value.GetString(), JsonValueKind.True => true, JsonValueKind.False => false, JsonValueKind.Number => value.GetDouble(), _ => throw new InvalidOperationException("Unsupported DetailLayout scalar.") };
        static ScrollViewer Scroller(UIElement content, double width, double height) => new() { Content = content, Width = width, Height = height, IsTabStop = false, UseLayoutRounding = false,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, HorizontalScrollMode = ScrollMode.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollMode = ScrollMode.Enabled, ZoomMode = ZoomMode.Disabled };
        try
        {
            Content = canvas; await AwaitPresentationAsync(canvas);
            using var input = new NativeInputSession(canvas.XamlRoot, folder, "detail-layout"); await input.AcquireAsync(canvas); await input.MoveAsync(anchor);
            foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                var spec = row.GetProperty("spec"); var theme = Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!); canvas.RequestedTheme = theme;
                await Viewport(spec.GetProperty("viewport").GetInt32());
                var mainBody = new PageFrameBody { Height = Number(spec, "mainHeight", 400) };
                var railBody = new PageFrameBody(Windows.UI.Color.FromArgb(255, 166, 93, 87)) { Height = Number(spec, "railHeight", 100) };
                var empty = spec.TryGetProperty("empty", out var emptyValue) && emptyValue.GetBoolean(); var railValue = spec.GetProperty("rail");
                var boxedRail = railValue.ValueKind == JsonValueKind.String && railValue.GetString() == "box";
                var layout = new BleeckerDetailLayout { Width = spec.GetProperty("width").GetDouble(), Content = empty ? null : mainBody,
                    Ratio = Option(spec, "ratio", BleeckerDetailLayoutRatio.Primary), Side = Option(spec, "side", BleeckerDetailLayoutSide.End),
                    Sticky = !spec.TryGetProperty("sticky", out var sticky) || sticky.GetBoolean(), SideRail = boxedRail ? railBody : Scalar(railValue),
                    FlowDirection = spec.TryGetProperty("rtl", out var rtl) && rtl.GetBoolean() ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                if (spec.TryGetProperty("label", out var label)) layout.SideRailLabel = label.GetString();
                if (spec.TryGetProperty("layoutHeight", out var height)) layout.Height = height.GetDouble();
                var scrollMode = spec.TryGetProperty("scroll", out var scrollValue) && scrollValue.GetBoolean(); ScrollViewer? viewer = null; FrameworkElement host = layout;
                if (scrollMode)
                {
                    var stack = new StackPanel { Width = layout.Width, UseLayoutRounding = false }; stack.Children.Add(new Border { Height = 120 }); stack.Children.Add(layout); stack.Children.Add(new Border { Height = 600 });
                    viewer = Scroller(stack, layout.Width, 240); host = viewer;
                }
                Canvas.SetLeft(host, 20); Canvas.SetTop(host, 20); canvas.Children.Add(host); await AwaitPresentationAsync(layout);
                if (viewer is not null) await Scroll(viewer, row.GetProperty("measurement").GetProperty("scrollOffset").GetDouble());
                var main = Desc<FrameworkElement>(layout).Single(element => element.Name == "DetailContent");
                var rail = Desc<FrameworkElement>(layout).Single(element => element.Name == "DetailSideRail");
                var rootPeer = FrameworkElementAutomationPeer.CreatePeerForElement(layout) ?? throw new InvalidOperationException("DetailLayout group peer is missing.");
                var railPeer = rail.Visibility == Visibility.Visible
                    ? FrameworkElementAutomationPeer.CreatePeerForElement(rail) ?? throw new InvalidOperationException("Visible DetailLayout complementary peer is missing.") : null;
                frames.Add(new { id = row.GetProperty("id").GetString(), scheme = theme.ToString(), viewport = canvas.XamlRoot.Size.Width,
                    measurement = new { layout = Box(layout), main = Box(main), rail = rail.Visibility == Visibility.Visible ? Box(rail) : null,
                        mainBody = empty ? null : Box(mainBody), railBody = boxedRail ? Box(railBody) : null,
                        scrollOffset = viewer?.VerticalOffset ?? 0, scrollViewport = viewer is not null ? Box(viewer) : null,
                        label = railPeer?.GetName(), complementary = railPeer is not null && railPeer.GetItemType() == "complementary" && railPeer.GetLandmarkType() == AutomationLandmarkType.Custom,
                        passive = !layout.IsTabStop && !((Control)rail).IsTabStop, group = rootPeer.GetAutomationControlType() == AutomationControlType.Group,
                        contentRetained = empty ? layout.Content is null : ReferenceEquals(layout.Content, mainBody), railRetained = !boxedRail || ReferenceEquals(layout.SideRail, railBody) } });
                if (row.TryGetProperty("file", out var file))
                {
                    await File.WriteAllTextAsync(Path.Combine(folder, "detail-layout-progress.json"), JsonSerializer.Serialize(new { passed = false, frames }, new JsonSerializerOptions { WriteIndented = true }));
                    using var png = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.DetailLayout." + file.GetString()) ?? throw new InvalidOperationException("DetailLayout source PNG is missing.");
                    using var bytes = new MemoryStream(); await png.CopyToAsync(bytes);
                    Require(Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes.ToArray())) == row.GetProperty("pngSha256").GetString(), "DetailLayout PNG changed.");
                    using var random = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                    using (var writer = new Windows.Storage.Streams.DataWriter(random)) { writer.WriteBytes(bytes.ToArray()); await writer.StoreAsync(); writer.DetachStream(); }
                    random.Seek(0); var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(random);
                    var pixels = (await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Straight,
                        new Windows.Graphics.Imaging.BitmapTransform(), Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation, Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
                    await NativeScreenCapture.CaptureExactClientAsync(input.Window, Path.Combine(folder, file.GetString()!), pixels);
                }
                canvas.Children.Remove(host);
                if (frames.Count % 64 == 0) await File.WriteAllTextAsync(Path.Combine(folder, "detail-layout-progress.json"), JsonSerializer.Serialize(new { passed = false, frames }, new JsonSerializerOptions { WriteIndented = true }));
            }
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                canvas.RequestedTheme = theme; await Viewport(1280);
                var main = new BleeckerInput { Text = "Main value" }; var rail = new BleeckerInput { Text = "Rail value" };
                var layout = new BleeckerDetailLayout { Width = 900, Content = main, SideRail = rail, SideRailLabel = "Related details", Sticky = false };
                Canvas.SetLeft(layout, 20); Canvas.SetTop(layout, 20); canvas.Children.Add(layout); await AwaitPresentationAsync(layout);
                await input.ClickAsync(main); await input.KeyAsync(0x41, main); await input.ClickAsync(rail); await input.KeyAsync(0x42, rail);
                var mainValue = main.Text; var railValue = rail.Text; layout.Side = BleeckerDetailLayoutSide.Start; layout.Ratio = BleeckerDetailLayoutRatio.Balanced; await AwaitPresentationAsync(layout);
                Require(ReferenceEquals(layout.Content, main) && ReferenceEquals(layout.SideRail, rail) && main.Text == mainValue && rail.Text == railValue, "DetailLayout replaced consumer input or values.");
                Require(main.Focus(FocusState.Keyboard), "DetailLayout main input could not receive keyboard focus.");
                var tabReleased = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var tabDowns = 0; var tabUps = 0; var tabTargets = true;
                var tabDown = new KeyEventHandler((_, args) => { if ((ushort)args.Key == 0x09) { tabDowns++; tabTargets &= ReferenceEquals(args.OriginalSource, main); } });
                var tabUp = new KeyEventHandler((_, args) => { if ((ushort)args.Key == 0x09) { tabUps++; tabTargets &= ReferenceEquals(args.OriginalSource, rail); tabReleased.TrySetResult(); } });
                canvas.AddHandler(UIElement.KeyDownEvent, tabDown, true); canvas.AddHandler(UIElement.KeyUpEvent, tabUp, true);
                try { input.Key(0x09); await tabReleased.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
                finally { canvas.RemoveHandler(UIElement.KeyDownEvent, tabDown); canvas.RemoveHandler(UIElement.KeyUpEvent, tabUp); }
                Require(tabDowns == 1 && tabUps == 1 && tabTargets && ReferenceEquals(FocusManager.GetFocusedElement(canvas.XamlRoot), rail),
                    "Visual start rail changed physical main-then-rail Tab order or event targets.");
                layout.SideRail = null; await AwaitPresentationAsync(layout); layout.SideRail = rail; await AwaitPresentationAsync(layout);
                Require(ReferenceEquals(layout.SideRail, rail) && rail.Text == railValue, "DetailLayout lost a removed/reinserted consumer rail.");
                var template = layout.Template; layout.Template = null; await AwaitPresentationAsync(canvas); layout.Template = template; await AwaitPresentationAsync(layout);
                Require(ReferenceEquals(layout.Content, main) && main.Text == mainValue && rail.Text == railValue, "DetailLayout template replacement lost original input state.");
                canvas.Children.Remove(layout); await AwaitPresentationAsync(canvas); canvas.Children.Add(layout); await AwaitPresentationAsync(layout);
                Require(main.Text == mainValue && rail.Text == railValue, "DetailLayout remount reset consumer values.");
                foreach (var (width, gap, columns) in new[] { (1023, 40d, false), (1024, 48d, true), (1279, 48d, true), (1280, 64d, true) })
                {
                    await Viewport(width); await AwaitPresentationAsync(layout);
                    var mainSlot = Desc<FrameworkElement>(layout).Single(element => element.Name == "DetailContent");
                    var railSlot = Desc<FrameworkElement>(layout).Single(element => element.Name == "DetailSideRail");
                    var mainBounds = mainSlot.TransformToVisual(layout).TransformBounds(new Rect(0, 0, mainSlot.ActualWidth, mainSlot.ActualHeight));
                    var railBounds = railSlot.TransformToVisual(layout).TransformBounds(new Rect(0, 0, railSlot.ActualWidth, railSlot.ActualHeight));
                    Require(columns ? Math.Abs(mainBounds.Left - railBounds.Right - gap) < .001 : Math.Abs(railBounds.Top - mainBounds.Bottom - gap) < .001,
                        "Existing DetailLayout did not update from its actual window viewport.");
                    Require(main.Text == mainValue && rail.Text == railValue, "DetailLayout viewport change reset consumer values.");
                }
                var aside = Desc<FrameworkElement>(layout).Single(element => element.Name == "DetailSideRail"); layout.SideRailLabel = "Updated related details";
                Require(FrameworkElementAutomationPeer.CreatePeerForElement(aside).GetName() == "Updated related details", "DetailLayout did not update its complementary label.");
                layout.Content = "Template main"; layout.ContentTemplate = (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding}'/></DataTemplate>");
                layout.SideRail = "Template rail"; layout.SideRailTemplate = layout.ContentTemplate; await AwaitPresentationAsync(layout);
                var rendered = Desc<TextBlock>(layout).Single(text => text.Text == "Template main"); layout.Side = BleeckerDetailLayoutSide.End; await AwaitPresentationAsync(layout);
                Require(ReferenceEquals(rendered, Desc<TextBlock>(layout).Single(text => text.Text == "Template main")), "DetailLayout rebuilt unchanged consumer template content.");
                var selector = new PageFrameSelector(layout.SideRailTemplate); layout.SideRailTemplate = null; layout.SideRailTemplateSelector = selector; await AwaitPresentationAsync(layout);
                Require(selector.Selections > 0 && Desc<TextBlock>(layout).Any(text => text.Text == "Template rail"), "DetailLayout lost its side-rail template selector.");
                foreach (var value in new object[] { true, 42, 0, false })
                {
                    layout.Content = value; layout.SideRail = value; await AwaitPresentationAsync(layout);
                    foreach (var slotName in new[] { "DetailContent", "DetailRailText" })
                    {
                        var slot = Desc<FrameworkElement>(layout).Single(element => element.Name == slotName);
                        var presenter = Desc<ContentPresenter>(slot).Single();
                        Require(presenter.Content?.GetType() == value.GetType() && Equals(presenter.Content, value),
                            "DetailLayout converted an original numeric/Boolean model before its caller template or selector received it.");
                    }
                    Require(Desc<FrameworkElement>(layout).Single(element => element.Name == "DetailSideRail").Visibility == Visibility.Visible,
                        "An explicit rail template lost its false/zero model.");
                }
                layout.ContentTemplate = null; layout.SideRailTemplateSelector = null; layout.SideRail = null;
                var action = new BleeckerButton { Content = "Consumer action" }; var actions = 0; action.Click += (_, _) => actions++; layout.Content = action; await AwaitPresentationAsync(layout);
                var button = Desc<Button>(action).Single(); await input.ClickAsync(button); await input.KeyAsync(0x20, button); Require(actions == 2, "DetailLayout lost or duplicated consumer actions.");
                try { layout.Ratio = (BleeckerDetailLayoutRatio)999; throw new InvalidOperationException("DetailLayout accepted an invalid ratio."); } catch (ArgumentOutOfRangeException) { }
                interactions.Add(new { theme = theme.ToString(), passed = true, mainValue, railValue, inputsRetained = true, sourceTabOrder = true, railRemount = true,
                    templateReplacement = true, remount = true, liveViewport = true, labelUpdate = true, templateRetained = true, selectorRetained = true, originalTemplateValues = true, nativeActions = actions, invalidRejected = true });
                canvas.Children.Remove(layout);
            }
            var scrollOwners = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                canvas.RequestedTheme = theme; await Viewport(1280);
                var main = new PageFrameBody { Height = 400 }; var rail = new PageFrameBody { Height = 100 };
                var layout = new BleeckerDetailLayout { Width = 900, Content = main, SideRail = rail };
                StackPanel Stack()
                {
                    var stack = new StackPanel { Width = 900, UseLayoutRounding = false }; stack.Children.Add(new Border { Height = 120 });
                    stack.Children.Add(layout); stack.Children.Add(new Border { Height = 600 }); return stack;
                }
                double RailTop(FrameworkElement relative) => rail.TransformToVisual(relative).TransformPoint(new Point()).Y;
                var innerStack = Stack(); var inner = Scroller(innerStack, 900, 240);
                var outerStack = new StackPanel(); outerStack.Children.Add(new Border { Height = 50 }); outerStack.Children.Add(inner); outerStack.Children.Add(new Border { Height = 600 });
                var outer = Scroller(outerStack, 900, 320); Canvas.SetLeft(outer, 20); Canvas.SetTop(outer, 20); canvas.Children.Add(outer); await AwaitPresentationAsync(layout);
                await Scroll(inner, 160); Require(Math.Abs(RailTop(inner) - 32) < .001, "DetailLayout did not use its nearest ScrollViewer.");
                await Scroll(outer, 40); Require(Math.Abs(RailTop(inner) - 32) < .001, "Outer scrolling changed the nearest sticky viewport.");
                layout.Sticky = false; await AwaitPresentationAsync(layout); Require(Math.Abs(RailTop(inner) + 40) < .001, "Disabling sticky kept its owned translation.");
                layout.Sticky = true; await AwaitPresentationAsync(layout); Require(Math.Abs(RailTop(inner) - 32) < .001, "Re-enabling sticky did not reconnect its scroll owner.");
                innerStack.Children.Remove(layout); await AwaitPresentationAsync(canvas);
                var modernStack = Stack(); var modern = new ScrollView { Width = 900, Height = 240, Content = modernStack, UseLayoutRounding = false,
                    HorizontalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden,
                    HorizontalScrollMode = ScrollingScrollMode.Disabled, VerticalScrollMode = ScrollingScrollMode.Enabled, ZoomMode = ScrollingZoomMode.Disabled };
                Canvas.SetLeft(modern, 20); Canvas.SetTop(modern, 20); canvas.Children.Add(modern); await AwaitPresentationAsync(layout);
                async Task ModernScroll(double offset)
                {
                    modern.ScrollTo(0, offset, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore)); var clock = Stopwatch.StartNew();
                    while (Math.Abs(modern.VerticalOffset - offset) >= .001)
                    { if (clock.ElapsedMilliseconds > 3000) throw new InvalidOperationException("DetailLayout ScrollView offset did not settle."); await Task.Delay(10); }
                    await AwaitPresentationAsync(layout);
                }
                await ModernScroll(160); Require(Math.Abs(RailTop(modern) - 32) < .001, "Remounted DetailLayout did not retarget to its ScrollView.");
                await Scroll(inner, 80); Require(Math.Abs(RailTop(modern) - 32) < .001, "Detached ScrollViewer still affected DetailLayout.");
                await ModernScroll(400); Require(Math.Abs(RailTop(modern) - 20) < .001, "ScrollView sticky rail escaped its container end.");
                Require(ReferenceEquals(layout.Content, main) && ReferenceEquals(layout.SideRail, rail), "Scroll-owner replacement lost caller content.");
                scrollOwners.Add(new { theme = theme.ToString(), nearestViewer = true, outerScroll = true, stickyToggle = true, modernRetarget = true,
                    oldOwnerDetached = true, containerClamp = true, contentRetained = true }); canvas.Children.Remove(modern); canvas.Children.Remove(outer);
            }
            await File.WriteAllTextAsync(Path.Combine(folder, "detail-layout.json"), JsonSerializer.Serialize(new { passed = true, frames, interactions, scrollOwners }, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "detail-layout", environment = RuntimeEnvironment() }));
        }
        finally { Content = previous; AppWindow.ResizeClient(originalSize); await AwaitPresentationAsync(CatalogRoot); }
    }
}
