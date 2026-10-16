using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.ViewManagement;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Catalog;

public sealed partial class MainWindow : Window
{
    private int actionCount;
    private bool smokeStarted;

    public MainWindow()
    {
        InitializeComponent();
        InitializeNavigation();
        AppWindow.Resize(new SizeInt32(1100, 900));
    }

    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CatalogRoot is null) return; // SelectionChanged can fire during XAML initialization.
        CatalogRoot.RequestedTheme = ((ComboBox)sender).SelectedIndex switch
        {
            0 => ElementTheme.Default,
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => throw new InvalidOperationException("Unknown catalog appearance.")
        };
    }

    private void OnPrimaryAction(object sender, RoutedEventArgs e)
    {
        actionCount++;
        ActionResult.Text = $"Action invoked {actionCount} time{(actionCount == 1 ? "" : "s")}.";
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, "--smoke-output");
        if (index < 0 || smokeStarted) return;
        smokeStarted = true;
        if (index + 1 >= args.Length) { Environment.Exit(2); return; }
        var folder = Path.GetFullPath(args[index + 1]);
        Directory.CreateDirectory(folder);
        try
        {
            CatalogRuntimeProfile.Start(folder);
            if (args.Contains("--smoke-brush-alpha")) await RunBrushAlphaProbeAsync(folder);
            else if (args.Contains("--smoke-stepper"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(Steppers);
                    await Steppers.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await CaptureFixtureAsync(Steppers, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "stepper", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-tooltips")) await RunTooltipSmokeAsync(folder);
            else if (args.Contains("--smoke-input")) await RunInputSmokeAsync(folder);
            else if (args.Contains("--smoke-sidebar"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme=theme;await SelectFixtureAsync(Sidebars);
                    await Sidebars.VerifyAsync(theme==ElementTheme.Light?BleeckerColorScheme.Light:BleeckerColorScheme.Dark,folder);
                    await CaptureFixtureAsync(Sidebars,Path.Combine(folder,$"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="sidebar",environment=RuntimeEnvironment()}));
            }
            else if (args.Contains("--smoke-menus"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(Menus);
                    await Menus.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await CaptureFixtureAsync(Menus, Path.Combine(folder,$"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new { passed = true, component = "menus", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-select-shadow"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(DataTableFilters);
                    await DataTableFilters.VerifyShadowPointerAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await CaptureFixtureAsync(DataTableFilters, Path.Combine(folder,$"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new { passed = true, component = "select-shadow", environment = RuntimeEnvironment() }));
            }
            else if(args.Contains("--smoke-data-table-filters") || args.Contains("--smoke-data-table-filters-native") || args.Contains("--smoke-data-table-filters-live"))
            {
                var filterFailures = new List<string>();
                var desktopFocusContract = args.Contains("--smoke-data-table-filters-native");
                var liveOptions = args.Contains("--smoke-data-table-filters-live");
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(DataTableFilters);
                    try { await DataTableFilters.VerifySourceAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder, desktopFocusContract, liveOptions); await DataTableFilters.VerifyLifecycleAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder); }
                    catch (Exception error) { filterFailures.Add(theme + ": " + error); }
                    await CaptureFixtureAsync(DataTableFilters, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                Require(filterFailures.Count == 0, "Filter source replay failed; both theme reports retain all independent evidence: " + string.Join("; ", filterFailures));
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = liveOptions ? "data-table-filters-live" : desktopFocusContract ? "data-table-filters-native" : "data-table-filters", environment = RuntimeEnvironment() }));
            }
            else if(args.Contains("--smoke-data-table-column-toggle"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(DataTableColumnToggles);
                    await DataTableColumnToggles.VerifySourceAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await DataTableColumnToggles.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await CaptureFixtureAsync(DataTableColumnToggles, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "data-table-column-toggle", environment = RuntimeEnvironment() }));
            }
            else if(args.Contains("--smoke-data-table-pagination-scroll"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(DataTablePaginations);
                    await DataTablePaginations.VerifyScrollAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await CaptureFixtureAsync(DataTablePaginations, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "data-table-pagination-scroll", environment = RuntimeEnvironment() }));
            }
            else if(args.Contains("--smoke-data-table-pagination-keyboard") || args.Contains("--smoke-data-table-pagination-paired"))
            {
                var pairedKeys = args.Contains("--smoke-data-table-pagination-paired");
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(DataTablePaginations);
                    await DataTablePaginations.VerifyKeyboardAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder, pairedKeys);
                    await CaptureFixtureAsync(DataTablePaginations, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = pairedKeys ? "data-table-pagination-paired" : "data-table-pagination-keyboard", environment = RuntimeEnvironment() }));
            }
            else if(args.Contains("--smoke-data-table-pagination-input"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(DataTablePaginations);
                    await DataTablePaginations.VerifyPhysicalAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await CaptureFixtureAsync(DataTablePaginations, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "data-table-pagination-input", environment = RuntimeEnvironment() }));
            }
            else if(args.Contains("--smoke-data-table-pagination-source"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(DataTablePaginations);
                    await DataTablePaginations.VerifySourceAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await CaptureFixtureAsync(DataTablePaginations, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "data-table-pagination-source", environment = RuntimeEnvironment() }));
            }
            else if(args.Contains("--smoke-data-table-pagination"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(DataTablePaginations);
                    await DataTablePaginations.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await CaptureFixtureAsync(DataTablePaginations, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "data-table-pagination", environment = RuntimeEnvironment() }));
            }
            else if(args.Contains("--smoke-data-table-toolbar-press"))
            {
                var results = new List<bool>();
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(DataTableToolbars);
                    results.Add(await DataTableToolbars.VerifyPressAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder));
                    await CaptureFixtureAsync(DataTableToolbars, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                Require(results.All(passed => passed), "Toolbar press paths differ from the frozen source; see per-state reports.");
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "data-table-toolbar-press", environment = RuntimeEnvironment() }));
            }
            else if(args.Contains("--smoke-data-table-toolbar-interactions"))
            {
                var results=new List<object>();
                foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
                {
                    CatalogRoot.RequestedTheme=theme;await SelectFixtureAsync(DataTableToolbars);
                    results.Add(await DataTableToolbars.VerifyInteractionsAsync(theme==ElementTheme.Light?BleeckerColorScheme.Light:BleeckerColorScheme.Dark,folder));
                    await CaptureFixtureAsync(DataTableToolbars,Path.Combine(folder,$"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="data-table-toolbar-interactions",environment=RuntimeEnvironment(),results}));
            }
            else if(args.Contains("--smoke-data-table-toolbar"))
            {
                foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
                {
                    CatalogRoot.RequestedTheme=theme;await SelectFixtureAsync(DataTableToolbars);
                    await DataTableToolbars.VerifyAsync(theme==ElementTheme.Light?BleeckerColorScheme.Light:BleeckerColorScheme.Dark,folder);
                    await CaptureFixtureAsync(DataTableToolbars,Path.Combine(folder,$"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="data-table-toolbar",environment=RuntimeEnvironment()}));
            }
            else if (args.Contains("--smoke-breadcrumb")) await RunBreadcrumbSmokeAsync(folder);
            else if (args.Contains("--smoke-alerts")) await RunAlertSmokeAsync(folder);
            else if (args.Contains("--smoke-brand")) await RunBrandSmokeAsync(folder);
            else if (args.Contains("--smoke-bauhaus")) await RunBauhausSmokeAsync(folder);
            else if (args.Contains("--smoke-switches")) await RunSwitchSmokeAsync(folder);
            else if (args.Contains("--smoke-checkboxes"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    await SelectFixtureAsync(Foundation); CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(Checkboxes);
                    await Checkboxes.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "checkboxes", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-activity")) await RunActivitySmokeAsync(folder);
            else if (args.Contains("--smoke-attention")) await RunAttentionSmokeAsync(folder);
            else if (args.Contains("--smoke-notifications")) await RunNotificationSmokeAsync(folder);
            else if (args.Contains("--smoke-presentation")) await RunPresentationSmokeAsync(folder);
            else if (args.Contains("--smoke-chart-theme")) await RunChartThemeSmokeAsync(folder);
            else if (args.Contains("--smoke-line-chart")) await RunLineChartSmokeAsync(folder);
            else if (args.Contains("--smoke-currency-chart") || args.Contains("--smoke-currency-chart-sequence"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    var scheme = theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark;
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(CurrencyCharts); await CurrencyCharts.VerifyAsync(scheme, folder);
                    if (args.Contains("--smoke-currency-chart-sequence"))
                    {
                        await SelectFixtureAsync(LineCharts); await LineCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(AreaCharts); await AreaCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(BarCharts); await BarCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(ScatterCharts); await ScatterCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(RadarCharts); await RadarCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(RadialBarCharts); await RadialBarCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(FunnelCharts); await FunnelCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(SankeyCharts); await SankeyCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(SunburstCharts); await SunburstCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(Sparklines); await Sparklines.VerifyAsync(scheme, folder);
                    }
                    await CaptureFixtureAsync(CurrencyCharts, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "currency-chart", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-sunburst-chart") || args.Contains("--smoke-sunburst-chart-sequence"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    var scheme = theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark;
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(SunburstCharts); await SunburstCharts.VerifyAsync(scheme, folder);
                    if (args.Contains("--smoke-sunburst-chart-sequence"))
                    {
                        await SelectFixtureAsync(SankeyCharts); await SankeyCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(FunnelCharts); await FunnelCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(RadialBarCharts); await RadialBarCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(RadarCharts); await RadarCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(ScatterCharts); await ScatterCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(Sparklines); await Sparklines.VerifyAsync(scheme, folder);
                    }
                    await CaptureFixtureAsync(SunburstCharts, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "sunburst-chart", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-sankey-chart") || args.Contains("--smoke-sankey-chart-sequence"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    var scheme = theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark;
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(SankeyCharts); await SankeyCharts.VerifyAsync(scheme, folder);
                    if (args.Contains("--smoke-sankey-chart-sequence"))
                    {
                        await SelectFixtureAsync(FunnelCharts); await FunnelCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(RadialBarCharts); await RadialBarCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(RadarCharts); await RadarCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(ScatterCharts); await ScatterCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(Sparklines); await Sparklines.VerifyAsync(scheme, folder);
                    }
                    await CaptureFixtureAsync(SankeyCharts, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "sankey-chart", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-funnel-chart") || args.Contains("--smoke-funnel-chart-sequence"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    var scheme = theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark;
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(FunnelCharts); await FunnelCharts.VerifyAsync(scheme, folder);
                    if (args.Contains("--smoke-funnel-chart-sequence"))
                    {
                        await SelectFixtureAsync(RadialBarCharts); await RadialBarCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(RadarCharts); await RadarCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(ScatterCharts); await ScatterCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(Sparklines); await Sparklines.VerifyAsync(scheme, folder);
                    }
                    await CaptureFixtureAsync(FunnelCharts, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "funnel-chart", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-radial-bar-chart") || args.Contains("--smoke-radial-bar-chart-sequence"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    var scheme = theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark;
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(RadialBarCharts); await RadialBarCharts.VerifyAsync(scheme, folder);
                    if (args.Contains("--smoke-radial-bar-chart-sequence"))
                    {
                        await SelectFixtureAsync(RadarCharts); await RadarCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(ScatterCharts); await ScatterCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(Sparklines); await Sparklines.VerifyAsync(scheme, folder);
                    }
                    await CaptureFixtureAsync(RadialBarCharts, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "radial-bar-chart", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-radar-chart") || args.Contains("--smoke-radar-chart-sequence"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    var scheme = theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark;
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(RadarCharts);
                    await RadarCharts.VerifyAsync(scheme, folder);
                    if (args.Contains("--smoke-radar-chart-sequence"))
                    {
                        await SelectFixtureAsync(ScatterCharts); await ScatterCharts.VerifyAsync(scheme, folder);
                        await SelectFixtureAsync(Sparklines); await Sparklines.VerifyAsync(scheme, folder);
                    }
                    await CaptureFixtureAsync(RadarCharts, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "radar-chart", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-scatter-chart") || args.Contains("--smoke-scatter-chart-sequence"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(ScatterCharts);
                    await ScatterCharts.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    if (args.Contains("--smoke-scatter-chart-sequence"))
                    {
                        await SelectFixtureAsync(Sparklines);
                        await Sparklines.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    }
                    await CaptureFixtureAsync(ScatterCharts, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "scatter-chart", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-pie-chart") || args.Contains("--smoke-donut-chart"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(PieCharts);
                    await PieCharts.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "pie-chart", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-area-chart"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(AreaCharts);
                    await AreaCharts.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "area-chart", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-bar-chart") || args.Contains("--smoke-bar-chart-sequence"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(BarCharts);
                    await BarCharts.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    if (args.Contains("--smoke-bar-chart-sequence"))
                    {
                        await SelectFixtureAsync(AreaCharts);
                        await AreaCharts.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                        await SelectFixtureAsync(Sparklines);
                        await Sparklines.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                        await SelectFixtureAsync(StatCards);
                        await StatCards.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    }
                    await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "bar-chart", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-sparkline"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(Sparklines);
                    await Sparklines.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "sparkline", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-stat-card"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(StatCards);
                    await StatCards.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "stat-card", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-font-audit")) await RunFontAuditAsync(folder);
            else if (args.Contains("--smoke-dashboard-grid"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(DashboardGrids);
                    await DashboardGrids.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "dashboard-grid", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-dashboard-section"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(DashboardSections);
                    await DashboardSections.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "dashboard-section", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-section-header"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(SectionHeaders);
                    await SectionHeaders.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "section-header", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-page-header"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(PageHeaders);
                    await PageHeaders.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "page-header", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-error-state") || args.Contains("--smoke-error-state-after-chart"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    await SelectFixtureAsync(Foundation); CatalogRoot.RequestedTheme = theme;
                    if (args.Contains("--smoke-error-state-after-chart"))
                    {
                        await SelectFixtureAsync(LineCharts);
                        await LineCharts.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark,folder);
                    }
                    await SelectFixtureAsync(ErrorStates);
                    await ErrorStates.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
                    await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component=args.Contains("--smoke-error-state-after-chart")?"error-state-after-chart":"error-state",environment=RuntimeEnvironment()}));
            }
            else if (args.Contains("--smoke-error-state-contrast"))
            {
                CatalogRoot.RequestedTheme=ElementTheme.Dark;await SelectFixtureAsync(ErrorStates);
                await ErrorStates.VerifyContrastAsync(folder);
                await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="error-state-contrast",environment=RuntimeEnvironment()}));
            }
            else if (args.Contains("--smoke-contrast-entry"))
            {
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    await SelectFixtureAsync(Foundation); CatalogRoot.RequestedTheme = theme; await SelectFixtureAsync(Contrast);
                    await Contrast.VerifyKeyboardEntryAsync(folder, theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark);
                    await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
                }
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "contrast-entry", environment = RuntimeEnvironment() }));
            }
            else if (args.Contains("--smoke-contrast"))
            {
                CatalogRoot.RequestedTheme = ElementTheme.Dark;
                await SelectFixtureAsync(Contrast);
                await Contrast.VerifyAsync(CatalogRoot, folder);
                await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "contrast", environment = RuntimeEnvironment() }));
            }
            else await RunSmokeAsync(folder);
            CatalogRuntimeProfile.Finish(true);
            Environment.Exit(0);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"),
                JsonSerializer.Serialize(new { passed = false, error = error.ToString(), environment = RuntimeEnvironment() }, new JsonSerializerOptions { WriteIndented = true }));
            CatalogRuntimeProfile.Finish(false);
            Environment.Exit(1);
        }
    }

    // Catalog-only runtime check. The reusable library has no command-line or automation dependencies.
    private async Task RunLineChartSmokeAsync(string folder)
    {
        foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
        {
            await SelectFixtureAsync(Foundation);CatalogRoot.RequestedTheme=theme;await SelectFixtureAsync(LineCharts);
            await LineCharts.VerifyAsync(theme==ElementTheme.Light?BleeckerColorScheme.Light:BleeckerColorScheme.Dark,folder);
            await CaptureAsync(Path.Combine(folder,$"catalog-{theme.ToString().ToLowerInvariant()}.png"));
        }
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="line-chart",environment=RuntimeEnvironment()}));
    }
    private async Task RunChartThemeSmokeAsync(string folder)
    {
        foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
        {
            await SelectFixtureAsync(Foundation);CatalogRoot.RequestedTheme=theme;await SelectFixtureAsync(ChartThemes);
            await ChartThemes.VerifyAsync(CatalogRoot,theme==ElementTheme.Light?BleeckerColorScheme.Light:BleeckerColorScheme.Dark,folder);
            await CaptureAsync(Path.Combine(folder,$"catalog-{theme.ToString().ToLowerInvariant()}.png"));
        }
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="chart-theme",environment=RuntimeEnvironment()}));
    }
    private async Task RunNotificationSmokeAsync(string folder)
    {
        foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
        {
            await SelectFixtureAsync(Foundation);CatalogRoot.RequestedTheme=theme;await SelectFixtureAsync(Notifications);
            await Notifications.VerifyAsync(theme==ElementTheme.Light?BleeckerColorScheme.Light:BleeckerColorScheme.Dark,folder);
            await CaptureAsync(Path.Combine(folder,$"catalog-{theme.ToString().ToLowerInvariant()}.png"));
        }
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="notifications",environment=RuntimeEnvironment()}));
    }
    private async Task RunAttentionSmokeAsync(string folder)
    {
        foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
        {
            await SelectFixtureAsync(Foundation);CatalogRoot.RequestedTheme=theme;await SelectFixtureAsync(AttentionSurfaces);
            await AttentionSurfaces.VerifyAsync(theme==ElementTheme.Light?BleeckerColorScheme.Light:BleeckerColorScheme.Dark,folder);
            await CaptureAsync(Path.Combine(folder,$"catalog-{theme.ToString().ToLowerInvariant()}.png"));
        }
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="attention",environment=RuntimeEnvironment()}));
    }
    private async Task RunActivitySmokeAsync(string folder)
    {
        foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
        {
            await SelectFixtureAsync(Foundation);CatalogRoot.RequestedTheme=theme;await SelectFixtureAsync(ActivityItems);
            await ActivityItems.VerifyAsync(theme==ElementTheme.Light?BleeckerColorScheme.Light:BleeckerColorScheme.Dark,folder);
            await CaptureAsync(Path.Combine(folder,$"catalog-{theme.ToString().ToLowerInvariant()}.png"));
        }
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="activity",environment=RuntimeEnvironment()}));
    }
    private async Task RunSwitchSmokeAsync(string folder)
    {
        foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
        {
            await SelectFixtureAsync(Foundation);CatalogRoot.RequestedTheme=theme;await SelectFixtureAsync(Switches);
            await Switches.VerifyAsync(theme==ElementTheme.Light?BleeckerColorScheme.Light:BleeckerColorScheme.Dark,folder,physicalInput:true);
            await CaptureAsync(Path.Combine(folder,$"catalog-{theme.ToString().ToLowerInvariant()}.png"));
        }
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="switches",environment=RuntimeEnvironment()}));
    }

    private async Task RunBauhausSmokeAsync(string folder)
    {
        foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
        {
            await SelectFixtureAsync(Foundation);CatalogRoot.RequestedTheme=theme;await SelectFixtureAsync(BauhausBackgrounds);
            await BauhausBackgrounds.VerifyAsync(theme==ElementTheme.Light?BleeckerColorScheme.Light:BleeckerColorScheme.Dark,folder);
            await CaptureAsync(Path.Combine(folder,$"catalog-{theme.ToString().ToLowerInvariant()}.png"));
        }
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="bauhaus",environment=RuntimeEnvironment()}));
    }

    private async Task RunBrandSmokeAsync(string folder)
    {
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            await SelectFixtureAsync(Foundation); CatalogRoot.RequestedTheme = theme;
            await SelectFixtureAsync(BrandLockups);
            await BrandLockups.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
            await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "brand", environment = RuntimeEnvironment() }));
    }

    private async Task RunAlertSmokeAsync(string folder)
    {
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            await SelectFixtureAsync(Foundation);
            CatalogRoot.RequestedTheme = theme;
            await SelectFixtureAsync(Alerts);
            await Alerts.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
            await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "alerts", environment = RuntimeEnvironment() }));
    }

    private async Task RunBreadcrumbSmokeAsync(string folder)
    {
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            await SelectFixtureAsync(Foundation);
            CatalogRoot.RequestedTheme = theme;
            await SelectFixtureAsync(Breadcrumbs);
            await Breadcrumbs.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
            await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "breadcrumb", environment = RuntimeEnvironment() }));
    }

    private async Task RunTooltipSmokeAsync(string folder)
    {
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            await SelectFixtureAsync(Foundation);
            CatalogRoot.RequestedTheme = theme;
            await SelectFixtureAsync(Tooltips);
            await Tooltips.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
            await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "tooltips", environment = RuntimeEnvironment() }));
    }

    private async Task RunInputSmokeAsync(string folder)
    {
        await SelectFixtureAsync(Inputs);
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            CatalogRoot.RequestedTheme = theme;
            await Task.Delay(250);
            CatalogRoot.UpdateLayout();
            await Inputs.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
            await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "input", environment = RuntimeEnvironment() }));
    }

    private async Task RunSmokeAsync(string folder)
    {
        await VerifyNavigationAsync();
        var captures = new List<object>();
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            await SelectFixtureAsync(Foundation);
            ThemeSelector.SelectedIndex = theme == ElementTheme.Light ? 1 : 2;
            await AwaitPresentationAsync(CatalogRoot);
            CatalogRoot.UpdateLayout();
            Require(CatalogRoot.ActualWidth > 0 && PrimaryAction.ActualWidth > 0, "Controls did not lay out.");
            var scheme = theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark;
            await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
            Require(PrimaryAction.Background is SolidColorBrush brush &&
                ColorHex(brush.Color) == BleeckerTokens.Colors(scheme)["primary"],
                $"Primary action does not use the Bleecker theme: requested={theme}, actual={CatalogRoot.ActualTheme}, background={DescribeBrush(PrimaryAction.Background)}, expected={BleeckerTokens.Colors(scheme)["primary"]}.");
            await VerifyButtonStatesAsync(PrimaryAction, scheme);
            await VerifyButtonStatesAsync(DisabledAction, scheme, disabled: true);
            VerifyButtonGroup();
            await SelectFixtureAsync(Toggles);
            await Toggles.VerifyAsync(scheme);
            await SelectFixtureAsync(Buttons);
            using (var buttonInput = new NativeInputSession(CatalogRoot.XamlRoot, folder, $"buttons-rest-{scheme}"))
            {
                var heading = (FrameworkElement)((StackPanel)Buttons.Content).Children[0];
                await buttonInput.AcquireAsync(heading);
                await buttonInput.MoveAsync(heading);
                await Buttons.VerifyAsync(scheme);
            }
            await SelectFixtureAsync(IconButtons);
            await IconButtons.VerifyAsync(scheme);
            await SelectFixtureAsync(ToggleGroups);
            await ToggleGroups.VerifyAsync();
            await SelectFixtureAsync(LoadingSpinners);
            await LoadingSpinners.VerifyAsync(scheme);
            await SelectFixtureAsync(Cards);
            await Cards.VerifyAsync(scheme);
            await SelectFixtureAsync(Skeletons);
            await Skeletons.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(Avatars);
            await Avatars.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(Breadcrumbs);
            await Breadcrumbs.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(Separators);
            await Separators.VerifyAsync(scheme);
            await SelectFixtureAsync(StatusBadges);
            await StatusBadges.VerifyAsync(scheme);
            await SelectFixtureAsync(Menus);
            await Menus.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(Notifications);
            await Notifications.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(Alerts);
            await Alerts.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(BrandLockups);
            await BrandLockups.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(BauhausBackgrounds);
            await BauhausBackgrounds.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(ActivityItems);
            await ActivityItems.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(AttentionSurfaces);
            await AttentionSurfaces.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(ChartThemes);
            await ChartThemes.VerifyAsync(CatalogRoot,scheme,folder);
            await SelectFixtureAsync(LineCharts);
            await LineCharts.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(ErrorStates);
            await ErrorStates.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(PageHeaders);
            await PageHeaders.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(SectionHeaders);
            await SectionHeaders.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(DashboardSections);
            await DashboardSections.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(DashboardGrids);
            await DashboardGrids.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(Sidebars);
            await Sidebars.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(Steppers);
            await Steppers.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(DataTableToolbars);
            await DataTableToolbars.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(DataTablePaginations);
            await DataTablePaginations.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(DataTableColumnToggles);
            await DataTableColumnToggles.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(DataTableFilters);
            await DataTableFilters.VerifyLifecycleAsync(scheme,folder);
            await CaptureFixtureAsync(DataTableFilters,Path.Combine(folder,$"data-table-filters-{scheme}.png"));
            await SelectFixtureAsync(BarCharts);
            await BarCharts.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(AreaCharts);
            await AreaCharts.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(PieCharts);
            await PieCharts.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(ScatterCharts);
            await ScatterCharts.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(RadarCharts);
            await RadarCharts.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(RadialBarCharts);
            await RadialBarCharts.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(FunnelCharts);
            await FunnelCharts.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(SankeyCharts);
            await SankeyCharts.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(SunburstCharts);
            await SunburstCharts.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(CurrencyCharts);
            await CurrencyCharts.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(Sparklines);
            await Sparklines.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(StatCards);
            await StatCards.VerifyAsync(scheme,folder);
            await SelectFixtureAsync(Tooltips);
            await Tooltips.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(FieldHelp);
            await FieldHelp.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(Labels);
            await Labels.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(Inputs);
            await Inputs.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(Keyboard);
            await Keyboard.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(Eyebrows);
            await Eyebrows.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(Progress);
            await Progress.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(Checkboxes);
            await Checkboxes.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(Switches);
            await Switches.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(Contrast);
            await Contrast.VerifyKeyboardEntryAsync(folder, scheme);
            foreach (var (fixture, name) in new (FrameworkElement Fixture, string Name)[]
            {
                (StatusBadges, "status-badges"), (Separators, "separators"), (Cards, "cards"),
                (LoadingSpinners, "loading-spinners"), (ToggleGroups, "toggle-groups"),
                (IconButtons, "icon-buttons"), (Buttons, "buttons"), (Toggles, "toggles")
            })
                await CaptureFixtureAsync(fixture, Path.Combine(folder, $"{name}-{theme.ToString().ToLowerInvariant()}.png"));
            await SelectFixtureAsync(Foundation);
            CatalogScroll.ChangeView(null, 0, null, disableAnimation: true);
            await AwaitPresentationAsync(Foundation);
            Require(HeadingSample.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf#Encode Sans", StringComparison.Ordinal), "Bundled headline font was not selected.");
            foreach (var font in new[] { "EncodeSans-Regular.ttf", "EncodeSans-Medium.ttf", "EncodeSans-SemiBold.ttf", "LibreFranklin-Regular.ttf", "LibreFranklin-SemiBold.ttf" })
                Require(File.Exists(Path.Combine(AppContext.BaseDirectory, "Orchard", "Assets", "Fonts", font)), $"Missing font: {font}");
            var peer = new ButtonAutomationPeer(PrimaryAction);
            ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();
            await AwaitPresentationAsync(Foundation);
            Require(actionCount == captures.Count + 1 && ActionResult.Text.StartsWith("Action invoked", StringComparison.Ordinal), "Button invocation did not reach the handler.");
            Require(!DisabledAction.IsEnabled, "Disabled action became enabled.");
            HeadlineInput.Text = "A Windows-native newsroom";
            Require(HeadlineInput.Text.Length > 0, "Text input rejected its value.");
            var size = await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
            captures.Add(new { theme = theme.ToString(), width = size.Width, height = size.Height });
        }
        ThemeSelector.SelectedIndex = 0;
        Require(CatalogRoot.RequestedTheme == ElementTheme.Default, "System appearance did not reset.");
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(
            new { passed = true, actionCount, systemAppearance = true, environment = RuntimeEnvironment(),
                buttonStates = new[] { "Normal", "PointerOver", "Pressed", "Disabled" },
                buttonSpinner = new { referenceVector = true, animationsEnabled = Buttons.SpinnerAnimationsEnabled,
                    rotationObserved = Buttons.SpinnerAnimationsEnabled, reducedMotionStaticObserved = !Buttons.SpinnerAnimationsEnabled, stopAndReset = true },
                buttonProperties = new[] { "Background", "Foreground", "BorderBrush", "Opacity" },
                loadingSpinner = new { sizes = new[] { 16, 32, 48 }, animationsEnabled = LoadingSpinners.AnimationsEnabled, hiddenAndUnloadedReset = true },
                toggles = new { automationOnOff = true, consumerStatePropagation = true, variants = new[] { "default", "outline" },
                    states = new[] { "Normal", "PointerOver", "Pressed", "Disabled", "Checked", "CheckedPointerOver", "CheckedPressed", "CheckedDisabled" } },
                captures }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private object RuntimeEnvironment()
    {
        var settings = new UISettings();
        return new
        {
            processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            osArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            osDescription = RuntimeInformation.OSDescription,
            framework = RuntimeInformation.FrameworkDescription,
            displayScale = CatalogRoot.XamlRoot?.RasterizationScale,
            textScale = settings.TextScaleFactor,
            animationsEnabled = settings.AnimationsEnabled,
            highContrast = new AccessibilitySettings().HighContrast
        };
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private void VerifyButtonGroup()
    {
        var first = (Button)ActionGroup.Children[0];
        var middle = (Button)ActionGroup.Children[1];
        var last = (Button)ActionGroup.Children[2];
        var radius = BleeckerTokens.Radii["button"];
        Require(first.CornerRadius.TopLeft == radius && first.CornerRadius.TopRight == 0,
            "Horizontal group first corners are incorrect.");
        Require(middle.CornerRadius == new CornerRadius(0) && first.BorderThickness.Right == 0 && last.BorderThickness.Right == 1,
            "Group internal corners/borders are incorrect.");
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(ActionGroup)?.GetAutomationControlType() == AutomationControlType.Group,
            "Button group has no accessible group role.");
        Require(!last.IsEnabled, "Group enabled a disabled action.");
        last.Visibility = Visibility.Collapsed;
        ActionGroup.UpdateLayout();
        Require(middle.CornerRadius.TopRight == radius && middle.BorderThickness.Right == 1,
            "Collapsed action left an internal edge at the end of the group.");
        last.Visibility = Visibility.Visible;
        ActionGroup.Orientation = Orientation.Vertical;
        ActionGroup.UpdateLayout();
        Require(first.CornerRadius.TopRight == radius && first.CornerRadius.BottomLeft == 0 && first.BorderThickness.Bottom == 0,
            "Vertical group did not update its edges.");
        ActionGroup.Orientation = Orientation.Horizontal;
        ActionGroup.Width = 280;
        ActionGroup.UpdateLayout();
        Require(first.ActualWidth + middle.ActualWidth + last.ActualWidth <= 281,
            "Constrained group overflowed its available width.");
        ActionGroup.ClearValue(FrameworkElement.WidthProperty);
        ActionGroup.UpdateLayout();
    }

    private static async Task VerifyButtonStatesAsync(Button button, BleeckerColorScheme scheme, bool disabled = false)
    {
        button.ApplyTemplate();
        var presenter = FindPresenter(button) ?? throw new InvalidOperationException("Button template has no content presenter.");
        foreach (var state in disabled ? new[] { "Disabled" } : new[] { "Normal", "PointerOver", "Pressed", "Normal" })
        {
            Require(VisualStateManager.GoToState(button, state, false), $"Missing native button state: {state}.");
            await AwaitPresentationAsync(button);
            foreach (var (property, actual) in new[] { ("Background", presenter.Background), ("Foreground", presenter.Foreground), ("BorderBrush", presenter.BorderBrush) })
            {
                var token = property == "Foreground" ? "primaryForeground" : "primary";
                var expected = BleeckerTokens.Colors(scheme)[token];
                var opacity = state switch { "Disabled" => 0.4, "Pressed" => 0.8, "PointerOver" => 0.9, _ => 1.0 };
                Require(actual is SolidColorBrush solid && ColorHex(solid.Color) == expected && Math.Abs(solid.Opacity - opacity) < 0.001,
                    $"{scheme} {state} {property}: rendered={DescribeBrush(actual)}, expected={expected} at opacity {opacity}.");
            }
        }
    }

    private static ContentPresenter? FindPresenter(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is ContentPresenter presenter) return presenter;
            if (FindPresenter(child) is { } nested) return nested;
        }
        return null;
    }

    private static string ColorHex(Windows.UI.Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    private static string DescribeBrush(Brush? brush) => brush is SolidColorBrush solid ? ColorHex(solid.Color) : brush?.GetType().FullName ?? "null";

    private Task<SizeInt32> CaptureAsync(string path) => CaptureElementAsync(CatalogRoot, path);
    internal static async Task<SizeInt32> CaptureElementAsync(UIElement visual, string path)
    {
        using var capture = CatalogRuntimeProfile.Measure("bitmap.total");
        var bitmap = new RenderTargetBitmap();
        using (CatalogRuntimeProfile.Measure("bitmap.render")) await bitmap.RenderAsync(visual);
        Require(bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0, "The catalog did not render.");
        Windows.Storage.Streams.IBuffer pixels;
        using (CatalogRuntimeProfile.Measure("bitmap.read")) pixels = await bitmap.GetPixelsAsync();
        var bytes = new byte[checked((int)pixels.Length)];
        using (var reader = DataReader.FromBuffer(pixels)) reader.ReadBytes(bytes);
        Require(bytes.Where((_, offset) => offset % 4 != 3).Distinct().Take(9).Count() > 8, "The render is blank.");
        await SavePngAsync(path, bytes, bitmap.PixelWidth, bitmap.PixelHeight);
        return new SizeInt32(bitmap.PixelWidth, bitmap.PixelHeight);
    }

    internal static async Task SavePngAsync(string path, byte[] pixels, int width, int height)
    {
        using var save = CatalogRuntimeProfile.Measure("png.total");
        // Use Windows' own conversion, including its premultiplied-alpha
        // rounding, then encode losslessly with faster Deflate compression.
        var rgba = new byte[pixels.Length];
        using (CatalogRuntimeProfile.Measure("png.convert"))
        {
            using var source = SoftwareBitmap.CreateCopyFromBuffer(pixels.AsBuffer(), BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Premultiplied);
            using var straight = SoftwareBitmap.Convert(source, BitmapPixelFormat.Rgba8, BitmapAlphaMode.Straight);
            straight.CopyToBuffer(rgba.AsBuffer());
        }
        byte[] output;
        using (CatalogRuntimeProfile.Measure("png.encode")) output = NativeCapturePng.Encode(rgba, width, height);
        using (CatalogRuntimeProfile.Measure("png.write")) await File.WriteAllBytesAsync(path, output);
    }
}
