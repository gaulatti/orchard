using System.Security.Cryptography;
using System.Text.Json;
namespace Orchard.Tests;
[TestClass]
public sealed class SliderContractTests
{
    private static JsonDocument Read() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Slider.json")));
    private static double Number(JsonElement spec, string key, double otherwise) => spec.TryGetProperty(key + "Kind", out var kind) ? kind.GetString() switch { "nan" => double.NaN, "infinity" => double.PositiveInfinity, "negative-infinity" => double.NegativeInfinity, _ => throw new InvalidOperationException() } : spec.TryGetProperty(key, out var value) ? value.GetDouble() : otherwise;
    private static BleeckerSliderModel.Range Range(JsonElement spec, double? value = null) => BleeckerSliderModel.Normalize(Number(spec,"min",0),Number(spec,"max",100),Number(spec,"step",1),value??Number(spec,"value",0));
    [TestMethod]
    public void OriginalComponentClientCollectorFontsAndCompletePngsRemainBound()
    {
        using var document=Read();var root=document.RootElement;Assert.AreEqual("b5ab4cb0ed7af06317843f42934f640a90da62eed20c0fc59a166e071faf6b6a",root.GetProperty("sourceSha256").GetString());Assert.AreEqual(182,root.GetProperty("cases").GetArrayLength());
        foreach(var(file,key)in new[]{("CaptureSlider.cjs","scriptSha256"),("SliderClient.tsx","clientSha256")})Assert.AreEqual(root.GetProperty(key).GetString(),Hash(Path.Combine(AppContext.BaseDirectory,"Fixtures",file)));
        var pngs=0;foreach(var row in root.GetProperty("cases").EnumerateArray())if(row.TryGetProperty("file",out var file)){Assert.AreEqual(row.GetProperty("pngSha256").GetString(),Hash(Path.Combine(AppContext.BaseDirectory,"Fixtures","Slider",file.GetString()!)));pngs++;}Assert.AreEqual(44,pngs);
    }
    [TestMethod]
    public void AuthoritativePropsAndSanitizedInputThumbRemainSeparate()
    {
        using var document=Read();foreach(var row in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec=row.GetProperty("spec");var expected=row.GetProperty("measurement");var range=Range(spec);var context=row.GetProperty("id").GetString();Assert.AreEqual(expected.GetProperty("actualValue").GetDouble(),range.Value,context);
            var style=new BleeckerSliderModel.FillStyle();style.Apply(Number(spec,"min",0),Number(spec,"max",100),Number(spec,"value",0));Assert.AreEqual(expected.GetProperty("fill").GetProperty("width").GetDouble(),style.Width(spec.GetProperty("width").GetDouble()),.001,context);
            var rtl=spec.TryGetProperty("rtl",out var direction)&&direction.GetBoolean();var thumb=row.GetProperty("thumb").GetProperty("box");var x=expected.GetProperty("row").GetProperty("x").GetDouble()+BleeckerSliderModel.ThumbLeft(spec.GetProperty("width").GetDouble(),range,rtl);Assert.AreEqual(thumb.GetProperty("x").GetDouble(),x,.001,context);
            if(spec.TryGetProperty("showValue",out var visible)&&visible.GetBoolean())Assert.AreEqual(expected.GetProperty("display").GetProperty("text").GetString(),BleeckerNumberText.Format(Number(spec,"value",0)),context);
        }
    }
    [TestMethod]
    public void NativeStyleAssignmentsRetainTheSourceCssHistory()
    {
        using var document=Read();foreach(var scheme in new[]{"Light","Dark"}){var style=new BleeckerSliderModel.FillStyle();foreach(var row in document.RootElement.GetProperty("history").EnumerateArray().Where(r=>r.GetProperty("scheme").GetString()==scheme))
        {var spec=row.GetProperty("spec");style.Apply(Number(spec,"min",0),Number(spec,"max",100),Number(spec,"value",0));Assert.AreEqual(row.GetProperty("measurement").GetProperty("fillWidth").GetDouble(),style.Width(spec.GetProperty("width").GetDouble()),.001,row.GetProperty("id").GetString());}}
    }
    [TestMethod]
    public void EverySourceKeyboardRequestAndFrozenControlledValueMatch()
    {
        using var document=Read();foreach(var row in document.RootElement.GetProperty("interactions").EnumerateArray())
        {
            var spec=row.GetProperty("spec");var value=Number(spec,"value",0);var frozen=spec.TryGetProperty("freeze",out var freeze)&&freeze.GetBoolean();var rtl=spec.TryGetProperty("rtl",out var direction)&&direction.GetBoolean();var requested=new List<double>();
            Assert.AreEqual(row.GetProperty("before").GetDouble(),Range(spec,value).Value);
            foreach(var sample in row.GetProperty("samples").EnumerateArray()){var range=Range(spec,value);var next=BleeckerSliderModel.Key(range,sample.GetProperty("key").GetString()!,rtl);if(next!=range.Value){requested.Add(next);if(!frozen)value=next;}Assert.AreEqual(sample.GetProperty("value").GetDouble(),Range(spec,value).Value,row.GetProperty("id").GetString()+"/"+sample.GetProperty("key").GetString());}
            CollectionAssert.AreEqual(row.GetProperty("requests").EnumerateArray().Select(v=>v.GetDouble()).ToArray(),requested.ToArray(),row.GetProperty("id").GetString());
        }
    }
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
}
