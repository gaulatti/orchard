using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class StatCardsFixture
{
    private async Task VerifyTypographyAsync(Grid host, NativeInputSession input, string folder, BleeckerColorScheme scheme)
    {
        using var reference = JsonDocument.Parse(typeof(StatCardsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.StatCardTypography.json")!);
        var reports = new List<object>();
        var failures = new List<string>();
        foreach (var sample in reference.RootElement.GetProperty("cases").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == scheme.ToString()))
        {
            var id = sample.GetProperty("id").GetString()!;
            var props = sample.GetProperty("props");
            var expected = sample.GetProperty("layout");
            var card = new BleeckerStatCard
            {
                Title = String(props, "title"), Value = Scalar(props.GetProperty("value")), Description = String(props, "description"),
                Width = expected.GetProperty("width").GetDouble(), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top
            };
            if (props.TryGetProperty("icon", out var icon)) card.Icon = Scalar(icon);
            if (props.TryGetProperty("footer", out var footer)) card.Footer = Scalar(footer);
            if (props.TryGetProperty("trend", out var trend)) card.Trend = new BleeckerStatTrend
            {
                Value = String(trend, "value"), Label = String(trend, "label"),
                Direction = Enum.Parse<BleeckerStatTrendDirection>(String(trend, "direction")!, true)
            };
            host.Children.Clear(); host.Children.Add(card);
            await AwaitAsync(() => card.IsLoaded, "Typography Stat Card did not mount.");
            card.ApplyTemplate(); UpdateLayout(); await FrameAsync(); UpdateLayout();
            var path = System.IO.Path.Combine(folder, $"stat-typography-{id}-{scheme}.png");
            await input.RequireForegroundAsync();
            var client = await NativeScreenCapture.CaptureClientAsync(input.Window, path);
            var origin = input.ScreenBounds(card);
            using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            using (var writer = new Windows.Storage.Streams.DataWriter(stream))
            {
                writer.WriteBytes(await File.ReadAllBytesAsync(path)); await writer.StoreAsync(); writer.DetachStream();
            }
            stream.Seek(0);
            var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
            var pixels = (await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
                Windows.Graphics.Imaging.BitmapAlphaMode.Straight, new Windows.Graphics.Imaging.BitmapTransform(),
                Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation, Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
            foreach (var field in expected.GetProperty("texts").EnumerateObject())
            {
                var text = Part<TextBlock>(card, "PART_" + char.ToUpperInvariant(field.Name[0]) + field.Name[1..]);
                var baseline = text.TransformToVisual(card).TransformPoint(new Point()).Y + text.BaselineOffset;
                var cssBaseline = field.Value.GetProperty("baseline").GetDouble();
                var expectedBaseline = DeviceBaseline(card, cssBaseline);
                var ink = field.Value.GetProperty("ink");
                var left = ink.GetProperty("left").GetInt32(); var right = ink.GetProperty("right").GetInt32();
                var top = ink.GetProperty("top").GetInt32(); var bottom = ink.GetProperty("bottom").GetInt32();
                var x0 = checked((int)(origin.X - client.X)); var y0 = checked((int)(origin.Y - client.Y));
                var background = checked(((y0 + top) * (int)decoder.PixelWidth + x0 + left) * 4);
                var rows = new List<int>();
                for (var y = top; y < bottom; y++)
                    for (var x = left; x < right; x++)
                    {
                        var offset = checked(((y0 + y) * (int)decoder.PixelWidth + x0 + x) * 4);
                        if (Enumerable.Range(0, 3).Any(channel => pixels[offset + channel] != pixels[background + channel]))
                        { rows.Add(y); break; }
                    }
                var expectedRows = ink.GetProperty("rows").EnumerateArray().Select(item => item.GetInt32()).ToArray();
                var baselinePassed = Math.Abs(baseline - expectedBaseline) < .001;
                var rowsPassed = rows.SequenceEqual(expectedRows);
                reports.Add(new { id, field = field.Name, baseline, cssBaseline, expectedBaseline, baselinePassed, rows, expectedRows, rowsPassed });
                if (!baselinePassed || !rowsPassed) failures.Add($"{id} {field.Name}: baseline {baseline:R}/{expectedBaseline:R}, ink rows equal={rowsPassed}");
            }
            Near(card.ActualHeight, expected.GetProperty("height").GetDouble(), id + " typography card height");
            if (id == "slots") await VerifyTypographyLifecycleAsync(card, host, expected, folder, scheme);
        }
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"stat-card-typography-{scheme}.json"),
            JsonSerializer.Serialize(new { passed = failures.Count == 0, reports }, new JsonSerializerOptions { WriteIndented = true }));
        Require(failures.Count == 0, "Stat Card typography mismatches: " + string.Join(" | ", failures));
    }

    private static double DeviceBaseline(BleeckerStatCard card, double cssBaseline)
    {
        var origin = card.TransformToVisual(card.XamlRoot.Content).TransformPoint(new Point()).Y;
        return Math.Round(origin + cssBaseline, MidpointRounding.AwayFromZero) - origin;
    }

    private async Task VerifyTypographyLifecycleAsync(BleeckerStatCard card, Grid host, JsonElement expected, string folder, BleeckerColorScheme scheme)
    {
        var reports = new List<object>();
        async Task CheckAsync(string phase)
        {
            UpdateLayout(); await FrameAsync(); UpdateLayout(); await FrameAsync();
            foreach (var field in expected.GetProperty("texts").EnumerateObject())
            {
                var text = Part<TextBlock>(card, "PART_" + char.ToUpperInvariant(field.Name[0]) + field.Name[1..]);
                var actual = text.TransformToVisual(card).TransformPoint(new Point()).Y + text.BaselineOffset;
                var source = DeviceBaseline(card, field.Value.GetProperty("baseline").GetDouble());
                reports.Add(new { phase, field = field.Name, actual, expected = source });
                Near(actual, source, phase + " " + field.Name + " baseline");
            }
        }
        var margin = host.Margin;
        var passed = false;
        try
        {
            card.Footer = "Changed locally.";
            await CheckAsync("content-change");
            host.Margin = new Thickness(margin.Left, margin.Top + .5, margin.Right, margin.Bottom);
            await CheckAsync("half-pixel-origin");
            host.Margin = new Thickness(margin.Left, margin.Top + .25, margin.Right, margin.Bottom);
            await CheckAsync("quarter-pixel-origin");
            host.Children.Remove(card);
            await AwaitAsync(() => !card.IsLoaded, "Typography Stat Card did not unmount.");
            card.Footer = "Updated locally."; host.Margin = margin; host.Children.Add(card);
            await AwaitAsync(() => card.IsLoaded, "Typography Stat Card did not remount.");
            await CheckAsync("remount");
            var previous = Part<TextBlock>(card, "PART_FooterText");
            var shift = previous.RenderTransform; var template = card.Template;
            card.Template = (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Grid /></ControlTemplate>");
            card.ApplyTemplate(); UpdateLayout();
            Require(!ReferenceEquals(previous.RenderTransform, shift), "Stat Card retained its typography transform on a detached template part.");
            previous.FontSize = 33;
            card.Template = template; card.ApplyTemplate(); UpdateLayout();
            await CheckAsync("template-restore");
            Require(!ReferenceEquals(previous, Part<TextBlock>(card, "PART_FooterText")), "Stat Card retained its detached text part.");
            passed = true;
        }
        finally
        {
            host.Margin = margin;
            await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"stat-card-typography-lifecycle-{scheme}.json"),
                JsonSerializer.Serialize(new { passed, reports }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
