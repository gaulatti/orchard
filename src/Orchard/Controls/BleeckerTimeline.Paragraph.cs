using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerTimeline
{
    private sealed class Paragraph : Panel
    {
        internal TextBlock Text { get; } = new() { UseLayoutRounding = false, TextWrapping = TextWrapping.WrapWholeWords, LineStackingStrategy = LineStackingStrategy.BlockLineHeight, TextReadingOrder = TextReadingOrder.UseFlowDirection };
        private readonly TextBlock metrics = new() { UseLayoutRounding = false, Text = "Mg" };
        private readonly TextBlock measure = new() { UseLayoutRounding = false };
        private readonly TranslateTransform shift = new();
        private readonly double size, lineHeight;
        private readonly string font;
        private readonly Windows.UI.Text.FontWeight weight;
        internal double MinimumWidth { get; private set; }
        internal double PreferredWidth { get; private set; }
        internal Paragraph(double size, double lineHeight, string font, Windows.UI.Text.FontWeight weight)
        {
            this.size = size; this.lineHeight = lineHeight; this.font = font; this.weight = weight;
            UseLayoutRounding = false; Text.RenderTransform = shift; Children.Add(Text);
        }
        internal void Set(BleeckerTimeline owner, string value, Brush foreground)
        {
            Text.Text = Regex.Replace(value, "[ \\t\\r\\n\\f]+", " ").Trim(' '); FlowDirection = owner.FlowDirection;
            foreach (var text in new[] { Text, metrics, measure })
            {
                text.FontFamily = font == "BleeckerSecondaryFontFamily" ? (FontFamily)Application.Current.Resources[font] : owner.Font(font);
                text.FontSize = owner.FontSize * size / 14; text.FontWeight = weight;
                text.CharacterSpacing = owner.CharacterSpacing; text.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled;
            }
            Text.LineHeight = owner.FontSize * lineHeight / 14; Text.Foreground = foreground;
            metrics.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            measure.Text = Text.Text; measure.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); PreferredWidth = measure.DesiredSize.Width;
            MinimumWidth = 0;
            foreach (var word in Text.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            { measure.Text = word; measure.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); MinimumWidth = Math.Max(MinimumWidth, measure.DesiredSize.Width); }
            InvalidateMeasure();
        }
        protected override Size MeasureOverride(Size available)
        {
            Text.Measure(available);
            return new Size(Math.Min(available.Width, PreferredWidth), Text.Text.Length == 0 ? 0 : Text.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size final) { Text.Arrange(new Rect(0, 0, final.Width, final.Height)); return final; }
        internal void Align(BleeckerTimeline owner)
        {
            if (owner.XamlRoot is null || Text.Text.Length == 0 || Visibility != Visibility.Visible) return;
            var scale = owner.XamlRoot.RasterizationScale; var factor = Text.IsTextScaleFactorEnabled ? owner.settings.TextScaleFactor : 1;
            var ascent = Math.Floor(metrics.BaselineOffset * scale + .5) / scale;
            var descent = Math.Floor((metrics.DesiredSize.Height - metrics.BaselineOffset) * scale + .5) / scale;
            shift.Y = Math.Floor(((Text.LineHeight * factor - ascent - descent) / 2 + ascent) * scale) / scale - Text.BaselineOffset;
        }
    }
}
