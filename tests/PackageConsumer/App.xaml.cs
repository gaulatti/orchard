using Microsoft.UI.Xaml;
using System.Text.Json;

namespace PackageConsumer;

public partial class App : Application
{
    private Window? window;
    private async void RunFileInputAsync(Window target,string output){try{await Orchard.Testing.FileInputFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private async void RunMediaLibraryAsync(Window target,string output){try{await Orchard.Testing.MediaLibraryFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private async void RunAuthShellLayoutAsync(Window target,string output){try{await Orchard.Testing.AuthShellLayoutFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private async void RunAuthShellAsync(Window target,string output){try{await Orchard.Testing.AuthShellFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private async void RunCommandSpotlightSurfaceAsync(Window target,string output,string reference){try{await Orchard.Testing.CommandSpotlightSurfaceFixture.RunAsync(target,output,reference,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private async void RunOverlayShadowAsync(Window target,string output,string reference){try{await Orchard.Testing.OverlayShadowFixture.RunAsync(target,output,reference,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private async void RunOverlayNearOwnerAsync(Window target,string output,string reference){try{await Orchard.Testing.OverlayNearOwnerFixture.RunAsync(target,output,reference,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private async void RunOverlayNearGeometryAsync(Window target,string output,string reference){try{await Orchard.Testing.OverlayNearGeometryFixture.RunAsync(target,output,reference,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private async void RunOverlayCombinedAsync(Window target,string output,string reference){try{await Orchard.Testing.OverlayCombinedFixture.RunAsync(target,output,reference,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private async void RunOverlayFarAsync(Window target,string output,string reference){try{await Orchard.Testing.OverlayFarFixture.RunAsync(target,output,reference,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private async void RunOverlayShadowLifetimeAsync(Window target,string output,string mode){try{await Orchard.Testing.OverlayShadowLifetimeFixture.RunAsync(target,output,mode,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private async void RunCommandSpotlightPaintAsync(Window target,string output,string reference){try{await Orchard.Testing.CommandSpotlightPaintFixture.RunAsync(target,output,reference,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private async void RunCommandSpotlightRefreshAsync(Window target,string output){try{await Orchard.Testing.CommandSpotlightRefreshFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private async void RunCommandSpotlightCollectionsAsync(Window target,string output){try{await Orchard.Testing.CommandSpotlightCollectionFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private async void RunCommandSpotlightAsync(Window target,string output){try{await Orchard.Testing.CommandSpotlightFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}

    public App()
    {
        UnhandledException += (_, args) => Report(false, args.Exception.ToString());
        try { InitializeComponent(); }
        catch (Exception error) { Report(false, error.ToString()); throw; }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (Environment.GetCommandLineArgs().Contains("--probe-hidden-auth-shell-scale"))
        {
            var a = Environment.GetCommandLineArgs(); var o = Array.IndexOf(a, "--verify-output");
            if (o < 0 || o + 1 >= a.Length) { Environment.Exit(2); return; }
            window = new Window(); _ = Orchard.Testing.AuthShellScaleFixture.RunAsync(window, Path.GetFullPath(a[o + 1])); return;
        }
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-rich-content-layout")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.RichContentLayoutFixture.RunAsync(window,Path.GetFullPath(a[o+1]),typeof(App).Assembly.Location);return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-display-metadata")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.DisplayMetadataFixture.RunAsync(window,Path.GetFullPath(a[o+1]),typeof(App).Assembly.Location);return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-progress-range")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.ProgressRangeFixture.RunAsync(window,Path.GetFullPath(a[o+1]),typeof(App).Assembly.Location);return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-content-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.ContentScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]),typeof(App).Assembly.Location);return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-notification-badge-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.NotificationBadgeScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]),typeof(App).Assembly.Location);return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-alert-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.AlertScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-bauhaus-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.BauhausScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-theme-toggle-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.ThemeToggleScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-segmented-control-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.SegmentedControlScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-otp-input-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.OtpInputScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-feed-column-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.FeedColumnScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-app-tab-bar-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.AppTabBarScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-scroll-area-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.ScrollAreaScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-stepper-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.StepperScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-timeline-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.TimelineScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-loading-overlay-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.LoadingOverlayScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-field-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.FieldScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-panel-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.PanelScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-search-input-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.SearchInputScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-pagination-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.PaginationScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-tabs-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.TabsScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-nav-menu-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.NavMenuScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-collection-filters-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.CollectionFiltersScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-input-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.InputScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-sidebar-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.SidebarScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-textarea-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.TextareaScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-secure-field-scale")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.SecureFieldScaleFixture.RunAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-text-validity")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = TextValidityFixture.RunAsync(window,Path.GetFullPath(a[o+1]),typeof(App).Assembly.Location);return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-status-badge")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.StatusBadgeFixture.RunAsync(window,Path.GetFullPath(a[o+1]),typeof(App).Assembly.Location);return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-separator")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.SeparatorFixture.RunAsync(window,Path.GetFullPath(a[o+1]),typeof(App).Assembly.Location);return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-avatar-sizing")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.AvatarSizingFixture.RunAsync(window,Path.GetFullPath(a[o+1]),typeof(App).Assembly.Location);return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-kbd-content")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.KbdContentFixture.RunAsync(window,Path.GetFullPath(a[o+1]),typeof(App).Assembly.Location);return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-kbd-corners")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.KbdCornersFixture.RunAsync(window,Path.GetFullPath(a[o+1]),typeof(App).Assembly.Location);return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-skeleton-content")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.SkeletonContentFixture.RunAsync(window,Path.GetFullPath(a[o+1]),typeof(App).Assembly.Location);return;}
        if (Environment.GetCommandLineArgs().Contains("--probe-hidden-page-frame-overrides"))
        {
            var overrideArguments = Environment.GetCommandLineArgs(); var overrideOutput = Array.IndexOf(overrideArguments, "--verify-output");
            if (overrideOutput < 0 || overrideOutput + 1 >= overrideArguments.Length) { Environment.Exit(2); return; }
            window = new Window();
            _ = Orchard.Testing.PageFrameOverridesFixture.RunAsync(window, Path.GetFullPath(overrideArguments[overrideOutput + 1]), typeof(App).Assembly.Location); return;
        }
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-render-owner-lifetime")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();RunRenderOwnerLifetimeAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-chart-theme-consumers")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();_ = Orchard.Testing.ChartThemeConsumersFixture.RunAsync(window,Path.GetFullPath(a[o+1]),typeof(App).Assembly.Location);return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-button-lifecycle")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();RunButtonLifecycleAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-checkbox-content")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();RunCheckboxContentAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-switch-lifecycle")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();RunSwitchLifecycleAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-toggle-group")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();RunToggleGroupAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-label-association")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();RunLabelAssociationAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-button-group")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();RunButtonGroupAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-overlay-near-owner")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");var r=Array.IndexOf(a,"--owner-reference");if(o<0||o+1>=a.Length||r<0||r+1>=a.Length){Environment.Exit(2);return;}window=new Window();RunOverlayNearOwnerAsync(window,Path.GetFullPath(a[o+1]),Path.GetFullPath(a[r+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-overlay-lifetime")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");var m=Array.IndexOf(a,"--shadow-mode");if(o<0||o+1>=a.Length||m<0||m+1>=a.Length){Environment.Exit(2);return;}window=new Window();RunOverlayShadowLifetimeAsync(window,Path.GetFullPath(a[o+1]),a[m+1]);return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-overlay-combined")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");var r=Array.IndexOf(a,"--combined-reference");if(o<0||o+1>=a.Length||r<0||r+1>=a.Length){Environment.Exit(2);return;}window=new Window();RunOverlayCombinedAsync(window,Path.GetFullPath(a[o+1]),Path.GetFullPath(a[r+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-overlay-far")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");var r=Array.IndexOf(a,"--far-reference");if(o<0||o+1>=a.Length||r<0||r+1>=a.Length){Environment.Exit(2);return;}window=new Window();RunOverlayFarAsync(window,Path.GetFullPath(a[o+1]),Path.GetFullPath(a[r+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-overlay-near-geometry")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");var r=Array.IndexOf(a,"--geometry-reference");if(o<0||o+1>=a.Length||r<0||r+1>=a.Length){Environment.Exit(2);return;}window=new Window();RunOverlayNearGeometryAsync(window,Path.GetFullPath(a[o+1]),Path.GetFullPath(a[r+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-file-input")){var fileInputArguments=Environment.GetCommandLineArgs();var fileInputOutput=Array.IndexOf(fileInputArguments,"--verify-output");if(fileInputOutput<0||fileInputOutput+1>=fileInputArguments.Length){Environment.Exit(2);return;}window=new Window();RunFileInputAsync(window,Path.GetFullPath(fileInputArguments[fileInputOutput+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-media-library")){var mediaArguments=Environment.GetCommandLineArgs();var mediaOutput=Array.IndexOf(mediaArguments,"--verify-output");if(mediaOutput<0||mediaOutput+1>=mediaArguments.Length){Environment.Exit(2);return;}window=new Window();RunMediaLibraryAsync(window,Path.GetFullPath(mediaArguments[mediaOutput+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-auth-shell-layout")){var layoutArguments=Environment.GetCommandLineArgs();var layoutOutput=Array.IndexOf(layoutArguments,"--verify-output");if(layoutOutput<0||layoutOutput+1>=layoutArguments.Length){Environment.Exit(2);return;}window=new Window();RunAuthShellLayoutAsync(window,Path.GetFullPath(layoutArguments[layoutOutput+1]));return;}
            if(Environment.GetCommandLineArgs().Contains("--probe-hidden-auth-shell")){var authArguments=Environment.GetCommandLineArgs();var authOutput=Array.IndexOf(authArguments,"--verify-output");if(authOutput<0||authOutput+1>=authArguments.Length){Environment.Exit(2);return;}window=new Window();RunAuthShellAsync(window,Path.GetFullPath(authArguments[authOutput+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-command-spotlight-surface")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");var r=Array.IndexOf(a,"--surface-reference");if(o<0||o+1>=a.Length||r<0||r+1>=a.Length){Environment.Exit(2);return;}window=new Window();RunCommandSpotlightSurfaceAsync(window,Path.GetFullPath(a[o+1]),Path.GetFullPath(a[r+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-overlay-shadow")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");var r=Array.IndexOf(a,"--shadow-reference");if(o<0||o+1>=a.Length||r<0||r+1>=a.Length){Environment.Exit(2);return;}window=new Window();RunOverlayShadowAsync(window,Path.GetFullPath(a[o+1]),Path.GetFullPath(a[r+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-command-spotlight-paint")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");var r=Array.IndexOf(a,"--paint-reference");if(o<0||o+1>=a.Length||r<0||r+1>=a.Length){Environment.Exit(2);return;}window=new Window();RunCommandSpotlightPaintAsync(window,Path.GetFullPath(a[o+1]),Path.GetFullPath(a[r+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-command-spotlight-refresh")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();RunCommandSpotlightRefreshAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-command-spotlight-collections")){var a=Environment.GetCommandLineArgs();var o=Array.IndexOf(a,"--verify-output");if(o<0||o+1>=a.Length){Environment.Exit(2);return;}window=new Window();RunCommandSpotlightCollectionsAsync(window,Path.GetFullPath(a[o+1]));return;}
        if(Environment.GetCommandLineArgs().Contains("--probe-hidden-command-spotlight")){var spotlightArguments=Environment.GetCommandLineArgs();var spotlightOutput=Array.IndexOf(spotlightArguments,"--verify-output");if(spotlightOutput<0||spotlightOutput+1>=spotlightArguments.Length){Environment.Exit(2);return;}window=new Window();RunCommandSpotlightAsync(window,Path.GetFullPath(spotlightArguments[spotlightOutput+1]));return;}
        var arguments=Environment.GetCommandLineArgs();var hidden=Array.IndexOf(arguments,"--verify-hidden-progress-stack");
        if(arguments.Contains("--probe-hidden-sonner")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunSonnerAsync(window,Path.GetFullPath(arguments[output+1]));return;}
        if(arguments.Contains("--probe-hidden-hero-carousel")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunHeroCarouselAsync(window,Path.GetFullPath(arguments[output+1]));return;}
        if(arguments.Contains("--probe-hidden-navigation-menu")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunNavigationMenuAsync(window,Path.GetFullPath(arguments[output+1]));return;}
        if(arguments.Contains("--probe-hidden-menubar")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunMenubarAsync(window,Path.GetFullPath(arguments[output+1]));return;}
        if(arguments.Contains("--probe-hidden-brand-mark")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunBrandMarkAsync(window,Path.GetFullPath(arguments[output+1]));return;}
        if(arguments.Contains("--probe-hidden-data-table-performance")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunDataTablePerformanceAsync(window,Path.GetFullPath(arguments[output+1]),arguments.Contains("--paging-only"));return;}
        if(arguments.Contains("--probe-hidden-data-table-filter-plan")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunDataTableFilterPlansAsync(window,Path.GetFullPath(arguments[output+1]));return;}
        if(arguments.Contains("--probe-hidden-data-table-live")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunDataTableLiveAsync(window,Path.GetFullPath(arguments[output+1]));return;}
        if(arguments.Contains("--probe-hidden-data-table-reference")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunDataTableReferenceAsync(window,Path.GetFullPath(arguments[output+1]));return;}
        if(arguments.Contains("--probe-hidden-data-table")){var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunDataTableAsync(window,Path.GetFullPath(arguments[output+1]));return;}
        if(arguments.Contains("--probe-hidden-table-performance")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunTablePerformanceAsync(window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-table-label-raster")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunTableLabelRasterAsync(window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-table-minimum")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunTableMinimumAsync(window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-table-button-content-foreground")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunTableButtonContentForegroundAsync(window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-table-navigation-foreground")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunTableNavigationForegroundAsync(window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-table-template")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunTableTemplateAsync(window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-table-customization")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunTableCustomizationAsync(window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-table-text")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunTableTextAsync(window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-simple-table")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunSimpleTableAsync(window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-table")){var tableOutput=Array.IndexOf(arguments,"--verify-output");if(tableOutput<0||tableOutput+1>=arguments.Length){Environment.Exit(2);return;}window=new Window();RunTableAsync(window,Path.GetFullPath(arguments[tableOutput+1]));return;}
        if(arguments.Contains("--probe-hidden-context-menu-options"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}
            window=new Window();RunContextMenuOptionsAsync(window,Path.GetFullPath(arguments[output+1]));return;
        }
        if(arguments.Contains("--probe-hidden-context-menu-modal"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}
            window=new Window();RunContextMenuModalAsync(window,Path.GetFullPath(arguments[output+1]));return;
        }
        if(arguments.Contains("--probe-hidden-context-menu-root"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}
            window=new Window();RunContextMenuRootAsync(window,Path.GetFullPath(arguments[output+1]));return;
        }
        if(arguments.Contains("--probe-hidden-otp-input"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");var source=Array.IndexOf(arguments,"--source-reference");if(output<0||output+1>=arguments.Length||source<0||source+1>=arguments.Length){Environment.Exit(2);return;}
            window=new Window();RunOtpInputAsync(window,Path.GetFullPath(arguments[output+1]),Path.GetFullPath(arguments[source+1]));return;
        }
        if(arguments.Contains("--probe-hidden-app-tab-bar"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Environment.Exit(2);return;}
            window=new Window();RunAppTabBarAsync(window,Path.GetFullPath(arguments[output+1]));return;
        }
        if(arguments.Contains("--probe-hidden-segmented-control"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Report(false,"Missing Segmented Control output path.");return;}
            window=new Window();RunSegmentedControlAsync(window,Path.GetFullPath(arguments[output+1]));return;
        }
        if(arguments.Contains("--probe-hidden-secure-field"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Report(false,"Missing Secure Field output path.");return;}
            window=new Window();RunSecureFieldAsync(window,Path.GetFullPath(arguments[output+1]));return;
        }
        if(arguments.Contains("--probe-hidden-edge-motion"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Report(false,"Missing edge motion output path.");return;}
            window=new Window();RunEdgeMotionAsync(window,Path.GetFullPath(arguments[output+1]));return;
        }
        if(arguments.Contains("--probe-hidden-edge-composition"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Report(false,"Missing edge composition output path.");return;}
            window=new Window();RunEdgeCompositionAsync(window,Path.GetFullPath(arguments[output+1]));return;
        }
        var edge=Array.IndexOf(arguments,"--probe-hidden-edge-dialog");
        if(edge>=0)
        {
            var output=Array.IndexOf(arguments,"--verify-output");if(edge+1>=arguments.Length||output<0||output+1>=arguments.Length){Report(false,"Missing Drawer/Sheet reference/output path.");return;}
            window=new Window();RunEdgeDialogAsync(window,Path.GetFullPath(arguments[edge+1]),Path.GetFullPath(arguments[output+1]));return;
        }
        if(arguments.Contains("--probe-hidden-modal-content"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Report(false,"Missing Modal content output path.");return;}
            window=new Window();RunModalContentAsync(window,Path.GetFullPath(arguments[output+1]));return;
        }
        var modal=Array.IndexOf(arguments,"--probe-hidden-modal");
        if(modal>=0)
        {
            var output=Array.IndexOf(arguments,"--verify-output");if(modal+1>=arguments.Length||output<0||output+1>=arguments.Length){Report(false,"Missing Modal reference/output path.");return;}
            window=new Window();RunModalAsync(window,Path.GetFullPath(arguments[modal+1]),Path.GetFullPath(arguments[output+1]));return;
        }
        if(arguments.Contains("--probe-hidden-tooltip-modal-root"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Report(false,"Missing Tooltip modal root output path.");return;}
            window=new Window();RunTooltipModalRootAsync(window,Path.GetFullPath(arguments[output+1]));return;
        }
        if(arguments.Contains("--probe-hidden-select-modal-root"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");if(output<0||output+1>=arguments.Length){Report(false,"Missing Select modal root output path.");return;}
            window=new Window();RunSelectModalRootAsync(window,Path.GetFullPath(arguments[output+1]));return;
        }
        if(arguments.Contains("--probe-hidden-popover-modal-root"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");
            if(output<0||output+1>=arguments.Length){Report(false,"Missing Popover modal root output path.");return;}
            window=new Window();RunPopoverModalRootAsync(window,Path.GetFullPath(arguments[output+1]));return;
        }
        var tabsBorder=Array.IndexOf(arguments,"--probe-hidden-tabs-border");
        if(tabsBorder>=0){var output=Array.IndexOf(arguments,"--verify-output");if(tabsBorder+1>=arguments.Length||output<0||output+1>=arguments.Length){Report(false,"Missing Tabs reference/output folder.");return;}window=new Window();RunTabsBorderAsync(window,Path.GetFullPath(arguments[tabsBorder+1]),Path.GetDirectoryName(Path.GetFullPath(arguments[output+1]))!);return;}
        var alertDialog=Array.IndexOf(arguments,"--probe-hidden-alert-dialog");
        if(alertDialog>=0)
        {
            var output=Array.IndexOf(arguments,"--verify-output");
            if(alertDialog+1>=arguments.Length||output<0||output+1>=arguments.Length){Report(false,"Missing AlertDialog reference/output path.");return;}
            window=new Window();RunAlertDialogAsync(window,Path.GetFullPath(arguments[alertDialog+1]),Path.GetFullPath(arguments[output+1]));return;
        }
        if(arguments.Contains("--probe-hidden-button-default-style"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");
            if(output<0||output+1>=arguments.Length){Report(false,"Missing button default style probe output path.");return;}
            window=new Window();RunButtonDefaultStyleProbeAsync(window,Path.GetFullPath(arguments[output+1]));return;
        }
        var scrollAreas=Array.IndexOf(arguments,"--verify-hidden-scroll-areas");
        if(scrollAreas>=0)
        {
            var output=Array.IndexOf(arguments,"--verify-output");
            if(scrollAreas+1>=arguments.Length||output<0||output+1>=arguments.Length){Report(false,"Missing ScrollArea reference/output path.");return;}
            window=new Window();RunScrollAreasAsync(window,Path.GetFullPath(arguments[scrollAreas+1]),Path.GetFullPath(arguments[output+1]));return;
        }
        if(arguments.Contains("--probe-hidden-scroll-area"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");
            if(output<0||output+1>=arguments.Length){Report(false,"Missing ScrollArea primitive output path.");return;}
            window=new Window();_=Orchard.Testing.ScrollAreaPrimitiveProbe.RunAsync(window,Path.GetFullPath(arguments[output+1]));return;
        }
        var hoverCards=Array.IndexOf(arguments,"--verify-hidden-hover-cards");
        if(hoverCards>=0)
        {
            var output=Array.IndexOf(arguments,"--verify-output");
            if(hoverCards+1>=arguments.Length||output<0||output+1>=arguments.Length){Report(false,"Missing HoverCard reference/output path.");return;}
            window=new Window();_=Orchard.Testing.HoverCardReferenceFixture.RunAsync(window,Path.GetFullPath(arguments[hoverCards+1]),Path.GetFullPath(arguments[output+1]),typeof(App).Assembly.Location);return;
        }
        if(arguments.Contains("--probe-hidden-hover-card-inline"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");
            if(output<0||output+1>=arguments.Length){Report(false,"Missing HoverCard inline probe output path.");return;}
            window=new Window();_=Orchard.Testing.HoverCardPlacementProbe.RunAsync(window,Path.GetFullPath(arguments[output+1]));return;
        }
        var popovers=Array.IndexOf(arguments,"--verify-hidden-popovers");
        if(popovers>=0)
        {
            var output=Array.IndexOf(arguments,"--verify-output");
            if(popovers+1>=arguments.Length||output<0||output+1>=arguments.Length){Report(false,"Missing Popover reference/output path.");return;}
            window=new Window();_=Orchard.Testing.PopoverReferenceFixture.RunAsync(window,Path.GetFullPath(arguments[popovers+1]),Path.GetFullPath(arguments[output+1]),typeof(App).Assembly.Location);return;
        }
        if(arguments.Contains("--probe-hidden-popover-placement"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");
            if(output<0||output+1>=arguments.Length){Report(false,"Missing Popover placement probe output path.");return;}
            window=new Window();_=Orchard.Testing.PopoverPlacementProbe.RunAsync(window,Path.GetFullPath(arguments[output+1]));return;
        }
        if(arguments.Contains("--probe-hidden-header-paint"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");
            if(output<0||output+1>=arguments.Length){Report(false,"Missing paint probe output path.");return;}
            window=new Window();RunHeaderPaintProbeAsync(window,Path.GetFullPath(arguments[output+1]));return;
        }
        var headerContracts=Array.IndexOf(arguments,"--verify-hidden-header-contracts");
        if(headerContracts>=0)
        {
            var output=Array.IndexOf(arguments,"--verify-output");
            if(headerContracts+1>=arguments.Length||output<0||output+1>=arguments.Length){Report(false,"Missing Header composition reference/output path.");return;}
            window=new Window();_=Orchard.Testing.HeaderReferenceFixture.RunContractsAsync(window,Path.GetFullPath(arguments[headerContracts+1]),Path.GetFullPath(arguments[output+1]),typeof(App).Assembly.Location);return;
        }
        var headers=Array.IndexOf(arguments,"--verify-hidden-headers");
        if(headers>=0)
        {
            var output=Array.IndexOf(arguments,"--verify-output");
            if(headers+1>=arguments.Length||output<0||output+1>=arguments.Length){Report(false,"Missing Header reference/output path.");return;}
            window=new Window();_=Orchard.Testing.HeaderReferenceFixture.RunAsync(window,Path.GetFullPath(arguments[headers+1]),Path.GetFullPath(arguments[output+1]),typeof(App).Assembly.Location);return;
        }
        var contracts=Array.IndexOf(arguments,"--verify-hidden-component-contracts");
        var sliders=Array.IndexOf(arguments,"--verify-hidden-slider-contracts");
        var feedColumns=Array.IndexOf(arguments,"--verify-hidden-feed-columns");
        var feedItems=Array.IndexOf(arguments,"--verify-hidden-feed-items");
        var selects=Array.IndexOf(arguments,"--verify-hidden-selects");
        var appShells=Array.IndexOf(arguments,"--verify-hidden-app-shells");
        var adminShells=Array.IndexOf(arguments,"--verify-hidden-admin-shells");
        var paginations=Array.IndexOf(arguments,"--verify-hidden-paginations");
        var collectionFilters=Array.IndexOf(arguments,"--verify-hidden-collection-filters");
        var navMenus=Array.IndexOf(arguments,"--verify-hidden-nav-menus");
        var footers=Array.IndexOf(arguments,"--verify-hidden-footers");
        if(arguments.Contains("--probe-hidden-header-placement"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");
            if(output<0||output+1>=arguments.Length){Report(false,"Missing Header placement probe output.");return;}
            window=new Window();_=Orchard.Testing.HeaderPlacementProbe.RunAsync(window,Path.GetFullPath(arguments[output+1]));return;
        }
        if(arguments.Contains("--verify-hidden-footer-contracts"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");
            if(output<0||output+1>=arguments.Length){Report(false,"Missing Footer contract report path.");return;}
            window=new Window();
            _=Orchard.Testing.FooterContractFixture.RunAsync(window,Path.GetFullPath(arguments[output+1]),typeof(App).Assembly.Location);return;
        }
        if(arguments.Contains("--verify-hidden-producer-threading"))
        {
            var output=Array.IndexOf(arguments,"--verify-output");
            if(output<0||output+1>=arguments.Length){Report(false,"Missing producer-threading report path.");return;}
            window=new Window();
            _=Orchard.Testing.ProducerThreadingFixture.RunAsync(window,Path.GetFullPath(arguments[output+1]),typeof(App).Assembly.Location);return;
        }
        if(footers>=0)
        {
            if(footers+1>=arguments.Length){Report(false,"Missing hidden Footer reference directory.");return;}
            RunHiddenFootersAsync(arguments[footers+1]);return;
        }
        if(navMenus>=0)
        {
            if(navMenus+1>=arguments.Length){Report(false,"Missing hidden Nav Menu reference directory.");return;}
            RunHiddenNavMenusAsync(arguments[navMenus+1]);return;
        }
        if(collectionFilters>=0)
        {
            if(collectionFilters+1>=arguments.Length){Report(false,"Missing hidden Collection Filters reference directory.");return;}
            RunHiddenCollectionFiltersAsync(arguments[collectionFilters+1]);return;
        }
        if(paginations>=0)
        {
            if(paginations+1>=arguments.Length){Report(false,"Missing hidden Pagination reference directory.");return;}
            RunHiddenPaginationsAsync(arguments[paginations+1]);return;
        }
        if(adminShells>=0)
        {
            if(adminShells+1>=arguments.Length){Report(false,"Missing hidden AdminShell reference directory.");return;}
            RunHiddenAdminShellsAsync(arguments[adminShells+1]);return;
        }
        if(appShells>=0)
        {
            if(appShells+1>=arguments.Length){Report(false,"Missing hidden AppShell reference directory.");return;}
            RunHiddenAppShellsAsync(arguments[appShells+1]);return;
        }
        if(selects>=0)
        {
            if(selects+1>=arguments.Length){Report(false,"Missing hidden Select reference directory.");return;}
            RunHiddenSelectsAsync(arguments[selects+1]);return;
        }
        if(feedItems>=0)
        {
            if(feedItems+1>=arguments.Length){Report(false,"Missing hidden FeedItem reference directory.");return;}
            RunHiddenFeedItemsAsync(arguments[feedItems+1]);return;
        }
        if(feedColumns>=0)
        {
            if(feedColumns+1>=arguments.Length){Report(false,"Missing hidden FeedColumn reference directory.");return;}
            RunHiddenFeedColumnsAsync(arguments[feedColumns+1]);return;
        }
        if(sliders>=0)
        {
            if(sliders+1>=arguments.Length){Report(false,"Missing hidden Slider reference directory.");return;}
            RunHiddenSliderContractsAsync(arguments[sliders+1]);return;
        }
        if(contracts>=0)
        {
            if(contracts+1>=arguments.Length){Report(false,"Missing hidden component reference directory.");return;}
            RunHiddenComponentContractsAsync(arguments[contracts+1]);return;
        }
        if(hidden>=0)
        {
            if(hidden+1>=arguments.Length){Report(false,"Missing hidden ProgressStack reference directory.");return;}
            RunHiddenProgressStacksAsync(arguments[hidden+1]);return;
        }
        if(arguments.Contains("--probe-select-foreground"))
        {
            var themeIndex=Array.IndexOf(arguments,"--select-theme");
            if(themeIndex<0||themeIndex+1>=arguments.Length||!Enum.TryParse<ElementTheme>(arguments[themeIndex+1],out var theme)||theme is not (ElementTheme.Light or ElementTheme.Dark))
                throw new ArgumentException("Select foreground probe requires --select-theme Light or Dark.");
            window=new SelectForegroundWindow(theme);window.Activate();return;
        }
        if(arguments.Contains("--probe-otp-foreground"))
        {
            var themeIndex=Array.IndexOf(arguments,"--otp-theme");
            if(themeIndex<0||themeIndex+1>=arguments.Length||!Enum.TryParse<ElementTheme>(arguments[themeIndex+1],out var theme)||theme is not (ElementTheme.Light or ElementTheme.Dark))
                throw new ArgumentException("OTP foreground probe requires --otp-theme Light or Dark.");
            window=new OtpInputForegroundWindow(theme);window.Activate();return;
        }
        if(arguments.Contains("--probe-segmented-foreground"))
        {
            var themeIndex=Array.IndexOf(arguments,"--segmented-theme");
            if(themeIndex<0||themeIndex+1>=arguments.Length||!Enum.TryParse<ElementTheme>(arguments[themeIndex+1],out var theme)||theme is not (ElementTheme.Light or ElementTheme.Dark))
                throw new ArgumentException("Segmented Control foreground probe requires --segmented-theme Light or Dark.");
            window=new SegmentedControlForegroundWindow(theme);window.Activate();return;
        }
        if(arguments.Contains("--probe-hero-foreground"))
        {
            var themeIndex=Array.IndexOf(arguments,"--hero-theme");
            if(themeIndex<0||themeIndex+1>=arguments.Length||!Enum.TryParse<ElementTheme>(arguments[themeIndex+1],out var theme)||theme is not (ElementTheme.Light or ElementTheme.Dark))
                throw new ArgumentException("Hero foreground probe requires --hero-theme Light or Dark.");
            window=new HeroCarouselForegroundWindow(theme);window.Activate();return;
        }
        if(arguments.Contains("--probe-footer-foreground"))
        {
            var themeIndex=Array.IndexOf(arguments,"--footer-theme");
            var outputIndex=Array.IndexOf(arguments,"--verify-output");
            if(themeIndex<0||themeIndex+1>=arguments.Length||outputIndex<0||outputIndex+1>=arguments.Length||
                !Enum.TryParse<ElementTheme>(arguments[themeIndex+1],out var theme)||theme is not (ElementTheme.Light or ElementTheme.Dark))
                throw new ArgumentException("Footer foreground probe requires --footer-theme Light or Dark and --verify-output path.");
            window=new FooterForegroundWindow(theme,Path.GetFullPath(arguments[outputIndex+1]));window.Activate();return;
        }
        try { window = new MainWindow(); window.Activate(); }
        catch (Exception error) { Report(false, error.ToString()); throw; }
    }

    private static async void RunButtonDefaultStyleProbeAsync(Window target, string output)
    {
        try { await Orchard.Testing.ButtonDefaultStyleProbe.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { Report(false, error.ToString()); }
    }

    private static async void RunRenderOwnerLifetimeAsync(Window target,string output){try{await Orchard.Testing.RenderOwnerLifetimeFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunButtonLifecycleAsync(Window target,string output){try{await Orchard.Testing.ButtonLifecycleFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunCheckboxContentAsync(Window target, string output)
    {
        try { await Orchard.Testing.CheckboxContentFixture.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { Report(false, error.ToString()); }
    }
    private static async void RunSwitchLifecycleAsync(Window target, string output)
    {
        try { await Orchard.Testing.SwitchLifecycleFixture.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { Report(false, error.ToString()); }
    }

    private static async void RunToggleGroupAsync(Window target, string output)
    {
        try { await Orchard.Testing.ToggleGroupFixture.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { Report(false, error.ToString()); }
    }

    private static async void RunLabelAssociationAsync(Window target, string output)
    {
        try { await Orchard.Testing.LabelAssociationFixture.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { Report(false, error.ToString()); }
    }

    private static async void RunButtonGroupAsync(Window target, string output)
    {
        try { await Orchard.Testing.ButtonGroupFixture.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { Report(false, error.ToString()); }
    }
    private static async void RunHeaderPaintProbeAsync(Window target, string output)
    {
        try { await Orchard.Testing.HeaderPaintProbe.RunAsync(target, output); }
        catch (Exception error) { Report(false, error.ToString()); }
    }

    private static async void RunScrollAreasAsync(Window target,string reference,string output)
    {
        try { await Orchard.Testing.ScrollAreaReferenceFixture.RunAsync(target,reference,output,typeof(App).Assembly.Location); }
        catch(Exception error) { Report(false,error.ToString()); }
    }
    private static async void RunAlertDialogAsync(Window target,string reference,string output)
    {
        try { await Orchard.Testing.AlertDialogReferenceFixture.RunAsync(target,reference,output,typeof(App).Assembly.Location); }
        catch(Exception error) { Report(false,error.ToString()); }
    }
    private static async void RunModalAsync(Window target,string reference,string output)
    {
        try { await Orchard.Testing.ModalReferenceFixture.RunAsync(target,reference,output,typeof(App).Assembly.Location); }
        catch(Exception error) { Report(false,error.ToString()); }
    }
    private static async void RunModalContentAsync(Window target,string output)
    {
        try { await Orchard.Testing.ModalContentFixture.RunAsync(target,output,typeof(App).Assembly.Location); }
        catch(Exception error) { Report(false,error.ToString()); }
    }
    private static async void RunEdgeDialogAsync(Window target,string reference,string output)
    {
        try{await Orchard.Testing.EdgeDialogReferenceFixture.RunAsync(target,reference,output,typeof(App).Assembly.Location);}
        catch(Exception error){Report(false,error.ToString());}
    }
    private static async void RunEdgeCompositionAsync(Window target,string output)
    {
        try{await Orchard.Testing.EdgeDialogCompositionFixture.RunAsync(target,output,typeof(App).Assembly.Location);}
        catch(Exception error){Report(false,error.ToString());}
    }
    private static async void RunEdgeMotionAsync(Window target,string output)
    {
        try{await Orchard.Testing.EdgeMotionFixture.RunAsync(target,output,typeof(App).Assembly.Location);}
        catch(Exception error){Report(false,error.ToString());}
    }
    private static async void RunSecureFieldAsync(Window target, string output)
    {
        try { await Orchard.Testing.SecureFieldFixture.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { Report(false, error.GetType().Name + ": " + error.Message); }
    }
    private static async void RunSegmentedControlAsync(Window target, string output)
    {
        try { await Orchard.Testing.SegmentedControlFixture.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { Report(false, error.GetType().Name + ": " + error.Message); }
    }
    private static async void RunAppTabBarAsync(Window target, string output)
    {
        try { await Orchard.Testing.AppTabBarFixture.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { Report(false, error.ToString()); }
    }
    private static async void RunOtpInputAsync(Window target, string output, string source)
    {
        try { await Orchard.Testing.OtpInputFixture.RunAsync(target, output, typeof(App).Assembly.Location, source); }
        catch (Exception error) { Report(false, error.ToString()); }
    }
    private static async void RunContextMenuRootAsync(Window target, string output)
    {
        try { await Orchard.Testing.ContextMenuFixture.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { Report(false, error.ToString()); }
    }
    private static async void RunContextMenuModalAsync(Window target, string output)
    {
        try { await Orchard.Testing.ContextMenuModalFixture.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { Report(false, error.ToString()); }
    }
    private static async void RunTablePerformanceAsync(Window target,string output){try{await Orchard.Testing.TablePerformanceFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunTableLabelRasterAsync(Window target,string output){try{await Orchard.Testing.TableTemplateFixture.RunAsync(target,output,typeof(App).Assembly.Location,true);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunTableTemplateAsync(Window target,string output){try{await Orchard.Testing.TableTemplateFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunTableMinimumAsync(Window target,string output){try{await Orchard.Testing.TableMinimumFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunTabsBorderAsync(Window target,string reference,string folder){try{await Orchard.Testing.TabsBorderPaintFixture.RunAsync(target,reference,folder,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunTableButtonContentForegroundAsync(Window target,string output){try{await Orchard.Testing.TableButtonContentForegroundFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunTableNavigationForegroundAsync(Window target,string output){try{await Orchard.Testing.TableNavigationForegroundFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunTableCustomizationAsync(Window target,string output){try{await Orchard.Testing.TableCustomizationFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunTableTextAsync(Window target,string output){try{await Orchard.Testing.TableTextFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunDataTableLiveAsync(Window target,string output){try{await Orchard.Testing.DataTableFixture.RunLiveAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunDataTablePerformanceAsync(Window target,string output,bool pagingOnly=false){try{await Orchard.Testing.DataTablePerformanceFixture.RunAsync(target,output,typeof(App).Assembly.Location,pagingOnly);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunNavigationMenuAsync(Window target,string output){try{await Orchard.Testing.NavigationMenuFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunSonnerAsync(Window target,string output){try{await Orchard.Testing.SonnerFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunHeroCarouselAsync(Window target,string output){try{await Orchard.Testing.HeroCarouselFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunMenubarAsync(Window target,string output){try{await Orchard.Testing.MenubarFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunBrandMarkAsync(Window target,string output){try{await Orchard.Testing.BrandMarkFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunDataTableFilterPlansAsync(Window target,string output){try{await Orchard.Testing.DataTableReferenceFixture.RunFilterPlansAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunDataTableReferenceAsync(Window target,string output){try{await Orchard.Testing.DataTableReferenceFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunDataTableAsync(Window target,string output){try{await Orchard.Testing.DataTableFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunSimpleTableAsync(Window target,string output){try{await Orchard.Testing.SimpleDataTableFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunTableAsync(Window target,string output){try{await Orchard.Testing.TablePrimitiveFixture.RunAsync(target,output,typeof(App).Assembly.Location);}catch(Exception error){Report(false,error.ToString());}}
    private static async void RunContextMenuOptionsAsync(Window target, string output)
    {
        try { await Orchard.Testing.ContextMenuOptionsFixture.RunAsync(target, output, typeof(App).Assembly.Location); }
        catch (Exception error) { Report(false, error.ToString()); }
    }
    private static async void RunPopoverModalRootAsync(Window target,string output)
    {
        try { await PopoverModalRootFixture.RunAsync(target,output); }
        catch(Exception error) { Report(false,error.ToString()); }
    }
    private static async void RunSelectModalRootAsync(Window target,string output)
    {
        try { await SelectModalRootFixture.RunAsync(target,output); }
        catch(Exception error) { Report(false,error.ToString()); }
    }
    private static async void RunTooltipModalRootAsync(Window target,string output)
    {
        try { await TooltipModalRootFixture.RunAsync(target,output); }
        catch(Exception error) { Report(false,error.ToString()); }
    }
    internal static void Report(bool passed, string message)
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, "--verify-output");
        if (index < 0) return;
        var path = args[index + 1];
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new { passed, message,
            assembly = typeof(Orchard.BleeckerTokens).Assembly.Location,
            architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            osArchitecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
            osDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription },
            new JsonSerializerOptions { WriteIndented = true }));
        Environment.Exit(passed ? 0 : 1);
    }
}
