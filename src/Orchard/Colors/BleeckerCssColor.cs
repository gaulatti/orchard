namespace Orchard;

// CSS keeps legacy rgb()/hex endpoints in encoded sRGB. A color-mix(in
// oklab, ...) endpoint promotes that transition, including interruptions, to
// premultiplied Oklab. Preserve the space until the exact target is reached.
internal readonly record struct BleeckerCssColor(bool Legacy,double First,double Second,double Third,double Alpha)
{
    internal static BleeckerCssColor FromRgba((byte R,byte G,byte B,double Alpha) colour,bool legacy)
    {
        if(legacy){var rgb=BleeckerSrgbColorMix.Premultiply(colour);return new(true,rgb.R,rgb.G,rgb.B,rgb.Alpha);}
        var lab=BleeckerColorMix.Premultiply(colour);return new(false,lab.L,lab.A,lab.B,lab.Alpha);
    }
    private BleeckerCssColor Oklab()
    {
        if(!Legacy)return this;
        if(Alpha<=0)return new(false,0,0,0,0);
        var lab=BleeckerColorMix.Premultiply(First/Alpha/255,Second/Alpha/255,Third/Alpha/255,Alpha);return new(false,lab.L,lab.A,lab.B,lab.Alpha);
    }
    internal static BleeckerCssColor Mix(BleeckerCssColor from,BleeckerCssColor to,double progress)
    {
        var legacy=from.Legacy&&to.Legacy;if(!legacy){from=from.Oklab();to=to.Oklab();}
        return new(legacy,from.First+(to.First-from.First)*progress,from.Second+(to.Second-from.Second)*progress,from.Third+(to.Third-from.Third)*progress,from.Alpha+(to.Alpha-from.Alpha)*progress);
    }
    internal (byte R,byte G,byte B,double Alpha) Encode()=>Legacy?BleeckerSrgbColorMix.EncodePremultiplied((First,Second,Third,Alpha)):BleeckerColorMix.EncodePremultiplied((First,Second,Third,Alpha));
}
