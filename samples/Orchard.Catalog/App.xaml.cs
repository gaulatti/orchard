using Microsoft.UI.Xaml;

namespace Orchard.Catalog;

public partial class App : Application
{
    private Window? _window;
    private static readonly List<string> ResourceErrors = [];
    private static readonly List<object> AvatarStartupErrors = [];
    private static readonly List<string> BindingErrors = [];

    public App()
    {
        UnhandledException += (_, args) => ReportSmokeFailure(args.Exception);
        if (Environment.GetCommandLineArgs().Any(argument => argument is "--smoke-output" or "--verify-hidden-workspaces" or "--verify-hidden-footers"))
        {
            System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
            DebugSettings.IsXamlResourceReferenceTracingEnabled = true;
            DebugSettings.XamlResourceReferenceFailed += (_, args) => ResourceErrors.Add(args.Message);
            DebugSettings.IsBindingTracingEnabled = true;
            DebugSettings.BindingFailed += (_, args) =>
            {
                if (BindingErrors.Count < 16) BindingErrors.Add(args.Message);
            };
            // XAML can replace a managed setter error with a generic parse
            // failure. Retain the original control/metadata error in smoke mode.
            AppDomain.CurrentDomain.FirstChanceException += (_, args) =>
            {
                var stack = args.Exception.StackTrace;
                if (AvatarStartupErrors.Count < 8 && stack?.Contains("BleeckerAvatar", StringComparison.Ordinal) == true)
                    AvatarStartupErrors.Add(new { type = args.Exception.GetType().FullName, args.Exception.Message, stack });
            };
        }
        try { InitializeComponent(); }
        catch (Exception error) { ReportSmokeFailure(error); throw; }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-kbd-corners")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}_window=new Window();_ = Orchard.Testing.KbdCornersFixture.RunAsync(_window,Path.GetFullPath(a[o+1]),typeof(App).Assembly.Location);return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-skeleton-content")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}_window=new Window();_ = Orchard.Testing.SkeletonContentFixture.RunAsync(_window,Path.GetFullPath(a[o+1]),typeof(App).Assembly.Location);return;}
        if (Environment.GetCommandLineArgs().Contains("--probe-hidden-page-frame-overrides"))
        {
            var arguments = Environment.GetCommandLineArgs(); var output = Array.IndexOf(arguments, "--verify-output");
            if (output < 0 || output + 1 >= arguments.Length) { Environment.Exit(2); return; }
            _window = new Window();
            _ = Orchard.Testing.PageFrameOverridesFixture.RunAsync(_window, Path.GetFullPath(arguments[output + 1]), typeof(App).Assembly.Location); return;
        }
        var resourceArguments = Environment.GetCommandLineArgs();
        if (resourceArguments.Contains("--verify-catalog-reference-resources"))
        {
            var resourceOutput = Array.IndexOf(resourceArguments, "--verify-output");
            if (resourceOutput < 0 || resourceOutput + 1 >= resourceArguments.Length) { Environment.Exit(2); return; }
            Environment.Exit(CatalogReferenceAudit.Run(Path.GetFullPath(resourceArguments[resourceOutput + 1]))); return;
        }
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-render-owner-lifetime")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}_window=new Window();RunRenderOwnerLifetimeAsync(_window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-chart-theme-consumers")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}_window=new Window();_ = Orchard.Testing.ChartThemeConsumersFixture.RunAsync(_window,Path.GetFullPath(a[o+1]),typeof(App).Assembly.Location);return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-button-lifecycle")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}_window=new Window();RunButtonLifecycleAsync(_window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-checkbox-content")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}_window=new Window();RunCheckboxContentAsync(_window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-switch-lifecycle")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}_window=new Window();RunSwitchLifecycleAsync(_window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-toggle-group")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}_window=new Window();RunToggleGroupAsync(_window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-label-association")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}_window=new Window();RunLabelAssociationAsync(_window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-button-group")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}_window=new Window();RunButtonGroupAsync(_window,Path.GetFullPath(a[o+1]));return;}
        try
        {
            var arguments = Environment.GetCommandLineArgs();
            if(arguments.Contains("--probe-hidden-overlay-lifetime")){var o=Array.IndexOf(arguments,"--verify-output");var m=Array.IndexOf(arguments,"--shadow-mode");if(o<0||o+1>=arguments.Length||m<0||m+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunOverlayShadowLifetimeAsync(_window,Path.GetFullPath(arguments[o+1]),arguments[m+1]);return;}
            if(arguments.Contains("--probe-hidden-overlay-combined")){var o=Array.IndexOf(arguments,"--verify-output");var r=Array.IndexOf(arguments,"--combined-reference");if(o<0||o+1>=arguments.Length||r<0||r+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunOverlayCombinedAsync(_window,Path.GetFullPath(arguments[o+1]),Path.GetFullPath(arguments[r+1]));return;}
            if(arguments.Contains("--probe-hidden-overlay-far")){var o=Array.IndexOf(arguments,"--verify-output");var r=Array.IndexOf(arguments,"--far-reference");if(o<0||o+1>=arguments.Length||r<0||r+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunOverlayFarAsync(_window,Path.GetFullPath(arguments[o+1]),Path.GetFullPath(arguments[r+1]));return;}
            if(arguments.Contains("--probe-hidden-file-input")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunFileInputAsync(_window,Path.GetFullPath(arguments[output+1]));return;}
            if(arguments.Contains("--probe-hidden-media-library")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunMediaLibraryAsync(_window,Path.GetFullPath(arguments[output+1]));return;}
            if(Environment.GetCommandLineArgs().Contains("--probe-hidden-auth-shell-layout")){var layoutArguments=Environment.GetCommandLineArgs();var layoutOutput=Array.IndexOf(layoutArguments,"--verify-output");if(layoutOutput<0||layoutOutput+1>=layoutArguments.Length){Environment.Exit(2);return;}_window=new Window();RunAuthShellLayoutAsync(_window,Path.GetFullPath(layoutArguments[layoutOutput+1]));return;}
            if(arguments.Contains("--probe-hidden-auth-shell")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunAuthShellAsync(_window,Path.GetFullPath(arguments[output+1]));return;}
            if(arguments.Contains("--probe-hidden-command-spotlight-surface")){var o=Array.IndexOf(arguments,"--verify-output");var r=Array.IndexOf(arguments,"--surface-reference");if(o<0||o+1>=arguments.Length||r<0||r+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunCommandSpotlightSurfaceAsync(_window,Path.GetFullPath(arguments[o+1]),Path.GetFullPath(arguments[r+1]));return;}
            if(arguments.Contains("--probe-hidden-overlay-near-owner")){var o=Array.IndexOf(arguments,"--verify-output");var r=Array.IndexOf(arguments,"--owner-reference");if(o<0||o+1>=arguments.Length||r<0||r+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunOverlayNearOwnerAsync(_window,Path.GetFullPath(arguments[o+1]),Path.GetFullPath(arguments[r+1]));return;}
            if(arguments.Contains("--probe-hidden-overlay-near-geometry")){var o=Array.IndexOf(arguments,"--verify-output");var r=Array.IndexOf(arguments,"--geometry-reference");if(o<0||o+1>=arguments.Length||r<0||r+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunOverlayNearGeometryAsync(_window,Path.GetFullPath(arguments[o+1]),Path.GetFullPath(arguments[r+1]));return;}
            if(arguments.Contains("--probe-hidden-overlay-shadow")){var o=Array.IndexOf(arguments,"--verify-output");var r=Array.IndexOf(arguments,"--shadow-reference");if(o<0||o+1>=arguments.Length||r<0||r+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunOverlayShadowAsync(_window,Path.GetFullPath(arguments[o+1]),Path.GetFullPath(arguments[r+1]));return;}
            if(arguments.Contains("--probe-hidden-command-spotlight-paint")){var o=Array.IndexOf(arguments,"--verify-output");var r=Array.IndexOf(arguments,"--paint-reference");if(o<0||o+1>=arguments.Length||r<0||r+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunCommandSpotlightPaintAsync(_window,Path.GetFullPath(arguments[o+1]),Path.GetFullPath(arguments[r+1]));return;}
            if(arguments.Contains("--probe-hidden-command-spotlight-refresh")){var o=Array.IndexOf(arguments,"--verify-output");if(o<0||o+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunCommandSpotlightRefreshAsync(_window,Path.GetFullPath(arguments[o+1]));return;}
            if(arguments.Contains("--probe-hidden-command-spotlight-collections")){var o=Array.IndexOf(arguments,"--verify-output");if(o<0||o+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunCommandSpotlightCollectionsAsync(_window,Path.GetFullPath(arguments[o+1]));return;}
            if(arguments.Contains("--probe-hidden-command-spotlight")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunCommandSpotlightAsync(_window,Path.GetFullPath(arguments[output+1]));return;}
            if(arguments.Contains("--probe-hidden-sonner")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunSonnerAsync(_window,Path.GetFullPath(arguments[output+1]));return;}
            if(arguments.Contains("--probe-hidden-hero-carousel")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunHeroCarouselAsync(_window,Path.GetFullPath(arguments[output+1]));return;}
            if(arguments.Contains("--probe-hidden-data-table-catalog")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunDataTableCatalogAsync(_window,Path.GetFullPath(arguments[output+1]));return;}
            if(arguments.Contains("--probe-hidden-navigation-menu")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunNavigationMenuAsync(_window,Path.GetFullPath(arguments[output+1]));return;}
            if(arguments.Contains("--probe-hidden-menubar")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunMenubarAsync(_window,Path.GetFullPath(arguments[output+1]));return;}
            if(arguments.Contains("--probe-hidden-brand-mark")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunBrandMarkAsync(_window,Path.GetFullPath(arguments[output+1]));return;}
            if(arguments.Contains("--probe-hidden-data-table-performance")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunDataTablePerformanceAsync(_window,Path.GetFullPath(arguments[output+1]),arguments.Contains("--paging-only"));return;}
            if(arguments.Contains("--probe-hidden-data-table-filter-plan")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunDataTableFilterPlansAsync(_window,Path.GetFullPath(arguments[output+1]));return;}
            if(arguments.Contains("--probe-hidden-data-table-live")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunDataTableLiveAsync(_window,Path.GetFullPath(arguments[output+1]));return;}
            if(arguments.Contains("--probe-hidden-data-table-reference")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunDataTableReferenceAsync(_window,Path.GetFullPath(arguments[output+1]));return;}
            if(arguments.Contains("--probe-hidden-data-table")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunDataTableAsync(_window,Path.GetFullPath(arguments[output+1]));return;}
            if(arguments.Contains("--probe-hidden-table-catalog")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunTableCatalogAsync(_window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-simple-table-catalog")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunSimpleTableCatalogAsync(_window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-table-performance")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunTablePerformanceAsync(_window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-table-label-raster")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunTableLabelRasterAsync(_window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-table-minimum")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunTableMinimumAsync(_window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-table-button-content-foreground")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunTableButtonContentForegroundAsync(_window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-table-navigation-foreground")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunTableNavigationForegroundAsync(_window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-table-template")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunTableTemplateAsync(_window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-table-customization")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunTableCustomizationAsync(_window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-table-text")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunTableTextAsync(_window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-simple-table")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunSimpleTableAsync(_window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-table")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunTableAsync(_window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-context-menu-options"))
            {
                var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}
                _window=new Window();RunContextMenuOptionsAsync(_window,Path.GetFullPath(arguments[output+1]));return;
            }
            if(arguments.Contains("--probe-hidden-context-menu-modal"))
            {
                var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}
                _window=new Window();RunContextMenuModalAsync(_window,Path.GetFullPath(arguments[output+1]));return;
            }
            if(arguments.Contains("--probe-hidden-context-menu-styled-portal"))
            {
                var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}
                _window=new Window();RunContextMenuStyledPortalAsync(_window,Path.GetFullPath(arguments[output+1]));return;
            }
            if(arguments.Contains("--probe-hidden-context-menu-catalog"))
            {
                var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}
                _window=new Window();RunContextMenuCatalogAsync(_window,Path.GetFullPath(arguments[output+1]));return;
            }
            if(arguments.Contains("--probe-hidden-context-menu-root"))
            {
                var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}
                _window=new Window();RunContextMenuRootAsync(_window,Path.GetFullPath(arguments[output+1]));return;
            }
            if(arguments.Contains("--probe-hidden-otp-input-catalog"))
            {
                var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}
                _window=new Window();RunOtpInputCatalogAsync(_window,Path.GetFullPath(arguments[output+1]));return;
            }
            if(arguments.Contains("--probe-hidden-otp-input"))
            {
                var output=Array.IndexOf(arguments,"--verify-output");var source=Array.IndexOf(arguments,"--source-reference");if(output<0||output+1>=arguments.Length||source<0||source+1>=arguments.Length){Environment.Exit(2);return;}
                _window=new Window();RunOtpInputAsync(_window,Path.GetFullPath(arguments[output+1]),Path.GetFullPath(arguments[source+1]));return;
            }
            if(arguments.Contains("--probe-hidden-app-tab-bar"))
            {
                var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}
                _window=new Window();RunAppTabBarAsync(_window,Path.GetFullPath(arguments[output+1]));return;
            }
            if(arguments.Contains("--probe-hidden-app-tab-bar-catalog"))
            {
                var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}
                _window=new Window();RunAppTabBarCatalogAsync(_window,Path.GetFullPath(arguments[output+1]));return;
            }
            if(arguments.Contains("--probe-hidden-segmented-control-catalog"))
            {
                var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}
                _window=new Window();RunSegmentedControlCatalogAsync(_window,Path.GetFullPath(arguments[output+1]));return;
            }
            if(arguments.Contains("--probe-hidden-segmented-control"))
            {
                var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}
                _window=new Window();RunSegmentedControlAsync(_window,Path.GetFullPath(arguments[output+1]));return;
            }
            if(arguments.Contains("--probe-hidden-secure-field-catalog"))
            {
                var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}
                _window=new Window();RunSecureFieldCatalogAsync(_window,Path.GetFullPath(arguments[output+1]));return;
            }
            if(arguments.Contains("--probe-hidden-secure-field"))
            {
                var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}
                _window=new Window();RunSecureFieldAsync(_window,Path.GetFullPath(arguments[output+1]));return;
            }
            if(arguments.Contains("--probe-hidden-edge-motion"))
            {
                var edgeOutput=Array.IndexOf(arguments,"--verify-output");if(edgeOutput<0||edgeOutput+1>=arguments.Length){Environment.Exit(2);return;}
                _window=new Window();RunEdgeMotionAsync(_window,Path.GetFullPath(arguments[edgeOutput+1]));return;
            }
            if(arguments.Contains("--probe-hidden-edge-composition"))
            {
                var edgeOutput=Array.IndexOf(arguments,"--verify-output");if(edgeOutput<0||edgeOutput+1>=arguments.Length){Environment.Exit(2);return;}
                _window=new Window();RunEdgeCompositionAsync(_window,Path.GetFullPath(arguments[edgeOutput+1]));return;
            }
            if(arguments.Contains("--probe-hidden-edge-dialog-catalog"))
            {
                var edgeOutput=Array.IndexOf(arguments,"--verify-output");if(edgeOutput<0||edgeOutput+1>=arguments.Length){Environment.Exit(2);return;}
                _window=new Window();RunEdgeDialogCatalogAsync(_window,Path.GetFullPath(arguments[edgeOutput+1]));return;
            }
            var edgeProbe=Array.IndexOf(arguments,"--probe-hidden-edge-dialog");
            if(edgeProbe>=0)
            {
                var edgeOutput=Array.IndexOf(arguments,"--verify-output");if(edgeProbe+1>=arguments.Length||edgeOutput<0||edgeOutput+1>=arguments.Length){Environment.Exit(2);return;}
                _window=new Window();RunEdgeDialogAsync(_window,Path.GetFullPath(arguments[edgeProbe+1]),Path.GetFullPath(arguments[edgeOutput+1]));return;
            }
            if (arguments.Contains("--probe-hidden-modal-catalog"))
            {
                var output = Array.IndexOf(arguments, "--verify-output"); if (output < 0 || output + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window(); RunModalCatalogAsync(_window, Path.GetFullPath(arguments[output + 1])); return;
            }
            if (arguments.Contains("--probe-hidden-modal-content"))
            {
                var output = Array.IndexOf(arguments, "--verify-output"); if (output < 0 || output + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window(); RunModalContentAsync(_window, Path.GetFullPath(arguments[output + 1])); return;
            }
            var modalProbe = Array.IndexOf(arguments, "--probe-hidden-modal");
            if (modalProbe >= 0)
            {
                var output = Array.IndexOf(arguments, "--verify-output"); if (modalProbe + 1 >= arguments.Length || output < 0 || output + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window(); RunModalProbeAsync(_window, Path.GetFullPath(arguments[modalProbe + 1]), Path.GetFullPath(arguments[output + 1])); return;
            }
            var tooltipBranch = Array.IndexOf(arguments, "--probe-hidden-tooltip-modal-branch");
            if (tooltipBranch >= 0)
            {
                var output = Array.IndexOf(arguments, "--verify-output"); if (tooltipBranch + 1 >= arguments.Length || output < 0 || output + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window(); RunTooltipModalBranchProbeAsync(_window, Path.GetFullPath(arguments[tooltipBranch + 1]), Path.GetFullPath(arguments[output + 1])); return;
            }
            if (arguments.Contains("--probe-hidden-menu-portal-primitive"))
            {
                var output = Array.IndexOf(arguments, "--verify-output"); if (output < 0 || output + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window(); RunMenuPortalPrimitiveAsync(_window, Path.GetFullPath(arguments[output + 1])); return;
            }
            var selectBranch = Array.IndexOf(arguments, "--probe-hidden-select-modal-branch");
            if (selectBranch >= 0)
            {
                var output = Array.IndexOf(arguments, "--verify-output"); if (selectBranch + 1 >= arguments.Length || output < 0 || output + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window(); RunSelectModalBranchProbeAsync(_window, Path.GetFullPath(arguments[selectBranch + 1]), Path.GetFullPath(arguments[output + 1])); return;
            }
            if (arguments.Contains("--probe-hidden-popover-modal-branch"))
            {
                var output = Array.IndexOf(arguments, "--verify-output"); if (output < 0 || output + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window(); RunPopoverModalBranchProbeAsync(_window, Path.GetFullPath(arguments[output + 1])); return;
            }
            if (arguments.Contains("--probe-hidden-alert-dialog-actions"))
            {
                var output = Array.IndexOf(arguments, "--verify-output"); if (output < 0 || output + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window(); RunAlertDialogActionsProbeAsync(_window, Path.GetFullPath(arguments[output + 1])); return;
            }
            var tabsBorderProbe = Array.IndexOf(arguments, "--probe-hidden-tabs-border");
            if(tabsBorderProbe>=0){var output=Array.IndexOf(arguments,"--verify-output");if(tabsBorderProbe+1>=arguments.Length||output<0||output+1>=arguments.Length){Environment.Exit(2);return;}_window=new Window();RunTabsBorderProbeAsync(_window,Path.GetFullPath(arguments[tabsBorderProbe+1]),Path.GetDirectoryName(Path.GetFullPath(arguments[output+1]))!);return;}
            var alertDialogProbe = Array.IndexOf(arguments, "--probe-hidden-alert-dialog");
            if (alertDialogProbe >= 0)
            {
                var output = Array.IndexOf(arguments, "--verify-output"); if (alertDialogProbe + 1 >= arguments.Length || output < 0 || output + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window(); RunAlertDialogProbeAsync(_window, Path.GetFullPath(arguments[alertDialogProbe + 1]), Path.GetFullPath(arguments[output + 1])); return;
            }
            if (arguments.Contains("--probe-hidden-public-modal-host"))
            {
                var output = Array.IndexOf(arguments, "--verify-output"); if (output < 0 || output + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window(); RunPublicModalHostProbeAsync(_window, Path.GetFullPath(arguments[output + 1])); return;
            }
            if (arguments.Contains("--probe-hidden-modal-ownership"))
            {
                var output = Array.IndexOf(arguments, "--verify-output"); if (output < 0 || output + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window(); RunModalOwnershipProbeAsync(_window, Path.GetFullPath(arguments[output + 1])); return;
            }
            var modalHostProbe = Array.IndexOf(arguments, "--probe-hidden-modal-host");
            if (modalHostProbe >= 0)
            {
                var output = Array.IndexOf(arguments, "--verify-output");
                if (modalHostProbe + 1 >= arguments.Length || output < 0 || output + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window(); RunModalHostProbeAsync(_window, Path.GetFullPath(arguments[modalHostProbe + 1]), Path.GetFullPath(arguments[output + 1])); return;
            }
            var scrollAreas = Array.IndexOf(arguments, "--verify-hidden-scroll-areas");
            if (scrollAreas >= 0)
            {
                var output = Array.IndexOf(arguments, "--verify-output");
                if (scrollAreas + 1 >= arguments.Length || output < 0 || output + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window(); RunScrollAreasAsync(_window, Path.GetFullPath(arguments[scrollAreas + 1]), Path.GetFullPath(arguments[output + 1])); return;
            }
            var hoverCards = Array.IndexOf(arguments, "--verify-hidden-hover-cards");
            if (hoverCards >= 0)
            {
                var output = Array.IndexOf(arguments, "--verify-output");
                if (hoverCards + 1 >= arguments.Length || output < 0 || output + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window();
                _ = Orchard.Testing.HoverCardReferenceFixture.RunAsync(_window, Path.GetFullPath(arguments[hoverCards + 1]), Path.GetFullPath(arguments[output + 1]), typeof(App).Assembly.Location); return;
            }
            var popovers = Array.IndexOf(arguments, "--verify-hidden-popovers");
            if (popovers >= 0)
            {
                var output = Array.IndexOf(arguments, "--verify-output");
                if (popovers + 1 >= arguments.Length || output < 0 || output + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window();
                _ = Orchard.Testing.PopoverReferenceFixture.RunAsync(_window, Path.GetFullPath(arguments[popovers + 1]), Path.GetFullPath(arguments[output + 1]), typeof(App).Assembly.Location); return;
            }
            var headerContracts = Array.IndexOf(arguments, "--verify-hidden-header-contracts");
            if (headerContracts >= 0)
            {
                var output = Array.IndexOf(arguments, "--verify-output");
                if (headerContracts + 1 >= arguments.Length || output < 0 || output + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window();
                _ = Orchard.Testing.HeaderReferenceFixture.RunContractsAsync(_window, Path.GetFullPath(arguments[headerContracts + 1]), Path.GetFullPath(arguments[output + 1]), typeof(App).Assembly.Location); return;
            }
            var headers = Array.IndexOf(arguments, "--verify-hidden-headers");
            if (headers >= 0)
            {
                var output = Array.IndexOf(arguments, "--verify-output");
                if (headers + 1 >= arguments.Length || output < 0 || output + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window();
                _ = Orchard.Testing.HeaderReferenceFixture.RunAsync(_window, Path.GetFullPath(arguments[headers + 1]), Path.GetFullPath(arguments[output + 1]), typeof(App).Assembly.Location); return;
            }
            var hidden = Array.IndexOf(arguments, "--verify-hidden-workspaces");
            var footers = Array.IndexOf(arguments, "--verify-hidden-footers");
            var producers = Array.IndexOf(arguments, "--verify-hidden-producer-threading");
            var footerContracts = Array.IndexOf(arguments, "--verify-hidden-footer-contracts");
            if (footerContracts >= 0)
            {
                if (footerContracts + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window();
                _ = Orchard.Testing.FooterContractFixture.RunAsync(_window, Path.GetFullPath(Path.Combine(arguments[footerContracts + 1], "hidden-footer-contracts.json")), typeof(App).Assembly.Location); return;
            }
            if (producers >= 0)
            {
                if (producers + 1 >= arguments.Length) { Environment.Exit(2); return; }
                _window = new Window();
                _ = Orchard.Testing.ProducerThreadingFixture.RunAsync(_window, Path.GetFullPath(Path.Combine(arguments[producers + 1], "hidden-producer-threading.json")), typeof(App).Assembly.Location); return;
            }
            if (footers >= 0)
            {
                if (footers + 1 >= arguments.Length) { Environment.Exit(2); return; }
                RunHiddenFootersAsync(Path.GetFullPath(arguments[footers + 1])); return;
            }
            if (hidden >= 0)
            {
                if (hidden + 1 >= arguments.Length) { Environment.Exit(2); return; }
                RunHiddenWorkspacesAsync(Path.GetFullPath(arguments[hidden + 1]));
                return;
            }
            if (arguments.Any(argument => argument.StartsWith("--verify-hidden-", StringComparison.Ordinal) || argument.StartsWith("--probe-hidden-", StringComparison.Ordinal))) { Environment.Exit(2); return; }
            foreach (var name in new[] { "Heading", "Body", "PrimaryButton", "SecondaryButton", "TextBox", "ComboBox" })
            {
                var key = $"Bleecker{name}Style";
                try { _ = FindResource(Resources, key); }
                catch (Exception error) { throw new InvalidOperationException($"Could not load Orchard resource {key}.", error); }
            }
            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception error) { ReportSmokeFailure(error); throw; }
    }

    private static object FindResource(ResourceDictionary dictionary, string key)
    {
        if (dictionary.TryGetValue(key, out var value)) return value;
        foreach (var merged in dictionary.MergedDictionaries.Reverse())
        {
            try { return FindResource(merged, key); }
            catch (KeyNotFoundException) { }
        }
        throw new KeyNotFoundException(key);
    }

    private static object DescribeResources(ResourceDictionary dictionary) => new
    {
        source = dictionary.Source?.ToString(),
        count = dictionary.Count,
        merged = dictionary.MergedDictionaries.Select(DescribeResources).ToArray()
    };

    private async void RunModalOwnershipProbeAsync(Window target, string output)
    {
        try { await Orchard.Testing.ModalHostOwnershipProbe.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunPopoverModalBranchProbeAsync(Window target, string output)
    {
        try { await Orchard.Testing.PopoverModalBranchProbe.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunSelectModalBranchProbeAsync(Window target, string reference, string output)
    {
        try { await Orchard.Testing.SelectModalBranchProbe.RunAsync(target, reference, output, typeof(App).Assembly.Location); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunMenuPortalPrimitiveAsync(Window target, string output)
    {
        try { await Orchard.Testing.MenuPortalPrimitiveProbe.RunAsync(target, output); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunTooltipModalBranchProbeAsync(Window target, string reference, string output)
    {
        try { await Orchard.Testing.TooltipModalBranchProbe.RunAsync(target, reference, output, typeof(App).Assembly.Location); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunModalProbeAsync(Window target, string reference, string output)
    {
        try { await Orchard.Testing.ModalReferenceFixture.RunAsync(target, reference, output, typeof(App).Assembly.Location); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunEdgeDialogAsync(Window target,string reference,string output)
    {
        try{await Orchard.Testing.EdgeDialogReferenceFixture.RunAsync(target,reference,output,typeof(App).Assembly.Location);}
        catch(Exception error){ReportSmokeFailure(error);}
    }
    private async void RunEdgeDialogCatalogAsync(Window target,string output)
    {
        try{await EdgeDialogCatalogProbe.RunAsync(target,output);}
        catch(Exception error){ReportSmokeFailure(error);}
    }
    private async void RunEdgeCompositionAsync(Window target,string output)
    {
        try{await Orchard.Testing.EdgeDialogCompositionFixture.RunAsync(target,output,typeof(App).Assembly.Location);}
        catch(Exception error){ReportSmokeFailure(error);}
    }
    private async void RunEdgeMotionAsync(Window target,string output)
    {
        try{await Orchard.Testing.EdgeMotionFixture.RunAsync(target,output,typeof(App).Assembly.Location);}
        catch(Exception error){ReportSmokeFailure(error);}
    }
    private async void RunSecureFieldAsync(Window target, string output)
    {
        try { await Orchard.Testing.SecureFieldFixture.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunSegmentedControlAsync(Window target, string output)
    {
        try { await Orchard.Testing.SegmentedControlFixture.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunAppTabBarAsync(Window target, string output)
    {
        try { await Orchard.Testing.AppTabBarFixture.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunOtpInputAsync(Window target, string output, string source)
    {
        try { await Orchard.Testing.OtpInputFixture.RunAsync(target, output, typeof(App).Assembly.Location, source); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunOtpInputCatalogAsync(Window target, string output)
    {
        try { await OtpInputCatalogProbe.RunAsync(target, output); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunContextMenuRootAsync(Window target, string output)
    {
        try { await Orchard.Testing.ContextMenuFixture.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunContextMenuCatalogAsync(Window target, string output)
    {
        try { await ContextMenuCatalogProbe.RunAsync(target, output); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunContextMenuStyledPortalAsync(Window target, string output)
    {
        try { await Orchard.Testing.ContextMenuStyledPortalProbe.RunAsync(target, output); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunContextMenuModalAsync(Window target, string output)
    {
        try { await Orchard.Testing.ContextMenuModalFixture.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunTableCatalogAsync(Window target,string output){try{await TableCatalogProbe.RunAsync(target,output);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunSimpleTableCatalogAsync(Window target,string output){try{await SimpleDataTableCatalogProbe.RunAsync(target,output);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunTablePerformanceAsync(Window target,string output){try{await Orchard.Testing.TablePerformanceFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunTableLabelRasterAsync(Window target,string output){try{await Orchard.Testing.TableTemplateFixture.RunAsync(target,output,typeof(App).Assembly.Location,true);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunTableTemplateAsync(Window target,string output){try{await Orchard.Testing.TableTemplateFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunTableMinimumAsync(Window target,string output){try{await Orchard.Testing.TableMinimumFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunTabsBorderProbeAsync(Window target,string reference,string folder){try{await Orchard.Testing.TabsBorderPaintFixture.RunAsync(target,reference,folder,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunTableButtonContentForegroundAsync(Window target,string output){try{await Orchard.Testing.TableButtonContentForegroundFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunTableNavigationForegroundAsync(Window target,string output){try{await Orchard.Testing.TableNavigationForegroundFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunTableCustomizationAsync(Window target,string output){try{await Orchard.Testing.TableCustomizationFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunTableTextAsync(Window target,string output){try{await Orchard.Testing.TableTextFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunDataTableCatalogAsync(Window target,string output){try{await Orchard.Testing.DataTableCatalogProbe.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunDataTablePerformanceAsync(Window target,string output,bool pagingOnly=false){try{await Orchard.Testing.DataTablePerformanceFixture.RunAsync(target,output,typeof(App).Assembly.Location,pagingOnly);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunNavigationMenuAsync(Window target,string output){try{await Orchard.Testing.NavigationMenuFixture.RunAsync(target,output,typeof(App).Assembly.Location,()=>new NavigationMenusFixture());}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunSonnerAsync(Window target,string output){try{await Orchard.Testing.SonnerFixture.RunAsync(target,output,typeof(App).Assembly.Location,()=>new SonnersFixture());}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunHeroCarouselAsync(Window target,string output){try{await Orchard.Testing.HeroCarouselFixture.RunAsync(target,output,typeof(App).Assembly.Location,()=>new HeroCarouselsFixture());}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunFileInputAsync(Window target,string output){try{await Orchard.Testing.FileInputFixture.RunAsync(target,output,typeof(App).Assembly.Location,()=>new FileInputsFixture());}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunMediaLibraryAsync(Window target,string output){try{await Orchard.Testing.MediaLibraryFixture.RunAsync(target,output,typeof(App).Assembly.Location,()=>new MediaLibrariesFixture());}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunAuthShellLayoutAsync(Window target,string output){try{await Orchard.Testing.AuthShellLayoutFixture.RunAsync(target,output,typeof(App).Assembly.Location,()=>new AuthShellsFixture());}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunAuthShellAsync(Window target,string output){try{await Orchard.Testing.AuthShellFixture.RunAsync(target,output,typeof(App).Assembly.Location,()=>new AuthShellsFixture());}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunRenderOwnerLifetimeAsync(Window target,string output){try{await Orchard.Testing.RenderOwnerLifetimeFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunButtonLifecycleAsync(Window target,string output){try{await Orchard.Testing.ButtonLifecycleFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunCheckboxContentAsync(Window target,string output){try{await Orchard.Testing.CheckboxContentFixture.RunAsync(target,output,typeof(App).Assembly.Location,()=>new CheckboxesFixture());}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunSwitchLifecycleAsync(Window target,string output){try{await Orchard.Testing.SwitchLifecycleFixture.RunAsync(target,output,typeof(App).Assembly.Location,()=>new SwitchesFixture());}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunToggleGroupAsync(Window target,string output){try{await Orchard.Testing.ToggleGroupFixture.RunAsync(target,output,typeof(App).Assembly.Location,()=>new ToggleGroupsFixture());}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunLabelAssociationAsync(Window target,string output){try{await Orchard.Testing.LabelAssociationFixture.RunAsync(target,output,typeof(App).Assembly.Location,()=>new LabelsFixture());}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunButtonGroupAsync(Window target,string output){try{await Orchard.Testing.ButtonGroupFixture.RunAsync(target,output,typeof(App).Assembly.Location,()=>new ButtonsFixture());}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunCommandSpotlightSurfaceAsync(Window target,string output,string reference){try{await Orchard.Testing.CommandSpotlightSurfaceFixture.RunAsync(target,output,reference,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunOverlayShadowAsync(Window target,string output,string reference){try{await Orchard.Testing.OverlayShadowFixture.RunAsync(target,output,reference,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunOverlayNearOwnerAsync(Window target,string output,string reference){try{await Orchard.Testing.OverlayNearOwnerFixture.RunAsync(target,output,reference,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunOverlayNearGeometryAsync(Window target,string output,string reference){try{await Orchard.Testing.OverlayNearGeometryFixture.RunAsync(target,output,reference,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunOverlayCombinedAsync(Window target,string output,string reference){try{await Orchard.Testing.OverlayCombinedFixture.RunAsync(target,output,reference,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunOverlayFarAsync(Window target,string output,string reference){try{await Orchard.Testing.OverlayFarFixture.RunAsync(target,output,reference,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunOverlayShadowLifetimeAsync(Window target,string output,string mode){try{await Orchard.Testing.OverlayShadowLifetimeFixture.RunAsync(target,output,mode,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunCommandSpotlightPaintAsync(Window target,string output,string reference){try{await Orchard.Testing.CommandSpotlightPaintFixture.RunAsync(target,output,reference,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunCommandSpotlightRefreshAsync(Window target,string output){try{await Orchard.Testing.CommandSpotlightRefreshFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunCommandSpotlightCollectionsAsync(Window target,string output){try{await Orchard.Testing.CommandSpotlightCollectionFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunCommandSpotlightAsync(Window target,string output){try{await Orchard.Testing.CommandSpotlightFixture.RunAsync(target,output,typeof(App).Assembly.Location,()=>new CommandSpotlightsFixture());}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunMenubarAsync(Window target,string output){try{await Orchard.Testing.MenubarFixture.RunAsync(target,output,typeof(App).Assembly.Location,()=>new MenubarsFixture());}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunBrandMarkAsync(Window target,string output){try{await Orchard.Testing.BrandMarkFixture.RunAsync(target,output,typeof(App).Assembly.Location,()=>new BrandMarksFixture());}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunDataTableFilterPlansAsync(Window target,string output){try{await Orchard.Testing.DataTableReferenceFixture.RunFilterPlansAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunDataTableLiveAsync(Window target,string output){try{await Orchard.Testing.DataTableFixture.RunLiveAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunDataTableReferenceAsync(Window target,string output){try{await Orchard.Testing.DataTableReferenceFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunDataTableAsync(Window target,string output){try{await Orchard.Testing.DataTableFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunSimpleTableAsync(Window target,string output){try{await Orchard.Testing.SimpleDataTableFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunTableAsync(Window target,string output){try{await Orchard.Testing.TablePrimitiveFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){ReportSmokeFailure(error);}}
    private async void RunContextMenuOptionsAsync(Window target, string output)
    {
        try { await Orchard.Testing.ContextMenuOptionsFixture.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunAppTabBarCatalogAsync(Window target, string output)
    {
        try { await AppTabBarCatalogProbe.RunAsync(target, output); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunSegmentedControlCatalogAsync(Window target, string output)
    {
        try { await SegmentedControlCatalogProbe.RunAsync(target, output); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunSecureFieldCatalogAsync(Window target, string output)
    {
        try { await SecureFieldCatalogProbe.RunAsync(target, output); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunModalContentAsync(Window target, string output)
    {
        try { await Orchard.Testing.ModalContentFixture.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunModalCatalogAsync(Window target, string output)
    {
        try { await ModalCatalogProbe.RunAsync(target, output); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunPublicModalHostProbeAsync(Window target, string output)
    {
        try { await Orchard.Testing.ModalHostPublicProbe.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunAlertDialogProbeAsync(Window target, string reference, string output)
    {
        try { await Orchard.Testing.AlertDialogReferenceFixture.RunAsync(target, reference, output, typeof(App).Assembly.Location); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunAlertDialogActionsProbeAsync(Window target, string output)
    {
        try { await Orchard.Testing.AlertDialogActionsProbe.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunModalHostProbeAsync(Window target, string reference, string output)
    {
        try { await Orchard.Testing.ModalHostCoreProbe.RunAsync(target, reference, output, typeof(App).Assembly.Location); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private async void RunScrollAreasAsync(Window target, string reference, string output)
    {
        try { await Orchard.Testing.ScrollAreaReferenceFixture.RunAsync(target, reference, output, typeof(App).Assembly.Location); }
        catch (Exception error) { ReportSmokeFailure(error); }
    }
    private void ReportSmokeFailure(Exception error)
    {
        var arguments = Environment.GetCommandLineArgs();
        if (arguments.Contains("--probe-hidden-button-lifecycle"))
        {
            var output = Array.IndexOf(arguments, "--verify-output");
            if (output >= 0 && output + 1 < arguments.Length)
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(arguments[output + 1]))!, "unhandled-button-lifecycle-error.json"), System.Text.Json.JsonSerializer.Serialize(new { passed = false, error = error.ToString(), buttons = (_window?.Content as Microsoft.UI.Xaml.Controls.Panel)?.Children.OfType<BleeckerButton>().Select(button => new { button.IsLoaded, templateIsNull = button.Template is null, localTemplateIsNull = button.ReadLocalValue(Microsoft.UI.Xaml.Controls.Control.TemplateProperty) is null, localTemplateType = button.ReadLocalValue(Microsoft.UI.Xaml.Controls.Control.TemplateProperty)?.GetType().FullName, children = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(button) }).ToArray() }));
            Environment.Exit(1);
        }
        var popovers = Array.IndexOf(arguments, "--verify-hidden-popovers");
        var popoverOutput = Array.IndexOf(arguments, "--verify-output");
        var hoverCards = arguments.Contains("--verify-hidden-hover-cards");
        var scrollAreas = arguments.Contains("--verify-hidden-scroll-areas");
        if ((arguments.Contains("--probe-hidden-tabs-border") || arguments.Contains("--probe-hidden-table-button-content-foreground") || arguments.Contains("--probe-hidden-table-navigation-foreground") || arguments.Contains("--probe-hidden-table-label-raster") || arguments.Contains("--probe-hidden-table-performance") || arguments.Contains("--probe-hidden-table-template") || arguments.Contains("--probe-hidden-table-customization") || arguments.Contains("--probe-hidden-table-text") || arguments.Contains("--probe-hidden-simple-table-catalog") || arguments.Contains("--probe-hidden-simple-table") || arguments.Contains("--probe-hidden-table-catalog") || arguments.Contains("--probe-hidden-table") || arguments.Contains("--probe-hidden-context-menu-options") || arguments.Contains("--probe-hidden-context-menu-modal") || arguments.Contains("--probe-hidden-context-menu-styled-portal") || arguments.Contains("--probe-hidden-context-menu-catalog") || arguments.Contains("--probe-hidden-context-menu-root") || arguments.Contains("--probe-hidden-otp-input-catalog") || arguments.Contains("--probe-hidden-otp-input") || arguments.Contains("--probe-hidden-app-tab-bar") || arguments.Contains("--probe-hidden-app-tab-bar-catalog") || arguments.Contains("--probe-hidden-segmented-control-catalog") || arguments.Contains("--probe-hidden-segmented-control") || arguments.Contains("--probe-hidden-secure-field") || arguments.Contains("--probe-hidden-secure-field-catalog")) && popoverOutput >= 0 && popoverOutput + 1 < arguments.Length)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(arguments[popoverOutput + 1]))!; Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "unhandled-secure-field-error.json"), System.Text.Json.JsonSerializer.Serialize(new { passed = false, error = arguments.Contains("--probe-hidden-table-customization") ? error.ToString() : error.GetType().Name + ": " + error.Message })); Environment.Exit(1);
        }
        if (arguments.Contains("--probe-hidden-menu-portal-primitive") && popoverOutput >= 0 && popoverOutput + 1 < arguments.Length)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(arguments[popoverOutput + 1]))!; Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "unhandled-menu-primitive-error.json"), System.Text.Json.JsonSerializer.Serialize(new { passed = false, error = error.ToString() })); Environment.Exit(1);
        }
        if ((arguments.Contains("--probe-hidden-edge-motion") || arguments.Contains("--probe-hidden-edge-composition") || arguments.Contains("--probe-hidden-edge-dialog-catalog") || arguments.Contains("--probe-hidden-edge-dialog") || arguments.Contains("--probe-hidden-modal-host") || arguments.Contains("--probe-hidden-modal-ownership") || arguments.Contains("--probe-hidden-public-modal-host") || arguments.Contains("--probe-hidden-modal") || arguments.Contains("--probe-hidden-modal-content") || arguments.Contains("--probe-hidden-modal-catalog") || arguments.Contains("--probe-hidden-alert-dialog") || arguments.Contains("--probe-hidden-alert-dialog-actions") || arguments.Contains("--probe-hidden-popover-modal-branch") || arguments.Contains("--probe-hidden-select-modal-branch") || arguments.Contains("--probe-hidden-tooltip-modal-branch")) && popoverOutput >= 0 && popoverOutput + 1 < arguments.Length)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(arguments[popoverOutput + 1]))!; Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "unhandled-modal-host-error.json"), System.Text.Json.JsonSerializer.Serialize(new { passed = false, error = error.ToString() })); Environment.Exit(1);
        }
        if ((popovers >= 0 || hoverCards || scrollAreas) && popoverOutput >= 0 && popoverOutput + 1 < arguments.Length)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(arguments[popoverOutput + 1]))!;
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, scrollAreas ? "unhandled-scroll-area-error.json" : hoverCards ? "unhandled-hover-card-error.json" : "unhandled-popover-error.json"), System.Text.Json.JsonSerializer.Serialize(new { passed = false, stage = "unhandled-native-callback", error = error.ToString() }));
            Environment.Exit(1);
        }
        var index = Array.IndexOf(arguments, "--smoke-output");
        if (index < 0) index = Array.IndexOf(arguments, "--verify-hidden-workspaces");
        if (index < 0) index = Array.IndexOf(arguments, "--verify-hidden-footers");
        if (index < 0 || index + 1 >= arguments.Length) return;
        var folder = Path.GetFullPath(arguments[index + 1]);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "result.json"), System.Text.Json.JsonSerializer.Serialize(
            new { passed = false, stage = "startup", error = error.ToString(), resourceErrors = ResourceErrors,
                avatarStartupErrors = AvatarStartupErrors, bindingErrors = BindingErrors,
                resourceEnumerationSkipped = true }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        Environment.Exit(1);
    }
}
