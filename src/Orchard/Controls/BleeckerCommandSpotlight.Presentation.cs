using System.Diagnostics;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

public sealed partial class BleeckerCommandSpotlight
{
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly UISettings settings = new();
    private readonly BleeckerCssTransition<double> transition = new((a, b, t) => a + (b - a) * t);
    private Grid? overlay, backdrop, blurHost;
    private Portal? portal;
    private FocusSurface? focusSurface;
    private BleeckerOverlaySurface? paint;
    private Body? body;
    private BackdropPaint? backdropPaint;
    private SelectedFill? selectedFill;
    private ResourceDictionary? styles;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? presenceTimer;
    private bool present, desiredOpen, visible, subscribedAnimations;
    private double visibleAt, removeAt, animationEnd;
    private readonly List<IDisposable> blurResources = [];
    private SpriteVisual? blurVisual;
    private readonly CompositeTransform surfaceTransform = new();
    private static readonly string[] PaletteNames = ["Primary", "Secondary", "Surface", "Border", "HeaderBorder", "SelectedBackground", "SelectedForeground", "RowHover", "IconBackground", "IconBorder", "TriggerBackground", "TriggerBorder", "TriggerHover", "TriggerHoverBorder", "Focus", "Backdrop"];
    private void UpdatePresence()
    {
        if (!subscribedAnimations && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) { settings.AnimationsEnabledChanged += AnimationsChanged; subscribedAnimations = true; }
        var now = clock.Elapsed.TotalMilliseconds;
        if (IsOpen != desiredOpen)
        {
            desiredOpen = IsOpen;
            if (desiredOpen) { present = true; visibleAt = now + 20; if (overlay is null) MountPresentation(); else if (registration is { Layer.Active: false, IsAttached: true }) registration.Host.Activate(registration); }
            else { visible = false; removeAt = now + 160; Retarget(false, now); if (registration is { Layer.Active: true, IsAttached: true }) registration.Host.Deactivate(registration); }
        }
        AdvancePresentation();
    }
    private void AnimationsChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) => DispatcherQueue.TryEnqueue(() => { if (present) { Retarget(visible, clock.Elapsed.TotalMilliseconds); AdvancePresentation(); } });
    private void Retarget(bool show, double now)
    {
        var duration = settings.AnimationsEnabled ? show ? 190d : 160d : .01;
        transition.Retarget(show ? 1 : 0, now, duration, true, progress => BleeckerAlertDialogModel.SurfaceTiming(show, progress)); animationEnd = now + duration;
    }
    private void AdvancePresentation()
    {
        if (!present || overlay is null || focusSurface is null || backdrop is null) return;
        var now = clock.Elapsed.TotalMilliseconds;
        if (desiredOpen && !visible && now >= visibleAt) { visible = true; Retarget(true, now); }
        if (!desiredOpen && now >= removeAt) { DetachPresentation(); return; }
        var value = transition.ValueAt(now); focusSurface.Opacity = backdrop.Opacity = value;
        surfaceTransform.TranslateY = -4 * Rem * (1 - value); surfaceTransform.ScaleX = surfaceTransform.ScaleY = .992 + .008 * value;
        overlay.IsHitTestVisible = desiredOpen; focusSurface.IsEnabled = desiredOpen && DeclarationEnabled;
        if (desiredOpen && visible && now >= animationEnd) { presenceTimer?.Stop(); return; }
        presenceTimer ??= DispatcherQueue.CreateTimer(); presenceTimer.IsRepeating = false; presenceTimer.Tick -= PresenceTick; presenceTimer.Tick += PresenceTick; presenceTimer.Interval = TimeSpan.FromMilliseconds(15); presenceTimer.Start();
    }
    private void PresenceTick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args) => AdvancePresentation();
    private void MountPresentation()
    {
        host = BleeckerModalHost.Require(this);
        styles = new() { Source = new Uri("ms-appx:///Orchard/Themes/CommandSpotlight.xaml") };
        selectedFill = new(palette["SelectedBackground"]);
        body = new(this);
        paint = new() { Name = "SpotlightPaint", Content = body, Style = SurfaceStyle ?? (Style)styles["BleeckerCommandSpotlightSurfaceStyle"], HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch, UseLayoutRounding = false };
        focusSurface = new(this) { Name = "SpotlightSurface", Content = paint, IsTabStop = false, UseSystemFocusVisuals = false, HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch, UseLayoutRounding = false, RenderTransform = surfaceTransform, RenderTransformOrigin = new(.5, .5), TabFocusNavigation = KeyboardNavigationMode.Cycle };
        AutomationProperties.SetName(focusSurface, "Command spotlight"); AutomationProperties.SetIsDialog(focusSurface, true);
        blurHost = new(); backdrop = new() { Name = "SpotlightBackdrop", Background = Brush("Backdrop"), UseLayoutRounding = false }; backdrop.Children.Add(blurHost);
        // Blur belongs behind the scrim, not above its black tint.
        var tint = new Border(); backdropPaint = new(palette["Backdrop"], tint); backdrop.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent); backdrop.Children.Add(tint); backdrop.Tapped += BackdropTapped;
        portal = new(this) { UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight }; portal.Children.Add(backdrop); portal.Children.Add(focusSurface);
        overlay = new() { Name = "SpotlightOverlay", UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight, Style = OverlayStyle }; overlay.Children.Add(portal);
        body.Update(); registration = host.Attach(this, focusSurface, overlay, Close); host.Activate(registration);
        UpdatePresentation();
    }
    private void BackdropTapped(object sender, TappedRoutedEventArgs args) { Close(); args.Handled = true; }
    private void UpdatePresentation()
    {
        if (paint is null || focusSurface is null || overlay is null || styles is null) return;
        var standard = (Style)styles["BleeckerCommandSpotlightSurfaceStyle"];
        paint.Style = SurfaceStyle ?? standard;
        // A local default otherwise masks a style applied after the surface opened.
        if (CallerStyleSets(SurfaceStyle, MaxWidthProperty)) paint.ClearValue(MaxWidthProperty);
        else paint.MaxWidth = 672 * Rem;
        overlay.Style = OverlayStyle; focusSurface.RequestedTheme = ActualTheme;
        paint.FlowDirection = body!.FlowDirection = FlowDirection;
        backdropPaint?.Refresh();
        selectedFill?.Refresh();
    }
    private void UpdateBlur(Size size)
    {
        if (blurHost is null) return;
        if (blurVisual is null)
        {
            var compositor = ElementCompositionPreview.GetElementVisual(blurHost).Compositor; var source = compositor.CreateBackdropBrush();
            using var blur = new GaussianBlurEffect { BlurAmount = (float)(4 * Rem), BorderMode = EffectBorderMode.Hard, Optimization = EffectOptimization.Quality, Source = new CompositionEffectSourceParameter("backdrop") };
            var factory = compositor.CreateEffectFactory(blur); var brush = factory.CreateBrush(); brush.SetSourceParameter("backdrop", source); blurVisual = compositor.CreateSpriteVisual(); blurVisual.Brush = brush;
            blurResources.AddRange([source, factory, brush, blurVisual]); ElementCompositionPreview.SetElementChildVisual(blurHost, blurVisual);
        }
        blurVisual.Size = new((float)size.Width, (float)size.Height);
    }
    private void DetachPresentation()
    {
        presenceTimer?.Stop(); if (presenceTimer is not null) presenceTimer.Tick -= PresenceTick; presenceTimer = null;
        registration?.Dispose(); registration = null;
        if (backdrop is not null) backdrop.Tapped -= BackdropTapped;
        backdropPaint?.Dispose(); backdropPaint = null;
        selectedFill?.Dispose(); selectedFill = null;
        if (blurHost is not null) ElementCompositionPreview.SetElementChildVisual(blurHost, null); for (var i = blurResources.Count - 1; i >= 0; i--) blurResources[i].Dispose(); blurResources.Clear(); blurVisual = null;
        body?.Dispose(); if (paint is not null) paint.Content = null; if (focusSurface is not null) focusSurface.Content = null; portal?.Children.Clear(); overlay?.Children.Clear();
        body = null; paint = null; focusSurface = null; portal = null; overlay = null; backdrop = null; blurHost = null; styles = null;
        present = desiredOpen = visible = false; transition.Retarget(0, clock.Elapsed.TotalMilliseconds, 0, false);
        if (subscribedAnimations && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= AnimationsChanged; subscribedAnimations = false;
    }
    private sealed partial class Portal(BleeckerCommandSpotlight owner) : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            owner.UpdatePresentation(); var width = Math.Max(2, Math.Min(owner.paint!.MaxWidth, availableSize.Width - 32 * owner.Rem));
            owner.body!.MaximumListHeight = .55 * (owner.XamlRoot?.Size.Height ?? availableSize.Height);
            owner.focusSurface!.Measure(new(width, double.PositiveInfinity)); owner.backdrop!.Measure(availableSize); owner.UpdateBlur(availableSize); return availableSize;
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            owner.backdrop!.Arrange(new(0, 0, finalSize.Width, finalSize.Height)); var size = owner.focusSurface!.DesiredSize;
            owner.focusSurface.Arrange(new(Math.Max(0, (finalSize.Width - size.Width) / 2), double.IsNaN(owner.OverlayTopOffset) ? Math.Floor(.12 * (owner.XamlRoot?.Size.Height ?? finalSize.Height) * 64) / 64 : owner.OverlayTopOffset, size.Width, size.Height)); return finalSize;
        }
    }
    private sealed class FocusSurface(BleeckerCommandSpotlight owner) : ContentControl
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new SurfacePeer(this, owner);
    }
    private sealed class SurfacePeer(FocusSurface surface, BleeckerCommandSpotlight owner) : FrameworkElementAutomationPeer(surface), IWindowProvider
    {
        protected override string GetClassNameCore() => nameof(BleeckerCommandSpotlight);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;
        protected override bool IsDialogCore() => true;
        protected override string GetNameCore() => "Command spotlight";
        protected override object GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.Window ? this : base.GetPatternCore(pattern);
        public bool IsModal => true;
        public bool IsTopmost => ReferenceEquals(owner.host?.ActiveSurface, surface);
        public bool Maximizable => false;
        public bool Minimizable => false;
        public WindowInteractionState InteractionState => owner.IsOpen ? WindowInteractionState.ReadyForUserInteraction : WindowInteractionState.Closing;
        public WindowVisualState VisualState => WindowVisualState.Normal;
        public void Close() => owner.Close();
        public void SetVisualState(WindowVisualState state) { if (state != WindowVisualState.Normal) throw new InvalidOperationException("Spotlight cannot be minimized or maximized."); }
        public bool WaitForInputIdle(int milliseconds) { if (milliseconds < 0) throw new ArgumentOutOfRangeException(nameof(milliseconds)); return owner.IsOpen; }
    }
}
