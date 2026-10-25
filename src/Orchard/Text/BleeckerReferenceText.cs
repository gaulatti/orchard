using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

// The pinned Windows browser's platform-font receipts establish these faces.
// Consumer-supplied families retain their own shaping and font resolution.
internal static class BleeckerReferenceText
{
    internal static void Set(TextBlock target,string value,bool cjkPunctuation=false)
    {
        if(!target.FontFamily.Source.Contains("/Orchard/Assets/Fonts/EncodeSans-",StringComparison.Ordinal)){target.Inlines.Clear();target.Text=value;return;}
        target.Text=value;
        var runs=BleeckerReferenceFontRuns.Split(value,cjkPunctuation);
        if(!runs.Any(run=>run.Family is not null))return;
        target.Inlines.Clear();foreach(var item in runs){var run=new Run{Text=item.Text};if(item.Family is not null)run.FontFamily=new FontFamily(item.Family);target.Inlines.Add(run);}
    }
    internal sealed class Presentation
    {
        private string? family;
        internal void Update(TextBlock target,string value,bool cjkPunctuation=false)
        {
            if(target.Text==value&&family==target.FontFamily.Source)return;
            Set(target,value,cjkPunctuation);family=target.FontFamily.Source;
        }
    }
    internal sealed class IntrinsicWidth(bool joinWhitespace = false,bool inlineWidths=false)
    {
        // These five package assets are immutable for the running application.
        // Cache their validated locations, not text metrics or consumer fonts.
        private static readonly IReadOnlyDictionary<string,BleeckerBundledFontAsset> bundledFonts =
            new[]{"EncodeSans-Regular.ttf","EncodeSans-Medium.ttf","EncodeSans-SemiBold.ttf","LibreFranklin-Regular.ttf","LibreFranklin-SemiBold.ttf"}
                .ToDictionary(name=>"/Orchard/Assets/Fonts/"+name,name=>new BleeckerBundledFontAsset(
                    new Uri(System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory,"Orchard","Assets","Fonts",name))),Validate),StringComparer.Ordinal);
        private object? key;
        private double width;
        internal double RawWidth { get; private set; }
        internal double WhitespaceJoinedWidth { get; private set; }
        internal double InlineWidth { get; private set; }
        private BleeckerGlyphWidthRenderer? glyphRenderer;
        internal Func<int,int,double> CaptureInlineSpanMeasure()=>inlineWidths&&glyphRenderer is {} renderer?renderer.InlineSpanWidth:throw new InvalidOperationException("Measure text with inline glyph positions before capturing its span widths.");
        internal double[] RegionWidths { get; private set; }=[];
        internal object[] GlyphRuns { get; private set; }=[];
        private readonly UISettings settings=new();
        internal double Measure(TextBlock target,string text)
        {
            var scale=target.IsTextScaleFactorEnabled?settings.TextScaleFactor:1;
            var numeralAlignment=Microsoft.UI.Xaml.Documents.Typography.GetNumeralAlignment(target);
            var next=new{text,target.FontFamily.Source,target.FontSize,weight=target.FontWeight.Weight,target.FontStyle,target.FontStretch,target.FlowDirection,target.Language,scale,target.CharacterSpacing,numeralAlignment};
            if(Equals(key,next))return width;
            if(text.Length==0){key=next;RawWidth=0;WhitespaceJoinedWidth=0;InlineWidth=0;glyphRenderer=new(joinWhitespace,inlineWidths);RegionWidths=[];GlyphRuns=[];return width=0;}
            using var format=new CanvasTextFormat{FontFamily=Resolve(target.FontFamily.Source),FontSize=(float)(target.FontSize*scale),FontWeight=target.FontWeight,FontStyle=target.FontStyle,FontStretch=target.FontStretch,LocaleName=target.Language,Direction=target.FlowDirection==FlowDirection.RightToLeft?CanvasTextDirection.RightToLeftThenTopToBottom:CanvasTextDirection.LeftToRightThenTopToBottom,HorizontalAlignment=CanvasHorizontalAlignment.Left,WordWrapping=CanvasWordWrapping.NoWrap};
            using var layout=new CanvasTextLayout(CanvasDevice.GetSharedDevice(),text,format,float.MaxValue,float.MaxValue);
            using var typography=numeralAlignment==Microsoft.UI.Xaml.FontNumeralAlignment.Tabular?BleeckerTextTypography.Tabular():null;
            if(typography is not null)layout.SetTypography(0,text.Length,typography);
            layout.SetPairKerning(0,text.Length,true);BleeckerCssLetterSpacing.Apply(layout,text,format.Direction,(float)(target.CharacterSpacing*target.FontSize*scale/1000));
            var start=0;foreach(var run in target.Inlines.OfType<Run>()){if(run.ReadLocalValue(TextElement.FontFamilyProperty)!=DependencyProperty.UnsetValue)layout.SetFontFamily(start,run.Text.Length,Resolve(run.FontFamily.Source));start+=run.Text.Length;}
            RegionWidths=layout.GetCharacterRegions(0,text.Length).Select(region=>region.LayoutBounds.Width).ToArray();RawWidth=RegionWidths.Sum();
            var renderer=new BleeckerGlyphWidthRenderer(joinWhitespace,inlineWidths);layout.DrawToTextRenderer(renderer,Vector2.Zero);GlyphRuns=renderer.Runs.ToArray();glyphRenderer=renderer;
            width=renderer.Width;WhitespaceJoinedWidth=renderer.WhitespaceJoinedWidth;InlineWidth=renderer.InlineWidth;key=next;return width;
        }
        internal static string Resolve(string family)
        {
            var marker=family.IndexOf('#');if(marker<0||!Uri.TryCreate(family[..marker],UriKind.Absolute,out var uri))return family;
            if(uri.Scheme=="ms-appx"&&uri.Host.Length==0)
            {
                if(bundledFonts.TryGetValue(uri.AbsolutePath,out var asset))return asset.Resolve()+family[marker..];
                uri=new Uri(System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory,Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/'))));family=uri.AbsoluteUri+family[marker..];
            }
            Validate(uri);return family;
        }
        private static void Validate(Uri uri){using var fonts=new CanvasFontSet(uri);if(fonts.Fonts.Count==0)throw new InvalidOperationException("The requested text font contains no faces.");}
    }
}
