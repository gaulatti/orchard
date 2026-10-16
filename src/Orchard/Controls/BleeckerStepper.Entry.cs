using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace Orchard;

public sealed partial class BleeckerStepper
{
    private sealed class Entry : Panel, IDisposable
    {
        private readonly BleeckerStepper owner;
        internal string Key { get; }
        internal BleeckerStep Step { get; private set; }
        internal StepButton Button { get; }
        private readonly Border line = new() { IsHitTestVisible = false };
        private readonly Grid circle = new() { UseLayoutRounding = false, RenderTransformOrigin = new Point(.5, .5) };
        private readonly Border fill = new() { CornerRadius = new CornerRadius(16) };
        private readonly Border ring = new() { CornerRadius = new CornerRadius(18), Margin = new Thickness(-2), BorderThickness = new Thickness(2) };
        private readonly Border focus = new() { CornerRadius = new CornerRadius(20), Margin = new Thickness(-4), BorderThickness = new Thickness(2) };
        private readonly Border offset = new() { CornerRadius = new CornerRadius(18), Margin = new Thickness(-2), BorderThickness = new Thickness(2) };
        private readonly TextBlock number = new() { TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, UseLayoutRounding = false, LineStackingStrategy = LineStackingStrategy.BlockLineHeight };
        private readonly TextBlock numberMetrics = new() { Text = "Mg", UseLayoutRounding = false };
        private readonly TranslateTransform numberShift = new();
        private readonly Polyline check = new() { Points = new PointCollection { new(20, 6), new(9, 17), new(5, 13) }, StrokeThickness = 2, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round };
        private readonly Viewbox checkBox;
        private readonly StackPanel labels = new() { UseLayoutRounding = false };
        private readonly Paragraph label = new(14, 20, "BleeckerPrimaryMediumFontFamily", Microsoft.UI.Text.FontWeights.Medium);
        private readonly Paragraph description = new(12, 16, "BleeckerPrimaryFontFamily", Microsoft.UI.Text.FontWeights.Normal);
        private readonly SolidColorBrush fillPaint = new(), foregroundPaint = new();
        private readonly BleeckerCssTransition<(double R, double G, double B, double Alpha)> fillMotion = new(BleeckerSrgbColorMix.MixPremultiplied), foregroundMotion = new(BleeckerSrgbColorMix.MixPremultiplied);
        private readonly ScaleTransform scale = new();
        private BleeckerStepStatus status;
        private bool last;
        private double labelWidth, circleWidth, height;
        private readonly BleeckerDisabledPointer disabledPointer;

        internal Entry(BleeckerStepper owner, BleeckerStep step)
        {
            this.owner = owner; Step = step; Key = step.Id; UseLayoutRounding = false; Name = "StepEntry";
            line.Name = "Connector"; labels.Name = "Labels"; label.Name = "LabelParagraph"; description.Name = "DescriptionParagraph";
            fill.Name = "CircleFill"; ring.Name = "CircleRing"; focus.Name = "CircleFocus"; offset.Name = "CircleOffset"; circle.Name = "Circle";
            Button = new StepButton { Style = (Style)owner.actions[typeof(Button)], Content = circle, Name = "StepButton" };
            Button.Click += (_, _) => owner.Invoke(this);
            Button.RegisterPropertyChangedCallback(Microsoft.UI.Xaml.Controls.Primitives.ButtonBase.IsPressedProperty, (_, _) => UpdateScale());
            Button.RegisterPropertyChangedCallback(Control.FocusStateProperty, (_, _) => UpdateRings());
            Button.Loaded += (_, _) => disabledPointer!.Update();
            Button.Unloaded += (_, _) => { disabledPointer!.Detach(); disabledPointer.ReconcileLifecycle(); };
            disabledPointer = new BleeckerDisabledPointer(Button, () => Step.IsDisabled ? Button : null, _ => { });
            fill.Background = fillPaint; number.Foreground = foregroundPaint; number.RenderTransform = numberShift; check.Stroke = foregroundPaint;
            var checkGrid = new Grid { Width = 24, Height = 24 }; checkGrid.Children.Add(check);
            checkBox = new Viewbox { Width = 16, Height = 16, Child = checkGrid, IsHitTestVisible = false };
            circle.RenderTransform = scale;
            circle.Children.Add(focus); circle.Children.Add(offset); circle.Children.Add(ring); circle.Children.Add(fill); circle.Children.Add(number); circle.Children.Add(checkBox);
            labels.Children.Add(label); labels.Children.Add(description);
            Children.Add(line); Children.Add(Button); Children.Add(labels);
            foreach (var child in new UIElement[] { line, labels, circle }) AutomationProperties.SetAccessibilityView(child, AccessibilityView.Raw);
        }

