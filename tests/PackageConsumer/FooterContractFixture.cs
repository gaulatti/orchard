using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Testing;

// Public-API contract fixture, shared by the catalog and independent NuGet app.
// No window activation, focus, native input, URI launch or preference changes.
internal static class FooterContractFixture
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private sealed class Command : ICommand
    {
        internal bool Enabled = true; internal int Requests; internal object? Parameter;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => Enabled;
        public void Execute(object? parameter) { Requests++; Parameter = parameter; }
        internal void Publish() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
    private sealed class Selector(DataTemplate template) : DataTemplateSelector
    {
        internal readonly List<object> Items = [];
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) { Items.Add(item); return template; }
    }
    private sealed class OriginalItem(string href, string label, string marker) : BleeckerNavItem(href, label) { internal string Marker { get; } = marker; }
    [Bindable]
    private sealed class SlotModel : System.ComponentModel.INotifyPropertyChanged
    {
        private string tag = "";
        public string Tag { get => tag; set { tag = value; PropertyChanged?.Invoke(this, new(nameof(Tag))); } }
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }
    internal static async Task RunAsync(Window window, string reportPath, string hostPath)
    {
        var checks = new List<object>(); var failures = new List<object>(); var guards = 0; var activations = 0; var focus = 0;
        var root = new Canvas { Width = 1280, Height = 900 }; window.Content = root;
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++;
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard() { guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var owner); Require(!window.Visible && !window.AppWindow.IsVisible && !IsWindowVisible(handle) && owner != Environment.ProcessId && activations == 0 && focus == 0, "Hidden Footer contract window acquired visibility, activation or focus."); }
        async Task Layout() { for (var index = 0; index < 4; index++) { root.Measure(new Size(1280, 900)); root.Arrange(new Rect(0, 0, 1280, 900)); root.UpdateLayout(); Guard(); await Task.Delay(10); } Require(root.XamlRoot is { IsHostVisible: false }, "Contract XamlRoot must remain hidden."); }
        async Task Check(string id, Func<Task> action) { try { Guard(); await action(); checks.Add(new { id, passed = true }); } catch (Exception error) { checks.Add(new { id, passed = false }); failures.Add(new { id, error = error.ToString() }); } finally { root.Children.Clear(); await Layout(); } }
        async Task Mount(BleeckerFooter footer) { root.Children.Add(footer); await Layout(); Require(footer.IsLoaded, "Footer did not mount."); }
        static BleeckerFooter Create() => new() { Width = 1200, Brand = new() { Href = "#brand", BrandName = "Fictional library", Description = "Local component demonstration." }, Sections = new ObservableCollection<BleeckerFooterSection> { new() { Title = "Resources", Items = new ObservableCollection<BleeckerNavItem> { new OriginalItem("https://github.com/example/fictional", "Original source", "original"), new("#other", "Other") } } } };
        static Binding Bind(object source, string path) => new() { Source = source, Path = new PropertyPath(path), Mode = BindingMode.OneWay };
        static DataTemplate Template(string body) => (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" + body + "</DataTemplate>");
        static HyperlinkButton[] Links(BleeckerFooter footer) => Nodes(footer).OfType<HyperlinkButton>().Where(button => button.DataContext is BleeckerNavLinkContext).ToArray();
        static BleeckerNavLinkContext Context(HyperlinkButton button) => (BleeckerNavLinkContext)button.DataContext;
        static bool Text(DependencyObject owner, string value) => Nodes(owner).OfType<TextBlock>().Any(block => block.Text == value);
        try
        {
            await Layout();
            await Check("bottom-right-bound-nullish-scalars", async () =>
            {
                var producer = new TextBlock(); var footer = Create(); footer.SetBinding(BleeckerFooter.BottomRightProperty, Bind(producer, "Tag")); await Mount(footer);
                Require(Links(footer).Any(button => button.Name == "FooterSourceLink"), "Null right binding did not expose the source link.");
                foreach (var scalar in new object[] { "", false, 0 }) { producer.Tag = scalar; await Layout(); Require(!Links(footer).Any(button => button.Name == "FooterSourceLink") && Equals(footer.BottomRight, scalar), "Bound explicit right scalar was replaced with a source link."); if (scalar is int) Require(Text(footer, "0"), "Bound zero did not render."); }
                producer.Tag = null; await Layout(); Require(Links(footer).Any(button => button.Name == "FooterSourceLink") && footer.GetBindingExpression(BleeckerFooter.BottomRightProperty) is not null, "Nullish transition replaced the original binding.");
            });
            await Check("bottom-left-model-template-binding", async () =>
            {
                var model = new SlotModel { Tag = "Original model" }; var producer = new TextBlock { Tag = model }; var footer = Create();
                footer.BottomLeftTemplate = Template("<TextBox x:Name='TemplateEditor' Width='200' Tag='{Binding}' Text='{Binding Tag, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}'/>"); footer.SetBinding(BleeckerFooter.BottomLeftProperty, Bind(producer, "Tag")); await Mount(footer);
                var editor = Nodes(footer).OfType<TextBox>().Single(); Require(ReferenceEquals(editor.Tag, model) && editor.Text == "Original model", "Left template lost the original model.");
                editor.Text = "Caller edit"; await Layout(); Require(Equals(model.Tag, "Caller edit"), "Templated caller editor did not update the original model.");
                var replacement = new SlotModel { Tag = "Replacement model" }; producer.Tag = replacement; await Layout(); Require(ReferenceEquals(Nodes(footer).OfType<TextBox>().Single().Tag, replacement) && footer.GetBindingExpression(BleeckerFooter.BottomLeftProperty) is not null, "Left template replacement lost its binding or original model.");
            });
            await Check("bottom-right-selector-original-model", async () =>
            {
                var model = new SlotModel { Tag = "Selected original" }; var footer = Create(); var selector = new Selector(Template("<TextBlock x:Name='SelectedSlot' Text='{Binding Tag}' Tag='{Binding}'/>")); footer.BottomRight = model; footer.BottomRightTemplateSelector = selector; await Mount(footer);
                Require(selector.Items.Any(item => ReferenceEquals(item, model)) && ReferenceEquals(Nodes(footer).OfType<TextBlock>().Single(block => block.Name == "SelectedSlot").Tag, model), "Right selector did not receive the original model.");
                model.Tag = "Updated original"; await Layout(); Require(Text(footer, "Updated original") && !Links(footer).Any(button => button.Name == "FooterSourceLink"), "Right selector content did not retain live binding.");
            });
            await Check("original-editor-survives-section-mutations", async () =>
            {
                var editor = new TextBox { Width = 200, Text = "Caller original" }; editor.Select(2, 5); var footer = Create(); var producer = new TextBlock { Tag = editor }; footer.SetBinding(BleeckerFooter.BottomLeftProperty, Bind(producer, "Tag")); await Mount(footer);
                var sections = (ObservableCollection<BleeckerFooterSection>)footer.Sections; sections.Add(new() { Title = "Other section" }); sections.Move(0, 1); sections[0].Title = "Renamed section"; footer.Brand.Description = "Mutable description"; await Layout();
                Require(ReferenceEquals(Nodes(footer).OfType<TextBox>().Single(), editor) && editor.Text == "Caller original" && editor.SelectionStart == 2 && editor.SelectionLength == 5 && footer.GetBindingExpression(BleeckerFooter.BottomLeftProperty) is not null, "Section mutations recreated caller input or lost selection/binding.");
            });
            await Check("original-editors-survive-template-replacement", async () =>
            {
                var left = new TextBox { Width = 180, Text = "Original left" }; var right = new TextBox { Width = 180, Text = "Original right" }; left.Select(2, 4); right.Select(1, 5);
                var footer = Create(); footer.BottomLeft = left; footer.BottomRight = right; await Mount(footer); var template = footer.Template; footer.Template = null; await Layout(); footer.Template = template; await Layout();
                Require(Nodes(footer).OfType<TextBox>().Contains(left) && Nodes(footer).OfType<TextBox>().Contains(right) && left.SelectionStart == 2 && left.SelectionLength == 4 && right.SelectionStart == 1 && right.SelectionLength == 5, "Template replacement lost original editor identity or selection.");
            });
            await Check("fractional-host-original-slots", async () =>
            {
                var left = new TextBox { Width = 400, Text = "Caller left" }; var right = new TextBox { Width = 250, Text = "Caller right" };
                left.Select(1, 4); right.Select(2, 3);
                var footer = Create(); footer.Width = 640.5; footer.BottomLeft = left; footer.BottomRight = right; await Mount(footer);
                var bottom = Nodes(footer).OfType<FrameworkElement>().Single(element => element.Name == "FooterBottom");
                var leftSlot = Nodes(footer).OfType<FrameworkElement>().Single(element => element.Name == "FooterBottomLeft");
                var rightSlot = Nodes(footer).OfType<FrameworkElement>().Single(element => element.Name == "FooterBottomRight");
                Rect Box(FrameworkElement element) => element.TransformToVisual(bottom).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
                void Verify(double width, bool constrained)
                {
                    var l = Box(leftSlot); var r = Box(rightSlot);
                    Require(Math.Abs(bottom.ActualWidth - (width - 32)) <= .01 && Math.Abs(l.X) <= .01 &&
                        Math.Abs(r.Right - bottom.ActualWidth) <= .01 && r.X - l.Right >= 16 - .01 &&
                        (!constrained || Math.Abs(r.X - l.Right - 16) <= .01),
                        $"Fractional host lost bottom-slot spacing: host={width}, bottom={bottom.ActualWidth}, left={l}, right={r}.");
                    Require(ReferenceEquals(Nodes(footer).OfType<TextBox>().Single(editor => editor.Text == "Caller left"), left) &&
                        ReferenceEquals(Nodes(footer).OfType<TextBox>().Single(editor => editor.Text == "Caller right"), right) &&
                        left.SelectionStart == 1 && left.SelectionLength == 4 && right.SelectionStart == 2 && right.SelectionLength == 3,
                        "Fractional host replaced caller editors or lost their selection.");
                }
                Verify(640.5, true); footer.Width = 768.5; await Layout(); Verify(768.5, false);
            });
            await Check("custom-link-original-item-command-and-style", async () =>
            {
                var footer = Create(); var command = new Command(); var parameter = new object(); var requests = new List<BleeckerNavItem>(); footer.Command = command; footer.CommandParameter = parameter; footer.ItemInvoked += (_, item) => requests.Add(item);
                var brush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 80, 120)) { Opacity = .33 }; var style = new Style(typeof(HyperlinkButton)); style.Setters.Add(new Setter(Control.ForegroundProperty, brush)); style.Setters.Add(new Setter(Control.FontSizeProperty, 19d)); footer.LinkStyle = style;
                footer.LinkTemplate = Template("<HyperlinkButton Content='{Binding Label}' Tag='{Binding Item}' Command='{Binding Command}' Style='{Binding Style}' Foreground='{Binding Foreground}'/>"); await Mount(footer);
                var original = footer.Sections.First().Items.First(); var link = Links(footer).First(button => ReferenceEquals(Context(button).Item, original)); var context = Context(link);
                Require(ReferenceEquals(link.Tag, original) && context.Item is OriginalItem { Marker: "original" } && ReferenceEquals(context.Style, style) && link.FontSize == 19 && context.Label == "Original source", "Custom link did not retain original item/style/label.");
                context.Command.Execute(null); Require(command.Requests == 1 && ReferenceEquals(command.Parameter, parameter) && ReferenceEquals(requests.Single(), original), "Custom link changed the explicit parameter or original event item.");
                command.Enabled = false; command.Publish(); await Layout(); context.Command.Execute(null); Require(!context.Command.CanExecute(null) && command.Requests == 1, "Disabled custom action executed.");
            });
            await Check("keyed-item-replacement-and-retirement", async () =>
            {
                var footer = Create(); await Mount(footer); var section = footer.Sections.First(); var items = (ObservableCollection<BleeckerNavItem>)section.Items; var original = items[1]; var context = Context(Links(footer).Single(button => ReferenceEquals(Context(button).Item, original)));
                var replacement = new OriginalItem(original.Href, original.Label, "replacement"); items[1] = replacement; await Layout(); Require(Links(footer).Any(button => ReferenceEquals(Context(button), context) && ReferenceEquals(context.Item, replacement)), "Equal-key item replacement lost the retained view or original replacement.");
                replacement.Label = "New key"; await Layout(); Require(!context.Command.CanExecute(null) && Links(footer).Any(button => ReferenceEquals(Context(button).Item, replacement) && Context(button).Label == "New key"), "Changed item key did not retire its old action.");
            });
            await Check("custom-renderer-owned-container-metrics", async () =>
            {
                var baseline = Create(); await Mount(baseline); var plain = Links(baseline).Single(button => button.Name == "FooterSectionLink" && Context(button).Label == "Original source"); var expected = plain.ActualWidth + 16;
                var custom = Create(); custom.LinkTemplate = Template("<Border x:Name='CallerLinkContainer' Padding='8'><HyperlinkButton Content='{Binding Content}' Command='{Binding Command}' Style='{Binding Style}'/></Border>"); await Mount(custom);
                var container = Nodes(custom).OfType<Border>().Single(border => border.Name == "CallerLinkContainer" && border.DataContext is BleeckerNavLinkContext context && context.Content is FrameworkElement { Name: "FooterLinkText" } && context.Label == "Original source");
                Require(Math.Abs(container.ActualWidth - expected) <= .001 && container.Padding == new Thickness(8), "Footer forced its inline text width onto the caller's padded renderer container.");
            });
            await Check("source-raw-first-match-and-explicit-suppression", async () =>
            {
                var footer = Create(); var first = new OriginalItem("#query?github.com=literal", "Raw first", "first"); var second = new OriginalItem("https://github.com/example/second", "Second", "second"); footer.Sections.First().Items = new ObservableCollection<BleeckerNavItem> { new("https://GITHUB.COM/ignored", "Uppercase"), first, second }; await Mount(footer);
                var action = Links(footer).Single(button => button.Name == "FooterSourceLink"); var stale = Context(action); Require(ReferenceEquals(stale.Item, first) && stale.Href == first.Href && stale.Label == "Raw first" && Text(action, "View source on GitHub"), "Default source repaired the raw href, changed metadata or ignored first-match order.");
                footer.BottomRight = false; await Layout(); Require(!stale.Command.CanExecute(null) && !Links(footer).Any(button => button.Name == "FooterSourceLink"), "Explicit suppression left a source action live.");
                first.Href = "#removed"; footer.BottomRight = null; await Layout(); Require(ReferenceEquals(Context(Links(footer).Single(button => button.Name == "FooterSourceLink")).Item, second), "Live source recomputation did not preserve the next original item.");
            });
            await Check("bound-brand-metadata-and-explicit-null-logo", async () =>
            {
                var footer = Create(); var original = footer.Brand; var producer = new TextBlock { Tag = "Bound brand" }; BindingOperations.SetBinding(original, BleeckerFooterBrand.BrandNameProperty, Bind(producer, "Tag")); original.LogoSource = null; await Mount(footer);
                producer.Tag = "Updated brand"; original.Href = "#raw-destination"; original.Description = "Updated description"; await Layout(); var brand = Nodes(footer).OfType<BleeckerBrandLockup>().Single();
                Require(brand.BrandName == "Updated brand" && brand.Href == "#raw-destination" && brand.LogoSource is null && Text(footer, "Updated description"), "Footer brand replaced original binding, explicit-null logo or live metadata.");
                footer.Brand = new() { BrandName = "Replacement brand", Href = "#replacement" }; await Layout(); producer.Tag = "Retired brand"; await Layout(); Require(Nodes(footer).OfType<BleeckerBrandLockup>().Single().BrandName == "Replacement brand", "Retired brand producer changed replacement content.");
            });
            await Check("custom-template-retired-and-remounted-actions", async () =>
            {
                var footer = Create(); await Mount(footer); var stale = Context(Links(footer).First()); footer.LinkTemplate = Template("<HyperlinkButton Content='{Binding Content}' Command='{Binding Command}' Style='{Binding Style}'/>"); await Layout(); Require(!stale.Command.CanExecute(null), "Changing custom template left its retired action live.");
                var current = Context(Links(footer).First()); root.Children.Remove(footer); await Layout(); Require(!current.Command.CanExecute(null), "Detached action remained live."); root.Children.Add(footer); await Layout(); Require(Links(footer).All(button => Context(button).Command.CanExecute(null)), "Remounted custom actions were not refreshed.");
            });
        }
        catch (Exception error) { failures.Add(new { id = "harness", error = error.ToString() }); }
        var isolated = true; try { Guard(); } catch (Exception error) { isolated = false; failures.Add(new { id = "isolation", error = error.ToString() }); }
        var passed = failures.Count == 0 && checks.Count == 12 && isolated;
        static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(new { passed, diagnosticOnly = true, scope = "hidden-footer-public-contracts", checks, failures, guardChecks = guards, isolated, hiddenWindow = true, activateCalled = false, showCalled = false, focusCalled = false, inputSent = false, activationEvents = activations, focusEvents = focus, processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), hostSha256 = Hash(hostPath), librarySha256 = Hash(typeof(BleeckerFooter).Assembly.Location), visibleRuntimeVerified = false, sourceRasterParityVerified = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
        window.Close(); Environment.Exit(passed ? 0 : 1);
    }
    private static IEnumerable<DependencyObject> Nodes(DependencyObject owner) { yield return owner; for (var index = 0; index < VisualTreeHelper.GetChildrenCount(owner); index++) foreach (var node in Nodes(VisualTreeHelper.GetChild(owner, index))) yield return node; }
}
