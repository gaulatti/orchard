using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;
using Windows.UI.ViewManagement;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Orchard.Catalog;

public sealed partial class ContrastFixture : UserControl
{
    public ContrastFixture()
    {
        InitializeComponent();
        var image = new WriteableBitmap(4, 1);
        using (var pixels = image.PixelBuffer.AsStream()) pixels.Write(new byte[] { 0,0,0,255, 110,70,30,255, 55,35,15,128, 0,0,0,0 });
        image.Invalidate(); Brand.LogoSource = image;
        Bauhaus.ImageSource=image;
    }
    internal async Task VerifyKeyboardEntryAsync(string folder, BleeckerColorScheme scheme)
    {
        using var input = new NativeInputSession(XamlRoot, folder, $"contrast-entry-{scheme}");
        await input.AcquireAsync(Heading);
        await input.MoveAsync(Heading);
        var tooltip = await OpenHelpAsync(input);
        try
        {
            await MainWindow.CaptureElementAsync(tooltip, System.IO.Path.Combine(folder, $"contrast-entry-popup-{scheme}.png"));
            input.Key(0x1B);
            await AwaitAsync(() => !tooltip.IsOpen, "Physical Escape did not dismiss contextual help.");
            Require(ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Help) &&
                Help.FocusState == FocusState.Keyboard, "Dismissing contextual help lost keyboard focus.");
        }
        finally { tooltip.IsOpen = false; }
    }

    private async Task<BleeckerTooltip> OpenHelpAsync(NativeInputSession input)
    {
        Primary.ApplyTemplate(); Editor.ApplyTemplate(); Path.ApplyTemplate();
        var tooltip = AutomationProperties.GetDescribedBy(Help).OfType<BleeckerTooltip>().Single();
        var expansion = Descendants<Button>(Path).Single();
        Require(expansion.Focus(FocusState.Programmatic), "Breadcrumb expansion refused the keyboard entry anchor.");
        input.Key(0x09); // Actual Tab establishes keyboard modality before the OS transition.
        await AwaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Help) &&
            Help.FocusState == FocusState.Keyboard, "Physical Tab did not enter contextual help with keyboard focus.");
        await AwaitAsync(() => tooltip.IsOpen && tooltip.IsLoaded, "Contextual help did not open before the baseline capture.");
        return tooltip;
    }

    internal async Task VerifyAsync(Grid root, string folder)
    {
        using var contrast = new NativeContrastSession(folder);
        using var input = new NativeInputSession(XamlRoot, folder, "contrast");
        await input.AcquireAsync(Heading);
        await input.MoveAsync(Heading);
        var tooltip = await OpenHelpAsync(input);
        Button Action() => Descendants<Button>(Primary).Single();
        ContentPresenter Presenter() => Descendants<ContentPresenter>(Action()).First();
        var accessibility = new AccessibilitySettings();
        var expansion = Descendants<Button>(Path).Single();
        var originalBackground = Hex(root.Background);
        var originalHeading = Hex(Heading.Foreground);
        var attentionRoot=(FrameworkElement)VisualTreeHelper.GetChild(Attention,0);
        var attentionBackground=(Border)attentionRoot.FindName("PART_Background");
        var attentionSurface=(Border)attentionRoot.FindName("PART_Surface");
        var attentionText=(TextBlock)attentionRoot.FindName("PART_String");
        var attentionShadow=(Grid)attentionRoot.FindName("PART_ShadowHost");
        var attentionImage=attentionSurface.Background;
        var attentionBorder=Hex(attentionSurface.BorderBrush);var attentionForeground=Hex(attentionText.Foreground);
        var before = Snapshot();
        var samples = new List<object> { new { phase = "before", state = before } };
        var focusEvents = new List<object>();
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        var externalDeactivation = false;
        void RecordFocus(string kind) => focusEvents.Add(new { kind, elapsedMilliseconds = elapsed.Elapsed.TotalMilliseconds,
            Help.FocusState, tooltip.IsOpen, focused = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot)?.GetType().Name,
            foreground = input.OwnsForeground, foreignForeground = input.HasForeignForeground,
            os = NativeContrastSession.Read().Enabled, cursor = NativeInputSession.DescribeCursor(), native = input.DescribeCurrentPoint() });
        void GotFocus(object sender, RoutedEventArgs args) => RecordFocus("got");
        void LostFocus(object sender, RoutedEventArgs args)
        {
            if (input.HasForeignForeground && NativeContrastSession.Read().Enabled &&
                ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Help)) externalDeactivation = true;
            RecordFocus("lost");
        }
        void Closed(object? sender, EventArgs args) => RecordFocus("tooltip-closed");
        void Pressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args) => RecordFocus("pointer-pressed");
        Help.GotFocus += GotFocus; Help.LostFocus += LostFocus;
        tooltip.Closed += Closed;
        Help.AddHandler(PointerPressedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(Pressed), true);
        var headingInk = await ReadInkAsync(Heading,folder,"heading");
        var labelInk = await ReadInkAsync(Descendants<TextBlock>(Action()).First(item => item.Text == "Contrast action"),folder,"action-label");
        var attentionInk = await ReadInkAsync(attentionText,folder,"attention-label");
        var passed = false;
        try
        {
            await MainWindow.CaptureElementAsync(this, System.IO.Path.Combine(folder, "contrast-before.png"));
            contrast.Enable();
            await AwaitAsync(() => accessibility.HighContrast && NativeContrastSession.Read().Enabled, "Windows did not enter actual contrast mode.");
            await AwaitAsync(() => Matches(root.Background, NativeContrastSession.Color(5)) && Matches(Heading.Foreground, NativeContrastSession.Color(8)) &&
                Matches(Presenter().Background, NativeContrastSession.Color(13)) && Matches(Presenter().Foreground, NativeContrastSession.Color(14)) &&
                Matches(Editor.Background, NativeContrastSession.Color(5)) && Matches(Editor.Foreground, NativeContrastSession.Color(8)) &&
                Matches(tooltip.Background, NativeContrastSession.Color(5)) && Matches(tooltip.Foreground, NativeContrastSession.Color(8)),
                "Live controls did not adopt Windows semantic colors.");
            if (externalDeactivation)
            {
                Require(!tooltip.IsOpen, "Contextual help did not dismiss on native window deactivation.");
                samples.Add(new { phase = "native-deactivation", state = Snapshot() });
                await AwaitRecoveryTargetAsync(input, folder);
                await input.RequireForegroundAsync();
                Require(ReferenceEquals(tooltip, await OpenHelpAsync(input)), "Keyboard re-entry replaced the contextual-help control.");
                await AwaitAsync(() => Matches(tooltip.Background, NativeContrastSession.Color(5)) &&
                    Matches(tooltip.Foreground, NativeContrastSession.Color(8)), "Re-entered contextual help did not retain Windows colors.");
            }
            Require(input.OwnsForeground && tooltip.IsOpen && Help.FocusState == FocusState.Keyboard,
                "Contrast switching closed contextual help or lost its focus while the catalog owned foreground.");
            var action = Action();
            await VerifyPaintAsync(action, folder);
            await VerifyInkAsync(Heading, 8, headingInk, folder, "heading");
            await VerifyInkAsync(Descendants<TextBlock>(action).First(item => item.Text == "Contrast action"), 14, labelInk, folder, "action-label");
            await VerifyBrandPaintAsync(input, folder);
            var activitySurface = (Border)VisualTreeHelper.GetChild(Activity, 0);
            Require(activitySurface.Name == "PART_Surface", "Activity contrast verification requires its own template surface.");
            var activityTitle = Descendants<TextBlock>(Activity).Single(item => item.Name == "PART_Title");
            var activityTime = Descendants<TextBlock>(Activity).Single(item => item.Name == "PART_Time");
            await input.MoveAsync(Activity);
            await AwaitAsync(() => Matches(activitySurface.Background, NativeContrastSession.Color(13)) &&
                Matches(activityTitle.Foreground, NativeContrastSession.Color(14)) && Matches(activityTime.Foreground, NativeContrastSession.Color(14)),
                "Hovered activity did not pair Windows Highlight and HighlightText.");
            await input.MoveAsync(Heading);
            await AwaitAsync(() => BrushOpacity(activitySurface.Background) == 0 &&
                Matches(activityTitle.Foreground, NativeContrastSession.Color(8)) && Matches(activityTime.Foreground, NativeContrastSession.Color(8)),
                "Activity hover exit did not restore Windows text and transparency.");
            await input.MoveAsync(Attention);
            await AwaitAsync(()=>Matches(attentionBackground.Background,NativeContrastSession.Color(5)) &&
                Matches(attentionSurface.BorderBrush,NativeContrastSession.Color(8)) && Matches(attentionText.Foreground,NativeContrastSession.Color(8)) &&
                attentionSurface.Background is null && ((TranslateTransform)attentionRoot.RenderTransform).Y==0 &&
                Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementChildVisual(attentionShadow) is null,
                "Attention did not adopt Windows semantic paint or suppress gradient, lift and shadow in actual contrast mode.");
            await VerifyInkAsync(attentionText,8,attentionInk,folder,"attention-label");
            await MainWindow.CaptureElementAsync(Attention,System.IO.Path.Combine(folder,"contrast-attention-active.png"));
            await input.MoveAsync(expansion);
            var chrome = Descendants<Border>(expansion).Single(item => item.Name == "PART_Chrome");
            var icon = Descendants<Microsoft.UI.Xaml.Shapes.Path>(expansion).Single(item => item.Name == "PART_Icon");
            await AwaitAsync(() => Matches(chrome.Background, NativeContrastSession.Color(13)) && Matches(icon.Stroke, NativeContrastSession.Color(14)),
                "Hovered breadcrumb did not pair Windows Highlight and HighlightText.");
            samples.Add(new { phase = "contrast", state = Snapshot() });
            await MainWindow.CaptureElementAsync(this, System.IO.Path.Combine(folder, "contrast-active.png"));
            await MainWindow.CaptureElementAsync(tooltip, System.IO.Path.Combine(folder, "contrast-popup-active.png"));
            await NativeScreenCapture.CaptureClientAsync(input.Window, System.IO.Path.Combine(folder, "contrast-window-active.png"));
            passed = true;
        }
        catch (Exception error)
        {
            samples.Add(new { phase = "failure", error = error.Message, state = Snapshot() });
            await MainWindow.CaptureElementAsync(this, System.IO.Path.Combine(folder, "contrast-failure.png"));
            throw;
        }
        finally
        {
            contrast.Restore();
            await AwaitAsync(() => !accessibility.HighContrast && !NativeContrastSession.Read().Enabled &&
                Hex(root.Background) == originalBackground && Hex(Heading.Foreground) == originalHeading,
                "Windows mode or the original live palette did not restore.");
            await AwaitAsync(()=>ReferenceEquals(attentionSurface.Background,attentionImage) && Hex(attentionSurface.BorderBrush)==attentionBorder &&
                Hex(attentionText.Foreground)==attentionForeground,"Attention did not restore its exact producer image, border and foreground after contrast mode.");
            tooltip.IsOpen = false;
            Help.GotFocus -= GotFocus; Help.LostFocus -= LostFocus;
            tooltip.Closed -= Closed;
            Help.RemoveHandler(PointerPressedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(Pressed));
            samples.Add(new { phase = "restored", state = Snapshot() });
            await MainWindow.CaptureElementAsync(this, System.IO.Path.Combine(folder, "contrast-restored.png"));
            await File.WriteAllTextAsync(System.IO.Path.Combine(folder, "contrast-measurements.json"),
                JsonSerializer.Serialize(new { passed, original = contrast.Original, samples, focusEvents }, new JsonSerializerOptions { WriteIndented = true }));
        }

        object Snapshot() => new { os = NativeContrastSession.Read(), accessibility.HighContrast, rootBackground = Hex(root.Background),
            attention=new{background=Hex(attentionBackground.Background),border=Hex(attentionSurface.BorderBrush),foreground=Hex(attentionText.Foreground),
                image=attentionSurface.Background?.GetType().Name,lift=((TranslateTransform)attentionRoot.RenderTransform).Y,
                shadow=Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementChildVisual(attentionShadow)?.GetType().Name},
            opacity = new { root = BrushOpacity(root.Background), heading = BrushOpacity(Heading.Foreground), primaryBackground = BrushOpacity(Presenter().Background),
                primaryForeground = BrushOpacity(Presenter().Foreground), inputBackground = BrushOpacity(Editor.Background), inputForeground = BrushOpacity(Editor.Foreground),
                tooltipBackground = BrushOpacity(tooltip.Background), tooltipForeground = BrushOpacity(tooltip.Foreground) },
            systemWindow = Hex(new SolidColorBrush(NativeContrastSession.Color(5))), systemText = Hex(new SolidColorBrush(NativeContrastSession.Color(8))),
            systemHighlight = Hex(new SolidColorBrush(NativeContrastSession.Color(13))), systemHighlightText = Hex(new SolidColorBrush(NativeContrastSession.Color(14))),
            heading = Hex(Heading.Foreground), primaryBackground = Hex(Presenter().Background), primaryForeground = Hex(Presenter().Foreground),
            inputBackground = Hex(Editor.Background), inputForeground = Hex(Editor.Foreground),
            tooltipBackground = Hex(tooltip.Background), tooltipForeground = Hex(tooltip.Foreground), tooltip.IsOpen, Help.FocusState,
            focused = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot)?.GetType().Name,
            foreground = input.OwnsForeground, foreignForeground = input.HasForeignForeground,
            cursor = NativeInputSession.DescribeCursor(), helpBounds = input.ScreenBounds(Help), headingBounds = input.ScreenBounds(Heading) };
    }
    private sealed record InkSample(int Width, int Height, int[] Pixels, object Geometry);
    private static async Task<InkSample> ReadInkAsync(TextBlock text,string folder,string name)
    {
        var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(text);
        var buffer = await bitmap.GetPixelsAsync();
        var pixels = new byte[checked((int)buffer.Length)];
        using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
        var color = ((SolidColorBrush)text.Foreground).Color;
        var ink = Enumerable.Range(0, bitmap.PixelWidth * bitmap.PixelHeight).Where(index =>
            pixels[index * 4] == color.B && pixels[index * 4 + 1] == color.G && pixels[index * 4 + 2] == color.R && pixels[index * 4 + 3] == 255).ToArray();
        Require(ink.Length > 0, "Initial label has no solid glyph pixels to establish its ink mask.");
        await SaveInkRasterAsync(bitmap,pixels,System.IO.Path.Combine(folder,$"contrast-ink-{name}-before.png"));
        return new InkSample(bitmap.PixelWidth, bitmap.PixelHeight, ink,InkGeometry(text));
    }
    private static async Task VerifyInkAsync(TextBlock text, int systemColor, InkSample sample, string folder, string name)
    {
        var observations = new List<object>();
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        var matching = 0;
        byte[]? lastPixels=null; RenderTargetBitmap? lastBitmap=null;
        do
        {
            var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(text);
            Require(bitmap.PixelWidth == sample.Width && bitmap.PixelHeight == sample.Height, "Contrast transition changed label raster dimensions.");
            var buffer = await bitmap.GetPixelsAsync();
            var pixels = new byte[checked((int)buffer.Length)];
            using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
            lastPixels=pixels;lastBitmap=bitmap;
            var expected = NativeContrastSession.Color(systemColor);
            matching = sample.Pixels.Count(index => pixels[index * 4] == expected.B && pixels[index * 4 + 1] == expected.G &&
                pixels[index * 4 + 2] == expected.R && pixels[index * 4 + 3] == 255);
            observations.Add(new { elapsedMilliseconds = elapsed.Elapsed.TotalMilliseconds, expected = ColorHex(expected), matching, total = sample.Pixels.Length });
            if (matching == sample.Pixels.Length) break;
            await Task.Delay(32);
        } while (elapsed.Elapsed < TimeSpan.FromSeconds(8));
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"contrast-ink-{name}.json"), JsonSerializer.Serialize(observations));
        var finalExpected=NativeContrastSession.Color(systemColor);
        var unmatched=sample.Pixels.Where(index=>lastPixels![index*4]!=finalExpected.B || lastPixels[index*4+1]!=finalExpected.G ||
            lastPixels[index*4+2]!=finalExpected.R || lastPixels[index*4+3]!=255).Select(index=>new{x=index%sample.Width,y=index/sample.Width,
                actual=new[]{(int)lastPixels![index*4+2],(int)lastPixels[index*4+1],(int)lastPixels[index*4],(int)lastPixels[index*4+3]}}).ToArray();
        await SaveInkRasterAsync(lastBitmap!,lastPixels!,System.IO.Path.Combine(folder,$"contrast-ink-{name}-after.png"));
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder,$"contrast-ink-{name}-details.json"),JsonSerializer.Serialize(new{
            before=sample.Geometry,after=InkGeometry(text),width=sample.Width,height=sample.Height,expected=ColorHex(finalExpected),sample.Pixels,unmatched}));
        Require(matching == sample.Pixels.Length, $"Only {matching}/{sample.Pixels.Length} established glyph pixels adopted the Windows foreground.");
    }
    private static object InkGeometry(TextBlock text)
    {
        var origin=text.TransformToVisual(text.XamlRoot.Content).TransformPoint(new Windows.Foundation.Point());
        return new{origin.X,origin.Y,text.ActualWidth,text.ActualHeight,text.LineHeight,text.FontSize,fontWeight=text.FontWeight.Weight,
            text.ActualTheme,rasterScale=text.XamlRoot.RasterizationScale,textScale=new UISettings().TextScaleFactor,
            transformY=(text.RenderTransform as TranslateTransform)?.Y,foreground=Hex(text.Foreground)};
    }
    private static async Task SaveInkRasterAsync(RenderTargetBitmap bitmap,byte[] pixels,string path)
    {
        using var stream=File.Open(path,FileMode.Create,FileAccess.ReadWrite);
        using var random=stream.AsRandomAccessStream();
        var encoder=await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId,random);
        encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied,
            (uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);
        await encoder.FlushAsync();
    }
    private static async Task VerifyPaintAsync(Button action, string folder)
    {
        var observations = new List<object>();
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        var matching = false;
        do
        {
            var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(action);
            var buffer = await bitmap.GetPixelsAsync();
            var pixels = new byte[checked((int)buffer.Length)];
            using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
            var scale = bitmap.PixelWidth / action.ActualWidth;
            var x = bitmap.PixelWidth / 2; var y = (int)(3 * scale);
            var offset = (y * bitmap.PixelWidth + x) * 4;
            var actual = Color.FromArgb(pixels[offset + 3], pixels[offset + 2], pixels[offset + 1], pixels[offset]);
            var expected = NativeContrastSession.Color(13);
            matching = actual == expected;
            observations.Add(new { elapsedMilliseconds = elapsed.Elapsed.TotalMilliseconds, x, y, expected = ColorHex(expected), actual = ColorHex(actual), matching });
            if (matching) break;
            await Task.Delay(32);
        } while (elapsed.Elapsed < TimeSpan.FromSeconds(8));
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, "contrast-painted-background.json"), JsonSerializer.Serialize(observations));
        Require(matching, "Contrast action's painted background differs from Windows Highlight.");
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject owner) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(owner); index++)
        {
            var child = VisualTreeHelper.GetChild(owner, index);
            if (child is T value) yield return value;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private async Task AwaitAsync(Func<bool> condition, string message)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && elapsed.Elapsed < TimeSpan.FromSeconds(8)) { UpdateLayout(); await Task.Delay(32); }
        Require(condition(), message);
    }
    private async Task VerifyBrandPaintAsync(NativeInputSession input, string folder)
    {
        var image = Descendants<Image>(Brand).Single();
        var visibleHost = Descendants<Grid>(Brand).Single(item => item.Name == "PART_Logo");
        var origin = visibleHost.TransformToVisual(XamlRoot.Content).TransformPoint(new Windows.Foundation.Point());
        var bauhausOrigin=Bauhaus.TransformToVisual(XamlRoot.Content).TransformPoint(new Windows.Foundation.Point());
        var observations = new List<object>();
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        var matched = false;
        do
        {
            var path = System.IO.Path.Combine(folder, "contrast-brand-window.png");
            await NativeScreenCapture.CaptureClientAsync(input.Window, path);
            using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            await stream.WriteAsync((await File.ReadAllBytesAsync(path)).AsBuffer()); stream.Seek(0);
            var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
            var pixels = (await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
                Windows.Graphics.Imaging.BitmapAlphaMode.Straight, new Windows.Graphics.Imaging.BitmapTransform(),
                Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation, Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
            matched = true;
            foreach (var (fraction, system) in new[] { (.125, 8), (.875, 5) })
            {
                var x = (int)Math.Round((origin.X + image.ActualWidth * fraction) * XamlRoot.RasterizationScale);
                var y = (int)Math.Round((origin.Y + image.ActualHeight / 2) * XamlRoot.RasterizationScale);
                Require(x >= 0 && y >= 0 && x < decoder.PixelWidth && y < decoder.PixelHeight, "Contrast brand pixel exceeds the native capture.");
                var offset = (y * (int)decoder.PixelWidth + x) * 4;
                var actual = Color.FromArgb(pixels[offset + 3], pixels[offset + 2], pixels[offset + 1], pixels[offset]);
                var expected = NativeContrastSession.Color(system);
                matched &= actual == expected;
                observations.Add(new { milliseconds = elapsed.Elapsed.TotalMilliseconds, x, y, system, actual = ColorHex(actual), expected = ColorHex(expected), matched = actual == expected });
            }
            foreach(var fraction in new[]{.125,.875})
            {
                var x=(int)Math.Round((bauhausOrigin.X+Bauhaus.ActualWidth*fraction)*XamlRoot.RasterizationScale);
                var y=(int)Math.Round((bauhausOrigin.Y+Bauhaus.ActualHeight/2)*XamlRoot.RasterizationScale);
                Require(x>=0&&y>=0&&x<decoder.PixelWidth&&y<decoder.PixelHeight,"Contrast background pixel exceeds the native capture.");
                var offset=(y*(int)decoder.PixelWidth+x)*4;var actual=Color.FromArgb(pixels[offset+3],pixels[offset+2],pixels[offset+1],pixels[offset]);var expected=NativeContrastSession.Color(5);
                matched&=actual==expected;
                observations.Add(new{component="bauhaus",milliseconds=elapsed.Elapsed.TotalMilliseconds,x,y,actual=ColorHex(actual),expected=ColorHex(expected),matched=actual==expected});
            }
            if (matched) break;
            await Task.Delay(32);
        } while (elapsed.Elapsed < TimeSpan.FromSeconds(8));
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, "contrast-brand-pixels.json"), JsonSerializer.Serialize(observations));
        Require(matched, "Contrast decorative imagery did not preserve brand alpha/WindowText and a flat Window background for Bauhaus.");
    }
    private async Task AwaitRecoveryTargetAsync(NativeInputSession input, string folder)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        TimeSpan? readySince = null;
        var observations = new List<object>();
        (bool Point, bool Foreground, bool Foreign)? previous = null;
        var ready = false;
        do
        {
            var state = (Point: input.OwnsCurrentPoint, Foreground: input.OwnsForeground, Foreign: input.HasForeignForeground);
            if (state != previous)
            {
                observations.Add(new { milliseconds = elapsed.Elapsed.TotalMilliseconds, state.Point, state.Foreground, state.Foreign,
                    native = input.DescribeCurrentPoint() });
                previous = state;
            }
            // The system cover can briefly expose our point while foreground
            // is zero, or return focus between cover updates. Require an owned
            // point and a known foreground for a continuous settling interval
            // before entering the existing single guarded recovery path.
            if (state.Point && (state.Foreground || state.Foreign)) readySince ??= elapsed.Elapsed;
            else readySince = null;
            ready = readySince is TimeSpan since && elapsed.Elapsed - since >= TimeSpan.FromMilliseconds(250);
            if (ready) break;
            UpdateLayout(); await Task.Delay(32);
        } while (elapsed.Elapsed < TimeSpan.FromSeconds(8));
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, "contrast-recovery-target.json"),
            JsonSerializer.Serialize(new { ready, elapsedMilliseconds = elapsed.Elapsed.TotalMilliseconds, observations }));
        Require(ready && input.OwnsCurrentPoint && (input.OwnsForeground || input.HasForeignForeground),
            "Windows contrast transition did not settle on the catalog's owned pointer target and a known foreground.");
    }
    private static bool Matches(Brush brush, Color color) => brush is SolidColorBrush solid && solid.Color == color && solid.Opacity == 1;
    private static string ColorHex(Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    private static string? Hex(Brush? brush) => brush is SolidColorBrush solid ? ColorHex(solid.Color) : brush?.GetType().Name;
    private static double? BrushOpacity(Brush? brush) => brush?.Opacity;
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
