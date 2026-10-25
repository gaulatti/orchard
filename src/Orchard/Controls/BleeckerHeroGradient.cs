namespace Orchard;

// Opaque CSS Oklab gradient with the GPU's ordered 8x8 RGBA8 dither.
// The lookup quantization follows Skia DitherUtils (Google LLC, BSD license;
// licenses/LICENSE-Skia.txt). It applies to every pixel, not fixture widths.
internal static class BleeckerHeroGradient
{
    private static readonly double[] noise=CreateNoise();
    internal static byte[] Paint(int width,int height,double logicalWidth,double scale,
        (byte R,byte G,byte B) first,(byte R,byte G,byte B) middle,(byte R,byte G,byte B) last)
    {
        if(width<=0||height<=0||!double.IsFinite(logicalWidth)||logicalWidth<=0||!double.IsFinite(scale)||scale<=0)throw new ArgumentOutOfRangeException(nameof(width));
        var stops=new[]{Lab(first),Lab(middle),Lab(last)};
        var columns=new (double R,double G,double B)[width];
        for(var x=0;x<width;x++)
        {
            var p=Math.Clamp((x+.5)/(logicalWidth*scale),0,1);var progress=p<.5?p*2:(p-.5)*2;var a=stops[p<.5?0:1];var b=stops[p<.5?1:2];
            columns[x]=Rgb((a.L+(b.L-a.L)*progress,a.A+(b.A-a.A)*progress,a.B+(b.B-a.B)*progress));
        }
        var bytes=new byte[checked(width*height*4)];
        for(var y=0;y<height;y++)for(var x=0;x<width;x++)
        {
            var c=columns[x];var dither=noise[(y&7)*8+(x&7)];var at=(y*width+x)*4;
            bytes[at]=Channel(c.B+dither);bytes[at+1]=Channel(c.G+dither);bytes[at+2]=Channel(c.R+dither);bytes[at+3]=255;
        }
        return bytes;
    }
    private static double[] CreateNoise()
    {
        var values=new double[64];
        for(var y=0;y<8;y++)for(var x=0;x<8;x++)
        {
            var rank=0;for(var bit=0;bit<3;bit++){rank|=((x>>bit)&1)<<(4-2*bit);rank|=((y>>bit)&1)<<(5-2*bit);}
            var texel=Math.Floor((rank/64d+1d/128)*255+.5);values[y*8+x]=texel/255-.5;
        }
        return values;
    }
    private static (double L,double A,double B) Lab((byte R,byte G,byte B) color)
    {
        var r=Linear(color.R/255d);var g=Linear(color.G/255d);var b=Linear(color.B/255d);
        var l=Math.Cbrt(.4122214708*r+.5363325363*g+.0514459929*b);
        var m=Math.Cbrt(.2119034982*r+.6806995451*g+.1073969566*b);
        var s=Math.Cbrt(.0883024619*r+.2817188376*g+.6299787005*b);
        return(.2104542553*l+.7936177850*m-.0040720468*s,1.9779984951*l-2.4285922050*m+.4505937099*s,.0259040371*l+.7827717662*m-.8086757660*s);
    }
    private static (double R,double G,double B) Rgb((double L,double A,double B) color)
    {
        var l=Math.Pow(.9999999984505198*color.L+.39633779217376786*color.A+.2158037580607588*color.B,3);
        var m=Math.Pow(1.0000000088817609*color.L-.10556134232365635*color.A-.06385417477170591*color.B,3);
        var s=Math.Pow(1.0000000546724108*color.L-.08948418209496575*color.A-1.2914855378640917*color.B,3);
        return(Encode(4.0765482669796285*l-3.3078604530458593*m+.23101769051428148*s),Encode(-1.2684805115952908*l+2.6099303721593876*m-.3413293183059835*s),Encode(-.004155928994110783*l-.7035236918221306*m+1.7078390411687758*s));
    }
    private static double Linear(double v)=>v<=.04045?v/12.92:Math.Pow((v+.055)/1.055,2.4);
    private static double Encode(double v)=>(v<=.0031308?12.92*v:1.055*Math.Pow(v,1/2.4)-.055)*255;
    private static byte Channel(double value)=>(byte)Math.Clamp(Math.Floor(value+.5),0,255);
}
