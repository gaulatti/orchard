using System.Security.Cryptography;

namespace Orchard.Testing;

// Diagnostic serialization only. The component receives no reference pixels,
// codec bytes or readbacks. This pinned mapping preserves every native byte.
internal static class ScreenshotPngCodec
{
    internal const string Sha256 = "70687f71ba2fda25459eec921f249115865e5dafd86cdad5d22658df1179154c";
    internal const string Name = "pinned-edge-page-screenshot-injective-straight-v1";
    private static readonly byte[] table = Load();

    private static byte[] Load()
    {
        using var source = typeof(ScreenshotPngCodec).Assembly.GetManifestResourceStream("Orchard.Testing.ScreenshotPngCodec.u8")
            ?? throw new InvalidOperationException("The pinned reference PNG codec is missing.");
        using var output = new MemoryStream();
        source.CopyTo(output);
        var bytes = output.ToArray();
        if (bytes.Length != 65536 || Convert.ToHexStringLower(SHA256.HashData(bytes)) != Sha256)
            throw new InvalidOperationException("The pinned reference PNG codec has changed.");
        for (var alpha = 0; alpha < 256; alpha++)
        for (var channel = 0; channel <= alpha; channel++)
            if (Math.Round(bytes[alpha * 256 + channel] * alpha / 255d, MidpointRounding.ToEven) != channel)
                throw new InvalidOperationException("The reference PNG codec loses a premultiplied byte.");
        return bytes;
    }

    internal static byte[] EncodeBgra(byte[] premultiplied)
    {
        if (premultiplied.Length % 4 != 0) throw new ArgumentException("A complete BGRA buffer is required.", nameof(premultiplied));
        var straight = premultiplied.ToArray();
        for (var at = 0; at < straight.Length; at += 4)
        {
            var alpha = premultiplied[at + 3];
            for (var channel = 0; channel < 3; channel++)
            {
                var value = premultiplied[at + channel];
                if (value > alpha) throw new ArgumentException("The diagnostic buffer is not premultiplied BGRA.", nameof(premultiplied));
                straight[at + channel] = table[alpha * 256 + value];
            }
        }
        return straight;
    }
}

