using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
namespace Orchard.Tests;
[TestClass]
public sealed class FeedItemContractTests
{
    private static JsonDocument Read()=>JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","FeedItem.json")));
    [TestMethod]
    public void OriginalSourceFontsCollectorAndEveryCompleteScreenshotStayBound()
    {
        using var document=Read();var root=document.RootElement;Assert.AreEqual("ff6b85b5ae82e3b7b50a873bdfb080be557ca0d464a6e2a0f36e8e4db77256a0",root.GetProperty("sourceSha256").GetString());Assert.AreEqual(100,root.GetProperty("cases").GetArrayLength());
        foreach(var(file,key)in new[]{("CaptureFeedItem.cjs","scriptSha256"),("FeedItemClient.tsx","clientSha256")})Assert.AreEqual(root.GetProperty(key).GetString(),Hash(Path.Combine(AppContext.BaseDirectory,"Fixtures",file)));
        var pngs=0;foreach(var row in root.GetProperty("cases").EnumerateArray())if(row.TryGetProperty("file",out var file)){Assert.AreEqual(row.GetProperty("pngSha256").GetString(),Hash(Path.Combine(AppContext.BaseDirectory,"Fixtures","FeedItem",file.GetString()!)));pngs++;}Assert.AreEqual(64,pngs);
        foreach(var font in root.GetProperty("fonts").EnumerateArray())Assert.AreEqual(font.GetProperty("sha256").GetString(),Hash(Path.Combine(AppContext.BaseDirectory,"Fixtures",font.GetProperty("name").GetString()!)));
    }
    [TestMethod]
    public void AllPinnedTimestampLocalesAndInclusiveTwelveHourBoundariesMatch()
    {
        using var document=Read();var root=document.RootElement;var now=DateTimeOffset.Parse(root.GetProperty("now").GetString()!,CultureInfo.InvariantCulture);var zone=TimeZoneInfo.FindSystemTimeZoneById("America/New_York");Assert.AreEqual(21,root.GetProperty("formats").GetArrayLength());
        foreach(var row in root.GetProperty("formats").EnumerateArray()){var actual=BleeckerFeedItemModel.FormatTimestamp(row.GetProperty("postedAt").GetString()!,now,CultureInfo.GetCultureInfo(row.GetProperty("locale").GetString()!),zone);Assert.AreEqual(row.GetProperty("label").GetString(),actual.Label,row.ToString());Assert.AreEqual(row.GetProperty("iso").GetString(),actual.Iso);}
    }
    [TestMethod]
    public void SourceCategoriesRelevanceHostLabelsAndFallbackInitialsMatch()
    {
        using var document=Read();foreach(var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec=row.GetProperty("spec");var m=row.GetProperty("measurement");var values=spec.TryGetProperty("categories",out var list)?list.EnumerateArray().Select(v=>v.GetString()!).ToArray():["Local","Review"];var normalized=values.Select(BleeckerFeedItemModel.Category).Where(s=>s.Length>0).ToArray();CollectionAssert.AreEqual(m.GetProperty("labels").EnumerateArray().Skip(1).Select(v=>v.GetProperty("text").GetString()!).ToArray(),normalized);
            var relevance=spec.TryGetProperty("relevanceKind",out var kind)?kind.GetString()=="nan"?double.NaN:double.PositiveInfinity:spec.TryGetProperty("relevance",out var numeric)?numeric.GetDouble():87.5;Assert.AreEqual(m.GetProperty("relevance").GetProperty("text").GetString(),BleeckerFeedItemModel.Relevance(relevance));
            Assert.AreEqual(m.GetProperty("link").GetProperty("text").GetString(),BleeckerFeedItemModel.SourceLabel(spec.TryGetProperty("sourceUrl",out var source)?source.GetString()!:"https://news.example/story"));
        }
        Assert.AreEqual("SS",BleeckerFeedItemModel.Fallback(" ßexample",null));Assert.AreEqual("?",BleeckerFeedItemModel.Fallback("\ufeff ",null));Assert.AreEqual("Local Desk",BleeckerFeedItemModel.Fallback("A","Local Desk"));Assert.AreEqual("\u0085",BleeckerFeedItemModel.Category("\u0085"));
    }
    [TestMethod]
    public void BrowserKeepsAvatarErrorLatchAndOriginalLinkAcrossUpdates()
    {
        using var document=Read();foreach(var row in document.RootElement.GetProperty("interactions").EnumerateArray()){Assert.IsTrue(row.GetProperty("avatarRetained").GetBoolean());Assert.IsTrue(row.GetProperty("linkRetained").GetBoolean());Assert.IsTrue(row.GetProperty("imageFailureLatched").GetBoolean());Assert.AreEqual("https://updated.example/path",row.GetProperty("href").GetString());}
        Assert.AreEqual(2,document.RootElement.GetProperty("tooltips").GetArrayLength());foreach(var tooltip in document.RootElement.GetProperty("tooltips").EnumerateArray())Assert.AreEqual("2026-10-18T15:00:00.000Z",tooltip.GetProperty("content").GetString());
    }
    [TestMethod]
    public void EmergencyWrappingKeepsWholeWordsThenSplitsOversizedGraphemes()
    {
        static double Width(string value)=>StringInfo.ParseCombiningCharacters(value).Length;
        var text="One oversizedword next e\u0301e\u0301e\u0301e\u0301";var model=new BleeckerInlineText(text,text.Select((c,i)=>(c,i)).Where(pair=>pair.c==' ').Select(pair=>pair.i+1).Append(text.Length).ToArray(),Width,(start,count)=>Width(text.Substring(start,count)));
        CollectionAssert.AreEqual(new[]{"One","overs","izedw","ord","next","e\u0301e\u0301e\u0301e\u0301"},model.WrapBreakWords(5).ToArray());
    }
    private static string Hash(string path)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
}
