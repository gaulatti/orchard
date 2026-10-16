using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Catalog;

internal static class CapturePersistenceProbe
{
    internal static async Task VerifyAsync(string folder)
    {
        using var source = typeof(CapturePersistenceProbe).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.CapturePersistence.json")
            ?? throw new InvalidOperationException("Capture persistence reference is missing.");
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer);
        var referenceBytes = buffer.ToArray();
        using var reference = JsonDocument.Parse(referenceBytes);
        if (reference.RootElement.GetProperty("format").GetString() != "orchard-capture-persistence-reference-v1")
            throw new InvalidOperationException("Capture persistence reference format is invalid.");
        var expectedCases = reference.RootElement.GetProperty("cases").EnumerateArray().ToArray();
        if (expectedCases.Length != 4) throw new InvalidOperationException("Capture persistence reference must contain four cases.");
        var reports = new List<object>();
        foreach (var (id, width, height) in new[]
        {
            ("transparent", 1, 1), ("odd-size", 17, 3),
            ("alpha-grid", 256, 256), ("catalog-size", 1024, 745)
        })
        {
            var pixels = new byte[checked(width * height * 4)];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var offset = (y * width + x) * 4;
                var alpha = y & 255;
                pixels[offset] = (byte)Math.Min(x & 255, alpha);
                pixels[offset + 1] = (byte)Math.Min((x * 37 + y * 13) & 255, alpha);
                pixels[offset + 2] = (byte)Math.Min((x * 11 + y * 7) & 255, alpha);
                pixels[offset + 3] = (byte)alpha;
            }
            var inputSha256 = Convert.ToHexString(SHA256.HashData(pixels)).ToLowerInvariant();
            var file = $"capture-persistence-{id}.png";
            var path = Path.Combine(folder, file);
            var watch = Stopwatch.StartNew();
            await MainWindow.SavePngAsync(path, pixels, width, height);
            var milliseconds = watch.Elapsed.TotalMilliseconds;
            // Read immediately: completion must mean that the entire file exists.
            var png = await File.ReadAllBytesAsync(path);
            var pngSha256 = Convert.ToHexString(SHA256.HashData(png)).ToLowerInvariant();
            var expected = expectedCases.Single(item => item.GetProperty("id").GetString() == id);
            if (expected.GetProperty("width").GetInt32() != width || expected.GetProperty("height").GetInt32() != height ||
                expected.GetProperty("inputSha256").GetString() != inputSha256 || expected.GetProperty("pngSha256").GetString() != pngSha256)
                throw new InvalidOperationException($"Capture persistence changed frozen pixels or PNG bytes: {id}.");
            reports.Add(new { id, width, height, inputSha256, file,
                pngSha256, milliseconds });
        }
        var failurePropagated = false;
        try { await MainWindow.SavePngAsync(folder, new byte[4], 1, 1); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { failurePropagated = true; }
        if (!failurePropagated) throw new InvalidOperationException("Capture persistence suppressed a write failure.");
        await File.WriteAllTextAsync(Path.Combine(folder, "capture-persistence.json"), JsonSerializer.Serialize(new
        {
            format = "orchard-capture-persistence-v1", generator = "premultiplied-grid-v1",
            referenceSha256 = Convert.ToHexString(SHA256.HashData(referenceBytes)).ToLowerInvariant(),
            matchedFrozenReference = true, immediateFilesRead = true, writeFailurePropagated = failurePropagated, reports
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
