using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

public enum BleeckerIconBadgeSize { Medium, Large }
public enum BleeckerIconBadgeVariant { Primary, Subtle, Outlined }

/// <summary>A passive Bleecker icon surface for consumer-owned content.</summary>
public sealed class BleeckerIconBadge : ContentControl
{
    private ResourceDictionary? palette;
    private Grid? host;
    private readonly BadgeContent slot;

    public BleeckerIconBadge()
    {
        DefaultStyleKey = typeof(BleeckerIconBadge);
        slot = new(this);
        ApplyVariant();
        foreach (var property in new[] { ForegroundProperty, FontFamilyProperty, FontSizeProperty,
            FontWeightProperty, FontStyleProperty, FontStretchProperty, CharacterSpacingProperty,
            IsTextScaleFactorEnabledProperty, FlowDirectionProperty, LanguageProperty })
            RegisterPropertyChangedCallback(property, (_, _) => slot.RefreshPresentation());
        RegisterPropertyChangedCallback(HorizontalContentAlignmentProperty, (_, _) => slot.InvalidateArrange());
        RegisterPropertyChangedCallback(VerticalContentAlignmentProperty, (_, _) => slot.InvalidateArrange());
    }

    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(nameof(Size),
        typeof(BleeckerIconBadgeSize), typeof(BleeckerIconBadge), new PropertyMetadata(BleeckerIconBadgeSize.Large,
            (owner, _) => { var badge = (BleeckerIconBadge)owner; badge.ValidateSize(); badge.InvalidateMeasure(); }));
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(nameof(Variant),
        typeof(BleeckerIconBadgeVariant), typeof(BleeckerIconBadge), new PropertyMetadata(BleeckerIconBadgeVariant.Primary,
            (owner, _) => ((BleeckerIconBadge)owner).ApplyVariant()));

