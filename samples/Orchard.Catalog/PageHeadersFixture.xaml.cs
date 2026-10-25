using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace Orchard.Catalog;

public sealed partial class PageHeadersFixture : UserControl
{
    public PageHeadersFixture()
    {
        InitializeComponent();
        Specimen.Actions = Actions();
    }
    private static ObservableCollection<UIElement> Actions() => [
        new BleeckerButton { Content = "Save draft", Size = BleeckerButtonSize.Small },
        new BleeckerButton { Content = "Preview", Size = BleeckerButtonSize.Small, Variant = BleeckerButtonVariant.Secondary }
    ];
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        using var input = new NativeInputSession(XamlRoot, folder, $"page-header-{scheme}");
        await input.AcquireAsync(Heading);
        Require(XamlRoot.RasterizationScale == 1 && new Windows.UI.ViewManagement.UISettings().TextScaleFactor == 1,
            "Frozen Page Header comparison requires display/text scale 1; scaling acceptance remains open.");
        var window = AppWindow.GetFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
        var size = window.ClientSize;
        var presenter = (OverlappedPresenter)window.Presenter;
        var maximized = presenter.State == OverlappedPresenterState.Maximized;
        using var reference = JsonDocument.Parse(CatalogReferenceResources.Open(typeof(PageHeadersFixture).Assembly, "Orchard.Catalog.Reference.PageHeaderLayout.json")!);
        var reports = new List<object>();
        var root = (Grid)XamlRoot.Content;
        var specimenHost = new Grid { Margin = new Thickness(20), Background = root.Background };
        Grid.SetRowSpan(specimenHost, root.RowDefinitions.Count); Grid.SetColumnSpan(specimenHost, root.ColumnDefinitions.Count);
        root.Children.Add(specimenHost);
        try
        {
            presenter.Restore(false);
            foreach (var item in reference.RootElement.GetProperty("cases").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == scheme.ToString()))
            {
                var id = item.GetProperty("id").GetString()!;
                var viewport = item.GetProperty("viewport").GetInt32();
                window.ResizeClient(new SizeInt32(viewport, 745));
                await AwaitAsync(() => Math.Abs(XamlRoot.Size.Width - viewport) < .001, "Page Header viewport did not resize.");
                var props = item.GetProperty("props");
                var command = new BackCommand(); var parameter = new object(); var notifications = 0;
                var control = new BleeckerPageHeader { Background = root.Background, Width = item.GetProperty("width").GetDouble(), HorizontalAlignment = HorizontalAlignment.Left,
                    Title = props.GetProperty("title").GetString(), Description = props.TryGetProperty("description", out var description) ? description.GetString() : null,
                    VerticalAlignment = VerticalAlignment.Top, FlowDirection = item.GetProperty("rtl").GetBoolean() ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                if (props.TryGetProperty("breadcrumbs", out _)) control.Breadcrumbs = new TextBlock { Text = "Workspace / Editorial" };
                if (props.TryGetProperty("actions", out _)) control.Actions = Actions();
                if (props.TryGetProperty("back", out _)) { control.BackCommand = command; control.BackCommandParameter = parameter; }
                if (props.TryGetProperty("href", out var href)) control.BackHref = href.GetString();
                control.BackRequested += (_, _) => notifications++;
                specimenHost.Children.Clear(); specimenHost.Children.Add(control);
                await AwaitAsync(() => control.IsLoaded, "Page Header did not mount.");
                control.ApplyTemplate(); UpdateLayout(); await MainWindow.AwaitPresentationAsync(control); UpdateLayout();
                var title = Part<TextBlock>(control, "PART_Title"); var desc = Part<TextBlock>(control, "PART_Description");
                var backHost = Part<Grid>(control, "PART_Back"); var back = (Button)backHost.Children[0];
                var lead = Part<Grid>(control, "PART_Lead"); var actionHost = Part<Grid>(control, "PART_Actions");
                var expected = item.GetProperty("layout");
                reports.Add(new { id, viewport = XamlRoot.Size.Width, width = control.ActualWidth, height = control.ActualHeight,
                    expectedHeight = expected.GetProperty("header").GetProperty("height").GetDouble(),
                    leadWidth = lead.ActualWidth, leadHeight = lead.ActualHeight, titleWidth = title.ActualWidth, titleHeight = title.ActualHeight,
                    descriptionWidth = desc.ActualWidth, descriptionHeight = desc.ActualHeight, actionsWidth = actionHost.ActualWidth, actionsHeight = actionHost.ActualHeight });
                await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"page-header-layout-{scheme}.json"), JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true }));
                Require(title.FontSize == 32 && title.LineHeight == 36.796875 && title.CharacterSpacing == -20 && AutomationProperties.GetHeadingLevel(title) == AutomationHeadingLevel.Level1,
                    $"Page Header lost its source title or heading semantics: {title.FontSize:R}/{title.LineHeight:R}/{title.CharacterSpacing}/{AutomationProperties.GetHeadingLevel(title)}.");
                Require(FrameworkElementAutomationPeer.CreatePeerForElement(control).GetName() == control.Title, "Page Header changed its accessible title.");
                if (back.Visibility == Visibility.Visible)
                {
                    Require(back.ActualWidth == 32 && back.ActualHeight == 32 && AutomationProperties.GetName(back) == "Back", "Page Header lost its named native Back action.");
                    Require(Descendants<Viewbox>(back).Single().FlowDirection == FlowDirection.LeftToRight, "RTL changed the source chevron direction.");
                    Require(FrameworkElementAutomationPeer.CreatePeerForElement(back).GetAutomationControlType() == (control.BackHref is null ? AutomationControlType.Button : AutomationControlType.Hyperlink), "Back route role differs.");
                    await input.ClickAsync(back);
                    await AwaitAsync(() => notifications == 1, "Back pointer did not deliver exactly one notification.");
                    if (control.BackCommand is not null)
                    {
                        Require(command.Count == 1 && ReferenceEquals(command.Parameter, parameter), "Back replaced its consumer command or parameter.");
                        Require(back.Focus(FocusState.Keyboard), "Back refused keyboard focus."); input.Key(0x20);
                        await AwaitAsync(() => command.Count == 2 && notifications == 2, "Back Space activation differs.");
                        command.Allowed = false; command.RaiseChanged();
                        await AwaitAsync(() => !back.IsEnabled, "Back CanExecute did not disable the native action.");
                        command.Allowed = true; command.RaiseChanged();
                        await AwaitAsync(() => back.IsEnabled, "Back CanExecute did not restore the native action.");
                    }
                    if (control.BackHref is not null) Require(control.BackHref == "#fictional-editorial", "Back modified the consumer route.");
                }
                if (control.Actions is ObservableCollection<UIElement> actions)
                {
                    var first = (BleeckerButton)actions[0]; first.ApplyTemplate(); var firstNative = Part<Button>(first, "PART_Action");
                    Require(firstNative.Focus(FocusState.Keyboard), "Consumer action refused focus.");
                    actions.Add(new BleeckerButton { Content = "Extra", Size = BleeckerButtonSize.Small }); UpdateLayout();
                    Require(ReferenceEquals(VisualTreeHelper.GetParent(first), actionHost.Children[0]) && ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), firstNative), "Live actions replaced consumer elements or focus.");
                    actions.RemoveAt(2); UpdateLayout();
                    specimenHost.Children.Remove(control); actions.Add(new BleeckerButton { Content = "Detached", Size = BleeckerButtonSize.Small });
                    specimenHost.Children.Add(control); await AwaitAsync(() => control.IsLoaded, "Page Header did not remount."); UpdateLayout();
                    await PresentedAsync();
                    Require(((Panel)actionHost.Children[0]).Children.Count == 3, "Page Header ignored actions changed while detached.");
                    actions.RemoveAt(2); UpdateLayout();
                    try { await AwaitAsync(() => ((Panel)Part<Grid>(control, "PART_Actions").Children[0]).Children.Count == 2, "Remounted Page Header stopped observing its action collection."); }
                    catch
                    {
                        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, "page-header-remount-failure.json"), JsonSerializer.Serialize(new
                        {
                            control.IsLoaded, values = actions.Count, sameHost = ReferenceEquals(actionHost, Part<Grid>(control, "PART_Actions")),
                            actualChildren = ((Panel)Part<Grid>(control, "PART_Actions").Children[0]).Children.Count,
                            observing = typeof(BleeckerPageHeader).GetField("observedActions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(control) is not null
                        }));
                        throw;
                    }
                }
                await PresentedAsync();
                if (control.ActualHeight > 0) await MainWindow.CaptureElementAsync(control, System.IO.Path.Combine(folder, $"page-header-{id}-{scheme}.png"));
            }
            // All geometry is retained before the comparison so a failure remains reviewable.
            foreach (var report in reports)
            {
                using var data = JsonDocument.Parse(JsonSerializer.Serialize(report)); var value = data.RootElement;
                Require(Math.Abs(value.GetProperty("height").GetDouble() - value.GetProperty("expectedHeight").GetDouble()) < .01,
                    $"Page Header {value.GetProperty("id").GetString()} block geometry differs from the frozen browser.");
            }
            await VerifyMutationAsync(specimenHost);
            await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"page-header-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, cases = reports.Count }));
        }
        finally
        {
            root.Children.Remove(specimenHost);
            if (maximized) presenter.Maximize(); else window.ResizeClient(size);
        }
    }
    private async Task VerifyMutationAsync(Grid host)
    {
        var values = new ObservableCollection<string> { "Consumer action" };
        var header = new BleeckerPageHeader { Title = "Consumer heading", Width = 400, VerticalAlignment = VerticalAlignment.Top,
            Breadcrumbs = "Consumer path", BreadcrumbsTemplate = (DataTemplate)Resources["BreadcrumbTemplate"],
            Actions = values, ActionsTemplate = (DataTemplate)Resources["ActionTemplate"], BackHref = "app-route:unchanged", BackLabel = "Return to archive" };
        host.Children.Clear(); host.Children.Add(header);
        await PresentedAsync(); header.ApplyTemplate(); UpdateLayout(); await PresentedAsync();
        var breadcrumb = Part<ContentPresenter>(header, "PART_Breadcrumbs");
        Require(Descendants<TextBlock>(breadcrumb).Single().Text == "Consumer path" && Descendants<TextBlock>(breadcrumb).Single().LineHeight == 28,
            "Page Header altered its consumer breadcrumb template or line height.");
        var actionHost = Part<Grid>(header, "PART_Actions");
        Require(Descendants<BleeckerButton>(actionHost).Single().Content as string == "Consumer action", "Page Header did not render the consumer action template.");
        values[0] = "Changed action"; header.Title = "Changed heading"; header.Description = "Changed description"; UpdateLayout(); await PresentedAsync();
        Require(Part<TextBlock>(header, "PART_Title").Text == "Changed heading" && Part<TextBlock>(header, "PART_Description").Text == "Changed description" &&
            Descendants<BleeckerButton>(actionHost).Single().Content as string == "Changed action", "Mounted Page Header bindings did not update.");
        AutomationProperties.SetName(header, "Consumer accessible name");
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(header).GetName() == "Consumer accessible name", "Page Header overwrote the consumer accessible name.");
        header.IsEnabled = false; UpdateLayout();
        Require(!((Button)Part<Grid>(header, "PART_Back").Children[0]).IsEnabled && !Descendants<Button>(actionHost).Single().IsEnabled, "Disabled Page Header retained enabled actions.");
        header.IsEnabled = true;
        var template = header.Template; header.Template = null; header.ApplyTemplate(); header.Template = template; header.ApplyTemplate();
        UpdateLayout(); await PresentedAsync();
        Require(Descendants<BleeckerButton>(Part<Grid>(header, "PART_Actions")).Single().Content as string == "Changed action" &&
            AutomationProperties.GetName((Button)Part<Grid>(header, "PART_Back").Children[0]) == "Return to archive" && header.BackHref == "app-route:unchanged",
            "Page Header template replacement lost consumer state.");
        header.Title = ""; header.Description = ""; header.Breadcrumbs = null; header.BackHref = null;
        var shortAction = new Border { Width = 20, Height = 20 }; var tallAction = new Border { Width = 20, Height = 40 };
        header.ActionsTemplate = null; header.Actions = new UIElement[] { shortAction, tallAction }; header.Padding = new Thickness(8);
        UpdateLayout(); await PresentedAsync();
        Require(header.ActualHeight == 56 && Math.Abs(shortAction.TransformToVisual(tallAction).TransformPoint(new Windows.Foundation.Point()).Y - 10) < .001,
            "Page Header ignored consumer padding or center alignment of mixed action heights.");
        header.Padding = new Thickness(0); header.Actions = new ObservableCollection<UIElement>(); UpdateLayout();
        Require(Part<Grid>(header, "PART_Actions").Visibility == Visibility.Visible, "Page Header collapsed a supplied empty action slot.");
        header.Actions = null; header.Breadcrumbs = null; header.Description = null; header.Title = null; header.BackHref = null;
        UpdateLayout(); await PresentedAsync();
        Require(header.ActualHeight == 0, "Removed Page Header content retained a phantom layout.");
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject owner) where T : DependencyObject
    {
        if (owner is T item) yield return item;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++)
            foreach (var descendant in Descendants<T>(VisualTreeHelper.GetChild(owner, i))) yield return descendant;
    }
    private static T Part<T>(DependencyObject owner, string name) where T : FrameworkElement
    {
        if (owner is T element && element.Name == name) return element;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++)
        { try { return Part<T>(VisualTreeHelper.GetChild(owner, i), name); } catch (InvalidOperationException) { } }
        throw new InvalidOperationException($"Missing Page Header part {name}.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static async Task PresentedAsync()
    {
        var presented = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Frame(object? sender, object args) => presented.TrySetResult();
        CompositionTarget.Rendering += Frame;
        try { await presented.Task.WaitAsync(TimeSpan.FromSeconds(2)); } finally { CompositionTarget.Rendering -= Frame; }
    }
    private static async Task AwaitAsync(Func<bool> condition, string message)
    {
        var elapsed = Stopwatch.StartNew(); while (!condition() && elapsed.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(16);
        Require(condition(), message);
    }
    private sealed class BackCommand : ICommand
    {
        internal bool Allowed = true; internal int Count; internal object? Parameter;
        public bool CanExecute(object? parameter) => Allowed;
        public void Execute(object? parameter) { Require(Allowed, "Rejected Back executed."); Count++; Parameter = parameter; }
        public event EventHandler? CanExecuteChanged;
        internal void RaiseChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
