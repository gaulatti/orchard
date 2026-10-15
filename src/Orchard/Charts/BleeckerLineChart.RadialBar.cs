using System.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard;

public sealed partial class BleeckerLineChart
{
    private bool radialBarMode;
    private object radialInnerSetting = "10%", radialOuterSetting = "80%";
    private double radialStart = 90, radialEnd = -270, radialCx, radialCy, radialInner, radialOuter, radialBand, radialLegendWidth;
    private readonly List<RadialBarEntry> radialEntries = [];
    private sealed record RadialBarEntry(int Index, object? Value, double Inner, double Outer, double Start, double End, double Tick);
    internal void UseRadialBarProfile(object inner, object outer, double start, double end)
    {
        if (!double.IsFinite(start) || !double.IsFinite(end)) throw new ArgumentOutOfRangeException(nameof(start), "Radial Bar angles must be finite.");
        radialBarMode = true; sparklineMode = barMode = areaMode = pieMode = scatterMode = radarMode = funnelMode = sankeyMode = currencyMode = false;
        radialInnerSetting = inner; radialOuterSetting = outer; radialStart = start; radialEnd = end; QueueRender();
    }
    private Brush? RadialPaint(int index)
    {
        var paint = BleeckerChartData.Read(Data[index], "fill");
        return paint is null ? null : paint as Brush ?? throw new InvalidOperationException("Radial Bar fill requires a native Brush.");
    }
    private void RenderRadialBar()
    {
        plot!.Children.Clear(); plot.Clip = null; legend!.Children.Clear(); rendered.Clear(); radialEntries.Clear(); radialLabels.Clear();
        radialLegendWidth = RenderRadialLegend(); radialCx = RenderWidth / 2; radialCy = RenderHeight / 2;
        var maximum = Math.Max(0, Math.Min(RenderWidth - 10 - radialLegendWidth, RenderHeight - 10) / 2);
        radialInner = BleeckerPieGeometry.Radius(radialInnerSetting, maximum); radialOuter = BleeckerPieGeometry.Radius(radialOuterSetting, maximum, maximum * .8);
        radialBand = Data.Count == 0 ? 0 : Math.Abs(radialOuter - radialInner) / Data.Count;
        var values = Data.SelectMany(row => BleeckerChartData.NumericDomain(BleeckerChartData.Read(row, "value"))).ToArray();
        if (values.Length > 0 && radialBand > 0)
        {
            var min = Math.Min(0, values.Min()); var max = values.Max(); var baseline = min <= 0 && max >= 0 ? 0 : max < 0 ? max : min;
            double Angle(object? value, double missing)
            {
                var number = value is bool flag ? flag ? 1d : 0d : BleeckerChartData.Number(value);
                return number is not double n ? missing : min == max ? (radialStart + radialEnd) / 2 : BleeckerChartScale.Position(n, min, max, radialStart, radialEnd);
            }
            var size = radialBand * .8; if (size > 1) size = Math.Truncate(size);
            for (var i = 0; i < Data.Count; i++)
            {
                var raw = BleeckerChartData.Read(Data[i], "value");
                var low = raw is IList list ? list.Count > 0 ? list[0] : null : baseline;
                var high = raw is IList range ? range.Count > 1 ? range[1] : null : raw;
                var slot = Math.Min(radialInner, radialOuter) + (radialInner <= radialOuter ? i : Data.Count - i - 1) * radialBand;
                var inner = slot + radialBand * .1; var outer = inner + size;
                radialEntries.Add(new(i, high, inner, outer, Angle(low, radialStart), Angle(high, radialEnd), slot + radialBand / 2));
            }
            foreach (var entry in radialEntries) AddRadialSector(entry, true);
            foreach (var entry in radialEntries) AddRadialSector(entry, false);
            BleeckerChartTextStyle? textStyle = null;
            try { foreach (var entry in radialEntries) RenderRadialLabel(entry, ref textStyle); }
            finally { textStyle?.Dispose(); }
        }
        PaintInteraction(); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private void AddRadialSector(RadialBarEntry entry, bool background)
    {
        var data = BleeckerRadialBarGeometry.Path(radialCx, radialCy, entry.Inner, entry.Outer, background ? radialStart : entry.Start, background ? radialEnd : entry.End);
        if (data is null || data.Contains("NaN", StringComparison.Ordinal)) return;
        var path = Resource<Path>($"<Path Data=\"{data}\" Fill=\"{{ThemeResource {(background ? "BleeckerMutedBrush" : BleeckerChartTheme.PickColor(entry.Index))}}}\" />");
        path.Tag = (background ? "radial-background" : "radial-foreground", entry.Index);
        if (!background && RadialPaint(entry.Index) is Brush paint) path.Fill = paint;
        plot!.Children.Add(path);
    }
    private double RenderRadialLegend()
    {
        if (!ShowLegend) return 0;
        var entries = new List<(Canvas Item, double Width)>();
        foreach (var i in Enumerable.Range(0, Data.Count).OrderBy(i => ChartReactText(BleeckerChartData.Read(Data[i], "name")), StringComparer.Ordinal))
        {
            var item = new Canvas { Height = 24 }; var icon = Resource<Microsoft.UI.Xaml.Shapes.Rectangle>($"<Rectangle Width=\"10\" Height=\"7.5\" Fill=\"{{ThemeResource {BleeckerChartTheme.PickColor(i)}}}\" />"); item.Children.Add(icon);
            var label = Text(ChartReactText(BleeckerChartData.Read(Data[i], "name")), 16, BleeckerChartTheme.PickColor(i)!); label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var empty = label.Text.Length == 0; item.Height = empty ? 10 : 24; Canvas.SetTop(icon, empty ? 1.25 : 9.90625);
            Canvas.SetLeft(label, 14); Canvas.SetTop(label, (empty ? 9.34375 : 18) - label.BaselineOffset); item.Children.Add(label);
            if (RadialPaint(i) is Brush paint) icon.Fill = label.Foreground = paint;
            var width = 24 + Math.Ceiling(BleeckerPieText.InlineWidth(label) * 64) / 64; item.Width = width; entries.Add((item, width));
        }
        var maximum = entries.Count == 0 ? 0 : entries.Max(entry => entry.Width); var y = (RenderHeight - entries.Sum(entry => entry.Item.Height)) / 2;
        foreach (var entry in entries) { Canvas.SetLeft(entry.Item, RenderWidth - 5 - maximum); Canvas.SetTop(entry.Item, y); legend!.Children.Add(entry.Item); y += entry.Item.Height; }
        return maximum;
    }
}
