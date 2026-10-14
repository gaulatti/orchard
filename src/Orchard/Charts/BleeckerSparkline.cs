using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>A consumer-owned compact monotone chart with native inspection.</summary>
public sealed class BleeckerSparkline:Control
{
    private BleeckerLineChart? chart;
    private readonly BleeckerChartSeries series=new(){Key="",Name=""};
    public BleeckerSparkline()
    {
        DefaultStyleKey=typeof(BleeckerSparkline);Data=new ObservableCollection<IReadOnlyDictionary<string,object?>>();
        Loaded+=(_,_)=>Synchronize();
        RegisterPropertyChangedCallback(AutomationProperties.NameProperty,(_,_)=>Synchronize());
        RegisterPropertyChangedCallback(FontFamilyProperty,(_,_)=>Synchronize());
    }
    private static DependencyProperty Property(string name,Type type,object? value)=>DependencyProperty.Register(name,type,typeof(BleeckerSparkline),new PropertyMetadata(value,(owner,_)=>((BleeckerSparkline)owner).Synchronize()));
    public static readonly DependencyProperty DataProperty=Property(nameof(Data),typeof(object),null);
    public static readonly DependencyProperty DataKeyProperty=Property(nameof(DataKey),typeof(string),null);
    public static readonly DependencyProperty ColorProperty=Property(nameof(Color),typeof(SolidColorBrush),null);
    public static readonly DependencyProperty ShowAreaProperty=Property(nameof(ShowArea),typeof(bool),false);
    public ObservableCollection<IReadOnlyDictionary<string,object?>> Data{get=>(ObservableCollection<IReadOnlyDictionary<string,object?>>)GetValue(DataProperty);set{ArgumentNullException.ThrowIfNull(value);SetValue(DataProperty,value);}}
    public string? DataKey{get=>(string?)GetValue(DataKeyProperty);set=>SetValue(DataKeyProperty,value);}
    public SolidColorBrush? Color{get=>(SolidColorBrush?)GetValue(ColorProperty);set=>SetValue(ColorProperty,value);}
    /// <summary>Retains the source option. Its pinned Recharts Line still paints no fill.</summary>
    public bool ShowArea{get=>(bool)GetValue(ShowAreaProperty);set=>SetValue(ShowAreaProperty,value);}
    protected override void OnApplyTemplate(){base.OnApplyTemplate();chart=GetTemplateChild("PART_Chart") as BleeckerLineChart;Synchronize();}
    private void Synchronize()
    {
        if(IsLoaded&&(Data is null||DataKey is null))throw new InvalidOperationException("A mounted Sparkline requires Data and DataKey.");
        if(chart is null)return;
        chart.UseSparklineProfile();chart.Height=double.NaN;chart.ShowGrid=chart.ShowLegend=false;chart.FontFamily=FontFamily;
        AutomationProperties.SetName(chart,AutomationProperties.GetName(this));
        if(Data is null||DataKey is null)return;
        series.Key=DataKey;series.Color=Color;
        if(chart.Series.Count!=1||!ReferenceEquals(chart.Series[0],series)){chart.Series.Clear();chart.Series.Add(series);}
        if(!ReferenceEquals(chart.Data,Data))chart.Data=Data;
    }
    protected override AutomationPeer OnCreateAutomationPeer()=>new SparklinePeer(this);
    private sealed class SparklinePeer(BleeckerSparkline owner):FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore()=>nameof(BleeckerSparkline);
        protected override bool IsControlElementCore()=>false;
        protected override bool IsContentElementCore()=>false;
    }
}
