using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Orchard;

/// <summary>Consumer-owned funnel records, inside labels and native inspection.</summary>
public sealed class BleeckerFunnelChart : Control
{
    private BleeckerLineChart? chart;
    public BleeckerFunnelChart()
    {
        DefaultStyleKey = typeof(BleeckerFunnelChart); Data = new(); Loaded += (_, _) => Synchronize();
        RegisterPropertyChangedCallback(AutomationProperties.NameProperty, (_, _) => Synchronize());
        RegisterPropertyChangedCallback(FontFamilyProperty, (_, _) => Synchronize());
    }
    private static DependencyProperty Property(string name, Type type, object? initial = null) => DependencyProperty.Register(name, type, typeof(BleeckerFunnelChart), new PropertyMetadata(initial, (owner, _) => ((BleeckerFunnelChart)owner).Synchronize()));
    public static readonly DependencyProperty DataProperty = Property(nameof(Data), typeof(object));
    public static readonly DependencyProperty ShowLabelsProperty = Property(nameof(ShowLabels), typeof(bool), true);
    public static readonly DependencyProperty ValueFormatterProperty = Property(nameof(ValueFormatter), typeof(object));
    public ObservableCollection<IReadOnlyDictionary<string, object?>> Data { get => (ObservableCollection<IReadOnlyDictionary<string, object?>>)GetValue(DataProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(DataProperty, value); } }
    public bool ShowLabels { get => (bool)GetValue(ShowLabelsProperty); set => SetValue(ShowLabelsProperty, value); }
    public Func<double, string>? ValueFormatter { get => (Func<double, string>?)GetValue(ValueFormatterProperty); set => SetValue(ValueFormatterProperty, value); }
    protected override void OnApplyTemplate() { base.OnApplyTemplate(); chart = GetTemplateChild("PART_Chart") as BleeckerLineChart; Synchronize(); }
    private void Synchronize()
    {
        if (IsLoaded && Data is null) throw new InvalidOperationException("A mounted Funnel Chart requires Data.");
        if (chart is null || Data is null) return;
        chart.UseFunnelProfile(ShowLabels); chart.Height = double.NaN; chart.TooltipFormatter = ValueFormatter;
        chart.FontFamily = FontFamily; AutomationProperties.SetName(chart, AutomationProperties.GetName(this));
        if (!ReferenceEquals(chart.Data, Data)) chart.Data = Data;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new FunnelPeer(this);
    private sealed class FunnelPeer(BleeckerFunnelChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerFunnelChart);
        protected override bool IsControlElementCore() => false;
        protected override bool IsContentElementCore() => false;
    }
}
