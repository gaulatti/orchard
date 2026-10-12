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
            ("Loading spinners", LoadingSpinners), ("Cards", Cards), ("Separators", Separators),
            ("Status badges", StatusBadges), ("Menus", Menus), ("Notifications", Notifications),
            ("Tooltips", Tooltips), ("Field help", FieldHelp), ("Labels", Labels), ("Input", Inputs), ("Keyboard shortcuts", Keyboard), ("Eyebrows", Eyebrows)
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

    private async Task VerifyNavigationAsync()
    {
        await SelectFixtureAsync(Foundation);
        HeadlineInput.Text = "Preserved while browsing";
        ComponentSearch.Text = "iNpUt";
        await AwaitNavigationAsync(() => ComponentNavigation.Children.Count == 1, "Component search did not filter case-insensitively.");
        CatalogRoot.UpdateLayout();
        Require(ComponentNavigation.Children.Count == 1, "Component search did not filter case-insensitively.");
        var button = (BleeckerButton)ComponentNavigation.Children[0];
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
