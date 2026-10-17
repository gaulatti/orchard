using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;
public sealed partial class BleeckerAccordion
{
    private static bool Numeric(object? value)=>value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
    private static bool Present(object? value)=>value switch{null or false=>false,string text=>text.Length>0,_ when Numeric(value)=>Convert.ToDouble(value,CultureInfo.InvariantCulture) is var number&&number!=0&&!double.IsNaN(number),_=>true};
    private static string Scalar(object? value)=>value switch{null or bool=>"",string text=>text,double number=>BleeckerNumberText.Format(number),float number=>BleeckerNumberText.Format(double.Parse(number.ToString("R",CultureInfo.InvariantCulture),CultureInfo.InvariantCulture)),decimal number when number==0=>"0",IFormattable number when Numeric(value)=>number.ToString(null,CultureInfo.InvariantCulture),_=>""};
    private sealed class Slot:Panel
    {
        private readonly TextBlock text=new(){UseLayoutRounding=false,TextWrapping=TextWrapping.WrapWholeWords,LineStackingStrategy=LineStackingStrategy.BlockLineHeight};
        private readonly TextBlock measure=new(){UseLayoutRounding=false};
        private readonly WrapPlan wrapping=new();
        private string logicalText="";
        private readonly ContentPresenter rich=new(){UseLayoutRounding=false,HorizontalContentAlignment=HorizontalAlignment.Stretch};
        private readonly BleeckerReferenceText.IntrinsicWidth intrinsic=new();
        private readonly double fontSize,lineHeight;
        private readonly bool secondary;
        private bool plain;
        private TextBlock? richText;
        private readonly List<(TextBlock Probe,BleeckerReferenceText.IntrinsicWidth Width)> richMeasures=[];
        internal Slot(string name,double fontSize,double lineHeight,bool secondary=false){Name=name;this.fontSize=fontSize;this.lineHeight=lineHeight;this.secondary=secondary;UseLayoutRounding=false;Children.Add(text);Children.Add(rich);}
        internal string AccessibleName=>plain?logicalText:ReadName(rich);
        private static string ReadName(UIElement element)
        {
            var specified=AutomationProperties.GetName(element);if(specified.Length>0)return specified;
            if(element is TextBlock text)return text.Text;
            if(FrameworkElementAutomationPeer.CreatePeerForElement(element) is {} peer&&peer.GetName() is {Length:>0} name)return name;
            var names=new List<string>();for(var index=0;index<VisualTreeHelper.GetChildrenCount(element);index++){var value=ReadName((UIElement)VisualTreeHelper.GetChild(element,index));if(value.Length>0)names.Add(value);}return string.Join(" ",names);
        }
        internal void Set(BleeckerAccordion owner,object? value,DataTemplate? template,Brush foreground)
        {
            plain=template is null&&(value is null or bool or string||Numeric(value));if(!plain&&template is null&&value is not UIElement)throw new ArgumentException("Accordion object content requires a DataTemplate or UIElement.");
            FlowDirection=owner.FlowDirection;var family=secondary?owner.Font("BleeckerSecondaryFontFamily"):fontSize==14&&owner.FontFamily.Source==owner.Font("BleeckerPrimaryFontFamily").Source?owner.Font("BleeckerPrimaryMediumFontFamily"):owner.FontFamily;var size=owner.FontSize*fontSize/14;
            text.FontFamily=rich.FontFamily=family;text.FontSize=rich.FontSize=size;text.FontWeight=rich.FontWeight=secondary?Microsoft.UI.Text.FontWeights.Normal:fontSize==14?Microsoft.UI.Text.FontWeights.Medium:Microsoft.UI.Text.FontWeights.Normal;text.Foreground=rich.Foreground=foreground;text.CharacterSpacing=rich.CharacterSpacing=owner.CharacterSpacing;text.IsTextScaleFactorEnabled=rich.IsTextScaleFactorEnabled=owner.IsTextScaleFactorEnabled;text.LineHeight=owner.FontSize*lineHeight/14;
            logicalText=Regex.Replace(plain?Scalar(value):"","[ \\t\\r\\n\\f]+"," ").Trim(' ');BleeckerReferenceText.Set(text,logicalText);text.Visibility=plain&&logicalText.Length>0?Visibility.Visible:Visibility.Collapsed;rich.Visibility=plain?Visibility.Collapsed:Visibility.Visible;
            var content=plain?null:value;richText=template is null?content as TextBlock:null;if(!ReferenceEquals(rich.Content,content)||!ReferenceEquals(rich.ContentTemplate,template)){rich.Content=null;rich.ContentTemplate=template;rich.Content=content;}InvalidateMeasure();
        }
        internal void Release()=>rich.Content=null;
        protected override Size MeasureOverride(Size available)
        {
            if(!plain){if(richText is not null){var richWidth=Math.Min(available.Width,RichWidth(richText));rich.Measure(new(richWidth,available.Height));return new(richWidth,rich.DesiredSize.Height);}rich.Measure(available);return rich.DesiredSize;}if(text.Text.Length==0)return new(0,0);
            measure.FontFamily=text.FontFamily;measure.FontSize=text.FontSize;measure.FontWeight=text.FontWeight;measure.FontStyle=text.FontStyle;measure.FontStretch=text.FontStretch;measure.CharacterSpacing=text.CharacterSpacing;measure.IsTextScaleFactorEnabled=text.IsTextScaleFactorEnabled;measure.FlowDirection=text.FlowDirection;measure.Language=text.Language;BleeckerReferenceText.Set(measure,logicalText);
            var width=Math.Min(available.Width,intrinsic.Measure(measure,logicalText));var rendered=wrapping.Render(measure,width);text.TextWrapping=rendered.Overflow?TextWrapping.NoWrap:TextWrapping.WrapWholeWords;if(text.Text!=rendered.Text)BleeckerReferenceText.Set(text,rendered.Text);text.Measure(new(width,double.PositiveInfinity));return new(width,text.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size final){text.Arrange(new(0,0,final.Width,final.Height));if(plain&&text.ActualWidth>final.Width)text.Arrange(new(0,0,text.ActualWidth,final.Height));rich.Arrange(new(0,0,final.Width,final.Height));return final;}
        private double RichWidth(TextBlock target)
        {
            var initial=new TextStyle(target.FontFamily,target.FontSize,target.FontWeight,target.FontStyle,target.FontStretch,target.CharacterSpacing);
            var fragments=new List<(string Text,TextStyle Style,UIElement? Embedded)>();
            void Append(string value,TextStyle style){if(fragments.Count>0&&fragments[^1].Embedded is null&&fragments[^1].Style==style)fragments[^1]=(fragments[^1].Text+value,style,null);else fragments.Add((value,style,null));}
            void Walk(InlineCollection values,TextStyle inherited){foreach(var inline in values){var current=inherited.Apply(inline);if(inline is Run run)Append(run.Text,current);else if(inline is Span span)Walk(span.Inlines,current);else if(inline is LineBreak)Append("\n",current);else if(inline is InlineUIContainer container&&container.Child is {} child)fragments.Add(("",current,child));}}
            if(target.Inlines.Count==0)Append(target.Text,initial);else Walk(target.Inlines,initial);var line=0d;var maximum=0d;var index=0;
            foreach(var fragment in fragments){if(fragment.Embedded is {} embedded){embedded.Measure(new(double.PositiveInfinity,double.PositiveInfinity));line+=embedded.DesiredSize.Width;continue;}var parts=fragment.Text.Split('\n');for(var part=0;part<parts.Length;part++){if(index==richMeasures.Count)richMeasures.Add((new TextBlock(),new BleeckerReferenceText.IntrinsicWidth()));var measure=richMeasures[index++];var style=fragment.Style;measure.Probe.FontFamily=style.Family;measure.Probe.FontSize=style.Size;measure.Probe.FontWeight=style.Weight;measure.Probe.FontStyle=style.Style;measure.Probe.FontStretch=style.Stretch;measure.Probe.CharacterSpacing=style.Spacing;measure.Probe.Language=target.Language;measure.Probe.FlowDirection=target.FlowDirection;measure.Probe.IsTextScaleFactorEnabled=target.IsTextScaleFactorEnabled;line+=measure.Width.Measure(measure.Probe,parts[part]);if(part<parts.Length-1){maximum=Math.Max(maximum,line);line=0;}}}
            if(index<richMeasures.Count)richMeasures.RemoveRange(index,richMeasures.Count-index);return Math.Max(maximum,line);
        }
        private sealed record TextStyle(FontFamily Family,double Size,Windows.UI.Text.FontWeight Weight,Windows.UI.Text.FontStyle Style,Windows.UI.Text.FontStretch Stretch,int Spacing)
        {
            internal TextStyle Apply(Inline value)=>new(value.ReadLocalValue(TextElement.FontFamilyProperty)==DependencyProperty.UnsetValue?Family:value.FontFamily,value.ReadLocalValue(TextElement.FontSizeProperty)==DependencyProperty.UnsetValue?Size:value.FontSize,value.ReadLocalValue(TextElement.FontWeightProperty)==DependencyProperty.UnsetValue?Weight:value.FontWeight,value.ReadLocalValue(TextElement.FontStyleProperty)==DependencyProperty.UnsetValue?Style:value.FontStyle,value.ReadLocalValue(TextElement.FontStretchProperty)==DependencyProperty.UnsetValue?Stretch:value.FontStretch,value.ReadLocalValue(TextElement.CharacterSpacingProperty)==DependencyProperty.UnsetValue?Spacing:value.CharacterSpacing);
        }
    }
}
