namespace Orchard.Tests;

[TestClass]
public sealed class CommandSpotlightKeyboardTests
{
    private const int Shift = BleeckerCommandSpotlightKeyboard.Shift, Control = BleeckerCommandSpotlightKeyboard.Control,
        Alt = BleeckerCommandSpotlightKeyboard.Alt, Caps = BleeckerCommandSpotlightKeyboard.CapsLock, Meta = BleeckerCommandSpotlightKeyboard.Meta;
    [TestMethod]
    public void BrowserNamedKeysDoNotDependOnWindowsEnumNamesOrCharacterTranslation()
    {
        foreach (var (key, expected) in new (uint, string)[] { (0x08,"Backspace"),(0x26,"ArrowUp"),(0x28,"ArrowDown"),(0x25,"ArrowLeft"),(0x27,"ArrowRight"),(0x90,"NumLock"),(0x14,"CapsLock"),(0x12,"Alt"),(0x5b,"Meta"),(0x5d,"ContextMenu"),(0xad,"AudioVolumeMute"),(0xb0,"MediaTrackNext"),(0xe5,"Process"),(0x87,"F24") })
        {
            var actual = BleeckerCommandSpotlightKeyboard.Resolve(key, Control, false, false, 0x409, _ => throw new AssertFailedException("Named keys must not translate characters"));
            Assert.AreEqual(expected, actual.Name); Assert.AreEqual(Control, actual.Modifiers);
        }
    }
    [TestMethod]
    public void CurrentLayoutAndShiftCapsSelectTheBrowserCharacterWithoutControlCharacters()
    {
        var tried = new List<int>();
        var result = BleeckerCommandSpotlightKeyboard.Resolve(0xbf, Control | Shift | Caps | Meta, false, false, 0x409,
            flags => { tried.Add(flags); return flags == (Shift | Caps) ? (1,"?") : (1,"\u001f"); });
        Assert.AreEqual("?", result.Name); Assert.AreEqual(Control | Shift | Caps | Meta, result.Modifiers);
        CollectionAssert.AreEqual(new[] { Control | Shift | Caps, Shift | Caps }, tried);
    }
    [TestMethod]
    public void AltGraphCharactersAreNotReportedAsControlShortcuts()
    {
        var actual = BleeckerCommandSpotlightKeyboard.Resolve(0x51, Control | Alt, false, false, 0x407, _ => (1,"@"));
        Assert.AreEqual("@", actual.Name); Assert.AreEqual(BleeckerCommandSpotlightKeyboard.AltGraph, actual.Modifiers);
        actual = BleeckerCommandSpotlightKeyboard.Resolve(0x51, Control | Alt | Meta, false, false, 0x407, _ => (1,"@"));
        Assert.AreEqual(Meta | BleeckerCommandSpotlightKeyboard.AltGraph, actual.Modifiers);
        actual = BleeckerCommandSpotlightKeyboard.Resolve(0xde,Control|Alt,false,false,0x407,_=>(-1,"´"));
        Assert.AreEqual("Dead",actual.Name);Assert.AreEqual(BleeckerCommandSpotlightKeyboard.AltGraph,actual.Modifiers);
    }
    [TestMethod]
    public void DeadKeysAndUnidentifiedKeysKeepTheirBrowserNamesWithoutUsLayoutSubstitution()
    {
        Assert.AreEqual("Dead", BleeckerCommandSpotlightKeyboard.Resolve(0xde, Control, false, false, 0x409, _ => (-1,"´")).Name);
        var calls = new List<int>();var actual = BleeckerCommandSpotlightKeyboard.Resolve(0xba, Control | Shift, false, false, 0x409, flags => { calls.Add(flags); return (0,""); });
        Assert.AreEqual("Unidentified",actual.Name);CollectionAssert.AreEqual(new[] { Control | Shift, Shift, 0 },calls);
    }
    [TestMethod]
    public void LayoutSpecificNamedKeysAndRightAltStayDistinct()
    {
        Assert.AreEqual("HangulMode", BleeckerCommandSpotlightKeyboard.Named(0x15,0x412));
        Assert.AreEqual("KanaMode", BleeckerCommandSpotlightKeyboard.Named(0x15,0x411));
        Assert.AreEqual("HanjaMode", BleeckerCommandSpotlightKeyboard.Named(0x19,0x412));
        Assert.AreEqual("KanjiMode", BleeckerCommandSpotlightKeyboard.Named(0x19,0x411));
        Assert.AreEqual("Alphanumeric", BleeckerCommandSpotlightKeyboard.Named(0xf0,0x411));
        Assert.AreEqual("AltGraph", BleeckerCommandSpotlightKeyboard.Resolve(0x12,Control|Alt,true,true,0x407,_=>throw new AssertFailedException()).Name);
        Assert.AreEqual("Alt", BleeckerCommandSpotlightKeyboard.Resolve(0x12,Control|Alt,false,true,0x407,_=>throw new AssertFailedException()).Name);
    }
    [TestMethod]
    public void LayoutCharactersKeepUnicodeAndCanonicalKeyNames()
    {
        Assert.AreEqual("é", BleeckerCommandSpotlightKeyboard.Resolve(0xba,Control,false,false,0x409,_=>(2,"e\u0301")).Name);
        Assert.AreEqual("𐐀", BleeckerCommandSpotlightKeyboard.Resolve(0xba,Control,false,false,0x409,_=>(2,"𐐀")).Name);
        Assert.AreEqual(" ", BleeckerCommandSpotlightKeyboard.Resolve(0x20,Control,false,false,0x409,_=>(1," ")).Name);
    }
}
