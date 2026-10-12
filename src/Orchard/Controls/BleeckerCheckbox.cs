using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A native checkbox with Bleecker chrome and consumer-owned label/description content.</summary>
public sealed class BleeckerCheckbox : CheckBox
{
    private readonly UISettings settings = new();
    private readonly AccessibilitySettings accessibility = new();
    private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
    private readonly BleeckerCssTransition<(double L, double A, double B, double Alpha)> fillMotion = new(BleeckerColorMix.MixPremultiplied);
    private readonly BleeckerCssTransition<(double L, double A, double B, double Alpha)> strokeMotion = new(BleeckerColorMix.MixPremultiplied);
    private readonly BleeckerCssTransition<double> glyphMotion = new(Lerp), opacityMotion = new(Lerp), focusMotion = new(Lerp);
    private readonly SolidColorBrush fill = new(), stroke = new();
    private readonly BleeckerRichTextDefaults richDefaults = new();
    private readonly List<(DependencyObject Owner, DependencyProperty Property, long Token)> observers = [];
    private readonly TextBlock intrinsicLabel = new() { TextWrapping = TextWrapping.NoWrap, UseLayoutRounding = false };
    private readonly TextBlock intrinsicDescription = new() { TextWrapping = TextWrapping.NoWrap, UseLayoutRounding = false };
    private Border? box, focusRing, focusOffset;
    private Grid? inputChrome, textColumn;
    private Viewbox? glyph;
    private TextBlock? label, description;
    private ContentPresenter? richLabel, richDescription;
    private TextBlock[] probes = [];
    private VisualStateGroup? states;
    private string visualState = "";
    private bool pointerOver, rendering, richLayoutObserved;

