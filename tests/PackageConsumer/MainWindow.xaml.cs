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
        PackageBauhaus.ImageSource = new WriteableBitmap(4, 4);
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
                Require(PackageFieldLabel.FontFamily.Source.Contains("EncodeSans-Medium.ttf") && PackageFieldLabel.FontSize == 13 &&
                    !PackageFieldLabel.IsTabStop && ReferenceEquals(Microsoft.UI.Xaml.Automation.AutomationProperties.GetLabeledBy(Input), PackageFieldLabel) &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(PackageFieldLabel).GetName() == "Title", "Packaged field label lost typography or input association.");
                Require(PackageHelp.ActualWidth == 16 && PackageHelp.ActualHeight == 16 &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(PackageHelp).GetName() == PackageHelp.Text &&
                    BleeckerTooltipService.GetToolTip(PackageHelp)?.Placement == Microsoft.UI.Xaml.Controls.Primitives.PlacementMode.Bottom,
                    "Packaged field help lost its size, accessible name or bottom tooltip.");
                PackageMenu.ShowAt(MenuTrigger);
                await Task.Delay(200);
                var menuPresenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot).Select(popup => popup.Child).OfType<MenuFlyoutPresenter>().Single();
                Require(menuPresenter.Padding.Left == 6 && menuPresenter.CornerRadius.TopLeft == 7 &&
                    menuPresenter.FontFamily.Source.Contains("EncodeSans-Regular.ttf"), "Packaged menu lost its branded presenter.");
                var menuSurface = (BleeckerOverlaySurface)VisualTreeHelper.GetChild(menuPresenter, 0);
                var menuSurfaceRoot = (Grid)VisualTreeHelper.GetChild(menuSurface, 0);
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
                await VerifyCustomBreadcrumbFocusAsync();
                await VerifyAlertAsync();
                await VerifyBrandAsync();
                VerifyBauhaus();
                VerifyActivity();
                VerifyAttention();
                await VerifyChartThemeAsync(theme);
                await VerifyLineChartAsync();
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
