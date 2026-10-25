using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Orchard;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class LabelAssociationFixture
{
    internal static async Task RunAsync(Window window, string output, string hostAssembly, Func<FrameworkElement>? catalogFactory = null)
    {
        var checks = new List<object>(); var observations = new List<object>(); var failures = new List<object>(); var pngs = new List<object>();
        var guards = 0; var activations = 0; var focus = 0; var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var root = new Canvas { Width = 900, Height = 1100 }; window.Content = root;
        window.Activated += (_, e) => { if (e.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++;
        var property = AutomationProperties.LabeledByProperty;
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard() { guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); Require(!window.Visible && !window.AppWindow.IsVisible && !IsWindowVisible(handle) && foreground != Environment.ProcessId && activations == 0 && focus == 0, "Label fixture acquired visibility, activation or focus."); }
        async Task Layout() { for (var i = 0; i < 4; i++) { root.Measure(new Size(900, 1100)); root.Arrange(new Rect(0, 0, 900, 1100)); root.UpdateLayout(); Guard(); await Task.Delay(10); } }
        async Task<StackPanel> Mount(params FrameworkElement[] elements) { var panel = new StackPanel { Width = 420, Spacing = 8 }; root.Children.Add(panel); foreach (var e in elements) panel.Children.Add(e); await Layout(); Require(elements.All(e => e.IsLoaded), "Label case did not mount in real XAML."); return panel; }
        async Task Case(string id, Func<Task> action) { try { Guard(); await action(); Guard(); checks.Add(new { id, passed = true }); } catch (Exception e) { checks.Add(new { id, passed = false }); failures.Add(new { id, error = e.ToString() }); } finally { root.Children.Clear(); await Layout(); } }
        static TextBlock Caption(string id) => new() { Name = id, Text = id switch { "first" => "First caption", "second" => "Second caption", "third" => "Third caption", _ => id } };
        TextBox Input() => new() { Width = 384, Style = (Style)Application.Current.Resources["BleeckerTextBoxStyle"] };
        static Binding Bind(object source, string name, BindingMode mode = BindingMode.OneWay) => new() { Source = source, Path = new PropertyPath(name), Mode = mode };
        static Style Styled(TextBox input, UIElement caption) { var style = new Style { TargetType = typeof(TextBox), BasedOn = input.Style }; style.Setters.Add(new Setter(AutomationProperties.LabeledByProperty, caption)); return style; }
        void Observe(string id, Control input, UIElement? expected, Binding? binding = null, Border? producer = null, string? inputName = null)
        {
            var actual = AutomationProperties.GetLabeledBy(input); var expression = input.GetBindingExpression(property); var local = input.ReadLocalValue(property);
            var kind = expression is not null ? "binding" : local == DependencyProperty.UnsetValue ? "unset" : local is null ? "null" : "element";
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(input); var expectedName = inputName ?? (expected is BleeckerLabel label ? AutomationProperties.GetName(label).Length > 0 ? AutomationProperties.GetName(label) : label.Content as string ?? "" : (expected as TextBlock)?.Text ?? "");
            var match = binding is null ? expression is null : ReferenceEquals(expression?.ParentBinding, binding);
            observations.Add(new { id, relationshipId = (actual as FrameworkElement)?.Name, relationshipName = peer.GetLabeledBy()?.GetName(), inputName = peer.GetName(), localKind = kind, originalBindingRetained = match, bindingMode = expression?.ParentBinding.Mode.ToString(), producerId = (producer?.Tag as FrameworkElement)?.Name, inputLoaded = input.IsLoaded });
            Require(ReferenceEquals(actual, expected) && match && peer.GetName() == expectedName, "Caller relationship, binding or accessible name was not preserved.");
        }
        async Task Capture(string name, FrameworkElement element)
        {
            Guard(); var image = new RenderTargetBitmap(); await image.RenderAsync(element); Guard(); var pixels = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await image.GetPixelsAsync());
            Require(image.PixelWidth > 0 && image.PixelHeight > 0 && pixels.Any(p => p != 0), "Label fixture capture is empty."); using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)image.PixelWidth, (uint)image.PixelHeight, 96, 96, pixels); await encoder.FlushAsync(); stream.Seek(0); using var reader = new DataReader(stream.GetInputStreamAt(0)); await reader.LoadAsync((uint)stream.Size); var bytes = new byte[(int)stream.Size]; reader.ReadBytes(bytes);
            var file = name + ".png"; await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, file), bytes); pngs.Add(new { file, width = image.PixelWidth, height = image.PixelHeight, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)) }); Guard();
        }
        try
        {
            Guard(); await Layout();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                root.RequestedTheme = theme;
                foreach (var kind in new[] { "unset", "null", "local", "style", "OneWay", "TwoWay" }) await Case(theme + "/" + kind, async () =>
                {
                    var first = Caption("first"); var second = Caption("second"); var third = Caption("third"); var producer = new Border { Tag = first }; var input = Input(); Binding? binding = null;
                    if (kind == "null") AutomationProperties.SetLabeledBy(input, null);
                    if (kind == "local") AutomationProperties.SetLabeledBy(input, first);
                    if (kind == "style") input.Style = Styled(input, first);
                    if (kind is "OneWay" or "TwoWay") { binding = Bind(producer, "Tag", Enum.Parse<BindingMode>(kind)); input.SetBinding(property, binding); }
                    var label = new BleeckerLabel { Name = "temporary", Content = "Temporary caption", Target = input }; await Mount(first, second, third, input, label);
                    Observe(theme + "/" + kind + "/mounted", input, label, producer: binding is null ? null : producer); Require(ReferenceEquals(producer.Tag, first), "Label mutated its caller's source.");
                    producer.Tag = second; if (kind == "style") input.Style = Styled(input, second); await Layout(); label.Target = null; await Layout();
                    var expected = kind is "unset" or "null" ? null : kind == "local" ? first : second; Observe(theme + "/" + kind + "/released", input, expected, binding, binding is null ? null : producer);
                    if (kind is "OneWay" or "TwoWay") producer.Tag = third; else input.Style = Styled(input, third); await Layout();
                    expected = kind == "null" ? null : kind == "local" ? first : third; Observe(theme + "/" + kind + "/future", input, expected, binding, binding is null ? null : producer);
                });
                await Case(theme + "/newer-local", async () => { var third = Caption("third"); var input = Input(); var label = new BleeckerLabel { Name = "temporary", Content = "Temporary caption", Target = input }; await Mount(third, input, label); AutomationProperties.SetLabeledBy(input, third); label.Target = null; await Layout(); Observe(theme + "/newer-local/released", input, third); });
                await Case(theme + "/newer-style-same", async () =>
                {
                    var first = Caption("first"); var third = Caption("third"); var input = Input(); AutomationProperties.SetLabeledBy(input, first); var label = new BleeckerLabel { Name = "temporary", Content = "Temporary caption", Target = input }; await Mount(first, third, input, label);
                    input.Style = Styled(input, label); input.ClearValue(property); label.Target = null; await Layout(); Observe(theme + "/newer-style-same/released", input, label); input.Style = Styled(input, third); await Layout(); Observe(theme + "/newer-style-same/updated", input, third);
                });
                foreach (var same in new[] { false, true }) await Case(theme + "/newer-binding-" + (same ? "same" : "different"), async () =>
                {
                    var second = Caption("second"); var third = Caption("third"); var input = Input(); var label = new BleeckerLabel { Name = "temporary", Content = "Temporary caption", Target = input }; await Mount(second, third, input, label);
                    var producer = new Border { Tag = same ? label : second }; var binding = Bind(producer, "Tag"); input.SetBinding(property, binding); await Layout(); label.Target = null; await Layout(); var id = theme + "/newer-binding-" + (same ? "same" : "different"); Observe(id + "/released", input, same ? label : second, binding, producer);
                    producer.Tag = third; await Layout(); Observe(id + "/updated", input, third, binding, producer);
                });
                await Case(theme + "/retarget-binding", async () =>
                {
                    var first = Caption("first"); var second = Caption("second"); var third = Caption("third"); var a = Input(); var b = Input(); var pa = new Border { Tag = first }; var pb = new Border { Tag = first }; var ba = Bind(pa, "Tag"); var bb = Bind(pb, "Tag"); a.SetBinding(property, ba); b.SetBinding(property, bb);
                    var controller = new Border { Tag = a }; var targetBinding = Bind(controller, "Tag"); var label = new BleeckerLabel { Name = "temporary", Content = "Temporary caption" }; label.SetBinding(BleeckerLabel.TargetProperty, targetBinding); await Mount(first, second, third, a, b, label);
                    pa.Tag = second; pb.Tag = third; controller.Tag = b; await Layout(); Observe(theme + "/retarget-binding/first-released", a, second, ba, pa); Observe(theme + "/retarget-binding/second-owned", b, label);
                    controller.Tag = null; await Layout(); Observe(theme + "/retarget-binding/second-released", b, third, bb, pb); Require(ReferenceEquals(label.GetBindingExpression(BleeckerLabel.TargetProperty)?.ParentBinding, targetBinding), "Label replaced caller Target binding.");
                });
                await Case(theme + "/unload-reload", async () =>
                {
                    var first = Caption("first"); var second = Caption("second"); var third = Caption("third"); var producer = new Border { Tag = first }; var input = Input(); var binding = Bind(producer, "Tag"); input.SetBinding(property, binding); var label = new BleeckerLabel { Name = "temporary", Content = "Temporary caption", Target = input }; var panel = await Mount(first, second, third, input, label);
                    producer.Tag = second; panel.Children.Remove(label); await Layout(); Require(!label.IsLoaded, "Label did not unload."); Observe(theme + "/unload-reload/released", input, second, binding, producer);
                    producer.Tag = third; await Layout(); Observe(theme + "/unload-reload/unloaded-updated", input, third, binding, producer); panel.Children.Add(label); await Layout(); Require(ReferenceEquals(AutomationProperties.GetLabeledBy(input), label), "Reload lost association."); panel.Children.Remove(label); await Layout(); Observe(theme + "/unload-reload/remounted-released", input, third, binding, producer);
                });
                await Case(theme + "/content-rich-disabled", async () =>
                {
                    var producer = new TextBlock { Text = "Temporary caption" }; var input = Input(); var label = new BleeckerLabel { Name = "temporary", Target = input }; var binding = Bind(producer, "Text"); label.SetBinding(ContentControl.ContentProperty, binding); await Mount(input, label);
                    producer.Text = "Updated temporary caption"; await Layout(); Observe(theme + "/content-rich-disabled/updated", input, label); Require(ReferenceEquals(label.GetBindingExpression(ContentControl.ContentProperty)?.ParentBinding, binding) && !label.IsTabStop, "Label replaced content binding or added a tab stop.");
                    label.IsEnabled = false; input.IsEnabled = false; await Layout(); Observe(theme + "/content-rich-disabled/disabled", input, label);
                    label.IsEnabled = true; input.IsEnabled = true; label.ClearValue(ContentControl.ContentProperty); var rich = new StackPanel { Orientation = Orientation.Horizontal }; var text = new TextBlock { Text = "Rich caption" }; rich.Children.Add(text); rich.Children.Add(new Button { Content = "Help", IsTabStop = false }); label.Content = rich; AutomationProperties.SetName(label, "Rich temporary caption"); AutomationProperties.SetName(input, "Explicit input name"); await Layout(); Require(ReferenceEquals(((ContentPresenter)((Grid)VisualTreeHelper.GetChild(label, 0)).FindName("PART_Content")).Content, rich), "Rich content was replaced."); Observe(theme + "/content-rich-disabled/rich", input, label, inputName: "Explicit input name");
                    label.Content = "Plain caption"; AutomationProperties.SetName(label, ""); input.ClearValue(AutomationProperties.NameProperty); await Layout(); Require(VisualTreeHelper.GetParent(rich) is null, "Obsolete rich content remained parented."); Observe(theme + "/content-rich-disabled/plain", input, label);
                });
                if (catalogFactory is not null) await Case(theme + "/catalog", async () =>
                {
                    var fixture = catalogFactory(); var input = (TextBox)fixture.FindName("AssociationInput"); var originalBinding = input.GetBindingExpression(property)?.ParentBinding; Require(originalBinding is not null, "Catalog did not declare its original association binding."); root.Children.Add(fixture); await Layout(); var label = (BleeckerLabel)fixture.FindName("AssociationLabel"); var toggle = (ToggleSwitch)fixture.FindName("AssociationEnabled"); var producer = (Border)fixture.FindName("AssociationProducer"); var original = (TextBlock)fixture.FindName("AssociationOriginal"); var alternate = (TextBlock)fixture.FindName("AssociationAlternate");
                    Observe(theme + "/catalog/active", input, label); toggle.IsOn = false; await Layout(); Observe(theme + "/catalog/restored", input, original, originalBinding, producer); producer.Tag = alternate; await Layout(); Observe(theme + "/catalog/updated", input, alternate, originalBinding, producer); await Capture(theme + "-catalog", fixture);
                });
            }
        }
        catch (Exception error) { failures.Add(new { id = "fixture", error = error.ToString() }); }
        finally
        {
            root.Children.Clear(); await Layout(); Guard(); var passed = failures.Count == 0;
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-label-association-native-v1", passed, checks, observations, failures, pngs, guards, activations, focus, hostVisible = window.Visible || window.AppWindow.IsVisible || IsWindowVisible(handle), hardwareInputSent = false, activateCalled = false, focusCalled = false, enabledButtonInvoked = false, catalogFixtureVerified = catalogFactory is not null, sourceSha256 = "d3e029333182d8d9fd6170fb37e47de522b594e928671232dba05937b3276dfe", librarySha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(BleeckerLabel).Assembly.Location))), hostSha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(hostAssembly))), processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), physicalInputVerified = false, actualDisplayOrTextScalingVerified = false, sourceRasterParityVerified = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close(); Environment.Exit(passed ? 0 : 1);
        }
    }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
}
