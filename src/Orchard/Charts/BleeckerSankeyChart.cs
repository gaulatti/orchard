using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>Native Sankey nodes and cubic flows driven by original consumer data.</summary>
public sealed class BleeckerSankeyChart : Control
{
    private BleeckerLineChart? chart;
    public BleeckerSankeyChart()
    {
        DefaultStyleKey = typeof(BleeckerSankeyChart); Data = new(); Loaded += (_, _) => Synchronize();
        RegisterPropertyChangedCallback(AutomationProperties.NameProperty, (_, _) => Synchronize());
        RegisterPropertyChangedCallback(FontFamilyProperty, (_, _) => Synchronize());
    }
    private static DependencyProperty Property(string name, Type type, object? initial = null) => DependencyProperty.Register(name, type, typeof(BleeckerSankeyChart), new PropertyMetadata(initial, (owner, _) => ((BleeckerSankeyChart)owner).Synchronize()));
    public static readonly DependencyProperty DataProperty = Property(nameof(Data), typeof(BleeckerSankeyChartData));
    public static readonly DependencyProperty NodeColorsProperty = Property(nameof(NodeColors), typeof(object));
    public static readonly DependencyProperty NodeWidthProperty = Property(nameof(NodeWidth), typeof(double), 12d);
    public static readonly DependencyProperty LinkCurvatureProperty = Property(nameof(LinkCurvature), typeof(double), .5);
    public static readonly DependencyProperty LinkOpacityProperty = Property(nameof(LinkOpacity), typeof(double), .35);
    public static readonly DependencyProperty NodeRadiusProperty = Property(nameof(NodeRadius), typeof(double), 4d);
    public BleeckerSankeyChartData Data { get => (BleeckerSankeyChartData)GetValue(DataProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(DataProperty, value); } }
    public ObservableCollection<Brush>? NodeColors { get => (ObservableCollection<Brush>?)GetValue(NodeColorsProperty); set => SetValue(NodeColorsProperty, value); }
    public double NodeWidth { get => (double)GetValue(NodeWidthProperty); set => Finite(NodeWidthProperty, value); }
    public double LinkCurvature { get => (double)GetValue(LinkCurvatureProperty); set => Finite(LinkCurvatureProperty, value); }
    public double LinkOpacity { get => (double)GetValue(LinkOpacityProperty); set => Finite(LinkOpacityProperty, value); }
    public double NodeRadius { get => (double)GetValue(NodeRadiusProperty); set => Finite(NodeRadiusProperty, value); }
    private void Finite(DependencyProperty property, double value) { if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(property, value); }
    protected override void OnApplyTemplate() { base.OnApplyTemplate(); chart = GetTemplateChild("PART_Chart") as BleeckerLineChart; Synchronize(); }
    private void Synchronize()
    {
        if (IsLoaded && Data is null) throw new InvalidOperationException("A mounted Sankey Chart requires Data.");
        if (chart is null || Data is null) return;
        chart.UseSankeyProfile(Data, NodeColors, NodeWidth, LinkCurvature, LinkOpacity, NodeRadius); chart.Height = double.NaN;
        chart.FontFamily = FontFamily; AutomationProperties.SetName(chart, AutomationProperties.GetName(this));
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new SankeyPeer(this);
    private sealed class SankeyPeer(BleeckerSankeyChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerSankeyChart);
        protected override bool IsControlElementCore() => false;
        protected override bool IsContentElementCore() => false;
    }
}
