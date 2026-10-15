using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Orchard;

/// <summary>Numeric scatter/bubble plots over original consumer-owned collections.</summary>
public sealed class BleeckerScatterChart : Control
{
    private BleeckerLineChart? chart;
    public BleeckerScatterChart()
    {
        DefaultStyleKey = typeof(BleeckerScatterChart); Data = new(); Series = new(); Loaded += (_, _) => Synchronize();
        RegisterPropertyChangedCallback(AutomationProperties.NameProperty, (_, _) => Synchronize());
        RegisterPropertyChangedCallback(FontFamilyProperty, (_, _) => Synchronize());
    }
    private static DependencyProperty Property(string name, Type type, object? initial) => DependencyProperty.Register(name, type, typeof(BleeckerScatterChart), new PropertyMetadata(initial, (owner, _) => ((BleeckerScatterChart)owner).Synchronize()));
    public static readonly DependencyProperty DataProperty = Property(nameof(Data), typeof(object), null);
    public static readonly DependencyProperty SeriesProperty = Property(nameof(Series), typeof(object), null);
    public static readonly DependencyProperty ShowGridProperty = Property(nameof(ShowGrid), typeof(bool), true);
    public static readonly DependencyProperty ShowLegendProperty = Property(nameof(ShowLegend), typeof(bool), true);
    public static readonly DependencyProperty XAxisLabelProperty = Property(nameof(XAxisLabel), typeof(string), null);
    public static readonly DependencyProperty YAxisLabelProperty = Property(nameof(YAxisLabel), typeof(string), null);
    public static readonly DependencyProperty XAxisTickFormatterProperty = Property(nameof(XAxisTickFormatter), typeof(object), null);
    public static readonly DependencyProperty YAxisTickFormatterProperty = Property(nameof(YAxisTickFormatter), typeof(object), null);
    public static readonly DependencyProperty TooltipFormatterProperty = Property(nameof(TooltipFormatter), typeof(object), null);
    public ObservableCollection<IReadOnlyDictionary<string, object?>> Data { get => (ObservableCollection<IReadOnlyDictionary<string, object?>>)GetValue(DataProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(DataProperty, value); } }
    public ObservableCollection<BleeckerScatterSeries> Series { get => (ObservableCollection<BleeckerScatterSeries>)GetValue(SeriesProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(SeriesProperty, value); } }
    public bool ShowGrid { get => (bool)GetValue(ShowGridProperty); set => SetValue(ShowGridProperty, value); }
    public bool ShowLegend { get => (bool)GetValue(ShowLegendProperty); set => SetValue(ShowLegendProperty, value); }
    public string? XAxisLabel { get => (string?)GetValue(XAxisLabelProperty); set => SetValue(XAxisLabelProperty, value); }
    public string? YAxisLabel { get => (string?)GetValue(YAxisLabelProperty); set => SetValue(YAxisLabelProperty, value); }
    public Func<object?, string>? XAxisTickFormatter { get => (Func<object?, string>?)GetValue(XAxisTickFormatterProperty); set => SetValue(XAxisTickFormatterProperty, value); }
    public Func<object?, string>? YAxisTickFormatter { get => (Func<object?, string>?)GetValue(YAxisTickFormatterProperty); set => SetValue(YAxisTickFormatterProperty, value); }
    public Func<double, string>? TooltipFormatter { get => (Func<double, string>?)GetValue(TooltipFormatterProperty); set => SetValue(TooltipFormatterProperty, value); }
    protected override void OnApplyTemplate() { base.OnApplyTemplate(); chart = GetTemplateChild("PART_Chart") as BleeckerLineChart; Synchronize(); }
    private void Synchronize()
    {
        if (IsLoaded && (Data is null || Series is null)) throw new InvalidOperationException("A mounted Scatter Chart requires Data and Series.");
        if (chart is null || Data is null || Series is null) return;
        chart.UseScatterProfile(Series, XAxisTickFormatter, YAxisTickFormatter); chart.Height = double.NaN;
        chart.ShowGrid = ShowGrid; chart.ShowLegend = ShowLegend; chart.FontFamily = FontFamily; chart.XAxisLabel = XAxisLabel; chart.YAxisLabel = YAxisLabel; chart.TooltipFormatter = TooltipFormatter;
        AutomationProperties.SetName(chart, AutomationProperties.GetName(this));
        if (!ReferenceEquals(chart.Data, Data)) chart.Data = Data;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new ScatterPeer(this);
    private sealed class ScatterPeer(BleeckerScatterChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerScatterChart);
        protected override bool IsControlElementCore() => false;
        protected override bool IsContentElementCore() => false;
    }
}
