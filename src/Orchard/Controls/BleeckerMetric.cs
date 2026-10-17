using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Data;
using System.Globalization;
using Windows.Foundation;

namespace Orchard;

/// <summary>A noninteractive formatted metric with consumer-owned inline content.</summary>
public sealed class BleeckerMetric : Control
{
    private RichTextBlock? text;
    private Paragraph? paragraph;
    private Run? valueRun;
    private Inline? prefixInline, suffixInline;
    private object? renderedPrefix, renderedSuffix;
    private DataTemplate? renderedPrefixTemplate, renderedSuffixTemplate;
    private string accessibleText = string.Empty;
    private Canvas? slots;
    private ContentPresenter? prefixHost, suffixHost;

    public BleeckerMetric()
    {
        DefaultStyleKey = typeof(BleeckerMetric);
        LayoutUpdated += (_, _) => AlignSlots();
        RegisterPropertyChangedCallback(LanguageProperty, (_, _) => Update());
        RegisterPropertyChangedCallback(FontSizeProperty, (_, _) => UpdateLineHeight());
    }

    // XAML Binding cannot convert a boxed primitive to IReference<T> at a
    // nullable-typed custom DP boundary. Object storage preserves null and the
    // original boxed number while the public CLR contract remains nullable.
    public static readonly DependencyProperty ValueProperty = Register(nameof(Value), typeof(object), null);
    public static readonly DependencyProperty FormatProperty = Register(nameof(Format), typeof(BleeckerMetricFormat), BleeckerMetricFormat.Number);
    public static readonly DependencyProperty CurrencyProperty = Register(nameof(Currency), typeof(string), "USD");
    public static readonly DependencyProperty DecimalsProperty = Register(nameof(Decimals), typeof(object), null);
    public static readonly DependencyProperty PrefixProperty = Register(nameof(Prefix), typeof(object), null);
    public static readonly DependencyProperty SuffixProperty = Register(nameof(Suffix), typeof(object), null);
    public static readonly DependencyProperty PrefixTemplateProperty = Register(nameof(PrefixTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty SuffixTemplateProperty = Register(nameof(SuffixTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty LineHeightProperty = DependencyProperty.Register(nameof(LineHeight), typeof(double), typeof(BleeckerMetric), new PropertyMetadata(0d, (owner, _) => ((BleeckerMetric)owner).UpdateLineHeight()));

    public double? Value { get => ReadNumber(GetValue(ValueProperty)); set => SetValue(ValueProperty, value); }
    public BleeckerMetricFormat Format { get => (BleeckerMetricFormat)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }
    public string Currency { get => (string)GetValue(CurrencyProperty); set => SetValue(CurrencyProperty, value); }
    public int? Decimals
    {
        get
        {
            var value = ReadNumber(GetValue(DecimalsProperty));
            if (value is null) return null;
            if (!double.IsFinite(value.Value) || value < int.MinValue || value > int.MaxValue || value != Math.Truncate(value.Value)) throw new ArgumentOutOfRangeException(nameof(Decimals));
            return (int)value.Value;
        }
        set => SetValue(DecimalsProperty, value);
    }
    public object? Prefix { get => GetValue(PrefixProperty); set => SetValue(PrefixProperty, value); }
    public object? Suffix { get => GetValue(SuffixProperty); set => SetValue(SuffixProperty, value); }
    public DataTemplate? PrefixTemplate { get => (DataTemplate?)GetValue(PrefixTemplateProperty); set => SetValue(PrefixTemplateProperty, value); }
    public DataTemplate? SuffixTemplate { get => (DataTemplate?)GetValue(SuffixTemplateProperty); set => SetValue(SuffixTemplateProperty, value); }
    /// <summary>Zero uses the source's 1.5 line-height ratio.</summary>
    public double LineHeight { get => (double)GetValue(LineHeightProperty); set => SetValue(LineHeightProperty, value); }
    public string FormattedValue => BleeckerMetricFormatter.Format(Value, Format, Currency, Decimals, Language);

    private static DependencyProperty Register(string name, Type type, object? initial) =>
        DependencyProperty.Register(name, type, typeof(BleeckerMetric), new PropertyMetadata(initial, (owner, _) => ((BleeckerMetric)owner).Update()));

    private static double? ReadNumber(object? value) => value switch
    {
        null => null,
        byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => Convert.ToDouble(value, CultureInfo.InvariantCulture),
        _ => throw new ArgumentException("Metric values and precision require numeric data.")
    };

    protected override void OnApplyTemplate()
    {
        if (prefixHost is not null) prefixHost.Content = null;
        if (suffixHost is not null) suffixHost.Content = null;
        slots?.Children.Clear(); prefixHost = suffixHost = null;
        if (paragraph is not null) foreach (var inline in paragraph.Inlines.OfType<InlineUIContainer>()) inline.Child = null;
        if (paragraph is not null) paragraph.Inlines.Clear();
        if (text is not null) text.Blocks.Clear();
        base.OnApplyTemplate();
        text = GetTemplateChild("PART_Text") as RichTextBlock;
        slots = GetTemplateChild("PART_Slots") as Canvas;
        if ((text is null) != (slots is null)) throw new InvalidOperationException("Metric templates require PART_Text and PART_Slots together.");
        paragraph = text is null ? null : new Paragraph();
        prefixInline = suffixInline = null;
        valueRun = new Run();
        if (paragraph is not null) paragraph.Inlines.Add(valueRun);
        if (text is not null) { text.Blocks.Clear(); text.Blocks.Add(paragraph!); Typography.SetNumeralAlignment(text, FontNumeralAlignment.Tabular); }
        UpdateLineHeight();
        Update();
    }

    private void Update()
    {
        var formatted = FormattedValue;
        accessibleText = (PrefixTemplate is null ? Scalar(Prefix) : "") + formatted + (SuffixTemplate is null ? Scalar(Suffix) : "");
        if (text is not null)
        {
            SyncSlot(Prefix, PrefixTemplate, true);
            valueRun!.Text = formatted;
            SyncSlot(Suffix, SuffixTemplate, false);
        }
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }

    private void AlignSlots()
    {
        if (!IsLoaded || slots is null) return;
        Align(prefixHost, prefixInline); Align(suffixHost, suffixInline);
    }

    private void Align(ContentPresenter? host, Inline? inline)
    {
        if (host is null || inline is not InlineUIContainer { Child: Border spacer }) return;
        var size = host.DesiredSize;
        if (spacer.Width != size.Width || spacer.Height != size.Height)
        {
            spacer.Width = size.Width; spacer.Height = size.Height;
            return;
        }
        if (!spacer.IsLoaded || spacer.ActualWidth != size.Width || spacer.ActualHeight != size.Height) return;
        var position = spacer.TransformToVisual(slots).TransformPoint(new Point());
        if (Canvas.GetLeft(host) != position.X) Canvas.SetLeft(host, position.X);
        if (Canvas.GetTop(host) != position.Y) Canvas.SetTop(host, position.Y);
        host.Opacity = 1; host.IsHitTestVisible = true;
    }

    private void UpdateLineHeight()
    {
        if (!double.IsFinite(LineHeight) || LineHeight < 0) throw new ArgumentOutOfRangeException(nameof(LineHeight));
        if (text is not null) text.LineHeight = LineHeight == 0 ? FontSize * 1.5 : LineHeight;
    }

    private void SyncSlot(object? content, DataTemplate? template, bool prefix)
    {
        var previous = prefix ? prefixInline : suffixInline;
        if (previous is not null && ReferenceEquals(content, prefix ? renderedPrefix : renderedSuffix) && ReferenceEquals(template, prefix ? renderedPrefixTemplate : renderedSuffixTemplate)) return;
        if (previous is not null)
        {
            var oldHost = prefix ? prefixHost : suffixHost;
            if (oldHost is not null) { oldHost.Content = null; slots!.Children.Remove(oldHost); }
            if (prefix) prefixHost = null; else suffixHost = null;
            if (previous is InlineUIContainer container) container.Child = null;
            paragraph!.Inlines.Remove(previous);
        }
        Inline next;
        if (template is not null || content is UIElement)
        {
            // Document mutations unload InlineUIContainer children. Only a
            // noninteractive spacer belongs to the document; original consumer
            // controls stay mounted in the sibling slot plane.
            var host = new ContentPresenter { Content = content, ContentTemplate = template, Opacity = 0, IsHitTestVisible = false };
            foreach (var (target, property) in new[] { (ContentPresenter.FontFamilyProperty, nameof(FontFamily)), (ContentPresenter.FontSizeProperty, nameof(FontSize)),
                (ContentPresenter.FontWeightProperty, nameof(FontWeight)), (ContentPresenter.FontStyleProperty, nameof(FontStyle)), (ContentPresenter.FontStretchProperty, nameof(FontStretch)),
                (ContentPresenter.ForegroundProperty, nameof(Foreground)), (ContentPresenter.CharacterSpacingProperty, nameof(CharacterSpacing)) })
                host.SetBinding(target, new Binding { Source = this, Path = new PropertyPath(property) });
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(host, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
            slots!.Children.Add(host);
            if (prefix) prefixHost = host; else suffixHost = host;
            var spacer = new Border { IsHitTestVisible = false };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(spacer, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
            next = new InlineUIContainer { Child = spacer };
        }
        else next = new Run { Text = Scalar(content) };
        if (prefix)
        {
            paragraph!.Inlines.Insert(0, next); prefixInline = next;
            renderedPrefix = content; renderedPrefixTemplate = template;
        }
        else
        {
            paragraph!.Inlines.Add(next); suffixInline = next;
            renderedSuffix = content; renderedSuffixTemplate = template;
        }
    }

    private static string Scalar(object? value) => value switch
    {
        null or bool => string.Empty,
        string text => text,
        byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => Convert.ToString(value, CultureInfo.InvariantCulture)!,
        UIElement => string.Empty,
        _ => throw new ArgumentException("Metric slots require scalar content, UIElement content or a DataTemplate.")
    };

    protected override AutomationPeer OnCreateAutomationPeer() => new MetricPeer(this);
    private sealed class MetricPeer(BleeckerMetric owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerMetric);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
        protected override string GetNameCore() => string.IsNullOrEmpty(base.GetNameCore()) ? owner.accessibleText : base.GetNameCore();
    }
}
