using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard;

public sealed partial class BleeckerLineChart
{
    private bool sankeyMode;
    private readonly BleeckerSankeyInteraction sankeyInteraction = new();
    private BleeckerSankeyChartData? sankeyData, observedSankeyData;
    private ObservableCollection<IReadOnlyDictionary<string, object?>>? observedSankeyLinks;
    private ObservableCollection<Brush>? sankeyColors, observedSankeyColors;
    private readonly HashSet<INotifyPropertyChanged> observedSankeyRecords = [];
    private double sankeyNodeWidth = 12, sankeyCurvature = .5, sankeyOpacity = .35, sankeyRadius = 4;
    private BleeckerSankeyGeometry.Layout sankeyLayout = new([], []);
    private int? sankeyHoveredLink;
    private readonly Stopwatch sankeyClock = Stopwatch.StartNew();
    private readonly Dictionary<int, BleeckerCssTransition<double>> sankeyMotions = [];
    private bool sankeyRendering;
    private readonly Dictionary<int, CanvasGeometry> sankeyFlowGeometry = [];
    private void ReleaseSankeyGeometry() { foreach (var geometry in sankeyFlowGeometry.Values) geometry.Dispose(); sankeyFlowGeometry.Clear(); }
    internal void UseSankeyProfile(BleeckerSankeyChartData data, ObservableCollection<Brush>? colors, double nodeWidth, double curvature, double opacity, double radius)
    {
        sankeyMode = true; sparklineMode = barMode = areaMode = pieMode = scatterMode = radarMode = radialBarMode = funnelMode = currencyMode = false;
        var changed = !ReferenceEquals(sankeyData, data) || !ReferenceEquals(sankeyColors, colors);
        if (changed) StopObservingSankey();
        sankeyData = data; sankeyColors = colors; sankeyNodeWidth = nodeWidth; sankeyCurvature = curvature; sankeyOpacity = opacity; sankeyRadius = radius;
        if (!ReferenceEquals(Data, data.Nodes)) Data = data.Nodes;
        else if (changed && IsLoaded) ObserveSankey();
        QueueRender();
    }
    private void ObserveSankey()
    {
        if (!sankeyMode || sankeyData is null) return;
        observedSankeyData = sankeyData; observedSankeyData.PropertyChanged += OnSankeyRootChanged;
        observedSankeyLinks = sankeyData.Links; observedSankeyLinks.CollectionChanged += OnCollectionChanged;
        foreach (var record in observedSankeyLinks.OfType<INotifyPropertyChanged>()) if (observedSankeyRecords.Add(record)) record.PropertyChanged += OnRecordChanged;
        observedSankeyColors = sankeyColors; if (observedSankeyColors is not null) observedSankeyColors.CollectionChanged += OnSankeyPaletteChanged;
    }
    private void StopObservingSankey()
    {
        if (observedSankeyData is not null) observedSankeyData.PropertyChanged -= OnSankeyRootChanged;
        if (observedSankeyLinks is not null) observedSankeyLinks.CollectionChanged -= OnCollectionChanged;
        if (observedSankeyColors is not null) observedSankeyColors.CollectionChanged -= OnSankeyPaletteChanged;
        foreach (var record in observedSankeyRecords) record.PropertyChanged -= OnRecordChanged;
        observedSankeyRecords.Clear(); observedSankeyData = null; observedSankeyLinks = null; observedSankeyColors = null;
        if (sankeyRendering) CompositionTarget.Rendering -= OnSankeyFrame; sankeyRendering = false;
    }
    private void OnSankeyRootChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (sankeyData is null) return;
        if (!ReferenceEquals(Data, sankeyData.Nodes)) Data = sankeyData.Nodes;
        else { Observe(); QueueRender(); }
    }
    private void OnSankeyPaletteChanged(object? sender, NotifyCollectionChangedEventArgs args) => QueueRender();
    private Brush? SankeyPaint(int nodeIndex, double paletteIndex)
    {
        var fill = BleeckerChartData.Read(Data[nodeIndex], "fill");
        if (fill is not null) return fill as Brush ?? throw new InvalidOperationException("Sankey fill requires a native Brush.");
        // An explicitly empty source palette omits the SVG fill attribute;
        // SVG's measured initial fill is black, in both themes.
        if (sankeyColors is { Count: 0 }) return new SolidColorBrush(Microsoft.UI.Colors.Black);
        var count = sankeyColors?.Count ?? BleeckerChartTheme.Palette.Count; var remainder = paletteIndex % count;
        if (!double.IsFinite(remainder) || remainder < 0 || remainder != Math.Truncate(remainder)) return new SolidColorBrush(Microsoft.UI.Colors.Black);
        return sankeyColors is null ? null : sankeyColors[(int)remainder];
    }
    private void RenderSankey()
    {
        ReleaseSankeyGeometry();
        plot!.Children.Clear(); plot.Clip = new RectangleGeometry { Rect = new Rect(0, 0, RenderWidth, RenderHeight) }; legend!.Children.Clear(); rendered.Clear();
        sankeyLayout = BleeckerSankeyGeometry.Compute(Data.ToArray(), sankeyData!.Links.ToArray(), RenderWidth, RenderHeight, sankeyNodeWidth, sankeyCurvature);
        foreach (var link in sankeyLayout.Links)
        {
            if (link.Path is not string data) continue;
            var rawIndex = BleeckerChartData.Read(Data[link.Source], "index"); var index = rawIndex is null ? 0 : BleeckerChartData.Number(rawIndex) ?? double.NaN;
            var key = BleeckerChartTheme.PickColor(index);
            var fill = key is null ? "Black" : $"{{ThemeResource {key}}}";
            var path = Resource<Path>($"<Path Data=\"{data}\" Fill=\"{fill}\" />");
            ((PathGeometry)path.Data).FillRule = FillRule.Nonzero;
            if (SankeyPaint(link.Source, index) is Brush paint) path.Fill = paint;
            path.Tag = ("sankey-link", link.Index); plot.Children.Add(path);
            var v = link.Coordinates; Vector2 Point(int at) => new((float)v[at], (float)v[at + 1]);
            using var builder = new CanvasPathBuilder(CanvasDevice.GetSharedDevice()); builder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            builder.BeginFigure(Point(0)); builder.AddCubicBezier(Point(2), Point(4), Point(6)); builder.AddLine(Point(8)); builder.AddCubicBezier(Point(10), Point(12), Point(14)); builder.EndFigure(CanvasFigureLoop.Closed);
            sankeyFlowGeometry.Add(link.Index, CanvasGeometry.CreatePath(builder));
        }
        foreach (var node in sankeyLayout.Nodes)
        {
            if (!node.Paintable || node.Width == 0 || node.Height == 0) continue;
            var x = node.X; var y = node.Y; var w = node.Width; var h = node.Height;
            var rx = Math.Min(Math.Max(0, sankeyRadius), w / 2); var ry = Math.Min(Math.Max(0, sankeyRadius), h / 2);
            var data = rx == 0 || ry == 0 ? $"M{N(x)},{N(y)}h{N(w)}v{N(h)}h{N(-w)}Z" :
                $"M{N(x + rx)},{N(y)}H{N(x + w - rx)}A{N(rx)},{N(ry)} 0 0 1 {N(x + w)},{N(y + ry)}V{N(y + h - ry)}A{N(rx)},{N(ry)} 0 0 1 {N(x + w - rx)},{N(y + h)}H{N(x + rx)}A{N(rx)},{N(ry)} 0 0 1 {N(x)},{N(y + h - ry)}V{N(y + ry)}A{N(rx)},{N(ry)} 0 0 1 {N(x + rx)},{N(y)}Z";
            var path = Resource<Path>($"<Path Data=\"{data}\" Fill=\"{{ThemeResource {BleeckerChartTheme.PickColor(node.Index)}}}\" Stroke=\"{{ThemeResource BleeckerCardBrush}}\" StrokeThickness=\"2\" />");
            if (SankeyPaint(node.Index, node.Index) is Brush paint) path.Fill = paint;
            path.Tag = ("sankey-node", node.Index); plot.Children.Add(path);
        }
        UpdateSankeyOpacity(); PaintInteraction(); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private void MoveSankeyPointer(PointerRoutedEventArgs args)
    {
        if (args.IsGenerated) return;
        for (var source = args.OriginalSource as DependencyObject; source is not null && !ReferenceEquals(source, this); source = VisualTreeHelper.GetParent(source))
            if (source is Path { Tag: ValueTuple<string, int> tag } && tag.Item1 is "sankey-node" or "sankey-link")
            {
                if (tag.Item1 == "sankey-node") { SetSankeyHover(null); sankeyInteraction.Pointer(false, tag.Item2); PaintInteraction(); return; }
                break;
            }
        // XAML's approximate curved-path hit can include a neighboring band
        // outside its fill. Test the same native point against the actual cubic
        // geometry with tighter flattening precision; do not enlarge hit bounds.
        var raw = args.GetCurrentPoint(this).Position;
        if (raw.X >= 0 && raw.Y >= 0 && raw.X < RenderWidth && raw.Y < RenderHeight)
            foreach (var link in sankeyLayout.Links.Reverse())
                if (sankeyFlowGeometry.TryGetValue(link.Index, out var geometry) && geometry.FillContainsPoint(new Vector2((float)raw.X, (float)raw.Y), Matrix3x2.Identity, .001f))
                { SetSankeyHover(link.Index); sankeyInteraction.Pointer(true, link.Index); PaintInteraction(); return; }
        SetSankeyHover(null); sankeyInteraction.Exit(); PaintInteraction();
    }
    private void SetSankeyHover(int? index) { if (!sankeyMode || sankeyHoveredLink == index) return; sankeyHoveredLink = index; UpdateSankeyOpacity(); }
    private void UpdateSankeyOpacity()
    {
        var now = sankeyClock.Elapsed.TotalMilliseconds; var running = false;
        foreach (var path in plot!.Children.OfType<Path>().Where(path => path.Tag is ValueTuple<string, int> tag && tag.Item1 == "sankey-link"))
        {
            var index = ((ValueTuple<string, int>)path.Tag).Item2;
            if (!sankeyMotions.TryGetValue(index, out var motion)) { motion = new((from, to, t) => from + (to - from) * t, BleeckerCssEasing.Default); motion.Retarget(1, now, 0, false); sankeyMotions[index] = motion; }
            motion.Retarget(sankeyHoveredLink == index ? .8 : 1, now, 300, IsLoaded && tooltipSettings.AnimationsEnabled);
            path.Opacity = Math.Clamp(sankeyOpacity, 0, 1) * motion.ValueAt(now); running |= motion.IsRunning(now);
        }
        if (running && !sankeyRendering) CompositionTarget.Rendering += OnSankeyFrame;
        else if (!running && sankeyRendering) CompositionTarget.Rendering -= OnSankeyFrame;
        sankeyRendering = running;
    }
    private void OnSankeyFrame(object? sender, object args) => UpdateSankeyOpacity();
    private void ShowSankeyActive(int index)
    {
        if (tooltip is null || tooltipItems is null || tooltipContainer is null || active is null) return;
        string Name(int at) => at >= 0 && at < Data.Count ? ChartReactText(BleeckerChartData.Read(Data[at], "name")) : "";
        string name; double value, x, y;
        if (index < sankeyLayout.Nodes.Length) { var node = sankeyLayout.Nodes[index]; name = Name(index); value = node.Value; x = node.X + node.Width / 2; y = node.CenterY; }
        else { var link = sankeyLayout.Links.ElementAtOrDefault(index - sankeyLayout.Nodes.Length); if (link is null || !link.Valid) { ClearActive(); return; } name = Name(link.Source) + " - " + Name(link.Target); value = link.Value; x = (link.SourceX + link.TargetX) / 2; y = (link.SourceY + link.TargetY) / 2; }
        active.Children.Clear(); tooltipItems.Children.Clear(); var number = ChartReactText(value); var content = name + " : " + number;
        var row = Text(ChartVisibleText(content), 16, "BleeckerTextPrimaryBrush"); AutomationProperties.SetName(row, content);
        row.LineHeight = 24; row.LineStackingStrategy = LineStackingStrategy.BlockLineHeight; row.Margin = new Thickness(0, 4, 0, 4);
        var fragments = new[] { ChartVisibleText(name), name.Length == 0 ? ": \u200B" : "\u200B : \u200B", ChartVisibleText(number) };
        row.Width = fragments.Sum(fragment => { var text = Text(fragment, 16, "BleeckerTextPrimaryBrush"); text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); return Math.Ceiling(BleeckerPieText.InlineWidth(text) * 64) / 64; });
        tooltipItems.Children.Add(row); tooltip.Visibility = tooltipContainer.Visibility = Visibility.Visible;
        tooltip.Width = double.NaN; tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); tooltip.Width = Math.Ceiling(tooltip.DesiredSize.Width * 64) / 64; tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); row.RenderTransform = new TranslateTransform { Y = 18 - row.BaselineOffset };
        tooltipContainer.Margin = double.IsFinite(x) && double.IsFinite(y) ? new Thickness(BleeckerTooltipPlacement.Position(x, tooltip.DesiredSize.Width, 20, RenderWidth - 20), BleeckerTooltipPlacement.Position(y, tooltip.DesiredSize.Height, 10, RenderHeight - 10), 0, 0) : new Thickness(0);
        UpdateTooltipEffects(); AutomationProperties.SetHelpText(this, content);
    }
}
