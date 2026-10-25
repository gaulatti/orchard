using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class HeroCarouselGradientTests
{
    [TestMethod]
    public void DitheredGradientMatchesFrozenOriginalSurfaceProbesAcrossThemesAndWidths()
    {
        var root=Path.Combine(AppContext.BaseDirectory,"Fixtures");using var reference=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root,"HeroCarousel.json")));var probes=0;
        foreach(var c in reference.RootElement.GetProperty("cases").EnumerateArray())
        {
            if(c.GetProperty("id").GetString()=="custom-background")continue;
            var slides=c.GetProperty("measurement").GetProperty("sections")[0].GetProperty("slides");if(slides.GetArrayLength()>0&&slides[0].GetProperty("image").ValueKind!=JsonValueKind.Null)continue;
            var file=Path.Combine(root,"HeroCarousel",c.GetProperty("file").GetString()!);var bytes=File.ReadAllBytes(file);Assert.AreEqual(c.GetProperty("pngSha256").GetString(),Convert.ToHexStringLower(SHA256.HashData(bytes)));
            var png=Decode(bytes);var dark=c.GetProperty("scheme").GetString()=="Dark";var native=BleeckerHeroGradient.Paint(png.Width,32,png.Width,1,dark?((byte)24,(byte)37,(byte)51):((byte)26,(byte)55,(byte)77),(105,79,93),(118,50,63));
            foreach(var x in new[]{0,png.Width/2,png.Width-1}){var at=(20*png.Width+x)*4;CollectionAssert.AreEqual(png.Rgba[at..(at+4)],new[]{native[at+2],native[at+1],native[at],native[at+3]},c.GetProperty("id").GetString()+"/"+c.GetProperty("scheme").GetString()+"/"+x);probes++;}
        }
        Assert.AreEqual(228,probes);
    }
    private static (int Width,byte[] Rgba) Decode(byte[] png)
    {
        using var compressed=new MemoryStream();var width=0;var height=0;var channels=0;
        for(var offset=8;offset<png.Length;){var length=BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset));var name=System.Text.Encoding.ASCII.GetString(png,offset+4,4);if(name=="IHDR"){width=BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset+8));height=BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset+12));Assert.AreEqual(8,png[offset+16]);Assert.IsTrue(png[offset+17]is 2 or 6);channels=png[offset+17]==6?4:3;}if(name=="IDAT")compressed.Write(png,offset+8,length);offset+=length+12;if(name=="IEND")break;}
        compressed.Position=0;using var inflater=new ZLibStream(compressed,CompressionMode.Decompress);using var decoded=new MemoryStream();inflater.CopyTo(decoded);var source=decoded.ToArray();var stride=width*channels;Assert.AreEqual((stride+1)*height,source.Length);var raw=new byte[stride*height];var cursor=0;
        static int Paeth(int a,int b,int c){var p=a+b-c;var da=Math.Abs(p-a);var db=Math.Abs(p-b);var dc=Math.Abs(p-c);return da<=db&&da<=dc?a:db<=dc?b:c;}
        for(var y=0;y<height;y++){var filter=source[cursor++];Assert.IsTrue(filter<=4);for(var x=0;x<stride;x++){var at=y*stride+x;var left=x>=channels?raw[at-channels]:0;var above=y>0?raw[at-stride]:0;var corner=y>0&&x>=channels?raw[at-stride-channels]:0;var prediction=filter switch{0=>0,1=>left,2=>above,3=>(left+above)/2,4=>Paeth(left,above,corner),_=>throw new InvalidDataException()};raw[at]=unchecked((byte)(source[cursor++]+prediction));}}
        var rgba=new byte[width*height*4];for(var i=0;i<width*height;i++){rgba[4*i]=raw[channels*i];rgba[4*i+1]=raw[channels*i+1];rgba[4*i+2]=raw[channels*i+2];rgba[4*i+3]=channels==4?raw[channels*i+3]:(byte)255;}return(width,rgba);
    }
}
