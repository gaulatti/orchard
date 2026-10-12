using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class CheckboxesFixture : UserControl
{
    private readonly ConsumerState state = new();
    private readonly List<object> motion = [];
    public CheckboxesFixture() { InitializeComponent(); DataContext = state; }

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        var controls = Rows.Children.OfType<BleeckerCheckbox>().ToArray();
        var passed = false;
        try
        {
            UpdateLayout();
            foreach (var control in controls) VerifyPresentation(control, scheme);
            Require(Default.ActualHeight == 42 && Math.Abs(Default.ActualWidth - 291.71875) < .1,
                $"Checkbox default differs from the reference 291.71875x42: {Default.ActualWidth:R}x{Default.ActualHeight:R}.");
            Require(Bare.ActualWidth == 20 && Bare.ActualHeight == 20 && DescriptionOnly.ActualHeight == 22, "Bare or description-only checkbox retained the wrong spacing.");
            Require(Long.ActualHeight > 42 && Find<TextBlock>(Long, "PART_Label").ActualHeight > 20,
                "Constrained checkbox text did not wrap on first display.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(Default);
            Require(peer.GetAutomationControlType() == AutomationControlType.CheckBox && peer.IsControlElement() && peer.IsContentElement() &&
                peer.GetName() == "Enable notifications Send alerts when a device starts playback." && peer.IsKeyboardFocusable(),
                "Checkbox lost its native role, keyboard access or complete label.");
            var provider = (IToggleProvider)peer.GetPattern(PatternInterface.Toggle);
            var events = 0;
            void Count(object sender, RoutedEventArgs args) => events++;
            Default.Checked += Count; Default.Unchecked += Count;
            try
            {
                provider.Toggle(); await SettleAsync(Default);
                Require(Default.IsChecked == false && provider.ToggleState == ToggleState.Off && events == 1, "UIA toggle did not reach consumer state exactly once.");
                Default.IsChecked = true; await SettleAsync(Default);
                Require(provider.ToggleState == ToggleState.On && events == 2, "Consumer state did not reach the native checkbox exactly once.");
            }
            finally { Default.Checked -= Count; Default.Unchecked -= Count; }
            foreach (var control in new[] { DisabledOff, DisabledOn })
            {
                var before = control.IsChecked; var rejected = false;
                try { ((IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(control).GetPattern(PatternInterface.Toggle)).Toggle(); }
                catch (ElementNotEnabledException) { rejected = true; }
                catch (COMException error) when (error.HResult == unchecked((int)0x80040200)) { rejected = true; }
                Require(rejected && control.IsChecked == before && !control.Focus(FocusState.Keyboard), "Disabled checkbox accepted activation or keyboard focus.");
            }
            var bound = (IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Bound).GetPattern(PatternInterface.Toggle);
            bound.Toggle(); await SettleAsync(Bound);
            Require(state.Selected && BindingStatus.Text == "True", "Checkbox did not update its consumer binding.");
            state.Selected = false; await SettleAsync(Bound);
            Require(Bound.IsChecked == false && bound.ToggleState == ToggleState.Off && BindingStatus.Text == "False", "Consumer binding did not update the checkbox.");
            Default.Content = "Updated label"; Default.Description = "Updated description"; UpdateLayout();
            Require(peer.GetName() == "Updated label Updated description" && Find<TextBlock>(Default, "PART_Label").Text == "Updated label",
                "Checkbox content update lost presentation or its complete name.");
            AutomationProperties.SetName(Default, "Explicit name");
            Require(peer.GetName() == "Explicit name", "Checkbox ignored the consumer's explicit accessible name.");
            AutomationProperties.SetName(Default, "");
            Default.Content = "Enable notifications"; Default.Description = "Send alerts when a device starts playback.";
            var richLabel = Rich.Content; var richDescription = Rich.Description;
            Require(ReferenceEquals(Find<ContentPresenter>(Rich, "PART_RichLabel").Content, richLabel) &&
                ReferenceEquals(Find<ContentPresenter>(Rich, "PART_RichDescription").Content, richDescription), "Checkbox replaced consumer-owned rich content.");
            Require(Default.Focus(FocusState.Keyboard), "Checkbox did not receive native keyboard focus.");
            await WaitAsync(() => Find<Border>(Default, "PART_FocusRing").Opacity == 1, "Keyboard checkbox focus ring did not settle.");
            Require(Find<Border>(Default, "PART_FocusRing").ActualWidth == 28 && Find<Border>(Default, "PART_FocusOffset").ActualWidth == 24,
                "Checkbox focus ring lost its 2px ring and 2px offset.");
            Require(Default.Focus(FocusState.Pointer), "Checkbox did not receive pointer focus.");
            await WaitAsync(() => Find<Border>(Default, "PART_FocusRing").Opacity == 0, "Pointer focus retained a keyboard ring.");
            foreach (var visualState in new[] { "UncheckedPointerOver", "UncheckedPressed", "UncheckedNormal" })
            {
                Require(VisualStateManager.GoToState(Off, visualState, false), $"Missing checkbox state {visualState}.");
                await SettleAsync(Off);
                var border = Find<Border>(Off, "PART_Box").BorderBrush;
                Require(Math.Abs(border.Opacity - (float)(visualState == "UncheckedPointerOver" ? .5 : scheme == BleeckerColorScheme.Light ? .6 : .2)) < 1e-9,
                    $"Checkbox border differs in {visualState}.");
            }
            var template = Off.Template;
            Off.Template = null; Off.ApplyTemplate(); Off.Template = template; Off.ApplyTemplate(); UpdateLayout();
            await SettleAsync(Off); VerifyPresentation(Off, scheme);
            await VerifyMotionAsync();
            var original = ((FrameworkElement)XamlRoot.Content).RequestedTheme;
            var values = controls.Select(control => control.IsChecked).ToArray();
            try
            {
                ((FrameworkElement)XamlRoot.Content).RequestedTheme = scheme == BleeckerColorScheme.Light ? ElementTheme.Dark : ElementTheme.Light;
                foreach (var control in controls) await SettleAsync(control);
                foreach (var control in controls) VerifyPresentation(control, scheme == BleeckerColorScheme.Light ? BleeckerColorScheme.Dark : BleeckerColorScheme.Light);
                Require(controls.Select(control => control.IsChecked).SequenceEqual(values), "Appearance change altered checkbox consumer state.");
            }
            finally { ((FrameworkElement)XamlRoot.Content).RequestedTheme = original; }
            foreach (var control in controls) await SettleAsync(control);
            foreach (var control in controls) VerifyPresentation(control, scheme);
            passed = true;
        }
        finally
        {
            await File.WriteAllTextAsync(Path.Combine(folder, $"checkbox-measurements-{scheme}.json"),
                JsonSerializer.Serialize(new { passed, scheme, displayScale = XamlRoot.RasterizationScale,
                    animationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled,
                    measurements = controls.Select(Measure).ToArray(), motion }, new JsonSerializerOptions { WriteIndented = true }));
            await MainWindow.CaptureElementAsync(this, Path.Combine(folder, $"checkbox-{scheme}.png"));
        }
    }

    private async Task VerifyMotionAsync()
    {
        motion.Clear();
        var animations = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
        Off.IsChecked = true;
        var timer = Stopwatch.StartNew(); var intermediate = false;
        while (timer.ElapsedMilliseconds < 350)
        {
            var alpha = Find<Viewbox>(Off, "PART_Glyph").Opacity;
            motion.Add(new { elapsed = timer.Elapsed.TotalMilliseconds, glyphOpacity = alpha, fill = Hex(Find<Border>(Off, "PART_Box").Background) });
            intermediate |= alpha > 0 && alpha < 1;
            if (alpha == 1) break;
            await Task.Delay(1);
        }
        Require(!animations || intermediate, "Enabled checkbox motion skipped every intermediate glyph frame.");
        Require(animations || Find<Viewbox>(Off, "PART_Glyph").Opacity == 1, "Reduced-motion checkbox did not settle immediately.");
        await SettleAsync(Off); Off.IsChecked = false;
        if (animations)
        {
            await WaitAsync(() => Find<Viewbox>(Off, "PART_Glyph").Opacity is > 0 and < 1, "Checkbox reversal did not start from an intermediate frame.");
            Off.IsChecked = true; await SettleAsync(Off); Off.IsChecked = false;
        }
        await SettleAsync(Off);
    }
    private void VerifyPresentation(BleeckerCheckbox control, BleeckerColorScheme scheme)
    {
        var box = Find<Border>(control, "PART_Box"); var glyph = Find<Viewbox>(control, "PART_Glyph");
        var label = Find<TextBlock>(control, "PART_Label"); var description = Find<TextBlock>(control, "PART_Description");
        var checkedState = control.IsChecked == true;
        var expected = checkedState ? scheme == BleeckerColorScheme.Light ? "#FF2C5784" : "#FF5BA3F5" : scheme == BleeckerColorScheme.Light ? "#FFFFFFFF" : "#FF1A374D";
        Require(box.ActualWidth == 20 && box.ActualHeight == 20 && box.CornerRadius == new CornerRadius(4) && box.BorderThickness == new Thickness(1) &&
            glyph.ActualWidth == 14 && glyph.ActualHeight == 14 && glyph.Opacity == (checkedState ? 1 : 0) && glyph.FlowDirection == FlowDirection.LeftToRight,
            $"Checkbox geometry/glyph differs: {JsonSerializer.Serialize(Measure(control))}.");
        Require(Hex(box.Background) == expected && box.Background.Opacity == 1 &&
            Find<Grid>(control, "PART_InputChrome").Opacity == (control.IsEnabled ? 1 : .5) &&
            Find<Grid>(control, "PART_TextColumn").Opacity == 1 && Find<Grid>(control, "PART_Root").Opacity == 1,
            $"Checkbox palette or disabled text/glyph opacity differs: {JsonSerializer.Serialize(Measure(control))}.");
        Require(control.FontFamily.Source.Contains("EncodeSans-Medium.ttf") && label.FontWeight.Weight == 500 && description.FontWeight.Weight == 400 &&
            description.FontFamily.Source.Contains("EncodeSans-Regular.ttf") && label.LineHeight == 20 && description.LineHeight == 20 &&
            Find<Grid>(control, "PART_TextColumn").Margin.Left == 12 && description.Margin.Top == 2 && !control.UseSystemFocusVisuals,
            "Checkbox acquired Windows typography, spacing or focus styling.");
    }
    private object Measure(BleeckerCheckbox control)
    {
        var box = Find<Border>(control, "PART_Box"); var label = Find<TextBlock>(control, "PART_Label"); var description = Find<TextBlock>(control, "PART_Description");
        var glyph = Find<Viewbox>(control, "PART_Glyph");
        return new { control.Name, control.IsChecked, control.IsEnabled, control.ActualWidth, control.ActualHeight,
            box = new { box.ActualWidth, box.ActualHeight, fill = Hex(box.Background), opacity = box.Background.Opacity, stroke = Hex(box.BorderBrush), strokeOpacity = box.BorderBrush.Opacity },
            glyph = new { glyph.ActualWidth, glyph.ActualHeight, glyph.Opacity, direction = glyph.FlowDirection.ToString(),
                bounds = glyph.TransformToVisual(control).TransformBounds(new Rect(0, 0, glyph.ActualWidth, glyph.ActualHeight)) },
            inputOpacity = Find<Grid>(control, "PART_InputChrome").Opacity,
            label = new { label.Text, label.ActualWidth, label.ActualHeight, label.BaselineOffset },
            description = new { description.Text, description.ActualWidth, description.ActualHeight, description.BaselineOffset } };
    }
    private Task SettleAsync(BleeckerCheckbox control) => WaitAsync(() =>
        Find<Viewbox>(control, "PART_Glyph").Opacity == (control.IsChecked == true ? 1 : 0) &&
        Find<Grid>(control, "PART_InputChrome").Opacity == (control.IsEnabled ? 1 : .5) &&
        Hex(Find<Border>(control, "PART_Box").Background) == (control.IsChecked == true ? control.ActualTheme == ElementTheme.Light ? "#FF2C5784" : "#FF5BA3F5" : control.ActualTheme == ElementTheme.Light ? "#FFFFFFFF" : "#FF1A374D") &&
        Math.Abs(Find<Border>(control, "PART_Box").BorderBrush.Opacity - (float)(control.IsChecked == true ? 1 :
            VisualStateManager.GetVisualStateGroups(Find<Grid>(control, "PART_Root")).Single(group => group.Name == "CombinedStates").CurrentState?.Name.EndsWith("PointerOver", StringComparison.Ordinal) == true ? .5 : control.ActualTheme == ElementTheme.Light ? .6 : .2)) < 1e-9,
        $"Checkbox {control.Name} did not settle to its native state.");
    private async Task WaitAsync(Func<bool> condition, string message) { var timer = Stopwatch.StartNew(); while (!condition() && timer.ElapsedMilliseconds < 2000) { UpdateLayout(); await Task.Delay(1); } Require(condition(), message); }
    private static string Hex(Brush brush) { var color = ((SolidColorBrush)brush).Color; return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}"; }
    private static T Find<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        if (parent is T element && element.Name == name) return element;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            try { return Find<T>(VisualTreeHelper.GetChild(parent, index), name); } catch (InvalidOperationException) { }
        }
        throw new InvalidOperationException($"Missing checkbox part {name}.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class ConsumerState : INotifyPropertyChanged
    {
        private bool selected;
        public bool Selected { get => selected; set { if (selected == value) return; selected = value; PropertyChanged?.Invoke(this, new(nameof(Selected))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
