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
    private readonly List<string> physicalInputSteps = [];

    public SwitchesFixture() { InitializeComponent(); DataContext = state; }

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        UpdateLayout();
        await Task.Delay(100);
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
        await VerifyLiveAppearanceAsync(scheme);
        // Physical input is restricted to the dedicated hosted CI desktop.
        // Ordinary local catalog verification does not take over the user's input.
        if (Environment.GetEnvironmentVariable("CI") == "true")
        {
            try { await VerifyPhysicalInputAsync(); }
            finally
            {
                await File.WriteAllTextAsync(Path.Combine(folder, $"switch-physical-input-{scheme}.json"),
                    JsonSerializer.Serialize(physicalInputSteps, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
        await VerifyMotionAsync();
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
        var root = Find<Grid>(control, "PART_Root");
        var point = thumb.TransformToVisual(track).TransformPoint(new Point());
        Require(track.ActualWidth == 36 && track.ActualHeight == 20 && track.CornerRadius.TopLeft == 10 && track.BorderThickness == new Thickness(1) &&
            thumb.ActualWidth == 16 && thumb.ActualHeight == 16 &&
            Math.Abs(point.X - (control.IsOn ? 18 : 2)) < .001 && Math.Abs(point.Y - 2) < .001,
            $"Switch geometry differs from its 36x20 reference: {control.Name}, track={track.ActualWidth}x{track.ActualHeight}, thumb={thumb.ActualWidth}x{thumb.ActualHeight} at {point}.");
        var offColor = scheme == BleeckerColorScheme.Light ? "#FFE6D5B8" : "#FFFFFFFF";
        var onColor = scheme == BleeckerColorScheme.Light ? "#FF2C5784" : "#FF5BA3F5";
        var offOpacity = scheme == BleeckerColorScheme.Light ? .25 : .15;
        Require(Hex(track.Background) == (control.IsOn ? onColor : offColor) && Math.Abs(track.Background.Opacity - (control.IsOn ? 1 : offOpacity)) < .000001 &&
            Math.Abs(track.BorderBrush.Opacity - (control.IsOn ? 0 : scheme == BleeckerColorScheme.Light ? .35 : .10)) < .000001 &&
            Hex(thumb.Fill) == "#FFFFFFFF" && root.Opacity == (control.IsEnabled ? 1 : .5), $"Switch palette/disabled opacity differs for {control.Name}.");
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
        if (control.Name == "LongLabel") Require(control.ActualHeight == 80 && Math.Abs(label.ActualWidth - 152) < .01,
            $"Long switch label differs from its 152px-wide, four 20px reference lines: label={label.ActualWidth:R}x{label.ActualHeight:R}.");
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

    private async Task VerifyMotionAsync()
    {
        motionSamples.Clear();
        var animations = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
        Off.IsOn = true;
        var timer = Stopwatch.StartNew();
        var sawIntermediate = false;
        while (timer.ElapsedMilliseconds < 300)
        {
            var x = ((TranslateTransform)Find<Grid>(Off, "SwitchKnob").RenderTransform).X;
            motionSamples.Add(new { elapsed = timer.Elapsed.TotalMilliseconds, x });
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
    }

    private async Task VerifyLiveAppearanceAsync(BleeckerColorScheme scheme)
    {
        var host = (FrameworkElement)XamlRoot.Content;
        var originalTheme = host.RequestedTheme;
        var alternate = scheme == BleeckerColorScheme.Light ? BleeckerColorScheme.Dark : BleeckerColorScheme.Light;
        try
        {
            host.RequestedTheme = alternate == BleeckerColorScheme.Light ? ElementTheme.Light : ElementTheme.Dark;
            await WaitAsync(() => Hex(Find<Border>(On, "PART_Track").Background) == (alternate == BleeckerColorScheme.Light ? "#FF2C5784" : "#FF5BA3F5"),
                "Checked switch did not observe a live appearance change.");
            foreach (var control in Rows.Children.OfType<BleeckerSwitch>()) VerifyPresentation(control, alternate);
            Require(On.IsOn && !Off.IsOn && DisabledOn.IsOn && !DisabledOff.IsOn, "Appearance change altered consumer switch state.");
        }
        finally { host.RequestedTheme = originalTheme; }
        await WaitAsync(() => Hex(Find<Border>(On, "PART_Track").Background) == (scheme == BleeckerColorScheme.Light ? "#FF2C5784" : "#FF5BA3F5"),
            "Checked switch did not restore its original appearance.");
        foreach (var control in Rows.Children.OfType<BleeckerSwitch>()) VerifyPresentation(control, scheme);
    }

    private async Task VerifyPhysicalInputAsync()
    {
        physicalInputSteps.Clear();
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        var window = process.MainWindowHandle;
        Require(window != IntPtr.Zero, "Physical switch check could not identify its own window.");
        Require(GetCursorPos(out var originalPointer), "Physical switch check could not read its pointer position.");
        try
        {
            // The smoke process starts hidden. Expose only its own hosted-CI
            // window, then activate it through real pointer input if Windows
            // refuses a programmatic foreground request.
            ShowWindow(window, 9); // SW_RESTORE
            Require(SetWindowPos(window, new IntPtr(-1), 0, 0, 0, 0, 0x0043), "Could not expose the CI catalog window.");
            SetForegroundWindow(window);
            await Task.Delay(100);
            if (GetForegroundWindow() != window) await ClickAsync(Find<Border>(Off, "PART_Track"), false);
            await WaitAsync(() => GetForegroundWindow() == window, "Physical switch check did not own the foreground window.");
            physicalInputSteps.Add("Own catalog window acquired foreground input");
            Off.IsOn = false;
            await SettleAsync(Off, false);
            var native = Find<ToggleSwitch>(Off, "PART_Switch");
            Require(native.Focus(FocusState.Keyboard), "Native switch could not receive keyboard focus.");
            SendKey(0x20); // Space
            await WaitAsync(() => Off.IsOn, "Physical Space did not toggle the focused switch.");
            physicalInputSteps.Add("Space toggled the focused native input");
            SendKey(0x09); // Tab
            await WaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Find<ToggleSwitch>(On, "PART_Switch")),
                "Physical Tab did not reach the next switch.");
            SendKey(0x09);
            await WaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Find<ToggleSwitch>(WithoutLabel, "PART_Switch")),
                "Physical Tab did not skip both disabled switches.");
            physicalInputSteps.Add("Tab advanced once per switch and skipped disabled inputs");
            await ClickAsync(Find<Border>(Off, "PART_Track"));
            await WaitAsync(() => !Off.IsOn, "Physical track click did not toggle the switch off.");
            physicalInputSteps.Add("Track click toggled off");
            await ClickAsync(Find<TextBlock>(Off, "PART_Label"));
            await WaitAsync(() => Off.IsOn, "Physical label click did not activate its switch.");
            physicalInputSteps.Add("Label click toggled on");
            foreach (var control in new[] { DisabledOff, DisabledOn })
            {
                var before = control.IsOn;
                await ClickAsync(Find<Border>(control, "PART_Track"));
                Require(control.IsOn == before, "Physical click changed a disabled switch.");
            }
            physicalInputSteps.Add("Disabled off/on rejected physical activation");
            await ClickAsync(Find<Border>(Rtl, "PART_Track"));
            await WaitAsync(() => !Rtl.IsOn, "Physical RTL track click did not toggle off.");
            await ClickAsync(Find<TextBlock>(Rtl, "PART_Label"));
            await WaitAsync(() => Rtl.IsOn, "Physical RTL label click did not toggle on.");
            physicalInputSteps.Add("RTL track and label both activated the native input");
        }
        finally
        {
            Off.IsOn = false; Rtl.IsOn = true;
            SetWindowPos(window, new IntPtr(-2), 0, 0, 0, 0, 0x0013); // Remove temporary topmost status.
            SetCursorPos(originalPointer.X, originalPointer.Y);
        }
        await SettleAsync(Off, false);

        async Task ClickAsync(FrameworkElement target, bool requireForeground = true)
        {
            if (requireForeground) Require(GetForegroundWindow() == window, "Physical input lost its foreground window.");
            // Painted template parts such as Border do not own automation
            // peers. Anchor their visual bounds to the real native input's
            // screen rectangle and convert DIPs through this XamlRoot's scale.
            DependencyObject? ancestor = target;
            while (ancestor is not null && ancestor is not ToggleSwitch) ancestor = VisualTreeHelper.GetParent(ancestor);
            Require(ancestor is ToggleSwitch, "Physical switch target has no native input ancestor.");
            var input = (ToggleSwitch)ancestor!;
            var screen = FrameworkElementAutomationPeer.CreatePeerForElement(input).GetBoundingRectangle();
            var local = target.TransformToVisual(input).TransformBounds(new Rect(0, 0, target.ActualWidth, target.ActualHeight));
            var scale = XamlRoot.RasterizationScale;
            var bounds = new Rect(screen.X + local.X * scale, screen.Y + local.Y * scale, local.Width * scale, local.Height * scale);
            Require(bounds.Width > 0 && bounds.Height > 0, "Physical switch target has no screen bounds.");
            Require(SetCursorPos((int)(bounds.X + bounds.Width / 2), (int)(bounds.Y + bounds.Height / 2)), "Could not position CI pointer.");
            Send([new NativeInput { Type = 0, Data = new InputData { Mouse = new MouseInput { Flags = 0x0002 } } },
                  new NativeInput { Type = 0, Data = new InputData { Mouse = new MouseInput { Flags = 0x0004 } } }]);
            await Task.Delay(100);
        }
        void SendKey(ushort key) => Send([new NativeInput { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Key = key } } },
            new NativeInput { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Key = key, Flags = 2 } } }]);
        void Send(NativeInput[] inputs) => Require(SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeInput>()) == inputs.Length,
            "Windows rejected physical CI input.");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeInput { public uint Type; public InputData Data; }
    [StructLayout(LayoutKind.Explicit)]
    private struct InputData { [FieldOffset(0)] public MouseInput Mouse; [FieldOffset(0)] public KeyboardInput Keyboard; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput { public int X, Y; public uint MouseData, Flags, Time; public IntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput { public ushort Key, Scan; public uint Flags, Time; public IntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)]
    private struct CursorPoint { public int X, Y; }
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, NativeInput[] inputs, int size);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetCursorPos(out CursorPoint point);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetCursorPos(int x, int y);

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
