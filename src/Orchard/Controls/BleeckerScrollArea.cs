using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;

namespace Orchard;

/// <summary>A naturally sized scrolling boundary that retains original caller content.</summary>
public sealed partial class BleeckerScrollArea : ContentControl
{
    private Grid? root;
    private ScrollView? view;
    private Border? padding;
    private BleeckerContentText? slot;
    private (object? Value, DataTemplate? Template, DataTemplateSelector? Selector)? rendered;
    private bool mounting, rendering, renderPending;
    private int pendingScroll = -1;
    private double requestedHorizontal, requestedVertical;

    public BleeckerScrollArea()
    {
        DefaultStyleKey = typeof(BleeckerScrollArea); UseLayoutRounding = false; IsTabStop = false;
        Loaded += (_, _) => Render();
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
        foreach (var property in new[] { ContentProperty, ContentTemplateProperty, ContentTemplateSelectorProperty, PaddingProperty, FlowDirectionProperty,
            FontFamilyProperty, FontSizeProperty, FontWeightProperty, ForegroundProperty, LanguageProperty, IsTextScaleFactorEnabledProperty })
            RegisterPropertyChangedCallback(property, (_, _) => Render());
    }

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(BleeckerScrollAreaOrientation), typeof(BleeckerScrollArea),
        new PropertyMetadata(BleeckerScrollAreaOrientation.Vertical, (owner, _) => ((BleeckerScrollArea)owner).Render()));
    public BleeckerScrollAreaOrientation Orientation { get => (BleeckerScrollAreaOrientation)GetValue(OrientationProperty); set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(OrientationProperty, value); } }

    /// <summary>Positive distance from the leading edge; in RTL this is distance from the right.</summary>
    public double HorizontalOffset => view?.HorizontalOffset ?? 0;
    public double VerticalOffset => view?.VerticalOffset ?? 0;
    public double ViewportWidth => view?.ViewportWidth ?? 0;
    public double ViewportHeight => view?.ViewportHeight ?? 0;
    public double ExtentWidth => Math.Max(ViewportWidth, view?.ExtentWidth ?? 0);
    public double ExtentHeight => Math.Max(ViewportHeight, view?.ExtentHeight ?? 0);
    public event EventHandler? ViewChanged;

    /// <summary>Scrolls both programmatic axes immediately, independently of user orientation.</summary>
    /// <returns>The native ScrollView correlation identifier, or -1 when no operation starts.</returns>
    public int ScrollTo(double horizontalOffset, double verticalOffset)
    {
        if (!double.IsFinite(horizontalOffset) || !double.IsFinite(verticalOffset)) throw new ArgumentOutOfRangeException(nameof(horizontalOffset));
        ApplyTemplate();
        var target = view ?? throw new InvalidOperationException("ScrollArea requires its mounted template before scrolling.");
        requestedHorizontal = Math.Clamp(horizontalOffset, 0, Math.Max(0, target.ExtentWidth - target.ViewportWidth));
        requestedVertical = Math.Clamp(verticalOffset, 0, Math.Max(0, target.ExtentHeight - target.ViewportHeight));
        pendingScroll = target.ScrollTo(requestedHorizontal, requestedVertical, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore));
        return pendingScroll;
    }

    protected override void OnApplyTemplate()
    {
        Detach(); base.OnApplyTemplate(); if (Template is null) return;
        root = GetTemplateChild("PART_Layout") as Grid ?? throw new InvalidOperationException("ScrollArea requires a Grid named PART_Layout.");
        slot = new("ScrollAreaContent", "BleeckerPrimaryFontFamily", 16, 24, "BleeckerTextPrimaryBrush", stretchContent: true);
        slot.FlowMinimumChanged += UpdateMinimum;
        slot.SizeChanged += ContentSizeChanged;
        padding = new() { Name = "ScrollAreaPadding", Child = slot, UseLayoutRounding = false };
        view = new() { Name = "ScrollAreaViewport", Content = padding, UseLayoutRounding = false, IsTabStop = false,
            ContentOrientation = ScrollingContentOrientation.Vertical, HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Top, ZoomMode = ScrollingZoomMode.Disabled,
            IgnoredInputKinds = ScrollingInputKinds.MouseWheel | ScrollingInputKinds.Keyboard,
            Style = (Style)new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/ScrollArea.xaml") }["BleeckerScrollAreaViewportStyle"] };
        view.ViewChanged += NativeViewChanged; view.ScrollCompleted += ScrollCompleted;
        view.PointerWheelChanged += NativeWheel; view.KeyDown += NativeKey;
        view.Clip = new RectangleGeometry { Rect = new Rect(0, 0, 0, 0) }; view.SizeChanged += ViewSizeChanged;
        root.Children.Add(view); mounting = true; var mounted = view;
        if (!DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => { if (ReferenceEquals(view, mounted)) { mounting = false; Render(); } }))
            throw new InvalidOperationException("ScrollArea could not mount its original content.");
    }

    private void Render()
    {
        if (mounting || slot is null || view is null || padding is null) return;
        if (rendering) { renderPending = true; return; }
        if (!Enum.IsDefined(Orientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
        rendering = true;
        try
        {
            if (rendered is not { } old || !ReferenceEquals(old.Value, Content) || !ReferenceEquals(old.Template, ContentTemplate) || !ReferenceEquals(old.Selector, ContentTemplateSelector))
            {
                var next = (Value: Content, Template: ContentTemplate, Selector: ContentTemplateSelector);
                slot.Set(next.Value, next.Template, selector: next.Selector); rendered = next;
            }
            slot.Visibility = ContentTemplate is null && ContentTemplateSelector is null && BleeckerReactContent.Display(Content) is null or "" ? Visibility.Collapsed : Visibility.Visible;
            view.HorizontalScrollMode = BleeckerScrollAreaModel.UserHorizontal(Orientation) ? ScrollingScrollMode.Enabled : ScrollingScrollMode.Disabled;
            view.VerticalScrollMode = BleeckerScrollAreaModel.UserVertical(Orientation) ? ScrollingScrollMode.Enabled : ScrollingScrollMode.Disabled;
            view.HorizontalScrollBarVisibility = BleeckerScrollAreaModel.UserHorizontal(Orientation) ? ScrollingScrollBarVisibility.Auto : ScrollingScrollBarVisibility.Hidden;
            view.VerticalScrollBarVisibility = BleeckerScrollAreaModel.UserVertical(Orientation) ? ScrollingScrollBarVisibility.Auto : ScrollingScrollBarVisibility.Hidden;
            view.FlowDirection = slot.FlowDirection = FlowDirection;
            if (padding.Padding != Padding) padding.Padding = Padding;
            slot.Text.FontFamily = FontFamily; slot.Text.FontSize = FontSize; slot.Text.FontWeight = FontWeight; slot.Text.Foreground = Foreground;
            slot.Text.LineHeight = FontSize * 1.5; slot.Text.Language = Language; slot.Text.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled;
            UpdateMinimum();
            (FrameworkElementAutomationPeer.FromElement(this) as Peer)?.Publish();
        }
        finally { rendering = false; }
        if (renderPending) { renderPending = false; Render(); }
    }

    private void UpdateMinimum()
    {
        if (slot is null || padding is null) return;
        var minimum = slot.Visibility == Visibility.Collapsed ? 0 : slot.FlowMinimumWidth;
        if (slot.MinWidth != minimum) slot.MinWidth = minimum;
        var width = slot.Visibility == Visibility.Collapsed ? null : slot.FlowWidth;
        if (!(double.IsNaN(slot.Width) && width is null) && slot.Width != width) slot.Width = width ?? double.NaN;
        var alignment = width is null ? HorizontalAlignment.Stretch : HorizontalAlignment.Left;
        if (slot.HorizontalAlignment != alignment) slot.HorizontalAlignment = alignment;
        var padded = minimum + Padding.Left + Padding.Right;
        if (padding.MinWidth != padded) padding.MinWidth = padded;
    }
    private void ContentSizeChanged(object sender, SizeChangedEventArgs args) => UpdateMinimum();
    private void ViewSizeChanged(object sender, SizeChangedEventArgs args) { if (ReferenceEquals(sender, view)) view!.Clip = new RectangleGeometry { Rect = new Rect(0, 0, view.ActualWidth, view.ActualHeight) }; }
    private void NativeViewChanged(ScrollView sender, object args) { (FrameworkElementAutomationPeer.FromElement(this) as Peer)?.Publish(); ViewChanged?.Invoke(this, EventArgs.Empty); }
    private void ScrollCompleted(ScrollView sender, ScrollingScrollCompletedEventArgs args) { if (args.CorrelationId == pendingScroll) pendingScroll = -1; }
    private void Detach()
    {
        mounting = false; pendingScroll = -1;
        if (view is not null) { view.ViewChanged -= NativeViewChanged; view.ScrollCompleted -= ScrollCompleted; view.PointerWheelChanged -= NativeWheel; view.KeyDown -= NativeKey; view.SizeChanged -= ViewSizeChanged; view.Content = null; }
        if (slot is not null) { slot.FlowMinimumChanged -= UpdateMinimum; slot.SizeChanged -= ContentSizeChanged; slot.Release(); }
        if (padding is not null) padding.Child = null;
        root?.Children.Clear(); root = null; view = null; padding = null; slot = null; rendered = null;
    }

    private bool ScrollBy(double horizontal, double vertical)
    {
        if (!IsEnabled || view is null || !IsLoaded) return false;
        var x = pendingScroll >= 0 ? requestedHorizontal : HorizontalOffset; var y = pendingScroll >= 0 ? requestedVertical : VerticalOffset;
        var nextX = Math.Clamp(x + horizontal, 0, Math.Max(0, ExtentWidth - ViewportWidth)); var nextY = Math.Clamp(y + vertical, 0, Math.Max(0, ExtentHeight - ViewportHeight));
        if (nextX == x && nextY == y) return false;
        ScrollTo(nextX, nextY); return true;
    }
    private bool ProcessWheel(double delta, bool horizontal) => horizontal
        ? BleeckerScrollAreaModel.UserHorizontal(Orientation) && ScrollBy(FlowDirection == FlowDirection.RightToLeft ? -delta : delta, 0)
        : BleeckerScrollAreaModel.UserVertical(Orientation) && ScrollBy(0, -delta);
    private bool ProcessKey(VirtualKey key)
    {
        var vertical = BleeckerScrollAreaModel.UserVertical(Orientation); var horizontal = BleeckerScrollAreaModel.UserHorizontal(Orientation); var direction = FlowDirection == FlowDirection.RightToLeft ? -1 : 1;
        return key switch
        {
            VirtualKey.Down when vertical => ScrollBy(0, 40), VirtualKey.Up when vertical => ScrollBy(0, -40),
            VirtualKey.Right when horizontal => ScrollBy(direction * 40, 0), VirtualKey.Left when horizontal => ScrollBy(direction * -40, 0),
            VirtualKey.PageDown when vertical => ScrollBy(0, ViewportHeight), VirtualKey.PageUp when vertical => ScrollBy(0, -ViewportHeight),
            VirtualKey.Home when vertical => ScrollBy(0, -ExtentHeight), VirtualKey.End when vertical => ScrollBy(0, ExtentHeight),
            _ => false
        };
    }
    protected override void OnKeyDown(KeyRoutedEventArgs args) { if (!args.Handled && ProcessKey(args.Key)) args.Handled = true; if (!args.Handled) base.OnKeyDown(args); }
    private void NativeWheel(object sender, PointerRoutedEventArgs args)
    {
        var point = args.GetCurrentPoint((UIElement)sender);
        if (!args.Handled && ProcessWheel(point.Properties.MouseWheelDelta, point.Properties.IsHorizontalMouseWheel)) args.Handled = true;
    }
    private void NativeKey(object sender, KeyRoutedEventArgs args) { if (!args.Handled && ProcessKey(args.Key)) args.Handled = true; }
    protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);
    private sealed class Peer(BleeckerScrollArea owner) : FrameworkElementAutomationPeer(owner), IScrollProvider
    {
        protected override string GetClassNameCore() => nameof(BleeckerScrollArea);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
        protected override object? GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.Scroll ? this : base.GetPatternCore(pattern);
        public bool HorizontallyScrollable => BleeckerScrollAreaModel.UserHorizontal(owner.Orientation) && owner.ExtentWidth > owner.ViewportWidth;
        public bool VerticallyScrollable => BleeckerScrollAreaModel.UserVertical(owner.Orientation) && owner.ExtentHeight > owner.ViewportHeight;
        public double HorizontalScrollPercent => HorizontallyScrollable ? owner.HorizontalOffset / (owner.ExtentWidth - owner.ViewportWidth) * 100 : -1;
        public double VerticalScrollPercent => VerticallyScrollable ? owner.VerticalOffset / (owner.ExtentHeight - owner.ViewportHeight) * 100 : -1;
        public double HorizontalViewSize => owner.ExtentWidth > 0 ? Math.Min(100, owner.ViewportWidth / owner.ExtentWidth * 100) : 100;
        public double VerticalViewSize => owner.ExtentHeight > 0 ? Math.Min(100, owner.ViewportHeight / owner.ExtentHeight * 100) : 100;
        public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
        {
            if (!owner.IsEnabled) throw new ElementNotEnabledException();
            double Delta(ScrollAmount amount, bool allowed, double viewport)
            {
                if (amount != ScrollAmount.NoAmount && !allowed) throw new InvalidOperationException("The requested user axis is not scrollable.");
                return amount switch { ScrollAmount.NoAmount => 0, ScrollAmount.SmallDecrement => -40, ScrollAmount.SmallIncrement => 40, ScrollAmount.LargeDecrement => -viewport, ScrollAmount.LargeIncrement => viewport, _ => throw new ArgumentOutOfRangeException(nameof(amount)) };
            }
            owner.ScrollBy(Delta(horizontalAmount, HorizontallyScrollable, owner.ViewportWidth), Delta(verticalAmount, VerticallyScrollable, owner.ViewportHeight));
        }
        public void SetScrollPercent(double horizontalPercent, double verticalPercent)
        {
            if (!owner.IsEnabled) throw new ElementNotEnabledException();
            double Offset(double percent, bool allowed, double current, double range)
            {
                if (percent == -1) return current;
                if (!double.IsFinite(percent) || percent < 0 || percent > 100) throw new ArgumentOutOfRangeException(nameof(percent));
                if (!allowed) throw new InvalidOperationException("The requested user axis is not scrollable.");
                return percent / 100 * range;
            }
            owner.ScrollTo(Offset(horizontalPercent, HorizontallyScrollable, owner.HorizontalOffset, owner.ExtentWidth - owner.ViewportWidth), Offset(verticalPercent, VerticallyScrollable, owner.VerticalOffset, owner.ExtentHeight - owner.ViewportHeight));
        }
        private double[]? previous;
        internal void Publish()
        {
            var values = new[] { HorizontalScrollPercent, VerticalScrollPercent, HorizontalViewSize, VerticalViewSize, HorizontallyScrollable ? 1d : 0, VerticallyScrollable ? 1d : 0 };
            var properties = new[] { ScrollPatternIdentifiers.HorizontalScrollPercentProperty, ScrollPatternIdentifiers.VerticalScrollPercentProperty, ScrollPatternIdentifiers.HorizontalViewSizeProperty, ScrollPatternIdentifiers.VerticalViewSizeProperty, ScrollPatternIdentifiers.HorizontallyScrollableProperty, ScrollPatternIdentifiers.VerticallyScrollableProperty };
            if (previous is { } old) for (var i = 0; i < values.Length; i++) if (old[i] != values[i]) RaisePropertyChangedEvent(properties[i], i < 4 ? old[i] : old[i] != 0, i < 4 ? values[i] : values[i] != 0);
            previous = values;
        }
    }
}