    public BleeckerCheckbox()
    {
        DefaultStyleKey = typeof(BleeckerCheckbox);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/Checkbox.xaml") });
        RegisterPropertyChangedCallback(IsCheckedProperty, (_, _) => UpdateVisuals(true));
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => UpdateVisuals(true));
        RegisterPropertyChangedCallback(FocusStateProperty, (_, _) => UpdateVisuals(true));
        RegisterPropertyChangedCallback(ContentProperty, (_, _) => UpdateContent());
        RegisterPropertyChangedCallback(ContentTemplateProperty, (_, _) => UpdateContent());
        RegisterPropertyChangedCallback(ContentTemplateSelectorProperty, (_, _) => UpdateContent());
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); });
        RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => { if (Visibility != Visibility.Visible) UpdateVisuals(false); });
        PointerEntered += (_, _) => { pointerOver = true; UpdateVisuals(true); };
        PointerExited += (_, _) => { pointerOver = false; UpdateVisuals(true); };
        Loaded += (_, _) =>
        {
            settings.ColorValuesChanged += OnColorsChanged;
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged += OnMotionChanged;
            ObservePalette(); UpdateVisuals(false); ObserveRichLayout();
        };
        Unloaded += (_, _) =>
        {
            settings.ColorValuesChanged -= OnColorsChanged;
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= OnMotionChanged;
            StopMotion(); StopObservingPalette(); StopObservingRichLayout(); richDefaults.Clear(); pointerOver = false;
        };
    }

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(object), typeof(BleeckerCheckbox),
        new PropertyMetadata(null, (owner, _) => ((BleeckerCheckbox)owner).UpdateContent()));
    public object? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public static readonly DependencyProperty DescriptionTemplateProperty = DependencyProperty.Register(nameof(DescriptionTemplate), typeof(DataTemplate), typeof(BleeckerCheckbox),
        new PropertyMetadata(null, (owner, _) => ((BleeckerCheckbox)owner).UpdateContent()));
    public DataTemplate? DescriptionTemplate { get => (DataTemplate?)GetValue(DescriptionTemplateProperty); set => SetValue(DescriptionTemplateProperty, value); }

    protected override void OnApplyTemplate()
    {
        try { ApplyBleeckerTemplate(); }
        catch (Exception error)
        {
            // A native ApplyTemplate boundary can replace a managed exception
            // with a generic XamlParseException. Retain the original diagnostic.
            System.Diagnostics.Trace.TraceError("BleeckerCheckbox template application failed (Template null: {0}): {1}", Template is null, error);
            throw;
        }
    }

    private void ApplyBleeckerTemplate()
    {
        DetachTemplate();
        if (Template is null) return;
        base.OnApplyTemplate();
        box = Part<Border>("PART_Box"); inputChrome = Part<Grid>("PART_InputChrome"); glyph = Part<Viewbox>("PART_Glyph");
        focusRing = Part<Border>("PART_FocusRing"); focusOffset = Part<Border>("PART_FocusOffset");
        textColumn = Part<Grid>("PART_TextColumn"); label = Part<TextBlock>("PART_Label"); description = Part<TextBlock>("PART_Description");
        richLabel = Part<ContentPresenter>("PART_RichLabel"); richDescription = Part<ContentPresenter>("PART_RichDescription");
        probes = new[] { "PART_OffFill", "PART_OnFill", "PART_OffStroke", "PART_HoverStroke", "PART_OnStroke", "PART_FocusStroke" }.Select(Part<TextBlock>).ToArray();
        states = VisualStateManager.GetVisualStateGroups(Part<Grid>("PART_Root")).Single(group => group.Name == "CombinedStates");
        states.CurrentStateChanged += OnStateChanged;
        visualState = states.CurrentState?.Name ?? "";
        box.Background = fill; box.BorderBrush = stroke;
        UpdateContent();
        if (IsLoaded) ObservePalette();
        UpdateVisuals(false);
    }

    private void DetachTemplate()
    {
        StopMotion(); StopObservingPalette(); StopObservingRichLayout(); richDefaults.Clear();
        if (states is not null) states.CurrentStateChanged -= OnStateChanged;
        if (richLabel is not null) richLabel.Content = null;
        if (richDescription is not null) richDescription.Content = null;
        box = focusRing = focusOffset = null;
        inputChrome = textColumn = null;
        glyph = null; label = description = null; richLabel = richDescription = null;
        probes = []; states = null; visualState = ""; pointerOver = false;
    }

    private T Part<T>(string name) where T : FrameworkElement => GetTemplateChild(name) as T
        ?? throw new InvalidOperationException($"BleeckerCheckbox requires {name}.");
    private void OnStateChanged(object sender, VisualStateChangedEventArgs args) { visualState = args.NewState?.Name ?? ""; UpdateVisuals(true); }

    private void UpdateContent()
    {
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
        if (label is null || description is null || richLabel is null || richDescription is null || textColumn is null) return;
        richDefaults.Clear();
        var plainLabel = (Content is null or string) && ContentTemplate is null && ContentTemplateSelector is null;
        var plainDescription = (Description is null or string) && DescriptionTemplate is null;
        var hasLabel = plainLabel ? !string.IsNullOrEmpty(Content as string) : Content is not null || ContentTemplate is not null;
        var hasDescription = plainDescription ? !string.IsNullOrEmpty(Description as string) : Description is not null || DescriptionTemplate is not null;
        label.Text = plainLabel ? Content as string ?? "" : "";
        description.Text = plainDescription ? Description as string ?? "" : "";
        label.Visibility = plainLabel && hasLabel ? Visibility.Visible : Visibility.Collapsed;
        description.Visibility = plainDescription && hasDescription ? Visibility.Visible : Visibility.Collapsed;
        richLabel.Content = plainLabel ? null : Content;
        richLabel.ContentTemplate = ContentTemplate; richLabel.ContentTemplateSelector = ContentTemplateSelector;
        richDescription.Content = plainDescription ? null : Description; richDescription.ContentTemplate = DescriptionTemplate;
        richLabel.Visibility = !plainLabel && hasLabel ? Visibility.Visible : Visibility.Collapsed;
        richDescription.Visibility = !plainDescription && hasDescription ? Visibility.Visible : Visibility.Collapsed;
        textColumn.Visibility = hasLabel || hasDescription ? Visibility.Visible : Visibility.Collapsed;
        ObserveRichLayout();
        InvalidateMeasure();
    }

    private void ObserveRichLayout()
    {
        var needed = IsLoaded && (richLabel?.Visibility == Visibility.Visible || richDescription?.Visibility == Visibility.Visible);
        if (needed && !richLayoutObserved) { LayoutUpdated += OnRichLayout; richLayoutObserved = true; }
        else if (!needed) StopObservingRichLayout();
        if (needed) ApplyRichDefaults();
    }
    private void StopObservingRichLayout() { LayoutUpdated -= OnRichLayout; richLayoutObserved = false; }
    private void OnRichLayout(object? sender, object args) => ApplyRichDefaults();
    private void ApplyRichDefaults()
    {
        if (textColumn is not null) richDefaults.Apply(textColumn);
    }

    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size available)
    {
        if (label is null || description is null || textColumn is null) return base.MeasureOverride(available);
        if (textColumn.Visibility == Visibility.Collapsed) return base.MeasureOverride(new(20, available.Height));
        if (richLabel?.Visibility == Visibility.Visible || richDescription?.Visibility == Visibility.Visible) return base.MeasureOverride(available);
        var width = MathF.BitIncrement((float)(32 + Math.Max(MeasureText(intrinsicLabel, label), MeasureText(intrinsicDescription, description))));
        width = (float)Math.Min(available.Width, width);
        var measured = base.MeasureOverride(new(width, available.Height));
        return new(width, measured.Height);
    }
    private static double MeasureText(TextBlock measure, TextBlock visible)
    {
        measure.Text = visible.Text; measure.FontFamily = visible.FontFamily; measure.FontSize = visible.FontSize; measure.FontWeight = visible.FontWeight;
        measure.FontStyle = visible.FontStyle; measure.FontStretch = visible.FontStretch; measure.CharacterSpacing = visible.CharacterSpacing;
        measure.IsTextScaleFactorEnabled = visible.IsTextScaleFactorEnabled; measure.Language = visible.Language; measure.FlowDirection = visible.FlowDirection;
        measure.Measure(new(double.PositiveInfinity, double.PositiveInfinity));
        return measure.DesiredSize.Width;
    }
    private void OnColorsChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(() => UpdateVisuals(true));
    private void OnMotionChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) => DispatcherQueue.TryEnqueue(() => UpdateVisuals(false));
    private void ObservePalette()
    {
        StopObservingPalette();
        foreach (var probe in probes)
        {
            observers.Add((probe, TextBlock.ForegroundProperty, probe.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty, (_, _) => { ObservePalette(); UpdateVisuals(true); })));
            if (probe.Foreground is SolidColorBrush brush)
                foreach (var property in new[] { SolidColorBrush.ColorProperty, Brush.OpacityProperty })
                    observers.Add((brush, property, brush.RegisterPropertyChangedCallback(property, (_, _) => UpdateVisuals(true))));
        }
    }
    private void StopObservingPalette() { foreach (var observer in observers) observer.Owner.UnregisterPropertyChangedCallback(observer.Property, observer.Token); observers.Clear(); }
    private void StopMotion() { CompositionTarget.Rendering -= OnFrame; rendering = false; }
    private void UpdateVisuals(bool animate)
    {
        if (box is null || probes.Length != 6) return;
        var time = clock.Elapsed.TotalMilliseconds;
        var enabled = animate && IsLoaded && Visibility == Visibility.Visible && settings.AnimationsEnabled && !accessibility.HighContrast;
        var duration = BleeckerTokens.Durations["control"];
        var selected = IsChecked == true;
        fillMotion.Retarget(BleeckerColorMix.Premultiply(Sample(probes[selected ? 1 : 0].Foreground)), time, duration, enabled);
        strokeMotion.Retarget(BleeckerColorMix.Premultiply(Sample(probes[selected ? 4 : pointerOver || visualState.EndsWith("PointerOver", StringComparison.Ordinal) ? 3 : 2].Foreground)), time, duration, enabled);
        glyphMotion.Retarget(selected ? 1 : 0, time, duration, enabled);
        opacityMotion.Retarget(IsEnabled ? 1 : .5, time, duration, enabled);
        focusMotion.Retarget(FocusState == FocusState.Keyboard ? 1 : 0, time, duration, enabled);
        PaintFrame(time);
        if (IsMoving(time)) { if (!rendering) { CompositionTarget.Rendering += OnFrame; rendering = true; } }
        else StopMotion();
    }
    private bool IsMoving(double time) => fillMotion.IsRunning(time) || strokeMotion.IsRunning(time) || glyphMotion.IsRunning(time) || opacityMotion.IsRunning(time) || focusMotion.IsRunning(time);
    private void OnFrame(object? sender, object args) { var time = clock.Elapsed.TotalMilliseconds; PaintFrame(time); if (!IsMoving(time)) StopMotion(); }
    private void PaintFrame(double time)
    {
        if (box is null) return;
        Paint(fill, BleeckerColorMix.EncodePremultiplied(fillMotion.ValueAt(time)));
        Paint(stroke, BleeckerColorMix.EncodePremultiplied(strokeMotion.ValueAt(time)));
        glyph!.Opacity = glyphMotion.ValueAt(time); inputChrome!.Opacity = opacityMotion.ValueAt(time);
        focusRing!.BorderBrush = probes[5].Foreground; focusRing.Opacity = focusMotion.ValueAt(time);
        focusOffset!.Background = probes[0].Foreground; focusOffset.Opacity = focusRing.Opacity;
    }
    private static double Lerp(double from, double to, double progress) => from + (to - from) * progress;
    private static (byte R, byte G, byte B, double Alpha) Sample(Brush brush) { var solid = (SolidColorBrush)brush; return (solid.Color.R, solid.Color.G, solid.Color.B, solid.Color.A / 255d * solid.Opacity); }
    private static void Paint(SolidColorBrush brush, (byte R, byte G, byte B, double Alpha) color) { brush.Color = Windows.UI.Color.FromArgb(255, color.R, color.G, color.B); brush.Opacity = color.Alpha; }
    protected override AutomationPeer OnCreateAutomationPeer() => new CheckboxPeer(this);
    private sealed class CheckboxPeer(BleeckerCheckbox owner) : CheckBoxAutomationPeer(owner)
    {
        protected override string GetNameCore()
        {
            if (!string.IsNullOrEmpty(AutomationProperties.GetName(owner)) || AutomationProperties.GetLabeledBy(owner) is not null) return base.GetNameCore();
            var label = owner.Content is string plain ? plain : base.GetNameCore();
            var description = owner.Description is string text ? text : owner.Description is UIElement element ? FrameworkElementAutomationPeer.CreatePeerForElement(element)?.GetName() ?? "" : "";
            return string.Join(" ", new[] { label, description }.Where(value => !string.IsNullOrEmpty(value)));
        }
    }
}
