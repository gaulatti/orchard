using System.Buffers.Binary;
using System.IO.Compression;

namespace Orchard.Catalog;

internal static class NativeCapturePng
{
    private static readonly uint[] CrcTable = CreateCrcTable();
    internal static byte[] Encode(byte[] pixels, int width, int height)
    {
        if (width <= 0 || height <= 0 || pixels.Length != checked(width * height * 4)) throw new ArgumentException("Invalid RGBA capture.");
        using var output = new MemoryStream();
        output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width); BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8; header[9] = 6; Chunk(output, "IHDR"u8, header);
        Chunk(output, "sRGB"u8, new byte[] { 0 });
        var gamma = new byte[4]; BinaryPrimitives.WriteInt32BigEndian(gamma, 45455); Chunk(output, "gAMA"u8, gamma);
        var resolution = new byte[9]; BinaryPrimitives.WriteInt32BigEndian(resolution, 3779); BinaryPrimitives.WriteInt32BigEndian(resolution.AsSpan(4), 3779); resolution[8] = 1; Chunk(output, "pHYs"u8, resolution);
        using var data = new MemoryStream();
        using (var zlib = new ZLibStream(data, CompressionLevel.Fastest, leaveOpen: true))
        {
            var row = new byte[checked(width * 4 + 1)];
            for (var y = 0; y < height; y++)
            {
                pixels.AsSpan(y * width * 4, width * 4).CopyTo(row.AsSpan(1));
                zlib.Write(row);
            }
        }
        Chunk(output, "IDAT"u8, data.GetBuffer().AsSpan(0, checked((int)data.Length))); Chunk(output, "IEND"u8, []);
        return output.ToArray();
    }
    private static void Chunk(Stream output, ReadOnlySpan<byte> type, ReadOnlySpan<byte> bytes)
    {
        Span<byte> value = stackalloc byte[4]; BinaryPrimitives.WriteInt32BigEndian(value, bytes.Length); output.Write(value); output.Write(type); output.Write(bytes);
        var crc = uint.MaxValue; foreach (var b in type) crc = CrcTable[(crc ^ b) & 255] ^ (crc >> 8); foreach (var b in bytes) crc = CrcTable[(crc ^ b) & 255] ^ (crc >> 8);
        BinaryPrimitives.WriteUInt32BigEndian(value, crc ^ uint.MaxValue); output.Write(value);
    }
    private static uint[] CreateCrcTable()
    {
        var table = new uint[256]; for (uint i = 0; i < table.Length; i++) { var value = i; for (var bit = 0; bit < 8; bit++) value = (value & 1) == 0 ? value >> 1 : 0xedb88320 ^ (value >> 1); table[i] = value; } return table;
    }
}
