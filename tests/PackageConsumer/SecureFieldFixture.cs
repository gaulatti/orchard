using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

// Public API boundary shared by the source catalog and an isolated NuGet app.
// It never focuses, shows or activates a window and never serializes a password.
internal static class SecureFieldFixture
{
    private sealed class ValueOwner : INotifyPropertyChanged
    {
        private string value = string.Empty;
        public string Value { get => value; set { this.value = value; PropertyChanged?.Invoke(this, new(nameof(Value))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private delegate bool Visitor(nint handle, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    {
        if (node is FrameworkElement element) yield return element;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            foreach (var child in Elements(VisualTreeHelper.GetChild(node, i))) yield return child;
    }
    internal static async Task RunAsync(Window window, string output, string hostPath)
    {
        var folder = Path.GetDirectoryName(output)!; Directory.CreateDirectory(folder);
        var root = new Grid { Width = 720, Height = 520, Padding = new Thickness(40) };
        window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(720, 520));
        var guards = 0; var focus = 0; var activations = 0; var invokes = 0;
        var cases = new List<object>(); var pngs = new List<object>(); var stage = "initial";
        root.GotFocus += (_, _) => focus++;
        window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var owner); visible |= owner == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0 && VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0, "Secure Field probe left its hidden boundary.");
        }
        async Task Layout()
        {
            for (var i = 0; i < 4; i++)
            {
                var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Could not drain Secure Field layout.");
                await idle.Task; root.Measure(new Size(720, 520)); root.Arrange(new Rect(0, 0, 720, 520)); root.UpdateLayout(); Guard(); await Task.Delay(12);
            }
        }
        async Task Capture(string name, FrameworkElement target)
        {
            Guard(); var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(target); Guard();
            var pixels = await bitmap.GetPixelsAsync(); using var stream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(pixels));
            await encoder.FlushAsync(); stream.Seek(0); using var reader = new DataReader(stream.GetInputStreamAt(0)); await reader.LoadAsync((uint)stream.Size); var bytes = new byte[(int)stream.Size]; reader.ReadBytes(bytes);
            await File.WriteAllBytesAsync(Path.Combine(folder, name), bytes); pngs.Add(new { file = name, width = bitmap.PixelWidth, height = bitmap.PixelHeight, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)) }); Guard();
        }
        try
        {
            foreach (var scheme in new[] { "Light", "Dark" })
            {
                root.RequestedTheme = Enum.Parse<ElementTheme>(scheme);
                foreach (var width in new[] { 160d, 320d, 480d })
                {
                    stage = scheme + "-" + width; var owner = new ValueOwner(); var changes = 0;
                    var field = new BleeckerSecureField { Width = width, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, PlaceholderText = "Fictional sample" };
                    AutomationProperties.SetName(field, "Fictional sample"); field.PasswordChanged += (_, _) => changes++;
                    field.SetBinding(BleeckerSecureField.PasswordProperty, new Binding { Source = owner, Path = new PropertyPath(nameof(ValueOwner.Value)), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                    root.Children.Add(field); await Layout();
                    var reveal = Elements(field).OfType<BleeckerIconButton>().Single(e => e.Name == "SecureRevealButton");
                    var nativeEditor = Elements(field).OfType<ScrollViewer>().Single(e => e.Name == "ContentElement");
                    var placeholder = Elements(field).OfType<TextBlock>().Single(e => e.Name == "PlaceholderTextContentPresenter");
                    var editor = Elements(field).OfType<PasswordBox>().Single();
                    var peer = FrameworkElementAutomationPeer.CreatePeerForElement(editor);
                    var buttonPeer = FrameworkElementAutomationPeer.CreatePeerForElement(reveal);
                    var provider = (IInvokeProvider)buttonPeer.GetPattern(PatternInterface.Invoke);
                    async Task Toggle() { Guard(); provider.Invoke(); invokes++; await Layout(); Guard(); }
                    Require(!field.IsRevealed && editor.PasswordRevealMode == PasswordRevealMode.Hidden && field.Password.Length == 0 && peer.IsPassword() && peer.GetName() == "Fictional sample", "Native concealed default or accessible name is incorrect.");
                    Require(field.FontSize == 14 && field.ActualHeight == 40 && field.CornerRadius == new CornerRadius(7) && field.Padding.Left == 14 && field.Padding.Right == 14 && nativeEditor.ActualWidth > 0 && reveal.ActualWidth == 32 && reveal.ActualHeight == 32, "Secure Field source metrics were lost.");
                    Require(placeholder.Visibility == Visibility.Visible && buttonPeer.GetName() == "Show password", "Empty placeholder or reveal action missing.");
                    string Color(Microsoft.UI.Xaml.Media.Brush brush) => ((SolidColorBrush)brush).Color.ToString().ToLowerInvariant();
                    var backgroundColor = Color(field.Background); var borderColor = Color(field.BorderBrush); var foregroundColor = Color(field.Foreground);
                    Require(backgroundColor == (scheme == "Light" ? "#ffffffff" : "#ff182533") && borderColor == (scheme == "Light" ? "#14000000" : "#14ffffff") && foregroundColor == (scheme == "Light" ? "#ff2d2d2d" : "#fff0f4f8"), "The actual themed surface differs from Sabella tokens.");
                    var shadowHost = Elements(field).Single(e => e.Name == "SecureShadowHost"); var shadowVisual = ElementCompositionPreview.GetElementChildVisual(shadowHost);
                    if (scheme == "Dark") Require(shadowVisual is null, "Dark Secure Field retained the source's light-only shadow.");
                    else { var actualShadow = (Microsoft.UI.Composition.DropShadow)((Microsoft.UI.Composition.SpriteVisual)shadowVisual).Shadow; Require(Math.Abs(actualShadow.Opacity - .055) < .000001 && actualShadow.BlurRadius == 14 && actualShadow.Offset.Y == 5 && actualShadow.Color.ToString().ToLowerInvariant() == "#ff1a374d", "Native shadow properties differ from the explicit Sabella contract."); }
                    cases.Add(new { scheme, width, state = "empty", passed = true, nativePassword = true, concealed = true, sourceMetrics = true, height = field.ActualHeight, font = field.FontSize, radius = field.CornerRadius.TopLeft, inset = field.Padding.Left, revealWidth = reveal.ActualWidth, editorWidth = nativeEditor.ActualWidth, backgroundColor, borderColor, foregroundColor, sourceShadowProperties = true });
                    owner.Value = "Fictional component sample"; await Layout();
                    Require(field.Password == owner.Value && changes > 0 && placeholder.Visibility == Visibility.Collapsed, "Caller-owned value did not reach the native editor.");
                    var valueHidden = false;
                    if (peer.GetPattern(PatternInterface.Value) is IValueProvider value)
                    { try { _ = value.Value; } catch (Exception) { valueHidden = true; } }
                    else valueHidden = true;
                    Require(valueHidden && peer.IsPassword(), "Concealed UIA exposed the native password.");
                    var before = changes; await Toggle();
                    Require(field.IsRevealed && editor.PasswordRevealMode == PasswordRevealMode.Visible && editor.Password == owner.Value && field.Password == owner.Value && changes == before && buttonPeer.GetName() == "Hide password", "Reveal mutated the caller-owned value or failed to persist.");
                    await Layout(); Require(field.IsRevealed && editor.PasswordRevealMode == PasswordRevealMode.Visible, "Reveal did not persist through idle layout.");
                    cases.Add(new { scheme, width, state = "revealed", passed = true, valuePreserved = true, passwordEventsUnchanged = true, persistent = true, hiddenValueProtected = valueHidden });
                    if (width == 320) await Capture($"secure-{scheme}-revealed.png", field);
                    await Toggle(); Require(!field.IsRevealed && editor.PasswordRevealMode == PasswordRevealMode.Hidden && field.Password == owner.Value && buttonPeer.GetName() == "Show password", "Conceal failed to preserve the value.");
                    editor.Password = "Fictional replacement"; await Layout(); Require(owner.Value == field.Password && changes > before && editor.Password == owner.Value, "Native value update did not return to the caller binding.");
                    cases.Add(new { scheme, width, state = "concealed", passed = true, twoWayBinding = true, hiddenValueProtected = valueHidden });
                    if (width == 320) await Capture($"secure-{scheme}-concealed.png", field);
                    field.IsEnabled = false; await Layout(); Require(!reveal.IsEnabled && !buttonPeer.IsEnabled(), "Disabled field retained an enabled reveal action.");
                    var disabledOpacity = Elements(field).Single(e => e.Name == "SecureInterior").Opacity;
                    Require(Math.Abs(disabledOpacity - .55) < .000001, $"Disabled visual opacity was {disabledOpacity} instead of .55.");
                    var disabledBefore = field.IsRevealed; var rejectedDisabled = false;
                    try { provider.Invoke(); invokes++; }
                    catch (Microsoft.UI.Xaml.Automation.ElementNotEnabledException) { rejectedDisabled = true; }
                    catch (COMException error) when (error.HResult == unchecked((int)0x80040200)) { rejectedDisabled = true; }
                    await Layout();
                    Require(field.IsRevealed == disabledBefore && field.Password == owner.Value && rejectedDisabled, "Disabled invocation changed the field or was not rejected.");
                    cases.Add(new { scheme, width, state = "disabled", passed = true, revealDisabled = true, valuePreserved = true, rejectedDisabled, disabledOpacity });
                    field.IsEnabled = true; await Layout(); Require(Elements(field).Single(e => e.Name == "SecureInterior").Opacity == 1, "Reenabled field retained disabled opacity.");
                    var originalTemplate = field.Template; field.Template = null; field.Template = originalTemplate; await Layout();
                    reveal = Elements(field).OfType<BleeckerIconButton>().Single(e => e.Name == "SecureRevealButton");
                    buttonPeer = FrameworkElementAutomationPeer.CreatePeerForElement(reveal); provider = (IInvokeProvider)buttonPeer.GetPattern(PatternInterface.Invoke);
                    editor = Elements(field).OfType<PasswordBox>().Single();
                    await Toggle(); Require(field.IsRevealed && editor.PasswordRevealMode == PasswordRevealMode.Visible && field.Password == owner.Value && editor.Password == owner.Value, "Retemplate duplicated a reveal handler or lost the value.");
                    root.Children.Remove(field); await Layout(); root.Children.Add(field); await Layout(); Require(field.IsRevealed && editor.PasswordRevealMode == PasswordRevealMode.Visible && field.Password == owner.Value, "Unload/reload lost the caller's state.");
                    field.FontSize = 28; field.FlowDirection = FlowDirection.RightToLeft; await Layout();
                    nativeEditor = Elements(field).OfType<ScrollViewer>().Single(e => e.Name == "ContentElement");
                    Require(field.ActualHeight >= 40 && nativeEditor.ActualWidth > 0 && field.Password == owner.Value && field.IsTextScaleFactorEnabled, "Enlarged text or RTL lost native editing space/state.");
                    if (width == 320) await Capture($"secure-{scheme}-large-rtl.png", field);
                    root.Children.Remove(field); var form = new BleeckerField { Label = "Fictional field label", Description = "Fictional helper", Error = "Fictional validation", Content = field }; root.Children.Add(form); await Layout();
                    var compositePeer = FrameworkElementAutomationPeer.CreatePeerForElement(field); var formPeer = FrameworkElementAutomationPeer.CreatePeerForElement(editor);
                    Require(compositePeer.GetAutomationControlType() == AutomationControlType.Group && !compositePeer.IsDataValidForForm() && AutomationProperties.GetDescribedBy(field).Count == 2 && formPeer.GetLabeledBy() is not null && AutomationProperties.GetLabeledBy(editor) == AutomationProperties.GetLabeledBy(field), "Field label, descriptions or invalid state did not reach the native composite.");
                    form.Error = null; await Layout(); Require(compositePeer.IsDataValidForForm() && AutomationProperties.GetDescribedBy(field).Count == 1, "Clearing Field error left stale invalid metadata.");
                    form.Description = null; await Layout(); Require(AutomationProperties.GetDescribedBy(field).Count == 0, "Clearing Field description left stale relationships.");
                    cases.Add(new { scheme, width, state = "lifecycle-large-rtl", passed = true, retemplate = true, reload = true, valuePreserved = true, textScalingEnabled = true, enlargedFont = 28, height = field.ActualHeight, editorWidth = nativeEditor.ActualWidth, fieldMetadata = true });
                    owner.Value = string.Empty; await Layout(); Require(field.Password.Length == 0 && editor.Password.Length == 0, "Caller clear did not reach the native editor."); root.Children.Remove(form); form.Content = null; await Layout();
                }
            }
            string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-secure-field-native-v1", passed = true, cases, pngs, guards, focus, activations, invokes, publicApisOnly = true, passwordContentsLogged = false, activateCalled = false, showCalled = false, focusCalled = false, hardwareInputSent = false, physicalInputVerified = false, appleRenderingVerified = false, nativeComponentAccepted = false, librarySha256 = Hash(typeof(BleeckerSecureField).Assembly.Location), hostSha256 = Hash(hostPath) }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close(); Environment.Exit(0);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-secure-field-native-v1", passed = false, stage, error = error.GetType().Name + ": " + error.Message, hresult = error.HResult, stack = error.StackTrace, cases, guards, focus, activations, invokes }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close(); Environment.Exit(1);
        }
    }
}
