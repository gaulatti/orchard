using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerTimeline
{
    private sealed partial class Entry : Panel
    {
        private readonly BleeckerTimeline owner;
        private BleeckerTimelineItem item;
        internal string Key { get; }
        private readonly Border connector = new() { Name = "TimelineConnector", IsHitTestVisible = false };
        private readonly Grid icon = new() { Name = "TimelineIcon", Width = 36, Height = 36, UseLayoutRounding = false };
        private readonly Border frame = new() { Name = "TimelineIconFrame", CornerRadius = new CornerRadius(9), BorderThickness = new Thickness(1), BackgroundSizing = BackgroundSizing.OuterBorderEdge, IsHitTestVisible = false };
        private readonly Paragraph ordinal = new(12, 16, "BleeckerPrimarySemiboldFontFamily", Microsoft.UI.Text.FontWeights.SemiBold) { Name = "TimelineOrdinal", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        private readonly Paragraph iconText = new(16, 24, "BleeckerPrimaryFontFamily", Microsoft.UI.Text.FontWeights.Normal) { Name = "TimelineIconText", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        private readonly ContentPresenter rich = new() { Margin = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        private readonly LayoutHost body = new() { Name = "TimelineBody", UseLayoutRounding = false };
        private readonly LayoutHost header = new() { Name = "TimelineHeader", UseLayoutRounding = false };
        private readonly Paragraph title = new(14, 20, "BleeckerPrimaryMediumFontFamily", Microsoft.UI.Text.FontWeights.Medium) { Name = "TimelineTitle" };
        private readonly Paragraph timestamp = new(11, 16.5, "BleeckerSecondaryFontFamily", Microsoft.UI.Text.FontWeights.Normal) { Name = "TimelineTimestamp" };
        private readonly Paragraph description = new(14, 24, "BleeckerSecondaryFontFamily", Microsoft.UI.Text.FontWeights.Normal) { Name = "TimelineDescription" };
        private bool last, hasTimestamp, hasDescription;
        private double bodyWidth, bodyHeight, titleWidth, timeWidth, headerHeight;
        private (double Body, double Title, double Time, bool Compact)? measuredWidths;

        internal Entry(BleeckerTimeline owner, BleeckerTimelineItem item)
        {
            this.owner = owner; this.item = item; Key = item.Id; Name = "TimelineRow"; UseLayoutRounding = false; FlowDirection = FlowDirection.LeftToRight;
            InitializeShadow(); icon.Children.Add(frame); icon.Children.Add(ordinal); icon.Children.Add(iconText); icon.Children.Add(rich);
            header.Children.Add(title); header.Children.Add(timestamp); body.Children.Add(header); body.Children.Add(description);
            header.MeasureChildren = () =>
            {
                title.Measure(new Size(titleWidth, double.PositiveInfinity)); timestamp.Measure(new Size(timeWidth, double.PositiveInfinity));
                headerHeight = owner.Compact ? title.DesiredSize.Height + (hasTimestamp ? 4 + timestamp.DesiredSize.Height : 0) : Math.Max(title.DesiredSize.Height, hasTimestamp ? timestamp.DesiredSize.Height : 0);
                return new Size(bodyWidth, headerHeight);
            };
            body.MeasureChildren = () =>
            {
                header.Measure(new Size(bodyWidth, double.PositiveInfinity)); description.Measure(new Size(bodyWidth, double.PositiveInfinity));
                bodyHeight = Math.Max(36, 4 + headerHeight + (hasDescription ? 4 + description.DesiredSize.Height : 0));
                return new Size(bodyWidth, bodyHeight);
            };
            Children.Add(connector); Children.Add(icon); Children.Add(body);
            foreach (var decoration in new UIElement[] { connector, frame, ordinal, iconText }) AutomationProperties.SetAccessibilityView(decoration, AccessibilityView.Raw);
        }
        internal void Update(BleeckerTimelineItem value, int index, int count)
        {
            item = value; last = index == count - 1; var status = item.ResolveStatus(index);
            hasTimestamp = !string.IsNullOrEmpty(item.Timestamp); hasDescription = !string.IsNullOrEmpty(item.Description);
            title.Set(owner, item.Title, owner.Paint("Primary")); timestamp.Set(owner, item.Timestamp ?? "", owner.Paint("Secondary")); description.Set(owner, item.Description ?? "", owner.Paint("Secondary"));
            timestamp.Visibility = hasTimestamp ? Visibility.Visible : Visibility.Collapsed; description.Visibility = hasDescription ? Visibility.Visible : Visibility.Collapsed;
            frame.Background = owner.Paint(status.ToString()); frame.BorderBrush = owner.Paint("Border"); connector.Background = owner.Paint("Line"); connector.Visibility = last ? Visibility.Collapsed : Visibility.Visible;
            var foreground = owner.Paint(status == BleeckerTimelineStatus.Pending ? "Secondary" : "OnStatus");
            var defaultIcon = item.Icon is null && owner.IconTemplate is null;
            var scalar = owner.IconTemplate is null && (item.Icon is string or bool || IsNumber(item.Icon));
            if (!defaultIcon && !scalar && owner.IconTemplate is null && item.Icon is not UIElement)
                throw new InvalidOperationException("Timeline object icons require an IconTemplate or UIElement.");
            ordinal.Set(owner, defaultIcon ? (index + 1).ToString(CultureInfo.InvariantCulture) : "", foreground);
            iconText.Set(owner, scalar ? Scalar(item.Icon) : "", foreground);
            ordinal.Visibility = defaultIcon ? Visibility.Visible : Visibility.Collapsed; iconText.Visibility = scalar ? Visibility.Visible : Visibility.Collapsed;
            rich.Visibility = !defaultIcon && !scalar ? Visibility.Visible : Visibility.Collapsed;
            var content = rich.Visibility == Visibility.Visible ? item.Icon : null;
            if (!ReferenceEquals(rich.Content, content) || !ReferenceEquals(rich.ContentTemplate, owner.IconTemplate))
            { rich.Content = null; rich.ContentTemplate = owner.IconTemplate; rich.Content = content; }
            rich.FontFamily = owner.Font("BleeckerPrimaryFontFamily"); rich.FontSize = owner.FontSize * 16 / 14; rich.Foreground = foreground; rich.FlowDirection = owner.FlowDirection;
            AutomationProperties.SetItemStatus(this, status.ToString()); AutomationProperties.SetPositionInSet(this, index + 1); AutomationProperties.SetSizeOfSet(this, count);
            InvalidateMeasure(); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
        }
        internal void Release() { rich.Content = null; ClearShadow(); }
        protected override Size MeasureOverride(Size available)
        {
            var width = available.Width;
            if (double.IsInfinity(width)) width = 52 + Math.Max(description.PreferredWidth, title.PreferredWidth + (hasTimestamp ? 4 + timestamp.PreferredWidth : 0));
            var headerMinimum = owner.Compact ? Math.Max(title.MinimumWidth, hasTimestamp ? timestamp.MinimumWidth : 0) : title.MinimumWidth + (hasTimestamp ? 4 + timestamp.MinimumWidth : 0);
            bodyWidth = Math.Max(Math.Max(0, width - 52), Math.Max(headerMinimum, hasDescription ? description.MinimumWidth : 0));
            titleWidth = owner.Compact ? bodyWidth : title.PreferredWidth; timeWidth = owner.Compact ? bodyWidth : timestamp.PreferredWidth;
            if (!owner.Compact && hasTimestamp && titleWidth + timeWidth + 4 > bodyWidth)
            {
                var remaining = Math.Max(0, bodyWidth - 4); var total = titleWidth + timeWidth;
                var proposedTitle = total == 0 ? 0 : remaining * titleWidth / total;
                var proposedTime = total == 0 ? 0 : remaining * timeWidth / total;
                if (proposedTitle < title.MinimumWidth) { titleWidth = title.MinimumWidth; timeWidth = Math.Max(timestamp.MinimumWidth, remaining - titleWidth); }
                else if (proposedTime < timestamp.MinimumWidth) { timeWidth = timestamp.MinimumWidth; titleWidth = Math.Max(title.MinimumWidth, remaining - timeWidth); }
                else { titleWidth = proposedTitle; timeWidth = proposedTime; }
            }
            var widths = (bodyWidth, titleWidth, timeWidth, owner.Compact);
            if (measuredWidths != widths) { header.InvalidateMeasure(); body.InvalidateMeasure(); measuredWidths = widths; }
            body.Measure(new Size(bodyWidth, double.PositiveInfinity));
            // Keep even the hidden scalar hosts measured when a consumer icon
            // is active, so native layout can reach a stable presentation.
            ordinal.Text.Measure(new Size(34, double.PositiveInfinity)); iconText.Text.Measure(new Size(34, double.PositiveInfinity));
            MeasureIcon(ordinal); MeasureIcon(iconText);
            icon.Measure(new Size(36, 36)); connector.Measure(new Size(1, bodyHeight + (last ? 0 : 28)));
            return new Size(width, bodyHeight + (last ? 0 : 28));
        }
        private static void MeasureIcon(Paragraph paragraph)
        {
            // The source flex item's automatic minimum preserves long words;
            // wrapped text can overflow the fixed-height frame vertically.
            var width = Math.Max(paragraph.MinimumWidth, Math.Min(34, paragraph.PreferredWidth));
            paragraph.Text.Measure(new Size(width, double.PositiveInfinity));
            paragraph.Width = width; paragraph.Height = paragraph.Text.Text.Length == 0 ? 0 : paragraph.Text.DesiredSize.Height;
        }
        protected override Size ArrangeOverride(Size final)
        {
            var rtl = owner.FlowDirection == FlowDirection.RightToLeft;
            icon.Arrange(new Rect(rtl ? final.Width - 36 : 0, 0, 36, 36));
            var bx = rtl ? final.Width - 52 - bodyWidth : 52;
            body.Arrange(new Rect(bx, 0, bodyWidth, bodyHeight)); header.Arrange(new Rect(0, 4, bodyWidth, headerHeight));
            var tx = owner.Compact || !rtl ? 0 : bodyWidth - titleWidth;
            var timeX = owner.Compact || rtl ? 0 : bodyWidth - timeWidth;
            title.Arrange(new Rect(tx, owner.Compact ? 0 : (headerHeight - title.DesiredSize.Height) / 2, titleWidth, title.DesiredSize.Height));
            timestamp.Arrange(new Rect(timeX, owner.Compact ? title.DesiredSize.Height + 4 : (headerHeight - timestamp.DesiredSize.Height) / 2, timeWidth, timestamp.DesiredSize.Height));
            description.Arrange(new Rect(0, 4 + headerHeight + 4, bodyWidth, description.DesiredSize.Height));
            // The pinned source uses physical left:17px even under RTL.
            connector.Arrange(new Rect(17, 32, 1, last ? 0 : final.Height));
            return final;
        }
        internal void Present() { title.Align(owner); timestamp.Align(owner); description.Align(owner); ordinal.Align(owner); iconText.Align(owner); UpdateShadow(); }
        protected override AutomationPeer OnCreateAutomationPeer() => new RowPeer(this);
        private sealed class RowPeer(Entry owner) : FrameworkElementAutomationPeer(owner)
        {
            protected override string GetClassNameCore() => nameof(BleeckerTimelineItem);
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;
            protected override string GetNameCore() => string.IsNullOrEmpty(base.GetNameCore()) ? owner.item.Title : base.GetNameCore();
            protected override string GetHelpTextCore() => string.IsNullOrEmpty(base.GetHelpTextCore()) ? owner.item.Description ?? "" : base.GetHelpTextCore();
        }
    }
    private sealed class LayoutHost : Panel
    {
        internal Func<Size>? MeasureChildren { get; set; }
        protected override Size MeasureOverride(Size available) => (MeasureChildren ?? throw new InvalidOperationException("Timeline layout requires a measurement owner."))();
        protected override Size ArrangeOverride(Size final) => final;
    }
    private static bool IsNumber(object? value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
    private static string Scalar(object? value) => value switch { null or bool => "", string text => text, IFormattable number when IsNumber(value) => Convert.ToDouble(value, CultureInfo.InvariantCulture) == 0 ? "0" : number.ToString(null, CultureInfo.InvariantCulture), _ => "" };
}
