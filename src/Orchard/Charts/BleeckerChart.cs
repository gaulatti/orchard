using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Orchard;

/// <summary>The fixed currency Chart: original name/total records and a desert monotone line.</summary>
public sealed class BleeckerChart : Control
{
    private BleeckerLineChart? chart;
    public BleeckerChart()
    {
        DefaultStyleKey = typeof(BleeckerChart); Data = [];
        Loaded += (_, _) => Synchronize();
        RegisterPropertyChangedCallback(AutomationProperties.NameProperty, (_, _) => Synchronize());
        RegisterPropertyChangedCallback(FontFamilyProperty, (_, _) => Synchronize());
    }
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(nameof(Data), typeof(object), typeof(BleeckerChart), new PropertyMetadata(null, (owner, _) => ((BleeckerChart)owner).Synchronize()));
    /// <summary>Original consumer records; null and primitive rows retain the source's missing-field behavior.</summary>
    public ObservableCollection<object?> Data { get => (ObservableCollection<object?>)GetValue(DataProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(DataProperty, value); } }
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        chart = GetTemplateChild("PART_Chart") as BleeckerLineChart ?? throw new InvalidOperationException("Chart requires PART_Chart.");
        Synchronize();
    }
    private void Synchronize()
    {
        if (IsLoaded && Data is null) throw new InvalidOperationException("A mounted Chart requires Data.");
        if (chart is null || Data is null) return;
        chart.UseCurrencyProfile(Data); chart.Height = double.NaN; chart.FontFamily = FontFamily;
        AutomationProperties.SetName(chart, AutomationProperties.GetName(this));
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new CurrencyPeer(this);
    private sealed class CurrencyPeer(BleeckerChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerChart);
        protected override bool IsControlElementCore() => false;
        protected override bool IsContentElementCore() => false;
    }
}
