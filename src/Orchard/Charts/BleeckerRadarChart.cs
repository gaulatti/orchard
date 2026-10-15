using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Orchard;

/// <summary>Polar subject plots over original consumer-owned data and series.</summary>
public sealed class BleeckerRadarChart : Control
{
    private BleeckerLineChart? chart;
    public BleeckerRadarChart()
    {
        DefaultStyleKey = typeof(BleeckerRadarChart); Data = new(); Series = new(); Loaded += (_, _) => Synchronize();
        RegisterPropertyChangedCallback(AutomationProperties.NameProperty, (_, _) => Synchronize());
        RegisterPropertyChangedCallback(FontFamilyProperty, (_, _) => Synchronize());
    }
    private static DependencyProperty Property(string name, Type type, object? initial = null) => DependencyProperty.Register(name, type, typeof(BleeckerRadarChart),
        new PropertyMetadata(initial, (owner, _) => ((BleeckerRadarChart)owner).Synchronize()));
    public static readonly DependencyProperty DataProperty = Property(nameof(Data), typeof(object));
    public static readonly DependencyProperty SeriesProperty = Property(nameof(Series), typeof(object));
    public static readonly DependencyProperty SubjectKeyProperty = Property(nameof(SubjectKey), typeof(string));
    public static readonly DependencyProperty ShowLegendProperty = Property(nameof(ShowLegend), typeof(bool), true);
    public static readonly DependencyProperty TickFormatterProperty = Property(nameof(TickFormatter), typeof(object));
    public ObservableCollection<IReadOnlyDictionary<string, object?>> Data { get => (ObservableCollection<IReadOnlyDictionary<string, object?>>)GetValue(DataProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(DataProperty, value); } }
    public ObservableCollection<BleeckerRadarSeries> Series { get => (ObservableCollection<BleeckerRadarSeries>)GetValue(SeriesProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(SeriesProperty, value); } }
    public string SubjectKey { get => (string?)GetValue(SubjectKeyProperty) ?? throw new InvalidOperationException("Radar SubjectKey must be supplied before mounting."); set { ArgumentNullException.ThrowIfNull(value); SetValue(SubjectKeyProperty, value); } }
    public bool ShowLegend { get => (bool)GetValue(ShowLegendProperty); set => SetValue(ShowLegendProperty, value); }
    public Func<double, string>? TickFormatter { get => (Func<double, string>?)GetValue(TickFormatterProperty); set => SetValue(TickFormatterProperty, value); }
    protected override void OnApplyTemplate() { base.OnApplyTemplate(); chart = GetTemplateChild("PART_Chart") as BleeckerLineChart; Synchronize(); }
    private void Synchronize()
    {
        var subject = (string?)GetValue(SubjectKeyProperty);
        if (IsLoaded && (Data is null || Series is null || subject is null)) throw new InvalidOperationException("A mounted Radar Chart requires Data, Series and SubjectKey.");
        if (chart is null || Data is null || Series is null || subject is null) return;
        chart.UseRadarProfile(Series); chart.Height = double.NaN; chart.XAxisKey = subject; chart.ShowLegend = ShowLegend;
        chart.FontFamily = FontFamily; chart.YAxisTickFormatter = TickFormatter; chart.TooltipFormatter = TickFormatter;
        AutomationProperties.SetName(chart, AutomationProperties.GetName(this));
        if (!ReferenceEquals(chart.Data, Data)) chart.Data = Data;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new RadarPeer(this);
    private sealed class RadarPeer(BleeckerRadarChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerRadarChart);
        protected override bool IsControlElementCore() => false;
        protected override bool IsContentElementCore() => false;
    }
}
