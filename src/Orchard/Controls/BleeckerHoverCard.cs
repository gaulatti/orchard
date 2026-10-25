using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>An inline hover surface that retains the original trigger and content.</summary>
public sealed partial class BleeckerHoverCard : ContentControl
{
    private readonly BleeckerHoverCardModel model = new();
    private readonly UISettings settings = new();
    private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
    private readonly Style defaultSurfaceStyle = CreateSurfaceStyle();
    private Grid? root;
    private InlineLayout? layout;
    private BleeckerContentText? triggerSlot, bodySlot;
    private BleeckerOverlaySurface? surface;
    private TooltipHost? host;
    private bool rendering, renderPending, motionSubscribed, mountPending;
    private (object? Value, DataTemplate? Template, DataTemplateSelector? Selector)? triggerValue, bodyValue;

    public BleeckerHoverCard()
    {
        DefaultStyleKey = typeof(BleeckerHoverCard); IsTabStop = false; UseLayoutRounding = false;
        AddHandler(PointerEnteredEvent, new PointerEventHandler(PointerChanged), true);
        AddHandler(PointerExitedEvent, new PointerEventHandler(PointerChanged), true);
        AddHandler(PointerMovedEvent, new PointerEventHandler(PointerChanged), true);
        Loaded += LoadedOwner; Unloaded += UnloadedOwner;
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
        foreach (var property in new[] { ContentProperty, ContentTemplateProperty, ContentTemplateSelectorProperty, IsEnabledProperty, FlowDirectionProperty, IsTextScaleFactorEnabledProperty, LanguageProperty, FontFamilyProperty, FontSizeProperty, FontWeightProperty, ForegroundProperty })
            RegisterPropertyChangedCallback(property, (_, _) => Render());
    }
    private static DependencyProperty Property(string name, Type type, object? value) => DependencyProperty.Register(name, type, typeof(BleeckerHoverCard), new PropertyMetadata(value, (owner, _) => ((BleeckerHoverCard)owner).Render()));
    public static readonly DependencyProperty TriggerProperty = Property(nameof(Trigger), typeof(object), null);
    public static readonly DependencyProperty TriggerTemplateProperty = Property(nameof(TriggerTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty TriggerTemplateSelectorProperty = Property(nameof(TriggerTemplateSelector), typeof(DataTemplateSelector), null);
    public static readonly DependencyProperty SideProperty = Property(nameof(Side), typeof(BleeckerHoverCardSide), BleeckerHoverCardSide.Bottom);
    public static readonly DependencyProperty SurfaceStyleProperty = Property(nameof(SurfaceStyle), typeof(Style), null);
    public object? Trigger { get => GetValue(TriggerProperty); set => SetValue(TriggerProperty, value); }
    public DataTemplate? TriggerTemplate { get => (DataTemplate?)GetValue(TriggerTemplateProperty); set => SetValue(TriggerTemplateProperty, value); }
    public DataTemplateSelector? TriggerTemplateSelector { get => (DataTemplateSelector?)GetValue(TriggerTemplateSelectorProperty); set => SetValue(TriggerTemplateSelectorProperty, value); }
    public BleeckerHoverCardSide Side { get => (BleeckerHoverCardSide)GetValue(SideProperty); set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(SideProperty, value); } }
    public Style? SurfaceStyle { get => (Style?)GetValue(SurfaceStyleProperty); set => SetValue(SurfaceStyleProperty, value); }
    public bool IsHovered => model.Hovered;

    private static Style CreateSurfaceStyle() { var style = new Style(typeof(BleeckerOverlaySurface)); style.Setters.Add(new Setter(WidthProperty, 288d)); return style; }
    private void LoadedOwner(object sender, RoutedEventArgs args)
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged += MotionChanged;
        Render();
    }
    private void UnloadedOwner(object sender, RoutedEventArgs args)
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= MotionChanged;
        StopMotion(); model.SetHovered(false, clock.Elapsed.TotalMilliseconds, false); Present(clock.Elapsed.TotalMilliseconds + .01);
    }
    protected override void OnApplyTemplate()
    {
        Detach(); base.OnApplyTemplate(); if (Template is null) return;
        root = GetTemplateChild("PART_Layout") as Grid ?? throw new InvalidOperationException("HoverCard requires a Grid named PART_Layout.");
        root.FlowDirection = FlowDirection.LeftToRight;
        triggerSlot = new("HoverCardTrigger", "BleeckerPrimaryFontFamily", 16, 24, "BleeckerTextPrimaryBrush");
        bodySlot = new("HoverCardContent", "BleeckerPrimaryFontFamily", 16, 24, "BleeckerTextPrimaryBrush");
        surface = new() { Name = "HoverCardSurface", Content = bodySlot, UseLayoutRounding = false };
        foreach (var property in new[] { nameof(Background), nameof(BorderBrush), nameof(BorderThickness), nameof(CornerRadius), nameof(Padding), nameof(FontFamily), nameof(FontSize), nameof(FontWeight), nameof(Foreground), nameof(HorizontalContentAlignment), nameof(VerticalContentAlignment) })
            surface.SetBinding(SurfaceProperty(property), new Binding { Source = this, Path = new PropertyPath(property) });
        host = new(this) { Name = "HoverCardTooltip", Content = surface, IsTabStop = false, UseLayoutRounding = false };
        layout = new(this, triggerSlot, host) { Name = "HoverCardInline", UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight };
        layout.Mount();
        root.Children.Add(layout);
        var mountedRoot = root; mountPending = true;
        if (!DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => { if (ReferenceEquals(root, mountedRoot)) { mountPending = false; Render(); } })) throw new InvalidOperationException("HoverCard could not mount its template.");
    }
    private static DependencyProperty SurfaceProperty(string name) => name switch
    {
        nameof(Background) => Control.BackgroundProperty, nameof(BorderBrush) => Control.BorderBrushProperty, nameof(BorderThickness) => Control.BorderThicknessProperty,
        nameof(CornerRadius) => Control.CornerRadiusProperty, nameof(Padding) => Control.PaddingProperty, nameof(FontFamily) => Control.FontFamilyProperty,
        nameof(FontSize) => Control.FontSizeProperty, nameof(FontWeight) => Control.FontWeightProperty, nameof(Foreground) => Control.ForegroundProperty,
        nameof(HorizontalContentAlignment) => Control.HorizontalContentAlignmentProperty, nameof(VerticalContentAlignment) => Control.VerticalContentAlignmentProperty,
        _ => throw new ArgumentException("Unsupported HoverCard surface property.", nameof(name))
    };
    private static void SetSlot(BleeckerContentText slot, object? value, DataTemplate? template, DataTemplateSelector? selector, ref (object? Value, DataTemplate? Template, DataTemplateSelector? Selector)? previous)
    {
        if (previous is { } old && ReferenceEquals(old.Value, value) && ReferenceEquals(old.Template, template) && ReferenceEquals(old.Selector, selector)) return;
        slot.Set(value, template, selector: selector); previous = (value, template, selector);
    }
    private void Render()
    {
        if (mountPending) return; if (rendering) { renderPending = true; return; }
        if (!Enum.IsDefined(Side)) throw new ArgumentException("Invalid HoverCard side.");
        if (root is null || triggerSlot is null || bodySlot is null || surface is null || host is null) return;
        rendering = true;
        try
        {
            SetSlot(triggerSlot, Trigger, TriggerTemplate, TriggerTemplateSelector, ref triggerValue);
            SetSlot(bodySlot, Content, ContentTemplate, ContentTemplateSelector, ref bodyValue);
            foreach (var slot in new[] { triggerSlot, bodySlot })
            {
                if (slot.FlowDirection != FlowDirection) slot.FlowDirection = FlowDirection;
                if (slot.Text.FontFamily.Source != FontFamily.Source) slot.Text.FontFamily = FontFamily;
                if (slot.Text.FontSize != FontSize) slot.Text.FontSize = FontSize;
                if (slot.Text.FontWeight != FontWeight) slot.Text.FontWeight = FontWeight;
                if (!ReferenceEquals(slot.Text.Foreground, Foreground)) slot.Text.Foreground = Foreground;
                if (slot.Text.LineHeight != FontSize * 1.5) slot.Text.LineHeight = FontSize * 1.5;
                if (slot.Text.IsTextScaleFactorEnabled != IsTextScaleFactorEnabled) slot.Text.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled;
                if (slot.Text.Language != Language) slot.Text.Language = Language;
            }
            if (!ReferenceEquals(surface.Style, SurfaceStyle ?? defaultSurfaceStyle)) surface.Style = SurfaceStyle ?? defaultSurfaceStyle;
            if (surface.FlowDirection != FlowDirection) surface.FlowDirection = FlowDirection;
            if (!IsEnabled) model.SetHovered(false, clock.Elapsed.TotalMilliseconds, settings.AnimationsEnabled);
            layout!.InvalidateMeasure(); Present(clock.Elapsed.TotalMilliseconds);
        }
        finally { rendering = false; }
        if (renderPending) { renderPending = false; Render(); }
    }
    private static bool Within(DependencyObject? node, DependencyObject? ancestor)
    {
        if (ancestor is null) return false;
        for (; node is not null; node = VisualTreeHelper.GetParent(node)) if (ReferenceEquals(node, ancestor)) return true;
        return false;
    }
    private void PointerChanged(object sender, PointerRoutedEventArgs args)
    {
        if (!IsLoaded || !IsEnabled || XamlRoot?.Content is not UIElement input || root is null) return;
        UpdateHoverAtPoint(args.GetCurrentPoint(input).Position);
    }
    private void UpdateHoverAtPoint(Point point)
    {
        if (!IsLoaded || !IsEnabled || root is null) return;
        UIElement scene = this;
        for (var parent = VisualTreeHelper.GetParent(scene); parent is UIElement element; parent = VisualTreeHelper.GetParent(scene)) scene = element;
        var top = VisualTreeHelper.FindElementsInHostCoordinates(point, scene).FirstOrDefault();
        SetHovered(Within(top, this), clock.Elapsed.TotalMilliseconds);
    }
    private void SetHovered(bool value, double time)
    {
        if (model.Hovered == value) return;
        model.SetHovered(value, time, settings.AnimationsEnabled); Present(time);
    }
    private void Present(double time)
    {
        if (host is null || layout is null) return;
        host.Opacity = model.OpacityAt(time); host.IsHitTestVisible = model.HitTestVisible;
        layout.InvalidateArrange();
        if (IsLoaded && XamlRoot?.IsHostVisible == true && model.IsAnimating(time)) { if (!motionSubscribed) { CompositionTarget.Rendering += Frame; motionSubscribed = true; } }
        else StopMotion();
    }
    private void Frame(object? sender, object args) => Present(clock.Elapsed.TotalMilliseconds);
    private void StopMotion() { if (motionSubscribed) CompositionTarget.Rendering -= Frame; motionSubscribed = false; }
    private void MotionChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(() => { model.SetHovered(model.Hovered, clock.Elapsed.TotalMilliseconds, settings.AnimationsEnabled); Present(clock.Elapsed.TotalMilliseconds); });
    private void Detach()
    {
        mountPending = false; StopMotion();
        if (host is not null) host.Content = null;
        if (surface is not null) surface.Content = null;
        triggerSlot?.Release(); bodySlot?.Release(); layout?.Children.Clear(); root?.Children.Clear();
        root = null; layout = null; triggerSlot = bodySlot = null; surface = null; host = null; triggerValue = bodyValue = null;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
    private sealed class InlineLayout(BleeckerHoverCard owner, BleeckerContentText trigger, TooltipHost tooltip) : Panel
    {
        protected override Size MeasureOverride(Size available)
        {
            trigger.Measure(available); tooltip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); return trigger.DesiredSize;
        }
        protected override Size ArrangeOverride(Size final)
        {
            trigger.Arrange(new Rect(0, 0, trigger.DesiredSize.Width, trigger.DesiredSize.Height));
            var p = BleeckerHoverCardModel.Place(new(0, 0, trigger.DesiredSize.Width, trigger.DesiredSize.Height), tooltip.DesiredSize.Width, tooltip.DesiredSize.Height, owner.Side, owner.model.Hovered);
            tooltip.Arrange(new Rect(p.X, p.Y, p.Width, p.Height)); return final;
        }
        internal void Mount() { Children.Add(trigger); Children.Add(tooltip); Canvas.SetZIndex(tooltip, 50); }
    }
    private sealed class TooltipHost : ContentControl
    {
        private readonly BleeckerHoverCard owner;
        internal TooltipHost(BleeckerHoverCard owner)
        {
            this.owner = owner;
            Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ContentControl'><ContentPresenter Content='{TemplateBinding Content}' HorizontalAlignment='Stretch' VerticalAlignment='Stretch'/></ControlTemplate>");
        }
        protected override AutomationPeer OnCreateAutomationPeer() => new TooltipPeer(this);
        private sealed class TooltipPeer(TooltipHost host) : FrameworkElementAutomationPeer(host)
        {
            protected override string GetClassNameCore() => "BleeckerHoverCardContent";
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolTip;
            protected override string GetNameCore() => host.owner.bodySlot?.AccessibleName() ?? "";
        }
    }
}
