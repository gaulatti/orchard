namespace Orchard.Catalog;

// One exact-size idle buffer per stage. Active rentals never share storage.
internal sealed class NativeCaptureBufferPool
{
    internal static NativeCaptureBufferPool Bgra { get; } = new();
    internal static NativeCaptureBufferPool Rgba { get; } = new();
    private byte[]? available;

    internal Lease Rent(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        var buffer = Interlocked.Exchange(ref available, null);
        if (buffer is null || buffer.Length != length) buffer = new byte[length];
        return new(this, buffer);
    }

    internal sealed class Lease(NativeCaptureBufferPool owner, byte[] storage) : IDisposable
    {
        private byte[]? buffer = storage;
        internal byte[] Buffer => buffer ?? throw new ObjectDisposedException(nameof(Lease));
        public void Dispose()
        {
            var returned = Interlocked.Exchange(ref buffer, null);
            if (returned is not null) Interlocked.Exchange(ref owner.available, returned);
        }
    }
}
