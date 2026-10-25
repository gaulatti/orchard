using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

// A source text slot keeps the original consumer value and shapes only its
// presentation. The required mark is a separate decorative inline box.
internal sealed partial class BleeckerContentText : Panel
{
    private readonly TextBlock text, probe, marker;
    private readonly ContentPresenter rich = new() { UseLayoutRounding = false };
    private readonly BleeckerReferenceText.IntrinsicWidth intrinsic = new(inlineWidths: true), markerWidth = new();
    private readonly UISettings settings = new();
    private readonly TextBlock natural = new() { Text = "Mg", IsTextScaleFactorEnabled = false };
    private readonly TranslateTransform textShift = new(), markerShift = new();
    private readonly TranslateTransform richShift = new();
    private readonly List<(TextBlock Probe, BleeckerReferenceText.IntrinsicWidth Width)> richWidths = [];
    private bool referenceRich;
    private double baseline;
    private readonly BleeckerReferenceText.Presentation presentation = new(), probePresentation = new();
    private object? value;
    private DataTemplate? template;
    private bool required, plain;
    private string normalized = "";
    private IReadOnlyList<string> lines = [];
    private Func<int, int, double>? spans;
    private double lineHeight, lastWidth, markWidth;
    private FrameworkElement? observedFlowRoot;
    private readonly List<(DependencyProperty Property, long Token)> flowRootTokens = [];
    private FrameworkElement? stackingRoot;
    private long stackingToken;
    internal event Action? FlowMinimumChanged;
    internal event Action? StackingOrderChanged;
    internal int ContentZIndex => stackingRoot is null ? 0 : Canvas.GetZIndex(stackingRoot);
    private void ObserveStackingRoot()
    {
        var next = IsLoaded && !plain && StackingOrderChanged is not null && VisualTreeHelper.GetChildrenCount(rich) > 0 ? VisualTreeHelper.GetChild(rich, 0) as FrameworkElement : null;
        if (ReferenceEquals(next, stackingRoot)) return;
        DisconnectStackingRoot(); stackingRoot = next;
        if (next is not null) stackingToken = next.RegisterPropertyChangedCallback(Canvas.ZIndexProperty, (_, _) => StackingOrderChanged?.Invoke());
        StackingOrderChanged?.Invoke();
    }
    private void DisconnectStackingRoot()
    {
        var previous = stackingRoot;
        if (previous is not null) previous.UnregisterPropertyChangedCallback(Canvas.ZIndexProperty, stackingToken);
        stackingRoot = null;
    }
    internal TextBlock Text => text;
    internal void SetForeground(Brush brush) { text.Foreground = brush; rich.Foreground = brush; }
    internal TextBlock Marker => marker;
    internal double NaturalWidth { get; private set; }
    internal double NaturalHeight { get; private set; }
    internal double MinimumWidth { get; private set; }
    // A flow wrapper may scroll an explicitly sized native root without
    // treating the unwrapped natural width of a wrapping panel as its minimum.
    internal double FlowMinimumWidth
    {
        get
        {
            if (plain) { ObserveFlowRoot(null); return MinimumWidth; }
            if (VisualTreeHelper.GetChildrenCount(rich) == 0 || VisualTreeHelper.GetChild(rich, 0) is not FrameworkElement root) { ObserveFlowRoot(null); return 0; }
            ObserveFlowRoot(root);
            return Math.Max(root.MinWidth, Math.Min(root.MaxWidth, double.IsFinite(root.Width) ? root.Width : 0)) + root.Margin.Left + root.Margin.Right;
        }
    }
    // A source block with an explicit width stays at its leading edge even
    // when narrower than its viewport. Keep this separate from a min-width:
    // an auto-width root still fills the available block width.
    internal double? FlowWidth
    {
        get
        {
            if (plain || VisualTreeHelper.GetChildrenCount(rich) == 0 || VisualTreeHelper.GetChild(rich, 0) is not FrameworkElement root) return null;
            ObserveFlowRoot(root);
            return double.IsFinite(root.Width) ? Math.Max(root.MinWidth, Math.Min(root.MaxWidth, root.Width)) + root.Margin.Left + root.Margin.Right : null;
        }
    }
    private void ObserveFlowRoot(FrameworkElement? root)
    {
        if (!IsLoaded || FlowMinimumChanged is null) root = null;
        if (ReferenceEquals(observedFlowRoot, root)) return;
        DisconnectFlowRoot(); observedFlowRoot = root;
        if (root is null) return;
        foreach (var property in new[] { WidthProperty, MinWidthProperty, MaxWidthProperty, MarginProperty })
            flowRootTokens.Add((property, root.RegisterPropertyChangedCallback(property, (_, _) => FlowMinimumChanged?.Invoke())));
    }
    private void DisconnectFlowRoot()
    {
        if (observedFlowRoot is not null) foreach (var (property, token) in flowRootTokens) observedFlowRoot.UnregisterPropertyChangedCallback(property, token);
        flowRootTokens.Clear(); observedFlowRoot = null;
    }
    internal bool NoWrap { get; set; }
    internal bool BalanceWrap { get; set; }
    internal bool BreakWords { get; set; }
    internal bool ConstrainLayoutWidth { get; set; }
    internal bool DirectionalMaxContent { get; set; }
    internal bool BlockMaxContent { get; set; }
    internal double InlineContentWidth => plain ? lines.Select(line => required && line.EndsWith('*') ? MeasureWidth(line[..^1]) + 4 + markWidth : MeasureWidth(line)).DefaultIfEmpty(0).Max() : NaturalWidth;

