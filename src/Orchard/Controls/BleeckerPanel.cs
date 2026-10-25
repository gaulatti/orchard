using System.Diagnostics;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

public enum BleeckerPanelVariant { Monitor }

/// <summary>A titled, independently scrolling surface for caller-owned content and controls.</summary>
public sealed partial class BleeckerPanel : ContentControl
{
    private Grid? root, clipRoot;
    private Border? left, header, filterHost, toolbarHost, countHost;
    private TextBlock? accentSource, handleIdleSource, handleHoverSource;
    private BleeckerContentText? titleText, countText, filterText, toolbarText, dragText, bodyText;
    private HandleHost? handle;
    private HeaderRow? row;
    private SurfacePanel? surface;
    private ScrollViewer? feed;
    private Ellipse? dot;
    private readonly SolidColorBrush leftPaint = new(), rightPaint = new(), handlePaint = new();
    private readonly BleeckerCssTransition<BleeckerCssColor> leftMotion = new(BleeckerCssColor.Mix, BleeckerCssEasing.Default);
    private readonly BleeckerCssTransition<BleeckerCssColor> rightMotion = new(BleeckerCssColor.Mix, BleeckerCssEasing.Default);
    private readonly BleeckerCssTransition<BleeckerCssColor> handleMotion = new(BleeckerCssColor.Mix);
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly UISettings settings = new();
    private readonly List<(DependencyObject Owner, DependencyProperty Property, long Token)> observers = [];
    private bool painting, running;
    private readonly Dictionary<BleeckerContentText, (object? Value, DataTemplate? Template, DataTemplateSelector? Selector)> renderedSlots = [];

