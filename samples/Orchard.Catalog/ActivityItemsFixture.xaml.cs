using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class ActivityItemsFixture : UserControl
{
    public ActivityItemsFixture() => InitializeComponent();
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        using var input = new NativeInputSession(XamlRoot, folder, $"activity-{scheme}"); await input.AcquireAsync(Heading); await input.MoveAsync(Heading);
        UpdateLayout(); await MainWindow.AwaitPresentationAsync(this); var measurements = new List<object>();
        using var reference = JsonDocument.Parse(typeof(ActivityItemsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.ActivityLayout.json")
            ?? throw new InvalidOperationException("Missing pinned browser activity reference."));
        var browserRows = reference.RootElement.GetProperty("schemes").EnumerateArray().Single(item => item.GetProperty("scheme").GetString() == scheme.ToString()).GetProperty("measurements");
        Require(XamlRoot.RasterizationScale == 1 && new Windows.UI.ViewManagement.UISettings().TextScaleFactor == 1,
            "The browser activity layout comparison currently requires display/text scale 1; scaling acceptance remains open.");
        foreach (var control in new[] { Normal, Empty, Long, Rtl })
        {
            var root = Find<Border>(control, "PART_Surface"); var title = Find<TextBlock>(control, "PART_Title"); var time = Find<TextBlock>(control, "PART_Time"); var avatar = Find<BleeckerAvatar>(control, "PART_Avatar");
            Require(avatar.AvatarSize == BleeckerAvatarSize.Small && avatar.ActualWidth == 32 && avatar.ActualHeight == 32 && avatar.Fallback == control.Init, "Activity avatar did not preserve its small source size and exact consumer fallback.");
            Require(title.Text == control.Title && time.Text == control.Time && title.FontSize == 13 && title.LineHeight == 20 && time.FontSize == 11 && time.LineHeight == 16.5 &&
                title.FontFamily.Source.Contains("EncodeSans-Medium.ttf") && time.FontFamily.Source.Contains("LibreFranklin-Regular.ttf"), "Activity text/typography differs from the source.");
            Require(root.Padding == new Thickness(16) && root.BorderThickness == new Thickness(0, 0, 0, 1) && ((Grid)root.Child).ColumnSpacing == 14, "Activity spacing/border geometry differs.");
            var border = (SolidColorBrush)root.BorderBrush; var expectedAlpha = scheme == BleeckerColorScheme.Light ? .2 : .07;
            Require(Hex(border) == (scheme == BleeckerColorScheme.Light ? "#FFE6D5B8" : "#FFFFFFFF") && Math.Abs(border.Opacity - expectedAlpha) < 1e-7, "Activity border did not use the source theme color/alpha.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
            Require(peer.GetAutomationControlType() == AutomationControlType.Group && !peer.IsKeyboardFocusable() && peer.GetPattern(PatternInterface.Invoke) is null && !control.IsTabStop, "Activity fabricated a keyboard action or native button contract.");
            var expected = browserRows.EnumerateArray().Single(row => row.GetProperty("name").GetString() == control.Name);
            Require(Math.Abs(control.ActualHeight - expected.GetProperty("height").GetDouble()) < .02 &&
                Math.Abs(title.ActualHeight - expected.GetProperty("titleHeight").GetDouble()) < .02 &&
                Math.Abs(time.ActualHeight - expected.GetProperty("timeHeight").GetDouble()) < .02,
                $"Native {control.Name} paragraph/row layout differs from the frozen browser.");
            if (title.Visibility == Visibility.Visible) Require(Math.Abs(Find<Grid>(control,"PART_TitleParagraph").TransformToVisual(control).TransformPoint(new Windows.Foundation.Point()).Y - expected.GetProperty("titleTop").GetDouble()) < .02, "Activity title paragraph offset differs from the browser.");
            if (time.Visibility == Visibility.Visible) Require(Math.Abs(Find<Grid>(control,"PART_TimeParagraph").TransformToVisual(control).TransformPoint(new Windows.Foundation.Point()).Y - expected.GetProperty("timeTop").GetDouble()) < .02, "Activity time paragraph offset differs from the browser.");
            if (ReferenceEquals(control, Normal))
            {
                Require(Math.Abs(title.TransformToVisual(control).TransformPoint(new Windows.Foundation.Point()).Y + title.BaselineOffset - expected.GetProperty("titleBaseline").GetDouble()) < .001,
                    "Activity title baseline differs from the frozen browser.");
                Require(Math.Abs(time.TransformToVisual(control).TransformPoint(new Windows.Foundation.Point()).Y + time.BaselineOffset - expected.GetProperty("timeBaseline").GetDouble()) < .001,
                    "Activity time baseline differs from the frozen browser.");
            }
            measurements.Add(new { control.Name, control.ActualWidth, control.ActualHeight, titleHeight = title.ActualHeight, timeHeight = time.ActualHeight });
        }
        Require(Find<TextBlock>(Long, "PART_Title").ActualHeight > 20, "Long activity text did not wrap.");
        await File.WriteAllTextAsync(Path.Combine(folder, $"activity-measurements-{scheme}.json"), JsonSerializer.Serialize(measurements, new JsonSerializerOptions { WriteIndented = true }));
        Require(Empty.ActualHeight == 65 && Find<TextBlock>(Empty, "PART_Title").ActualHeight == 0 && Find<TextBlock>(Empty, "PART_Time").ActualHeight == 0, $"Empty activity paragraphs differ: row={Empty.ActualHeight:R}, title={Find<TextBlock>(Empty, "PART_Title").ActualHeight:R}, time={Find<TextBlock>(Empty, "PART_Time").ActualHeight:R}.");
        var names = FrameworkElementAutomationPeer.CreatePeerForElement(Normal).GetChildren().Select(peer => peer.GetName()).ToArray();
        Require(names.Contains(Normal.Title) && names.Contains(Normal.Time) && !names.Contains("Mg"), "Activity's accessible container omitted content or exposed font metrics: " + JsonSerializer.Serialize(names));
        var sourceTitle = Normal.Title; var sourceTime = Normal.Time;
        Normal.Title = ""; UpdateLayout();
        Require(Normal.ActualHeight == 65 && Find<Grid>(Normal, "PART_TimeParagraph").TransformToVisual(Normal).TransformPoint(new Windows.Foundation.Point()).Y == 22,
            "Empty title lost the source paragraph gap or time offset.");
        Normal.Title = sourceTitle; Normal.Time = ""; UpdateLayout();
        Require(Normal.ActualHeight == 65 && Find<TextBlock>(Normal, "PART_Title").ActualHeight == 20, "Empty time retained a fabricated line box.");
        Normal.Time = sourceTime; UpdateLayout();
        Require(KeyboardStart.Focus(FocusState.Programmatic), "Activity keyboard anchor refused focus."); input.Key(0x09);
        await AwaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), KeyboardEnd), "Physical Tab did not skip actionless activity rows.");
        await input.MoveAsync(Normal); var surface = Find<Border>(Normal, "PART_Surface"); var alpha = scheme == BleeckerColorScheme.Light ? .35 : .03;
        await AwaitAsync(() => Math.Abs(((SolidColorBrush)surface.Background).Opacity - alpha) < 1e-7, "Activity hover did not reach its source alpha.");
        Require(Hex((SolidColorBrush)surface.Background) == (scheme == BleeckerColorScheme.Light ? "#FFF9F6F2" : "#FFFFFFFF"), "Activity hover changed its source RGB.");
        foreach (var part in new FrameworkElement[] { Find<BleeckerAvatar>(Normal, "PART_Avatar"), Find<TextBlock>(Normal, "PART_Title"), Find<TextBlock>(Normal, "PART_Time") })
        { await input.MoveAsync(part); Require(Math.Abs(((SolidColorBrush)surface.Background).Opacity - alpha) < 1e-7, "Moving within an activity row dropped hover."); }
        await AwaitAsync(HandCursor, "Activity did not display the global Windows Hand cursor.");
        var tapped = 0; void Tap(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs args) => tapped++;
        Rows.Tapped += Tap; try { await input.ClickAsync(Normal); Require(tapped == 1, "Activity intercepted or duplicated consumer ancestor pointer activation."); } finally { Rows.Tapped -= Tap; }
        await input.RequireForegroundAsync();
        var capturedClient = await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"activity-window-{scheme}.png"));
        var capturedTitle = Find<TextBlock>(Normal, "PART_Title"); var capturedTime = Find<TextBlock>(Normal, "PART_Time");
        await File.WriteAllTextAsync(Path.Combine(folder, $"activity-capture-{scheme}.json"), JsonSerializer.Serialize(new
        {
            client = capturedClient, row = input.ScreenBounds(Normal),
            title = new { bounds = input.ScreenBounds(capturedTitle), capturedTitle.BaselineOffset, color = Hex((SolidColorBrush)capturedTitle.Foreground) },
            time = new { bounds = input.ScreenBounds(capturedTime), capturedTime.BaselineOffset, color = Hex((SolidColorBrush)capturedTime.Foreground) }
        }, new JsonSerializerOptions { WriteIndented = true }));
        await VerifyInkRowsAsync(Path.Combine(folder, $"activity-window-{scheme}.png"), capturedClient, input.ScreenBounds(Normal),
            reference.RootElement.GetProperty("schemes").EnumerateArray().Single(item => item.GetProperty("scheme").GetString() == scheme.ToString()).GetProperty("ink"), scheme, folder);
        await input.MoveAsync(Heading); await AwaitAsync(() => ((SolidColorBrush)surface.Background).Opacity == 0, "Activity hover exit did not return to transparency.");
        await VerifyMotionAsync(input, scheme, folder);
        var initialTitle = Normal.Title; var initialTime = Normal.Time; var initialInit = Normal.Init;
        Normal.Title = "Updated exact title"; Normal.Time = "opaque consumer time / 07:03"; Normal.Init = "Bob Reed";
        Require(Find<TextBlock>(Normal, "PART_Title").Text == Normal.Title && Find<TextBlock>(Normal, "PART_Time").Text == Normal.Time && Find<BleeckerAvatar>(Normal, "PART_Avatar").Fallback == Normal.Init, "Activity live consumer changes were transformed or dropped.");
        Normal.Title = initialTitle; Normal.Time = initialTime; Normal.Init = initialInit;
        var template = Normal.Template; Normal.Template = null; Normal.Template = template; Normal.ApplyTemplate(); UpdateLayout(); await MainWindow.AwaitPresentationAsync(this);
        Require(Find<TextBlock>(Normal, "PART_Time").Text == Normal.Time && ((SolidColorBrush)Find<Border>(Normal, "PART_Surface").Background).Opacity == 0, "Activity template replacement lost content or retained hover.");
        var parent = (Panel)VisualTreeHelper.GetParent(Normal); var index = parent.Children.IndexOf(Normal); parent.Children.Remove(Normal);
        await AwaitAsync(() => !Normal.IsLoaded, "Activity did not unload from its consumer host."); parent.Children.Insert(index, Normal);
        await AwaitAsync(() => Normal.IsLoaded && Normal.ActualHeight > 0, "Activity did not remount.");
        await input.MoveAsync(Normal); await AwaitAsync(() => Math.Abs(((SolidColorBrush)Find<Border>(Normal, "PART_Surface").Background).Opacity - alpha) < 1e-7, "Remounted activity lost hover observation.");
        await input.MoveAsync(Heading);
        var owner = (FrameworkElement)XamlRoot.Content; var ownerTheme = owner.RequestedTheme;
        try
        {
            owner.RequestedTheme = scheme == BleeckerColorScheme.Light ? ElementTheme.Dark : ElementTheme.Light;
            await AwaitAsync(() => Hex((SolidColorBrush)Find<Border>(Normal, "PART_Surface").BorderBrush) == (scheme == BleeckerColorScheme.Light ? "#FFFFFFFF" : "#FFE6D5B8"), "Live activity theme change retained the previous border.");
            await input.MoveAsync(Normal); var opposite = scheme == BleeckerColorScheme.Light ? .03 : .35;
            await AwaitAsync(() => Math.Abs(((SolidColorBrush)Find<Border>(Normal, "PART_Surface").Background).Opacity - opposite) < 1e-7, "Live activity theme change retained the previous hover alpha.");
            await input.MoveAsync(Heading);
        }
        finally { owner.RequestedTheme = ownerTheme; }
        await AwaitAsync(() => Normal.ActualTheme == (scheme == BleeckerColorScheme.Light ? ElementTheme.Light : ElementTheme.Dark), "Activity did not restore its owner theme.");
        await RecordTypographyStrategiesAsync(scheme, folder);
        await File.WriteAllTextAsync(Path.Combine(folder, $"activity-measurements-{scheme}.json"), JsonSerializer.Serialize(measurements, new JsonSerializerOptions { WriteIndented = true }));
    }
    private static async Task VerifyInkRowsAsync(string path, Windows.Foundation.Rect client, Windows.Foundation.Rect row, JsonElement references, BleeckerColorScheme scheme, string folder)
    {
        using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
        using (var writer = new Windows.Storage.Streams.DataWriter(stream)) { writer.WriteBytes(await File.ReadAllBytesAsync(path)); await writer.StoreAsync(); writer.DetachStream(); }
        stream.Seek(0);
        var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
        var pixels = (await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Straight,
            new Windows.Graphics.Imaging.BitmapTransform(), Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation, Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
        var left = checked((int)(row.X - client.X)); var top = checked((int)(row.Y - client.Y));
        var samples = new List<object>(); var passed = true;
        foreach (var expected in references.EnumerateArray())
        {
            int? first = null, last = null;
            for (var y = expected.GetProperty("top").GetInt32(); y < expected.GetProperty("bottom").GetInt32(); y++)
                for (var x = expected.GetProperty("left").GetInt32(); x < expected.GetProperty("right").GetInt32(); x++)
                {
                    var offset = checked(((top + y) * (int)decoder.PixelWidth + left + x) * 4);
                    var average = (pixels[offset] + pixels[offset + 1] + pixels[offset + 2]) / 3d;
                    var threshold = expected.GetProperty("threshold").GetDouble();
                    if (expected.GetProperty("lightInk").GetBoolean() ? average > threshold : average < threshold) { first = first is null ? y : Math.Min(first.Value, y); last = last is null ? y : Math.Max(last.Value, y); }
                }
            var matches = first == expected.GetProperty("first").GetInt32() && last == expected.GetProperty("last").GetInt32(); passed &= matches;
            samples.Add(new { name = expected.GetProperty("name").GetString(), first, last, matches });
        }
        await File.WriteAllTextAsync(Path.Combine(folder, $"activity-ink-{scheme}.json"), JsonSerializer.Serialize(new { passed, samples }, new JsonSerializerOptions { WriteIndented = true }));
        Require(passed, "Composed Activity Item glyph rows differ from the frozen browser.");
    }
    private async Task RecordTypographyStrategiesAsync(BleeckerColorScheme scheme, string folder)
    {
        var host = new StackPanel { Width = 322, Opacity = 0, IsHitTestVisible = false };
        var probes = new List<(string Kind, TextBlock Text)>();
        foreach (var (kind, original) in new[] { ("title", Find<TextBlock>(Normal, "PART_Title")), ("time", Find<TextBlock>(Normal, "PART_Time")) })
            foreach (var strategy in Enum.GetValues<LineStackingStrategy>())
                foreach (var bounds in Enum.GetValues<TextLineBounds>())
                {
                    var probe = new TextBlock { Text = original.Text, FontFamily = original.FontFamily, FontWeight = original.FontWeight,
                        FontSize = original.FontSize, LineHeight = original.LineHeight, LineStackingStrategy = strategy, TextLineBounds = bounds,
                        TextWrapping = TextWrapping.Wrap, UseLayoutRounding = false };
                    probes.Add((kind, probe)); host.Children.Add(probe);
                }
        foreach (var (kind, original) in new[] { ("title-natural", Find<TextBlock>(Normal, "PART_Title")), ("time-natural", Find<TextBlock>(Normal, "PART_Time")) })
        {
            var probe = new TextBlock { Text = original.Text, FontFamily = original.FontFamily, FontWeight = original.FontWeight,
                FontSize = original.FontSize, TextWrapping = TextWrapping.NoWrap, UseLayoutRounding = false };
            probes.Add((kind, probe)); host.Children.Add(probe);
        }
        var parent = (Panel)Content; parent.Children.Add(host);
        try
        {
            UpdateLayout(); await MainWindow.AwaitPresentationAsync(this);
            await File.WriteAllTextAsync(Path.Combine(folder, $"activity-typography-strategies-{scheme}.json"), JsonSerializer.Serialize(
                probes.Select(probe => new { probe.Kind, strategy = probe.Text.LineStackingStrategy.ToString(), bounds = probe.Text.TextLineBounds.ToString(),
                    probe.Text.ActualHeight, probe.Text.BaselineOffset }), new JsonSerializerOptions { WriteIndented = true }));
        }
        finally { parent.Children.Remove(host); }
    }
    private async Task VerifyMotionAsync(NativeInputSession input, BleeckerColorScheme scheme, string folder)
    {
        var surface = Find<Border>(Normal, "PART_Surface"); var endpoint = scheme == BleeckerColorScheme.Light ? .35 : .03;
        var frames = new List<(string Phase, double Milliseconds, double Alpha)>(); var clock = Stopwatch.StartNew(); var phase = "rest"; var start = 0d;
        void Enter(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args) { if (phase == "enter") return; phase = "enter"; start = clock.Elapsed.TotalMilliseconds; }
        void Exit(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args)
        { var point = args.GetCurrentPoint(Normal).Position; if (new Windows.Foundation.Rect(0, 0, Normal.ActualWidth, Normal.ActualHeight).Contains(point)) return; phase = "exit"; start = clock.Elapsed.TotalMilliseconds; }
        void Frame(object? sender, object args) { if (phase != "rest") frames.Add((phase, clock.Elapsed.TotalMilliseconds - start, ((SolidColorBrush)surface.Background).Opacity)); }
        Normal.PointerEntered += Enter; Normal.PointerExited += Exit; CompositionTarget.Rendering += Frame;
        var enabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled; var passed = false;
        try
        {
            await input.MoveAsync(Normal); await AwaitAsync(() => Math.Abs(((SolidColorBrush)surface.Background).Opacity - endpoint) < 1e-7, "Activity enter motion did not settle.");
            await input.MoveAsync(Heading); await AwaitAsync(() => ((SolidColorBrush)surface.Background).Opacity == 0, "Activity exit motion did not settle.");
            Require(frames.All(frame => frame.Alpha >= 0 && frame.Alpha <= endpoint + 1e-7), "Activity motion exceeded its source alpha range.");
            if (enabled) Require(frames.Any(frame => frame.Phase == "enter" && frame.Alpha > 1e-6 && frame.Alpha < endpoint - 1e-6) && frames.Any(frame => frame.Phase == "exit" && frame.Alpha > 1e-6 && frame.Alpha < endpoint - 1e-6), "Enabled activity motion did not present intermediate enter and exit frames.");
            else Require(frames.All(frame => Math.Abs(frame.Alpha - (frame.Phase == "enter" ? endpoint : 0)) < 1e-7), "Reduced-motion activity retained intermediate alpha.");
            passed = true;
        }
        finally
        { Normal.PointerEntered -= Enter; Normal.PointerExited -= Exit; CompositionTarget.Rendering -= Frame; await File.WriteAllTextAsync(Path.Combine(folder, $"activity-motion-{scheme}.json"), JsonSerializer.Serialize(new { passed, enabled, frames = frames.Select(frame => new { frame.Phase, frame.Milliseconds, frame.Alpha }) })); }
    }
    private static bool HandCursor()
    {
        using var cursor = JsonDocument.Parse(JsonSerializer.Serialize(NativeInputSession.DescribeCursor())); var root = cursor.RootElement;
        return (root.GetProperty("Flags").GetUInt32() & 1) != 0 && root.GetProperty("global").GetInt64() == root.GetProperty("systemShapes").EnumerateArray().Single(shape => shape.GetProperty("Name").GetString() == "Hand").GetProperty("handle").GetInt64();
    }
    private async Task AwaitAsync(Func<bool> condition, string message) { var clock = Stopwatch.StartNew(); var readiness = new NativeReadiness(DispatcherQueue); while (!condition() && clock.ElapsedMilliseconds < 3000) { UpdateLayout(); if (condition()) break; await readiness.WaitAsync(); } Require(condition(), message); }
    private static string Hex(SolidColorBrush brush) => $"#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";
    private static T Find<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        if (parent is T found && found.Name == name) return found;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++) { var foundChild = TryFind<T>(VisualTreeHelper.GetChild(parent, index), name); if (foundChild is not null) return foundChild; }
        throw new InvalidOperationException($"Missing activity part {name}.");
    }
    private static T? TryFind<T>(DependencyObject parent, string name) where T : FrameworkElement
    { if (parent is T found && found.Name == name) return found; for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++) { var foundChild = TryFind<T>(VisualTreeHelper.GetChild(parent, index), name); if (foundChild is not null) return foundChild; } return null; }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
