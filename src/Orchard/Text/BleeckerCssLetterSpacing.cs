using System.Text;
using Microsoft.Graphics.Canvas.Text;

namespace Orchard;

internal static class BleeckerCssLetterSpacing
{
    internal static void Apply(CanvasTextLayout layout, string text, CanvasTextDirection direction, float spacing)
    {
        layout.SetCharacterSpacing(0, text.Length, 0, spacing, 0);
        if (spacing == 0 || !text.Any(character => character > 0x7f)) return;
        // CSS suppresses tracking inside cursive scripts. XAML text already
        // follows that rule; the explicit Win2D measurement spacing must agree.
        var analyzer = new CanvasTextAnalyzer(text, direction);
        foreach (var script in analyzer.GetScript())
        {
            if (!analyzer.GetScriptProperties(script.Value).IsConnectedWriting) continue;
            var start = script.Key.CharacterIndex; var end = start + script.Key.CharacterCount;
            while (start < end)
            {
                var rune = Rune.GetRuneAt(text, start);
                if (Rune.IsWhiteSpace(rune)) { start += rune.Utf16SequenceLength; continue; }
                var first = start;
                while (start < end && !Rune.IsWhiteSpace(rune)) { start += rune.Utf16SequenceLength; if (start < end) rune = Rune.GetRuneAt(text, start); }
                layout.SetCharacterSpacing(first, start - first, 0, 0, 0);
            }
        }
    }
}
