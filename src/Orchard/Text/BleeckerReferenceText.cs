using System.Text;
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
    internal static void Set(TextBlock target,string value)
    {
        target.Text=value;
        if(!target.FontFamily.Source.Contains("/Orchard/Assets/Fonts/EncodeSans-",StringComparison.Ordinal))return;
        var runs=new List<(string? Family,StringBuilder Text)>();
        foreach(var rune in value.EnumerateRunes())
        {
            var scalar=rune.Value;
            var family=scalar is >=0x4E00 and <=0x9FFF or >=0x3400 and <=0x4DBF?"Microsoft YaHei":scalar is >=0x3040 and <=0x30FF?"Yu Gothic":scalar is >=0x0600 and <=0x06FF or >=0x0750 and <=0x077F or >=0x08A0 and <=0x08FF?"Segoe UI":null;
            if(runs.Count==0||runs[^1].Family!=family)runs.Add((family,new()));runs[^1].Text.Append(rune.ToString());
        }
        if(!runs.Any(run=>run.Family is not null))return;
        target.Inlines.Clear();foreach(var item in runs){var run=new Run{Text=item.Text.ToString()};if(item.Family is not null)run.FontFamily=new FontFamily(item.Family);target.Inlines.Add(run);}
    }
    internal sealed class IntrinsicWidth
    {
        private object? key;
        private double width;
        internal double RawWidth { get; private set; }
        internal double[] RegionWidths { get; private set; }=[];
        internal object[] GlyphRuns { get; private set; }=[];
        private readonly UISettings settings=new();
        internal double Measure(TextBlock target,string text)
        {
            var scale=target.IsTextScaleFactorEnabled?settings.TextScaleFactor:1;
            var next=new{text,target.FontFamily.Source,target.FontSize,weight=target.FontWeight.Weight,target.FontStyle,target.FontStretch,target.FlowDirection,target.Language,scale,target.CharacterSpacing};
            if(Equals(key,next))return width;
            if(text.Length==0){key=next;RawWidth=0;RegionWidths=[];GlyphRuns=[];return width=0;}
            using var format=new CanvasTextFormat{FontFamily=Resolve(target.FontFamily.Source),FontSize=(float)(target.FontSize*scale),FontWeight=target.FontWeight,FontStyle=target.FontStyle,FontStretch=target.FontStretch,LocaleName=target.Language,Direction=target.FlowDirection==FlowDirection.RightToLeft?CanvasTextDirection.RightToLeftThenTopToBottom:CanvasTextDirection.LeftToRightThenTopToBottom,HorizontalAlignment=CanvasHorizontalAlignment.Left,WordWrapping=CanvasWordWrapping.NoWrap};
            using var layout=new CanvasTextLayout(CanvasDevice.GetSharedDevice(),text,format,float.MaxValue,float.MaxValue);
            layout.SetPairKerning(0,text.Length,true);layout.SetCharacterSpacing(0,text.Length,0,(float)(target.CharacterSpacing*target.FontSize*scale/1000),0);
            var start=0;foreach(var run in target.Inlines.OfType<Run>()){if(run.ReadLocalValue(TextElement.FontFamilyProperty)!=DependencyProperty.UnsetValue)layout.SetFontFamily(start,run.Text.Length,Resolve(run.FontFamily.Source));start+=run.Text.Length;}
            RegionWidths=layout.GetCharacterRegions(0,text.Length).Select(region=>region.LayoutBounds.Width).ToArray();RawWidth=RegionWidths.Sum();
            var renderer=new WidthRenderer();layout.DrawToTextRenderer(renderer,Vector2.Zero);GlyphRuns=renderer.Runs.ToArray();
            width=renderer.Width;key=next;return width;
        }
        private sealed class WidthRenderer:ICanvasTextRenderer
        {
            internal List<object> Runs { get; }=[];
            internal double Width { get; private set; }
            public float Dpi=>96;
            public bool PixelSnappingDisabled=>true;
            public Matrix3x2 Transform=>Matrix3x2.Identity;
            public void DrawGlyphRun(Vector2 point,CanvasFontFace face,float size,CanvasGlyph[] glyphs,bool sideways,uint bidiLevel,object brush,CanvasTextMeasuringMode measuring,string locale,string text,int[] clusters,uint characterIndex,CanvasGlyphOrientation orientation)
            {
                var nominal=face.GetGlyphMetrics(glyphs.Select(g=>g.Index).ToArray(),sideways).Select(g=>g.AdvanceWidth*size).ToArray();
                // Blink/Skia truncates nominal advances to 16.16; OpenType
                // positioning rounds its separate adjustment. Each shaped
                // run then rounds up to a 1/64px browser layout unit.
                // Quantizing an already-kerned region changes RTL allocation.
                var advance=0d;
                for(var i=0;i<glyphs.Length;i++)advance+=(Math.Truncate(nominal[i]*65536d)+Math.Floor(((double)glyphs[i].Advance-nominal[i])*65536+.5))/65536;
                Width+=Math.Ceiling(advance*64)/64;
                Runs.Add(new{text,characterIndex,bidiLevel,advances=glyphs.Select(g=>g.Advance).ToArray(),nominal,allocatedWidth=Math.Ceiling(advance*64)/64});
            }
            public void DrawInlineObject(Vector2 point,ICanvasTextInlineObject inlineObject,bool sideways,bool rtl,object brush,CanvasGlyphOrientation orientation)=>throw new NotSupportedException("Intrinsic text cannot contain inline objects.");
            public void DrawUnderline(Vector2 point,float width,float thickness,float offset,float height,CanvasTextDirection direction,object brush,CanvasTextMeasuringMode measuring,string locale,CanvasGlyphOrientation orientation) { }
            public void DrawStrikethrough(Vector2 point,float width,float thickness,float offset,CanvasTextDirection direction,object brush,CanvasTextMeasuringMode measuring,string locale,CanvasGlyphOrientation orientation) { }
        }
        internal static string Resolve(string family)
        {
            var marker=family.IndexOf('#');if(marker<0||!Uri.TryCreate(family[..marker],UriKind.Absolute,out var uri))return family;
            if(uri.Scheme=="ms-appx"&&uri.Host.Length==0){uri=new Uri(System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory,Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/'))));family=uri.AbsoluteUri+family[marker..];}
            using var fonts=new CanvasFontSet(uri);if(fonts.Fonts.Count==0)throw new InvalidOperationException("The requested text font contains no faces.");return family;
        }
    }
}
