using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Orchard;

/// <summary>Consumer-owned pie or ring sectors with native category inspection.</summary>
public sealed class BleeckerPieChart : Control
{
    private BleeckerLineChart? chart;
    private bool donutIdentity;
    public BleeckerPieChart()
    {
        DefaultStyleKey = typeof(BleeckerPieChart); Data = new(); Loaded += (_, _) => Synchronize();
        RegisterPropertyChangedCallback(AutomationProperties.NameProperty, (_, _) => Synchronize());
        RegisterPropertyChangedCallback(FontFamilyProperty, (_, _) => Synchronize());
    }
    private static DependencyProperty Property(string name, Type type, object? initial) => DependencyProperty.Register(name, type, typeof(BleeckerPieChart), new PropertyMetadata(initial, (owner, _) => ((BleeckerPieChart)owner).Synchronize()));
    public static readonly DependencyProperty DataProperty = Property(nameof(Data), typeof(object), null);
    public static readonly DependencyProperty NameKeyProperty = Property(nameof(NameKey), typeof(string), "name");
    public static readonly DependencyProperty DataKeyProperty = Property(nameof(DataKey), typeof(string), "value");
    public static readonly DependencyProperty InnerRadiusProperty = Property(nameof(InnerRadius), typeof(object), 0d);
    public static readonly DependencyProperty ShowLegendProperty = Property(nameof(ShowLegend), typeof(bool), true);
    public ObservableCollection<IReadOnlyDictionary<string, object?>> Data { get => (ObservableCollection<IReadOnlyDictionary<string, object?>>)GetValue(DataProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(DataProperty, value); } }
    public string NameKey { get => (string)GetValue(NameKeyProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(NameKeyProperty, value); } }
    public string DataKey { get => (string)GetValue(DataKeyProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(DataKeyProperty, value); } }
    public object InnerRadius { get => GetValue(InnerRadiusProperty); set { ArgumentNullException.ThrowIfNull(value); if (value is not string && BleeckerChartData.Number(value) is null) throw new ArgumentException("InnerRadius requires a finite number or string.", nameof(value)); SetValue(InnerRadiusProperty, value); } }
    public bool ShowLegend { get => (bool)GetValue(ShowLegendProperty); set => SetValue(ShowLegendProperty, value); }
    internal void UseDonutIdentity() { donutIdentity = true; Synchronize(); }
    protected override void OnApplyTemplate() { base.OnApplyTemplate(); chart = GetTemplateChild("PART_Chart") as BleeckerLineChart; Synchronize(); }
    private void Synchronize()
    {
        if (IsLoaded && (Data is null || NameKey is null || DataKey is null || InnerRadius is null)) throw new InvalidOperationException("A mounted Pie Chart requires Data, NameKey, DataKey and InnerRadius.");
        if (chart is null) return;
        chart.UsePieProfile(NameKey, DataKey, InnerRadius, donutIdentity); chart.Height = double.NaN; chart.ShowLegend = ShowLegend; chart.FontFamily = FontFamily;
        AutomationProperties.SetName(chart, AutomationProperties.GetName(this));
        if (Data is not null && !ReferenceEquals(chart.Data, Data)) chart.Data = Data;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new PiePeer(this);
    private sealed class PiePeer(BleeckerPieChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerPieChart);
        protected override bool IsControlElementCore() => false;
        protected override bool IsContentElementCore() => false;
    }
}
