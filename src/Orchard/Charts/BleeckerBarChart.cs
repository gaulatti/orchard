using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Orchard;

/// <summary>The source layout names: Horizontal draws vertical columns; Vertical draws horizontal bars.</summary>
public enum BleeckerBarChartLayout { Horizontal, Vertical }

/// <summary>Consumer-owned grouped bars with source axes, palette and native category inspection.</summary>
public sealed class BleeckerBarChart : Control
{
    private BleeckerLineChart? chart;
    public BleeckerBarChart()
    {
        DefaultStyleKey = typeof(BleeckerBarChart);
        Data = new(); Series = new();
        Loaded += (_, _) => Synchronize();
        RegisterPropertyChangedCallback(AutomationProperties.NameProperty, (_, _) => Synchronize());
        RegisterPropertyChangedCallback(FontFamilyProperty, (_, _) => Synchronize());
    }
    private static DependencyProperty Property(string name, Type type, object? initial) => DependencyProperty.Register(
        name, type, typeof(BleeckerBarChart), new PropertyMetadata(initial, (owner, _) => ((BleeckerBarChart)owner).Synchronize()));
    public static readonly DependencyProperty DataProperty = Property(nameof(Data), typeof(object), null);
    public static readonly DependencyProperty SeriesProperty = Property(nameof(Series), typeof(object), null);
    public static readonly DependencyProperty XAxisKeyProperty = Property(nameof(XAxisKey), typeof(string), null);
    public static readonly DependencyProperty LayoutProperty = Property(nameof(Layout), typeof(BleeckerBarChartLayout), BleeckerBarChartLayout.Horizontal);
    public static readonly DependencyProperty ShowGridProperty = Property(nameof(ShowGrid), typeof(bool), true);
    public static readonly DependencyProperty ShowLegendProperty = Property(nameof(ShowLegend), typeof(bool), true);
    public static readonly DependencyProperty YAxisTickFormatterProperty = Property(nameof(YAxisTickFormatter), typeof(object), null);
    public static readonly DependencyProperty TooltipFormatterProperty = Property(nameof(TooltipFormatter), typeof(object), null);
    public ObservableCollection<IReadOnlyDictionary<string, object?>> Data { get => (ObservableCollection<IReadOnlyDictionary<string, object?>>)GetValue(DataProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(DataProperty, value); } }
    public ObservableCollection<BleeckerChartSeries> Series { get => (ObservableCollection<BleeckerChartSeries>)GetValue(SeriesProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(SeriesProperty, value); } }
    public string? XAxisKey { get => (string?)GetValue(XAxisKeyProperty); set => SetValue(XAxisKeyProperty, value); }
    public BleeckerBarChartLayout Layout { get => (BleeckerBarChartLayout)GetValue(LayoutProperty); set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(LayoutProperty, value); } }
    public bool ShowGrid { get => (bool)GetValue(ShowGridProperty); set => SetValue(ShowGridProperty, value); }
    public bool ShowLegend { get => (bool)GetValue(ShowLegendProperty); set => SetValue(ShowLegendProperty, value); }
    /// <summary>Receives numeric values for Horizontal, and original categories for Vertical, as in the rendered source.</summary>
    public Func<object?, string>? YAxisTickFormatter { get => (Func<object?, string>?)GetValue(YAxisTickFormatterProperty); set => SetValue(YAxisTickFormatterProperty, value); }
    public Func<double, string>? TooltipFormatter { get => (Func<double, string>?)GetValue(TooltipFormatterProperty); set => SetValue(TooltipFormatterProperty, value); }
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate(); chart = GetTemplateChild("PART_Chart") as BleeckerLineChart; Synchronize();
    }
    private void Synchronize()
    {
        if (!Enum.IsDefined(Layout)) throw new ArgumentOutOfRangeException(nameof(Layout));
        if (IsLoaded && (Data is null || Series is null || XAxisKey is null)) throw new InvalidOperationException("A mounted Bar Chart requires Data, Series and XAxisKey.");
        if (chart is null) return;
        chart.UseBarProfile(Layout, YAxisTickFormatter); chart.Height = double.NaN;
        chart.ShowGrid = ShowGrid; chart.ShowLegend = ShowLegend; chart.FontFamily = FontFamily; chart.TooltipFormatter = TooltipFormatter;
        AutomationProperties.SetName(chart, AutomationProperties.GetName(this));
        if (Data is null || Series is null || XAxisKey is null) return;
        chart.XAxisKey = XAxisKey;
        if (!ReferenceEquals(chart.Data, Data)) chart.Data = Data;
        if (!ReferenceEquals(chart.Series, Series)) chart.Series = Series;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new BarPeer(this);
    private sealed class BarPeer(BleeckerBarChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerBarChart);
        protected override bool IsControlElementCore() => false;
        protected override bool IsContentElementCore() => false;
    }
}
