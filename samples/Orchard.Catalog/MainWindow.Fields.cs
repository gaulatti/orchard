using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunFieldsAsync(string folder)
    {
        using var stream = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.Field.json") ?? throw new InvalidOperationException("Field reference is missing.");
        using var document = await JsonDocument.ParseAsync(stream);
        var previous = Content; var previousSize = AppWindow.ClientSize;
        var canvas = new Canvas { Width = 640, Height = 360, UseLayoutRounding = false, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var anchor = new Button { Width = 20, Height = 20, Opacity = 0 }; Canvas.SetLeft(anchor, 580); Canvas.SetTop(anchor, 320); canvas.Children.Add(anchor);
        var external = new TextBlock { Text = "Consumer description", Opacity = 0 }; AutomationProperties.SetAutomationId(external, "external-help"); canvas.Children.Add(external);
        var frames = new List<object>(); var interactions = new List<object>();
        object Box(FrameworkElement element)
        {
            var box = element.TransformToVisual(canvas).TransformBounds(new Windows.Foundation.Rect(0, 0, element.ActualWidth, element.ActualHeight));
            return new { x = box.X, y = box.Y, width = box.Width, height = box.Height };
        }
        static object Paint(Brush? brush)
        {
            if (brush is not SolidColorBrush solid) throw new InvalidOperationException("Field reference requires solid paint.");
            return new { r = (int)solid.Color.R, g = (int)solid.Color.G, b = (int)solid.Color.B, a = (int)solid.Color.A, opacity = solid.Opacity };
        }
        object Text(FrameworkElement slot, string name, string? logical = null)
        {
            var text = Desc<TextBlock>(slot).Single(value => value.Name == name);
            return new { box = Box(slot), text = logical ?? text.Text, foreground = Paint(text.Foreground), fontSize = text.FontSize, fontWeight = text.FontWeight.Weight, lineHeight = text.LineHeight, fontFamily = text.FontFamily.Source };
        }
        object RequiredMark(TextBlock mark)
        {
            var origin = mark.TransformToVisual(canvas).TransformPoint(new());
            using var fonts = new Microsoft.Graphics.Canvas.Text.CanvasFontSet(new Uri(Path.Combine(AppContext.BaseDirectory, "Orchard/Assets/Fonts/EncodeSans-Medium.ttf")));
            var face = fonts.Fonts.Single();
            var ascent = Math.Round(face.Ascent * mark.FontSize, MidpointRounding.AwayFromZero);
            var descent = Math.Round(face.Descent * mark.FontSize, MidpointRounding.AwayFromZero);
            var allocation = Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(mark);
            return new { box = Box(mark), layoutSlot = new { x = allocation.X, y = allocation.Y, width = allocation.Width, height = allocation.Height },
                explicitWidth = mark.Width, fontBox = new { x = origin.X, y = origin.Y + mark.BaselineOffset - ascent, width = allocation.Width, height = ascent + descent },
                fontMetrics = new { face.Ascent, face.Descent, face.LineGap },
                foreground = Paint(mark.Foreground), fontSize = mark.FontSize, fontWeight = mark.FontWeight.Weight, lineHeight = mark.LineHeight };
        }
        IEnumerable<TextBlock> VisibleTexts(FrameworkElement owner)
        {
            bool Visible(FrameworkElement element)
            {
                for (DependencyObject? node = element; node is FrameworkElement current && !ReferenceEquals(node, owner); node = VisualTreeHelper.GetParent(node))
                    if (current.Visibility != Visibility.Visible) return false;
                return true;
            }
            return Desc<TextBlock>(owner).Where(item => Visible(item) && item.Text.Length > 0);
        }
        object RightText(FrameworkElement owner)
        {
            var visible = VisibleTexts(owner).ToArray();
            Require(visible.Length == 1, "The Field reference action/optional specimen must expose exactly one visible text node.");
            var text = visible[0];
            return new { box = Box(owner), text = text.Text, foreground = Paint(text.Foreground), fontSize = text.FontSize,
                fontWeight = text.FontWeight.Weight, lineHeight = text.LineHeight, fontFamily = text.FontFamily.Source };
        }
        static object? Value(JsonElement spec, string name) => !spec.TryGetProperty(name, out var value) ? null : value.ValueKind switch
        { JsonValueKind.String => value.GetString(), JsonValueKind.True => true, JsonValueKind.False => false, JsonValueKind.Number => value.GetDouble(), JsonValueKind.Null => null, _ => throw new InvalidOperationException("Unsupported Field scalar fixture.") };
        BleeckerField Create(JsonElement spec, out BleeckerInput editor)
        {
            editor = new BleeckerInput { Text = "Consumer text", UseLayoutRounding = false };
            if (spec.TryGetProperty("controlId", out var controlId)) AutomationProperties.SetAutomationId(editor, controlId.GetString()!);
            if (spec.TryGetProperty("invalid", out var invalid)) AutomationProperties.SetIsDataValidForForm(editor, !invalid.GetBoolean());
            if (spec.TryGetProperty("childRequired", out var childRequired)) AutomationProperties.SetIsRequiredForForm(editor, childRequired.GetBoolean());
            if (spec.TryGetProperty("describedBy", out _)) AutomationProperties.GetDescribedBy(editor).Add(external);
            if (spec.TryGetProperty("disabled", out var disabled)) editor.IsEnabled = !disabled.GetBoolean();
            if (spec.TryGetProperty("childName", out var childName)) AutomationProperties.SetName(editor, childName.GetString());
            var field = new BleeckerField { Width = spec.TryGetProperty("width", out var width) ? width.GetDouble() : 320,
                Label = spec.TryGetProperty("label", out var label) ? label.GetString() : "Coverage title", Content = editor,
                Required = spec.TryGetProperty("required", out var required) && required.GetBoolean(), Optional = spec.TryGetProperty("optional", out var optional) && optional.GetBoolean(),
                Description = Value(spec, "description"), Error = Value(spec, "error"), Action = Value(spec, "action"), UseLayoutRounding = false };
            if (field.Action is "button") field.Action = new BleeckerButton { Variant = BleeckerButtonVariant.Link, Content = "Reset" };
            if (spec.TryGetProperty("rich", out var rich) && rich.GetBoolean())
            {
                var richText = (TextBlock)XamlReader.Load("<TextBlock xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' FontFamily='{StaticResource BleeckerPrimaryMediumFontFamily}' FontWeight='Medium' FontSize='13' LineHeight='19.5' LineStackingStrategy='BlockLineHeight' UseLayoutRounding='False' Foreground='{ThemeResource BleeckerTextPrimaryBrush}'/>");
                richText.Inlines.Add(new Run { Text = "Coverage " });
                // The browser receipt resolves strong to this actual pinned
                // face, despite the computed CSS bolder weight being 700.
                richText.Inlines.Add(new Run { Text = "details", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontFamily = new FontFamily("ms-appx:///Orchard/Assets/Fonts/EncodeSans-SemiBold.ttf#Encode Sans") });
                field.Label = richText;
            }
            Canvas.SetLeft(field, 20); Canvas.SetTop(field, 20); return field;
        }
        try
        {
            Content = canvas; await AwaitPresentationAsync(canvas);
            using var input = new NativeInputSession(canvas.XamlRoot, folder, "field"); await input.AcquireAsync(canvas);
            foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
            {
                var theme = Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!); canvas.RequestedTheme = theme;
                canvas.Background = new SolidColorBrush(theme == ElementTheme.Light ? Microsoft.UI.Colors.White : Windows.UI.Color.FromArgb(255, 13, 24, 33));
                var field = Create(row.GetProperty("spec"), out var editor); canvas.Children.Add(field);
                anchor.Focus(FocusState.Programmatic); await input.MoveAsync(anchor); await AwaitPresentationAsync(field);
                var label = Desc<FrameworkElement>(field).Single(value => value.Name == "FieldLabel");
                var header = Desc<FrameworkElement>(field).Single(value => value.Name == "FieldHeader");
                var right = Desc<FrameworkElement>(field).Single(value => value.Name == "FieldRight");
                var description = Desc<FrameworkElement>(field).Single(value => value.Name == "FieldDescription");
                var error = Desc<FrameworkElement>(field).Single(value => value.Name == "FieldError");
                var rootPart = Desc<Border>(editor).Single(value => value.Name == "BorderElement");
                var uia = await ProbeFieldAsync(folder, input.Window, row.GetProperty("id").GetString() + "-" + theme);
                frames.Add(new { id = row.GetProperty("id").GetString(), scheme = theme.ToString(), measurement = new {
                    root = new { box = Box(field) }, header = new { box = Box(header) }, label = Text(label, "FieldLabelTextText", FrameworkElementAutomationPeer.CreatePeerForElement(label).GetName() + (field.Required ? "*" : "")),
                    required = field.Required ? RequiredMark(Desc<TextBlock>(field).Single(value => value.Name == "FieldLabelTextRequired")) : null,
                    right = right.Visibility == Visibility.Visible ? RightText(right) : null,
                    description = description.Visibility == Visibility.Visible ? Text(description, "FieldDescriptionText") : null,
                    error = error.Visibility == Visibility.Visible ? Text(error, "FieldErrorText") : null,
                    control = new { box = Box(editor) }, controlId = AutomationProperties.GetAutomationId(editor),
                    valid = AutomationProperties.GetIsDataValidForForm(editor), nativeRequired = AutomationProperties.GetIsRequiredForForm(editor), disabled = !editor.IsEnabled,
                    describedBy = AutomationProperties.GetDescribedBy(editor).Select(value => ReferenceEquals(value, description) ? "description" : ReferenceEquals(value, error) ? "error" : AutomationProperties.GetAutomationId(value)).ToArray(),
                    value = editor.Text, inputBackground = Paint(rootPart.Background), inputBorder = Paint(rootPart.BorderBrush), uia
                } });
                await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, row.GetProperty("file").GetString()!));
                canvas.Children.Remove(field);
                await File.WriteAllTextAsync(Path.Combine(folder, "field-progress.json"), JsonSerializer.Serialize(new { passed = false, frames }, new JsonSerializerOptions { WriteIndented = true }));
            }
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                canvas.RequestedTheme = theme;
                var editor = new BleeckerInput { Text = "Consumer text" }; AutomationProperties.GetDescribedBy(editor).Add(external);
                AutomationProperties.SetIsDataValidForForm(editor, true);
                var field = new BleeckerField { Label = "Coverage title", Content = editor, Width = 320 }; Canvas.SetLeft(field, 20); Canvas.SetTop(field, 20); canvas.Children.Add(field);
                await AwaitPresentationAsync(field);
                var initialId = AutomationProperties.GetAutomationId(editor); var initial = await ProbeFieldAsync(folder, input.Window, "live-initial-" + theme);
                var label = Desc<FrameworkElement>(field).Single(value => value.Name == "FieldLabel");
                await input.ClickAsync(label); Require(ReferenceEquals(FocusManager.GetFocusedElement(canvas.XamlRoot), editor), "Field label did not focus its consumer control.");
                await input.KeyAsync(0x41, editor); await AwaitPresentationAsync(editor); var retainedText = editor.Text;
                field.Required = true; field.Description = "Live description."; field.Error = "Live error."; await AwaitPresentationAsync(field);
                Require(ReferenceEquals(field.Content, editor) && editor.Text == retainedText && AutomationProperties.GetAutomationId(editor) == initialId && !AutomationProperties.GetIsRequiredForForm(editor), "Field changed child identity, text, ID or native required state.");
                var added = await ProbeFieldAsync(folder, input.Window, "live-added-" + theme);
                Require(!AutomationProperties.GetIsDataValidForForm(editor) && !editor.IsError, "Field error failed to expose invalid semantics or changed the child's visual validation.");
                field.Error = null; await AwaitPresentationAsync(field); Require(AutomationProperties.GetIsDataValidForForm(editor), "Field did not restore child validity.");
                var removedError = await ProbeFieldAsync(folder, input.Window, "live-removed-error-" + theme);
                field.Description = null; field.Optional = true;
                var action = new BleeckerButton { Variant = BleeckerButtonVariant.Link, Content = "Reset" }; var actions = 0; action.Click += (_, _) => actions++; field.Action = action;
                await AwaitPresentationAsync(field); var actionButton = Desc<Button>(action).Single(); await input.ClickAsync(actionButton);
                Require(actions == 1 && editor.Text == retainedText && AutomationProperties.GetDescribedBy(editor).SequenceEqual(new[] { external }), "Field action or removal of message references crossed the consumer boundary.");
                var removedMessages = await ProbeFieldAsync(folder, input.Window, "live-removed-messages-" + theme);
                var template = field.Template; field.Template = null; await AwaitPresentationAsync(canvas);
                Require(AutomationProperties.GetLabeledBy(editor) is null && AutomationProperties.GetAutomationId(editor) == "" && AutomationProperties.GetDescribedBy(editor).SequenceEqual(new[] { external }), "Field template detach retained owned child metadata.");
                field.Template = template; await AwaitPresentationAsync(field); Require(AutomationProperties.GetAutomationId(editor) == initialId, "Field template replacement changed its generated identity.");
                canvas.Children.Remove(field); await AwaitPresentationAsync(canvas);
                Require(AutomationProperties.GetLabeledBy(editor) is null && AutomationProperties.GetAutomationId(editor) == "", "Field unload retained generated child metadata.");
                canvas.Children.Add(field); await AwaitPresentationAsync(field); Require(AutomationProperties.GetAutomationId(editor) == initialId && editor.Text == retainedText, "Field remount lost child identity/value.");
                var replacement = new BleeckerInput { Text = "Replacement" }; field.Content = replacement; await AwaitPresentationAsync(field);
                Require(AutomationProperties.GetLabeledBy(editor) is null && AutomationProperties.GetAutomationId(editor) == "" && AutomationProperties.GetDescribedBy(editor).SequenceEqual(new[] { external }), "Field content replacement retained metadata on the previous child.");
                field.Content = editor; await AwaitPresentationAsync(field);
                // WinUI normally inherits this property. An explicit child
                // value must retain normal precedence over the wrapper.
                editor.IsTextScaleFactorEnabled = true;
                field.IsTextScaleFactorEnabled = false; await AwaitPresentationAsync(field);
                Require(!Desc<TextBlock>(field).Single(value => value.Name == "FieldLabelTextText").IsTextScaleFactorEnabled && editor.IsTextScaleFactorEnabled,
                    "Field's text-scale opt-out did not reach its owned text or changed the consumer control.");
                field.IsTextScaleFactorEnabled = true; await AwaitPresentationAsync(field);
                var producer = new TextBlock { Tag = true, Text = "bound-control" };
                var bound = new BleeckerInput { Text = "Bound child" };
                bound.SetBinding(AutomationProperties.IsDataValidForFormProperty, new Binding { Source = producer, Path = new PropertyPath("Tag"), Mode = BindingMode.OneWay });
                bound.SetBinding(AutomationProperties.AutomationIdProperty, new Binding { Source = producer, Path = new PropertyPath("Text"), Mode = BindingMode.OneWay });
                field.Content = bound; field.Error = "Temporary error"; await AwaitPresentationAsync(field);
                Require(!AutomationProperties.GetIsDataValidForForm(bound) && Equals(producer.Tag, true), "Field error wrote into the consumer binding source.");
                producer.Tag = false; producer.Text = "updated-bound-control"; field.Error = null; await AwaitPresentationAsync(field);
                Require(!AutomationProperties.GetIsDataValidForForm(bound) && bound.GetBindingExpression(AutomationProperties.IsDataValidForFormProperty) is not null &&
                    AutomationProperties.GetAutomationId(bound) == "updated-bound-control", "Field did not restore the current bound metadata producer.");
                producer.Tag = true; await AwaitPresentationAsync(field); Require(AutomationProperties.GetIsDataValidForForm(bound), "Restored validity binding stopped receiving producer updates.");
                field.Content = editor; await AwaitPresentationAsync(field);
                Require(bound.GetBindingExpression(AutomationProperties.IsDataValidForFormProperty) is not null && bound.GetBindingExpression(AutomationProperties.AutomationIdProperty) is not null, "Field child replacement removed consumer bindings.");
                interactions.Add(new { theme = theme.ToString(), passed = true, retainedText, initialId, initial, added, removedError, removedMessages, actions, identityRetained = true, labelFocused = true, templateReplacement = true, remount = true, childReplacement = true, restoredMetadata = true, boundMetadataRestored = true, ownedTextScaleRestored = true });
                canvas.Children.Remove(field);
            }
            await File.WriteAllTextAsync(Path.Combine(folder, "field.json"), JsonSerializer.Serialize(new { passed = true, frames, interactions }, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "field", environment = RuntimeEnvironment() }));
        }
        finally { Content = previous; AppWindow.ResizeClient(previousSize); await AwaitPresentationAsync(CatalogRoot); }
    }
    private static async Task<JsonElement> ProbeFieldAsync(string folder, IntPtr window, string id)
    {
        var executable = Path.Combine(folder, "FieldUiaProbe.exe");
        var source = Path.Combine(AppContext.BaseDirectory, "FieldUiaProbe.cs");
        var signature = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(source)));
        var stamp = executable + ".source.sha256";
        if (!File.Exists(executable) || !File.Exists(stamp) || await File.ReadAllTextAsync(stamp) != signature)
        {
            var framework = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET/Framework64/v4.0.30319");
            var compile = new ProcessStartInfo(Path.Combine(framework, "csc.exe")) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var argument in new[] { "/nologo", "/target:exe", "/out:" + executable, "/reference:" + Path.Combine(framework, "System.Runtime.Serialization.dll"), Path.Combine(AppContext.BaseDirectory, "FieldUiaProbe.cs") }) compile.ArgumentList.Add(argument);
            using var compiler = Process.Start(compile) ?? throw new InvalidOperationException("Could not compile Field UIA client.");
            var messages = compiler.StandardOutput.ReadToEndAsync(); var errors = compiler.StandardError.ReadToEndAsync();
            await compiler.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)); Require(compiler.ExitCode == 0, "Field UIA compiler failed: " + await messages + await errors);
            await File.WriteAllTextAsync(stamp, signature);
        }
        var output = Path.Combine(folder, "uia-" + id + ".json");
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardError = true };
        foreach (var argument in new[] { window.ToInt64().ToString(), Environment.ProcessId.ToString(), output }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start Field UIA client."); var errorsRead = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)); Require(process.ExitCode == 0, "Field UIA client failed: " + await errorsRead);
        using var result = JsonDocument.Parse(await File.ReadAllTextAsync(output)); return result.RootElement.Clone();
    }
}
