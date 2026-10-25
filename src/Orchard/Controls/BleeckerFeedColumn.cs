using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

/// <summary>A fixed-width feed surface with a retained header and independently scrolling caller content.</summary>
public sealed partial class BleeckerFeedColumn : ContentControl
{
    private Grid? root;
    private Border? surface, header, countHost, dot;
    private ScrollViewer? viewport;
    private XamlRoot? observedHost;
    private BleeckerContentText? titleText, countText, filterText, bodyText;
    private BleeckerCssShadow? shadow, glow;
    private readonly BleeckerFeedColumnGradient gradient = new();
    private readonly SolidColorBrush generatedBorder = new();
    private readonly List<(DependencyObject Owner, DependencyProperty Property, long Token)> observers = [];
    private readonly Dictionary<BleeckerContentText, (object? Value, DataTemplate? Template, DataTemplateSelector? Selector)> slots = [];
    public BleeckerFeedColumn()
    {
        DefaultStyleKey = typeof(BleeckerFeedColumn); IsTabStop = false; UseLayoutRounding = false;
        Loaded += (_, _) => { Observe(); Render(); Paint(); LayoutUpdated += UpdateViewportFocus; if (observedHost is not null) observedHost.Changed -= HostChanged; observedHost = XamlRoot; if (observedHost is not null) observedHost.Changed += HostChanged; };
        Unloaded += (_, _) => { Disconnect(); gradient.Clear(); LayoutUpdated -= UpdateViewportFocus; if (observedHost is not null) observedHost.Changed -= HostChanged; observedHost = null; };
        ActualThemeChanged += (_, _) => DispatcherQueue.TryEnqueue(() => { Observe(); Render(); Paint(); });
        SizeChanged += (_, _) => Paint();
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
        RegisterPropertyChangedCallback(StyleProperty, (_, _) => FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer());
        foreach (var property in new[] { ContentTemplateProperty, ContentTemplateSelectorProperty, FlowDirectionProperty, IsTextScaleFactorEnabledProperty, LanguageProperty }) RegisterPropertyChangedCallback(property, (_, _) => Render());
        foreach (var property in new[] { BackgroundProperty, BorderBrushProperty, BorderThicknessProperty, CornerRadiusProperty, PaddingProperty }) RegisterPropertyChangedCallback(property, (_, _) => Paint());
    }
    private void HostChanged(XamlRoot sender, XamlRootChangedEventArgs args) => Paint();
    private static DependencyProperty Property(string name, Type type, object? value) => DependencyProperty.Register(name, type, typeof(BleeckerFeedColumn), new PropertyMetadata(value, (owner, _) => ((BleeckerFeedColumn)owner).Render()));
    public static readonly DependencyProperty TitleProperty = Property(nameof(Title), typeof(string), "");
    public static readonly DependencyProperty CountProperty = Property(nameof(Count), typeof(object), null);
    public static readonly DependencyProperty FilterProperty = Property(nameof(Filter), typeof(object), null);
    public static readonly DependencyProperty FilterTemplateProperty = Property(nameof(FilterTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty FilterTemplateSelectorProperty = Property(nameof(FilterTemplateSelector), typeof(DataTemplateSelector), null);
    public static readonly DependencyProperty AccentProperty = DependencyProperty.Register(nameof(Accent), typeof(Brush), typeof(BleeckerFeedColumn), new PropertyMetadata(null, (owner, _) => { var column = (BleeckerFeedColumn)owner; column.Observe(); column.Paint(); }));
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public double? Count { get { var value = GetValue(CountProperty); return value is null ? null : BleeckerReactContent.Numeric(value) ? Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture) : throw new ArgumentException("FeedColumn count requires numeric data."); } set => SetValue(CountProperty, value); }
    public object? Filter { get => GetValue(FilterProperty); set => SetValue(FilterProperty, value); }
    public DataTemplate? FilterTemplate { get => (DataTemplate?)GetValue(FilterTemplateProperty); set => SetValue(FilterTemplateProperty, value); }
    public DataTemplateSelector? FilterTemplateSelector { get => (DataTemplateSelector?)GetValue(FilterTemplateSelectorProperty); set => SetValue(FilterTemplateSelectorProperty, value); }
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set { if (value is not null and not SolidColorBrush) throw new ArgumentException("FeedColumn accent requires a solid color brush."); SetValue(AccentProperty, value); } }
    /// <summary>The current native scrolling viewport; null before template application and after template removal.</summary>
    public ScrollViewer? Viewport => viewport;
    protected override void OnContentChanged(object oldContent, object newContent) { if (bodyText is not null) { slots.Remove(bodyText); bodyText.Release(); } base.OnContentChanged(oldContent, newContent); Render(); }
    protected override void OnApplyTemplate()
    {
        Detach(); if (Template is null) return; base.OnApplyTemplate(); root = GetTemplateChild("PART_Root") as Grid;
        if (root is null) throw new InvalidOperationException("FeedColumn requires a PART_Root Grid.");
        T Part<T>(string name) where T : class => GetTemplateChild(name) as T ?? throw new InvalidOperationException($"FeedColumn requires {name} as {typeof(T).Name}.");
        surface = Part<Border>("PART_Surface");
        titleText = new("FeedColumnTitle", "BleeckerPrimarySemiboldFontFamily", 11, 13.1875, "BleeckerSeaBrush"); titleText.Text.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold; titleText.Text.CharacterSpacing = 120;
        countText = new("FeedColumnCountText", "BleeckerPrimarySemiboldFontFamily", 11, 16.5, "BleeckerTextPrimaryBrush"); countText.Text.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold; countText.Opacity = .85; Typography.SetNumeralAlignment(countText.Text, FontNumeralAlignment.Tabular);
        countHost = Create<Border>("<Border x:Name='FeedColumnCount' CornerRadius='999' Padding='10,4' BorderThickness='1' BackgroundSizing='OuterBorderEdge' Background='{StaticResource FeedColumnCountBackground}' BorderBrush='{StaticResource FeedColumnCountBorder}'/>"); countHost.Child = countText;
        filterText = new("FeedColumnFilter", "BleeckerPrimaryFontFamily", 16, 24, "BleeckerTextPrimaryBrush", stretchContent: true);
        bodyText = new("FeedColumnBody", "BleeckerPrimaryFontFamily", 16, 24, "BleeckerTextPrimaryBrush", stretchContent: true) { VerticalAlignment = VerticalAlignment.Top };
        dot = new Border { Name = "FeedColumnDot", Width = 8, Height = 8, CornerRadius = new CornerRadius(4), UseLayoutRounding = false, IsHitTestVisible = false }; AutomationProperties.SetAccessibilityView(dot, AccessibilityView.Raw);
        var dotHost = new Grid { Width = 8, Height = 8, UseLayoutRounding = false }; var glowHost = new Grid { UseLayoutRounding = false, IsHitTestVisible = false }; dotHost.Children.Add(glowHost); dotHost.Children.Add(dot);
        var mask = new Microsoft.UI.Xaml.Shapes.Path { Visibility = Visibility.Collapsed, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) }; var exterior = new Microsoft.UI.Xaml.Shapes.Path { Visibility = Visibility.Collapsed, Fill = new SolidColorBrush(Microsoft.UI.Colors.White) }; root.Children.Add(mask); root.Children.Add(exterior); glow = new(this, dot, glowHost, mask, exterior);
        var row = new HeaderRow(dotHost, titleText, countHost) { Name = "FeedColumnRow", UseLayoutRounding = false };
        header = Create<Border>("<Border x:Name='FeedColumnHeader' Padding='16' BorderThickness='0,0,0,1' BorderBrush='{StaticResource FeedColumnHeaderBorder}' BackgroundSizing='OuterBorderEdge'/>"); header.Child = new HeaderStack(row, filterText) { UseLayoutRounding = false };
        viewport = Create<ScrollViewer>("<ScrollViewer x:Name='FeedColumnViewport' IsTabStop='False' HorizontalScrollMode='Disabled' HorizontalScrollBarVisibility='Disabled' VerticalScrollMode='Enabled' VerticalScrollBarVisibility='Auto' ZoomMode='Disabled' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Stretch'/>");
        viewport.Content = new Border { Name = "FeedColumnBodyPadding", Child = bodyText, Padding = Padding, UseLayoutRounding = false };
        var content = new SurfacePanel(header, viewport) { Name = "FeedColumnSurface", UseLayoutRounding = false, FlowDirection = FlowDirection }; content.SizeChanged += (_, _) => ClipContent(); surface.Child = content;
        shadow = new(this, surface, Part<Grid>("PART_ShadowHost"), Part<Microsoft.UI.Xaml.Shapes.Path>("PART_ShadowMask"), Part<Microsoft.UI.Xaml.Shapes.Path>("PART_ShadowExterior")); shadow.Set(new(54, 22, -38, .95f, 0, 0, 0));
        Observe(); Render(); Paint();
    }
    private static T Create<T>(string markup) => (T)Microsoft.UI.Xaml.Markup.XamlReader.Load(markup.Insert(markup.IndexOf(' '), " xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' UseLayoutRounding='False'"));
    private void SetSlot(BleeckerContentText slot, object? value, DataTemplate? template, DataTemplateSelector? selector)
    {
        if (slots.TryGetValue(slot, out var previous) && (value is string or bool || BleeckerReactContent.Numeric(value) ? Equals(previous.Value, value) : ReferenceEquals(previous.Value, value)) && ReferenceEquals(previous.Template, template) && ReferenceEquals(previous.Selector, selector)) return;
        slot.Set(value, template, selector: selector); slots[slot] = (value, template, selector);
    }
    private void Render()
    {
        var count = Count; if (titleText is null) return;
        SetSlot(titleText, BleeckerTextCasing.Uppercase(Title ?? ""), null, null); SetSlot(countText!, count is { } number ? BleeckerNumberText.Format(number) : null, null, null); countHost!.Visibility = count.HasValue ? Visibility.Visible : Visibility.Collapsed;
        SetSlot(filterText!, Filter, FilterTemplate, FilterTemplateSelector); SetSlot(bodyText!, Content, ContentTemplate, ContentTemplateSelector);
        surface!.Child.SetValue(FrameworkElement.FlowDirectionProperty, FlowDirection);
        foreach (var text in new[] { titleText.Text, countText!.Text, filterText!.Text, bodyText!.Text }) { text.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled; text.Language = Language; }
        InvalidateMeasure(); Paint(); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private void Observe()
    {
        Disconnect(); if (!IsLoaded || Accent is not SolidColorBrush solid) return;
        foreach (var property in new[] { SolidColorBrush.ColorProperty, Brush.OpacityProperty }) observers.Add((solid, property, solid.RegisterPropertyChangedCallback(property, (_, _) => Paint())));
    }
    private void Disconnect() { foreach (var (owner, property, token) in observers) owner.UnregisterPropertyChangedCallback(property, token); observers.Clear(); }
    private void Paint()
    {
        if (surface is null || Accent is null) return; if (Accent is not SolidColorBrush accent) throw new ArgumentException("FeedColumn accent requires a solid color brush.");
        var rgba = (accent.Color.R, accent.Color.G, accent.Color.B, accent.Color.A / 255d * accent.Opacity); var border = BleeckerColorMix.EncodePremultiplied(BleeckerFeedColumnModel.Border(rgba));
        generatedBorder.Color = Windows.UI.Color.FromArgb(255, border.R, border.G, border.B); generatedBorder.Opacity = border.Alpha;
        surface.BorderBrush = BorderBrush ?? generatedBorder; surface.BorderThickness = BorderThickness; surface.CornerRadius = CornerRadius;
        if (viewport?.Content is Border bodyPadding) bodyPadding.Padding = Padding;
        if (viewport is not null) viewport.MinHeight = Padding.Top + Padding.Bottom;
        titleText!.Text.Foreground = Accent; dot!.Background = Accent; glow?.Set(new(14, 0, 0, (float)rgba.Item4, accent.Color.R, accent.Color.G, accent.Color.B));
        if (Background is null && IsLoaded && XamlRoot is { } host) gradient.Update(rgba, ActualWidth, ActualHeight, BorderThickness, host.RasterizationScale);
        surface.Background = Background ?? gradient.Brush;
        ClipContent();
    }
    private void ClipContent() { if (surface?.Child is FrameworkElement content) content.Clip = new RectangleGeometry { Rect = new Rect(0, 0, content.ActualWidth, content.ActualHeight) }; }
    private void UpdateViewportFocus(object? sender, object args)
    {
        if (viewport is null || bodyText is not { IsLoaded: true }) return;
        var focusable = viewport.ScrollableHeight > 0 && viewport.ViewportHeight > 0 && FocusManager.FindFirstFocusableElement(bodyText) is null; if (viewport.IsTabStop != focusable) viewport.IsTabStop = focusable;
    }
    private void Detach()
    {
        Disconnect(); shadow?.Dispose(); glow?.Dispose(); shadow = glow = null; gradient.Clear(); foreach (var slot in slots.Keys) slot.Release(); slots.Clear();
        if (viewport is not null) viewport.Content = null; if (surface is not null) surface.Child = null; root = null; surface = header = countHost = dot = null; viewport = null; titleText = countText = filterText = bodyText = null;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new FeedPeer(this);
    private sealed class FeedPeer(BleeckerFeedColumn owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerFeedColumn);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetNameCore()
        {
            if (owner.ReadLocalValue(AutomationProperties.NameProperty) != DependencyProperty.UnsetValue || (string)owner.GetValue(AutomationProperties.NameProperty) != "" || AutomationProperties.GetLabeledBy(owner) is not null) return base.GetNameCore();
            for (var style = owner.Style; style is not null; style = style.BasedOn) if (style.Setters.OfType<Setter>().Any(setter => setter.Property == AutomationProperties.NameProperty)) return base.GetNameCore();
            return owner.Title ?? "";
        }
    }
    private sealed partial class HeaderRow(FrameworkElement dot, BleeckerContentText title, FrameworkElement count) : Panel
    {
        private double titleWidth, rowHeight, countWidth;
        private void Ensure() { if (Children.Count == 0) { Children.Add(dot); Children.Add(title); Children.Add(count); } }
        protected override Size MeasureOverride(Size available)
        {
            Ensure(); count.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); dot.Measure(new Size(8, 8)); title.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); countWidth = count.Visibility == Visibility.Visible ? count.DesiredSize.Width : 0;
            var width = double.IsFinite(available.Width) ? available.Width : title.NaturalWidth + 18 + (count.Visibility == Visibility.Visible ? countWidth + 10 : 0);
            titleWidth = BleeckerFeedColumnModel.TitleWidth(width, title.MinimumWidth, countWidth, count.Visibility == Visibility.Visible); title.Measure(new Size(titleWidth, double.PositiveInfinity)); rowHeight = BleeckerFeedColumnModel.RowHeight(title.DesiredSize.Height, count.DesiredSize.Height); return new(width, rowHeight);
        }
        protected override Size ArrangeOverride(Size final) { dot.Arrange(new Rect(0, (rowHeight - 8) / 2, 8, 8)); title.Arrange(new Rect(18, (rowHeight - title.DesiredSize.Height) / 2, titleWidth, title.DesiredSize.Height)); if (count.Visibility == Visibility.Visible) count.Arrange(new Rect(28 + titleWidth, (rowHeight - count.DesiredSize.Height) / 2, countWidth, count.DesiredSize.Height)); return final; }
    }
    private sealed partial class HeaderStack(FrameworkElement row, FrameworkElement filter) : Panel
    {
        protected override Size MeasureOverride(Size available) { if (Children.Count == 0) { Children.Add(row); Children.Add(filter); } row.Measure(new Size(available.Width, double.PositiveInfinity)); filter.Measure(new Size(available.Width, double.PositiveInfinity)); return new(double.IsFinite(available.Width) ? available.Width : Math.Max(row.DesiredSize.Width, filter.DesiredSize.Width), row.DesiredSize.Height + 16 + filter.DesiredSize.Height); }
        protected override Size ArrangeOverride(Size final) { row.Arrange(new Rect(0, 0, final.Width, row.DesiredSize.Height)); filter.Arrange(new Rect(0, row.DesiredSize.Height + 16, final.Width, filter.DesiredSize.Height)); return final; }
    }
    private sealed partial class SurfacePanel(FrameworkElement header, FrameworkElement feed) : Panel
    {
        protected override Size MeasureOverride(Size available)
        {
            if (Children.Count == 0) { Children.Add(header); Children.Add(feed); }
            header.Measure(new Size(available.Width, double.PositiveInfinity)); feed.Measure(new Size(available.Width, double.IsFinite(available.Height) ? Math.Max(feed.MinHeight, available.Height - header.DesiredSize.Height) : double.PositiveInfinity));
            var width = double.IsFinite(available.Width) ? available.Width : Math.Max(header.DesiredSize.Width, feed.DesiredSize.Width);
            if (!double.IsFinite(available.Width)) { header.Measure(new Size(width, double.PositiveInfinity)); feed.Measure(new Size(width, double.IsFinite(available.Height) ? Math.Max(feed.MinHeight, available.Height - header.DesiredSize.Height) : double.PositiveInfinity)); }
            return new(width, double.IsFinite(available.Height) ? available.Height : header.DesiredSize.Height + feed.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size final) { header.Arrange(new Rect(0, 0, final.Width, header.DesiredSize.Height)); feed.Arrange(new Rect(0, header.DesiredSize.Height, final.Width, Math.Max(feed.MinHeight, final.Height - header.DesiredSize.Height))); return final; }
    }
}
