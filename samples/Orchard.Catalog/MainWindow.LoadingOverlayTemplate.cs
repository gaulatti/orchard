using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunLoadingOverlayTemplateAsync(string folder)
    {
        var original = Content; var page = new Page { RequestedTheme = ElementTheme.Light };
        var host = new Canvas(); page.Content = host;
        var action = new Button { Name = "TemplateAction", Content = "Original action", Width = 140, Height = 32 };
        var activations = 0; action.Click += (_, _) => activations++;
        Canvas.SetLeft(action, 20); Canvas.SetTop(action, 20); host.Children.Add(action);
        var anchor = new Grid { Width = 80, Height = 40 }; Canvas.SetLeft(anchor, 53.25); Canvas.SetTop(anchor, 117.5); host.Children.Add(anchor);
        var overlay = new BleeckerLoadingOverlay { Label = "Caller template 日本語", IsVisible = true };
        AutomationProperties.SetName(overlay, "Caller operation");
        var palettes = new Dictionary<ElementTheme, (SolidColorBrush Card, SolidColorBrush Text, SolidColorBrush Border, SolidColorBrush Tint)>();
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            var colors = theme == ElementTheme.Light
                ? (Microsoft.UI.Colors.Beige, Microsoft.UI.Colors.Purple, Microsoft.UI.Colors.Green, Microsoft.UI.Colors.LightBlue)
                : (Microsoft.UI.Colors.DarkSlateBlue, Microsoft.UI.Colors.LightYellow, Microsoft.UI.Colors.Coral, Microsoft.UI.Colors.DarkCyan);
            var palette = (new SolidColorBrush(colors.Item1), new SolidColorBrush(colors.Item2), new SolidColorBrush(colors.Item3), new SolidColorBrush(colors.Item4) { Opacity = .4 });
            palettes[theme] = palette;
            var dictionary = new ResourceDictionary { ["BleeckerCardBrush"] = palette.Item1, ["BleeckerTextPrimaryBrush"] = palette.Item2,
                ["BleeckerLoadingOverlayBorderBrush"] = palette.Item3, ["BleeckerLoadingOverlayTintBrush"] = palette.Item4 };
            overlay.Resources.ThemeDictionaries[theme.ToString()] = dictionary;
        }
        anchor.Children.Add(overlay);
        var reports = new List<object>(); var focusTrace = new List<object>(); var phase = "mount"; var passed = false; NativeInputSession? input = null;
        try
        {
            Content = page; await AwaitPresentationAsync(page);
            input = new NativeInputSession(page.XamlRoot, folder, "loading-overlay-template"); await input.AcquireAsync(action);
            Require(action.Focus(FocusState.Keyboard), "Could not focus the original template action."); await SettleAsync();
            var defaultTemplate = overlay.Template;
            await CheckAsync("default-resources", palettes[ElementTheme.Light], false);
            var old = Portal(); overlay.Template = ConsumerTemplate(); overlay.ApplyTemplate(); await SettleAsync();
            Require(!old.IsLoaded && !ReferenceEquals(old, Portal()), "Custom template retained the previous portal.");
            await CheckAsync("custom-light", palettes[ElementTheme.Light], true);
            foreach (var theme in new[] { ElementTheme.Dark, ElementTheme.Light })
            {
                page.RequestedTheme = theme; await SettleAsync(); await CheckAsync("custom-theme-" + theme, palettes[theme], true);
                await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
            }
            var light = palettes[ElementTheme.Light]; light.Card.Color = Microsoft.UI.Colors.Azure; light.Text.Color = Microsoft.UI.Colors.DarkOrange; light.Tint.Opacity = .3;
            await SettleAsync(); await CheckAsync("mutated-resources", light, true);
            overlay.Padding = new Thickness(31, 19, 23, 17); overlay.BorderThickness = new Thickness(2, 3, 4, 5); overlay.CornerRadius = new CornerRadius(6, 8, 10, 12);
            overlay.FontSize = 18; overlay.CharacterSpacing = 70; overlay.FlowDirection = FlowDirection.RightToLeft;
            overlay.Label = "Updated caller label العربية"; AutomationProperties.SetName(overlay, "Updated caller operation");
            await SettleAsync(); await CheckAsync("live-properties", light, true);
            var replacement = new SolidColorBrush(Microsoft.UI.Colors.MistyRose); overlay.Background = replacement;
            page.RequestedTheme = ElementTheme.Dark; await SettleAsync();
            var dark = palettes[ElementTheme.Dark]; await CheckAsync("local-precedence", (replacement, dark.Text, dark.Border, dark.Tint), true);
            overlay.ClearValue(Control.BackgroundProperty); await SettleAsync(); await CheckAsync("clear-local", dark, true);
            var replaced = (Card: new SolidColorBrush(Microsoft.UI.Colors.MidnightBlue), Text: new SolidColorBrush(Microsoft.UI.Colors.Ivory),
                Border: new SolidColorBrush(Microsoft.UI.Colors.OrangeRed), Tint: new SolidColorBrush(Microsoft.UI.Colors.DarkSlateGray) { Opacity = .55 });
            overlay.Resources.ThemeDictionaries["Dark"] = new ResourceDictionary { ["BleeckerCardBrush"] = replaced.Card, ["BleeckerTextPrimaryBrush"] = replaced.Text,
                ["BleeckerLoadingOverlayBorderBrush"] = replaced.Border, ["BleeckerLoadingOverlayTintBrush"] = replaced.Tint };
            page.RequestedTheme = ElementTheme.Light; await SettleAsync(); page.RequestedTheme = ElementTheme.Dark; await SettleAsync();
            await CheckAsync("replaced-dictionary", replaced, true);
            var custom = Portal(); overlay.Template = defaultTemplate; overlay.ApplyTemplate(); await SettleAsync();
            Require(!custom.IsLoaded && !ReferenceEquals(custom, Portal()), "Default template restoration retained custom visuals.");
            await CheckAsync("default-restored", replaced, false);
            overlay.IsVisible = false; await SettleAsync(); Require(Portals().Length == 0 && ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), action), "Hiding the custom portal retained visuals or moved focus.");
            passed = true;
        }
        finally
        {
            overlay.IsVisible = false; input?.Dispose(); Content = original; await AwaitPresentationAsync(CatalogRoot);
            await File.WriteAllTextAsync(Path.Combine(folder, "loading-overlay-template.json"), JsonSerializer.Serialize(new { passed, phase, reports, focusTrace, originalContentRestored = ReferenceEquals(Content, original) }, new JsonSerializerOptions { WriteIndented = true }));
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "loading-overlay-template", environment = RuntimeEnvironment() }));
        Task SettleAsync() { page.UpdateLayout(); return AwaitPresentationAsync(page); }
        Popup[] Portals() => VisualTreeHelper.GetOpenPopupsForXamlRoot(page.XamlRoot).Where(p => p.Child is FrameworkElement child && child.Name == "PART_Portal").ToArray();
        FrameworkElement Portal() => (FrameworkElement)Portals().Single().Child;
        async Task CheckAsync(string id, (SolidColorBrush Card, SolidColorBrush Text, SolidColorBrush Border, SolidColorBrush Tint) palette, bool custom)
        {
            phase = id; var portal = Portal(); var card = Parts<BleeckerOverlaySurface>(portal).Single(); var tint = Parts<Border>(portal).Single(p => p.Name == "PART_Tint");
            var label = Parts<TextBlock>(portal).Single(p => p.Name == "PART_Label"); var spinner = Parts<BleeckerLoadingSpinner>(portal).Single();
            var at = portal.TransformToVisual(page).TransformPoint(new Point()); var viewport = page.XamlRoot.Size;
            var colors = ReferenceEquals(card.Background, palette.Card) && ReferenceEquals(card.BorderBrush, palette.Border) && ReferenceEquals(label.Foreground, palette.Text) && ReferenceEquals(tint.Background, palette.Tint);
            var name = FrameworkElementAutomationPeer.CreatePeerForElement(spinner).GetName();
            var focusPreserved = ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), action);
            var customVisual = Parts<Border>(portal).Any(p => p.Name == "ConsumerDecoration");
            reports.Add(new { id, theme = page.RequestedTheme.ToString(), at, viewport, portal.ActualWidth, portal.ActualHeight, colors, focusPreserved, customVisual, name,
                cardColor = ColorHex(((SolidColorBrush)card.Background).Color), tintOpacity = tint.Background.Opacity, label.Text, label.FontSize, label.CharacterSpacing, direction = label.FlowDirection.ToString(), card.Padding, card.BorderThickness, card.CornerRadius });
            Require(colors && focusPreserved && customVisual == custom && name == AutomationProperties.GetName(overlay), "Custom portal lost caller resources, focus, visuals or accessible name: " + id);
            Require(Math.Abs(at.X) < .05 && Math.Abs(at.Y) < .05 && Math.Abs(portal.ActualWidth - viewport.Width) < .05 && Math.Abs(portal.ActualHeight - viewport.Height) < .05, "Custom portal lost viewport coverage: " + id);
            Require(card.Padding == overlay.Padding && card.BorderThickness == overlay.BorderThickness && card.CornerRadius == overlay.CornerRadius && label.FontSize == overlay.FontSize && label.CharacterSpacing == overlay.CharacterSpacing && label.FlowDirection == overlay.FlowDirection, "Custom portal lost live geometry or typography: " + id);
            var before = activations; var rootTabStop = page.IsTabStop;
            ObserveFocus("before-pointer");
            await input!.ClickAsync(portal, localPoint: new Point(90, 36)); Require(activations == before, "Custom portal passed a pointer click to the original action.");
            Require(ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), page), "Custom portal did not clear pointer focus to its root.");
            ObserveFocus("after-pointer");
            Require(action.Focus(FocusState.Keyboard), "Could not restore original keyboard focus after custom pointer barrier check.");
            ObserveFocus("focus-requested"); await SettleAsync(); ObserveFocus("focus-presented");
            Require(ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), action), "Original action did not regain focus after presentation.");
            Require(page.IsTabStop == rootTabStop, "Custom portal retained its temporary root tab stop.");
            void ObserveFocus(string kind) => focusTrace.Add(new { id, kind, page.IsTabStop, focused = FocusManager.GetFocusedElement(page.XamlRoot)?.GetType().Name });
        }
        static IEnumerable<T> Parts<T>(DependencyObject parent) where T : DependencyObject
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            { var child = VisualTreeHelper.GetChild(parent, index); if (child is T match) yield return match; foreach (var nested in Parts<T>(child)) yield return nested; }
        }
        static ControlTemplate ConsumerTemplate() => (ControlTemplate)XamlReader.Load("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:local="using:Orchard" TargetType="local:BleeckerLoadingOverlay">
             <Grid Width="0" Height="0" HorizontalAlignment="Left" VerticalAlignment="Top" FlowDirection="LeftToRight">
              <Popup x:Name="PART_Popup" IsLightDismissEnabled="False" ShouldConstrainToRootBounds="True" FlowDirection="LeftToRight">
               <Canvas x:Name="PART_Portal" Background="Transparent" UseLayoutRounding="False" FlowDirection="LeftToRight">
                <Grid x:Name="PART_Backdrop" IsHitTestVisible="False" />
                <Border x:Name="PART_Tint" Background="{TemplateBinding OverlayBrush}" />
                <Border x:Name="ConsumerDecoration" Width="24" Height="24" CornerRadius="12" Background="Gold" Canvas.Left="12" Canvas.Top="70" IsHitTestVisible="False" />
                <local:BleeckerOverlaySurface x:Name="PART_Card" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="{TemplateBinding CornerRadius}" Padding="{TemplateBinding Padding}" UseLayoutRounding="False">
                 <Canvas x:Name="PART_Body" UseLayoutRounding="False">
                  <local:BleeckerLoadingSpinner x:Name="PART_Spinner" Size="Large" IsHitTestVisible="False" />
                  <Canvas x:Name="PART_LabelHost" UseLayoutRounding="False"><TextBlock x:Name="PART_Label" Foreground="{TemplateBinding Foreground}" TextWrapping="WrapWholeWords" TextReadingOrder="UseFlowDirection" LineStackingStrategy="BlockLineHeight" UseLayoutRounding="False" /></Canvas>
                 </Canvas>
                </local:BleeckerOverlaySurface>
               </Canvas>
              </Popup>
             </Grid>
            </ControlTemplate>
            """);
    }
}
