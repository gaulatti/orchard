using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Orchard;
using Windows.Foundation;
using Windows.Graphics;

namespace PackageConsumer;

public partial class App
{
    private sealed class PackageNavItem(string href, string label, bool external, string marker) : BleeckerNavItem(href, label, external)
    {
        internal string Marker { get; } = marker;
    }
    private sealed class PackageNavCommand : ICommand
    {
        internal bool Enabled = true;
        internal int Calls;
        internal object? Parameter;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => Enabled;
        public void Execute(object? parameter) { Calls++; Parameter = parameter; }
        internal void SetEnabled(bool enabled) { Enabled = enabled; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    }
    private async void RunHiddenNavMenusAsync(string referenceDirectory)
    {
        var arguments = Environment.GetCommandLineArgs(); var output = arguments[Array.IndexOf(arguments, "--verify-output") + 1];
        nint handle = 0; var frames = new List<object>(); var checks = new List<object>(); var failures = new List<string>();
        var completed = false; var guards = 0; var activationEvents = 0; var focusEvents = 0;
        string? failure = null, isolationFailure = null, referenceHash = null;
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground);
            if (isolationFailure is not null || window!.Visible || window.AppWindow.IsVisible || IsWindowVisible(handle) || foreground == Environment.ProcessId || focusEvents != 0)
                throw new InvalidOperationException(isolationFailure ?? "Hidden Nav Menu became visible, focused or owned foreground input.");
        }
        static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        void Check(string id, Action action)
        {
            Guard(); try { action(); checks.Add(new { id, passed = true }); }
            catch (Exception error) { failures.Add(id + ": " + error.Message); checks.Add(new { id, passed = false, error = error.Message }); } Guard();
        }
        static HyperlinkButton[] Links(BleeckerNavMenu control) => HiddenDesc<HyperlinkButton>(control).Where(link => link.Name is "NavLink" or "CustomNavLink").ToArray();
        static BleeckerNavLinkContext Context(HyperlinkButton link) => (BleeckerNavLinkContext)link.DataContext;
        static object Paint(Brush brush)
        {
            var solid = brush as SolidColorBrush ?? throw new InvalidOperationException("Nav Menu paint must be solid.");
            return new { color = solid.Color.ToString(), opacity = solid.Opacity };
        }
        static BleeckerNavMenu Create(JsonElement spec)
        {
            var control = new BleeckerNavMenu
            {
                Width = spec.GetProperty("width").GetDouble(), Direction = Enum.Parse<BleeckerNavDirection>(spec.GetProperty("direction").GetString()!, true),
                FlowDirection = spec.TryGetProperty("rtl", out var rtl) && rtl.GetBoolean() ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                Items = spec.GetProperty("items").EnumerateArray().Select(item => new PackageNavItem(item.GetProperty("href").GetString()!, item.GetProperty("label").GetString()!, item.TryGetProperty("external", out var external) && external.GetBoolean(), item.GetProperty("marker").GetString()!)).ToArray()
            };
            if (spec.TryGetProperty("custom", out var custom) && custom.GetBoolean()) control.LinkTemplate = (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><HyperlinkButton x:Name='CustomNavLink' Content='{Binding Content}' Style='{Binding Style}' Command='{Binding Command}' Foreground='{Binding Foreground}' AutomationProperties.Name='{Binding Label}'/></DataTemplate>");
            return control;
        }
        try
        {
            window = new Window(); handle = WinRT.Interop.WindowNative.GetWindowHandle(window); Guard();
            window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) { activationEvents++; isolationFailure = "Hidden Nav Menu window activated."; } };
            window.VisibilityChanged += (_, args) => { if (args.Visible) isolationFailure = "Hidden Nav Menu window became visible."; };
            var root = new Canvas { Width = 1280, Height = 900, UseLayoutRounding = false }; root.GotFocus += (_, _) => focusEvents++; window.Content = root; window.AppWindow.ResizeClient(new SizeInt32(1280, 900));
            async Task Layout() { for (var index = 0; index < 3; index++) { root.Measure(new Size(1280, 900)); root.Arrange(new Rect(0, 0, 1280, 900)); root.UpdateLayout(); Guard(); await Task.Delay(10); } }
            object Box(FrameworkElement element) { var box = element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight)); return new { x = box.X, y = box.Y, width = box.Width, height = box.Height }; }
            void Mount(BleeckerNavMenu control) { Canvas.SetLeft(control, 20); Canvas.SetTop(control, 20); root.Children.Add(control); }
            await Layout(); Assert(root.XamlRoot is not null && root.XamlRoot.RasterizationScale == 1, "Hidden Nav Menu requires the actual 100% root.");
            var bytes = await File.ReadAllBytesAsync(Path.Combine(referenceDirectory, "NavMenu.json")); referenceHash = Convert.ToHexStringLower(SHA256.HashData(bytes)); using var reference = JsonDocument.Parse(bytes);
            Assert(reference.RootElement.GetProperty("sourceSha256").GetString() == "1a9b3d9c7c7d43c52d23080c37af477211d37ed457f1c6a8eb2a18269b2f8cc5", "Unexpected Nav Menu source.");
            foreach (var source in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                var spec = source.GetProperty("spec"); root.RequestedTheme = Enum.Parse<ElementTheme>(source.GetProperty("scheme").GetString()!); var control = Create(spec); var original = control.Items.ToArray(); Mount(control); await Layout();
                var normalPaint = !spec.TryGetProperty("state", out _);
                var links = Links(control).Select((link, index) =>
                {
                    var context = Context(link); var text = HiddenDesc<TextBlock>(link).Single(node => node.Name == "NavLinkTextText");
                    return new { box = Box(link), label = context.Label, name = AutomationProperties.GetName(link), href = context.Href, external = context.External, marker = ((PackageNavItem)context.Item).Marker, originalItem = ReferenceEquals(context.Item, original[index]), role = FrameworkElementAutomationPeer.CreatePeerForElement(link).GetAutomationControlType().ToString(), fontFamily = text.FontFamily.Source, fontSize = text.FontSize, fontWeight = text.FontWeight.Weight, lineHeight = text.LineHeight, characterSpacing = text.CharacterSpacing, paint = normalPaint ? Paint(text.Foreground) : null };
                }).ToArray();
                frames.Add(new { id = source.GetProperty("id").GetString(), scheme = source.GetProperty("scheme").GetString(), host = Box(control), root = Box(HiddenDesc<Grid>(control).Single(node => node.Name == "PART_Items")), normalPaint, links }); root.Children.Remove(control); await Layout();
            }
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                root.RequestedTheme = theme; var first = new PackageNavItem("../raw?q=1#two", "Original", false, "first"); var second = new PackageNavItem("#two", "Second", true, "second"); var items = new ObservableCollection<BleeckerNavItem> { first, second }; var producer = new TextBlock { Tag = items }; var command = new PackageNavCommand(); var parameter = new object(); var requests = new List<BleeckerNavItem>();
                var control = new BleeckerNavMenu { Width = 900, Command = command, CommandParameter = parameter }; control.SetBinding(BleeckerNavMenu.ItemsProperty, new Binding { Source = producer, Path = new PropertyPath("Tag"), Mode = BindingMode.OneWay }); control.ItemInvoked += (_, item) => requests.Add(item); Mount(control); await Layout();
                var context = Context(Links(control)[0]); var content = context.Content;
                Check(theme + "-binding-original", () => Assert(ReferenceEquals(control.Items, items) && ReferenceEquals(context.Item, first) && control.GetBindingExpression(BleeckerNavMenu.ItemsProperty) is not null, "Package lost caller ownership or binding."));
                items.Move(0, 1); await Layout();
                Check(theme + "-reorder", () => Assert(ReferenceEquals(context, Context(Links(control)[1])) && ReferenceEquals(content, context.Content) && ReferenceEquals(context.Item, first), "Package rebuilt a stable keyed link."));
                var replacement = new PackageNavItem(first.Href, first.Label, true, "replacement"); items[1] = replacement; await Layout(); first.Label = "Retired mutation"; await Layout();
                Check(theme + "-same-key-original", () => Assert(ReferenceEquals(context, Context(Links(control)[1])) && ReferenceEquals(context.Item, replacement) && context.Label == "Original" && context.External, "Package copied or retained a retired original item."));
                context.Command.Execute(null);
                Check(theme + "-command-original", () => Assert(command.Calls == 1 && ReferenceEquals(command.Parameter, parameter) && requests.Count == 1 && ReferenceEquals(requests[0], replacement), "Package changed command parameters or original invocation identity."));
                command.SetEnabled(false); context.Command.Execute(null); command.SetEnabled(true); control.IsEnabled = false; context.Command.Execute(null); control.IsEnabled = true; control.Visibility = Visibility.Collapsed; context.Command.Execute(null); control.Visibility = Visibility.Visible; await Layout();
                Check(theme + "-inactive", () => Assert(command.Calls == 1 && requests.Count == 1, "Inactive package link invoked."));
                replacement.Label = "New key"; await Layout(); context.Command.Execute(null); var current = Context(Links(control)[1]);
                Check(theme + "-changed-key-stale", () => Assert(!ReferenceEquals(context, current) && ReferenceEquals(current.Item, replacement) && requests.Count == 1, "Package kept a changed-key command live."));
                var template = control.Template; control.Template = null; control.Template = template; await Layout(); current.Command.Execute(null);
                Check(theme + "-template-binding", () => Assert(control.GetBindingExpression(BleeckerNavMenu.ItemsProperty) is not null && ReferenceEquals(control.Items, items) && requests.Count == 1, "Package lost a binding or retained a retired template command."));
                var detached = Context(Links(control)[1]); root.Children.Remove(control); await Layout(); detached.Command.Execute(null);
                Check(theme + "-detached", () => Assert(command.Calls == 1 && requests.Count == 1, "Detached package link invoked."));
            }
            Assert(frames.Count == 42 && checks.Count == 16, "Incomplete hidden Nav Menu matrix."); completed = failures.Count == 0;
        }
        catch (Exception error) { failure = error.ToString(); }
        finally
        {
            var visible = window?.Visible ?? false; var nativeVisible = handle != 0 && IsWindowVisible(handle); GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var isolated = !visible && !nativeVisible && activationEvents == 0 && focusEvents == 0 && isolationFailure is null && foreground != Environment.ProcessId;
            var librarySha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(BleeckerNavMenu).Assembly.Location)));
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = completed && isolated, scope = "hidden-nav-menu-contracts", hiddenWindow = true, activateCalled = false, showCalled = false, focusCalled = false, inputSent = false, visible, nativeVisible, activationEvents, focusEvents, guardChecks = guards, isolated, processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), referenceSha256 = referenceHash, librarySha256, frames, checks, failures, failure, isolationFailure, visibleRuntimeVerified = false, sourceRasterParityVerified = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true })); window?.Close(); Environment.Exit(completed && isolated ? 0 : 1);
        }
    }
}
