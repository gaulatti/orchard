using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Orchard;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class SwitchLifecycleFixture
{
    internal static async Task RunAsync(Window window, string output, string hostAssembly, Func<FrameworkElement>? catalogFactory = null)
    {
        var checks = new List<object>(); var failures = new List<object>(); var observations = new List<object>(); var images = new List<object>();
        var fontObservations = new List<object>(); var fontImages = new List<object>();
        var guards = 0; var focus = 0; var activations = 0; var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var root = new Canvas { Width = 900, Height = 1100 }; window.Content = root;
        root.GotFocus += (_, _) => focus++; window.Activated += (_, e) => { if (e.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard() { guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); Require(!window.Visible && !window.AppWindow.IsVisible && !IsWindowVisible(handle) && foreground != Environment.ProcessId && focus == 0 && activations == 0, "Switch lifecycle fixture acquired visibility, activation or focus."); }
        async Task Layout() { for (var i = 0; i < 4; i++) { root.Measure(new Size(900, 1100)); root.Arrange(new Rect(0, 0, 900, 1100)); root.UpdateLayout(); Guard(); await Task.Delay(10); } }
        async Task Case(string id, Func<Task> action) { try { Guard(); await action(); Guard(); checks.Add(new { id, passed = true }); } catch (Exception error) { checks.Add(new { id, passed = false }); failures.Add(new { id, error = error.ToString() }); } finally { root.Children.Clear(); await Layout(); } }
        static AutomationPeer Peer(BleeckerSwitch control) => FrameworkElementAutomationPeer.CreatePeerForElement(control);
        static IToggleProvider Toggle(BleeckerSwitch control) => (IToggleProvider)Peer(control).GetPattern(PatternInterface.Toggle);
        static Binding Bind(Border producer) => new() { Source = producer, Path = new PropertyPath("Tag"), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
        async Task Mount(BleeckerSwitch control) { root.Children.Add(control); await Layout(); Require(control.IsLoaded && Find<ToggleSwitch>(control, "PART_Switch")?.IsLoaded == true, "Native switch input did not mount."); }
        void State(string id, BleeckerSwitch control, Border producer, Binding binding, int? events, bool expected, bool nativeRequired = true)
        {
            var native = Find<ToggleSwitch>(control, "PART_Switch"); var retained = ReferenceEquals(control.GetBindingExpression(BleeckerSwitch.IsOnProperty)?.ParentBinding, binding); var toggleState = Toggle(control).ToggleState;
            observations.Add(new { id, publicValue = control.IsOn, callerValue = producer.Tag, toggleState = toggleState.ToString(), originalBindingRetained = retained, nativeValue = native?.IsOn, nativeLoaded = native?.IsLoaded ?? false, controlLoaded = control.IsLoaded, events });
            Require(control.IsOn == expected && Equals(producer.Tag, expected) && toggleState == (expected ? ToggleState.On : ToggleState.Off) && retained && (!nativeRequired || native?.IsOn == expected), "Switch state, metadata or caller binding diverged.");
        }
        async Task Reject(string id, BleeckerSwitch control, Border? producer, Action action, string expected)
        {
            var value = control.IsOn; var source = producer?.Tag; var events = 0; RoutedEventHandler listener = (_, _) => events++; control.Toggled += listener;
            string? exception = null; int? hresult = null; try { action(); } catch (Exception e) { exception = e.GetType().Name; hresult = e.HResult; }
            await Layout(); control.Toggled -= listener; var unchanged = value == control.IsOn && Equals(source, producer?.Tag); var expectedHResult = expected == nameof(ElementNotAvailableException) ? unchecked((int)0x80040201) : unchecked((int)0x80040200);
            observations.Add(new { id, expectedException = expected, expectedHResult, exception, hresult, stateUnchanged = unchanged, events, controlLoaded = control.IsLoaded, nativeLoaded = Find<ToggleSwitch>(control, "PART_Switch")?.IsLoaded ?? false, enabled = control.IsEnabled });
            Require((exception == expected || hresult == expectedHResult) && unchanged && events == 0, $"Retired/disabled Switch action was accepted: {id}; error={exception ?? "none"}, changed={!unchanged}, events={events}.");
        }
        async Task Capture(string name, FrameworkElement element, List<object>? targetImages = null)
        {
            Guard(); var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(element); Guard(); var pixels = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync()); Require(bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0 && pixels.Any(p => p != 0), "Switch capture is empty."); using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream); encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels); await encoder.FlushAsync(); stream.Seek(0); using var reader = new DataReader(stream.GetInputStreamAt(0)); await reader.LoadAsync((uint)stream.Size); var bytes = new byte[(int)stream.Size]; reader.ReadBytes(bytes); var file = name + ".png"; await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, file), bytes); (targetImages ?? images).Add(new { file, width = bitmap.PixelWidth, height = bitmap.PixelHeight, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)) });
        }
        async Task RetiredNative(string id, BleeckerSwitch control, Border producer, ToggleSwitch input, IToggleProvider provider)
        {
            var value = control.IsOn; var beforeNative = input.IsOn; var events = 0; RoutedEventHandler listener = (_, _) => events++; control.Toggled += listener; string? exception = null;
            try { provider.Toggle(); } catch (Exception e) when (e is ElementNotAvailableException || e.HResult == unchecked((int)0x80040201)) { exception = e.GetType().Name; }
            await Layout(); control.Toggled -= listener; var unchanged = control.IsOn == value && Equals(producer.Tag, value);
            observations.Add(new { id, retiredNativeActionAttempted = true, beforeNative, afterNative = input.IsOn, publicValue = control.IsOn, callerValue = producer.Tag, stateUnchanged = unchanged, events, controlLoaded = control.IsLoaded, nativeLoaded = input.IsLoaded, exception });
            Require(unchanged && events == 0, "Retired native switch still changed caller state.");
        }
        try
        {
            await Layout();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                root.RequestedTheme = theme;
                foreach (var initial in new[] { false, true })
                {
                    var name = initial ? "on" : "off";
                    await Case($"{theme}/untemplated/{name}", async () =>
                    {
                        var control = new BleeckerSwitch { IsOn = initial, Label = "Unparented switch" }; var peer = Peer(control); Require(VisualTreeHelper.GetChildrenCount(control) == 0 && peer.IsControlElement() && peer.IsContentElement() && peer.IsEnabled(), "Untemplated Switch owner metadata is unavailable."); string? error = null; ToggleState? state = null; try { state = Toggle(control).ToggleState; } catch (Exception e) { error = e.GetType().Name; }
                        observations.Add(new { id = $"{theme}/untemplated/{name}", publicValue = control.IsOn, toggleState = state?.ToString(), exception = error, visualChildren = VisualTreeHelper.GetChildrenCount(control) }); Require(error is null && state == (initial ? ToggleState.On : ToggleState.Off), "Untemplated Toggle state did not reflect authoritative IsOn.");
                        AutomationProperties.SetAccessibilityView(control, AccessibilityView.Raw); Require(!peer.IsControlElement() && !peer.IsContentElement(), "Switch ignored Raw accessibility view."); AutomationProperties.SetAccessibilityView(control, AccessibilityView.Content); Require(peer.IsControlElement() && peer.IsContentElement(), "Switch ignored Content accessibility view."); await Layout();
                    });
                    await Case($"{theme}/never-mounted/{name}", async () => { var control = new BleeckerSwitch { IsOn = initial }; var provider = Toggle(control); await Reject($"{theme}/never-mounted/{name}", control, null, provider.Toggle, nameof(ElementNotAvailableException)); });
                    await Case($"{theme}/two-way/{name}", async () =>
                    {
                        var producer = new Border { Tag = initial }; var binding = Bind(producer); var control = new BleeckerSwitch { Label = "Caller state" }; control.SetBinding(BleeckerSwitch.IsOnProperty, binding); var events = 0; control.Toggled += (_, _) => events++; await Mount(control); events = 0; var provider = Toggle(control); provider.Toggle(); await Layout(); State($"{theme}/two-way/{name}/toggled", control, producer, binding, events, !initial); Require(events == 1, "Native Toggle raised duplicate caller events."); producer.Tag = initial; await Layout(); State($"{theme}/two-way/{name}/producer", control, producer, binding, events, initial); Require(events == 2, "Caller update raised duplicate Toggle events.");
                    });
                    await Case($"{theme}/unloaded/{name}", async () =>
                    {
                        var producer = new Border { Tag = initial }; var binding = Bind(producer); var control = new BleeckerSwitch { Label = "Retained handle" }; control.SetBinding(BleeckerSwitch.IsOnProperty, binding); await Mount(control); var provider = Toggle(control); root.Children.Remove(control); await Layout(); Require(!control.IsLoaded, "Switch did not unload."); await Reject($"{theme}/unloaded/{name}", control, producer, provider.Toggle, nameof(ElementNotAvailableException)); producer.Tag = !initial; await Layout(); State($"{theme}/unloaded/{name}/producer", control, producer, binding, null, !initial, false); root.Children.Add(control); await Layout(); provider.Toggle(); await Layout(); State($"{theme}/unloaded/{name}/remounted", control, producer, binding, null, initial);
                    });
                    await Case($"{theme}/template-cleared/{name}", async () =>
                    {
                        var producer = new Border { Tag = initial }; var binding = Bind(producer); var control = new BleeckerSwitch { Label = "Template lifecycle" }; control.SetBinding(BleeckerSwitch.IsOnProperty, binding); await Mount(control); var template = control.Template; var provider = Toggle(control); var input = Find<ToggleSwitch>(control, "PART_Switch")!; control.Template = null; await Layout(); Require(control.IsLoaded && !input.IsLoaded && Find<ToggleSwitch>(control, "PART_Switch") is null, "Template clearing did not retire its native input."); await Reject($"{theme}/template-cleared/{name}", control, producer, provider.Toggle, nameof(ElementNotAvailableException)); producer.Tag = !initial; await Layout(); State($"{theme}/template-cleared/{name}/producer", control, producer, binding, null, !initial, false); control.Template = template; control.ApplyTemplate(); await Layout(); provider.Toggle(); await Layout(); State($"{theme}/template-cleared/{name}/restored", control, producer, binding, null, initial);
                    });
                    await Case($"{theme}/disabled/{name}", async () => { var producer = new Border { Tag = initial }; var control = new BleeckerSwitch { IsEnabled = false, Label = "Disabled state" }; control.SetBinding(BleeckerSwitch.IsOnProperty, Bind(producer)); await Mount(control); await Reject($"{theme}/disabled/{name}", control, producer, Toggle(control).Toggle, nameof(ElementNotEnabledException)); });
                    foreach (var cleared in new[] { false, true }) await Case($"{theme}/native-{(cleared ? "template-cleared" : "unloaded")}/{name}", async () =>
                    {
                        var kind = cleared ? "template-cleared" : "unloaded"; var producer = new Border { Tag = initial }; var binding = Bind(producer); var control = new BleeckerSwitch { Label = "Retired native input" }; control.SetBinding(BleeckerSwitch.IsOnProperty, binding); await Mount(control); var template = control.Template; var input = Find<ToggleSwitch>(control, "PART_Switch")!; var retired = (IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(input).GetPattern(PatternInterface.Toggle); if (cleared) control.Template = null; else root.Children.Remove(control); await Layout(); Require(!input.IsLoaded, "Native input did not retire."); await RetiredNative($"{theme}/native-{kind}/{name}", control, producer, input, retired);
                        if (cleared) { control.Template = template; control.ApplyTemplate(); } else root.Children.Add(control); await Layout(); State($"{theme}/native-{kind}/{name}/restored", control, producer, binding, null, initial); Toggle(control).Toggle(); await Layout(); State($"{theme}/native-{kind}/{name}/current", control, producer, binding, null, !initial);
                    });
                    await Case($"{theme}/retemplate/{name}", async () =>
                    {
                        var producer = new Border { Tag = initial }; var binding = Bind(producer); var control = new BleeckerSwitch { Label = "Replaced input" }; control.SetBinding(BleeckerSwitch.IsOnProperty, binding); await Mount(control); var template = control.Template; var provider = Toggle(control); var old = Find<ToggleSwitch>(control, "PART_Switch")!; var retired = (IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(old).GetPattern(PatternInterface.Toggle); control.Template = null; await Layout(); control.Template = template; control.ApplyTemplate(); await Layout(); var next = Find<ToggleSwitch>(control, "PART_Switch")!; Require(next.IsLoaded && !ReferenceEquals(old, next), "Template replacement reused the retired input."); var events = 0; control.Toggled += (_, _) => events++; try { retired.Toggle(); } catch (Exception e) when (e is ElementNotAvailableException || e.HResult == unchecked((int)0x80040201)) { } await Layout(); State($"{theme}/retemplate/{name}/retired", control, producer, binding, events, initial); Require(events == 0, "Retired native input remained subscribed to public state."); provider.Toggle(); await Layout(); State($"{theme}/retemplate/{name}/current", control, producer, binding, events, !initial); Require(events == 1, "Current template did not deliver exactly one toggle.");
                    });
                    await Case($"{theme}/appearance/{name}", async () =>
                    {
                        var control = new BleeckerSwitch { IsOn = initial, Label = "Enable notifications" }; await Mount(control); var track = Find<Border>(control, "PART_Track")!; var thumb = Find<Microsoft.UI.Xaml.Shapes.Ellipse>(control, "PART_Thumb")!; var label = Find<TextBlock>(control, "PART_Label")!; var knob = Find<Grid>(control, "SwitchKnob")!; observations.Add(new { id = $"{theme}/appearance/{name}", trackWidth = track.ActualWidth, trackHeight = track.ActualHeight, radius = track.CornerRadius.TopLeft, thumbWidth = thumb.ActualWidth, thumbHeight = thumb.ActualHeight, translateX = ((TranslateTransform)knob.RenderTransform).X, labelSize = label.FontSize, font = label.FontFamily.Source }); Require(track.ActualWidth == 36 && track.ActualHeight == 20 && thumb.ActualWidth == 16 && thumb.ActualHeight == 16 && ((TranslateTransform)knob.RenderTransform).X == (initial ? 16 : 0), "Switch geometry diverged."); await Capture($"{theme}-{name}", control);
                    });
                }
                await Case(theme + "/names", async () => { var control = new BleeckerSwitch { Label = "Source label" }; await Mount(control); var peer = Peer(control); Require(peer.GetName() == "Source label", "Default accessible label is wrong."); AutomationProperties.SetName(control, "Explicit label"); control.Label = "Changed visible label"; Require(peer.GetName() == "Explicit label", "Explicit name lost precedence."); var caption = new TextBlock { Text = "Associated label" }; root.Children.Add(caption); AutomationProperties.SetName(control, ""); AutomationProperties.SetLabeledBy(control, caption); await Layout(); Require(peer.GetName() == "Associated label", "Associated label lost precedence."); observations.Add(new { id = theme + "/names", associatedName = peer.GetName(), children = peer.GetChildren()?.Count ?? 0 }); Require((peer.GetChildren()?.Count ?? 0) == 0, "Switch duplicated its native accessibility child."); });
                await Case(theme + "/constrained", async () => { var panel = new StackPanel { Width = 220, Spacing = 12 }; root.Children.Add(panel); foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft }) panel.Children.Add(new BleeckerSwitch { Width = 200, Label = "Keep notifications enabled while reviewing the editorial queue — 通知 — الإشعارات", FlowDirection = direction }); await Layout(); Require(panel.Children.Cast<BleeckerSwitch>().All(c => c.ActualWidth == 200 && c.ActualHeight > 20), "Constrained Switch label did not wrap."); await Capture(theme + "-constrained", panel); });
                // Font fallback uses the same public Label/FontFamily bindings.
                // Record baseline failures without weakening lifecycle checks.
                {
                    var labelProducer = new Border { Tag = "通知を有効にする" };
                    var control = new BleeckerSwitch(); var labelBinding = Bind(labelProducer);
                    control.SetBinding(BleeckerSwitch.LabelProperty, labelBinding);
                    await Mount(control);
                    var defaultFamily = control.FontFamily;
                    Require(defaultFamily.Source.Contains("/EncodeSans-Medium.ttf#Encode Sans", StringComparison.Ordinal), "Read the styled default font after mounting.");
                    var fontProducer = new Border { Tag = defaultFamily }; var fontBinding = Bind(fontProducer);
                    control.SetBinding(Control.FontFamilyProperty, fontBinding);
                    var toggles = 0; control.Toggled += (_, _) => toggles++;
                    await Layout();
                    async Task RecordFont(string stage, bool unloadedEditObserved = false)
                    {
                        await Layout(); var text = Find<TextBlock>(control, "PART_Label")!;
                        var labelRetained = ReferenceEquals(control.GetBindingExpression(BleeckerSwitch.LabelProperty)?.ParentBinding, labelBinding);
                        var fontRetained = ReferenceEquals(control.GetBindingExpression(Control.FontFamilyProperty)?.ParentBinding, fontBinding);
                        Require(labelRetained && fontRetained && text.Text == control.Label && control.Label == (string)labelProducer.Tag && toggles == 0, "Font probe lost caller bindings, displayed text or toggle isolation.");
                        fontObservations.Add(new { id = theme + "/" + stage, label = text.Text, ownerWidth = control.ActualWidth, ownerHeight = control.ActualHeight, labelWidth = text.ActualWidth, labelHeight = text.ActualHeight, ownerFamily = control.FontFamily.Source, labelFamily = text.FontFamily.Source, labelWeight = text.FontWeight.Weight, accessibleName = Peer(control).GetName(), labelBindingRetained = labelRetained, fontBindingRetained = fontRetained, nativePartTextBinding = text.GetBindingExpression(TextBlock.TextProperty) is not null, unloadedEditObserved, toggles, runs = text.Inlines.OfType<Run>().Select(run => new { text = run.Text, family = run.FontFamily.Source, explicitFamily = run.ReadLocalValue(TextElement.FontFamilyProperty) != DependencyProperty.UnsetValue }).ToArray() });
                    }
                    await RecordFont("japanese"); await Capture(theme + "-japanese", control, fontImages);
                    labelProducer.Tag = "Enable notifications"; await RecordFont("latin");
                    labelProducer.Tag = "通知を有効にする"; await RecordFont("japanese-rebound");
                    fontProducer.Tag = new FontFamily("Segoe UI"); await RecordFont("caller-font");
                    fontProducer.Tag = defaultFamily; await RecordFont("font-restored");
                    root.Children.Remove(control); await Layout(); labelProducer.Tag = "Unloaded caller edit"; await Layout();
                    Require(control.Label == "Unloaded caller edit" && ReferenceEquals(control.GetBindingExpression(BleeckerSwitch.LabelProperty)?.ParentBinding, labelBinding), "Unloaded label binding stopped observing its producer.");
                    labelProducer.Tag = "通知を有効にする"; await Mount(control); await RecordFont("reloaded", true);
                    var original = control.Template; control.Template = null; await Layout(); control.Template = original; await Layout(); await RecordFont("template-restored");
                    root.Children.Clear(); await Layout();
                }
                if (catalogFactory is not null) await Case(theme + "/catalog", async () => { var fixture = catalogFactory(); root.Children.Add(fixture); await Layout(); var control = (BleeckerSwitch)fixture.FindName("Bound"); var before = control.IsOn; Toggle(control).Toggle(); await Layout(); Require(control.IsOn != before && ((TextBlock)fixture.FindName("BindingStatus")).Text == control.IsOn.ToString(), "Catalog did not display the bound Toggle result."); observations.Add(new { id = theme + "/catalog", before, after = control.IsOn, displayed = ((TextBlock)fixture.FindName("BindingStatus")).Text }); });
            }
        }
        catch (Exception error) { failures.Add(new { id = "fixture", error = error.ToString() }); }
        finally
        {
            root.Children.Clear(); await Layout(); Guard(); var passed = failures.Count == 0;
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-switch-lifecycle-native-v1", passed, checks, failures, observations, images, fontObservations, fontImages, guards, focus, activations, hostVisible = window.Visible || window.AppWindow.IsVisible || IsWindowVisible(handle), activateCalled = false, focusCalled = false, hardwareInputSent = false, enabledButtonInvoked = false, catalogFixtureVerified = catalogFactory is not null, sourceSha256 = "fef5462601f57383097a272f3fc5fb522e7fc29e6e84ba5720ef45a48dcb430c", librarySha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(BleeckerSwitch).Assembly.Location))), hostSha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(hostAssembly))), processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), physicalInputVerified = false, actualDisplayOrTextScalingVerified = false, sourceRasterParityVerified = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close(); Environment.Exit(passed ? 0 : 1);
        }
    }
    private static T? Find<T>(DependencyObject root, string name) where T : FrameworkElement { if (root is T match && match.Name == name) return match; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) if (Find<T>(VisualTreeHelper.GetChild(root, i), name) is { } found) return found; return null; }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
}
