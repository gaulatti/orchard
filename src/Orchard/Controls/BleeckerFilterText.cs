using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

internal sealed class BleeckerFilterText : Panel
{
    internal TextBlock Text { get; } = new() { UseLayoutRounding=false,TextWrapping=TextWrapping.WrapWholeWords,LineStackingStrategy=LineStackingStrategy.BlockLineHeight,TextReadingOrder=TextReadingOrder.UseFlowDirection };
    private readonly TextBlock measure = new() { UseLayoutRounding=false }, metrics = new() { Text="Mg",UseLayoutRounding=false };
    private readonly TranslateTransform shift = new();
    private readonly UISettings settings = new();
    internal double PreferredWidth { get; private set; }
    internal double MinimumWidth { get; private set; }
    internal BleeckerFilterText() { UseLayoutRounding=false;Text.RenderTransform=shift;Children.Add(Text);LayoutUpdated+=(_,_)=>Align(); }
    internal void Set(Control owner,string value,bool semibold,double lineHeight)
    {
        Text.Text=Regex.Replace(value,"[ \\t\\r\\n\\f]+"," ").Trim(' ');
        var family=owner.FontFamily;
        if(semibold&&family.Source.Contains("EncodeSans-Medium.ttf",StringComparison.Ordinal))family=(FontFamily)Application.Current.Resources["BleeckerPrimarySemiboldFontFamily"];
        foreach(var text in new[]{Text,measure,metrics})
        {text.FontFamily=family;text.FontSize=owner.FontSize;text.FontWeight=semibold?Microsoft.UI.Text.FontWeights.SemiBold:owner.FontWeight;text.CharacterSpacing=owner.CharacterSpacing;text.IsTextScaleFactorEnabled=owner.IsTextScaleFactorEnabled;text.FlowDirection=owner.FlowDirection;}
        Text.LineHeight=lineHeight*owner.FontSize/12;
        measure.Text=Text.Text;measure.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));PreferredWidth=measure.DesiredSize.Width;MinimumWidth=0;
        foreach(var word in Text.Text.Split(' ',StringSplitOptions.RemoveEmptyEntries))
        {measure.Text=word;measure.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));MinimumWidth=Math.Max(MinimumWidth,measure.DesiredSize.Width);}
        metrics.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size available)
    {Text.Measure(available);return new Size(Math.Min(available.Width,PreferredWidth),Text.Text.Length==0?0:Text.DesiredSize.Height);}
    protected override Size ArrangeOverride(Size final){Text.Arrange(new Rect(0,0,final.Width,final.Height));Align();return final;}
    private void Align()
    {
        if(XamlRoot is null||Text.Text.Length==0)return;
        var origin=TransformToVisual(XamlRoot.Content).TransformPoint(new Point()).Y;var factor=Text.IsTextScaleFactorEnabled?settings.TextScaleFactor:1;var scale=XamlRoot.RasterizationScale;
        shift.Y=Math.Floor((origin+(Text.LineHeight*factor-metrics.DesiredSize.Height)/2+metrics.BaselineOffset)*scale)/scale-origin-Text.BaselineOffset;
    }
}
