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
    [DataRow("\u1C8A", "\u1C89")]
    [DataRow("\U00010D70", "\U00010D50")]
    public void UppercasePreservesFullMappingsAndUncasedContent(string source, string expected) =>
        Assert.AreEqual(expected, BleeckerTextCasing.Uppercase(source));

    [TestMethod]
    [DataRow("istanbul", "tr-TR", "İSTANBUL")]
    [DataRow("istanbul", "en-US", "ISTANBUL")]
    public void UppercaseUsesTheRequestedLanguage(string source, string language, string expected) =>
        Assert.AreEqual(expected, BleeckerTextCasing.Uppercase(source, language));
}
