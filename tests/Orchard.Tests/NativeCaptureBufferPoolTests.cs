using Orchard.Catalog;

namespace Orchard.Tests;

[TestClass]
public sealed class NativeCaptureBufferPoolTests
{
    [TestMethod]
    public void ExactSizeStorageIsReusedOnlyAfterItsLeaseEnds()
    {
        var pool = new NativeCaptureBufferPool();
        var first = pool.Rent(1024);
        var original = first.Buffer;
        using var active = pool.Rent(1024);
        Assert.AreNotSame(original, active.Buffer);
        original[0] = 13; active.Buffer[0] = 27;
        first.Dispose();
        using var reused = pool.Rent(1024);
        Assert.AreSame(original, reused.Buffer);
        Assert.AreEqual((byte)27, active.Buffer[0]);
        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = first.Buffer);
        first.Dispose();
        using var another = pool.Rent(1024);
        Assert.AreNotSame(reused.Buffer, another.Buffer);
        Assert.AreNotSame(active.Buffer, another.Buffer);
    }

    [TestMethod]
    public void OnlyTheMostRecentlyReturnedBufferIsRetained()
    {
        var pool = new NativeCaptureBufferPool();
        var first = pool.Rent(17); var second = pool.Rent(17);
        var firstStorage = first.Buffer; var secondStorage = second.Buffer;
        first.Dispose(); second.Dispose();
        using var reused = pool.Rent(17); using var fresh = pool.Rent(17);
        Assert.AreSame(secondStorage, reused.Buffer);
        Assert.AreNotSame(firstStorage, fresh.Buffer);
        Assert.AreNotSame(secondStorage, fresh.Buffer);
        using var different = pool.Rent(19);
        Assert.AreEqual(19, different.Buffer.Length);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => pool.Rent(0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => pool.Rent(-1));
    }

    [TestMethod]
    public void ASizeChangeDiscardsThePreviousIdleStorage()
    {
        var pool = new NativeCaptureBufferPool();
        var small = pool.Rent(17); var old = small.Buffer; small.Dispose();
        var large = pool.Rent(4096); Assert.AreEqual(4096, large.Buffer.Length); large.Dispose();
        using var resized = pool.Rent(17);
        Assert.AreEqual(17, resized.Buffer.Length);
        Assert.AreNotSame(old, resized.Buffer);
    }

    [TestMethod]
    public void ExceptionalExitReturnsTheRentalWithoutSharingAnActiveBuffer()
    {
        var pool = new NativeCaptureBufferPool(); byte[]? original = null;
        Assert.ThrowsExactly<IOException>(() =>
        {
            using var lease = pool.Rent(4096);
            original = lease.Buffer;
            throw new IOException("Expected capture failure.");
        });
        using var reused = pool.Rent(4096);
        Assert.AreSame(original, reused.Buffer);
    }

    [TestMethod]
    public async Task ConcurrentRentalsKeepIndependentPixels()
    {
        var pool = new NativeCaptureBufferPool();
        await Task.WhenAll(Enumerable.Range(1, 8).Select(value => Task.Run(() =>
        {
            for (var iteration = 0; iteration < 32; iteration++)
            {
                using var lease = pool.Rent(1024);
                Array.Fill(lease.Buffer, (byte)value);
                Thread.Yield();
                Assert.IsTrue(lease.Buffer.All(item => item == (byte)value));
            }
        })));
    }
}
