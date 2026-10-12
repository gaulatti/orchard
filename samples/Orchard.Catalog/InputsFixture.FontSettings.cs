using Microsoft.UI.Xaml.Controls;
using Windows.UI.Text;

namespace Orchard.Catalog;

public sealed partial class InputsFixture
{
    private async Task VerifyFontSettingsAsync(BleeckerColorScheme scheme, string folder)
    {
        var input = MediumInput;
        var original = (input.Text, input.PlaceholderText, input.FontStyle, input.FontStretch, input.IsTextScaleFactorEnabled);
        try
        {
            input.Text = "";
            input.PlaceholderText = "Harbor update";
            input.FontStyle = FontStyle.Italic;
            input.FontStretch = FontStretch.Condensed;
            input.IsTextScaleFactorEnabled = false;
            UpdateLayout();
            await Task.Delay(100);
            await MainWindow.CaptureElementAsync(this, Path.Combine(folder, $"input-font-settings-placeholder-{scheme}.png"));
            var editor = Find<ScrollViewer>(input, "ContentElement");
            var placeholder = Find<TextBlock>(input, "PlaceholderTextContentPresenter");
            var metrics = Find<TextBlock>(input, "InputFontMetrics");
            Require(editor.FontStyle == input.FontStyle && placeholder.FontStyle == input.FontStyle && metrics.FontStyle == input.FontStyle,
                "Font style did not reach all input typography parts.");
            Require(editor.FontStretch == input.FontStretch && placeholder.FontStretch == input.FontStretch && metrics.FontStretch == input.FontStretch,
                "Font stretch did not reach all input typography parts.");
            Require(editor.IsTextScaleFactorEnabled == input.IsTextScaleFactorEnabled &&
                placeholder.IsTextScaleFactorEnabled == input.IsTextScaleFactorEnabled && metrics.IsTextScaleFactorEnabled == input.IsTextScaleFactorEnabled,
                "Text-scale preference did not reach all input typography parts.");
            input.Text = input.PlaceholderText;
            UpdateLayout();
            await Task.Delay(100);
            await MainWindow.CaptureElementAsync(this, Path.Combine(folder, $"input-font-settings-filled-{scheme}.png"));
        }
        finally
        {
            input.Text = original.Text;
            input.PlaceholderText = original.PlaceholderText;
            input.FontStyle = original.FontStyle;
            input.FontStretch = original.FontStretch;
            input.IsTextScaleFactorEnabled = original.IsTextScaleFactorEnabled;
            UpdateLayout();
        }
    }
}
