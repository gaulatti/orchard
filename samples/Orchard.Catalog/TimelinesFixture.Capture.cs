using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Catalog;

public sealed partial class TimelinesFixture
{
    private sealed class ViewportCapture(TimelinesFixture fixture, Grid root, string folder, BleeckerColorScheme scheme) : IDisposable
    {
        private readonly Panel parent = (Panel)fixture.Frame.Parent;
        private readonly int index = ((Panel)fixture.Frame.Parent).Children.IndexOf(fixture.Frame);
        private readonly Grid host = new() { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        private readonly NativeInputSession input = new(root.XamlRoot, folder, "timeline-source-" + scheme);
        private readonly List<object> reports = [];
        private bool mounted;
        internal async Task MountAsync(FrameworkElement foregroundTarget)
        {
            Require(root.XamlRoot.RasterizationScale == 1 && new Windows.UI.ViewManagement.UISettings().TextScaleFactor == 1, "Timeline source capture requires display/text scale 1.");
            await input.AcquireAsync(foregroundTarget);
            Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count);
            parent.Children.Remove(fixture.Frame); host.Children.Add(fixture.Frame); root.Children.Add(host); mounted = true;
            root.UpdateLayout(); await MainWindow.AwaitPresentationAsync(fixture.Frame);
        }
        internal async Task CaptureAsync(string file)
        {
            var client = NativeScreenCapture.ClientBounds(input.Window); var screen = input.ScreenBounds(fixture.Frame);
            var bounds = new Rect(screen.X - client.X, screen.Y - client.Y, screen.Width, screen.Height);
            Require(bounds.Left >= 0 && bounds.Top >= 0 && bounds.Right <= client.Width && bounds.Bottom <= client.Height &&
                bounds.X == Math.Floor(bounds.X) && bounds.Y == Math.Floor(bounds.Y) && bounds.Width == fixture.Frame.Width && bounds.Height == fixture.Frame.Height,
                "Timeline source viewport must be fully visible at integer device coordinates.");
            string? clientFile = null;
            if (fixture.Timeline.Items.Count == 0) await fixture.CaptureEmptyAsync(Path.Combine(folder, file));
            else
            {
                await input.MoveAsync(fixture.Frame, localPoint: new Point(fixture.Frame.Width - 4, fixture.Frame.Height - 4));
                Directory.CreateDirectory(Path.Combine(folder, "timeline-client")); clientFile = "timeline-client/" + file;
                await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, clientFile));
                using var stream = new InMemoryRandomAccessStream();
                using (var writer = new DataWriter(stream.GetOutputStreamAt(0))) { writer.WriteBytes(await File.ReadAllBytesAsync(Path.Combine(folder, clientFile))); await writer.StoreAsync(); await writer.FlushAsync(); writer.DetachStream(); }
                stream.Seek(0); var decoder = await BitmapDecoder.CreateAsync(stream);
                var transform = new BitmapTransform { Bounds = new BitmapBounds { X = (uint)bounds.X, Y = (uint)bounds.Y, Width = (uint)bounds.Width, Height = (uint)bounds.Height } };
                var pixels = await decoder.GetPixelDataAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Straight, transform, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
                await File.WriteAllBytesAsync(Path.Combine(folder, file), NativeCapturePng.Encode(pixels.DetachPixelData(), (int)bounds.Width, (int)bounds.Height));
            }
            reports.Add(new { file, clientFile, pipeline = clientFile is null ? "empty-bitmap" : "owned-client-integer-crop", masks = 0, resampling = false,
                frame = new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height }, client = new { width = client.Width, height = client.Height } });
            await File.WriteAllTextAsync(Path.Combine(folder, $"timeline-viewport-{scheme}.json"), JsonSerializer.Serialize(new { reports }, new JsonSerializerOptions { WriteIndented = true }));
        }
        public void Dispose()
        {
            if (mounted) { host.Children.Remove(fixture.Frame); root.Children.Remove(host); parent.Children.Insert(index, fixture.Frame); }
            input.Dispose();
            Require(ReferenceEquals(fixture.Frame.Parent, parent) && parent.Children.IndexOf(fixture.Frame) == index && !root.Children.Contains(host), "Timeline capture did not restore its original catalog parent.");
            File.WriteAllText(Path.Combine(folder, $"timeline-viewport-restored-{scheme}.json"), JsonSerializer.Serialize(new { restored = true, originalIndex = index, overlayRemoved = true }));
        }
    }
}
