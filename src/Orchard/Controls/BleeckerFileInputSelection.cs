using System.Collections.ObjectModel;

namespace Orchard;

// Selection belongs to the native input, not to its size/error/filter recipe.
// Snapshot the list while retaining each original OS/consumer file object.
internal sealed class BleeckerFileInputSelection<T> where T : class
{
    internal IReadOnlyList<T> Items { get; private set; } = Array.Empty<T>();
    internal void Replace(IEnumerable<T> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        var snapshot = files.ToArray();
        if (snapshot.Any(file => file is null)) throw new ArgumentException("A file selection cannot contain null.", nameof(files));
        Items = new ReadOnlyCollection<T>(snapshot);
    }
    internal void Clear() => Items = Array.Empty<T>();
}
