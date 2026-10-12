using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class InputsFixture
{
    private async Task VerifyAdornmentDirectionAsync(BleeckerColorScheme scheme, string folder)
    {
        var input = IconsInput;
        var original = (input.FlowDirection, input.InputSize, input.Text, input.PlaceholderText, input.StartIcon, input.EndIcon);
        var results = new List<object>();
        try
        {
            foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            foreach (var size in Enum.GetValues<BleeckerInputSize>())
            foreach (var adornments in new[] { "start", "end", "both" })
            foreach (var filled in new[] { true, false })
            {
                input.FlowDirection = direction;
                input.InputSize = size;
                input.StartIcon = adornments == "end" ? null : original.StartIcon;
                input.EndIcon = adornments == "start" ? null : original.EndIcon;
                input.Text = filled ? "Harbor update" : "";
                input.PlaceholderText = "Harbor update";
                UpdateLayout();
                await Task.Delay(50);
                var bounds = input.TransformToVisual(this).TransformBounds(new Rect(0, 0, input.ActualWidth, input.ActualHeight));
                Rect PartBounds(FrameworkElement part) => part.TransformToVisual(this)
                    .TransformBounds(new Rect(0, 0, part.ActualWidth, part.ActualHeight));
                var start = PartBounds(Find<ContentPresenter>(input, "StartAdornment"));
                var end = PartBounds(Find<ContentPresenter>(input, "EndAdornment"));
                var editor = Find<ScrollViewer>(input, "ContentElement");
                var presenter = Find<TextBlock>(input, "PlaceholderTextContentPresenter");
                var content = PartBounds(editor);
                var placeholder = PartBounds(presenter);
                var expectedLeft = input.BorderThickness.Left + input.Padding.Left;
                var expectedRight = input.BorderThickness.Right + input.Padding.Right;
                var first = filled ? input.GetRectFromCharacterIndex(0, false) : new Rect();
                var last = filled ? input.GetRectFromCharacterIndex(input.Text.Length - 1, true) : new Rect();
                results.Add(new { direction = direction.ToString(), size = size.ToString(), adornments, filled,
                    startX = start.Left - bounds.Left, endX = end.Left - bounds.Left,
                    contentX = content.Left - bounds.Left, contentWidth = content.Width,
                    placeholderX = placeholder.Left - bounds.Left, placeholderWidth = placeholder.Width,
                    firstX = first.X, lastX = last.X, padding = input.Padding.ToString() });
                File.WriteAllText(Path.Combine(folder, $"input-adornment-direction-{scheme}.json"),
                    JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
                await MainWindow.CaptureElementAsync(this, Path.Combine(folder,
                    $"input-adornment-{direction}-{size}-{adornments}-{filled}-{scheme}.png"));
                Require(Math.Abs(content.Left - bounds.Left - expectedLeft) < .1 &&
                    Math.Abs(bounds.Right - content.Right - expectedRight) < .1,
                    "Input content did not retain its physical left/right inset.");
                Require(editor.FlowDirection == direction && presenter.FlowDirection == direction,
                    "Text direction did not reach both native editing and placeholder presenters.");
                if (!filled)
                    Require(direction == FlowDirection.LeftToRight
                        ? Math.Abs(placeholder.Left - content.Left) < .1
                        : Math.Abs(placeholder.Right - content.Right) < .1,
                        "Placeholder did not align to the decorated viewport's reading edge.");
                if (filled)
                    Require(first.X >= 0 && last.X <= content.Width + 1 && last.X > first.X,
                        "Entered Latin text did not fit the decorated editing viewport.");
                if (input.StartIcon is not null)
                    Require(Math.Abs(start.Left - bounds.Left - 12) < .1, "Start adornment moved from the physical left edge.");
                if (input.EndIcon is not null)
                    Require(Math.Abs(bounds.Right - end.Right - 12) < .1, "End adornment moved from the physical right edge.");
            }
        }
        finally
        {
            input.FlowDirection = original.FlowDirection;
            input.InputSize = original.InputSize;
            input.Text = original.Text;
            input.PlaceholderText = original.PlaceholderText;
            input.StartIcon = original.StartIcon;
            input.EndIcon = original.EndIcon;
            UpdateLayout();
        }
    }
}
