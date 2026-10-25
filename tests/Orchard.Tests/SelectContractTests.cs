using System.Security.Cryptography;
using System.Text.Json;
namespace Orchard.Tests;
[TestClass]
public sealed class SelectContractTests
{
    private static JsonDocument Read()=>JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","Select.json")));
    private static BleeckerSelectOption[] Options(JsonElement spec,bool header)
    {
        var original=spec.TryGetProperty("options",out var options)?options.EnumerateArray().Select(value=>new BleeckerSelectOption(value.GetProperty("value").GetString()!,value.GetProperty("label").GetString()!,value.TryGetProperty("disabled",out var disabled)&&disabled.GetBoolean())).ToArray():[new("a","Library Alpha"),new("b","Library Beta"),new("disabled","Disabled library",true)];
        var placeholder=spec.TryGetProperty("placeholder",out var p)?p.GetString()!:header?"Select option":"Select…";return header&&!original.Any(option=>option.Value=="")?[new("",placeholder),..original]:original;
    }
    [TestMethod]
    public void OriginalSelectHeaderDependenciesCollectorFontsAndAllScreenshotsStayBound()
    {
        using var document=Read();var root=document.RootElement;Assert.AreEqual("874ad668945afb6f045d881447a275f73b9dc84ffbf56f4beed02a83f8d3fecd",root.GetProperty("sourceSha256").GetString());Assert.AreEqual("848685cb40ba19fdcf8991b0332f98f447b8bd2af6e7febcfc7c27e9517d8f2e",root.GetProperty("headerSourceSha256").GetString());Assert.AreEqual(292,root.GetProperty("cases").GetArrayLength());Assert.AreEqual(32,root.GetProperty("openCases").GetArrayLength());
        foreach(var(file,key)in new[]{("CaptureSelect.cjs","scriptSha256"),("SelectClient.tsx","clientSha256")})Assert.AreEqual(root.GetProperty(key).GetString(),Hash(Path.Combine(AppContext.BaseDirectory,"Fixtures",file)));
        var pngs=0;foreach(var row in root.GetProperty("cases").EnumerateArray().Concat(root.GetProperty("openCases").EnumerateArray()))if(row.TryGetProperty("file",out var file)){Assert.AreEqual(row.GetProperty("pngSha256").GetString(),Hash(Path.Combine(AppContext.BaseDirectory,"Fixtures","Select",file.GetString()!)));pngs++;}Assert.AreEqual(92,pngs);foreach(var font in root.GetProperty("fonts").EnumerateArray())Assert.AreEqual(font.GetProperty("sha256").GetString(),Hash(Path.Combine(AppContext.BaseDirectory,"Fixtures",font.GetProperty("name").GetString()!)));
    }
    [TestMethod]
    public void EmptyPlaceholderUnknownAndDuplicateSentinelDisplayMatchEveryOriginalState()
    {
        using var document=Read();foreach(var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec=row.GetProperty("spec");var header=spec.GetProperty("header").GetBoolean();var options=Options(spec,header);var value=spec.TryGetProperty("value",out var v)?v.GetString()!:"";var placeholder=spec.TryGetProperty("placeholder",out var p)?p.GetString()!:header?"Select option":"Select…";var state=new BleeckerSelectModel();
            Assert.AreEqual(row.GetProperty("measurement").GetProperty("value").GetProperty("text").GetString(),state.Display(value,placeholder,options),row.GetProperty("id").GetString());Assert.AreEqual(row.GetProperty("measurement").GetProperty("placeholder").GetBoolean(),state.Effective(value,options)is null);
        }
    }
    [TestMethod]
    public void SourceRequestsPreserveControlledHeaderAndOrdinaryUncontrolledHistory()
    {
        using var document=Read();Assert.AreEqual(8,document.RootElement.GetProperty("interactions").GetArrayLength());foreach(var row in document.RootElement.GetProperty("interactions").EnumerateArray())
        {
            var header=row.GetProperty("header").GetBoolean();var options=new[]{new BleeckerSelectOption("a","Library Alpha"),new BleeckerSelectOption("b","Library Beta"),new BleeckerSelectOption("disabled","Disabled library",true)};if(header)options=[new("","Select option"),..options];var state=new BleeckerSelectModel();var value="";var placeholder=header?"Select option":"Select…";var original=options.Single(option=>option.Value=="b");var request=state.Request(value,options,original);Assert.AreEqual("b",request);if(row.GetProperty("accept").GetBoolean())value=request!;
            Assert.AreEqual(row.GetProperty("first").GetProperty("value").GetProperty("text").GetString(),state.Display(value,placeholder,options));Assert.AreEqual(row.GetProperty("controlled").GetProperty("value").GetProperty("text").GetString(),state.Display("a",placeholder,options));Assert.AreEqual(row.GetProperty("released").GetProperty("value").GetProperty("text").GetString(),state.Display("",placeholder,options));Assert.AreEqual("b",original.Value);Assert.AreEqual("Library Beta",original.Label);
        }
    }
    [TestMethod]
    public void DisabledStaleSameValueAndSentinelRequestsStayAtTheOriginalBoundary()
    {
        var a=new BleeckerSelectOption("a","A");var disabled=new BleeckerSelectOption("disabled","Disabled",true);var empty=new BleeckerSelectOption("","Empty");var sentinel=new BleeckerSelectOption(BleeckerSelectModel.EmptyOption,"Sentinel");var options=new[]{a,disabled,empty,sentinel};var state=new BleeckerSelectModel();Assert.IsNull(state.Request("a",options,a));Assert.IsNull(state.Request("a",options,disabled));Assert.IsNull(state.Request("a",options,new("b","Stale")));Assert.AreEqual("",state.Request("a",options,sentinel));Assert.AreEqual("EmptySentinel",state.Display("","Placeholder",options));Assert.IsNull(state.Request("",options,empty));Assert.AreEqual("a",a.Value);Assert.AreEqual(BleeckerSelectModel.EmptyOption,sentinel.Value);
    }
    [TestMethod]
    public void SourceTriggerSizesAndPaddingStayExact()
    {
        using var document=Read();foreach(var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec=row.GetProperty("spec");var header=spec.GetProperty("header").GetBoolean();var size=header?0:spec.TryGetProperty("size",out var s)?s.GetString()switch{"sm"=>0,"md"=>1,"lg"=>2,_=>throw new AssertFailedException()}:1;var expected=row.GetProperty("measurement");var metrics=BleeckerSelectModel.Sizes(size);Assert.AreEqual(expected.GetProperty("trigger").GetProperty("height").GetDouble(),metrics.Height);Assert.AreEqual(expected.GetProperty("fontSize").GetDouble(),metrics.Font);Assert.AreEqual(expected.GetProperty("lineHeight").GetDouble(),metrics.Line);Assert.AreEqual(expected.GetProperty("padding")[0].GetDouble(),metrics.Padding);Assert.AreEqual(expected.GetProperty("gap").GetDouble(),metrics.Gap);
        }
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(()=>BleeckerSelectModel.Sizes(3));
    }
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
}