        internal void Update(BleeckerStep step, int index, int count, BleeckerStepStatus state, bool animate)
        {
            Step = step; status = state; last = index == count - 1;
            label.Set(owner, step.Label, true); description.Set(owner, step.Description ?? "", false);
            description.Margin = string.IsNullOrEmpty(step.Description) ? new Thickness() : new Thickness(0, 2, 0, 0);
            labels.FlowDirection = owner.FlowDirection;
            var horizontal = owner.Orientation == Orientation.Horizontal;
            label.Text.TextAlignment = description.Text.TextAlignment = horizontal ? TextAlignment.Center : TextAlignment.Left;
            number.Text = (index + 1).ToString(CultureInfo.InvariantCulture);
            foreach (var text in new[] { number, numberMetrics })
            {
                text.FontFamily = owner.Font("BleeckerPrimarySemiboldFontFamily"); text.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                text.FontSize = owner.FontSize; text.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled; text.CharacterSpacing = owner.CharacterSpacing;
            }
            number.LineHeight = 20 * owner.FontSize / 14;
            numberMetrics.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            number.Visibility = state == BleeckerStepStatus.Completed ? Visibility.Collapsed : Visibility.Visible;
            checkBox.Visibility = state == BleeckerStepStatus.Completed ? Visibility.Visible : Visibility.Collapsed;
            Button.IsEnabled = owner.CanInvoke(step); circle.Opacity = step.IsDisabled ? .5 : 1;
            Button.FlowDirection = FlowDirection.LeftToRight;
            AutomationProperties.SetName(Button, step.Label);
            AutomationProperties.SetHelpText(Button, step.Description ?? "");
            AutomationProperties.SetItemStatus(Button, state.ToString());
            AutomationProperties.SetPositionInSet(Button, index + 1); AutomationProperties.SetSizeOfSet(Button, count);
            line.Visibility = last ? Visibility.Collapsed : Visibility.Visible;
            line.Background = owner.Paint(state == BleeckerStepStatus.Completed ? "Accent" : "Line");
            label.Text.Foreground = owner.Paint(state == BleeckerStepStatus.Active ? "Primary" : "Secondary");
            description.Text.Foreground = owner.Paint("Secondary");
            var now = owner.clock.Elapsed.TotalMilliseconds;
            var motion = animate && owner.IsLoaded && owner.settings.AnimationsEnabled;
            fillMotion.Retarget(Color(owner.Paint(state switch { BleeckerStepStatus.Completed => "Accent", BleeckerStepStatus.Error => "Error", _ => "Surface" })), now, 190, motion);
            foregroundMotion.Retarget(Color(owner.Paint(state switch { BleeckerStepStatus.Completed or BleeckerStepStatus.Error => "OnAccent", BleeckerStepStatus.Active => "Accent", _ => "Secondary" })), now, 190, motion);
            UpdateScale(); UpdateRings(); Animate(now);
            disabledPointer.Update(); InvalidateMeasure();
            if (IsAnimating(now)) owner.StartRendering();
        }

