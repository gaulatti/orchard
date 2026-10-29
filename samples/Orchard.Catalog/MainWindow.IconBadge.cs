using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Path = System.IO.Path;

namespace Orchard.Catalog;
public sealed partial class MainWindow
{
    private async Task RunIconBadgeAsync(string folder, bool milestoneOnly = false)
    {
        using var stream = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.IconBadge.json") ?? throw new InvalidOperationException("IconBadge reference missing.");
        using var document = await JsonDocument.ParseAsync(stream);
        var original = Content; var canvas = new Canvas { UseLayoutRounding = false }; var frames = new List<object>(); var lifecycle = new List<object>();
        try
        {
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(IconBadges);
                await IconBadges.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark);
            }
            if (milestoneOnly)
                await File.WriteAllTextAsync(Path.Combine(folder, "icon-badge-states.json"),
                    JsonSerializer.Serialize(new { passed = true, themes = new[] { "Light", "Dark" },
                        sizeVariantCases = 6, emptyScalarNamedAndInteractiveFixture = true }));
            Content = canvas; await AwaitPresentationAsync(canvas);
            using var input = new NativeInputSession(canvas.XamlRoot, folder, "icon-badge"); await input.AcquireAsync(canvas);
            object Box(FrameworkElement element) { var p = element.TransformToVisual(canvas).TransformPoint(new Point()); return new { x = p.X, y = p.Y, width = element.ActualWidth, height = element.ActualHeight }; }
            object Paint(Brush value) { var brush = (SolidColorBrush)value; return new { r = brush.Color.R, g = brush.Color.G, b = brush.Color.B, a = brush.Color.A, opacity = brush.Opacity }; }
            var outside = new Border { Width = 10, Height = 10, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) }; Canvas.SetLeft(outside, 280); Canvas.SetTop(outside, 180);
            if (!milestoneOnly) foreach (var reference in document.RootElement.GetProperty("cases").EnumerateArray())
            {
                var theme = Enum.Parse<ElementTheme>(reference.GetProperty("scheme").GetString()!); canvas.RequestedTheme = theme;
                canvas.Background = new SolidColorBrush(theme == ElementTheme.Light ? Microsoft.UI.Colors.White : Windows.UI.Color.FromArgb(255, 13, 24, 33));
                var spec = reference.GetProperty("spec"); var badge = new BleeckerIconBadge();
                if (spec.TryGetProperty("size", out var size)) badge.Size = size.GetString() == "md" ? BleeckerIconBadgeSize.Medium : BleeckerIconBadgeSize.Large;
                if (spec.TryGetProperty("variant", out var variant)) badge.Variant = Enum.Parse<BleeckerIconBadgeVariant>(variant.GetString()!, true);
                if (spec.TryGetProperty("parentFlex", out _)) badge.MaxWidth = 40;
                if (spec.TryGetProperty("named", out _)) AutomationProperties.SetName(badge, "Consumer icon marker");
                FrameworkElement? child = null;
                var kind = spec.TryGetProperty("child", out var childSpec) ? childSpec.GetString() : "vector";
                var value = spec.TryGetProperty("value", out var valueSpec) ? valueSpec.GetString()! : "";
                switch (kind)
                {
                    case "empty": break;
                    case "text": badge.Content = value; break;
                    case "number": badge.Content = double.Parse(value, CultureInfo.InvariantCulture); break;
                    case "bigint": badge.Content = BigInteger.Parse(value, CultureInfo.InvariantCulture); break;
                    case "boolean": badge.Content = value == "true"; break;
                    case "large":
                        child = new Border { Width = 100, Height = 80, UseLayoutRounding = false };
                        child.SetBinding(Border.BackgroundProperty, new Binding { Source = badge, Path = new PropertyPath("Foreground") }); badge.Content = child; break;
                    case "rich":
                        var row = new StackPanel { Orientation = Orientation.Horizontal }; row.Children.Add(new Button { Content = "A" }); row.Children.Add(new TextBox { Text = "Harbor", Width = 40 }); child = row; badge.Content = child; break;
                    default:
                        child = IconBadgesFixture.Vector(badge); badge.Content = child;
                        if (spec.TryGetProperty("childColor", out _)) ((Rectangle)((Grid)child).Children[0]).Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 180, 55, 90));
                        break;
                }
                if (spec.TryGetProperty("style", out _))
                {
                    badge.Width = 88; badge.Height = 40; badge.BorderThickness = new(2); badge.Padding = new(4); badge.CornerRadius = new(6);
                    badge.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 180, 55, 90));
                    badge.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 237, 220));
                    badge.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 33, 67, 99));
                }
                canvas.Children.Clear(); FrameworkElement mounted = badge;
                if (spec.TryGetProperty("parentWidth", out var parentWidth))
                {
                    var block = new Canvas { Width = parentWidth.GetDouble(), Height = 64, UseLayoutRounding = false };
                    block.Children.Add(badge); mounted = block;
                }
                Canvas.SetLeft(mounted, 20); Canvas.SetTop(mounted, 20); canvas.Children.Add(mounted); canvas.Children.Add(outside);
                await AwaitPresentationAsync(badge); await input.MoveAsync(outside); await AwaitPresentationAsync(badge);
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(badge);
                var text = Desc<TextBlock>(badge).FirstOrDefault(block => block.Visibility == Visibility.Visible);
                frames.Add(new { id = reference.GetProperty("id").GetString(), scheme = theme.ToString(), motion = reference.GetProperty("motion").GetString(),
                    measurement = new { box = Box(badge), child = child is null ? null : Box(child),
                        childForeground = child switch { Grid vector => Paint(((Rectangle)vector.Children[0]).Stroke), Border block => Paint(block.Background), _ => null },
                        text = text?.Text.Replace('\n', ' ') ?? "", textBox = text is null ? null : Box(text),
                        background = Paint(badge.Background), border = Paint(badge.BorderBrush), foreground = Paint(badge.Foreground), borderWidth = badge.BorderThickness.Left, radius = badge.CornerRadius.TopLeft,
                        badge.IsTabStop, name = peer.GetName(), role = peer.GetAutomationControlType().ToString(), hasInvoke = peer.GetPattern(PatternInterface.Invoke) is not null } });
                var capturePath = Path.Combine(folder, reference.GetProperty("file").GetString()!);
                if (kind == "large")
                {
                    // This reference legitimately has only one/two flat colors.
                    // Require every composed pixel to match its frozen PNG.
                    using var frozen = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.IconBadgeLarge" + theme + ".png")!;
                    using var bytes = new MemoryStream(); await frozen.CopyToAsync(bytes);
                    Require(Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes.ToArray())) == reference.GetProperty("pngSha256").GetString(), "Large IconBadge frozen frame changed.");
                    using var random = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                    using (var writer = new Windows.Storage.Streams.DataWriter(random)) { writer.WriteBytes(bytes.ToArray()); await writer.StoreAsync(); writer.DetachStream(); }
                    random.Seek(0); var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(random);
                    var expected = (await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Straight, new Windows.Graphics.Imaging.BitmapTransform(), Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation, Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
                    await NativeScreenCapture.CaptureExactClientAsync(input.Window, capturePath, expected);
                }
                else await NativeScreenCapture.CaptureClientAsync(input.Window, capturePath);
            }
            var reportName = milestoneOnly ? "icon-badge-milestone.json" : "icon-badge.json";
            await File.WriteAllTextAsync(Path.Combine(folder, reportName), JsonSerializer.Serialize(new { passed = false, milestoneOnly, frames, lifecycle }, new JsonSerializerOptions { WriteIndented = true }));
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                canvas.RequestedTheme = theme; canvas.Children.Clear();
                var child = new StackPanel { Orientation = Orientation.Horizontal };
                var button = new Button { Content = "A", Width = 36, MinWidth = 0, MinHeight = 0, Height = 32, Padding = new(0) };
                var editor = new TextBox { Text = "Harbor", Width = 80, MinWidth = 0, MinHeight = 0, Height = 32 };
                AutomationProperties.SetName(button, "Child action"); AutomationProperties.SetName(editor, "Child value");
                child.Children.Add(button); child.Children.Add(editor);
                var badge = new BleeckerIconBadge { Content = child, Width = 140 }; Canvas.SetLeft(badge, 40); Canvas.SetTop(badge, 40); canvas.Children.Add(badge); canvas.Children.Add(outside);
                var clicks = 0; var bubbled = 0; button.Click += (_, _) => clicks++; badge.AddHandler(UIElement.PointerPressedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler((_, _) => bubbled++), true);
                await AwaitPresentationAsync(badge); await input.ClickAsync(button);
                await input.HeldKeyAsync(0x09, button, () => editor, () => AwaitPresentationAsync(badge));
                Require(ReferenceEquals(FocusManager.GetFocusedElement(canvas.XamlRoot), editor), "Passive badge interrupted child Tab order.");
                editor.Text = "Retained";
                foreach (var (size, variant) in new[] { (BleeckerIconBadgeSize.Medium, BleeckerIconBadgeVariant.Outlined), (BleeckerIconBadgeSize.Large, BleeckerIconBadgeVariant.Subtle), (BleeckerIconBadgeSize.Large, BleeckerIconBadgeVariant.Primary) })
                {
                    badge.Size = size; badge.Variant = variant; await AwaitPresentationAsync(badge);
                    Require(ReferenceEquals(badge.Content, child) && editor.Text == "Retained" && ReferenceEquals(FocusManager.GetFocusedElement(canvas.XamlRoot), editor), "Live badge property update replaced or unfocused consumer content.");
                    Require(badge.Width == 140 && badge.Style is null, "Badge update replaced a consumer size or style.");
                    lifecycle.Add(new { scheme = theme.ToString(), phase = "live", size = size.ToString(), variant = variant.ToString(), retained = true });
                }
                var template = badge.Template; badge.Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:o='using:Orchard' TargetType='o:BleeckerIconBadge'><Grid><Border Background='{TemplateBinding Background}'/><Grid x:Name='PART_ContentHost'/></Grid></ControlTemplate>");
                badge.ApplyTemplate(); await AwaitPresentationAsync(badge); Require(ReferenceEquals(badge.Content, child) && editor.Text == "Retained", "Retemplate replaced consumer content.");
                badge.Template = template; badge.ApplyTemplate(); await AwaitPresentationAsync(badge);
                canvas.Children.Remove(badge); canvas.Children.Add(badge); await AwaitPresentationAsync(badge);
                await input.ClickAsync(button); Require(clicks == 2 && bubbled == 2, "Native child input or bubbled pointer routing changed.");
                var style = new Style(typeof(BleeckerIconBadge)); var styledColor = new SolidColorBrush(Microsoft.UI.Colors.Green);
                style.Setters.Add(new Setter(Control.ForegroundProperty, styledColor)); style.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(8)));
                badge.Style = style; badge.Size = BleeckerIconBadgeSize.Medium; badge.Variant = BleeckerIconBadgeVariant.Outlined; await AwaitPresentationAsync(badge);
                Require(ReferenceEquals(badge.Style, style) && ReferenceEquals(badge.Foreground, styledColor) && badge.CornerRadius == new CornerRadius(8), "Badge update replaced consumer style setters.");
                badge.Style = null;
                var foreground = new SolidColorBrush(Microsoft.UI.Colors.Red); badge.Foreground = foreground; badge.Variant = BleeckerIconBadgeVariant.Subtle;
                Require(ReferenceEquals(badge.Foreground, foreground), "Variant replaced consumer foreground.");
                var model = new BadgeModel("Model text"); var selector = new BadgeSelector(); badge.Content = null; badge.ContentTemplateSelector = selector; badge.Content = model;
                await AwaitPresentationAsync(badge); Require(ReferenceEquals(badge.Content, model) && Desc<TextBlock>(badge).Any(block => block.Text == model.Label), "Badge DataTemplateSelector lost its original model.");
                badge.Content = null; badge.ContentTemplateSelector = null; badge.ContentTemplate = selector.Template; badge.Content = model;
                await AwaitPresentationAsync(badge); Require(ReferenceEquals(badge.Content, model) && Desc<TextBlock>(badge).Any(block => block.Text == model.Label), "Badge DataTemplate lost its original model.");
                badge.Content = null; badge.ContentTemplate = null; badge.Content = new TextBlock { Text = "Explicit", Foreground = foreground };
                await AwaitPresentationAsync(badge); badge.Variant = BleeckerIconBadgeVariant.Primary;
                Require(ReferenceEquals(((TextBlock)badge.Content).Foreground, foreground), "Badge replaced explicit child foreground.");
                lifecycle.Add(new { scheme = theme.ToString(), phase = "retemplate-detach-template-selector-colors", retained = true });
                await input.MoveAsync(outside); await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, "catalog-" + theme.ToString().ToLowerInvariant() + ".png"));
            }
            await File.WriteAllTextAsync(Path.Combine(folder, reportName), JsonSerializer.Serialize(new { passed = true, milestoneOnly, frames, lifecycle }, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "icon-badge", milestoneOnly, environment = RuntimeEnvironment() }));
        }
        finally { Content = original; await AwaitPresentationAsync(CatalogRoot); }
    }
    private sealed record BadgeModel(string Label);
    private sealed class BadgeSelector : DataTemplateSelector
    {
        internal DataTemplate Template { get; } = (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding Label}'/></DataTemplate>");
        protected override DataTemplate SelectTemplateCore(object item) => Template;
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => Template;
    }
}
