using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Orchard;

namespace Orchard.Text.Tests;

[TestClass]
public sealed class AppTabBadgeFontTests
{
    [TestMethod]
    public void BundledBoldFontIsPinnedAndDirectWriteUsesARealWeight700Face()
    {
        var file=Path.Combine(AppContext.BaseDirectory,"Fixtures","EncodeSans-Bold.ttf");var bytes=File.ReadAllBytes(file);
        Assert.AreEqual("b6f0d04888140d10006fbeeebc194d33f28fdb50ece52ff16f6e4f952d2ccc85",Convert.ToHexStringLower(SHA256.HashData(bytes)));
        var tables=BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(4,2));var found=false;
        for(var i=0;i<tables;i++)if(System.Text.Encoding.ASCII.GetString(bytes,12+i*16,4)=="OS/2")
        {
            var offset=(int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(12+i*16+8,4));Assert.AreEqual(700,BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset+4,2)));found=true;
        }
        Assert.IsTrue(found);
        using var format=new CanvasTextFormat{FontFamily=new Uri(file).AbsoluteUri+"#Encode Sans",FontSize=8,FontWeight=new(){Weight=700},WordWrapping=CanvasWordWrapping.NoWrap,LocaleName="en-US"};
        using var layout=new CanvasTextLayout(CanvasDevice.GetSharedDevice(),"99+",format,100,100);var renderer=new FaceRenderer();layout.DrawToTextRenderer(renderer,Vector2.Zero);Assert.IsTrue(renderer.Runs>0);
    }
    private sealed class FaceRenderer : ICanvasTextRenderer
    {
        internal int Runs;
        public float Dpi=>96; public bool PixelSnappingDisabled=>true; public Matrix3x2 Transform=>Matrix3x2.Identity;
        public void DrawGlyphRun(Vector2 point,CanvasFontFace face,float size,CanvasGlyph[] glyphs,bool sideways,uint bidi,object brush,CanvasTextMeasuringMode measuring,string locale,string text,int[] clusters,uint index,CanvasGlyphOrientation orientation)
        {
            Runs++;Assert.AreEqual(700,face.Weight.Weight);Assert.IsTrue(face.GetInformationalStrings(CanvasFontInformation.PostscriptName).Any(pair=>pair.Value.Contains("EncodeSans-Bold")));Assert.AreEqual(CanvasFontSimulations.None,face.Simulations);Assert.IsTrue(glyphs.All(glyph=>glyph.Index!=0));
        }
        public void DrawInlineObject(Vector2 point,ICanvasTextInlineObject value,bool sideways,bool rtl,object brush,CanvasGlyphOrientation orientation)=>throw new NotSupportedException();
        public void DrawUnderline(Vector2 point,float width,float thickness,float offset,float height,CanvasTextDirection direction,object brush,CanvasTextMeasuringMode measuring,string locale,CanvasGlyphOrientation orientation) { }
        public void DrawStrikethrough(Vector2 point,float width,float thickness,float offset,CanvasTextDirection direction,object brush,CanvasTextMeasuringMode measuring,string locale,CanvasGlyphOrientation orientation) { }
    }
}
