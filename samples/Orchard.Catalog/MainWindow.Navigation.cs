using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private readonly List<(string Title, FrameworkElement Fixture)> components = [];
    private FrameworkElement? selectedFixture;

    private void InitializeNavigation()
    {
        components.AddRange([
            ("Foundation", Foundation), ("Toggles", Toggles), ("Buttons", Buttons),
            ("Icon buttons", IconButtons), ("Toggle groups", ToggleGroups),
            ("Loading spinners", LoadingSpinners), ("Cards", Cards), ("Skeletons", Skeletons), ("Avatars", Avatars), ("Breadcrumbs", Breadcrumbs), ("Separators", Separators),
            ("Status badges", StatusBadges), ("Menus", Menus), ("Notifications", Notifications), ("Alerts", Alerts), ("Brand lockups", BrandLockups), ("Bauhaus backgrounds", BauhausBackgrounds),
            ("Activity items",ActivityItems),
            ("Attention surfaces",AttentionSurfaces),
            ("Chart theme",ChartThemes),
            ("Line charts",LineCharts),
            ("Bar charts",BarCharts),
            ("Area charts",AreaCharts),
            ("Pie and donut charts",PieCharts),
            ("Scatter charts",ScatterCharts),
            ("Radar charts",RadarCharts),
            ("Radial bar charts",RadialBarCharts),
            ("Funnel charts",FunnelCharts),
            ("Sankey charts",SankeyCharts),
            ("Sunburst charts",SunburstCharts),
            ("Currency chart",CurrencyCharts),
            ("Sparklines",Sparklines),
            ("Stat cards",StatCards),
            ("Metrics",Metrics),
            ("Error states",ErrorStates),
            ("Page headers",PageHeaders),
            ("Section headers",SectionHeaders),
            ("Dashboard sections",DashboardSections),
            ("Dashboard grids",DashboardGrids),
            ("Sidebar",Sidebars),
            ("Stepper",Steppers),
            ("Data list",DataLists),
            ("Timeline",Timelines),
            ("Loading overlay",LoadingOverlays),
            ("Filter chips",FilterChips),
            ("Search inputs",SearchInputs),
            ("Text areas",Textareas),
            ("Data table toolbar",DataTableToolbars),
            ("Data table pagination",DataTablePaginations),
            ("Data table columns",DataTableColumnToggles),
            ("Data table filters",DataTableFilters),
            ("Tooltips", Tooltips), ("Field help", FieldHelp), ("Labels", Labels), ("Input", Inputs), ("Keyboard shortcuts", Keyboard), ("Eyebrows", Eyebrows), ("Progress", Progress), ("Switches", Switches), ("Checkboxes", Checkboxes), ("Windows contrast", Contrast)
        ]);
        var declared = FixtureHost.Children.OfType<FrameworkElement>().ToArray();
        Require(declared.Length == components.Count && components.Select(entry => entry.Fixture).Distinct().Count() == declared.Length &&
            declared.All(fixture => components.Any(entry => ReferenceEquals(entry.Fixture, fixture))),
            "Every declared catalog fixture must have exactly one navigation entry.");
        FixtureHost.Children.Clear();
        ShowFixture(Foundation);
        RenderNavigation();
    }

    private void OnComponentSearchChanged(object sender, TextChangedEventArgs args)
    {
        if (components.Count > 0) RenderNavigation();
    }

    private void RenderNavigation()
    {
        var query = ComponentSearch.Text.Trim();
        ComponentNavigation.Children.Clear();
        foreach (var (title, fixture) in components.Where(entry => entry.Title.Contains(query, StringComparison.OrdinalIgnoreCase)))
        {
            var button = new BleeckerButton
            {
                Content = title, Size = BleeckerButtonSize.Small,
                Variant = ReferenceEquals(fixture, selectedFixture) ? BleeckerButtonVariant.Secondary : BleeckerButtonVariant.Ghost,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            button.Tag = fixture;
            AutomationProperties.SetName(button, $"Show {title}");
            button.Click += (_, _) => ShowFixture(fixture);
            ComponentNavigation.Children.Add(button);
        }
        UpdateNavigationState();
    }

    private void UpdateNavigationState()
    {
        foreach (var button in ComponentNavigation.Children.OfType<BleeckerButton>())
            button.Variant = ReferenceEquals(button.Tag, selectedFixture) ? BleeckerButtonVariant.Secondary : BleeckerButtonVariant.Ghost;
        var current = components.Single(entry => ReferenceEquals(entry.Fixture, selectedFixture)).Title;
        NavigationStatus.Text = ComponentNavigation.Children.Count == 0 ? $"No matching components. Showing {current}." : $"Showing {current}";
    }

    private void ShowFixture(FrameworkElement fixture)
    {
        if (!components.Any(entry => ReferenceEquals(entry.Fixture, fixture))) throw new InvalidOperationException("Unregistered catalog fixture.");
        if (!ReferenceEquals(selectedFixture, fixture))
        {
            FixtureHost.Children.Clear();
            FixtureHost.Children.Add(fixture);
            selectedFixture = fixture;
            CatalogScroll.ChangeView(null, 0, null, disableAnimation: true);
        }
        UpdateNavigationState();
    }

    private async Task SelectFixtureAsync(FrameworkElement fixture)
    {
        CatalogRuntimeProfile.EnterFixture(fixture.GetType().Name, CatalogRoot.RequestedTheme.ToString());
        using var selection = CatalogRuntimeProfile.Measure("navigation.mount");
        ShowFixture(fixture);
        CatalogRoot.UpdateLayout();
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (!fixture.IsLoaded && elapsed.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(16);
        Require(fixture.IsLoaded && FixtureHost.Children.Count == 1 && ReferenceEquals(FixtureHost.Children[0], fixture), "Catalog did not mount exactly the selected fixture.");
        fixture.UpdateLayout();
        // IsLoaded can become true before initial bindings and child Loaded
        // callbacks have finished. Exercise the page only after presentation.
        var presented = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnPresented(object? sender, object args) => presented.TrySetResult();
        CompositionTarget.Rendering += OnPresented;
        try { await presented.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
        finally { CompositionTarget.Rendering -= OnPresented; }
    }

    private async Task CaptureFixtureAsync(FrameworkElement fixture, string path)
    {
        await SelectFixtureAsync(fixture);
        fixture.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        (double Vertical, double Horizontal, double X, double Y, double Width, double Height, double Extent)? previous = null;
        var stable = 0; var frames = 0;
        void Presented(object? sender, object args)
        {
            if (!fixture.IsLoaded || !ReferenceEquals(selectedFixture, fixture)) { stable = 0; return; }
            var origin = fixture.TransformToVisual(CatalogRoot).TransformPoint(new Windows.Foundation.Point());
            var state = (CatalogScroll.VerticalOffset, CatalogScroll.HorizontalOffset, origin.X, origin.Y, fixture.ActualWidth, fixture.ActualHeight, CatalogScroll.ExtentHeight);
            stable = previous == state ? stable + 1 : 0; previous = state; frames++;
            if (stable >= 2) ready.TrySetResult();
        }
        CompositionTarget.Rendering += Presented;
        try { await ready.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
        finally { CompositionTarget.Rendering -= Presented; }
        Require(fixture.IsLoaded && ReferenceEquals(selectedFixture, fixture) && FixtureHost.Children.Count == 1 && ReferenceEquals(FixtureHost.Children[0], fixture), "Capture lost its selected fixture.");
        await File.WriteAllTextAsync(path + ".layout.json", System.Text.Json.JsonSerializer.Serialize(new { fixture = fixture.GetType().Name, milliseconds = watch.Elapsed.TotalMilliseconds, frames, stable, CatalogScroll.VerticalOffset, CatalogScroll.HorizontalOffset, fixture.ActualWidth, fixture.ActualHeight }));
        await CaptureAsync(path);
    }

    private async Task VerifyNavigationAsync()
    {
        await SelectFixtureAsync(Foundation);
        HeadlineInput.Text = "Preserved while browsing";
        ComponentSearch.Text = "iNpUt";
        var expected = components.Where(entry => entry.Title.Contains("iNpUt", StringComparison.OrdinalIgnoreCase)).ToArray();
        Require(expected.Any(entry => ReferenceEquals(entry.Fixture, Inputs)) && expected.Any(entry => ReferenceEquals(entry.Fixture, SearchInputs)), "Navigation regression must cover both input fixtures.");
        bool Matches()
        {
            var actual = ComponentNavigation.Children.OfType<BleeckerButton>().ToArray();
            return actual.Length == ComponentNavigation.Children.Count && actual.Length == expected.Length &&
                actual.Zip(expected).All(pair => Equals(pair.First.Content, pair.Second.Title) && ReferenceEquals(pair.First.Tag, pair.Second.Fixture));
        }
        await AwaitNavigationAsync(Matches, "Component search did not return every matching fixture in registry order.");
        CatalogRoot.UpdateLayout();
        Require(Matches(), "Component search changed its matching fixtures or labels after layout.");
        var button = ComponentNavigation.Children.OfType<BleeckerButton>().Single(entry => ReferenceEquals(entry.Tag, Inputs));
        button.ApplyTemplate();
        var native = (Button)((FrameworkElement)VisualTreeHelper.GetChild(button, 0)).FindName("PART_Action");
        ((IInvokeProvider)new ButtonAutomationPeer(native).GetPattern(PatternInterface.Invoke)).Invoke();
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (!ReferenceEquals(selectedFixture, Inputs) && elapsed.Elapsed < TimeSpan.FromSeconds(1)) await Task.Delay(16);
        Require(ReferenceEquals(selectedFixture, Inputs), "Navigation activation did not select the Input fixture.");
        await SelectFixtureAsync(Inputs);
        Require(!Foundation.IsLoaded, "Previous fixture remained loaded after navigation.");
        ComponentSearch.Text = "no-such-component";
        await AwaitNavigationAsync(() => ComponentNavigation.Children.Count == 0, "Unmatched search retained component results.");
        Require(ComponentNavigation.Children.Count == 0 && NavigationStatus.Text.StartsWith("No matching components.", StringComparison.Ordinal) &&
            ReferenceEquals(selectedFixture, Inputs), "Empty search results discarded the active fixture.");
        ComponentSearch.Text = "";
        await AwaitNavigationAsync(() => ComponentNavigation.Children.Count == components.Count, "Clearing search did not restore all components.");
        Require(ComponentNavigation.Children.Count == components.Count, "Clearing search did not restore all components.");
        await SelectFixtureAsync(Foundation);
        Require(HeadlineInput.Text == "Preserved while browsing" && !Inputs.IsLoaded, "Navigation discarded consumer state or retained the previous fixture.");
    }

    private static async Task AwaitNavigationAsync(Func<bool> condition, string message)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && elapsed.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(16);
        Require(condition(), message);
    }
}
