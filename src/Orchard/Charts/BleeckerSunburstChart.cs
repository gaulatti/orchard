using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard;

/// <summary>Native sectors and outlined values from an original consumer-owned tree.</summary>
public sealed class BleeckerSunburstChart : Control
{
    private Canvas? plot;
    private bool queued;
    private BleeckerSunburstGeometry.Layout? layout;
    private readonly List<BleeckerSunburstText.Label> labels = [];
    private readonly HashSet<INotifyPropertyChanged> observedRecords = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<INotifyCollectionChanged> observedCollections = new(ReferenceEqualityComparer.Instance);
    private readonly Windows.UI.ViewManagement.UISettings settings = new();
    private bool observingSettings;
    public BleeckerSunburstChart()
    {
        DefaultStyleKey = typeof(BleeckerSunburstChart);
        Loaded += (_, _) => { if (!observingSettings) { settings.TextScaleFactorChanged += OnTextScaleChanged; observingSettings = true; } StopObserving(); Render(); };
        Unloaded += (_, _) => { if (!IsLoaded) { StopObserving(); if (observingSettings) settings.TextScaleFactorChanged -= OnTextScaleChanged; observingSettings = false; } };
        SizeChanged += (_, _) => QueueRender(); ActualThemeChanged += (_, _) => QueueRender();
        foreach (var property in new[] { FontFamilyProperty, FontSizeProperty, FontWeightProperty }) RegisterPropertyChangedCallback(property, (_, _) => QueueRender());
    }
    private static DependencyProperty Property(string name, Type type, object? initial) => DependencyProperty.Register(name, type, typeof(BleeckerSunburstChart), new PropertyMetadata(initial, (owner, _) => { var chart = (BleeckerSunburstChart)owner; chart.StopObserving(); chart.QueueRender(); }));
    public static readonly DependencyProperty DataProperty = Property(nameof(Data), typeof(object), null);
    public static readonly DependencyProperty DataKeyProperty = Property(nameof(DataKey), typeof(string), "value");
    public static readonly DependencyProperty NameKeyProperty = Property(nameof(NameKey), typeof(string), "name");
    public IReadOnlyDictionary<string, object?> Data { get => (IReadOnlyDictionary<string, object?>)GetValue(DataProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(DataProperty, value); } }
    public string DataKey { get => (string)GetValue(DataKeyProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(DataKeyProperty, value); } }
    public string NameKey { get => (string)GetValue(NameKeyProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(NameKeyProperty, value); } }
    protected override void OnApplyTemplate() { base.OnApplyTemplate(); plot = GetTemplateChild("PART_Plot") as Canvas ?? throw new InvalidOperationException("Sunburst requires PART_Plot."); QueueRender(); }
    private void QueueRender() { if (!IsLoaded || queued) return; queued = true; DispatcherQueue.TryEnqueue(() => { queued = false; if (IsLoaded) Render(); }); }
    private void StopObserving()
    {
        foreach (var record in observedRecords) record.PropertyChanged -= OnRecordChanged;
        foreach (var collection in observedCollections) collection.CollectionChanged -= OnCollectionChanged;
        observedRecords.Clear(); observedCollections.Clear();
    }
    private void OnRecordChanged(object? sender, PropertyChangedEventArgs args) => QueueRender();
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) => QueueRender();
    private void OnTextScaleChanged(Windows.UI.ViewManagement.UISettings sender, object args) => DispatcherQueue.TryEnqueue(QueueRender);
    private void Render()
    {
        if (!IsLoaded || plot is null || ActualWidth <= 0 || ActualHeight <= 0) return;
        if (Data is null || DataKey is null || NameKey is null) throw new InvalidOperationException("Mounted Sunburst requires Data, DataKey and NameKey.");
        var width = Math.Round(ActualWidth, MidpointRounding.AwayFromZero); var height = Math.Round(ActualHeight, MidpointRounding.AwayFromZero);
        layout = BleeckerSunburstGeometry.Compute(Data, DataKey, width, height); StopObserving();
        foreach (var node in layout.Nodes)
        {
            if (node.Record is INotifyPropertyChanged record && observedRecords.Add(record)) record.PropertyChanged += OnRecordChanged;
            if (node.ChildSource is INotifyCollectionChanged collection && observedCollections.Add(collection)) collection.CollectionChanged += OnCollectionChanged;
        }
        plot.Children.Clear(); plot.Clip = new RectangleGeometry { Rect = new Rect(0, 0, width, height) }; labels.Clear();
        var family = FontFamily.Source; var weight = FontWeight.Weight;
        if (family.EndsWith("/EncodeSans-Regular.ttf#Encode Sans", StringComparison.Ordinal))
        {
            var resource = weight < 450 ? "BleeckerPrimaryFontFamily" : weight < 550 ? "BleeckerPrimaryMediumFontFamily" : "BleeckerPrimarySemiboldFontFamily";
            family = Element<TextBlock>($"<TextBlock FontFamily=\"{{StaticResource {resource}}}\" />").FontFamily.Source;
            weight = (ushort)(weight < 450 ? 400 : weight < 550 ? 500 : 600);
        }
        using var textStyle = layout.Entries.Any(entry => entry.HasLabel) ? new BleeckerChartTextStyle(family, (float)(FontSize * settings.TextScaleFactor), weight) : null;
        for (var i = 0; i < layout.Entries.Length; i++)
        {
            var entry = layout.Entries[i];
            if (entry.Path(layout.CenterX, layout.CenterY) is string data)
            {
                var path = Element<Path>($"<Path Data=\"{data}\" Fill=\"{{ThemeResource {BleeckerChartTheme.PickColor(entry.Node.Sibling)}}}\" Stroke=\"{{ThemeResource BleeckerSunburstSectorStrokeBrush}}\" StrokeThickness=\"2\" />");
                ((PathGeometry)path.Data).FillRule = FillRule.Nonzero;
                var fill = BleeckerSunburstGeometry.Read(entry.Node.Record, "fill"); if (fill is not null) path.Fill = fill as Brush ?? throw new InvalidOperationException("Sunburst fill requires a native Brush.");
                path.Tag = ("sunburst-sector", i); plot.Children.Add(path);
            }
            if (entry.HasLabel)
            {
                var label = BleeckerSunburstText.Create(entry.Label, textStyle!, entry.LabelX, entry.LabelY); labels.Add(label);
                var stroke = Element<Path>("<Path Stroke=\"{ThemeResource BleeckerSunburstTextOutlineBrush}\" StrokeThickness=\"1\" IsHitTestVisible=\"False\" />"); stroke.Data = label.OutlineGeometry; stroke.Tag = ("sunburst-label-outline", i); plot.Children.Add(stroke);
                var ink = Element<Path>("<Path Fill=\"{ThemeResource BleeckerSunburstTextBrush}\" IsHitTestVisible=\"False\" />"); ink.Data = label.Geometry; ink.Tag = ("sunburst-label", i); plot.Children.Add(ink);
            }
        }
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private static T Element<T>(string markup) where T : FrameworkElement => (T)XamlReader.Load(markup.Insert(markup.IndexOf(' '), " xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\""));
    protected override AutomationPeer OnCreateAutomationPeer() => new SunburstPeer(this);
    private sealed class SunburstPeer(BleeckerSunburstChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerSunburstChart);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;
        protected override bool IsKeyboardFocusableCore() => false;
        protected override bool IsControlElementCore() => true;
        protected override bool IsContentElementCore() => true;
        protected override string GetHelpTextCore()
        {
            if (owner.layout is not { } current) return "";
            string Name(BleeckerSunburstGeometry.Node node) => BleeckerSunburstGeometry.String(BleeckerSunburstGeometry.Read(node.Record, owner.NameKey));
            return string.Join("; ", current.Entries.Select(entry =>
            {
                var node = current.Root; var names = new List<string> { Name(node) };
                foreach (var index in entry.Node.Path) { node = node.Children[index]; names.Add(Name(node)); }
                return string.Join(" > ", names.Where(name => name.Length > 0)) + ": " + entry.Label;
            }));
        }
    }
}
