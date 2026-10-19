using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Windows.UI.ViewManagement;

namespace Orchard;
public sealed partial class BleeckerAccordion
{
    // Preserve line allocation before arranging an overflowing text block to
    // its unclipped width. Widening a wrapping block alone combines short words.
    private sealed class WrapPlan
    {
        private object? key;
        private (string Text,bool Overflow) value;
        private readonly UISettings settings=new();
        internal (string Text,bool Overflow) Render(TextBlock text,double width)
        {
            var scale=text.IsTextScaleFactorEnabled?settings.TextScaleFactor:1;var next=new{Text=text.Text,width,text.FontFamily.Source,text.FontSize,text.FontWeight,text.FontStyle,text.FontStretch,text.CharacterSpacing,text.FlowDirection,text.Language,scale};if(Equals(key,next))return value;
            using var format=new CanvasTextFormat{FontFamily=BleeckerReferenceText.IntrinsicWidth.Resolve(text.FontFamily.Source),FontSize=(float)(text.FontSize*scale),FontWeight=text.FontWeight,FontStyle=text.FontStyle,FontStretch=text.FontStretch,LocaleName=text.Language,Direction=text.FlowDirection==FlowDirection.RightToLeft?CanvasTextDirection.RightToLeftThenTopToBottom:CanvasTextDirection.LeftToRightThenTopToBottom,HorizontalAlignment=CanvasHorizontalAlignment.Left,WordWrapping=CanvasWordWrapping.WholeWord};
            using var layout=new CanvasTextLayout(CanvasDevice.GetSharedDevice(),text.Text,format,(float)width,float.MaxValue);layout.SetPairKerning(0,text.Text.Length,true);layout.SetCharacterSpacing(0,text.Text.Length,0,(float)(text.CharacterSpacing*text.FontSize*scale/1000),0);
            var start=0;foreach(var run in text.Inlines.OfType<Run>()){if(run.ReadLocalValue(TextElement.FontFamilyProperty)!=DependencyProperty.UnsetValue)layout.SetFontFamily(start,run.Text.Length,BleeckerReferenceText.IntrinsicWidth.Resolve(run.FontFamily.Source));start+=run.Text.Length;}
            var overflow=layout.GetMinimumLineLength()>width;var lines=new List<string>();if(overflow){start=0;foreach(var line in layout.LineMetrics){lines.Add(text.Text.Substring(start,line.CharacterCount).TrimEnd(' '));start+=line.CharacterCount;}if(start!=text.Text.Length)throw new InvalidOperationException("Accordion line allocation omitted text.");}
            key=next;return value=(overflow?string.Join('\n',lines):text.Text,overflow);
        }
    }
}
