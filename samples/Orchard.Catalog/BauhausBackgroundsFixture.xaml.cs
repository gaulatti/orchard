using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
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
        await Task.Delay(250); UpdateLayout();
        var controls = new[] { Nazca, Autostrada, Pompeii, Monitor, Square, Tall };
        foreach (var control in controls)
        {
            Require(ReferenceEquals(Find<Image>(control, "PART_Image").Source, source), "Bauhaus replaced the consumer image source.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
            Require(!peer.IsControlElement() && !peer.IsContentElement() && !peer.IsKeyboardFocusable(), "Decorative background entered the accessible control tree.");
            Require(!control.IsHitTestVisible && !control.IsTabStop, "Decorative background intercepted user interaction.");
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
        await CapturePixelsAsync(input, scheme, folder);
        using (var pixels = source.PixelBuffer.AsStream())
            for (var y = 0; y < 4; y++) { pixels.Position = y * 48; pixels.Write(new byte[] { 110, 70, 30, 255, 110, 70, 30, 255, 110, 70, 30, 255 }); }
        source.Invalidate(); await Task.Delay(80); await CapturePixelsAsync(input, scheme, folder, mutated: true);
        using (var pixels = source.PixelBuffer.AsStream())
            for (var y = 0; y < 4; y++) { pixels.Position = y * 48; pixels.Write(new byte[] { 0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255 }); }
        source.Invalidate(); await Task.Delay(80);
        Nazca.Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,80,96,48));await Task.Delay(80);
        try{await CapturePixelsAsync(input,scheme,folder,consumerBackground:true);}finally{Nazca.Background=null;}
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
        await CapturePixelsAsync(input, scheme, folder);
        Require(failed, "Unavailable application image did not report its native decode/load failure.");
    }
    private async Task CapturePixelsAsync(NativeInputSession input, BleeckerColorScheme scheme, string folder, bool mutated = false,bool consumerBackground=false)
    {
        // Motion is checked above with the actual system preference. The raster
        // reference has a defined zero-angle timeline, so align only these
        // fixture-owned storyboards while comparing layer pixels.
        var field=typeof(BleeckerBauhausBackground).GetField("rotation",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)??throw new InvalidOperationException("Missing Bauhaus fixture animation timeline.");
        var timelines=new[]{Nazca,Monitor}.Select(control=>field.GetValue(control)).OfType<Storyboard>().ToArray();
        try
        {
        foreach(var timeline in timelines){timeline.Pause();timeline.SeekAlignedToLastTick(TimeSpan.Zero);}
        await Task.Delay(80);
        foreach(var control in new[]{Nazca,Monitor})
            Require(Find<Canvas>(control,"PART_Pattern").Children.Cast<Border>().All(shape=>Math.Abs(((RotateTransform)shape.RenderTransform).Angle)<.001),"Bauhaus raster reference timeline did not align to zero.");
        await input.RequireForegroundAsync();
        var suffix = mutated ? "-mutated" : consumerBackground?"-consumer-background":""; var path = Path.Combine(folder, $"bauhaus-window-{scheme}{suffix}.png"); var client = await NativeScreenCapture.CaptureClientAsync(input.Window, path);
        using var stream = new InMemoryRandomAccessStream(); using (var writer = new DataWriter(stream)) { writer.WriteBytes(await File.ReadAllBytesAsync(path)); await writer.StoreAsync(); writer.DetachStream(); }
        stream.Seek(0);
        var decoder = await BitmapDecoder.CreateAsync(stream); var bytes = (await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, new BitmapTransform(), ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage)).DetachPixelData();
        var samples = new List<object>(); var scale = XamlRoot.RasterizationScale;
        using var referenceStream = typeof(BauhausBackgroundsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.BauhausPixels.json") ?? throw new InvalidOperationException("Missing pinned browser pixel reference.");
        using var reference = JsonDocument.Parse(referenceStream); var matching = true;
        foreach (var control in new[] { Nazca, Autostrada, Pompeii, Monitor })
        {
            var origin = control.TransformToVisual(XamlRoot.Content).TransformPoint(new Point());
            foreach (var y in new[] { 12, 84 }) foreach (var x in new[] { 36, 108, 180, 252 })
            {
                var px = (int)Math.Round((origin.X + x + .5) * scale); var py = (int)Math.Round((origin.Y + y + .5) * scale); var offset = checked((py * (int)decoder.PixelWidth + px) * 4);
                var referenceX = mutated && x == 36 ? 108 : x;
                var collection=consumerBackground&&ReferenceEquals(control,Nazca)?"consumerBackgroundSamples":"samples";
                var expected = reference.RootElement.GetProperty(collection).EnumerateArray().Single(sample => sample.GetProperty("type").GetString() == control.Type.ToString() && sample.GetProperty("x").GetInt32() == referenceX && sample.GetProperty("y").GetInt32() == y);
                var actual = new[] { (int)bytes[offset + 2], bytes[offset + 1], bytes[offset] }; var expectedColor = new[] { expected.GetProperty("red").GetInt32(), expected.GetProperty("green").GetInt32(), expected.GetProperty("blue").GetInt32() };
                var matched = actual.Zip(expectedColor).All(pair => Math.Abs(pair.First - pair.Second) <= 3); matching &= matched;
                samples.Add(new { type = control.Type.ToString(), x, y, actual, expected = expectedColor, matched, px, py });
            }
        }
        await File.WriteAllTextAsync(Path.Combine(folder, $"bauhaus-pixels-{scheme}{suffix}.json"), JsonSerializer.Serialize(new { scale, client, samples }, new JsonSerializerOptions { WriteIndented = true }));
        Require(matching, "Bauhaus composition differs from the pinned browser blend pixels; see the retained capture and pixel report.");
        if(!mutated&&!consumerBackground)await VerifyRectangularRastersAsync(bytes,decoder.PixelWidth,scheme,folder);
        }
        finally{foreach(var timeline in timelines)timeline.Resume();}
    }
    private async Task VerifyRectangularRastersAsync(byte[] native,uint nativeWidth,BleeckerColorScheme scheme,string folder)
    {
        Require(XamlRoot.RasterizationScale==1,"The full Bauhaus raster reference currently requires display scale 1; scaling acceptance remains open.");
        using var curveStream=typeof(BauhausBackgroundsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.BauhausRaster.json")??throw new InvalidOperationException("Missing frozen curve pixel reference.");
        using var curves=JsonDocument.Parse(curveStream);
        var reports=new List<object>();var passed=true;
        foreach(var control in new[]{Autostrada,Pompeii})
        {
            using var reference=typeof(BauhausBackgroundsFixture).Assembly.GetManifestResourceStream($"Orchard.Catalog.Reference.Bauhaus{control.Type}.png")??throw new InvalidOperationException("Missing full frozen browser raster.");
            using var stream=new InMemoryRandomAccessStream();using(var reader=new MemoryStream()){reference.CopyTo(reader);await stream.WriteAsync(reader.ToArray().AsBuffer());}stream.Seek(0);
            var decoder=await BitmapDecoder.CreateAsync(stream);var expected=(await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Straight,new BitmapTransform(),ExifOrientationMode.IgnoreExifOrientation,ColorManagementMode.DoNotColorManage)).DetachPixelData();
            var origin=control.TransformToVisual(XamlRoot.Content).TransformPoint(new Point());var left=(int)Math.Round(origin.X+.5);var top=(int)Math.Round(origin.Y+.5);
            Require(left>=0&&top>=0&&left+decoder.PixelWidth<=nativeWidth&&top+decoder.PixelHeight<=native.Length/4/nativeWidth,"Bauhaus full raster exceeds the owned client capture.");
            var maximum=0;var differences=0;
            for(var y=0;y<(int)decoder.PixelHeight;y++)for(var x=0;x<(int)decoder.PixelWidth;x++)
            {
                var source=(y*(int)decoder.PixelWidth+x)*4;var target=((top+y)*(int)nativeWidth+left+x)*4;
                for(var channel=0;channel<3;channel++){var delta=Math.Abs(native[target+channel]-expected[source+channel]);maximum=Math.Max(maximum,delta);if(delta>3)differences++;}
            }
            passed&=differences==0;reports.Add(new{type=control.Type.ToString(),left,top,width=decoder.PixelWidth,height=decoder.PixelHeight,maximumRgbDifference=maximum,channelsOverThree=differences});
        }
        foreach(var sample in curves.RootElement.GetProperty("curveSamples").EnumerateArray())
        {
            var type=sample.GetProperty("type").GetString();var control=type=="Nazca"?Nazca:Monitor;var origin=control.TransformToVisual(XamlRoot.Content).TransformPoint(new Point());
            var x=sample.GetProperty("x").GetInt32();var y=sample.GetProperty("y").GetInt32();var left=(int)Math.Round(origin.X+.5);var top=(int)Math.Round(origin.Y+.5);var offset=((top+y)*(int)nativeWidth+left+x)*4;
            var expected=new[]{sample.GetProperty("blue").GetInt32(),sample.GetProperty("green").GetInt32(),sample.GetProperty("red").GetInt32()};var actual=new[]{(int)native[offset],native[offset+1],native[offset+2]};
            var maximum=actual.Zip(expected).Max(pair=>Math.Abs(pair.First-pair.Second));passed&=maximum<=3;reports.Add(new{type,x,y,maximumRgbDifference=maximum,expectedBgr=expected,actualBgr=actual});
        }
        await File.WriteAllTextAsync(Path.Combine(folder,$"bauhaus-full-raster-{scheme}.json"),JsonSerializer.Serialize(new{passed,reports},new JsonSerializerOptions{WriteIndented=true}));
        Require(passed,"Bauhaus rectangular borders differ from the actual frozen React/Tailwind raster; see the full raster report.");
    }
    private void VerifyNarrowGeometry()
    {
        using var stream=typeof(BauhausBackgroundsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.BauhausNarrow.json")??throw new InvalidOperationException("Missing narrow browser geometry reference.");
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
