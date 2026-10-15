using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Orchard;

/// <summary>Concentric native bars, curved values and original consumer records.</summary>
public sealed class BleeckerRadialBarChart : Control
{
    private BleeckerLineChart? chart;
    public BleeckerRadialBarChart()
    {
        DefaultStyleKey = typeof(BleeckerRadialBarChart); Data = new(); Loaded += (_, _) => Synchronize();
        RegisterPropertyChangedCallback(AutomationProperties.NameProperty, (_, _) => Synchronize());
        RegisterPropertyChangedCallback(FontFamilyProperty, (_, _) => Synchronize());
    }
    private static DependencyProperty Property(string name, Type type, object? initial = null) => DependencyProperty.Register(name, type, typeof(BleeckerRadialBarChart), new PropertyMetadata(initial, (owner, _) => ((BleeckerRadialBarChart)owner).Synchronize()));
    public static readonly DependencyProperty DataProperty = Property(nameof(Data), typeof(object));
    public static readonly DependencyProperty InnerRadiusProperty = Property(nameof(InnerRadius), typeof(object), "10%");
    public static readonly DependencyProperty OuterRadiusProperty = Property(nameof(OuterRadius), typeof(object), "80%");
    public static readonly DependencyProperty ShowLegendProperty = Property(nameof(ShowLegend), typeof(bool), true);
    public static readonly DependencyProperty StartAngleProperty = Property(nameof(StartAngle), typeof(double), 90d);
    public static readonly DependencyProperty EndAngleProperty = Property(nameof(EndAngle), typeof(double), -270d);
    public ObservableCollection<IReadOnlyDictionary<string, object?>> Data { get => (ObservableCollection<IReadOnlyDictionary<string, object?>>)GetValue(DataProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(DataProperty, value); } }
    public object InnerRadius { get => GetValue(InnerRadiusProperty); set { ValidateRadius(value); SetValue(InnerRadiusProperty, value); } }
    public object OuterRadius { get => GetValue(OuterRadiusProperty); set { ValidateRadius(value); SetValue(OuterRadiusProperty, value); } }
    public bool ShowLegend { get => (bool)GetValue(ShowLegendProperty); set => SetValue(ShowLegendProperty, value); }
    public double StartAngle { get => (double)GetValue(StartAngleProperty); set { if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(StartAngleProperty, value); } }
    public double EndAngle { get => (double)GetValue(EndAngleProperty); set { if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(EndAngleProperty, value); } }
    private static void ValidateRadius(object value) { ArgumentNullException.ThrowIfNull(value); if (value is not string && BleeckerChartData.Number(value) is null) throw new ArgumentException("Radius requires a finite number or string.", nameof(value)); }
    protected override void OnApplyTemplate() { base.OnApplyTemplate(); chart = GetTemplateChild("PART_Chart") as BleeckerLineChart; Synchronize(); }
    private void Synchronize()
    {
        if (IsLoaded && Data is null) throw new InvalidOperationException("A mounted Radial Bar Chart requires Data.");
        if (chart is null || Data is null) return;
        chart.UseRadialBarProfile(InnerRadius, OuterRadius, StartAngle, EndAngle); chart.Height = double.NaN; chart.ShowLegend = ShowLegend;
        chart.FontFamily = FontFamily; AutomationProperties.SetName(chart, AutomationProperties.GetName(this));
        if (!ReferenceEquals(chart.Data, Data)) chart.Data = Data;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new RadialPeer(this);
    private sealed class RadialPeer(BleeckerRadialBarChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerRadialBarChart);
        protected override bool IsControlElementCore() => false;
        protected override bool IsContentElementCore() => false;
    }
}
