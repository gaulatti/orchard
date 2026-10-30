using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard.Catalog;

public sealed partial class AlertsFixture : UserControl
{
    public AlertsFixture()
    {
        InitializeComponent();
        Specimen.Opened += (_, _) => Outcome.Text = $"{Specimen.Type} alert opened.";
        Specimen.Closed += (_, _) => Outcome.Text = "Consumer close callback delivered.";
    }
    private void ShowAlert(object sender, RoutedEventArgs args)
    {
        if (!double.TryParse(DurationBox.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var duration) || !double.IsFinite(duration))
        { Outcome.Text = "Enter a finite duration in milliseconds."; return; }
        Specimen.Type = Enum.Parse<BleeckerAlertType>((string)((FrameworkElement)sender).Tag);
        Specimen.Message = MessageBox.Text; Specimen.Duration = duration; Specimen.IsOpen = true;
    }
    private void PublishInfo(object sender, RoutedEventArgs args) => BleeckerAlertContainer.ShowAlert(XamlRoot, MessageBox.Text);
    private void PublishWarning(object sender, RoutedEventArgs args) => BleeckerAlertContainer.ShowAlert(XamlRoot, MessageBox.Text, BleeckerAlertType.Warning);
    private void CloseStandalone(object sender, RoutedEventArgs args) => Specimen.Close();

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        var samples = new List<object>();
        var callbacks = 0;
        void Closed(object? sender, EventArgs args) => callbacks++;
        Specimen.Closed += Closed;
        using var input = new NativeInputSession(XamlRoot, folder, $"alerts-{scheme}");
        await input.AcquireAsync(Heading); await input.MoveAsync(Heading);
        Specimen.ApplyTemplate();
        var colors = BleeckerTokens.Colors(scheme);
        var root = (FrameworkElement)VisualTreeHelper.GetChild(Specimen, 0);
        var popup = (Popup)root.FindName("PART_Popup");
        var surface = (BleeckerOverlaySurface)root.FindName("PART_Surface");
        var close = (Button)root.FindName("PART_Close");
        var text = (TextBlock)root.FindName("PART_Message");
        var motion = (FrameworkElement)root.FindName("PART_MotionRoot");
        Require(Specimen.Duration == 5000 && Specimen.Type == BleeckerAlertType.Info, "Alert defaults diverged from the reference.");
        try
        {
            foreach (var type in Enum.GetValues<BleeckerAlertType>())
            {
                Require(MessageBox.Focus(FocusState.Keyboard), "Alert fixture could not establish consumer focus.");
                Specimen.Type = type; Specimen.Duration = 0;
                Specimen.Message = type == BleeckerAlertType.Info ? "A fictional coverage note\n東京 · العربية · café — a long line that wraps across the notification surface." : $"Fictional {type.ToString().ToLowerInvariant()} notification.";
                Specimen.IsOpen = true;
                await AwaitAsync(() => popup.IsOpen && surface.ActualWidth > 0 && motion.Opacity == 1, "Alert did not enter its visible native popup.");
                Require(ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), MessageBox), "Opening an alert stole consumer focus.");
                Require(surface.ActualWidth == Math.Min(448, XamlRoot.Size.Width - 32), "Alert width differs from the source.");
                var client = NativeScreenCapture.ClientBounds(input.Window);
                var closeLocal = close.TransformToVisual(surface).TransformPoint(new Windows.Foundation.Point(0, 0));
                var closePeer = new ButtonAutomationPeer(close).GetBoundingRectangle();
                var closeScreen = input.ScreenBounds(close);
                var scale = XamlRoot.RasterizationScale;
                var expectedX = client.X + ((XamlRoot.Size.Width - surface.ActualWidth) / 2 + closeLocal.X) * scale;
                var expectedY = client.Y + (96 + closeLocal.Y) * scale;
                Require(Math.Abs(closeScreen.X - expectedX) <= 1 && Math.Abs(closeScreen.Y - expectedY) <= 1,
                    $"Alert close screen coordinates differ from the window-owned reference: actual={closeScreen.X},{closeScreen.Y}, expected={expectedX},{expectedY}.");
                Require(surface.Padding == new Thickness(16) && surface.BorderThickness == new Thickness(1) && surface.CornerRadius == new CornerRadius(12), "Alert surface geometry differs from the reference.");
                Require(text.Text == Specimen.Message && text.FontSize == 14 && text.LineHeight == 24 &&
                    text.FontFamily.Source.Contains("EncodeSans-Medium.ttf#Encode Sans"), "Alert message did not reach the wrapped branded text.");
                var status = Descendants<Path>(surface).Single(path => (path.Name is "SuccessIcon" or "ErrorIcon" or "WarningIcon" or "InfoIcon") && path.Visibility == Visibility.Visible);
                var expectedIcon = type switch { BleeckerAlertType.Success => colors[scheme == BleeckerColorScheme.Dark ? "accentBlue" : "sea"], BleeckerAlertType.Error => colors["terracotta"], BleeckerAlertType.Warning => colors[scheme == BleeckerColorScheme.Dark ? "accentGold" : "sunset"], _ => colors[scheme == BleeckerColorScheme.Dark ? "accentGold" : "desert"] };
                var expectedBorder = type switch { BleeckerAlertType.Success => colors[scheme == BleeckerColorScheme.Dark ? "accentBlue" : "sea"], BleeckerAlertType.Error => colors["terracotta"], BleeckerAlertType.Warning => colors["sunset"], _ => colors["desert"] };
                var expectedAlpha = type switch { BleeckerAlertType.Error when scheme == BleeckerColorScheme.Dark => .3, BleeckerAlertType.Warning => scheme == BleeckerColorScheme.Dark ? .2 : .3, _ => .25 };
                Require(Hex(status.Stroke) == expectedIcon && Hex(surface.BorderBrush) == expectedBorder && surface.BorderBrush.Opacity == (double)(float)expectedAlpha &&
                    Hex(surface.Background) == (scheme == BleeckerColorScheme.Dark ? colors["deepSea"] : "#FFFFFFFF") && Hex(text.Foreground) == colors["textPrimary"],
                    $"Alert {scheme}/{type} palette differs: icon={Hex(status.Stroke)} expected={expectedIcon}, border={Hex(surface.BorderBrush)} expected={expectedBorder}, opacity={surface.BorderBrush.Opacity} expected={expectedAlpha}, background={Hex(surface.Background)}, text={Hex(text.Foreground)} expected={colors["textPrimary"]}.");
                Require(new ButtonAutomationPeer(close).GetName() == "Close alert" && AutomationProperties.GetLiveSetting(text) == AutomationLiveSetting.Polite &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(Specimen).GetName() == Specimen.Message, "Alert accessible message/close action is incorrect.");
                samples.Add(new { type, width = surface.ActualWidth, height = surface.ActualHeight, popup.HorizontalOffset, popup.VerticalOffset,
                    closePeer, closeScreen, expectedX, expectedY, message = text.Text, icon = Hex(status.Stroke), border = Hex(surface.BorderBrush), borderOpacity = surface.BorderBrush.Opacity });
                await MainWindow.CaptureElementAsync(surface, System.IO.Path.Combine(folder, $"alert-{scheme}-{type}.png"));
                var windowCapture = System.IO.Path.Combine(folder, $"alert-window-{scheme}-{type}.png");
                await input.RequireForegroundAsync();
                await NativeScreenCapture.CaptureClientAsync(input.Window, windowCapture);
                var expectedCallbacks = callbacks + 1;
                if (type == BleeckerAlertType.Success)
                {
                    var closeScale = (ScaleTransform)((FrameworkElement)VisualTreeHelper.GetChild(close, 0)).RenderTransform;
                    var closeRoot = (FrameworkElement)VisualTreeHelper.GetChild(close, 0);
                    var wash = (SolidColorBrush)((Border)closeRoot.FindName("PART_CloseChrome")).Background;
                    await input.MoveAsync(close);
                    await AwaitAsync(() => Math.Abs(wash.Opacity - (scheme == BleeckerColorScheme.Dark ? .1 : .05)) < .000001,
                        "Alert close hover did not reach the reference wash opacity.");
                    Require(wash.Color == Windows.UI.Color.FromArgb(255, scheme == BleeckerColorScheme.Dark ? (byte)255 : (byte)0,
                        scheme == BleeckerColorScheme.Dark ? (byte)255 : (byte)0, scheme == BleeckerColorScheme.Dark ? (byte)255 : (byte)0),
                        "Alert close hover changed the reference wash RGB.");
                    await input.PressPointerAsync();
                    await AwaitAsync(() => close.IsPressed && Math.Abs(closeScale.ScaleX - .9) < .000001 && Specimen.IsOpen,
                        "Held alert close did not reach the reference pressed scale.");
                    await MainWindow.CaptureElementAsync(close, System.IO.Path.Combine(folder, $"alert-close-held-{scheme}.png"));
                    await input.MoveAsync(Heading); input.PointerButton(false);
                    await AwaitAsync(() => !close.IsPressed && closeScale.ScaleX == 1 && wash.Opacity == 0, "Cancelled close press did not restore its scale and wash.");
                    Require(Specimen.IsOpen && callbacks < expectedCallbacks, "A cancelled pointer press closed the alert.");
                    await input.ClickAsync(close);
                }
                else if (type == BleeckerAlertType.Warning)
                {
                    Require(MessageBox.Focus(FocusState.Programmatic), "Alert could not establish its Tab entry anchor.");
                    // This walk traverses the catalog navigation as well as the
                    // fixture. Its bound must follow the mounted native controls.
                    var tabWalkLimit = Descendants<Control>(XamlRoot.Content)
                        .Append(close).Distinct().Count(control => control.IsLoaded && control.IsEnabled && control.IsTabStop && control.Visibility == Visibility.Visible) + 1;
                    var visited = new List<string>();
                    for (var step = 0; step < tabWalkLimit && !ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), close); step++)
                    {
                        input.Key(0x09); await Task.Delay(16);
                        visited.Add((FocusManager.GetFocusedElement(XamlRoot) as FrameworkElement)?.Name ?? "unnamed");
                    }
                    samples.Add(new { phase = "physical-tab", tabWalkLimit, visited });
                    Require(ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), close) && close.FocusState == FocusState.Keyboard,
                        "Physical Tab navigation did not reach the alert's close action.");
                    input.Key(0x09); await Task.Delay(16);
                    Require(!ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), close), "Alert popup trapped forward Tab navigation.");
                    var revisited = new List<string>();
                    for (var step = 0; step < tabWalkLimit && !ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), close); step++)
                    { input.Key(0x09); await Task.Delay(16); revisited.Add((FocusManager.GetFocusedElement(XamlRoot) as FrameworkElement)?.Name ?? "unnamed"); }
                    samples.Add(new { phase = "physical-tab-return", tabWalkLimit, visited = revisited });
                    Require(ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), close), "Alert's close action could not be revisited with Tab.");
                    input.ShiftKey(0x09); await Task.Delay(16);
                    Require(!ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), close), "Alert popup trapped reverse Tab navigation.");
                    input.Key(0x09); await Task.Delay(16);
                    Require(ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), close), "Forward Tab did not return from the alert's preceding control.");
                    input.Key(0x20);
                }
                else { Specimen.Close(); Specimen.Close(); }
                await AwaitAsync(() => !popup.IsOpen && callbacks == expectedCallbacks, "Alert close did not deliver exactly one consumer callback.");
            }

            Specimen.Message = string.Empty; Specimen.Duration = -1; Specimen.IsOpen = true;
            await AwaitAsync(() => popup.IsOpen && motion.Opacity == 1, "Persistent empty alert did not render.");
            await Task.Delay(300);
            Require(Specimen.IsOpen && text.Text == string.Empty, "Negative duration or empty message changed the pinned alert contract.");
            var closedBeforeTimer = callbacks;
            Specimen.Duration = 300; await Task.Delay(80); Specimen.Duration = 0; await Task.Delay(350);
            Require(Specimen.IsOpen && callbacks == closedBeforeTimer, "Changing duration did not cancel the previous lifetime timer.");
            Specimen.Duration = 100.5;
            await AwaitAsync(() => !popup.IsOpen && callbacks == closedBeforeTimer + 1, "Positive duration did not close through the consumer callback.");

            var defaultLifetime = System.Diagnostics.Stopwatch.StartNew();
            var beforeDefault = callbacks;
            Specimen.Duration = 5000; Specimen.IsOpen = true;
            await AwaitAsync(() => callbacks == beforeDefault + 1 && !popup.IsOpen, "Default lifetime did not dismiss the actual alert.", 7000);
            Require(defaultLifetime.Elapsed.TotalMilliseconds >= 5000, "Default lifetime dismissed early.");
            samples.Add(new { phase = "default-lifetime", elapsedMilliseconds = defaultLifetime.Elapsed.TotalMilliseconds });

            Specimen.Duration = 0; Specimen.Message = "A persistent fictional alert with a long line that wraps at a constrained width.";
            Specimen.MaxWidth = 280; Specimen.IsOpen = true;
            await AwaitAsync(() => popup.IsOpen && surface.ActualWidth == 280 && motion.Opacity == 1, "Constrained alert did not respect consumer MaxWidth.");
            await MainWindow.CaptureElementAsync(surface, System.IO.Path.Combine(folder, $"alert-constrained-{scheme}.png"));
            var savedTheme = Specimen.ReadLocalValue(RequestedThemeProperty);
            var opposite = scheme == BleeckerColorScheme.Light ? BleeckerColorScheme.Dark : BleeckerColorScheme.Light;
            Specimen.RequestedTheme = opposite == BleeckerColorScheme.Dark ? ElementTheme.Dark : ElementTheme.Light;
            await AwaitAsync(() => Hex(surface.Background) == (opposite == BleeckerColorScheme.Dark ? BleeckerTokens.Colors(opposite)["deepSea"] : "#FFFFFFFF") &&
                Hex(text.Foreground) == BleeckerTokens.Colors(opposite)["textPrimary"], "Open alert did not follow a live consumer theme change.");
            Require(popup.IsOpen && Specimen.IsOpen, "A consumer theme change dismissed its open alert.");
            await MainWindow.CaptureElementAsync(surface, System.IO.Path.Combine(folder, $"alert-live-theme-{scheme}.png"));
            if (savedTheme == DependencyProperty.UnsetValue) Specimen.ClearValue(RequestedThemeProperty); else Specimen.SetValue(RequestedThemeProperty, savedTheme);
            await AwaitAsync(() => Hex(text.Foreground) == colors["textPrimary"], "Alert theme did not restore.");

            var parent = (StackPanel)Content; var slot = parent.Children.IndexOf(Specimen);
            var beforeUnload = callbacks;
            parent.Children.Remove(Specimen);
            await AwaitAsync(() => !Specimen.IsLoaded && !popup.IsOpen, "Unloading an alert left its native popup open.");
            await Task.Delay(300);
            Require(callbacks == beforeUnload, "Unloading invoked an unsolicited consumer close callback.");
            parent.Children.Insert(slot, Specimen);
            await AwaitAsync(() => Specimen.IsLoaded && popup.IsOpen && motion.Opacity == 1, "Reloading the same open alert did not rehydrate its popup.");
            var beforeReplacement = callbacks;
            Specimen.Close();
            var template = Specimen.Template;
            Specimen.Template = null; Specimen.Template = template; Specimen.ApplyTemplate();
            root = (FrameworkElement)VisualTreeHelper.GetChild(Specimen, 0);
            popup = (Popup)root.FindName("PART_Popup"); surface = (BleeckerOverlaySurface)root.FindName("PART_Surface");
            close = (Button)root.FindName("PART_Close"); text = (TextBlock)root.FindName("PART_Message"); motion = (FrameworkElement)root.FindName("PART_MotionRoot");
            await AwaitAsync(() => !popup.IsOpen && callbacks == beforeReplacement + 1, "Template replacement lost the pending close callback.");
            Specimen.ClearValue(MaxWidthProperty);

            BleeckerAlertContainer.ShowAlert(XamlRoot, "Shared fictional first note", BleeckerAlertType.Success);
            BleeckerAlertContainer.ShowAlert(XamlRoot, "Shared fictional second note", BleeckerAlertType.Warning);
            await AwaitAsync(() => Host.Children.Count == 2 && Host.Children.OfType<BleeckerAlert>().All(IsPresented), "Shared alert stream did not present both messages.");
            var first = (BleeckerAlert)Host.Children[0]; var second = (BleeckerAlert)Host.Children[1];
            Require(first.Message == "Shared fictional first note" && second.Message == "Shared fictional second note", "Shared messages were reordered or replaced.");
            first.Close();
            await AwaitAsync(() => Host.Children.Count == 1, "Shared close did not remove the matching message.");
            Require(ReferenceEquals(Host.Children[0], second), "Removing a shared alert replaced the retained message's control.");
            second.Close(); await AwaitAsync(() => Host.Children.Count == 0, "Shared stream did not remove its final message.");
            var hostSlot = parent.Children.IndexOf(Host);
            var unmounted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void HostUnloaded(object sender, RoutedEventArgs args) => unmounted.TrySetResult();
            Host.Unloaded += HostUnloaded;
            try { parent.Children.Remove(Host); await unmounted.Task.WaitAsync(TimeSpan.FromSeconds(3)); }
            finally { Host.Unloaded -= HostUnloaded; }
            BleeckerAlertContainer.ShowAlert(XamlRoot, "Published while container was unmounted");
            var mounted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void HostLoaded(object sender, RoutedEventArgs args) => mounted.TrySetResult();
            Host.Loaded += HostLoaded;
            try { parent.Children.Insert(hostSlot, Host); await mounted.Task.WaitAsync(TimeSpan.FromSeconds(3)); }
            finally { Host.Loaded -= HostLoaded; }
            Require(Host.Children.Count == 0, "Subscription alone replayed a stream the reference only publishes on mutation.");
            BleeckerAlertContainer.ShowAlert(XamlRoot, "Published after container remounted");
            await AwaitAsync(() => Host.Children.Count == 2 && Host.Children.OfType<BleeckerAlert>().All(IsPresented), "Remounted container did not present the complete stream snapshot.");
            foreach (var alert in Host.Children.OfType<BleeckerAlert>().ToArray()) alert.Close();
            await AwaitAsync(() => Host.Children.Count == 0, "Remounted stream did not close its published messages.");
            samples.Add(new { phase = "lifecycle", positiveDuration = true, durationCancellation = true, persistentEmpty = true,
                repeatedClose = true, physicalPointer = true, physicalTabSpace = true, heldCancelledPress = true, fractionalDuration = true, sharedIdentity = true, sharedReload = true,
                templateExitCallback = true, unloadReload = true, liveTheme = true, constrainedWidth = true, callbacks });
        }
        catch (Exception error)
        {
            samples.Add(new { phase = "failure", error = error.Message, popup.HorizontalOffset, popup.VerticalOffset,
                client = NativeScreenCapture.ClientBounds(input.Window), closePeer = new ButtonAutomationPeer(close).GetBoundingRectangle(),
                closeScreen = input.ScreenBounds(close), ownerOrigin = Specimen.TransformToVisual(XamlRoot.Content).TransformPoint(new Windows.Foundation.Point(0, 0)) });
            try
            {
                if (popup.IsOpen) await MainWindow.CaptureElementAsync(surface, System.IO.Path.Combine(folder, $"alert-failure-{scheme}.png"));
                await NativeScreenCapture.CaptureClientAsync(input.Window, System.IO.Path.Combine(folder, $"alert-window-failure-{scheme}.png"));
            }
            catch(Exception captureError)
            {
                samples.Add(new{phase="failure-capture",error=captureError.Message,foreground=input.OwnsForeground});
            }
            throw;
        }
        finally
        {
            Specimen.Close(); Specimen.Closed -= Closed;
            Specimen.Duration = 5000; Specimen.Type = BleeckerAlertType.Info;
            Specimen.ClearValue(MaxWidthProperty);
            foreach (var alert in Host.Children.OfType<BleeckerAlert>().ToArray()) alert.Close();
            await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"alert-measurements-{scheme}.json"), JsonSerializer.Serialize(samples, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
    private async Task AwaitAsync(Func<bool> condition, string message, int milliseconds = 3000)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var readiness = new NativeReadiness(DispatcherQueue); while (!condition() && watch.Elapsed < TimeSpan.FromMilliseconds(milliseconds)) { UpdateLayout(); if (condition()) break; await readiness.WaitAsync(); }
        Require(condition(), message);
    }
    private static bool IsPresented(BleeckerAlert alert)
    {
        if (!alert.IsLoaded || VisualTreeHelper.GetChildrenCount(alert) == 0) return false;
        var root = (FrameworkElement)VisualTreeHelper.GetChild(alert, 0);
        return root.FindName("PART_Popup") is Popup { IsOpen: true } &&
            root.FindName("PART_Surface") is FrameworkElement { ActualWidth: > 0 } &&
            root.FindName("PART_MotionRoot") is FrameworkElement { Opacity: 1 };
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject owner) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(owner); index++)
        {
            var child = VisualTreeHelper.GetChild(owner, index);
            if (child is T item) yield return item;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private static string? Hex(Brush brush) => brush is SolidColorBrush solid ? $"#{solid.Color.A:X2}{solid.Color.R:X2}{solid.Color.G:X2}{solid.Color.B:X2}" : brush?.GetType().Name;
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
