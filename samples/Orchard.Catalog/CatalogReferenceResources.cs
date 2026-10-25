using System.IO.Compression;
using System.Reflection;

namespace Orchard.Catalog;

internal static class CatalogReferenceResources
{
    // CatalogReferences.targets compresses this explicitly named JSON set.
    // Return the original seekable, read-only bytes to every existing consumer.
    internal static Stream Open(Assembly assembly, string name)
    {
        var stream = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Missing catalog resource: {name}");
        if (!name.StartsWith("Orchard.Catalog.Reference.", StringComparison.Ordinal) || !name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) return stream;
        using (stream)
        using (var gzip = new GZipStream(stream, CompressionMode.Decompress))
        using (var decoded = new MemoryStream())
        {
            gzip.CopyTo(decoded);
            return new MemoryStream(decoded.ToArray(), writable: false);
        }
    }
}
