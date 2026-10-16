using System.Numerics;
using System.Text.RegularExpressions;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A viewport loading portal. Pointer input is blocked; existing keyboard focus is retained.</summary>
public sealed class BleeckerLoadingOverlay : Control
{
    private Popup? popup;
    private Canvas? portal, body, labelHost;
    private Grid? backdrop;
    private Border? tint;
    private BleeckerOverlaySurface? card;
    private BleeckerLoadingSpinner? spinner;
    private TextBlock? label;
    private readonly TextBlock measure = new() { UseLayoutRounding = false };
    private readonly TextBlock metrics = new() { Text = "Mg", UseLayoutRounding = false };
    private readonly TranslateTransform labelShift = new();
    private readonly TranslateTransform portalShift = new();
    private readonly UISettings settings = new();
    private readonly AccessibilitySettings accessibility = new();
    private readonly List<IDisposable> effects = [];
    private SpriteVisual? blurVisual;
    private XamlRoot? observedRoot;
    private bool updating;
    private bool trackingPresentation;
    private object? lastLayout;

    public BleeckerLoadingOverlay()
    {
        DefaultStyleKey = typeof(BleeckerLoadingOverlay);
        Loaded += OnLoaded; Unloaded += OnUnloaded; LayoutUpdated += (_, _) => Present();
        ActualThemeChanged += (_, _) => Present();
        RegisterPropertyChangedCallback(AutomationProperties.NameProperty, (_, _) => UpdateAccessibleName());
        RegisterPropertyChangedCallback(AutomationProperties.LabeledByProperty, (_, _) => UpdateAccessibleName());
        foreach (var property in new[] { FontFamilyProperty, FontSizeProperty, FontWeightProperty, CharacterSpacingProperty, IsTextScaleFactorEnabledProperty, FlowDirectionProperty, VisibilityProperty, PaddingProperty, BorderThicknessProperty })
            RegisterPropertyChangedCallback(property, (_, _) => Present());
    }

