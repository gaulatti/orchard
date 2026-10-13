using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class BreadcrumbsFixture : UserControl
{
    private int commands, invocations;
    private object? parameter;
    private BleeckerBreadcrumbItem? invoked;
    public BreadcrumbsFixture()
    {
        InitializeComponent();
        DefaultPath.Items[0].Command = new FixtureCommand(value => { commands++; parameter = value; });
        DefaultPath.Items[0].CommandParameter = DefaultPath.Items[0];
        DefaultPath.ItemInvoked += (_, item) => { invocations++; invoked = item; Invocation.Text = $"Consumer invoked {item.Href}, external={item.External}"; };
    }
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        var controls = new[] { DefaultPath, Narrow, Collapsed, CollapsedTwo, Negative, Empty, Single, Threshold, Unicode, Custom };
        var measurements = new List<object>();
        foreach (var control in controls)
        {
            control.ApplyTemplate(); control.UpdateLayout();
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
            Require(peer.GetName() == "Breadcrumb" && peer.GetAutomationControlType() == AutomationControlType.Group && !control.IsTabStop, "Breadcrumb navigation semantics changed.");
            var children = Children(control);
            if (control == Empty) Require(children.Count == 0 && control.ActualHeight == 0, "Empty path gained content or height.");
            else
            {
                Require(children[^1] is TextBlock current && current.FontSize == 14 && current.LineHeight == 20 && current.FontWeight.Weight == 500 &&
                    Hex(current.Foreground) == (scheme == BleeckerColorScheme.Dark ? "#FFF0F4F8" : "#FF2D2D2D"), "Current page typography or palette changed.");
                foreach (var link in children.OfType<HyperlinkButton>())
                {
                    link.ApplyTemplate();
                    var text = Find<TextBlock>(link, "PART_Text");
                    Require(text.FontSize == 14 && text.LineHeight == 20 && text.FontWeight.Weight == 400 && text.FontFamily.Source.Contains("EncodeSans-Regular.ttf"), "Native link defaults replaced breadcrumb typography.");
                    Require(link.Padding == new Thickness(0) && link.MinHeight == 0 && link.MinWidth == 0, "Native link dimensions replaced the source layout.");
                    Require(FrameworkElementAutomationPeer.CreatePeerForElement(link).GetAutomationControlType() == AutomationControlType.Hyperlink, "Preceding path lost link semantics.");
                }
            }
            measurements.Add(new { control.Name, control.ActualWidth, control.ActualHeight, children = children.Cast<FrameworkElement>().Select(child => new
            {
                type = child.GetType().Name, child.ActualWidth, child.ActualHeight,
                origin = child.TransformToVisual(control).TransformPoint(new Point()),
                label = child is TextBlock text ? text.Text : child is HyperlinkButton link ? link.Content : null
            }).ToArray() });
            if (control.ActualHeight > 0) await MainWindow.CaptureElementAsync(control, Path.Combine(folder, $"breadcrumb-{scheme}-{control.Name}.png"));
        }
        Require(Children(DefaultPath).Count == 9 && DefaultPath.ActualHeight == 20 && Narrow.ActualHeight == 72,
            "Default/narrow wrapping no longer matches the source line and gap geometry.");
        var sourceWidths = new[] { 36.984375, 43.453125, 42.171875, 37.890625 };
        var defaultLinks = Children(DefaultPath).OfType<HyperlinkButton>().ToArray();
        for (var index = 0; index < sourceWidths.Length; index++)
            Require(!defaultLinks[index].UseLayoutRounding &&
                Math.Abs(defaultLinks[index].ActualWidth - sourceWidths[index]) <= 1d / 32,
                $"Link {index} width {defaultLinks[index].ActualWidth:R} differs from pinned source {sourceWidths[index]:R}; rounding={defaultLinks[index].UseLayoutRounding}.");
        Require(Children(CollapsedTwo).Count == 5 && Children(CollapsedTwo).OfType<HyperlinkButton>().Count() == 1 && CollapsedTwo.ActualHeight == 24,
            "Collapse threshold incorrectly retained extra trailing links.");
        Require(Children(Threshold).Count == 5 && Children(Threshold).OfType<Button>().Count() == 0 && Children(Negative).Count == 9,
            "Exact or negative collapse threshold changed.");
        var customLinks = Children(Custom).OfType<ContentPresenter>().Where(child => child.Content is BleeckerBreadcrumbItem).ToArray();
        Require(customLinks.Length == 4 && customLinks.Select(child => child.Content).SequenceEqual(DefaultPath.Items.Take(4)) && Children(Custom)[^1] is TextBlock,
            "Custom renderer lost original data or replaced current-page text.");
        foreach (var separator in Children(Custom).OfType<ContentControl>())
        {
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(separator);
            Require(!peer.IsContentElement() && !peer.IsControlElement() && peer.GetChildren() is null or { Count: 0 }, "Custom separator leaked its aria-hidden subtree to accessibility.");
        }

        var first = Children(DefaultPath).OfType<HyperlinkButton>().First();
        var commandBefore = commands; var invokeBefore = invocations;
        ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(first).GetPattern(PatternInterface.Invoke)).Invoke();
        await AwaitAsync(() => commands == commandBefore + 1 && invocations == invokeBefore + 1, "Default link did not activate the consumer command/event exactly once.");
        Require(ReferenceEquals(invoked, DefaultPath.Items[0]) && ReferenceEquals(parameter, DefaultPath.Items[0]) && DefaultPath.Items[1].External && DefaultPath.Items[1].Href == "/library",
            "Activation or rendering rewrote consumer path metadata.");

        using var input = new NativeInputSession(XamlRoot, folder, $"breadcrumb-{scheme}");
        await input.AcquireAsync((FrameworkElement)Rows.Children[0]);
        // Remount a fresh interaction sample for each appearance. Persistent
        // catalog instances retain their already-expanded lifetime state.
        var interaction = new BleeckerBreadcrumb { Items = DefaultPath.Items, CollapsedAfter = 1, Width = 320, HorizontalAlignment = HorizontalAlignment.Left };
        Rows.Children.Add(interaction);
        await AwaitAsync(() => interaction.IsLoaded, "Interaction breadcrumb did not mount.");
        interaction.ApplyTemplate(); UpdateLayout();
        var expand = Children(interaction).OfType<Button>().Single();
        try
        {
            await input.MoveAsync(expand);
            var hoverPassed = false;
            try
            {
                await AwaitAsync(() => Hex(Find<Border>(expand, "PART_Chrome").Background) == (scheme == BleeckerColorScheme.Dark ? "#331A2332" : "#1AE6D5B8"),
                    "Ellipsis hover palette did not settle.", () => JsonSerializer.Serialize(HoverDetails()));
                hoverPassed = true;
            }
            finally
            {
                await File.WriteAllTextAsync(Path.Combine(folder, $"breadcrumb-hover-{scheme}.json"), JsonSerializer.Serialize(new { passed = hoverPassed,
                    scheme, details = HoverDetails(), measurements }, new JsonSerializerOptions { WriteIndented = true }));
                await MainWindow.CaptureElementAsync(interaction, Path.Combine(folder, $"breadcrumb-hover-{scheme}.png"));
            }
            object HoverDetails()
            {
                var nativeRoot = (FrameworkElement)VisualTreeHelper.GetChild(expand, 0);
                var palette = Find<Border>(expand, "PART_Palette");
                return new { expand.IsEnabled, expand.IsLoaded, expand.FocusState, expand.IsPressed,
                    paletteBackground = Hex(palette.Background), paletteForeground = Hex(palette.BorderBrush),
                    paintedBackground = Hex(Find<Border>(expand, "PART_Chrome").Background),
                    states = VisualStateManager.GetVisualStateGroups(nativeRoot).Select(group => new { group.Name, current = group.CurrentState?.Name }).ToArray(),
                    input.OwnsForeground, bounds = input.ScreenBounds(expand), pathBounds = input.ScreenBounds(interaction),
                    hit = NativeScreenCapture.DescribePoint(input.Window, (int)(input.ScreenBounds(expand).X + 12), (int)(input.ScreenBounds(expand).Y + 12)) };
            }
            Require(expand.ActualWidth == 24 && expand.ActualHeight == 24 && expand.CornerRadius == new CornerRadius(6), "Ellipsis native defaults replaced source dimensions.");
            expand.Focus(FocusState.Keyboard); input.Key(0x0D);
            await AwaitAsync(() => Children(interaction).Count == 9 && Children(interaction).OfType<Button>().Count() == 0, "Keyboard expansion did not expose the entire path.");
            await AwaitAsync(() => Children(interaction).OfType<HyperlinkButton>().Skip(1).First().FocusState == FocusState.Keyboard, "Expansion did not restore keyboard focus to the revealed path.");
        }
        finally { Rows.Children.Remove(interaction); }
        var persistentExpand = Children(Collapsed).OfType<Button>().SingleOrDefault();
        if (persistentExpand is not null)
        {
            ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(persistentExpand).GetPattern(PatternInterface.Invoke)).Invoke();
            await AwaitAsync(() => Children(Collapsed).Count == 9, "Persistent path did not expand.");
        }
        var collection = DefaultPath.Items;
        var replacement = new ObservableCollection<BleeckerBreadcrumbItem>(collection.Take(3));
        DefaultPath.Items = replacement;
        await AwaitAsync(() => ReferenceEquals(Collapsed.Items, replacement) && Children(Collapsed).Count == 5, "Bound path replacement lost the expansion latch or original data.");
        DefaultPath.Items = collection;
        await AwaitAsync(() => Children(Collapsed).Count == 9, "Expanded path collapsed after consumer data restoration.");
        var unchanged = Children(DefaultPath).OfType<HyperlinkButton>().First();
        collection.Add(new BleeckerBreadcrumbItem { Label = "Review", Href = "/review" }); UpdateLayout();
        Require(ReferenceEquals(Children(DefaultPath).OfType<HyperlinkButton>().First(), unchanged) && Children(Collapsed).Count == 11, "Appending data remounted unchanged keys or reset expansion.");
        collection.RemoveAt(collection.Count - 1);
        var label = collection[^1].Label; collection[^1].Label = "Updated current page"; UpdateLayout();
        Require(((TextBlock)Children(DefaultPath)[^1]).Text == "Updated current page", "Item changes did not reach the current page.");
        collection[^1].Label = label;
        var template = Collapsed.Template; Collapsed.Template = null; Collapsed.ApplyTemplate();
        Require(VisualTreeHelper.GetChildrenCount(Collapsed) == 0, "Cleared breadcrumb retained template content.");
        Collapsed.Template = template; Collapsed.ApplyTemplate(); UpdateLayout();
        Require(Children(Collapsed).Count == 9, "Template restoration lost expanded state.");
        var client = await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"breadcrumb-window-{scheme}.png"));
        await File.WriteAllTextAsync(Path.Combine(folder, $"breadcrumb-measurements-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, scheme, scale = XamlRoot.RasterizationScale, measurements, client }, new JsonSerializerOptions { WriteIndented = true }));
    }
    private static IList<UIElement> Children(BleeckerBreadcrumb control) => Find<Panel>(control, "PART_Items").Children.OfType<Panel>().Single().Children;
    private static T Find<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        if (root is T current && current.Name == name) return current;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            try { return Find<T>(VisualTreeHelper.GetChild(root, index), name); } catch (KeyNotFoundException) { }
        }
        throw new KeyNotFoundException(name);
    }
    private static string Hex(Brush brush) => brush is SolidColorBrush solid ? $"#{(byte)Math.Round(solid.Color.A * solid.Opacity):X2}{solid.Color.R:X2}{solid.Color.G:X2}{solid.Color.B:X2}" : brush.GetType().Name;
    private async Task AwaitAsync(Func<bool> condition, string message, Func<string>? details = null)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && elapsed.Elapsed < TimeSpan.FromSeconds(2)) { UpdateLayout(); await Task.Delay(16); }
        Require(condition(), $"{message} {details?.Invoke()}");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class FixtureCommand(Action<object?> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute(parameter);
    }
}
