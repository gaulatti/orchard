using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;
[TestClass]
public sealed class AccordionContractTests
{
    private static JsonDocument Reference()=>JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","Accordion.json")));
    [TestMethod]
    public void SourceCapturesBothThemesAndTheExactResponsiveBoundary()
    {
        using var document=Reference();var root=document.RootElement;Assert.AreEqual("5160176bd8dd07041af0c5dee9c6647fef47d132bac95384128a78822a6e0cc0",root.GetProperty("sourceSha256").GetString());var cases=root.GetProperty("cases");Assert.AreEqual(60,cases.GetArrayLength());
        foreach(var frame in cases.EnumerateArray())
        {
            var width=frame.GetProperty("width").GetInt32();Assert.IsTrue(width is 320 or 639 or 640);var box=frame.GetProperty("measurement").GetProperty("box");Assert.AreEqual(width-40,box.GetProperty("width").GetDouble());
            var rows=frame.GetProperty("measurement").GetProperty("rows");if(frame.GetProperty("id").GetString()=="empty"){Assert.AreEqual(0,rows.GetArrayLength());Assert.AreEqual(2,box.GetProperty("height").GetDouble());continue;}
            var first=rows[0];Assert.AreEqual(width<640?44:48,first.GetProperty("title").GetProperty("box").GetProperty("x").GetDouble()-first.GetProperty("button").GetProperty("x").GetDouble());
            if(frame.GetProperty("id").GetString()=="zero-subtitle")Assert.AreEqual(JsonValueKind.Null,first.GetProperty("subtitle").ValueKind);
        }
    }
    [TestMethod]
    public void SourceActionsAndDefaultUpdatesKeepTheirSeparateStateBoundaries()
    {
        using var document=Reference();var cases=document.RootElement.GetProperty("interactions");Assert.AreEqual(6,cases.GetArrayLength());foreach(var sequence in cases.EnumerateArray())
        {
            var states=sequence.GetProperty("states");Assert.AreEqual(6,states.GetArrayLength());CollectionAssert.AreEqual(new[]{true,false},states[0].GetProperty("expanded").EnumerateArray().Select(row=>row.GetBoolean()).ToArray());CollectionAssert.AreEqual(new[]{true,false},states[1].GetProperty("expanded").EnumerateArray().Select(row=>row.GetBoolean()).ToArray());Assert.AreEqual("first",states[1].GetProperty("actions")[0].GetString());CollectionAssert.AreEqual(new[]{false,true},states[2].GetProperty("expanded").EnumerateArray().Select(row=>row.GetBoolean()).ToArray());foreach(var index in new[]{3,4})CollectionAssert.AreEqual(new[]{false,false},states[index].GetProperty("expanded").EnumerateArray().Select(row=>row.GetBoolean()).ToArray());Assert.AreEqual(false,states[5].GetProperty("expanded")[0].GetBoolean());
        }
    }
    [TestMethod]
    public void ContentModelsRetainConsumerObjectsAndNotifyLiveChanges()
    {
        var content=new object();var title=new object();var item=new BleeckerAccordionItem("  original  ",title,content);var names=new List<string?>();item.PropertyChanged+=(_,args)=>names.Add(args.PropertyName);Assert.AreSame(title,item.Title);Assert.AreSame(content,item.Content);Assert.AreEqual("  original  ",item.Id);item.Content=content;item.Title="Updated";item.Subtitle=0;item.Actions=title;item.Id="second";CollectionAssert.AreEqual(new[]{"Title","Subtitle","Actions","Id"},names.ToArray());Assert.ThrowsExactly<ArgumentNullException>(()=>item.Id=null!);
    }
    [TestMethod]
    public void SourceReferenceRetainsCollectorAndClientBytes()
    {
        using var document=Reference();foreach(var(file,field)in new[]{("CaptureAccordion.cjs","scriptSha256"),("AccordionClient.tsx","clientSha256")})Assert.AreEqual(document.RootElement.GetProperty(field).GetString(),Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures",file)))));
        foreach(var frame in document.RootElement.GetProperty("cases").EnumerateArray()){
            Assert.AreEqual(frame.GetProperty("pngSha256").GetString(),Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","Accordion",frame.GetProperty("file").GetString()!)))));
            if(frame.GetProperty("id").GetString()=="rich"){var fonts=frame.GetProperty("platformFonts");Assert.AreEqual("EncodeSans-SmBold",fonts[0].GetProperty("fonts")[0].GetProperty("postScriptName").GetString());Assert.IsTrue(fonts[0].GetProperty("fonts")[0].GetProperty("isCustomFont").GetBoolean());}
        }
    }
}
