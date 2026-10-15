using System.Text.Json;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class InputsFixture
{
    private async Task VerifyScriptTypographyAsync(BleeckerColorScheme scheme, string folder)
    {
        var inputs = new[] { SmallInput, MediumInput, LargeInput };
        var originals = inputs.Select(input => (input.Text, input.PlaceholderText)).ToArray();
        var samples = new[]
        {
            (Name: "japanese", Text: "東京のニュース"),
            (Name: "arabic", Text: "أخبار القاهرة"),
            (Name: "hindi", Text: "दिल्ली समाचार"),
            (Name: "mixed", Text: "Café · Á · 👩🏽‍💻")
        };
        var measurements = new List<object>();
        try
        {
            foreach (var sample in samples)
            {
                foreach (var input in inputs)
                {
                    input.Text = "";
                    input.PlaceholderText = sample.Text;
                }
                UpdateLayout();
                await MainWindow.CaptureElementAsync(this, Path.Combine(folder, $"input-script-{sample.Name}-placeholder-{scheme}.png"));
                foreach (var input in inputs) input.Text = sample.Text;
                UpdateLayout();
                foreach (var input in inputs)
                {
                    input.Select(0, input.Text.Length);
                    Require(input.SelectedText == sample.Text, $"{sample.Name} selection did not preserve the full Unicode value.");
                    input.Select(0, 0);
                }
                await MainWindow.CaptureElementAsync(this, Path.Combine(folder, $"input-script-{sample.Name}-filled-{scheme}.png"));
                UpdateLayout();
                foreach (var input in inputs)
                {
                    var line = input.GetRectFromCharacterIndex(0, false);
                    var editor = Find<ScrollViewer>(input, "ContentElement");
                    var placeholder = Find<TextBlock>(input, "PlaceholderTextContentPresenter");
                    var origin = editor.TransformToVisual(input).TransformPoint(new Point());
                    Require(line.Height > 0 && line.Height <= editor.ActualHeight && origin.Y >= 0 &&
                        origin.Y + editor.ActualHeight <= input.ActualHeight,
                        $"{sample.Name} {input.InputSize} line exceeds the input viewport.");
                    measurements.Add(new { script = sample.Name, size = input.InputSize.ToString(),
                        scale = XamlRoot.RasterizationScale, height = input.ActualHeight,
                        lineHeight = line.Height, editorY = origin.Y, editorHeight = editor.ActualHeight,
                        placeholderY = placeholder.TransformToVisual(input).TransformPoint(new Point()).Y,
                        placeholderHeight = placeholder.ActualHeight, placeholderBaseline = placeholder.BaselineOffset });
                }
            }
            File.WriteAllText(Path.Combine(folder, $"input-script-geometry-{scheme}.json"),
                JsonSerializer.Serialize(measurements, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            for (var index = 0; index < inputs.Length; index++)
            {
                inputs[index].Text = originals[index].Text;
                inputs[index].PlaceholderText = originals[index].PlaceholderText;
            }
            UpdateLayout();
        }
    }
}
