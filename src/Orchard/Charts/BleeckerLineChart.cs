using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.System;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard;

/// <summary>A consumer-owned, responsive native monotone line chart.</summary>
public sealed partial class BleeckerLineChart : Control
{
    private Canvas? plot, legend, active;
    private Border? tooltip;
    private Grid? tooltipContainer;
    private StackPanel? tooltipItems;
    private readonly HashSet<BleeckerChartSeries> observedSeries = [];
    private readonly HashSet<INotifyPropertyChanged> observedRecords = [];
    private ObservableCollection<IReadOnlyDictionary<string, object?>>? observedData;
    private ObservableCollection<BleeckerChartSeries>? observedCollection;
    private double left, right, top, bottom, min, max;
    private readonly BleeckerChartInteraction interaction=new();
    private bool queued;
    private bool sparklineMode;
    internal void UseSparklineProfile(){sparklineMode=true;barMode=false;QueueRender();}
    private readonly List<(BleeckerChartSeries Series, Brush Brush, BleeckerMonotoneCurve.Point[] Points)> rendered = [];
    private const string Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private double RenderWidth => barMode ? Math.Round(ActualWidth, MidpointRounding.AwayFromZero) : ActualWidth;
    private double RenderHeight => barMode ? Math.Round(ActualHeight, MidpointRounding.AwayFromZero) : ActualHeight;

    public BleeckerLineChart()
    {
        DefaultStyleKey = typeof(BleeckerLineChart);
        Data = new ObservableCollection<IReadOnlyDictionary<string, object?>>(); Series = new ObservableCollection<BleeckerChartSeries>();
        Loaded += (_, _) => { Observe(); Render(); ConnectTooltipEffects(); }; Unloaded += (_, _) => { StopObserving();interaction.Reset(); ClearActive(); DisconnectTooltipEffects(); };
        SizeChanged += (_, _) => QueueRender(); ActualThemeChanged += (_, _) => QueueRender();
        RegisterPropertyChangedCallback(FontFamilyProperty, (_, _) => QueueRender());
        PointerMoved += OnPointerMoved; PointerExited += (_, _) => {interaction.Exit();PaintInteraction();};
        GotFocus+=(_,_)=>{interaction.Focus();PaintInteraction();};
        LostFocus += (_, _) => {interaction.Blur();PaintInteraction();}; KeyDown += OnKeyDown;
    }
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(nameof(Data), typeof(object), typeof(BleeckerLineChart), new PropertyMetadata(null, OnDataChanged));
    public static readonly DependencyProperty SeriesProperty = DependencyProperty.Register(nameof(Series), typeof(object), typeof(BleeckerLineChart), new PropertyMetadata(null, OnDataChanged));
    public ObservableCollection<IReadOnlyDictionary<string, object?>> Data { get => (ObservableCollection<IReadOnlyDictionary<string, object?>>)GetValue(DataProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(DataProperty, value); } }
    public ObservableCollection<BleeckerChartSeries> Series { get => (ObservableCollection<BleeckerChartSeries>)GetValue(SeriesProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(SeriesProperty, value); } }
    public static readonly DependencyProperty XAxisKeyProperty = DependencyProperty.Register(nameof(XAxisKey), typeof(string), typeof(BleeckerLineChart), new PropertyMetadata(null, OnAppearanceChanged));
    public string? XAxisKey { get => (string?)GetValue(XAxisKeyProperty); set => SetValue(XAxisKeyProperty, value); }
    public static readonly DependencyProperty ShowGridProperty = DependencyProperty.Register(nameof(ShowGrid), typeof(bool), typeof(BleeckerLineChart), new PropertyMetadata(true, OnAppearanceChanged));
    public static readonly DependencyProperty ShowLegendProperty = DependencyProperty.Register(nameof(ShowLegend), typeof(bool), typeof(BleeckerLineChart), new PropertyMetadata(true, OnAppearanceChanged));
    public bool ShowGrid { get => (bool)GetValue(ShowGridProperty); set => SetValue(ShowGridProperty, value); }
    public bool ShowLegend { get => (bool)GetValue(ShowLegendProperty); set => SetValue(ShowLegendProperty, value); }
    public static readonly DependencyProperty XAxisLabelProperty = DependencyProperty.Register(nameof(XAxisLabel), typeof(string), typeof(BleeckerLineChart), new PropertyMetadata(null, OnAppearanceChanged));
    public static readonly DependencyProperty YAxisLabelProperty = DependencyProperty.Register(nameof(YAxisLabel), typeof(string), typeof(BleeckerLineChart), new PropertyMetadata(null, OnAppearanceChanged));
    public string? XAxisLabel { get => (string?)GetValue(XAxisLabelProperty); set => SetValue(XAxisLabelProperty, value); }
    public string? YAxisLabel { get => (string?)GetValue(YAxisLabelProperty); set => SetValue(YAxisLabelProperty, value); }
    public static readonly DependencyProperty YAxisTickFormatterProperty = DependencyProperty.Register(nameof(YAxisTickFormatter), typeof(object), typeof(BleeckerLineChart), new PropertyMetadata(null, OnAppearanceChanged));
    public static readonly DependencyProperty TooltipFormatterProperty = DependencyProperty.Register(nameof(TooltipFormatter), typeof(object), typeof(BleeckerLineChart), new PropertyMetadata(null, OnAppearanceChanged));
    public Func<double, string>? YAxisTickFormatter { get => (Func<double, string>?)GetValue(YAxisTickFormatterProperty); set => SetValue(YAxisTickFormatterProperty, value); }
    public Func<double, string>? TooltipFormatter { get => (Func<double, string>?)GetValue(TooltipFormatterProperty); set => SetValue(TooltipFormatterProperty, value); }

