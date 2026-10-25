using System.Numerics;
using Microsoft.Graphics.Canvas.Text;

namespace Orchard;

internal sealed class BleeckerGlyphWidthRenderer(bool joinWhitespace,bool captureInline=false):ICanvasTextRenderer
{
    internal List<object> Runs { get; }=[];
    internal double Width { get; private set; }
    private double inlineAdvance;
    internal double InlineWidth => Math.Ceiling(inlineAdvance*64)/64;
    private readonly List<InlineRun> inlineRuns=[];
    private sealed record InlineRun(int[] Starts,double[] Positions);
    private InlineIndex? inlineIndex;
    private sealed class InlineIndex
    {
        private readonly record struct Cluster(int Start,int Run,int Offset);
        private readonly InlineRun[] runs;
        private readonly Cluster[] clusters;
        private readonly double[] advances;
        private readonly int[] transitions;
        internal InlineIndex(IReadOnlyList<InlineRun> source)
        {
            runs=source.ToArray();clusters=new Cluster[runs.Sum(run=>run.Starts.Length)];var at=0;
            for(var run=0;run<runs.Length;run++)for(var offset=0;offset<runs[run].Starts.Length;offset++)clusters[at++]=new(runs[run].Starts[offset],run,offset);
            Array.Sort(clusters,static(a,b)=>{var order=a.Start.CompareTo(b.Start);if(order!=0)return order;order=a.Run.CompareTo(b.Run);return order!=0?order:a.Offset.CompareTo(b.Offset);});
            advances=new double[clusters.Length+1];transitions=new int[clusters.Length];
            for(var i=0;i<clusters.Length;i++)
            {
                var item=clusters[i];var positions=runs[item.Run].Positions;
                advances[i+1]=advances[i]+(positions[item.Offset+1]-positions[item.Offset]);
                transitions[i]=i==0?0:transitions[i-1]+(clusters[i-1].Run==item.Run?0:1);
            }
        }
        private int LowerBound(int offset){var first=0;var end=clusters.Length;while(first<end){var middle=first+(end-first)/2;if(clusters[middle].Start<offset)first=middle+1;else end=middle;}return first;}
        internal double Advance(int start,int length){var first=LowerBound(start);var end=LowerBound(start+length);return advances[end]-advances[first];}
        internal double Width(int start,int length)
        {
            var first=LowerBound(start);var end=LowerBound(start+length);if(first==end)return 0;
            if(transitions[first]!=transitions[end-1])return Math.Ceiling((advances[end]-advances[first])*64)/64;
            var run=runs[clusters[first].Run];
            return Math.Ceiling(run.Positions[clusters[end-1].Offset+1]*64)/64-Math.Ceiling(run.Positions[clusters[first].Offset]*64)/64;
        }
    }
    internal double InlineSpanAdvance(int start,int length)=>(inlineIndex??=new(inlineRuns)).Advance(start,length);
    internal double InlineSpanWidth(int start,int length)=>(inlineIndex??=new(inlineRuns)).Width(start,length);
    private CanvasFontFace? previousFace;
    private float previousSize;
    private uint previousBidi;
    private CanvasGlyphOrientation previousOrientation;
    private bool previousSideways, previousWhitespace;
    private double completedWhitespaceGroups, whitespaceGroupAdvance;
    internal double WhitespaceJoinedWidth => joinWhitespace ? completedWhitespaceGroups + Math.Ceiling(whitespaceGroupAdvance*64)/64 : Width;
    private static object FontIdentity(CanvasFontFace face)
    {
        static string Names(IEnumerable<KeyValuePair<string,string>> values)=>string.Join('\n',values.OrderBy(pair=>pair.Key,StringComparer.Ordinal).Select(pair=>pair.Key+"="+pair.Value));
        return new{family=Names(face.FamilyNames),name=Names(face.FaceNames),postscript=Names(face.GetInformationalStrings(CanvasFontInformation.PostscriptName)),version=Names(face.GetInformationalStrings(CanvasFontInformation.VersionStrings)),face.GlyphCount,weight=face.Weight.Weight,face.Stretch,face.Style,face.Simulations};
    }
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
        var glyphPositions=captureInline?new double[glyphs.Length+1]:null;var advance=0d;
        for(var i=0;i<glyphs.Length;i++)
        {
            var next=(Math.Truncate(nominal[i]*65536d)+Math.Floor(((double)glyphs[i].Advance-nominal[i])*65536+.5))/65536;
            advance+=next;if(glyphPositions is not null)glyphPositions[i+1]=advance;
        }
        if(captureInline)
        {
            // A fragment's width is the difference between rounded shaping
            // positions in its run. Rounding the fragment advance again loses
            // a layout unit at a kerned punctuation boundary.
            var glyphStarts=clusters.Distinct().Order().Append(glyphs.Length).ToArray();
            var characterStarts=new List<int>();var characterPositions=new List<double>{0};
            for(var offset=0;offset<clusters.Length;)
            {
                var end=offset+1;while(end<clusters.Length&&clusters[end]==clusters[offset])end++;
                var glyphStart=clusters[offset];var glyphEnd=glyphStarts[Array.BinarySearch(glyphStarts,glyphStart)+1];
                characterStarts.Add((int)characterIndex+offset);characterPositions.Add(characterPositions[^1]+glyphPositions![glyphEnd]-glyphPositions[glyphStart]);offset=end;
            }
            inlineRuns.Add(new(characterStarts.ToArray(),characterPositions.ToArray()));inlineIndex=null;
        }
        Width+=Math.Ceiling(advance*64)/64;
        inlineAdvance+=advance;
        // DirectWrite can split trailing whitespace from an otherwise
        // continuous browser span. Keep the ordinary per-run result,
        // and separately round compatible whitespace fragments together.
        if(joinWhitespace)
        {
            var whitespace=text.Length>0&&text.All(char.IsWhiteSpace);
            var compatible=previousFace is not null&&previousSize==size&&previousBidi==bidiLevel&&previousOrientation==orientation&&previousSideways==sideways&&(previousWhitespace||whitespace);
            // Win2D can return distinct wrappers for the same typeface.
            // Match its authoritative face metadata, including version,
            // weight, stretch and simulations, rather than wrapper identity.
            if(compatible)compatible=previousFace!.Equals(face)||FontIdentity(previousFace).Equals(FontIdentity(face));
            if(!compatible){completedWhitespaceGroups+=Math.Ceiling(whitespaceGroupAdvance*64)/64;whitespaceGroupAdvance=0;}
            whitespaceGroupAdvance+=advance;previousFace=face;previousSize=size;previousBidi=bidiLevel;previousOrientation=orientation;previousSideways=sideways;previousWhitespace=whitespace;
        }
        Runs.Add(new{text,characterIndex,bidiLevel,advances=glyphs.Select(g=>g.Advance).ToArray(),nominal,allocatedWidth=Math.Ceiling(advance*64)/64});
    }
    public void DrawInlineObject(Vector2 point,ICanvasTextInlineObject inlineObject,bool sideways,bool rtl,object brush,CanvasGlyphOrientation orientation)=>throw new NotSupportedException("Intrinsic text cannot contain inline objects.");
    public void DrawUnderline(Vector2 point,float width,float thickness,float offset,float height,CanvasTextDirection direction,object brush,CanvasTextMeasuringMode measuring,string locale,CanvasGlyphOrientation orientation) { }
    public void DrawStrikethrough(Vector2 point,float width,float thickness,float offset,CanvasTextDirection direction,object brush,CanvasTextMeasuringMode measuring,string locale,CanvasGlyphOrientation orientation) { }
}
