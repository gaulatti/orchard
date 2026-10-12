namespace Orchard.Tests;

[TestClass]
public sealed class TextCasingTests
{
    [TestMethod]
    [DataRow("Straße", "STRASSE")]
    [DataRow("ﬃ · ﬄ", "FFI · FFL")]
    [DataRow("ΐ", "Ι\u0308\u0301")]
    [DataRow("և", "ԵՒ")]
    [DataRow("Café · 東京 · 👩🏽‍💻", "CAFÉ · 東京 · 👩🏽‍💻")]
    [DataRow("", "")]
    public void UppercasePreservesFullMappingsAndUncasedContent(string source, string expected) =>
        Assert.AreEqual(expected, BleeckerTextCasing.Uppercase(source));
}