    private static void OnDataChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        var chart = (BleeckerLineChart)owner;
        if (args.NewValue is null && chart.IsLoaded) throw new ArgumentException("A mounted chart requires non-null data and series collections.");
        chart.StopObserving(); if (chart.IsLoaded) chart.Observe(); chart.QueueRender();
    }
    private static void OnAppearanceChanged(DependencyObject owner, DependencyPropertyChangedEventArgs _) => ((BleeckerLineChart)owner).QueueRender();
    protected override void OnApplyTemplate()
    {
        DisconnectTooltipEffects();
        base.OnApplyTemplate();
        plot = GetTemplateChild("PART_Plot") as Canvas ?? throw new InvalidOperationException("Line Chart requires PART_Plot.");
        legend = GetTemplateChild("PART_Legend") as Canvas ?? throw new InvalidOperationException("Line Chart requires PART_Legend.");
        active = GetTemplateChild("PART_Active") as Canvas ?? throw new InvalidOperationException("Line Chart requires PART_Active.");
        tooltip = GetTemplateChild("PART_Tooltip") as Border ?? throw new InvalidOperationException("Line Chart requires PART_Tooltip.");
        tooltipItems = GetTemplateChild("PART_TooltipItems") as StackPanel ?? throw new InvalidOperationException("Line Chart requires PART_TooltipItems.");
        tooltipContainer = GetTemplateChild("PART_TooltipContainer") as Grid ?? throw new InvalidOperationException("Line Chart requires PART_TooltipContainer.");
        ApplyTooltipEffectsTemplate();
        QueueRender();
    }
    private void Observe()
    {
        StopObserving(); observedData = Data; observedCollection = Series;
        observedData.CollectionChanged += OnCollectionChanged; observedCollection.CollectionChanged += OnCollectionChanged;
        foreach (var item in Series) { _ = item.Key; _ = item.Name; if (observedSeries.Add(item)) item.Changed += OnSeriesChanged; }
        foreach (var record in Data.OfType<INotifyPropertyChanged>()) if (observedRecords.Add(record)) record.PropertyChanged += OnRecordChanged;
    }
    private void StopObserving()
    {
        if (observedData is not null) observedData.CollectionChanged -= OnCollectionChanged;
        if (observedCollection is not null) observedCollection.CollectionChanged -= OnCollectionChanged;
        foreach (var item in observedSeries) item.Changed -= OnSeriesChanged;
        foreach (var item in observedRecords) item.PropertyChanged -= OnRecordChanged;
        observedSeries.Clear(); observedRecords.Clear(); observedData = null; observedCollection = null;
    }
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) { Observe(); QueueRender(); }
    private void OnSeriesChanged(object? sender, EventArgs args) => QueueRender();
    private void OnRecordChanged(object? sender, PropertyChangedEventArgs args) => QueueRender();
    private void QueueRender()
    {
        if (queued || !IsLoaded) return; queued = true;
        DispatcherQueue.TryEnqueue(() => { queued = false; if (IsLoaded) Render(); });
    }
    private void Render()
    {
        if (!IsLoaded || plot is null || legend is null || active is null || ActualWidth <= 0 || ActualHeight <= 0) return;
        if (!sparklineMode && XAxisKey is null) throw new InvalidOperationException("Line Chart XAxisKey must be supplied before mounting.");
        foreach (var item in Series) { _ = item.Key; _ = item.Name; }
        if (barMode) { RenderBars(); return; }
        plot.Clip = null; plot.Children.Clear(); legend.Children.Clear(); rendered.Clear();
        var legendHeight = RenderLegend();
        left = 60 + (string.IsNullOrEmpty(YAxisLabel) ? 0 : 20); right = ActualWidth - 20; top = 10;
        bottom = ActualHeight - (string.IsNullOrEmpty(XAxisLabel) ? 5 : 25) - 30 - legendHeight;
        if(sparklineMode){left=top=2;right=ActualWidth-2;bottom=ActualHeight-2;}
        if (right <= left || bottom <= top) { ClearActive(); return; }
        var ticks = BleeckerChartScale.Ticks(Series.SelectMany(series => Data.Select(row => BleeckerChartData.Number(BleeckerChartData.Read(row, series.Key)))).OfType<double>());
        if (ticks.Length > 0) { min = ticks[0]; max = ticks[^1]; }
        if (ShowGrid)
        {
            var positions = ticks.Length == 0 ? new[] { top, bottom } : ticks.Select(value => Y(value));
            foreach (var y in positions)
            {
                var line = Resource<Line>($"<Line X1=\"{N(left)}\" X2=\"{N(right)}\" Y1=\"{N(y)}\" Y2=\"{N(y)}\" Stroke=\"{{ThemeResource BleeckerBorderBrush}}\" StrokeThickness=\"1\" StrokeDashArray=\"3,3\" />");
                plot.Children.Add(line);
            }
        }
        if(!sparklineMode)foreach (var value in ticks)
        {
            var label = Text(YAxisTickFormatter is null ? BleeckerChartScale.TickLabel(value) : YAxisTickFormatter(value), 12, "BleeckerTextSecondaryBrush");
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            AddText(plot, label, left - 8 - label.DesiredSize.Width, Math.Clamp(Y(value), top + 2, bottom) + 4.26 - label.BaselineOffset);
        }
        if(!sparklineMode)RenderCategoryTicks();
        if (!sparklineMode&&!string.IsNullOrEmpty(XAxisLabel))
        {
            var label = Text(XAxisLabel, 12, "BleeckerTextSecondaryBrush"); label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            AddText(plot, label, (left + right - label.DesiredSize.Width) / 2, bottom + 40);
        }
        if (!sparklineMode&&!string.IsNullOrEmpty(YAxisLabel))
        {
            var label = Text(YAxisLabel, 12, "BleeckerTextSecondaryBrush"); label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            label.RenderTransform = new RotateTransform { Angle = -90 }; AddText(plot, label, 16, (top + bottom + label.DesiredSize.Width) / 2);
        }
        for (var index = 0; index < Series.Count; index++)
        {
            var series = Series[index]; var points = Data.Select((row, i) => new BleeckerMonotoneCurve.Point(X(i), ticks.Length > 0 && PointValue(BleeckerChartData.Read(row, series.Key)) is double value ? Y(value) : double.NaN)).ToArray();
            var path = Resource<Path>($"<Path Stroke=\"{{ThemeResource {BleeckerChartTheme.PickColor(index)}}}\" StrokeThickness=\"2\" />");
            if (series.Color is not null) path.Stroke = series.Color;
            path.Data = Geometry(BleeckerMonotoneCurve.Create(points));
            if (ticks.Length > 0 && points.Length >= 2) plot.Children.Add(path);
            rendered.Add((series, path.Stroke, points));
        }
        PaintInteraction();
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private double? PointValue(object? value)=>sparklineMode&&value is bool boolean?(boolean?1:0):BleeckerChartData.Number(value);
    private double RenderLegend()
    {
        if (!ShowLegend || Series.Count == 0) return 0;
        List<List<(FrameworkElement Element, double Width)>> rows = [[]]; var rowWidths = new List<double> { 0 };
        foreach (var series in Series.OrderBy(item => item.Name, StringComparer.Ordinal))
        {
            var index = Series.IndexOf(series); var group = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            var icon = Resource<Path>($"<Path Data=\"M0,16H10.667 A5.333,5.333 0 1 1 21.333,16 H32 M21.333,16 A5.333,5.333 0 1 1 10.667,16\" Stroke=\"{{ThemeResource {BleeckerChartTheme.PickColor(index)}}}\" StrokeThickness=\"4\" />");
            icon.Fill = null;
            if (barMode) icon = Resource<Path>($"<Path Data=\"M0,4h32v24h-32z\" Fill=\"{{ThemeResource {BleeckerChartTheme.PickColor(index)}}}\" />");
            var iconCanvas = new Canvas { Width = 32, Height = 32 }; iconCanvas.Children.Add(icon);
            var iconView = new Viewbox { Width = 14, Height = 14, Child = iconCanvas, VerticalAlignment = VerticalAlignment.Center };
            var label = Text(series.Name, 16, BleeckerChartTheme.PickColor(index)!);
            if (series.Color is not null) { if (barMode) icon.Fill = series.Color; else icon.Stroke = series.Color; label.Foreground = series.Color; }
            group.Children.Add(iconView); group.Children.Add(label); group.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var width = group.DesiredSize.Width + 10;
            if (rowWidths[^1] > 0 && rowWidths[^1] + width > RenderWidth - 20) { rows.Add([]); rowWidths.Add(0); }
            rows[^1].Add((group, width)); rowWidths[^1] += width;
        }
        var height = 16 + rows.Count * 24;
        for (var row = 0; row < rows.Count; row++)
        {
            var x = (RenderWidth - rowWidths[row]) / 2;
            foreach (var entry in rows[row]) { Canvas.SetLeft(entry.Element, x); Canvas.SetTop(entry.Element, RenderHeight - 5 - height + 16 + row * 24); legend!.Children.Add(entry.Element); x += entry.Width; }
        }
        return height;
    }
    private void RenderCategoryTicks()
    {
        if (Data.Count == 0) return;
        var boundary = ActualWidth;
        for (var index = Data.Count - 1; index >= 0; index--)
        {
            var label = Text(BleeckerChartData.Label(BleeckerChartData.Read(Data[index], XAxisKey!)), 12, "BleeckerTextSecondaryBrush");
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var coordinate = X(index); if (index == Data.Count - 1) coordinate = Math.Min(coordinate, ActualWidth - label.DesiredSize.Width / 2);
            if (coordinate - label.DesiredSize.Width / 2 < 0 || coordinate + label.DesiredSize.Width / 2 > boundary) continue;
            AddText(plot!, label, coordinate - label.DesiredSize.Width / 2, bottom + 8 + 8.52 - label.BaselineOffset); boundary = coordinate - label.DesiredSize.Width / 2 - 5;
        }
    }
    private double X(int index) => Data.Count == 1 ? (left + right) / 2 : left + index * (right - left) / (Data.Count - 1);
    private double Y(double value) => BleeckerChartScale.Position(value, min, max, bottom, top);
    private void OnPointerMoved(object sender, PointerRoutedEventArgs args)
    {
        if (barMode) { MoveBarPointer(args); return; }
        if (Data.Count == 0 || right <= left) return;
        var point = args.GetCurrentPoint(this).Position;
        if (point.X < left || point.X > right || point.Y < top || point.Y > bottom) {interaction.Exit();PaintInteraction();return;}
        var index = Data.Count == 1 ? 0 : (int)Math.Round((point.X - left) / (right - left) * (Data.Count - 1), MidpointRounding.AwayFromZero);
        if (index > 0 && point.X <= (X(index - 1) + X(index)) / 2) index--;
        interaction.Pointer(Math.Clamp(index,0,Data.Count-1),point.Y);PaintInteraction();
    }
    private void OnKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (args.Key is VirtualKey.Left or VirtualKey.Right or VirtualKey.Enter)
        {
            var direction = args.Key==VirtualKey.Enter?0:args.Key==VirtualKey.Right?1:-1;
            if (barMode && VerticalBars) direction = -direction;
            interaction.Key(direction,Data.Count);PaintInteraction();args.Handled=true;
        }
    }
    private void PaintInteraction(){if(interaction.Index(Data.Count) is int index)ShowActive(index,interaction.PointerY);else ClearActive();}
    private void ShowActive(int index, double? pointerY = null)
    {
        if (barMode) { ShowBarActive(index, pointerY); return; }
        if (active is null || tooltip is null || tooltipItems is null || tooltipContainer is null || index < 0 || index >= Data.Count) return;
        active.Children.Clear(); tooltipItems.Children.Clear();
        var category = sparklineMode?"":BleeckerChartData.Label(BleeckerChartData.Read(Data[index], XAxisKey!));
        var heading = Text(category, 16, "BleeckerTextSecondaryBrush"); heading.FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 };
        heading.LineHeight = 24; heading.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;if(!sparklineMode)tooltipItems.Children.Add(heading);
        var cursor = Resource<Line>($"<Line X1=\"{N(X(index))}\" X2=\"{N(X(index))}\" Y1=\"{N(top)}\" Y2=\"{N(bottom)}\" Stroke=\"{{ThemeResource {(sparklineMode?"BleeckerSparklineCursorBrush":"BleeckerBorderBrush")}}}\" />"); active.Children.Add(cursor);
        var descriptions = sparklineMode?new List<string>():new List<string> { category };
        foreach (var entry in rendered.OrderBy(item => item.Series.Name, StringComparer.Ordinal))
        {
            var raw = BleeckerChartData.Read(Data[index], entry.Series.Key); if (raw is null) continue;
            var value = BleeckerChartData.Number(raw);
            var point = entry.Points[index];
            if (double.IsFinite(point.X) && double.IsFinite(point.Y))
            {
                var dot = Resource<Ellipse>("<Ellipse Width=\"10\" Height=\"10\" Stroke=\"{ThemeResource BleeckerCardBrush}\" StrokeThickness=\"2\" />"); dot.Fill = entry.Brush;
                var radius=sparklineMode?3:5;if(sparklineMode){dot.Width=dot.Height=6;dot.StrokeThickness=0;}
                Canvas.SetLeft(dot, point.X - radius); Canvas.SetTop(dot, point.Y - radius); active.Children.Add(dot);
            }
            var formatted = sparklineMode&&raw is bool?"":TooltipFormatter is not null && raw is not string && value is double numeric ? TooltipFormatter(numeric) : BleeckerChartData.Label(raw);
            var text = Text(entry.Series.Name + " : " + formatted, 16, "BleeckerTextPrimaryBrush");
            text.LineHeight = 24; text.LineStackingStrategy = LineStackingStrategy.BlockLineHeight; text.Margin = new Thickness(0, 4, 0, 4);
            tooltipItems.Children.Add(text); descriptions.Add(text.Text);
        }
        tooltip.Visibility = Visibility.Visible; tooltipContainer.Visibility = Visibility.Visible; tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var x = BleeckerTooltipPlacement.Position(X(index), tooltip.DesiredSize.Width, left, right);
        var y = BleeckerTooltipPlacement.Position(pointerY ?? (top + ActualHeight) / 2, tooltip.DesiredSize.Height, top, bottom);
        tooltipContainer.Margin = new Thickness(x, y, 0, 0);
        if (tooltipItems.Children.Count == (sparklineMode?0:1)) { tooltip.Visibility = Visibility.Collapsed; tooltipContainer.Visibility = Visibility.Collapsed; }
        UpdateTooltipEffects();
        AutomationProperties.SetHelpText(this, string.Join("; ", descriptions));
    }
    private void ClearActive() { ClearBarCursor(); active?.Children.Clear(); if (tooltip is not null) tooltip.Visibility = Visibility.Collapsed; if (tooltipContainer is not null) tooltipContainer.Visibility = Visibility.Collapsed; ClearTooltipEffects(); AutomationProperties.SetHelpText(this, ""); }
    private TextBlock Text(string? value, double size, string key)
    {
        var text = Resource<TextBlock>($"<TextBlock FontSize=\"{N(size)}\" Foreground=\"{{ThemeResource {key}}}\" UseLayoutRounding=\"False\" HighContrastAdjustment=\"None\" />"); text.FontFamily = FontFamily; text.Text = value ?? ""; return text;
    }
    private static T Resource<T>(string markup) where T : FrameworkElement
    {
        var start = markup.IndexOf(' '); return (T)XamlReader.Load(markup.Insert(start, $" xmlns=\"{Presentation}\""));
    }
    private static void AddText(Canvas canvas, TextBlock text, double x, double y) { Canvas.SetLeft(text, x); Canvas.SetTop(text, y); canvas.Children.Add(text); }
    private static string N(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
    private static PathGeometry Geometry(IReadOnlyList<BleeckerMonotoneCurve.Command> commands)
    {
        var geometry = new PathGeometry(); PathFigure? figure = null;
        foreach (var command in commands)
        {
            var v = command.Values;
            if (command.Kind == "M") { figure = new PathFigure { StartPoint = new Point(v[0], v[1]), IsClosed = false, IsFilled = false }; geometry.Figures.Add(figure); }
            else if (command.Kind == "L") figure!.Segments.Add(new LineSegment { Point = new Point(v[0], v[1]) });
            else if (command.Kind == "C") figure!.Segments.Add(new BezierSegment { Point1 = new Point(v[0], v[1]), Point2 = new Point(v[2], v[3]), Point3 = new Point(v[4], v[5]) });
            else if (command.Kind == "Z") figure!.IsClosed = true;
        }
        return geometry;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new ChartPeer(this);
    private sealed class ChartPeer(BleeckerLineChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => owner.barMode?nameof(BleeckerBarChart):owner.sparklineMode?nameof(BleeckerSparkline):nameof(BleeckerLineChart);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override bool IsControlElementCore() => true;
        protected override bool IsContentElementCore() => true;
    }
}
