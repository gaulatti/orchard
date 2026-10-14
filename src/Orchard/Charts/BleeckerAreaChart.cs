using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Orchard;

/// <summary>Consumer-owned overlapping or stacked monotone areas with native category inspection.</summary>
public sealed class BleeckerAreaChart : Control
{
    private BleeckerLineChart? chart;
    public BleeckerAreaChart()
    {
        DefaultStyleKey = typeof(BleeckerAreaChart);
        Data = new(); Series = new();
        Loaded += (_, _) => Synchronize();
        RegisterPropertyChangedCallback(AutomationProperties.NameProperty, (_, _) => Synchronize());
        RegisterPropertyChangedCallback(FontFamilyProperty, (_, _) => Synchronize());
    }
    private static DependencyProperty Property(string name, Type type, object? initial) => DependencyProperty.Register(
        name, type, typeof(BleeckerAreaChart), new PropertyMetadata(initial, (owner, _) => ((BleeckerAreaChart)owner).Synchronize()));
    public static readonly DependencyProperty DataProperty = Property(nameof(Data), typeof(object), null);
    public static readonly DependencyProperty SeriesProperty = Property(nameof(Series), typeof(object), null);
    public static readonly DependencyProperty XAxisKeyProperty = Property(nameof(XAxisKey), typeof(string), null);
    public static readonly DependencyProperty StackedProperty = Property(nameof(Stacked), typeof(bool), false);
    public static readonly DependencyProperty ShowGridProperty = Property(nameof(ShowGrid), typeof(bool), true);
    public static readonly DependencyProperty ShowLegendProperty = Property(nameof(ShowLegend), typeof(bool), true);
    public static readonly DependencyProperty YAxisTickFormatterProperty = Property(nameof(YAxisTickFormatter), typeof(object), null);
    public static readonly DependencyProperty TooltipFormatterProperty = Property(nameof(TooltipFormatter), typeof(object), null);
    public ObservableCollection<IReadOnlyDictionary<string, object?>> Data { get => (ObservableCollection<IReadOnlyDictionary<string, object?>>)GetValue(DataProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(DataProperty, value); } }
    public ObservableCollection<BleeckerChartSeries> Series { get => (ObservableCollection<BleeckerChartSeries>)GetValue(SeriesProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(SeriesProperty, value); } }
    public string? XAxisKey { get => (string?)GetValue(XAxisKeyProperty); set => SetValue(XAxisKeyProperty, value); }
    public bool Stacked { get => (bool)GetValue(StackedProperty); set => SetValue(StackedProperty, value); }
    public bool ShowGrid { get => (bool)GetValue(ShowGridProperty); set => SetValue(ShowGridProperty, value); }
    public bool ShowLegend { get => (bool)GetValue(ShowLegendProperty); set => SetValue(ShowLegendProperty, value); }
    public Func<double, string>? YAxisTickFormatter { get => (Func<double, string>?)GetValue(YAxisTickFormatterProperty); set => SetValue(YAxisTickFormatterProperty, value); }
    public Func<double, string>? TooltipFormatter { get => (Func<double, string>?)GetValue(TooltipFormatterProperty); set => SetValue(TooltipFormatterProperty, value); }
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate(); chart = GetTemplateChild("PART_Chart") as BleeckerLineChart; Synchronize();
    }
    private void Synchronize()
    {
        if (IsLoaded && (Data is null || Series is null || XAxisKey is null)) throw new InvalidOperationException("A mounted Area Chart requires Data, Series and XAxisKey.");
        if (chart is null) return;
        chart.UseAreaProfile(Stacked); chart.YAxisTickFormatter = YAxisTickFormatter; chart.Height = double.NaN;
        chart.ShowGrid = ShowGrid; chart.ShowLegend = ShowLegend; chart.FontFamily = FontFamily; chart.TooltipFormatter = TooltipFormatter;
        AutomationProperties.SetName(chart, AutomationProperties.GetName(this));
        if (Data is null || Series is null || XAxisKey is null) return;
        chart.XAxisKey = XAxisKey;
        if (!ReferenceEquals(chart.Data, Data)) chart.Data = Data;
        if (!ReferenceEquals(chart.Series, Series)) chart.Series = Series;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new AreaPeer(this);
    private sealed class AreaPeer(BleeckerAreaChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerAreaChart);
        protected override bool IsControlElementCore() => false;
        protected override bool IsContentElementCore() => false;
    }
}
