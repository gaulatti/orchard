using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerDataList
{
    private sealed class Entry : Panel
    {
        private readonly BleeckerDataList owner;
        internal string Key { get; }
        private BleeckerDataListItem item;
        internal Slot Label { get; } = new("DataListLabel", 13, 19.5, false);
        internal Slot Value { get; } = new("DataListValue", 14, 20, false);
        internal Slot Description { get; } = new("DataListDescription", 12, 20, true);
        private readonly Border background = new() { IsHitTestVisible = false };
        private readonly Border divider = new() { IsHitTestVisible = false };
        private bool stripe, hasDescription, last;
        private double labelWidth, valueWidth, labelHeight, valueHeight, descriptionHeight, bodyHeight;

        internal Entry(BleeckerDataList owner, BleeckerDataListItem item)
        {
            this.owner = owner; this.item = item; Key = item.Id; Name = "DataListRow"; UseLayoutRounding = false; FlowDirection = FlowDirection.LeftToRight;
            Children.Add(background); Children.Add(divider); Children.Add(Label); Children.Add(Value); Children.Add(Description);
            AutomationProperties.SetAccessibilityView(background, AccessibilityView.Raw);
            AutomationProperties.SetAccessibilityView(divider, AccessibilityView.Raw);
        }
        internal void Update(BleeckerDataListItem value, int index, int count)
        {
            item = value; stripe = owner.IsStriped && index % 2 == 0; last = index == count - 1;
            hasDescription = owner.DescriptionTemplate is not null || Present(item.Description);
            background.Background = stripe ? owner.Paint("Stripe") : null;
            background.CornerRadius = new CornerRadius(stripe ? 6 : 0);
            divider.Background = owner.Paint("Divider"); divider.Visibility = last ? Visibility.Collapsed : Visibility.Visible;
            Label.Set(owner, item.Label, owner.LabelTemplate, owner.Paint("Secondary"));
            Value.Set(owner, item.Value, owner.ValueTemplate, owner.Paint("Primary"), AnonymousNumber(item.Description) ? Scalar(item.Description) : "");
            Description.Set(owner, hasDescription ? item.Description : null, owner.DescriptionTemplate, owner.Paint("Secondary"));
            AutomationProperties.SetPositionInSet(this, index + 1); AutomationProperties.SetSizeOfSet(this, count);
            InvalidateMeasure(); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
        }
        internal void ReleaseContent() { Label.Release(); Value.Release(); Description.Release(); }
        protected override Size MeasureOverride(Size available)
        {
            var pad = stripe ? 12 : 0;
            var width = available.Width;
            if (double.IsInfinity(width))
            {
                Label.Measure(available); Value.Measure(available); Description.Measure(available);
                width = owner.Compact ? Math.Max(Label.DesiredSize.Width, Math.Max(Value.DesiredSize.Width, Description.DesiredSize.Width)) + pad * 2 :
                    Math.Max(Label.DesiredSize.Width / .4, Math.Max(Value.DesiredSize.Width, Description.DesiredSize.Width) / .6) + 24 + pad * 2;
            }
            var inner = Math.Max(0, width - pad * 2);
            labelWidth = owner.Compact ? inner : Math.Max(0, inner - 24) * .4;
            valueWidth = owner.Compact ? inner : Math.Max(0, inner - 24) * .6;
            Label.AlignRight = owner.FlowDirection == FlowDirection.RightToLeft;
            Value.AlignRight = Description.AlignRight = !owner.Compact || owner.FlowDirection == FlowDirection.RightToLeft;
            Label.Measure(new Size(labelWidth, double.PositiveInfinity)); Value.Measure(new Size(valueWidth, double.PositiveInfinity)); Description.Measure(new Size(valueWidth, double.PositiveInfinity));
            labelHeight = Label.DesiredSize.Height; valueHeight = Value.DesiredSize.Height;
            descriptionHeight = hasDescription ? 4 + Description.DesiredSize.Height : 0;
            bodyHeight = owner.Compact ? labelHeight + 6 + valueHeight + descriptionHeight : Math.Max(labelHeight, valueHeight + descriptionHeight);
            return new Size(width, 28 + bodyHeight + (last ? 0 : 1));
        }
        protected override Size ArrangeOverride(Size final)
        {
            var pad = stripe ? 12 : 0;
            var rtl = owner.FlowDirection == FlowDirection.RightToLeft;
            var labelX = owner.Compact || !rtl ? pad : final.Width - pad - labelWidth;
            var valueX = owner.Compact || rtl ? pad : pad + labelWidth + 24;
            var valueY = owner.Compact ? 14 + labelHeight + 6 : 14;
            background.Arrange(new Rect(0, 0, final.Width, final.Height)); divider.Arrange(new Rect(0, Math.Max(0, final.Height - 1), final.Width, last ? 0 : 1));
            Label.Arrange(new Rect(labelX, 14, labelWidth, labelHeight)); Value.Arrange(new Rect(valueX, valueY, valueWidth, valueHeight));
            Description.Arrange(new Rect(valueX, valueY + valueHeight + (hasDescription ? 4 : 0), valueWidth, Description.DesiredSize.Height));
            return final;
        }
        protected override AutomationPeer OnCreateAutomationPeer() => new RowPeer(this);
        private sealed class RowPeer(Entry owner) : FrameworkElementAutomationPeer(owner)
        {
            protected override string GetClassNameCore() => nameof(BleeckerDataListItem);
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;
            protected override string GetNameCore() => string.IsNullOrEmpty(base.GetNameCore()) ? string.Join(": ", new[] { Scalar(owner.item.Label), Scalar(owner.item.Value) }.Where(text => text.Length != 0)) : base.GetNameCore();
        }
    }
    private static bool Number(object? value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
    private static bool Zero(object? value) => Number(value) && Convert.ToDouble(value, CultureInfo.InvariantCulture) == 0;
    private static bool AnonymousNumber(object? value) => Zero(value) || value is double d && double.IsNaN(d) || value is float f && float.IsNaN(f);
    private static bool Present(object? value) => value switch { null or false => false, string text => text.Length != 0, _ => !AnonymousNumber(value) };
    private static string Scalar(object? value) => value switch
    {
        null or bool => "", string text => text,
        IFormattable number when Number(value) => Zero(value) ? "0" : number.ToString(null, CultureInfo.InvariantCulture),
        _ => ""
    };

    private sealed class Slot : Panel
    {
        internal TextBlock Text { get; } = new() { UseLayoutRounding = false, TextWrapping = TextWrapping.WrapWholeWords, LineStackingStrategy = LineStackingStrategy.BlockLineHeight };
        private readonly ContentPresenter rich = new();
        private readonly TextBlock suffix = new() { UseLayoutRounding = false };
        private readonly double fontSize, lineHeight;
        private readonly bool secondary;
        internal bool AlignRight { get; set; }
        private bool plain;
        internal Slot(string name, double fontSize, double lineHeight, bool secondary)
        {
            Name = name; this.fontSize = fontSize; this.lineHeight = lineHeight; this.secondary = secondary;
            UseLayoutRounding = false; Children.Add(Text); Children.Add(rich); Children.Add(suffix);
        }
        internal void Set(BleeckerDataList owner, object? content, DataTemplate? template, Brush foreground, string trailing = "")
        {
            plain = template is null && (content is null or string or bool || Number(content));
            if (!plain && template is null && content is not UIElement)
                throw new InvalidOperationException("DataList object content requires a DataTemplate or UIElement.");
            FlowDirection = owner.FlowDirection;
            foreach (var text in new[] { Text, suffix })
            {
                text.FontFamily = secondary ? owner.Font("BleeckerSecondaryFontFamily") : owner.FontFamily;
                text.FontSize = owner.FontSize * fontSize / 14; text.LineHeight = owner.FontSize * lineHeight / 14;
                text.FontWeight = secondary ? Microsoft.UI.Text.FontWeights.Normal : Microsoft.UI.Text.FontWeights.Medium;
                text.Foreground = foreground; text.CharacterSpacing = owner.CharacterSpacing; text.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled;
            }
            Text.Text = Regex.Replace(plain ? Scalar(content) + trailing : "", "[ \\t\\r\\n\\f]+", " ").Trim(' ');
            Text.Visibility = plain && Text.Text.Length != 0 ? Visibility.Visible : Visibility.Collapsed;
            suffix.Text = plain ? "" : trailing; suffix.Visibility = suffix.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
            rich.Visibility = plain ? Visibility.Collapsed : Visibility.Visible;
            if (!ReferenceEquals(rich.Content, plain ? null : content) || !ReferenceEquals(rich.ContentTemplate, template))
            {
                rich.Content = null; rich.ContentTemplate = template; rich.Content = plain ? null : content;
            }
            rich.FontFamily = Text.FontFamily; rich.FontSize = Text.FontSize; rich.FontWeight = Text.FontWeight; rich.Foreground = foreground;
            InvalidateMeasure();
        }
        internal void Release() => rich.Content = null;
        protected override Size MeasureOverride(Size available)
        {
            var alignment = AlignRight ? TextAlignment.Right : TextAlignment.Left;
            if (Text.TextAlignment != alignment) Text.TextAlignment = alignment;
            // Measure every child, including the collapsed scalar host. Leaving
            // that host dirty keeps native layout pending for rich content.
            Text.Measure(available);
            suffix.Measure(available); rich.Measure(new Size(Math.Max(0, available.Width - suffix.DesiredSize.Width), available.Height));
            if (plain) return new Size(Text.DesiredSize.Width, Text.Text.Length == 0 ? 0 : Text.DesiredSize.Height);
            return new Size(rich.DesiredSize.Width + suffix.DesiredSize.Width, Math.Max(rich.DesiredSize.Height, suffix.DesiredSize.Height));
        }
        protected override Size ArrangeOverride(Size final)
        {
            Text.Arrange(new Rect(0, 0, final.Width, final.Height));
            if (!plain)
            {
                var width = rich.DesiredSize.Width + suffix.DesiredSize.Width;
                var x = AlignRight ? final.Width - width : 0;
                rich.Arrange(new Rect(x, 0, rich.DesiredSize.Width, final.Height)); suffix.Arrange(new Rect(x + rich.DesiredSize.Width, 0, suffix.DesiredSize.Width, final.Height));
            }
            return final;
        }
    }
}
