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
    private readonly List<object> richSamples = [];
    public CheckboxesFixture() { InitializeComponent(); DataContext = state; }

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        var controls = Rows.Children.OfType<BleeckerCheckbox>().ToArray();
        var passed = false;
        Exception? originalFailure = null;
        var verifiedSteps = new List<string>();
        richSamples.Clear();
        try
        {
            UpdateLayout();
            await MainWindow.CaptureElementAsync(this, Path.Combine(folder, $"checkbox-initial-{scheme}.png"));
            foreach (var control in controls) VerifyPresentation(control, scheme);
            Require(Default.ActualHeight == 42 && Math.Abs(Default.ActualWidth - 291.71875) < .1,
                $"Checkbox default differs from the reference 291.71875x42: {Default.ActualWidth:R}x{Default.ActualHeight:R}.");
            Require(Bare.ActualWidth == 20 && Bare.ActualHeight == 20 && DescriptionOnly.ActualHeight == 22, "Bare or description-only checkbox retained the wrong spacing.");
            Require(Rich.ActualHeight == 42 && Math.Abs(Rich.ActualWidth - 165.109375) < .1 &&
                Rich.Content is TextBlock { LineHeight: 20 } && Rich.Description is TextBlock { LineHeight: 20 },
                $"Rich checkbox lost the reference's 20px line height: {JsonSerializer.Serialize(Measure(Rich))}.");
            Require(Long.ActualHeight > 42 && Find<TextBlock>(Long, "PART_Label").ActualHeight > 20,
                "Constrained checkbox text did not wrap on first display.");
            verifiedSteps.Add("Initial row geometry, palette, opacity separation and wrapping");
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
            verifiedSteps.Add("Native UIA role/name/value and exactly-once consumer events");
            foreach (var control in new[] { DisabledOff, DisabledOn })
            {
                var before = control.IsChecked; var rejected = false;
                try { ((IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(control).GetPattern(PatternInterface.Toggle)).Toggle(); }
                catch (ElementNotEnabledException) { rejected = true; }
                catch (COMException error) when (error.HResult == unchecked((int)0x80040200)) { rejected = true; }
                Require(rejected && control.IsChecked == before && !control.Focus(FocusState.Keyboard), "Disabled checkbox accepted activation or keyboard focus.");
            }
            verifiedSteps.Add("Disabled off/on rejected native activation and focus");
            var bound = (IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Bound).GetPattern(PatternInterface.Toggle);
            bound.Toggle(); await SettleAsync(Bound);
            Require(state.Selected && BindingStatus.Text == "True", "Checkbox did not update its consumer binding.");
            state.Selected = false; await SettleAsync(Bound);
            Require(Bound.IsChecked == false && bound.ToggleState == ToggleState.Off && BindingStatus.Text == "False", "Consumer binding did not update the checkbox.");
            verifiedSteps.Add("Two-way consumer binding in both directions");
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
            verifiedSteps.Add("Live text, explicit accessible name and consumer rich-content identity");
            try
            {
                var consumerStyle = (Style)Resources["ConsumerCheckboxTextStyle"];
                var styled = new TextBlock { Text = "Consumer label", Style = consumerStyle };
                Rich.Content = styled; UpdateLayout();
                richSamples.Add(new { phase = "consumer-style", row = Measure(Rich) });
                Require(ReferenceEquals(styled.Style, consumerStyle) && styled.LineHeight == 28 && Rich.ActualHeight == 50,
                    "Checkbox replaced the consumer's explicit rich text style.");
                await MainWindow.CaptureElementAsync(Rich, Path.Combine(folder, $"checkbox-rich-style-{scheme}.png"));
                var local = new TextBlock { Text = "Consumer label", LineHeight = 30, LineStackingStrategy = LineStackingStrategy.BlockLineHeight };
                Rich.Content = local; UpdateLayout();
                richSamples.Add(new { phase = "local-properties", row = Measure(Rich) });
                Require(local.LineHeight == 30 && Rich.ActualHeight == 52, "Checkbox replaced the consumer's local rich text properties.");
                await MainWindow.CaptureElementAsync(Rich, Path.Combine(folder, $"checkbox-rich-local-{scheme}.png"));
                var data = new ConsumerLabel("Consumer label", "Second template line");
                var template = (DataTemplate)Resources["ConsumerCheckboxLabelTemplate"];
                Rich.Content = data; Rich.ContentTemplate = template; UpdateLayout();
                await WaitAsync(() => Find<TextBlock>(Rich, "ConsumerTitle").Text == data.Title && Find<TextBlock>(Rich, "ConsumerDetails").Text == data.Details,
                    "Consumer template bindings did not reach the owned data.");
                var templateSnapshot = new { phase = "data-template", row = Measure(Rich),
                    title = MeasureRichContent(Find<TextBlock>(Rich, "ConsumerTitle")), details = MeasureRichContent(Find<TextBlock>(Rich, "ConsumerDetails")),
                    contextType = Find<TextBlock>(Rich, "ConsumerTitle").DataContext?.GetType().FullName };
                richSamples.Add(templateSnapshot);
                await MainWindow.CaptureElementAsync(Rich, Path.Combine(folder, $"checkbox-rich-template-{scheme}.png"));
                Require(ReferenceEquals(Rich.Content, data) && ReferenceEquals(Rich.ContentTemplate, template) &&
                    Find<TextBlock>(Rich, "ConsumerTitle").Text == data.Title && Find<TextBlock>(Rich, "ConsumerDetails").Text == data.Details &&
                    Find<TextBlock>(Rich, "ConsumerTitle").LineHeight == 20 && Find<TextBlock>(Rich, "ConsumerDetails").LineHeight == 20 && Rich.ActualHeight == 62,
                    $"Consumer data template did not receive the checkbox's branded defaults: {JsonSerializer.Serialize(templateSnapshot)}.");
                Rich.Content = data with { Title = "Updated template label" }; UpdateLayout();
                await WaitAsync(() => Find<TextBlock>(Rich, "ConsumerTitle").Text == "Updated template label", "Updated consumer data did not reach the label template.");
                verifiedSteps.Add("Consumer rich styles, local line height and data templates");
            }
            finally
            {
                richSamples.Add(new { phase = "before-restoration", row = Measure(Rich),
                    contentType = Rich.Content?.GetType().FullName,
                    templateText = HostedText(Find<ContentPresenter>(Rich, "PART_RichLabel")).Select(text => new {
                        metrics = MeasureRichContent(text), contextType = text.DataContext?.GetType().FullName }).ToArray() });
                Rich.ContentTemplate = null; Rich.Content = richLabel; UpdateLayout();
            }
            Require(ReferenceEquals(Find<ContentPresenter>(Rich, "PART_RichLabel").Content, richLabel) && Rich.ActualHeight == 42,
                "Restoring rich content lost its identity or branded geometry.");
            Require(Default.Focus(FocusState.Keyboard), "Checkbox did not receive native keyboard focus.");
            await WaitAsync(() => Find<Border>(Default, "PART_FocusRing").Opacity == 1, "Keyboard checkbox focus ring did not settle.");
            Require(Find<Border>(Default, "PART_FocusRing").ActualWidth == 28 && Find<Border>(Default, "PART_FocusOffset").ActualWidth == 24,
                "Checkbox focus ring lost its 2px ring and 2px offset.");
            Require(Default.Focus(FocusState.Pointer), "Checkbox did not receive pointer focus.");
            await WaitAsync(() => Find<Border>(Default, "PART_FocusRing").Opacity == 0, "Pointer focus retained a keyboard ring.");
            verifiedSteps.Add("Native keyboard and pointer focus modalities");
            foreach (var visualState in new[] { "UncheckedPointerOver", "UncheckedPressed", "UncheckedNormal" })
            {
                Require(VisualStateManager.GoToState(Off, visualState, false), $"Missing checkbox state {visualState}.");
                await SettleAsync(Off);
                var border = Find<Border>(Off, "PART_Box").BorderBrush;
                Require(Math.Abs(border.Opacity - (float)(visualState == "UncheckedPointerOver" ? .5 : scheme == BleeckerColorScheme.Light ? .6 : .2)) < 1e-9,
                    $"Checkbox border differs in {visualState}.");
            }
            verifiedSteps.Add("Unchecked hover, pressed and normal border states");
            foreach (var control in new[] { Off, Rich })
            {
                var template = control.Template;
                var checkedValue = control.IsChecked;
                var labelValue = control.Content;
                var descriptionValue = control.Description;
                try
                {
                    control.Template = null; control.ApplyTemplate();
                    Require(control.IsChecked == checkedValue && ReferenceEquals(control.Content, labelValue) &&
                        ReferenceEquals(control.Description, descriptionValue), "Clearing the checkbox template changed consumer state or content.");
                    verifiedSteps.Add($"{control.Name} cleared its template with retained state and content");
                }
                finally { control.Template = template; control.ApplyTemplate(); }
                verifiedSteps.Add($"{control.Name} restored its template");
            }
            UpdateLayout();
            await SettleAsync(Off); VerifyPresentation(Off, scheme);
            Require(ReferenceEquals(Find<ContentPresenter>(Rich, "PART_RichLabel").Content, richLabel) &&
                ReferenceEquals(Find<ContentPresenter>(Rich, "PART_RichDescription").Content, richDescription), "Template replacement lost consumer-owned rich content.");
            verifiedSteps.Add("Plain and rich template replacement with retained content");
            await VerifyMotionAsync();
            verifiedSteps.Add("Applicable intermediate/reversed or reduced glyph motion");
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
            verifiedSteps.Add("All rows changed and restored appearance with retained consumer state");
            passed = true;
        }
        catch (Exception error) { originalFailure = error; throw; }
        finally
        {
            await File.WriteAllTextAsync(Path.Combine(folder, $"checkbox-measurements-{scheme}.json"),
                JsonSerializer.Serialize(new { passed, scheme, displayScale = XamlRoot.RasterizationScale,
                    animationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled,
                    originalFailure = originalFailure?.ToString(), verifiedSteps, measurements = controls.Select(MeasureForReport).ToArray(), motion, richSamples }, new JsonSerializerOptions { WriteIndented = true }));
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
        var expected = checkedState ? scheme == BleeckerColorScheme.Light ? "#FF2C5784" : "#FF5BA3F5" : scheme == BleeckerColorScheme.Light ? "#FFFFFFFF" : "#FF182533";
        var sea = scheme == BleeckerColorScheme.Light ? "#FF2C5784" : "#FF5BA3F5";
        Require(Hex(Find<Border>(control, "PART_FocusRing").BorderBrush) == sea &&
            Math.Abs(Find<Border>(control, "PART_FocusRing").BorderBrush.Opacity - (float).2) < 1e-9 &&
            Hex(Find<Border>(control, "PART_FocusOffset").Background) == (scheme == BleeckerColorScheme.Light ? "#FFFFFFFF" : "#FF182533"),
            "Checkbox focus colors differ from the active Bleecker theme.");
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
            richLabel = MeasureRichContent(control.Content), richDescription = MeasureRichContent(control.Description),
            label = new { label.Text, label.ActualWidth, label.ActualHeight, label.BaselineOffset },
            description = new { description.Text, description.ActualWidth, description.ActualHeight, description.BaselineOffset } };
    }
    private static object? MeasureRichContent(object? content) => content is TextBlock text
        ? new { text.Text, text.ActualWidth, text.ActualHeight, text.LineHeight,
            strategy = text.LineStackingStrategy.ToString(), family = text.FontFamily.Source, weight = text.FontWeight.Weight }
        : null;
    private static IEnumerable<TextBlock> HostedText(DependencyObject parent)
    {
        if (parent is TextBlock text) yield return text;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            foreach (var child in HostedText(VisualTreeHelper.GetChild(parent, index))) yield return child;
    }
    private object MeasureForReport(BleeckerCheckbox control)
    {
        try { return Measure(control); }
        catch (InvalidOperationException error) when (error.Message.StartsWith("Missing checkbox part ", StringComparison.Ordinal))
        { return new { control.Name, measurementError = error.Message }; }
    }
    private Task SettleAsync(BleeckerCheckbox control) => WaitAsync(() =>
        Find<Viewbox>(control, "PART_Glyph").Opacity == (control.IsChecked == true ? 1 : 0) &&
        Find<Grid>(control, "PART_InputChrome").Opacity == (control.IsEnabled ? 1 : .5) &&
        Hex(Find<Border>(control, "PART_Box").Background) == (control.IsChecked == true ? control.ActualTheme == ElementTheme.Light ? "#FF2C5784" : "#FF5BA3F5" : control.ActualTheme == ElementTheme.Light ? "#FFFFFFFF" : "#FF182533") &&
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
    private sealed record ConsumerLabel(string Title, string Details);
    private sealed class ConsumerState : INotifyPropertyChanged
    {
        private bool selected;
        public bool Selected { get => selected; set { if (selected == value) return; selected = value; PropertyChanged?.Invoke(this, new(nameof(Selected))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