    internal BleeckerContentText(string name, string family, double size, double height, string brush, bool medium = false, bool stretchContent = false)
    {
        Name = name; UseLayoutRounding = false;
        Loaded += (_, _) => { FlowMinimumChanged?.Invoke(); ObserveStackingRoot(); };
        Unloaded += (_, _) => { DisconnectFlowRoot(); DisconnectStackingRoot(); };
        if (stretchContent) rich.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        TextBlock Create(string foreground) => (TextBlock)XamlReader.Load(
            $"<TextBlock xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' FontFamily='{{StaticResource {family}}}' FontSize='{size.ToString(System.Globalization.CultureInfo.InvariantCulture)}' FontWeight='{(medium ? "Medium" : "Normal")}' LineHeight='{height.ToString(System.Globalization.CultureInfo.InvariantCulture)}' LineStackingStrategy='BlockLineHeight' TextWrapping='NoWrap' UseLayoutRounding='False' Foreground='{{ThemeResource {foreground}}}'/>");
        text = Create(brush); probe = Create(brush); marker = Create("BleeckerTerracottaBrush"); marker.Text = "*";
        text.Name = name + "Text"; marker.Name = name + "Required";
        text.RenderTransform = textShift; marker.RenderTransform = markerShift;
        rich.RenderTransform = richShift;
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(text, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(marker, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
        Children.Add(text); Children.Add(rich); Children.Add(marker);
    }

    internal void Set(object? content, DataTemplate? contentTemplate, bool mark = false, DataTemplateSelector? selector = null)
    {
        DisconnectFlowRoot();
        DisconnectStackingRoot();
        if (contentTemplate is null && selector is null && content is not null && content is not UIElement && content is not string && content is not bool && !BleeckerReactContent.Numeric(content))
            throw new ArgumentException("Model content requires a DataTemplate or DataTemplateSelector.");
        value = content; template = contentTemplate; required = mark;
        // A native template owns presentation of its original model, including
        // boxed numeric/Boolean values. Only untemplated scalar text uses the
        // source component's React formatting rules.
        var display = contentTemplate is not null || selector is not null ? content : BleeckerReactContent.Display(content);
        plain = contentTemplate is null && selector is null && display is not UIElement;
        normalized = plain ? BleeckerInlineText.Normalize(display as string ?? "") : "";
        var original = plain ? null : display;
        if (!ReferenceEquals(rich.Content, original) || !ReferenceEquals(rich.ContentTemplate, contentTemplate) || !ReferenceEquals(rich.ContentTemplateSelector, selector))
        {
            // Selecting before assigning the original model can cache a
            // null-item template even after Content updates. Detach the old
            // renderer, supply current data, then select its presentation.
            // Preserve an unchanged presenter so unrelated property updates
            // keep caller controls, bindings, input and selection intact.
            rich.ContentTemplate = null; rich.ContentTemplateSelector = null;
            rich.Content = original; rich.ContentTemplate = contentTemplate; rich.ContentTemplateSelector = selector;
        }
        rich.Visibility = plain ? Visibility.Collapsed : Visibility.Visible;
        text.Visibility = plain ? Visibility.Visible : Visibility.Collapsed;
        marker.Visibility = mark ? Visibility.Visible : Visibility.Collapsed;
        ObserveStackingRoot();
        StackingOrderChanged?.Invoke();
        InvalidateMeasure();
    }
    internal void Release() { DisconnectFlowRoot(); DisconnectStackingRoot(); rich.ContentTemplate = null; rich.ContentTemplateSelector = null; rich.Content = null; richWidths.Clear(); }
    internal string AccessibleName()
    {
        if (plain) return normalized;
        if (BleeckerReactContent.Display(value) is FrameworkElement element)
            return FrameworkElementAutomationPeer.CreatePeerForElement(element)?.GetName() ?? "";
        return string.Join(' ', Descendants(rich).Select(element => FrameworkElementAutomationPeer.CreatePeerForElement(element)?.GetName())
            .Where(name => !string.IsNullOrEmpty(name)).Distinct());
    }
    private static IEnumerable<FrameworkElement> Descendants(DependencyObject owner)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++)
        {
            var child = VisualTreeHelper.GetChild(owner, i); if (child is FrameworkElement element) yield return element;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
    private double MeasureWidth(string content)
    {
        probePresentation.Update(probe, content); var directionalWidth = intrinsic.Measure(probe, content);
        // One inline span rounds the combined shaping advance. Rounding each
        // fallback/script run separately adds a layout unit to mixed text.
        // A nowrap RTL inline box allocates its visual bidi fragments before
        // summing them. Preserve that allocation instead of rounding the
        // combined advance again (notably punctuation around an LTR time).
        return NoWrap && text.FlowDirection == FlowDirection.RightToLeft ? directionalWidth : intrinsic.InlineWidth;
    }
    protected override Size MeasureOverride(Size available)
    {
        probe.FlowDirection = text.FlowDirection; probe.Language = text.Language; probe.IsTextScaleFactorEnabled = text.IsTextScaleFactorEnabled;
        probe.FontFamily = text.FontFamily; probe.FontSize = text.FontSize; probe.FontWeight = text.FontWeight;
        probe.FontStyle = text.FontStyle; probe.FontStretch = text.FontStretch; probe.CharacterSpacing = text.CharacterSpacing;
        Typography.SetNumeralAlignment(probe, Typography.GetNumeralAlignment(text));
        var scale = text.IsTextScaleFactorEnabled ? settings.TextScaleFactor : 1; lineHeight = text.LineHeight * scale;
        marker.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); markWidth = markerWidth.Measure(marker, "*");
        marker.Width = markWidth;
        marker.Measure(new Size(markWidth, double.PositiveInfinity));
        if (!plain)
        {
            rich.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            referenceRich = template is null && rich.ContentTemplateSelector is null && value is TextBlock block && block.TextWrapping == TextWrapping.NoWrap && block.LineHeight > 0 &&
                double.IsNaN(block.Width) && double.IsNaN(block.Height) && block.MinWidth == 0 && double.IsPositiveInfinity(block.MaxWidth) &&
                block.Margin == new Thickness(0) && block.Padding == new Thickness(0) && block.TextTrimming == TextTrimming.None &&
                block.FontFamily.Source.Contains("/Orchard/Assets/Fonts/", StringComparison.Ordinal) && block.Inlines.All(item => item is Run);
            var richWidth = referenceRich ? MeasureRichText((TextBlock)value!) : rich.DesiredSize.Width;
            NaturalWidth = richWidth + (required ? markWidth + 4 : 0);
            NaturalHeight = Math.Max(rich.DesiredSize.Height, required ? lineHeight : 0);
            MinimumWidth = NaturalWidth; rich.Measure(new Size(Math.Max(0, available.Width - (required ? markWidth + 4 : 0)), available.Height));
            ObserveStackingRoot();
            lastWidth = referenceRich ? richWidth : rich.DesiredSize.Width;
            if (referenceRich) MeasureBaseline((TextBlock)value!, ((TextBlock)value!).LineHeight * (((TextBlock)value!).IsTextScaleFactorEnabled ? settings.TextScaleFactor : 1));
            else richShift.Y = markerShift.Y = 0;
            return new(Math.Min(NaturalWidth, available.Width), Math.Max(rich.DesiredSize.Height, required ? lineHeight : 0));
        }
        probePresentation.Update(probe, normalized); var directionalWidth = intrinsic.Measure(probe, normalized);
        spans = intrinsic.CaptureInlineSpanMeasure(); var labelLength = BleeckerInlineText.Paint(normalized).Length;
        var sourceText = normalized + (required ? "*" : "");
        double Span(int start, int end) => end <= labelLength ? spans(start, end) : (start < labelLength ? spans(start, labelLength) : 0) + 4 + markWidth;
        double Measure(string content) => required && content.EndsWith('*') ? MeasureWidth(content[..^1]) + 4 + markWidth : MeasureWidth(content);
        var model = new BleeckerInlineText(sourceText, BleeckerInlineBreaks.Analyze(sourceText, FlowDirection == FlowDirection.RightToLeft, Language), Measure, Span);
        NaturalWidth = BlockMaxContent || DirectionalMaxContent && FlowDirection == FlowDirection.RightToLeft ? directionalWidth : model.MaximumWidth; MinimumWidth = model.MinimumWidth;
        var width = Math.Min(NaturalWidth, NoWrap || BreakWords ? Math.Max(0, available.Width) : Math.Max(MinimumWidth, available.Width));
        lines = NoWrap ? sourceText.Length == 0 ? [] : [sourceText] : BreakWords ? model.WrapBreakWords(width) : model.Wrap(width);
        if (BalanceWrap && !NoWrap && !BreakWords && lines.Count is > 1 and <= 6)
        {
            // Chromium balances a short heading without increasing its line
            // count. Search in its 1/64px layout units; the containing line box
            // retains the original block width and centers the painted lines.
            var count = lines.Count; var low = Math.Max(1, (long)Math.Ceiling(MinimumWidth * 64)); var high = Math.Max(low, (long)Math.Floor(width * 64));
            while (low < high) { var middle = low + (high - low) / 2; if (model.Wrap(middle / 64d).Count <= count) high = middle; else low = middle + 1; }
            lines = model.Wrap(low / 64d);
        }
        var painted = lines.ToArray(); if (required && painted.Length > 0) painted[^1] = painted[^1][..^1];
        lastWidth = painted.Length > 0 ? MeasureWidth(painted[^1]) : 0;
        presentation.Update(text, string.Join('\n', painted)); text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        MeasureBaseline(text, lineHeight);
        if (lines.Count == 0) baseline = 0;
        NaturalHeight = lines.Count * lineHeight;
        return new(ConstrainLayoutWidth ? Math.Min(width, available.Width) : width, NaturalHeight);
    }
    private void MeasureBaseline(TextBlock subject, double height)
    {
        var scale = subject.IsTextScaleFactorEnabled ? settings.TextScaleFactor : 1;
        natural.FontFamily = subject.FontFamily; natural.FontSize = subject.FontSize * scale;
        natural.FontWeight = subject.FontWeight; natural.FontStyle = subject.FontStyle; natural.FontStretch = subject.FontStretch;
        natural.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var ascent = Math.Round(natural.BaselineOffset, MidpointRounding.AwayFromZero);
        var descent = Math.Round(natural.DesiredSize.Height - natural.BaselineOffset, MidpointRounding.AwayFromZero);
        baseline = ascent + Math.Floor((height - ascent - descent) / 2);
    }
    private double MeasureRichText(TextBlock subject)
    {
        var runs = subject.Inlines.OfType<Run>().ToArray();
        while (richWidths.Count < runs.Length) richWidths.Add((new TextBlock(), new(joinWhitespace: true)));
        var width = 0d;
        for (var index = 0; index < runs.Length; index++)
        {
            var run = runs[index]; var (probe, measure) = richWidths[index];
            probe.FontFamily = run.FontFamily; probe.FontSize = run.FontSize; probe.FontWeight = run.FontWeight;
            probe.FontStyle = run.FontStyle; probe.FontStretch = run.FontStretch; probe.CharacterSpacing = run.CharacterSpacing;
            probe.FlowDirection = subject.FlowDirection; probe.Language = run.Language; probe.IsTextScaleFactorEnabled = subject.IsTextScaleFactorEnabled;
            measure.Measure(probe, run.Text); width += measure.WhitespaceJoinedWidth;
        }
        return width;
    }
    protected override Size ArrangeOverride(Size final)
    {
        if (plain) text.Arrange(new Rect(0, 0, Math.Max(ConstrainLayoutWidth ? MinimumWidth : 0, final.Width), lines.Count * lineHeight));
        else rich.Arrange(new Rect(0, 0, Math.Max(0, final.Width - (required ? markWidth + 4 : 0)), final.Height));
        if (required) marker.Arrange(new Rect(lastWidth + 4, plain ? Math.Max(0, lines.Count - 1) * lineHeight : 0, markWidth, lineHeight));
        if (plain)
        {
            textShift.Y = baseline - text.BaselineOffset; markerShift.Y = baseline - marker.BaselineOffset;
        }
        else if (referenceRich)
        {
            richShift.Y = baseline - ((TextBlock)value!).BaselineOffset;
            markerShift.Y = baseline - marker.BaselineOffset;
        }
        return final;
    }
    internal double FirstBaseline => plain || referenceRich ? baseline : FirstTextBaseline(rich);
    protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);
    private sealed class Peer(BleeckerContentText owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => owner.plain ? AutomationControlType.Text : AutomationControlType.Group;
        protected override string GetNameCore() => owner.AccessibleName();
        protected override IList<AutomationPeer> GetChildrenCore() => owner.plain ? [] : base.GetChildrenCore();
    }
    internal static double FirstTextBaseline(FrameworkElement owner)
    {
        if (owner is TextBlock text) return text.BaselineOffset;
        bool Visible(FrameworkElement element)
        {
            for (DependencyObject? node = element; node is FrameworkElement current && !ReferenceEquals(node, owner); node = VisualTreeHelper.GetParent(node))
                if (current.Visibility != Visibility.Visible) return false;
            return true;
        }
        var first = Descendants(owner).OfType<TextBlock>().FirstOrDefault(text => Visible(text) && text.Text.Length > 0);
        return first is null ? owner.DesiredSize.Height : first.TransformToVisual(owner).TransformPoint(new()).Y + first.BaselineOffset;
    }
}
