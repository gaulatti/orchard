using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Catalog;

public partial class App
{
    private sealed class FooterBackgroundCommand : ICommand
    {
        private volatile bool enabled = true;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => enabled;
        public void Execute(object? parameter) { }
        internal void SetEnabled(bool value) { enabled = value; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    }
    private async void RunHiddenFootersAsync(string folder)
    {
        Directory.CreateDirectory(folder);
        var frames = new List<object>(); var checks = new List<object>(); var guards = 0; var activationEvents = 0; var focusEvents = 0; nint handle = 0;
        var completed = false; var recordingException = false; var exceptionCount = 0;
        string? failure = null, isolationFailure = null, referenceHash = null;
        void Guard()
        {
            guards++; WorkspaceWindowThread(WorkspaceForeground(), out var foreground);
            if (isolationFailure is not null || _window!.Visible || _window.AppWindow.IsVisible || WorkspaceVisible(handle) || foreground == Environment.ProcessId || focusEvents != 0)
                throw new InvalidOperationException(isolationFailure ?? "Hidden Footer window became visible, focused or owned foreground input.");
        }
        void FirstChance(object? sender, FirstChanceExceptionEventArgs args)
        {
            if (recordingException || exceptionCount >= 8) return;
            var stack = args.Exception.StackTrace;
            if (stack is null || !stack.Contains("BleeckerFooter", StringComparison.Ordinal)) return;
            recordingException = true;
            try { exceptionCount++; File.AppendAllText(Path.Combine(folder, "footer-exceptions.txt"), args.Exception.GetType().FullName + ": " + args.Exception.Message + "\n" + stack + "\n\n"); }
            finally { recordingException = false; }
        }
        AppDomain.CurrentDomain.FirstChanceException += FirstChance;
        try
        {
            _window = new Window(); handle = WinRT.Interop.WindowNative.GetWindowHandle(_window); Guard();
            _window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) { activationEvents++; isolationFailure = "Hidden Footer activated."; } };
            _window.VisibilityChanged += (_, args) => { if (args.Visible) isolationFailure = "Hidden Footer became visible."; };
            var root = new Canvas { Width = 1280, Height = 900, UseLayoutRounding = false }; root.GotFocus += (_, _) => focusEvents++; _window.Content = root;
            async Task Layout()
            {
                for (var index = 0; index < 4; index++) { root.Measure(new Size(root.Width, root.Height)); root.Arrange(new Rect(0, 0, root.Width, root.Height)); root.UpdateLayout(); Guard(); await Task.Delay(10); }
                if (root.XamlRoot is null || root.XamlRoot.IsHostVisible || root.XamlRoot.RasterizationScale != 1) throw new InvalidOperationException("Hidden Footer root is missing, visible or scaled.");
            }
            object Box(FrameworkElement element) { var box = element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight)); return new { x = box.X, y = box.Y, width = box.Width, height = box.Height }; }
            static object Paint(Brush brush) { var solid = brush as SolidColorBrush ?? throw new InvalidOperationException("Expected a solid Footer source brush."); return new { color = solid.Color.ToString(), opacity = solid.Opacity }; }
            static IEnumerable<T> Nodes<T>(DependencyObject owner) where T : FrameworkElement { for (var index = 0; index < VisualTreeHelper.GetChildrenCount(owner); index++) { var child = VisualTreeHelper.GetChild(owner, index); if (child is T target) yield return target; foreach (var nested in Nodes<T>(child)) yield return nested; } }
            using var stream = CatalogReferenceResources.Open(typeof(App).Assembly, "Orchard.Catalog.Reference.Footer.json") ?? throw new InvalidOperationException("Footer reference missing.");
            using var bytes = new MemoryStream(); await stream.CopyToAsync(bytes); referenceHash = Convert.ToHexStringLower(SHA256.HashData(bytes.ToArray())); using var reference = JsonDocument.Parse(bytes.ToArray());
            await Layout();
            for (var cycle = 0; cycle < 3; cycle++) foreach (var source in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                var spec = source.GetProperty("spec"); root.Width = spec.GetProperty("viewportWidth").GetDouble(); _window.AppWindow.ResizeClient(new SizeInt32((int)root.Width, 900)); root.RequestedTheme = Enum.Parse<ElementTheme>(source.GetProperty("scheme").GetString()!);
                File.AppendAllText(Path.Combine(folder, "hidden-footer-stages.jsonl"), JsonSerializer.Serialize(new { cycle, id = source.GetProperty("id").GetString(), scheme = source.GetProperty("scheme").GetString(), stage = "mount" }) + "\n");
                var footer = FooterReferenceFactory.Create(source.TryGetProperty("initialSpec", out var initialSpec) ? initialSpec : spec); root.Children.Add(footer); await Layout(); var mutationsApplied = 0; if (spec.TryGetProperty("mutations", out var mutations)) foreach (var mutation in mutations.EnumerateArray()) { FooterReferenceFactory.Mutate(footer, mutation); mutationsApplied++; await Layout(); }
                FrameworkElement Part(string name) => Nodes<FrameworkElement>(footer).Single(node => node.Name == name);
                var brand = Part("FooterBrandContent"); var original = footer.Sections.SelectMany(section => section.Items).ToArray(); var sectionFrames = Nodes<Panel>(footer).Where(node => node.Name == "FooterSection").Select(section => new { title = Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(Nodes<FrameworkElement>(section).Single(node => node.Name == "FooterSectionHeading")), box = Box(section), heading = Box(Nodes<FrameworkElement>(section).Single(node => node.Name == "FooterSectionHeading")) }).ToArray();
                var links = Nodes<HyperlinkButton>(footer).Where(link => link.Name is "FooterBrandLink" or "FooterSourceLink" or "FooterSectionLink" or "CustomFooterLink").Select(link =>
                {
                    var context = (BleeckerNavLinkContext)link.DataContext; var text = Nodes<TextBlock>(link).FirstOrDefault(text => text.Name is "FooterLinkTextText" or "FooterSourceTextText");
                    return new { href = context.Href, label = context.Label, originalItem = original.Any(item => ReferenceEquals(context.Item, item)), marker = (context.Item as FooterReferenceItem)?.Marker, box = Box(link), paint = text is null ? null : Paint(text.Foreground) };
                }).ToArray();
                frames.Add(new { cycle, mutationsApplied, id = source.GetProperty("id").GetString(), scheme = source.GetProperty("scheme").GetString(), footer = Box(footer), container = Box(Part("FooterContainer")), grid = Box(Part("FooterGrid")), brand = Box(Part("FooterBrandColumn")), preparedBrand = Box(brand), description = string.IsNullOrEmpty(footer.Brand.Description) ? null : Box(Part("FooterDescription")), bottom = Box(Part("FooterBottom")), bottomLeft = Box(Part("FooterBottomLeft")), bottomRight = Box(Part("FooterBottomRight")), sections = sectionFrames, links, paint = new { footer = Paint(footer.Background), topRule = Paint(((Border)Part("FooterTopRule")).Background), bottomRule = Paint(((Border)Part("FooterBottomRule")).Background), accent = Paint(((Border)Part("FooterAccent")).Background) } });
                await File.WriteAllTextAsync(Path.Combine(folder, "hidden-footer-partial.json"), JsonSerializer.Serialize(new { diagnosticOnly = true, complete = false, frames, guards, activationEvents, focusEvents }));
                root.Children.Remove(footer); await Layout(); Guard();
                File.AppendAllText(Path.Combine(folder, "hidden-footer-stages.jsonl"), JsonSerializer.Serialize(new { cycle, id = source.GetProperty("id").GetString(), scheme = source.GetProperty("scheme").GetString(), stage = "unmount" }) + "\n");
            }
            root.Width = 1280; _window.AppWindow.ResizeClient(new SizeInt32(1280, 900));
            var initial = reference.RootElement.GetProperty("cases")[0].GetProperty("spec"); var uiThread = Environment.CurrentManagedThreadId;
            var tested = FooterReferenceFactory.Create(initial); var footerCommand = new FooterBackgroundCommand(); tested.Command = footerCommand; root.Children.Add(tested); await Layout();
            BleeckerNavLinkContext[] Contexts(FrameworkElement control) => Nodes<HyperlinkButton>(control).Where(link => link.DataContext is BleeckerNavLinkContext).Select(link => (BleeckerNavLinkContext)link.DataContext).ToArray();
            async Task CommandCheck(string id, FrameworkElement control, FooterBackgroundCommand command)
            {
                var producerThread = await Task.Run(() => { command.SetEnabled(false); return Environment.CurrentManagedThreadId; }); await Layout();
                if (producerThread == uiThread || Contexts(control).Length == 0 || Contexts(control).Any(context => context.Command.CanExecute(null))) throw new InvalidOperationException(id + " failed to marshal command availability.");
                await Task.Run(() => command.SetEnabled(true)); await Layout();
                if (Contexts(control).Any(context => !context.Command.CanExecute(null))) throw new InvalidOperationException(id + " failed to restore command availability.");
                checks.Add(new { id, passed = true, producerThread, uiThread });
            }
            await CommandCheck("footer-background-command", tested, footerCommand);
            var motionCallback = typeof(BleeckerFooter).GetMethod("MotionChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) ?? throw new InvalidOperationException("Footer motion callback missing.");
            var motionThread = await Task.Run(() => { motionCallback.Invoke(tested, [null, null]); return Environment.CurrentManagedThreadId; }); await Layout();
            if (motionThread == uiThread) throw new InvalidOperationException("Motion callback regression did not run off the UI thread."); checks.Add(new { id = "footer-background-motion-callback", passed = true, producerThread = motionThread, uiThread });
            var item = new BleeckerNavItem("#background", "Background item"); var items = (ObservableCollection<BleeckerNavItem>)tested.Sections.First().Items;
            await Task.Run(() => items.Add(item)); await Layout(); if (!Contexts(tested).Any(context => ReferenceEquals(context.Item, item))) throw new InvalidOperationException("Footer background collection item did not render."); checks.Add(new { id = "footer-background-collection", passed = true });
            var originalSections = tested.Sections; var requests = 0; tested.ItemInvoked += (_, _) => requests++; var stale = Contexts(tested).First(); var template = tested.Template; tested.Template = null; tested.Template = template; await Layout(); stale.Command.Execute(null);
            if (!ReferenceEquals(originalSections, tested.Sections) || stale.Command.CanExecute(null) || requests != 0) throw new InvalidOperationException("Footer template replacement lost ownership or kept a retired action live."); checks.Add(new { id = "footer-template-retirement", passed = true });
            var detached = Contexts(tested).First(); root.Children.Remove(tested); await Layout(); detached.Command.Execute(null); if (detached.Command.CanExecute(null) || requests != 0) throw new InvalidOperationException("Detached Footer action remained live."); checks.Add(new { id = "footer-detached-action", passed = true });
            var navItems = new ObservableCollection<BleeckerNavItem> { new("#original", "Original") }; var navCommand = new FooterBackgroundCommand(); var nav = new BleeckerNavMenu { Width = 900, Items = navItems, Command = navCommand }; root.Children.Add(nav); await Layout();
            await CommandCheck("nav-background-command", nav, navCommand); var navItem = new BleeckerNavItem("#new", "New original"); await Task.Run(() => navItems.Add(navItem)); await Layout();
            if (!Contexts(nav).Any(context => ReferenceEquals(context.Item, navItem))) throw new InvalidOperationException("Nav Menu background collection item did not render."); checks.Add(new { id = "nav-background-collection", passed = true }); root.Children.Remove(nav); await Layout();
            completed = frames.Count == 3 * reference.RootElement.GetProperty("cases").GetArrayLength() && checks.Count == 7;
        }
        catch (Exception error) { failure = error.ToString(); }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= FirstChance;
            var visible = _window?.Visible ?? false; var nativeVisible = handle != 0 && WorkspaceVisible(handle); WorkspaceWindowThread(WorkspaceForeground(), out var foreground);
            var isolated = !visible && !nativeVisible && activationEvents == 0 && focusEvents == 0 && isolationFailure is null && foreground != Environment.ProcessId;
            static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
            await File.WriteAllTextAsync(Path.Combine(folder, "hidden-footers.json"), JsonSerializer.Serialize(new { passed = completed && isolated, diagnosticOnly = true, scope = "hidden-footer-repeated-layout-diagnostic", referenceSha256 = referenceHash, frames, checks, failure, isolationFailure, exceptionCount, hiddenWindow = true, activateCalled = false, showCalled = false, focusCalled = false, inputSent = false, visible, nativeVisible, activationEvents, focusEvents, isolated, guardChecks = guards, processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), catalogSha256 = Hash(typeof(App).Assembly.Location), librarySha256 = Hash(typeof(BleeckerFooter).Assembly.Location), visibleRuntimeVerified = false, sourceRasterParityVerified = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
            _window?.Close(); Environment.Exit(completed && isolated ? 0 : 1);
        }
    }
}
