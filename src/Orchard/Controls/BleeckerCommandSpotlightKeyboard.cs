using System.Runtime.InteropServices;
using System.Text;

namespace Orchard;

internal static class BleeckerCommandSpotlightKeyboard
{
    internal const int Shift = 1, Control = 2, Alt = 4, CapsLock = 8, Meta = 16, AltGraph = 32;
    internal readonly record struct Key(string Name, int Modifiers);
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("user32.dll", ExactSpelling = true)] private static extern nint GetKeyboardLayout(uint thread);
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int ToUnicodeEx(uint key, uint scan, byte[] state, [Out] char[] text, int capacity, uint flags, nint layout);

    internal static Key Read(uint key, uint scan, bool extended, int modifiers)
    {
        var layout = GetKeyboardLayout(0);
        if (layout == 0) throw new InvalidOperationException("The current thread has no keyboard layout.");
        return ReadLayout(key, scan, extended, modifiers, layout);
    }
    internal static Key ReadLayout(uint key, uint scan, bool extended, int modifiers, nint layout)
    {
        if (layout == 0) throw new ArgumentException("A current installed keyboard layout is required.", nameof(layout));
        (int Count, string Text) Translate(int flags) => TranslateLayout(key, scan, flags, layout);
        // Only the Alt modifier's name needs a layout-wide AltGraph capability check.
        var altGraph = extended && (key is 0x12 or 0xa5) && UsesAltGraph(layout);
        return Resolve(key, modifiers, extended, altGraph, unchecked((ushort)(long)layout), Translate);
    }
    private static (int Count, string Text) TranslateLayout(uint key, uint scan, int modifiers, nint layout)
    {
        var state = new byte[256];
        if ((modifiers & Shift) != 0) state[0x10] = 0x80;
        if ((modifiers & Control) != 0) state[0x11] = 0x80;
        if ((modifiers & Alt) != 0) state[0x12] = 0x80;
        if ((modifiers & CapsLock) != 0) state[0x14] = 1;
        var text = new char[256];
        // Bit 2 leaves the keyboard/dead-key buffer unchanged (Windows 10 1607+).
        // No keyboard layout is loaded/activated and no keyboard state is written.
        var count = ToUnicodeEx(key, scan, state, text, text.Length, 4, layout);
        if (Math.Abs(count) > text.Length) throw new InvalidOperationException("Keyboard translation exceeded its native buffer.");
        return (count, count == 0 ? "" : new string(text, 0, Math.Abs(count)));
    }
    private static bool UsesAltGraph(nint layout)
    {
        for (uint key = 0; key <= 255; key++)
            foreach (var flags in new[] { Control | Alt, Control | Alt | Shift, Control | Alt | CapsLock, Control | Alt | Shift | CapsLock })
            {
                var result = TranslateLayout(key, 0, flags, layout);
                if (result.Count > 0 && Printable(result.Text)) return true;
            }
        return false;
    }
    internal static Key Resolve(uint key, int modifiers, bool extended, bool usesAltGraph, ushort language,
        Func<int, (int Count, string Text)> translate)
    {
        if ((key is 0x12 or 0xa5) && extended && usesAltGraph) return new("AltGraph", modifiers);
        if (Named(key, language) is { } named) return new(named, modifiers);
        // UI Events selects a character with the active modifiers, then with
        // Shift/Caps only, then without modifiers. Never assume a US layout.
        var previous = -1;
        foreach (var flags in new[] { modifiers & 15, modifiers & (Shift | CapsLock), 0 })
        {
            if (flags == previous) continue; previous = flags;
            var result = translate(flags);
            if (result.Count < 0) return new("Dead", EffectiveModifiers(modifiers, flags));
            if (result.Count > 0 && Printable(result.Text))
            {
                var effective = EffectiveModifiers(modifiers, flags);
                return new(result.Text.Normalize(NormalizationForm.FormC), effective);
            }
        }
        return new("Unidentified", modifiers);
    }
    private static bool Printable(string text) => text.Length > 0 && text.All(c => !char.IsControl(c));
    private static int EffectiveModifiers(int modifiers, int translatedFlags) => (translatedFlags & (Control | Alt)) == (Control | Alt)
        ? (modifiers & ~(Control | Alt)) | AltGraph : modifiers;
    internal static string? Named(uint key, ushort language = 0)
    {
        var primary = language & 0x3ff;
        if (key == 0x15) return primary == 0x12 ? "HangulMode" : primary == 0x11 ? "KanaMode" : null;
        if (key == 0x19) return primary == 0x12 ? "HanjaMode" : primary == 0x11 ? "KanjiMode" : null;
        if (primary == 0x11 && key is >= 0xf0 and <= 0xf6)
            return key switch { 0xf0 => "Alphanumeric", 0xf1 => "Katakana", 0xf2 => "Hiragana", 0xf3 => "Hankaku", 0xf4 => "Zenkaku", 0xf5 => "Romaji", _ => "KanaMode" };
        if (key is >= 0x70 and <= 0x87) return "F" + (key - 0x6f);
        return key switch
        {
            0x03 => "Cancel", 0x08 => "Backspace", 0x09 => "Tab", 0x0c => "Clear", 0x0d => "Enter",
            0x10 or 0xa0 or 0xa1 => "Shift", 0x11 or 0xa2 or 0xa3 => "Control", 0x12 or 0xa4 or 0xa5 => "Alt",
            0x13 => "Pause", 0x14 => "CapsLock", 0x17 => "JunjaMode", 0x18 => "FinalMode", 0x1b => "Escape",
            0x1c => "Convert", 0x1d => "NonConvert", 0x1e => "Accept", 0x1f => "ModeChange",
            0x21 => "PageUp", 0x22 => "PageDown", 0x23 => "End", 0x24 => "Home", 0x25 => "ArrowLeft",
            0x26 => "ArrowUp", 0x27 => "ArrowRight", 0x28 => "ArrowDown", 0x29 => "Select", 0x2a => "Print",
            0x2b => "Execute", 0x2c => "PrintScreen", 0x2d => "Insert", 0x2e => "Delete", 0x2f => "Help",
            0x5b or 0x5c => "Meta", 0x5d => "ContextMenu", 0x5f => "Standby", 0x90 => "NumLock", 0x91 => "ScrollLock",
            0xa6 => "BrowserBack", 0xa7 => "BrowserForward", 0xa8 => "BrowserRefresh", 0xa9 => "BrowserStop",
            0xaa => "BrowserSearch", 0xab => "BrowserFavorites", 0xac => "BrowserHome",
            0xad => "AudioVolumeMute", 0xae => "AudioVolumeDown", 0xaf => "AudioVolumeUp", 0xb0 => "MediaTrackNext",
            0xb1 => "MediaTrackPrevious", 0xb2 => "MediaStop", 0xb3 => "MediaPlayPause", 0xb4 => "LaunchMail",
            0xb5 => "LaunchMediaPlayer", 0xb6 => "LaunchMyComputer", 0xb7 => "LaunchCalculator", 0xe5 => "Process",
            0xf6 => "Attn", 0xf7 => "CrSel", 0xf8 => "ExSel", 0xf9 => "EraseEof", 0xfa => "Play", 0xfb => "ZoomToggle", 0xfe => "Clear", _ => null
        };
    }
}
