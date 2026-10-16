using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunLoadingOverlayLifecycleAsync(string folder)
    {
        var originalContent = Content; var originalTheme = CatalogRoot.RequestedTheme;
        var windowSize = AppWindow.ClientSize; var windowPosition = AppWindow.Position;
        var page = new Page { RequestedTheme = ElementTheme.Light };
        var host = new Canvas { Background = new SolidColorBrush(Microsoft.UI.Colors.White) }; page.Content = host;
        var button = new Button { Name = "LifecycleAction", Content = "Original action", Width = 140, Height = 32 };
        var activations = 0; button.Click += (_, _) => activations++;
        Canvas.SetLeft(button, 20); Canvas.SetTop(button, 20); host.Children.Add(button);
        var anchor = new Grid { Width = 100, Height = 40, RenderTransform = new TranslateTransform { X = 31.25, Y = 27.5 }, Clip = new RectangleGeometry { Rect = new Rect(0, 0, 100, 40) } };
        Canvas.SetLeft(anchor, 70); Canvas.SetTop(anchor, 120); host.Children.Add(anchor);
        var alternate = new Grid { Width = 80, Height = 30 }; Canvas.SetLeft(alternate, 230.5); Canvas.SetTop(alternate, 90.25); host.Children.Add(alternate);
        var overlay = new BleeckerLoadingOverlay { Label = "Caller-owned loading" }; anchor.Children.Add(overlay);
        var reports = new List<object>(); var phase = "mount"; var passed = false;
        var focusEvents = new List<object>();
        page.GotFocus += (_, args) => ObserveFocus("got", args.OriginalSource);
        page.LostFocus += (_, args) => ObserveFocus("lost", args.OriginalSource);
        NativeInputSession? input = null;
        try
        {
            Content = page; await AwaitPresentationAsync(page);
            input = new NativeInputSession(page.XamlRoot, folder, "loading-overlay-lifecycle"); await input.AcquireAsync(button);
            Require(button.Focus(FocusState.Keyboard), "Loading overlay lifecycle could not focus the original action.");
            overlay.IsVisible = true; await SettleAsync();
            var first = Portal(); CheckViewport(first); CheckFocus(button); Record("page-root");

            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark, ElementTheme.Light })
            {
                phase = "live-theme-" + theme; page.RequestedTheme = theme; await SettleAsync();
                Require(ReferenceEquals(Portal(), first), "Live theme recreated the loading portal.");
                var surface = Part<BleeckerOverlaySurface>(first, "PART_Card"); var tint = Part<Border>(first, "PART_Tint");
                var expectedCard = theme == ElementTheme.Light ? "#FFFFFFFF" : "#FF182533";
                var expectedTint = theme == ElementTheme.Light ? "#FFFFFFFF" : "#FF0D1821";
                Require(surface.ActualTheme == theme && ColorHex(((SolidColorBrush)surface.Background).Color) == expectedCard && ColorHex(((SolidColorBrush)tint.Background).Color) == expectedTint && Math.Abs(tint.Background.Opacity - .72) < .001,
                    "An open loading portal did not follow its owner's live palette.");
                CheckViewport(first); CheckFocus(button);
                await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png")); Record(phase);
            }

            phase = "consumer-brushes";
            var foreground = new SolidColorBrush(Microsoft.UI.Colors.Purple); var background = new SolidColorBrush(Microsoft.UI.Colors.Beige);
            var outline = new SolidColorBrush(Microsoft.UI.Colors.Green); var tintOverride = new SolidColorBrush(Microsoft.UI.Colors.LightBlue) { Opacity = .45 };
            overlay.Foreground = foreground; overlay.Background = background; overlay.BorderBrush = outline; overlay.OverlayBrush = tintOverride;
            overlay.Padding = new Thickness(31, 19, 23, 17); overlay.BorderThickness = new Thickness(2, 3, 4, 5); overlay.CornerRadius = new CornerRadius(6, 8, 10, 12);
            await SettleAsync(); var card = Part<BleeckerOverlaySurface>(first, "PART_Card"); var label = Part<TextBlock>(first, "PART_Label");
            Require(ReferenceEquals(card.Background, background) && ReferenceEquals(card.BorderBrush, outline) && ReferenceEquals(label.Foreground, foreground) && ReferenceEquals(Part<Border>(first, "PART_Tint").Background, tintOverride), "Loading portal replaced consumer brush instances.");
            Require(card.Padding == overlay.Padding && card.BorderThickness == overlay.BorderThickness && card.CornerRadius == overlay.CornerRadius, "Loading portal ignored consumer geometry.");
            foreground.Color = Microsoft.UI.Colors.DarkOrange; background.Color = Microsoft.UI.Colors.Azure; tintOverride.Opacity = .35; await SettleAsync();
            Require(ColorHex(((SolidColorBrush)label.Foreground).Color) == ColorHex(foreground.Color) && Math.Abs(Part<Border>(first, "PART_Tint").Background.Opacity - .35) < .001, "Loading portal ignored live brush mutation.");
            CheckFocus(button); Record(phase);

            foreach (var width in new[] { 320, 1024, 640 })
            {
                phase = "resize-" + width; AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(width, width == 320 ? 240 : 480)); await SettleAsync();
                Require(ReferenceEquals(Portal(), first), "Window resize recreated the loading portal."); CheckViewport(first); CheckFocus(button); Record(phase);
            }

            phase = "immediate-reparent"; anchor.Children.Remove(overlay); alternate.Children.Add(overlay); await SettleAsync();
            CheckViewport(Portal()); CheckFocus(button); Record(phase);

            phase = "template-remove"; var template = overlay.Template; overlay.Template = null; overlay.ApplyTemplate(); await SettleAsync();
            Require(Portals().Length == 0 && !first.IsLoaded, "Template removal retained an old loading portal."); Record(phase);
            phase = "template-restore"; overlay.Template = template; overlay.ApplyTemplate(); await SettleAsync();
            var restored = Portal(); Require(!ReferenceEquals(restored, first), "Template restore reused the discarded loading portal."); CheckViewport(restored); CheckFocus(button); Record(phase);

            phase = "unload"; alternate.Children.Remove(overlay); await SettleAsync();
            Require(!overlay.IsLoaded && Portals().Length == 0 && !restored.IsLoaded, "Detachment retained a loading portal."); Record(phase);
            phase = "remount"; anchor.Children.Add(overlay); await SettleAsync(); CheckViewport(Portal()); CheckFocus(button); Record(phase);

            phase = "stack"; var second = new BleeckerLoadingOverlay { IsVisible = true, Label = "Second caller" }; alternate.Children.Add(second); await SettleAsync();
            Require(Portals().Length == 2, "Independent loading portals did not stack.");
            overlay.IsVisible = false; await SettleAsync(); Require(Portals().Length == 1 && second.IsVisible, "Hiding one caller removed another loading portal.");
            var originalPageTabStop = page.IsTabStop;
            ObserveFocus("before-pointer", page);
            var beforePointer = activations;
            await input.ClickAsync(Portal(), localPoint: new Point(90, 36)); Require(activations == beforePointer, "The surviving loading portal did not block the underlying action.");
            Require(ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), page), "Pointer clearing did not focus the Page root.");
            ObserveFocus("after-pointer", page);
            second.IsVisible = false; await SettleAsync();
            Require(Portals().Length == 0 && ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), page), "Hiding a pointer-focused portal moved root focus.");
            ObserveFocus("after-hide", page);
            input.Key(0x09); await SettleAsync(); CheckFocus(button); ObserveFocus("after-tab", page); Require(page.IsTabStop == originalPageTabStop, "Page root did not restore its original tab stop after real focus moved."); Record(phase);
            alternate.Children.Remove(second); passed = true;
        }
        finally
        {
            overlay.IsVisible = false; input?.Dispose(); Content = originalContent; CatalogRoot.RequestedTheme = originalTheme;
            AppWindow.ResizeClient(windowSize); AppWindow.Move(windowPosition); await AwaitPresentationAsync(CatalogRoot);
            await File.WriteAllTextAsync(Path.Combine(folder, "loading-overlay-lifecycle.json"), JsonSerializer.Serialize(new { passed, phase, reports, focusEvents, originalContentRestored = ReferenceEquals(Content, originalContent) }, new JsonSerializerOptions { WriteIndented = true }));
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "loading-overlay-lifecycle", environment = RuntimeEnvironment() }));
        Task SettleAsync() { page.UpdateLayout(); return AwaitPresentationAsync(page); }
        void ObserveFocus(string kind, object original)
        {
            if (page.XamlRoot is null) return;
            var local = page.ReadLocalValue(UIElement.IsTabStopProperty);
            focusEvents.Add(new { kind, original = original.GetType().Name, focused = FocusManager.GetFocusedElement(page.XamlRoot)?.GetType().Name,
                page.IsTabStop, localValue = ReferenceEquals(local, DependencyProperty.UnsetValue) ? "unset" : local.ToString(), effectiveValue = page.GetValue(UIElement.IsTabStopProperty)?.ToString() });
        }
        Popup[] Portals() => VisualTreeHelper.GetOpenPopupsForXamlRoot(page.XamlRoot).Where(p => p.Child is FrameworkElement child && child.Name == "PART_Portal").ToArray();
        FrameworkElement Portal() => (FrameworkElement)Portals().Single().Child;
        void CheckFocus(DependencyObject owner) => Require(ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), owner), "A loading portal lifecycle update stole original focus.");
        void CheckViewport(FrameworkElement portal)
        {
            var at = portal.TransformToVisual(page).TransformPoint(new Point()); var size = page.XamlRoot.Size;
            Require(Math.Abs(at.X) < .05 && Math.Abs(at.Y) < .05 && Math.Abs(portal.ActualWidth - size.Width) < .05 && Math.Abs(portal.ActualHeight - size.Height) < .05, "Loading portal did not cover its current root viewport.");
        }
        void Record(string id) => reports.Add(new
        {
            id, viewport = page.XamlRoot.Size, focus = (FocusManager.GetFocusedElement(page.XamlRoot) as FrameworkElement)?.Name,
            originalFocus = ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), button), rootTabStop = page.IsTabStop,
            portals = Portals().Select(p =>
            {
                var visual = (FrameworkElement)p.Child; var at = visual.TransformToVisual(page).TransformPoint(new Point());
                var surface = Part<BleeckerOverlaySurface>(visual, "PART_Card"); var tint = Part<Border>(visual, "PART_Tint");
                return new { x = at.X, y = at.Y, width = visual.ActualWidth, height = visual.ActualHeight,
                    theme = surface.ActualTheme.ToString(), cardColor = ColorHex(((SolidColorBrush)surface.Background).Color),
                    tintColor = ColorHex(((SolidColorBrush)tint.Background).Color), tintOpacity = tint.Background.Opacity,
                    surface.Padding, surface.BorderThickness, surface.CornerRadius };
            }).ToArray()
        });
        static T Part<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            { var child = VisualTreeHelper.GetChild(parent, index); if (child is T match && match.Name == name) return match; try { return Part<T>(child, name); } catch (KeyNotFoundException) { } }
            throw new KeyNotFoundException(name);
        }
    }
}
