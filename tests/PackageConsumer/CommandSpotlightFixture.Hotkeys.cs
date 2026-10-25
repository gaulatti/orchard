using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Testing;

internal static partial class CommandSpotlightFixture
{
    [DllImport("user32.dll", ExactSpelling = true)] private static extern int GetKeyboardLayoutList(int size, [Out] nint[]? layouts);
    private static async Task VerifyCatalogHotkeyCycle(FrameworkElement fixture, Func<int, Task> layout, Action<bool, string> require)
    {
        var cycle = Elements(fixture).OfType<Button>().Single(b => b.Name == "HotkeyCycle");
        var control = Elements(fixture).OfType<BleeckerCommandSpotlight>().Single(c => c.Name == "Unicode");
        var original = control.Actions.First();
        foreach (var (hotkey, hint) in new[] { ("ArrowUp", "ARROWUP"), ("/", "/"), ("ß", "SS") })
        {
            ((Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(cycle)
                .GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)).Invoke(); await layout(40);
            var keys = Elements(control).OfType<BleeckerKbd>().Single(k => k.Name == "SpotlightTriggerHint").Keys!;
            require(control.Hotkey == hotkey && keys.SequenceEqual(["Ctrl", hint]) && ReferenceEquals(control.Actions.First(), original), "Catalog hotkey cycle retains caller data and renders full hint");
        }
    }
    private static async Task RunHotkeyContracts(BleeckerModalHost host, ElementTheme theme, string folder,
        Func<bool?, Task<BleeckerCommandSpotlight>> create, Func<int, Task> layout, Action<double, double> resize,
        Func<string, Task<string>> capture, List<object> logical, List<object> windowsKeys, List<object> caseSearch,
        List<object> readOnlyKeys, Action<bool, string> require)
    {
        using var reference = JsonDocument.Parse(await File.ReadAllBytesAsync(Path.Combine(folder, "hotkey-reference.json")));
        var current = await create(false); resize(1000, 240); await layout(50);
        var type = typeof(BleeckerCommandSpotlight); const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
        var process = type.GetMethod("ProcessHotkey", instance)!;
        var windows = type.GetMethod("ProcessWindowsHotkey", instance);
        var legacyName = type.GetMethod("KeyName", BindingFlags.Static | BindingFlags.NonPublic);
        var requests = new List<bool>(); var differences = new List<string>();
        current.OpenRequested += (_, open) => { requests.Add(open); current.Open = open; };
        bool Invoke(string key, bool control, bool meta, DependencyObject? target) => (bool)process.Invoke(current, [key, control, meta, target])!;
        string[] Hint() => Elements(current).OfType<BleeckerKbd>().Single(e => e.Name == "SpotlightTriggerHint").Keys!.ToArray();
        async Task Reset(string hotkey, bool open = false)
        {
            current.Open = false; current.SetQuery(""); current.Hotkey = hotkey; current.Open = open;
            await layout(open ? 45 : 25); requests.Clear();
        }
        foreach (var c in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("theme").GetString() == theme.ToString()))
        {
            var spec = c.GetProperty("spec"); var id = c.GetProperty("id").GetString()!;
            string Text(string name) => spec.GetProperty(name).GetString()!;
            bool Flag(string name) => spec.TryGetProperty(name, out var v) && v.GetBoolean();
            await Reset(Text("hotkey"), Flag("open"));
            var hint = Hint(); var expectedHint = c.GetProperty("before").GetProperty("hint").EnumerateArray().Select(k => k.GetString()!).ToArray();
            var renderedHint = Elements(Elements(current).OfType<BleeckerKbd>().Single(e => e.Name == "SpotlightTriggerHint"))
                .OfType<BleeckerKbd>().Where(k => k.Keys is null).Select(k => k.Content?.ToString() ?? "").ToArray();
            DependencyObject? target = null;
            if (Flag("open")) target = Elements(host).OfType<TextBox>().Single(e => e.Name == "SpotlightSearch");
            else if (spec.TryGetProperty("editor", out var editor)) target = editor.GetString() switch
            { "input" => new TextBox(), "textarea" => new TextBox { AcceptsReturn = true }, "select" => new ComboBox(), "editable" => new RichEditBox(), "button" => new Button(), _ => throw new InvalidOperationException("Unknown source editor") };
            var file = new[] { "ß", "ﬃ", "İ", "𐐀" }.Contains(Text("hotkey")) && id.StartsWith("literal-", StringComparison.Ordinal)
                ? await capture("command-spotlight-hotkeys-" + theme + "-" + id) : null;
            var handled = Invoke(Text("key"), Flag("ctrlKey"), Flag("metaKey"), target); await layout(25);
            var expectedRequests = c.GetProperty("requests").EnumerateArray().Select(v => v.GetBoolean()).ToArray();
            var correct = hint.SequenceEqual(expectedHint) && renderedHint.SequenceEqual(expectedHint) && handled == c.GetProperty("defaultPrevented").GetBoolean()
                && current.IsOpen == c.GetProperty("open").GetBoolean() && requests.SequenceEqual(expectedRequests);
            if (!correct) differences.Add("logical/" + id);
            logical.Add(new { theme = theme.ToString(), id, passed = correct, hint, renderedHint, handled, open = current.IsOpen, requests = requests.ToArray(), file, physicalKeyRoutingVerified = false });
        }
        foreach (var pair in new (uint Code, string Name)[] { (0x26,"ArrowUp"),(0x28,"ArrowDown"),(0x25,"ArrowLeft"),(0x27,"ArrowRight"),(0x70,"F1"),(0x87,"F24"),(0x08,"Backspace"),(0x14,"CapsLock"),(0x90,"NumLock"),(0x91,"ScrollLock"),(0x1b,"Escape"),(0x12,"Alt"),(0x5b,"Meta"),(0x5d,"ContextMenu"),(0x21,"PageUp"),(0x22,"PageDown"),(0x2e,"Delete"),(0x09,"Tab"),(0x0d,"Enter") })
        {
            await Reset(pair.Name); string? translated = null; bool handled;
            if (windows is not null) handled = (bool)windows.Invoke(current, [pair.Code, 0u, false, 2, null])!;
            else { translated = (string)legacyName!.Invoke(null, [(Windows.System.VirtualKey)pair.Code])!; handled = Invoke(translated, true, false, null); }
            await layout(25); var correct = handled && current.IsOpen && requests.SequenceEqual([true]);
            if (!correct) differences.Add("windows/" + pair.Name);
            windowsKeys.Add(new { theme = theme.ToString(), keyCode = pair.Code, expectedName = pair.Name, legacyName = translated, handled, open = current.IsOpen, passed = correct, physicalKeyRoutingVerified = false });
        }
        await VerifyHotkeyCaseSearch(current, host, theme, layout, caseSearch, differences);
        await VerifyReadOnlyKeyboard(type.Assembly, theme, readOnlyKeys, differences);
        require(differences.Count == 0, "Hotkey/casing boundary differences: " + string.Join(", ", differences.Take(24)));
    }
    private static async Task VerifyHotkeyCaseSearch(BleeckerCommandSpotlight control, BleeckerModalHost host, ElementTheme theme,
        Func<int, Task> layout, List<object> results, List<string> differences)
    {
        var samples = new[] { ("cyrillic", "\u1c89", "\u1c8a"), ("latin", "\ua7cb", "\u0264"),
            ("garay", "\U00010d50", "\U00010d70"), ("sigma", "A:Σ", "a:ς") };
        var originals = samples.Select(s => new BleeckerCommandSpotlightAction(s.Item1, "Original " + s.Item2)).ToArray();
        control.Actions = originals; control.Open = true; await layout(45);
        var editor = Elements(host).OfType<TextBox>().Single(e => e.Name == "SpotlightSearch");
        foreach (var (id, originalText, query) in samples)
        {
            editor.Text = query; await layout(25);
            var original = originals.Single(a => a.Id == id);
            var rows = Elements(host).OfType<Button>().Where(b => b.Name == "SpotlightAction").ToArray();
            var correct = control.Query == query && ReferenceEquals(control.SelectedAction, original)
                && rows.Length == 1 && ReferenceEquals(rows[0].Tag, original) && original.Title == "Original " + originalText;
            if (!correct) differences.Add("search/" + id);
            results.Add(new { theme = theme.ToString(), id, query, actualQuery = control.Query,
                selectedId = control.SelectedActionId, originalTitle = original.Title, originalRetained = ReferenceEquals(control.SelectedAction, original), rows = rows.Length, passed = correct });
        }
    }
    private static Task VerifyReadOnlyKeyboard(Assembly library, ElementTheme theme, List<object> results, List<string> differences)
    {
        var type = library.GetType("Orchard.BleeckerCommandSpotlightKeyboard");
        var read = type?.GetMethod("ReadLayout", BindingFlags.Static | BindingFlags.NonPublic);
        if (read is null) { differences.Add("read-only keyboard bridge absent"); results.Add(new { theme = theme.ToString(), available = false, passed = false }); return Task.CompletedTask; }
        var count = GetKeyboardLayoutList(0, null);
        if (count <= 0) throw new InvalidOperationException("No installed keyboard layouts were returned");
        var layouts = new nint[count]; var returned = GetKeyboardLayoutList(count, layouts);
        if (returned <= 0 || returned > count) throw new InvalidOperationException("Installed keyboard layout enumeration failed");
        foreach (var layout in layouts.Take(returned))
        {
            var language = unchecked((ushort)(long)layout);
            foreach (var (key, flags, expected) in new (uint, int, string)[] { (0x4b,2,"k"),(0x4b,3,"K"),(0x4b,10,"K"),(0x31,2,"1"),(0x31,3,"!"),(0xba,2,";"),(0xba,3,":"),(0xbf,2,"/"),(0xbf,3,"?"),(0xbb,2,"="),(0xbb,3,"+"),(0x61,2,"1"),(0x20,2," ") })
            {
                // Exact character expectations apply only to the already installed US layout.
                // Other installed layouts are observed twice without loading or activating them.
                var first = read.Invoke(null, [key, 0u, false, flags, layout])!;
                var second = read.Invoke(null, [key, 0u, false, flags, layout])!;
                var name = (string)first.GetType().GetProperty("Name")!.GetValue(first)!;
                var repeated = (string)second.GetType().GetProperty("Name")!.GetValue(second)!;
                var modifiers = (int)first.GetType().GetProperty("Modifiers")!.GetValue(first)!;
                var correct = name == repeated && (language != 0x409 || name == expected && modifiers == flags);
                if (!correct) differences.Add("layout/" + language.ToString("x4") + "/" + key);
                results.Add(new { theme = theme.ToString(), available = true, layout = unchecked((ulong)(long)layout).ToString("x"), language,
                    keyCode = key, modifiers = flags, actualModifiers = modifiers, name, repeatedName = repeated,
                    expectedUsName = language == 0x409 ? expected : null, exactUsLayoutVerified = language == 0x409,
                    keyboardStateWritten = false, keyboardLayoutLoaded = false, hardwareInputSent = false, passed = correct });
            }
        }
        return Task.CompletedTask;
    }
}
