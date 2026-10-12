using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class InputsFixture
{
    private async Task VerifyTypographyAsync(BleeckerColorScheme scheme, string folder)
    {
        var inputs = new[] { SmallInput, MediumInput, LargeInput };
        var originals = inputs.Select(input => (input.Text, input.PlaceholderText)).ToArray();
        var measurements = new List<object>();
        try
        {
            foreach (var input in inputs)
            {
                input.Text = "";
                input.PlaceholderText = "Harbor update";
            }
            UpdateLayout();
            await Task.Delay(50);
            foreach (var input in inputs) input.Text = "Harbor update";
            UpdateLayout();
            await Task.Delay(50);
            foreach (var input in inputs)
            {
                var caret = input.GetRectFromCharacterIndex(0, false);
                var last = input.GetRectFromCharacterIndex(input.Text.Length - 1, true);
                var placeholder = Find<TextBlock>(input, "PlaceholderTextContentPresenter");
                var editor = Find<ScrollViewer>(input, "ContentElement");
                var placeholderOrigin = placeholder.TransformToVisual(input).TransformPoint(new Point());
                var editorOrigin = editor.TransformToVisual(input).TransformPoint(new Point());
                Require(caret.Height > 0 && caret.Height <= editor.ActualHeight &&
                    editorOrigin.Y >= 0 && editorOrigin.Y + editor.ActualHeight <= input.ActualHeight,
                    $"{input.InputSize} native line does not fit its editor viewport.");
                Require(last.X > caret.X && last.X - caret.X <=
                    input.ActualWidth - input.Padding.Left - input.Padding.Right - input.BorderThickness.Left - input.BorderThickness.Right,
                    $"{input.InputSize} native text extent exceeds the available width.");
                measurements.Add(new
                {
                    size = input.InputSize.ToString(), scale = XamlRoot.RasterizationScale,
                    fontSize = input.FontSize, width = input.ActualWidth, height = input.ActualHeight,
                    caret = new { caret.X, caret.Y, caret.Height }, trailingEdge = last.X,
                    editor = new { editorOrigin.X, editorOrigin.Y, editor.ActualHeight },
                    placeholder = new { placeholderOrigin.X, placeholderOrigin.Y,
                        placeholder.ActualHeight, placeholder.BaselineOffset }
                });
            }
            await MainWindow.CaptureElementAsync(this, Path.Combine(folder, $"input-typography-filled-{scheme}.png"));
            foreach (var input in inputs) await VerifyTypographyPixelsAsync(input, "filled", folder, scheme);
            foreach (var input in inputs) input.Text = "";
            UpdateLayout();
            await Task.Delay(50);
            await MainWindow.CaptureElementAsync(this, Path.Combine(folder, $"input-typography-placeholder-{scheme}.png"));
            foreach (var input in inputs) await VerifyTypographyPixelsAsync(input, "placeholder", folder, scheme);
            File.WriteAllText(Path.Combine(folder, $"input-typography-{scheme}.json"),
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

    private async Task VerifyTypographySizeChangesAsync(BleeckerColorScheme scheme, string folder)
    {
        var originalSize = MediumInput.InputSize;
        var originalText = MediumInput.Text;
        var originalPlaceholder = MediumInput.PlaceholderText;
        var output = Path.Combine(folder, "typography-size-changes");
        Directory.CreateDirectory(output);
        try
        {
            MediumInput.PlaceholderText = "Harbor update";
            foreach (var size in new[] { BleeckerInputSize.Large, BleeckerInputSize.Small, BleeckerInputSize.Medium })
            {
                MediumInput.InputSize = size;
                MediumInput.Text = "Harbor update";
                UpdateLayout();
                await Task.Delay(50);
                await VerifyTypographyPixelsAsync(MediumInput, "filled", output, scheme);
                MediumInput.Text = "";
                UpdateLayout();
                await Task.Delay(50);
                await VerifyTypographyPixelsAsync(MediumInput, "placeholder", output, scheme);
            }
        }
        finally
        {
            MediumInput.InputSize = originalSize;
            MediumInput.Text = originalText;
            MediumInput.PlaceholderText = originalPlaceholder;
            UpdateLayout();
        }
    }
}
