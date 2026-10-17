using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

public sealed partial class BleeckerInput
{
    private readonly UISettings motionSettings = new();
    private Border? palette;
    private Border? ringPalette;
    private Border? chrome;
    private Border? focusRing;
    private Grid? editor;
    private readonly Stopwatch paintClock = Stopwatch.StartNew();
    private readonly BleeckerCssTransition<(double L,double A,double B,double Alpha)> backgroundMotion = new(BleeckerColorMix.MixPremultiplied);
    private readonly BleeckerCssTransition<(double L,double A,double B,double Alpha)> borderMotion = new(BleeckerColorMix.MixPremultiplied);
    private readonly BleeckerCssTransition<(double L,double A,double B,double Alpha)> ringMotion = new(BleeckerColorMix.MixPremultiplied);
    private readonly BleeckerCssTransition<double> editorOpacityMotion = new((from,to,progress)=>from+(to-from)*progress);
    private readonly BleeckerCssTransition<double> ringOpacityMotion = new((from,to,progress)=>from+(to-from)*progress);
    private bool paintRendering;
    private int motionRequestVersion;
    private readonly List<(SolidColorBrush Brush, long Color, long Opacity)> observedBrushes = [];
    private readonly List<(DependencyObject Owner, DependencyProperty Property, long Token)> paletteSubscriptions = [];

