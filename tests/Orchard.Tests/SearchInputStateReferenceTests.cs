using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class SearchInputStateReferenceTests
{
    [TestMethod]
    public void FrozenBrowserSeparatesSpaceReleaseEnterDownAndPressedEdgeActivation()
    {
        using var document=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","SearchInputStates.json")));var root=document.RootElement;
        var script=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","CaptureSearchInputStates.cjs")).Replace("\r\n","\n");Assert.AreEqual(root.GetProperty("scriptSha256").GetString(),Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(script))));Assert.IsTrue(root.GetProperty("freshMountPerCase").GetBoolean());Assert.AreEqual(48,root.GetProperty("cases").GetArrayLength());
        foreach(var test in root.GetProperty("cases").EnumerateArray())
        {
            var id=test.GetProperty("id").GetString()!;var state=test.GetProperty("state");var clear=state.GetProperty("clear");var held=test.GetProperty("props").TryGetProperty("hold",out _);
            Assert.AreEqual(18,clear.GetProperty("layoutBox").GetProperty("width").GetDouble());Assert.AreEqual("9px 9px",clear.GetProperty("paint").GetProperty("transformOrigin").GetString());Assert.AreEqual("4px",clear.GetProperty("paint").GetProperty("radius").GetString());
            Assert.AreEqual(id=="clear-enter-pressed"?1:0,state.GetProperty("clearActions").GetInt32());Assert.AreEqual(held?1:0,test.GetProperty("released").GetProperty("clearActions").GetInt32());Assert.AreEqual("Culture",test.GetProperty("released").GetProperty("value").GetString());
            var scale=held&&id!="clear-enter-pressed"?.92:1;Assert.AreEqual(scale.ToString(System.Globalization.CultureInfo.InvariantCulture),clear.GetProperty("paint").GetProperty("scale").GetString());Assert.AreEqual(18*scale,clear.GetProperty("box").GetProperty("width").GetDouble(),.0001);
            foreach(var input in test.GetProperty("released").GetProperty("inputEvents").EnumerateArray())Assert.IsTrue(input.GetProperty("trusted").GetBoolean());
            if(id=="clear-focus")Assert.AreEqual("rgb(16, 16, 16) auto 1px",clear.GetProperty("paint").GetProperty("outline").GetString());
        }
    }
}
