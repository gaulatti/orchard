using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunChartLoadFocusAsync(string folder)
    {
        using var input = new NativeInputSession(CatalogRoot.XamlRoot, folder, "chart-load-focus");
        await input.AcquireAsync(ComponentSearch);
        var host = new Grid { Width = 640, Height = 340, Margin = new Thickness(20),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Background = CatalogRoot.Background };
        Grid.SetRowSpan(host, CatalogRoot.RowDefinitions.Count); Grid.SetColumnSpan(host, CatalogRoot.ColumnDefinitions.Count);
        var outside = new Button { Content = "Outside chart", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom };
        host.Children.Add(outside); CatalogRoot.Children.Add(host);
        var reports = new List<object>(); var failures = new List<string>(); var traces = new List<object>();
        try
        {
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                CatalogRoot.RequestedTheme = theme; await AwaitPresentationAsync(host);
                foreach (var profile in new[] { "bar", "sankey" })
                {
                    await input.MoveAsync(outside); Require(outside.Focus(FocusState.Keyboard), "Outside chart refused initial focus.");
                    Control wrapper;
                    if (profile == "bar")
                    {
                        var bar = new BleeckerBarChart { XAxisKey = "label", ShowLegend = false };
                        bar.Series.Add(new BleeckerChartSeries { Key = "v", Name = "Consumer" });
                        bar.Data.Add(new Dictionary<string, object?> { ["label"] = "A", ["v"] = 10d });
                        bar.Data.Add(new Dictionary<string, object?> { ["label"] = "B", ["v"] = 20d });
                        wrapper = bar;
                    }
                    else
                    {
                        wrapper = new BleeckerSankeyChart { Data = new()
                        {
                            Nodes = [new BleeckerSankeyNode { Name = "A" }, new BleeckerSankeyNode { Name = "B" }],
                            Links = [new BleeckerSankeyLink { Source = 0, Target = 1, Value = 10 }]
                        } };
                    }
                    wrapper.Width = 320; wrapper.Height = 240;
                    wrapper.HorizontalAlignment = HorizontalAlignment.Left; wrapper.VerticalAlignment = VerticalAlignment.Top;
                    host.Children.Insert(0, wrapper); CatalogRoot.UpdateLayout(); await AwaitPresentationAsync(wrapper); await AwaitPresentationAsync(wrapper);
                    var chart = LoadFocusDescendants<BleeckerLineChart>(wrapper).Single();
                    var trace = new List<object>(); var cycle = -1; var armed = false; var accepted = false; var eager = false; var gotFocusBeforeLoaded = false;
                    traces.Add(new { profile, theme = theme.ToString(), trace });
                    var loadingFocused = false; var loadingEvents = 0; var loadedEvents = 0;
                    void Snapshot(string phase) => trace.Add(new { phase, cycle, chart.IsLoaded, focusState = chart.FocusState.ToString(),
                        focused = ReferenceEquals(FocusManager.GetFocusedElement(CatalogRoot.XamlRoot), chart), help = AutomationProperties.GetHelpText(chart) });
                    chart.GotFocus += (_, _) => { if (armed && loadedEvents == 0) gotFocusBeforeLoaded = true; Snapshot("got-focus"); };
                    chart.LostFocus += (_, _) => Snapshot("lost-focus");
                    chart.Loading += (_, _) =>
                    {
                        if (!armed) return;
                        loadingEvents++; Snapshot("loading");
                        if (!eager)
                        {
                            accepted = chart.Focus(FocusState.Keyboard);
                            loadingFocused = ReferenceEquals(FocusManager.GetFocusedElement(CatalogRoot.XamlRoot), chart);
                            Snapshot("loading-focus");
                        }
                    };
                    chart.Loaded += (_, _) => { if (armed) { loadedEvents++; Snapshot("loaded"); } };
                    Check(AutomationProperties.GetHelpText(chart) == "", "Unfocused initial mount inspected a record.");
                    for (cycle = 0; cycle < 6; cycle++)
                    {
                        eager = cycle >= 3; armed = false;
                        await input.MoveAsync(outside); Require(outside.Focus(FocusState.Keyboard), "Outside chart refused focus.");
                        if (!eager)
                        {
                            await AwaitPresentationAsync(host); await AwaitPresentationAsync(host);
                            Check(AutomationProperties.GetHelpText(chart) == "", "Genuine blur retained inspection.");
                            Snapshot("after-blur");
                        }
                        host.Children.Remove(wrapper);
                        if (!eager) { await AwaitPresentationAsync(host); await AwaitPresentationAsync(host); }
                        Require(!wrapper.IsLoaded && !chart.IsLoaded, "Chart did not genuinely unmount before focus regression.");
                        accepted = loadingFocused = gotFocusBeforeLoaded = false; loadingEvents = loadedEvents = 0; armed = true;
                        host.Children.Insert(0, wrapper); wrapper.ApplyTemplate(); CatalogRoot.UpdateLayout();
                        if (eager)
                        {
                            accepted = chart.Focus(FocusState.Keyboard);
                            loadingFocused = ReferenceEquals(FocusManager.GetFocusedElement(CatalogRoot.XamlRoot), chart);
                            Snapshot("eager-focus");
                        }
                        await AwaitPresentationAsync(wrapper); await AwaitPresentationAsync(wrapper);
                        Check(loadingEvents == 1 && loadedEvents == 1, "Reload did not deliver one native Loading/Loaded pair.");
                        Check(accepted && loadingFocused, "Native focus was not accepted during the mount boundary.");
                        Check(ReferenceEquals(FocusManager.GetFocusedElement(CatalogRoot.XamlRoot), chart), "Loaded changed the native focus owner.");
                        var expected = profile == "bar" ? "A;  : 10" : "A : 10";
                        var actual = AutomationProperties.GetHelpText(chart);
                        Check(actual == expected, $"Loaded lost first inspection: actual='{actual}', expected='{expected}'.");
                        if (ReferenceEquals(FocusManager.GetFocusedElement(CatalogRoot.XamlRoot), chart))
                        {
                            await input.KeyAsync(0x27, chart); await AwaitPresentationAsync(chart);
                            Check(AutomationProperties.GetHelpText(chart) == (profile == "bar" ? "B;  : 20" : "B : 10"), "Physical Right lost the original consumer's next record.");
                        }
                        Snapshot("after-right");
                        reports.Add(new { profile, theme = theme.ToString(), cycle, mode = eager ? "eager-reparent" : "loading", acceptedFocus = accepted,
                            focusedBeforePresentation = loadingFocused, gotFocusBeforeLoaded, loadingEvents, loadedEvents, firstHelp = actual, expectedFirstHelp = expected,
                            afterRight = AutomationProperties.GetHelpText(chart) });
                        await File.WriteAllTextAsync(Path.Combine(folder, "chart-load-focus.json"), JsonSerializer.Serialize(new { reports, failures, traces }, new JsonSerializerOptions { WriteIndented = true }));
                    }
                    armed = false;
                    await CaptureElementAsync(host, Path.Combine(folder, $"chart-load-focus-{profile}-{theme}.png"));
                    host.Children.Remove(wrapper); await AwaitPresentationAsync(host);
                    void Check(bool condition, string message) { if (!condition) failures.Add($"{profile}/{theme}/{cycle}: {message}"); }
                }
                await CaptureElementAsync(host, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
            }
        }
        finally { CatalogRoot.Children.Remove(host); }
        Require(failures.Count == 0, "Chart load/focus regression: " + string.Join("; ", failures.Take(8)));
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "chart-load-focus", cases = reports.Count, environment = RuntimeEnvironment() }));
    }
    private static IEnumerable<T> LoadFocusDescendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i); if (child is T found) yield return found;
            foreach (var nested in LoadFocusDescendants<T>(child)) yield return nested;
        }
    }
}
