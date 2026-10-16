using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunLoadingOverlayContrastAsync(string folder, bool entryOnly)
    {
        // The standard mutation helper enforces the documented hosted-runner
        // boundary. Entry-only verification never constructs that helper.
        var original = Content; var page = new Page(); var host = new Canvas { Background = new SolidColorBrush(Microsoft.UI.Colors.White) }; page.Content = host;
        var action = new Button { Name = "ContrastAction", Content = "Original action", Width = 140, Height = 32 };
        var activations = 0; action.Click += (_, _) => activations++; Canvas.SetLeft(action, 20); Canvas.SetTop(action, 20); host.Children.Add(action);
        var overlay = new BleeckerLoadingOverlay { Label = "Preparing local report" }; AutomationProperties.SetName(overlay, "Preparing local report"); host.Children.Add(overlay);
        var reports = new List<object>(); var phase = "mount"; var passed = false; NativeInputSession? input = null;
        try
        {
            Content = page; await AwaitPresentationAsync(page); input = new NativeInputSession(page.XamlRoot, folder, "loading-overlay-contrast"); await input.AcquireAsync(action);
            Require(action.Focus(FocusState.Keyboard), "Could not establish loading-contrast keyboard focus.");
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                using var contrast = entryOnly ? null : new NativeContrastSession(folder);
                page.RequestedTheme = theme; overlay.IsVisible = true; await AwaitPresentationAsync(page);
                var portal = Portal(); var card = Part<BleeckerOverlaySurface>(portal, "PART_Card"); var label = Part<TextBlock>(portal, "PART_Label");
                var backdrop = Part<Grid>(portal, "PART_Backdrop"); var tint = Part<Border>(portal, "PART_Tint");
                var spinner = Part<BleeckerLoadingSpinner>(portal, "PART_Spinner"); var arc = Part<Microsoft.UI.Xaml.Shapes.Path>(spinner, "PART_Arc");
                var shadow = Part<Grid>(card, "PART_ShadowHost"); var initial = Snapshot();
                phase = "before-" + theme; Record(phase); await SaveAsync();
                Require(!NativeContrastSession.Read().Enabled && !new AccessibilitySettings().HighContrast && HasBlur() && HasShadow(), "Loading contrast baseline did not have the ordinary effects enabled.");
                Require(ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), action), "Opening the loading contrast specimen moved keyboard focus.");
                var mask = await ContrastFixture.ReadInkAsync(input, label, folder, "loading-label-" + theme);
                await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, entryOnly ? $"catalog-{theme.ToString().ToLowerInvariant()}.png" : $"contrast-before-{theme}.png"));
                if (!entryOnly)
                {
                    try
                    {
                        phase = "enable-" + theme; await SaveAsync(); contrast!.Enable();
                        await AwaitAsync(() => NativeContrastSession.Read().Enabled && new AccessibilitySettings().HighContrast &&
                            Matches(tint.Background,5) && Matches(card.Background,5) && Matches(card.BorderBrush,8) && Matches(label.Foreground,8) && Matches(arc.Stroke,13) && !HasBlur() && !HasShadow(),
                            "Open loading portal did not adopt opaque Windows contrast colors and suppress effects.");
                        await ContrastFixture.AwaitRecoveryTargetAsync(input, folder, page); await input.RequireForegroundAsync();
                        Require(ReferenceEquals(Portal(), portal), "Contrast transition recreated the loading portal.");
                        Require(ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), action), "Contrast transition moved the logical loading action focus.");
                        await ContrastFixture.VerifyInkAsync(input, label, 8, mask, folder, "loading-label-" + theme);
                        phase = "active-" + theme; Record(phase); await SaveAsync();
                        await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"contrast-active-{theme}.png"));
                        var before = activations; await input.ClickAsync(portal, localPoint: new Windows.Foundation.Point(90,36));
                        Require(activations == before, "Contrast loading portal passed a pointer click to its underlying action.");
                        Require(action.Focus(FocusState.Keyboard), "Could not restore contrast loading action focus."); await AwaitPresentationAsync(page);
                        await input.KeyAsync(0x20, action); await AwaitPresentationAsync(page); Require(activations == before + 1 && !page.IsTabStop, "Contrast loading portal changed keyboard activation or root restoration.");
                    }
                    finally
                    {
                        phase = "restore-" + theme; contrast!.Restore();
                        await AwaitAsync(() => !NativeContrastSession.Read().Enabled && !new AccessibilitySettings().HighContrast &&
                            Brush(tint.Background) == initial.Tint && Brush(card.Background) == initial.Card && Brush(card.BorderBrush) == initial.Border &&
                            Brush(label.Foreground) == initial.Text && Brush(arc.Stroke) == initial.Spinner && HasBlur() && HasShadow(), "Loading portal did not restore its original palette and effects.");
                        Record("restored-" + theme); await SaveAsync();
                        await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"contrast-restored-{theme}.png"));
                    }
                }
                overlay.IsVisible = false; await AwaitPresentationAsync(page);
                bool HasBlur() => ElementCompositionPreview.GetElementChildVisual(backdrop) is not null;
                bool HasShadow() => shadow.Visibility == Visibility.Visible && ElementCompositionPreview.GetElementChildVisual(shadow) is not null;
                LoadingContrastPalette Snapshot() => new(Brush(tint.Background),Brush(card.Background),Brush(card.BorderBrush),Brush(label.Foreground),Brush(arc.Stroke));
                void Record(string id) => reports.Add(new { id, scheme=theme.ToString(), palette=Snapshot(), hasBlur=HasBlur(), hasShadow=HasShadow(),
                    os=NativeContrastSession.Read(), highContrast=new AccessibilitySettings().HighContrast,
                    systemWindow=ColorHex(NativeContrastSession.Color(5)),systemText=ColorHex(NativeContrastSession.Color(8)),systemHighlight=ColorHex(NativeContrastSession.Color(13)),
                    name=FrameworkElementAutomationPeer.CreatePeerForElement(spinner).GetName(),focusPreserved=ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot),action),
                    rootTabStop=page.IsTabStop, viewport=page.XamlRoot.Size });
            }
            passed = true;
        }
        finally
        {
            overlay.IsVisible = false; input?.Dispose(); Content = original; await AwaitPresentationAsync(CatalogRoot); await SaveAsync();
        }
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new {passed=true,component=entryOnly?"loading-overlay-contrast-entry":"loading-overlay-contrast",environment=RuntimeEnvironment()}));
        FrameworkElement Portal() => (FrameworkElement)VisualTreeHelper.GetOpenPopupsForXamlRoot(page.XamlRoot).Single(p=>p.Child is FrameworkElement child&&child.Name=="PART_Portal").Child;
        Task SaveAsync() => File.WriteAllTextAsync(Path.Combine(folder,"loading-overlay-contrast.json"),JsonSerializer.Serialize(new {passed,phase,entryOnly,reports,originalContentRestored=ReferenceEquals(Content,original)},new JsonSerializerOptions{WriteIndented=true}));
        async Task AwaitAsync(Func<bool> condition,string message)
        { var timer=System.Diagnostics.Stopwatch.StartNew();while(!condition()&&timer.ElapsedMilliseconds<2000)await AwaitPresentationAsync(page);Require(condition(),message); }
        static bool Matches(Brush brush,int system) => brush is SolidColorBrush solid&&solid.Color==NativeContrastSession.Color(system)&&solid.Opacity==1;
        static LoadingContrastBrush Brush(Brush value) { var solid=(SolidColorBrush)value;return new(ColorHex(solid.Color),solid.Opacity); }
        static T Part<T>(DependencyObject parent,string name) where T:FrameworkElement
        {
            for(var index=0;index<VisualTreeHelper.GetChildrenCount(parent);index++)
            {var child=VisualTreeHelper.GetChild(parent,index);if(child is T match&&match.Name==name)return match;try{return Part<T>(child,name);}catch(KeyNotFoundException){}}
            throw new KeyNotFoundException(name);
        }
    }
    private sealed record LoadingContrastBrush(string Color,double Opacity);
    private sealed record LoadingContrastPalette(LoadingContrastBrush Tint,LoadingContrastBrush Card,LoadingContrastBrush Border,LoadingContrastBrush Text,LoadingContrastBrush Spinner);
}
