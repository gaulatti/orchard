using System.Text.Json;
using System.Windows.Input;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunDialogCloseButtonsAsync(string folder)
    {
        using var stream = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.DialogCloseButton.json")
            ?? throw new InvalidOperationException("Dialog Close Button reference is missing.");
        using var document = await JsonDocument.ParseAsync(stream);
        var previous = Content; var previousSize = AppWindow.ClientSize;
        var canvas = new Canvas { Width = 200, Height = 120, UseLayoutRounding = false, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var anchor = new Button { Width = 20, Height = 20, Opacity = 0 };
        Canvas.SetLeft(anchor, 150); Canvas.SetTop(anchor, 80); canvas.Children.Add(anchor);
        var frames = new List<object>(); var interactions = new List<object>();
        object Box(FrameworkElement element)
        {
            var box = element.TransformToVisual(canvas).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
            return new { x = box.X, y = box.Y, width = box.Width, height = box.Height };
        }
        static object Paint(Brush? brush)
        {
            if (brush is null) return new { r = 0, g = 0, b = 0, a = 0, opacity = 1d };
            if (brush is not SolidColorBrush solid) throw new InvalidOperationException("Close reference requires solid paint.");
            return new { r = (int)solid.Color.R, g = (int)solid.Color.G, b = (int)solid.Color.B, a = (int)solid.Color.A, opacity = solid.Opacity };
        }
        static object? Shadow(Grid host)
        {
            if (ElementCompositionPreview.GetElementChildVisual(host) is not SpriteVisual output) return null;
            var mask = (CompositionMaskBrush)output.Brush; var color = (CompositionColorBrush)mask.Source;
            var surface = (CompositionVisualSurface)((CompositionSurfaceBrush)mask.Mask).Surface;
            var effect = (CompositionEffectBrush)((SpriteVisual)surface.SourceVisual).Brush;
            Require(effect.Properties.TryGetScalar("cssBlur.BlurAmount", out var sigma) == CompositionGetValueStatus.Succeeded, "Close shadow lost its Gaussian blur.");
            var casterSurface = (CompositionVisualSurface)((CompositionSurfaceBrush)effect.GetSourceParameter("mask")).Surface;
            var caster = (SpriteVisual)casterSurface.SourceVisual; var inset = (host.ActualWidth - caster.Size.X) / 2;
            return new { blur = 2 * sigma, y = output.Offset.Y - casterSurface.SourceOffset.Y - inset, spread = -inset, alpha = output.Opacity * color.Color.A / 255d,
                r = color.Color.R, g = color.Color.G, b = color.Color.B, exteriorMask = effect.GetSourceParameter("exterior") is CompositionSurfaceBrush };
        }
        try
        {
            Content = canvas;
            await AwaitPresentationAsync(canvas);
            using var input = new NativeInputSession(canvas.XamlRoot, folder, "dialog-close");
            await input.AcquireAsync(canvas);
            foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
            {
                var theme = Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!); canvas.RequestedTheme = theme;
                canvas.Background = new SolidColorBrush(theme == ElementTheme.Light ? Microsoft.UI.Colors.White : Windows.UI.Color.FromArgb(255, 13, 24, 33));
                var spec = row.GetProperty("spec"); var id = row.GetProperty("id").GetString()!;
                var button = new BleeckerDialogCloseButton(); AutomationProperties.SetName(button, "Close preview");
                if (spec.TryGetProperty("iconSize", out var iconSize)) button.IconSize = iconSize.GetDouble();
                if (spec.TryGetProperty("disabled", out var disabled)) button.IsEnabled = !disabled.GetBoolean();
                Canvas.SetLeft(button, 20); Canvas.SetTop(button, 20); canvas.Children.Add(button);
                anchor.Focus(FocusState.Programmatic); await input.MoveAsync(anchor); await AwaitPresentationAsync(button);
                var surface = Desc<Grid>(button).Single(value => value.Name == "CloseRoot");
                var border = Desc<Border>(button).Single(value => value.Name == "CloseBorder");
                var icon = Desc<Viewbox>(button).Single(value => value.Name == "CloseIcon");
                var shadowHost = Desc<Grid>(button).Single(value => value.Name == "CloseShadowHost");
                var ink = Desc<Microsoft.UI.Xaml.Shapes.Line>(button).First();
                async Task Capture()
                {
                    await AwaitPresentationAsync(button);
                    var transform = (CompositeTransform)surface.RenderTransform;
                    frames.Add(new { id, scheme = theme.ToString(), measurement = new { box = Box(surface), icon = Box(icon), disabled = !button.IsEnabled,
                        foreground = Paint(ink.Stroke), background = Paint(border.Background), border = Paint(border.BorderBrush), radius = border.CornerRadius.TopLeft,
                        borderWidth = border.BorderThickness.Left, opacity = surface.Opacity, translateY = transform.TranslateY, scale = transform.ScaleX,
                        shadow = Shadow(shadowHost), focusVisible = button.FocusState == FocusState.Keyboard, strokeWidth = ink.StrokeThickness,
                        name = FrameworkElementAutomationPeer.CreatePeerForElement(button).GetName() } });
                    await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, row.GetProperty("file").GetString()!));
                }
                if (id == "pressed") await input.HeldPointerAsync(button, Capture);
                else
                {
                    if (id == "hover") await input.MoveAsync(button);
                    if (id is "focus" or "keyboard-pressed")
                        await input.HeldKeyAsync(0x09, anchor, () => button, () => AwaitPresentationAsync(button));
                    if (id == "keyboard-pressed") await input.HeldKeyAsync(0x20, button, () => button, Capture);
                    else await Capture();
                }
                canvas.Children.Remove(button);
            }
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                canvas.RequestedTheme = theme;
                var button = new BleeckerDialogCloseButton(); AutomationProperties.SetName(button, "Consumer close action");
                Canvas.SetLeft(button, 20); Canvas.SetTop(button, 20); canvas.Children.Add(button); await AwaitPresentationAsync(button);
                var parameter = new object(); var command = new CloseFixtureCommand(); button.Command = command; button.CommandParameter = parameter;
                var clicks = 0; void Requested(object sender, RoutedEventArgs args) => clicks++; button.Click += Requested;
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
                Require(peer.GetAutomationControlType() == AutomationControlType.Button && peer.GetName() == "Consumer close action" && peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider, "Close button lost its native action semantics.");
                object? heldTransform = null;
                await input.HeldPointerAsync(button, async () =>
                {
                    await AwaitPresentationAsync(button);
                    var transform = (CompositeTransform)Desc<Grid>(button).Single(value => value.Name == "CloseRoot").RenderTransform;
                    heldTransform = new { transform.TranslateY, transform.ScaleX, transform.ScaleY, clicks };
                    Require(transform.TranslateY == 1 && Math.Abs(transform.ScaleX - .97) < .000001 && Math.Abs(transform.ScaleY - .97) < .000001 && clicks == 0,
                        $"Held close pointer differs: Y={transform.TranslateY:R}, ScaleX={transform.ScaleX:R}, ScaleY={transform.ScaleY:R}, clicks={clicks}.");
                });
                Require(clicks == 1 && command.Executions == 1 && ReferenceEquals(command.Parameter, parameter) && button.IsLoaded, "Close activation changed the consumer parameter, fired twice or dismissed content.");
                await input.KeyAsync(0x20, button); await input.KeyAsync(0x0D, button); await AwaitPresentationAsync(button);
                Require(clicks == 3 && command.Executions == 3, "Close Space/Enter activation did not reach the consumer.");
                command.Enabled = false; command.Notify(); await AwaitPresentationAsync(button);
                Require(!button.IsEnabled, "Close command CanExecute did not disable the action.");
                var blocked = false;
                try { ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke(); }
                catch (ElementNotEnabledException) { blocked = true; }
                catch (System.Runtime.InteropServices.COMException error) when (error.HResult == unchecked((int)0x80040200)) { blocked = true; }
                Require(blocked && command.Executions == 3, "Disabled close invocation reached the consumer.");
                command.Enabled = true; command.Notify(); await AwaitPresentationAsync(button);
                await input.MoveAsync(anchor); await input.MoveAsync(button); await AwaitPresentationAsync(button);
                var previousShadowHost = Desc<Grid>(button).Single(value => value.Name == "CloseShadowHost");
                Require(ElementCompositionPreview.GetElementChildVisual(previousShadowHost) is SpriteVisual, "Close hover shadow did not reach the compositor.");
                var original = button.Template; button.Template = null; await AwaitPresentationAsync(button);
                Require(ElementCompositionPreview.GetElementChildVisual(previousShadowHost) is null, "Close template replacement retained its previous compositor graph.");
                button.Template = original; await AwaitPresentationAsync(button);
                Require(Desc<Grid>(button).Any(value => value.Name == "CloseRoot"), "Close template did not remount.");
                var remountedShadowHost = Desc<Grid>(button).Single(value => value.Name == "CloseShadowHost");
                canvas.Children.Remove(button); await AwaitPresentationAsync(canvas);
                Require(ElementCompositionPreview.GetElementChildVisual(remountedShadowHost) is null, "Close unload retained its compositor graph.");
                canvas.Children.Add(button); await AwaitPresentationAsync(button);
                Require(button.IsLoaded && button.IconSize == 18 && ReferenceEquals(button.CommandParameter, parameter), "Close remount lost consumer state.");
                await input.MoveAsync(anchor);
                button.RequestedTheme = theme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
                await AwaitPresentationAsync(button);
                var liveBackground = (SolidColorBrush)Desc<Border>(button).Single(value => value.Name == "CloseBorder").Background;
                var liveDark = button.ActualTheme == ElementTheme.Dark;
                var liveHover = button.IsPointerOver;
                var expectedColor = liveHover ? liveDark ? Windows.UI.Color.FromArgb(255, 26, 35, 50) : Windows.UI.Color.FromArgb(255, 249, 246, 242)
                    : liveDark ? Windows.UI.Color.FromArgb(255, 24, 37, 51) : Microsoft.UI.Colors.White;
                var expectedAlpha = liveHover ? liveDark ? .1 : .5 : 1;
                if (liveBackground.Color != expectedColor || Math.Abs(liveBackground.Opacity - expectedAlpha) >= .000001)
                {
                    var palette = Desc<Border>(button).Single(value => value.Name == "ClosePalette");
                    var surface = Desc<Grid>(button).Single(value => value.Name == "CloseRoot");
                    await File.WriteAllTextAsync(Path.Combine(folder, "live-theme-diagnostics.json"), JsonSerializer.Serialize(new {
                        requested = button.RequestedTheme.ToString(), actual = button.ActualTheme.ToString(), button.IsPointerOver,
                        commonState = VisualStateManager.GetVisualStateGroups(surface).Single(group => group.Name == "CommonStates").CurrentState?.Name,
                        control = Paint(button.Background), palette = Paint(palette.Background), painted = Paint(liveBackground), expected = expectedColor.ToString() }));
                }
                Require(liveBackground.Color == expectedColor && Math.Abs(liveBackground.Opacity - expectedAlpha) < .000001, "Close live theme did not update painted chrome.");
                var livePaint = new { scheme = button.ActualTheme.ToString(), id = liveHover ? "hover" : "normal", background = Paint(liveBackground) };
                button.RequestedTheme = ElementTheme.Default; await AwaitPresentationAsync(button);
                interactions.Add(new { theme = theme.ToString(), passed = true, clicks, command.Executions, parameterIdentity = true, consumerContentRetained = true, disabledRejected = true, templateReplacement = true, remount = true, releasedShadowGraphs = true, liveTheme = true, livePaint, immediatePressTransform = true, heldTransform });
                button.Click -= Requested; canvas.Children.Remove(button);
            }
            await File.WriteAllTextAsync(Path.Combine(folder, "dialog-close.json"), JsonSerializer.Serialize(new { passed = true, frames, interactions }, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "dialog-close", environment = RuntimeEnvironment() }));
        }
        finally { Content = previous; AppWindow.ResizeClient(previousSize); await AwaitPresentationAsync(CatalogRoot); }
    }

    private sealed class CloseFixtureCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        internal bool Enabled { get; set; } = true;
        internal int Executions { get; private set; }
        internal object? Parameter { get; private set; }
        public bool CanExecute(object? parameter) => Enabled;
        public void Execute(object? parameter) { Require(Enabled, "A disabled close command executed."); Executions++; Parameter = parameter; }
        internal void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
