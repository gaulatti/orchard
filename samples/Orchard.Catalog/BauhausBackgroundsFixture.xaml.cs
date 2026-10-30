using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard.Catalog;

public sealed partial class BauhausBackgroundsFixture : UserControl
{
    private readonly WriteableBitmap source = CreateSource();
    private bool failed;
    public BauhausBackgroundsFixture()
    {
        InitializeComponent();
        foreach (var control in new[] { Nazca, Autostrada, Pompeii, Monitor, Square, Tall, Narrow }) control.ImageSource = source;
        Unavailable.ImageFailed += (_, _) => { failed = true; Outcome.Text = "The application-owned default image is unavailable; no alternate artwork was selected."; };
    }
    private static WriteableBitmap CreateSource()
    {
        var bitmap = new WriteableBitmap(12, 4);
        using (var stream = bitmap.PixelBuffer.AsStream())
            for (var y = 0; y < 4; y++) for (var x = 0; x < 12; x++)
                stream.Write((x / 3) switch { 0 => new byte[] { 0, 0, 0, 255 }, 1 => new byte[] { 110, 70, 30, 255 }, 2 => new byte[] { 55, 35, 15, 128 }, _ => new byte[] { 0, 0, 0, 0 } });
        bitmap.Invalidate(); return bitmap;
    }
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        using var input = new NativeInputSession(XamlRoot, folder, $"bauhaus-{scheme}");
        await input.AcquireAsync(Heading); await input.MoveAsync(Heading);
        await MainWindow.AwaitPresentationAsync(this);
        var controls = new[] { Nazca, Autostrada, Pompeii, Monitor, Square, Tall };
        foreach (var control in controls)
        {
            Require(ReferenceEquals(Find<Image>(control, "PART_Image").Source, source), "Bauhaus replaced the consumer image source.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
            Require(!peer.IsControlElement() && !peer.IsContentElement() && !peer.IsKeyboardFocusable(), "Decorative background entered the accessible control tree.");
            Require(!control.IsHitTestVisible && !control.IsTabStop, "Decorative background intercepted user interaction.");
            var visualRoot = Find<Grid>(control, "PART_Root");
            var output = Find<Border>(control, "PART_Output");
            Require(control.ActualTheme == (scheme == BleeckerColorScheme.Dark ? ElementTheme.Dark : ElementTheme.Light) &&
                visualRoot.Clip is RectangleGeometry clip && clip.Rect.Width == control.ActualWidth && clip.Rect.Height == control.ActualHeight &&
                ElementCompositionPreview.GetElementChildVisual(output) is { IsVisible: true },
                "Bauhaus theme, owned clip or native composition output is missing.");
        }
        Require(Find<Canvas>(Nazca, "PART_Pattern").Children.Count == 2 && Find<Canvas>(Autostrada, "PART_Pattern").Children.Count == 6 && Find<Canvas>(Pompeii, "PART_Pattern").Children.Count == 9, "Variant shape counts differ.");
        VerifyNarrowGeometry();
        var outer = (Border)Find<Canvas>(Nazca, "PART_Pattern").Children[0];
        Require(outer.ActualWidth == 144 && outer.ActualHeight == 48 && outer.CornerRadius.TopLeft + outer.BorderThickness.Left / 2 == 24 && outer.BorderThickness.Left == 8, "Wide circle outer geometry differs from CSS full rounding.");
        var squares = Find<Canvas>(Pompeii, "PART_Pattern").Children.Cast<Border>().ToArray();
        Require(Math.Abs(squares[0].ActualWidth - 224d / 3) < .01 && Math.Abs(squares[0].ActualHeight - 32d / 3) < .01 && Canvas.GetLeft(squares[0]) == 16 && Canvas.GetTop(squares[8]) == 16 + 2 * (32d / 3 + 16), "Pompeii grid padding/gaps differ.");
        var rotationField = typeof(BleeckerBauhausBackground).GetField("rotation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Missing Bauhaus animation clock for fixture diagnostics.");
        var timeline = rotationField.GetValue(Nazca) as Storyboard;
        var frames = new List<BauhausMotionFrame>(); var elapsed = new Stopwatch();
        var sampled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Sample(object? sender,object args)
        {
            Require(args is RenderingEventArgs, "Bauhaus motion sample did not receive the native frame timestamp.");
            var rendering = (RenderingEventArgs)args;
            if(!elapsed.IsRunning)elapsed.Start();
            frames.Add(new(elapsed.Elapsed.TotalMilliseconds, rendering.RenderingTime.TotalMilliseconds, timeline?.GetCurrentTime().TotalMilliseconds, ((RotateTransform)outer.RenderTransform).Angle, ((RotateTransform)((Border)Find<Canvas>(Nazca, "PART_Pattern").Children[1]).RenderTransform).Angle));
            if(frames[^1].Milliseconds-frames[0].Milliseconds>=320 && frames[^1].RenderingMilliseconds-frames[0].RenderingMilliseconds>=320)sampled.TrySetResult();
        }
        CompositionTarget.Rendering+=Sample;
        try { await sampled.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
        finally { CompositionTarget.Rendering-=Sample; }
        var enabled = new UISettings().AnimationsEnabled;
        var measurement = BauhausMotionTiming.Measure(frames, enabled);
        await File.WriteAllTextAsync(Path.Combine(folder, $"bauhaus-motion-{scheme}.json"), JsonSerializer.Serialize(new { enabled,
            expectedDelta = measurement.ExpectedDelta, expectedFrameDelta = measurement.ExpectedFrameDelta,
            outerDelta = measurement.OuterDelta, innerDelta = measurement.InnerDelta, maximumFrameError = measurement.MaximumFrameError,
            measurement.ObserverSpan, measurement.FrameSpan, measurement.Monotonic, measurement.Passed, frames }, new JsonSerializerOptions { WriteIndented = true }));
        Require(measurement.Passed, "Bauhaus counter-rotation did not respect the 20-second linear cycle or native reduced-motion preference.");
        await input.RequireForegroundAsync();
        await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"bauhaus-window-{scheme}.png"));
        using (var pixels = source.PixelBuffer.AsStream())
            for (var y = 0; y < 4; y++) { pixels.Position = y * 48; pixels.Write(new byte[] { 110, 70, 30, 255, 110, 70, 30, 255, 110, 70, 30, 255 }); }
        source.Invalidate(); await Task.Delay(80);
        Require(ReferenceEquals(Find<Image>(Nazca, "PART_Image").Source, source), "Live consumer image mutation changed source ownership.");
        using (var pixels = source.PixelBuffer.AsStream())
            for (var y = 0; y < 4; y++) { pixels.Position = y * 48; pixels.Write(new byte[] { 0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255 }); }
        source.Invalidate(); await Task.Delay(80);
        Nazca.Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,80,96,48));await Task.Delay(80);
        try { Require(ReferenceEquals(Find<Grid>(Nazca, "PART_Root").Background, Nazca.Background), "Bauhaus lost the consumer background override."); }
        finally { Nazca.Background = null; }
        Nazca.Type = BleeckerBauhausType.Autostrada;
        Require(Find<Canvas>(Nazca, "PART_Pattern").Children.Count == 6, "Live variant did not replace geometry."); Nazca.Type = BleeckerBauhausType.Nazca;
        Nazca.DefaultImageSource = source; Nazca.ImageSource = null;
        Require(ReferenceEquals(Find<Image>(Nazca, "PART_Image").Source, source), "Null custom image did not select the explicit default.");
        Nazca.DefaultImageSource = null;
        Require(Find<Image>(Nazca, "PART_Image").Source is null, "Missing image selected unrequested artwork."); Nazca.ImageSource = source;
        var empty = new BitmapImage(); Nazca.ImageSource = empty;
        Require(ReferenceEquals(Find<Image>(Nazca, "PART_Image").Source, empty), "An explicit empty image object silently selected another source."); Nazca.ImageSource = source;
        var template = Nazca.Template; Nazca.Template = null; Nazca.Template = template; Nazca.ApplyTemplate(); UpdateLayout(); await Task.Delay(80);
        Require(ReferenceEquals(Find<Image>(Nazca, "PART_Image").Source, source), "Retemplating lost the authoritative image.");
        Require(ElementCompositionPreview.GetElementChildVisual(Find<Border>(Nazca, "PART_Output")) is { IsVisible: true },
            "Bauhaus did not restore its native composition output after retemplating.");
        Require(failed, "Unavailable application image did not report its native decode/load failure.");
    }
    private void VerifyNarrowGeometry()
    {
        using var stream=CatalogReferenceResources.Open(typeof(BauhausBackgroundsFixture).Assembly, "Orchard.Catalog.Reference.BauhausNarrow.json")??throw new InvalidOperationException("Missing narrow browser geometry reference.");
        using var reference=JsonDocument.Parse(stream);
        try
        {
            foreach(var sample in reference.RootElement.EnumerateArray())
            {
                Narrow.Width=sample.GetProperty("width").GetDouble();Narrow.Height=sample.GetProperty("height").GetDouble();Narrow.Type=Enum.Parse<BleeckerBauhausType>(sample.GetProperty("type").GetString()!);UpdateLayout();
                var shapes=Find<Canvas>(Narrow,"PART_Pattern").Children.Cast<Border>().ToArray();var expected=sample.GetProperty("shapes").EnumerateArray().ToArray();
                Require(shapes.Length==expected.Length,"Narrow shape count differs.");
                for(var index=0;index<shapes.Length;index++)
                    Require(Math.Abs(shapes[index].ActualWidth-expected[index].GetProperty("width").GetDouble())<.03&&Math.Abs(shapes[index].ActualHeight-expected[index].GetProperty("height").GetDouble())<.03&&
                        Math.Abs(Canvas.GetLeft(shapes[index])-expected[index].GetProperty("x").GetDouble())<.03&&Math.Abs(Canvas.GetTop(shapes[index])-expected[index].GetProperty("y").GetDouble())<.03,
                        $"Narrow {Narrow.Type} {Narrow.Width}x{Narrow.Height} shape {index} differs from the browser's minimum border-box/grid geometry.");
            }
        }
        finally{Narrow.Width=40;Narrow.Height=32;Narrow.Type=BleeckerBauhausType.Pompeii;UpdateLayout();}
    }
    private static T Find<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        if (root is T found && found.Name == name) return found;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) { var child = FindOrNull<T>(VisualTreeHelper.GetChild(root, i), name); if (child is not null) return child; }
        throw new InvalidOperationException($"Missing {name}.");
    }
    private static T? FindOrNull<T>(DependencyObject root, string name) where T : FrameworkElement
    { if (root is T found && found.Name == name) return found; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) { var result = FindOrNull<T>(VisualTreeHelper.GetChild(root, i), name); if (result is not null) return result; } return null; }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
