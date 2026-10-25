using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunThemeTogglesAsync(string folder)
    {
        using var stream = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.ThemeToggle.json") ?? throw new InvalidOperationException("Theme Toggle source reference missing.");
        using var reference = await JsonDocument.ParseAsync(stream);
        var previous = Content; var originalSize = AppWindow.ClientSize;
        var canvas = (Canvas)Microsoft.UI.Xaml.Markup.XamlReader.Load("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' UseLayoutRounding='False'/>");
        var anchor = new Button { Width = 10, Height = 10, Opacity = 0 }; Canvas.SetLeft(anchor, 210); Canvas.SetTop(anchor, 130); canvas.Children.Add(anchor);
        var frames = new List<object>(); var interactions = new List<object>(); var captures = new Dictionary<string, string>(); var rasterMatches = new Dictionary<string, bool>();
        object Box(FrameworkElement node) { var box = node.TransformToVisual(canvas).TransformBounds(new Rect(0, 0, node.ActualWidth, node.ActualHeight)); return new { x = box.X, y = box.Y, width = box.Width, height = box.Height }; }
        void Mount(BleeckerThemeToggle toggle) { Canvas.SetLeft(toggle, 24); Canvas.SetTop(toggle, 24); canvas.Children.Add(toggle); }
        static string Label(BleeckerThemeMode mode) => mode switch { BleeckerThemeMode.Light => "Switch to dark theme", BleeckerThemeMode.Dark => "Switch to system theme", _ => "Switch to light theme" };
        try
        {
            Content = canvas; AppWindow.ResizeClient(new SizeInt32(240, 160)); await AwaitPresentationAsync(canvas);
            using var input = new NativeInputSession(canvas.XamlRoot, folder, "theme-toggle"); await input.AcquireAsync(canvas); AppWindow.ResizeClient(new SizeInt32(240, 160)); await AwaitPresentationAsync(canvas);
            foreach (var row in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                canvas.RequestedTheme = Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!);
                var spec = row.GetProperty("spec"); var toggle = new BleeckerThemeToggle();
                if (spec.TryGetProperty("mode", out var mode)) toggle.Mode = Enum.Parse<BleeckerThemeMode>(mode.GetString()!, true);
                if (spec.TryGetProperty("size", out var size)) toggle.Size = size.GetString() switch { "sm" => BleeckerIconButtonSize.Small, "lg" => BleeckerIconButtonSize.Large, _ => BleeckerIconButtonSize.Medium };
                if (spec.TryGetProperty("variant", out var variant)) toggle.Variant = Enum.Parse<BleeckerIconButtonVariant>(variant.GetString()!, true);
                if (spec.TryGetProperty("disabled", out var disabled)) toggle.IsEnabled = !disabled.GetBoolean();
                if (spec.TryGetProperty("rtl", out var rtl) && rtl.GetBoolean()) toggle.FlowDirection = FlowDirection.RightToLeft;
                if (spec.TryGetProperty("name", out var name)) AutomationProperties.SetName(toggle, name.GetString()!);
                if (spec.TryGetProperty("title", out var title)) ToolTipService.SetToolTip(toggle, title.GetString());
                Mount(toggle); await AwaitPresentationAsync(toggle); await input.MoveAsync(anchor);
                var state = spec.GetProperty("state").GetString();
                if (state is "hover" or "pressed") await input.MoveAsync(toggle);
                if (state == "focus") Require(toggle.Focus(FocusState.Keyboard), "Theme Toggle did not receive keyboard focus.");
                if (state == "pressed") await input.PressPointerAsync();
                try
                {
                    await AwaitPresentationAsync(toggle); var peer = FrameworkElementAutomationPeer.CreatePeerForElement(toggle);
                    var icon = Desc<ContentControl>(toggle).Single(node => node.Name == "ThemeIcon"); var vector = Desc<Viewbox>(icon).Single(); var shapes = Desc<Microsoft.UI.Xaml.Shapes.Shape>(icon).ToArray();
                    var stroke = (SolidColorBrush)shapes[0].Stroke; var tip = ToolTipService.GetToolTip(toggle);
                    frames.Add(new { id = row.GetProperty("id").GetString(), scheme = row.GetProperty("scheme").GetString(), measurement = new { mode = toggle.Mode.ToString().ToLowerInvariant(), label = peer.GetName(), title = tip is ToolTip toolTip ? toolTip.Content : tip, disabled = !toggle.IsEnabled, box = Box(toggle), icon = Box(vector), strokeRgba = new[] { (int)stroke.Color.R, stroke.Color.G, stroke.Color.B, stroke.Color.A }, strokeWidth = shapes[0].StrokeThickness, iconOpacity = icon.Opacity, role = peer.GetAutomationControlType().ToString(), tabStop = toggle.IsTabStop, focused = ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(canvas.XamlRoot), toggle) } });
                    using var png = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.ThemeToggle." + row.GetProperty("file").GetString()) ?? throw new InvalidOperationException("Theme Toggle PNG missing.");
                    using var bytes = new MemoryStream(); await png.CopyToAsync(bytes); Require(Convert.ToHexStringLower(SHA256.HashData(bytes.ToArray())) == row.GetProperty("pngSha256").GetString(), "Theme Toggle source PNG hash differs.");
                    using var random = new Windows.Storage.Streams.InMemoryRandomAccessStream(); using (var writer = new Windows.Storage.Streams.DataWriter(random)) { writer.WriteBytes(bytes.ToArray()); await writer.StoreAsync(); writer.DetachStream(); } random.Seek(0);
                    var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(random); var pixels = (await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Straight, new Windows.Graphics.Imaging.BitmapTransform(), Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation, Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
                    var file = row.GetProperty("file").GetString()!; try { await NativeScreenCapture.CaptureExactClientAsync(input.Window, Path.Combine(folder, file), pixels); rasterMatches[file] = true; } catch (InvalidOperationException error) when (error.Message == "The composed capture differs from its complete exact reference frame.") { rasterMatches[file] = false; }
                    captures[file] = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(folder, file))));
                }
                finally { if (state == "pressed") input.PointerButton(false); canvas.Children.Remove(toggle); }
            }
            foreach (var scheme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                canvas.RequestedTheme = scheme;
                var producer = new TextBlock { Tag = BleeckerThemeMode.System }; var toggle = new BleeckerThemeToggle();
                toggle.SetBinding(BleeckerThemeToggle.ModeProperty, new Binding { Source = producer, Path = new PropertyPath("Tag"), Mode = BindingMode.OneWay });
                var requests = new List<BleeckerThemeMode>(); var accept = true; toggle.ThemeRequested += (_, args) => { requests.Add(args.Mode); if (accept) producer.Tag = args.Mode; };
                Mount(toggle); await AwaitPresentationAsync(toggle);
                await input.ClickAsync(toggle); await AwaitPresentationAsync(toggle); Require(toggle.Mode == BleeckerThemeMode.Light, "Theme Toggle pointer request failed.");
                Require(toggle.Focus(FocusState.Keyboard), "Theme Toggle could not focus."); await input.KeyAsync(0x20, toggle); await AwaitPresentationAsync(toggle); await input.KeyAsync(0x0D, toggle); await AwaitPresentationAsync(toggle);
                Require(requests.SequenceEqual(new[] { BleeckerThemeMode.Light, BleeckerThemeMode.Dark, BleeckerThemeMode.System }) && toggle.Mode == BleeckerThemeMode.System, "Theme Toggle physical cycle differs.");
                accept = false; var peer = FrameworkElementAutomationPeer.CreatePeerForElement(toggle); var provider = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke); provider.Invoke(); await AwaitPresentationAsync(toggle);
                Require(requests.Count == 4 && requests[^1] == BleeckerThemeMode.Light && toggle.Mode == BleeckerThemeMode.System && toggle.GetBindingExpression(BleeckerThemeToggle.ModeProperty) is not null, "Theme Toggle mutated the rejected consumer mode.");
                producer.Tag = BleeckerThemeMode.Dark; await AwaitPresentationAsync(toggle); Require(peer.GetName() == Label(BleeckerThemeMode.Dark) && ((ToolTip)ToolTipService.GetToolTip(toggle)).Content as string == Label(BleeckerThemeMode.Dark), "Theme Toggle live metadata did not update.");
                AutomationProperties.SetName(toggle, "Caller — 東京"); ToolTipService.SetToolTip(toggle, "Caller tooltip"); producer.Tag = BleeckerThemeMode.Light; await AwaitPresentationAsync(toggle);
                Require(peer.GetName() == "Caller — 東京" && ToolTipService.GetToolTip(toggle) as string == "Caller tooltip", "Theme Toggle replaced caller metadata.");
                AutomationProperties.SetName(toggle, ""); Require(peer.GetName() == "", "Theme Toggle ignored an explicit empty name.");
                toggle.ClearValue(AutomationProperties.NameProperty); toggle.ClearValue(ToolTipService.ToolTipProperty);
                var originalStyle = toggle.Style; var metadataBase = new Style(typeof(BleeckerThemeToggle)) { BasedOn = originalStyle };
                metadataBase.Setters.Add(new Setter(AutomationProperties.NameProperty, "")); metadataBase.Setters.Add(new Setter(ToolTipService.ToolTipProperty, "Styled tooltip"));
                toggle.Style = new Style(typeof(BleeckerThemeToggle)) { BasedOn = metadataBase }; producer.Tag = BleeckerThemeMode.Dark; await AwaitPresentationAsync(toggle);
                Require(peer.GetName() == "" && ToolTipService.GetToolTip(toggle) as string == "Styled tooltip" && requests.Count == 4, "Theme Toggle replaced BasedOn metadata or emitted a programmatic request.");
                toggle.Style = originalStyle; await AwaitPresentationAsync(toggle); Require(peer.GetName() == Label(BleeckerThemeMode.Dark) && ((ToolTip)ToolTipService.GetToolTip(toggle)).Content as string == Label(BleeckerThemeMode.Dark), "Theme Toggle did not restore default metadata after style removal."); AutomationProperties.SetName(toggle, "");
                toggle.IsEnabled = false; var disabledRejected = false; try { provider.Invoke(); } catch (ElementNotEnabledException) { disabledRejected = true; } Require(disabledRejected, "Disabled Theme Toggle remained actionable."); toggle.IsEnabled = true;
                var template = toggle.Template; toggle.Template = null; await AwaitPresentationAsync(canvas); toggle.Template = template; await AwaitPresentationAsync(toggle); producer.Tag = BleeckerThemeMode.System; await AwaitPresentationAsync(toggle); Require(toggle.Mode == BleeckerThemeMode.System && toggle.GetBindingExpression(BleeckerThemeToggle.ModeProperty) is not null, "Theme Toggle template change lost binding.");
                canvas.Children.Remove(toggle); await AwaitPresentationAsync(canvas); var staleRejected = false; try { provider.Invoke(); } catch (ElementNotAvailableException) { staleRejected = true; } Require(staleRejected, "Detached Theme Toggle remained actionable.");
                Mount(toggle); await AwaitPresentationAsync(toggle); producer.Tag = BleeckerThemeMode.Dark; await AwaitPresentationAsync(toggle); Require(toggle.Mode == BleeckerThemeMode.Dark && peer.GetName() == "", "Theme Toggle remount lost binding or explicit metadata."); canvas.Children.Remove(toggle);
                interactions.Add(new { scheme = scheme.ToString(), passed = true, requests = requests.Select(mode => mode.ToString()).ToArray(), bindingRetained = true, disabledRejected, staleRejected, metadataRetained = true, styleMetadataRetained = true, templateReplacement = true, remount = true });
            }
            await File.WriteAllTextAsync(Path.Combine(folder, "theme-toggle.json"), JsonSerializer.Serialize(new { passed = true, frames, interactions, captures, rasterMatches, sourceRasterParityVerified = rasterMatches.Values.All(value => value) }, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "theme-toggle", environment = RuntimeEnvironment() }));
        }
        finally { Content = previous; AppWindow.ResizeClient(originalSize); await AwaitPresentationAsync(CatalogRoot); }
    }
}
