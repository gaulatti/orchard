using System.ComponentModel;
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
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class ButtonLifecycleFixture
{
    private sealed class Model : INotifyPropertyChanged
    {
        public string Text { get; private set; } = "Fictional action";
        public bool Loading { get; private set; }
        public bool Enabled { get; private set; } = true;
        public event PropertyChangedEventHandler? PropertyChanged;
        internal void Change(string text, bool loading, bool enabled = true)
        {
            Text = text; Loading = loading; Enabled = enabled;
            foreach (var property in new[] { nameof(Text), nameof(Loading), nameof(Enabled) })
                PropertyChanged?.Invoke(this, new(property));
        }
    }
    private sealed class Command : ICommand
    {
        internal int Executions;
        public bool CanExecute(object? value) => true;
        public void Execute(object? value) { if (!Equals(value, "fictional-parameter")) throw new InvalidOperationException("Command parameter changed."); Executions++; }
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
    private delegate bool Visitor(nint handle, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    {
        if (node is FrameworkElement element) yield return element;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            foreach (var child in Elements(VisualTreeHelper.GetChild(node, i))) yield return child;
    }
    private static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static IInvokeProvider Provider(Button button) => (IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke);

    internal static async Task RunAsync(Window window, string output, string hostAssembly)
    {
        var root = new Grid { Width = 700, Height = 400 };
        window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(700, 400));
        var checks = new List<object>(); var observations = new List<object>(); var failures = new List<string>(); var images = new List<object>();
        var guards = 0; var focus = 0; var activations = 0;
        root.GotFocus += (_, _) => focus++;
        window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var process); visible |= process == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            if (visible || window.Visible || window.AppWindow.IsVisible || foreground == Environment.ProcessId || focus != 0 || activations != 0)
                throw new InvalidOperationException("Button lifecycle fixture left the hidden boundary.");
        }
        async Task Layout()
        {
            for (var i = 0; i < 3; i++)
            {
                var idle = new TaskCompletionSource();
                if (!root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult())) throw new InvalidOperationException("Dispatcher refused layout.");
                await idle.Task; root.Measure(new Size(700, 400)); root.Arrange(new Rect(0, 0, 700, 400)); root.UpdateLayout(); Guard();
            }
            await NativeRenderReadiness.RenderAsync(root); Guard();
            if (root.Children.OfType<FrameworkElement>().Any(child => !child.IsLoaded)) throw new InvalidOperationException("Rendered button fixture child did not load.");
        }
        void Check(string id, bool passed, object observed)
        {
            checks.Add(new { id, passed, observed }); if (!passed) failures.Add(id);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "checks-progress.json"), JsonSerializer.Serialize(new { checks, failures }));
        }
        async Task Capture(string id, FrameworkElement element)
        {
            Guard(); var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(element); Guard();
            var pixels = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());
            if (bitmap.PixelWidth < 1 || bitmap.PixelHeight < 1 || !pixels.Any(value => value != 0)) throw new InvalidOperationException("Button capture is empty.");
            using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels);
            await encoder.FlushAsync(); stream.Seek(0); using var reader = new DataReader(stream.GetInputStreamAt(0)); await reader.LoadAsync((uint)stream.Size);
            var bytes = new byte[(int)stream.Size]; reader.ReadBytes(bytes); var file = id + ".png"; await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, file), bytes);
            images.Add(new { file, width = bitmap.PixelWidth, height = bitmap.PixelHeight, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)) });
        }
        try
        {
            await Layout();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var variant in Enum.GetValues<BleeckerButtonVariant>())
            foreach (var custom in new[] { false, true })
            {
                root.RequestedTheme = theme; var id = theme + "/" + variant + "/" + (custom ? "custom" : "default");
                var model = new Model(); var command = new Command();
                var owner = new BleeckerButton { Variant = variant, Command = command, CommandParameter = "fictional-parameter", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(20) };
                var contentBinding = new Binding { Source = model, Path = new PropertyPath(nameof(Model.Text)) };
                var loadingBinding = new Binding { Source = model, Path = new PropertyPath(nameof(Model.Loading)) };
                var enabledBinding = new Binding { Source = model, Path = new PropertyPath(nameof(Model.Enabled)) };
                owner.SetBinding(ContentControl.ContentProperty, contentBinding); owner.SetBinding(BleeckerButton.IsLoadingProperty, loadingBinding); owner.SetBinding(Control.IsEnabledProperty, enabledBinding);
                if (custom) owner.Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:o='using:Orchard' TargetType='o:BleeckerButton'><Button x:Name='PART_Action' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Command='{TemplateBinding Command}' CommandParameter='{TemplateBinding CommandParameter}'><StackPanel Orientation='Horizontal'><ProgressRing x:Name='PART_Progress' Width='16' Height='16'/><ContentPresenter Content='{TemplateBinding Content}'/></StackPanel></Button></ControlTemplate>");
                var clicks = 0; owner.Click += (_, _) => clicks++;
                var templateTransitions = new List<object>();
                owner.RegisterPropertyChangedCallback(Control.TemplateProperty, (_, _) =>
                {
                    templateTransitions.Add(new { templateIsNull = owner.Template is null, localTemplateIsNull = owner.ReadLocalValue(Control.TemplateProperty) is null, localTemplateType = owner.ReadLocalValue(Control.TemplateProperty)?.GetType().FullName, owner.IsLoaded, children = VisualTreeHelper.GetChildrenCount(owner) });
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!, "template-transitions.json"), JsonSerializer.Serialize(new { id, templateTransitions }));
                });
                root.Children.Add(owner); await Layout();
                var action = Elements(owner).OfType<Button>().Single(element => element.Name == "PART_Action");
                var progress = Elements(owner).OfType<ProgressRing>().Single(element => element.Name == "PART_Progress");
                var originalTemplate = owner.Template;
                var readyFocusEligible = ReferenceEquals(FocusManager.FindFirstFocusableElement(owner), action);
                Check(id + "/mounted", owner.IsLoaded && action.IsLoaded && progress.IsLoaded && !owner.IsLoading && action.IsEnabled && readyFocusEligible, new { owner.IsLoaded, actionLoaded = action.IsLoaded, progressLoaded = progress.IsLoaded, readyFocusEligible });
                Provider(action).Invoke(); await Layout();
                Check(id + "/activation", clicks == 1 && command.Executions == 1, new { clicks, command.Executions });
                await Capture(id.Replace('/', '-') + "-initial", owner);
                await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(output)!, "stage.json"), JsonSerializer.Serialize(new { id, stage = "before-clear", checks, failures }));
                owner.Template = null;
                await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(output)!, "stage.json"), JsonSerializer.Serialize(new { id, stage = "after-clear-before-layout", templateIsNull = owner.Template is null, localTemplateIsNull = owner.ReadLocalValue(Control.TemplateProperty) is null, localTemplateType = owner.ReadLocalValue(Control.TemplateProperty)?.GetType().FullName, checks, failures }));
                await Layout();
                Check(id + "/cleared", owner.IsLoaded && !action.IsLoaded && !progress.IsLoaded, new { owner.IsLoaded, actionLoaded = action.IsLoaded, progressLoaded = progress.IsLoaded });
                var oldStyle = action.Style; var oldHeight = action.MinHeight; var oldFont = action.FontSize; var oldPadding = action.Padding; var oldName = AutomationProperties.GetName(action); var oldActive = progress.IsActive; var oldVisibility = progress.Visibility;
                owner.Size = BleeckerButtonSize.Large; owner.Variant = variant == BleeckerButtonVariant.Link ? BleeckerButtonVariant.Primary : BleeckerButtonVariant.Link;
                AutomationProperties.SetName(owner, "Updated caller name"); model.Change("Updated fictional action", true); await Layout();
                Check(id + "/retired-presentation", ReferenceEquals(action.Style, oldStyle) && action.MinHeight == oldHeight && action.FontSize == oldFont && action.Padding == oldPadding,
                    new { styleRetained = ReferenceEquals(action.Style, oldStyle), oldHeight, height = action.MinHeight, oldFont, font = action.FontSize, oldPadding = oldPadding.ToString(), padding = action.Padding.ToString() });
                Check(id + "/retired-metadata", AutomationProperties.GetName(action) == oldName, new { oldName, name = AutomationProperties.GetName(action) });
                Check(id + "/retired-progress", progress.IsActive == oldActive && progress.Visibility == oldVisibility, new { oldActive, active = progress.IsActive, oldVisibility = oldVisibility.ToString(), visibility = progress.Visibility.ToString() });
                await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(output)!, "stage.json"), JsonSerializer.Serialize(new { id, stage = "before-restore", checks, failures }));
                owner.Template = originalTemplate;
                await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(output)!, "stage.json"), JsonSerializer.Serialize(new { id, stage = "after-restore-before-layout", checks, failures }));
                await Layout();
                var restored = Elements(owner).OfType<Button>().Single(element => element.Name == "PART_Action");
                var restoredProgress = Elements(owner).OfType<ProgressRing>().Single(element => element.Name == "PART_Progress");
                var loadingFocusAbsent = FocusManager.FindFirstFocusableElement(owner) is null;
                Check(id + "/restored-loading", !ReferenceEquals(restored, action) && !ReferenceEquals(restoredProgress, progress) && restored.IsLoaded && !restored.IsEnabled && restoredProgress.IsActive && restoredProgress.Visibility == Visibility.Visible && AutomationProperties.GetName(restored) == "Updated caller name" && loadingFocusAbsent,
                    new { newAction = !ReferenceEquals(restored, action), newProgress = !ReferenceEquals(restoredProgress, progress), restored.IsLoaded, restored.IsEnabled, restoredProgress.IsActive, name = AutomationProperties.GetName(restored), loadingFocusAbsent });
                var contentBindingRetained = ReferenceEquals(owner.GetBindingExpression(ContentControl.ContentProperty)?.ParentBinding, contentBinding);
                var loadingBindingRetained = ReferenceEquals(owner.GetBindingExpression(BleeckerButton.IsLoadingProperty)?.ParentBinding, loadingBinding);
                var enabledBindingRetained = ReferenceEquals(owner.GetBindingExpression(Control.IsEnabledProperty)?.ParentBinding, enabledBinding);
                Check(id + "/bindings", contentBindingRetained && loadingBindingRetained && enabledBindingRetained, new { content = owner.Content, owner.IsLoading, owner.IsEnabled, contentBindingRetained, loadingBindingRetained, enabledBindingRetained });
                owner.Size = BleeckerButtonSize.Medium; owner.Variant = variant; AutomationProperties.SetName(owner, ""); model.Change("Fictional action", false); await Layout();
                Provider(restored).Invoke(); await Layout();
                var restoredFocusEligible = ReferenceEquals(FocusManager.FindFirstFocusableElement(owner), restored);
                Check(id + "/restored-activation", clicks == 2 && command.Executions == 2 && restoredFocusEligible, new { clicks, command.Executions, restoredFocusEligible });
                // Native retired-part activation is observed, not assigned a new library policy.
                string? retiredInvokeError = null; var beforeClicks = clicks; var beforeCommands = command.Executions;
                try { Provider(action).Invoke(); } catch (Exception error) { retiredInvokeError = error.GetType().FullName; }
                await Layout(); var retiredCommandDelta = command.Executions - beforeCommands;
                observations.Add(new { id = id + "/retired-native-invoke", retiredInvokeError, clickDelta = clicks - beforeClicks, commandDelta = retiredCommandDelta });
                Check(id + "/retired-event-detached", clicks == beforeClicks, new { beforeClicks, clicks });
                await Capture(id.Replace('/', '-') + "-restored", owner);
                model.Change("Fictional action", false, false); await Layout();
                var disabledFocusAbsent = FocusManager.FindFirstFocusableElement(owner) is null;
                Check(id + "/caller-disabled", !owner.IsEnabled && !restored.IsEnabled && disabledFocusAbsent, new { owner.IsEnabled, actionEnabled = restored.IsEnabled, disabledFocusAbsent });
                model.Change("Fictional action", false); root.Children.Clear(); await Layout();
                Check(id + "/unloaded", !owner.IsLoaded && !restored.IsLoaded, new { owner.IsLoaded, actionLoaded = restored.IsLoaded });
                root.Children.Add(owner); await Layout(); Provider(restored).Invoke(); await Layout();
                Check(id + "/remounted-activation", clicks == beforeClicks + 1 && command.Executions == beforeCommands + retiredCommandDelta + 1, new { clicks, command.Executions });
                model.Change("Fictional action", true); await Layout();
                var rotation = Elements(restoredProgress).OfType<Microsoft.UI.Xaml.Shapes.Path>().SingleOrDefault(element => element.Name == "PART_SpinnerPath")?.RenderTransform as RotateTransform;
                owner.Template = null; await Layout(); await Task.Delay(80); await Layout();
                Check(id + "/loading-retirement", !restored.IsLoaded && !restoredProgress.IsLoaded && owner.IsLoading && owner.IsEnabled && (custom || rotation is not null && rotation.Angle == 0), new { actionLoaded = restored.IsLoaded, progressLoaded = restoredProgress.IsLoaded, owner.IsLoading, owner.IsEnabled, customProgress = custom, ownedRotationPresent = rotation is not null, angle = rotation?.Angle });
                owner.ClearValue(Control.TemplateProperty); await Layout();
                var resumed = Elements(owner).OfType<Button>().Single(element => element.Name == "PART_Action");
                var resumedProgress = Elements(owner).OfType<ProgressRing>().Single(element => element.Name == "PART_Progress");
                Check(id + "/default-resumed", !ReferenceEquals(resumed, restored) && resumed.IsLoaded && !resumed.IsEnabled && resumedProgress.IsActive && owner.Template is not null && ReferenceEquals(owner.GetBindingExpression(BleeckerButton.IsLoadingProperty)?.ParentBinding, loadingBinding), new { newAction = !ReferenceEquals(resumed, restored), resumed.IsLoaded, resumed.IsEnabled, resumedProgress.IsActive, loadingBindingRetained = ReferenceEquals(owner.GetBindingExpression(BleeckerButton.IsLoadingProperty)?.ParentBinding, loadingBinding) });
                model.Change("Fictional action", false); await Layout(); Provider(resumed).Invoke(); await Layout();
                Check(id + "/default-resumed-activation", clicks == beforeClicks + 2 && command.Executions == beforeCommands + retiredCommandDelta + 2, new { clicks, command.Executions });
                await Capture(id.Replace('/', '-') + "-default-resumed", owner);
                root.Children.Clear(); await Layout();
            }
            Guard();
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-button-lifecycle-native-v2", passed = failures.Count == 0, checks, observations, failures, images, guards, focus, activations, activateCalled = false, showWindowCalled = false, focusCalled = false, hardwareInputSent = false, settingsChanged = false, nativeReadinessAsserted = true, renderOwnerLifetimeControlled = true, nativeComponentAccepted = false, librarySha256 = Hash(typeof(BleeckerButton).Assembly.Location), hostSha256 = Hash(hostAssembly) }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close(); Environment.Exit(failures.Count == 0 ? 0 : 1);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-button-lifecycle-native-v2", passed = false, error = error.ToString(), checks, observations, failures, guards, focus, activations }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close(); Environment.Exit(1);
        }
    }
}
