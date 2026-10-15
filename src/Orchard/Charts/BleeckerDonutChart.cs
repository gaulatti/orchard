using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard;

/// <summary>A source Pie Chart ring with independent consumer-owned center text.</summary>
public sealed class BleeckerDonutChart : Control
{
    private BleeckerPieChart? chart;
    private Canvas? center;
    public BleeckerDonutChart()
    {
        DefaultStyleKey = typeof(BleeckerDonutChart); Data = new(); Loaded += (_, _) => Synchronize(); SizeChanged += (_, _) => RenderCenter();
        RegisterPropertyChangedCallback(AutomationProperties.NameProperty, (_, _) => Synchronize());
        RegisterPropertyChangedCallback(FontFamilyProperty, (_, _) => Synchronize());
    }
    private static DependencyProperty Property(string name, Type type, object? initial) => DependencyProperty.Register(name, type, typeof(BleeckerDonutChart), new PropertyMetadata(initial, (owner, _) => ((BleeckerDonutChart)owner).Synchronize()));
    public static readonly DependencyProperty DataProperty = Property(nameof(Data), typeof(object), null);
    public static readonly DependencyProperty ShowLegendProperty = Property(nameof(ShowLegend), typeof(bool), true);
    public static readonly DependencyProperty CenterLabelProperty = Property(nameof(CenterLabel), typeof(string), null);
    public static readonly DependencyProperty CenterValueProperty = Property(nameof(CenterValue), typeof(string), null);
    public ObservableCollection<IReadOnlyDictionary<string, object?>> Data { get => (ObservableCollection<IReadOnlyDictionary<string, object?>>)GetValue(DataProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(DataProperty, value); } }
    public bool ShowLegend { get => (bool)GetValue(ShowLegendProperty); set => SetValue(ShowLegendProperty, value); }
    public string? CenterLabel { get => (string?)GetValue(CenterLabelProperty); set => SetValue(CenterLabelProperty, value); }
    public string? CenterValue { get => (string?)GetValue(CenterValueProperty); set => SetValue(CenterValueProperty, value); }
    protected override void OnApplyTemplate() { base.OnApplyTemplate(); chart = GetTemplateChild("PART_Chart") as BleeckerPieChart; center = GetTemplateChild("PART_Center") as Canvas; Synchronize(); }
    private void Synchronize()
    {
        if (IsLoaded && Data is null) throw new InvalidOperationException("A mounted Donut Chart requires Data.");
        if (chart is not null) { chart.UseDonutIdentity(); chart.Height = double.NaN; chart.InnerRadius = "60%"; chart.ShowLegend = ShowLegend; chart.FontFamily = FontFamily; AutomationProperties.SetName(chart, AutomationProperties.GetName(this)); if (Data is not null && !ReferenceEquals(chart.Data, Data)) chart.Data = Data; }
        RenderCenter();
    }
    private void RenderCenter()
    {
        if (center is null || ActualWidth <= 0 || ActualHeight <= 0) return;
        center.Children.Clear(); var y = (ActualHeight - 24 - (string.IsNullOrEmpty(CenterValue) ? 0 : 32) - (string.IsNullOrEmpty(CenterLabel) ? 0 : 16)) / 2;
        if (!string.IsNullOrEmpty(CenterValue)) Add(CenterValue, 24, 32, true, "value");
        if (!string.IsNullOrEmpty(CenterLabel)) Add(CenterLabel, 12, 16, false, "label");
        void Add(string value, double size, double lineHeight, bool bold, string role)
        {
            var text = (TextBlock)Microsoft.UI.Xaml.Markup.XamlReader.Load($"<TextBlock xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' FontFamily='{{StaticResource {(bold ? "BleeckerPrimarySemiboldFontFamily" : "BleeckerPrimaryFontFamily")}}}' Foreground='{{ThemeResource {(bold ? "BleeckerTextPrimaryBrush" : "BleeckerTextSecondaryBrush")}}}' UseLayoutRounding='False' HighContrastAdjustment='None' />");
            if (!bold || !FontFamily.Source.EndsWith("/EncodeSans-Regular.ttf#Encode Sans", StringComparison.Ordinal)) text.FontFamily = FontFamily;
            text.FontSize = size; text.FontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)(bold ? 600 : 400) }; text.CharacterSpacing = bold ? -20 : 0;
            BleeckerPieText.Set(text, Regex.Replace(value, @"[ \t\n\r\f]+", " ").Trim(' ')); text.Tag = "pie-center-" + role;
            text.LineHeight = lineHeight; text.LineStackingStrategy = LineStackingStrategy.BlockLineHeight; text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var metrics = new TextBlock { Text = "Mg", FontFamily = text.FontFamily, FontSize = size, FontWeight = text.FontWeight, UseLayoutRounding = false }; metrics.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var ascent = Math.Round(metrics.BaselineOffset, MidpointRounding.AwayFromZero); var descent = Math.Round(metrics.DesiredSize.Height - metrics.BaselineOffset, MidpointRounding.AwayFromZero);
            var baseline = ascent + Math.Floor((lineHeight - ascent - descent) / 2);
            // CSS tracking includes the final text element. Measure untracked
            // advances so WinUI's internal spacing quantization does not enter
            // the browser's independent inline allocation.
            BleeckerPieText.Set(metrics, text.Text); metrics.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var tracking = bold ? -.48 * new System.Globalization.StringInfo(text.Text).LengthInTextElements : 0;
            var inlineWidth = Math.Ceiling((BleeckerPieText.InlineWidth(metrics) + tracking) * 64) / 64;
            Canvas.SetLeft(text, Math.Floor((ActualWidth - inlineWidth) * 32) / 64); Canvas.SetTop(text, y + baseline - text.BaselineOffset); center.Children.Add(text); y += lineHeight;
        }
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new DonutPeer(this);
    private sealed class DonutPeer(BleeckerDonutChart owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerDonutChart);
        protected override bool IsControlElementCore() => false;
        protected override bool IsContentElementCore() => false;
    }
}