    public static readonly DependencyProperty IsVisibleProperty = DependencyProperty.Register(nameof(IsVisible), typeof(bool), typeof(BleeckerLoadingOverlay), new PropertyMetadata(false, OnChanged));
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(BleeckerLoadingOverlay), new PropertyMetadata("Loading...", OnChanged));
    public static readonly DependencyProperty OverlayBrushProperty = DependencyProperty.Register(nameof(OverlayBrush), typeof(Brush), typeof(BleeckerLoadingOverlay), new PropertyMetadata(null));
    public bool IsVisible { get => (bool)GetValue(IsVisibleProperty); set => SetValue(IsVisibleProperty, value); }
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public Brush OverlayBrush { get => (Brush)GetValue(OverlayBrushProperty); set => SetValue(OverlayBrushProperty, value); }
    private static void OnChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        var overlay = (BleeckerLoadingOverlay)owner; overlay.UpdateAccessibleName(); overlay.Present();
    }

    protected override void OnApplyTemplate()
    {
        if (portal is not null) portal.PointerPressed -= OnPortalPointerPressed;
        Close();
        base.OnApplyTemplate();
        popup = GetTemplateChild("PART_Popup") as Popup;
        portal = GetTemplateChild("PART_Portal") as Canvas;
        backdrop = GetTemplateChild("PART_Backdrop") as Grid;
        tint = GetTemplateChild("PART_Tint") as Border;
        card = GetTemplateChild("PART_Card") as BleeckerOverlaySurface;
        body = GetTemplateChild("PART_Body") as Canvas;
        spinner = GetTemplateChild("PART_Spinner") as BleeckerLoadingSpinner;
        label = GetTemplateChild("PART_Label") as TextBlock;
        labelHost = GetTemplateChild("PART_LabelHost") as Canvas;
        if (label is not null) label.RenderTransform = labelShift;
        if (portal is not null) { portal.RenderTransform = portalShift; portal.PointerPressed += OnPortalPointerPressed; }
        UpdateAccessibleName();
        Present();
    }

    private void UpdateAccessibleName()
    {
        if (spinner is null) return;
        var explicitName = AutomationProperties.GetName(this); var associatedLabel = AutomationProperties.GetLabeledBy(this);
        AutomationProperties.SetLabeledBy(spinner, associatedLabel);
        AutomationProperties.SetName(spinner, !string.IsNullOrEmpty(explicitName) ? explicitName : associatedLabel is not null ? "" : Label ?? "");
    }

    private void OnPortalPointerPressed(object sender, PointerRoutedEventArgs args)
    {
        // HTML moves focus to BODY when this non-interactive surface is hit.
        // WinUI needs a temporary focusable root to produce the same next-Tab
        // behavior. The lease outlives the overlay if the caller hides it while
        // the root still owns focus, and releases on the next real focus move.
        var root = XamlRoot; var target = root.Content;
        if (ReferenceEquals(FocusManager.GetFocusedElement(root), target)) return;
        if (target.IsTabStop) { target.Focus(FocusState.Pointer); return; }
        var originalTabStop = target.IsTabStop;
        var local = target.ReadLocalValue(IsTabStopProperty);
        var element = target as FrameworkElement; var binding = element?.GetBindingExpression(IsTabStopProperty)?.ParentBinding;
        target.IsTabStop = true;
        RoutedEventHandler? lost = null;
        lost = (_, _) =>
        {
            if (ReferenceEquals(FocusManager.GetFocusedElement(root), target)) return;
            target.LostFocus -= lost; RestoreTabStop();
        };
        target.LostFocus += lost;
        if (!target.Focus(FocusState.Pointer)) { target.LostFocus -= lost; RestoreTabStop(); throw new InvalidOperationException("The loading overlay requires a focusable XAML root for pointer focus clearing."); }
        void RestoreTabStop()
        {
            if (binding is not null) element!.SetBinding(IsTabStopProperty, binding);
            else if (ReferenceEquals(local, DependencyProperty.UnsetValue))
            {
                target.ClearValue(IsTabStopProperty);
                // A Page used as Window.Content has an effective non-tab-stop
                // root default. ClearValue can instead restore Control's true
                // metadata default. Preserve the captured root value in that case.
                if (target.IsTabStop != originalTabStop) target.IsTabStop = originalTabStop;
            }
            else target.SetValue(IsTabStopProperty, local);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        DetachObservers();
        observedRoot = XamlRoot; observedRoot.Changed += OnRootChanged;
        settings.ColorValuesChanged += OnSettingsChanged;
        settings.TextScaleFactorChanged += OnTextScaleChanged;
        Present();
    }
    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        // A queued Unloaded notification may follow a synchronous reparent.
        if (IsLoaded) return;
        DetachObservers(); Close();
    }
    private void DetachObservers()
    {
        if (observedRoot is not null) observedRoot.Changed -= OnRootChanged;
        observedRoot = null; settings.ColorValuesChanged -= OnSettingsChanged; settings.TextScaleFactorChanged -= OnTextScaleChanged;
    }
    private void OnRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => Present();
    private void OnSettingsChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(Present);
    private void OnTextScaleChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(Present);

    private void Close()
    {
        if (trackingPresentation) { CompositionTarget.Rendering -= OnRendering; trackingPresentation = false; }
        if (popup is not null) popup.IsOpen = false;
        if (backdrop is not null) ElementCompositionPreview.SetElementChildVisual(backdrop, null);
        for (var i = effects.Count - 1; i >= 0; i--) effects[i].Dispose();
        effects.Clear(); blurVisual = null;
        lastLayout = null;
    }

    private void Present()
    {
        if (updating) return;
        if (!IsLoaded || !IsVisible || Visibility != Visibility.Visible) { Close(); return; }
        if (popup is null || portal is null || backdrop is null || tint is null || card is null || body is null || spinner is null || label is null || labelHost is null) return;
        updating = true;
        try
        {
            var root = XamlRoot; var size = root.Size;
            var origin = TransformToVisual(root.Content).TransformPoint(new Point());
            var layout = (size, origin, ActualTheme, Label, FontFamily, FontSize, FontWeight, CharacterSpacing, IsTextScaleFactorEnabled, FlowDirection, Padding, BorderThickness, root.RasterizationScale, settings.TextScaleFactor, accessibility.HighContrast);
            if (Equals(lastLayout, layout)) return;
            lastLayout = layout;
            popup.XamlRoot = root; portal.RequestedTheme = ActualTheme;
            portal.Width = backdrop.Width = tint.Width = size.Width; portal.Height = backdrop.Height = tint.Height = size.Height;
            // A template Popup's origin is the templated control. Cancel every
            // ancestor offset, including transforms, to establish the portal.
            var scale = root.RasterizationScale;
            popup.HorizontalOffset = Math.Round(-origin.X * scale) / scale;
            popup.VerticalOffset = Math.Round(-origin.Y * scale) / scale;
            portalShift.X = -origin.X - popup.HorizontalOffset; portalShift.Y = -origin.Y - popup.VerticalOffset;
            label.Text = Regex.Replace(Label ?? "", "[ \\t\\r\\n\\f]+", " ").Trim(' ');
            var hasLabel = !string.IsNullOrEmpty(Label);
            labelHost.Visibility = hasLabel ? Visibility.Visible : Visibility.Collapsed;
            label.FlowDirection = FlowDirection;
            foreach (var text in new[] { label, measure, metrics })
            {
                text.FontFamily = FontFamily; text.FontSize = FontSize; text.FontWeight = FontWeight;
                text.CharacterSpacing = CharacterSpacing; text.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled;
            }
            label.LineHeight = FontSize * 20 / 14;
            measure.Text = label.Text; measure.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var preferred = measure.DesiredSize.Width; var minimum = 0d;
            foreach (var word in label.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            { measure.Text = word; measure.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); minimum = Math.Max(minimum, measure.DesiredSize.Width); }
            var horizontal = Padding.Left + Padding.Right + BorderThickness.Left + BorderThickness.Right;
            var vertical = Padding.Top + Padding.Bottom + BorderThickness.Top + BorderThickness.Bottom;
            var width = Math.Max(48 + horizontal, Math.Max(minimum + horizontal, Math.Min(size.Width, preferred + horizontal)));
            var textWidth = Math.Min(preferred, width - horizontal);
            label.Height = double.NaN;
            label.Width = textWidth; label.Measure(new Size(textWidth, double.PositiveInfinity));
            var textHeight = label.Text.Length == 0 ? 0 : label.DesiredSize.Height;
            label.Height = textHeight;
            labelHost.Width = textWidth; labelHost.Height = textHeight;
            card.Width = width; card.Height = 48 + vertical + (hasLabel ? 12 + textHeight : 0);
            body.Width = width - horizontal; body.Height = card.Height - vertical;
            spinner.Width = spinner.Height = 48;
            Canvas.SetLeft(spinner, (body.Width - 48) / 2); Canvas.SetTop(spinner, 0);
            Canvas.SetLeft(labelHost, (body.Width - textWidth) / 2); Canvas.SetTop(labelHost, 60);
            Canvas.SetLeft(card, (size.Width - card.Width) / 2); Canvas.SetTop(card, (size.Height - card.Height) / 2);
            metrics.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var factor = IsTextScaleFactorEnabled ? settings.TextScaleFactor : 1;
            var ascent = Math.Floor(metrics.BaselineOffset * scale + .5) / scale;
            var descent = Math.Floor((metrics.DesiredSize.Height - metrics.BaselineOffset) * scale + .5) / scale;
            labelShift.Y = Math.Floor(((label.LineHeight * factor - ascent - descent) / 2 + ascent) * scale) / scale - label.BaselineOffset;
            popup.IsOpen = true;
            UpdateBlur(size);
            // Commit a changed native Popup anchor before this frame is drawn.
            // Render-only ancestor motion does not otherwise run a layout pass.
            portal.UpdateLayout();
            if (!trackingPresentation) { CompositionTarget.Rendering += OnRendering; trackingPresentation = true; }
        }
        finally { updating = false; }
    }

    private void OnRendering(object? sender, object args) => Present();

    private void UpdateBlur(Size size)
    {
        if (backdrop is null) return;
        if (accessibility.HighContrast)
        {
            ElementCompositionPreview.SetElementChildVisual(backdrop, null);
            return;
        }
        if (blurVisual is null)
        {
            var compositor = ElementCompositionPreview.GetElementVisual(backdrop).Compositor;
            var source = compositor.CreateBackdropBrush();
            using var blur = new GaussianBlurEffect { BlurAmount = 2, BorderMode = EffectBorderMode.Hard, Optimization = EffectOptimization.Quality, Source = new CompositionEffectSourceParameter("backdrop") };
            var factory = compositor.CreateEffectFactory(blur); var brush = factory.CreateBrush(); brush.SetSourceParameter("backdrop", source);
            blurVisual = compositor.CreateSpriteVisual(); blurVisual.Brush = brush;
            effects.Add(source); effects.Add(factory); effects.Add(brush); effects.Add(blurVisual);
        }
        blurVisual.Size = new Vector2((float)size.Width, (float)size.Height);
        ElementCompositionPreview.SetElementChildVisual(backdrop, blurVisual);
    }
}
