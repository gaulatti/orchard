using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class FileInputSelectionTests
{
    private sealed record OriginalFile(string Name, string Text);
    [TestMethod]
    public void SelectionKeepsOriginalFileObjectsAndSnapshotsTheSuppliedList()
    {
        var original = new OriginalFile("fictional-note.txt", "Original fictional bytes");
        var files = new List<OriginalFile> { original }; var selection = new BleeckerFileInputSelection<OriginalFile>();
        selection.Replace(files); files.Clear();
        Assert.AreEqual(1, selection.Items.Count); Assert.AreSame(original, selection.Items[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<OriginalFile>)selection.Items).Clear());
    }
    [TestMethod]
    public void InvalidFileAssignmentFailsAtomicallyAndKeepsThePreviousOriginalSelection()
    {
        var original = new OriginalFile("fictional-empty.txt", ""); var selection = new BleeckerFileInputSelection<OriginalFile>(); selection.Replace([original]); var before = selection.Items;
        Assert.Throws<ArgumentException>(() => selection.Replace([null!])); Assert.AreSame(before, selection.Items); Assert.AreSame(original, selection.Items[0]);
        Assert.Throws<ArgumentNullException>(() => selection.Replace(null!)); Assert.AreSame(before, selection.Items);
    }
    [TestMethod]
    public void ReplaceAndClearMatchTheOriginalBrowserSelectionBoundary()
    {
        using var reference = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","FileInput.json")));
        foreach (var theme in new[] { "Light", "Dark" })
        {
            var history = reference.RootElement.GetProperty("histories").EnumerateArray().Single(h => h.GetProperty("id").GetString() == "choose-replace-clear" && h.GetProperty("scheme").GetString() == theme);
            var selection = new BleeckerFileInputSelection<OriginalFile>();
            foreach (var step in history.GetProperty("steps").EnumerateArray())
            {
                var expected = step.GetProperty("value").GetProperty("input").GetProperty("files").EnumerateArray().Select(file => new OriginalFile(file.GetProperty("name").GetString()!,file.GetProperty("text").GetString()!)).ToArray();
                if (expected.Length == 0) selection.Clear(); else selection.Replace(expected);
                CollectionAssert.AreEqual(expected.Select(file=>file.Name).ToArray(),selection.Items.Select(file=>file.Name).ToArray());
                for (var i = 0; i < expected.Length; i++) Assert.AreSame(expected[i],selection.Items[i]);
            }
        }
    }
}