    private void InitializeMotion()
    {
        ActualThemeChanged += (_, _) => DispatcherQueue.TryEnqueue(() => RequestMotion(false));
        RegisterPropertyChangedCallback(BackgroundProperty, (_, _) => DispatcherQueue.TryEnqueue(() => RequestMotion(false)));
        RegisterPropertyChangedCallback(BorderBrushProperty, (_, _) => DispatcherQueue.TryEnqueue(() => RequestMotion(false)));
        Loaded += (_, _) =>
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) motionSettings.AnimationsEnabledChanged += OnInputMotionPreferenceChanged;
        };
        Unloaded += (_, _) =>
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) motionSettings.AnimationsEnabledChanged -= OnInputMotionPreferenceChanged;
            RequestMotion(false);
            ClearObservedBrushes();
        };
    }

    private void OnInputMotionPreferenceChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) =>
        DispatcherQueue.TryEnqueue(() => { RequestMotion(false); UpdateShadow(); });

    private void RequestMotion(bool animate)
    {
        var version = ++motionRequestVersion;
        if (!animate || !IsLoaded || !motionSettings.AnimationsEnabled)
        {
            UpdateMotion(false);
            return;
        }
        // Coalesce changes from one input event before starting a storyboard.
        // Otherwise a second same-frame change can sample its predecessor's
        // destination before the compositor has presented the first keyframe.
        DispatcherQueue.TryEnqueue(() => { if (version == motionRequestVersion) UpdateMotion(true); });
    }

    private void ApplyMotionTemplate()
    {
        StopPaintRendering();
        ClearObservedBrushes();
        foreach (var (owner, property, token) in paletteSubscriptions) owner.UnregisterPropertyChangedCallback(property, token);
        paletteSubscriptions.Clear();
        palette = GetTemplateChild("InputPalette") as Border;
        ringPalette = GetTemplateChild("InputRingPalette") as Border;
        chrome = GetTemplateChild("BorderElement") as Border;
        focusRing = GetTemplateChild("InputRing") as Border;
        editor = GetTemplateChild("Editor") as Grid;
        if (chrome is not null) { chrome.Background = new SolidColorBrush(); chrome.BorderBrush = new SolidColorBrush(); }
        if (focusRing is not null) focusRing.BorderBrush = new SolidColorBrush();
        if (palette is not null)
        {
            ObservePalette(palette, Border.BackgroundProperty);
            ObservePalette(palette, Border.BorderBrushProperty);
        }
        if (ringPalette is not null) ObservePalette(ringPalette, Border.BorderBrushProperty);
    }

    private void ObservePalette(DependencyObject owner, DependencyProperty property)
    {
        var token = owner.RegisterPropertyChangedCallback(property, (_, _) =>
        {
            if (!applyingAppearance) DispatcherQueue.TryEnqueue(() => RequestMotion(false));
        });
        paletteSubscriptions.Add((owner, property, token));
    }

    private void ClearObservedBrushes()
    {
        foreach (var (brush, color, opacity) in observedBrushes)
        {
            brush.UnregisterPropertyChangedCallback(SolidColorBrush.ColorProperty, color);
            brush.UnregisterPropertyChangedCallback(Brush.OpacityProperty, opacity);
        }
        observedBrushes.Clear();
    }

    private void UpdateMotion(bool animate)
    {
        if (palette is null || ringPalette is null || chrome is null || focusRing is null || editor is null) return;
        var now = paintClock.Elapsed.TotalMilliseconds;
        var duration = BleeckerTokens.Durations["control"];
        var enabled = animate && IsLoaded && motionSettings.AnimationsEnabled && !shadowAccessibility.HighContrast;
        Retarget(chrome, Border.BackgroundProperty, palette.Background, backgroundMotion);
        Retarget(chrome, Border.BorderBrushProperty, palette.BorderBrush, borderMotion);
        Retarget(focusRing, Border.BorderBrushProperty, ringPalette.BorderBrush, ringMotion);
        editorOpacityMotion.Retarget(palette.Opacity,now,duration,enabled);
        ringOpacityMotion.Retarget(ringPalette.Opacity,now,duration,enabled);
        ClearObservedBrushes();
        if (IsLoaded)
            foreach (var brush in new[] { palette.Background, palette.BorderBrush, ringPalette.BorderBrush }.OfType<SolidColorBrush>().Distinct())
                observedBrushes.Add((brush,
                    brush.RegisterPropertyChangedCallback(SolidColorBrush.ColorProperty, (_, _) => RequestMotion(false)),
                    brush.RegisterPropertyChangedCallback(Brush.OpacityProperty, (_, _) => RequestMotion(false))));
        PaintFrame(now);
        if (PaintIsRunning(now)) { if (!paintRendering) { paintRendering=true; CompositionTarget.Rendering+=OnPaintFrame; } }
        else StopPaintRendering();

        void Retarget(Border target, DependencyProperty property, Brush source, BleeckerCssTransition<(double L,double A,double B,double Alpha)> transition)
        {
            if (source is not SolidColorBrush solid) { target.SetValue(property,source); transition.Retarget((0,0,0,0),now,duration,false); return; }
            var canAnimate=enabled&&target.GetValue(property)is SolidColorBrush;
            if (target.GetValue(property)is not SolidColorBrush) target.SetValue(property,new SolidColorBrush());
            var color=solid.Color;
            transition.Retarget(BleeckerColorMix.Premultiply((color.R,color.G,color.B,color.A/255d*solid.Opacity)),now,duration,canAnimate);
        }
    }
    private bool PaintIsRunning(double now)=>backgroundMotion.IsRunning(now)||borderMotion.IsRunning(now)||ringMotion.IsRunning(now)||editorOpacityMotion.IsRunning(now)||ringOpacityMotion.IsRunning(now);
    private void OnPaintFrame(object? sender,object args)
    {
        var now=paintClock.Elapsed.TotalMilliseconds;PaintFrame(now);if(!PaintIsRunning(now))StopPaintRendering();
    }
    private void StopPaintRendering(){if(paintRendering)CompositionTarget.Rendering-=OnPaintFrame;paintRendering=false;}
    private void PaintFrame(double now)
    {
        if(palette is null||ringPalette is null||chrome is null||focusRing is null||editor is null)return;
        Paint(chrome,Border.BackgroundProperty,palette.Background,backgroundMotion);
        Paint(chrome,Border.BorderBrushProperty,palette.BorderBrush,borderMotion);
        Paint(focusRing,Border.BorderBrushProperty,ringPalette.BorderBrush,ringMotion);
        editor.Opacity=editorOpacityMotion.ValueAt(now);focusRing.Opacity=ringOpacityMotion.ValueAt(now);
        void Paint(Border target,DependencyProperty property,Brush source,BleeckerCssTransition<(double L,double A,double B,double Alpha)> transition)
        {
            if(source is not SolidColorBrush)return;
            var brush=(SolidColorBrush)target.GetValue(property);var color=BleeckerColorMix.EncodePremultiplied(transition.ValueAt(now));
            brush.Color=Windows.UI.Color.FromArgb(255,color.R,color.G,color.B);brush.Opacity=color.Alpha;
        }
    }
}
