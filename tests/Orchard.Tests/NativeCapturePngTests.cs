using System.Buffers.Binary;
using System.IO.Compression;
using Orchard.Catalog;

namespace Orchard.Tests;

[TestClass]
public sealed class NativeCapturePngTests
{
    [TestMethod]
    public void RgbaCaptureSurvivesPngScanlinesAndZlibRoundTrip()
    {
        const int width=257,height=258;
        var pixels=new byte[width*height*4];
        for(var y=0;y<height;y++)for(var x=0;x<width;x++)
        {var at=(y*width+x)*4;pixels[at]=(byte)x;pixels[at+1]=(byte)y;pixels[at+2]=(byte)(x^y);pixels[at+3]=(byte)(x+y);}
        var png=NativeCapturePng.Encode(pixels,width,height);var chunks=Chunks(png);
        CollectionAssert.AreEqual(new byte[]{137,80,78,71,13,10,26,10},png[..8]);
        Assert.AreEqual(width,BinaryPrimitives.ReadInt32BigEndian(chunks["IHDR"]));
        Assert.AreEqual(height,BinaryPrimitives.ReadInt32BigEndian(chunks["IHDR"].AsSpan(4)));
        CollectionAssert.AreEqual(new byte[]{8,6,0,0,0},chunks["IHDR"][8..]);
        using var zlib=new ZLibStream(new MemoryStream(chunks["IDAT"]),CompressionMode.Decompress);
        using var decoded=new MemoryStream();zlib.CopyTo(decoded);var rows=decoded.ToArray();
        Assert.AreEqual(height*(width*4+1),rows.Length);
        for(var y=0;y<height;y++)
        {
            Assert.AreEqual((byte)0,rows[y*(width*4+1)],"PNG scanline filter");
            CollectionAssert.AreEqual(pixels[(y*width*4)..((y+1)*width*4)],rows[(y*(width*4+1)+1)..((y+1)*(width*4+1))]);
        }
        CollectionAssert.AreEqual(new byte[]{0xae,0x42,0x60,0x82},png[^4..],"PNG IEND checksum");
    }
    [TestMethod]
    public void CaptureRetainsWindowsSrgbGammaAndPhysicalResolution()
    {
        var chunks=Chunks(NativeCapturePng.Encode([12,34,56,128],1,1));
        CollectionAssert.AreEqual(new byte[]{0},chunks["sRGB"]);
        Assert.AreEqual(45455,BinaryPrimitives.ReadInt32BigEndian(chunks["gAMA"]));
        Assert.AreEqual(3779,BinaryPrimitives.ReadInt32BigEndian(chunks["pHYs"]));
        Assert.AreEqual(3779,BinaryPrimitives.ReadInt32BigEndian(chunks["pHYs"].AsSpan(4)));
        Assert.AreEqual((byte)1,chunks["pHYs"][8]);
    }
    [TestMethod]
    public void InvalidCaptureDimensionsAndBufferLengthsFail()
    {
        Assert.ThrowsExactly<ArgumentException>(()=>NativeCapturePng.Encode([],0,1));
        Assert.ThrowsExactly<ArgumentException>(()=>NativeCapturePng.Encode([],1,-1));
        Assert.ThrowsExactly<ArgumentException>(()=>NativeCapturePng.Encode([1,2,3],1,1));
    }
    private static Dictionary<string,byte[]> Chunks(byte[] png)
    {
        var result=new Dictionary<string,byte[]>();
        for(var at=8;at<png.Length;)
        {var count=BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(at));var type=System.Text.Encoding.ASCII.GetString(png,at+4,4);result.Add(type,png[(at+8)..(at+8+count)]);at+=count+12;}
        return result;
    }
}