    public BleeckerPanel()
    {
        DefaultStyleKey = typeof(BleeckerPanel); IsTabStop = false; UseLayoutRounding = false;
        Loaded += (_, _) => { LayoutUpdated += UpdateViewportFocus; ObserveBrushes(); Render(); Paint(false); };
        Unloaded += (_, _) => { LayoutUpdated -= UpdateViewportFocus; DisconnectBrushes(); Stop(); };
        ActualThemeChanged += (_, _) => DispatcherQueue.TryEnqueue(() => { ObserveBrushes(); Paint(true); });
        SizeChanged += (_, _) => UpdateClip();
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
        foreach (var property in new[] { ContentTemplateProperty, ContentTemplateSelectorProperty, FlowDirectionProperty, IsTextScaleFactorEnabledProperty, LanguageProperty })
            RegisterPropertyChangedCallback(property, (_, _) => Render());
        foreach (var property in new[] { BorderBrushProperty, AccentProperty })
            RegisterPropertyChangedCallback(property, (_, _) => { ObserveBrushes(); Paint(true); });
        RegisterPropertyChangedCallback(BorderThicknessProperty, (_, _) => { if (left is not null) left.Width = BorderThickness.Left; });
    }
    private static DependencyProperty Property(string name, Type type, object? value) => DependencyProperty.Register(name, type, typeof(BleeckerPanel),
        new PropertyMetadata(value, (owner, _) => ((BleeckerPanel)owner).Render()));
    public static readonly DependencyProperty TitleProperty = Property(nameof(Title), typeof(string), "");
    public static readonly DependencyProperty CountProperty = Property(nameof(Count), typeof(object), null);
    public static readonly DependencyProperty AccentProperty = DependencyProperty.Register(nameof(Accent), typeof(Brush), typeof(BleeckerPanel), new PropertyMetadata(null));
    public static readonly DependencyProperty FilterProperty = Property(nameof(Filter), typeof(object), null);
    public static readonly DependencyProperty FilterTemplateProperty = Property(nameof(FilterTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty FilterTemplateSelectorProperty = Property(nameof(FilterTemplateSelector), typeof(DataTemplateSelector), null);
    public static readonly DependencyProperty ToolbarProperty = Property(nameof(Toolbar), typeof(object), null);
    public static readonly DependencyProperty ToolbarTemplateProperty = Property(nameof(ToolbarTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty ToolbarTemplateSelectorProperty = Property(nameof(ToolbarTemplateSelector), typeof(DataTemplateSelector), null);
    public static readonly DependencyProperty DragHandleProperty = Property(nameof(DragHandle), typeof(object), null);
    public static readonly DependencyProperty DragHandleTemplateProperty = Property(nameof(DragHandleTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty DragHandleTemplateSelectorProperty = Property(nameof(DragHandleTemplateSelector), typeof(DataTemplateSelector), null);
    public static readonly DependencyProperty GrowProperty = DependencyProperty.Register(nameof(Grow), typeof(bool), typeof(BleeckerPanel), new PropertyMetadata(false));
    public static readonly DependencyProperty IsDraggingProperty = DependencyProperty.Register(nameof(IsDragging), typeof(bool), typeof(BleeckerPanel), new PropertyMetadata(false));
    public static readonly DependencyProperty VariantProperty = Property(nameof(Variant), typeof(BleeckerPanelVariant), BleeckerPanelVariant.Monitor);
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public double? Count
    {
        get { var value = GetValue(CountProperty); return value is null ? null : BleeckerReactContent.Numeric(value) ? Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture) : throw new ArgumentException("Panel count requires numeric data."); }
        set => SetValue(CountProperty, value);
    }
    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set { if (value is not null and not SolidColorBrush) throw new ArgumentException("Panel accent requires a solid color brush."); SetValue(AccentProperty, value); } }
    public object? Filter { get => GetValue(FilterProperty); set => SetValue(FilterProperty, value); }
    public DataTemplate? FilterTemplate { get => (DataTemplate?)GetValue(FilterTemplateProperty); set => SetValue(FilterTemplateProperty, value); }
    public DataTemplateSelector? FilterTemplateSelector { get => (DataTemplateSelector?)GetValue(FilterTemplateSelectorProperty); set => SetValue(FilterTemplateSelectorProperty, value); }
    public object? Toolbar { get => GetValue(ToolbarProperty); set => SetValue(ToolbarProperty, value); }
    public DataTemplate? ToolbarTemplate { get => (DataTemplate?)GetValue(ToolbarTemplateProperty); set => SetValue(ToolbarTemplateProperty, value); }
    public DataTemplateSelector? ToolbarTemplateSelector { get => (DataTemplateSelector?)GetValue(ToolbarTemplateSelectorProperty); set => SetValue(ToolbarTemplateSelectorProperty, value); }
    public object? DragHandle { get => GetValue(DragHandleProperty); set => SetValue(DragHandleProperty, value); }
    public DataTemplate? DragHandleTemplate { get => (DataTemplate?)GetValue(DragHandleTemplateProperty); set => SetValue(DragHandleTemplateProperty, value); }
    public DataTemplateSelector? DragHandleTemplateSelector { get => (DataTemplateSelector?)GetValue(DragHandleTemplateSelectorProperty); set => SetValue(DragHandleTemplateSelectorProperty, value); }
    public bool Grow { get => (bool)GetValue(GrowProperty); set => SetValue(GrowProperty, value); }
    public bool IsDragging { get => (bool)GetValue(IsDraggingProperty); set => SetValue(IsDraggingProperty, value); }
    public BleeckerPanelVariant Variant { get => (BleeckerPanelVariant)GetValue(VariantProperty); set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(VariantProperty, value); } }
    private static bool Present(object? value, DataTemplate? template, DataTemplateSelector? selector) => template is not null || selector is not null || BleeckerReactContent.Present(value);
    protected override void OnContentChanged(object oldContent, object newContent) { if (bodyText is not null) { renderedSlots.Remove(bodyText); bodyText.Release(); } base.OnContentChanged(oldContent, newContent); Render(); }
    protected override void OnApplyTemplate()
    {
        Detach(); base.OnApplyTemplate(); root = GetTemplateChild("PART_Root") as Grid;
        if (root is null) { if (ReadLocalValue(TemplateProperty) is null) return; throw new InvalidOperationException("Panel requires a PART_Root Grid."); }
        T Part<T>(string name) where T : class => GetTemplateChild(name) as T ?? throw new InvalidOperationException($"Panel requires {name} as {typeof(T).Name}.");
        clipRoot = Part<Grid>("PART_ClipRoot"); left = Part<Border>("PART_LeftAccent"); accentSource = Part<TextBlock>("PART_AccentSource");
        handleIdleSource = Part<TextBlock>("PART_HandleIdleSource"); handleHoverSource = Part<TextBlock>("PART_HandleHoverSource");
        titleText = new("PanelTitle", "BleeckerPrimarySemiboldFontFamily", 11, 16.5, "BleeckerSeaBrush");
        titleText.Text.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold; titleText.Text.CharacterSpacing = 100;
        countText = new("PanelCountText", "BleeckerPrimarySemiboldFontFamily", 11, 16.5, "BleeckerTextPrimaryBrush");
        countText.Text.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold; Typography.SetNumeralAlignment(countText.Text, FontNumeralAlignment.Tabular);
        countHost = Create<Border>("<Border x:Name='PanelCount' CornerRadius='5' BorderThickness='1' Padding='8,2' BackgroundSizing='OuterBorderEdge' Background='{ThemeResource PanelCountBackground}' BorderBrush='{ThemeResource PanelCountBorder}'/>"); countHost.Child = countText;
        filterText = new("PanelFilterText", "BleeckerPrimaryFontFamily", 16, 24, "BleeckerTextPrimaryBrush", stretchContent: true);
        toolbarText = new("PanelToolbarText", "BleeckerPrimaryFontFamily", 16, 24, "BleeckerTextPrimaryBrush", stretchContent: true);
        dragText = new("PanelDragText", "BleeckerPrimaryFontFamily", 16, 24, "BleeckerTextPrimaryBrush", stretchContent: true);
        bodyText = new("PanelBodyText", "BleeckerPrimaryFontFamily", 16, 24, "BleeckerTextPrimaryBrush", stretchContent: true);
        handle = new HandleHost { Name = "PanelHandle", Content = dragText, IsTabStop = false, UseLayoutRounding = false,
            HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Center };
        handle.PointerStateChanged += () => Paint(true);
        dot = new Ellipse { Name = "PanelDot", Width = 6, Height = 6, Opacity = .8, IsHitTestVisible = false, UseLayoutRounding = false };
        AutomationProperties.SetAccessibilityView(dot, AccessibilityView.Raw);
        row = new(handle, dot, titleText, countHost) { Name = "PanelRow", UseLayoutRounding = false };
        filterHost = new Border { Name = "PanelFilter", Padding = new Thickness(20, 0, 20, 16), Child = filterText, UseLayoutRounding = false };
        toolbarHost = Create<Border>("<Border x:Name='PanelToolbar' Padding='20,12' BorderThickness='0,1,0,0' BackgroundSizing='OuterBorderEdge' BorderBrush='{ThemeResource PanelToolbarBorder}' Background='{ThemeResource PanelToolbarBackground}'/>"); toolbarHost.Child = toolbarText;
        header = Create<Border>("<Border x:Name='PanelHeader' BackgroundSizing='OuterBorderEdge' Background='{ThemeResource PanelHeaderBackground}' BorderBrush='{ThemeResource PanelHeaderBorder}' BorderThickness='0,0,0,1'/>");
        header.Child = new HeaderStack(row, filterHost, toolbarHost) { UseLayoutRounding = false };
        feed = Create<ScrollViewer>("<ScrollViewer x:Name='PanelFeed' IsTabStop='False' HorizontalScrollMode='Disabled' HorizontalScrollBarVisibility='Disabled' VerticalScrollMode='Enabled' VerticalScrollBarVisibility='Auto' ZoomMode='Disabled' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Stretch' Background='{ThemeResource PanelFeedBackground}'/>");
        feed.Content = bodyText; surface = new SurfacePanel(header, feed) { Name = "PanelSurface", UseLayoutRounding = false }; Canvas.SetZIndex(header, 10); root.Children.Add(surface);
        left.Background = leftPaint; root.BorderBrush = rightPaint; left.Width = BorderThickness.Left;
        if (IsLoaded) ObserveBrushes(); Render(); Paint(false); UpdateClip();
    }
    private static T Create<T>(string markup) => (T)Microsoft.UI.Xaml.Markup.XamlReader.Load(markup.Insert(markup.IndexOf(' '), " xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' UseLayoutRounding='False'"));
    private void Render()
    {
        if (!Enum.IsDefined(Variant)) throw new ArgumentOutOfRangeException(nameof(Variant)); var count = Count;
        if (surface is null || titleText is null || countText is null || filterText is null || toolbarText is null || dragText is null || bodyText is null) return;
        surface.FlowDirection = FlowDirection;
        SetSlot(titleText, BleeckerTextCasing.Uppercase(Title ?? ""), null, null); SetSlot(countText, count is { } value ? BleeckerNumberText.Format(value) : null, null, null);
        countHost!.Visibility = count.HasValue ? Visibility.Visible : Visibility.Collapsed;
        SetSlot(filterText, Filter, FilterTemplate, FilterTemplateSelector); filterHost!.Visibility = Present(Filter, FilterTemplate, FilterTemplateSelector) ? Visibility.Visible : Visibility.Collapsed;
        SetSlot(toolbarText, Toolbar, ToolbarTemplate, ToolbarTemplateSelector); toolbarHost!.Visibility = Present(Toolbar, ToolbarTemplate, ToolbarTemplateSelector) ? Visibility.Visible : Visibility.Collapsed;
        SetSlot(dragText, DragHandle, DragHandleTemplate, DragHandleTemplateSelector); handle!.Visibility = Present(DragHandle, DragHandleTemplate, DragHandleTemplateSelector) ? Visibility.Visible : Visibility.Collapsed;
        SetSlot(bodyText, Content, ContentTemplate, ContentTemplateSelector);
        foreach (var text in new[] { titleText.Text, countText.Text, filterText.Text, toolbarText.Text, dragText.Text, bodyText.Text })
        { text.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled; text.Language = Language; }
        surface.InvalidateMeasure(); InvalidateMeasure(); Paint(true); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private void SetSlot(BleeckerContentText slot, object? value, DataTemplate? template, DataTemplateSelector? selector)
    {
        if (renderedSlots.TryGetValue(slot, out var previous))
        {
            var same = value is string or bool || BleeckerReactContent.Numeric(value) ? Equals(previous.Value, value) : ReferenceEquals(previous.Value, value);
            if (same && ReferenceEquals(previous.Template, template) && ReferenceEquals(previous.Selector, selector)) return;
        }
        slot.Set(value, template, selector: selector); renderedSlots[slot] = (value, template, selector);
    }
    private void UpdateClip() { if (clipRoot is not null) clipRoot.Clip = new RectangleGeometry { Rect = new Rect(0, 0, clipRoot.ActualWidth, clipRoot.ActualHeight) }; }
    private void UpdateViewportFocus(object? sender, object args)
    {
        if (feed is null || bodyText is not { IsLoaded: true }) return;
        // Match a keyboard-focusable overflow region without inserting an
        // extra stop ahead of focusable caller content. Native focus traversal
        // also handles hyperlinks, disabled children and custom controls.
        var focusable = feed.ScrollableHeight > 0 && feed.ViewportHeight > 0 && FocusManager.FindFirstFocusableElement(bodyText) is null;
        if (feed.IsTabStop != focusable) feed.IsTabStop = focusable;
    }
    private void Detach()
    {
        DisconnectBrushes(); Stop();
        foreach (var slot in new[] { titleText, countText, filterText, toolbarText, dragText, bodyText }) slot?.Release();
        if (feed is not null) feed.Content = null; if (handle is not null) { handle.Content = null; handle.Dispose(); }
        surface?.Children.Clear(); if (root is not null && surface is not null) root.Children.Remove(surface);
        root = clipRoot = null; left = header = filterHost = toolbarHost = countHost = null; accentSource = handleIdleSource = handleHoverSource = null;
        renderedSlots.Clear();
        titleText = countText = filterText = toolbarText = dragText = bodyText = null; handle = null; row = null; surface = null; feed = null; dot = null;
    }
    private void DisconnectBrushes() { foreach (var (owner, property, token) in observers) owner.UnregisterPropertyChangedCallback(property, token); observers.Clear(); }
    private void ObserveBrushes()
    {
        DisconnectBrushes(); if (!IsLoaded || accentSource is null) return;
        foreach (var brush in new[] { Accent, BorderBrush, accentSource.Foreground, handleIdleSource?.Foreground, handleHoverSource?.Foreground }.OfType<SolidColorBrush>().Distinct())
        foreach (var property in new[] { SolidColorBrush.ColorProperty, Brush.OpacityProperty }) observers.Add((brush, property, brush.RegisterPropertyChangedCallback(property, (_, _) => Paint(true))));
        foreach (var source in new[] { accentSource, handleIdleSource, handleHoverSource }.OfType<TextBlock>())
            observers.Add((source, TextBlock.ForegroundProperty, source.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty, (_, _) => { ObserveBrushes(); Paint(true); })));
    }
    private static BleeckerCssColor Color(Brush? brush, bool legacy)
    {
        if (brush is null) return BleeckerCssColor.FromRgba((0, 0, 0, 0), legacy);
        if (brush is not SolidColorBrush solid) throw new ArgumentException("Panel border and accent brushes must be solid colors.");
        return BleeckerCssColor.FromRgba((solid.Color.R, solid.Color.G, solid.Color.B, solid.Color.A / 255d * solid.Opacity), legacy);
    }
    private static void SetColor(SolidColorBrush brush, BleeckerCssColor value)
    { var color = value.Encode(); brush.Color = Windows.UI.Color.FromArgb(255, color.R, color.G, color.B); brush.Opacity = color.Alpha; }
    private void Paint(bool animate)
    {
        var currentHandle = handle;
        if (painting || root is null || accentSource is null || currentHandle is null) return; painting = true;
        try
        {
            var accent = Accent ?? accentSource.Foreground; var time = clock.Elapsed.TotalMilliseconds; var motion = animate && IsLoaded && settings.AnimationsEnabled;
            leftMotion.Retarget(Color(accent, true), time, 200, motion); rightMotion.Retarget(Color(BorderBrush, false), time, 200, motion);
            handleMotion.Retarget(Color(currentHandle.IsOver ? handleHoverSource!.Foreground : handleIdleSource!.Foreground, false), time, 190, motion);
            titleText!.Text.Foreground = accent; dot!.Fill = accent;
            currentHandle.Foreground = handlePaint; dragText!.Text.Foreground = handlePaint;
            SetColor(leftPaint, leftMotion.ValueAt(time)); SetColor(rightPaint, rightMotion.ValueAt(time));
            SetColor(handlePaint, handleMotion.ValueAt(time));
            if (!running && (leftMotion.IsRunning(time) || rightMotion.IsRunning(time) || handleMotion.IsRunning(time))) { running = true; CompositionTarget.Rendering += Frame; }
        }
        finally { painting = false; }
    }
    private void Frame(object? sender, object args)
    {
        var time = clock.Elapsed.TotalMilliseconds; SetColor(leftPaint, leftMotion.ValueAt(time)); SetColor(rightPaint, rightMotion.ValueAt(time));
        SetColor(handlePaint, handleMotion.ValueAt(time));
        if (!leftMotion.IsRunning(time) && !rightMotion.IsRunning(time) && !handleMotion.IsRunning(time)) Stop();
    }
    private void Stop() { if (running) CompositionTarget.Rendering -= Frame; running = false; }
    protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);
    private sealed class Peer(BleeckerPanel owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetNameCore()
        {
            var name = AutomationProperties.GetName(owner);
            if (owner.ReadLocalValue(AutomationProperties.NameProperty) != DependencyProperty.UnsetValue || !string.IsNullOrEmpty(name)) return name;
            return AutomationProperties.GetLabeledBy(owner) is not null ? base.GetNameCore() : owner.Title ?? "";
        }
        protected override string GetClassNameCore() => nameof(BleeckerPanel);
    }
    private sealed class HandleHost : ContentControl, IDisposable
    {
        private readonly InputSystemCursor cursor = InputSystemCursor.Create(InputSystemCursorShape.SizeAll);
        internal bool IsOver { get; private set; }
        internal event Action? PointerStateChanged;
        internal HandleHost()
        {
            ProtectedCursor = cursor;
            var state = new PointerEventHandler((_, args) =>
            {
                var point = args.GetCurrentPoint(this).Position;
                var over = point.X >= 0 && point.X < ActualWidth && point.Y >= 0 && point.Y < ActualHeight;
                if (IsOver == over) return; IsOver = over; PointerStateChanged?.Invoke();
            });
            AddHandler(UIElement.PointerEnteredEvent, state, true);
            AddHandler(UIElement.PointerExitedEvent, state, true);
            AddHandler(UIElement.PointerCanceledEvent, state, true);
            Unloaded += (_, _) => IsOver = false;
        }
        public void Dispose() { PointerStateChanged = null; ProtectedCursor = null; cursor.Dispose(); }
    }
    private sealed partial class HeaderRow(FrameworkElement drag, FrameworkElement dot, BleeckerContentText title, FrameworkElement count) : Panel
    {
        private BleeckerPanelModel.Row layout;
        private void Ensure() { if (Children.Count == 0) { Children.Add(drag); Children.Add(dot); Children.Add(title); Children.Add(count); } }
        protected override Size MeasureOverride(Size available)
        {
            Ensure(); drag.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); dot.Measure(new Size(6, 6)); count.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            title.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); var hasDrag = drag.Visibility == Visibility.Visible; var hasCount = count.Visibility == Visibility.Visible;
            var width = double.IsFinite(available.Width) ? available.Width : title.NaturalWidth + 58 + (hasDrag ? drag.DesiredSize.Width + 12 : 0) + (hasCount ? count.DesiredSize.Width + 12 : 0);
            var titleWidth = BleeckerPanelModel.TitleWidth(width, title.MinimumWidth, drag.DesiredSize.Width, count.DesiredSize.Width, hasDrag, hasCount);
            title.Measure(new Size(titleWidth, double.PositiveInfinity));
            layout = BleeckerPanelModel.Header(width, title.MinimumWidth, title.DesiredSize.Height, drag.DesiredSize.Width, drag.DesiredSize.Height, count.DesiredSize.Width, count.DesiredSize.Height, hasDrag, hasCount);
            return new(width, layout.Height);
        }
        protected override Size ArrangeOverride(Size final)
        {
            if (drag.Visibility == Visibility.Visible) drag.Arrange(new Rect(layout.DragX, (layout.Height - drag.DesiredSize.Height) / 2, drag.DesiredSize.Width, drag.DesiredSize.Height));
            dot.Arrange(new Rect(layout.DotX, (layout.Height - 6) / 2, 6, 6)); title.Arrange(new Rect(layout.TitleX, (layout.Height - title.DesiredSize.Height) / 2, layout.TitleWidth, title.DesiredSize.Height));
            if (count.Visibility == Visibility.Visible) count.Arrange(new Rect(layout.CountX, (layout.Height - count.DesiredSize.Height) / 2, count.DesiredSize.Width, count.DesiredSize.Height));
            return final;
        }
    }
    private sealed partial class HeaderStack(FrameworkElement row, FrameworkElement filter, FrameworkElement toolbar) : Panel
    {
        private void Ensure() { if (Children.Count == 0) { Children.Add(row); Children.Add(filter); Children.Add(toolbar); } }
        protected override Size MeasureOverride(Size available)
        { Ensure(); foreach (var child in Children) child.Measure(new Size(available.Width, double.PositiveInfinity)); return new(double.IsFinite(available.Width) ? available.Width : Children.Max(child => child.DesiredSize.Width), Children.Sum(child => child.DesiredSize.Height)); }
        protected override Size ArrangeOverride(Size final)
        { var y = 0d; foreach (var child in Children) if (child.Visibility == Visibility.Visible) { child.Arrange(new Rect(0, y, final.Width, child.DesiredSize.Height)); y += child.DesiredSize.Height; } return final; }
    }
    private sealed partial class SurfacePanel(FrameworkElement header, FrameworkElement body) : Panel
    {
        private void Ensure() { if (Children.Count == 0) { Children.Add(header); Children.Add(body); } }
        protected override Size MeasureOverride(Size available)
        {
            Ensure(); header.Measure(new Size(available.Width, double.PositiveInfinity)); var bodyHeight = double.IsFinite(available.Height) ? Math.Max(0, available.Height - header.DesiredSize.Height) : double.PositiveInfinity;
            body.Measure(new Size(available.Width, bodyHeight)); var width = double.IsFinite(available.Width) ? available.Width : Math.Max(header.DesiredSize.Width, body.DesiredSize.Width);
            if (!double.IsFinite(available.Width))
            {
                header.Measure(new Size(width, double.PositiveInfinity)); bodyHeight = double.IsFinite(available.Height) ? Math.Max(0, available.Height - header.DesiredSize.Height) : double.PositiveInfinity;
                body.Measure(new Size(width, bodyHeight));
            }
            return new(width, double.IsFinite(available.Height) ? available.Height : header.DesiredSize.Height + body.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size final)
        { header.Arrange(new Rect(0, 0, final.Width, header.DesiredSize.Height)); body.Arrange(new Rect(0, header.DesiredSize.Height, final.Width, Math.Max(0, final.Height - header.DesiredSize.Height))); return final; }
    }
}
