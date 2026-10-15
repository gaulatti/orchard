using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using Orchard;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Runtime.InteropServices;

namespace PackageConsumer;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        PackageSunburstChart.Data = SunburstData();
        PackageCurrencyChart.Data = CurrencyData();
        PackageBauhaus.ImageSource = new WriteableBitmap(4, 4);
        foreach(var value in new[]{10d,30d,20d,40d})PackageSparkline.Data.Add(new Dictionary<string,object?>{{"v",value}});
        PackageBarChart.Data.Add(new Dictionary<string,object?>{{"label","A"},{"v",10d}});
        PackageBarChart.Data.Add(new Dictionary<string,object?>{{"label","B"},{"v",20d}});
        PackageAreaChart.Data.Add(new Dictionary<string,object?>{{"label","A"},{"v",10d}});
        PackageAreaChart.Data.Add(new Dictionary<string,object?>{{"label","B"},{"v",20d}});
        foreach (var data in new[] { PackagePieChart.Data, PackageDonutChart.Data })
        {
            data.Add(new BleeckerPieChartDatum { Name = "A", Value = 10 });
            data.Add(new BleeckerPieChartDatum { Name = "B", Value = 20 });
        }
        ResetScatterData();
        AppWindow.Resize(new SizeInt32(1100, 760));
    }
    private void OnSave(object sender, RoutedEventArgs e) => Result.Text = $"Saved: {Input.Text}";

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!Environment.GetCommandLineArgs().Contains("--verify-output")) return;
        try
        {
            foreach (var font in new[] { "EncodeSans-Regular.ttf", "EncodeSans-Medium.ttf", "EncodeSans-SemiBold.ttf", "LibreFranklin-Regular.ttf", "LibreFranklin-SemiBold.ttf", "OFL-EncodeSans.txt", "OFL-LibreFranklin.txt" })
                Require(File.Exists(Path.Combine(AppContext.BaseDirectory, "Orchard", "Assets", "Fonts", font)), $"Missing package asset: {font}");
            Require(Heading.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf#Encode Sans"), "Package font URI not selected.");
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                Root.RequestedTheme = theme;
                await Task.Delay(250);
                Root.UpdateLayout();
                var switchPeer = FrameworkElementAutomationPeer.CreatePeerForElement(PackageSwitch);
                var nativeSwitchPeer = FrameworkElementAutomationPeer.CreatePeerForElement((ToggleSwitch)VisualTreeHelper.GetChild(PackageSwitch, 0));
                var switchToggle = (IToggleProvider)switchPeer.GetPattern(PatternInterface.Toggle);
                Require(PackageSwitch.FontSize == 14 && PackageSwitch.FontFamily.Source.Contains("EncodeSans-Medium.ttf") &&
                    switchPeer.GetName() == "Package notifications" && switchPeer.GetAutomationControlType() == nativeSwitchPeer.GetAutomationControlType() &&
                    switchPeer.IsControlElement() && switchPeer.IsContentElement(),
                    "Packaged switch lost its native semantics, label or bundled font.");
                switchToggle.Toggle();
                Require(PackageSwitch.IsOn && switchToggle.ToggleState == ToggleState.On, "Packaged native switch did not update consumer state.");
                PackageSwitch.IsOn = false;
                Require(switchToggle.ToggleState == ToggleState.Off, "Packaged consumer state did not reach the native switch.");
                var checkboxPeer = FrameworkElementAutomationPeer.CreatePeerForElement(PackageCheckbox);
                var checkboxToggle = (IToggleProvider)checkboxPeer.GetPattern(PatternInterface.Toggle);
                Require(checkboxPeer.GetAutomationControlType() == AutomationControlType.CheckBox && checkboxPeer.GetName() == "Package notifications Consumer-owned checkbox state" &&
                    PackageCheckbox.FontFamily.Source.Contains("EncodeSans-Medium.ttf") && PackageCheckbox.ActualHeight == 42,
                    "Packaged checkbox lost its template, content, font or native role.");
                checkboxToggle.Toggle();
                Require(PackageCheckbox.IsChecked == true && checkboxToggle.ToggleState == ToggleState.On, "Packaged checkbox did not reach consumer state.");
                PackageCheckbox.IsChecked = false;
                Require(checkboxToggle.ToggleState == ToggleState.Off, "Packaged consumer state did not reach its checkbox.");
                var packagedRange = (IRangeValueProvider)FrameworkElementAutomationPeer.CreatePeerForElement(PackageProgress).GetPattern(PatternInterface.RangeValue);
                Require(packagedRange.IsReadOnly && packagedRange.Value == 65 && packagedRange.Maximum == 100 && PackageProgress.FontSize == 12,
                    "Packaged progress lost its resources or native range semantics.");
                Require(PackageEyebrow.ActualHeight == 16 && PackageEyebrow.FontSize == 11 && !PackageEyebrow.IsTabStop &&
                    PackageEyebrow.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf"), "Packaged eyebrow lost its resources or geometry.");
                Require(((Grid)VisualTreeHelper.GetChild(PackageEyebrow, 0)).Children.OfType<TextBlock>().Single().Text == "STRASSE" &&
                    (string)PackageEyebrow.Content == "Straße", "Packaged eyebrow lost the embedded Unicode mapping or changed consumer content.");
                Require(PackageKeyboard.ActualHeight == 20 && PackageKeyboard.FontSize == 11 && !PackageKeyboard.IsTabStop &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(PackageKeyboard).GetName() == "Esc",
                    "Packaged keyboard display lost its template, typography or accessible content.");
                Require(Input.ActualHeight == 40 && Input.FontSize == 14 && Input.FontFamily.Source.Contains("EncodeSans-Regular.ttf") &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(Input).GetAutomationControlType() == AutomationControlType.Edit,
                    "Packaged input lost its geometry, font or native editor.");
                var inputBorder=(Border)((FrameworkElement)VisualTreeHelper.GetChild(Input,0)).FindName("BorderElement");
                Require(inputBorder.BackgroundSizing==BackgroundSizing.OuterBorderEdge,
                    "Packaged input background does not cover its source border box.");
                Input.BackgroundSizing=BackgroundSizing.InnerBorderEdge;Root.UpdateLayout();
                Require(inputBorder.BackgroundSizing==BackgroundSizing.InnerBorderEdge,"Packaged input ignored native consumer sizing.");
                Input.ClearValue(Control.BackgroundSizingProperty);Root.UpdateLayout();
                Require(inputBorder.BackgroundSizing==BackgroundSizing.OuterBorderEdge,"Packaged input did not restore border-box sizing.");
                Require(PackageFieldLabel.FontFamily.Source.Contains("EncodeSans-Medium.ttf") && PackageFieldLabel.FontSize == 13 &&
                    !PackageFieldLabel.IsTabStop && ReferenceEquals(Microsoft.UI.Xaml.Automation.AutomationProperties.GetLabeledBy(Input), PackageFieldLabel) &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(PackageFieldLabel).GetName() == "Title", "Packaged field label lost typography or input association.");
                Require(PackageHelp.ActualWidth == 16 && PackageHelp.ActualHeight == 16 &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(PackageHelp).GetName() == PackageHelp.Text &&
                    BleeckerTooltipService.GetToolTip(PackageHelp)?.Placement == Microsoft.UI.Xaml.Controls.Primitives.PlacementMode.Bottom,
                    "Packaged field help lost its size, accessible name or bottom tooltip.");
                await AcquireConsumerInputAsync();
                PackageMenu.ShowAt(MenuTrigger);
                await Task.Delay(200);
                var menuPresenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot).Select(popup => popup.Child).OfType<MenuFlyoutPresenter>().Single();
                menuPresenter.ApplyTemplate(); menuPresenter.UpdateLayout();
                await AwaitAsync(() => menuPresenter.IsLoaded && VisualTreeHelper.GetChildrenCount(menuPresenter) > 0, "Packaged menu presenter did not load its template.");
                Require(menuPresenter.Padding.Left == 6 && menuPresenter.CornerRadius.TopLeft == 7 &&
                    menuPresenter.FontFamily.Source.Contains("EncodeSans-Regular.ttf"), "Packaged menu lost its branded presenter.");
                var menuSurface = (BleeckerOverlaySurface)VisualTreeHelper.GetChild(menuPresenter, 0);
                menuSurface.ApplyTemplate(); menuSurface.UpdateLayout();
                await AwaitAsync(() => menuSurface.IsLoaded && VisualTreeHelper.GetChildrenCount(menuSurface) > 0, "Packaged menu surface did not load its template.");
                var menuSurfaceRoot = (Grid)VisualTreeHelper.GetChild(menuSurface, 0);
                await AwaitAsync(() => ElementCompositionPreview.GetElementChildVisual((Grid)menuSurfaceRoot.Children[1]) is ContainerVisual layers && layers.Children.Count == 2, "Packaged menu shadow layers did not attach after loading.");
                Require(ElementCompositionPreview.GetElementChildVisual((Grid)menuSurfaceRoot.Children[1]) is ContainerVisual menuShadows && menuShadows.Children.Count == 2,
                    "Packaged menu did not render its overlay shadow layers.");
                var labelPeer = FrameworkElementAutomationPeer.CreatePeerForElement(PackageLabel);
                Require(PackageContentItem.ActualHeight == 32 && PackageContentText.FontSize == 14 &&
                    PackageContentText.FontFamily.Source.Contains("EncodeSans-Regular.ttf") &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(PackageContentItem).GetName() == "Review locally",
                    "Packaged composed menu lost its default template, typography or action name.");
                Require(PackageInset.Padding == new Thickness(32, 6, 8, 6) && PackageInset.ActualHeight == 32,
                    "Packaged inset item lost its reference geometry.");
                var presentedLabel = FrameworkElementAutomationPeer.CreatePeerForElement(menuPresenter).GetChildren()
                    .SingleOrDefault(peer => peer.GetName() == "Account");
                Require(presentedLabel is not null && presentedLabel.GetAutomationControlType() == AutomationControlType.Text &&
                    presentedLabel.GetPattern(PatternInterface.Invoke) is null, "Packaged presenter omitted or made the label actionable.");
                Require(PackageLabel.ActualHeight == 27 && PackageLabel.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf") &&
                    labelPeer.IsControlElement() && labelPeer.IsContentElement() && labelPeer.GetName() == "Account" &&
                    labelPeer.GetAutomationControlType() == AutomationControlType.Text,
                    $"Packaged menu label mismatch: height={PackageLabel.ActualHeight}, font={PackageLabel.FontFamily.Source}, control={labelPeer.IsControlElement()}, content={labelPeer.IsContentElement()}, name={labelPeer.GetName()}, type={labelPeer.GetAutomationControlType()}.");
                PackageMenu.Hide();
                await Task.Delay(150);
                var notificationTrigger = (BleeckerIconButton)PackageNotifications.FindName("Trigger");
                ((IInvokeProvider)new ButtonAutomationPeer(notificationTrigger).GetPattern(PatternInterface.Invoke)).Invoke();
                await Task.Delay(200);
                var notificationPresenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot)
                    .Select(popup => popup.Child).OfType<FlyoutPresenter>().Single();
                Require(notificationPresenter.ActualWidth == 320 && notificationTrigger.ActualHeight == 40 &&
                    ((TextBlock)PackageNotifications.FindName("CountText")).Text == "1",
                    "Packaged notification control lost its XAML resources or geometry.");
                Require(((ItemsControl)PackageNotifications.FindName("NotificationItems")).Items.Count == 1,
                    "Packaged notification collection did not reach the popup.");
                var notificationFocus=FocusManager.GetFocusedElement(Root.XamlRoot);
                try
                {
                    var opposite=theme==ElementTheme.Light?ElementTheme.Dark:ElementTheme.Light;
                    Root.RequestedTheme=opposite;
                    await VerifyNotificationThemeAsync(notificationPresenter,opposite);
                    PackageNotifications.RequestedTheme=theme;
                    await VerifyNotificationThemeAsync(notificationPresenter,theme);
                    Require(Root.RequestedTheme==opposite,"Packaged local notification theme changed its consumer root.");
                    PackageNotifications.ClearValue(FrameworkElement.RequestedThemeProperty);
                    await VerifyNotificationThemeAsync(notificationPresenter,opposite);
                    Require(ReferenceEquals(FocusManager.GetFocusedElement(Root.XamlRoot),notificationFocus)&&notificationPresenter.ActualWidth==320,
                        "Packaged notification theme changes moved focus or resized the open popup.");
                }
                finally { PackageNotifications.ClearValue(FrameworkElement.RequestedThemeProperty);Root.RequestedTheme=theme; }
                await VerifyNotificationThemeAsync(notificationPresenter,theme);
                notificationTrigger.Flyout.Hide();
                await Task.Delay(150);
                PackageTooltip.IsOpen = true;
                await Task.Delay(150);
                var tooltipRoot = (Grid)VisualTreeHelper.GetChild(PackageTooltip, 0);
                var tooltipSurface = (BleeckerOverlaySurface)tooltipRoot.Children[0];
                Require(tooltipSurface.ActualWidth > 0 && tooltipSurface.ActualWidth <= 224 &&
                    tooltipSurface.Padding.Left == 12 && PackageTooltip.FontSize == 12 &&
                    PackageTooltip.FontFamily.Source.Contains("EncodeSans-Regular.ttf") &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(PackageTooltip).GetAutomationControlType() == AutomationControlType.ToolTip,
                    "Packaged tooltip lost its template, font, geometry or native role.");
                var tooltipColor = ((SolidColorBrush)tooltipSurface.Background).Color;
                Root.RequestedTheme = theme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
                await Task.Delay(250);
                Require(PackageTooltip.IsOpen && PackageTooltip.ActualTheme == Root.ActualTheme && ((SolidColorBrush)tooltipSurface.Background).Color != tooltipColor,
                    "Packaged tooltip did not follow its owner's live appearance.");
                Root.RequestedTheme = theme;
                await Task.Delay(250);
                Require(((SolidColorBrush)tooltipSurface.Background).Color == tooltipColor, "Packaged tooltip did not restore its original appearance.");
                var tooltipOwnerPeer = FrameworkElementAutomationPeer.CreatePeerForElement(PackageTooltip.PlacementTarget!);
                ((IInvokeProvider)tooltipOwnerPeer.GetPattern(PatternInterface.Invoke)).Invoke();
                await Task.Delay(50);
                Require(!PackageTooltip.IsOpen, "Packaged tooltip did not dismiss after native activation.");
                PackageTooltip.IsOpen = false;
                await Task.Delay(80);
                Require(Action.ActualWidth > 0 && Heading.ActualHeight > 0, "Consumer controls did not lay out.");
                Require(PackageCard.Padding.Left == 16 && PackageCard.CornerRadius.TopLeft == 20 && PackageCard.ActualWidth > 0,
                    "Packaged card did not load its geometry and content template.");
                Require(PackageSkeleton.ActualHeight == 16 && PackageSkeleton.ActualWidth == 160 && PackageSkeletonCard.ActualHeight == 88 &&
                    PackageSkeletonTable.ActualHeight == 32 && !FrameworkElementAutomationPeer.CreatePeerForElement(PackageSkeleton).IsControlElement(),
                    "Packaged Skeleton controls lost their independent templates, geometry or decorative semantics.");
                var avatarRoot = (FrameworkElement)VisualTreeHelper.GetChild(PackageAvatar, 0);
                Require(PackageAvatar.ActualWidth == 40 && PackageAvatar.ActualHeight == 40 && PackageAvatar.FontFamily.Source.Contains("EncodeSans-Medium.ttf") &&
                    ((TextBlock)avatarRoot.FindName("PART_Initials")).Text == "SSSS" &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(PackageAvatar).GetName() == "SSSS",
                    "Packaged Avatar lost its template, font, Unicode mappings or semantics.");
                Require(PackageAvatarGroup.ActualHeight == 40 && PackageAvatarGroup.Avatars.Count == 3 &&
                    ((Panel)((Grid)VisualTreeHelper.GetChild(PackageAvatarGroup, 0)).Children.Single()).Children.Count == 3,
                    "Packaged AvatarGroup lost its data model or overflow composition.");
                var breadcrumbPanel = ((Grid)VisualTreeHelper.GetChild(PackageBreadcrumb, 0)).Children.OfType<Panel>().Single();
                Require(PackageBreadcrumb.Items.Count == 4 && breadcrumbPanel.Children.Count == 5 && PackageBreadcrumb.ActualHeight == 24 &&
                    breadcrumbPanel.Children.OfType<HyperlinkButton>().Single().FontFamily.Source.Contains("EncodeSans-Regular.ttf") &&
                    ((TextBlock)breadcrumbPanel.Children.Last()).Text == "Harbor",
                    "Packaged Breadcrumb lost its independent templates, font or consumer path.");
                await VerifyMutableBreadcrumbAsync(breadcrumbPanel.Children.OfType<HyperlinkButton>().Single());
                await VerifyCustomBreadcrumbFocusAsync();
                await VerifyAlertAsync();
                await VerifyBrandAsync();
                VerifyBauhaus();
                VerifyActivity();
                VerifyAttention();
                await VerifyAttentionScrollAsync();
                await VerifyChartThemeAsync(theme);
                await VerifyLineChartAsync();
                await VerifyErrorStateAsync();
                await VerifyPageHeaderAsync();
                VerifySectionHeader();
                VerifyDashboardSection();
                VerifyDashboardGrid();
                await VerifySidebarAsync();
                await VerifySparklineAsync();
                await VerifyBarChartAsync();
                await VerifyAreaChartAsync();
                await VerifyCircularChartsAsync();
                await VerifyScatterChartAsync();
                await VerifyRadarChartAsync();
                await VerifyRadialBarChartAsync();
                await VerifyFunnelChartAsync();
                await VerifySankeyChartAsync();
                await VerifySunburstChartAsync();
                await VerifyCurrencyChartAsync();
                await VerifyStatCardAsync();
                Require(PackageSeparator.ActualHeight == 1 && PackageSeparator.ActualWidth > 0 &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(PackageSeparator).GetAutomationControlType() == AutomationControlType.Separator,
                    "Packaged separator lost its geometry or semantics.");
                Require(PackageStatus.ActualHeight == 24 && FrameworkElementAutomationPeer.CreatePeerForElement(PackageStatus).GetName() == "LOCAL",
                    "Packaged status badge lost its template or accessible name.");
                var cardRoot = (Grid)VisualTreeHelper.GetChild(PackageCard, 0);
                var cardMask = (Microsoft.UI.Xaml.Shapes.Path)((Canvas)cardRoot.Children[0]).Children[0];
                Require(((PathGeometry)cardMask.Data).Figures.Single().Segments.OfType<ArcSegment>().Select(arc => arc.Size.Width).SequenceEqual(new double[] { 8, 4, 16, 20 }),
                    "Packaged elevation did not retain consumer corner geometry.");
                Require(PackageSpinner.ActualWidth == 16 && PackageSpinner.ActualHeight == 16 &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(PackageSpinner).GetAutomationControlType() == AutomationControlType.ProgressBar,
                    "Packaged loading spinner lost its geometry or progress role.");
                var scheme = theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark;
                var color = ((SolidColorBrush)Action.Background).Color;
                Require($"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}" == BleeckerTokens.Colors(scheme)["primary"], $"Package theme mismatch: {theme}");
                PackageAction.ApplyTemplate();
                var native = VisualTreeHelper.GetChild(PackageAction, 0) as Button;
                Require(native is not null && native.ActualWidth > 0, "Packaged component template did not load.");
                Require(new ButtonAutomationPeer(native!).GetName() == "Package component", "Packaged text action lost its accessible name.");
                Require(native!.FontWeight.Weight == 500 && native.FontFamily.Source.Contains("EncodeSans-Medium.ttf#Encode Sans"),
                    "Packaged component did not resolve the bundled medium font.");
                Require(PackageIcon.ActualWidth == 40 && PackageIcon.ActualHeight == 40, "Packaged icon control did not lay out.");
                PackageIcon.IsEnabled = false;
                await Task.Delay(100);
                var iconPresenter = (ContentPresenter)VisualTreeHelper.GetChild(PackageIcon, 0);
                Require(SaveVector.Foreground is SolidColorBrush iconBrush && iconPresenter.Foreground is SolidColorBrush iconText && iconBrush.Color == iconText.Color && Math.Abs(iconBrush.Opacity - iconText.Opacity) < 0.001,
                    "Consumer-owned vector did not follow the packaged disabled foreground.");
                PackageIcon.IsEnabled = true;
                Require(PackageGroup.ActualWidth > 0 && PackageGridItem.ActualHeight >= 36, "Packaged toggle group template did not lay out.");
                var componentColor = ((SolidColorBrush)native!.Background).Color;
                Require($"#{componentColor.A:X2}{componentColor.R:X2}{componentColor.G:X2}{componentColor.B:X2}" == BleeckerTokens.Colors(scheme)["primary"], "Packaged per-variant resources did not load.");
                PackageAction.IsLoading = true;
                await Task.Delay(100);
                Require(HasReferenceSpinner(PackageAction), "Packaged loading action lost its reference vector template.");
                await CaptureAsync(theme);
                PackageAction.IsLoading = false;
            }
            Input.Text = "Package interaction verified";
            ((IInvokeProvider)new ButtonAutomationPeer(Action).GetPattern(PatternInterface.Invoke)).Invoke();
            await Task.Delay(100);
            Require(Result.Text == "Saved: Package interaction verified", "Package button invocation failed.");
            Result.Text = "Ready";
            ((IInvokeProvider)new ButtonAutomationPeer((Button)VisualTreeHelper.GetChild(PackageAction, 0)).GetPattern(PatternInterface.Invoke)).Invoke();
            await Task.Delay(100);
            Require(Result.Text == "Saved: Package interaction verified", "Packaged component Click event failed.");
            Result.Text = "Ready";
            ((IInvokeProvider)new ButtonAutomationPeer(PackageIcon).GetPattern(PatternInterface.Invoke)).Invoke();
            await Task.Delay(100);
            Require(Result.Text == "Saved: Package interaction verified", "Packaged icon activation failed.");
            var selection = (ISelectionItemProvider)FrameworkElementAutomationPeer.CreatePeerForElement(PackageGridItem).GetPattern(PatternInterface.SelectionItem);
            selection.Select();
            Require(PackageGroup.SelectedValue == "grid" && PackageGridItem.IsChecked == true, "Packaged group selection failed.");
            selection.RemoveFromSelection();
            Require(string.IsNullOrEmpty(PackageGroup.SelectedValue), "Packaged group optional selection failed.");
            App.Report(true, "Package resources, fonts, light/dark themes, layout and native action verified.");
        }
        catch (Exception error) { App.Report(false, error.ToString()); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private async Task VerifyMutableBreadcrumbAsync(HyperlinkButton link)
    {
        var originalPaint = link.ReadLocalValue(Control.ForegroundProperty); var originalEnabled = link.IsEnabled;
        var producer = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 142, 227, 240));
        try
        {
            // Disabled is a normal consumer state; it removes pointer hover as
            // a competing palette producer without changing native input paths.
            link.IsEnabled = false; link.Foreground = producer; link.ApplyTemplate(); Root.UpdateLayout();
            var text = (TextBlock)((FrameworkElement)VisualTreeHelper.GetChild(link, 0)).FindName("PART_Text");
            await PaintAsync(producer.Color);
            producer.Color = Windows.UI.Color.FromArgb(255, 38, 59, 80); await PaintAsync(producer.Color);
            producer.Opacity = .5; await PaintAsync(Windows.UI.Color.FromArgb(128, 38, 59, 80));
            Require(!link.IsEnabled, "Packaged brush updates enabled a disabled consumer link.");
            async Task PaintAsync(Windows.UI.Color expected)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                bool Matches() => text.Foreground is SolidColorBrush actual && actual.Color == expected && actual.Opacity == 1;
                while (!Matches() && watch.Elapsed < TimeSpan.FromSeconds(2)) { Root.UpdateLayout(); await Task.Delay(16); }
                Require(Matches(), "Packaged Breadcrumb did not observe its consumer-owned brush color/opacity.");
            }
        }
        finally
        {
            if (originalPaint == DependencyProperty.UnsetValue) link.ClearValue(Control.ForegroundProperty);
            else link.SetValue(Control.ForegroundProperty, originalPaint);
            link.IsEnabled = originalEnabled;
        }
    }

    private async Task VerifySparklineAsync()
    {
        // Each theme starts a fresh lifecycle, just like the independent Line
        // Chart fixture. Refocus alone intentionally retains hidden inspection.
        var parent=(Panel)VisualTreeHelper.GetParent(PackageSparkline);var position=parent.Children.IndexOf(PackageSparkline);
        parent.Children.Remove(PackageSparkline);await AwaitAsync(()=>!PackageSparkline.IsLoaded,"Packaged Sparkline did not unmount.");
        parent.Children.Insert(position,PackageSparkline);await AwaitAsync(()=>PackageSparkline.IsLoaded,"Packaged Sparkline did not remount.");
        PackageSparkline.ApplyTemplate();Root.UpdateLayout();
        var chart=BrandDescendants<BleeckerLineChart>(PackageSparkline).Single();var plot=BrandDescendants<Canvas>(chart).Single(x=>x.Name=="PART_Plot");
        await AwaitAsync(()=>plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Count()==1,"Packaged Sparkline did not render.");
        var path=plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Single();var geometry=(PathGeometry)path.Data;
        Require(ReferenceEquals(chart.Data,PackageSparkline.Data)&&Math.Abs(geometry.Figures[0].StartPoint.X-2)<.001&&Math.Abs(geometry.Figures[0].StartPoint.Y-29)<.001&&path.Fill is null&&path.StrokeThickness==2,
            "Packaged Sparkline lost compact geometry, source ShowArea behavior or consumer data identity.");
        Require(AutomationProperties.GetName(chart)=="Independent compact series"&&!PackageSparkline.IsTabStop&&chart.IsTabStop,"Packaged Sparkline lost its inspection boundary.");
        await AcquireConsumerInputAsync();Require(chart.Focus(FocusState.Keyboard),"Could not focus packaged Sparkline.");
        await AwaitAsync(()=>AutomationProperties.GetHelpText(chart)==" : 10","Packaged Sparkline did not inspect its first raw value.");
        Require(GetForegroundWindow()==WinRT.Interop.WindowNative.GetWindowHandle(this),"Packaged Sparkline lost its native foreground.");
        var keys=new[]{new ConsumerInput{Type=1,Data=new ConsumerInputData{Keyboard=new ConsumerKeyboard{VirtualKey=0x27}}},new ConsumerInput{Type=1,Data=new ConsumerInputData{Keyboard=new ConsumerKeyboard{VirtualKey=0x27,Flags=2}}}};
        Require(SendInput(2,keys,Marshal.SizeOf<ConsumerInput>())==2,"Packaged Sparkline Right did not enter native input.");
        await AwaitAsync(()=>AutomationProperties.GetHelpText(chart)==" : 30","Packaged Sparkline did not inspect the next raw value through physical Right.");
        var original=PackageSparkline.Data;PackageSparkline.Data=new();
        await AwaitAsync(()=>plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Count()==0,"Packaged Sparkline retained an empty curve.");
        PackageSparkline.Data=original;await AwaitAsync(()=>plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Count()==1,"Packaged Sparkline did not restore consumer data.");
    }

    private async Task VerifyAreaChartAsync()
    {
        PackageAreaChart.Stacked = false;
        PackageAreaChart.Data[1] = new Dictionary<string, object?> { ["label"] = "B", ["v"] = 20d };
        var parent = (Panel)VisualTreeHelper.GetParent(PackageAreaChart); var position = parent.Children.IndexOf(PackageAreaChart);
        parent.Children.Remove(PackageAreaChart); await AwaitAsync(() => !PackageAreaChart.IsLoaded, "Packaged Area Chart did not unmount.");
        parent.Children.Insert(position, PackageAreaChart); await AwaitAsync(() => PackageAreaChart.IsLoaded, "Packaged Area Chart did not remount.");
        PackageAreaChart.ApplyTemplate(); Root.UpdateLayout();
        var chart = BrandDescendants<BleeckerLineChart>(PackageAreaChart).Single(); var plot = BrandDescendants<Canvas>(chart).Single(canvas => canvas.Name == "PART_Plot");
        Microsoft.UI.Xaml.Shapes.Path[] Paths() => plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().ToArray();
        await AwaitAsync(() => Paths().Length == 2, "Packaged Area Chart did not render its area and boundary.");
        var area = (PathGeometry)Paths()[0].Data; var first = area.Figures[0].StartPoint;
        // The native Opacity getter returns 0.2 rounded to float precision.
        Require(Math.Abs(first.X - 60) < .001 && Math.Abs(first.Y - 87.5) < .001 && area.Figures[0].IsFilled && area.Figures[0].IsClosed && Paths()[0].Opacity == (double)(float).2 && Paths()[1].StrokeThickness == 2,
            $"Packaged Area Chart lost its source fill/boundary geometry: start=({first.X:R},{first.Y:R}), filled={area.Figures[0].IsFilled}, closed={area.Figures[0].IsClosed}, opacity={Paths()[0].Opacity:R}, stroke={Paths()[1].StrokeThickness:R}.");
        Require(ReferenceEquals(chart.Data, PackageAreaChart.Data) && ReferenceEquals(chart.Series, PackageAreaChart.Series) && !PackageAreaChart.IsTabStop && chart.IsTabStop && AutomationProperties.GetName(chart) == "Independent areas" && FrameworkElementAutomationPeer.CreatePeerForElement(chart).GetClassName() == nameof(BleeckerAreaChart), "Packaged Area Chart lost consumer identity or its native inspection boundary.");
        await AcquireConsumerInputAsync(); Require(chart.Focus(FocusState.Keyboard), "Could not focus packaged Area Chart.");
        await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "A;  : 10", "Packaged Area Chart did not inspect its first category.");
        var keys = new[] { new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = 0x27 } } }, new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = 0x27, Flags = 2 } } } };
        Require(SendInput(2, keys, Marshal.SizeOf<ConsumerInput>()) == 2, "Packaged Area Chart key did not enter native input.");
        await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "B;  : 20", "Packaged Area Chart did not inspect through native Right.");
        var second = new BleeckerChartSeries { Key = "v", Name = "Second" }; PackageAreaChart.Series.Add(second); PackageAreaChart.Stacked = true;
        await AwaitAsync(() => Paths().Length == 4 && ((PathGeometry)Paths()[3].Data).Figures[0].StartPoint.Y < ((PathGeometry)Paths()[1].Data).Figures[0].StartPoint.Y, "Packaged Area Chart did not accumulate live consumer series.");
        PackageAreaChart.Series.Remove(second);
        PackageAreaChart.Data[1] = new Dictionary<string, object?> { ["label"] = "B", ["v"] = 30d };
        try { await AwaitAsync(() => AutomationProperties.GetHelpText(chart) == "B;  : 30", "Packaged Area Chart ignored replacement records."); }
        catch (Exception error) { throw new InvalidOperationException(error.Message + " " + ChartObservation(chart), error); }
    }

    private static string ChartObservation(BleeckerLineChart chart)
    {
        object? Field(string name) => typeof(BleeckerLineChart).GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(chart);
        return System.Text.Json.JsonSerializer.Serialize(new { chart.IsLoaded, help = AutomationProperties.GetHelpText(chart), data = chart.Data, observedData = Field("observedData") });
    }

    private async Task VerifyBarChartAsync()
    {
        var lifecycle = new List<object>();
        object Snapshot(BleeckerLineChart item, string kind) {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var observed = typeof(BleeckerLineChart).GetField("observedData", flags)!.GetValue(item);
            return new { kind, item.IsLoaded, sameData = ReferenceEquals(item.Data, PackageBarChart.Data), observedNull = observed is null, observedSame = ReferenceEquals(observed, item.Data) };
        }
        void Trace(BleeckerLineChart item) {
            lifecycle.Add(Snapshot(item, "attach"));
            item.Loaded += (_, _) => lifecycle.Add(Snapshot(item, "loaded"));
            item.Unloaded += (_, _) => lifecycle.Add(Snapshot(item, "unloaded"));
            item.RegisterPropertyChangedCallback(BleeckerLineChart.DataProperty, (_, _) => lifecycle.Add(Snapshot(item, "data")));
            item.RegisterPropertyChangedCallback(BleeckerLineChart.SeriesProperty, (_, _) => lifecycle.Add(Snapshot(item, "series")));
        }
        var prior = BrandDescendants<BleeckerLineChart>(PackageBarChart).FirstOrDefault(); if (prior is not null) Trace(prior);
        PackageBarChart.Layout=BleeckerBarChartLayout.Horizontal;
        PackageBarChart.Data[1]=new Dictionary<string,object?>{{"label","B"},{"v",20d}};
        var parent=(Panel)VisualTreeHelper.GetParent(PackageBarChart);var position=parent.Children.IndexOf(PackageBarChart);
        parent.Children.Remove(PackageBarChart);await AwaitAsync(()=>!PackageBarChart.IsLoaded,"Packaged Bar Chart did not unmount.");
        parent.Children.Insert(position,PackageBarChart);await AwaitAsync(()=>PackageBarChart.IsLoaded,"Packaged Bar Chart did not remount.");
        PackageBarChart.ApplyTemplate();Root.UpdateLayout();
        var chart=BrandDescendants<BleeckerLineChart>(PackageBarChart).Single();var plot=BrandDescendants<Canvas>(chart).Single(canvas=>canvas.Name=="PART_Plot");
        if (!ReferenceEquals(chart, prior)) Trace(chart);
        var pointerTrace = new List<object>();
        chart.PointerMoved += (_, e) => { var p = e.GetCurrentPoint(chart).Position; pointerTrace.Add(new { kind = "move", p.X, p.Y }); };
        chart.PointerExited += (_, _) => pointerTrace.Add(new { kind = "exit" });
        await AwaitAsync(()=>plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Count()==2,"Packaged Bar Chart did not render its consumer records.");
        var first=((PathGeometry)plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().First().Data).Figures[0].StartPoint;
        Require(Math.Abs(first.X-72)<.001&&Math.Abs(first.Y-91.5)<.001&&ReferenceEquals(chart.Data,PackageBarChart.Data)&&ReferenceEquals(chart.Series,PackageBarChart.Series),"Packaged Bar Chart lost source geometry or original consumer collections.");
        Require(!PackageBarChart.IsTabStop&&chart.IsTabStop&&AutomationProperties.GetName(chart)=="Independent grouped bars"&&FrameworkElementAutomationPeer.CreatePeerForElement(chart).GetClassName()==nameof(BleeckerBarChart),"Packaged Bar Chart lost its single native inspection boundary.");
        await AcquireConsumerInputAsync();Require(chart.Focus(FocusState.Keyboard),"Could not focus packaged Bar Chart.");
        await AwaitAsync(()=>AutomationProperties.GetHelpText(chart)=="A;  : 10","Packaged Bar Chart did not inspect its first category.");
        var keys=new[]{new ConsumerInput{Type=1,Data=new ConsumerInputData{Keyboard=new ConsumerKeyboard{VirtualKey=0x27}}},new ConsumerInput{Type=1,Data=new ConsumerInputData{Keyboard=new ConsumerKeyboard{VirtualKey=0x27,Flags=2}}}};
        Require(SendInput(2,keys,Marshal.SizeOf<ConsumerInput>())==2,"Packaged Bar Chart key did not enter native input.");
        await AwaitAsync(()=>AutomationProperties.GetHelpText(chart)=="B;  : 20","Packaged Bar Chart did not inspect consumer values through native Right.");
        PackageBarChart.Layout=BleeckerBarChartLayout.Vertical;
        await AwaitAsync(()=>((PathGeometry)plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().First().Data).Figures[0].StartPoint.X==60,"Packaged Bar Chart ignored a live orientation change.");
        PackageBarChart.Data[1]=new Dictionary<string,object?>{{"label","B"},{"v",30d}};
        try { await AwaitAsync(()=>AutomationProperties.GetHelpText(chart)=="B;  : 30","Packaged Bar Chart ignored live replacement records."); }
        catch (Exception error)
        {
            var interaction = typeof(BleeckerLineChart).GetField("interaction", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(chart)!;
            var state = interaction.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).ToDictionary(field => field.Name, field => field.GetValue(interaction));
            GetCursorPos(out var pointer); var origin = chart.TransformToVisual(Root).TransformPoint(new Windows.Foundation.Point());
            object? Field(string name) => typeof(BleeckerLineChart).GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(chart);
            throw new InvalidOperationException(error.Message + " " + ChartObservation(chart) + " " + System.Text.Json.JsonSerializer.Serialize(new { help = AutomationProperties.GetHelpText(chart), focus = ReferenceEquals(FocusManager.GetFocusedElement(Root.XamlRoot), chart), foreground = GetForegroundWindow().ToInt64(), nativeX = pointer.X, nativeY = pointer.Y, origin.X, origin.Y, chart.ActualWidth, chart.ActualHeight, chart.IsLoaded, wrapperLoaded = PackageBarChart.IsLoaded, sameData = ReferenceEquals(chart.Data, PackageBarChart.Data), observedData = ReferenceEquals(Field("observedData"), chart.Data), observedNull = Field("observedData") is null, queued = Field("queued"), current = BrandDescendants<BleeckerLineChart>(PackageBarChart).Select(item => ReferenceEquals(item, chart)).ToArray(), data = chart.Data, ownerData = PackageBarChart.Data, state, pointerTrace, lifecycle }), error);
        }
    }

    [DllImport("user32.dll")] private static extern bool GetCursorPos(out ConsumerCursorPoint point);
    [StructLayout(LayoutKind.Sequential)] private struct ConsumerCursorPoint { public int X, Y; }

    private async Task VerifyStatCardAsync()
    {
        PackageStatCard.ApplyTemplate();Root.UpdateLayout();
        var title=BrandDescendants<TextBlock>(PackageStatCard).Single(text=>text.Name=="PART_Title");
        var trendValue=BrandDescendants<TextBlock>(PackageStatCard).Single(text=>text.Name=="PART_TrendValue");
        var description=BrandDescendants<TextBlock>(PackageStatCard).Single(text=>text.Name=="PART_Description");
        Require(title.Text=="INDEPENDENT STATISTIC"&&Math.Abs(PackageStatCard.ActualHeight-132.5)<.001&&!PackageStatCard.IsTabStop&&
            FrameworkElementAutomationPeer.CreatePeerForElement(PackageStatCard).GetAutomationControlType()==AutomationControlType.Group,"Packaged Stat Card lost typography, source layout or passive semantics.");
        var valueText=BrandDescendants<TextBlock>(PackageStatCard).Single(text=>text.Name=="PART_Value");
        foreach(var (text,sourceBaseline) in new[]{(title,37d),(valueText,76.5d),(trendValue,102.5d)})
            await AwaitAsync(()=>
            {
                var origin=PackageStatCard.TransformToVisual(Root.XamlRoot.Content).TransformPoint(new Windows.Foundation.Point()).Y;
                var actual=text.TransformToVisual(Root.XamlRoot.Content).TransformPoint(new Windows.Foundation.Point()).Y+text.BaselineOffset;
                return Math.Abs(actual-Math.Round(origin+sourceBaseline,MidpointRounding.AwayFromZero))<.001;
            },"Packaged Stat Card lost its source text baseline.");
        var trend=PackageStatCard.Trend!;trend.Value="-2%";trend.Direction=BleeckerStatTrendDirection.Down;
        await AwaitAsync(()=>trendValue.Text=="-2%","Packaged Stat Card ignored the consumer trend.");
        PackageStatCard.Trend=null;Root.UpdateLayout();Require(description.Visibility==Visibility.Visible,"Packaged Stat Card did not restore its consumer description.");
        PackageStatCard.Trend=trend;
        var data=new System.Collections.ObjectModel.ObservableCollection<IReadOnlyDictionary<string,object?>>(new[]{10d,30d,20d,40d}.Select(value=>(IReadOnlyDictionary<string,object?>)new Dictionary<string,object?>{{"v",value}}));
        PackageStatCard.SparklineData=data;PackageStatCard.SparklineDataKey="v";
        await AwaitAsync(()=>BrandDescendants<BleeckerSparkline>(PackageStatCard).Any(),"Packaged Stat Card did not mount consumer history.");
        var spark=BrandDescendants<BleeckerSparkline>(PackageStatCard).Single();Require(ReferenceEquals(spark.Data,data)&&spark.Height==48&&spark.ShowArea,"Packaged Stat Card replaced consumer history or changed its compact profile.");
        PackageStatCard.SparklineDataKey="";await AwaitAsync(()=>!BrandDescendants<BleeckerSparkline>(PackageStatCard).Any(),"Packaged Stat Card retained empty-key history.");
        PackageStatCard.SparklineData=null;
        PackageStatCard.Footer=0d;Root.UpdateLayout();
        Require(BrandDescendants<TextBlock>(PackageStatCard).Single(text=>text.Name=="PART_FooterScalar").Text=="0"&&
            BrandDescendants<Border>(PackageStatCard).Single(border=>border.Name=="PART_Footer").Visibility==Visibility.Collapsed&&Math.Abs(PackageStatCard.ActualHeight-176.5)<.001,"Packaged Stat Card changed zero footer content or its source allocation.");
        PackageStatCard.Footer=true;Root.UpdateLayout();
        Require(BrandDescendants<TextBlock>(PackageStatCard).Single(text=>text.Name=="PART_FooterText").Text==""&&Math.Abs(PackageStatCard.ActualHeight-169.5)<.001,"Packaged Stat Card rendered Boolean footer text.");
        PackageStatCard.Footer="Consumer footer wraps over several words.";Root.UpdateLayout();
        Require(BrandDescendants<TextBlock>(PackageStatCard).Single(text=>text.Name=="PART_FooterText").LineHeight==24,"Packaged Stat Card lost scalar footer typography.");
        PackageStatCard.Icon=0d;Root.UpdateLayout();Require(BrandDescendants<TextBlock>(PackageStatCard).Single(text=>text.Name=="PART_IconScalar").Text=="0"&&BrandDescendants<Border>(PackageStatCard).Single(border=>border.Name=="PART_Icon").Visibility==Visibility.Collapsed,"Packaged Stat Card put zero into an icon box.");
        PackageStatCard.FooterTemplate=(DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><TextBlock Text=\"Independent footer template\" Tag=\"{Binding}\" Height=\"24\" /></DataTemplate>");PackageStatCard.Footer=false;
        await AwaitAsync(()=>BrandDescendants<TextBlock>(PackageStatCard).Any(text=>text.Text=="Independent footer template"&&Equals(text.Tag,false)),"Packaged Stat Card discarded template-owned false data.");
        PackageStatCard.Footer=null;PackageStatCard.FooterTemplate=null;PackageStatCard.Icon=null;
    }

    private async Task VerifySidebarAsync()
    {
        PackageSidebar.ApplyTemplate();Root.UpdateLayout();
        Microsoft.UI.Xaml.Controls.Primitives.ButtonBase[] Rows()=>BrandDescendants<Microsoft.UI.Xaml.Controls.Primitives.ButtonBase>(PackageSidebar).Where(action=>action.Tag is BleeckerSidebarItem).ToArray();
        await AwaitAsync(()=>Rows().Length==4,"Packaged Sidebar did not render its initially active child.");
        var first=(HyperlinkButton)Rows()[0];var item=PackageSidebar.Items[0];BleeckerSidebarItem? invoked=null;var calls=0;
        void Invoked(object? sender,BleeckerSidebarItem value){invoked=value;calls++;}
        PackageSidebar.ItemInvoked+=Invoked;
        try
        {
            ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(first).GetPattern(PatternInterface.Invoke)).Invoke();
            await AwaitAsync(()=>calls==1,"Packaged Sidebar did not invoke the consumer.");
            Require(ReferenceEquals(invoked,item)&&item.Href=="opaque:consumer/route?x=1#part"&&first.NavigateUri is null,"Packaged Sidebar cloned the original item or interpreted its opaque route.");
            var group=Rows().Single(row=>row is not HyperlinkButton);var provider=(IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(group).GetPattern(PatternInterface.ExpandCollapse);
            Require(provider.ExpandCollapseState==Microsoft.UI.Xaml.Automation.ExpandCollapseState.Expanded&&group.IsEnabled,"Packaged group lost source disabled/active-child semantics.");
            provider.Collapse();await AwaitAsync(()=>Rows().Length==3,"Packaged group did not collapse.");provider.Expand();await AwaitAsync(()=>Rows().Length==4,"Packaged group did not expand.");
            Require(calls==1,"Packaged group invoked a navigation callback.");
            var disabled=Rows().Last();Require(disabled.IsEnabled&&!disabled.IsHitTestVisible&&disabled.Opacity==.5,"Packaged CSS-disabled link changed its keyboard/pointer boundary.");
            PackageSidebar.Collapsed=true;Root.UpdateLayout();await AwaitAsync(()=>Rows().Length==2&&Math.Abs(PackageSidebar.ActualWidth-64)<.001,"Packaged collapsed Sidebar retained non-route groups or the expanded width.");
            PackageSidebar.Collapsed=false;Root.UpdateLayout();await AwaitAsync(()=>Rows().Length==4,"Packaged Sidebar lost expansion across collapse/expand.");
            PackageSidebar.LinkTemplate=(DataTemplate)Root.Resources["ConsumerSidebarLink"];Root.UpdateLayout();
            await AwaitAsync(()=>Rows().Length==4,"Packaged consumer template did not render its original contexts.");
            var custom=(HyperlinkButton)Rows()[0];((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(custom).GetPattern(PatternInterface.Invoke)).Invoke();
            await AwaitAsync(()=>calls==2,"Packaged custom link lost its consumer activation.");
            Require(ReferenceEquals(custom.Tag,item)&&ReferenceEquals(invoked,item),"Packaged link template lost original-item identity.");
            PackageSidebar.LinkTemplate=null;Root.UpdateLayout();
        }
        finally{PackageSidebar.ItemInvoked-=Invoked;}
    }

    private void VerifyDashboardGrid()
    {
        Root.UpdateLayout();
        Require(Root.XamlRoot.Size.Width>=640,"Packaged Dashboard Grid requires the documented desktop viewport.");
        var first=(BleeckerDashboardSpan)PackageDashboardGrid.Children[0];var second=(BleeckerDashboardSpan)PackageDashboardGrid.Children[1];var full=(BleeckerDashboardSpan)PackageDashboardGrid.Children[2];
        Require(Math.Abs(first.ActualWidth-288)<.001&&Math.Abs(second.TransformToVisual(PackageDashboardGrid).TransformPoint(new Windows.Foundation.Point()).X-312)<.001&&
            Math.Abs(full.ActualWidth-600)<.001&&Math.Abs(PackageDashboardGrid.ActualHeight-112)<.001&&!first.IsTabStop&&!PackageDashboardGrid.IsTabStop,
            "Packaged Dashboard Grid lost its responsive tracks, span or passive semantics.");
        var content=first.Content;first.Span=BleeckerDashboardSpanSize.Full;Root.UpdateLayout();
        Require(Math.Abs(first.ActualWidth-600)<.001&&ReferenceEquals(first.Content,content),"Packaged Dashboard Grid lost live span changes or child identity.");
        first.Span=BleeckerDashboardSpanSize.One;Root.UpdateLayout();
    }

    private void VerifyDashboardSection()
    {
        PackageDashboardSection.ApplyTemplate(); Root.UpdateLayout();
        var root=(FrameworkElement)VisualTreeHelper.GetChild(PackageDashboardSection,0);
        var title=(TextBlock)root.FindName("PART_Title");
        var description=(TextBlock)root.FindName("PART_Description");
        Require(title.FontFamily.Source.Contains("EncodeSans-Medium.ttf") && title.FontSize==18 && title.LineHeight==28 &&
            description.FontFamily.Source.Contains("LibreFranklin-Regular.ttf") && description.LineHeight==20 &&
            FrameworkElementAutomationPeer.CreatePeerForElement(title).GetHeadingLevel()==AutomationHeadingLevel.Level2 &&
            !PackageDashboardSection.IsTabStop,"Packaged Dashboard Section lost typography or heading semantics.");
        var content=PackageDashboardSection.Content; var action=PackageDashboardSection.Action;
        PackageDashboardSection.Title=null; PackageDashboardSection.Description=null; PackageDashboardSection.Action=null;Root.UpdateLayout();
        Require(PackageDashboardSection.ActualHeight==64,"Packaged Dashboard Section retained an empty header gap.");
        PackageDashboardSection.Content=null;Root.UpdateLayout();Require(PackageDashboardSection.ActualHeight==0,"Packaged Dashboard Section did not become empty.");
        PackageDashboardSection.Title="Updated independent dashboard";PackageDashboardSection.Description="Consumer-owned dashboard summary";
        PackageDashboardSection.Content=content;PackageDashboardSection.Action=action;Root.UpdateLayout();
        Require(title.Text==PackageDashboardSection.Title&&ReferenceEquals(((ContentPresenter)root.FindName("PART_Content")).Content,content),
            "Packaged Dashboard Section lost live content.");
        PackageDashboardSection.Title="  Monitoring\t queue\r\n";PackageDashboardSection.Description="  Consumer-owned\t section\nsummary. ";Root.UpdateLayout();
        Require(title.Text=="Monitoring queue"&&description.Text=="Consumer-owned section summary."&&PackageDashboardSection.Title=="  Monitoring\t queue\r\n",
            "Packaged Dashboard Section did not collapse display whitespace while preserving consumer values.");
        var originalWidth=PackageDashboardSection.Width;
        PackageDashboardSection.Title="Monitoring";PackageDashboardSection.Description="Consumer-owned section summary.";PackageDashboardSection.Width=280;Root.UpdateLayout();
        Require(Math.Abs(PackageDashboardSection.ActualHeight-156)<.001&&Math.Abs(((FrameworkElement)action!).TransformToVisual(PackageDashboardSection).TransformPoint(new Windows.Foundation.Point()).X-200)<.001,
            "Packaged Dashboard Section shrank its fixed action or misplaced constrained content.");
        PackageDashboardSection.Width=originalWidth;Root.UpdateLayout();
    }

    private void VerifySectionHeader()
    {
        PackageSectionHeader.ApplyTemplate(); Root.UpdateLayout();
        var surface = (FrameworkElement)VisualTreeHelper.GetChild(PackageSectionHeader, 0);
        var title = (TextBlock)surface.FindName("PART_Title"); var description = (TextBlock)surface.FindName("PART_Description");
        var eyebrow = (BleeckerEyebrow)surface.FindName("PART_Eyebrow");
        Require(title.Text == "Independent section" && title.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf") && title.FontSize == 28 && title.LineHeight == 35 &&
            FrameworkElementAutomationPeer.CreatePeerForElement(title).GetHeadingLevel() == AutomationHeadingLevel.Level2 &&
            description.FontFamily.Source.Contains("LibreFranklin-Regular.ttf") && description.LineHeight == 28 && eyebrow.ActualHeight == 16 &&
            !PackageSectionHeader.IsTabStop, "Packaged Section Header lost its fonts, layout or heading semantics.");
        Require(Math.Abs(description.TransformToVisual(PackageSectionHeader).TransformPoint(new Windows.Foundation.Point()).Y + description.BaselineOffset - 96) < .001,
            "Packaged Section Header summary lost its source baseline.");
        PackageSectionHeader.Eyebrow = ""; PackageSectionHeader.Description = ""; Root.UpdateLayout();
        Require(eyebrow.Visibility == Visibility.Collapsed && description.Visibility == Visibility.Collapsed && PackageSectionHeader.ActualHeight == 35,
            "Packaged Section Header retained removed content gaps.");
        PackageSectionHeader.Title = "Updated independent section"; Root.UpdateLayout();
        Require(title.Text == PackageSectionHeader.Title, "Packaged Section Header did not render consumer changes.");
        PackageSectionHeader.Title = "Independent section"; PackageSectionHeader.Eyebrow = "Workspace"; PackageSectionHeader.Description = "Consumer-owned section summary";
    }

    private async Task VerifyPageHeaderAsync()
    {
        var command = new ConsumerRetryCommand(); var parameter = new object(); var notified = 0;
        var breadcrumb = new TextBlock { Text = "Independent / Editorial" };
        var actions = new System.Collections.ObjectModel.ObservableCollection<UIElement> { new BleeckerButton { Content = "Save", Size = BleeckerButtonSize.Small } };
        PackagePageHeader.BackCommand = command; PackagePageHeader.BackCommandParameter = parameter;
        PackagePageHeader.Breadcrumbs = breadcrumb; PackagePageHeader.Actions = actions;
        RoutedEventHandler handler = (_, _) => notified++;
        PackagePageHeader.BackRequested += handler;
        try
        {
            PackagePageHeader.ApplyTemplate(); Root.UpdateLayout();
            var root = (FrameworkElement)VisualTreeHelper.GetChild(PackagePageHeader, 0);
            var back = (Button)((Grid)root.FindName("PART_Back")).Children[0];
            var title = (TextBlock)root.FindName("PART_Title");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(back);
            Require(peer.GetAutomationControlType() == AutomationControlType.Hyperlink && peer.GetName() == "Back" &&
                AutomationProperties.GetHeadingLevel(title) == AutomationHeadingLevel.Level1 &&
                PackagePageHeader.BackHref == "#consumer-editorial" && ReferenceEquals(((ContentPresenter)root.FindName("PART_Breadcrumbs")).Content, breadcrumb),
                "Packaged Page Header lost heading, route, content or Back semantics.");
            ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();
            await AwaitAsync(() => command.Executions == 1 && notified == 1, "Packaged Back did not deliver consumer invocation.");
            Require(ReferenceEquals(command.Parameter, parameter), "Packaged Back changed its parameter.");
            command.Allowed = false; command.RaiseChanged();
            await AwaitAsync(() => !back.IsEnabled, "Packaged Back ignored CanExecute.");
            command.Allowed = true; command.RaiseChanged();
            await AwaitAsync(() => back.IsEnabled, "Packaged Back failed to restore CanExecute.");
            actions.Add(new BleeckerButton { Content = "Preview", Size = BleeckerButtonSize.Small }); Root.UpdateLayout();
            Require(((Panel)((Grid)root.FindName("PART_Actions")).Children[0]).Children.Count == 2, "Packaged live action collection did not render.");
            PackagePageHeader.Description = ""; PackagePageHeader.BackCommand = null; PackagePageHeader.BackHref = null; Root.UpdateLayout();
            Require(back.Visibility == Visibility.Collapsed && ((TextBlock)root.FindName("PART_Description")).Visibility == Visibility.Collapsed,
                "Packaged Page Header retained removed content or Back action.");
        }
        finally
        {
            PackagePageHeader.BackRequested -= handler; PackagePageHeader.BackHref = "#consumer-editorial";
            PackagePageHeader.Description = "Consumer-owned page summary";
        }
    }

    private async Task VerifyErrorStateAsync()
    {
        var command = new ConsumerRetryCommand(); var parameter = new object();
        PackageErrorState.RetryCommand = command; PackageErrorState.RetryCommandParameter = parameter;
        var icon = new TextBlock { Text = "!", FontSize = 20 };
        PackageErrorState.Icon = icon; PackageErrorState.ApplyTemplate(); Root.UpdateLayout();
        var surface = (Grid)VisualTreeHelper.GetChild(PackageErrorState, 0);
        var retry = (BleeckerButton)surface.FindName("PART_Retry"); retry.ApplyTemplate();
        var native = (Button)((FrameworkElement)VisualTreeHelper.GetChild(retry, 0)).FindName("PART_Action");
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(PackageErrorState);
        Require(peer.GetAutomationControlType() == AutomationControlType.Group && peer.GetName() == PackageErrorState.Title &&
            peer.GetHelpText() == PackageErrorState.Description && !peer.IsKeyboardFocusable() &&
            ReferenceEquals(((ContentPresenter)surface.FindName("PART_Icon")).Content, icon),
            "Packaged Error State lost its summary semantics or consumer icon.");
        Require(retry.Visibility == Visibility.Visible && retry.Size == BleeckerButtonSize.Small && retry.Variant == BleeckerButtonVariant.Secondary &&
            new ButtonAutomationPeer(native).GetName() == "Retry archive", "Packaged Error State lost its native retry action.");
        ((IInvokeProvider)new ButtonAutomationPeer(native).GetPattern(PatternInterface.Invoke)).Invoke();
        await AwaitAsync(() => command.Executions == 1, "Packaged retry did not reach the consumer command.");
        Require(ReferenceEquals(command.Parameter, parameter), "Packaged retry changed the consumer parameter.");
        command.Allowed = false; command.RaiseChanged();
        await AwaitAsync(() => !native.IsEnabled, "Packaged retry did not respect CanExecute.");
        command.Allowed = true; command.RaiseChanged();
        await AwaitAsync(() => native.IsEnabled, "Packaged retry did not restore CanExecute.");
        PackageErrorState.RetryCommand = null; PackageErrorState.Description = ""; PackageErrorState.Icon = null;
        Root.UpdateLayout();
        Require(retry.Visibility == Visibility.Collapsed && ((TextBlock)surface.FindName("PART_Description")).Visibility == Visibility.Collapsed &&
            ((Viewbox)surface.FindName("PART_DefaultIcon")).Visibility == Visibility.Visible,
            "Packaged Error State did not remove retry/description or restore its default icon.");
        PackageErrorState.Description = "Consumer-owned failure";
    }
    private sealed class ConsumerRetryCommand : System.Windows.Input.ICommand
    {
        public bool Allowed { get; set; } = true;
        public int Executions { get; private set; }
        public object? Parameter { get; private set; }
        public bool CanExecute(object? parameter) => Allowed;
        public void Execute(object? parameter) { if (!Allowed) throw new InvalidOperationException("Rejected retry executed."); Executions++; Parameter=parameter; }
        public event EventHandler? CanExecuteChanged;
        public void RaiseChanged() => CanExecuteChanged?.Invoke(this,EventArgs.Empty);
    }
    private async Task AwaitAsync(Func<bool> condition, string message)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && elapsed.ElapsedMilliseconds < 2000) { Root.UpdateLayout(); await Task.Delay(16); }
        Require(condition(), message);
    }
    private async Task VerifyAttentionScrollAsync()
    {
        var surface = new BleeckerAttentionSurface { Width = 320, Element = BleeckerAttentionElement.Button,
            Density = BleeckerAttentionDensity.Compact, Hue = 48, Intensity = 8, Content = "Contrast surface" };
        var content = new StackPanel(); content.Children.Add(new Border { Height = 40 });
        content.Children.Add(surface); content.Children.Add(new Border { Height = 300 });
        var viewport = new ScrollViewer { Width = 360, Height = 180, Content = content, Margin = new Thickness(32),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Root.Children.Add(viewport);
        try
        {
            var mounted = System.Diagnostics.Stopwatch.StartNew();
            while (!surface.IsLoaded && mounted.Elapsed < TimeSpan.FromSeconds(2)) { Root.UpdateLayout(); await Task.Delay(16); }
            Require(surface.IsLoaded, "Independent Attention viewport did not mount its consumer surface.");
            surface.ApplyTemplate(); Root.UpdateLayout();
            var text = (TextBlock)((FrameworkElement)VisualTreeHelper.GetChild(surface, 0)).FindName("PART_String");
            foreach (var offset in new[] { .5, 37, .5 })
            {
                var paint = new SolidColorBrush(offset == 37 ? Microsoft.UI.Colors.White : Windows.UI.Color.FromArgb(255, 240, 244, 248));
                surface.Foreground = paint; viewport.ChangeView(null, offset, null, true);
                var watch = System.Diagnostics.Stopwatch.StartNew();
                bool Matches()
                {
                    var origin = text.TransformToVisual(Root).TransformPoint(new Windows.Foundation.Point()).Y * Root.XamlRoot.RasterizationScale;
                    return Math.Abs(viewport.VerticalOffset-offset) < .001 && text.ActualHeight == 24 &&
                        ReferenceEquals(text.Foreground, paint) && Math.Abs(origin-Math.Round(origin)) < .001;
                }
                while (!Matches() && watch.Elapsed < TimeSpan.FromSeconds(2)) { Root.UpdateLayout(); await Task.Delay(16); }
                Require(Matches(), "Packaged Attention text lost its device-grid origin or consumer paint during fractional scrolling.");
            }
        }
        finally { Root.Children.Remove(viewport); }
    }

    private async Task VerifyLineChartAsync()
    {
        var series=new BleeckerChartSeries{Key="consumer.value",Name="Consumer total"};
        var chart=new BleeckerLineChart{Width=640,XAxisKey="category",HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(32)};
        AutomationProperties.SetName(chart,"Independent package chart");
        chart.Series.Add(series);
        chart.Data.Add(new Dictionary<string,object?>{{"category","Harbor"},{"consumer.value",100d}});
        chart.Data.Add(new Dictionary<string,object?>{{"category","Reed"},{"consumer.value",220d}});
        chart.Data.Add(new Dictionary<string,object?>{{"category","North"},{"consumer.value",310d}});
        Root.Children.Add(chart);
        try
        {
            chart.ApplyTemplate();Root.UpdateLayout();
            var template=(FrameworkElement)VisualTreeHelper.GetChild(chart,0);var plot=(Canvas)template.FindName("PART_Plot");var legend=(Canvas)template.FindName("PART_Legend");
            await AwaitAsync(()=>plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Count()==1,"Packaged chart did not render independent consumer records.");
            var path=plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Single();var geometry=(PathGeometry)path.Data;
            Require(chart.ActualHeight==300&&geometry.Figures.Single().Segments.Count==2&&geometry.Figures.Single().Segments.All(segment=>segment is BezierSegment)&&
                Math.Abs(geometry.Figures.Single().StartPoint.X-60)<.001&&Math.Abs(geometry.Figures.Single().StartPoint.Y-157.8125)<.001,
                "Packaged chart lost its template, source scale or exact literal data key.");
            var peer=FrameworkElementAutomationPeer.CreatePeerForElement(chart);
            await AcquireConsumerInputAsync();
            Require(peer.GetName()=="Independent package chart"&&chart.IsTabStop&&chart.Focus(FocusState.Keyboard),"Packaged chart lost its consumer accessible name or keyboard entry.");
            var tooltip=(Border)template.FindName("PART_Tooltip");var tooltipItems=(StackPanel)template.FindName("PART_TooltipItems");var tooltipContainer=(Grid)template.FindName("PART_TooltipContainer");
            await AwaitAsync(()=>tooltip.Visibility==Visibility.Visible&&tooltipItems.Children.OfType<TextBlock>().FirstOrDefault()?.Text=="Harbor","Packaged initial focus did not inspect the first category.");
            Require(GetForegroundWindow()==WinRT.Interop.WindowNative.GetWindowHandle(this),"Independent tooltip input lost its own foreground window.");
            Key(0x27);
            await AwaitAsync(()=>tooltip.Visibility==Visibility.Visible&&tooltip.ActualHeight==78&&tooltipItems.Children.OfType<TextBlock>().First().Text=="Reed","Packaged tooltip lost source content or row dimensions.");
            Require(Math.Abs(tooltipContainer.Margin.Top-67)<.001,"Packaged tooltip lost its keyboard coordinate and vertical placement.");
            var shadow=(ContainerVisual)ElementCompositionPreview.GetElementChildVisual((Grid)template.FindName("PART_TooltipShadow"));
            Require(shadow.Children.Single() is SpriteVisual shadowSprite&&shadowSprite.Shadow is DropShadow drop&&drop.BlurRadius==32&&drop.Mask is not null&&shadow.Clip is CompositionGeometricClip&&ElementCompositionPreview.GetElementChildVisual((Grid)template.FindName("PART_TooltipBackdrop")) is SpriteVisual{Brush:CompositionEffectBrush},"Packaged tooltip lost its native clipped shadow/backdrop graph.");
            Key(0x0D);
            await AwaitAsync(()=>tooltip.Visibility==Visibility.Collapsed&&chart.FocusState==FocusState.Keyboard,"Packaged Enter did not hide inspection while retaining focus.");
            Key(0x0D);
            await AwaitAsync(()=>tooltip.Visibility==Visibility.Visible&&tooltipItems.Children.OfType<TextBlock>().First().Text=="Reed","Packaged Enter did not restore its retained category.");
            var exit=new Button{Content="Consumer focus exit",HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top};
            Root.Children.Add(exit);
            try
            {
                Require(exit.Focus(FocusState.Keyboard),"Independent consumer could not leave chart focus.");
                await AwaitAsync(()=>tooltip.Visibility==Visibility.Collapsed,"Packaged chart did not hide inspection on blur.");
                Require(chart.Focus(FocusState.Keyboard),"Independent consumer could not return to chart focus.");
                Require(tooltip.Visibility==Visibility.Collapsed,"Packaged refocus reopened retained inspection without Enter.");
                Key(0x0D);
                await AwaitAsync(()=>tooltip.Visibility==Visibility.Visible&&tooltipItems.Children.OfType<TextBlock>().First().Text=="Reed","Packaged chart lost retained selection across blur/refocus.");
            }
            finally{Root.Children.Remove(exit);}
            series.Name="Changed packaged total";
            await AwaitAsync(()=>legend.Children.OfType<StackPanel>().Single().Children.OfType<TextBlock>().Single().Text==series.Name,"Packaged chart did not observe series changes.");
            var transparent=new SolidColorBrush(Microsoft.UI.Colors.Transparent);series.Color=transparent;
            await AwaitAsync(()=>ReferenceEquals(plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Single().Stroke,transparent),"Packaged chart did not preserve explicit transparent paint.");
            chart.Data[0]=new Dictionary<string,object?>{{"category","Changed category"},{"consumer.value",0d}};
            await AwaitAsync(()=>Math.Abs(((PathGeometry)plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Single().Data).Figures.Single().StartPoint.Y-225)<.001,"Packaged chart did not observe record replacement.");
            chart.ShowGrid=false;chart.ShowLegend=false;
            await AwaitAsync(()=>!plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Line>().Any()&&legend.Children.Count==0,"Packaged chart did not apply consumer grid/legend visibility.");
            chart.Data.Clear();await AwaitAsync(()=>!plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Any(),"Packaged empty chart retained live line artifacts.");
            Require(ElementCompositionPreview.GetElementChildVisual((Grid)template.FindName("PART_TooltipShadow")) is null&&ElementCompositionPreview.GetElementChildVisual((Grid)template.FindName("PART_TooltipBackdrop")) is null,"Packaged empty tooltip retained composition resources.");
        }
        finally{Root.Children.Remove(chart);}
        async Task AwaitAsync(Func<bool> condition,string message)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();while(!condition()&&watch.Elapsed<TimeSpan.FromSeconds(2)){Root.UpdateLayout();await Task.Delay(16);}
            Require(condition(),message);
        }
        void Key(ushort key)
        {
            Require(GetForegroundWindow()==WinRT.Interop.WindowNative.GetWindowHandle(this),"Independent tooltip input lost its own foreground window.");
            var keys=new[]{new ConsumerInput{Type=1,Data=new ConsumerInputData{Keyboard=new ConsumerKeyboard{VirtualKey=key}}},new ConsumerInput{Type=1,Data=new ConsumerInputData{Keyboard=new ConsumerKeyboard{VirtualKey=key,Flags=2}}}};
            Require(SendInput(2,keys,Marshal.SizeOf<ConsumerInput>())==2,"Independent tooltip key did not enter the normal native input path.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]private struct ConsumerInput{internal uint Type;internal ConsumerInputData Data;}
    [StructLayout(LayoutKind.Explicit)]private struct ConsumerInputData{[FieldOffset(0)]internal ConsumerKeyboard Keyboard;[FieldOffset(0)]internal ConsumerMouse Mouse;}
    [StructLayout(LayoutKind.Sequential)]private struct ConsumerKeyboard{internal ushort VirtualKey,ScanCode;internal uint Flags,Time;internal UIntPtr ExtraInfo;}
    [StructLayout(LayoutKind.Sequential)]private struct ConsumerMouse{internal int X,Y;internal uint Data,Flags,Time;internal UIntPtr ExtraInfo;}
    [DllImport("user32.dll")]private static extern uint SendInput(uint count,[In]ConsumerInput[] inputs,int size);
    [DllImport("user32.dll")]private static extern IntPtr GetForegroundWindow();

    private async Task AcquireConsumerInputAsync()
    {
        var own = WinRT.Interop.WindowNative.GetWindowHandle(this);
        GetWindowThreadProcessId(own, out var process);
        Require(own != IntPtr.Zero && process == Environment.ProcessId && IsWindowVisible(own),
            "Independent input requires its own visible native window.");
        var before = DescribeWindow(GetForegroundWindow());
        var watch = System.Diagnostics.Stopwatch.StartNew();
        Activate();
        var requested = SetForegroundWindow(own);
        var activationKeySent = false;
        if (GetForegroundWindow() != own)
        {
            // Use the catalog's existing transient-Alt foreground preparation.
            // Physical component input still requires the actual owned foreground.
            var keys = new[] { new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = 0x12 } } },
                new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = 0x12, Flags = 2 } } } };
            Require(SendInput(2, keys, Marshal.SizeOf<ConsumerInput>()) == 2, "Windows rejected independent foreground preparation.");
            activationKeySent = true; await Task.Delay(16); requested = SetForegroundWindow(own);
        }
        while (GetForegroundWindow() != own && watch.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(16);
        var args = Environment.GetCommandLineArgs(); var index = Array.IndexOf(args, "--verify-output");
        if (index >= 0)
            await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(args[index+1])!, $"consumer-input-{Root.RequestedTheme}.json"),
                System.Text.Json.JsonSerializer.Serialize(new { requested, activationKeySent, milliseconds = watch.Elapsed.TotalMilliseconds,
                    own = DescribeWindow(own), before, after = DescribeWindow(GetForegroundWindow()), acquired = GetForegroundWindow() == own }));
        Require(GetForegroundWindow() == own, "Independent consumer did not acquire its own foreground window for native input.");
        static object DescribeWindow(IntPtr handle)
        {
            GetWindowThreadProcessId(handle, out var processId);
            var className = new System.Text.StringBuilder(256); GetClassName(handle, className, className.Capacity);
            return new { handle = handle.ToInt64(), processId, windowClass = className.ToString(), visible = IsWindowVisible(handle) };
        }
    }
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(IntPtr window,out uint processId);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetClassName(IntPtr window,System.Text.StringBuilder className,int maxCount);

    private async Task VerifyChartThemeAsync(ElementTheme originalTheme)
    {
        var series=new BleeckerChartSeries{Key="consumer.key[0]",Name="東京 · consumer series",Color=new SolidColorBrush(Windows.UI.Color.FromArgb(255,25,115,145))};
        var seriesPaint=new Microsoft.UI.Xaml.Shapes.Rectangle{Width=32,Height=24};
        seriesPaint.SetBinding(Microsoft.UI.Xaml.Shapes.Shape.FillProperty,new Microsoft.UI.Xaml.Data.Binding{Source=series,Path=new PropertyPath(nameof(BleeckerChartSeries.Color)),Mode=Microsoft.UI.Xaml.Data.BindingMode.OneWay});
        var seriesLabel=new TextBlock();
        seriesLabel.SetBinding(TextBlock.TextProperty,new Microsoft.UI.Xaml.Data.Binding{Source=series,Path=new PropertyPath(nameof(BleeckerChartSeries.Name)),Mode=Microsoft.UI.Xaml.Data.BindingMode.OneWay});
        var host=new Grid{HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(32)};
        var brandScope=new StackPanel{Orientation=Orientation.Horizontal,Spacing=4};
        var regular=new StackPanel{Orientation=Orientation.Horizontal,Spacing=4};
        host.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});host.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        Grid.SetRow(brandScope,1);host.Children.Add(regular);host.Children.Add(brandScope);
        host.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        var seriesRow=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};seriesRow.Children.Add(seriesPaint);seriesRow.Children.Add(seriesLabel);
        Grid.SetRow(seriesRow,2);host.Children.Add(seriesRow);
        var brand=new SolidColorBrush(Windows.UI.Color.FromArgb(255,25,115,145));brandScope.Resources["BleeckerSeaBrush"]=brand;
        var regularSwatches=new List<Microsoft.UI.Xaml.Shapes.Rectangle>();var brandSwatches=new List<Microsoft.UI.Xaml.Shapes.Rectangle>();
        foreach(var key in BleeckerChartTheme.Palette)
        {
            foreach(var pair in new[]{(Parent:regular,Swatches:regularSwatches),(Parent:brandScope,Swatches:brandSwatches)})
            {
                var swatch=(Microsoft.UI.Xaml.Shapes.Rectangle)Microsoft.UI.Xaml.Markup.XamlReader.Load(
                    $"<Rectangle xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Width=\"32\" Height=\"24\" Fill=\"{{ThemeResource {key}}}\" />");
                pair.Parent.Children.Add(swatch);pair.Swatches.Add(swatch);
            }
        }
        Root.Children.Add(host);
        try
        {
            foreach(var theme in new[]{originalTheme,originalTheme==ElementTheme.Light?ElementTheme.Dark:ElementTheme.Light,originalTheme})
            {
                Root.RequestedTheme=theme;Root.UpdateLayout();
                var watch=System.Diagnostics.Stopwatch.StartNew();
                var scheme=theme==ElementTheme.Light?BleeckerColorScheme.Light:BleeckerColorScheme.Dark;
                bool Matches()
                {
                    for(var index=0;index<BleeckerChartTheme.Palette.Count;index++)
                    {
                        var key=BleeckerChartTheme.Palette[index]["Bleecker".Length..^"Brush".Length];key=char.ToLowerInvariant(key[0])+key[1..];
                        if(Hex(regularSwatches[index].Fill)!=BleeckerTokens.Colors(scheme)[key] ||
                            Hex(brandSwatches[index].Fill)!=(index==0?Hex(brand):BleeckerTokens.Colors(scheme)[key]))return false;
                    }
                    return true;
                }
                while(!Matches()&&watch.Elapsed<TimeSpan.FromSeconds(2))await Task.Delay(16);
                Require(Matches(),"Packaged chart theme lost live appearance, palette order or scoped brand resources.");
            }
            Require(BleeckerChartTheme.Colors.Count==12&&BleeckerChartTheme.PickColor(10)==BleeckerChartTheme.Palette[0]&&
                BleeckerChartTheme.PickColor(-1) is null && BleeckerChartTheme.PickColor(.5) is null,
                "Packaged chart theme changed the public source contract.");
            BleeckerChartGridStyle grid=BleeckerChartTheme.Grid;BleeckerChartAxisStyle axis=BleeckerChartTheme.Axis;BleeckerChartTooltipStyle tooltip=BleeckerChartTheme.Tooltip;
            Require(grid.StrokeDashes.SequenceEqual(new[]{3d,3d})&&axis.TickFontSize==12&&tooltip.CornerRadius==12&&
                tooltip.ShadowBlurCssPixels==32&&tooltip.BackdropBlurCssPixels==8&&tooltip.LabelFontWeight==600,
                "Packaged chart style metadata is incomplete.");
            brand.Color=Windows.UI.Color.FromArgb(255,145,55,95);
            Require(ReferenceEquals(brandSwatches[0].Fill,brand)&&Hex(regularSwatches[0].Fill)!=Hex(brand),
                "Packaged chart brand brush mutation lost ownership or escaped its scope.");
            var sourceBrush=series.Color;
            Require(ReferenceEquals(seriesPaint.Fill,sourceBrush)&&seriesLabel.Text==series.Name&&series.Key=="consumer.key[0]",
                "Packaged chart series lost consumer strings or native brush binding.");
            series.Name="Updated · العربية";
            var seriesWatch=System.Diagnostics.Stopwatch.StartNew();
            while(seriesLabel.Text!=series.Name&&seriesWatch.Elapsed<TimeSpan.FromSeconds(2))await Task.Delay(16);
            Require(seriesLabel.Text==series.Name,"Packaged chart series name mutation did not reach its mounted consumer.");
            var transparent=new SolidColorBrush(Microsoft.UI.Colors.Transparent);series.Color=transparent;
            seriesWatch.Restart();while(!ReferenceEquals(seriesPaint.Fill,transparent)&&seriesWatch.Elapsed<TimeSpan.FromSeconds(2))await Task.Delay(16);
            Require(ReferenceEquals(seriesPaint.Fill,transparent),"Packaged chart series replaced explicit transparent paint.");
            series.Color=null;seriesWatch.Restart();while(seriesPaint.Fill is not null&&seriesWatch.Elapsed<TimeSpan.FromSeconds(2))await Task.Delay(16);
            Require(seriesPaint.Fill is null,"Packaged chart series did not expose the absent-color state to its consumer.");
            series.Key="";series.Name="";
            Require(series.Key==""&&series.Name=="","Packaged chart series rewrote explicit empty strings.");
            var missing=new BleeckerChartSeries();var rejected=false;
            try{_=missing.Key;}catch(InvalidOperationException){rejected=true;}
            Require(rejected,"Packaged chart series silently supplied a missing required key.");
        }
        finally{Root.Children.Remove(host);Root.RequestedTheme=originalTheme;}
        static string? Hex(Brush? brush)
        {
            if(brush is not SolidColorBrush solid)return null;
            var color=solid.Color;return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }

    private async Task VerifyCustomBreadcrumbFocusAsync()
    {
        var path = new BleeckerBreadcrumb { Items = PackageBreadcrumb.Items, CollapsedAfter = 1, Width = 320,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            LinkTemplate = (DataTemplate)Root.Resources["ConsumerBreadcrumbLink"] };
        Root.Children.Add(path);
        try
        {
            await Task.Delay(100); path.ApplyTemplate(); Root.UpdateLayout();
            var panel = ((Grid)VisualTreeHelper.GetChild(path, 0)).Children.OfType<Panel>().Single();
            var expansion = panel.Children.OfType<Button>().Single();
            Require(expansion.Focus(FocusState.Keyboard), "Packaged custom path expansion refused focus.");
            ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(expansion).GetPattern(PatternInterface.Invoke)).Invoke();
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            while (panel.Children.Count != 7 && elapsed.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(16);
            Require(panel.Children.Count == 7, "Packaged custom path did not expand.");
            var host = panel.Children.OfType<ContentPresenter>().Skip(1).First();
            while (FocusManager.GetFocusedElement(Root.XamlRoot) is not Button { FocusState: FocusState.Keyboard, DataContext: BleeckerBreadcrumbItem item } ||
                !ReferenceEquals(item, PackageBreadcrumb.Items[1]))
            {
                Require(elapsed.Elapsed < TimeSpan.FromSeconds(2), "Packaged custom renderer did not receive keyboard focus.");
                Root.UpdateLayout(); await Task.Delay(16);
            }
            Require(ReferenceEquals(host.Content, PackageBreadcrumb.Items[1]),
                "Packaged custom renderer did not receive restored focus with its original consumer item.");
        }
        finally { Root.Children.Remove(path); }
    }

    private async Task VerifyAlertAsync()
    {
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnClosed(object? sender, EventArgs args) => closed.TrySetResult();
        PackageAlert.Closed += OnClosed;
        try
        {
            PackageAlert.IsOpen = true;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (!Presented(PackageAlert))
            {
                Require(watch.Elapsed < TimeSpan.FromSeconds(3), "Packaged alert did not present its native overlay.");
                Root.UpdateLayout(); await Task.Delay(16);
            }
            var template = (FrameworkElement)VisualTreeHelper.GetChild(PackageAlert, 0);
            var surface = (BleeckerOverlaySurface)template.FindName("PART_Surface");
            var text = (TextBlock)template.FindName("PART_Message");
            var close = (Button)template.FindName("PART_Close");
            Require(surface.ActualWidth == Math.Min(448, Root.XamlRoot.Size.Width - 32) && text.Text == PackageAlert.Message &&
                text.FontFamily.Source.Contains("EncodeSans-Medium.ttf") && new ButtonAutomationPeer(close).GetName() == "Close alert",
                "Packaged alert lost its resources, geometry, font or accessible close action.");
            ((IInvokeProvider)new ButtonAutomationPeer(close).GetPattern(PatternInterface.Invoke)).Invoke();
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(3));
            Require(!((Microsoft.UI.Xaml.Controls.Primitives.Popup)template.FindName("PART_Popup")).IsOpen, "Packaged close callback left its native popup open.");
            BleeckerAlertContainer.ShowAlert(Root.XamlRoot, "Independent shared fictional notice", BleeckerAlertType.Warning);
            watch.Restart();
            while (PackageAlertHost.Children.Count != 1 || !Presented((BleeckerAlert)PackageAlertHost.Children[0]))
            {
                Require(watch.Elapsed < TimeSpan.FromSeconds(3), "Packaged shared alert did not reach its native popup.");
                Root.UpdateLayout(); await Task.Delay(16);
            }
            var shared = (BleeckerAlert)PackageAlertHost.Children[0];
            Require(shared.Message == "Independent shared fictional notice" && shared.Type == BleeckerAlertType.Warning, "Packaged shared stream changed consumer data.");
            shared.Close(); watch.Restart();
            while (PackageAlertHost.Children.Count != 0)
            {
                Require(watch.Elapsed < TimeSpan.FromSeconds(3), "Packaged shared close did not remove its message.");
                await Task.Delay(16);
            }
        }
        finally { PackageAlert.Closed -= OnClosed; PackageAlert.Close(); }

        static bool Presented(BleeckerAlert alert)
        {
            if (!alert.IsLoaded || VisualTreeHelper.GetChildrenCount(alert) == 0) return false;
            var template = (FrameworkElement)VisualTreeHelper.GetChild(alert, 0);
            return template.FindName("PART_Popup") is Microsoft.UI.Xaml.Controls.Primitives.Popup { IsOpen: true } &&
                template.FindName("PART_MotionRoot") is FrameworkElement { Opacity: 1 };
        }
    }

    private async Task VerifyBrandAsync()
    {
        Require(File.Exists(Path.Combine(AppContext.BaseDirectory, "Orchard", "Assets", "Images", "gaulatti-logo.svg")), "Package omitted the pinned logo asset.");
        PackageBrand.ApplyTemplate(); Root.UpdateLayout();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var image = BrandDescendants<Image>(PackageBrand).Single();
        while (image.ActualWidth <= 0)
        {
            Require(watch.Elapsed < TimeSpan.FromSeconds(3), "Packaged brand logo did not load.");
            Root.UpdateLayout(); await Task.Delay(16);
        }
        var text = BrandDescendants<TextBlock>(PackageBrand).Single(item => item.Name == "PART_Name");
        var native = BrandDescendants<HyperlinkButton>(PackageBrand).Single();
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(native);
        Require(Math.Abs(image.ActualHeight - 32) < .01 && Math.Abs(image.ActualWidth - 32 * 1000 / 1289.11) < .05 &&
            text.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf") && peer.GetName() == "gaulatti Consumer studio" &&
            peer.GetAutomationControlType() == AutomationControlType.Hyperlink && native.NavigateUri is null,
            "Packaged brand lost its source asset, typography, native semantics or consumer route ownership.");
        var count = 0; string? href = null;
        void OnInvoked(object? sender, string route) { count++; href = route; }
        PackageBrand.Invoked += OnInvoked;
        try
        {
            ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();
            Require(count == 1 && href == "/consumer?keep=%2F#section", "Packaged brand invocation changed or duplicated the consumer route.");
        }
        finally { PackageBrand.Invoked -= OnInvoked; }
    }

    private static IEnumerable<T> BrandDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index); if (child is T match) yield return match;
            foreach (var nested in BrandDescendants<T>(child)) yield return nested;
        }
    }

    private void VerifyAttention()
    {
        PackageAttention.ApplyTemplate();PackageAttentionAction.ApplyTemplate();Root.UpdateLayout();
        var root=(FrameworkElement)VisualTreeHelper.GetChild(PackageAttention,0);
        var surface=(Border)root.FindName("PART_Surface");
        var actionRoot=(FrameworkElement)VisualTreeHelper.GetChild(PackageAttentionAction,0);
        var action=(Button)actionRoot.FindName("PART_Action");
        var actionSurface=(Border)actionRoot.FindName("PART_Surface");
        Require(PackageAttention.Background is ImageBrush {ImageSource:WriteableBitmap bitmap} && bitmap.PixelWidth==320 && bitmap.PixelHeight==120 &&
            surface.Padding==new Thickness(20) && surface.BorderThickness==new Thickness(1) && PackageAttention.CornerRadius==new CornerRadius(11),
            "Packaged Attention Surface lost its gradient, geometry or default resources.");
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(PackageAttention).GetAutomationControlType()==AutomationControlType.Group &&
            FrameworkElementAutomationPeer.CreatePeerForElement(PackageAttention).GetPattern(PatternInterface.Invoke) is null && !PackageAttention.IsTabStop,
            "Packaged non-button surface invented action semantics.");
        Require(new ButtonAutomationPeer(action).GetName()=="Independent surface action" && action.Visibility==Visibility.Visible && action.IsEnabled,
            "Packaged attention action lost its real button or consumer accessible name.");
        Require(action.ActualWidth==PackageAttentionAction.ActualWidth && action.ActualHeight==PackageAttentionAction.ActualHeight &&
            actionSurface.ActualWidth==PackageAttentionAction.ActualWidth && actionSurface.ActualHeight==PackageAttentionAction.ActualHeight,
            "Packaged attention paint or native action shrank inside the consumer-sized surface.");
        Result.Text="Ready";
        ((IInvokeProvider)new ButtonAutomationPeer(action).GetPattern(PatternInterface.Invoke)).Invoke();
        Require(Result.Text==$"Saved: {Input.Text}","Packaged attention native activation did not reach the consumer Click handler.");
        var prior=PackageAttention.Background;
        PackageAttention.Hue=-721;PackageAttention.Intensity=11;Root.UpdateLayout();
        Require(ReferenceEquals(prior,PackageAttention.Background) && prior is ImageBrush {ImageSource:not null},
            "Packaged attention recipe mutation replaced its instance brush or lost its renderer.");
        PackageAttention.Hue=210;PackageAttention.Intensity=5;
    }
    private void VerifyActivity()
    {
        PackageActivity.ApplyTemplate(); Root.UpdateLayout();
        var surface = (Border)VisualTreeHelper.GetChild(PackageActivity, 0);
        var grid = (Grid)surface.Child;
        var avatar = grid.Children.OfType<BleeckerAvatar>().Single();
        var textColumn = grid.Children.OfType<Grid>().Single();
        var text = textColumn.Children.OfType<Grid>().SelectMany(paragraph => paragraph.Children.OfType<TextBlock>()).ToArray();
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(PackageActivity);
        Require(avatar.AvatarSize == BleeckerAvatarSize.Small && avatar.ActualWidth == 32 && avatar.Fallback == PackageActivity.Init &&
            text[0].Text == PackageActivity.Title && text[1].Text == PackageActivity.Time &&
            text[0].FontFamily.Source.Contains("EncodeSans-Medium.ttf") && text[1].FontFamily.Source.Contains("LibreFranklin-Regular.ttf") &&
            surface.Padding == new Thickness(16) && surface.BorderThickness == new Thickness(0, 0, 0, 1),
            "Independent package lost activity strings, fonts, avatar or template geometry.");
        Require(peer.GetAutomationControlType() == AutomationControlType.Group && !peer.IsKeyboardFocusable() &&
            peer.GetPattern(PatternInterface.Invoke) is null && !PackageActivity.IsTabStop,
            "Independent package fabricated an activity action.");
        var original = PackageActivity.Title; PackageActivity.Title = ""; Root.UpdateLayout();
        Require(text[0].Visibility == Visibility.Collapsed && text[1].Text == PackageActivity.Time,
            "Packaged activity did not preserve an empty consumer paragraph.");
        PackageActivity.Title = original;
    }

    private void VerifyBauhaus()
    {
        PackageBauhaus.ApplyTemplate();Root.UpdateLayout();
        var root=(FrameworkElement)VisualTreeHelper.GetChild(PackageBauhaus,0);
        var image=(Image)root.FindName("PART_Image");var pattern=(Canvas)root.FindName("PART_Pattern");
        Require(ReferenceEquals(image.Source,PackageBauhaus.ImageSource)&&pattern.Children.Count==9&&PackageBauhaus.ActualWidth==192&&PackageBauhaus.ActualHeight==96,
            "Independent package lost Bauhaus source or geometry.");
        var peer=FrameworkElementAutomationPeer.CreatePeerForElement(PackageBauhaus);
        Require(!peer.IsControlElement()&&!peer.IsContentElement()&&!PackageBauhaus.IsHitTestVisible,"Packaged background lost decorative semantics.");
    }

    private async Task VerifyNotificationThemeAsync(FlyoutPresenter presenter,ElementTheme theme)
    {
        var colors=BleeckerTokens.Colors(theme==ElementTheme.Light?BleeckerColorScheme.Light:BleeckerColorScheme.Dark);
        var surface=BrandDescendants<BleeckerOverlaySurface>(presenter).Single();
        var items=(ItemsControl)PackageNotifications.FindName("NotificationItems");
        var title=BrandDescendants<TextBlock>(items).Single(text=>text.Text=="Local review"&&text.Visibility==Visibility.Visible);
        string Paint(Brush brush){var color=((SolidColorBrush)brush).Color;return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";}
        bool Matches()=>presenter.ActualTheme==theme&&Paint(surface.Background)==colors[theme==ElementTheme.Light?"card":"deepSea"]&&
            Paint(title.Foreground)==colors["textPrimary"];
        var elapsed=System.Diagnostics.Stopwatch.StartNew();while(!Matches()&&elapsed.Elapsed<TimeSpan.FromSeconds(1))await Task.Delay(16);
        Require(Matches(),"Packaged open notification surface/text did not follow its consumer owner's current theme.");
    }

    private static bool HasReferenceSpinner(DependencyObject owner)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++)
        {
            var child = VisualTreeHelper.GetChild(owner, i);
            if (child is Microsoft.UI.Xaml.Shapes.Path { Name: "PART_SpinnerPath" }) return true;
            if (HasReferenceSpinner(child)) return true;
        }
        return false;
    }

    private async Task CaptureAsync(ElementTheme theme)
    {
        var arguments = Environment.GetCommandLineArgs();
        var folder = Path.GetDirectoryName(arguments[Array.IndexOf(arguments, "--verify-output") + 1])!;
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(Root);
        Require(bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0, "Consumer rendered an empty frame.");
        var pixels = await bitmap.GetPixelsAsync();
        var bytes = new byte[checked((int)pixels.Length)];
        using (var reader = DataReader.FromBuffer(pixels)) reader.ReadBytes(bytes);
        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
            (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, bytes);
        await encoder.FlushAsync();
        using var outputReader = new DataReader(stream.GetInputStreamAt(0));
        await outputReader.LoadAsync(checked((uint)stream.Size));
        var output = new byte[checked((int)stream.Size)];
        outputReader.ReadBytes(output);
        await File.WriteAllBytesAsync(Path.Combine(folder, $"consumer-{theme.ToString().ToLowerInvariant()}.png"), output);
    }
}
