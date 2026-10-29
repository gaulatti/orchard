using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

// The public owned menu keeps its native presenter so physical item input reaches
// caller actions; this hidden fixture checks ownership and lifecycle separately.
internal static class ContextMenuModalFixture
{
    private sealed class Command(Action<object?> execute) : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? value) => true;
        public void Execute(object? value) => execute(value);
    }

    private delegate bool Visitor(nint handle, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);

    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    {
        if (node is FrameworkElement element) yield return element;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++)
            foreach (var child in Elements(VisualTreeHelper.GetChild(node, index))) yield return child;
    }

    internal static async Task RunAsync(Window window, string output, string hostPath)
    {
        var folder = Path.GetDirectoryName(output)!;
        var root = new Grid { Width = 900, Height = 760 };
        window.Content = root;
        window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900, 760));
        var resources = new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/Orchard.xaml") };
        var guards = 0;
        var focus = 0;
        var activations = 0;
        var cases = new List<object>();
        var pngs = new List<object>();
        var stage = "initial";
        BleeckerContextMenu? active = null;

        root.GotFocus += (_, _) => focus++;
        window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++;
            GetWindowThreadProcessId(GetForegroundWindow(), out var foreground);
            var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var process); visible |= process == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "Owned Context Menu probe left its hidden boundary.");
        }
        async Task Layout()
        {
            for (var index = 0; index < 8; index++)
            {
                var idle = new TaskCompletionSource();
                Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Could not drain owned Context Menu layout.");
                await idle.Task;
                root.Measure(new Size(root.Width, root.Height));
                root.Arrange(new Rect(0, 0, root.Width, root.Height));
                root.UpdateLayout();
                Guard();
                await Task.Delay(15);
            }
        }
        async Task Capture(string file, FrameworkElement element)
        {
            Guard();
            var bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(element);
            Guard();
            var pixels = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());
            using var stream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels);
            await encoder.FlushAsync();
            stream.Seek(0);
            using var reader = new DataReader(stream.GetInputStreamAt(0));
            await reader.LoadAsync((uint)stream.Size);
            var bytes = new byte[(int)stream.Size];
            reader.ReadBytes(bytes);
            await File.WriteAllBytesAsync(Path.Combine(folder, file), bytes);
            pngs.Add(new { file, width = bitmap.PixelWidth, height = bitmap.PixelHeight, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)) });
            Guard();
        }
        Popup Popup() => VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Single(p => p.Child is MenuFlyoutPresenter);
        MenuFlyoutPresenter Presenter() => (MenuFlyoutPresenter)Popup().Child;
        BleeckerOverlaySurface Surface() => Elements(Presenter()).OfType<BleeckerOverlaySurface>().Single();

        try
        {
            foreach (var scheme in new[] { "Light", "Dark" })
            {
                stage = scheme + "-setup";
                root.RequestedTheme = Enum.Parse<ElementTheme>(scheme);
                var target = new Button { Content = "Fictional context target", Width = 220, Height = 70 };
                var upper = new BleeckerAlertDialog { Title = "Fictional upper component", Description = "The original context items remain owned by this component." };
                upper.CancelRequested += (_, _) => upper.IsOpen = false;
                var content = new StackPanel();
                content.Children.Add(target);
                content.Children.Add(upper);
                var lower = new BleeckerModal { Title = "Fictional component owner", Content = content, IsOpen = true };
                root.Children.Add(new BleeckerModalHost { Content = lower });
                await Layout();

                var enabled = new ToggleSwitch { IsOn = true };
                var menu = new BleeckerContextMenu();
                active = menu;
                target.ContextFlyout = menu;
                var clicks = 0;
                var commands = 0;
                var opens = 0;
                var closes = 0;
                var cancelNextClose = false;
                var openUpper = false;
                var parameter = new object();
                var command = new Command(value => { Require(ReferenceEquals(value, parameter), "Original command parameter changed."); commands++; });
                var action = new MenuFlyoutItem { Text = "Original action", Style = (Style)resources["BleeckerContextMenuItemStyle"], Command = command, CommandParameter = parameter };
                action.SetBinding(Control.IsEnabledProperty, new Binding { Source = enabled, Path = new PropertyPath(nameof(ToggleSwitch.IsOn)) });
                action.Click += (_, _) => { clicks++; if (openUpper) { cancelNextClose = true; upper.IsOpen = true; } };
                var disabled = new MenuFlyoutItem { Text = "Unavailable action", Style = (Style)resources["BleeckerContextMenuItemStyle"], IsEnabled = false };
                var originalContent = new TextBlock { Text = "Original composed action", LineHeight = 19.5, LineStackingStrategy = LineStackingStrategy.BlockLineHeight };
                var composed = new BleeckerMenuContentItem { Text = "Original composed action", Content = originalContent, Style = (Style)resources["BleeckerContextMenuInsetContentItemStyle"] };
                menu.Items.Add(action);
                menu.Items.Add(disabled);
                menu.Items.Add(composed);
                menu.Opened += (_, _) => opens++;
                menu.Closed += (_, _) => closes++;
                menu.Closing += (_, args) => { if (cancelNextClose) { args.Cancel = true; cancelNextClose = false; } };

                async Task Open()
                {
                    ((FlyoutBase)menu).ShowAt(target, new FlyoutShowOptions { Position = new Point(80, 35), ShowMode = FlyoutShowMode.Transient });
                    await Layout();
                    Require(menu.IsOpen && action.IsLoaded && ReferenceEquals(menu.Items[0], action) && ReferenceEquals(composed.Content, originalContent) && ReferenceEquals(action.Command, command) && ReferenceEquals(action.CommandParameter, parameter), "Owned Context Menu lost original native objects.");
                    Require(Popup().Visibility == Visibility.Visible, "Owned native presenter did not open visibly.");
                }
                void Invoke() => ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(action).GetPattern(PatternInterface.Invoke)).Invoke();

                stage = scheme + "-native-originals";
                await Open();
                var popup = Popup();
                var itemPeer = FrameworkElementAutomationPeer.CreatePeerForElement(action);
                Require(itemPeer.GetAutomationControlType() == AutomationControlType.MenuItem && itemPeer.GetName() == action.Text, "Original action lost its native accessible item.");
                Require(Presenter().ActualWidth >= 160 && Surface().ActualWidth == Presenter().ActualWidth, "Native owned menu lost its source size.");
                var initialWidth = Presenter().ActualWidth;
                await Capture($"context-modal-{scheme}-initial.png", Surface());
                cases.Add(new { scheme, name = "native-originals", passed = true, originalObjectsRetained = true, nativeItemAccessibility = true, width = initialWidth, height = Presenter().ActualHeight });

                stage = scheme + "-live-content-theme-and-binding";
                action.Text = "Fictional original action with a substantially wider label";
                await Layout();
                Require(Presenter().ActualWidth > initialWidth + 10, "Native presenter did not reflow changed caller text.");
                enabled.IsOn = false;
                await Layout();
                Require(!action.IsEnabled && action.GetBindingExpression(Control.IsEnabledProperty) is not null, "Caller enabled binding was replaced.");
                enabled.IsOn = true;
                root.RequestedTheme = scheme == "Light" ? ElementTheme.Dark : ElementTheme.Light;
                await Layout();
                Require(Surface().ActualTheme == root.RequestedTheme && ReferenceEquals(Popup(), popup), "Live theme or native presenter identity changed.");
                await Capture($"context-modal-{scheme}-dynamic.png", Surface());
                cases.Add(new { scheme, name = "live-content-theme-and-binding", passed = true, contentWidthGrew = true, enabledBindingRetained = true, liveThemeUpdated = true, originalPresenterRetained = true, width = Presenter().ActualWidth, height = Presenter().ActualHeight });
                root.RequestedTheme = Enum.Parse<ElementTheme>(scheme);
                await Layout();

                stage = scheme + "-caller-content-and-style";
                var replacement = new TextBlock { Text = "Fictional replacement content", Width = 450, LineHeight = 19.5, LineStackingStrategy = LineStackingStrategy.BlockLineHeight };
                composed.Content = replacement;
                await Layout();
                Require(replacement.IsLoaded && !originalContent.IsLoaded && Presenter().ActualWidth >= 450, "Caller content replacement did not reach native layout.");
                composed.Content = originalContent;
                await Layout();
                Require(originalContent.IsLoaded && !replacement.IsLoaded, "Original caller content did not return.");
                var originalStyle = menu.MenuFlyoutPresenterStyle;
                var customStyle = new Style(typeof(MenuFlyoutPresenter)) { BasedOn = originalStyle };
                customStyle.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 480d));
                menu.MenuFlyoutPresenterStyle = customStyle;
                await Layout();
                Require(Presenter().MinWidth == 480 && Presenter().ActualWidth >= 480, "Caller presenter style did not reach the native menu.");
                menu.MenuFlyoutPresenterStyle = originalStyle;
                await Layout();
                Require(ReferenceEquals(Popup(), popup) && Presenter().ActualWidth < 480, "Restoring caller style replaced the native presenter.");
                cases.Add(new { scheme, name = "caller-content-and-style", passed = true, replacementContentMounted = true, originalContentRestored = true, livePresenterStyleApplied = true, styleRestorationReflows = true });

                stage = scheme + "-owner-shield-and-resume";
                openUpper = true;
                Invoke();
                await Layout();
                Require(menu.IsOpen && upper.IsOpen && Popup().Visibility == Visibility.Collapsed && action.IsEnabled && action.GetBindingExpression(Control.IsEnabledProperty) is not null && clicks == 1 && commands == 1 && closes == 0, "Upper modal failed to shield the original menu without changing caller state.");
                Invoke();
                await Layout();
                Require(menu.IsOpen && clicks == 2 && commands == 2, "Direct caller-owned invocation stopped under upper modal.");
                upper.Cancel();
                await Task.Delay(180);
                await Layout();
                Require(Popup().Visibility == Visibility.Visible && ReferenceEquals(Popup(), popup), "Original native menu did not resume after upper cancellation.");
                openUpper = false;
                Invoke();
                await Layout();
                Require(!menu.IsOpen && opens == 1 && closes == 1 && clicks == 3 && commands == 3, "Original action did not dismiss the native menu once.");
                cases.Add(new { scheme, name = "owner-shield-and-resume", passed = true, inactiveMenuHidden = true, callerStateRetained = true, directInvocationRetained = true, originalMenuResumed = true, nativeDismissalVerified = true });

                stage = scheme + "-canceled-hide";
                await Open();
                cancelNextClose = true;
                ((FlyoutBase)menu).Hide();
                await Layout();
                Require(menu.IsOpen && Popup().Visibility == Visibility.Visible, "Canceled native Hide discarded the menu.");
                ((FlyoutBase)menu).Hide();
                await Layout();
                Require(!menu.IsOpen && opens == 2 && closes == 2, "Inherited Hide failed after cancellation.");
                cases.Add(new { scheme, name = "canceled-hide", passed = true, canceledHideRetainsMenu = true, subsequentHideClosesMenu = true });

                stage = scheme + "-owner-teardown-and-remount";
                await Open();
                cancelNextClose = true;
                lower.IsOpen = false;
                await Task.Delay(190);
                await Layout();
                Require(!menu.IsOpen && closes == 3 && !action.IsLoaded && VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0, "Owner teardown retained a native menu after canceled closing.");
                lower.IsOpen = true;
                await Layout();
                cancelNextClose = false;
                await Open();
                Invoke();
                await Layout();
                Require(!menu.IsOpen && opens == 4 && closes == 4 && clicks == 4 && commands == 4 && ReferenceEquals(action.Command, command) && ReferenceEquals(composed.Content, originalContent), "Owner remount lost original actions or lifecycle.");
                cases.Add(new { scheme, name = "owner-teardown-and-remount", passed = true, canceledMenuRetired = true, originalActionsSurviveRemount = true, opens, closes, clicks, commands });
                root.Children.Clear();
                active = null;
                await Layout();
            }

            static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-context-menu-modal-native-v2", passed = true, cases, pngs, guards, focus, activations, publicApisOnly = true, hardwareInputSent = false, physicalInputVerified = false, nativeFocusVerified = false, nativeComponentAccepted = false, librarySha256 = Hash(typeof(BleeckerContextMenu).Assembly.Location), hostSha256 = Hash(hostPath) }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close();
            Environment.Exit(0);
        }
        catch (Exception error)
        {
            active?.Hide();
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-context-menu-modal-native-v2", passed = false, stage, error = error.ToString(), cases, guards, focus, activations }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close();
            Environment.Exit(1);
        }
    }
}
