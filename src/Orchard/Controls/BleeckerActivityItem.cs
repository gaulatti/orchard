using System.Diagnostics;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A consumer-owned activity summary with an Avatar and exact title/time strings.</summary>
public sealed class BleeckerActivityItem : Control
{
    private readonly UISettings settings = new();
    private readonly AccessibilitySettings accessibility = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly BleeckerCssTransition<(double L, double A, double B, double Alpha)> motion = new(BleeckerColorMix.MixPremultiplied);
    private readonly SolidColorBrush paint = new();
    private readonly List<(DependencyObject Owner, DependencyProperty Property, long Token)> observers = [];
    private Border? surface;
    private BleeckerAvatar? avatar;
    private TextBlock? title, time, hover, hoverText, timeText;
    private bool over, running;
    private ElementTheme? renderedTheme;
    private bool? renderedContrast;

    public BleeckerActivityItem()
    {
        DefaultStyleKey = typeof(BleeckerActivityItem);
        Loaded += (_, _) =>
        {
            ValidateContent();
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
            settings.ColorValuesChanged += OnColorsChanged;
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged += OnAnimationsChanged;
            CompositionTarget.Rendering += OnFrame; ObservePalette(); UpdateContent(); UpdateAppearance(false);
        };
        Unloaded += (_, _) =>
        {
            settings.ColorValuesChanged -= OnColorsChanged;
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= OnAnimationsChanged;
            CompositionTarget.Rendering -= OnFrame; StopObserving(); over = false; UpdateAppearance(false);
        };
        PointerEntered += (_, _) => { if (IsEnabled) { over = true; UpdateAppearance(true); } };
        PointerExited += (_, args) =>
        {
            var point = args.GetCurrentPoint(this).Position;
            if (point.X >= 0 && point.Y >= 0 && point.X < ActualWidth && point.Y < ActualHeight) return;
            over = false; UpdateAppearance(true);
        };
        ActualThemeChanged += (_, _) => DispatcherQueue.TryEnqueue(() => { if (IsLoaded) UpdateAppearance(true); });
        RegisterPropertyChangedCallback(BackgroundProperty, (_, _) => { ObservePalette(); UpdateAppearance(true); });
        RegisterPropertyChangedCallback(ForegroundProperty, (_, _) => UpdateAppearance(false));
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => { if (!IsEnabled) over = false; UpdateAppearance(false); });
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); });
    }
    public static readonly DependencyProperty InitProperty = DependencyProperty.Register(nameof(Init), typeof(string), typeof(BleeckerActivityItem), new PropertyMetadata(null, OnContentChanged));
    public string? Init { get => (string?)GetValue(InitProperty); set => SetValue(InitProperty, value); }
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(BleeckerActivityItem), new PropertyMetadata(null, OnContentChanged));
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public static readonly DependencyProperty TimeProperty = DependencyProperty.Register(nameof(Time), typeof(string), typeof(BleeckerActivityItem), new PropertyMetadata(null, OnContentChanged));
    public string? Time { get => (string?)GetValue(TimeProperty); set => SetValue(TimeProperty, value); }
    private static void OnContentChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        var control = (BleeckerActivityItem)owner; if (control.IsLoaded) control.ValidateContent(); control.UpdateContent();
    }
    private void ValidateContent() { if (Init is null || Title is null || Time is null) throw new InvalidOperationException("ActivityItem Init, Title and Time must resolve while mounted."); }
    protected override void OnApplyTemplate()
    {
        DetachTemplate(); base.OnApplyTemplate(); if (Template is null) return;
        surface = Part<Border>("PART_Surface"); avatar = Part<BleeckerAvatar>("PART_Avatar"); title = Part<TextBlock>("PART_Title"); time = Part<TextBlock>("PART_Time");
        hover = Part<TextBlock>("PART_Hover"); hoverText = Part<TextBlock>("PART_HoverText"); timeText = Part<TextBlock>("PART_TimeText"); surface.Background = paint;
        ObservePalette(); UpdateContent(); UpdateAppearance(false);
    }
    private T Part<T>(string name) where T : FrameworkElement => GetTemplateChild(name) as T ?? throw new InvalidOperationException($"ActivityItem requires {name}.");
    private void UpdateContent()
    {
        if (avatar is null || title is null || time is null) return;
        avatar.Fallback = Init; title.Text = Title ?? ""; time.Text = Time ?? "";
        // Empty source paragraphs have no line boxes, but retain their sibling gap.
        title.Visibility = string.IsNullOrEmpty(Title) ? Visibility.Collapsed : Visibility.Visible;
        time.Visibility = string.IsNullOrEmpty(Time) ? Visibility.Collapsed : Visibility.Visible;
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private void UpdateAppearance(bool animate)
    {
        if (surface is null || title is null || time is null || hover is null || hoverText is null || timeText is null) return;
        renderedTheme = ActualTheme; var contrast = accessibility.HighContrast; renderedContrast = contrast;
        var target = over ? hover.Foreground : Background;
        (byte R, byte G, byte B, double Alpha) color = target is null ? ((byte)0, (byte)0, (byte)0, 0d) : target is SolidColorBrush brush
            ? (brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A / 255d * brush.Opacity)
            : throw new InvalidOperationException("ActivityItem background color transitions require a SolidColorBrush.");
        var milliseconds = clock.Elapsed.TotalMilliseconds;
        motion.Retarget(BleeckerColorMix.Premultiply(color), milliseconds, BleeckerTokens.Durations["surface"], animate && IsLoaded && settings.AnimationsEnabled && !contrast);
        running = motion.IsRunning(milliseconds); Paint(milliseconds);
        title.Foreground = contrast && over ? hoverText.Foreground : Foreground;
        time.Foreground = contrast ? (over ? hoverText.Foreground : Foreground) : timeText.Foreground;
    }
    private void Paint(double milliseconds)
    {
        var color = BleeckerColorMix.EncodePremultiplied(motion.ValueAt(milliseconds));
        paint.Color = Windows.UI.Color.FromArgb(255, color.R, color.G, color.B); paint.Opacity = color.Alpha;
    }
    private void OnFrame(object? sender, object args)
    {
        if (renderedTheme != ActualTheme || renderedContrast != accessibility.HighContrast) UpdateAppearance(true);
        if (running) { var milliseconds = clock.Elapsed.TotalMilliseconds; Paint(milliseconds); running = motion.IsRunning(milliseconds); }
    }
    private void ObservePalette()
    {
        StopObserving(); if (!IsLoaded) return;
        foreach (var probe in new[] { hover, hoverText, timeText }.OfType<TextBlock>())
        {
            observers.Add((probe, TextBlock.ForegroundProperty, probe.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty, (_, _) => { ObservePalette(); UpdateAppearance(true); })));
            if (probe.Foreground is SolidColorBrush brush) foreach (var property in new[] { SolidColorBrush.ColorProperty, Brush.OpacityProperty })
                observers.Add((brush, property, brush.RegisterPropertyChangedCallback(property, (_, _) => UpdateAppearance(true))));
        }
        if (Background is SolidColorBrush background) foreach (var property in new[] { SolidColorBrush.ColorProperty, Brush.OpacityProperty })
            observers.Add((background, property, background.RegisterPropertyChangedCallback(property, (_, _) => UpdateAppearance(true))));
    }
    private void StopObserving() { foreach (var observer in observers) observer.Owner.UnregisterPropertyChangedCallback(observer.Property, observer.Token); observers.Clear(); }
    private void OnColorsChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(() => UpdateAppearance(true));
    private void OnAnimationsChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) => DispatcherQueue.TryEnqueue(() => UpdateAppearance(false));
    private void DetachTemplate() { StopObserving(); surface = null; avatar = null; title = time = hover = hoverText = timeText = null; running = false; }
    protected override AutomationPeer OnCreateAutomationPeer() => new ActivityPeer(this);
    private sealed class ActivityPeer(BleeckerActivityItem owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerActivityItem);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override bool IsKeyboardFocusableCore() => false;
    }
}
