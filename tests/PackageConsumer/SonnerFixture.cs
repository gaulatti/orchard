using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class SonnerFixture
{
    private delegate bool Visitor(nint handle, nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    { if (node is FrameworkElement element) yield return element; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Elements(VisualTreeHelper.GetChild(node, i))) yield return child; }
    private sealed class ActionCommand(Action<object?> invoke) : ICommand
    {
        internal bool Allowed = true;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => Allowed;
        public void Execute(object? parameter) => invoke(parameter);
        internal void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
    internal static async Task RunAsync(Window window, string output, string hostAssembly, Func<FrameworkElement>? catalogFactory = null)
    {
        var folder = Path.GetDirectoryName(output)!; using var source = JsonDocument.Parse(await File.ReadAllBytesAsync(Path.Combine(folder, "source-reference.json")));
        var root = (Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='1000' Height='760' Background='{ThemeResource BleeckerBackgroundBrush}'/>");
        root.Clip = new RectangleGeometry { Rect = new Rect(0, 0, 1000, 760) }; window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1000, 760));
        var scene = new Grid(); var modal = new BleeckerModalHost { Content = scene, Clip = new RectangleGeometry { Rect = new Rect(0, 0, 1000, 760) } }; root.Children.Add(modal);
        var states = new List<object>(); var contracts = new List<object>(); var presence = new List<object>(); var standalone = new List<object>(); var catalog = new List<object>(); var pngs = new List<object>();
        var guards = 0; var focus = 0; var activations = 0; var stage = "initial";
        root.GotFocus += (_, _) => focus++; window.Activated += (_, a) => { if (a.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var process); visible |= process == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "Sonner probe left its hidden boundary.");
        }
        void Fail(Exception error)
        { File.WriteAllText(output, JsonSerializer.Serialize(new { format = "orchard-sonner-native-v1", passed = false, stage, error = error.ToString(), states, contracts, presence, standalone, catalog, pngs, guards, focus, activations }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(1); }
        Application.Current.UnhandledException += (_, args) => Fail(args.Exception);
        async Task Layout(int delay = 35)
        {
            File.WriteAllText(Path.Combine(folder, "sonner-stage.json"), JsonSerializer.Serialize(new { stage, states = states.Count, contracts = contracts.Count, guards, focus, activations }));
            for (var i = 0; i < 4; i++) { var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Sonner dispatcher rejected layout."); await idle.Task; root.Measure(new Size(root.Width, root.Height)); root.Arrange(new Rect(0, 0, root.Width, root.Height)); root.UpdateLayout(); Guard(); }
            if (delay > 0) await Task.Delay(delay); root.UpdateLayout(); Guard();
        }
        async Task Ready(Func<bool> condition, string message, int timeout = 1200, Func<string>? diagnostic = null)
        { var watch = System.Diagnostics.Stopwatch.StartNew(); do { await Layout(16); if (condition()) return; } while (watch.ElapsedMilliseconds < timeout); Require(false, message + (diagnostic is null ? "" : " " + diagnostic())); }
        IEnumerable<FrameworkElement> All() => Elements(root).Concat(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Where(p => p.Child is not null).SelectMany(p => Elements(p.Child))).Distinct();
        BleeckerOverlaySurface[] Surfaces() => All().OfType<BleeckerOverlaySurface>().Where(s => s.IsLoaded && s.Name == "SonnerToastSurface").ToArray();
        Button Close(BleeckerOverlaySurface surface) => Elements(surface).OfType<Button>().Single(e => e.Name == "SonnerDismiss");
        Button Action(BleeckerOverlaySurface surface) => Elements(surface).OfType<Button>().Single(e => e.Name != "SonnerDismiss");
        void Execute(Button button) { Require(button.Command is not null && button.Command.CanExecute(button.CommandParameter), "Expected an enabled native Sonner command."); button.Command!.Execute(button.CommandParameter); Guard(); }
        IInvokeProvider Provider(Button button) => (IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke);
        bool Reject(IInvokeProvider provider)
        {
            try { provider.Invoke(); }
            catch (ElementNotEnabledException) { Guard(); return true; }
            catch (COMException error) when (error.HResult == unchecked((int)0x80040200)) { Guard(); return true; } // UIA_E_ELEMENTNOTENABLED
            throw new InvalidOperationException("An inactive Sonner provider accepted invocation.");
        }
        BleeckerOverlaySurface OriginalSurface(BleeckerSonnerToast toast) => Surfaces().Single(s => ReferenceEquals(s.Tag, toast));
        object Box(FrameworkElement element) { var b = element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight)); return new { x = b.X, y = b.Y, width = b.Width, height = b.Height }; }
        object Paint(Brush? brush) => brush is SolidColorBrush color ? new { a = color.Color.A, r = color.Color.R, g = color.Color.G, b = color.Color.B, opacity = color.Opacity } : throw new InvalidOperationException("Sonner expected a concrete brush.");
        object Text(BleeckerOverlaySurface surface, string name)
        {
            var panel = Elements(surface).Single(e => e.Name == name); var text = Elements(panel).OfType<TextBlock>().Single(e => e.Name == name + "Text");
            return new { box = Box(panel), text = text.Text, foreground = Paint(text.Foreground), fontSize = text.FontSize, lineHeight = text.LineHeight, fontWeight = text.FontWeight.Weight };
        }
        object ButtonSnapshot(Button button)
        {
            var isClose = button.Name == "SonnerDismiss"; var painted = isClose ? Elements(button).Single(e => e.Name == "Root") : button;
            var foreground = isClose ? Elements(button).OfType<Microsoft.UI.Xaml.Shapes.Shape>().First().Stroke : Elements(button).OfType<TextBlock>().Single(e => e.Name == "SonnerActionContentText").Foreground;
            return new { name = AutomationProperties.GetName(button), box = Box(painted), layoutBox = Box(button), foreground = Paint(foreground), enabled = button.IsEnabled, role = FrameworkElementAutomationPeer.CreatePeerForElement(button).GetAutomationControlType().ToString() };
        }
        object Snapshot(IReadOnlyList<BleeckerSonnerToast> originals)
        {
            var stack = All().Single(e => e.Name == "SonnerNotifications");
            return new { host = new { box = Box(stack), name = AutomationProperties.GetName(stack), live = AutomationProperties.GetLiveSetting(stack).ToString() }, items = Surfaces().Select(s => new
            {
                box = Box(s), opacity = s.Opacity, background = Paint(s.Background), border = Paint(s.BorderBrush), radius = s.CornerRadius.TopLeft, originalRetained = originals.Any(o => ReferenceEquals(o, s.Tag)),
                actionOriginalRetained = ((BleeckerSonnerToast)s.Tag).Action is not { } action || Elements(s).Any(e => ReferenceEquals(e.Tag, action)),
                title = Text(s, "SonnerTitle"), description = ((BleeckerSonnerToast)s.Tag).Description is { Length: > 0 } ? new[] { Text(s, "SonnerDescription") } : [], icon = Box(Elements(s).Single(e => e.Name == "SonnerVariantIcon")),
                buttons = Elements(s).OfType<Button>().OrderBy(b => b.Name == "SonnerDismiss" ? 1 : 0).Select(ButtonSnapshot).ToArray()
            }).ToArray() };
        }
        async Task<string> Capture(string name)
        {
            Guard(); var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root); Guard(); var pixels = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());
            using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream); encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels); await encoder.FlushAsync(); stream.Seek(0);
            using var reader = new DataReader(stream.GetInputStreamAt(0)); await reader.LoadAsync((uint)stream.Size); var bytes = new byte[(int)stream.Size]; reader.ReadBytes(bytes); var file = "sonner-" + name + ".png"; await File.WriteAllBytesAsync(Path.Combine(folder, file), bytes);
            pngs.Add(new { file, width = bitmap.PixelWidth, height = bitmap.PixelHeight, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)) }); return file;
        }
        async Task<BleeckerSonner> Add(ElementTheme theme, double width = 1000, double font = 16, bool rtl = false, BleeckerSonnerPosition position = BleeckerSonnerPosition.BottomRight, double duration = 4000)
        {
            scene.Children.Clear(); await Layout(25); root.Width = width; root.Height = 760; root.Clip = new RectangleGeometry { Rect = new Rect(0, 0, width, 760) }; modal.Clip = new RectangleGeometry { Rect = new Rect(0, 0, width, 760) }; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)width, 760)); root.RequestedTheme = theme;
            var host = new BleeckerSonner { FontSize = font, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, Position = position, Duration = duration }; scene.Children.Add(host); await Layout(55); return host;
        }
        BleeckerSonnerToast Request(JsonElement spec)
        {
            var title = spec.TryGetProperty("title", out var t) ? t.GetString()! : "Fictional notification"; var description = spec.TryGetProperty("description", out var d) ? d.GetString() : null;
            var variant = spec.TryGetProperty("variant", out var v) ? Enum.Parse<BleeckerSonnerVariant>(v.GetString()!, true) : BleeckerSonnerVariant.Default;
            BleeckerSonnerAction? action = null; if (spec.TryGetProperty("hasAction", out var enabled) && enabled.GetBoolean()) action = new BleeckerSonnerAction(spec.TryGetProperty("actionLabel", out var l) ? l.GetString()! : "Continue", new ActionCommand(_ => { }), variant: spec.TryGetProperty("actionVariant", out var a) ? Enum.Parse<BleeckerButtonVariant>(a.GetString()!, true) : BleeckerButtonVariant.Primary);
            return new BleeckerSonnerToast(title) { Description = description, Variant = variant, Duration = 0, Action = action };
        }
        void Contract(string scheme, string id, object? details = null) => contracts.Add(new { scheme, id, passed = true, details });
        try
        {
            await Layout();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                var scheme = theme.ToString();
                var modalVariants = new List<object>();
                foreach (var variant in new[] { BleeckerButtonVariant.Primary, BleeckerButtonVariant.Link })
                {
                    stage = scheme + "-active-modal-" + variant;
                    var background = await Add(theme, duration: 0); var producer = new Border { Tag = true };
                    background.SetBinding(Control.IsEnabledProperty, new Binding { Source = producer, Path = new PropertyPath(nameof(Border.Tag)) });
                    var modalParameter = new object(); var backgroundCalls = 0; var ownedCalls = 0;
                    var backgroundAction = new BleeckerSonnerAction("Original background", new ActionCommand(p => { Require(ReferenceEquals(p, modalParameter), "Background parameter was replaced."); backgroundCalls++; }), modalParameter, variant);
                    var backgroundToast = new BleeckerSonnerToast("Fictional background ownership") { Duration = 0, Action = backgroundAction };
                    var backgroundId = background.Show(backgroundToast); await Layout(260); var backgroundSurface = OriginalSurface(backgroundToast);
                    var backgroundButton = Action(backgroundSurface); var backgroundCommand = backgroundButton.Command!; var backgroundClose = Close(backgroundSurface).Command!;
                    var backgroundProvider = Provider(backgroundButton); var dismissProvider = Provider(Close(backgroundSurface));
                    var owned = new BleeckerSonner { Duration = 0 };
                    var upper = new BleeckerModal { Title = "Fictional nested toast ownership", Content = new TextBlock { Text = "Nested component ownership." } };
                    var originalContent = new StackPanel(); originalContent.Children.Add(new TextBlock { Text = "Original component content." }); originalContent.Children.Add(owned); originalContent.Children.Add(upper);
                    var lower = new BleeckerModal { Title = "Fictional toast ownership", Content = originalContent }; scene.Children.Add(lower); await Layout();
                    Require(root.XamlRoot is { IsHostVisible: false }, "Sonner modal checks require a hidden host."); lower.IsOpen = true; await Layout(260);
                    Require(!backgroundCommand.CanExecute(null) && !backgroundClose.CanExecute(null), $"Background Sonner commands remained eligible (enabled={background.IsEnabled}, binding retained={background.GetBindingExpression(Control.IsEnabledProperty) is not null}, modal open={lower.IsOpen}, owned loaded={owned.IsLoaded}).");
                    var backgroundProviderRejections = (Reject(backgroundProvider) ? 1 : 0) + (Reject(dismissProvider) ? 1 : 0);
                    backgroundCommand.Execute(null); backgroundClose.Execute(null); Require(backgroundCalls == 0 && ReferenceEquals(backgroundSurface, OriginalSurface(backgroundToast)), "Blocked background commands routed or dismissed a toast.");
                    producer.Tag = false; await Layout(); Require(!background.IsEnabled, "Caller enabled binding stopped updating behind the modal."); producer.Tag = true; await Layout();
                    Require(background.IsEnabled && !backgroundCommand.CanExecute(null) && background.GetBindingExpression(Control.IsEnabledProperty) is not null, "Caller enablement bypassed modal ownership or lost its binding.");
                    var ownedAction = new BleeckerSonnerAction("Original owned", new ActionCommand(p => { Require(ReferenceEquals(p, modalParameter), "Owned parameter was replaced."); ownedCalls++; }), modalParameter, variant);
                    var ownedToast = new BleeckerSonnerToast("Fictional owned notification") { Duration = 0, Action = ownedAction }; owned.Show(ownedToast); await Layout(260);
                    var ownedSurface = OriginalSurface(ownedToast); var ownedButton = Action(ownedSurface); var ownedCommand = ownedButton.Command!; var ownedClose = Close(ownedSurface).Command!; var ownedProvider = Provider(ownedButton);
                    Execute(ownedButton); Require(ownedCalls == 1 && backgroundCalls == 0, "Active modal failed to invoke only its original toast action.");
                    upper.IsOpen = true; await Layout(260); Require(!ownedCommand.CanExecute(null) && !ownedClose.CanExecute(null) && !backgroundCommand.CanExecute(null), "Nested modal retained lower/background command eligibility.");
                    var nestedProviderRejections = (Reject(ownedProvider) ? 1 : 0) + (Reject(Provider(Close(ownedSurface))) ? 1 : 0); ownedCommand.Execute(null); ownedClose.Execute(null);
                    upper.IsOpen = false; await Layout(260);
                    Require(ReferenceEquals(ownedSurface, OriginalSurface(ownedToast)) && ownedCommand.CanExecute(null) && !backgroundCommand.CanExecute(null) && ownedCalls == 1, "Nested modal close lost original toast state or released background input.");
                    Execute(ownedButton); Require(ownedCalls == 2, "Restored original toast action did not execute.");
                    lower.IsOpen = false; Require(!ownedCommand.CanExecute(null) && !ownedClose.CanExecute(null), "Closing modal owner left retained toast commands eligible."); var closingProviderRejected = Reject(ownedProvider); ownedCommand.Execute(null); await Layout(260);
                    Require(!ownedSurface.IsLoaded && !ownedCommand.CanExecute(null) && ReferenceEquals(lower.Content, originalContent), "Owner removal retained toast input or replaced original content.");
                    Require(ReferenceEquals(backgroundSurface, OriginalSurface(backgroundToast)) && backgroundCommand.CanExecute(null) && background.IsEnabled && background.GetBindingExpression(Control.IsEnabledProperty) is not null, "Modal close failed to restore original background toast and binding.");
                    Execute(backgroundButton); Require(backgroundCalls == 1 && ownedCalls == 2 && Elements(backgroundSurface).Any(e => ReferenceEquals(e.Tag, backgroundAction)) && ReferenceEquals(ownedToast.Action, ownedAction), "Modal lifecycle replaced original actions or routed an extra callback.");
                    backgroundClose.Execute(null); await Task.Delay(80); Guard(); Require(backgroundSurface.IsLoaded, "Modal checks removed a retained toast early."); Execute(backgroundButton); Require(backgroundCalls == 2, "Modal repair suppressed ordinary retained-exit action behavior."); background.Dismiss(backgroundId);
                    modalVariants.Add(new { variant = variant.ToString(), backgroundProviderRejections, nestedProviderRejections, closingProviderRejected, backgroundCalls, ownedCalls, originalParameterRetained = true, originalToastAndActionRetained = true, nestedOriginalStateRestored = true, callerEnabledBindingRetained = true, retainedActionStillAllowed = true, ownedCommandsRevokedOnRemoval = !ownedCommand.CanExecute(null) });
                }
                Contract(scheme, "active-modal-isolation", new { variants = modalVariants });
                foreach (var row in source.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("scheme").GetString() == scheme && !c.GetProperty("id").GetString()!.EndsWith("focus", StringComparison.Ordinal)))
                {
                    var id = row.GetProperty("id").GetString()!; stage = scheme + "-" + id; var spec = row.GetProperty("spec");
                    var width = spec.TryGetProperty("viewportWidth", out var w) ? w.GetDouble() : 1000; var font = spec.TryGetProperty("rootFontSize", out var f) ? f.GetDouble() : 16; var rtl = spec.TryGetProperty("rtl", out var r) && r.GetBoolean();
                    var position = spec.TryGetProperty("position", out var p) ? Enum.Parse<BleeckerSonnerPosition>(p.GetString()!.Replace("-", ""), true) : BleeckerSonnerPosition.BottomRight;
                    var host = await Add(theme, width, font, rtl, position); var originals = row.GetProperty("toasts").EnumerateArray().Select(Request).ToArray(); var ids = originals.Select(host.Show).ToArray(); Require(ids.All(id => id.Length > 0) && ids.Distinct().Count() == ids.Length, "Toast additions lost opaque IDs.");
                    await Layout(260); await Ready(() => Surfaces().Length == originals.Length && Surfaces().All(s => s.Opacity == 1), "Sonner initial rendering frame or source stacking failed.");
                    if (id.EndsWith("hover", StringComparison.Ordinal) || id.EndsWith("pressed", StringComparison.Ordinal)) { var target = id.StartsWith("action", StringComparison.Ordinal) ? Action(Surfaces().Single()) : Close(Surfaces().Single()); Require(VisualStateManager.GoToState(target, id.EndsWith("hover", StringComparison.Ordinal) ? "PointerOver" : "Pressed", false), "Native template state unavailable."); await Layout(45); }
                    var snapshot = Snapshot(originals); var file = await Capture(id + "-" + scheme); states.Add(new { id, scheme, snapshot, file, forcedTemplateState = row.GetProperty("state").ValueKind != JsonValueKind.Null });
                }
                stage = scheme + "-original-action";
                var current = await Add(theme); var parameter = new object(); var called = 0; var command = new ActionCommand(p => { Require(ReferenceEquals(p, parameter), "Original action parameter was copied."); called++; });
                var originalAction = new BleeckerSonnerAction("Continue", command, parameter); var original = new BleeckerSonnerToast("Original callback") { Duration = 0, Action = originalAction }; var originalId = current.Show(original); await Layout(250);
                var originalSurface = Surfaces().Single(); Require(ReferenceEquals(originalSurface.Tag, original), "Original toast data was not retained."); Execute(Action(originalSurface)); await Layout(); Require(called == 1 && ReferenceEquals(originalSurface, Surfaces().Single()), "Action implicitly dismissed or replaced the toast.");
                command.Allowed = false; command.Notify(); await Layout(); Require(!Action(originalSurface).IsEnabled && !Action(originalSurface).Command!.CanExecute(null), "CanExecute did not disable the native action."); command.Allowed = true; command.Notify(); await Layout(); Execute(Action(originalSurface)); Require(called == 2, "Re-enabled original command did not run."); Contract(scheme, "original-action-command", new { originalIdentity = true, called, denied = true });
                current.Dismiss(originalId); Require(Surfaces().Length == 0, "Imperative dismissal did not remove immediately."); Contract(scheme, "api-dismiss-immediate");
                stage = scheme + "-button-presence"; current.Show(new BleeckerSonnerToast("Retained") { Duration = 0, Action = new BleeckerSonnerAction("Original retained", command, parameter) }); await Layout(250); var retained = Surfaces().Single(); var closeCommand = Close(retained).Command!; closeCommand.Execute(null); await Task.Delay(80); Guard(); Require(retained.IsLoaded, "Dismiss button removed its item early."); Execute(Action(retained)); Require(called == 3, "Transparent retained action was suppressed."); await Ready(() => !retained.IsLoaded && Surfaces().Length == 0, "Dismiss button never removed its retained item."); Require(!closeCommand.CanExecute(null), "Retired command stayed enabled."); presence.Add(new { scheme, retainedAt80 = true, retiredByDeadline = true, retainedActionInvoked = true, oldCommandRevoked = true });
                stage = scheme + "-timeout"; current.Duration = 100; current.Show(new BleeckerSonnerToast("Timed")); await Ready(() => Surfaces().Length == 0, "Default duration did not expire."); Contract(scheme, "default-timeout");
                current.Duration = 90; var persistentId = current.Show(new BleeckerSonnerToast("Explicit persistent") { Duration = 0 }); await Layout(450); Require(Surfaces().Length == 1, "Explicit zero did not override the default."); current.Dismiss(persistentId); Contract(scheme, "explicit-duration-override");
                stage = scheme + "-duration-change"; current.Duration = 140; current.Show(new BleeckerSonnerToast("Old timer")); current.Duration = 0; current.Show(new BleeckerSonnerToast("New persistent")); await Layout(500); Require(Surfaces().Length == 1 && ((BleeckerSonnerToast)Surfaces()[0].Tag).Title == "New persistent", "Default change restarted an old timer or changed a new duration."); Contract(scheme, "live-default-capture");
                stage = scheme + "-global"; current = await Add(theme, duration: 0); var options = new BleeckerSonnerOptions { Description = "Fictional global.", Duration = 0 }; var global = new[] { BleeckerToast.Show(new("default")), BleeckerToast.Success("success", options), BleeckerToast.Error("error", options), BleeckerToast.Warning("warning", options), BleeckerToast.Info("info", options) }; await Layout(250); Require(Surfaces().Length == 5 && global.Distinct().Count() == 5, "Imperative variants did not reach the mounted host."); BleeckerToast.Dismiss(global[1]); Require(Surfaces().Length == 4, "Global dismissal did not use its original ID."); Contract(scheme, "global-variants");
                var first = current; var last = new BleeckerSonner { Duration = 0, Position = BleeckerSonnerPosition.TopLeft }; scene.Children.Add(last); await Layout(); BleeckerToast.Info("last host", options); await Layout(250); Require(Surfaces().Count(s => ((BleeckerSonnerToast)s.Tag).Title == "last host") == 1, "Last host was not registered."); scene.Children.Remove(first); await Layout(); Require(BleeckerToast.Info("not queued", options) == "", "Earlier-host cleanup silently rerouted the global API."); Contract(scheme, "multiple-host-cleanup");
                stage = scheme + "-hidden-disabled"; current = await Add(theme); var disabledCalls = 0; current.Show(new BleeckerSonnerToast("Guarded") { Duration = 0, Action = new BleeckerSonnerAction("Guarded action", new ActionCommand(_ => disabledCalls++)) }); await Layout(250); var guarded = Action(Surfaces().Single()).Command!; current.IsEnabled = false; Require(!guarded.CanExecute(null), "Disabled host accepted a command."); guarded.Execute(null); current.IsEnabled = true; current.Visibility = Visibility.Collapsed; Require(!guarded.CanExecute(null), "Hidden declaration accepted a command."); current.Visibility = Visibility.Visible; scene.Visibility = Visibility.Collapsed; Require(!guarded.CanExecute(null), "Hidden declaration ancestor accepted a command."); guarded.Execute(null); scene.Visibility = Visibility.Visible; await Layout(); Require(disabledCalls == 0, "Denied command invoked the caller."); Contract(scheme, "disabled-hidden-owner-ancestor");
                stage = scheme + "-unload"; var old = Surfaces().Single(); var oldAction = Action(old).Command!; scene.Children.Clear(); await Layout(350); Require(!old.IsLoaded && !oldAction.CanExecute(null) && BleeckerToast.Info("unmounted") == "", "Unload retained a surface, action or global registration."); scene.Children.Add(current); await Layout(); Require(Surfaces().Length == 0, "Reload restored retired toast state."); Contract(scheme, "unload-reload-retirement");
                stage = scheme + "-theme"; current.Show(new BleeckerSonnerToast("Live theme") { Duration = 0 }); await Layout(250); var live = Surfaces().Single(); var beforeColor = ((SolidColorBrush)live.Background).Color; root.RequestedTheme = theme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light; await Ready(() => ((SolidColorBrush)live.Background).Color != beforeColor, "Live ported appearance stayed stale.", diagnostic: () => JsonSerializer.Serialize(new { rootRequested = root.RequestedTheme.ToString(), rootActual = root.ActualTheme.ToString(), ownerRequested = current.RequestedTheme.ToString(), ownerActual = current.ActualTheme.ToString(), surfaceRequested = live.RequestedTheme.ToString(), surfaceActual = live.ActualTheme.ToString(), background = Paint(live.Background) })); Require(ReferenceEquals(live, Surfaces().Single()), "Theme change replaced the toast."); Contract(scheme, "live-theme-retained");
                stage = scheme + "-standalone"; scene.Children.Clear(); root.Children.Clear(); await Layout(); var plain = new BleeckerSonner { Duration = 0 }; root.Children.Add(plain); await Layout(); plain.Show(new("Standalone")); await Layout(250); var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Single(p => p.Child is Canvas c && c.Name == "SonnerPlane"); Require(popup.IsOpen && popup.ShouldConstrainToRootBounds && !popup.IsLightDismissEnabled, "Standalone popup lost its root policy."); root.Children.Clear(); await Layout(350); Require(!popup.IsOpen && popup.Child is null, "Standalone popup survived owner unload."); standalone.Add(new { scheme, rootConstrained = true, unloadedPopupRetired = true }); root.Children.Add(modal); await Layout();
            }
            if (catalogFactory is not null) foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                stage = theme + "-catalog"; scene.Children.Clear(); root.RequestedTheme = theme; var fixture = catalogFactory(); scene.Children.Add(fixture); await Layout(); var host = Elements(fixture).OfType<BleeckerSonner>().Single();
                var id = host.Show(new("Catalog example") { Duration = 0, Action = new BleeckerSonnerAction("Continue", new ActionCommand(_ => { })) }); await Layout(250);
                var surface = Surfaces().Single(); Require(ReferenceEquals(host.SurfaceStyle, surface.Style) && ReferenceEquals(host.DismissButtonStyle, Close(surface).Style) && ReferenceEquals(host.ButtonActionStyle, Elements(surface).OfType<BleeckerButton>().Single().Style), "Catalog surface/dismiss/ordinary action styles did not reach native consumers.");
                host.Dismiss(id); host.Show(new("Catalog link example") { Duration = 0, Action = new BleeckerSonnerAction("Continue", new ActionCommand(_ => { }), variant: BleeckerButtonVariant.Link) }); await Layout(250);
                Require(ReferenceEquals(host.LinkActionStyle, Action(Surfaces().Single()).Style), "Catalog link action style did not reach its native button.");
                catalog.Add(new { scheme = theme.ToString(), stylesApplied = true, publicStylesVerified = 4, file = await Capture("catalog-" + theme) });
            }
            string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-sonner-native-v1", passed = true, states, contracts, presence, standalone, catalog, pngs, guards, focus, activations, processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), publicApisOnly = true, activateCalled = false, showWindowCalled = false, focusCalled = false, hardwareInputSent = false, physicalInputVerified = false, sourceRasterParityVerified = false, nativeComponentAccepted = false, librarySha256 = Hash(typeof(BleeckerSonner).Assembly.Location), hostSha256 = Hash(hostAssembly) }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(0);
        }
        catch (Exception error) { Fail(error); }
    }
}
