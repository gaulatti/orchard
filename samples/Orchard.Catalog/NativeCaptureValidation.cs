namespace Orchard.Catalog;

internal static class NativeCaptureValidation
{
    internal static void Verify(byte[] bgra,(byte R,byte G,byte B)? expectedEmptyCanvas=null)
    {
        if(bgra.Length==0||bgra.Length%4!=0)throw new InvalidOperationException("Composed capture has an invalid pixel buffer.");
        if(expectedEmptyCanvas is {} expected)
        {
            for(var index=0;index<bgra.Length;index+=4)
                if(bgra[index]!=expected.B||bgra[index+1]!=expected.G||bgra[index+2]!=expected.R||bgra[index+3]!=255)
                    throw new InvalidOperationException("The empty reference canvas differs from its exact expected colour.");
        }
        else if(bgra.Where((_,offset)=>offset%4!=3).Distinct().Take(9).Count()<=8)
            throw new InvalidOperationException("Composed catalog capture is blank.");
    }
}
