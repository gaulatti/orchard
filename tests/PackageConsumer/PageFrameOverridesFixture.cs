using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Testing;

internal static class PageFrameOverridesFixture
{
    private delegate bool Visitor(nint handle, nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);

    internal static async Task RunAsync(Window window, string output, string hostAssembly)
    {
        var root = new Canvas { Width = 1000, Height = 800 };
        window.Content = root;
        window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1000, 800));
        var checks = new List<object>();
        var guards = 0; var focus = 0; var activations = 0;
        root.GotFocus += (_, _) => focus++;
        window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var process); visible |= process == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0,
                "PageFrame override check left its background boundary.");
        }
        async Task Layout()
        {
            for (var i = 0; i < 3; i++)
            {
                root.Measure(new Size(1000, 800)); root.Arrange(new Rect(0, 0, 1000, 800)); root.UpdateLayout(); Guard(); await Task.Delay(16);
            }
            await NativeRenderReadiness.RenderAsync(root); Guard();
        }
        void Check(string id, bool passed, object observed)
        {
            Guard(); checks.Add(new { id, passed, observed }); Require(passed, id);
        }
        try
        {
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            foreach (var width in new[] { 280d, 900d })
            {
                root.RequestedTheme = theme; root.FlowDirection = direction;
                var text = new TextBlock { Text = "Caller content retains its original text and wraps as its font grows. A larger font needs more room.", TextWrapping = TextWrapping.Wrap };
                var font = new TextBlock { Tag = 16d };
                var frame = new BleeckerPageFrame { FrameWidth = BleeckerPageFrameWidth.Full, Gutter = BleeckerPageFrameGutter.None,
                    VerticalSpacing = BleeckerPageFrameVerticalSpacing.None, Padding = new Thickness(12), Content = text };
                frame.SetBinding(Control.FontSizeProperty, new Binding { Source = font, Path = new PropertyPath("Tag"), Mode = BindingMode.OneWay });
                var host = new Grid { Width = width }; host.Children.Add(frame); root.Children.Add(host);
                var prefix = $"{theme}/{direction}/{width}"; double previousHeight = 0;
                foreach (var size in new[] { 16d, 24d, 32d })
                {
                    font.Tag = size; await Layout();
                    Check(prefix + $"/font-{size}", frame.FontSize == size && text.FontSize == size && text.ActualHeight > 0 &&
                        frame.ActualHeight >= text.ActualHeight + 24 - .01 && frame.ActualHeight >= previousHeight &&
                        Math.Abs(frame.ActualWidth - width) < .01 && text.ActualWidth <= width - 24 + .01 &&
                        ReferenceEquals(frame.Content, text) && frame.GetBindingExpression(Control.FontSizeProperty) is not null,
                        new { requestedFont = size, renderedFont = text.FontSize, frameWidth = frame.ActualWidth, frameHeight = frame.ActualHeight,
                            textWidth = text.ActualWidth, textHeight = text.ActualHeight, contentRetained = ReferenceEquals(frame.Content, text) });
                    previousHeight = frame.ActualHeight;
                }
                text.FontSize = 11; font.Tag = 40d; await Layout();
                Check(prefix + "/child-font-override", frame.FontSize == 40 && text.FontSize == 11, new { frameFont = frame.FontSize, childFont = text.FontSize });
                text.ClearValue(TextBlock.FontSizeProperty); await Layout();
                Check(prefix + "/restore-inherited-font", text.FontSize == 40 && ReferenceEquals(frame.Content, text), new { childFont = text.FontSize });
                var wide = new Border { Width = 1200, Height = 48 };
                frame.Content = wide; frame.FrameWidth = BleeckerPageFrameWidth.Reading; frame.Padding = new Thickness(20); await Layout();
                Check(prefix + "/oversized-content", wide.ActualWidth == 1200 && frame.ActualWidth == Math.Min(width, 768) &&
                    ReferenceEquals(frame.Content, wide) && frame.ActualHeight >= 88,
                    new { width = frame.ActualWidth, childWidth = wide.ActualWidth, height = frame.ActualHeight });
                foreach (var alignment in new[] { HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right })
                {
                    frame.HorizontalAlignment = alignment; frame.MaxWidth = 200; await Layout();
                    var x = frame.TransformToVisual(host).TransformPoint(new Point()).X;
                    var expected = alignment == HorizontalAlignment.Left ? 0 : alignment == HorizontalAlignment.Right ? width - 200 : (width - 200) / 2;
                    Check(prefix + "/alignment-" + alignment, Math.Abs(frame.ActualWidth - 200) < .01 && Math.Abs(x - expected) < .01 && ReferenceEquals(frame.Content, wide),
                        new { x, expected, width = frame.ActualWidth });
                }
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(frame);
                Check(prefix + "/passive-metadata", !frame.IsTabStop && peer.GetAutomationControlType() == AutomationControlType.Group, new { tabStop = frame.IsTabStop, role = peer.GetAutomationControlType().ToString() });
                root.Children.Remove(host); await Layout();
            }
            Guard();
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-page-frame-overrides-v1", passed = true, checks, guards, focus, activations,
                componentFontOverridesVerified = true, systemTextScalingVerified = false, displayScalingVerified = false, physicalInputVerified = false,
                nativeArm64RuntimeVerified = false, nativeComponentAccepted = false, focusCalled = false, showWindowCalled = false, hardwareInputSent = false,
                osSettingsChanged = false, processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(BleeckerPageFrame).Assembly.Location))),
                hostSha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(hostAssembly))) }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close(); Environment.Exit(0);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-page-frame-overrides-v1", passed = false, error = error.ToString(), checks, guards, focus, activations }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close(); Environment.Exit(1);
        }
    }
}
