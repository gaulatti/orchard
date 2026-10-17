namespace Orchard.Catalog;

internal static class NativeCaptureValidation
{
    internal static void VerifyExact(ReadOnlySpan<byte> bgra, ReadOnlySpan<byte> expectedBgra)
    {
        if (expectedBgra.Length == 0 || expectedBgra.Length % 4 != 0 || !bgra.SequenceEqual(expectedBgra))
            throw new InvalidOperationException("The composed capture differs from its complete exact reference frame.");
    }
    internal static void Verify(byte[] bgra,(byte R,byte G,byte B)? expectedEmptyCanvas=null)
    {
        if(bgra.Length==0||bgra.Length%4!=0)throw new InvalidOperationException("Composed capture has an invalid pixel buffer.");
        if(expectedEmptyCanvas is {} expected)
        {
            for(var index=0;index<bgra.Length;index+=4)
                if(bgra[index]!=expected.B||bgra[index+1]!=expected.G||bgra[index+2]!=expected.R||bgra[index+3]!=255)
                    throw new InvalidOperationException("The empty reference canvas differs from its exact expected colour.");
        }
        else if(!HasColorVariation(bgra))
            throw new InvalidOperationException("Composed catalog capture is blank.");
    }

    internal static bool HasColorVariation(ReadOnlySpan<byte> pixels)
    {
        // Keep the original distinct-channel threshold, including alpha exclusion.
        Span<bool> seen = stackalloc bool[256];
        seen.Clear();
        var count = 0;
        for (var at = 0; at < pixels.Length; at++)
        {
            if ((at & 3) == 3 || seen[pixels[at]]) continue;
            seen[pixels[at]] = true;
            if (++count == 9) return true;
        }
        return false;
    }
}
