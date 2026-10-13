using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

public enum BleeckerAlertType { Success, Error, Warning, Info }

/// <summary>A window-owned transient notification with a consumer-owned message.</summary>
public sealed partial class BleeckerAlert : Control
{
    private Popup? popup;
    private FrameworkElement? motionRoot;
    private Button? close;
    private BleeckerOverlaySurface? surface;
    private TextBlock? message;
    private XamlRoot? observedRoot;
    private readonly DispatcherQueueTimer lifetime, exit;
    private readonly UISettings settings = new();
    private bool entryScheduled, closing, leavingPopupFocus;
    private (double Width, double X, double Y, ElementTheme Theme)? geometry;

    public BleeckerAlert()
    {
        DefaultStyleKey = typeof(BleeckerAlert);
        lifetime = DispatcherQueue.CreateTimer(); lifetime.IsRepeating = false;
        lifetime.Tick += (_, _) => Close();
        exit = DispatcherQueue.CreateTimer(); exit.IsRepeating = false;
        exit.Interval = TimeSpan.FromMilliseconds(220);
        exit.Tick += (_, _) => FinishClose();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ActualThemeChanged += (_, _) => UpdateGeometry();
        GettingFocus += (_, args) =>
        {
            if (!leavingPopupFocus && ReferenceEquals(args.NewFocusedElement, this) && IsOpen && popup?.IsOpen == true && close?.IsEnabled == true &&
                !args.TrySetNewFocusedElement(close))
                throw new InvalidOperationException("The alert could not delegate keyboard focus to its close action.");
        };
    }

    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(nameof(Message), typeof(string), typeof(BleeckerAlert),
        new PropertyMetadata(string.Empty, (owner, args) =>
        {
            if (args.NewValue is null) throw new ArgumentNullException(nameof(Message));
            var alert = (BleeckerAlert)owner;
            FrameworkElementAutomationPeer.FromElement(alert)?.InvalidatePeer();
            alert.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, alert.Announce);
        }));
    public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(nameof(Type), typeof(BleeckerAlertType), typeof(BleeckerAlert),
        new PropertyMetadata(BleeckerAlertType.Info, (owner, args) =>
        {
            if (!Enum.IsDefined((BleeckerAlertType)args.NewValue)) throw new ArgumentOutOfRangeException(nameof(Type));
            ((BleeckerAlert)owner).UpdateType();
        }));
    public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(nameof(Duration), typeof(double), typeof(BleeckerAlert),
        new PropertyMetadata(5000d, (owner, args) =>
        {
            if (!double.IsFinite((double)args.NewValue)) throw new ArgumentOutOfRangeException(nameof(Duration));
            ((BleeckerAlert)owner).RestartLifetime();
        }));
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(BleeckerAlert),
        new PropertyMetadata(true, (owner, args) =>
        {
            var alert = (BleeckerAlert)owner;
            if ((bool)args.NewValue) alert.Open(); else alert.BeginClose();
        }));
    public string Message { get => (string)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
    public BleeckerAlertType Type { get => (BleeckerAlertType)GetValue(TypeProperty); set => SetValue(TypeProperty, value); }
    /// <summary>Lifetime in milliseconds; zero and negative values keep the alert open.</summary>
    public double Duration { get => (double)GetValue(DurationProperty); set => SetValue(DurationProperty, value); }
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public event EventHandler? Opened;
    public event EventHandler? Closed;
    public void Close() => IsOpen = false;

    protected override void OnApplyTemplate()
    {
        DetachPresentation();
        base.OnApplyTemplate();
        popup = GetTemplateChild("PART_Popup") as Popup ?? throw new InvalidOperationException("BleeckerAlert requires PART_Popup.");
        surface = GetTemplateChild("PART_Surface") as BleeckerOverlaySurface ?? throw new InvalidOperationException("BleeckerAlert requires PART_Surface.");
        motionRoot = GetTemplateChild("PART_MotionRoot") as FrameworkElement ?? throw new InvalidOperationException("BleeckerAlert requires PART_MotionRoot.");
        close = GetTemplateChild("PART_Close") as Button ?? throw new InvalidOperationException("BleeckerAlert requires PART_Close.");
        message = GetTemplateChild("PART_Message") as TextBlock ?? throw new InvalidOperationException("BleeckerAlert requires PART_Message.");
        close.Click += OnCloseClicked;
        close.KeyDown += OnCloseKeyDown;
        ConnectCloseMotion();
        UpdateType();
        if (IsLoaded && IsOpen) Open();
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        observedRoot = XamlRoot ?? throw new InvalidOperationException("An alert must be mounted in a native window.");
        observedRoot.Changed += OnRootChanged;
        LayoutUpdated += OnLayoutUpdated;
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged += OnMotionChanged;
        ApplyTemplate();
        if (IsOpen) Open();
    }
    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        if (observedRoot is not null) observedRoot.Changed -= OnRootChanged;
        observedRoot = null;
        LayoutUpdated -= OnLayoutUpdated;
        geometry = null;
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= OnMotionChanged;
        StopPresentation();
    }
    private void OnRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdateGeometry();
    private void OnLayoutUpdated(object? sender, object args) => UpdateGeometry();
    private void UpdateGeometry()
    {
        if (!IsLoaded || popup is null || surface is null || XamlRoot is null) return;
        var width = Math.Max(0, Math.Min(MaxWidth, XamlRoot.Size.Width - 32));
        // A Popup declared in a control template starts at that control's
        // layout origin. Translate the window-owned reference position into
        // that local coordinate system; offsets alone are not screen evidence.
        var origin = TransformToVisual(XamlRoot.Content).TransformPoint(new Windows.Foundation.Point(0, 0));
        var x = (XamlRoot.Size.Width - width) / 2 - origin.X;
        var y = 96 - origin.Y;
        var next = (width, x, y, ActualTheme);
        if (geometry == next) return;
        geometry = next;
        popup.XamlRoot = XamlRoot;
        popup.RequestedTheme = ActualTheme;
        surface.Width = width;
        popup.HorizontalOffset = x;
        popup.VerticalOffset = y;
    }
    private void UpdateType() => VisualStateManager.GoToState(this, "Alert" + Type, false);
    private void OnCloseClicked(object sender, RoutedEventArgs args) => Close();
    private void OnCloseKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs args)
    {
        if (args.Key != Windows.System.VirtualKey.Tab || XamlRoot is null) return;
        args.Handled = true;
        leavingPopupFocus = true;
        try
        {
            var direction = (GetKeyState(0x10) & 0x8000) != 0 ? Microsoft.UI.Xaml.Input.FocusNavigationDirection.Previous : Microsoft.UI.Xaml.Input.FocusNavigationDirection.Next;
            var anchored = Focus(FocusState.Keyboard);
            var moved = anchored && Microsoft.UI.Xaml.Input.FocusManager.TryMoveFocus(direction,
                new Microsoft.UI.Xaml.Input.FindNextElementOptions { SearchRoot = XamlRoot.Content });
            var focused = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot);
            if (!anchored || focused is null || ReferenceEquals(focused, this) || ReferenceEquals(focused, close))
                throw new InvalidOperationException($"The alert could not return Tab navigation: anchored={anchored}, moved={moved}, focused={focused?.GetType().Name}.");
        }
        finally { leavingPopupFocus = false; }
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern short GetKeyState(int key);
    private void Open()
    {
        if (!IsLoaded || popup is null || motionRoot is null) return;
        exit.Stop(); closing = false;
        if (popup.IsOpen) { RestartLifetime(); return; }
        UpdateGeometry();
        motionRoot.Opacity = 0;
        if (motionRoot.RenderTransform is TranslateTransform shift) shift.Y = 4;
        motionRoot.IsHitTestVisible = true;
        popup.IsOpen = true;
        RestartLifetime();
        if (!entryScheduled) { entryScheduled = true; CompositionTarget.Rendering += OnEntryFrame; }
    }
    private void OnEntryFrame(object? sender, object args)
    {
        CancelEntry();
        if (!IsLoaded || !IsOpen || popup?.IsOpen != true) return;
        AnimateSurface(true);
        Announce();
        Opened?.Invoke(this, EventArgs.Empty);
    }
    private void Announce()
    {
        if (!IsLoaded || !IsOpen || popup?.IsOpen != true || message is null) return;
        FrameworkElementAutomationPeer.CreatePeerForElement(message)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }
    private void RestartLifetime()
    {
        lifetime.Stop();
        if (!IsLoaded || !IsOpen || popup?.IsOpen != true || Duration <= 0) return;
        lifetime.Interval = TimeSpan.FromMilliseconds(Duration); lifetime.Start();
    }
    private void BeginClose()
    {
        lifetime.Stop(); CancelEntry();
        if (!IsLoaded || popup?.IsOpen != true || closing) return;
        closing = true;
        if (motionRoot is not null) motionRoot.IsHitTestVisible = false;
        AnimateSurface(false);
        exit.Start();
    }
    private void FinishClose()
    {
        exit.Stop();
        if (!closing) return;
        closing = false;
        StopSurfaceMotion();
        if (popup is not null) popup.IsOpen = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }
    private void CancelEntry()
    {
        if (entryScheduled) CompositionTarget.Rendering -= OnEntryFrame;
        entryScheduled = false;
    }
    private void StopPresentation(bool cancelExit = true)
    {
        lifetime.Stop(); CancelEntry(); StopSurfaceMotion(); StopCloseMotion();
        DisconnectWash();
        if (cancelExit) { exit.Stop(); closing = false; }
        if (popup is not null) popup.IsOpen = false;
    }
    private void DetachPresentation()
    {
        StopPresentation(cancelExit: false);
        geometry = null;
        if (close is not null) { close.Click -= OnCloseClicked; close.KeyDown -= OnCloseKeyDown; DisconnectCloseMotion(); }
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new AlertPeer(this);
    private sealed class AlertPeer(BleeckerAlert owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetClassNameCore() => nameof(BleeckerAlert);
        protected override string GetNameCore() => string.IsNullOrEmpty(base.GetNameCore()) ? owner.Message : base.GetNameCore();
    }
}
