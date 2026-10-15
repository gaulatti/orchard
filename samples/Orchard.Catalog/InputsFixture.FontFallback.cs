using System.Text.Json;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class InputsFixture
{
    private async Task InspectFontFallbackAsync(BleeckerColorScheme scheme, string folder)
    {
        var inputs = new[] { SmallInput, MediumInput, LargeInput };
        var originals = inputs.Select(input => (input.Text, input.FontFamily)).ToArray();
        var results = new List<object>();
        // Diagnostic candidates, not accepted fallback mappings or visual-parity assertions.
        var candidates = new[] { "", ", Segoe UI", ", Microsoft YaHei UI, Yu Gothic" };
        try
        {
            for (var candidate = 0; candidate < candidates.Length; candidate++)
            {
                for (var index = 0; index < inputs.Length; index++)
                {
                    inputs[index].Text = "東京のニュース";
                    inputs[index].FontFamily = new FontFamily(originals[index].FontFamily.Source + candidates[candidate]);
                }
                UpdateLayout();
                await MainWindow.CaptureElementAsync(this, Path.Combine(folder,
                    $"input-font-fallback-{candidate}-{scheme}.png"));
                UpdateLayout();
                foreach (var input in inputs)
                {
                    var start = input.GetRectFromCharacterIndex(0, false);
                    var end = input.GetRectFromCharacterIndex(input.Text.Length - 1, true);
                    results.Add(new { size = input.InputSize.ToString(), family = input.FontFamily.Source,
                        language = input.Language, width = end.X - start.X });
                }
            }
            File.WriteAllText(Path.Combine(folder, $"input-font-fallback-{scheme}.json"),
                JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            for (var index = 0; index < inputs.Length; index++)
            {
                inputs[index].Text = originals[index].Text;
                inputs[index].FontFamily = originals[index].FontFamily;
            }
            UpdateLayout();
        }
    }
}
