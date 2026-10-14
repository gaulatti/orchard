using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using System.Numerics;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A binary native switch with Bleecker track, thumb and optional label.</summary>
public sealed class BleeckerSwitch : Control
{
    private ToggleSwitch? action;
    private ResourceDictionary? nativePalette;
    private readonly UISettings settings = new();
    private readonly AccessibilitySettings accessibility = new();
    private readonly InputSystemCursor hand = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    private readonly InputSystemCursor prohibited = InputSystemCursor.Create(InputSystemCursorShape.UniversalNo);
    private readonly List<(DependencyObject Owner, DependencyProperty Property, long Token)> paletteObservers = [];
    private Border? track;
    private Microsoft.UI.Xaml.Shapes.Ellipse? thumb;
    private TextBlock? label;
    private readonly TextBlock intrinsicLabel = new() { TextWrapping = TextWrapping.NoWrap, UseLayoutRounding = false };
    private Grid? shadowHost;
    private TranslateTransform? translation;
    private TextBlock[] probes = [];
    private readonly SolidColorBrush trackFill = new();
    private readonly SolidColorBrush trackStroke = new();
    private readonly SolidColorBrush thumbFill = new();
    private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
    private readonly BleeckerCssTransition<double> thumbMotion = new((from, to, progress) => from + (to - from) * progress);
    private readonly BleeckerCssTransition<(double L, double A, double B, double Alpha)> fillMotion = new(BleeckerColorMix.MixPremultiplied);
    private readonly BleeckerCssTransition<(double L, double A, double B, double Alpha)> strokeMotion = new(BleeckerColorMix.MixPremultiplied);
    private bool rendering;

