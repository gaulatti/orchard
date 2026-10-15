using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class SwitchesFixture : UserControl
{
    private readonly ConsumerState state = new();
    private readonly List<object> motionSamples = [];
    private readonly List<object> appearanceSamples = [];
    private readonly List<string> physicalInputSteps = [];

    public SwitchesFixture() { InitializeComponent(); DataContext = state; }

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder, bool physicalInput = false)
    {
        var untemplated = new BleeckerSwitch { Label = "Unmounted switch metadata" };
        Require(VisualTreeHelper.GetChildrenCount(untemplated) == 0, "Untemplated switch regression already has presentation.");
        var earlyPeer = FrameworkElementAutomationPeer.CreatePeerForElement(untemplated);
        Require(earlyPeer.IsControlElement() && earlyPeer.IsContentElement() && earlyPeer.IsEnabled(),
            "Untemplated switch metadata cannot be queried by UI Automation.");
        untemplated.IsEnabled = false;
        Require(earlyPeer.IsControlElement() && earlyPeer.IsContentElement() && !earlyPeer.IsEnabled(),
            "Untemplated switch metadata lost disabled state.");
        AutomationProperties.SetAccessibilityView(untemplated,AccessibilityView.Raw);
        Require(!earlyPeer.IsControlElement() && !earlyPeer.IsContentElement(), "Untemplated switch ignored its consumer's Raw accessibility view.");
        AutomationProperties.SetAccessibilityView(untemplated,AccessibilityView.Content);
        Require(earlyPeer.IsControlElement() && earlyPeer.IsContentElement(), "Untemplated switch did not restore its consumer's Content accessibility view.");
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"switch-untemplated-{scheme}.json"),
            JsonSerializer.Serialize(new { passed=true, visualChildren=VisualTreeHelper.GetChildrenCount(untemplated), control=earlyPeer.IsControlElement(), content=earlyPeer.IsContentElement(), enabled=earlyPeer.IsEnabled() }));
        UpdateLayout();
        await MainWindow.AwaitPresentationAsync(this);
        await MainWindow.CaptureElementAsync(this, Path.Combine(folder, $"switch-initial-{scheme}.png"));
        await MainWindow.CaptureElementAsync(XamlRoot.Content, Path.Combine(folder, $"switch-context-{scheme}.png"));
        await File.WriteAllTextAsync(Path.Combine(folder, $"switch-initial-measurements-{scheme}.json"),
            JsonSerializer.Serialize(Rows.Children.OfType<BleeckerSwitch>().Select(control =>
            {
                var label = Find<TextBlock>(control, "PART_Label");
                var native = Find<ToggleSwitch>(control, "PART_Switch");
                return new { control.Name, control.ActualWidth, control.ActualHeight, desiredWidth = control.DesiredSize.Width,
                    desiredHeight = control.DesiredSize.Height, nativeWidth = native.ActualWidth, nativeHeight = native.ActualHeight,
                    label = new { label.Text, label.ActualWidth, label.ActualHeight, desiredWidth = label.DesiredSize.Width,
                        desiredHeight = label.DesiredSize.Height, slot = Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(label).ToString() } };
            }), new JsonSerializerOptions { WriteIndented = true }));
        foreach (var control in Rows.Children.OfType<BleeckerSwitch>()) VerifyPresentation(control, scheme);
        var trackBitmap = new RenderTargetBitmap();
        await trackBitmap.RenderAsync(Find<Border>(On, "PART_Track"));
        var trackPixels = await trackBitmap.GetPixelsAsync();
        var trackBytes = new byte[checked((int)trackPixels.Length)];
        using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(trackPixels)) reader.ReadBytes(trackBytes);
        Require(trackBytes[(trackBitmap.PixelHeight / 2 * trackBitmap.PixelWidth) * 4 + 3] >= 200 &&
            trackBytes[(trackBitmap.PixelHeight / 2 * trackBitmap.PixelWidth + trackBitmap.PixelWidth - 1) * 4 + 3] >= 200 &&
            trackBytes[(trackBitmap.PixelWidth / 2) * 4 + 3] >= 200,
            "Checked switch background was inset by its transparent border instead of filling the 36x20 outer pill.");
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(Off);
        var nativePeer = FrameworkElementAutomationPeer.CreatePeerForElement(Find<ToggleSwitch>(Off, "PART_Switch"));
        var toggle = (IToggleProvider)peer.GetPattern(PatternInterface.Toggle);
        Require(peer.GetAutomationControlType() == nativePeer.GetAutomationControlType() && peer.GetLocalizedControlType() == nativePeer.GetLocalizedControlType() &&
            peer.IsControlElement() && peer.IsContentElement() && (peer.GetChildren()?.Count ?? 0) == 0 && peer.GetName() == Off.Label &&
            toggle.ToggleState == ToggleState.Off && peer.IsKeyboardFocusable(), "Switch lost native toggle semantics, accessible visibility or its label.");
        var count = 0;
        void OnToggled(object sender, RoutedEventArgs args) => count++;
        Off.Toggled += OnToggled;
        try
        {
            toggle.Toggle();
            await SettleAsync(Off, true);
            Require(Off.IsOn && toggle.ToggleState == ToggleState.On && count == 1, "Native UIA toggle did not reach consumer state exactly once.");
            Off.IsOn = false;
            await SettleAsync(Off, false);
            Require(toggle.ToggleState == ToggleState.Off && count == 2, "Consumer state did not reach the native switch exactly once.");
            peer.SetFocus();
            Require(Find<ToggleSwitch>(Off, "PART_Switch").FocusState != FocusState.Unfocused && peer.HasKeyboardFocus(),
                "Switch UIA focus did not reach the native input.");
            Off.Label = "Updated label";
            await WaitAsync(() => Find<TextBlock>(Off, "PART_Label").Text == "Updated label", "Switch did not present the updated label.");
            Require(peer.GetName() == "Updated label" && Find<TextBlock>(Off, "PART_Label").Text == "Updated label", "Switch label update lost presentation or its accessible name.");
            AutomationProperties.SetName(Off, "Explicit name");
            Require(peer.GetName() == "Explicit name" && FrameworkElementAutomationPeer.CreatePeerForElement(Find<ToggleSwitch>(Off, "PART_Switch")).GetName() == "Explicit name",
                "Switch explicit accessible name did not reach its focused native input.");
            AutomationProperties.SetName(Off, "");
            AutomationProperties.SetLabeledBy(Off, SwitchHeading);
            Require(peer.GetName() == "Switch" && nativePeer.GetName() == "Switch", "Switch did not preserve its associated accessible label.");
        }
        finally { Off.Toggled -= OnToggled; Off.Label = "Enable notifications"; AutomationProperties.SetName(Off, ""); AutomationProperties.SetLabeledBy(Off, null); Off.IsOn = false; }
        foreach (var control in new[] { DisabledOff, DisabledOn })
        {
            var before = control.IsOn;
            var rejected = false;
            try { ((IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(control).GetPattern(PatternInterface.Toggle)).Toggle(); }
            catch (ElementNotEnabledException) { rejected = true; }
            catch (COMException error) when (error.HResult == unchecked((int)0x80040200)) { rejected = true; }
            Require(rejected && control.IsOn == before && !Find<ToggleSwitch>(control, "PART_Switch").Focus(FocusState.Keyboard),
                "Disabled switch accepted native activation or keyboard focus.");
        }
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(WithoutLabel).GetName() == "Toggle feature" && WithoutLabel.ActualWidth == 36,
            "Unlabeled switch lost its accessible name or retained label spacing.");
        var boundToggle = (IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Bound).GetPattern(PatternInterface.Toggle);
        state.Enabled = false;
        boundToggle.Toggle();
        await SettleAsync(Bound, true);
        Require(state.Enabled && BindingStatus.Text == "True", "Native switch did not update the consumer's two-way binding.");
        state.Enabled = false;
        await SettleAsync(Bound, false);
        Require(!Bound.IsOn && boundToggle.ToggleState == ToggleState.Off && BindingStatus.Text == "False", "Consumer binding did not update the native switch.");
        foreach (var name in new[] { "PointerOver", "Pressed", "Normal" })
        {
            Require(VisualStateManager.GoToState(Find<ToggleSwitch>(Off, "PART_Switch"), name, false), $"Missing switch state {name}.");
            VerifyPresentation(Off, scheme);
        }
        await VerifyLifecycleAsync(scheme);
        var appearanceVerified = false;
        try { await VerifyLiveAppearanceAsync(scheme); appearanceVerified = true; }
        finally
        {
            await File.WriteAllTextAsync(Path.Combine(folder, $"switch-appearance-{scheme}.json"),
                JsonSerializer.Serialize(new { passed = appearanceVerified,
                    animationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled, appearanceSamples },
                    new JsonSerializerOptions { WriteIndented = true }));
        }
        await InspectFontFallbackAsync(scheme, folder);
        try { await VerifyMotionAsync(scheme); }
        finally
        {
            await File.WriteAllTextAsync(Path.Combine(folder, $"switch-motion-{scheme}.json"),
                JsonSerializer.Serialize(new { animationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled,
                    motionSamples, appearanceSamples }, new JsonSerializerOptions { WriteIndented = true }));
        }
        // Full local runs retain their existing scope. The focused Switch mode
        // explicitly exercises this same owned-window path without spoofing CI.
        if (physicalInput || Environment.GetEnvironmentVariable("CI") == "true")
        {
            try { await VerifyPhysicalInputAsync(scheme, folder); }
            finally
            {
                await File.WriteAllTextAsync(Path.Combine(folder, $"switch-physical-input-{scheme}.json"),
                    JsonSerializer.Serialize(physicalInputSteps, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
        var measurements = Rows.Children.OfType<BleeckerSwitch>().Select(control =>
        {
            var track = Find<Border>(control, "PART_Track");
            var thumb = Find<Microsoft.UI.Xaml.Shapes.Ellipse>(control, "PART_Thumb");
            var label = Find<TextBlock>(control, "PART_Label");
            var thumbOrigin = thumb.TransformToVisual(track).TransformPoint(new Point());
            return new { control.Name, control.IsOn, control.IsEnabled, control.Label, control.ActualWidth, control.ActualHeight,
                track = new { track.ActualWidth, track.ActualHeight, color = Hex(track.Background), opacity = track.Background.Opacity,
                    stroke = Hex(track.BorderBrush), strokeOpacity = track.BorderBrush.Opacity, radius = track.CornerRadius.TopLeft },
                thumb = new { thumb.ActualWidth, thumb.ActualHeight, x = thumbOrigin.X, y = thumbOrigin.Y, color = Hex(thumb.Fill) },
                label = new { label.ActualWidth, label.ActualHeight, label.FontSize, label.LineHeight, label.BaselineOffset,
                    font = label.FontFamily.Source, weight = label.FontWeight.Weight, color = Hex(label.Foreground), label.Visibility } };
        });
        await File.WriteAllTextAsync(Path.Combine(folder, $"switch-measurements-{scheme}.json"),
            JsonSerializer.Serialize(new { scheme, displayScale = XamlRoot.RasterizationScale,
                textScale = new Windows.UI.ViewManagement.UISettings().TextScaleFactor,
                animationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled, measurements, motionSamples,
                physicalInputVerified = Environment.GetEnvironmentVariable("CI") == "true", physicalInputSteps },
                new JsonSerializerOptions { WriteIndented = true }));
        await MainWindow.CaptureElementAsync(this, Path.Combine(folder, $"switch-{scheme}.png"));
    }

    private void VerifyPresentation(BleeckerSwitch control, BleeckerColorScheme scheme)
    {
        var track = Find<Border>(control, "PART_Track");
        var thumb = Find<Microsoft.UI.Xaml.Shapes.Ellipse>(control, "PART_Thumb");
        var label = Find<TextBlock>(control, "PART_Label");
        var pointerInput = Find<Microsoft.UI.Xaml.Controls.Primitives.Thumb>(control, "SwitchThumb");
        Require(!pointerInput.IsTabStop && pointerInput.ActualWidth == control.ActualWidth && pointerInput.ActualHeight == control.ActualHeight,
            "Native switch pointer input does not cover its track and label exactly once.");
        var point = thumb.TransformToVisual(track).TransformPoint(new Point());
        Require(track.ActualWidth == 36 && track.ActualHeight == 20 && track.CornerRadius.TopLeft == 10 && track.BorderThickness == new Thickness(1) &&
            thumb.ActualWidth == 16 && thumb.ActualHeight == 16 &&
            Math.Abs(point.X - (control.IsOn ? 18 : 2)) < .001 && Math.Abs(point.Y - 2) < .001,
            $"Switch geometry differs from its 36x20 reference: {control.Name}, track={track.ActualWidth}x{track.ActualHeight}, thumb={thumb.ActualWidth}x{thumb.ActualHeight} at {point}.");
        var palette = ReadPalette(control);
        Require(PaletteSettled(palette, scheme), $"Switch palette/disabled opacity differs: {JsonSerializer.Serialize(palette)}.");
        Require(control.FontSize == 14 && label.FontWeight.Weight == 500 && label.LineHeight == 20 && label.Margin.Left == 12 &&
            control.FontFamily.Source.Contains("EncodeSans-Medium.ttf") && !Find<ToggleSwitch>(control, "PART_Switch").UseSystemFocusVisuals,
            "Switch acquired Windows typography, focus styling or label spacing.");
        if (!string.IsNullOrEmpty(control.Label))
        {
            var labelBounds = label.TransformToVisual(this).TransformBounds(new Rect(0, 0, label.ActualWidth, label.ActualHeight));
            var trackBounds = track.TransformToVisual(this).TransformBounds(new Rect(0, 0, track.ActualWidth, track.ActualHeight));
            var gap = control.FlowDirection == FlowDirection.RightToLeft ? trackBounds.Left - labelBounds.Right : labelBounds.Left - trackBounds.Right;
            Require(Math.Abs(gap - 12) < .01, $"Switch label gap differs: {control.Name}, gap={gap:R}.");
        }
        var labelAllocation = Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(label).Width - label.Margin.Left - label.Margin.Right;
        if (control.Name == "LongLabel") Require(control.ActualHeight == 80 && Math.Abs(labelAllocation - 152) < .01,
            $"Long switch label differs from its 152px-wide, four 20px reference lines: allocation={labelAllocation:R}, height={label.ActualHeight:R}.");
        if (control.Name == "Off") Require(control.ActualHeight == 20 && label.DesiredSize.Height == 20 && label.ActualWidth > 120,
            $"First-display switch label wrapped or clipped despite available width: control={control.ActualWidth:R}x{control.ActualHeight:R}, label={label.ActualWidth:R}x{label.ActualHeight:R}, desired={label.DesiredSize}.");
        if (control.Name == "Off") Require(Math.Abs(control.ActualWidth - 173.296875) < .05,
            $"Natural switch row width differs from the browser reference: {control.ActualWidth:R}.");
    }

    private async Task VerifyLifecycleAsync(BleeckerColorScheme scheme)
    {
        var originalTemplate = Off.Template;
        var oldNative = Find<ToggleSwitch>(Off, "PART_Switch");
        Off.Template = null; Off.ApplyTemplate();
        Off.Template = originalTemplate; Off.ApplyTemplate(); UpdateLayout();
        Require(!ReferenceEquals(oldNative, Find<ToggleSwitch>(Off, "PART_Switch")), "Switch did not replace its native template.");
        oldNative.IsOn = true;
        Require(!Off.IsOn, "Detached native switch still changed consumer state.");
        VerifyPresentation(Off, scheme);
        var unloaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnUnloaded(object sender, RoutedEventArgs args) => unloaded.TrySetResult();
        Off.Unloaded += OnUnloaded;
        try { Rows.Children.Remove(Off); await unloaded.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
        finally { Off.Unloaded -= OnUnloaded; }
        Require(!Off.IsLoaded, "Switch did not unload.");
        Require(ElementCompositionPreview.GetElementChildVisual(Find<Grid>(Off, "PART_ShadowHost")) is null, "Unloaded switch retained its composition shadow.");
        Off.IsOn = true;
        var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnLoaded(object sender, RoutedEventArgs args) => loaded.TrySetResult();
        Off.Loaded += OnLoaded;
        try { Rows.Children.Insert(1, Off); await loaded.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
        finally { Off.Loaded -= OnLoaded; }
        Require(Off.IsLoaded, "Switch did not reload.");
        UpdateLayout();
        await SettleAsync(Off, true);
        VerifyPresentation(Off, scheme);
        Require(ElementCompositionPreview.GetElementChildVisual(Find<Grid>(Off, "PART_ShadowHost")) is SpriteVisual { Shadow: DropShadow },
            "Reloaded switch did not restore its shadow.");
        Off.IsOn = false;
        await SettleAsync(Off, false);
    }

    private async Task VerifyMotionAsync(BleeckerColorScheme scheme)
    {
        motionSamples.Clear();
        var animations = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
        Off.IsOn = true;
        var timer = Stopwatch.StartNew();
        var sawIntermediate = false;
        while (timer.ElapsedMilliseconds < 300)
        {
            var x = ((TranslateTransform)Find<Grid>(Off, "SwitchKnob").RenderTransform).X;
            var fill = Find<Border>(Off, "PART_Track").Background;
            motionSamples.Add(new { elapsed = timer.Elapsed.TotalMilliseconds, x, color = Hex(fill), alpha = fill.Opacity });
            sawIntermediate |= x > .01 && x < 15.99;
            if (x >= 16) break;
            await Task.Delay(1);
        }
        Require(!animations || sawIntermediate, "Enabled switch motion skipped every intermediate thumb frame.");
        Require(animations || ((TranslateTransform)Find<Grid>(Off, "SwitchKnob").RenderTransform).X == 16, "Reduced-motion switch did not settle immediately.");
        await SettleAsync(Off, true);
        Off.IsOn = false;
        if (animations)
        {
            await WaitAsync(() => { var x = ((TranslateTransform)Find<Grid>(Off, "SwitchKnob").RenderTransform).X; return x > .01 && x < 15.99; }, "Switch reversal did not start from an intermediate frame.");
            Off.IsOn = true;
            await SettleAsync(Off, true);
        }
        Off.IsOn = false;
        await SettleAsync(Off, false);
        if (animations)
        {
            var host = (FrameworkElement)XamlRoot.Content;
            var originalTheme = host.RequestedTheme;
            var startColor = Hex(Find<Border>(On, "PART_Track").Background);
            var targetColor = scheme == BleeckerColorScheme.Light ? "#FF5BA3F5" : "#FF2C5784";
            try
            {
                Off.IsOn = true;
                await WaitAsync(() => { var x = ((TranslateTransform)Find<Grid>(Off, "SwitchKnob").RenderTransform).X; return x > .01 && x < 15.99; },
                    "Switch did not start its thumb before the palette interruption.");
                host.RequestedTheme = scheme == BleeckerColorScheme.Light ? ElementTheme.Dark : ElementTheme.Light;
                await WaitAsync(() => { var color = Hex(Find<Border>(On, "PART_Track").Background); return color != startColor && color != targetColor; },
                    "Palette interruption did not produce an intermediate track color.");
                var x = ((TranslateTransform)Find<Grid>(Off, "SwitchKnob").RenderTransform).X;
                motionSamples.Add(new { phase = "palette-during-toggle", x, checkedColor = Hex(Find<Border>(On, "PART_Track").Background) });
                Require(x > .01 && x < 15.99 && Off.IsOn, "Palette interruption snapped or reversed the moving thumb.");
                await SettleAsync(Off, true);
            }
            finally { host.RequestedTheme = originalTheme; Off.IsOn = false; }
            await SettleAsync(Off, false);
        }
    }

    private async Task VerifyLiveAppearanceAsync(BleeckerColorScheme scheme)
    {
        var host = (FrameworkElement)XamlRoot.Content;
        var controls = Rows.Children.OfType<BleeckerSwitch>().ToArray();
        var consumerStates = controls.Select(control => control.IsOn).ToArray();
        var originalTheme = host.RequestedTheme;
        var alternate = scheme == BleeckerColorScheme.Light ? BleeckerColorScheme.Dark : BleeckerColorScheme.Light;
        appearanceSamples.Clear();
        try
        {
            var initial = Hex(Find<Border>(On, "PART_Track").Background);
            var target = alternate == BleeckerColorScheme.Light ? "#FF2C5784" : "#FF5BA3F5";
            var animated = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
            var timer = Stopwatch.StartNew();
            host.RequestedTheme = alternate == BleeckerColorScheme.Light ? ElementTheme.Light : ElementTheme.Dark;
            var sawIntermediate = false;
            while (timer.ElapsedMilliseconds < 300 && !controls.All(control => PaletteSettled(ReadPalette(control), alternate)))
            {
                var fill = Find<Border>(On, "PART_Track").Background;
                var color = Hex(fill);
                var x = ((TranslateTransform)Find<Grid>(On, "SwitchKnob").RenderTransform).X;
                appearanceSamples.Add(new { elapsed = timer.Elapsed.TotalMilliseconds, color, alpha = fill.Opacity,
                    offAlpha = Find<Border>(Off, "PART_Track").Background.Opacity, x });
                sawIntermediate |= color != initial && color != target;
                Require(x == 16, "Appearance change restarted the settled checked thumb.");
                await Task.Delay(1);
            }
            Require(!animated || sawIntermediate, "Animated appearance change skipped every intermediate track color.");
            await WaitAsync(() => controls.All(control => PaletteSettled(ReadPalette(control), alternate)),
                "Switch rows did not all reach their live appearance endpoints.");
            appearanceSamples.Add(new { phase = "settled", elapsed = timer.Elapsed.TotalMilliseconds,
                color = Hex(Find<Border>(On, "PART_Track").Background), offAlpha = Find<Border>(Off, "PART_Track").Background.Opacity });
            foreach (var control in controls) VerifyPresentation(control, alternate);
            Require(controls.Select(control => control.IsOn).SequenceEqual(consumerStates), "Appearance change altered consumer switch state.");
        }
        finally
        {
            appearanceSamples.Add(new { phase = "alternate-end", rows = controls.Select(ReadPalette).ToArray() });
            host.RequestedTheme = originalTheme;
        }
        try
        {
            await WaitAsync(() => controls.All(control => PaletteSettled(ReadPalette(control), scheme)),
                "Switch rows did not all restore their original appearance endpoints.");
            foreach (var control in controls) VerifyPresentation(control, scheme);
            Require(controls.Select(control => control.IsOn).SequenceEqual(consumerStates), "Restoring appearance altered consumer switch state.");
        }
        finally { appearanceSamples.Add(new { phase = "restored-end", rows = controls.Select(ReadPalette).ToArray() }); }
    }

    private sealed record PaletteSnapshot(string Name, bool IsOn, bool IsEnabled, string Fill, double FillOpacity,
        string Stroke, double StrokeOpacity, string Thumb, double Opacity);

    private static PaletteSnapshot ReadPalette(BleeckerSwitch control)
    {
        var track = Find<Border>(control, "PART_Track");
        return new(control.Name, control.IsOn, control.IsEnabled, Hex(track.Background), track.Background.Opacity,
            Hex(track.BorderBrush), track.BorderBrush.Opacity, Hex(Find<Microsoft.UI.Xaml.Shapes.Ellipse>(control, "PART_Thumb").Fill),
            Find<Grid>(control, "PART_Root").Opacity);
    }

    private static bool PaletteSettled(PaletteSnapshot row, BleeckerColorScheme scheme)
    {
        var light = scheme == BleeckerColorScheme.Light;
        var fill = row.IsOn ? light ? "#FF2C5784" : "#FF5BA3F5" : light ? "#FFE6D5B8" : "#FFFFFFFF";
        // Brush.Opacity stores the XAML resource's native float endpoint.
        // Each row owns its clocks; one settled row does not settle its siblings.
        return row.Fill == fill && (row.IsOn || row.Stroke == fill) && Math.Abs(row.FillOpacity - (float)(row.IsOn ? 1 : light ? .25 : .15)) < 1e-9 &&
            Math.Abs(row.StrokeOpacity - (float)(row.IsOn ? 0 : light ? .35 : .10)) < 1e-9 &&
            row.Thumb == "#FFFFFFFF" && row.Opacity == (row.IsEnabled ? 1 : .5);
    }

    private async Task VerifyPhysicalInputAsync(BleeckerColorScheme scheme, string folder)
    {
        physicalInputSteps.Clear();
        using var input = new NativeInputSession(XamlRoot, folder, $"switch-{scheme}");
        await input.AcquireAsync(SwitchHeading);
        await input.MoveAsync(SwitchHeading);
        physicalInputSteps.Add("Own catalog window acquired foreground input");
        try
        {
            Off.IsOn = false;
            await SettleAsync(Off, false);
            await input.RequireForegroundAsync();
            var screenRect = await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"switch-window-{scheme}.png"));
            await File.WriteAllTextAsync(Path.Combine(folder, $"switch-window-bounds-{scheme}.json"),
                JsonSerializer.Serialize(new { client = screenRect, displayScale = XamlRoot.RasterizationScale,
                    rows = Rows.Children.OfType<BleeckerSwitch>().Select(control => new { control.Name, screen = input.ScreenBounds(control) }) },
                    new JsonSerializerOptions { WriteIndented = true }));
            physicalInputSteps.Add("Composed client pixels captured with native thumb shadows");
            var native = Find<ToggleSwitch>(Off, "PART_Switch");
            Require(native.Focus(FocusState.Keyboard), "Native switch could not receive keyboard focus.");
            await input.RequireForegroundAsync(); input.Key(0x20);
            await WaitAsync(() => Off.IsOn, "Physical Space did not toggle the focused switch.");
            physicalInputSteps.Add("Space toggled the focused native input");
            await input.RequireForegroundAsync(); input.Key(0x09);
            await WaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Find<ToggleSwitch>(On, "PART_Switch")),
                "Physical Tab did not reach the next switch.");
            await input.RequireForegroundAsync(); input.Key(0x09);
            await WaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Find<ToggleSwitch>(WithoutLabel, "PART_Switch")),
                "Physical Tab did not skip both disabled switches.");
            physicalInputSteps.Add("Tab advanced once per switch and skipped disabled inputs");
            await input.ClickAsync(Find<Border>(Off, "PART_Track"));
            await WaitAsync(() => !Off.IsOn, "Physical track click did not toggle the switch off.");
            physicalInputSteps.Add("Track click toggled off");
            await input.ClickAsync(Find<TextBlock>(Off, "PART_Label"));
            await WaitAsync(() => Off.IsOn, "Physical label click did not activate its switch.");
            physicalInputSteps.Add("Label click toggled on");
            foreach (var control in new[] { DisabledOff, DisabledOn })
            {
                var before = control.IsOn;
                await input.ClickAsync(Find<Border>(control, "PART_Track"));
                Require(control.IsOn == before, "Physical click changed a disabled switch.");
            }
            physicalInputSteps.Add("Disabled off/on rejected physical activation");
            await input.ClickAsync(Find<Border>(Rtl, "PART_Track"));
            await WaitAsync(() => !Rtl.IsOn, "Physical RTL track click did not toggle off.");
            await input.ClickAsync(Find<TextBlock>(Rtl, "PART_Label"));
            await WaitAsync(() => Rtl.IsOn, "Physical RTL label click did not toggle on.");
            physicalInputSteps.Add("RTL track and label both activated the native input");
        }
        finally { Off.IsOn = false; Rtl.IsOn = true; }
        await SettleAsync(Off, false);
    }

    private async Task InspectFontFallbackAsync(BleeckerColorScheme scheme, string folder)
    {
        var original = NonLatin.FontFamily;
        var results = new List<object>();
        // Evidence probes only; these do not change the library's font contract.
        var candidates = new[] { "", ", Segoe UI", ", Arial", ", Microsoft YaHei UI", ", MS Gothic" };
        try
        {
            for (var index = 0; index < candidates.Length; index++)
            {
                NonLatin.FontFamily = new FontFamily(original.Source + candidates[index]);
                UpdateLayout();
                await MainWindow.CaptureElementAsync(NonLatin, Path.Combine(folder, $"switch-font-fallback-{index}-{scheme}.png"));
                UpdateLayout();
                var label = Find<TextBlock>(NonLatin, "PART_Label");
                results.Add(new { font = label.FontFamily.Source, label.Language, label.ActualWidth, label.ActualHeight, label.BaselineOffset });
            }
            await File.WriteAllTextAsync(Path.Combine(folder, $"switch-font-fallback-{scheme}.json"),
                JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally { NonLatin.FontFamily = original; UpdateLayout(); }
    }

    private Task SettleAsync(BleeckerSwitch control, bool on) => WaitAsync(() =>
        Math.Abs(((TranslateTransform)Find<Grid>(control, "SwitchKnob").RenderTransform).X - (on ? 16 : 0)) < .000001 &&
        Math.Abs(Find<Border>(control, "PART_Track").Background.Opacity - (on ? 1 : control.ActualTheme == ElementTheme.Light ? .25 : .15)) < .000001,
        "Switch did not settle to its binary destination.");
    private async Task WaitAsync(Func<bool> condition, string message)
    {
        var timer = Stopwatch.StartNew();
        while (!condition() && timer.Elapsed < TimeSpan.FromSeconds(2)) { UpdateLayout(); await Task.Delay(1); }
        Require(condition(), message);
    }
    private static string Hex(Brush brush) { var c = ((SolidColorBrush)brush).Color; return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}"; }
    private static T Find<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        if (parent is T match && match.Name == name) return match;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            try { return Find<T>(VisualTreeHelper.GetChild(parent, i), name); }
            catch (InvalidOperationException) { }
        }
        throw new InvalidOperationException($"Missing switch template part {name}.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class ConsumerState : INotifyPropertyChanged
    {
        private bool enabled;
        public bool Enabled { get => enabled; set { if (enabled == value) return; enabled = value; PropertyChanged?.Invoke(this, new(nameof(Enabled))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
