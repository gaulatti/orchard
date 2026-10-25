using System.Text;

namespace Orchard;

// Font receipts from the pinned Windows browser establish these script faces.
// Keeping the mapping independent of XAML permits the same shaping path in
// background text verification and visible native controls.
internal static class BleeckerReferenceFontRuns
{
    internal readonly record struct FontRun(string? Family,string Text);
    internal static IReadOnlyList<FontRun> Split(string value,bool cjkPunctuation=false)
    {
        var runs=new List<(string? Family,StringBuilder Text)>();
        foreach(var rune in value.EnumerateRunes())
        {
            var scalar=rune.Value;
            var family=scalar is >=0x4E00 and <=0x9FFF or >=0x3400 and <=0x4DBF||cjkPunctuation&&scalar is >=0x3000 and <=0x303F?"Microsoft YaHei":scalar is >=0x3040 and <=0x30FF?"Yu Gothic":scalar is >=0x0600 and <=0x06FF or >=0x0750 and <=0x077F or >=0x08A0 and <=0x08FF?"Segoe UI":null;
            if(runs.Count==0||runs[^1].Family!=family)runs.Add((family,new()));
            runs[^1].Text.Append(rune.ToString());
        }
        return runs.Select(run=>new FontRun(run.Family,run.Text.ToString())).ToArray();
    }
}
