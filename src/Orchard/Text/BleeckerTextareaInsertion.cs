namespace Orchard;

/// <summary>Prepares one plain-text user insertion using the native zero-means-unlimited limit.</summary>
internal static class BleeckerTextareaInsertion
{
    internal static string Prepare(string text, string current, int start, int end, int maxLength)
    {
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        if (maxLength == 0) return text;
        // Native Select All can include RichEdit's immutable final paragraph.
        // It is absent from the public plain-text value and adds no capacity.
        var selected = Math.Min(end, current.Length) - Math.Min(start, current.Length);
        var available = Math.Max(0, maxLength - (current.Length - selected));
        if (text.Length <= available) return text;
        // HTML and the native editor count UTF-16 units. A clipboard insertion
        // must still end on a complete surrogate pair at the capacity boundary.
        if (available > 0 && char.IsHighSurrogate(text[available - 1]) && char.IsLowSurrogate(text[available])) available--;
        return text[..available];
    }
}
