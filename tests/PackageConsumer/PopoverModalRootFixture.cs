using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Orchard;
using Windows.Foundation;

namespace PackageConsumer;

// Public package APIs and native visual inspection only. No Orchard internals,
// activation, focus APIs, hardware input or system settings are used.
internal static class PopoverModalRootFixture
{
    private delegate bool WindowVisitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    internal static async Task RunAsync(Window window, string output)
    {
        var root = (Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}'/>");
        window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(800, 600));
        var guards = 0; var focus = 0; var activations = 0; var invokes = 0; var cases = new List<object>(); var captures = new List<object>();
        window.Activated += (_, a) => { if (a.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++;
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var owner); visible |= owner == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && activations == 0 && focus == 0, "Package popup acquired visibility, activation or focus.");
            Require(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0, "Package hosted popup escaped the shared plane.");
        }
        async Task Layout()
        {
            for (var i = 0; i < 6; i++) { var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Package root could not drain."); await idle.Task; await Task.Delay(12); root.Measure(new Size(800, 600)); root.Arrange(new Rect(0, 0, 800, 600)); root.UpdateLayout(); Guard(); }
            Require(root.XamlRoot is { IsHostVisible: false, RasterizationScale: 1 } && root.XamlRoot.Size == new Size(800, 600), "Package root requires an actual hidden unscaled viewport.");
        }
        IEnumerable<DependencyObject> Parents(DependencyObject child) { for (var node = VisualTreeHelper.GetParent(child); node is not null; node = VisualTreeHelper.GetParent(node)) yield return node; }
        foreach (var scheme in new[] { "Light", "Dark" })
        {
            root.RequestedTheme = Enum.Parse<ElementTheme>(scheme);
            var open = new ToggleSwitch { IsOn = true }; var enabled = new ToggleSwitch { IsOn = true };
            var editor = new TextBox { Text = "Edited fictional package value", Width = 210 }; editor.SetBinding(Control.IsEnabledProperty, new Binding { Source = enabled, Path = new(nameof(ToggleSwitch.IsOn)) });
            var confirmation = new BleeckerAlertDialog { Title = "Fictional package confirmation", Description = "Original popup content stays caller-owned while this confirmation is open." }; confirmation.CancelRequested += (_, _) => confirmation.IsOpen = false;
            var action = new Button { Content = "Confirm fictional package value" }; action.Click += (_, _) => confirmation.IsOpen = true;
            var content = new StackPanel { Width = 240, Spacing = 12 }; content.Children.Add(editor); content.Children.Add(action); content.Children.Add(confirmation);
            var popover = new BleeckerPopover { Trigger = new Button { Content = "Fictional package settings" }, Content = content, Margin = new(40, 40, 0, 0) }; popover.SetBinding(BleeckerPopover.OpenProperty, new Binding { Source = open, Path = new(nameof(ToggleSwitch.IsOn)) });
            var host = new BleeckerModalHost { Content = popover }; root.Children.Add(host); await Layout();
            var frame = Parents(editor).OfType<Border>().First(b => b.Child is ContentControl);
            Require(popover.IsOpen && editor.IsLoaded && frame.IsHitTestVisible && ReferenceEquals(popover.Content, content) && popover.GetBindingExpression(BleeckerPopover.OpenProperty) is not null, "Fresh package did not retain original hosted popup/binding.");
            cases.Add(new { scheme, name = "public-hosted-original-content", passed = true, evidence = new { logicalOpen = true, originalContent = true, originalEditorLoaded = true, openBindingRetained = true, nativePopupCount = 0 } });
            var provider = FrameworkElementAutomationPeer.CreatePeerForElement(action).GetPattern(PatternInterface.Invoke) as IInvokeProvider ?? throw new InvalidOperationException("Original package button has no native invoke provider."); provider.Invoke(); invokes++; await Task.Delay(260); await Layout();
            Require(confirmation.IsOpen && !frame.IsHitTestVisible && popover.IsOpen && editor.Text == "Edited fictional package value", "Public confirmation failed to shield its original popup.");
            enabled.IsOn = false; await Layout(); Require(!editor.IsEnabled && editor.GetBindingExpression(Control.IsEnabledProperty) is not null, "Popup shield rewrote the caller enabled binding."); enabled.IsOn = true; await Layout(); Require(editor.IsEnabled && !frame.IsHitTestVisible, "Caller binding did not update behind the modal shield.");
            var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root); Guard(); Require(bitmap.PixelWidth == 800 && bitmap.PixelHeight == 600, "Package popup capture was rescaled.");
            var buffer = await bitmap.GetPixelsAsync(); var pixels = new byte[buffer.Length]; using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
            using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream(); var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream); encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied, 800, 600, 96, 96, pixels); await encoder.FlushAsync();
            using var pngReader = new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0)); await pngReader.LoadAsync((uint)stream.Size); var bytes = new byte[stream.Size]; pngReader.ReadBytes(bytes); var file = "package-confirmation-" + scheme.ToLowerInvariant() + ".png"; await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, file), bytes); captures.Add(new { file, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)), width = 800, height = 600 });
            confirmation.Cancel(); await Task.Delay(180); await Layout(); Require(!confirmation.IsOpen && frame.IsHitTestVisible && popover.IsOpen && editor.Text == "Edited fictional package value", "Package cancellation lost original popup presentation or edit.");
            cases.Add(new { scheme, name = "native-confirmation-and-binding-shield", passed = true, evidence = new { nativeInvokes = 1, modalShielded = true, enabledBindingStayedLive = true, originalEditRetained = true, cancellationRestoredPopup = true } });
            open.IsOn = false; await Layout(); Require(!editor.IsLoaded && !popover.IsOpen, "Caller close retained its package portal."); open.IsOn = true; await Layout(); Require(editor.IsLoaded && editor.Text == "Edited fictional package value", "Caller reopen replaced its package input."); root.Children.Remove(host); await Layout(); Require(!editor.IsLoaded && popover.IsOpen, "Package host unload retained a portal or rewrote Open."); root.Children.Add(host); await Layout(); Require(editor.IsLoaded && editor.Text == "Edited fictional package value" && popover.GetBindingExpression(BleeckerPopover.OpenProperty) is not null, "Package host remount lost original content/binding.");
            cases.Add(new { scheme, name = "caller-close-and-root-remount", passed = true, evidence = new { closeReleasedPortal = true, reopenRetainedEdit = true, unloadReleasedPortal = true, remountRetainedBinding = true } });
            root.Children.Clear(); await Layout();
        }
        string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-package-popover-modal-root-v1", passed = true, cases, captures, guards, focus, activations, softwareNativeInvokes = invokes, publicApisOnly = true, librarySha256 = Hash(typeof(BleeckerPopover).Assembly.Location), hostSha256 = Hash(typeof(App).Assembly.Location), architecture = RuntimeInformation.ProcessArchitecture.ToString(), activateCalled = false, showCalled = false, focusCalled = false, hardwareInputSent = false, sourceRasterParityVerified = false, physicalInputVerified = false }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(0);
    }
}
