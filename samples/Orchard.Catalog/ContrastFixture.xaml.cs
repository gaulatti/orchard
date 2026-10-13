using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Orchard.Catalog;

public sealed partial class ContrastFixture : UserControl
{
    public ContrastFixture() => InitializeComponent();
    internal async Task VerifyAsync(Grid root, string folder)
    {
        using var contrast = new NativeContrastSession();
        using var input = new NativeInputSession(XamlRoot, folder, "contrast");
        await input.AcquireAsync(Heading);
        await input.MoveAsync(Heading);
        Primary.ApplyTemplate(); Editor.ApplyTemplate(); Path.ApplyTemplate();
        Button Action() => Descendants<Button>(Primary).Single();
        ContentPresenter Presenter() => Descendants<ContentPresenter>(Action()).First();
        var tooltip = AutomationProperties.GetDescribedBy(Help).OfType<BleeckerTooltip>().Single();
        var accessibility = new AccessibilitySettings();
        var before = Snapshot();
        var originalBackground = Hex(root.Background);
        var originalHeading = Hex(Heading.Foreground);
        var samples = new List<object> { new { phase = "before", state = before } };
        var headingInk = await ReadInkAsync(Heading);
        var labelInk = await ReadInkAsync(Descendants<TextBlock>(Action()).First(item => item.Text == "Contrast action"));
        var passed = false;
        try
        {
            Require(Help.Focus(FocusState.Keyboard), "Contextual help refused keyboard focus.");
            await AwaitAsync(() => tooltip.IsOpen && tooltip.IsLoaded, "Contextual help did not open before the mode transition.");
            await MainWindow.CaptureElementAsync(this, System.IO.Path.Combine(folder, "contrast-before.png"));
            contrast.Enable();
            await AwaitAsync(() => accessibility.HighContrast && NativeContrastSession.Read().Enabled, "Windows did not enter actual contrast mode.");
            var window = NativeContrastSession.Color(5); var foreground = NativeContrastSession.Color(8);
            var highlight = NativeContrastSession.Color(13); var selectedText = NativeContrastSession.Color(14);
            await AwaitAsync(() => Matches(root.Background, window) && Matches(Heading.Foreground, foreground) &&
                Matches(Presenter().Background, highlight) && Matches(Presenter().Foreground, selectedText) &&
                Matches(Editor.Background, window) && Matches(Editor.Foreground, foreground) &&
                Matches(tooltip.Background, window) && Matches(tooltip.Foreground, foreground),
                "Live controls did not adopt Windows semantic colors.");
            Require(tooltip.IsOpen && Help.FocusState == FocusState.Keyboard, "Contrast switching closed contextual help or lost its focus.");
            var action = Action();
            await VerifyPaintAsync(action, highlight);
            await VerifyInkAsync(Heading, foreground, headingInk);
            await VerifyInkAsync(Descendants<TextBlock>(action).First(item => item.Text == "Contrast action"), selectedText, labelInk);
            var expansion = Descendants<Button>(Path).Single();
            await input.MoveAsync(expansion);
            var chrome = Descendants<Border>(expansion).Single(item => item.Name == "PART_Chrome");
            var icon = Descendants<Microsoft.UI.Xaml.Shapes.Path>(expansion).Single(item => item.Name == "PART_Icon");
            await AwaitAsync(() => Matches(chrome.Background, highlight) && Matches(icon.Stroke, selectedText),
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
            tooltip.IsOpen = false;
            samples.Add(new { phase = "restored", state = Snapshot() });
            await MainWindow.CaptureElementAsync(this, System.IO.Path.Combine(folder, "contrast-restored.png"));
            await File.WriteAllTextAsync(System.IO.Path.Combine(folder, "contrast-measurements.json"),
                JsonSerializer.Serialize(new { passed, original = contrast.Original, samples }, new JsonSerializerOptions { WriteIndented = true }));
        }

        object Snapshot() => new { os = NativeContrastSession.Read(), accessibility.HighContrast, rootBackground = Hex(root.Background),
            systemWindow = Hex(new SolidColorBrush(NativeContrastSession.Color(5))), systemText = Hex(new SolidColorBrush(NativeContrastSession.Color(8))),
            systemHighlight = Hex(new SolidColorBrush(NativeContrastSession.Color(13))), systemHighlightText = Hex(new SolidColorBrush(NativeContrastSession.Color(14))),
            heading = Hex(Heading.Foreground), primaryBackground = Hex(Presenter().Background), primaryForeground = Hex(Presenter().Foreground),
            inputBackground = Hex(Editor.Background), inputForeground = Hex(Editor.Foreground),
            tooltipBackground = Hex(tooltip.Background), tooltipForeground = Hex(tooltip.Foreground), tooltip.IsOpen, Help.FocusState };
    }
    private sealed record InkSample(int Width, int Height, int[] Pixels);
    private static async Task<InkSample> ReadInkAsync(TextBlock text)
    {
        var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(text);
        var buffer = await bitmap.GetPixelsAsync();
        var pixels = new byte[checked((int)buffer.Length)];
        using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
        var color = ((SolidColorBrush)text.Foreground).Color;
        var ink = Enumerable.Range(0, bitmap.PixelWidth * bitmap.PixelHeight).Where(index =>
            pixels[index * 4] == color.B && pixels[index * 4 + 1] == color.G && pixels[index * 4 + 2] == color.R && pixels[index * 4 + 3] == 255).ToArray();
        Require(ink.Length > 0, "Initial label has no solid glyph pixels to establish its ink mask.");
        return new InkSample(bitmap.PixelWidth, bitmap.PixelHeight, ink);
    }
    private static async Task VerifyInkAsync(TextBlock text, Color expected, InkSample sample)
    {
        var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(text);
        Require(bitmap.PixelWidth == sample.Width && bitmap.PixelHeight == sample.Height, "Contrast transition changed label raster dimensions.");
        var buffer = await bitmap.GetPixelsAsync();
        var pixels = new byte[checked((int)buffer.Length)];
        using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
        var matching = sample.Pixels.Count(index => pixels[index * 4] == expected.B && pixels[index * 4 + 1] == expected.G &&
            pixels[index * 4 + 2] == expected.R && pixels[index * 4 + 3] == 255);
        Require(matching == sample.Pixels.Length, $"Only {matching}/{sample.Pixels.Length} established glyph pixels adopted the Windows foreground.");
    }
    private static async Task VerifyPaintAsync(Button action, Color background)
    {
        var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(action);
        var buffer = await bitmap.GetPixelsAsync();
        var pixels = new byte[checked((int)buffer.Length)];
        using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
        var scale = bitmap.PixelWidth / action.ActualWidth;
        var x = bitmap.PixelWidth / 2; var y = (int)(3 * scale);
        Require(Pixel(x, y, background), "Contrast action's painted background differs from Windows Highlight.");
        bool Pixel(int px, int py, Color color)
        {
            var offset = (py * bitmap.PixelWidth + px) * 4;
            return pixels[offset] == color.B && pixels[offset + 1] == color.G && pixels[offset + 2] == color.R && pixels[offset + 3] == 255;
        }
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
    private static bool Matches(Brush brush, Color color) => brush is SolidColorBrush solid && solid.Color == color && solid.Opacity == 1;
    private static string Hex(Brush brush) => brush is SolidColorBrush solid ? $"#{solid.Color.A:X2}{solid.Color.R:X2}{solid.Color.G:X2}{solid.Color.B:X2}" : brush.GetType().Name;
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
