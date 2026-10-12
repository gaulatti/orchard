using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Data.Text;

namespace Orchard;

public enum BleeckerStatusBadgeVariant { Default, Live, Offline, Warning, Info }

/// <summary>A consumer-owned status label with an optional description.</summary>
public sealed class BleeckerStatusBadge : Control
{
    private ResourceDictionary? palette;
    private TextBlock? description;
    private Border? surface;
    private Grid? layout;
    private TextBlock? labelText;
    private FrameworkElement? dot;
    private Grid? badgeContent;
    public BleeckerStatusBadge() { DefaultStyleKey = typeof(BleeckerStatusBadge); ApplyVariant(); }
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(BleeckerStatusBadge), new PropertyMetadata("", OnTextChanged));
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(BleeckerStatusBadge), new PropertyMetadata("", OnTextChanged));
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(nameof(Variant), typeof(BleeckerStatusBadgeVariant), typeof(BleeckerStatusBadge), new PropertyMetadata(BleeckerStatusBadgeVariant.Default, (owner, _) => ((BleeckerStatusBadge)owner).ApplyVariant()));
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string Description { get => (string)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public BleeckerStatusBadgeVariant Variant { get => (BleeckerStatusBadgeVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    private static void OnTextChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        var badge = (BleeckerStatusBadge)owner;
        badge.UpdateDescription();
        FrameworkElementAutomationPeer.FromElement(badge)?.InvalidatePeer();
    }
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        description = GetTemplateChild("PART_Description") as TextBlock;
        surface = GetTemplateChild("PART_Badge") as Border;
        layout = GetTemplateChild("PART_Layout") as Grid;
        labelText = GetTemplateChild("PART_Label") as TextBlock;
        dot = GetTemplateChild("PART_Dot") as FrameworkElement;
        badgeContent = GetTemplateChild("PART_BadgeContent") as Grid;
        UpdateDescription();
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (layout is not null && surface is not null && description is not null && labelText is not null && layout.ColumnDefinitions.Count == 2)
        {
            labelText.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            description.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            var fixedWidth = surface.Padding.Left + surface.Padding.Right + surface.BorderThickness.Left + surface.BorderThickness.Right;
            var badgeWidth = fixedWidth + 12 + labelText.DesiredSize.Width;
            var descriptionWidth = description.Visibility == Visibility.Visible ? description.DesiredSize.Width - description.Margin.Left - description.Margin.Right : 0;
            var gap = description.Visibility == Visibility.Visible ? description.Margin.Left + description.Margin.Right : 0;
            // CSS flex-shrink weights content widths; fixed border/padding and
            // the inter-item gap do not participate in proportional shrinking.
            var contentWidth = Math.Max(0, badgeWidth - fixedWidth);
            if (badgeWidth + descriptionWidth + gap > availableSize.Width && contentWidth + descriptionWidth > 0)
            {
                badgeWidth = fixedWidth + contentWidth * Math.Max(0, availableSize.Width - fixedWidth - gap) / (contentWidth + descriptionWidth);
                var labelMinimum = labelText is null ? 0 : MinimumWordWidth(labelText);
                var descriptionMinimum = description.Visibility == Visibility.Visible ? MinimumWordWidth(description) : 0;
                var badgeMinimum = fixedWidth + 12 + labelMinimum; // dot + inner gap
                badgeWidth = Math.Max(badgeMinimum, Math.Min(badgeWidth, availableSize.Width - gap - descriptionMinimum));
                description.MinWidth = descriptionMinimum;
            }
            else description.ClearValue(MinWidthProperty);
            if (dot is not null && badgeContent is not null && contentWidth > 6)
            {
                var dotWidth = 6 * Math.Min(1, Math.Max(0, badgeWidth - fixedWidth - 6) / (contentWidth - 6));
                dot.Width = dotWidth;
                badgeContent.ColumnDefinitions[0].Width = new GridLength(dotWidth);
            }
            layout.ColumnDefinitions[0].Width = new GridLength(badgeWidth);
        }
        return base.MeasureOverride(availableSize);
    }
    private static double MinimumWordWidth(TextBlock text)
    {
        var probe = new TextBlock { FontFamily = text.FontFamily, FontSize = text.FontSize, FontWeight = text.FontWeight,
            FontStyle = text.FontStyle, FontStretch = text.FontStretch, CharacterSpacing = text.CharacterSpacing,
            IsTextScaleFactorEnabled = text.IsTextScaleFactorEnabled, Language = text.Language };
        var words = new WordsSegmenter(text.Language).GetTokens(text.Text ?? "").Select(token => token.Text).ToArray();
        var width = 0d;
        foreach (var word in words.Length == 0 ? new[] { text.Text ?? "" } : words)
        {
            probe.Text = word;
            probe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            width = Math.Max(width, probe.DesiredSize.Width);
        }
        return Math.Ceiling(width);
    }
    private void UpdateDescription() { if (description is not null) description.Visibility = string.IsNullOrEmpty(Description) ? Visibility.Collapsed : Visibility.Visible; }
    private void ApplyVariant()
    {
        if (!Enum.IsDefined(Variant)) throw new ArgumentOutOfRangeException(nameof(Variant));
        var next = new ResourceDictionary { Source = new Uri($"ms-appx:///Orchard/Themes/StatusBadges/{Variant}.g.xaml") };
        if (palette is null) { palette = next; Resources.MergedDictionaries.Add(palette); return; }
        foreach (var theme in new[] { "Light", "Default" })
        foreach (var key in new[] { "StatusBadgeBackground", "StatusBadgeBorder", "StatusBadgeForeground", "StatusBadgeDot" })
        {
            var current = (SolidColorBrush)((ResourceDictionary)palette.ThemeDictionaries[theme])[key];
            var replacement = (SolidColorBrush)((ResourceDictionary)next.ThemeDictionaries[theme])[key];
            current.Color = replacement.Color; current.Opacity = replacement.Opacity;
        }
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new BadgePeer(this);
    private sealed class BadgePeer(BleeckerStatusBadge owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerStatusBadge);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
        protected override string GetNameCore() => string.IsNullOrEmpty(base.GetNameCore()) ? owner.Label ?? "" : base.GetNameCore();
        protected override string GetHelpTextCore() => string.IsNullOrEmpty(base.GetHelpTextCore()) ? owner.Description ?? "" : base.GetHelpTextCore();
        protected override IList<AutomationPeer> GetChildrenCore() => null!;
    }
}
