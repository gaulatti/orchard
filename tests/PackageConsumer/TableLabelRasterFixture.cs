using System.Globalization;
using System.Security.Cryptography;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static partial class TableTemplateFixture
{
    // Optional observation of the existing alignment history. Original frames
    // remain intact; later captures never replace their pixels or expectations.
    private sealed class LabelRasterProbe(string directory)
    {
        private readonly List<object> frames = [];
        private readonly List<object> pngs = [];
        internal object Read(Grid root, BleeckerSortableTableHeader heading)
        {
            var label = Elements(heading).OfType<TextBlock>().Single(e => e.Name == "PART_Label");
            var button = Elements(heading).OfType<Button>().Single();
            var sorted = heading.CurrentSort.Field == heading.Field;
            var palette = Elements(button).OfType<TextBlock>().Single(e => e.Name == (sorted ? "PART_Active" : "PART_Normal"));
            static object Paint(Brush brush) => brush is SolidColorBrush solid
                ? new { color = solid.Color.ToString(), opacity = solid.Opacity }
                : throw new InvalidOperationException("Header label palette requires its native solid brush.");
            object Box(FrameworkElement element)
            {
                var transform = element.TransformToVisual(root);
                var origin = transform.TransformPoint(new Point());
                var horizontal = transform.TransformPoint(new Point(1, 0));
                var vertical = transform.TransformPoint(new Point(0, 1));
                var bounds = transform.TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
                return new { type = element.GetType().FullName, element.Name, x = origin.X, y = origin.Y,
                    width = element.ActualWidth, height = element.ActualHeight,
                    desiredWidth = element.DesiredSize.Width, desiredHeight = element.DesiredSize.Height,
                    left = bounds.Left, top = bounds.Top, right = bounds.Right, bottom = bounds.Bottom,
                    xx = horizontal.X - origin.X, xy = horizontal.Y - origin.Y,
                    yx = vertical.X - origin.X, yy = vertical.Y - origin.Y,
                    opacity = element.Opacity, rounding = element.UseLayoutRounding,
                    marginLeft = element.Margin.Left, marginTop = element.Margin.Top,
                    marginRight = element.Margin.Right, marginBottom = element.Margin.Bottom,
                    direction = element.FlowDirection.ToString(), alignment = element.HorizontalAlignment.ToString(),
                    columns = element is Grid grid ? grid.ColumnDefinitions.Select(c => c.ActualWidth).ToArray() : [] };
            }
            var ancestry = new List<object>();
            for (DependencyObject? current = label; current is not null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is FrameworkElement element) ancestry.Add(Box(element));
                if (ReferenceEquals(current, root)) break;
                if (ancestry.Count > 64) throw new InvalidOperationException("Unexpected header ancestry.");
            }
            return new { text = label.Text, sorted, field = heading.Field,
                theme = label.ActualTheme.ToString(), requestedTheme = root.RequestedTheme.ToString(),
                rasterScale = root.XamlRoot.RasterizationScale,
                fontFamily = label.FontFamily.Source, headerFontFamily = heading.FontFamily.Source,
                fontSize = label.FontSize, headerFontSize = heading.FontSize,
                weight = label.FontWeight.Weight, headerWeight = heading.FontWeight.Weight,
                fontStyle = label.FontStyle.ToString(), fontStretch = label.FontStretch.ToString(),
                spacing = label.CharacterSpacing, headerSpacing = heading.CharacterSpacing,
                language = label.Language, baseline = label.BaselineOffset,
                lineHeight = label.LineHeight, wrapping = label.TextWrapping.ToString(),
                lineStacking = label.LineStackingStrategy.ToString(), scaling = label.IsTextScaleFactorEnabled,
                foreground = Paint(label.Foreground), buttonForeground = Paint(button.Foreground), palette = Paint(palette.Foreground),
                label = Box(label), button = Box(button), heading = Box(heading), ancestry };
        }
        internal void Original(string file, object before, object after) => frames.Add(new { caseId = Path.GetFileNameWithoutExtension(file), phase = "original", file, before, after });
        internal async Task CaptureAsync(Grid root, BleeckerSortableTableHeader heading, string originalFile, string phase, Action guard)
        {
            guard(); var before = Read(root, heading);
            var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root); guard();
            var width = bitmap.PixelWidth; var height = bitmap.PixelHeight;
            var pixels = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());
            GC.KeepAlive(bitmap);
            var after = Read(root, heading);
            using var stream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)width, (uint)height, 96, 96, pixels);
            await encoder.FlushAsync(); stream.Seek(0);
            using var reader = new DataReader(stream.GetInputStreamAt(0)); await reader.LoadAsync((uint)stream.Size);
            var bytes = new byte[(int)stream.Size]; reader.ReadBytes(bytes);
            var file = Path.GetFileNameWithoutExtension(originalFile) + "-label-" + phase + ".png";
            await File.WriteAllBytesAsync(Path.Combine(directory, file), bytes);
            pngs.Add(new { file, width, height, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)) });
            frames.Add(new { caseId = Path.GetFileNameWithoutExtension(originalFile), phase, file, before, after }); guard();
        }
        internal object Report() => new { diagnosticOnly = true, normalTimingRun = false, frames, pngs, originalFramesReplaced = false, imageMasksApplied = false };
    }
}