    public BleeckerSwitch()
    {
        DefaultStyleKey = typeof(BleeckerSwitch);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/Switch.xaml") });
        RegisterPropertyChangedCallback(AutomationProperties.NameProperty, (_, _) => UpdateAccessibleName());
        RegisterPropertyChangedCallback(AutomationProperties.LabeledByProperty, (_, _) => UpdateAccessibleName());
        RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => { if (Visibility != Visibility.Visible) UpdateVisuals(false); });
        IsEnabledChanged += (_, _) => ProtectedCursor = IsEnabled ? hand : prohibited;
        ProtectedCursor = hand;
        Loaded += (_, _) =>
        {
            settings.ColorValuesChanged += OnColorsChanged;
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged += OnMotionChanged;
            ObservePalette(); UpdateVisuals(false); UpdateShadow();
        };
        Unloaded += (_, _) =>
        {
            settings.ColorValuesChanged -= OnColorsChanged;
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= OnMotionChanged;
            StopMotion(); StopObservingPalette(); ClearShadow();
        };
    }

    public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(BleeckerSwitch),
        new PropertyMetadata(false, (owner, args) =>
        {
            var control = (BleeckerSwitch)owner;
            if (control.action is not null && control.action.IsOn != control.IsOn) control.action.IsOn = control.IsOn;
            control.UpdateVisuals(true);
            control.Toggled?.Invoke(control, new RoutedEventArgs());
        }));
    public bool IsOn { get => (bool)GetValue(IsOnProperty); set => SetValue(IsOnProperty, value); }
    public event RoutedEventHandler? Toggled;

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(BleeckerSwitch),
        new PropertyMetadata(string.Empty, (owner, _) =>
        {
            var control = (BleeckerSwitch)owner;
            control.UpdateLabel();
            control.UpdateAccessibleName();
            FrameworkElementAutomationPeer.FromElement(control)?.InvalidatePeer();
        }));
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    protected override void OnApplyTemplate()
    {
        StopMotion(); StopObservingPalette(); ClearShadow();
        if (action is not null)
        {
            action.Toggled -= OnNativeToggled;
            if (nativePalette is not null) action.Resources.MergedDictionaries.Remove(nativePalette);
        }
        base.OnApplyTemplate();
        action = GetTemplateChild("PART_Switch") as ToggleSwitch
            ?? throw new InvalidOperationException("BleeckerSwitch requires a native ToggleSwitch named PART_Switch.");
        nativePalette = new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/Switch.xaml") };
        action.Resources.MergedDictionaries.Add(nativePalette);
        action.ApplyTemplate();
        _ = Part<Microsoft.UI.Xaml.Controls.Primitives.Thumb>("SwitchThumb")
            ?? throw new InvalidOperationException("The native switch template requires its SwitchThumb pointer input part.");
        action.IsOn = IsOn;
        action.Toggled += OnNativeToggled;
        track = Part<Border>("PART_Track");
        thumb = Part<Microsoft.UI.Xaml.Shapes.Ellipse>("PART_Thumb");
        label = Part<TextBlock>("PART_Label");
        shadowHost = Part<Grid>("PART_ShadowHost");
        translation = Part<Grid>("SwitchKnob")?.RenderTransform as TranslateTransform;
        probes = new[] { "PART_OffFill", "PART_OnFill", "PART_OffStroke", "PART_OnStroke", "PART_OffThumb", "PART_OnThumb" }
            .Select(Part<TextBlock>).OfType<TextBlock>().ToArray();
        if (track is not null) { track.Background = trackFill; track.BorderBrush = trackStroke; }
        if (thumb is not null) thumb.Fill = thumbFill;
        UpdateLabel();
        UpdateAccessibleName();
        if (IsLoaded) ObservePalette();
        UpdateVisuals(false); UpdateShadow();
    }

    private T? Part<T>(string name) where T : FrameworkElement =>
        VisualTreeHelper.GetChildrenCount(action) > 0 ? ((FrameworkElement)VisualTreeHelper.GetChild(action, 0)).FindName(name) as T : null;
    private void OnNativeToggled(object sender, RoutedEventArgs args) { if (action!.IsOn != IsOn) IsOn = action.IsOn; }
    private void UpdateAccessibleName()
    {
        if (action is null) return;
        var explicitName = AutomationProperties.GetName(this);
        var labeledBy = AutomationProperties.GetLabeledBy(this);
        AutomationProperties.SetLabeledBy(action, labeledBy);
        AutomationProperties.SetName(action, !string.IsNullOrEmpty(explicitName) ? explicitName : labeledBy is not null ? "" : Label ?? "");
        var nativePeer = FrameworkElementAutomationPeer.CreatePeerForElement(action);
        nativePeer.EventsSource = FrameworkElementAutomationPeer.FromElement(this);
    }

    private void UpdateLabel() { if (label is not null) label.Visibility = string.IsNullOrEmpty(Label) ? Visibility.Collapsed : Visibility.Visible; }
    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        if (label is null) return base.MeasureOverride(availableSize);
        // Inline-flex prefers the full label width and wraps only when bounded.
        // A wrapping TextBlock in a star column can instead report its shortest
        // word width on first display, then clip after the final narrow arrange.
        intrinsicLabel.Text = Label ?? string.Empty;
        intrinsicLabel.FontFamily = label.FontFamily;
        intrinsicLabel.FontSize = label.FontSize;
        intrinsicLabel.FontWeight = label.FontWeight;
        intrinsicLabel.FontStyle = label.FontStyle;
        intrinsicLabel.FontStretch = label.FontStretch;
        intrinsicLabel.CharacterSpacing = label.CharacterSpacing;
        intrinsicLabel.IsTextScaleFactorEnabled = label.IsTextScaleFactorEnabled;
        intrinsicLabel.Language = label.Language;
        intrinsicLabel.FlowDirection = label.FlowDirection;
        intrinsicLabel.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var preferredWidth = 36 + (string.IsNullOrEmpty(Label) ? 0 : 12 + intrinsicLabel.DesiredSize.Width);
        // XAML's float-sized layout can round the row back below its measured
        // glyph advance after subtracting the track and margin. One outward
        // float step prevents an otherwise available final word from wrapping.
        if (!string.IsNullOrEmpty(Label)) preferredWidth = MathF.BitIncrement((float)preferredWidth);
        var width = Math.Min(availableSize.Width, preferredWidth);
        var measured = base.MeasureOverride(new Windows.Foundation.Size(width, availableSize.Height));
        return new Windows.Foundation.Size(width, measured.Height);
    }
    private void OnColorsChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(() => { UpdateVisuals(true); UpdateShadow(); });
    private void OnMotionChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) => DispatcherQueue.TryEnqueue(() => UpdateVisuals(false));

    private void ObservePalette()
    {
        StopObservingPalette();
        foreach (var probe in probes)
        {
            var token = probe.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty, (_, _) =>
            {
                ObservePalette(); UpdateVisuals(true);
            });
            paletteObservers.Add((probe, TextBlock.ForegroundProperty, token));
            if (probe.Foreground is SolidColorBrush brush)
                foreach (var property in new[] { SolidColorBrush.ColorProperty, Brush.OpacityProperty })
                    paletteObservers.Add((brush, property, brush.RegisterPropertyChangedCallback(property, (_, _) => UpdateVisuals(true))));
        }
    }
    private void StopObservingPalette()
    {
        foreach (var observer in paletteObservers) observer.Owner.UnregisterPropertyChangedCallback(observer.Property, observer.Token);
        paletteObservers.Clear();
    }
    private void StopMotion() { CompositionTarget.Rendering -= OnFrame; rendering = false; }

    private void UpdateVisuals(bool animate)
    {
        if (translation is null || probes.Length != 6) return;
        var time = clock.Elapsed.TotalMilliseconds;
        var enabled = animate && IsLoaded && Visibility == Visibility.Visible && settings.AnimationsEnabled && !accessibility.HighContrast;
        var colorDuration = BleeckerTokens.Durations["control"];
        fillMotion.Retarget(BleeckerColorMix.Premultiply(Sample(probes[IsOn ? 1 : 0].Foreground)), time, colorDuration, enabled);
        strokeMotion.Retarget(BleeckerColorMix.Premultiply(Sample(probes[IsOn ? 3 : 2].Foreground)), time, colorDuration, enabled);
        thumbMotion.Retarget(IsOn ? 16 : 0, time, BleeckerTokens.Durations["surface"], enabled);
        PaintFrame(time);
        if (IsMoving(time))
        {
            if (!rendering) { CompositionTarget.Rendering += OnFrame; rendering = true; }
        }
        else StopMotion();
    }
    private bool IsMoving(double time) => fillMotion.IsRunning(time) || strokeMotion.IsRunning(time) || thumbMotion.IsRunning(time);
    private void OnFrame(object? sender, object args)
    {
        var time = clock.Elapsed.TotalMilliseconds;
        PaintFrame(time);
        if (!IsMoving(time)) StopMotion();
    }
    private void PaintFrame(double time)
    {
        if (translation is null) return;
        translation.X = thumbMotion.ValueAt(time);
        Paint(trackFill, BleeckerColorMix.EncodePremultiplied(fillMotion.ValueAt(time)));
        Paint(trackStroke, BleeckerColorMix.EncodePremultiplied(strokeMotion.ValueAt(time)));
        Paint(thumbFill, Sample(probes[IsOn ? 5 : 4].Foreground));
    }
    private static (byte R, byte G, byte B, double Alpha) Sample(Brush brush)
    {
        var solid = (SolidColorBrush)brush;
        return (solid.Color.R, solid.Color.G, solid.Color.B, solid.Color.A / 255d * solid.Opacity);
    }
    private static void Paint(SolidColorBrush brush, (byte R, byte G, byte B, double Alpha) sample)
    {
        brush.Color = Windows.UI.Color.FromArgb(255, sample.R, sample.G, sample.B);
        brush.Opacity = sample.Alpha;
    }

    private void ClearShadow() { if (shadowHost is not null) ElementCompositionPreview.SetElementChildVisual(shadowHost, null); }
    private void UpdateShadow()
    {
        if (shadowHost is null || thumb is null) return;
        ClearShadow();
        shadowHost.Visibility = IsLoaded && !accessibility.HighContrast ? Visibility.Visible : Visibility.Collapsed;
        if (shadowHost.Visibility != Visibility.Visible) return;
        var compositor = ElementCompositionPreview.GetElementVisual(shadowHost).Compositor;
        var shadow = compositor.CreateDropShadow();
        shadow.BlurRadius = 2; shadow.Offset = new Vector3(0, 1, 0);
        shadow.Color = Windows.UI.Color.FromArgb(255, 26, 55, 77); shadow.Opacity = .18f;
        shadow.Mask = thumb.GetAlphaMask();
        var visual = compositor.CreateSpriteVisual(); visual.Size = new Vector2(16, 16); visual.Shadow = shadow;
        ElementCompositionPreview.SetElementChildVisual(shadowHost, visual);
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        var peer = new SwitchPeer(this);
        if (action is not null) FrameworkElementAutomationPeer.CreatePeerForElement(action).EventsSource = peer;
        return peer;
    }
    private sealed class SwitchPeer(BleeckerSwitch owner) : FrameworkElementAutomationPeer(owner), IToggleProvider
    {
        private AutomationPeer NativePeer => FrameworkElementAutomationPeer.CreatePeerForElement(owner.action
            ?? throw new InvalidOperationException("The switch template is not applied."));
        private IToggleProvider NativeToggle => (IToggleProvider)NativePeer.GetPattern(PatternInterface.Toggle);
        public ToggleState ToggleState => NativeToggle.ToggleState;
        public void Toggle() => NativeToggle.Toggle();
        protected override string GetClassNameCore() => nameof(BleeckerSwitch);
        protected override AutomationControlType GetAutomationControlTypeCore() => NativePeer.GetAutomationControlType();
        protected override string GetLocalizedControlTypeCore() => NativePeer.GetLocalizedControlType();
        // UIA queries these visibility contracts while XAML constructs the
        // tree, before PART_Switch exists. Use this public owner's framework
        // metadata, which also preserves an explicit accessibility view.
        protected override bool IsControlElementCore() => base.IsControlElementCore();
        protected override bool IsContentElementCore() => base.IsContentElementCore();
        protected override object GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.Toggle ? this : base.GetPatternCore(pattern);
        protected override IList<AutomationPeer> GetChildrenCore() => null!;
        protected override bool IsKeyboardFocusableCore() => NativePeer.IsKeyboardFocusable();
        protected override bool HasKeyboardFocusCore() => NativePeer.HasKeyboardFocus();
        protected override void SetFocusCore() => NativePeer.SetFocus();
        protected override string GetNameCore() => !string.IsNullOrEmpty(AutomationProperties.GetName(owner)) || AutomationProperties.GetLabeledBy(owner) is not null
            ? base.GetNameCore() : owner.Label ?? string.Empty;
    }
}
