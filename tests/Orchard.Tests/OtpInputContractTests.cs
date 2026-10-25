using System.Text.Json;
namespace Orchard.Tests;

[TestClass]
public sealed class OtpInputContractTests
{
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","OtpInput.json")));
    [TestMethod]
    public void EveryCapturedCallerValueDisplaysWithoutSanitizingTheOriginal()
    {
        using var reference=Reference();foreach(var row in reference.RootElement.GetProperty("cases").EnumerateArray())
        {
            var value=row.GetProperty("value").GetString()!;var slots=row.GetProperty("slots");
            for(var i=0;i<slots.GetArrayLength();i++)Assert.AreEqual(slots[i].GetProperty("value").GetString(),BleeckerOtpModel.Display(value,slots.GetArrayLength(),i));
        }
    }
    [TestMethod]
    public void NativeModelMatchesAcceptedAndRejectedBrowserEditRequests()
    {
        using var reference=Reference();foreach(var h in reference.RootElement.GetProperty("histories").EnumerateArray().Where(h=>h.GetProperty("samples").GetArrayLength()==12))
        {
            var samples=h.GetProperty("samples").EnumerateArray().ToDictionary(s=>s.GetProperty("id").GetString()!);string Value(string id)=>samples[id].GetProperty("value").GetString()!;
            void Check(string id,BleeckerOtpModel.Edit edit)
            {
                Assert.AreEqual(edit.RequestedValue,samples[id].GetProperty("requests").EnumerateArray().Last().GetString(),id);
                if(edit.FocusIndex.HasValue)Assert.AreEqual(edit.FocusIndex.Value,samples[id].GetProperty("focused").GetInt32(),id);
            }
            Check("replace-one",BleeckerOtpModel.Change(Value("initial"),6,1,"7"));
            Check("backspace-filled",BleeckerOtpModel.Backspace(Value("replace-one"),6,2));
            Check("backspace-empty",BleeckerOtpModel.Backspace(Value("backspace-filled"),6,4));
            Check("paste-global",BleeckerOtpModel.Paste("a9２8-7 b6 5 4 3",6));
            Check("paste-empty",BleeckerOtpModel.Paste("letters only",6));
            Check("reject-nondigit",BleeckerOtpModel.Change("123456",6,0,"x"));
        }
    }
    [TestMethod]
    public void BrowserBoundaryUsesAsciiDigitsAndEcmaWhitespace()
    {
        using var reference=Reference();foreach(var h in reference.RootElement.GetProperty("histories").EnumerateArray().Where(h=>h.TryGetProperty("modelBoundary",out _)))
        {
            var s=h.GetProperty("samples");var original=s[0].GetProperty("value").GetString()!;
            Assert.AreEqual(s[1].GetProperty("requests")[0].GetString(),BleeckerOtpModel.Change(original,6,4,"a7b3").RequestedValue);
            Assert.AreEqual(s[2].GetProperty("requests")[1].GetString(),BleeckerOtpModel.Paste("Ａ1٢2\n3",6).RequestedValue);
        }
        Assert.AreEqual("",BleeckerOtpModel.Display("\ufeff",1,0));Assert.AreEqual("\u0085",BleeckerOtpModel.Display("\u0085",1,0));
    }
    [TestMethod]
    public void SourceCompactionDoesNotInventPaddedOrPositionalValues()
    {
        Assert.AreEqual("127",BleeckerOtpModel.Change("12",6,5,"7").RequestedValue);
        Assert.AreEqual("13456",BleeckerOtpModel.Backspace("123456",6,1).RequestedValue);
        Assert.AreEqual("123",BleeckerOtpModel.Paste("a1２2٣3",6).RequestedValue);
        Assert.AreEqual("923456",BleeckerOtpModel.Change("123456789",6,0,"9").RequestedValue);
        Assert.IsNull(BleeckerOtpModel.Backspace("",6,0).RequestedValue);
    }
    [TestMethod]
    public void ZeroLengthAndInvalidNativeArgumentsStayExplicit()
    {
        Assert.AreEqual("",BleeckerOtpModel.Paste("123",0).RequestedValue);Assert.IsNull(BleeckerOtpModel.Paste("123",0).FocusIndex);
        Assert.Throws<ArgumentOutOfRangeException>(()=>BleeckerOtpModel.Display("123",0,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>BleeckerOtpModel.Validate("",-1));
        Assert.Throws<ArgumentNullException>(()=>BleeckerOtpModel.Validate(null!,6));
    }
}