    public BleeckerIconBadgeSize Size { get => (BleeckerIconBadgeSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public BleeckerIconBadgeVariant Variant { get => (BleeckerIconBadgeVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    private double Extent => (double)ResourcesForSize()[Size == BleeckerIconBadgeSize.Medium ? "BleeckerIconBadgeMediumExtent" : "BleeckerIconBadgeLargeExtent"];
    private ResourceDictionary ResourcesForSize()
    {
        ValidateSize();
        return Application.Current.Resources;
    }
    private void ValidateSize()
    {
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
    }
    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        var extent = Extent;
        var width = Math.Max(MinWidth, Math.Min(MaxWidth, double.IsNaN(Width) ? extent : Width));
        var height = Math.Max(MinHeight, Math.Min(MaxHeight, double.IsNaN(Height) ? extent : Height));
        base.MeasureOverride(new(width, height));
        return new(width, height);
    }
    protected override void OnApplyTemplate()
    {
        host?.Children.Remove(slot);
        base.OnApplyTemplate();
        host = GetTemplateChild("PART_ContentHost") as Grid;
        host?.Children.Add(slot);
        slot.Refresh();
    }
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        slot?.Refresh();
    }
    protected override void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)
    {
        base.OnContentTemplateChanged(oldContentTemplate, newContentTemplate);
        slot?.Refresh();
    }
    protected override void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)
    {
        base.OnContentTemplateSelectorChanged(oldContentTemplateSelector, newContentTemplateSelector);
        slot?.Refresh();
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new BadgePeer(this);
    private sealed class BadgePeer(BleeckerIconBadge owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerIconBadge);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetNameCore() => AutomationProperties.GetName(owner);
        protected override bool IsControlElementCore() => AutomationProperties.GetName(owner).Length > 0;
        protected override bool IsContentElementCore() => IsControlElementCore();
    }
    private void ApplyVariant()
    {
        if (!Enum.IsDefined(Variant)) throw new ArgumentOutOfRangeException(nameof(Variant));
        var next = new ResourceDictionary { Source = new Uri($"ms-appx:///Orchard/Themes/IconBadges/{Variant}.g.xaml") };
        if (palette is null) { palette = next; Resources.MergedDictionaries.Add(palette); }
        else foreach (var theme in new[] { "Light", "Default" })
        foreach (var key in new[] { "IconBadgeBackground", "IconBadgeBorderBrush", "IconBadgeForeground" })
        {
            var current = (SolidColorBrush)((ResourceDictionary)palette.ThemeDictionaries[theme])[key];
            var replacement = (SolidColorBrush)((ResourceDictionary)next.ThemeDictionaries[theme])[key];
            current.Color = replacement.Color;
            current.Opacity = replacement.Opacity;
        }
    }
    private sealed class BadgeContent(BleeckerIconBadge owner) : Panel
    {
        private readonly TextBlock text = new() { UseLayoutRounding = false, TextWrapping = TextWrapping.NoWrap,
            LineStackingStrategy = LineStackingStrategy.BlockLineHeight };
        private readonly ContentPresenter rich = new() { UseLayoutRounding = false,
            HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
        private readonly TextBlock measure = new();
        private readonly BleeckerReferenceText.IntrinsicWidth intrinsic = new();
        private string logical = "";
        private bool plain;
        private Windows.Foundation.Size childSize;
        internal void Refresh()
        {
            plain = owner.ContentTemplate is null && owner.ContentTemplateSelector is null &&
                owner.Content is null or bool or string or char or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal or BigInteger;
            if (!plain && owner.ContentTemplate is null && owner.ContentTemplateSelector is null && owner.Content is not UIElement)
                throw new ArgumentException("IconBadge object content requires a DataTemplate, DataTemplateSelector, or UIElement.");
            if (Children.Count == 0) { Children.Add(text); Children.Add(rich); }
            rich.Content = null;
            rich.ContentTemplate = owner.ContentTemplate;
            rich.ContentTemplateSelector = owner.ContentTemplateSelector;
            rich.Content = plain ? null : owner.Content;
            logical = Regex.Replace(plain ? Scalar(owner.Content) : "", "[ \\t\\r\\n\\f]+", " ").Trim(' ');
            text.Visibility = plain && logical.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            rich.Visibility = plain ? Visibility.Collapsed : Visibility.Visible;
            RefreshPresentation();
        }
        private static string Scalar(object? value) => value switch
        {
            null or bool => "", double number => BleeckerNumberText.Format(number),
            float number => BleeckerNumberText.Format(double.Parse(number.ToString("R", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)),
            decimal number when number == 0 => "0", IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()!
        };
        internal void RefreshPresentation()
        {
            // WinUI Language writes invalidate layout even for the same value.
            // Synchronize inherited presentation outside MeasureOverride.
            UseLayoutRounding = false;
            text.FontFamily = rich.FontFamily = measure.FontFamily = owner.FontFamily;
            text.FontSize = rich.FontSize = measure.FontSize = owner.FontSize;
            text.FontWeight = rich.FontWeight = measure.FontWeight = owner.FontWeight;
            text.FontStyle = rich.FontStyle = measure.FontStyle = owner.FontStyle;
            text.FontStretch = rich.FontStretch = measure.FontStretch = owner.FontStretch;
            text.CharacterSpacing = rich.CharacterSpacing = measure.CharacterSpacing = owner.CharacterSpacing;
            text.Foreground = rich.Foreground = owner.Foreground;
            text.IsTextScaleFactorEnabled = rich.IsTextScaleFactorEnabled = measure.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled;
            text.FlowDirection = rich.FlowDirection = measure.FlowDirection = owner.FlowDirection;
            if (text.Language != owner.Language) text.Language = owner.Language;
            if (rich.Language != owner.Language) rich.Language = owner.Language;
            if (measure.Language != owner.Language) measure.Language = owner.Language;
            text.LineHeight = owner.FontSize * 1.5;
            InvalidateMeasure();
        }
        protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
        {
            if (plain)
            {
                double WidthOf(string value) { BleeckerReferenceText.Set(measure, value); return intrinsic.Measure(measure, value); }
                var words = logical.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var width = logical.Length == 0 ? 0 : Math.Min(WidthOf(logical), Math.Max(availableSize.Width, words.Max(WidthOf)));
                var lines = new List<string>(); var line = "";
                foreach (var word in words)
                {
                    var next = line.Length == 0 ? word : line + " " + word;
                    if (line.Length > 0 && WidthOf(next) > width) { lines.Add(line); line = word; }
                    else line = next;
                }
                if (line.Length > 0) lines.Add(line);
                var rendered = string.Join('\n', lines);
                if (text.Text != rendered) BleeckerReferenceText.Set(text, rendered);
                text.Measure(new(width, double.PositiveInfinity));
                childSize = new(width, text.DesiredSize.Height);
            }
            else { rich.Measure(new(double.PositiveInfinity, double.PositiveInfinity)); childSize = rich.DesiredSize; }
            return new(Math.Min(availableSize.Width, childSize.Width), Math.Min(availableSize.Height, childSize.Height));
        }
        protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
        {
            var child = plain ? (FrameworkElement)text : rich;
            var width = owner.HorizontalContentAlignment == HorizontalAlignment.Stretch ? finalSize.Width : childSize.Width;
            var height = owner.VerticalContentAlignment == VerticalAlignment.Stretch ? finalSize.Height : childSize.Height;
            var x = owner.HorizontalContentAlignment switch { HorizontalAlignment.Left or HorizontalAlignment.Stretch => 0, HorizontalAlignment.Right => finalSize.Width - width, _ => (finalSize.Width - width) / 2 };
            var y = owner.VerticalContentAlignment switch { VerticalAlignment.Top or VerticalAlignment.Stretch => 0, VerticalAlignment.Bottom => finalSize.Height - height, _ => (finalSize.Height - height) / 2 };
            child.Arrange(new Rect(x, y, width, height));
            return finalSize;
        }
    }
}
