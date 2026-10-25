using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
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

                var original = new TextBlock { Text = "Original caller content" };
                var custom = new BleeckerPageFrame { FrameWidth = BleeckerPageFrameWidth.Reading, Gutter = BleeckerPageFrameGutter.None,
                    VerticalSpacing = BleeckerPageFrameVerticalSpacing.None, Content = original };
                var customHost = new Grid { Width = width }; customHost.Children.Add(custom); root.Children.Add(customHost); await Layout();
                var originalStyle = custom.Style; var originalFont = custom.FontSize;
                var style = new Style(typeof(BleeckerPageFrame)) { BasedOn = originalStyle };
                style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(7, 5, 7, 5)));
                style.Setters.Add(new Setter(FrameworkElement.MaxWidthProperty, 180d));
                style.Setters.Add(new Setter(Control.FontSizeProperty, 26d));
                custom.Style = style; custom.FrameWidth = BleeckerPageFrameWidth.Content; await Layout();
                Check(prefix + "/style-recipe-precedence", custom.Padding == new Thickness(7, 5, 7, 5) && custom.MaxWidth == 180 &&
                    custom.ActualWidth == 180 && original.FontSize == 26 && ReferenceEquals(custom.Style, style) && ReferenceEquals(custom.Content, original),
                    new { paddingLeft = custom.Padding.Left, paddingTop = custom.Padding.Top, maximum = custom.MaxWidth,
                        width = custom.ActualWidth, childFont = original.FontSize, styleRetained = ReferenceEquals(custom.Style, style), contentRetained = ReferenceEquals(custom.Content, original) });
                custom.Style = originalStyle; await Layout();
                Check(prefix + "/clear-style-restores-recipe", custom.Padding == new Thickness(0) && custom.MaxWidth == 1152 &&
                    custom.ActualWidth == width && original.FontSize == originalFont && ReferenceEquals(custom.Content, original),
                    new { padding = custom.Padding.Left, maximum = custom.MaxWidth, width = custom.ActualWidth,
                        originalFont, childFont = original.FontSize, contentRetained = ReferenceEquals(custom.Content, original) });
                var originalTemplate = custom.Template;
                var replacement = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:local='using:Orchard' TargetType='local:BleeckerPageFrame'><Border x:Name='CallerFrameBorder' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Padding='{TemplateBinding Padding}'><ContentPresenter Content='{TemplateBinding Content}'/></Border></ControlTemplate>");
                custom.Template = replacement; await Layout();
                var replacementApplied = ReferenceEquals(custom.Template, replacement) && VisualTreeHelper.GetChildrenCount(custom) == 1 &&
                    VisualTreeHelper.GetChild(custom, 0) is Border { Name: "CallerFrameBorder" } && original.IsLoaded && ReferenceEquals(custom.Content, original);
                custom.Template = originalTemplate; await Layout();
                Check(prefix + "/replacement-template", replacementApplied && ReferenceEquals(custom.Template, originalTemplate) &&
                    original.IsLoaded && ReferenceEquals(custom.Content, original),
                    new { replacementApplied, originalTemplateRestored = ReferenceEquals(custom.Template, originalTemplate),
                        originalContentLoaded = original.IsLoaded, contentRetained = ReferenceEquals(custom.Content, original) });
                var metadataStyle = new Style(typeof(BleeckerPageFrame)) { BasedOn = originalStyle };
                metadataStyle.Setters.Add(new Setter(AutomationProperties.NameProperty, "Styled section"));
                metadataStyle.Setters.Add(new Setter(AutomationProperties.LandmarkTypeProperty, AutomationLandmarkType.None));
                metadataStyle.Setters.Add(new Setter(AutomationProperties.LocalizedLandmarkTypeProperty, "Styled landmark"));
                metadataStyle.Setters.Add(new Setter(AutomationProperties.ItemTypeProperty, "styled-item"));
                custom.Element = BleeckerPageFrameElement.Section; custom.Style = metadataStyle; await Layout();
                var customPeer = FrameworkElementAutomationPeer.CreatePeerForElement(custom);
                Check(prefix + "/style-metadata", customPeer.GetName() == "Styled section" && customPeer.GetLandmarkType() == AutomationLandmarkType.None &&
                    customPeer.GetLocalizedLandmarkType() == "Styled landmark" && customPeer.GetItemType() == "styled-item",
                    new { name = customPeer.GetName(), landmark = customPeer.GetLandmarkType().ToString(), localized = customPeer.GetLocalizedLandmarkType(), itemType = customPeer.GetItemType() });
                var nameProducer = new TextBlock { Tag = "First local name" };
                custom.SetBinding(AutomationProperties.NameProperty, new Binding { Source = nameProducer, Path = new PropertyPath("Tag"), Mode = BindingMode.OneWay });
                AutomationProperties.SetLandmarkType(custom, AutomationLandmarkType.Custom);
                AutomationProperties.SetLocalizedLandmarkType(custom, "Local landmark"); AutomationProperties.SetItemType(custom, "local-item");
                await Layout(); var initialBindingName = customPeer.GetName();
                nameProducer.Tag = "Updated local name"; custom.Element = BleeckerPageFrameElement.Main; await Layout();
                Check(prefix + "/live-metadata-precedence", initialBindingName == "First local name" && customPeer.GetName() == "Updated local name" &&
                    customPeer.GetLandmarkType() == AutomationLandmarkType.Custom && customPeer.GetLocalizedLandmarkType() == "Local landmark" &&
                    customPeer.GetItemType() == "local-item" && custom.GetBindingExpression(AutomationProperties.NameProperty) is not null,
                    new { initialBindingName, name = customPeer.GetName(), landmark = customPeer.GetLandmarkType().ToString(),
                        localized = customPeer.GetLocalizedLandmarkType(), itemType = customPeer.GetItemType(), bindingRetained = custom.GetBindingExpression(AutomationProperties.NameProperty) is not null });
                foreach (var property in new[] { AutomationProperties.NameProperty, AutomationProperties.LandmarkTypeProperty,
                    AutomationProperties.LocalizedLandmarkTypeProperty, AutomationProperties.ItemTypeProperty }) custom.ClearValue(property);
                await Layout();
                var restoredStyleMetadata = customPeer.GetName() == "Styled section" && customPeer.GetLandmarkType() == AutomationLandmarkType.None &&
                    customPeer.GetLocalizedLandmarkType() == "Styled landmark" && customPeer.GetItemType() == "styled-item";
                custom.Style = originalStyle; custom.Element = BleeckerPageFrameElement.Section; AutomationProperties.SetName(custom, "Restored section"); await Layout();
                Check(prefix + "/clear-metadata-restores-source", restoredStyleMetadata && customPeer.GetName() == "Restored section" &&
                    customPeer.GetLandmarkType() == AutomationLandmarkType.Custom && customPeer.GetLocalizedLandmarkType() == "region" && customPeer.GetItemType() == "section",
                    new { restoredStyleMetadata, name = customPeer.GetName(), landmark = customPeer.GetLandmarkType().ToString(),
                        localized = customPeer.GetLocalizedLandmarkType(), itemType = customPeer.GetItemType() });
                root.Children.Remove(customHost); await Layout();
            }
            Guard();
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-page-frame-overrides-v2", passed = true, checks, guards, focus, activations,
                callerStylesVerified = true, replacementTemplateVerified = true, callerMetadataVerified = true,
                componentFontOverridesVerified = true, systemTextScalingVerified = false, displayScalingVerified = false, physicalInputVerified = false,
                nativeArm64RuntimeVerified = false, nativeComponentAccepted = false, focusCalled = false, showWindowCalled = false, hardwareInputSent = false,
                osSettingsChanged = false, processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(BleeckerPageFrame).Assembly.Location))),
                hostSha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(hostAssembly))) }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close(); Environment.Exit(0);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-page-frame-overrides-v2", passed = false, error = error.ToString(), checks, guards, focus, activations }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close(); Environment.Exit(1);
        }
    }
}
