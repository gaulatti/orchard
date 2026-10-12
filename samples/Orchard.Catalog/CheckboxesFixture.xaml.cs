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
using System.Windows.Input;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class CheckboxesFixture : UserControl
{
    private readonly ConsumerState state = new();
    private readonly List<object> motion = [];
    private readonly List<object> richSamples = [];
    private readonly List<object> templateSamples = [];
    private readonly List<string> physicalInputSteps = [];
    public CheckboxesFixture() { InitializeComponent(); DataContext = state; }

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        var controls = Rows.Children.OfType<BleeckerCheckbox>().ToArray();
        var passed = false;
        Exception? originalFailure = null;
        var verifiedSteps = new List<string>();
        richSamples.Clear();
        templateSamples.Clear();
        physicalInputSteps.Clear();
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
                var templateTitle = Find<TextBlock>(Rich, "ConsumerTitle");
                templateTitle.Style = consumerStyle; UpdateLayout();
                await WaitAsync(() => templateTitle.LineHeight == 28 && Rich.ActualHeight == 70,
                    "A live consumer style did not replace the checkbox's owned line defaults.");
                Require(ReferenceEquals(templateTitle.Style, consumerStyle), "Checkbox replaced a live consumer text style.");
                templateTitle.Style = null; UpdateLayout();
                await WaitAsync(() => templateTitle.LineHeight == 20 && Rich.ActualHeight == 62,
                    "Removing the consumer style did not restore the branded line defaults.");
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
            await InspectFontFallbackAsync(scheme, folder);
            await InspectScriptWeightsAsync(scheme, folder);
            foreach (var control in new[] { Off, Rich })
            {
                var template = control.Template;
                var checkedValue = control.IsChecked;
                var labelValue = control.Content;
                var descriptionValue = control.Description;
                try
                {
                    templateSamples.Add(DescribeTemplate(control, "before-clear"));
                    control.Template = null;
                    templateSamples.Add(DescribeTemplate(control, "assigned-null"));
                    // Native ContentControl creates its own content visuals if
                    // a null template is applied. Restore the branded template
                    // before layout; Visibility is the supported hide/show path.
                    Require(control.Template is null && VisualTreeHelper.GetChildrenCount(control) == 0 &&
                        control.IsChecked == checkedValue && ReferenceEquals(control.Content, labelValue) &&
                        ReferenceEquals(control.Description, descriptionValue), "Clearing the checkbox template changed consumer state or content.");
                    control.IsChecked = checkedValue != true;
                    Require(((IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(control).GetPattern(PatternInterface.Toggle)).ToggleState ==
                        (control.IsChecked == true ? ToggleState.On : ToggleState.Off), "Consumer updates lost the native value while the template was cleared.");
                    control.IsChecked = checkedValue;
                    if (labelValue is string)
                    {
                        try
                        {
                            control.Content = "Updated while empty"; control.Description = "Consumer description while empty";
                            Require(FrameworkElementAutomationPeer.CreatePeerForElement(control).GetName() == "Updated while empty Consumer description while empty",
                                "Consumer content updates lost the accessible name while the template was cleared.");
                        }
                        finally { control.Content = labelValue; control.Description = descriptionValue; }
                    }
                    verifiedSteps.Add($"{control.Name} detached its template with retained state and content");
                }
                finally
                {
                    control.Template = template;
                    templateSamples.Add(DescribeTemplate(control, "assigned-original"));
                    control.ApplyTemplate();
                    templateSamples.Add(DescribeTemplate(control, "restored"));
                }
                verifiedSteps.Add($"{control.Name} restored its template");
            }
            UpdateLayout();
            await SettleAsync(Off); VerifyPresentation(Off, scheme);
            Require(ReferenceEquals(Find<ContentPresenter>(Rich, "PART_RichLabel").Content, richLabel) &&
                ReferenceEquals(Find<ContentPresenter>(Rich, "PART_RichDescription").Content, richDescription), "Template replacement lost consumer-owned rich content.");
            verifiedSteps.Add("Plain and rich template replacement with retained content");
            foreach (var control in new[] { Off, Rich })
            {
                var checkedValue = control.IsChecked;
                var labelValue = control.Content;
                var descriptionValue = control.Description;
                var template = control.Template;
                // UIA does not expose a fresh Toggle pattern for a collapsed
                // control. Retain the provider while the control is visible.
                var toggle = (IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(control).GetPattern(PatternInterface.Toggle);
                try
                {
                    control.Visibility = Visibility.Collapsed;
                    UpdateLayout(); await Task.Delay(1); UpdateLayout();
                    Require(control.DesiredSize.Width == 0 && control.DesiredSize.Height == 0 &&
                        ReferenceEquals(control.Template, template) && ReferenceEquals(control.Content, labelValue) &&
                        ReferenceEquals(control.Description, descriptionValue), "Hiding a checkbox changed its template or consumer content.");
                    control.IsChecked = checkedValue != true;
                    Require(toggle.ToggleState ==
                        (control.IsChecked == true ? ToggleState.On : ToggleState.Off), "Hidden checkbox lost consumer value updates.");
                }
                finally { control.IsChecked = checkedValue; control.Visibility = Visibility.Visible; }
                UpdateLayout(); await SettleAsync(control); VerifyPresentation(control, scheme);
                Require(ReferenceEquals(control.Content, labelValue) && ReferenceEquals(control.Description, descriptionValue),
                    "Showing a checkbox lost consumer-owned content.");
            }
            Require(ReferenceEquals(Find<ContentPresenter>(Rich, "PART_RichLabel").Content, richLabel) &&
                ReferenceEquals(Find<ContentPresenter>(Rich, "PART_RichDescription").Content, richDescription), "Hide/show lost rich presenter ownership.");
            verifiedSteps.Add("Plain and rich hide/show retained the branded template, consumer content and native state");
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
            await VerifyPhysicalInputAsync(scheme, folder);
            verifiedSteps.Add("Physical hover/press/exit, Space/Tab, box/label/description/RTL clicks, disabled rejection and native commands");
            passed = true;
        }
        catch (Exception error) { originalFailure = error; throw; }
        finally
        {
            await File.WriteAllTextAsync(Path.Combine(folder, $"checkbox-measurements-{scheme}.json"),
                JsonSerializer.Serialize(new { passed, scheme, displayScale = XamlRoot.RasterizationScale,
                    animationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled,
                    originalFailure = originalFailure?.ToString(), verifiedSteps, physicalInputSteps,
                    measurements = controls.Select(MeasureForReport).ToArray(), motion, richSamples, templateSamples }, new JsonSerializerOptions { WriteIndented = true }));
            await MainWindow.CaptureElementAsync(this, Path.Combine(folder, $"checkbox-{scheme}.png"));
        }
    }

    private async Task VerifyPhysicalInputAsync(BleeckerColorScheme scheme, string folder)
    {
        using var input = new NativeInputSession(XamlRoot, folder, $"checkbox-{scheme}");
        var defaultValue = Default.IsChecked; var rtlValue = Rtl.IsChecked; var offValue = Off.IsChecked;
        var originalCommand = Default.Command; var originalParameter = Default.CommandParameter;
        var command = new ConsumerCommand(); var parameter = new object();
        var clicks = 0; var changes = 0;
        void Clicked(object sender, RoutedEventArgs args) => clicks++;
        void Changed(object sender, RoutedEventArgs args) => changes++;
        try
        {
            await input.AcquireAsync(CheckboxHeading);
            physicalInputSteps.Add("Own catalog window acquired foreground input");
            Off.IsChecked = false;
            await input.MoveAsync(Find<Border>(Off, "PART_Box"));
            await WaitAsync(() => Off.IsChecked == false && Math.Abs(Find<Border>(Off, "PART_Box").BorderBrush.Opacity - .5) < 1e-9,
                "Physical unchecked hover did not reach the source border.");
            await MainWindow.CaptureElementAsync(Off, Path.Combine(folder, $"checkbox-hover-{scheme}.png"));
            input.PointerButton(true);
            try
            {
                await WaitAsync(() => Off.IsChecked == false &&
                    VisualStateManager.GetVisualStateGroups(Find<Grid>(Off, "PART_Root")).Single(group => group.Name == "CombinedStates").CurrentState?.Name == "UncheckedPressed" &&
                    Math.Abs(Find<Border>(Off, "PART_Box").BorderBrush.Opacity - .5) < 1e-9,
                    "Physical unchecked press did not retain the source hover border.");
                await MainWindow.CaptureElementAsync(Off, Path.Combine(folder, $"checkbox-pressed-{scheme}.png"));
            }
            finally { input.PointerButton(false); }
            await WaitAsync(() => Off.IsChecked == true, "Physical release did not activate its checkbox.");
            await input.MoveAsync(Find<Border>(Default, "PART_Box"));
            Off.IsChecked = false; await SettleAsync(Off);
            Require(Math.Abs(Find<Border>(Off, "PART_Box").BorderBrush.Opacity - (float)(scheme == BleeckerColorScheme.Light ? .6 : .2)) < 1e-9,
                "Physical pointer exit did not restore the source normal border.");
            await MainWindow.CaptureElementAsync(Off, Path.Combine(folder, $"checkbox-normal-{scheme}.png"));
            physicalInputSteps.Add("Real hover/held press/release/pointer exit matched source border states and activation");
            Default.IsChecked = true; await SettleAsync(Default);
            Default.Command = command; Default.CommandParameter = parameter;
            Default.Click += Clicked; Default.Checked += Changed; Default.Unchecked += Changed;
            Require(Default.Focus(FocusState.Keyboard), "Physical checkbox could not receive keyboard focus.");
            input.Key(0x20);
            await WaitAsync(() => Default.IsChecked == false, "Physical Space did not toggle the checkbox.");
            await WaitAsync(() => Find<Border>(Default, "PART_FocusRing").Opacity == 1, "Physical keyboard focus lost its branded ring.");
            await Task.Delay(50);
            var focusCapture = Path.Combine(folder, $"checkbox-keyboard-window-{scheme}.png");
            await NativeScreenCapture.CaptureClientAsync(input.Window, focusCapture);
            await VerifyFocusPixelsAsync(input, focusCapture, scheme, folder);
            physicalInputSteps.Add("Space toggled the focused checkbox and retained its keyboard ring");
            input.Key(0x09);
            await WaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Off), "Physical Tab did not reach the next checkbox.");
            input.Key(0x09);
            await WaitAsync(() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Bare), "Physical Tab did not skip disabled checkboxes.");
            physicalInputSteps.Add("Tab advanced once per checkbox and skipped disabled off/on");
            await input.ClickAsync(Find<Border>(Default, "PART_Box"));
            await WaitAsync(() => Default.IsChecked == true, "Physical box click did not toggle on.");
            await WaitAsync(() => Find<Border>(Default, "PART_FocusRing").Opacity == 0, "Physical pointer activation retained a keyboard ring.");
            await input.ClickAsync(Find<TextBlock>(Default, "PART_Label"));
            await WaitAsync(() => Default.IsChecked == false, "Physical label click did not toggle off.");
            await input.ClickAsync(Find<TextBlock>(Default, "PART_Description"));
            await WaitAsync(() => Default.IsChecked == true, "Physical description click did not toggle on.");
            Require(clicks == 4 && changes == 4 && command.Parameters.Count == 4 && command.Parameters.All(value => ReferenceEquals(value, parameter)),
                "Physical checkbox actions did not reach Click, value events and the consumer command exactly once.");
            physicalInputSteps.Add("Box, label and description activated with exactly-once events/command and consumer parameter identity");
            command.Enabled = false;
            await WaitAsync(() => !Default.IsEnabled, "Native checkbox ignored Command.CanExecute changes.");
            await input.ClickAsync(Find<Border>(Default, "PART_Box"));
            Require(Default.IsChecked == true && clicks == 4 && changes == 4 && command.Parameters.Count == 4,
                "A rejected native command still activated the checkbox.");
            command.Enabled = true;
            await WaitAsync(() => Default.IsEnabled, "Native checkbox did not restore command availability.");
            physicalInputSteps.Add("Command.CanExecute disabled physical activation and restored availability");
            foreach (var control in new[] { DisabledOff, DisabledOn })
            {
                var value = control.IsChecked;
                await input.ClickAsync(Find<Border>(control, "PART_Box"));
                await input.ClickAsync(Find<TextBlock>(control, "PART_Label"));
                Require(control.IsChecked == value, "Physical box or label click changed a disabled checkbox.");
            }
            physicalInputSteps.Add("Disabled off/on rejected box and label activation");
            Rtl.IsChecked = true;
            await input.ClickAsync(Find<Border>(Rtl, "PART_Box"));
            await WaitAsync(() => Rtl.IsChecked == false, "Physical RTL box did not toggle off.");
            await input.ClickAsync(Find<TextBlock>(Rtl, "PART_Label"));
            await WaitAsync(() => Rtl.IsChecked == true, "Physical RTL label did not toggle on.");
            physicalInputSteps.Add("RTL box and label both activated the native input");
            await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"checkbox-pointer-window-{scheme}.png"));
        }
        finally
        {
            Default.Click -= Clicked; Default.Checked -= Changed; Default.Unchecked -= Changed;
            Default.Command = originalCommand; Default.CommandParameter = originalParameter;
            Default.IsChecked = defaultValue; Rtl.IsChecked = rtlValue; Off.IsChecked = offValue;
        }
        await SettleAsync(Default); await SettleAsync(Rtl);
    }

    private sealed class ConsumerCommand : ICommand
    {
        private bool enabled = true;
        internal bool Enabled { get => enabled; set { enabled = value; CanExecuteChanged?.Invoke(this, EventArgs.Empty); } }
        internal List<object?> Parameters { get; } = [];
        public bool CanExecute(object? parameter) => enabled;
        public void Execute(object? parameter) => Parameters.Add(parameter);
        public event EventHandler? CanExecuteChanged;
    }

    private async Task VerifyFocusPixelsAsync(NativeInputSession input, string path, BleeckerColorScheme scheme, string folder)
    {
        var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
        using var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
        var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
        var data = await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
            Windows.Graphics.Imaging.BitmapAlphaMode.Straight, new Windows.Graphics.Imaging.BitmapTransform(),
            Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation,
            Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage);
        var pixels = data.DetachPixelData();
        var box = input.ScreenBounds(Find<Border>(Default, "PART_Box"));
        var client = NativeScreenCapture.ClientBounds(input.Window);
        var scale = XamlRoot.RasterizationScale;
        var background = Pixel(box.Left - 6 * scale, box.Top + 10 * scale);
        var left = Pixel(box.Left - 3 * scale, box.Top + 10 * scale);
        var right = Pixel(box.Right + 3 * scale, box.Top + 10 * scale);
        var offset = Pixel(box.Left - scale, box.Top + 10 * scale);
        var brush = (SolidColorBrush)Find<Border>(Default, "PART_FocusRing").BorderBrush;
        var color = brush.Color; var opacity = brush.Opacity;
        var expected = new[] { Mix(color.R, background[0]), Mix(color.G, background[1]), Mix(color.B, background[2]) };
        var offsetColor = ((SolidColorBrush)Find<Border>(Default, "PART_FocusOffset").Background).Color;
        var expectedOffset = new[] { (int)offsetColor.R, offsetColor.G, offsetColor.B };
        await File.WriteAllTextAsync(Path.Combine(folder, $"checkbox-focus-pixels-{scheme}.json"),
            JsonSerializer.Serialize(new { scale, box, client, background, left, right, expected, offset, expectedOffset },
                new JsonSerializerOptions { WriteIndented = true }));
        Require(Matches(left, expected) && Matches(right, expected), "Composed checkbox focus ring is clipped or has the wrong rendered color.");
        Require(Matches(offset, expectedOffset), "Composed checkbox focus offset is clipped or has the wrong rendered color.");
        physicalInputSteps.Add("Composed pixels verified the ring beyond both box edges and its inner offset");

        int Mix(byte foreground, int behind) => (int)Math.Round(foreground * opacity + behind * (1 - opacity));
        int[] Pixel(double x, double y)
        {
            var column = (int)Math.Round(x - client.X); var row = (int)Math.Round(y - client.Y);
            Require(column >= 0 && row >= 0 && column < decoder.PixelWidth && row < decoder.PixelHeight,
                "Focus pixel probe exceeds the composed capture.");
            var index = checked((row * (int)decoder.PixelWidth + column) * 4);
            return [pixels[index + 2], pixels[index + 1], pixels[index]];
        }
        static bool Matches(int[] actual, int[] expected) => actual.Zip(expected).All(pair => Math.Abs(pair.First - pair.Second) <= 2);
    }

    private async Task InspectFontFallbackAsync(BleeckerColorScheme scheme, string folder)
    {
        var original = NonLatin.FontFamily;
        var probes = new List<object>();
        // Evidence only: identify the DOM/native Japanese font discrepancy
        // without replacing the library's typography contract.
        var suffixes = new[] { "", ", Yu Gothic", ", Yu Gothic UI", ", Meiryo" };
        try
        {
            for (var index = 0; index < suffixes.Length; index++)
            {
                NonLatin.FontFamily = new FontFamily(original.Source + suffixes[index]);
                UpdateLayout(); await Task.Delay(50); UpdateLayout();
                var label = Find<TextBlock>(NonLatin, "PART_Label");
                probes.Add(new { font = label.FontFamily.Source, label.Language, label.FontSize,
                    weight = label.FontWeight.Weight, label.ActualWidth, label.ActualHeight, label.BaselineOffset });
                await MainWindow.CaptureElementAsync(NonLatin, Path.Combine(folder, $"checkbox-font-fallback-{index}-{scheme}.png"));
            }
            await File.WriteAllTextAsync(Path.Combine(folder, $"checkbox-font-fallback-{scheme}.json"),
                JsonSerializer.Serialize(probes, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally { NonLatin.FontFamily = original; UpdateLayout(); }
        Require(ReferenceEquals(NonLatin.FontFamily, original), "Font evidence probe did not restore consumer typography.");
    }

    private async Task InspectScriptWeightsAsync(BleeckerColorScheme scheme, string folder)
    {
        var originalContent = NonLatin.Content;
        var originalDescription = NonLatin.Description;
        var originalLanguage = NonLatin.Language;
        var probes = new List<object>();
        var scripts = new[]
        {
            (Name: "japanese", Language: "ja-JP", Text: "通知を有効にする"),
            (Name: "simplified-chinese", Language: "zh-CN", Text: "启用通知"),
            (Name: "traditional-chinese", Language: "zh-TW", Text: "啟用通知"),
            (Name: "korean", Language: "ko-KR", Text: "알림 활성화"),
            (Name: "arabic", Language: "ar", Text: "تمكين الإشعارات"),
            (Name: "latin", Language: "en-US", Text: "Consumer label")
        };
        var weights = new[]
        {
            (Value: (ushort)400, Key: "BleeckerPrimaryFontFamily"),
            (Value: (ushort)500, Key: "BleeckerPrimaryMediumFontFamily"),
            (Value: (ushort)600, Key: "BleeckerPrimarySemiboldFontFamily")
        };
        try
        {
            NonLatin.Description = null;
            foreach (var script in scripts)
            foreach (var weight in weights)
            foreach (var candidate in ScriptFontCandidates(script.Name))
            {
                var source = FindFontResource(Application.Current.Resources, weight.Key);
                var text = new TextBlock
                {
                    Text = script.Text, Language = script.Language, FlowDirection = FlowDirection.LeftToRight,
                    FontFamily = candidate switch
                    {
                        "original" => source,
                        "yu-gothic" => new FontFamily(source.Source + ", Yu Gothic"),
                        "yu-gothic-direct" => new FontFamily("Yu Gothic"),
                        "segoe-ui-direct" => new FontFamily("Segoe UI"),
                        "arial-direct" => new FontFamily("Arial"),
                        "tahoma-direct" => new FontFamily("Tahoma"),
                        _ => throw new InvalidOperationException($"Unknown font evidence candidate {candidate}.")
                    },
                    FontWeight = new Windows.UI.Text.FontWeight { Weight = weight.Value },
                    FontSize = 14, TextWrapping = TextWrapping.NoWrap, UseLayoutRounding = false
                };
                NonLatin.Language = script.Language;
                NonLatin.Content = text;
                UpdateLayout(); await Task.Delay(50); UpdateLayout();
                Require(text.LineHeight == 20 && text.ActualHeight == 20 && double.IsFinite(text.ActualWidth),
                    "Script evidence probe did not receive the branded text layout.");
                probes.Add(new { name = script.Name, language = text.Language, weight = weight.Value, candidate,
                    text = text.Text, font = text.FontFamily.Source, text.FontSize, text.LineHeight,
                    text.ActualWidth, text.ActualHeight, text.BaselineOffset, rowWidth = NonLatin.ActualWidth });
                await MainWindow.CaptureElementAsync(NonLatin,
                    Path.Combine(folder, $"checkbox-script-{script.Name}-{weight.Value}-{candidate}-{scheme}.png"));
            }
            await File.WriteAllTextAsync(Path.Combine(folder, $"checkbox-script-weights-{scheme}.json"),
                JsonSerializer.Serialize(new { referenceCommit = "a86059013cd59684ce452849278ea81e734f2066",
                    evidenceOnly = true, probes }, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            NonLatin.Content = originalContent;
            NonLatin.Description = originalDescription;
            NonLatin.Language = originalLanguage;
            UpdateLayout();
        }
        Require(ReferenceEquals(NonLatin.Content, originalContent) && ReferenceEquals(NonLatin.Description, originalDescription) &&
            NonLatin.Language == originalLanguage, "Script evidence probe did not restore consumer content and language.");
    }

    private static FontFamily FindFontResource(ResourceDictionary dictionary, string key)
    {
        if (dictionary.TryGetValue(key, out var value)) return value as FontFamily
            ?? throw new InvalidOperationException($"Resource {key} is not a font family.");
        foreach (var merged in dictionary.MergedDictionaries.Reverse())
        {
            try { return FindFontResource(merged, key); }
            catch (KeyNotFoundException) { }
        }
        throw new KeyNotFoundException(key);
    }

    private static IEnumerable<string> ScriptFontCandidates(string name)
    {
        yield return "original";
        yield return "yu-gothic";
        if (name == "japanese") yield return "yu-gothic-direct";
        if (name == "arabic")
        {
            yield return "segoe-ui-direct";
            yield return "arial-direct";
            yield return "tahoma-direct";
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
    private static object DescribeTemplate(BleeckerCheckbox control, string phase) => new
    {
        control.Name, phase, templateIsNull = control.Template is null,
        localIsNull = control.ReadLocalValue(Control.TemplateProperty) is null,
        localIsUnset = control.ReadLocalValue(Control.TemplateProperty) == DependencyProperty.UnsetValue,
        visualChildren = Enumerable.Range(0, VisualTreeHelper.GetChildrenCount(control))
            .Select(index => VisualTreeHelper.GetChild(control, index)).Select(child => new
            {
                type = child.GetType().FullName, name = (child as FrameworkElement)?.Name,
                width = (child as FrameworkElement)?.ActualWidth, height = (child as FrameworkElement)?.ActualHeight,
                visibility = (child as UIElement)?.Visibility.ToString()
            }).ToArray()
    };
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
