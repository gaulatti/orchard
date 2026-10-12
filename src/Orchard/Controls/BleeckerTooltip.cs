using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Dispatching;
using Windows.Foundation;

namespace Orchard;

/// <summary>A branded tooltip with an application-constrained popup and explicit owner.</summary>
public sealed partial class BleeckerTooltip : ContentControl
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<XamlRoot, OpenTooltip> current = new();
    private sealed class OpenTooltip { public WeakReference<BleeckerTooltip>? Value; }
    private BleeckerOverlaySurface? surface;
    private Polygon? arrow;
    private UIElement? inputRoot;
    private Popup? popup;
    private bool positioning;
    private PlacementMode? arrowPlacement;
    private (Rect Bounds, double Width, double Height, double RootWidth, double RootHeight, PlacementMode Side)? positionedGeometry;
    public PlacementMode ActualPlacement { get; private set; } = PlacementMode.Top;
    private FrameworkElement? target;
    private readonly DispatcherQueueTimer hoverTimer;
    private DateTimeOffset lastClosed = DateTimeOffset.MinValue;
    private TooltipPoint[]? transitArea;
    public event EventHandler? Opened;
    public event EventHandler? Closed;

    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(BleeckerTooltip), new PropertyMetadata(false, (sender, args) => ((BleeckerTooltip)sender).SetOpen((bool)args.NewValue)));
    public PlacementMode Placement { get => (PlacementMode)GetValue(PlacementProperty); set => SetValue(PlacementProperty, value); }
    public static readonly DependencyProperty PlacementProperty = DependencyProperty.Register(nameof(Placement), typeof(PlacementMode), typeof(BleeckerTooltip), new PropertyMetadata(PlacementMode.Top, (sender, _) => ((BleeckerTooltip)sender).UpdatePlacement()));
    public FrameworkElement? PlacementTarget { get => (FrameworkElement?)GetValue(PlacementTargetProperty); set => SetValue(PlacementTargetProperty, value); }
    public static readonly DependencyProperty PlacementTargetProperty = DependencyProperty.Register(nameof(PlacementTarget), typeof(FrameworkElement), typeof(BleeckerTooltip), new PropertyMetadata(null, (sender, args) => ((BleeckerTooltip)sender).Connect((FrameworkElement?)args.NewValue)));
    public int DelayDuration { get => (int)GetValue(DelayDurationProperty); set => SetValue(DelayDurationProperty, value); }
    public static readonly DependencyProperty DelayDurationProperty = DependencyProperty.Register(nameof(DelayDuration), typeof(int), typeof(BleeckerTooltip), new PropertyMetadata(400, (_, args) => { if ((int)args.NewValue < 0) throw new ArgumentOutOfRangeException(nameof(DelayDuration)); }));

    public BleeckerTooltip()
    {
        DefaultStyleKey = typeof(BleeckerTooltip);
        hoverTimer = DispatcherQueue.CreateTimer();
        hoverTimer.IsRepeating = false;
        hoverTimer.Tick += (_, _) => { if (target?.IsLoaded == true && target is not Control { IsEnabled: false }) { delayedEntry = true; IsOpen = true; } };
        PointerEntered += (_, _) => transitArea = null;
        PointerExited += OnContentExited;
        AddHandler(PointerMovedEvent, new PointerEventHandler(OnPointerMoved), true);
    }

    private void Connect(FrameworkElement? value)
    {
        IsOpen = false;
        FinishClose();
        hoverTimer.Stop();
        if (target is not null)
        {
            target.PointerEntered -= OnTargetEntered;
            target.PointerExited -= OnTargetExited;
            target.PointerPressed -= OnTargetPressed;
            if (target is ButtonBase oldButton) oldButton.Click -= OnTargetActivated;
            if (target is BleeckerButton oldWrappedButton) oldWrappedButton.Click -= OnTargetActivated;
            target.GotFocus -= OnTargetFocused;
            target.LostFocus -= OnTargetBlurred;
            target.Unloaded -= OnTargetUnloaded;
            target.ActualThemeChanged -= OnTargetThemeChanged;
            AutomationProperties.GetDescribedBy(target).Remove(this);
        }
        target = value;
        if (target is null) return;
        target.PointerEntered += OnTargetEntered;
        target.PointerExited += OnTargetExited;
        target.PointerPressed += OnTargetPressed;
        if (target is ButtonBase button) button.Click += OnTargetActivated;
        if (target is BleeckerButton wrappedButton) wrappedButton.Click += OnTargetActivated;
        target.GotFocus += OnTargetFocused;
        target.LostFocus += OnTargetBlurred;
        target.Unloaded += OnTargetUnloaded;
        target.ActualThemeChanged += OnTargetThemeChanged;
        AutomationProperties.GetDescribedBy(target).Add(this);
    }

    private void OnTargetEntered(object sender, PointerRoutedEventArgs args)
    {
        transitArea = null;
        if (IsOpen) return;
        if (target is Control { IsEnabled: false }) return;
        if (DateTimeOffset.UtcNow - lastClosed <= TimeSpan.FromMilliseconds(150) || DelayDuration == 0) IsOpen = true;
        else { hoverTimer.Interval = TimeSpan.FromMilliseconds(DelayDuration); hoverTimer.Start(); }
    }
    private void OnTargetExited(object sender, PointerRoutedEventArgs args)
    {
        hoverTimer.Stop();
        if (target is not null && surface is not null) BeginTransit(args, target, surface);
    }
    private void OnContentExited(object sender, PointerRoutedEventArgs args)
    {
        if (target is not null && surface is not null) BeginTransit(args, surface, target);
    }

    private Rect Bounds(FrameworkElement element) => element.TransformToVisual(inputRoot).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
    private void BeginTransit(PointerRoutedEventArgs args, FrameworkElement leaving, FrameworkElement destination)
    {
        if (!IsOpen || inputRoot is null) return;
        var point = args.GetCurrentPoint(inputRoot).Position;
        var from = Bounds(leaving);
        if (point.X > from.Left && point.X < from.Right && point.Y > from.Top && point.Y < from.Bottom) return; // Ignore exits from a child while still inside its owner.
        var to = Bounds(destination);
        transitArea = BleeckerPointerTransit.Create(new TooltipPoint(point.X, point.Y),
            new TooltipBounds(from.Left, from.Top, from.Right, from.Bottom), new TooltipBounds(to.Left, to.Top, to.Right, to.Bottom));
    }
    private void OnPointerMoved(object sender, PointerRoutedEventArgs args)
    {
        if (!IsOpen || transitArea is null || inputRoot is null || target is null || surface is null) return;
        var point = args.GetCurrentPoint(inputRoot).Position;
        if (Bounds(target).Contains(point) || Bounds(surface).Contains(point)) transitArea = null;
        else if (!BleeckerPointerTransit.Contains(transitArea, new TooltipPoint(point.X, point.Y))) IsOpen = false;
    }
    private void OnTargetPressed(object sender, PointerRoutedEventArgs args) { hoverTimer.Stop(); IsOpen = false; }
    private void OnTargetActivated(object sender, RoutedEventArgs args) { hoverTimer.Stop(); IsOpen = false; }
    private void OnTargetFocused(object sender, RoutedEventArgs args)
    {
        if (target?.XamlRoot is not null && Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(target.XamlRoot) is Control { FocusState: FocusState.Keyboard })
        { hoverTimer.Stop(); IsOpen = true; }
    }
    private void OnTargetBlurred(object sender, RoutedEventArgs args) { hoverTimer.Stop(); IsOpen = false; }
    private void OnTargetUnloaded(object sender, RoutedEventArgs args) { hoverTimer.Stop(); IsOpen = false; FinishClose(); }
    private void OnTargetThemeChanged(FrameworkElement sender, object args)
    {
        if (popup is not null) popup.RequestedTheme = sender.ActualTheme;
    }

    private void SetOpen(bool open)
    {
        hoverTimer.Stop();
        if (!open)
        {
            if (popup is null) return;
            FrameworkElementAutomationPeer.FromElement(this)?.RaiseAutomationEvent(AutomationEvents.ToolTipClosed);
            lastClosed = DateTimeOffset.UtcNow;
            BeginMotion(false);
            Closed?.Invoke(this, EventArgs.Empty);
            return;
        }
        if (target?.XamlRoot is null) throw new InvalidOperationException("Attach BleeckerTooltip to a loaded PlacementTarget before opening it.");
        FinishClose();
        XamlRoot = target.XamlRoot;
        var slot = current.GetOrCreateValue(XamlRoot);
        if (slot.Value?.TryGetTarget(out var previous) == true && !ReferenceEquals(previous, this)) previous.IsOpen = false;
        slot.Value = new WeakReference<BleeckerTooltip>(this);
        popup = new Popup { XamlRoot = XamlRoot, RequestedTheme = target.ActualTheme, ShouldConstrainToRootBounds = true, Child = this };
        inputRoot = XamlRoot.Content;
        inputRoot.AddHandler(KeyDownEvent, new KeyEventHandler(OnRootKeyDown), true);
        inputRoot.AddHandler(PointerPressedEvent, new PointerEventHandler(OnTargetPressed), true);
        inputRoot.AddHandler(PointerMovedEvent, new PointerEventHandler(OnPointerMoved), true);
        LayoutUpdated += OnLayoutUpdated;
        popup.IsOpen = true;
        ApplyTemplate();
        UpdateLayout();
        UpdatePlacement();
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) motionSettings.AnimationsEnabledChanged += OnMotionSettingsChanged;
        BeginMotion(true);
        FrameworkElementAutomationPeer.CreatePeerForElement(this)?.RaiseAutomationEvent(AutomationEvents.ToolTipOpened);
        Opened?.Invoke(this, EventArgs.Empty);
    }

    private void OnRootKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (!IsOpen || args.Key != Windows.System.VirtualKey.Escape) return;
        IsOpen = false;
        args.Handled = true;
    }

    private void DetachInput()
    {
        inputRoot?.RemoveHandler(KeyDownEvent, new KeyEventHandler(OnRootKeyDown));
        inputRoot?.RemoveHandler(PointerPressedEvent, new PointerEventHandler(OnTargetPressed));
        inputRoot?.RemoveHandler(PointerMovedEvent, new PointerEventHandler(OnPointerMoved));
        inputRoot = null;
        LayoutUpdated -= OnLayoutUpdated;
        popup = null;
        positionedGeometry = null;
        transitArea = null;
    }

    private void OnLayoutUpdated(object? sender, object args)
    {
        // Reattached owners can change ActualTheme without delivering its event.
        // Keep the floating visual tree synchronized during its existing layout
        // observation, without replacing the tooltip's consumer theme override.
        if (popup is not null && target is not null && popup.RequestedTheme != target.ActualTheme)
            popup.RequestedTheme = target.ActualTheme;
        UpdatePlacement();
    }

    private void UpdatePlacement()
    {
        if (positioning || popup is null || surface is null || arrow is null || PlacementTarget is not FrameworkElement target || XamlRoot?.Content is not FrameworkElement root) return;
        if (surface.ActualWidth <= 0 || surface.ActualHeight <= 0) return;
        if (Placement is not (PlacementMode.Top or PlacementMode.Bottom or PlacementMode.Left or PlacementMode.Right))
            throw new InvalidOperationException("BleeckerTooltip supports Top, Bottom, Left and Right placement.");
        positioning = true;
        try
        {
            var bounds = target.TransformToVisual(root).TransformBounds(new Rect(0, 0, target.ActualWidth, target.ActualHeight));
            var width = surface.ActualWidth;
            var height = surface.ActualHeight;
            var geometry = (bounds, width, height, root.ActualWidth, root.ActualHeight, Placement);
            if (positionedGeometry == geometry) return;
            positionedGeometry = geometry;
            const double edge = 10, distance = 12;
            double Space(PlacementMode side) => side switch
            {
                PlacementMode.Top => bounds.Top - edge,
                PlacementMode.Bottom => root.ActualHeight - edge - bounds.Bottom,
                PlacementMode.Left => bounds.Left - edge,
                _ => root.ActualWidth - edge - bounds.Right
            };
            var opposite = Placement switch { PlacementMode.Top => PlacementMode.Bottom, PlacementMode.Bottom => PlacementMode.Top, PlacementMode.Left => PlacementMode.Right, _ => PlacementMode.Left };
            var required = (Placement is PlacementMode.Top or PlacementMode.Bottom ? height : width) + distance;
            ActualPlacement = Space(Placement) < required && Space(opposite) > Space(Placement) ? opposite : Placement;
            UpdateArrow();
            var x = ActualPlacement switch
            {
                PlacementMode.Left => bounds.Left - width - distance,
                PlacementMode.Right => bounds.Right + distance,
                _ => bounds.Left + (bounds.Width - width) / 2
            };
            var y = ActualPlacement switch
            {
                PlacementMode.Top => bounds.Top - height - distance,
                PlacementMode.Bottom => bounds.Bottom + distance,
                _ => bounds.Top + (bounds.Height - height) / 2
            };
            x = Math.Clamp(x, edge, Math.Max(edge, root.ActualWidth - edge - width));
            y = Math.Clamp(y, edge, Math.Max(edge, root.ActualHeight - edge - height));
            var vertical = ActualPlacement is PlacementMode.Top or PlacementMode.Bottom;
            var center = vertical ? bounds.Left + bounds.Width / 2 - x : bounds.Top + bounds.Height / 2 - y;
            var extent = vertical ? width : height;
            arrow.Visibility = center >= 5 && center <= extent - 5 ? Visibility.Visible : Visibility.Collapsed;
            if (vertical)
            {
                arrow.HorizontalAlignment = HorizontalAlignment.Left;
                arrow.Margin = new Thickness(Math.Clamp(center - 5, 0, Math.Max(0, width - 10)), 0, 0, 0);
            }
            else
            {
                arrow.VerticalAlignment = VerticalAlignment.Top;
                arrow.Margin = new Thickness(0, Math.Clamp(center - 5, 0, Math.Max(0, height - 10)), 0, 0);
            }
            var offsetX = x - (ActualPlacement == PlacementMode.Right ? 5 : 0);
            var offsetY = y - (ActualPlacement == PlacementMode.Bottom ? 5 : 0);
            if (Math.Abs(popup.HorizontalOffset - offsetX) > .01) popup.HorizontalOffset = offsetX;
            if (Math.Abs(popup.VerticalOffset - offsetY) > .01) popup.VerticalOffset = offsetY;
        }
        finally { positioning = false; }
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        motionRoot = GetTemplateChild("PART_MotionRoot") as FrameworkElement;
        surface = GetTemplateChild("PART_Surface") as BleeckerOverlaySurface;
        arrow = GetTemplateChild("PART_Arrow") as Polygon;
        arrowPlacement = null;
        UpdateArrow();
    }

    private void UpdateArrow()
    {
        if (surface is null || arrow is null) return;
        if (arrowPlacement == ActualPlacement) return;
        arrowPlacement = ActualPlacement;
        var vertical = ActualPlacement is PlacementMode.Top or PlacementMode.Bottom;
        arrow.Width = vertical ? 10 : 5;
        arrow.Height = vertical ? 5 : 10;
        arrow.Margin = new Thickness(0);
        arrow.HorizontalAlignment = ActualPlacement switch
        {
            PlacementMode.Left => HorizontalAlignment.Right,
            PlacementMode.Right => HorizontalAlignment.Left,
            _ => HorizontalAlignment.Center
        };
        arrow.VerticalAlignment = ActualPlacement switch
        {
            PlacementMode.Top => VerticalAlignment.Bottom,
            PlacementMode.Bottom => VerticalAlignment.Top,
            _ => VerticalAlignment.Center
        };
        surface.Margin = ActualPlacement switch
        {
            PlacementMode.Top => new Thickness(0, 0, 0, 5),
            PlacementMode.Bottom => new Thickness(0, 5, 0, 0),
            PlacementMode.Left => new Thickness(0, 0, 5, 0),
            PlacementMode.Right => new Thickness(5, 0, 0, 0),
            _ => new Thickness(0)
        };
        arrow.Visibility = ActualPlacement is PlacementMode.Top or PlacementMode.Bottom or PlacementMode.Left or PlacementMode.Right
            ? Visibility.Visible : Visibility.Collapsed;
        arrow.Points = ActualPlacement switch
        {
            PlacementMode.Bottom => Points(0, 5, 5, 0, 10, 5),
            PlacementMode.Left => Points(0, 0, 5, 5, 0, 10),
            PlacementMode.Right => Points(5, 0, 0, 5, 5, 10),
            _ => Points(0, 0, 10, 0, 5, 5)
        };
    }

    private static PointCollection Points(double x1, double y1, double x2, double y2, double x3, double y3)
        => [new Point(x1, y1), new Point(x2, y2), new Point(x3, y3)];

    protected override AutomationPeer OnCreateAutomationPeer() => new TooltipPeer(this);
    private sealed class TooltipPeer(BleeckerTooltip owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolTip;
        protected override string GetClassNameCore() => nameof(BleeckerTooltip);
        protected override string GetNameCore() => string.IsNullOrEmpty(base.GetNameCore()) ? owner.Content as string ?? string.Empty : base.GetNameCore();
    }
}