        private void UpdateRings()
        {
            var focused = Button.FocusState == FocusState.Keyboard;
            ring.Visibility = !focused && (status is BleeckerStepStatus.Active or BleeckerStepStatus.Pending) ? Visibility.Visible : Visibility.Collapsed;
            ring.BorderBrush = owner.Paint(status == BleeckerStepStatus.Active ? "Accent" : "PendingRing");
            focus.Visibility = offset.Visibility = focused ? Visibility.Visible : Visibility.Collapsed;
            focus.BorderBrush = owner.Paint("Accent");
            offset.BorderBrush = owner.Paint("OnAccent");
        }
        private void UpdateScale()
        {
            // Tailwind uses the individual scale property; the source transitions
            // transform, background-color and color, so scale changes immediately.
            scale.ScaleX = scale.ScaleY = Button.IsPressed && Button.IsEnabled ? .94 : 1;
        }
        internal void Animate(double now)
        {
            SetColor(fillPaint, fillMotion.ValueAt(now)); SetColor(foregroundPaint, foregroundMotion.ValueAt(now));
        }
        internal bool IsAnimating(double now) => fillMotion.IsRunning(now) || foregroundMotion.IsRunning(now);
        private static (double R, double G, double B, double Alpha) Color(Brush brush)
        {
            var solid = (SolidColorBrush)brush; var c = solid.Color;
            return BleeckerSrgbColorMix.Premultiply((c.R, c.G, c.B, c.A / 255d * solid.Opacity));
        }
        private static void SetColor(SolidColorBrush brush, (double R, double G, double B, double Alpha) color)
        {
            var c = BleeckerSrgbColorMix.EncodePremultiplied(color); brush.Color = Windows.UI.Color.FromArgb((byte)Math.Round(c.Alpha * 255), c.R, c.G, c.B);
        }
        internal double MinimumWidth => Math.Max(32, Math.Min(128, Math.Max(label.MinimumWidth, description.MinimumWidth)));
        internal double PreferredWidth => Math.Max(label.PreferredWidth, description.PreferredWidth);
        protected override Size MeasureOverride(Size available)
        {
            var horizontal = owner.Orientation == Orientation.Horizontal;
            if (horizontal)
            {
                circleWidth = 32; labelWidth = Math.Min(128, Math.Min(available.Width, PreferredWidth));
            }
            else
            {
                var preferred = PreferredWidth; var minimum = Math.Max(label.MinimumWidth, description.MinimumWidth);
                var remaining = Math.Max(0, available.Width - 16);
                var deficit = Math.Max(0, 32 + preferred - remaining);
                var minCircle = status == BleeckerStepStatus.Completed ? 16 : NumberWidth();
                var proposedCircle = 32 - deficit * 32 / (32 + preferred);
                var proposedLabel = preferred - deficit * preferred / (32 + preferred);
                if (proposedLabel < minimum) { labelWidth = minimum; circleWidth = Math.Clamp(remaining - minimum, minCircle, 32); }
                else if (proposedCircle < minCircle) { circleWidth = minCircle; labelWidth = Math.Max(minimum, remaining - minCircle); }
                else { labelWidth = proposedLabel; circleWidth = proposedCircle; }
            }
            labels.Measure(new Size(labelWidth, double.PositiveInfinity));
            height = horizontal ? 40 + labels.DesiredSize.Height : Math.Max(32, 8 + labels.DesiredSize.Height);
            circle.Width = circleWidth; circle.Height = 32;
            Button.Measure(new Size(circleWidth, 32)); line.Measure(new Size(available.Width, height));
            return new Size(available.Width, height);
        }
        private double NumberWidth() { number.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); return number.DesiredSize.Width; }
        protected override Size ArrangeOverride(Size final)
        {
            var horizontal = owner.Orientation == Orientation.Horizontal;
            var rtl = owner.FlowDirection == FlowDirection.RightToLeft;
            var x = horizontal ? Math.Floor((final.Width - circleWidth) / 2 * 64) / 64 : rtl ? final.Width - circleWidth : 0;
            Button.Arrange(new Rect(x, 0, circleWidth, 32));
            var lx = horizontal ? (final.Width - labelWidth) / 2 : rtl ? final.Width - circleWidth - 16 - labelWidth : circleWidth + 16;
            labels.Arrange(new Rect(lx, horizontal ? 40 : 8, labelWidth, labels.DesiredSize.Height));
            line.Arrange(horizontal ? new Rect(Math.Floor(final.Width / 2 * 64) / 64, 16, Math.Max(0, final.Width - 32), 2) : new Rect(16, 36, 2, Math.Max(0, height - 32)));
            return final;
        }
        internal void AlignText()
        {
            label.Align(owner); description.Align(owner);
            if (owner.XamlRoot is null || number.Visibility != Visibility.Visible || number.ActualHeight <= 0) return;
            var top = number.TransformToVisual(circle).TransformPoint(new Point()).Y - numberShift.Y;
            var dpi = owner.XamlRoot.RasterizationScale;
            var factor = number.IsTextScaleFactorEnabled ? owner.settings.TextScaleFactor : 1;
            var baseline = top + (number.LineHeight * factor - numberMetrics.DesiredSize.Height) / 2 + numberMetrics.BaselineOffset;
            numberShift.Y = Math.Floor(baseline * dpi) / dpi - top - number.BaselineOffset;
        }
        internal void DetachPointer() => disabledPointer.Detach();
        public void Dispose() { disabledPointer.Detach(); Button.IsEnabled = false; }
    }

    private FontFamily Font(string key) => FontFamily.Source == ((FontFamily)Application.Current.Resources["BleeckerPrimaryFontFamily"]).Source ? (FontFamily)Application.Current.Resources[key] : FontFamily;
    private sealed class StepButton : Button
    {
        internal StepButton() => ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }

    private sealed class Paragraph : Panel
    {
        internal TextBlock Text { get; } = new() { UseLayoutRounding = false, TextWrapping = TextWrapping.WrapWholeWords, LineStackingStrategy = LineStackingStrategy.BlockLineHeight };
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
        internal void Set(BleeckerStepper owner, string value, bool label)
        {
            Text.Text = Regex.Replace(value, "[ \\t\\r\\n\\f]+", " ").Trim(' ');
            foreach (var text in new[] { Text, measure, metrics })
            {
                text.FontFamily = owner.Font(font); text.FontSize = owner.FontSize * size / 14; text.FontWeight = weight;
                text.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled; text.CharacterSpacing = owner.CharacterSpacing;
            }
            Text.LineHeight = lineHeight * owner.FontSize / 14;
            metrics.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            measure.Text = Text.Text; measure.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); PreferredWidth = measure.DesiredSize.Width;
            MinimumWidth = 0;
            foreach (var word in Text.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                measure.Text = word; measure.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); MinimumWidth = Math.Max(MinimumWidth, measure.DesiredSize.Width);
            }
            InvalidateMeasure();
        }
        protected override Size MeasureOverride(Size available)
        {
            Text.Measure(available);
            return new Size(Math.Min(available.Width, PreferredWidth), Text.Text.Length == 0 ? 0 : Text.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size final) { Text.Arrange(new Rect(0, 0, final.Width, final.Height)); return final; }
        internal void Align(BleeckerStepper owner)
        {
            if (owner.XamlRoot is null || Text.Text.Length == 0) return;
            var origin = TransformToVisual(owner.XamlRoot.Content).TransformPoint(new Point()).Y;
            var factor = Text.IsTextScaleFactorEnabled ? owner.settings.TextScaleFactor : 1;
            var scale = owner.XamlRoot.RasterizationScale;
            // Chromium floors the measured CSS line baseline onto the device
            // grid. Rounding places the pinned 14/12px text one pixel too low.
            var target = Math.Floor((origin + (Text.LineHeight * factor - metrics.DesiredSize.Height) / 2 + metrics.BaselineOffset) * scale) / scale;
            shift.Y = target - origin - Text.BaselineOffset;
        }
    }
}
