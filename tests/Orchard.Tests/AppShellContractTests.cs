using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Orchard.Tests;
[TestClass]
public sealed class AppShellContractTests
{
    private static JsonDocument Read()=>JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","AppShell.json")));
    private static string Hash(string path)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
    [TestMethod]
    public void FrozenSourcesCollectorFontsAndAllCompletePngsAreBound()
    {
        using var document=Read();var root=document.RootElement;
        Assert.AreEqual("21cbb676b184bcad28ffb5577b19a1ccc34d18ab420221b08cf936a23919c3e1",root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(168,root.GetProperty("cases").GetArrayLength());
        foreach(var(file,key)in new[]{("CaptureAppShell.cjs","scriptSha256"),("AppShellClient.tsx","clientSha256")})Assert.AreEqual(root.GetProperty(key).GetString(),Hash(Path.Combine(AppContext.BaseDirectory,"Fixtures",file)));
        var pngs=0;foreach(var row in root.GetProperty("cases").EnumerateArray())if(row.TryGetProperty("file",out var file)){Assert.AreEqual(row.GetProperty("pngSha256").GetString(),Hash(Path.Combine(AppContext.BaseDirectory,"Fixtures","AppShell",file.GetString()!)));pngs++;}Assert.AreEqual(56,pngs);
        foreach(var font in root.GetProperty("fonts").EnumerateArray())Assert.AreEqual(font.GetProperty("sha256").GetString(),Hash(Path.Combine(AppContext.BaseDirectory,"Fixtures",font.GetProperty("name").GetString()!)));
    }
    [TestMethod]
    public void OriginalMainContentFooterTracksPreserveSourceFlexAndPadding()
    {
        using var document=Read();foreach(var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var expected=row.GetProperty("measurement");var spec=row.GetProperty("spec");var shell=expected.GetProperty("shell");var main=expected.GetProperty("main");var content=expected.GetProperty("content");var shellPadding=spec.TryGetProperty("padding",out var p)?p.GetDouble():0;
            var header=main.GetProperty("y").GetDouble()-shell.GetProperty("y").GetDouble()-shellPadding;
            var footer=expected.GetProperty("footer");var footerHeight=footer.ValueKind==JsonValueKind.Null?(spec.TryGetProperty("footer",out var f)&&f.ValueKind is JsonValueKind.Number or JsonValueKind.String?24:0):footer.GetProperty("height").GetDouble();
            var insets=expected.GetProperty("paint").GetProperty("main").GetProperty("padding");static double Css(JsonElement value)=>double.Parse(value.GetString()![..^2],System.Globalization.CultureInfo.InvariantCulture);
            var tracks=BleeckerAppShellModel.Arrange(shell.GetProperty("height").GetDouble()-2*shellPadding,header,footerHeight,Css(insets[1]),Css(insets[3]));
            Assert.AreEqual(main.GetProperty("height").GetDouble(),tracks.MainHeight,0.00001,row.GetProperty("id").GetString());
            Assert.AreEqual(content.GetProperty("y").GetDouble()-shell.GetProperty("y").GetDouble()-shellPadding,tracks.ContentY,0.00001);
            Assert.AreEqual(content.GetProperty("height").GetDouble(),tracks.ContentHeight,0.00001);
            if(footer.ValueKind!=JsonValueKind.Null)Assert.AreEqual(footer.GetProperty("y").GetDouble()-shell.GetProperty("y").GetDouble()-shellPadding,tracks.FooterY,0.00001);
        }
    }
    [TestMethod]
    public void ActualSourceUpdatesRetainEditedContentAndAction()
    {
        using var document=Read();var interactions=document.RootElement.GetProperty("interactions");Assert.AreEqual(2,interactions.GetArrayLength());foreach(var row in interactions.EnumerateArray()){Assert.IsTrue(row.GetProperty("inputRetained").GetBoolean());Assert.AreEqual("Edited shell input",row.GetProperty("value").GetString());Assert.AreEqual("body",row.GetProperty("events")[0].GetString());Assert.AreEqual(1,row.GetProperty("events").GetArrayLength());}
    }
}
