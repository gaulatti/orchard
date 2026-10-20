using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class SteppersFixture : UserControl
{
    private BleeckerStepper stepper => Stepper;
    private readonly Button before = new() { Content = "Before stepper", Visibility = Visibility.Collapsed };
    private readonly Button after = new() { Content = "After stepper", Visibility = Visibility.Collapsed };
    private readonly List<BleeckerStep> invocations = [];

    public SteppersFixture()
    {
        InitializeComponent();
        Frame.Children.Insert(0, before); Frame.Children.Add(after);
        Canvas.SetLeft(stepper, 20); Canvas.SetTop(stepper, 20);
        Canvas.SetLeft(before, 680); Canvas.SetLeft(after, 680); Canvas.SetTop(after, 50);
        stepper.Steps = BasicSteps(); stepper.StepInvoked += Invoked;
        AutomationProperties.SetName(stepper, "Editorial stages");
        using var reference = ReadReference();
        CasePicker.ItemsSource = reference.RootElement.GetProperty("cases").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == "Light").Select(item => item.GetProperty("id").GetString()!).ToArray();
        CasePicker.SelectedIndex = 0;
        ActualThemeChanged += (_, _) => UpdateFrameBackground();
        UpdateFrameBackground();
    }
    private static JsonDocument ReadReference() => JsonDocument.Parse(typeof(SteppersFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.Stepper.json")!);
    private void CaseChanged(object sender, SelectionChangedEventArgs args)
    {
        if (CasePicker.SelectedItem is not string id) return;
        using var reference = ReadReference();
        Apply(reference.RootElement.GetProperty("cases").EnumerateArray().First(item => item.GetProperty("id").GetString() == id));
    }
    private void UpdateFrameBackground() => Frame.Background = new SolidColorBrush(ActualTheme == ElementTheme.Dark ? Windows.UI.Color.FromArgb(255, 13, 24, 33) : Microsoft.UI.Colors.White);
    private void Invoked(object? sender, BleeckerStep step) => invocations.Add(step);
    private static ObservableCollection<BleeckerStep> BasicSteps() =>
    [new("draft", "Draft") { Description = "Write a story" }, new("review", "Review") { Description = "Check facts" }, new("publish", "Publish") { Description = "Release edition" }];

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        using var reference = JsonDocument.Parse(typeof(SteppersFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.Stepper.json")!);
        using var typography = JsonDocument.Parse(typeof(SteppersFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.StepperTypography.json")!);
        var cases = new List<object>(); var failures = new List<string>();
        foreach (var spec in reference.RootElement.GetProperty("cases").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == scheme.ToString()))
        {
            Apply(spec); await SettleAsync();
            var id = spec.GetProperty("id").GetString()!; var expected = spec.GetProperty("normal");
            var expectedTypography = typography.RootElement.GetProperty("reports").EnumerateArray().Single(item => item.GetProperty("id").GetString() == id && item.GetProperty("scheme").GetString() == scheme.ToString()).GetProperty("rows");
            var rows = Descendants<Panel>(stepper).Where(item => item.Name == "StepEntry").ToArray();
            var errors = new List<string>();
            Near(stepper.ActualWidth, expected.GetProperty("box").GetProperty("width").GetDouble(), "root width");
            Near(stepper.ActualHeight, expected.GetProperty("box").GetProperty("height").GetDouble(), "root height");
            var actual = new List<object>();
            Require(rows.Length == expected.GetProperty("rows").GetArrayLength(), "Stepper lost a source row.");
            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i]; var source = expected.GetProperty("rows")[i];
                var button = Descendants<Button>(row).Single();
                var label = Descendants<Panel>(row).Single(item => item.Name == "LabelParagraph");
                var description = Descendants<Panel>(row).Single(item => item.Name == "DescriptionParagraph");
                var labels = Descendants<StackPanel>(row).Single(item => item.Name == "Labels");
                var circle = Descendants<Grid>(row).Single(item => item.Name == "Circle");
                CheckBaseline(label, expectedTypography[i].GetProperty("label"), $"label {i}");
                CheckBaseline(description, expectedTypography[i].GetProperty("description"), $"description {i}");
                CheckBaseline(button, expectedTypography[i].GetProperty("button"), $"ordinal {i}");
                var fill = Descendants<Border>(row).Single(item => item.Name == "CircleFill");
                var connector = Descendants<Border>(row).Single(item => item.Name == "Connector");
                CheckBox(row, source.GetProperty("box"), $"row {i}");
                CheckBox(button, source.GetProperty("button").GetProperty("box"), $"button {i}");
                CheckBox(labels, source.GetProperty("labels"), $"labels {i}");
                CheckBox(label, source.GetProperty("label").GetProperty("box"), $"label {i}");
                if (source.GetProperty("description").ValueKind != JsonValueKind.Null) CheckBox(description, source.GetProperty("description").GetProperty("box"), $"description {i}");
                CheckColor(fill.Background, source.GetProperty("button").GetProperty("backgroundRgba"), $"button {i} background");
                CheckColor(Descendants<TextBlock>(button).Single().Foreground, source.GetProperty("button").GetProperty("foregroundRgba"), $"button {i} foreground");
                CheckColor(Descendants<TextBlock>(label).Single().Foreground, source.GetProperty("label").GetProperty("rgba"), $"label {i} color");
                if (source.GetProperty("connector").ValueKind != JsonValueKind.Null)
                {
                    CheckBox(connector, source.GetProperty("connector").GetProperty("box"), $"connector {i}");
                    CheckColor(connector.Background, source.GetProperty("connector").GetProperty("rgba"), $"connector {i} color");
                }
                else Require(connector.Visibility == Visibility.Collapsed, "Final step acquired a connector.");
                Require(button.IsEnabled != source.GetProperty("button").GetProperty("disabled").GetBoolean(), "Stepper disabled input differs from source.");
                Require(circle.Opacity == double.Parse(source.GetProperty("button").GetProperty("opacity").GetString()!, System.Globalization.CultureInfo.InvariantCulture), "Stepper disabled opacity differs from source.");
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
                Require(peer.GetAutomationControlType() == AutomationControlType.Button && peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider &&
                    peer.GetName() == stepper.Steps[i].Label && AutomationProperties.GetPositionInSet(button) == i + 1 && AutomationProperties.GetSizeOfSet(button) == rows.Length,
                    "Stepper lost native invocation, accessible label or ordinal metadata.");
                actual.Add(new { row = Box(row), button = Box(button), labels = Box(labels), label = Box(label), description = Box(description), labelTypography = Typography(label), descriptionTypography = Typography(description), buttonTypography = Typography(button), button.IsEnabled, circle.Opacity });
            }
            var file = Path.Combine(folder, $"stepper-{id}-{scheme}.png");
            if (rows.Length == 0)
            {
                var bitmap = new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap(); await bitmap.RenderAsync(Frame);
                var buffer = await bitmap.GetPixelsAsync(); var pixels = new byte[checked((int)buffer.Length)];
                using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
                Require(bitmap.PixelWidth == 1024 && bitmap.PixelHeight == 745, "Empty Stepper lost its full reference frame.");
                await MainWindow.SavePngAsync(file, pixels, bitmap.PixelWidth, bitmap.PixelHeight);
            }
            else await MainWindow.CaptureElementAsync(Frame, file);
            cases.Add(new { id, width = stepper.ActualWidth, height = stepper.ActualHeight, rows = actual, errors });
            failures.AddRange(errors.Select(error => id + ": " + error));
            await File.WriteAllTextAsync(Path.Combine(folder, $"stepper-source-{scheme}.json"), JsonSerializer.Serialize(new { cases, failures }, new JsonSerializerOptions { WriteIndented = true }));

            void Near(double value, double target, string what) { if (Math.Abs(value - target) > .05) errors.Add($"{what}: actual={value:R}, source={target:R}"); }
            void CheckBaseline(DependencyObject owner, JsonElement expectedText, string what)
            {
                if (expectedText.ValueKind == JsonValueKind.Null || expectedText.GetProperty("firstBaseline").ValueKind == JsonValueKind.Null) return;
                var text = Descendants<TextBlock>(owner).Single();
                var baseline = text.TransformToVisual(Frame).TransformPoint(new Point(0, text.BaselineOffset)).Y;
                var root = stepper.TransformToVisual(Frame).TransformBounds(new Rect(0, 0, stepper.ActualWidth, stepper.ActualHeight));
                Near(baseline - root.Y, expectedText.GetProperty("firstBaseline").GetDouble(), what + " baseline");
                Near(text.LineHeight, double.Parse(expectedText.GetProperty("lineHeight").GetString()!.Replace("px", ""), System.Globalization.CultureInfo.InvariantCulture), what + " line height");
            }
            void CheckColor(Brush brush, JsonElement rgba, string what)
            {
                var solid = (SolidColorBrush)brush; var color = solid.Color;
                // Chromium's canvas conversion of OKLab may round channels by two.
                var channels = new double[] { color.R, color.G, color.B, color.A * solid.Opacity };
                if (channels.Where((value, channel) => Math.Abs(value - rgba[channel].GetDouble()) > (channel == 3 ? 1 : 2)).Any()) errors.Add(what + ": native brush differs from browser RGBA");
            }
            void CheckBox(FrameworkElement element, JsonElement box, string what)
            {
                var bounds = PhysicalBox(element);
                Near(bounds.X, box.GetProperty("x").GetDouble(), what + " x"); Near(bounds.Y, box.GetProperty("y").GetDouble(), what + " y");
                Near(element.ActualWidth, box.GetProperty("width").GetDouble(), what + " width"); Near(element.ActualHeight, box.GetProperty("height").GetDouble(), what + " height");
            }
        }
        await File.WriteAllTextAsync(Path.Combine(folder, $"stepper-source-{scheme}.json"), JsonSerializer.Serialize(new { cases, failures }, new JsonSerializerOptions { WriteIndented = true }));
        await VerifyInputAsync(scheme, folder);
        Require(failures.Count == 0, "Stepper source geometry differs: " + string.Join("; ", failures.Take(8)));
    }
    private void Apply(JsonElement spec)
    {
        stepper.StepInvoked -= Invoked;
        var props = spec.GetProperty("props");
        stepper.Width = spec.GetProperty("width").GetDouble();
        stepper.FlowDirection = spec.GetProperty("direction").GetString() == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        stepper.Orientation = props.TryGetProperty("orientation", out var orientation) && orientation.GetString() == "vertical" ? Orientation.Vertical : Orientation.Horizontal;
        stepper.ActiveStep = props.TryGetProperty("activeStep", out var active) ? active.GetString() : null;
        stepper.Steps = new ObservableCollection<BleeckerStep>(props.GetProperty("steps").EnumerateArray().Select(item => new BleeckerStep(item.GetProperty("id").GetString()!, item.GetProperty("label").GetString()!)
        {
            Description = item.TryGetProperty("description", out var description) ? description.GetString() : null,
            Status = item.TryGetProperty("status", out var status) ? Enum.Parse<BleeckerStepStatus>(status.GetString()!, true) : null,
            IsDisabled = item.TryGetProperty("disabled", out var disabled) && disabled.GetBoolean()
        }));
        if (!props.TryGetProperty("interactive", out var interactive) || interactive.GetBoolean()) stepper.StepInvoked += Invoked;
        UpdateFrameBackground();
    }
    private async Task VerifyInputAsync(BleeckerColorScheme scheme, string folder)
    {
        stepper.StepInvoked -= Invoked; stepper.StepInvoked += Invoked;
        stepper.Steps = BasicSteps(); stepper.ActiveStep = "review"; stepper.Width = 640;
        stepper.Orientation = Orientation.Horizontal; stepper.FlowDirection = FlowDirection.LeftToRight;
        before.Visibility = after.Visibility = Visibility.Visible;
        await SettleAsync(); invocations.Clear();
        using var input = new NativeInputSession(XamlRoot, folder, $"stepper-{scheme}");
        var buttons = Buttons(); var original = stepper.Steps[0];
        await input.AcquireAsync(buttons[0]);
        await input.ClickAsync(buttons[0]);
        buttons[0].Focus(FocusState.Keyboard); await MainWindow.AwaitPresentationAsync(buttons[0]);
        await input.KeyAsync(0x20, buttons[0]); await input.KeyAsync(0x0D, buttons[0]);
        Require(invocations.Count == 3 && invocations.All(step => ReferenceEquals(step, original)) && stepper.ActiveStep == "review" && stepper.Steps.All(step => step.Status is null), "Stepper activation changed controlled state or lost original consumer objects.");
        var retained = buttons[0];
        var replacement = new BleeckerStep("draft", "Updated draft") { Description = "Live consumer update", Status = BleeckerStepStatus.Error };
        stepper.Steps[0] = replacement; await SettleAsync();
        Require(ReferenceEquals(Buttons()[0], retained), "Stepper replaced the native button for a stable key.");
        await input.KeyAsync(0x0D, retained);
        Require(ReferenceEquals(invocations.Last(), replacement), "Stepper invoked a stale model after keyed replacement.");
        replacement.IsDisabled = true; await SettleAsync();
        Require(!retained.IsEnabled, "Stepper did not apply a live disabled update.");
        var count = invocations.Count; await input.ClickAsync(retained);
        Require(invocations.Count == count, "Disabled step accepted pointer activation.");
        replacement.IsDisabled = false; stepper.StepInvoked -= Invoked; await SettleAsync();
        Require(Buttons().All(button => !button.IsEnabled), "Stepper retained input after callback removal.");
        stepper.StepInvoked += Invoked; await SettleAsync();
        await input.RequireForegroundAsync();
        before.Focus(FocusState.Keyboard); input.Key(0x09); await MainWindow.AwaitPresentationAsync(retained);
        Require(ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), retained), "Native Tab did not enter the first step.");
        stepper.Steps.Move(0, 2); await SettleAsync();
        Require(ReferenceEquals(Buttons()[2], retained), "Stepper lost keyed identity during reorder.");
        stepper.Steps.Remove(replacement); await SettleAsync();
        Require(!retained.IsEnabled, "Removed native step remained enabled.");
        stepper.Steps = BasicSteps(); await SettleAsync();
        var current = Buttons()[0]; current.Focus(FocusState.Keyboard); await MainWindow.AwaitPresentationAsync(current);
        await input.HeldKeyAsync(0x20, current, () => current, async () =>
        {
            await SettleAsync();
            var scale = (ScaleTransform)Descendants<Grid>(current).Single(item => item.Name == "Circle").RenderTransform;
            Require(current.IsPressed && Math.Abs(scale.ScaleX - .94) < .0001 && scale.ScaleY == scale.ScaleX, "Held native Space lost the source pressed scale.");
            await MainWindow.CaptureElementAsync(Frame, Path.Combine(folder, $"stepper-space-held-{scheme}.png"));
        });
        await SettleAsync();
        Require(!current.IsPressed, "Native Space did not release the pressed state.");
        var nativeInvoke = (IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(current).GetPattern(PatternInterface.Invoke);
        count = invocations.Count; nativeInvoke.Invoke(); await SettleAsync();
        Require(invocations.Count == count + 1 && ReferenceEquals(invocations.Last(), stepper.Steps[0]), "UIA invocation did not reach the original consumer step.");
        stepper.StepInvoked -= Invoked;
        var command = new StepCommand(); stepper.StepCommand = command; await SettleAsync();
        Require(Buttons().All(button => !button.IsEnabled), "Stepper ignored initial command rejection.");
        command.Enabled = true; await SettleAsync();
        current = Buttons()[0]; await input.ClickAsync(current);
        Require(command.Values.Count == 1 && ReferenceEquals(command.Values[0], stepper.Steps[0]), "StepCommand did not receive the original consumer object.");
        command.Enabled = false; await SettleAsync();
        Require(Buttons().All(button => !button.IsEnabled), "CanExecuteChanged did not disable native input.");
        var nextCommand = new StepCommand { Enabled = true }; stepper.StepCommand = nextCommand;
        command.Enabled = true; command.Enabled = false; await SettleAsync();
        Require(Buttons().All(button => button.IsEnabled), "Replaced command still controlled the mounted Stepper.");
        stepper.StepCommand = null; stepper.StepInvoked += Invoked;
        var template = stepper.Template; var stale = Buttons()[0];
        stepper.Template = null; await SettleAsync();
        Require(!stale.IsEnabled && Buttons().Length == 0, "Template removal retained an active native action.");
        stepper.Steps[0].Label = "Changed while detached";
        stepper.Template = template; stepper.ApplyTemplate(); await SettleAsync();
        Require(AutomationProperties.GetName(Buttons()[0]) == "Changed while detached", "Restored template lost current consumer metadata.");
        Frame.Children.Remove(stepper); await MainWindow.AwaitPresentationAsync(Frame);
        Require(!stepper.IsLoaded, "Stepper did not unload from its actual visual tree.");
        stepper.Steps[0].Label = "Changed while unloaded";
        count = invocations.Count;
        Frame.Children.Insert(1, stepper); await SettleAsync();
        Require(stepper.IsLoaded && AutomationProperties.GetName(Buttons()[0]) == "Changed while unloaded" && invocations.Count == count, "Stepper remount lost metadata or invoked a stale action.");
        await File.WriteAllTextAsync(Path.Combine(folder, $"stepper-input-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, originalActivations = 3, keyedReplacement = true, disabledRejected = true, callbackRemoval = true, tab = true, reorder = true, removal = true, nativeSpaceHeld = true, nativeUiaInvoke = true, commandOwnership = true, templateReplacement = true, unloadRemount = true, invocationCount = invocations.Count }));
        before.Visibility = after.Visibility = Visibility.Collapsed;
        stepper.Steps = BasicSteps(); stepper.ActiveStep = "review"; await SettleAsync();
    }
    private Button[] Buttons() => Descendants<Button>(stepper).ToArray();
    private object Box(FrameworkElement element)
    {
        var bounds = PhysicalBox(element);
        return new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height };
    }
    private object Typography(DependencyObject paragraph)
    {
        var text = Descendants<TextBlock>(paragraph).Single();
        var point = text.TransformToVisual(Frame).TransformPoint(new Point(0, text.BaselineOffset));
        var root = stepper.TransformToVisual(Frame).TransformBounds(new Rect(0, 0, stepper.ActualWidth, stepper.ActualHeight));
        return new { baseline = point.Y - root.Y, text.BaselineOffset, text.LineHeight, shift = (text.RenderTransform as TranslateTransform)?.Y ?? 0, text.FontSize, font = text.FontFamily.Source };
    }
    private Rect PhysicalBox(FrameworkElement element)
    {
        var bounds = element.TransformToVisual(Frame).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        var root = stepper.TransformToVisual(Frame).TransformBounds(new Rect(0, 0, stepper.ActualWidth, stepper.ActualHeight));
        return new Rect(bounds.X - root.X, bounds.Y - root.Y, bounds.Width, bounds.Height);
    }
    private async Task SettleAsync() { UpdateLayout(); await Task.Delay(220); await MainWindow.AwaitPresentationAsync(stepper); }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class StepCommand : System.Windows.Input.ICommand
    {
        private bool enabled;
        internal bool Enabled { get => enabled; set { enabled = value; CanExecuteChanged?.Invoke(this, EventArgs.Empty); } }
        internal List<BleeckerStep> Values { get; } = [];
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => enabled && parameter is BleeckerStep;
        public void Execute(object? parameter) { Require(CanExecute(parameter), "Rejected StepCommand executed."); Values.Add((BleeckerStep)parameter!); }
    }
}
